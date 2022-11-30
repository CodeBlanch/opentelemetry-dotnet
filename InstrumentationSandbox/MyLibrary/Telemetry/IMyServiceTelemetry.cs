using System.Diagnostics;

namespace MyLibrary.Telemetry;

internal interface IMyServiceTelemetry
{
    void InjectMessage(Message message);

    bool FilterReadMessageRequest(string? messagePrefix);

    void EnrichReadMessageTrace(string? messagePrefix, Activity activity, Message message);

    void EnrichReadMessagMetric(string? messagePrefix, Message message, out TagList tags);

    void ExtractMessage(Message message, out ActivityContext activityContext);

    bool FilterWriteMessageRequest(Message message);

    void EnrichWriteMessageTrace(Message message, Activity activity);

    void EnrichWriteMessagMetric(Message message, out TagList tags);

    IDisposable? SuppressDownstreamInstrumentation();
}
