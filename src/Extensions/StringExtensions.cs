// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text.RegularExpressions;

namespace CheckCloudSupport.Extensions;

/// <summary>
/// Contains extensions for the <see cref="string"/> type.
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// Normalizes id segments in API paths.
    /// </summary>
    /// <param name="path">The API path to normalize.</param>
    /// <returns>The normalized API path.</returns>
    public static string NormalizeIdSegments(this string path)
    {
        // Handle weird OneDrive path construct
        var normalizedPath = path.Replace("/root:/{item-path}:", "/items/{id}");

        // Handle Chart API's silly '{name}' placeholder which should be '{id}'
        normalizedPath = normalizedPath.Replace("/{name}", "/{id}");

        // Replace any segments wrapped in braces that contain "id"
        normalizedPath = IdSegmentRegex().Replace(normalizedPath, "{id}");

        return normalizedPath;
    }

    /// <summary>
    /// Normalizes parameter names and values in API paths.
    /// </summary>
    /// <param name="path">The API path to normalize.</param>
    /// <returns>The normalized API path.</returns>
    public static string NormalizeParameters(this string path)
    {
        var newPath = path;

        // Extract parameters if present
        var parameterListMatches = ParameterListRegex().Matches(path);

        foreach (var parameterListMatch in parameterListMatches.ToList())
        {
            var parameterList = parameterListMatch.Groups[1].Value;

            var parameterValuePairMatches = ParameterValuePairRegex().Matches(parameterList);

            var parameters = new List<string>();

            foreach (var parameterValuePairMatch in parameterValuePairMatches.ToList())
            {
                var parameterName = parameterValuePairMatch.Groups.GetValueOrDefault("param")?.Value;
                var valueName = parameterValuePairMatch.Groups.GetValueOrDefault("value")?.Value;

                if (!string.IsNullOrEmpty(valueName) && valueName.StartsWith('@'))
                {
                    parameters.Add($"{parameterName}='@{parameterName}'");
                }
                else
                {
                    parameters.Add($"{parameterName}='{{{parameterName}}}'");
                }
            }

            var newParameterList = string.Join(',', parameters);
            newPath = newPath.Replace(parameterList, newParameterList);
        }

        return newPath;
    }

    /// <summary>
    /// Fixes the OneDrive user's drive shortcut in an API path.
    /// </summary>
    /// <param name="path">The API path to fix.</param>
    /// <returns>The fixed API path.</returns>
    public static string FixUserDrivePath(this string path)
    {
        var fixedPath = UsersDriveRegex().Replace(path, "/drives/{id}/");
        return MeDriveRegex().Replace(fixedPath, "/drives/{id}/");
    }

    /// <summary>
    /// Fixes the OneDrive root /drive shortcut in an API path.
    /// </summary>
    /// <param name="path">The API path to fix.</param>
    /// <returns>The fixed API path.</returns>
    public static string FixDriveShortcut(this string path)
    {
        return DriveShortcutRegex().Replace(path, "/drives/{id}/");
    }

    /// <summary>
    /// Fixes the OneDrive share id in an API path.
    /// </summary>
    /// <param name="path">The API path to fix.</param>
    /// <returns>The fixed API path.</returns>
    public static string FixDriveShareId(this string path)
    {
        return path.Replace("/shares/{encoded-sharing-url}", "/shares/{id}");
    }

    /// <summary>
    /// Fixes the well-known mail folder names in API paths.
    /// </summary>
    /// <param name="path">The API path to fix.</param>
    /// <returns>The fixed API path.</returns>
    public static string FixWellKnownMailFoldersId(this string path)
    {
        return WellKnownFolderNamesRegex().Replace(path, "/mailFolders/{id}/");
    }

    /// <summary>
    /// Extracts the namespace from Markdown content.
    /// </summary>
    /// <param name="markdown">The Markdown content to extract from.</param>
    /// <returns>The namespace.</returns>
    public static string? ExtractNamespace(this string markdown)
    {
        var matches = NamespaceLineRegex().Matches(markdown);
        if (matches.Count <= 0)
        {
            return null;
        }

        return matches[0].Groups["namespace"].Value;
    }

    /// <summary>
    /// Compares the string against a provided value, ignoring case.
    /// </summary>
    /// <param name="value">The string to act on.</param>
    /// <param name="compareTo">The string to compare to.</param>
    /// <returns>A value indicating whether the strings are equal, ignoring case.</returns>
    public static bool IsEqualIgnoringCase(this string value, string compareTo)
    {
        int compareValue = string.Compare(value, compareTo, StringComparison.InvariantCultureIgnoreCase);

        // Handle inconsistent formatting for function parameters in OpenAPI
        // Sometimes value placeholders are like '{value}',
        // Sometimes just like {value}
        if (compareValue != 0 && value.Contains('(') && value.Contains('{') && compareTo.Contains('(') && compareTo.Contains('{'))
        {
            var pattern = $"^{compareTo.Replace("'", "'?").Replace("(", "\\(").Replace(")", "\\)")}$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            var isMatch = regex.IsMatch(value);
            return isMatch;
        }

        return compareValue == 0;
    }

    [GeneratedRegex("{[^{}]*id[^{}]*}", RegexOptions.IgnoreCase)]
    private static partial Regex IdSegmentRegex();

    [GeneratedRegex("\\(((?>\\w*='?\\{?[\\w-]*\\}?'?,?)+)\\)")]
    private static partial Regex ParameterListRegex();

    [GeneratedRegex("(?<param>\\w*)='?(?<value>\\{?[\\w-]*\\}?)'?")]
    private static partial Regex ParameterValuePairRegex();

    [GeneratedRegex("^\\/users\\/{id}\\/drive\\/")]
    private static partial Regex UsersDriveRegex();

    [GeneratedRegex("^\\/me\\/drive\\/")]
    private static partial Regex MeDriveRegex();

    [GeneratedRegex("^\\/drive\\/")]
    private static partial Regex DriveShortcutRegex();

    [GeneratedRegex("^\\s*namespace:\\s*(?'namespace'[\\w.]*)\\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex NamespaceLineRegex();

    [GeneratedRegex("\\/mail[Ff]olders\\/\\w*\\/")]
    private static partial Regex WellKnownFolderNamesRegex();
}
