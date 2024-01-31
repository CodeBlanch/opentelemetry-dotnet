// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

namespace OpenTelemetry.Context;

/// <summary>
/// The abstract context slot.
/// </summary>
/// <typeparam name="T">The type of the underlying value.</typeparam>
public abstract class RuntimeContextSlot<T> : IRuntimeContextSlot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeContextSlot{T}"/> class.
    /// </summary>
    /// <param name="name">The name of the context slot.</param>
    protected RuntimeContextSlot(string name)
    {
        this.Name = name;
    }

    /// <summary>
    /// Gets the name of the context slot.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Get the value from the context slot.
    /// </summary>
    /// <returns>The value retrieved from the context slot.</returns>
    public abstract T Get();

    /// <summary>
    /// Set the value to the context slot.
    /// </summary>
    /// <param name="value">The value to be set.</param>
    public abstract void Set(T value);

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

#nullable enable
    object? IRuntimeContextSlot.Get() => this.Get();

    void IRuntimeContextSlot.Set(object? value)
    {
        if (value is null)
        {
            this.Set(default);
            return;
        }

        if (value is not T typedValue)
        {
            throw new InvalidOperationException($"Value of type '{value.GetType()}' cannot be registered into RuntimeContextSlot of type '{typeof(T)}.'");
        }

        this.Set(typedValue);
    }
#nullable disable

    /// <summary>
    /// Releases the unmanaged resources used by this class and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
    }
}

#nullable enable
internal interface IRuntimeContextSlot : IDisposable
{
    object? Get();

    void Set(object? value);
}
#nullable disable
