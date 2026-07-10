using CheckCloudSupport.Docs;

namespace CheckCloudSupportTests;

public class ApiDocumentIncludeTests
{
    private const string SourceDoc = "../../../test-data/graph-api.md";

    private static string CopyToTemp()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"cloud-support-{Guid.NewGuid():N}.md");
        File.Copy(SourceDoc, tempPath, overwrite: true);
        return tempPath;
    }

    [Theory]
    [InlineData(CloudSupportStatus.AllClouds, "all-clouds.md")]
    [InlineData(CloudSupportStatus.GlobalAndChina, "global-china.md")]
    [InlineData(CloudSupportStatus.GlobalAndChinaAndUsGovL4, "global-china-us-l4.md")]
    [InlineData(CloudSupportStatus.GlobalAndChinaAndUsGovL5, "global-china-us-l5.md")]
    [InlineData(CloudSupportStatus.GlobalAndUSGov, "global-us.md")]
    [InlineData(CloudSupportStatus.GlobalAndUsGovL4, "global-us-l4.md")]
    [InlineData(CloudSupportStatus.GlobalAndUsGovL5, "global-us-l5.md")]
    [InlineData(CloudSupportStatus.Global, "global-only.md")]
    public async Task AddOrUpdateIncludeLine_InsertsIncludeForStatus(CloudSupportStatus status, string expectedFile)
    {
        var tempPath = CopyToTemp();
        try
        {
            var apiDocument = await ApiDocument.CreateFromMarkdownFile(tempPath);
            apiDocument.CloudSupportStatus = status;

            await apiDocument.AddOrUpdateIncludeLine(removeOldIncludes: false);

            var lines = await File.ReadAllLinesAsync(tempPath);
            var includeLines = lines.Where(l => l.Contains("[!INCLUDE [national-cloud-support]")).ToList();
            Assert.Single(includeLines);
            Assert.Contains($"includes/{expectedFile})]", includeLines[0]);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task AddOrUpdateIncludeLine_ThrowsForUnmappedStatus()
    {
        var tempPath = CopyToTemp();
        try
        {
            var apiDocument = await ApiDocument.CreateFromMarkdownFile(tempPath);
            apiDocument.CloudSupportStatus = CloudSupportStatus.China;

            await Assert.ThrowsAsync<ArgumentException>(
                () => apiDocument.AddOrUpdateIncludeLine(removeOldIncludes: false));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task AddOrUpdateIncludeLine_UpdatesExistingLineInPlace()
    {
        var tempPath = CopyToTemp();
        try
        {
            var apiDocument = await ApiDocument.CreateFromMarkdownFile(tempPath);

            apiDocument.CloudSupportStatus = CloudSupportStatus.Global;
            await apiDocument.AddOrUpdateIncludeLine(removeOldIncludes: false);

            apiDocument.CloudSupportStatus = CloudSupportStatus.AllClouds;
            await apiDocument.AddOrUpdateIncludeLine(removeOldIncludes: false);

            var lines = await File.ReadAllLinesAsync(tempPath);
            var includeLines = lines.Where(l => l.Contains("[!INCLUDE [national-cloud-support]")).ToList();
            Assert.Single(includeLines);
            Assert.Contains("includes/all-clouds.md)]", includeLines[0]);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task AddOrUpdatePivotedIncludeLine_WritesZonePivotsAndBothIncludes()
    {
        var tempPath = CopyToTemp();
        var includeDirectory = Path.Combine(Path.GetDirectoryName(tempPath)!, "includes");
        try
        {
            var apiDocument = await ApiDocument.CreateFromMarkdownFile(tempPath);

            await apiDocument.AddOrUpdatePivotedIncludeLine(
                CloudSupportStatus.Global,
                CloudSupportStatus.AllClouds,
                includeDirectory);

            var lines = await File.ReadAllLinesAsync(tempPath);

            Assert.Contains(lines, l => l.StartsWith(":::zone pivot=\"graph-v1\""));
            Assert.Contains(lines, l => l.StartsWith(":::zone pivot=\"graph-preview\""));
            Assert.Contains(lines, l => l.Contains("[!INCLUDE [version-support-differs]"));
            Assert.Contains(lines, l => l.Contains("global-only.md)]"));
            Assert.Contains(lines, l => l.Contains("all-clouds.md)]"));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
