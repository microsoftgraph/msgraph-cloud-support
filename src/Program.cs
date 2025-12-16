// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.CommandLine;
using CheckCloudSupport;
using CheckCloudSupport.Docs;
using CheckCloudSupport.Extensions;
using CheckCloudSupport.OpenAPI;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

var openApiOption = new Option<string>("--open-api", "-o")
{
    Description = "The path to a folder containing the OpenAPI descriptions",
    Required = true,
};

var apiDocsOption = new Option<string>("--api-docs", "-a")
{
    Description = "The path to a folder containing the API docs",
    Required = true,
};

var overridesFileOption = new Option<string>("--overrides", "-d")
{
    Description = "The path to a JSON file containing API overrides",
    Required = false,
};

var excludesFileOption = new Option<string>("--excludes", "-e")
{
    Description = "The path to a JSON file containing cloud exclusions",
    Required = false,
};

var batchOption = new Option<int>("--batch-size", "-b")
{
    Description = "If specified, process will pause after the specified size, allowing you to modify docs in batches",
    Required = false,
};

var outFileOption = new Option<string>("--out-file", "-f")
{
    Description = "If specified, all files that were not processed are logged to this file",
    Required = false,
};

var removeOldIncludesOption = new Option<bool>("--remove-old-includes", "-r")
{
    Description = "If specified, existing INCLUDE placement is ignored",
    Required = false,
};

var verboseOption = new Option<bool>("--verbose", "-v")
{
    Description = "Verbose logging",
    Required = false,
};

var rootCommand = new RootCommand()
{
    openApiOption,
    apiDocsOption,
    overridesFileOption,
    excludesFileOption,
    batchOption,
    outFileOption,
    removeOldIncludesOption,
    verboseOption,
};

rootCommand.SetAction(async (result, cancellationToken) =>
{
    var openApiFolder = result.GetValue(openApiOption) ??
        throw new ArgumentException("The --open-api option cannot be empty.");
    var docsFolder = result.GetValue(apiDocsOption) ??
        throw new ArgumentException("The --api-docs option cannot be empty.");
    var overridesFile = result.GetValue(overridesFileOption);
    var excludesFile = result.GetValue(excludesFileOption);
    var batchSize = result.GetValue(batchOption);
    var outFile = result.GetValue(outFileOption);
    var removeOldIncludes = result.GetValue(removeOldIncludesOption);
    var verbose = result.GetValue(verboseOption);

    OutputLogger.Initialize(verbose);

    OutputLogger.Logger?.LogInformation("Check Microsoft Graph cloud support");
    OutputLogger.Logger?.LogInformation("OpenAPI folder: {openApiFolder}", openApiFolder);
    OutputLogger.Logger?.LogInformation("Docs folder: {docsFolder}", docsFolder);
    if (batchSize > 0)
    {
        OutputLogger.Logger?.LogInformation("Batching with batch size: {batchSize}", batchSize);
    }

    OutputLogger.Logger?.LogInformation("API overrides file: {overrides}", overridesFile ?? "NONE");
    OutputLogger.Logger?.LogInformation("Cloud exclusions file: {excludes}", excludesFile ?? "NONE");
    OpenAPIOverrides.Initialize(overridesFile, excludesFile);

    Dictionary<string, string>? unProcessedFiles = null;
    if (!string.IsNullOrEmpty(outFile))
    {
        unProcessedFiles = [];
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
    var readerSettings = new OpenApiReaderSettings();
    readerSettings.AddYamlReader();

    foreach (var cloud in v1Clouds)
    {
        var fileStream = File.OpenRead(cloud.Value);
        var loadResult = await OpenApiDocument.LoadAsync(
            fileStream,
            settings: readerSettings,
            cancellationToken: cancellationToken);
        ArgumentNullException.ThrowIfNull(loadResult.Document);
        openApiTreeNode.Attach(loadResult.Document, cloud.Key);
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

var includeDirectoryOption = new Option<string>("--include-directory", "-i")
{
    Description = "The path to a folder containing INCLUDE files",
    Required = true,
};

var copilotCommand = new Command("copilot", "Run copilot tasks")
{
    openApiOption,
    apiDocsOption,
    overridesFileOption,
    excludesFileOption,
    batchOption,
    outFileOption,
    verboseOption,
    includeDirectoryOption,
};

copilotCommand.SetAction(async (result, cancellationToken) =>
{
    var openApiFolder = result.GetValue(openApiOption) ??
        throw new ArgumentException("The --open-api option cannot be empty.");
    var docsFolder = result.GetValue(apiDocsOption) ??
        throw new ArgumentException("The --api-docs option cannot be empty.");
    var includeDirectory = result.GetValue(includeDirectoryOption) ??
        throw new ArgumentException("The --include-directory option cannot be empty.");
    var overridesFile = result.GetValue(overridesFileOption);
    var excludesFile = result.GetValue(excludesFileOption);
    var batchSize = result.GetValue(batchOption);
    var outFile = result.GetValue(outFileOption);
    var verbose = result.GetValue(verboseOption);

    OutputLogger.Initialize(verbose);

    OutputLogger.Logger?.LogInformation("Check Copilot APIs cloud support");
    OutputLogger.Logger?.LogInformation("OpenAPI folder: {openApiFolder}", openApiFolder);
    OutputLogger.Logger?.LogInformation("Docs folder: {docsFolder}", docsFolder);
    if (batchSize > 0)
    {
        OutputLogger.Logger?.LogInformation("Batching with batch size: {batchSize}", batchSize);
    }

    OutputLogger.Logger?.LogInformation("API overrides file: {overrides}", overridesFile ?? "NONE");
    OutputLogger.Logger?.LogInformation("Cloud exclusions file: {excludes}", excludesFile ?? "NONE");
    OpenAPIOverrides.Initialize(overridesFile, excludesFile);

    Dictionary<string, string>? unProcessedFiles = null;
    if (!string.IsNullOrEmpty(outFile))
    {
        unProcessedFiles = [];
        File.Delete(outFile);
    }

    var apiDocs = await DocSet.CreateFromDirectory(docsFolder);

    // Define clouds
    var v1Clouds = new Dictionary<string, string>
    {
        { "Global", Path.Join(openApiFolder, "v1.0", "Prod.yml") },
        { "UsGov", Path.Join(openApiFolder, "v1.0", "Fairfax.yml") },
        { "China", Path.Join(openApiFolder, "v1.0", "Mooncake.yml") },
    };

    var betaClouds = new Dictionary<string, string>
    {
        { "Global", Path.Join(openApiFolder, "beta", "Prod.yml") },
        { "UsGov", Path.Join(openApiFolder, "beta", "Fairfax.yml") },
        { "China", Path.Join(openApiFolder, "beta", "Mooncake.yml") },
    };

    // Create OpenAPI tree node
    var readerSettings = new OpenApiReaderSettings();
    readerSettings.AddYamlReader();

    var v1OpenApiTreeNode = OpenApiUrlTreeNode.Create();
    foreach (var cloud in v1Clouds)
    {
        var fileStream = File.OpenRead(cloud.Value);
        var loadResult = await OpenApiDocument.LoadAsync(
            fileStream,
            settings: readerSettings,
            cancellationToken: cancellationToken);
        ArgumentNullException.ThrowIfNull(loadResult.Document);
        v1OpenApiTreeNode.Attach(loadResult.Document, cloud.Key);
    }

    var betaOpenApiTreeNode = OpenApiUrlTreeNode.Create();
    foreach (var cloud in betaClouds)
    {
        var fileStream = File.OpenRead(cloud.Value);
        var loadResult = await OpenApiDocument.LoadAsync(
            fileStream,
            settings: readerSettings,
            cancellationToken: cancellationToken);
        ArgumentNullException.ThrowIfNull(loadResult.Document);
        betaOpenApiTreeNode.Attach(loadResult.Document, cloud.Key);
    }

    var processedCount = 0;
    foreach (var apiDoc in apiDocs.ApiDocuments)
    {
        // Separate v1 and beta operations
        var v1Operations = apiDoc.ApiOperations
            .Where(op => op.Version == ApiVersion.V1)
            .ToList();

        var betaOperations = apiDoc.ApiOperations
            .Where(op => op.Version == ApiVersion.Beta)
            .ToList();

        var v1CloudSupportStatus = CloudSupportStatus.Unknown;
        foreach (var operation in v1Operations)
        {
            if (string.IsNullOrEmpty(operation.Path))
            {
                OutputLogger.Logger?.LogWarning("Empty path in {version} operation in {doc}", "v1", apiDoc.FilePath);
                continue;
            }

            var operationNode = v1OpenApiTreeNode.GetNodeByPath(operation, apiDoc.GraphNameSpace);
            if (operationNode == null)
            {
                OutputLogger.Logger?.LogWarning("Could not find {version} API node for {path}", "v1", operation.Path);
                continue;
            }

            var supportStatus = operationNode.GetCloudSupportStatus(operation.Method);
            OutputLogger.Logger?.LogInformation("{version} {path} support status: {status}", "v1", operation.Path, supportStatus);
            if (supportStatus != CloudSupportStatus.Unknown &&
                v1CloudSupportStatus != CloudSupportStatus.Unknown &&
                supportStatus != v1CloudSupportStatus)
            {
                OutputLogger.Logger?.LogWarning(
                    "Mismatched {version} support status in API doc {path}: {newStatus}, {oldStatus}",
                    "v1",
                    apiDoc.FilePath,
                    supportStatus,
                    v1CloudSupportStatus);

                v1CloudSupportStatus = DocSet.CombineStatuses(v1CloudSupportStatus, supportStatus);
            }
            else
            {
                v1CloudSupportStatus = supportStatus != CloudSupportStatus.Unknown ? supportStatus : v1CloudSupportStatus;
            }
        }

        var betaCloudSupportStatus = CloudSupportStatus.Unknown;
        foreach (var operation in betaOperations)
        {
            if (string.IsNullOrEmpty(operation.Path))
            {
                OutputLogger.Logger?.LogWarning("Empty path in {version} operation in {doc}", "beta", apiDoc.FilePath);
                continue;
            }

            var operationNode = betaOpenApiTreeNode.GetNodeByPath(operation, apiDoc.GraphNameSpace);
            if (operationNode == null)
            {
                OutputLogger.Logger?.LogWarning("Could not find {version} API node for {path}", "beta", operation.Path);
                continue;
            }

            var supportStatus = operationNode.GetCloudSupportStatus(operation.Method);
            OutputLogger.Logger?.LogInformation("{version} {path} support status: {status}", "beta", operation.Path, supportStatus);
            if (supportStatus != CloudSupportStatus.Unknown &&
                betaCloudSupportStatus != CloudSupportStatus.Unknown &&
                supportStatus != betaCloudSupportStatus)
            {
                OutputLogger.Logger?.LogWarning(
                    "Mismatched {version} support status in API doc {path}: {newStatus}, {oldStatus}",
                    "beta",
                    apiDoc.FilePath,
                    supportStatus,
                    betaCloudSupportStatus);

                betaCloudSupportStatus = DocSet.CombineStatuses(betaCloudSupportStatus, supportStatus);
            }
            else
            {
                betaCloudSupportStatus = supportStatus != CloudSupportStatus.Unknown ? supportStatus : betaCloudSupportStatus;
            }
        }

        if (v1Operations.Count > 0 && v1CloudSupportStatus == CloudSupportStatus.Unknown)
        {
            OutputLogger.Logger?.LogWarning("Could not determine v1 support status for {doc} - assuming beta status", apiDoc.FilePath);
        }

        if (v1CloudSupportStatus == betaCloudSupportStatus || v1CloudSupportStatus == CloudSupportStatus.Unknown)
        {
            apiDoc.CloudSupportStatus = betaCloudSupportStatus;

            try
            {
                await apiDoc.AddOrUpdateIncludeLine(true, includeDirectory);
            }
            catch (Exception ex)
            {
                OutputLogger.Logger?.LogError(
                    "Error adding INCLUDE to {file}: {message}",
                    apiDoc.FilePath,
                    ex.Message);

                unProcessedFiles?.Add(Path.GetFileName(apiDoc.FilePath), ex.Message);
            }
        }
        else
        {
            try
            {
                await apiDoc.AddOrUpdatePivotedIncludeLine(v1CloudSupportStatus, betaCloudSupportStatus, includeDirectory);
            }
            catch (Exception ex)
            {
                OutputLogger.Logger?.LogError(
                    "Error adding INCLUDE to {file}: {message}",
                    apiDoc.FilePath,
                    ex.Message);

                unProcessedFiles?.Add(Path.GetFileName(apiDoc.FilePath), ex.Message);
            }
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

rootCommand.Add(copilotCommand);

Environment.Exit(await rootCommand.Parse(args).InvokeAsync());
