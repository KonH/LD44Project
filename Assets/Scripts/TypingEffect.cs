using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TypingEffect : MonoBehaviour {
	TMP_Text _text;
	string _fullLine;

	public void SetupText(string text) {
		_text = GetComponent<TMP_Text>();
		_text.text = "";
		_fullLine = text;
		StartCoroutine(WriteText());
	}

	IEnumerator WriteText() {
		while ( _text.text != _fullLine ) {
			var nextChar = _fullLine[_text.text.Length];
			_text.text += nextChar;
			yield return new WaitForSeconds(0.025f);
		}
	}
}
