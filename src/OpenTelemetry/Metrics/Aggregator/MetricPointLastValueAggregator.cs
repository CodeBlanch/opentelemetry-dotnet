// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace OpenTelemetry.Metrics;

internal sealed class MetricPointLastValueAggregator : MetricPointAggregator
{
    public override void Update(ref MetricPoint metricPoint, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags, bool offerExemplar)
    {
        Debug.Assert(
            metricPoint.AggType == AggregationType.LongSumIncomingCumulative
            || metricPoint.AggType == AggregationType.LongGauge,
            "MetricPoint AggregationType was invalid");

        Interlocked.Exchange(ref metricPoint.RunningValue.AsLong, value);

        this.CompleteUpdate(ref metricPoint);

        this.UpdateExemplar(ref metricPoint, offerExemplar, value, tags);
    }

    public override void Update(ref MetricPoint metricPoint, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags, bool offerExemplar)
    {
        Debug.Assert(
            metricPoint.AggType == AggregationType.DoubleSumIncomingCumulative
            || metricPoint.AggType == AggregationType.DoubleGauge,
            "MetricPoint AggregationType was invalid");

        Interlocked.Exchange(ref metricPoint.RunningValue.AsDouble, value);

        this.CompleteUpdate(ref metricPoint);

        this.UpdateExemplar(ref metricPoint, offerExemplar, value, tags);
    }
}
