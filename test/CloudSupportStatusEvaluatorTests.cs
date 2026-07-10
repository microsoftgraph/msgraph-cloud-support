using CheckCloudSupport.Docs;

namespace CheckCloudSupportTests;

public class CloudSupportStatusEvaluatorTests
{
    [Fact]
    public void Merge_SetsStatusWhenAccumulatedIsUnknown()
    {
        var result = CloudSupportStatusEvaluator.Merge(
            CloudSupportStatus.Unknown,
            CloudSupportStatus.Global,
            out var mismatch);

        Assert.Equal(CloudSupportStatus.Global, result);
        Assert.False(mismatch);
    }

    [Fact]
    public void Merge_KeepsAccumulatedWhenOperationIsUnknown()
    {
        var result = CloudSupportStatusEvaluator.Merge(
            CloudSupportStatus.AllClouds,
            CloudSupportStatus.Unknown,
            out var mismatch);

        Assert.Equal(CloudSupportStatus.AllClouds, result);
        Assert.False(mismatch);
    }

    [Fact]
    public void Merge_KeepsUnknownWhenBothUnknown()
    {
        var result = CloudSupportStatusEvaluator.Merge(
            CloudSupportStatus.Unknown,
            CloudSupportStatus.Unknown,
            out var mismatch);

        Assert.Equal(CloudSupportStatus.Unknown, result);
        Assert.False(mismatch);
    }

    [Fact]
    public void Merge_NoMismatchWhenStatusesAgree()
    {
        var result = CloudSupportStatusEvaluator.Merge(
            CloudSupportStatus.GlobalAndChina,
            CloudSupportStatus.GlobalAndChina,
            out var mismatch);

        Assert.Equal(CloudSupportStatus.GlobalAndChina, result);
        Assert.False(mismatch);
    }

    [Fact]
    public void Merge_CombinesWithOrWhenKnownStatusesDisagree()
    {
        var result = CloudSupportStatusEvaluator.Merge(
            CloudSupportStatus.Global,
            CloudSupportStatus.GlobalAndChina,
            out var mismatch);

        Assert.True(mismatch);
        Assert.Equal(CloudSupportStatus.Global | CloudSupportStatus.GlobalAndChina, result);
        Assert.Equal(CloudSupportStatus.GlobalAndChina, result);
    }

    [Fact]
    public void Merge_OrCombinesDistinctClouds()
    {
        var result = CloudSupportStatusEvaluator.Merge(
            CloudSupportStatus.Global,
            CloudSupportStatus.China,
            out var mismatch);

        Assert.True(mismatch);
        Assert.Equal(CloudSupportStatus.GlobalAndChina, result);
    }

    [Theory]
    [InlineData(CloudSupportStatus.Global, CloudSupportStatus.Global, false)]
    [InlineData(CloudSupportStatus.Unknown, CloudSupportStatus.AllClouds, false)]
    [InlineData(CloudSupportStatus.Global, CloudSupportStatus.AllClouds, true)]
    [InlineData(CloudSupportStatus.AllClouds, CloudSupportStatus.Global, true)]
    [InlineData(CloudSupportStatus.GlobalAndChina, CloudSupportStatus.Unknown, true)]
    public void RequiresVersionPivot_ReturnsExpected(
        CloudSupportStatus v1Status,
        CloudSupportStatus betaStatus,
        bool expected)
    {
        Assert.Equal(expected, CloudSupportStatusEvaluator.RequiresVersionPivot(v1Status, betaStatus));
    }
}
