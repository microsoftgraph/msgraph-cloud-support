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
}
