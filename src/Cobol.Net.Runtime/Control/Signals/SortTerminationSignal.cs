// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// The format 1 SORT termination signal (ISO/IEC 1989:2023 §14.9.40.4 GR17, kb/Work PB1177): "If a USE procedure
/// invoked while a format 1 SORT statement is active does not complete normally, the SORT statement is terminated."
/// A USE procedure invoked by a statement of the SORT's INPUT or OUTPUT PROCEDURE runs in the procedure's bounded
/// dispatch, which the SORT statement entered through a nested <c>__Dispatch</c> call — or in an element the range
/// CALLed or INVOKEd — so no raise site there can jump to the statement's end. The generated <c>__RunUse</c> throws
/// this signal instead, through <see cref="IO.CobolSort.AfterUseProcedure"/>, once the declarative has finished — for
/// every USE procedure invoked while the SORT is active, whichever element declares it, so the rule has one mechanism;
/// the SORT statement's emitted <c>catch</c> lands it: the statement ends there (its <c>finally</c> releases the sort
/// store) and a RESUME AT procedure-name action of the statement's own declarative then transfers as a GO TO
/// (§14.9.33.4 GR3). Every activation boundary between the raise and the landing unwinds on the way, as the range it
/// belongs to has ended.
/// <para>A distinct type, like <see cref="ResumeSignal"/> and <see cref="RaiseResumeSignal"/>, because its landing
/// site is distinct: the active format 1 SORT statement, and no other.</para>
/// </summary>
public sealed class SortTerminationSignal(string file, int action) : Exception
{
    /// <summary>The sort file key of the terminated SORT statement — the statement's <c>catch</c> lands only its own.</summary>
    public string File { get; } = file;

    /// <summary>The action the landing takes (<see cref="DispatchResult"/>): the statement's own declarative's RESUME AT
    /// procedure-name pc, which it transfers to (§14.9.33.4 GR3), or <see cref="DispatchResult.ResumeNext"/> /
    /// <see cref="DispatchResult.NotNormal"/>, which end at the SORT statement's end — the latter also for every other
    /// element's USE procedure, whatever its own action was.</summary>
    public int Action { get; } = action;

    /// <summary>The declining exception FILTER an activation boundary notes this signal with (never a catch-and-rethrow,
    /// kb/Work PB2659): sets <paramref name="ended"/> and returns false, so the signal passes through untouched while the
    /// boundary's <c>finally</c> knows the activation ended with the terminated SORT's range rather than by a condition
    /// that terminates the run unit — and so still owes what an activation's end owes: the BY REFERENCE copy-out
    /// (§14.2.3 GR8) and an INITIAL program's implicit CANCEL (§14.9.18.4 GR2).</summary>
    public static bool EndsActivation(ref bool ended)
    {
        ended = true;
        return false;
    }
}
