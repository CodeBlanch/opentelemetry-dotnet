// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Context;

#nullable enable

public readonly struct RuntimeContextValues
{
    internal readonly Dictionary<string, object?>? Values;

    internal RuntimeContextValues(Dictionary<string, object?> values)
    {
        this.Values = values;
    }
}

public static class RuntimeContextValuesExtensions
{
    public static Baggage GetBaggage(this RuntimeContextValues context)
    {
        var values = context.Values;
        if (values != null
            && values.TryGetValue(Baggage.RuntimeContextSlotKey, out var baggageValue)
            && baggageValue is Baggage.BaggageHolder baggageHolder)
        {
            return baggageHolder.Baggage;
        }

        return default;
    }

    public static RuntimeContextValues SetBaggage(this RuntimeContextValues context, Baggage baggage)
    {
        var currentValues = context.Values;

        var newValues = currentValues == null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(currentValues);

        newValues[Baggage.RuntimeContextSlotKey] = baggage;

        return new(newValues);
    }

    public static ActivityContext GetActivityContext(this RuntimeContextValues context)
    {
        var values = context.Values;
        if (values != null
            && values.TryGetValue(RuntimeContext.RuntimeContextValuesActivityKey, out var activityValue)
            && activityValue is Activity activity)
        {
            return activity.Context;
        }

        return default;
    }

    public static RuntimeContextValues SetActivityContext(this RuntimeContextValues context, ActivityContext activityContext)
    {
        var currentValues = context.Values;

        var newValues = currentValues == null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(currentValues);

        if (activityContext == default)
        {
            newValues[RuntimeContext.RuntimeContextValuesActivityKey] = null;
        }
        else
        {
            var activity = new Activity(string.Empty);

            typeof(Activity)
                .GetField("_traceId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(activity, activityContext.TraceId.ToHexString());

            typeof(Activity)
                .GetField("_spanId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(activity, activityContext.SpanId.ToHexString());

            activity.ActivityTraceFlags = activityContext.TraceFlags;

            activity.TraceStateString = activityContext.TraceState;

            // Note: Remote information will be lost. Any Activity created under
            // this context will think it has a local parent.

            newValues[RuntimeContext.RuntimeContextValuesActivityKey] = activity;
        }

        return new(newValues);
    }
}

internal sealed class RuntimeContextScope : IDisposable
{
    private readonly RuntimeContextValues previousValues;

    public RuntimeContextScope(RuntimeContextValues previousValues)
    {
        this.previousValues = previousValues;
    }

    public void Dispose()
    {
        RuntimeContext.SetValues(this.previousValues);
    }
}

#nullable disable

/// <summary>
/// Generic runtime context management API.
/// </summary>
public static class RuntimeContext
{
    internal const string RuntimeContextValuesActivityKey = "otel.activity";

    private static readonly ConcurrentDictionary<string, IRuntimeContextSlot> Slots = new();

    private static Type contextSlotType = typeof(AsyncLocalRuntimeContextSlot<>);

    /// <summary>
    /// Gets or sets the actual context carrier implementation.
    /// </summary>
    public static Type ContextSlotType
    {
        get => contextSlotType;
        set
        {
            Guard.ThrowIfNull(value, nameof(value));

            if (value == typeof(AsyncLocalRuntimeContextSlot<>))
            {
                contextSlotType = value;
            }
            else if (value == typeof(ThreadLocalRuntimeContextSlot<>))
            {
                contextSlotType = value;
            }
#if NETFRAMEWORK
            else if (value == typeof(RemotingRuntimeContextSlot<>))
            {
                contextSlotType = value;
            }
#endif
            else
            {
                throw new NotSupportedException($"{value} is not a supported type.");
            }
        }
    }

    /// <summary>
    /// Register a named context slot.
    /// </summary>
    /// <param name="slotName">The name of the context slot.</param>
    /// <typeparam name="T">The type of the underlying value.</typeparam>
    /// <returns>The slot registered.</returns>
    public static RuntimeContextSlot<T> RegisterSlot<T>(string slotName)
    {
        Guard.ThrowIfNullOrEmpty(slotName);
        RuntimeContextSlot<T> slot = null;

        lock (Slots)
        {
            if (Slots.ContainsKey(slotName))
            {
                throw new InvalidOperationException($"Context slot already registered: '{slotName}'");
            }

            if (ContextSlotType == typeof(AsyncLocalRuntimeContextSlot<>))
            {
                slot = new AsyncLocalRuntimeContextSlot<T>(slotName);
            }
            else if (ContextSlotType == typeof(ThreadLocalRuntimeContextSlot<>))
            {
                slot = new ThreadLocalRuntimeContextSlot<T>(slotName);
            }

#if NETFRAMEWORK
            else if (ContextSlotType == typeof(RemotingRuntimeContextSlot<>))
            {
                slot = new RemotingRuntimeContextSlot<T>(slotName);
            }
#endif

            Slots[slotName] = slot;
            return slot;
        }
    }

    /// <summary>
    /// Get a registered slot from a given name.
    /// </summary>
    /// <param name="slotName">The name of the context slot.</param>
    /// <typeparam name="T">The type of the underlying value.</typeparam>
    /// <returns>The slot previously registered.</returns>
    public static RuntimeContextSlot<T> GetSlot<T>(string slotName)
    {
        Guard.ThrowIfNullOrEmpty(slotName);
        var slot = GuardNotFound(slotName);
        var contextSlot = Guard.ThrowIfNotOfType<RuntimeContextSlot<T>>(slot);
        return contextSlot;
    }

#nullable enable
    public static RuntimeContextValues GetValues()
    {
        Dictionary<string, object?> values = new(Slots.Count + 1);

        foreach (var slot in Slots)
        {
            values.Add(slot.Key, slot.Value.Get());
        }

        values[RuntimeContextValuesActivityKey] = Activity.Current;

        return new(values);
    }

    public static IDisposable Attach(RuntimeContextValues runtimeContextValues)
    {
        var current = GetValues();

        SetValues(runtimeContextValues);

        return new RuntimeContextScope(current);
    }
#nullable disable

    /// <summary>
    /// Sets the value to a registered slot.
    /// </summary>
    /// <param name="slotName">The name of the context slot.</param>
    /// <param name="value">The value to be set.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetValue<T>(string slotName, T value)
    {
        GetSlot<T>(slotName).Set(value);
    }

    /// <summary>
    /// Gets the value from a registered slot.
    /// </summary>
    /// <param name="slotName">The name of the context slot.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>The value retrieved from the context slot.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetValue<T>(string slotName)
    {
        return GetSlot<T>(slotName).Get();
    }

    /// <summary>
    /// Sets the value to a registered slot.
    /// </summary>
    /// <param name="slotName">The name of the context slot.</param>
    /// <param name="value">The value to be set.</param>
    public static void SetValue(string slotName, object value)
    {
        Guard.ThrowIfNullOrEmpty(slotName);
        var slot = GuardNotFound(slotName);
        var runtimeContextSlotValueAccessor = Guard.ThrowIfNotOfType<IRuntimeContextSlotValueAccessor>(slot);
        runtimeContextSlotValueAccessor.Value = value;
    }

    /// <summary>
    /// Gets the value from a registered slot.
    /// </summary>
    /// <param name="slotName">The name of the context slot.</param>
    /// <returns>The value retrieved from the context slot.</returns>
    public static object GetValue(string slotName)
    {
        Guard.ThrowIfNullOrEmpty(slotName);
        var slot = GuardNotFound(slotName);
        var runtimeContextSlotValueAccessor = Guard.ThrowIfNotOfType<IRuntimeContextSlotValueAccessor>(slot);
        return runtimeContextSlotValueAccessor.Value;
    }

    // For testing purpose
    internal static void Clear()
    {
        Slots.Clear();
    }

    internal static void SetValues(RuntimeContextValues runtimeContextValues)
    {
        var newValues = runtimeContextValues.Values;

        foreach (var kvp in Slots)
        {
            if (newValues.TryGetValue(kvp.Key, out var newValue))
            {
                kvp.Value.Set(newValue);
            }
            else
            {
                kvp.Value.Set(null);
            }
        }

        if (newValues.TryGetValue(RuntimeContextValuesActivityKey, out var activityValue)
            && activityValue is Activity activity)
        {
            Activity.Current = activity;
        }
        else
        {
            Activity.Current = null;
        }
    }

    private static object GuardNotFound(string slotName)
    {
        if (!Slots.TryGetValue(slotName, out var slot))
        {
            throw new ArgumentException($"Context slot not found: '{slotName}'");
        }

        return slot;
    }
}
