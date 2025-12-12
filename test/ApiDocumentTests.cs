using CheckCloudSupport.Docs;

namespace CheckCloudSupportTests;

public class ApiDocumentTests
{
    [Fact]
    public async Task CreateFromMarkdownFile_LoadsGraphApiFileCorrectly()
    {
        // Arrange
        var testFilePath = "../../../test-data/graph-api.md";
        var expectedNamespace = "microsoft.graph";

        // Act
        var apiDocument = await ApiDocument.CreateFromMarkdownFile(testFilePath);

        // Assert
        Assert.NotNull(apiDocument);
        Assert.Equal(testFilePath, apiDocument.FilePath);
        Assert.Equal(expectedNamespace, apiDocument.GraphNameSpace);
        Assert.Collection(apiDocument.ApiOperations,
            op => Assert.Equal("/me/messages/{id}", op.Path),
            op => Assert.Equal("/users/{id}/messages/{id}", op.Path),
            op => Assert.Equal("/me/mailFolders/{id}/messages/{id}", op.Path),
            op => Assert.Equal("/users/{id}/mailFolders/{id}/messages/{id}", op.Path),
            op => Assert.Equal("/me/messages/{id}/$value", op.Path),
            op => Assert.Equal("/users/{id}/messages/{id}/$value", op.Path),
            op => Assert.Equal("/me/mailFolders/{id}/messages/{id}/$value", op.Path),
            op => Assert.Equal("/users/{id}/mailFolders/{id}/messages/{id}/$value", op.Path)
        );
    }

    [Fact]
    public async Task CreateFromMarkdownFile_LoadsCopilotApiFileCorrectly()
    {
        // Arrange
        var testFilePath = "../../../test-data/copilot-api.md";

        // Act
        var apiDocument = await ApiDocument.CreateFromMarkdownFile(testFilePath);

        // Assert
        Assert.NotNull(apiDocument);
        Assert.Equal(testFilePath, apiDocument.FilePath);
        Assert.Null(apiDocument.GraphNameSpace);
        Assert.Single(apiDocument.ApiOperations,
            op => string.Compare("/copilot/users/{id}/onlineMeetings/{id}/aiInsights/{id}", op.Path, StringComparison.OrdinalIgnoreCase) == 0
        );
    }
}
