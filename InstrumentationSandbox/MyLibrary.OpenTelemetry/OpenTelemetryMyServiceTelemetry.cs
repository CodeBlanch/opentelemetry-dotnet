using System.Diagnostics;

namespace MyLibrary.Telemetry;

internal sealed class OpenTelemetryMyServiceTelemetry : IMyServiceTelemetry
{
    private readonly MyLibraryTelemetryOptions options;

    public OpenTelemetryMyServiceTelemetry(MyLibraryTelemetryOptions options)
    {
        this.options = options;
    }

    public void EnrichReadMessageTrace(string? messagePrefix, Activity activity, Message message)
    {
        if (!string.IsNullOrEmpty(messagePrefix))
        {
            activity.SetTag("messaging.mylibrary.message_prefix", messagePrefix);
        }

        activity.SetTag("messaging.system", "MyLibrary");
        activity.SetTag("messaging.destination", "default");
        activity.SetTag("messaging.destination_kind", "queue");
        activity.SetTag("messaging.message_id", message.Id);

        this.options.EnrichTrace?.Invoke("ReadMessage", activity, message);
    }

    public void EnrichReadMessagMetric(string? messagePrefix, Message message, out TagList tags)
    {
        tags = new()
        {
            new KeyValuePair<string, object?>("operation_name", "ReadMessage"),
            new KeyValuePair<string, object?>("messaging.destination", "default")
        };

        var enrichMetric = this.options.EnrichMetric;
        if (enrichMetric != null)
        {
            enrichMetric("ReadMessage", message, out var extraTags);

            foreach (var tag in extraTags)
            {
                tags.Add(tag);
            }
        }
    }

    public void EnrichWriteMessageTrace(Message message, Activity activity)
    {
        activity.SetTag("messaging.system", "MyLibrary");
        activity.SetTag("messaging.destination", "default");
        activity.SetTag("messaging.destination_kind", "queue");
        activity.SetTag("messaging.message_id", message.Id);

        this.options.EnrichTrace?.Invoke("WriteMessage", activity, message);
    }

    public void EnrichWriteMessagMetric(Message message, out TagList tags)
    {
        tags = new()
        {
            new KeyValuePair<string, object?>("operation_name", "WriteMessage"),
            new KeyValuePair<string, object?>("messaging.destination", "default")
        };

        var enrichMetric = this.options.EnrichMetric;
        if (enrichMetric != null)
        {
            enrichMetric("WriteMessage", message, out var extraTags);

            foreach (var tag in extraTags)
            {
                tags.Add(tag);
            }
        }
    }

    public bool FilterReadMessageRequest(string? messagePrefix)
    {
        return this.options.FilterReadMessageRequest?.Invoke(messagePrefix) ?? false;
    }

    public bool FilterWriteMessageRequest(Message message)
    {
        return this.options.FilterWriteMessageRequest?.Invoke(message) ?? false;
    }
}
