// <copyright file="InMemoryMetricExporter.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System.Collections.Generic;
using OpenTelemetry.Metrics;

namespace OpenTelemetry.Exporter
{
    public class InMemoryMetricExporter : BaseExporter<Metric>
    {
        private readonly ICollection<ExportedMetric> exportedItems;

        public InMemoryMetricExporter(ICollection<ExportedMetric> exportedItems)
        {
            this.exportedItems = exportedItems;
        }

        public override ExportResult Export(in Batch<Metric> batch)
        {
            if (this.exportedItems == null)
            {
                return ExportResult.Failure;
            }

            foreach (var metric in batch)
            {
                List<MetricPoint> metricPoints = new();

                foreach (ref readonly var metricPoint in metric.GetMetricPoints())
                {
                    metricPoints.Add(MetricPoint.Copy(in metricPoint));
                }

                this.exportedItems.Add(new ExportedMetric(metric, metricPoints));
            }

            return ExportResult.Success;
        }

        public readonly struct ExportedMetric
        {
            private readonly InstrumentIdentity instrumentIdentity;

            internal ExportedMetric(
                Metric metric,
                IReadOnlyList<MetricPoint> metricPoints)
            {
                this.instrumentIdentity = metric.InstrumentIdentity;
                this.MetricPoints = metricPoints;
            }

            public string Name => this.instrumentIdentity.InstrumentName;

            public string Description => this.instrumentIdentity.Description;

            public string Unit => this.instrumentIdentity.Unit;

            public string MeterName => this.instrumentIdentity.MeterName;

            public string MeterVersion => this.instrumentIdentity.MeterVersion;

            public IReadOnlyList<MetricPoint> MetricPoints { get; }
        }
    }
}
