// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;

public class Program
{
    private static readonly Meter MyMeter = new("MyCompany.MyProduct.MyLibrary", "1.0");

    private static readonly Histogram<long> MyFruitHistogramLong = MyMeter.CreateHistogram<long>(
        "MyFruitHistogramLong",
        unit: null,
        description: null,
        tags: null,
        new()
        {
            ExplicitBucketBoundaries = [0, 100, 1000],
        });

    private static readonly Histogram<double> MyFruitHistogramDouble = MyMeter.CreateHistogram<double>(
        "MyFruitHistogramLong",
        unit: null,
        description: null,
        tags: null,
        new()
        {
            ExplicitBucketBoundaries = [0.18D, 100.18D, 1000.18D],
        });

    public static void Main()
    {
        var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddMeter("MyCompany.MyProduct.MyLibrary")
            .AddConsoleExporter()
            .Build();

        MyFruitHistogramLong.Record(1, new("name", "apple"), new("color", "red"));
        MyFruitHistogramDouble.Record(1.18D, new("name", "apple"), new("color", "red"));

        // Dispose meter provider before the application ends.
        // This will flush the remaining metrics and shutdown the metrics pipeline.
        meterProvider.Dispose();
    }
}
