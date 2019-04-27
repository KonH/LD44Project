using System;
using TMPro;
using UnityEngine;

public class MainUI : MonoBehaviour {
	public TMP_Text DateText;
	public TMP_Text MoneyText;

	public void UpdateState(DateTime dt, int money) {
		DateText.text = $"<b>Date:</b> {dt.ToString()}";
		MoneyText.text = $"{money.ToString()}$";
	}
}
