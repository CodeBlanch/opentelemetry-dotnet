// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.Metrics;

internal abstract class MetricPointAggregator
{
    public abstract void Update(
        ref MetricPoint metricPoint,
        long value,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        bool offerExemplar);

    public abstract void Update(
        ref MetricPoint metricPoint,
        double value,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        bool offerExemplar);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void UpdateExemplar(
        ref MetricPoint metricPoint,
        bool offerExemplar,
        long value,
        ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (offerExemplar)
        {
            Debug.Assert(metricPoint.OptionalComponents?.ExemplarReservoir != null, "ExemplarReservoir was null");

            // TODO: A custom implementation of `ExemplarReservoir.Offer` might throw an exception.
            metricPoint.OptionalComponents!.ExemplarReservoir!.Offer(
                new ExemplarMeasurement<long>(value, tags));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void UpdateExemplar(
        ref MetricPoint metricPoint,
        bool offerExemplar,
        double value,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        int explicitBucketHistogramBucketIndex = -1)
    {
        if (offerExemplar)
        {
            Debug.Assert(metricPoint.OptionalComponents?.ExemplarReservoir != null, "ExemplarReservoir was null");

            // TODO: A custom implementation of `ExemplarReservoir.Offer` might throw an exception.
            metricPoint.OptionalComponents!.ExemplarReservoir!.Offer(
                new ExemplarMeasurement<double>(value, tags, explicitBucketHistogramBucketIndex));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void CompleteUpdate(ref MetricPoint metricPoint)
    {
        // There is a race with Snapshot:
        // Update() updates the value
        // Snapshot snapshots the value
        // Snapshot sets status to NoCollectPending
        // Update sets status to CollectPending -- this is not right as the Snapshot
        // already included the updated value.
        // In the absence of any new Update call until next Snapshot,
        // this results in exporting an Update even though
        // it had no update.
        // TODO: For Delta, this can be mitigated
        // by ignoring Zero points
        metricPoint.MetricPointStatus = MetricPointStatus.CollectPending;

        this.CompleteUpdateWithoutMeasurement(ref metricPoint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void CompleteUpdateWithoutMeasurement(ref MetricPoint metricPoint)
    {
        if (metricPoint.AggregatorStore.OutputDeltaWithUnusedMetricPointReclaimEnabled)
        {
            Interlocked.Decrement(ref metricPoint.ReferenceCount);
        }
    }
}
