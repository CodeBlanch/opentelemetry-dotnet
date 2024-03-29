// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace OpenTelemetry.Metrics;

internal sealed class MetricPointBase2ExponentialHistogramAggregator<TMinMax> : MetricPointAggregator
    where TMinMax : struct
{
    // Note: This flag and the logic it toggles is designed to be elided by the
    // JIT when it generates specialized implementations
    private static readonly bool HasMinMax = typeof(TMinMax) == typeof(MetricPointAggregatorBehaviorEnabledToggle);

    public override void Update(
        ref MetricPoint metricPoint,
        long value,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        bool offerExemplar)
        => this.Update(ref metricPoint, (double)value, tags, offerExemplar);

    public override void Update(ref MetricPoint metricPoint, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags, bool offerExemplar)
    {
        Debug.Assert(
            metricPoint.AggType == AggregationType.Base2ExponentialHistogram
            || metricPoint.AggType == AggregationType.Base2ExponentialHistogramWithMinMax,
            "MetricPoint AggregationType was invalid");

        if (value < 0)
        {
            this.CompleteUpdateWithoutMeasurement(ref metricPoint);
            return;
        }

        Debug.Assert(metricPoint.OptionalComponents?.Base2ExponentialBucketHistogram != null, "Base2ExponentialBucketHistogram was null");

        var exponentialHistogram = metricPoint.OptionalComponents!.Base2ExponentialBucketHistogram!;

        metricPoint.OptionalComponents.AcquireLock();

        unchecked
        {
            metricPoint.RunningValue.AsLong++;
            exponentialHistogram.RunningSum += value;
            exponentialHistogram.Record(value);
        }

        if (HasMinMax)
        {
            exponentialHistogram.RunningMin = Math.Min(exponentialHistogram.RunningMin, value);
            exponentialHistogram.RunningMax = Math.Max(exponentialHistogram.RunningMax, value);
        }

        metricPoint.OptionalComponents.ReleaseLock();

        this.UpdateExemplar(ref metricPoint, offerExemplar, value, tags);

        this.CompleteUpdate(ref metricPoint);
    }
}
