using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NoticeWindow : MonoBehaviour {
	public TMP_Text Header;
	public TMP_Text Content;
	public Button OkButton;
	public Button CancelButton;
	public UnityEvent Show;
	public UnityEvent Hide;

	Action<bool> _callback;
	
	public void Init(NoticeAction action) {
		Header.text = action.Title;
		Content.text = action.Content;
		_callback = action.Callback;
		CancelButton.gameObject.SetActive(action.Cancelable);
		Show.Invoke();
	}

	public void OnOkay() {
		if ( _callback == null ) {
			return;
		}
		_callback.Invoke(true);
		_callback = null;
		Hide.Invoke();
	}

	public void OnCancel() {
		if ( _callback == null ) {
			return;
		}
		_callback.Invoke(false);
		_callback = null;
		Hide.Invoke();
	}
}
