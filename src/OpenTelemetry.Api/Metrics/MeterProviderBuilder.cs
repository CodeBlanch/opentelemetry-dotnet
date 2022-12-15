// <copyright file="MeterProviderBuilder.cs" company="OpenTelemetry Authors">
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

using System.Diagnostics.Metrics;

namespace OpenTelemetry.Metrics
{
    /// <summary>
    /// MeterProviderBuilder base class.
    /// </summary>
    public abstract class MeterProviderBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MeterProviderBuilder"/> class.
        /// </summary>
        protected MeterProviderBuilder()
        {
        }

        /// <summary>
        /// Adds instrumentation to the provider.
        /// </summary>
        /// <typeparam name="TInstrumentation">Type of instrumentation class.</typeparam>
        /// <param name="instrumentationFactory">Function that builds instrumentation.</param>
        /// <returns>Returns <see cref="MeterProviderBuilder"/> for chaining.</returns>
        public abstract MeterProviderBuilder AddInstrumentation<TInstrumentation>(
            Func<TInstrumentation> instrumentationFactory)
            where TInstrumentation : class;

        /// <summary>
        /// Adds given meter names to the list of subscribed meters.
        /// </summary>
        /// <param name="names">Meter names.</param>
        /// <returns>Returns <see cref="MeterProviderBuilder"/> for chaining.</returns>
        public abstract MeterProviderBuilder AddMeter(params string[] names);

        /// <summary>
        /// Adds a function for subscribing to <see cref="Meter"/> instances.
        /// </summary>
        /// <remarks>
        /// Note: Return <see langword="true"/> from <paramref
        /// name="shouldListenToFunc"/> to add a <see cref="Meter"/> to the
        /// list of subscribed meters. Return <see langword="false"/> to ignore
        /// the <see cref="Meter"/>.
        /// </remarks>
        /// <param name="shouldListenToFunc">Function for determining a the
        /// provider should listen to a <see cref="Meter"/>.</param>
        /// <returns>Returns <see cref="MeterProviderBuilder"/> for
        /// chaining.</returns>
        public virtual MeterProviderBuilder AddMeter(Func<Meter, bool> shouldListenToFunc)
        {
            throw new NotSupportedException($"Type '{this.GetType()}' does not support listening to meters by function.");
        }
    }
}
