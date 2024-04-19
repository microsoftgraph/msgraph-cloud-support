// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using CheckCloudSupport.Docs.Extensions;
using CheckCloudSupport.Extensions;
using Markdig;
using Markdig.Syntax;

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
    /// <param name="removeOldIncludes">If specified, location of existing INCLUDE line is ignored.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddOrUpdateIncludeLine(bool removeOldIncludes)
    {
        var lines = new List<string>(await File.ReadAllLinesAsync(FilePath));

        // Check if INCLUDE line already exists
        var includeLine = lines.FirstOrDefault(line => line.Contains("[!INCLUDE [national-cloud-support]"));

        if (removeOldIncludes && !string.IsNullOrEmpty(includeLine))
        {
            var removeIndex = lines.IndexOf(includeLine);
            lines.RemoveAt(removeIndex);

            // Remove trailing blank line if present
            if (string.IsNullOrEmpty(lines[removeIndex]))
            {
                lines.RemoveAt(removeIndex);
            }

            includeLine = string.Empty;
        }

        if (string.IsNullOrEmpty(includeLine))
        {
            // Find the insert location
            var insertIndex = FindInsertIncludeLineIndex(lines);
            lines.Insert(insertIndex, string.Empty);
            lines.Insert(insertIndex, GetIncludeLine(CloudSupportStatus));
            if (!string.IsNullOrEmpty(lines[insertIndex - 1]))
            {
                lines.Insert(insertIndex, string.Empty);
            }
        }
        else
        {
            // Get the index
            var includeIndex = lines.IndexOf(includeLine);
            lines[includeIndex] = GetIncludeLine(CloudSupportStatus);
        }

        // Write file back
        await File.WriteAllLinesAsync(FilePath, lines);
    }

    private static string GetIncludeLine(CloudSupportStatus status)
    {
        return status switch
        {
            CloudSupportStatus.AllClouds => "[!INCLUDE [national-cloud-support](../../includes/all-clouds.md)]",
            CloudSupportStatus.GlobalAndUSGov => "[!INCLUDE [national-cloud-support](../../includes/global-us.md)]",
            CloudSupportStatus.GlobalAndChina => "[!INCLUDE [national-cloud-support](../../includes/global-china.md)]",
            CloudSupportStatus.GlobalOnly => "[!INCLUDE [national-cloud-support](../../includes/global-only.md)]",
            _ => throw new ArgumentException("Invalid cloud support status"),
        };
    }

    private async Task LoadMarkdown()
    {
        using var markdownFile = File.OpenRead(FilePath);
        using var streamReader = new StreamReader(markdownFile);

        var markdownContent = await streamReader.ReadToEndAsync();

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
                ApiOperations ??= new List<ApiOperation>();

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
