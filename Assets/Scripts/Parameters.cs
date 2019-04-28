using UnityEngine;

[CreateAssetMenu]
public class Parameters : ScriptableObject {
	public int StartAge;
	public int StartDay;
	public int StartMoney;
	public int MonthMoney;
	public int StressLimit;
	public int DiseaseLimit;
	public int MadLimit;
	public int LowMoneyStress;
	public int MinPromotionTimes;
	public int MinRecommendTimes;
	public float RanomEventChance;
	public int MaxSkipWorkDays;
	public float TimeScale;
	public int MaxApplyAge;
	public int MinDeathChanceAge;
	public float DeathChance;
	public int InflateDays;
	public int InflateValue;
}
