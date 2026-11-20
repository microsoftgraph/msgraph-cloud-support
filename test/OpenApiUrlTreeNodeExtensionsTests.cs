using CheckCloudSupport.Docs;
using CheckCloudSupport.Extensions;
using CheckCloudSupport.OpenAPI;
using Markdig.Helpers;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace CheckCloudSupportTests;

/// <summary>
/// Shared collection to serialize tests that mutate the static
/// <see cref="OpenAPIOverrides"/> state.
/// </summary>
[CollectionDefinition("OpenAPIState", DisableParallelization = true)]
public class OpenAPIStateCollection
{
}

[Collection("OpenAPIState")]
public class OpenApiUrlTreeNodeExtensionsTests
{
    private static ApiOperation BuildOperation(string httpLine)
    {
        var slice = new StringSlice(httpLine);
        var line = new StringLine(ref slice);
        var operation = ApiOperation.CreateFromStringLine(line);
        Assert.NotNull(operation);
        return operation!;
    }

    private static async Task<OpenApiUrlTreeNode> BuildTreeAsync()
    {
        var clouds = new Dictionary<string, string>
        {
            { "Global", "../../../test-data/openapi-global.yml" },
            { "UsGov", "../../../test-data/openapi-usgov.yml" },
            { "China", "../../../test-data/openapi-china.yml" },
        };

        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();

        var tree = OpenApiUrlTreeNode.Create();
        foreach (var cloud in clouds)
        {
            using var stream = File.OpenRead(cloud.Value);
            var loadResult = await OpenApiDocument.LoadAsync(stream, settings: settings);
            Assert.NotNull(loadResult.Document);
            tree.Attach(loadResult.Document!, cloud.Key);
        }

        return tree;
    }

    [Fact]
    public async Task GetNodeByPath_FindsNodeMatchingIdSegment()
    {
        var tree = await BuildTreeAsync();
        var operation = BuildOperation("GET https://graph.microsoft.com/v1.0/users/{user-id}/messages");

        var node = tree.GetNodeByPath(operation, "microsoft.graph");

        Assert.NotNull(node);
        Assert.EndsWith("messages", node!.Path.Replace('\\', '/'));
    }

    [Fact]
    public async Task GetNodeByPath_MatchesFunctionWithParenthesesSuffix()
    {
        var tree = await BuildTreeAsync();
        var operation = BuildOperation("GET https://graph.microsoft.com/v1.0/reports/getUsage");

        var node = tree.GetNodeByPath(operation, "microsoft.graph");

        Assert.NotNull(node);
    }

    [Fact]
    public async Task GetNodeByPath_ReturnsNullForUnknownPath()
    {
        var tree = await BuildTreeAsync();
        var operation = BuildOperation("GET https://graph.microsoft.com/v1.0/does/not/exist");

        var node = tree.GetNodeByPath(operation, "microsoft.graph");

        Assert.Null(node);
    }

    [Theory]
    // OpenAPI {x}; docs '{x}'
    [InlineData("GET /reports/getEmailActivityUserDetail(date='{date}')", "getEmailActivityUserDetail(date={date})")]
    // OpenAPI '{x}' on a key-as-segment; docs {x}
    [InlineData("GET /applications(appId={appId})", "applications(appId='{appId}')")]
    // OpenAPI mixed '{x}' and {x}; docs with quoting reversed
    [InlineData("GET /reports/getUserArchivedPrintJobs(userId={userId},startDateTime='{startDateTime}',endDateTime='{endDateTime}')", "getUserArchivedPrintJobs(userId='{userId}',startDateTime={startDateTime},endDateTime={endDateTime})")]
    // OpenAPI mixed '@x' and @x; docs {x}
    [InlineData("GET /users/{usersId}/adhocCalls/getAllRecordings(userId={userId},startDateTime={startDateTime},endDateTime={endDateTime})", "getAllRecordings(userId='@userId',startDateTime=@startDateTime,endDateTime=@endDateTime)")]
    // OpenAPI mixed '@x' and @x; docs @x with quoting reversed
    [InlineData("GET /users/{usersId}/adhocCalls/getAllRecordings(userId=@userId,startDateTime='@startDateTime',endDateTime='@endDateTime')", "getAllRecordings(userId='@userId',startDateTime=@startDateTime,endDateTime=@endDateTime)")]
    // OpenAPI '@x'; docs '{x}'
    [InlineData("GET /teams/{team-id}/primaryChannel/doesUserHaveAccess(userId='{userId}',tenantId='{tenantId}',userPrincipalName='{userPrincipalName}')", "doesUserHaveAccess(userId='@userId',tenantId='@tenantId',userPrincipalName='@userPrincipalName')")]
    // OpenAPI mixed '{x}' and '@x'; docs {x}
    [InlineData("GET /copilot/reports/getMicrosoft365CopilotUserCountTrend(period={period},version={version})", "getMicrosoft365CopilotUserCountTrend(period='{period}',version='@version')")]
    // Chained functions; OpenAPI has image() overloads with 1, 2, and 3 parameters
    [InlineData("GET /drives/{drive-id}/items/{item-id}/workbook/worksheets/{id}/charts/item(name={name})/image(width={width},height={height})", "image(width={width},height={height})")]
    public async Task GetNodeByPath_MatchesFunctionVariations(string httpLine, string expectedSegment)
    {
        /**
         * This test ensures that the GetNodeByPath method correctly matches function variations
         * with parentheses and parameters in the path. Functions are documented inconsistently in
         * Graph documentation - sometimes placeholders are wrapped with curly braces, sometimes
         * preceded by @, and sometimes surrounded by single-quotes. This is complicated further
         * by the fact that they are formatted inconsistently in the OpenAPI specification itself.
         */
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();

        var tree = OpenApiUrlTreeNode.Create();

        using var stream = File.OpenRead("../../../test-data/openapi-functions.yml");
        var loadResult = await OpenApiDocument.LoadAsync(stream, settings: settings);
        Assert.NotNull(loadResult.Document);
        tree.Attach(loadResult.Document!, "Global");

        var operation = BuildOperation(httpLine);

        var node = tree.GetNodeByPath(operation, "microsoft.graph");
        Assert.NotNull(node);
        Assert.Equal(expectedSegment, node!.Segment);
    }

    [Fact]
    public async Task GetCloudSupportStatus_ReturnsGlobalWhenOnlyGlobal()
    {
        OpenAPIOverrides.Initialize(null, null);
        var tree = await BuildTreeAsync();
        var operation = BuildOperation("GET https://graph.microsoft.com/v1.0/security/alerts_v2/{alert-id}");

        var node = tree.GetNodeByPath(operation, "microsoft.graph");
        Assert.NotNull(node);

        var status = node!.GetCloudSupportStatus(operation.Method, "test.md");

        Assert.Equal(CloudSupportStatus.Global, status);
    }

    [Fact]
    public async Task GetCloudSupportStatus_ReturnsAllCloudsWhenInEveryCloud()
    {
        OpenAPIOverrides.Initialize(null, null);
        var tree = await BuildTreeAsync();
        var operation = BuildOperation("GET https://graph.microsoft.com/v1.0/users/{user-id}/messages");

        var node = tree.GetNodeByPath(operation, "microsoft.graph");
        Assert.NotNull(node);

        var status = node!.GetCloudSupportStatus(operation.Method, "test.md");

        Assert.Equal(CloudSupportStatus.AllClouds, status);
    }

    [Fact]
    public async Task GetCloudSupportStatus_ReturnsGlobalAndChina()
    {
        OpenAPIOverrides.Initialize(null, null);
        var tree = await BuildTreeAsync();
        var operation = BuildOperation("GET https://graph.microsoft.com/v1.0/deviceManagement/managedDevices");

        var node = tree.GetNodeByPath(operation, "microsoft.graph");
        Assert.NotNull(node);

        var status = node!.GetCloudSupportStatus(operation.Method, "test.md");

        Assert.Equal(CloudSupportStatus.GlobalAndChina, status);
    }

    [Fact]
    public async Task GetCloudSupportStatus_ReturnsUnknownWhenMethodNotDefined()
    {
        OpenAPIOverrides.Initialize(null, null);
        var tree = await BuildTreeAsync();
        var operation = BuildOperation("POST https://graph.microsoft.com/v1.0/users/{user-id}/messages");

        var node = tree.GetNodeByPath(operation, "microsoft.graph");
        Assert.NotNull(node);

        var status = node!.GetCloudSupportStatus(operation.Method, "test.md");

        Assert.Equal(CloudSupportStatus.Unknown, status);
    }

    [Fact]
    public async Task GetCloudSupportStatus_ReturnsUnknownWhenMethodIsNull()
    {
        OpenAPIOverrides.Initialize(null, null);
        var tree = await BuildTreeAsync();
        var operation = BuildOperation("GET https://graph.microsoft.com/v1.0/users/{user-id}/messages");

        var node = tree.GetNodeByPath(operation, "microsoft.graph");
        Assert.NotNull(node);

        var status = node!.GetCloudSupportStatus(null, "test.md");

        Assert.Equal(CloudSupportStatus.Unknown, status);
    }

    [Fact]
    public async Task GetCloudSupportStatus_StripsUsGovL5WhenExcluded()
    {
        OpenAPIOverrides.Initialize(null, "../../../test-data/test-l5-exclusion.json");
        var tree = await BuildTreeAsync();
        var operation = BuildOperation("GET https://graph.microsoft.com/v1.0/users/{user-id}/messages");

        var node = tree.GetNodeByPath(operation, "microsoft.graph");
        Assert.NotNull(node);

        var status = node!.GetCloudSupportStatus(operation.Method, "test.md");

        // All clouds minus US Gov L5.
        Assert.Equal(CloudSupportStatus.GlobalAndChinaAndUsGovL4, status);
        OpenAPIOverrides.Initialize(null, null);
    }
}
