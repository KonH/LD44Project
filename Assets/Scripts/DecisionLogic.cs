﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DecisionLogic {
	readonly Messages _messages;
	readonly Environment _environment;
	readonly Parameters _parameters;
	readonly GameState _state;
	
	HashSet<Company> _bannedCompanies = new HashSet<Company>();
	HashSet<Company> _alreadyAppliedCompanies = new HashSet<Company>();
	(Company, Company.Position) _lastProposedPosition;
	
	public DecisionLogic(Messages messages, Environment environment, Parameters parameters, GameState state) {
		_messages = messages;
		_environment = environment;
		_parameters = parameters;
		_state = state;
	}

	public void BanCompany(Company company) {
		_bannedCompanies.Add(company);
	}

	public bool IsDecisionAvailable(DecisionId id) {
		switch ( id ) {
			case DecisionId.Work:          return (_state.WorkPlace != null);
			case DecisionId.WorkPromotion: return (_state.WorkPlace?.Times > _parameters.MinPromotionTimes) && (GetNextPosition() != null);
			case DecisionId.WorkRecommend: return (_state.WorkPlace?.Times > _parameters.MinRecommendTimes);
		}
		return true;
	}

	Company.Position GetNextPosition() {
		var position = _state.WorkPlace.Position;
		var positions = _state.WorkPlace.Company.Positions;
		var nextPositionIndex = (positions.IndexOf(position) + 1);
		return nextPositionIndex < positions.Count ? positions[nextPositionIndex] : null;
	}
	
	public void Apply(DecisionId id) {
		switch ( id ) {
			case DecisionId.PublishResume: 
				OnPublishResume();
				break;
			
			case DecisionId.Work:
				OnWork();
				break;
			
			case DecisionId.WorkPromotion:
				OnWorkPromotion();
				break;
			
			case DecisionId.WorkRecommend:
				_state.WorkPlace.Times = 0;
				break;
		}
	}

	void OnPublishResume() {
		var (company, positon) = FindSuitablePosition();
		NoticeAction act;
		if ( positon != null ) {
			var curPayment = (_state.WorkPlace != null) ? _state.GetPayment(_state.WorkPlace) : 0;
			var addCount = (_state.GetPayment(company, positon) - curPayment);
			var add = (addCount >= 0) ? "+" + addCount.ToString() : addCount.ToString();  
			act = new NoticeAction(
				_messages.WorkInvite.Format(company.Name, positon.Name, add),
				GameState.HighPriority,
				true,
				b => OnInviteConfirm(company, positon, b)
			);
		} else {
			act = new NoticeAction(_messages.NoWorkInvites, GameState.MiddlePriority);
		}
		_state.DelayNotice(DecisionId.PublishResume, act, TimeSpan.FromDays(3));
	}

	void OnWork() {
		_state.Inc(Trait.Money, _state.GetPayment(_state.WorkPlace));
		_state.WorkPlace.Times++;
		_state.WorkPlace.LastWorkDay = _state.Date;
		_state.EnqueNoticeOnce(new NoticeAction(_messages.WorkProgressNotice, GameState.HighPriority));
	}

	void OnWorkPromotion() {
		Message msg;
		var nextPosition = GetNextPosition();
		if ( IsApplyablePosition(nextPosition) ) {
			msg = _messages.PromotionOk.Format(nextPosition.Name);
			_state.WorkPlace = new GameState.WorkState {
				Company = _state.WorkPlace.Company,
				Position = nextPosition,
				LastWorkDay = _state.Date
			};
		} else {
			msg = _messages.PromotionNone;
			_state.WorkPlace.Times = 0;
		}
		_state.EnqueNotice(new NoticeAction(msg, GameState.HighPriority));
	}

	(Company, Company.Position) FindSuitablePosition() {
		if ( _state.Age > _parameters.MaxApplyAge ) {
			return (null, null);
		}
		var currentCompany = _state.WorkPlace?.Company;
		var allPositions = new List<(Company, Company.Position)>();
		foreach ( var company in _environment.Companies ) {
			if ( company == currentCompany ) {
				continue;
			}
			if ( _bannedCompanies.Contains(company) ) {
				continue;
			}
			foreach ( var pos in company.Positions) {
				allPositions.Add((company, pos));
			}
		}
		if ( allPositions.Count > 0 ) {
			if ( allPositions.Count > 1 ) {
				allPositions.Remove(_lastProposedPosition);
			}
			Debug.LogFormat(
				"All positions ({0}): {1}",
				allPositions.Count,
				string.Join(", ", allPositions.Select(p => $"({p.Item1.Name}, {p.Item2.Name})"))
			);
			var nonAppliedPositions = new List<(Company, Company.Position)>();
			foreach ( var pos in allPositions ) {
				var (company, _) = pos;
				if ( !_alreadyAppliedCompanies.Contains(company) ) {
					nonAppliedPositions.Add(pos);
				}
			}
			Debug.LogFormat(
				"Non-applied positions: ({0}): {1}",
				nonAppliedPositions.Count,
				string.Join(", ", nonAppliedPositions.Select(p => $"({p.Item1.Name}, {p.Item2.Name})"))
			);
			var positions = (nonAppliedPositions.Count > 0) ? nonAppliedPositions : allPositions;
			var applyablePositions = new List<(Company, Company.Position)>();
			foreach ( var pos in positions ) {
				var (_, position) = pos;
				if ( IsApplyablePosition(position) ) {
					applyablePositions.Add(pos);
				}
			}
			Debug.LogFormat(
				"Applicable positions ({0}): {1}",
				applyablePositions.Count,
				string.Join(", ", applyablePositions.Select(p => $"({p.Item1.Name}, {p.Item2.Name})"))
			);
			if ( applyablePositions.Count > 0 ) {
				positions = applyablePositions.OrderByDescending(p => _state.GetPayment(p.Item1, p.Item2)).Take(5).ToList();
			} else {
				positions = positions.OrderBy(p => _state.GetPayment(p.Item1, p.Item2)).Take(10).ToList();
			}
			Debug.LogFormat(
				"Positions to select ({0}): {1}",
				positions.Count,
				string.Join(", ", positions.Select(p => $"({p.Item1.Name}, {p.Item2.Name})"))
			);
			var result = positions[UnityEngine.Random.Range(0, positions.Count)];
			_lastProposedPosition = result;
			return result;
		}
		return (null, null);
	}

	void OnInviteConfirm(Company company, Company.Position position, bool confirm) {
		if ( !confirm ) {
			return;
		}
		var applied = IsApplyablePosition(position);
		var msg = (applied ? _messages.NewJob : _messages.InterviewFailed).Format(company.Name, position.Name);
		var delay = TimeSpan.FromDays(applied ? 1 : 3);
		_state.DelayNotice(
			DecisionId.PublishResume, 
			new NoticeAction(msg, GameState.MiddlePriority, callback: _ => {
				if ( applied ) {
					if ( _state.WorkPlace != null ) {
						_alreadyAppliedCompanies.Add(_state.WorkPlace.Company);
					}
					_state.WorkPlace = new GameState.WorkState {
						Company     = company,
						Position    = position,
						LastWorkDay = _state.Date
					};
				}
			}), 
			delay
		);
	}
	
	bool IsApplyablePosition(Company.Position position) {
		foreach ( var req in position.Requirements ) {
			if ( _state.Get(req.Trait) < req.Value ) {
				return false;
			}
		}
		return true;
	}
}
