// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace CheckCloudSupport.Docs;

/// <summary>
/// Represents the version of a Microsoft Graph API.
/// </summary>
public enum ApiVersion
{
    /// <summary>
    /// Unknown version.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Version v1.0.
    /// </summary>
    V1 = 1,

    /// <summary>
    /// Beta version.
    /// </summary>
    Beta = 2,
}
