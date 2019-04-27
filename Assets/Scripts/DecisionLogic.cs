using System;
using System.Collections.Generic;

public class DecisionLogic {
	readonly Messages _messages;
	readonly Environment _environment;
	readonly GameState _state;
	
	public DecisionLogic(Messages messages, Environment environment, GameState state) {
		_messages = messages;
		_environment = environment;
		_state = state;
	}

	public void Apply(DecisionId id) {
		switch ( id ) {
			case DecisionId.PublishResume: 
				OnPublishResume();
				break;
			case DecisionId.Work:
				OnWork();
				break;
		}
	}

	void OnPublishResume() {
		var (company, positon) = FindSuitablePosition();
		NoticeAction act;
		if ( positon != null ) {
			act = new NoticeAction(_messages.WorkInvite.Format(company.Name, positon.Name), true, b => OnInviteConfirm(company, positon, b));
		} else {
			act = new NoticeAction(_messages.NoWorkInvites);
		}
		_state.DelayNotice(DecisionId.PublishResume, act, TimeSpan.FromDays(3));
	}

	void OnWork() {
		var (_, position) = _state.WorkPlace;
		_state.Inc(Trait.Money, position.Payment);
	}

	(Company, Company.Position) FindSuitablePosition() {
		var (currentCompany, _) = _state.WorkPlace;
		var positions = new List<(Company, Company.Position)>();
		foreach ( var company in _environment.Companies ) {
			if ( company == currentCompany ) {
				continue;
			}
			foreach ( var position in company.Positions ) {
				var satisfied = true;
				foreach ( var precond in position.Preconditions ) {
					if ( _state.Get(precond.Trait) < precond.Value ) {
						satisfied = false;
					}
				}
				if ( satisfied ) {
					positions.Add((company, position));
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
		if ( applied ) {
			_state.WorkPlace = (company, position);
		}
		var msg = (applied ? _messages.NewJob : _messages.InterviewFailed).Format(company.Name, position.Name);
		var delay = TimeSpan.FromDays(applied ? 1 : 3);
		_state.DelayNotice(DecisionId.PublishResume, new NoticeAction(msg), delay);
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
