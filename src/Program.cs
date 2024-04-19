// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.CommandLine;
using CheckCloudSupport;
using CheckCloudSupport.Docs;
using CheckCloudSupport.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Services;

var openApiOption = new Option<string>(["--open-api", "-o"])
{
    Description = "The path to a folder containing the OpenAPI descriptions",
    IsRequired = true,
};

var apiDocsOption = new Option<string>(["--api-docs", "-a"])
{
    Description = "The path to a folder containing the API docs",
    IsRequired = true,
};

var batchOption = new Option<int>(["--batch-size", "-b"])
{
    Description = "If specified, process will pause after the specified size, allowing you to modify docs in batches",
    IsRequired = false,
};

var outFileOption = new Option<string>(["--out-file", "-f"])
{
    Description = "If specified, all files that were not processed are logged to this file",
    IsRequired = false,
};

var removeOldIncludesOption = new Option<bool>(["--remove-old-includes", "-r"])
{
    Description = "If specified, existing INCLUDE placement is ignored",
    IsRequired = false,
};

var verboseOption = new Option<bool>(["--verbose", "-v"])
{
    Description = "Verbose logging",
    IsRequired = false,
};

var rootCommand = new RootCommand();
rootCommand.AddOption(openApiOption);
rootCommand.AddOption(apiDocsOption);
rootCommand.AddOption(batchOption);
rootCommand.AddOption(outFileOption);
rootCommand.AddOption(removeOldIncludesOption);
rootCommand.AddOption(verboseOption);

rootCommand.SetHandler(async (context) =>
{
    var openApiFolder = context.ParseResult.GetValueForOption(openApiOption) ??
        throw new ArgumentException("The --open-api option cannot be empty.");
    var docsFolder = context.ParseResult.GetValueForOption(apiDocsOption) ??
        throw new ArgumentException("The --api-docs option cannot be empty.");
    var batchSize = context.ParseResult.GetValueForOption(batchOption);
    var outFile = context.ParseResult.GetValueForOption(outFileOption);
    var removeOldIncludes = context.ParseResult.GetValueForOption(removeOldIncludesOption);
    var verbose = context.ParseResult.GetValueForOption(verboseOption);

    OutputLogger.Initialize(verbose);

    OutputLogger.Logger?.LogInformation("Check Microsoft Graph cloud support");
    OutputLogger.Logger?.LogInformation("OpenAPI folder: {openApiFolder}", openApiFolder);
    OutputLogger.Logger?.LogInformation("Docs folder: {docsFolder}", docsFolder);
    if (batchSize > 0)
    {
        OutputLogger.Logger?.LogInformation("Batching with batch size: {batchSize}", batchSize);
    }

    Dictionary<string, string>? unProcessedFiles = null;
    if (!string.IsNullOrEmpty(outFile))
    {
        unProcessedFiles = new Dictionary<string, string>();
        File.Delete(outFile);
    }

    var apiDocs = await DocSet.CreateFromDirectory(docsFolder);

    // Define clouds
    var v1Clouds = new Dictionary<string, string>
    {
        { "Global", Path.Join(openApiFolder, "Prod.yml") },
        { "UsGov", Path.Join(openApiFolder, "Fairfax.yml") },
        { "China", Path.Join(openApiFolder, "Mooncake.yml") },
    };

    // Create OpenAPI tree node
    var openApiTreeNode = OpenApiUrlTreeNode.Create();
    var reader = new OpenApiStreamReader();

    foreach (var cloud in v1Clouds)
    {
        var fileStream = File.OpenRead(cloud.Value);
        var doc = await reader.ReadAsync(fileStream);
        openApiTreeNode.Attach(doc.OpenApiDocument, cloud.Key);
    }

    var processedCount = 0;
    foreach (var apiDoc in apiDocs.ApiDocuments)
    {
        foreach (var operation in apiDoc.ApiOperations)
        {
            if (string.IsNullOrEmpty(operation.Path))
            {
                OutputLogger.Logger?.LogWarning("Empty path in operation in {doc}", apiDoc.FilePath);
                continue;
            }

            var operationNode = openApiTreeNode.GetNodeByPath(operation, apiDoc.GraphNameSpace);
            if (operationNode == null)
            {
                OutputLogger.Logger?.LogWarning("Could not find API node for {path}", operation.Path);
                continue;
            }

            var supportStatus = operationNode.GetCloudSupportStatus(operation.Method);
            OutputLogger.Logger?.LogInformation("{path} support status: {status}", operation.Path, supportStatus);
            if (supportStatus != CloudSupportStatus.Unknown &&
                apiDoc.CloudSupportStatus != CloudSupportStatus.Unknown &&
                supportStatus != apiDoc.CloudSupportStatus)
            {
                OutputLogger.Logger?.LogWarning(
                    "Mismatched support status in API doc {path}: {newStatus}, {oldStatus}",
                    apiDoc.FilePath,
                    supportStatus,
                    apiDoc.CloudSupportStatus);

                apiDoc.CloudSupportStatus = DocSet.CombineStatuses(apiDoc.CloudSupportStatus, supportStatus);
            }
            else
            {
                apiDoc.CloudSupportStatus = supportStatus != CloudSupportStatus.Unknown ? supportStatus : apiDoc.CloudSupportStatus;
            }
        }

        try
        {
            await apiDoc.AddOrUpdateIncludeLine(removeOldIncludes);
        }
        catch (Exception ex)
        {
            OutputLogger.Logger?.LogError(
                "Error adding INCLUDE to {file}: {message}",
                apiDoc.FilePath,
                ex.Message);

            unProcessedFiles?.Add(Path.GetFileName(apiDoc.FilePath), ex.Message);
        }

        processedCount++;

        if (batchSize > 0 && processedCount >= batchSize)
        {
            // Write out any unprocessed files
            await OutputFileHelper.LogUnprocessedFilesAsync(unProcessedFiles, outFile);
            unProcessedFiles?.Clear();

            Console.WriteLine($"Reached batch size {batchSize}. Press any key to resume processing.");
            Console.ReadKey(true);
            processedCount = 0;
        }
    }

    // Write out any unprocessed files
    await OutputFileHelper.LogUnprocessedFilesAsync(unProcessedFiles, outFile);
    unProcessedFiles?.Clear();
});

Environment.Exit(await rootCommand.InvokeAsync(args));
