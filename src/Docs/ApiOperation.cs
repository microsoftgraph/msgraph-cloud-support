// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using CheckCloudSupport.Extensions;
using Markdig.Helpers;

namespace CheckCloudSupport.Docs;

/// <summary>
/// Represents an API operation.
/// </summary>
public class ApiOperation
{
    /// <summary>
    /// Gets the HTTP method for the API operation.
    /// </summary>
    public HttpMethod? Method { get; private set; }

    /// <summary>
    /// Gets the API path for the API operation.
    /// </summary>
    public string? Path { get; private set; }

    /// <summary>
    /// Creates an instance of the <see cref="ApiOperation"/> class from a <see cref="StringLine"/> instance.
    /// </summary>
    /// <param name="line">The <see cref="StringLine"/> instance to create from.</param>
    /// <returns><see cref="ApiOperation"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if the <see cref="StringLine"/> does not contain valid data.</exception>
    public static ApiOperation? CreateFromStringLine(StringLine line)
    {
        var lineText = line.Slice.ToString();
        if (string.IsNullOrWhiteSpace(lineText))
        {
            return null;
        }

        var parts = lineText.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            throw new ArgumentException($"Invalid line text: {lineText}");
        }

        var operation = new ApiOperation();

        try
        {
            operation.Method = new HttpMethod(parts[0]);
        }
        catch (Exception)
        {
            throw new ArgumentException($"Invalid HTTP operation: {parts[0]}");
        }

        operation.Path = parts[1]
            .MakePathRelativeToVersion()
            .NormalizeIdSegments()
            .NormalizeParameters()
            .FixUserDrivePath()
            .FixDriveShortcut()
            .FixDriveShareId()
            .FixWellKnownMailFoldersId();

        return operation;
    }
}
