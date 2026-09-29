// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>One attempt at an input-output operation under the RETRY discipline (ISO §14.7.9.3): the I-O status the
/// attempt produced and, when that status is a conflict, whether the holder of the locked resource is OUTSIDE the
/// executing run unit.
/// <para>⛔ THE HOLDER DECIDES WHETHER WAITING CAN HELP (kb/Work PB1163). §14.7.9.3 GR3 — <i>"If the FOREVER phrase is
/// specified, the mass storage control system shall attempt to gain access to a locked resource until the
/// input-output operation has been completed"</i> — is kept by polling only when something can release the
/// resource while this statement waits. A file connector of the EXECUTING run unit cannot: the run unit is
/// executing this statement, so its other connectors cannot close a file or release a record lock until the
/// statement ends. Another run unit, or another process of any language holding the host file, can. The
/// in-run-unit case is the deadlock <see cref="FileRegistry.RetryLoop"/> detects (Annex A.1 item 109); the other is
/// a wait that completes when the holder lets go.</para>
/// <para>Only the attempt knows which holder refused it: the Table 19 arbiter and the record-lock table see this run
/// unit's connectors, and a host sharing refusal (<see cref="HostFile.IsSharingRefusal"/>) is the only way another
/// run unit's hold is visible. So every attempt names its holder here, and <see cref="FileRegistry.RetryLoop"/> never
/// guesses it from the status digits.</para></summary>
/// <param name="Status">The I-O status of this attempt (§9.1.13).</param>
/// <param name="HolderOutsideRunUnit">True when <paramref name="Status"/> is a conflict held outside the executing run
/// unit, so a later attempt can succeed.</param>
public readonly record struct RetryAttempt(string Status, bool HolderOutsideRunUnit)
{
    /// <summary>An attempt whose outcome, success or conflict, was decided inside the executing run unit.</summary>
    public static RetryAttempt InRunUnit(string status) => new(status, HolderOutsideRunUnit: false);

    /// <summary>An attempt refused by a holder outside the executing run unit (the host reported the conflict).</summary>
    public static RetryAttempt OutsideRunUnit(string status) => new(status, HolderOutsideRunUnit: true);
}
