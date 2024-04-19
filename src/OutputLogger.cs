// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

namespace CheckCloudSupport;

/// <summary>
/// Static class for logging.
/// </summary>
public static class OutputLogger
{
    /// <summary>
    /// Gets the ILogger instance.
    /// </summary>
    public static ILogger? Logger { get; private set; }

    /// <summary>
    /// Initializes logging.
    /// </summary>
    /// <param name="verbose">Value indicating whether to use verbose logging.</param>
    public static void Initialize(bool verbose)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .ClearProviders()
                .SetMinimumLevel(verbose ? LogLevel.Trace : LogLevel.Warning)
                .AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                });
        });

        Logger = loggerFactory.CreateLogger<Program>();
    }
}
