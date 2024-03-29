// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace OpenTelemetry.Metrics;

internal sealed class MetricPointExplicitBucketHistogramAggregator<TBuckets, TMinMax> : MetricPointAggregator
    where TBuckets : struct
    where TMinMax : struct
{
    // Note: These flags and the logic they toggle are designed to be elided by
    // the JIT when it generates specialized implementations
    private static readonly bool HasBuckets = typeof(TBuckets) == typeof(MetricPointAggregatorBehaviorEnabledToggle);
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
            metricPoint.AggType == AggregationType.Histogram
            || metricPoint.AggType == AggregationType.HistogramWithMinMax
            || metricPoint.AggType == AggregationType.HistogramWithBuckets
            || metricPoint.AggType == AggregationType.HistogramWithMinMaxBuckets,
            "MetricPoint AggregationType was invalid");
        Debug.Assert(metricPoint.OptionalComponents?.HistogramBuckets != null, "histogramBuckets was null");

        var histogramBuckets = metricPoint.OptionalComponents!.HistogramBuckets!;

        int bucketIndex = HasBuckets
            ? histogramBuckets.FindBucketIndex(value)
            : -1;

        metricPoint.OptionalComponents.AcquireLock();

        unchecked
        {
            metricPoint.RunningValue.AsLong++;
            histogramBuckets.RunningSum += value;
            if (HasBuckets)
            {
                histogramBuckets.BucketCounts[bucketIndex].RunningValue++;
            }
        }

        if (HasMinMax)
        {
            histogramBuckets.RunningMin = Math.Min(histogramBuckets.RunningMin, value);
            histogramBuckets.RunningMax = Math.Max(histogramBuckets.RunningMax, value);
        }

        metricPoint.OptionalComponents.ReleaseLock();

        this.UpdateExemplar(ref metricPoint, offerExemplar, value, tags, bucketIndex);

        this.CompleteUpdate(ref metricPoint);
    }
}
