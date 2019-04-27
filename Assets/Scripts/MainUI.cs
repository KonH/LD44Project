using System;
using TMPro;
using UnityEngine;

public class MainUI : MonoBehaviour {
	static readonly int Appear = Animator.StringToHash("Appear");
	
	public TMP_Text DateText;
	public TMP_Text MoneyText;
	public TMP_Text DateEffect;
	public Animator DateAnimator;
	public TMP_Text MoneyIncomeEffect;
	public Animator MoneyIncomeAnimator;
	public TMP_Text MoneyOutcomeEffect;
	public Animator MoneyOutcomeAnimator;

	int _lastMoney;
	DateTime _lastDate;

	void Awake() {
		DateEffect.text = "";
		MoneyIncomeEffect.text = "";
		MoneyOutcomeEffect.text = "";
	}

	public void UpdateState(DateTime dt, int money, bool initial = false) {
		DateText.text = $"<b>Date:</b> {dt.ToString()}";
		MoneyText.text = $"{money.ToString()}$";
		if ( !initial ) {
			var dateDiff = Math.Round((dt - _lastDate).TotalDays, 2);
			if ( dateDiff > 0.01 ) {
				DateEffect.text = $"+{dateDiff.ToString()} Day";
				DateAnimator.SetTrigger(Appear);
			}
			var moneyDiff = money - _lastMoney;
			if ( moneyDiff == 0 ) {
				// Nothing
			} else if ( moneyDiff > 0 ) {
				MoneyIncomeEffect.text = $"+{moneyDiff.ToString()}$";
				MoneyIncomeAnimator.SetTrigger(Appear);
			} else {
				MoneyOutcomeEffect.text = $"{moneyDiff.ToString()}$";
				MoneyOutcomeAnimator.SetTrigger(Appear);
			}
		}
		_lastDate  = dt;
		_lastMoney = money;
	}
}
