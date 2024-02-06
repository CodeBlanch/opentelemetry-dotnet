// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Metrics;

/// <summary>
/// Options for configuring either a <see cref="BaseExportingMetricReader"/> or <see cref="PeriodicExportingMetricReader"/> .
/// </summary>
public class MetricReaderOptions
{
    private PeriodicExportingMetricReaderOptions periodicExportingMetricReaderOptions;
    private int? cardinalityLimit = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricReaderOptions"/> class.
    /// </summary>
    public MetricReaderOptions()
        : this(new ConfigurationBuilder().AddEnvironmentVariables().Build())
    {
    }

    internal MetricReaderOptions(IConfiguration configuration)
    {
        this.periodicExportingMetricReaderOptions = new PeriodicExportingMetricReaderOptions(configuration);
    }

    /// <summary>
    /// Gets or sets the <see cref="MetricReaderTemporalityPreference" />.
    /// </summary>
    public MetricReaderTemporalityPreference TemporalityPreference { get; set; } = MetricReaderTemporalityPreference.Cumulative;

#if EXPOSE_EXPERIMENTAL_FEATURES
    /// <summary>
    /// Gets or sets a positive integer value defining the maximum number of
    /// data points allowed for the metrics managed by the <see
    /// cref="MetricReader"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>WARNING</b>: This is an experimental API which might change or
    /// be removed in the future. Use at your own risk.</para>
    /// <para>Spec reference: <see
    /// href="https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/metrics/sdk.md#metricreader">MetricReader</see>.</para>
    /// Note: If not set, the MeterProvider cardinality limit value will be
    /// used, which defaults to 2000. Call <see
    /// cref="MeterProviderBuilderExtensions.SetCardinalityLimit"/> to configure
    /// the MeterProvider default.
    /// </remarks>
#if NET8_0_OR_GREATER
    [Experimental(DiagnosticDefinitions.CardinalityLimitExperimentalApi, UrlFormat = DiagnosticDefinitions.ExperimentalApiUrlFormat)]
#endif
    public
#else
    internal
#endif
    int? CardinalityLimit
    {
        get => this.cardinalityLimit;
        set
        {
            if (value != null)
            {
                Guard.ThrowIfOutOfRange(value.Value, min: 1, max: int.MaxValue);
            }

            this.cardinalityLimit = value;
        }
    }

    /// <summary>
    /// Gets or sets the <see cref="Metrics.PeriodicExportingMetricReaderOptions" />.
    /// </summary>
    public PeriodicExportingMetricReaderOptions PeriodicExportingMetricReaderOptions
    {
        get => this.periodicExportingMetricReaderOptions;
        set
        {
            Guard.ThrowIfNull(value);
            this.periodicExportingMetricReaderOptions = value;
        }
    }
}
