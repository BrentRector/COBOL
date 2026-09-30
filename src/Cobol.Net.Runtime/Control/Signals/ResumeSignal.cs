// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Exceptions;

/// <summary>
/// The RESUME control-unwind signal (ISO/IEC 1989:2023 §14.9.33): a RESUME statement inside a USE declarative
/// throws it; the generated <c>__RunUse</c> catches it and RETURNS the resume action to the raise site — the
/// exception-as-control pattern of <c>StopRun</c>/<c>ProgramReturn</c> (the as-built mapping of the deep-dive's
/// original "declarative returns a ResumeAction" sketch: declaratives are pc ranges run by the bounded
/// dispatcher, not methods — recorded in COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN). The unwind crosses any nested
/// PERFORM <c>__Dispatch</c> frames inside the declarative; the §14.9.33.4 GR3 NOTE 2 caveat (abandoned PERFORM
/// flow is undefined) is inherited deliberately.
/// </summary>
public sealed class ResumeSignal(int targetPc) : Exception
{
    /// <summary><c>RESUME AT NEXT STATEMENT</c> (§14.9.33.4 GR2): control transfers to the implicit CONTINUE
    /// after the statement that raised the condition — the raise site falls through (suppressing a fatal
    /// termination, §14.6.13.1.3 #5 NOTE 2).</summary>
    public const int NextStatement = -2;

    /// <summary>The dispatch result of a NONFATAL condition raised by a SUCCESSFUL statement that a WHEN phrase or a
    /// USE declarative HANDLED and that completed normally (kb/Work PB1120): §14.6.13.1.4 2)/3) — "If a conditional
    /// phrase with a NOT phrase is specified in the interrupted statement, the imperative-statement in that phrase is
    /// not executed" — so the statement's NOT phrase asks for exactly <c>-1</c> ("nothing applied") and this value,
    /// like <see cref="NextStatement"/>, keeps it from running. Distinct from <c>-1</c> so a raised-and-handled
    /// condition and no condition at all can be told apart.</summary>
    public const int HandledNonfatal = -4;

    /// <summary>The resume target: a nondeclarative pc (<c>RESUME AT procedure-name</c> ≡ GO TO, GR3), or
    /// <see cref="NextStatement"/>.</summary>
    public int TargetPc { get; } = targetPc;
}

/// <summary>
/// The SECOND leg of a resume that began at a RUNTIME raise site (kb/Work PB367b): a nonfatal exception condition
/// detected inside the runtime runs the §14.6.13.1.4 #3 selection there, so <see cref="ResumeSignal"/> has
/// already done its job — the declarative's RESUME unwound to <c>__RunUse</c>, which returned the resume action
/// to the raise site — and what remains is to leave the INTERRUPTED STATEMENT, whose frames sit between the raise
/// site and the emitted statement wrapper.
/// <para>⛔ IT IS A DISTINCT TYPE BECAUSE THE TWO SIGNALS HAVE DIFFERENT LANDING SITES, and sharing one made the
/// nearer landing site swallow the farther signal: the emitted nonfatal-gate wrapper caught the
/// <see cref="ResumeSignal"/> that an explicit RESUME statement inside a declarative was throwing AT
/// <c>__RunUse</c> — so <c>RESUME AT NEXT STATEMENT</c> in a declarative became a CONTINUE (§14.9.33.4 GR2 turned
/// into GR1's global-declarative behaviour) and <c>RESUME AT procedure-name</c> transferred inside the
/// declarative's own bounded dispatch instead of returning the action. Two landing sites, two types.</para>
/// </summary>
public sealed class RaiseResumeSignal(int targetPc) : Exception
{
    /// <summary>The resume action returned by the declarative that was selected at the raise site: a
    /// nondeclarative pc (<c>RESUME AT procedure-name</c> ≡ GO TO, §14.9.33.4 GR3), or
    /// <see cref="ResumeSignal.NextStatement"/>.</summary>
    public int TargetPc { get; } = targetPc;
}
