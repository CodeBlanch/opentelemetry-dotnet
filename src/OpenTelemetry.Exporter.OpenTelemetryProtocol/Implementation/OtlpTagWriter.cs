// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable enable

using Google.Protobuf.Collections;
using OpenTelemetry.Internal;
using OtlpCommon = OpenTelemetry.Proto.Common.V1;

namespace OpenTelemetry.Exporter.OpenTelemetryProtocol.Implementation;

internal sealed class OtlpTagWriter : TagWriter<RepeatedField<OtlpCommon.KeyValue>, OtlpCommon.ArrayValue>
{
    private OtlpTagWriter()
        : base(new OtlpArrayTagWriter())
    {
    }

    public static OtlpTagWriter Instance { get; } = new();

    internal static OtlpCommon.AnyValue ToAnyValue(long value)
        => new() { IntValue = value };

    internal static OtlpCommon.AnyValue ToAnyValue(double value)
       => new() { DoubleValue = value };

    internal static OtlpCommon.AnyValue ToAnyValue(bool value)
        => new() { BoolValue = value };

    internal static OtlpCommon.AnyValue ToAnyValue(string value)
        => new() { StringValue = value };

    protected override void WriteIntegralTag(RepeatedField<OtlpCommon.KeyValue> tags, string key, long value)
    {
        tags.Add(new OtlpCommon.KeyValue { Key = key, Value = ToAnyValue(value) });
    }

    protected override void WriteFloatingPointTag(RepeatedField<OtlpCommon.KeyValue> tags, string key, double value)
    {
        tags.Add(new OtlpCommon.KeyValue { Key = key, Value = ToAnyValue(value) });
    }

    protected override void WriteBooleanTag(RepeatedField<OtlpCommon.KeyValue> tags, string key, bool value)
    {
        tags.Add(new OtlpCommon.KeyValue { Key = key, Value = ToAnyValue(value) });
    }

    protected override void WriteStringTag(RepeatedField<OtlpCommon.KeyValue> tags, string key, string value)
    {
        tags.Add(new OtlpCommon.KeyValue { Key = key, Value = ToAnyValue(value) });
    }

    protected override void WriteArrayTag(RepeatedField<OtlpCommon.KeyValue> tags, string key, OtlpCommon.ArrayValue value)
    {
        tags.Add(new OtlpCommon.KeyValue
        {
            Key = key,
            Value = new OtlpCommon.AnyValue
            {
                ArrayValue = value,
            },
        });
    }

    protected override void OnUnsupportedTagDropped(
        string tagKey,
        string tagValueTypeFullName)
    {
        OpenTelemetryProtocolExporterEventSource.Log.UnsupportedAttributeType(
            tagValueTypeFullName,
            tagKey);
    }

    private sealed class OtlpArrayTagWriter : ArrayTagWriter<OtlpCommon.ArrayValue>
    {
        public override OtlpCommon.ArrayValue BeginWriteArray() => new();

        public override void WriteNullTag(OtlpCommon.ArrayValue array)
        {
            array.Values.Add(new OtlpCommon.AnyValue());
        }

        public override void WriteIntegralTag(OtlpCommon.ArrayValue array, long value)
        {
            array.Values.Add(ToAnyValue(value));
        }

        public override void WriteFloatingPointTag(OtlpCommon.ArrayValue array, double value)
        {
            array.Values.Add(ToAnyValue(value));
        }

        public override void WriteBooleanTag(OtlpCommon.ArrayValue array, bool value)
        {
            array.Values.Add(ToAnyValue(value));
        }

        public override void WriteStringTag(OtlpCommon.ArrayValue array, string value)
        {
            array.Values.Add(ToAnyValue(value));
        }

        public override void EndWriteArray(OtlpCommon.ArrayValue array)
        {
        }
    }
}
