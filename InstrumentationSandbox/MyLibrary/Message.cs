namespace MyLibrary;

public class Message
{
    public string Id { get; }

    public Dictionary<string, string> Headers { get; }

    public Message(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentNullException(nameof(id));
        }

        this.Id = id;
        this.Headers = new(StringComparer.OrdinalIgnoreCase);
    }

    public Message()
        : this(Guid.NewGuid().ToString("N"))
    {
    }
}
