using System;
using System.Collections.Generic;
using UnityEngine;

public class GameState {
	class DelayedNotice {
		public DecisionId Id;
		public DateTime Time;
		public NoticeAction Action;
	}

	public class WorkState {
		public Company Company;
		public Company.Position Position;
		public int Days;
	}
	
	static HashSet<string> _oneTimeNotices = new HashSet<string>();
	
	public DateTime Date;
	public bool Finished;
	public int Money => Get(Trait.Money);
	public Queue<NoticeAction> Notices = new Queue<NoticeAction>();
	public Dictionary<Trait, int> Traits = new Dictionary<Trait, int>();
	public WorkState WorkPlace = null;
	public HashSet<string> Achievements = new HashSet<string>();

	readonly Messages _messages;
	readonly Parameters _parameters;
	readonly DecisionLogic _decisionLogic;

	DateTime _startDate;
	DateTime _lastPayDate;
	List<DelayedNotice> _delayedNotices = new List<DelayedNotice>();

	public GameState(Messages messges, Environment environment, Parameters parameters) {
		_messages = messges;
		_parameters = parameters;
		_decisionLogic = new DecisionLogic(_messages, environment, _parameters, this);

		_startDate = DateTime.MinValue.AddDays(_parameters.StartDay);
		Date = _startDate;
		Traits[Trait.Money] = _parameters.StartMoney;

		_lastPayDate = Date;
		
		var startAct = new NoticeAction(_messages.Welcome);
		EnqueNoticeOnce(startAct);
	}
	
	public void ApplyDecision(DecisionTree.Decision decision) {
		foreach ( var change in decision.Changes ) {
			Inc(change.Trait, change.Value);
		}
		_decisionLogic.Apply(decision.Id);
		UpdateTime(TimeSpan.FromDays(decision.Days));
		Debug.LogFormat("Applied decision: '{0}', {1}\n{2}", decision.Name, decision.Id, this);
	}

	public void UpdateTime(TimeSpan span) {
		Date = Date.Add(span);
		UpdatePayment();
		UpdateTraits();
		UpdateAchievements();
		UpdateNotices();
	}

	void UpdatePayment() {
		while ((Date - _lastPayDate).TotalDays > 31 ) {
			Inc(Trait.Money, _parameters.MonthMoney);
			_lastPayDate = _lastPayDate.AddDays(31);
			EnqueNoticeOnce(new NoticeAction(_messages.MonthPayment));
		}
	}

	void UpdateTraits() {
		if ( Get(Trait.Money) == 0 ) {
			EnqueNoticeOnce(new NoticeAction(_messages.LowMoney));
			Inc(Trait.Stress, _parameters.LowMoneyStress);
		}
		if ( Get(Trait.Stress) > _parameters.StressLimit * 0.6f ) {
			EnqueNoticeOnce(new NoticeAction(_messages.StressWarning));
		}
		if ( Get(Trait.Stress) > _parameters.StressLimit ) {
			EnqueNotice(new NoticeAction(_messages.HeartAttack));
			Finish();
		}
	}

	void UpdateAchievements() {
		if ( WorkPlace != null ) {
			AddAchievement($"{WorkPlace.Position.Name} at '{WorkPlace.Company.Name}'");
		}
	}
	
	void UpdateNotices() {
		var usedNotices = new List<DelayedNotice>();
		foreach ( var notice in _delayedNotices ) {
			if ( notice.Time <= Date ) {
				EnqueNotice(notice.Action);
				usedNotices.Add(notice);
			}
		}
		foreach ( var used in usedNotices ) {
			_delayedNotices.Remove(used);
		}
	}

	void Finish() {
		var age = _parameters.StartAge + (Date - _startDate).TotalDays / 365;
		age = Math.Round(age);
		var msg = _messages.Finish.Format(age);
		EnqueNotice(new NoticeAction(msg));
		Finished = true;
		if ( Achievements.Count == 0 ) {
			Achievements.Add("Nothing");
		}
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
		Notices.Enqueue(act);
	}
	
	public void EnqueNoticeOnce(NoticeAction act) {
		if ( _oneTimeNotices.Contains(act.Title) ) {
			return;
		}
		_oneTimeNotices.Add(act.Title);
		Notices.Enqueue(act);
	}

	public void DelayNotice(DecisionId id, NoticeAction act, TimeSpan span) {
		_delayedNotices.RemoveAll(n => n.Id == id);
		_delayedNotices.Add(new DelayedNotice { Id = id, Time = Date.Add(span), Action = act });
	}

	public bool IsDecisionAvailable(DecisionTree.Decision decision) {
		if ( _delayedNotices.Find(n => n.Id == decision.Id) != null ) {
			return false;
		}
		foreach ( var ch in decision.Changes ) {
			if ( (ch.Trait == Trait.Money) && ((Money + ch.Value) < 0) ) {
				return false;
			}
		}
		return _decisionLogic.IsDecisionAvailable(decision.Id);
	}

	void AddAchievement(string value) {
		Achievements.Add(value);
	}
	
	public override string ToString() {
		var traits = new List<string>(Traits.Count);
		foreach ( var t in Traits ) {
			traits.Add($"{t.Key} = {t.Value}");
		}
		return string.Format(
			"Date: {0} Traits: {1}",
			Date, string.Join(", ", traits)
		);
	}
}
