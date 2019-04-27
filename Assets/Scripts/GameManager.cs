using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
	public DecisionTree DecisionTree;
	public Environment Environment;
	public Messages Messages;
	public Parameters Parameters;
	public MainUI MainUI;
	public NoticeWindow Notice;
	public DecideWindow Decide;
	public ResultWindow Result;
	
	GameState _state = null;
	
	DecisionTree.Category _selectedCategory = null;

	void Start() {
		_state = new GameState(Messages, Environment, Parameters);
		UpdateView();
	}

	void Update() {
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
		_state.UpdateTime(TimeSpan.FromDays(1));
		UpdateView();
	}
	
	[ContextMenu("AddMonth")]
	public void AdMonth() {
		_state.UpdateTime(TimeSpan.FromDays(31));
		UpdateView();
	}

	bool TryShowNoticeWindow() {
		var notices = _state.Notices;
		if ( notices.Count == 0 ) {
			return false;
		}
		TryResetDecideWindow();
		Notice.Init(notices.Dequeue());
		UpdateView();
		return true;
	}

	void ShowDecideWindow() {
		Decide.Init(GetCurrentActions());
	}

	Dictionary<string, Action> GetCurrentActions() {
		var isCategorySelected = (_selectedCategory != null);
		if ( isCategorySelected ) {
			return GetActionsForCategory(_selectedCategory);
		}
		return GetCategories();
	}

	Dictionary<string, Action> GetCategories() {
		var categiries = DecisionTree.Categories;
		var result = new Dictionary<string, Action>();
		foreach ( var cat in categiries ) {
			result.Add(cat.Name, () => _selectedCategory = cat);
		}
		return result;
	}
	
	public Dictionary<string, Action> GetActionsForCategory(DecisionTree.Category category) {
		var decisions = category.Decisions;
		var result    = new Dictionary<string, Action>();
		foreach ( var decision in decisions ) {
			if ( _state.IsDecisionAvailable(decision) ) {
				result.Add(decision.Name, () => ApplyDecision(decision));
			}
		}
		return result;
	}

	void ApplyDecision(DecisionTree.Decision decision) {
		TryResetDecideWindow();
		_state.ApplyDecision(decision);
		UpdateView();
	}

	void TryResetDecideWindow() {
		_selectedCategory = null;
		if ( Decide.isActiveAndEnabled ) {
			Decide.Hide.Invoke();
		}
	}
	
	void UpdateView() {
		MainUI.UpdateState(_state.Date, _state.Money);
	}
}
