using CheckCloudSupport.OpenAPI;

namespace CheckCloudSupportTests;

[Collection("OpenAPIState")]
public class OpenAPIOverridesTests
{
    private const string OverridesFile = "../../../test-data/test-overrides.json";
    private const string ExclusionsFile = "../../../test-data/test-exclusions.json";

    public OpenAPIOverridesTests()
    {
        // OpenAPIOverrides holds static state; re-initialize before each test.
        OpenAPIOverrides.Initialize(OverridesFile, ExclusionsFile);
    }

    [Fact]
    public void CheckForOverride_ReturnsOverrideWhenNoOperationConstraint()
    {
        var result = OpenAPIOverrides.CheckForOverride("/sites/{id}/pages/microsoft.graph.sitePage", HttpMethod.Get);
        Assert.Equal("/sites/{id}/pages", result);
    }

    [Fact]
    public void CheckForOverride_ReturnsOverrideWhenOperationMatches()
    {
        var result = OpenAPIOverrides.CheckForOverride("/places/{id}", HttpMethod.Get);
        Assert.Equal("/places/{id}/graph.room", result);
    }

    [Fact]
    public void CheckForOverride_ReturnsOriginalWhenOperationDoesNotMatch()
    {
        var result = OpenAPIOverrides.CheckForOverride("/places/{id}", HttpMethod.Patch);
        Assert.Equal("/places/{id}", result);
    }

    [Fact]
    public void CheckForOverride_ReturnsOriginalWhenNoMatch()
    {
        var result = OpenAPIOverrides.CheckForOverride("/me/messages", HttpMethod.Get);
        Assert.Equal("/me/messages", result);
    }

    [Fact]
    public void CheckForOverride_MatchesCaseInsensitively()
    {
        var result = OpenAPIOverrides.CheckForOverride("/PLACES/{id}", HttpMethod.Get);
        Assert.Equal("/places/{id}/graph.room", result);
    }

    [Fact]
    public void CheckIfCloudExcluded_TrueWhenPathMethodCloudMatch()
    {
        Assert.True(OpenAPIOverrides.CheckIfCloudExcluded("/appCatalogs/teamsApps", HttpMethod.Get, "China"));
    }

    [Fact]
    public void CheckIfCloudExcluded_NormalizesBackslashesInPath()
    {
        Assert.True(OpenAPIOverrides.CheckIfCloudExcluded("\\appCatalogs\\teamsApps", HttpMethod.Get, "China"));
    }

    [Fact]
    public void CheckIfCloudExcluded_FalseWhenCloudDiffers()
    {
        Assert.False(OpenAPIOverrides.CheckIfCloudExcluded("/appCatalogs/teamsApps", HttpMethod.Get, "UsGov"));
    }

    [Fact]
    public void CheckIfCloudExcluded_FalseWhenMethodDiffers()
    {
        Assert.False(OpenAPIOverrides.CheckIfCloudExcluded("/appCatalogs/teamsApps", HttpMethod.Post, "China"));
    }

    [Fact]
    public void CheckIfCloudExcludedForFile_TrueWhenFileAndCloudMatch()
    {
        Assert.True(OpenAPIOverrides.CheckIfCloudExcludedForFile("excluded-file.md", "UsGov"));
    }

    [Fact]
    public void CheckIfCloudExcludedForFile_FalseWhenFileNotExcluded()
    {
        Assert.False(OpenAPIOverrides.CheckIfCloudExcludedForFile("some-other-file.md", "UsGov"));
    }
}
