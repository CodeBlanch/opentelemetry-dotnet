// <copyright file="PropertyFetcherBenchmark.cs" company="OpenTelemetry Authors">
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

using System;
using BenchmarkDotNet.Attributes;
using OpenTelemetry.Instrumentation;

namespace Benchmarks.Instrumentation
{
    [MemoryDiagnoser]
    public class PropertyFetcherBenchmark
    {
        private readonly ReflectedType instance = new ReflectedType();
        private readonly PropertyFetcher<string> propertyFetcher = new PropertyFetcher<string>("Name");
        private readonly PropertyFetcherWithDictionary<string> propertyFetcherWithDictionary = new PropertyFetcherWithDictionary<string>("Name");

        [GlobalSetup]
        public void GlobalSetup()
        {
            if (this.propertyFetcher.Fetch(this.instance) != "OpenTelemetry")
            {
                throw new InvalidOperationException();
            }

            if (this.propertyFetcherWithDictionary.Fetch(this.instance) != "OpenTelemetry")
            {
                throw new InvalidOperationException();
            }
        }

        [Benchmark]
        public void PropertyFetcher()
        {
            this.propertyFetcher.Fetch(this.instance);
        }

        [Benchmark]
        public void PropertyFetcherWithDictionary()
        {
            this.propertyFetcherWithDictionary.Fetch(this.instance);
        }

        private class ReflectedType
        {
            public string Name { get; set; } = "OpenTelemetry";
        }
    }
}
