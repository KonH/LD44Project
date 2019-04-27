using System;

public class NoticeAction {
	public string Title;
	public string Content;
	public int Priority;
	public bool Cancelable;
	public Action<bool> Callback;
	
	public NoticeAction(Message message, int priority, bool cancelable = false, Action<bool> callback = null) {
		Title = message.Title;
		Content = message.Content;
		Priority = priority;
		Cancelable = cancelable;
		Callback = callback ?? ((b) => {});
	}
}