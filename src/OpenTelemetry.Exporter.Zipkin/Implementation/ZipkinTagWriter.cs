// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable enable

using System.Text.Json;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Exporter.Zipkin.Implementation;

internal sealed class ZipkinTagWriter : JsonStringArrayTagWriter<Utf8JsonWriter>
{
    private ZipkinTagWriter()
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

    protected override void WriteArrayTag(Utf8JsonWriter writer, string key, ArraySegment<byte> arrayUtf8JsonBytes)
    {
        writer.WritePropertyName(key);
        writer.WriteStringValue(arrayUtf8JsonBytes);
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
