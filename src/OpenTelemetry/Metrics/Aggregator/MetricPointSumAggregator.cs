// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Metrics;

internal sealed class MetricPointSumAggregator : MetricPointAggregator
{
    public override void Update(ref MetricPoint metricPoint, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags, bool offerExemplar)
    {
        Debug.Assert(
            metricPoint.AggType == AggregationType.LongSumIncomingDelta,
            "MetricPoint AggregationType was invalid");

        Interlocked.Add(ref metricPoint.RunningValue.AsLong, value);

        this.CompleteUpdate(ref metricPoint);

        this.UpdateExemplar(ref metricPoint, offerExemplar, value, tags);
    }

    public override void Update(ref MetricPoint metricPoint, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags, bool offerExemplar)
    {
        Debug.Assert(
            metricPoint.AggType == AggregationType.DoubleSumIncomingDelta,
            "MetricPoint AggregationType was invalid");

        InterlockedHelper.Add(ref metricPoint.RunningValue.AsDouble, value);

        this.CompleteUpdate(ref metricPoint);

        this.UpdateExemplar(ref metricPoint, offerExemplar, value, tags);
    }
}
