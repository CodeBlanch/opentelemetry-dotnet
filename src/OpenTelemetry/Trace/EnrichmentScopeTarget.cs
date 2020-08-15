﻿// <copyright file="EnrichmentScopeTarget.cs" company="OpenTelemetry Authors">
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

using System.Diagnostics;

namespace OpenTelemetry.Trace
{
    /// <summary>
    /// Describes the target of an <see cref="EnrichmentScope"/>.
    /// </summary>
    public enum EnrichmentScopeTarget
    {
        /// <summary>
        /// The first child <see cref="Activity"/> created under the scope will be enriched and then the scope will automatically be closed.
        /// </summary>
        FirstChild,

        /// <summary>
        /// All child <see cref="Activity"/> objects created under the scope will be enriched until the scope is closed.
        /// </summary>
        AllChildren,
    }
}
