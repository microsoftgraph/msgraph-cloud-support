// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

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
    /// Loads the Markdown files in the root directory into the <see cref="DocSet"/>.
    /// </summary>
    /// <returns>A task that represents the asynchronous load operation.</returns>
    public async Task LoadDirectory()
    {
        var markdownFiles = Directory.EnumerateFiles(RootDirectory, "*.md", SearchOption.AllDirectories);
        if (markdownFiles != null)
        {
            foreach (var file in markdownFiles)
            {
                try
                {
                    OutputLogger.Logger?.LogInformation("Loading document: {file}", file);
                    ApiDocuments.Add(await ApiDocument.CreateFromMarkdownFile(file));
                }
                catch (DocTypeException ex)
                {
                    OutputLogger.Logger?.LogWarning("Skipping file {file}: {message}", file, ex.Message);
                }
            }
        }
    }
}
