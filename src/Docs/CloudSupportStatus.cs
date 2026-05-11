// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace CheckCloudSupport.Docs;

/// <summary>
/// Represents the cloud support status of an API.
/// </summary>
[Flags]
public enum CloudSupportStatus
{
    /// <summary>
    /// Cloud support status is undetermined.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// API is supported in Chinese cloud.
    /// </summary>
    China = 1 << 0,

    /// <summary>
    /// API is supported in the global cloud.
    /// </summary>
    Global = 1 << 1,

    /// <summary>
    /// API is supported in the US Government L4 cloud.
    /// </summary>
    USGovL4 = 1 << 2,

    /// <summary>
    /// API is supported in the US Government L5 cloud.
    /// </summary>
    USGovL5 = 1 << 3,

    /// <summary>
    /// API is supported in the US Government cloud (L4 and L5).
    /// </summary>
    USGov = USGovL4 | USGovL5,

    /// <summary>
    /// API is supported in all clouds (China, Global, and US Government).
    /// </summary>
    AllClouds = China | Global | USGov,

    /// <summary>
    /// API is supported in both Global and US Government clouds.
    /// </summary>
    GlobalAndUSGov = Global | USGov,

    /// <summary>
    /// API is supported in both Global and Chinese clouds.
    /// </summary>
    GlobalAndChina = Global | China,
}
