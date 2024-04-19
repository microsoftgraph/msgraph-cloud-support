using CheckCloudSupport.Docs;

namespace CheckCloudSupportTests;

public class DocSetTests
{
    public static TheoryData<CloudSupportStatus, CloudSupportStatus, CloudSupportStatus> CombineTestData => new()
    {
        {CloudSupportStatus.Unknown, CloudSupportStatus.Unknown, CloudSupportStatus.Unknown},
        {CloudSupportStatus.Unknown, CloudSupportStatus.GlobalOnly, CloudSupportStatus.GlobalOnly},
        {CloudSupportStatus.Unknown, CloudSupportStatus.AllClouds, CloudSupportStatus.AllClouds},
        {CloudSupportStatus.Unknown, CloudSupportStatus.GlobalAndUSGov, CloudSupportStatus.GlobalAndUSGov},
        {CloudSupportStatus.Unknown, CloudSupportStatus.GlobalAndChina, CloudSupportStatus.GlobalAndChina},

        {CloudSupportStatus.GlobalOnly, CloudSupportStatus.Unknown, CloudSupportStatus.GlobalOnly},
        {CloudSupportStatus.GlobalOnly, CloudSupportStatus.GlobalOnly, CloudSupportStatus.GlobalOnly},
        {CloudSupportStatus.GlobalOnly, CloudSupportStatus.AllClouds, CloudSupportStatus.AllClouds},
        {CloudSupportStatus.GlobalOnly, CloudSupportStatus.GlobalAndUSGov, CloudSupportStatus.GlobalAndUSGov},
        {CloudSupportStatus.GlobalOnly, CloudSupportStatus.GlobalAndChina, CloudSupportStatus.GlobalAndChina},

        {CloudSupportStatus.AllClouds, CloudSupportStatus.Unknown, CloudSupportStatus.AllClouds},
        {CloudSupportStatus.AllClouds, CloudSupportStatus.GlobalOnly, CloudSupportStatus.AllClouds},
        {CloudSupportStatus.AllClouds, CloudSupportStatus.AllClouds, CloudSupportStatus.AllClouds},
        {CloudSupportStatus.AllClouds, CloudSupportStatus.GlobalAndUSGov, CloudSupportStatus.AllClouds},
        {CloudSupportStatus.AllClouds, CloudSupportStatus.GlobalAndChina, CloudSupportStatus.AllClouds},

        {CloudSupportStatus.GlobalAndUSGov, CloudSupportStatus.Unknown, CloudSupportStatus.GlobalAndUSGov},
        {CloudSupportStatus.GlobalAndUSGov, CloudSupportStatus.GlobalOnly, CloudSupportStatus.GlobalAndUSGov},
        {CloudSupportStatus.GlobalAndUSGov, CloudSupportStatus.AllClouds, CloudSupportStatus.AllClouds},
        {CloudSupportStatus.GlobalAndUSGov, CloudSupportStatus.GlobalAndUSGov, CloudSupportStatus.GlobalAndUSGov},
        {CloudSupportStatus.GlobalAndUSGov, CloudSupportStatus.GlobalAndChina, CloudSupportStatus.AllClouds},

        {CloudSupportStatus.GlobalAndChina, CloudSupportStatus.Unknown, CloudSupportStatus.GlobalAndChina},
        {CloudSupportStatus.GlobalAndChina, CloudSupportStatus.GlobalOnly, CloudSupportStatus.GlobalAndChina},
        {CloudSupportStatus.GlobalAndChina, CloudSupportStatus.AllClouds, CloudSupportStatus.AllClouds},
        {CloudSupportStatus.GlobalAndChina, CloudSupportStatus.GlobalAndUSGov, CloudSupportStatus.AllClouds},
        {CloudSupportStatus.GlobalAndChina, CloudSupportStatus.GlobalAndChina, CloudSupportStatus.GlobalAndChina},

    };

    [Theory]
    [MemberData(nameof(CombineTestData))]
    public void CloudStatusesCombineCorrectly(CloudSupportStatus a, CloudSupportStatus b, CloudSupportStatus combined)
    {
        Assert.Equal(combined, DocSet.CombineStatuses(a, b));
    }
}
