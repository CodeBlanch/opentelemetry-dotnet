﻿// <copyright file="JaegerUdpBatcherTests.cs" company="OpenTelemetry Authors">
// Copyright 2018, OpenTelemetry Authors
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
using OpenTelemetry.Exporter.Jaeger.Implementation;
using OpenTelemetry.Resources;
using Xunit;

namespace OpenTelemetry.Exporter.Jaeger.Tests.Implementation
{
    public class JaegerUdpBatcherTests
    {
        [Fact]
        public void JaegerUdpBatcherTests_ctor_ServiceNameFromResource()
        {
            var options = new JaegerExporterOptions();

            new JaegerUdpBatcher(
                options,
                new Resource(new Dictionary<string, object>
                {
                    [Resource.ServiceNameKey] = "TestService",
                }));

            Assert.Equal("TestService", options.ServiceName);
        }

        [Fact]
        public void JaegerUdpBatcherTests_ctor_ProcessTagFromResource()
        {
            var options = new JaegerExporterOptions
            {
                ServiceName = "TestService",
            };

            new JaegerUdpBatcher(
                options,
                new Resource(new Dictionary<string, object>
                {
                    ["process.tag.entry"] = "value",
                }));

            Assert.Equal("value", options.ProcessTags["process.tag.entry"]);
        }
    }
}
