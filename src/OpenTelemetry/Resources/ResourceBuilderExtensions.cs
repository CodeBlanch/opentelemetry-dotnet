// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Internal;

namespace OpenTelemetry.Resources;

/// <summary>
/// Describes the detection type to use when auto-resolving <c>service.version</c> when calling <see
/// cref="ResourceBuilderExtensions.AddService(ResourceBuilder, string, string?, string?, bool, string?, ServiceResourceVersionDetectionType?, Assembly?)"/>.
/// </summary>
public enum ServiceResourceVersionDetectionType
{
    /// <summary>
    /// Version taken from <see cref="AssemblyVersionAttribute"/>.
    /// </summary>
    AssemblyVersion,

    /// <summary>
    /// Version taken from <see cref="AssemblyFileVersionAttribute"/>.
    /// </summary>
    AssemblyFileVersion,

    /// <summary>
    /// Version taken from <see cref="AssemblyInformationalVersionAttribute"/>.
    /// </summary>
    AssemblyInformationVersion,

    /// <summary>
    /// Version taken from <see cref="AssemblyInformationalVersionAttribute"/>
    /// with any extra git commit information removed.
    /// </summary>
    PackageVersion
}

/// <summary>
/// Contains extension methods for building <see cref="Resource"/>s.
/// </summary>
public static class ResourceBuilderExtensions
{
    private static readonly string InstanceId = Guid.NewGuid().ToString();

    private static Resource TelemetryResource { get; } = new Resource(new Dictionary<string, object>
    {
        [ResourceSemanticConventions.AttributeTelemetrySdkName] = "opentelemetry",
        [ResourceSemanticConventions.AttributeTelemetrySdkLanguage] = "dotnet",
        [ResourceSemanticConventions.AttributeTelemetrySdkVersion] = Sdk.InformationalVersion,
    });

    /// <summary>
    /// Adds service information to a <see cref="ResourceBuilder"/>
    /// following <a
    /// href="https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/resource/semantic_conventions#service">semantic
    /// conventions</a>.
    /// </summary>
    /// <param name="resourceBuilder"><see cref="ResourceBuilder"/>.</param>
    /// <param name="serviceName">Name of the service.</param>
    /// <param name="serviceNamespace">Optional namespace of the service.</param>
    /// <param name="serviceVersion">Optional version of the service.</param>
    /// <param name="autoGenerateServiceInstanceId">Specify <see langword="true"/> to automatically generate a <see cref="Guid"/> for <paramref name="serviceInstanceId"/> if not supplied.</param>
    /// <param name="serviceInstanceId">Optional unique identifier of the service instance.</param>
    /// <returns>Returns <see cref="ResourceBuilder"/> for chaining.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ResourceBuilder AddService(
        this ResourceBuilder resourceBuilder,
        string serviceName,
        string? serviceNamespace,
        string? serviceVersion,
        bool autoGenerateServiceInstanceId,
        string? serviceInstanceId)
        => AddService(
            resourceBuilder,
            serviceName,
            serviceNamespace,
            serviceVersion,
            autoGenerateServiceInstanceId,
            serviceInstanceId,
            autoServiceVersionDetectionType: null,
            autoServiceVersionDetectionTargetAssembly: null);

    /// <summary>
    /// Adds service information to a <see cref="ResourceBuilder"/>
    /// following <a
    /// href="https://github.com/open-telemetry/opentelemetry-specification/tree/main/specification/resource/semantic_conventions#service">semantic
    /// conventions</a>.
    /// </summary>
    /// <param name="resourceBuilder"><see cref="ResourceBuilder"/>.</param>
    /// <param name="serviceName">Name of the service.</param>
    /// <param name="serviceNamespace">Optional namespace of the
    /// service.</param>
    /// <param name="serviceVersion">Optional version of the service.</param>
    /// <param name="autoGenerateServiceInstanceId">Specify <see
    /// langword="true"/> to automatically generate a <see cref="Guid"/> for
    /// <paramref name="serviceInstanceId"/> if not supplied.</param>
    /// <param name="serviceInstanceId">Optional unique identifier of the
    /// service instance.</param>
    /// <param name="autoServiceVersionDetectionType">Optional <see
    /// cref="ServiceResourceVersionDetectionType"/> to use for automatic
    /// population of service version when <paramref name="serviceVersion"/> is
    /// not supplied.</param>
    /// <param name="autoServiceVersionDetectionTargetAssembly">Optional <see
    /// cref="Assembly"/> to use for automatic population of service version
    /// when <paramref name="serviceVersion"/> is not supplied and <paramref
    /// name="autoServiceVersionDetectionType"/> is set. By default the target
    /// assembly is resolved by calling <see
    /// cref="Assembly.GetEntryAssembly"/>.</param>
    /// <returns>Returns <see cref="ResourceBuilder"/> for chaining.</returns>
    public static ResourceBuilder AddService(
        this ResourceBuilder resourceBuilder,
        string serviceName,
        string? serviceNamespace = null,
        string? serviceVersion = null,
        bool autoGenerateServiceInstanceId = true,
        string? serviceInstanceId = null,
        ServiceResourceVersionDetectionType? autoServiceVersionDetectionType = null,
        Assembly? autoServiceVersionDetectionTargetAssembly = null)
    {
        Dictionary<string, object> resourceAttributes = new Dictionary<string, object>();

        Guard.ThrowIfNullOrEmpty(serviceName);

        resourceAttributes.Add(ResourceSemanticConventions.AttributeServiceName, serviceName);

        if (!string.IsNullOrEmpty(serviceNamespace))
        {
            resourceAttributes.Add(ResourceSemanticConventions.AttributeServiceNamespace, serviceNamespace!);
        }

        if (string.IsNullOrEmpty(serviceVersion) && autoServiceVersionDetectionType.HasValue)
        {
            autoServiceVersionDetectionTargetAssembly ??= Assembly.GetEntryAssembly();

            if (autoServiceVersionDetectionTargetAssembly != null)
            {
                switch (autoServiceVersionDetectionType.Value)
                {
                    case ServiceResourceVersionDetectionType.AssemblyVersion:
                        serviceVersion = autoServiceVersionDetectionTargetAssembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version;
                        break;
                    case ServiceResourceVersionDetectionType.AssemblyFileVersion:
                        serviceVersion = autoServiceVersionDetectionTargetAssembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
                        break;
                    case ServiceResourceVersionDetectionType.AssemblyInformationVersion:
                        serviceVersion = autoServiceVersionDetectionTargetAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                        break;
                    case ServiceResourceVersionDetectionType.PackageVersion:
                        autoServiceVersionDetectionTargetAssembly.TryGetPackageVersion(out serviceVersion);
                        break;
                }
            }
        }

        if (!string.IsNullOrEmpty(serviceVersion))
        {
            resourceAttributes.Add(ResourceSemanticConventions.AttributeServiceVersion, serviceVersion!);
        }

        if (serviceInstanceId == null && autoGenerateServiceInstanceId)
        {
            serviceInstanceId = InstanceId;
        }

        if (serviceInstanceId != null)
        {
            resourceAttributes.Add(ResourceSemanticConventions.AttributeServiceInstance, serviceInstanceId);
        }

        return resourceBuilder.AddResource(new Resource(resourceAttributes));
    }

    /// <summary>
    /// Adds service information to a <see cref="ResourceBuilder"/>
    /// following <a
    /// href="https://github.com/open-telemetry/semantic-conventions/blob/main/docs/resource/README.md#telemetry-sdk">semantic
    /// conventions</a>.
    /// </summary>
    /// <param name="resourceBuilder"><see cref="ResourceBuilder"/>.</param>
    /// <returns>Returns <see cref="ResourceBuilder"/> for chaining.</returns>
    public static ResourceBuilder AddTelemetrySdk(this ResourceBuilder resourceBuilder)
    {
        return resourceBuilder.AddResource(TelemetryResource);
    }

    /// <summary>
    /// Adds attributes to a <see cref="ResourceBuilder"/>.
    /// </summary>
    /// <param name="resourceBuilder"><see cref="ResourceBuilder"/>.</param>
    /// <param name="attributes">An <see cref="IEnumerable{T}"/> of attributes that describe the resource.</param>
    /// <returns>Returns <see cref="ResourceBuilder"/> for chaining.</returns>
    public static ResourceBuilder AddAttributes(this ResourceBuilder resourceBuilder, IEnumerable<KeyValuePair<string, object>> attributes)
    {
        return resourceBuilder.AddResource(new Resource(attributes));
    }

    /// <summary>
    /// Adds resource attributes parsed from OTEL_RESOURCE_ATTRIBUTES, OTEL_SERVICE_NAME environment variables
    /// to a <see cref="ResourceBuilder"/> following the <a
    /// href="https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/resource/sdk.md#specifying-resource-information-via-an-environment-variable">Resource
    /// SDK</a>.
    /// </summary>
    /// <param name="resourceBuilder"><see cref="ResourceBuilder"/>.</param>
    /// <returns>Returns <see cref="ResourceBuilder"/> for chaining.</returns>
    public static ResourceBuilder AddEnvironmentVariableDetector(this ResourceBuilder resourceBuilder)
    {
        Lazy<IConfiguration> configuration = new Lazy<IConfiguration>(() => new ConfigurationBuilder().AddEnvironmentVariables().Build());

        return resourceBuilder
            .AddDetectorInternal(sp => new OtelEnvResourceDetector(sp?.GetService<IConfiguration>() ?? configuration.Value))
            .AddDetectorInternal(sp => new OtelServiceNameEnvVarDetector(sp?.GetService<IConfiguration>() ?? configuration.Value));
    }
}
