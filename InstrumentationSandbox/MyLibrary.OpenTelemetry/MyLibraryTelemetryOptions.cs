using System.Diagnostics;

namespace MyLibrary;

public class MyLibraryTelemetryOptions
{
    public bool TracingEnabled { get; set; } = true;

    public bool MetricsEnabled { get; set; } = true;

    public delegate void EnrichMetricAction(string operationName, Message message, out TagList tags);

    public Action<string, Activity, Message>? EnrichTrace { get; set; }

    public EnrichMetricAction? EnrichMetric { get; set; }

    public Func<string?, bool>? FilterReadMessageRequest { get; set; }

    public Func<Message, bool>? FilterWriteMessageRequest { get; set; }
}
