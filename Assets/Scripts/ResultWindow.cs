using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ResultWindow : MonoBehaviour {
	public Transform ItemRoot;
	public GameObject ItemPrefab;
	public UnityEvent Show;
	public UnityEvent Hide;

	Action _callback;
	
	public void Init(HashSet<string> achievements, Action callback) {
		foreach ( var achivement in achievements ) {
			var item = Instantiate(ItemPrefab, ItemRoot);
			item.GetComponentInChildren<TMP_Text>().text = achivement;
		}
		Show.Invoke();
		_callback = callback;
	}

	public void OnHide() {
		if ( _callback == null ) {
			return;
		}
		Hide.Invoke();
		_callback.Invoke();
		_callback = null;
	}
}
