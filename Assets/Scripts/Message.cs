using System;

[Serializable]
public class Message {
    public string Title;
    public string Content;
		
    public Message() {}

    public Message(string title, string content) {
        Title   = title;
        Content = content;
    }

    public Message Format(params object[] args) {
        return new Message(Title, string.Format(Content, args));
    }
}
