// <copyright file="ILogProcessor.cs" company="OpenTelemetry Authors">
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
using System.Threading;

namespace OpenTelemetry.Logs
{
    // TODO: Investigate making this public so users can build simple
    // allocation-free log exporters.
    internal interface ILogProcessor : IDisposable
    {
        void SetParentProvider(BaseProvider parentProvider);

        void OnEnd(in LogRecordStruct log);

        bool ForceFlush(int timeoutMilliseconds = Timeout.Infinite);

        bool Shutdown(int timeoutMilliseconds = Timeout.Infinite);
    }
}