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
    /// <param name="filePath">The API document file path.</param>
    /// <returns>The <see cref="CloudSupportStatus"/> indicating the cloud support status of the API.</returns>
    public static CloudSupportStatus GetCloudSupportStatus(
        this OpenApiUrlTreeNode node,
        HttpMethod? method,
        string filePath)
    {
        if (method == null)
        {
            return CloudSupportStatus.Unknown;
        }

        var fileName = Path.GetFileName(filePath);

        if (node.Path.EndsWith("\\bundles\\{driveItem-id}"))
        {
            method = HttpMethod.Get;
        }

        var supportStatus = node.PathItems.TryGetValue("Global", out IOpenApiPathItem? globalValue) &&
            (globalValue.Operations?.ContainsKey(method) ?? false)
                ? CloudSupportStatus.Global
                : CloudSupportStatus.Unknown;
        if (supportStatus == CloudSupportStatus.Unknown)
        {
            return supportStatus;
        }

        if (node.PathItems.TryGetValue("UsGov", out IOpenApiPathItem? usGovValue) &&
            (usGovValue.Operations?.ContainsKey(method) ?? false))
        {
            supportStatus |= GetUsGovCloudSupportStatus(node.Path, method, fileName);
        }

        if (node.PathItems.TryGetValue("China", out IOpenApiPathItem? chinaValue) &&
            (chinaValue.Operations?.ContainsKey(method) ?? false) &&
            !OpenAPIOverrides.CheckIfCloudExcluded(node.Path, method, "China") &&
            !OpenAPIOverrides.CheckIfCloudExcludedForFile(fileName, "China"))
        {
            supportStatus |= CloudSupportStatus.China;
        }

        return supportStatus;
    }

    private static CloudSupportStatus GetUsGovCloudSupportStatus(
        string apiPath,
        HttpMethod? method,
        string fileName)
    {
        var supportStatus = CloudSupportStatus.USGov;

        if (OpenAPIOverrides.CheckIfCloudExcluded(apiPath, method, "UsGov") ||
            OpenAPIOverrides.CheckIfCloudExcludedForFile(fileName, "UsGov"))
        {
            return CloudSupportStatus.Unknown;
        }

        if (OpenAPIOverrides.CheckIfCloudExcluded(apiPath, method, "UsGovL4") ||
            OpenAPIOverrides.CheckIfCloudExcludedForFile(fileName, "UsGovL4"))
        {
            supportStatus &= ~CloudSupportStatus.USGovL4;
        }

        if (OpenAPIOverrides.CheckIfCloudExcluded(apiPath, method, "UsGovL5") ||
            OpenAPIOverrides.CheckIfCloudExcludedForFile(fileName, "UsGovL5"))
        {
            supportStatus &= ~CloudSupportStatus.USGovL5;
        }

        return supportStatus;
    }
}
