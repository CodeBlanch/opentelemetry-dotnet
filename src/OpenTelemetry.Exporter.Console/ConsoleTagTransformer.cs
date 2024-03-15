// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#nullable enable

using OpenTelemetry.Internal;

namespace OpenTelemetry.Exporter;

internal sealed class ConsoleTagTransformer : TagTransformer<string>
{
    public ConsoleTagTransformer(Action<string, string> onLogUnsupportedAttributeType)
        : base(onLogUnsupportedAttributeType)
    {
    }

    protected override string TransformIntegralTag(string key, long value) => $"{key}: {value}";

    protected override string TransformFloatingPointTag(string key, double value) => $"{key}: {value}";

    protected override string TransformBooleanTag(string key, bool value) => $"{key}: {(value ? "true" : "false")}";

    protected override string TransformStringTag(string key, string value) => $"{key}: {value}";

    protected override string TransformArrayTag(string key, Array array)
        => this.TransformStringTag(key, TagTransformerJsonHelper.JsonSerializeArrayTag(array));
}
