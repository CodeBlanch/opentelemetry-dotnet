// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Exporter;

internal sealed class ConsoleTagWriter : TagWriter<List<string>, JsonStringArrayTagWriter.JsonStringArrayTagWriterState>
{
    private readonly List<string> tagStorage = new(1);
    private readonly Action<string, string> onUnsupportedTagDropped;

    public ConsoleTagWriter(Action<string, string> onUnsupportedTagDropped)
        : base(new JsonStringArrayTagWriter())
    {
        Debug.Assert(onUnsupportedTagDropped != null, "onUnsupportedTagDropped was null");

        this.onUnsupportedTagDropped = onUnsupportedTagDropped!;
    }

    public bool TryTransformTag(KeyValuePair<string, object?> tag, [NotNullWhen(true)] out string? result)
    {
        this.tagStorage.Clear();
        if (this.TryWriteTag(this.tagStorage, tag))
        {
            result = this.tagStorage[0];
            return true;
        }

        result = null;
        return false;
    }

    protected override void WriteIntegralTag(List<string> tags, string key, long value)
    {
        tags.Add($"{key}: {value}");
    }

    protected override void WriteFloatingPointTag(List<string> tags, string key, double value)
    {
        tags.Add($"{key}: {value}");
    }

    protected override void WriteBooleanTag(List<string> tags, string key, bool value)
    {
        tags.Add($"{key}: {(value ? "true" : "false")}");
    }

    protected override void WriteStringTag(List<string> tags, string key, string value)
    {
        tags.Add($"{key}: {value}");
    }

    protected override void WriteArrayTag(List<string> tags, string key, JsonStringArrayTagWriter.JsonStringArrayTagWriterState array)
    {
        var result = array.Stream.TryGetBuffer(out var buffer);

        Debug.Assert(result, "result was false");

        tags.Add($"{key}: {Encoding.UTF8.GetString(buffer.Array!, 0, buffer.Count)}");
    }

    protected override void OnUnsupportedTagDropped(
        string tagKey,
        string tagValueTypeFullName)
    {
        this.onUnsupportedTagDropped(tagKey, tagValueTypeFullName);
    }
}
