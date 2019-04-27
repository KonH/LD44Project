using System;

[Serializable]
public class RandomEvent {
	public DecisionTree.Decision Decision;
	public string Title;
	public string Content;
	public bool Cancelable;
}
