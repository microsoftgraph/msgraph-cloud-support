// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace CheckCloudSupport.Docs;

/// <summary>
/// Contains the decision logic for combining and interpreting
/// <see cref="CloudSupportStatus"/> values across an API document's operations.
/// </summary>
public static class CloudSupportStatusEvaluator
{
    /// <summary>
    /// Merges an operation's cloud support status into an accumulated status.
    /// </summary>
    /// <param name="accumulated">The status accumulated so far.</param>
    /// <param name="operationStatus">The status of the current operation.</param>
    /// <param name="mismatch">
    /// Set to <see langword="true"/> when both statuses are known but disagree,
    /// in which case they are combined with a bitwise OR. This allows callers to
    /// surface a warning about the disagreement.
    /// </param>
    /// <returns>The merged <see cref="CloudSupportStatus"/>.</returns>
    public static CloudSupportStatus Merge(
        CloudSupportStatus accumulated,
        CloudSupportStatus operationStatus,
        out bool mismatch)
    {
        mismatch = operationStatus != CloudSupportStatus.Unknown &&
            accumulated != CloudSupportStatus.Unknown &&
            operationStatus != accumulated;

        if (mismatch)
        {
            return accumulated | operationStatus;
        }

        return operationStatus != CloudSupportStatus.Unknown ? operationStatus : accumulated;
    }

    /// <summary>
    /// Determines whether an API document requires version-pivoted include lines.
    /// This is the case when the v1 and beta statuses differ and the v1 status is known.
    /// </summary>
    /// <param name="v1Status">The cloud support status for the v1 pivot.</param>
    /// <param name="betaStatus">The cloud support status for the beta pivot.</param>
    /// <returns>
    /// <see langword="true"/> if separate v1 and beta include lines are required;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool RequiresVersionPivot(CloudSupportStatus v1Status, CloudSupportStatus betaStatus)
    {
        return v1Status != betaStatus && v1Status != CloudSupportStatus.Unknown;
    }
}
