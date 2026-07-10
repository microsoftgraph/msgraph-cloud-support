using CheckCloudSupport.Docs;
using Markdig.Helpers;

namespace CheckCloudSupportTests;

public class ApiOperationTests
{
    private static ApiOperation? CreateFrom(string text)
    {
        var slice = new StringSlice(text);
        var line = new StringLine(ref slice);
        return ApiOperation.CreateFromStringLine(line);
    }

    [Fact]
    public void CreateFromStringLine_ParsesMethodAndNormalizedPath()
    {
        var operation = CreateFrom("GET https://graph.microsoft.com/v1.0/users/{user-id}/messages/{message-id}");

        Assert.NotNull(operation);
        Assert.Equal(HttpMethod.Get, operation!.Method);
        Assert.Equal("/users/{id}/messages/{id}", operation.Path);
        Assert.Equal(ApiVersion.V1, operation.Version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateFromStringLine_ReturnsNullForEmptyLine(string text)
    {
        Assert.Null(CreateFrom(text));
    }

    [Fact]
    public void CreateFromStringLine_ThrowsWhenNoPath()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateFrom("GET"));
        Assert.Contains("Invalid line text", ex.Message);
    }

    [Fact]
    public void CreateFromStringLine_ThrowsForInvalidHttpMethod()
    {
        var ex = Assert.Throws<ArgumentException>(() => CreateFrom("N@T-A-METHOD /me/messages"));
        Assert.Contains("Invalid HTTP operation", ex.Message);
    }
}
