using System.Diagnostics;
using System.Diagnostics.Metrics;
using MyLibrary.Telemetry;

namespace MyLibrary;

internal sealed class MyService : IMyService
{
    public const string TelemetryName = "MyLibrary";

    private static readonly string s_TelemetryVersion = typeof(MyService).Assembly.GetName().Version!.ToString();
    private static readonly ActivitySource s_ActivitySource = new(TelemetryName, s_TelemetryVersion);
    private static readonly Meter s_Meter = new(TelemetryName, s_TelemetryVersion);
    private static readonly Histogram<double> s_OperationDurationHistogram = s_Meter.CreateHistogram<double>("operation_duration", unit: "Ms", description: "MyLibrary service operation duration in milliseconds.");

    private readonly IMyServiceTelemetry myServiceTelemetry;

    public MyService(IMyServiceTelemetry? myServiceTelemetry = null)
    {
        this.myServiceTelemetry = myServiceTelemetry ?? new DefaultMyServiceTelemetry();
    }

    public async Task<Message> ReadMessage(string? messagePrefix)
    {
        var startingTimestamp = Stopwatch.GetTimestamp();

        bool filterTelemetry = this.myServiceTelemetry.FilterReadMessageRequest(messagePrefix);

        using var activity = filterTelemetry ? null : s_ActivitySource.StartActivity("ReadMessage", ActivityKind.Client);

        using (this.myServiceTelemetry.SuppressInstrumentation())
        {
            // Simulate some work.
            await Task.Delay(2000).ConfigureAwait(false);
        }

        Message response = new();

        if (!filterTelemetry)
        {
            this.myServiceTelemetry.EnrichReadMessagMetric(messagePrefix, response, out var tags);

            var elapsedTime = Stopwatch.GetElapsedTime(startingTimestamp);

            s_OperationDurationHistogram.Record(elapsedTime.TotalMilliseconds, in tags);
        }

        if (activity?.IsAllDataRequested == true)
        {
            this.myServiceTelemetry.EnrichReadMessageTrace(messagePrefix, activity, response);
        }

        return response;
    }

    public async Task WriteMessage(Message message)
    {
        var startingTimestamp = Stopwatch.GetTimestamp();

        bool filterTelemetry = this.myServiceTelemetry.FilterWriteMessageRequest(message);

        using var activity = filterTelemetry ? null : s_ActivitySource.StartActivity("WriteMessage", ActivityKind.Client);

        if (activity?.IsAllDataRequested == true)
        {
            this.myServiceTelemetry.EnrichWriteMessageTrace(message, activity);
        }

        this.myServiceTelemetry.InjectTelemetryContextIntoMessage(message);

        using (this.myServiceTelemetry.SuppressInstrumentation())
        {
            // Simulate some work.
            await Task.Delay(2000).ConfigureAwait(false);
        }

        if (!filterTelemetry)
        {
            this.myServiceTelemetry.EnrichWriteMessagMetric(message, out var tags);

            var elapsedTime = Stopwatch.GetElapsedTime(startingTimestamp);

            s_OperationDurationHistogram.Record(elapsedTime.TotalMilliseconds, in tags);
        }
    }
}
