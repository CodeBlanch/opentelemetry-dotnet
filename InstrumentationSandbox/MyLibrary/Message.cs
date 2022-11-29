namespace MyLibrary;

public class Message
{
    public string Id { get; }

    public Message(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentNullException(nameof(id));
        }

        this.Id = id;
    }

    public Message()
    {
        this.Id = Guid.NewGuid().ToString("N");
    }
}
