// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using CheckCloudSupport.Docs;
using CheckCloudSupport.OpenAPI;
using Microsoft.OpenApi;

namespace CheckCloudSupport.Extensions;

/// <summary>
/// Contains extensions for the <see cref="OpenApiUrlTreeNodeExtensions"/> class.
/// </summary>
public static class OpenApiUrlTreeNodeExtensions
{
    /// <summary>
    /// Finds the API node that matches the provided path.
    /// </summary>
    /// <param name="parentNode">The parent API URL node to search.</param>
    /// <param name="operation">The API to search for.</param>
    /// <param name="graphNamespace">The namespace the API belongs to.</param>
    /// <returns>The <see cref="OpenApiUrlTreeNode"/> that matches the API path.</returns>
    public static OpenApiUrlTreeNode? GetNodeByPath(this OpenApiUrlTreeNode parentNode, ApiOperation operation, string? graphNamespace)
    {
        graphNamespace ??= "microsoft.graph";

        // Remove any query params
        var querySplit = operation.Path?.Split('?')[0] ?? string.Empty;

        // Check for any override
        querySplit = OpenAPIOverrides.CheckForOverride(querySplit, operation.Method);

        // Break the path by segments
        var pathParts = querySplit.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        OpenApiUrlTreeNode result = parentNode;
        foreach (var segment in pathParts)
        {
            var segmentToMatch = segment;
            OpenApiUrlTreeNode? nextNode = null;

            if (segmentToMatch == "{id}")
            {
                nextNode = result.Children.FirstOrDefault(c => c.Value.Segment.StartsWith('{') &&
                    c.Value.Segment.EndsWith('}') && c.Value.Segment.Contains("id", StringComparison.InvariantCultureIgnoreCase)).Value;
            }
            else
            {
                var matchNodes = result.Children.Where(c => c.Value.Segment.IsEqualIgnoringCase(segmentToMatch) ||
                    c.Value.Segment.IsEqualIgnoringCase($"{graphNamespace}.{segmentToMatch}") ||
                    c.Value.Segment.IsEqualIgnoringCase($"{segmentToMatch}()") ||
                    c.Value.Segment.IsEqualIgnoringCase($"{graphNamespace}.{segmentToMatch}()"));

                if (matchNodes?.Count() == 0 &&
                    segmentToMatch.StartsWith("microsoft.graph", StringComparison.InvariantCultureIgnoreCase))
                {
                    // OpenAPI docs sometimes leave off the "microsoft." prefix
                    var trimmedSegment = segmentToMatch.Replace("microsoft.", string.Empty, StringComparison.InvariantCultureIgnoreCase);

                    matchNodes = result.Children.Where(c => c.Value.Segment.IsEqualIgnoringCase(trimmedSegment) ||
                    c.Value.Segment.IsEqualIgnoringCase($"{trimmedSegment}()"));

                    if (matchNodes?.Count() >= 1)
                    {
                        segmentToMatch = trimmedSegment;
                    }
                }

                if (matchNodes?.Count() > 1)
                {
                    var matchCount = matchNodes.Count();
                }

                nextNode = result.Children.FirstOrDefault(c => c.Value.Segment.IsEqualIgnoringCase(segmentToMatch) ||
                    c.Value.Segment.IsEqualIgnoringCase($"{graphNamespace}.{segmentToMatch}")).Value ??
                    result.Children.FirstOrDefault(c => c.Value.Segment.IsEqualIgnoringCase($"{segmentToMatch}()") ||
                    c.Value.Segment.IsEqualIgnoringCase($"{graphNamespace}.{segmentToMatch}()")).Value;
            }

            if (nextNode == null)
            {
                return null;
            }

            result = nextNode;

            // Return the /bundles/{id} segment to handle
            // omission in OpenAPI
            if (result.Path.EndsWith("\\bundles\\{driveItem-id}"))
            {
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the cloud support status of the API URL node.
    /// </summary>
    /// <param name="node">The API URL node to check.</param>
    /// <param name="method">The HTTP method to check for.</param>
    /// <returns>The <see cref="CloudSupportStatus"/> indicating the cloud support status of the API.</returns>
    public static CloudSupportStatus GetCloudSupportStatus(this OpenApiUrlTreeNode node, HttpMethod? method)
    {
        if (method == null)
        {
            return CloudSupportStatus.Unknown;
        }

        if (node.Path.EndsWith("\\bundles\\{driveItem-id}"))
            {
                method = HttpMethod.Get;
            }

        var supportsGlobal = node.PathItems.ContainsKey("Global") &&
            (node.PathItems["Global"].Operations?.ContainsKey(method) ?? false);
        var supportsUsGov = node.PathItems.ContainsKey("UsGov") &&
            (node.PathItems["UsGov"].Operations?.ContainsKey(method) ?? false) &&
            !OpenAPIOverrides.CheckIfCloudExcluded(node.Path, method, "UsGov");
        var supportsChina = node.PathItems.ContainsKey("China") &&
            (node.PathItems["China"].Operations?.ContainsKey(method) ?? false) &&
            !OpenAPIOverrides.CheckIfCloudExcluded(node.Path, method, "China");

        if (!supportsGlobal)
        {
            // Only process APIs that exist in Global cloud
            return CloudSupportStatus.Unknown;
        }

        if (supportsUsGov && supportsChina)
        {
            return CloudSupportStatus.AllClouds;
        }

        if (!supportsUsGov && !supportsChina)
        {
            return CloudSupportStatus.GlobalOnly;
        }

        return supportsUsGov ? CloudSupportStatus.GlobalAndUSGov : CloudSupportStatus.GlobalAndChina;
    }
}
