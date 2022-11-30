using System.Diagnostics;

namespace MyLibrary.Telemetry;

internal sealed class DefaultMyServiceTelemetry : IMyServiceTelemetry
{
    public void InjectTelemetryContextIntoMessage(Message message)
    {
    }

    public void ExtractTelemetryContextFromMessage(Message message, out ActivityContext activityContext)
    {
        activityContext = default;
    }

    public void EnrichReadMessageTrace(string? messagePrefix, Activity activity, Message message)
    {
        if (!string.IsNullOrEmpty(messagePrefix))
        {
            activity.SetTag("message_prefix", messagePrefix);
        }

        activity.SetTag("message_id", message.Id);
    }

    public void EnrichReadMessagMetric(string? messagePrefix, Message message, out TagList tags)
    {
        tags = new()
        {
            new KeyValuePair<string, object?>("operation_name", "ReadMessage")
        };
    }

    public void EnrichWriteMessageTrace(Message message, Activity activity)
    {
        activity.SetTag("message_id", message.Id);
    }

    public void EnrichWriteMessagMetric(Message message, out TagList tags)
    {
        tags = new()
        {
            new KeyValuePair<string, object?>("operation_name", "WriteMessage")
        };
    }

    public bool FilterReadMessageRequest(string? messagePrefix)
    {
        return false;
    }

    public bool FilterWriteMessageRequest(Message message)
    {
        return false;
    }

    public IDisposable? SuppressInstrumentation()
    {
        return null;
    }
}
