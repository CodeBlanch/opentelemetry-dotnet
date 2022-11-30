using System.Diagnostics;

namespace MyLibrary.Telemetry;

internal interface IMyServiceTelemetry
{
    void InjectTelemetryContextIntoMessage(Message message);

    void ExtractTelemetryContextFromMessage(Message message, out ActivityContext activityContext);

    bool FilterReadMessageRequest(string? messagePrefix);

    void EnrichReadMessageTrace(string? messagePrefix, Activity activity, Message message);

    void EnrichReadMessagMetric(string? messagePrefix, Message message, out TagList tags);

    bool FilterWriteMessageRequest(Message message);

    void EnrichWriteMessageTrace(Message message, Activity activity);

    void EnrichWriteMessagMetric(Message message, out TagList tags);

    IDisposable? SuppressInstrumentation();
}
