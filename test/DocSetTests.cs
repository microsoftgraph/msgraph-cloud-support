using CheckCloudSupport.Docs;

namespace CheckCloudSupportTests;

public class DocSetTests
{
    private const string DocSetRoot = "../../../test-data/docset";

    [Fact]
    public async Task CreateFromDirectory_LoadsOnlyApiDocsRecursively()
    {
        var docSet = await DocSet.CreateFromDirectory(DocSetRoot);

        Assert.Equal(DocSetRoot, docSet.RootDirectory);

        // Two apiPageType docs (one nested); the conceptual and no-doc_type files are skipped.
        Assert.Equal(2, docSet.ApiDocuments.Count);

        var fileNames = docSet.ApiDocuments.Select(d => Path.GetFileName(d.FilePath)).ToList();
        Assert.Contains("valid1.md", fileNames);
        Assert.Contains("valid-nested.md", fileNames);
        Assert.DoesNotContain("skip-conceptual.md", fileNames);
        Assert.DoesNotContain("skip-no-doctype.md", fileNames);
    }

    [Fact]
    public async Task CreateFromDirectory_ParsesOperationsFromLoadedDocs()
    {
        var docSet = await DocSet.CreateFromDirectory(DocSetRoot);

        var nested = docSet.ApiDocuments.Single(d => Path.GetFileName(d.FilePath) == "valid-nested.md");
        Assert.Collection(nested.ApiOperations, op => Assert.Equal("/me/valid-nested", op.Path));
    }

    [Fact]
    public async Task CreateFromMarkdownFile_ThrowsForNonApiDoc()
    {
        await Assert.ThrowsAsync<DocTypeException>(
            () => ApiDocument.CreateFromMarkdownFile($"{DocSetRoot}/skip-conceptual.md"));
    }

    [Fact]
    public async Task CreateFromMarkdownFile_ThrowsWhenDocTypeMissing()
    {
        await Assert.ThrowsAsync<DocTypeException>(
            () => ApiDocument.CreateFromMarkdownFile($"{DocSetRoot}/skip-no-doctype.md"));
    }
}
