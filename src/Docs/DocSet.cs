// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace CheckCloudSupport.Docs;

/// <summary>
/// Represents a collection of Markdown documents for an API.
/// </summary>
public class DocSet
{
    private DocSet(string docsRoot)
    {
        ApiDocuments = [];
        RootDirectory = docsRoot;
    }

    /// <summary>
    /// Gets the documents contained in the collection.
    /// </summary>
    public List<ApiDocument> ApiDocuments { get; private set; }

    /// <summary>
    /// Gets the root directory containing the documents.
    /// </summary>
    public string RootDirectory { get; private set; }

    /// <summary>
    /// Creates a <see cref="DocSet"/> from the files contained in a directory.
    /// </summary>
    /// <param name="docsRoot">The path to the directory to create the <see cref="DocSet"/> from.</param>
    /// <returns>A task that represents the asynchronous create operation. The task result contains the created <see cref="DocSet"/>.</returns>
    public static async Task<DocSet> CreateFromDirectory(string docsRoot)
    {
        var docSet = new DocSet(docsRoot);
        await docSet.LoadDirectory();
        return docSet;
    }

    /// <summary>
    /// Combines two <see cref="CloudSupportStatus"/> into the most inclusive value.
    /// </summary>
    /// <param name="a">The first status.</param>
    /// <param name="b">The second status.</param>
    /// <returns>The combined status.</returns>
    public static CloudSupportStatus CombineStatuses(CloudSupportStatus a, CloudSupportStatus b)
    {
        if (a == b)
        {
            return a;
        }

        if (a == CloudSupportStatus.Unknown || a == CloudSupportStatus.GlobalOnly)
        {
            return b == CloudSupportStatus.Unknown ? CloudSupportStatus.GlobalOnly : b;
        }

        if (b == CloudSupportStatus.Unknown || b == CloudSupportStatus.GlobalOnly)
        {
            return a;
        }

        return CloudSupportStatus.AllClouds;
    }

    /// <summary>
    /// Loads the Markdown files in the root directory into the <see cref="DocSet"/>.
    /// </summary>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    public async Task LoadDirectory()
    {
        var markdownFiles = Directory.EnumerateFiles(RootDirectory, "*.md");
        if (markdownFiles != null)
        {
            foreach (var file in markdownFiles)
            {
                ApiDocuments.Add(await ApiDocument.CreateFromMarkdownFile(file));
            }
        }
    }
}
