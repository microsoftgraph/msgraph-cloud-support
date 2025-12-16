using CheckCloudSupport.Docs;

namespace CheckCloudSupportTests;

public class ApiDocumentTests
{
    public static TheoryData<string, string, string> RelativePathData => new()
    {
        {"C:/Source/Repos/microsoft-graph-docs/api-reference/v1.0/api\\accesspackage-delete-incompatibleaccesspackage.md", "../../includes", "../../includes"},
        {"C:/Source/Repos/m365copilot-docs-pr/docs/api\\admin-settings\\copilotadminlimitedmode-get.md", "C:/Source/Repos/m365copilot-docs-pr/docs/api/includes", "../includes"},
        {"C:/Source/Repos/m365copilot-docs-pr/docs/api\\admin-settings\\reports\\copilotreportroot-getmicrosoft365copilotusageuserdetail.md", "C:/Source/Repos/m365copilot-docs-pr/docs/api/includes", "../../includes"},
    };

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
        Assert.True(apiDocument.ZonePivotsEnabled);
        Assert.Null(apiDocument.GraphNameSpace);
        Assert.Collection(apiDocument.ApiOperations,
            op => { Assert.Equal("/copilot/users/{id}/onlineMeetings/{id}/aiInsights/{id}", op.Path); Assert.Equal(ApiVersion.V1, op.Version); },
            op => { Assert.Equal("/copilot/users/{id}/onlineMeetings/{id}/aiInsights/{id}", op.Path); Assert.Equal(ApiVersion.Beta, op.Version); }
        );
    }

    [Fact]
    public async Task NonPivotIncludesRemoveCorrectly()
    {
        // Arrange
        var testFilePath = "../../../test-data/graph-api-with-includes.md";
        var lines = new List<string>(await File.ReadAllLinesAsync(testFilePath));

        // Act
        var insertIndex = ApiDocument.RemoveAllIncludeLines(lines);

        // Assert
        Assert.Equal(28, insertIndex);
        Assert.DoesNotContain(lines, line => line.Contains("[!INCLUDE [national-cloud-support]"));
    }

    [Fact]
    public async Task PivotIncludesRemoveCorrectly()
    {
        // Arrange
        var testFilePath = "../../../test-data/copilot-api-with-includes.md";
        var lines = new List<string>(await File.ReadAllLinesAsync(testFilePath));

        // Act
        var insertIndex = ApiDocument.RemoveAllIncludeLines(lines);

        // Assert
        Assert.Equal(30, insertIndex);
        Assert.DoesNotContain(lines, line => line.Contains("[!INCLUDE [national-cloud-support]"));
    }

    [Theory]
    [MemberData(nameof(RelativePathData))]
    public async Task IncludePathRelativeToFile_ComputesCorrectly(string filePath, string includeDirectory, string expectedRelativePath)
    {
        // Act
        var relativePath = ApiDocument.GetIncludePathRelativeToFile(filePath, includeDirectory);

        // Assert
        Assert.Equal(expectedRelativePath, relativePath);
    }
}
