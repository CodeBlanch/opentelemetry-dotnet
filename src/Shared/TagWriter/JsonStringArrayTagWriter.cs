// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable enable

using System.Text.Json;

namespace OpenTelemetry.Internal;

internal sealed class JsonStringArrayTagWriter : ArrayTagWriter<JsonStringArrayTagWriter.JsonStringArrayTagWriterState>
{
    [ThreadStatic]
    private static MemoryStream? threadStream;

    [ThreadStatic]
    private static Utf8JsonWriter? threadWriter;

    public override JsonStringArrayTagWriterState BeginWriteArray()
    {
        var state = EnsureWriter();
        state.Writer.WriteStartArray();
        return state;
    }

    public override void EndWriteArray(JsonStringArrayTagWriterState state)
    {
        state.Writer.WriteEndArray();
        state.Writer.Flush();
    }

    public override void WriteBooleanTag(JsonStringArrayTagWriterState state, bool value)
    {
        state.Writer.WriteBooleanValue(value);
    }

    public override void WriteFloatingPointTag(JsonStringArrayTagWriterState state, double value)
    {
        state.Writer.WriteNumberValue(value);
    }

    public override void WriteIntegralTag(JsonStringArrayTagWriterState state, long value)
    {
        state.Writer.WriteNumberValue(value);
    }

    public override void WriteNullTag(JsonStringArrayTagWriterState state)
    {
        state.Writer.WriteNullValue();
    }

    public override void WriteStringTag(JsonStringArrayTagWriterState state, string value)
    {
        state.Writer.WriteStringValue(value);
    }

    private static JsonStringArrayTagWriterState EnsureWriter()
    {
        if (threadStream == null)
        {
            threadStream = new MemoryStream();
            threadWriter = new Utf8JsonWriter(threadStream);
            return new(threadStream, threadWriter);
        }
        else
        {
            threadStream.SetLength(0);
            threadWriter!.Reset(threadStream);
            return new(threadStream, threadWriter);
        }
    }

    public readonly struct JsonStringArrayTagWriterState(MemoryStream stream, Utf8JsonWriter writer)
    {
        public MemoryStream Stream { get; } = stream;

        public Utf8JsonWriter Writer { get; } = writer;
    }
}
