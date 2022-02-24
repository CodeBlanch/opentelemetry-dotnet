// <copyright file="LogRecord.cs" company="OpenTelemetry Authors">
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
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace OpenTelemetry.Logs
{
    /// <summary>
    /// Stores details about a log message.
    /// </summary>
    public sealed class LogRecord
    {
        private static readonly Action<object, List<object>> AddScopeToBufferedList = (object scope, List<object> state) =>
        {
            state.Add(scope);
        };

        private IReadOnlyList<KeyValuePair<string, object>> stateValues;
        private List<object> bufferedScopes;
        private List<KeyValuePair<string, object>> bufferedStateValues;

        internal LogRecord(
            IExternalScopeProvider scopeProvider,
            DateTime timestamp,
            string categoryName,
            LogLevel logLevel,
            EventId eventId,
            string formattedMessage,
            object state,
            Exception exception,
            IReadOnlyList<KeyValuePair<string, object>> stateValues)
        {
            this.ScopeProvider = scopeProvider;

            var activity = Activity.Current;
            if (activity != null)
            {
                this.TraceId = activity.TraceId;
                this.SpanId = activity.SpanId;
                this.TraceState = activity.TraceStateString;
                this.TraceFlags = activity.ActivityTraceFlags;
            }

            this.Timestamp = timestamp;
            this.CategoryName = categoryName;
            this.LogLevel = logLevel;
            this.EventId = eventId;
            this.FormattedMessage = formattedMessage;
            this.State = state;
            this.stateValues = stateValues;
            this.Exception = exception;
        }

        public DateTime Timestamp { get; }

        public ActivityTraceId TraceId { get; }

        public ActivitySpanId SpanId { get; }

        public ActivityTraceFlags TraceFlags { get; }

        public string TraceState { get; }

        public string CategoryName { get; }

        public LogLevel LogLevel { get; }

        public EventId EventId { get; }

        public string FormattedMessage { get; }

        /// <summary>
        /// Gets the raw state attached to the log. Set to <see
        /// langword="null"/> when <see
        /// cref="OpenTelemetryLoggerOptions.ParseStateValues"/> is enabled.
        /// </summary>
        public object State { get; private set; }

        /// <summary>
        /// Gets the parsed state values attached to the log. Set when <see
        /// cref="OpenTelemetryLoggerOptions.ParseStateValues"/> is enabled
        /// otherwise <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Note: StateValues are only available during the lifecycle of the log
        /// message being written. If you need to capture state to be used later
        /// (for example in batching scenarios), call <see cref="Buffer"/> to
        /// safely capture the values (incurs allocation).
        /// </remarks>
        public IReadOnlyList<KeyValuePair<string, object>> StateValues => this.bufferedStateValues ?? this.stateValues;

        public Exception Exception { get; }

        internal IExternalScopeProvider ScopeProvider { get; private set; }

        // Note: Used by unit tests.
        internal IReadOnlyList<KeyValuePair<string, object>> ParsedStateValues => this.stateValues;

        /// <summary>
        /// Executes callback for each currently active scope objects in order
        /// of creation. All callbacks are guaranteed to be called inline from
        /// this method.
        /// </summary>
        /// <remarks>
        /// Note: Scopes are only available during the lifecycle of the log
        /// message being written. If you need to capture scopes to be used
        /// later (for example in batching scenarios), call <see cref="Buffer"/>
        /// to safely capture the values (incurs allocation).
        /// </remarks>
        /// <typeparam name="TState">State.</typeparam>
        /// <param name="callback">The callback to be executed for every scope object.</param>
        /// <param name="state">The state object to be passed into the callback.</param>
        public void ForEachScope<TState>(Action<LogRecordScope, TState> callback, TState state)
        {
            var forEachScopeState = new ScopeForEachState<TState>(callback, state);

            if (this.bufferedScopes != null)
            {
                foreach (object scope in this.bufferedScopes)
                {
                    ScopeForEachState<TState>.ForEachScope(scope, forEachScopeState);
                }
            }
            else if (this.ScopeProvider != null)
            {
                this.ScopeProvider.ForEachScope(ScopeForEachState<TState>.ForEachScope, forEachScopeState);
            }
        }

        /// <summary>
        /// Buffers the states and scopes attached to the log so that they can
        /// be safely processed after the log message lifecycle has ended.
        /// </summary>
        internal void Buffer()
        {
            if (this.stateValues != null && this.bufferedStateValues == null)
            {
                // Note: We copy the state values to capture anything deferred.
                // See:
                // https://github.com/open-telemetry/opentelemetry-dotnet/issues/2905

                this.bufferedStateValues = new(this.stateValues);
            }

            if (this.ScopeProvider != null && this.bufferedScopes == null)
            {
                List<object> scopes = new List<object>();

                this.ScopeProvider?.ForEachScope(AddScopeToBufferedList, scopes);

                this.bufferedScopes = scopes;
            }
        }

        /// <summary>
        /// Clear data which should not be accessed after the log message lifecycle has ended.
        /// </summary>
        internal void Clear()
        {
            this.State = null;
            this.stateValues = null;
            this.ScopeProvider = null;
        }

        private readonly struct ScopeForEachState<TState>
        {
            public static readonly Action<object, ScopeForEachState<TState>> ForEachScope = (object scope, ScopeForEachState<TState> state) =>
            {
                LogRecordScope logRecordScope = new LogRecordScope(scope);

                state.Callback(logRecordScope, state.UserState);
            };

            public readonly Action<LogRecordScope, TState> Callback;

            public readonly TState UserState;

            public ScopeForEachState(Action<LogRecordScope, TState> callback, TState state)
            {
                this.Callback = callback;
                this.UserState = state;
            }
        }
    }
}
