using System;

public class NoticeAction {
	public string Title;
	public string Content;
	public bool Cancelable;
	public Action<bool> Callback;
	
	public NoticeAction(Message message, bool cancelable = false, Action<bool> callback = null) {
		Title = message.Title;
		Content = message.Content;
		Cancelable = cancelable;
		Callback = callback ?? ((b) => {});
	}
}