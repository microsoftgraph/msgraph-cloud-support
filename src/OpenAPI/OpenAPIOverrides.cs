// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.Json;

namespace CheckCloudSupport.OpenAPI;

/// <summary>
/// Contains helper methods to lookup override paths.
/// </summary>
public static class OpenAPIOverrides
{
    private static List<Override>? overrides = null;
    private static List<CloudExclusion>? cloudExclusions = null;

    /// <summary>
    /// Returns override API path for a given API path, if one exists.
    /// </summary>
    /// <param name="originalPath">The original API path to check for an override for.</param>
    /// <param name="method">The HTTP method to check for an override for.</param>
    /// <returns>The override path.</returns>
    public static string CheckForOverride(string originalPath, HttpMethod? method)
    {
        if (overrides == null)
        {
            LoadOverridesFromJson();
        }

        var apiOverride = overrides?.SingleOrDefault(o => o.ApiPath?.ToLower() == originalPath.ToLower());
        if (!string.IsNullOrEmpty(apiOverride?.Operation) &&
            method != null &&
            string.Compare(apiOverride.Operation, method.Method, StringComparison.InvariantCultureIgnoreCase) != 0)
        {
            return originalPath;
        }

        return apiOverride == null ? originalPath : apiOverride.OverridePath ?? originalPath;
    }

    /// <summary>
    /// Checks if a cloud is excluded for a given API path and method.
    /// </summary>
    /// <param name="apiPath">The API path to check for an exclusion for.</param>
    /// <param name="method">The HTTP method to check for an exclusion for.</param>
    /// <param name="cloud">The cloud to check.</param>
    /// <returns>True if the given cloud is excluded.</returns>
    public static bool CheckIfCloudExcluded(string apiPath, HttpMethod? method, string cloud)
    {
        if (cloudExclusions == null)
        {
            LoadCloudExclusionsFromJson();
        }

        apiPath = apiPath.Replace("\\", "/");
        return cloudExclusions?.Any(e => string.Compare(e.ApiPath, apiPath, StringComparison.InvariantCultureIgnoreCase) == 0 &&
            string.Compare(e.Operation, method?.Method, StringComparison.InvariantCultureIgnoreCase) == 0 &&
            string.Compare(e.Cloud, cloud, StringComparison.InvariantCultureIgnoreCase) == 0) ?? false;
    }

    private static void LoadOverridesFromJson()
    {
        var json = File.ReadAllText("overrides.json");
        overrides = JsonSerializer.Deserialize<List<Override>>(json);
    }

    private static void LoadCloudExclusionsFromJson()
    {
        var json = File.ReadAllText("cloud-exclusions.json");
        cloudExclusions = JsonSerializer.Deserialize<List<CloudExclusion>>(json);
    }
}
