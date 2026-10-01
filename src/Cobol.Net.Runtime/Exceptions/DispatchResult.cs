// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// ⛔ THE DECLARATIVE DISPATCH-RESULT PROTOCOL, AS ONE NAMED TYPE (kb/Work PB1122 Task A, PB1761). Every selection
/// path — <c>__RunUse</c>, <c>__EcDispatch</c>, <c>__EcPerform</c>, <c>__EcObjDispatch</c>, <c>__IoCheck</c>,
/// <c>__IoCheckEc</c>, <c>__RunGlobalUse</c>, <c>INonfatalSelector.NonfatalDispatch</c> — answers the raise site with
/// one <see cref="int"/>, and the emitters used to spell its values as bare literals (<c>-1</c> … <c>-4</c>) at a
/// dozen sites, so adding a value meant finding every literal by hand. The emitters now render the NAMES below and the
/// questions the consumers ask are the predicates, so a new result value lands in one file; the
/// <c>DispatchResultProtocolDriftTests</c> census fails the build when a literal creeps back into an emitted string.
/// <list type="bullet">
/// <item><see cref="Normal"/> — the declarative ran and completed normally (§14.6.13.1.2), or no action was needed
/// (an I-O hook's successful arm).</item>
/// <item><see cref="ResumeNext"/> — RESUME AT NEXT STATEMENT (§14.9.33.4 GR2): fall through past the raising
/// statement, which also suppresses a fatal termination (§14.6.13.1.3 5) NOTE 2).</item>
/// <item><see cref="NoHandler"/> — no qualifying declarative (§14.9.49.4 GR3g tail; GR14 b)).</item>
/// <item><see cref="HandledNonfatal"/> — a NONFATAL condition raised by a SUCCESSFUL statement that a WHEN phrase or a
/// USE declarative handled and that completed normally (kb/Work PB1120): §14.6.13.1.4 2)/3) — the statement's NOT
/// phrase is skipped. Distinct from <see cref="Normal"/> so "handled" and "no condition at all" can be told apart.</item>
/// <item><see cref="NotNormal"/> — the declarative fell off its end but §14.6.13.1.2 1) says it did not complete
/// normally ("a fatal exception occurs within the scope of the declarative", or a RESUME that §14.9.33.4 GR1 made a
/// CONTINUE was executed): <c>__RunUse</c> answers it from the declarative's activation record (kb/Work PB1122 Task B).
/// Only a rule that KEYS on normal completion distinguishes it — the SORT (§14.9.40.4 GR17) and MERGE (§14.9.24.4 GR7,
/// GR12) implicit transfers, <see cref="TerminatesSortMerge"/>; every other consumer treats it as <see cref="Normal"/>
/// (<see cref="FinishesStatement"/>, <see cref="ForHandledWarning"/>), because §14.9.49.4 GR13 and GR7 b)/c), GR12 b)/c)
/// dispose of the condition by its FATALITY after the procedure returns, not by how it completed.</item>
/// <item>any value <c>≥ 0</c> — RESUME AT procedure-name's pc (≡ GO TO, §14.9.33.4 GR3).</item>
/// </list>
/// </summary>
public static class DispatchResult
{
    /// <summary>The declarative completed normally (§14.6.13.1.2), or there was nothing to do.</summary>
    public const int Normal = -1;

    /// <summary><c>RESUME AT NEXT STATEMENT</c> (§14.9.33.4 GR2): control transfers to the implicit CONTINUE after
    /// the statement that raised the condition — the raise site falls through (suppressing a fatal termination,
    /// §14.6.13.1.3 5) NOTE 2).</summary>
    public const int ResumeNext = -2;

    /// <summary>No qualifying declarative was selected (§14.9.49.4 GR3g tail, GR14 b)).</summary>
    public const int NoHandler = -3;

    /// <summary>A nonfatal condition raised by a successful statement was handled and completed normally
    /// (§14.6.13.1.4 2)/3); the statement's NOT phrase is skipped (kb/Work PB1120).</summary>
    public const int HandledNonfatal = -4;

    /// <summary>The declarative fell off its end but did not complete normally (§14.6.13.1.2 1): a fatal exception
    /// occurred within its scope, or a RESUME that §14.9.33.4 GR1 made a CONTINUE was executed (kb/Work PB1122 Task B).</summary>
    public const int NotNormal = -5;

    /// <summary>A RESUME AT procedure-name transfer: the result is the target pc (§14.9.33.4 GR3).</summary>
    public static bool IsTransfer(int result) => result >= 0;

    /// <summary>Does this result let execution continue past a FATAL condition's raising statement? Only RESUME does
    /// (§14.6.13.1.3 5) NOTE 2): RESUME AT NEXT STATEMENT, or a RESUME AT procedure-name transfer the landing
    /// already took.</summary>
    public static bool SuppressesFatal(int result) => result == ResumeNext || result >= 0;

    /// <summary>Does this result terminate a SORT or MERGE whose implicit I-O statement selected the procedure —
    /// §14.9.40.4 GR17 "a USE procedure … does not complete normally" (RESUME NEXT, or a declarative that
    /// <see cref="NotNormal"/>ly fell off its end).</summary>
    public static bool TerminatesSortMerge(int result) => result == ResumeNext || result == NotNormal;

    /// <summary>Does a raise site that selected a procedure LET THE INTERRUPTED STATEMENT FINISH? True when no RESUME
    /// redirected control: the procedure fell off its end (<see cref="Normal"/>, or <see cref="NotNormal"/> — how it
    /// completed does not matter to a statement whose own rule names the continuation, §14.6.13.1.4 3)) or none
    /// qualified (<see cref="NoHandler"/>, #4).</summary>
    public static bool FinishesStatement(int result) => result is Normal or NoHandler or NotNormal;

    /// <summary>The result an I-O hook's SUCCESSFUL arm hands the verb site after selecting over a nonfatal warning
    /// (§14.6.13.1.4 2)/3), kb/Work PB1120): nothing applied (<see cref="NoHandler"/>) is <see cref="Normal"/>, a
    /// handler that fell off its end (<see cref="Normal"/> or <see cref="NotNormal"/>) is <see cref="HandledNonfatal"/>
    /// — the statement's NOT phrase is skipped — and a RESUME action passes through unchanged.</summary>
    public static int ForHandledWarning(int selected) =>
        selected == NoHandler ? Normal : selected is Normal or NotNormal ? HandledNonfatal : selected;

    /// <summary>Did a USE procedure or WHEN handler actually RUN (as opposed to <see cref="NoHandler"/>)?</summary>
    public static bool RanAHandler(int result) => result != NoHandler;
}
