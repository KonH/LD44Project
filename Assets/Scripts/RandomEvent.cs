using System;

[Serializable]
public class RandomEvent {
	public string Name;
	public DecisionTree.Decision Decision;
	public Message EventMessage;
	public Message OkMessage;
	public Message CancelMessage;
	public bool Cancelable;
}
