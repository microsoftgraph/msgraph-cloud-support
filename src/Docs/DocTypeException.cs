// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace CheckCloudSupport.Docs;

/// <summary>
/// Exception thrown when a document is not the expected type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DocTypeException"/> class.
/// </remarks>
/// <param name="message">The message for the exception.</param>
public class DocTypeException(string message) : Exception(message)
{
}
