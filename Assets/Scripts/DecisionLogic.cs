using System;
using System.Collections.Generic;

public class DecisionLogic {
	readonly Messages _messages;
	readonly Environment _environment;
	readonly Parameters _parameters;
	readonly GameState _state;
	
	HashSet<Company> _bannedCompanies = new HashSet<Company>();
	
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
			case DecisionId.WorkPromotion: return (_state.WorkPlace?.Days > _parameters.MinPromotionDays) && (GetNextPosition() != null);
			case DecisionId.WorkRecommend: return (_state.WorkPlace?.Days > _parameters.MinRecommendDays);
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
		}
	}

	void OnPublishResume() {
		var (company, positon) = FindSuitablePosition();
		NoticeAction act;
		if ( positon != null ) {
			var curPayment = (_state.WorkPlace != null) ? _state.WorkPlace.Position.Payment : 0;
			var addCount = (positon.Payment - curPayment);
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
		var position = _state.WorkPlace.Position;
		_state.Inc(Trait.Money, position.Payment);
		_state.WorkPlace.Days++;
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
			_state.WorkPlace.Days = 0;
		}
		_state.EnqueNotice(new NoticeAction(msg, GameState.HighPriority));
	}

	(Company, Company.Position) FindSuitablePosition() {
		var currentCompany = _state.WorkPlace?.Company;
		var positions = new List<(Company, Company.Position)>();
		foreach ( var company in _environment.Companies ) {
			if ( company == currentCompany ) {
				continue;
			}
			if ( _bannedCompanies.Contains(company) ) {
				continue;
			}
			for ( var i = company.Positions.Count - 1; i >= 0; i-- ) {
				var position = company.Positions[i];
				var satisfied = true;
				foreach ( var precond in position.Preconditions ) {
					if ( _state.Get(precond.Trait) < precond.Value ) {
						satisfied = false;
					}
				}
				if ( satisfied ) {
					positions.Add((company, position));
					break;
				}
			}
		}
		if ( positions.Count > 0 ) {
			return positions[UnityEngine.Random.Range(0, positions.Count)];
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
