﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
	static readonly int StressProperty = Animator.StringToHash("Stress");
	static readonly int DiseaseProperty = Animator.StringToHash("Disease");
	static readonly int MadProperty = Animator.StringToHash("Mad");
	
	public DecisionTree DecisionTree;
	public Environment Environment;
	public Messages Messages;
	public Parameters Parameters;
	public Events Events;
	public MainUI MainUI;
	public NoticeWindow Notice;
	public DecideWindow Decide;
	public ResultWindow Result;
	public Animator StressAnim;
	public Animator DiseaseAnim;
	public Animator MadAnim;
	
	GameState _state = null;
	
	DecisionTree.Category _selectedCategory = null;

	void Start() {
		_state = new GameState(Messages, Environment, Parameters, Events, UpdateView);
		MainUI.UpdateState(_state.Date, _state.Money, initial: true);
	}

	void Update() {
		var normalizedStress = !_state.Finished ? (float)_state.Get(Trait.Stress) / Parameters.StressLimit : 0;
		StressAnim.SetFloat(StressProperty, normalizedStress);
		
		var normalizedDisease = !_state.Finished ? (float)_state.Get(Trait.Disease) / Parameters.DiseaseLimit : 0;
		DiseaseAnim.SetFloat(DiseaseProperty, normalizedDisease);
		
		var normalizedMad = !_state.Finished ? (float)_state.Get(Trait.Mad) / Parameters.MadLimit : 0;
		MadAnim.SetFloat(MadProperty, normalizedMad);
		
		if ( Notice.isActiveAndEnabled ) {
			return;
		}
		if ( TryShowNoticeWindow() ) {
			return;
		}
		if ( _state.Finished ) {
			if ( Result.isActiveAndEnabled ) {
				return;
			}
			if ( Decide.isActiveAndEnabled ) {
				Decide.Hide.Invoke();
			}
			Result.Init(_state.Achievements, () => { SceneManager.LoadScene(SceneManager.GetActiveScene().name); });
		}
		if ( Decide.isActiveAndEnabled ) {
			return;
		}
		ShowDecideWindow();
	}

	[ContextMenu("AddDay")]
	public void AddDay() {
		_state.UpdateTime(TimeSpan.FromDays(1), false, true);
	}
	
	[ContextMenu("AddMonth")]
	public void AdMonth() {
		_state.UpdateTime(TimeSpan.FromDays(31), false, true);
	}

	bool TryShowNoticeWindow() {
		var notices = _state.Notices;
		if ( notices.Count == 0 ) {
			return false;
		}
		TryResetDecideWindow();
		Notice.Init(notices[0]);
		notices.RemoveAt(0);
		return true;
	}

	void ShowDecideWindow() {
		Decide.Init(GetCurrentActions());
	}

	Dictionary<string, (Action, bool)> GetCurrentActions() {
		var isCategorySelected = (_selectedCategory != null);
		if ( isCategorySelected ) {
			return GetActionsForCategory(_selectedCategory);
		}
		return GetCategories();
	}

	Dictionary<string, (Action, bool)> GetCategories() {
		var categiries = DecisionTree.Categories;
		var result = new Dictionary<string, (Action, bool)>();
		foreach ( var cat in categiries ) {
			result.Add(cat.Name, (() => _selectedCategory = cat, true));
		}
		return result;
	}
	
	public Dictionary<string, (Action, bool)> GetActionsForCategory(DecisionTree.Category category) {
		var decisions = category.Decisions;
		var result    = new Dictionary<string, (Action, bool)>();
		foreach ( var decision in decisions ) {
			if ( _state.IsDecisionAvailable(decision) ) {
				var suffix = "";
				var money = decision.Changes.Find(t => t.Trait == Trait.Money)?.Value;
				if ( decision.Id == DecisionId.Work ) {
					money = _state.GetPayment(_state.WorkPlace);
				}
				if ( money.HasValue ) {
					suffix += $" ({money.Value}$)";
				}
				result.Add(decision.Name + suffix, (() => ApplyDecision(decision), _state.IsDecisionActive(decision)));
			}
		}
		result.Add("Back", (TryResetDecideWindow, true));
		return result;
	}

	void ApplyDecision(DecisionTree.Decision decision) {
		TryResetDecideWindow();
		_state.ApplyDecision(decision, true);
	}

	void TryResetDecideWindow() {
		_selectedCategory = null;
		if ( Decide.isActiveAndEnabled ) {
			Decide.Hide.Invoke();
		}
	}
	
	void UpdateView() {
		Debug.Log("UpdateView");
		MainUI.UpdateState(_state.Date, _state.Money);
	}
}
