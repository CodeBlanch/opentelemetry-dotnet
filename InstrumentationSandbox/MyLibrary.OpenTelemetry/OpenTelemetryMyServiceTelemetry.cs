using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;

namespace MyLibrary.Telemetry;

internal sealed class OpenTelemetryMyServiceTelemetry : IMyServiceTelemetry
{
    private static readonly Action<Message, string, string> s_Setter = (message, name, value) =>
    {
        message.Headers[name] = value;
    };

    private static readonly Func<Message, string, IEnumerable<string>> s_Getter = (message, name) =>
    {
        if (message.Headers.TryGetValue(name, out string? value))
        {
            return new string[] { value };
        }

        return Array.Empty<string>();
    };

    private readonly MyLibraryTelemetryOptions options;

    public OpenTelemetryMyServiceTelemetry(MyLibraryTelemetryOptions options)
    {
        this.options = options;
    }

    public void InjectTelemetryContextIntoMessage(Message message)
    {
        var activity = Activity.Current;
        if (activity != null && activity.IdFormat == ActivityIdFormat.W3C)
        {
            Propagators.DefaultTextMapPropagator.Inject(
                new PropagationContext(activity.Context, Baggage.Current),
                message,
                s_Setter);
        }
    }

    public void ExtractTelemetryContextFromMessage(Message message, out ActivityContext activityContext)
    {
        var context = Propagators.DefaultTextMapPropagator.Extract(default, message, s_Getter);

        activityContext = context.ActivityContext;
        Baggage.Current = context.Baggage;
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

    public IDisposable? SuppressInstrumentation()
    {
        return SuppressInstrumentationScope.Begin();
    }
}
