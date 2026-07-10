using CheckCloudSupport.Docs;
using CheckCloudSupport.Extensions;

namespace CheckCloudSupportTests;

public class StringExtensionsTests
{
    [Fact]
    public void ParameterNormalizationSucceeds()
    {
        // Arrange
        var path = "/identityGovernance/entitlementManagement/assignments/additionalAccess(accessPackageId='parameterValue',incompatibleAccessPackageId='parameterValue')";

        // Act
        var normalizedPath = path.NormalizeParameters();

        // Assert
        Assert.Equal("/identityGovernance/entitlementManagement/assignments/additionalAccess(accessPackageId='{accessPackageId}',incompatibleAccessPackageId='{incompatibleAccessPackageId}')", normalizedPath);
    }

    [Fact]
    public void OneDriveShortcutFixSucceeds()
    {
        // Arrange
        var path = "/drive/bundles/{id}/children";

        // Act
        var normalizedPath = path.FixDriveShortcut();

        // Assert
        Assert.Equal("/drives/{id}/bundles/{id}/children", normalizedPath);
    }

// cSpell:ignore Pstn williamlooney localizationpriority callrecords pstncalllogrow
    public readonly string markdownWithNamespace = """
---
title: "callRecord: getPstnCalls"
description: "Get log of PSTN calls."
author: "williamlooney"
ms.localizationpriority: medium
ms.prod: "cloud-communications"
doc_type: "apiPageType"
---

# callRecord: getPstnCalls

Namespace: microsoft.graph.callRecords

Get log of PSTN calls as a collection of [pstnCallLogRow](../resources/callrecords-pstncalllogrow.md) entries.

## Permissions

One of the following permissions is required to call this API. To learn more, including how to choose permissions, see [Permissions](/graph/permissions-reference).

|Permission type|Permissions (from least to most privileged)|
|:---------------------------------------|:--------------------------------------------|
| Delegated (work or school account)     | Not supported. |
| Delegated (personal Microsoft account) | Not supported. |
| Application                            | CallRecord-PstnCalls.Read.All, CallRecords.Read.All |

## HTTP request

<!-- {
  "blockType": "ignored"
}
-->

``` http
GET /communications/callRecords/getPstnCalls(fromDateTime={fromDateTime},toDateTime={toDateTime})
```

## Function parameters

In the request URL, provide the following query parameters with values.
The following table shows the parameters that can be used with this function.

|Parameter|Type|Description|
|:---|:---|:---|
|fromDateTime|DateTimeOffset|Start of time range to query. UTC, inclusive.<br/>Time range is based on the call start time.|
|toDateTime|DateTimeOffset|End of time range to query. UTC, inclusive.|

> [!IMPORTANT]
> The **fromDateTime** and **toDateTime** values cannot be more than a date range of 90 days.

## Request headers

|Name|Description|
|:---|:---|
|Authorization|Bearer {token}. Required.|

## Response

If successful, this function returns a `200 OK` response code and a collection of [pstnCallLogRow](../resources/callrecords-pstncalllogrow.md) entries in the response body.

If there are more than 1000 entries in the date range, the body also includes an `@odata.NextLink` with a URL to query the next page of call entries. The last page in the date range does not have `@odata.NextLink`. For more information, see [paging Microsoft Graph data in your app](/graph/paging).

## Example

The following example shows getting a collection of records for PSTN calls that occurred in the specified date range. The response includes `"@odata.count": 1000` to enumerate the number of records in this first response, and `@odata.NextLink` to get records beyond the first 1000. For readability, the response shows only a collection of 1 record. Please assume there are more than 1000 calls in that date range.
""";

    [Fact]
    public void NamespaceIsExtractedFromMarkdown()
    {
        // Arrange
        var content = markdownWithNamespace; // "Namespace: microsoft.graph.callRecords";

        // Act
        var extractedNamespace = content.ExtractNamespace();

        // Assert
        Assert.Equal("microsoft.graph.callRecords", extractedNamespace);
    }

    [Theory]
    [InlineData("https://graph.microsoft.com/v1.0/me/messages", "/me/messages")]
    [InlineData("https://graph.microsoft.com/beta/me/messages", "/me/messages")]
    [InlineData("/v1.0/users/{id}", "/users/{id}")]
    [InlineData("/beta/users/{id}", "/users/{id}")]
    [InlineData("/me/messages", "/me/messages")]
    public void MakePathRelativeToVersion_StripsHostAndVersion(string path, string expected)
    {
        Assert.Equal(expected, path.MakePathRelativeToVersion());
    }

    [Theory]
    [InlineData("/users/{user-id}/messages/{message-id}", "/users/{id}/messages/{id}")]
    [InlineData("/workbook/charts/{name}", "/workbook/charts/{id}")]
    [InlineData("/drive/root:/{item-path}:/children", "/drive/items/{id}/children")]
    [InlineData("/users/{USER-ID}", "/users/{id}")]
    public void NormalizeIdSegments_NormalizesToId(string path, string expected)
    {
        Assert.Equal(expected, path.NormalizeIdSegments());
    }

    [Theory]
    [InlineData("/users/{id}/drive/items/{id}", "/drives/{id}/items/{id}")]
    [InlineData("/me/drive/items/{id}", "/drives/{id}/items/{id}")]
    [InlineData("/sites/{id}/drive/items/{id}", "/sites/{id}/drive/items/{id}")]
    public void FixUserDrivePath_RewritesUserAndMeDriveShortcuts(string path, string expected)
    {
        Assert.Equal(expected, path.FixUserDrivePath());
    }

    [Fact]
    public void FixDriveShareId_NormalizesEncodedSharingUrl()
    {
        Assert.Equal("/shares/{id}/driveItem", "/shares/{encoded-sharing-url}/driveItem".FixDriveShareId());
    }

    [Theory]
    [InlineData("/me/mailFolders/inbox/messages", "/me/mailFolders/{id}/messages")]
    [InlineData("/users/{id}/mailfolders/archive/messages", "/users/{id}/mailFolders/{id}/messages")]
    public void FixWellKnownMailFoldersId_NormalizesFolderName(string path, string expected)
    {
        Assert.Equal(expected, path.FixWellKnownMailFoldersId());
    }

    [Fact]
    public void ExtractDocType_ReturnsDocType()
    {
        Assert.Equal("apiPageType", markdownWithNamespace.ExtractDocType());
    }

    [Fact]
    public void ExtractDocType_ReturnsNullWhenAbsent()
    {
        Assert.Null("# Just a heading\n\nSome text.".ExtractDocType());
    }

    [Fact]
    public void AreZonePivotsEnabled_TrueWhenGroupDeclared()
    {
        var markdown = "---\nzone_pivot_groups: graph-api-versions\n---\n";
        Assert.True(markdown.AreZonePivotsEnabled());
    }

    [Fact]
    public void AreZonePivotsEnabled_FalseWhenAbsent()
    {
        Assert.False(markdownWithNamespace.AreZonePivotsEnabled());
    }

    [Theory]
    [InlineData("https://graph.microsoft.com/v1.0/me", ApiVersion.V1)]
    [InlineData("https://graph.microsoft.com/beta/me", ApiVersion.Beta)]
    [InlineData("https://graph.microsoft.com/V1.0/me", ApiVersion.V1)]
    [InlineData("/me/messages", ApiVersion.Unknown)]
    public void ExtractApiVersion_ReturnsExpectedVersion(string path, ApiVersion expected)
    {
        Assert.Equal(expected, path.ExtractApiVersion());
    }

    [Theory]
    [InlineData("users", "USERS", true)]
    [InlineData("users", "users", true)]
    [InlineData("users", "messages", false)]
    public void IsEqualIgnoringCase_ComparesCaseInsensitively(string value, string compareTo, bool expected)
    {
        Assert.Equal(expected, value.IsEqualIgnoringCase(compareTo));
    }

    [Fact]
    public void IsEqualIgnoringCase_MatchesFunctionParametersWithInconsistentQuoting()
    {
        var value = "getPstnCalls(fromDateTime={fromDateTime})";
        var compareTo = "getPstnCalls(fromDateTime='{fromDateTime}')";

        Assert.True(value.IsEqualIgnoringCase(compareTo));
    }
}
