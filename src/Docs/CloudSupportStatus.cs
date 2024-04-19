// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace CheckCloudSupport.Docs;

/// <summary>
/// Represents the cloud support status of an API.
/// </summary>
public enum CloudSupportStatus
{
    /// <summary>
    /// Cloud support status is undetermined.
    /// </summary>
    Unknown,

    /// <summary>
    /// API is supported in all public national clouds.
    /// </summary>
    AllClouds,

    /// <summary>
    /// API is supported in the global and US Government clouds only.
    /// </summary>
    GlobalAndUSGov,

    /// <summary>
    /// API is supported in the global and Chinese clouds only.
    /// </summary>
    GlobalAndChina,

    /// <summary>
    /// API is supported in the global cloud only.
    /// </summary>
    GlobalOnly,
}
