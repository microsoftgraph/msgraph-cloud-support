// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Markdig.Syntax;

namespace CheckCloudSupport.Docs.Extensions;

/// <summary>
/// Contains extensions for the <see cref="HeadingBlock"/> class.
/// </summary>
public static class HeadingBlockExtensions
{
    /// <summary>
    /// Checks if the heading text equals a given value.
    /// </summary>
    /// <param name="block">The <see cref="HeadingBlock"/> instance to check.</param>
    /// <param name="value">The value to check against the heading text.</param>
    /// <returns>A value indicating whether the values are equal.</returns>
    public static bool TextEquals(this HeadingBlock block, string value)
    {
        var blockText = block.Inline?.FirstChild?.ToString() ?? string.Empty;
        return string.Compare(blockText, value, StringComparison.InvariantCultureIgnoreCase) == 0;
    }
}
