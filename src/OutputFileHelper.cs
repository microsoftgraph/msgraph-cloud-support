// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace CheckCloudSupport;

/// <summary>
/// Static class for writing unprocessed files to output file.
/// </summary>
public static class OutputFileHelper
{
    /// <summary>
    /// Writes unprocessed files to output file.
    /// </summary>
    /// <param name="unProcessedFiles">Dictionary containing unprocessed files.</param>
    /// <param name="outFile">Path to output file.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public static async Task LogUnprocessedFilesAsync(Dictionary<string, string>? unProcessedFiles, string? outFile)
    {
        if (unProcessedFiles == null || string.IsNullOrEmpty(outFile))
        {
            return;
        }

        using var outFileStream = File.Open(outFile, FileMode.Append);
        using var fileWriter = new StreamWriter(outFileStream);

        foreach (var unprocessed in unProcessedFiles)
        {
            await fileWriter.WriteLineAsync($"{unprocessed.Key},{unprocessed.Value}");
        }
    }
}
