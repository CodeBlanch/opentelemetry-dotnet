using System.Diagnostics;

namespace MyLibrary.Telemetry;

internal interface IMyServiceTelemetry
{
    bool FilterReadMessageRequest(string? messagePrefix);

    void EnrichReadMessageTrace(string? messagePrefix, Activity activity, Message message);

    void EnrichReadMessagMetric(string? messagePrefix, Message message, out TagList tags);

    bool FilterWriteMessageRequest(Message message);

    void EnrichWriteMessageTrace(Message message, Activity activity);

    void EnrichWriteMessagMetric(Message message, out TagList tags);
}
