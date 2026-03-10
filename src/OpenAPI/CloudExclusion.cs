// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json.Serialization;

namespace CheckCloudSupport.OpenAPI;

/// <summary>
/// Represents a cloud that should be excluded for a given API path.
/// This is necessary because some APIs are included in the OpenAPI for clouds
/// and are technically "there", but they don't function.
/// </summary>
public class CloudExclusion
{
    /// <summary>
    /// Gets or sets the API path to override.
    /// </summary>
    [JsonPropertyName("apiPath")]
    public string? ApiPath { get; set; }

    /// <summary>
    /// Gets or sets the HTTP operation.
    /// </summary>
    [JsonPropertyName("operation")]
    public string? Operation { get;  set; }

    /// <summary>
    /// Gets or sets the API doc file name. Setting this property excludes
    /// all APIs in the specified file from the specified cloud.
    /// </summary>
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    /// <summary>
    /// Gets or sets the cloud to exclude.
    /// </summary>
    [JsonPropertyName("cloud")]
    public string? Cloud { get; set; }
}
