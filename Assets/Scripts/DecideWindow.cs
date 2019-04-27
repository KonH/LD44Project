using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DecideWindow : MonoBehaviour {
	public Transform ActionRoot;
	public Button ActionPrefab;
	public UnityEvent Show;
	public UnityEvent Hide;
	
	List<Button> _buttons = new List<Button>();
	
	public void Init(Dictionary<string, Action> actions) {
		while ( _buttons.Count < actions.Count ) {
			var button = Instantiate(ActionPrefab, ActionRoot);
			_buttons.Add(button);
		}
		var index = 0;
		foreach ( var action in actions ) {
			var button = _buttons[index];
			button.gameObject.SetActive(true);
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(() => {
				action.Value();
				ClearListeners();
				Hide.Invoke();
			});
			button.GetComponentInChildren<TMP_Text>().text = action.Key;
			index++;
		}
		for ( var i = index; i < _buttons.Count; i++ ) {
			_buttons[i].gameObject.SetActive(false);
		}
		Show.Invoke();
	}

	void ClearListeners() {
		foreach ( var button in _buttons ) {
			button.onClick.RemoveAllListeners();
		}
	}
}
