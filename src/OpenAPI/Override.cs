// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json.Serialization;

namespace CheckCloudSupport.OpenAPI;

/// <summary>
/// Represents an API path override.
/// </summary>
public class Override
{
    /// <summary>
    /// Gets or sets the API path to override.
    /// </summary>
    [JsonPropertyName("apiPath")]
    public string? ApiPath { get; set; }

    /// <summary>
    /// Gets or sets the override path.
    /// </summary>
    [JsonPropertyName("overridePath")]
    public string? OverridePath { get; set; }

    /// <summary>
    /// Gets or sets the HTTP operation.
    /// </summary>
    [JsonPropertyName("operation")]
    public string? Operation { get;  set; }
}
