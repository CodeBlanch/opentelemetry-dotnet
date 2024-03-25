// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable enable

using System.Diagnostics;
using System.Text.Json;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Exporter.Zipkin.Implementation;

internal sealed class ZipkinTagWriter : TagWriter<Utf8JsonWriter, JsonStringArrayTagWriter.JsonStringArrayTagWriterState>
{
    private ZipkinTagWriter()
        : base(new JsonStringArrayTagWriter())
    {
    }

    public static ZipkinTagWriter Instance { get; } = new();

    protected override void WriteIntegralTag(Utf8JsonWriter writer, string key, long value)
        => writer.WriteNumber(key, value);

    protected override void WriteFloatingPointTag(Utf8JsonWriter writer, string key, double value)
        => writer.WriteNumber(key, value);

    protected override void WriteBooleanTag(Utf8JsonWriter writer, string key, bool value)
        => writer.WriteBoolean(key, value);

    protected override void WriteStringTag(Utf8JsonWriter writer, string key, string value)
        => writer.WriteString(key, value);

    protected override void WriteArrayTag(Utf8JsonWriter writer, string key, JsonStringArrayTagWriter.JsonStringArrayTagWriterState array)
    {
        var result = array.Stream.TryGetBuffer(out var buffer);

        Debug.Assert(result, "result was false");

        writer.WritePropertyName(key);
        writer.WriteStringValue(buffer);
    }

    protected override void OnUnsupportedTagDropped(
        string tagKey,
        string tagValueTypeFullName)
    {
        ZipkinExporterEventSource.Log.UnsupportedAttributeType(
            tagValueTypeFullName,
            tagKey);
    }
}
