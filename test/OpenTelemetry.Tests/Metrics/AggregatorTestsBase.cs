// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Metrics;
using Xunit;

namespace OpenTelemetry.Metrics.Tests;

#pragma warning disable SA1402

public abstract class AggregatorTestsBase
{
    private static readonly Meter Meter = new("testMeter");
    private static readonly Instrument Instrument = Meter.CreateHistogram<long>("testInstrument");
    private static readonly ExplicitBucketHistogramConfiguration HistogramConfiguration = new() { Boundaries = Metric.DefaultHistogramBounds };
    private static readonly MetricStreamIdentity MetricStreamIdentity = new(Instrument, HistogramConfiguration);

    private readonly bool emitOverflowAttribute;
    private readonly bool shouldReclaimUnusedMetricPoints;
    private readonly AggregatorStore aggregatorStore;

    protected AggregatorTestsBase(bool emitOverflowAttribute, bool shouldReclaimUnusedMetricPoints)
    {
        this.emitOverflowAttribute = emitOverflowAttribute;
        this.shouldReclaimUnusedMetricPoints = shouldReclaimUnusedMetricPoints;

        this.aggregatorStore = new(MetricStreamIdentity, AggregationType.HistogramWithBuckets, AggregationTemporality.Cumulative, 1024, emitOverflowAttribute, this.shouldReclaimUnusedMetricPoints);
    }

    [Fact]
    public void HistogramDistributeToAllBucketsDefault()
    {
        var histogramPoint = new MetricPoint(this.aggregatorStore, AggregationType.HistogramWithBuckets, null, Metric.DefaultHistogramBounds, Metric.DefaultExponentialHistogramMaxBuckets, Metric.DefaultExponentialHistogramMaxScale);
        var aggregator = MetricPointAggregatorFactory.GetAggregatorForAggregationType(histogramPoint.AggType);
        aggregator.Update(ref histogramPoint, -1, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 0, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 2, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 5, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 8, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 10, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 11, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 25, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 40, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 50, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 70, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 75, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 99, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 100, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 246, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 250, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 499, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 500, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 501, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 750, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 751, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 1000, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 1001, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 2500, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 2501, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 5000, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 5001, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 7500, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 7501, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 10000, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 10001, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 10000000, tags: default, offerExemplar: false);
        histogramPoint.TakeSnapshot(true);

        var count = histogramPoint.GetHistogramCount();

        Assert.Equal(32, count);

        int actualCount = 0;
        foreach (var histogramMeasurement in histogramPoint.GetHistogramBuckets())
        {
            Assert.Equal(2, histogramMeasurement.BucketCount);
            actualCount++;
        }
    }

    [Fact]
    public void HistogramDistributeToAllBucketsCustom()
    {
        var boundaries = new double[] { 10, 20 };
        var histogramPoint = new MetricPoint(this.aggregatorStore, AggregationType.HistogramWithBuckets, null, boundaries, Metric.DefaultExponentialHistogramMaxBuckets, Metric.DefaultExponentialHistogramMaxScale);
        var aggregator = MetricPointAggregatorFactory.GetAggregatorForAggregationType(histogramPoint.AggType);

        // 5 recordings <=10
        aggregator.Update(ref histogramPoint, -10, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 0, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 1, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 9, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 10, tags: default, offerExemplar: false);

        // 2 recordings >10, <=20
        aggregator.Update(ref histogramPoint, 11, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 19, tags: default, offerExemplar: false);

        histogramPoint.TakeSnapshot(true);

        var count = histogramPoint.GetHistogramCount();
        var sum = histogramPoint.GetHistogramSum();

        // Sum of all recordings
        Assert.Equal(40, sum);

        // Count  = # of recordings
        Assert.Equal(7, count);

        int index = 0;
        int actualCount = 0;
        var expectedBucketCounts = new long[] { 5, 2, 0 };
        foreach (var histogramMeasurement in histogramPoint.GetHistogramBuckets())
        {
            Assert.Equal(expectedBucketCounts[index], histogramMeasurement.BucketCount);
            index++;
            actualCount++;
        }

        Assert.Equal(boundaries.Length + 1, actualCount);
    }

    [Fact]
    public void HistogramBinaryBucketTest()
    {
        // Arrange
        // Bounds = (-Inf, 0] (0, 1], ... (49, +Inf)
        var boundaries = new double[HistogramBuckets.DefaultBoundaryCountForBinarySearch];
        for (var i = 0; i < boundaries.Length; i++)
        {
            boundaries[i] = i;
        }

        var histogramPoint = new MetricPoint(this.aggregatorStore, AggregationType.HistogramWithBuckets, null, boundaries, Metric.DefaultExponentialHistogramMaxBuckets, Metric.DefaultExponentialHistogramMaxScale);
        var aggregator = MetricPointAggregatorFactory.GetAggregatorForAggregationType(histogramPoint.AggType);

        // Act
        aggregator.Update(ref histogramPoint, -1, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, boundaries[0], tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, boundaries[boundaries.Length - 1], tags: default, offerExemplar: false);
        for (var i = 0.5; i < boundaries.Length; i++)
        {
            aggregator.Update(ref histogramPoint, i, tags: default, offerExemplar: false);
        }

        histogramPoint.TakeSnapshot(true);

        // Assert
        var index = 0;
        foreach (var histogramMeasurement in histogramPoint.GetHistogramBuckets())
        {
            var expectedCount = 1;

            if (index == 0 || index == boundaries.Length - 1)
            {
                expectedCount = 2;
            }

            Assert.Equal(expectedCount, histogramMeasurement.BucketCount);
            index++;
        }
    }

    [Fact]
    public void HistogramWithOnlySumCount()
    {
        var boundaries = Array.Empty<double>();
        var histogramPoint = new MetricPoint(this.aggregatorStore, AggregationType.Histogram, null, boundaries, Metric.DefaultExponentialHistogramMaxBuckets, Metric.DefaultExponentialHistogramMaxScale);
        var aggregator = MetricPointAggregatorFactory.GetAggregatorForAggregationType(histogramPoint.AggType);

        aggregator.Update(ref histogramPoint, -10, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 0, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 1, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 9, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 10, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 11, tags: default, offerExemplar: false);
        aggregator.Update(ref histogramPoint, 19, tags: default, offerExemplar: false);

        histogramPoint.TakeSnapshot(true);

        var count = histogramPoint.GetHistogramCount();
        var sum = histogramPoint.GetHistogramSum();

        // Sum of all recordings
        Assert.Equal(40, sum);

        // Count  = # of recordings
        Assert.Equal(7, count);

        // There should be no enumeration of BucketCounts and ExplicitBounds for HistogramSumCount
        var enumerator = histogramPoint.GetHistogramBuckets().GetEnumerator();
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void MultiThreadedHistogramUpdateAndSnapShotTest()
    {
        var boundaries = Array.Empty<double>();
        var histogramPoint = new MetricPoint(this.aggregatorStore, AggregationType.Histogram, null, boundaries, Metric.DefaultExponentialHistogramMaxBuckets, Metric.DefaultExponentialHistogramMaxScale);
        var argsToThread = new ThreadArguments
        {
            HistogramPoint = histogramPoint,
            Aggregator = MetricPointAggregatorFactory.GetAggregatorForAggregationType(histogramPoint.AggType),
            MreToEnsureAllThreadsStart = new ManualResetEvent(false),
        };

        var numberOfThreads = 2;
        var snapshotThread = new Thread(HistogramSnapshotThread);
        Thread[] updateThreads = new Thread[numberOfThreads];
        for (int i = 0; i < numberOfThreads; ++i)
        {
            updateThreads[i] = new Thread(HistogramUpdateThread);
            updateThreads[i].Start(argsToThread);
        }

        snapshotThread.Start(argsToThread);

        for (int i = 0; i < numberOfThreads; ++i)
        {
            updateThreads[i].Join();
        }

        snapshotThread.Join();

        // last snapshot
        histogramPoint.TakeSnapshot(outputDelta: true);

        var lastDelta = histogramPoint.GetHistogramSum();
        Assert.Equal(200, argsToThread.SumOfDelta + lastDelta);
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.Hosting", "http.server.request.duration", "s", KnownHistogramBuckets.DefaultShortSeconds)]
    [InlineData("Microsoft.AspNetCore.Hosting", "http.server.request.duration", "ms", KnownHistogramBuckets.Default)]
    [InlineData("Microsoft.AspNetCore.Hosting", "http.server.request.duration", "By", KnownHistogramBuckets.Default)]
    [InlineData("Microsoft.AspNetCore.Hosting", "http.server.request.duration", null, KnownHistogramBuckets.Default)]
    [InlineData("Microsoft.AspNetCore.Http.Connections", "signalr.server.connection.duration", "s", KnownHistogramBuckets.DefaultLongSeconds)]
    [InlineData("Microsoft.AspNetCore.RateLimiting", "aspnetcore.rate_limiting.request_lease.duration", "s", KnownHistogramBuckets.DefaultShortSeconds)]
    [InlineData("Microsoft.AspNetCore.RateLimiting", "aspnetcore.rate_limiting.request.time_in_queue", "s", KnownHistogramBuckets.DefaultShortSeconds)]
    [InlineData("Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration", "s", KnownHistogramBuckets.DefaultLongSeconds)]
    [InlineData("Microsoft.AspNetCore.Server.Kestrel", "kestrel.tls_handshake.duration", "s", KnownHistogramBuckets.DefaultShortSeconds)]
    [InlineData("OpenTelemetry.Instrumentation.AspNet", "http.server.duration", "ms", KnownHistogramBuckets.Default)]
    [InlineData("OpenTelemetry.Instrumentation.AspNet", "http.server.request.duration", "s", KnownHistogramBuckets.DefaultShortSeconds)]
    [InlineData("OpenTelemetry.Instrumentation.AspNetCore", "http.server.duration", "ms", KnownHistogramBuckets.Default)]
    [InlineData("OpenTelemetry.Instrumentation.Http", "http.client.duration", "ms", KnownHistogramBuckets.Default)]
    [InlineData("System.Net.Http", "http.client.connection.duration", "s", KnownHistogramBuckets.DefaultLongSeconds)]
    [InlineData("System.Net.Http", "http.client.request.duration", "s", KnownHistogramBuckets.DefaultShortSeconds)]
    [InlineData("System.Net.Http", "http.client.request.time_in_queue", "s", KnownHistogramBuckets.DefaultShortSeconds)]
    [InlineData("System.Net.NameResolution", "dns.lookup.duration", "s", KnownHistogramBuckets.DefaultShortSeconds)]
    [InlineData("General.App", "simple.alternative.counter", "s", KnownHistogramBuckets.Default)]
    public void HistogramBucketsDefaultUpdatesForSecondsTest(string meterName, string instrumentName, string unit, KnownHistogramBuckets expectedHistogramBuckets)
    {
        using var meter = new Meter(meterName);

        var instrument = meter.CreateHistogram<double>(instrumentName, unit);

        var metricStreamIdentity = new MetricStreamIdentity(instrument, metricStreamConfiguration: null);

        AggregatorStore aggregatorStore = new(
            metricStreamIdentity,
            AggregationType.Histogram,
            AggregationTemporality.Cumulative,
            cardinalityLimit: 1024,
            this.emitOverflowAttribute,
            this.shouldReclaimUnusedMetricPoints);

        KnownHistogramBuckets actualHistogramBounds = KnownHistogramBuckets.Default;
        if (aggregatorStore.HistogramBounds == Metric.DefaultHistogramBoundsShortSeconds)
        {
            actualHistogramBounds = KnownHistogramBuckets.DefaultShortSeconds;
        }
        else if (aggregatorStore.HistogramBounds == Metric.DefaultHistogramBoundsLongSeconds)
        {
            actualHistogramBounds = KnownHistogramBuckets.DefaultLongSeconds;
        }

        Assert.NotNull(aggregatorStore.HistogramBounds);
        Assert.Equal(expectedHistogramBuckets, actualHistogramBounds);
    }

    internal static void AssertExponentialBucketsAreCorrect(Base2ExponentialBucketHistogram expectedHistogram, ExponentialHistogramData data)
    {
        Assert.Equal(expectedHistogram.Scale, data.Scale);
        Assert.Equal(expectedHistogram.ZeroCount, data.ZeroCount);
        Assert.Equal(expectedHistogram.PositiveBuckets.Offset, data.PositiveBuckets.Offset);
        Assert.Equal(expectedHistogram.NegativeBuckets.Offset, data.NegativeBuckets.Offset);

        expectedHistogram.Snapshot();
        var expectedData = expectedHistogram.GetExponentialHistogramData();

        var actual = new List<long>();
        foreach (var bucketCount in data.PositiveBuckets)
        {
            actual.Add(bucketCount);
        }

        var expected = new List<long>();
        foreach (var bucketCount in expectedData.PositiveBuckets)
        {
            expected.Add(bucketCount);
        }

        Assert.Equal(expected, actual);

        actual = new List<long>();
        foreach (var bucketCount in data.NegativeBuckets)
        {
            actual.Add(bucketCount);
        }

        expected = new List<long>();
        foreach (var bucketCount in expectedData.NegativeBuckets)
        {
            expected.Add(bucketCount);
        }

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(AggregationType.Base2ExponentialHistogram, AggregationTemporality.Cumulative, true)]
    [InlineData(AggregationType.Base2ExponentialHistogram, AggregationTemporality.Delta, true)]
    [InlineData(AggregationType.Base2ExponentialHistogramWithMinMax, AggregationTemporality.Cumulative, true)]
    [InlineData(AggregationType.Base2ExponentialHistogramWithMinMax, AggregationTemporality.Delta, true)]
    [InlineData(AggregationType.Base2ExponentialHistogram, AggregationTemporality.Cumulative, false)]
    [InlineData(AggregationType.Base2ExponentialHistogram, AggregationTemporality.Delta, false)]
    [InlineData(AggregationType.Base2ExponentialHistogramWithMinMax, AggregationTemporality.Cumulative, false)]
    [InlineData(AggregationType.Base2ExponentialHistogramWithMinMax, AggregationTemporality.Delta, false)]
    internal void ExponentialHistogramTests(AggregationType aggregationType, AggregationTemporality aggregationTemporality, bool exemplarsEnabled)
    {
        var valuesToRecord = new[] { -10, 0, 1, 9, 10, 11, 19 };

        var streamConfiguration = new Base2ExponentialBucketHistogramConfiguration();
        var metricStreamIdentity = new MetricStreamIdentity(Instrument, streamConfiguration);

        var aggregatorStore = new AggregatorStore(
            metricStreamIdentity,
            aggregationType,
            aggregationTemporality,
            cardinalityLimit: 1024,
            this.emitOverflowAttribute,
            this.shouldReclaimUnusedMetricPoints,
            exemplarsEnabled ? ExemplarFilterType.AlwaysOn : null);

        var expectedHistogram = new Base2ExponentialBucketHistogram();

        foreach (var value in valuesToRecord)
        {
            aggregatorStore.Update(value, Array.Empty<KeyValuePair<string, object>>());

            if (value >= 0)
            {
                expectedHistogram.Record(value);
            }
        }

        aggregatorStore.Snapshot();

        var metricPoints = new List<MetricPoint>();

        foreach (ref readonly var mp in aggregatorStore.GetMetricPoints())
        {
            metricPoints.Add(mp);
        }

        Assert.Single(metricPoints);
        var metricPoint = metricPoints[0];

        var count = metricPoint.GetHistogramCount();
        var sum = metricPoint.GetHistogramSum();
        var hasMinMax = metricPoint.TryGetHistogramMinMaxValues(out var min, out var max);

        AssertExponentialBucketsAreCorrect(expectedHistogram, metricPoint.GetExponentialHistogramData());
        Assert.Equal(50, sum);
        Assert.Equal(6, count);

        if (aggregationType == AggregationType.Base2ExponentialHistogramWithMinMax)
        {
            Assert.True(hasMinMax);
            Assert.Equal(0, min);
            Assert.Equal(19, max);
        }
        else
        {
            Assert.False(hasMinMax);
        }

        metricPoint.TakeSnapshot(aggregationTemporality == AggregationTemporality.Delta);

        count = metricPoint.GetHistogramCount();
        sum = metricPoint.GetHistogramSum();
        hasMinMax = metricPoint.TryGetHistogramMinMaxValues(out min, out max);

        if (aggregationTemporality == AggregationTemporality.Cumulative)
        {
            AssertExponentialBucketsAreCorrect(expectedHistogram, metricPoint.GetExponentialHistogramData());
            Assert.Equal(50, sum);
            Assert.Equal(6, count);

            if (aggregationType == AggregationType.Base2ExponentialHistogramWithMinMax)
            {
                Assert.True(hasMinMax);
                Assert.Equal(0, min);
                Assert.Equal(19, max);
            }
            else
            {
                Assert.False(hasMinMax);
            }
        }
        else
        {
            expectedHistogram.Reset();
            AssertExponentialBucketsAreCorrect(expectedHistogram, metricPoint.GetExponentialHistogramData());
            Assert.Equal(0, sum);
            Assert.Equal(0, count);

            if (aggregationType == AggregationType.Base2ExponentialHistogramWithMinMax)
            {
                Assert.True(hasMinMax);
                Assert.Equal(double.PositiveInfinity, min);
                Assert.Equal(double.NegativeInfinity, max);
            }
            else
            {
                Assert.False(hasMinMax);
            }
        }
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(null)]
    internal void ExponentialMaxScaleConfigWorks(int? maxScale)
    {
        var streamConfiguration = new Base2ExponentialBucketHistogramConfiguration();
        if (maxScale.HasValue)
        {
            streamConfiguration.MaxScale = maxScale.Value;
        }

        var metricStreamIdentity = new MetricStreamIdentity(Instrument, streamConfiguration);

        var aggregatorStore = new AggregatorStore(
            metricStreamIdentity,
            AggregationType.Base2ExponentialHistogram,
            AggregationTemporality.Cumulative,
            cardinalityLimit: 1024,
            this.emitOverflowAttribute,
            this.shouldReclaimUnusedMetricPoints);

        aggregatorStore.Update(10, Array.Empty<KeyValuePair<string, object>>());

        aggregatorStore.Snapshot();

        var metricPoints = new List<MetricPoint>();

        foreach (ref readonly var mp in aggregatorStore.GetMetricPoints())
        {
            metricPoints.Add(mp);
        }

        Assert.Single(metricPoints);
        var metricPoint = metricPoints[0];

        // After a single measurement there will not have been a scale down.
        // Scale will equal MaxScale.
        var expectedScale = maxScale.HasValue ? maxScale : Metric.DefaultExponentialHistogramMaxScale;
        Assert.Equal(expectedScale, metricPoint.GetExponentialHistogramData().Scale);
    }

    private static void HistogramSnapshotThread(object obj)
    {
        var args = obj as ThreadArguments;
        var mreToEnsureAllThreadsStart = args.MreToEnsureAllThreadsStart;

        if (Interlocked.Increment(ref args.ThreadStartedCount) == 3)
        {
            mreToEnsureAllThreadsStart.Set();
        }

        mreToEnsureAllThreadsStart.WaitOne();

        double curSnapshotDelta;
        while (Interlocked.Read(ref args.ThreadsFinishedAllUpdatesCount) != 2)
        {
            args.HistogramPoint.TakeSnapshot(outputDelta: true);
            curSnapshotDelta = args.HistogramPoint.GetHistogramSum();
            args.SumOfDelta += curSnapshotDelta;
        }
    }

    private static void HistogramUpdateThread(object obj)
    {
        var args = obj as ThreadArguments;
        var mreToEnsureAllThreadsStart = args.MreToEnsureAllThreadsStart;

        if (Interlocked.Increment(ref args.ThreadStartedCount) == 3)
        {
            mreToEnsureAllThreadsStart.Set();
        }

        mreToEnsureAllThreadsStart.WaitOne();

        for (int i = 0; i < 10; ++i)
        {
            args.Aggregator.Update(ref args.HistogramPoint, 10, tags: default, offerExemplar: false);
        }

        Interlocked.Increment(ref args.ThreadsFinishedAllUpdatesCount);
    }

    private class ThreadArguments
    {
        public MetricPoint HistogramPoint;
        public MetricPointAggregator Aggregator;
        public ManualResetEvent MreToEnsureAllThreadsStart;
        public int ThreadStartedCount;
        public long ThreadsFinishedAllUpdatesCount;
        public double SumOfDelta;
    }
}

public class AggregatorTests : AggregatorTestsBase
{
    public AggregatorTests()
        : base(emitOverflowAttribute: false, shouldReclaimUnusedMetricPoints: false)
    {
    }
}

public class AggregatorTestsWithOverflowAttribute : AggregatorTestsBase
{
    public AggregatorTestsWithOverflowAttribute()
        : base(emitOverflowAttribute: true, shouldReclaimUnusedMetricPoints: false)
    {
    }
}

public class AggregatorTestsWithReclaimAttribute : AggregatorTestsBase
{
    public AggregatorTestsWithReclaimAttribute()
        : base(emitOverflowAttribute: false, shouldReclaimUnusedMetricPoints: true)
    {
    }
}

public class AggregatorTestsWithBothReclaimAndOverflowAttributes : AggregatorTestsBase
{
    public AggregatorTestsWithBothReclaimAndOverflowAttributes()
        : base(emitOverflowAttribute: true, shouldReclaimUnusedMetricPoints: true)
    {
    }
}
