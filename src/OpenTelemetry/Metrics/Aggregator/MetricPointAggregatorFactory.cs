// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace OpenTelemetry.Metrics;

internal static class MetricPointAggregatorFactory
{
    private static readonly MetricPointAggregator SumAggregator = new MetricPointSumAggregator();
    private static readonly MetricPointAggregator LastValueAggregator = new MetricPointLastValueAggregator();
    private static readonly MetricPointAggregator HistogramAggregator = new MetricPointExplicitBucketHistogramAggregator<MetricPointAggregatorBehaviorDisabledToggle, MetricPointAggregatorBehaviorDisabledToggle>();
    private static readonly MetricPointAggregator HistogramWithMinMaxAggregator = new MetricPointExplicitBucketHistogramAggregator<MetricPointAggregatorBehaviorDisabledToggle, MetricPointAggregatorBehaviorEnabledToggle>();
    private static readonly MetricPointAggregator HistogramWithBucketsAggregator = new MetricPointExplicitBucketHistogramAggregator<MetricPointAggregatorBehaviorEnabledToggle, MetricPointAggregatorBehaviorDisabledToggle>();
    private static readonly MetricPointAggregator HistogramWithMinMaxBucketsAggregator = new MetricPointExplicitBucketHistogramAggregator<MetricPointAggregatorBehaviorEnabledToggle, MetricPointAggregatorBehaviorEnabledToggle>();
    private static readonly MetricPointAggregator Base2ExponentialHistogramAggregator = new MetricPointBase2ExponentialHistogramAggregator<MetricPointAggregatorBehaviorDisabledToggle>();
    private static readonly MetricPointAggregator Base2ExponentialHistogramWithMinMaxAggregator = new MetricPointBase2ExponentialHistogramAggregator<MetricPointAggregatorBehaviorEnabledToggle>();

    public static MetricPointAggregator GetAggregatorForAggregationType(
        AggregationType aggregationType)
    {
        switch (aggregationType)
        {
            case AggregationType.LongSumIncomingDelta:
            case AggregationType.DoubleSumIncomingDelta:
                return SumAggregator;

            case AggregationType.LongSumIncomingCumulative:
            case AggregationType.LongGauge:
            case AggregationType.DoubleSumIncomingCumulative:
            case AggregationType.DoubleGauge:
                return LastValueAggregator;

            case AggregationType.Histogram:
                return HistogramAggregator;

            case AggregationType.HistogramWithMinMax:
                return HistogramWithMinMaxAggregator;

            case AggregationType.HistogramWithBuckets:
                return HistogramWithBucketsAggregator;

            case AggregationType.HistogramWithMinMaxBuckets:
                return HistogramWithMinMaxBucketsAggregator;

            case AggregationType.Base2ExponentialHistogram:
                return Base2ExponentialHistogramAggregator;

            case AggregationType.Base2ExponentialHistogramWithMinMax:
                return Base2ExponentialHistogramWithMinMaxAggregator;

            default:
                Debug.Fail("Unexpected AggregationType encountered");
                throw new NotSupportedException($"AggregationType '{aggregationType}' is not supported.");
        }
    }
}
