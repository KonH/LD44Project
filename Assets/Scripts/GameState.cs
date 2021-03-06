﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

public class GameState {
	public const int LowPriority = 4;
	public const int MiddlePriority = 2;
	public const int HighPriority = 0;
	
	class DelayedNotice {
		public DecisionId Id;
		public DateTime Time;
		public NoticeAction Action;
	}

	public class WorkState {
		public Company Company;
		public Company.Position Position;
		public int Times;
		public DateTime LastWorkDay;
	}
	
	static HashSet<string> _oneTimeNotices = new HashSet<string>();
	
	public DateTime Date;
	public bool Finished;
	public int Money => Get(Trait.Money);
	public List<NoticeAction> Notices = new List<NoticeAction>();
	public Dictionary<Trait, int> Traits = new Dictionary<Trait, int>();
	public WorkState WorkPlace = null;
	public List<string> Achievements = new List<string>();
	public Dictionary<string, int> SalaryInflation = new Dictionary<string, int>();

	public int Age {
		get {
			var age = _parameters.StartAge + (Date - _startDate).TotalDays / 365;
			return (int)Math.Round(age);
		}
	}

	readonly Messages _messages;
	readonly Environment _environment;
	readonly Parameters _parameters;
	readonly Events _events;
	readonly DecisionLogic _decisionLogic;
	readonly Action _onUpdated;

	DateTime _startDate;
	DateTime _lastPayDate;
	DateTime _lastInflateDate;
	List<DelayedNotice> _delayedNotices = new List<DelayedNotice>();
	List<RandomEvent> _usedEvents = new List<RandomEvent>();

	public GameState(Messages messges, Environment environment, Parameters parameters, Events events, Action onUpdated) {
		_messages = messges;
		_environment = environment;
		_parameters = parameters;
		_events = events;
		_onUpdated = onUpdated;
		
		_decisionLogic = new DecisionLogic(_messages, environment, _parameters, this);

		_startDate = DateTime.MinValue.AddDays(_parameters.StartDay);
		Date = _startDate;
		Traits[Trait.Money] = _parameters.StartMoney;

		_lastPayDate = Date;
		_lastInflateDate = Date;
		
		var startAct = new NoticeAction(_messages.Welcome, HighPriority);
		EnqueNoticeOnce(startAct);

		Analytics.enabled = !Application.isEditor;
		AnalyticsEvent.GameStart();
	}

	public int GetPayment(WorkState state) {
		return GetPayment(state.Company, state.Position);
	}
	
	public int GetPayment(Company company, Company.Position position) {
		if ( !SalaryInflation.TryGetValue(company.Name, out var inflation) ) {
			inflation = 0;
		}
		Debug.LogFormat("GetPayment({0}, {1}) = {2} + {3}", company.Name, position.Name, position.Payment, inflation);
		return position.Payment + inflation;
	}
	
	public void ApplyDecision(DecisionTree.Decision decision, bool fromDecide) {
		foreach ( var change in decision.Changes ) {
			Inc(change.Trait, change.Value);
		}
		_decisionLogic.Apply(decision.Id);
		UpdateTime(TimeSpan.FromDays(decision.Days), decision.Scaled, fromDecide);
		Debug.LogFormat("Applied decision: '{0}', {1}\n{2}", decision.Name, decision.Id, this);
	}

	public void UpdateTime(TimeSpan span, bool scaled, bool fromDecide) {
		var days = span.TotalDays;
		if ( scaled ) {
			days *= _parameters.TimeScale;
		}
		Date = Date.AddDays(days);
		UpdatePayment();
		UpdateJob();
		UpdateTraits();
		if ( fromDecide ) {
			UpdateEvents();
		}
		UpdateAchievements();
		UpdateNotices();
		UpdateInflation();
		_onUpdated.Invoke();
	}

	void UpdatePayment() {
		while ((Date - _lastPayDate).TotalDays > 31 ) {
			Inc(Trait.Money, _parameters.MonthMoney);
			_lastPayDate = _lastPayDate.AddDays(31);
			EnqueNoticeOnce(new NoticeAction(_messages.MonthPayment.Format(_parameters.MonthMoney.ToString()), HighPriority));
		}
	}

	void UpdateJob() {
		if ( WorkPlace != null ) {
			var days = (Date - WorkPlace.LastWorkDay).TotalDays;
			if ( days > _parameters.MaxSkipWorkDays * _parameters.TimeScale ) {
				_decisionLogic.BanCompany(WorkPlace.Company);
				AddAchievement($"Fired from '{WorkPlace.Company.Name}'");
				WorkPlace = null;
				EnqueNotice(new NoticeAction(_messages.LostJob, HighPriority));
				Inc(Trait.BadWorker, 1);
			}
		}
	}

	void UpdateTraits() {
		if ( Get(Trait.Money) == 0 ) {
			EnqueNoticeOnce(new NoticeAction(_messages.LowMoney, HighPriority));
			Inc(Trait.Stress, _parameters.LowMoneyStress);
		}
		if ( Get(Trait.Stress) > _parameters.StressLimit * 0.6f ) {
			EnqueNoticeOnce(new NoticeAction(_messages.StressWarning, HighPriority));
		}
		if ( Get(Trait.Stress) > _parameters.StressLimit ) {
			AddAchievement("Weak heart");
			Finish(_messages.HeartAttack, Trait.Stress.ToString());
			return;
		}
		if ( Get(Trait.Disease) > _parameters.DiseaseLimit * 0.6f ) {
			EnqueNoticeOnce(new NoticeAction(_messages.DiseaseWarning, HighPriority));
		}
		if ( Get(Trait.Disease) > _parameters.DiseaseLimit ) {
			AddAchievement("Bad health");
			Finish(_messages.DiseaseDeath, Trait.Disease.ToString());
			return;
		}
		if ( Get(Trait.Mad) > _parameters.MadLimit * 0.6f ) {
			EnqueNoticeOnce(new NoticeAction(_messages.MadWarning, HighPriority));
		}
		if ( Get(Trait.Mad) > _parameters.MadLimit ) {
			AddAchievement("Psycho");
			Finish(_messages.MadDeath, Trait.Mad.ToString());
			return;
		}
		if ( Age > _parameters.MinDeathChanceAge ) {
			if ( UnityEngine.Random.value < _parameters.DeathChance ) {
				AddAchievement("Long life");
				Finish(_messages.NaturalDeath, "NaturalDeath");
			}
		}
	}

	void UpdateEvents() {
		if ( UnityEngine.Random.value > _parameters.RanomEventChance ) {
			return;
		}
		var availableEvents = GetAvailableEvents();
		if ( availableEvents.Count > 0 ) {
			var ev = availableEvents[UnityEngine.Random.Range(0, availableEvents.Count)];
			_usedEvents.Add(ev);
			var msg = ev.EventMessage;
			var act = new NoticeAction(msg, LowPriority, ev.Cancelable, ok => {
				if ( ok ) {
					if ( !string.IsNullOrWhiteSpace(ev.OkMessage.Title) ) {
						EnqueNotice(new NoticeAction(ev.OkMessage, LowPriority));
					}
					ApplyDecision(ev.Decision, false);
				} else {
					if ( !string.IsNullOrWhiteSpace(ev.CancelMessage.Title) ) {
						EnqueNotice(new NoticeAction(ev.CancelMessage, LowPriority));
					}
				}
			});
			EnqueNotice(act);
		}
	}

	List<RandomEvent> GetAvailableEvents() {
		var result = new List<RandomEvent>();
		foreach ( var ev in _events.RandomEvents ) {
			if ( _usedEvents.Contains(ev) ) {
				continue;
			}
			var dec = ev.Decision;
			if ( IsDecisionAvailable(dec) && IsDecisionActive(dec) ) {
				result.Add(ev);
			}
		}
		return result;
	}

	void UpdateAchievements() {
		if ( WorkPlace != null ) {
			AddAchievement($"{WorkPlace.Position.Name} at '{WorkPlace.Company.Name}'");
		}
		if ( Get(Trait.Walker) > 50 ) {
			AddAchievement("Inborn walker");
		}
		if ( Get(Trait.DishWasher) > 25 ) {
			AddAchievement("Professional dish washer");
		}
		if ( Get(Trait.Bodybuilder) > 10 ) {
			AddAchievement("Bodybuilder");
		}
		if ( Get(Trait.Alcohol) > 25 ) {
			AddAchievement("Alcoholic");
		}
		if ( Get(Trait.Freelance) > 25 ) {
			AddAchievement("Freelancer");
		}
		if ( Get(Trait.GameJammer) > 15 ) {
			AddAchievement("Game Jam enthusiast");
		}
		if ( Get(Trait.OpenSource) > 10 ) {
			AddAchievement("Open Source enthusiast");
		}
		if ( Get(Trait.PopularPerson) > 15 ) {
			AddAchievement("Popular person");
		}
		if ( Get(Trait.Skill) > 200 ) {
			AddAchievement("Legendary Developer");
		}
		if ( Get(Trait.BadWorker) > 2 ) {
			AddAchievement("Bad Worker");
		}

		if ( Get(Trait.Theory) > 30 ) {
			AddAchievement("Computer Science Expert");
		}
		if ( Get(Trait.Resume) > 30 ) {
			AddAchievement("Resume Writer");
		}
		if ( Get(Trait.Talking) > 30 ) {
			AddAchievement("Interview Hacker");
		}

		if ( Get(Trait.Database) > 50 ) {
			AddAchievement("Database Expert");
		}
		if ( Get(Trait.MachineLearning) > 50 ) {
			AddAchievement("Machine Learning Expert");
		}
		if ( Get(Trait.BigData) > 50 ) {
			AddAchievement("Big Data Expert");
		}
		if ( Get(Trait.Cryptography) > 50 ) {
			AddAchievement("Cryptography Expert");
		}
		if ( Get(Trait.Mobile) > 50 ) {
			AddAchievement("Mobile Expert");
		}
		if ( Get(Trait.Assembler) > 50 ) {
			AddAchievement("Assembler Expert");
		}
		if ( Get(Trait.GameDev) > 50 ) {
			AddAchievement("GameDev Expert");
		}
		if ( Get(Trait.Graphics) > 50 ) { 
			AddAchievement("Graphics Expert");
		}
		if ( Get(Trait.Engine) > 50 ) {
			AddAchievement("Engine Expert");
		}
		if ( Get(Trait.DataScience) > 50 ) {
			AddAchievement("Data Science Expert");
		}
		if ( Get(Trait.Frontend) > 50 ) {
			AddAchievement("Frontend Expert");
		}
	}
	
	void UpdateNotices() {
		var usedNotices = new List<DelayedNotice>();
		foreach ( var notice in _delayedNotices ) {
			if ( Date >= notice.Time ) {
				EnqueNotice(notice.Action);
				usedNotices.Add(notice);
			}
		}
		foreach ( var used in usedNotices ) {
			_delayedNotices.Remove(used);
		}
	}

	void UpdateInflation() {
		var curCompany = WorkPlace?.Company;
		while ((Date - _lastInflateDate).TotalDays > _parameters.InflateDays ) {
			_lastInflateDate = _lastInflateDate.AddDays(_parameters.InflateDays);
			foreach ( var company in _environment.Companies ) {
				if ( company == curCompany ) {
					continue;
				}
				if ( !SalaryInflation.TryGetValue(company.Name, out var initial) ) {
					initial = 0;
				}
				SalaryInflation[company.Name] = initial + _parameters.InflateValue;
			}
		}
	}

	void Finish(Message explainMessage, string reason) {
		Notices.Clear();
		_delayedNotices.Clear();
		EnqueNotice(new NoticeAction(explainMessage, HighPriority));
		var msg = _messages.Finish.Format(Age);
		EnqueNotice(new NoticeAction(msg, HighPriority));
		Finished = true;
		if ( Achievements.Count == 0 ) {
			Achievements.Add("Nothing");
		}
		_onUpdated.Invoke();

		AnalyticsEvent.GameOver(reason);
	}

	public int Get(Trait trait) {
		if ( Traits.TryGetValue(trait, out var value) ) {
			return value;
		}
		return 0;
	}

	public void Inc(Trait trait, int inc) {
		var newValue = Get(trait) + inc;
		if ( newValue < 0 ) {
			newValue = 0;
		}
		Traits[trait] = newValue;
		Debug.LogFormat("Traits[{0}] + {1} = {2}", trait, inc, newValue);
	}

	public void EnqueNotice(NoticeAction act) {
		Notices.Add(act);
		Notices.Sort((a, b) => a.Priority.CompareTo(b.Priority));
	}
	
	public void EnqueNoticeOnce(NoticeAction act) {
		if ( _oneTimeNotices.Contains(act.Title) ) {
			return;
		}
		_oneTimeNotices.Add(act.Title);
		EnqueNotice(act);
	}

	public void DelayNotice(DecisionId id, NoticeAction act, TimeSpan span) {
		_delayedNotices.RemoveAll(n => n.Id == id);
		_delayedNotices.Add(new DelayedNotice { Id = id, Time = Date.Add(span), Action = act });
	}

	public bool IsDecisionAvailable(DecisionTree.Decision decision) {
		if ( _delayedNotices.Find(n => n.Id == decision.Id) != null ) {
			return false;
		}
		foreach ( var ch in decision.Min ) {
			if ( Get(ch.Trait) < ch.Value ) {
				return false;
			}
		}
		foreach ( var ch in decision.Max ) {
			if ( Get(ch.Trait) >= ch.Value ) {
				return false;
			}
		}
		return _decisionLogic.IsDecisionAvailable(decision.Id);
	}

	public bool IsDecisionActive(DecisionTree.Decision decision) {
		foreach ( var ch in decision.Changes ) {
			if ( (ch.Trait == Trait.Money) && ((Money + ch.Value) < 0) ) {
				return false;
			}
		}
		return true;
	}

	void AddAchievement(string value) {
		if ( !Achievements.Contains(value) ) {
			AnalyticsEvent.AchievementUnlocked(value);
			Achievements.Add(value);
		}
	}
	
	public override string ToString() {
		var traits = new List<string>(Traits.Count);
		foreach ( var t in Traits ) {
			traits.Add($"{t.Key} = {t.Value}");
		}
		return string.Format(
			"Date: {0} Age: {1} Traits: {2}",
			Date, Age, string.Join(", ", traits)
		);
	}
}
