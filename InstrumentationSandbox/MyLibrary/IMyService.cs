namespace MyLibrary;

public interface IMyService
{
    Task WriteMessage(Message message);

    Task<Message> ReadMessage(string? messagePrefix);
}
