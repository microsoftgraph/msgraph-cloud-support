// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using CheckCloudSupport.Docs.Extensions;
using CheckCloudSupport.Extensions;
using Markdig;
using Markdig.Syntax;
using Microsoft.Extensions.Logging;

namespace CheckCloudSupport.Docs;

/// <summary>
/// Represents a Markdown file that documents a Microsoft Graph API.
/// </summary>
public class ApiDocument
{
    private ApiDocument(string filePath)
    {
        FilePath = filePath;
        ApiOperations = [];
        CloudSupportStatus = CloudSupportStatus.Unknown;
        ZonePivotsEnabled = false;
    }

    /// <summary>
    /// Gets the list of API operations in this document.
    /// </summary>
    public List<ApiOperation> ApiOperations { get; private set; }

    /// <summary>
    /// Gets the path to the Markdown file for this document.
    /// </summary>
    public string FilePath { get; private set; }

    /// <summary>
    /// Gets or sets the cloud support status for the API in this document.
    /// </summary>
    public CloudSupportStatus CloudSupportStatus { get; set; }

    /// <summary>
    /// Gets the namespace declared in the document.
    /// </summary>
    public string? GraphNameSpace { get; private set; }

    /// <summary>
    /// Gets a value indicating whether zone pivots are enabled in this document.
    /// </summary>
    public bool ZonePivotsEnabled { get; private set; }

    private MarkdownDocument? MarkdownDocument { get; set; }

    /// <summary>
    /// Creates an instance of the <see cref="ApiDocument"/> class from a Markdown file.
    /// </summary>
    /// <param name="filePath">The path to the Markdown file to create from.</param>
    /// <returns>A task representing the asynchronous create operation. The result of the task contains the created <see cref="ApiDocument"/>.</returns>
    public static async Task<ApiDocument> CreateFromMarkdownFile(string filePath)
    {
        var doc = new ApiDocument(filePath);
        await doc.LoadMarkdown();
        return doc;
    }

    /// <summary>
    /// Adds or updates the INCLUDE line indicating cloud support status.
    /// </summary>
    /// <param name="removeOldIncludes">If true, location of existing INCLUDE line is ignored.</param>
    /// <param name="includeDirectory">The directory containing the INCLUDE files.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddOrUpdateIncludeLine(bool removeOldIncludes, string? includeDirectory = null)
    {
        var lines = new List<string>(await File.ReadAllLinesAsync(FilePath));
        var relativeIncludeDirectoryPath = string.IsNullOrEmpty(includeDirectory) ?
            "../../includes" : GetIncludePathRelativeToFile(FilePath, includeDirectory);

        // Check if INCLUDE line already exists
        var includeLine = lines.FirstOrDefault(line => line.Contains("[!INCLUDE [national-cloud-support]"));

        if (removeOldIncludes && !string.IsNullOrEmpty(includeLine))
        {
            RemoveAllIncludeLines(lines);
            includeLine = string.Empty;
        }

        if (string.IsNullOrEmpty(includeLine))
        {
            // Find the insert location
            var insertIndex = FindInsertIncludeLineIndex(lines);
            lines.Insert(insertIndex, string.Empty);
            lines.Insert(insertIndex, GetIncludeLine(CloudSupportStatus, relativeIncludeDirectoryPath));
            if (!string.IsNullOrEmpty(lines[insertIndex - 1]))
            {
                lines.Insert(insertIndex, string.Empty);
            }
        }
        else
        {
            // Get the index
            var includeIndex = lines.IndexOf(includeLine);
            lines[includeIndex] = GetIncludeLine(CloudSupportStatus, relativeIncludeDirectoryPath);
        }

        // Write file back
        await File.WriteAllLinesAsync(FilePath, lines);
    }

    /// <summary>
    /// Adds or updates the INCLUDE lines for zone pivots indicating cloud support status.
    /// </summary>
    /// <param name="v1Status">Support status for the v1 pivot.</param>
    /// <param name="betaStatus">Support status for the beta pivot.</param>
    /// <param name="includeDirectory">The directory containing the INCLUDE files.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddOrUpdatePivotedIncludeLine(CloudSupportStatus v1Status, CloudSupportStatus betaStatus, string includeDirectory)
    {
        var lines = new List<string>(await File.ReadAllLinesAsync(FilePath));
        var relativeIncludeDirectoryPath = GetIncludePathRelativeToFile(FilePath, includeDirectory);

        var insertIndex = RemoveAllIncludeLines(lines);
        if (insertIndex == -1)
        {
            insertIndex = FindInsertIncludeLineIndex(lines);
        }

        lines.Insert(insertIndex, string.Empty);
        lines.Insert(insertIndex, ":::zone-end");
        lines.Insert(insertIndex, GetIncludeLine(betaStatus, relativeIncludeDirectoryPath));
        lines.Insert(insertIndex, ":::zone pivot=\"graph-preview\"");
        lines.Insert(insertIndex, string.Empty);
        lines.Insert(insertIndex, ":::zone-end");
        lines.Insert(insertIndex, GetIncludeLine(v1Status, relativeIncludeDirectoryPath));
        lines.Insert(insertIndex, ":::zone pivot=\"graph-v1\"");
        if (!string.IsNullOrEmpty(lines[insertIndex - 1]))
        {
            lines.Insert(insertIndex, string.Empty);
        }

        // Write file back
        await File.WriteAllLinesAsync(FilePath, lines);
    }

    /// <summary>
    /// Removes all INCLUDE lines for national cloud support from the provided list of lines.
    /// </summary>
    /// <param name="lines">The list of lines from the markdown file.</param>
    /// <returns>The index at which the INCLUDE lines were removed, or -1 if none were found.</returns>
    internal static int RemoveAllIncludeLines(List<string> lines)
    {
        var includeLineCount = lines.Count(line => line.Contains("[!INCLUDE [national-cloud-support]"));
        if (includeLineCount <= 0)
        {
            return -1;
        }

        if (includeLineCount == 1)
        {
            var includeIndex = lines.FindIndex(line => line.Contains("[!INCLUDE [national-cloud-support]"));
            lines.RemoveAt(includeIndex);

            // Remove trailing blank line if present
            if (string.IsNullOrEmpty(lines[includeIndex]))
            {
                lines.RemoveAt(includeIndex);
            }

            return includeIndex;
        }

        // Find the first include line
        var firstIncludeIndex = lines.FindIndex(line => line.Contains("[!INCLUDE [national-cloud-support]"));

        // Walk backwards to find zone pivot
        for (int i = firstIncludeIndex - 1; i >= 0; i--)
        {
            if (lines[i].StartsWith(":::zone pivot="))
            {
                firstIncludeIndex = i;
                break;
            }
        }

        // Walk forward to find the end of the zone pivots
        var firstEndIndex = lines.FindIndex(firstIncludeIndex, line => line.StartsWith(":::zone-end"));
        var secondEndIndex = lines.FindIndex(firstEndIndex + 1, line => line.StartsWith(":::zone-end"));

        lines.RemoveRange(firstIncludeIndex, secondEndIndex - firstIncludeIndex + 1);

        // Remove trailing blank line if present
        if (string.IsNullOrEmpty(lines[firstIncludeIndex]))
        {
            lines.RemoveAt(firstIncludeIndex);
        }

        return firstIncludeIndex;
    }

    /// <summary>
    /// Gets the include path relative to the file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="includeDirectory">The include directory path.</param>
    /// <returns>The include directory path relative to the file.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the file path does not have a containing directory.</exception>
    internal static string GetIncludePathRelativeToFile(string filePath, string includeDirectory)
    {
        var fullContainingDirectoryPath = Path.GetDirectoryName(Path.GetFullPath(filePath)) ??
            throw new InvalidOperationException("File path does not have a containing directory");
        var fullIncludeDirectoryPath = Path.GetFullPath(includeDirectory) ??
            throw new InvalidOperationException("Include directory path is invalid");
        return Path.GetRelativePath(fullContainingDirectoryPath, fullIncludeDirectoryPath).Replace('\\', '/');
    }

    private static string GetIncludeLine(CloudSupportStatus status, string includeDirectory)
    {
        return status switch
        {
            CloudSupportStatus.AllClouds => $"[!INCLUDE [national-cloud-support]({includeDirectory}/all-clouds.md)]",
            CloudSupportStatus.GlobalAndUSGov => $"[!INCLUDE [national-cloud-support]({includeDirectory}/global-us.md)]",
            CloudSupportStatus.GlobalAndChina => $"[!INCLUDE [national-cloud-support]({includeDirectory}/global-china.md)]",
            CloudSupportStatus.GlobalOnly => $"[!INCLUDE [national-cloud-support]({includeDirectory}/global-only.md)]",
            _ => throw new ArgumentException("Invalid cloud support status"),
        };
    }

    private async Task LoadMarkdown()
    {
        using var markdownFile = File.OpenRead(FilePath);
        using var streamReader = new StreamReader(markdownFile);

        var markdownContent = await streamReader.ReadToEndAsync();

        var docType = markdownContent.ExtractDocType();
        if (string.IsNullOrEmpty(docType) || !docType.IsEqualIgnoringCase("apiPageType"))
        {
            throw new DocTypeException($"File is not an API document - doc_type: {docType ?? "NONE"}");
        }

        // Check if zone pivots are enabled
        ZonePivotsEnabled = markdownContent.AreZonePivotsEnabled();

        if (!ZonePivotsEnabled && (markdownContent.Contains("::zone pivot=\"graph-v1\"") || markdownContent.Contains("::zone pivot=\"graph-preview\"")))
        {
            OutputLogger.Logger?.LogWarning("Zone pivots are not enabled in {file} that contains zone pivots", FilePath);
        }

        // Extract namespace
        GraphNameSpace = markdownContent.ExtractNamespace();

        MarkdownDocument = Markdown.Parse(markdownContent);

        // Find the "HTTP request" block
        var insideRequestSection = false;
        var requestHeadingLevel = 0;
        foreach (var block in MarkdownDocument.ToList())
        {
            if (!insideRequestSection)
            {
                if (block is HeadingBlock headingBlock && headingBlock.TextEquals("HTTP request"))
                {
                    insideRequestSection = true;
                    requestHeadingLevel = headingBlock.Level;
                }

                continue;
            }

            if (block is FencedCodeBlock codeBlock)
            {
                if (ApiOperations == null)
                {
                    ApiOperations = codeBlock.GetApiOperations();
                }
                else
                {
                    ApiOperations.AddRange(codeBlock.GetApiOperations());
                }

                continue;
            }

            // Stop looking at next H2
            if (block is HeadingBlock nextHeading && nextHeading.Level <= requestHeadingLevel)
            {
                break;
            }

            // Anything else, recurse into children looking for a fenced
            // code block. this is to handle cases where authors get creative,
            // like putting multiple fenced code blocks inside a list
            var descendantCodeBlocks = block.Descendants<FencedCodeBlock>();
            if (descendantCodeBlocks.Any())
            {
                ApiOperations ??= [];

                foreach (var descendantBlock in descendantCodeBlocks)
                {
                    ApiOperations.AddRange(descendantBlock.GetApiOperations());
                }
            }
        }
    }

    private int FindInsertIncludeLineIndex(List<string> lines)
    {
        if (MarkdownDocument == null)
        {
            throw new MemberAccessException("MarkdownDocument is null");
        }

        var markdown = Markdown.Parse(string.Join(Environment.NewLine, lines));

        // Find the insert point
        var insideIntro = false;
        foreach (var block in markdown.ToList())
        {
            if (!insideIntro)
            {
                if (block is HeadingBlock headingBlock && headingBlock.Level == 1)
                {
                    insideIntro = true;
                }

                continue;
            }

            // Once inside the intro, return the next heading.
            if (block is HeadingBlock)
            {
                return block.Line;
            }
        }

        throw new Exception($"{FilePath} is malformed, cannot find insert point");
    }
}
