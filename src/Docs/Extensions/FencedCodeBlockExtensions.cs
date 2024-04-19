// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Markdig.Syntax;
using Microsoft.Extensions.Logging;

namespace CheckCloudSupport.Docs.Extensions;

/// <summary>
/// Contains extensions for the <see cref="FencedCodeBlock"/> class.
/// </summary>
public static class FencedCodeBlockExtensions
{
    /// <summary>
    /// Gets a list of API operations from a fenced code block.
    /// </summary>
    /// <param name="codeBlock">The <see cref="FencedCodeBlock"/> instance to get the list from.</param>
    /// <returns>A list of API operations.</returns>
    public static List<ApiOperation> GetApiOperations(this FencedCodeBlock codeBlock)
    {
        var operations = new List<ApiOperation>();

        for (int i = 0; i < codeBlock.Lines.Count; i++)
        {
            try
            {
                var operation = ApiOperation.CreateFromStringLine(codeBlock.Lines.Lines[i]);
                if (operation != null)
                {
                    operations.Add(operation);
                }
            }
            catch (ArgumentException ex)
            {
                OutputLogger.Logger?.LogWarning(
                    "Error parsing API line {line}: {message}",
                    codeBlock.Lines.Lines[i],
                    ex.Message);
            }
        }

        return operations;
    }
}
