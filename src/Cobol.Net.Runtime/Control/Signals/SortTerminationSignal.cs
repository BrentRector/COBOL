// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// The format 1 SORT termination signal (ISO/IEC 1989:2023 §14.9.40.4 GR17, kb/Work PB1177): "If a USE procedure
/// invoked while a format 1 SORT statement is active does not complete normally, the SORT statement is terminated."
/// A USE procedure invoked by a statement of the SORT's INPUT or OUTPUT PROCEDURE runs in the procedure's bounded
/// dispatch, which the SORT statement entered through a nested <c>__Dispatch</c> call — so no raise site there can
/// jump to the statement's end. The generated <c>__RunUse</c> throws this signal instead, through
/// <see cref="IO.CobolSort.AfterUseProcedure"/>, once the declarative has finished — for every USE procedure of the
/// scope the SORT registered, however it was invoked, so the rule has one mechanism; the SORT statement's emitted
/// <c>catch</c> lands it: the statement ends there (its <c>finally</c> releases the sort store) and a RESUME AT
/// procedure-name action then transfers as a GO TO (§14.9.33.4 GR3).
/// <para>A distinct type, like <see cref="ResumeSignal"/> and <see cref="RaiseResumeSignal"/>, because its landing
/// site is distinct: the SORT statement of the program instance whose declarative ended not normally, and no
/// other.</para>
/// </summary>
public sealed class SortTerminationSignal(string file, int action) : Exception
{
    /// <summary>The sort file key of the terminated SORT statement — the statement's <c>catch</c> lands only its own.</summary>
    public string File { get; } = file;

    /// <summary>The declarative's dispatch result (<see cref="DispatchResult"/>): a RESUME AT procedure-name pc the
    /// landing transfers to (§14.9.33.4 GR3), or <see cref="DispatchResult.ResumeNext"/> /
    /// <see cref="DispatchResult.NotNormal"/>, which end at the SORT statement's end.</summary>
    public int Action { get; } = action;
}
