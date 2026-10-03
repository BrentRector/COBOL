// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions;

namespace CobolNet.Frontend.Preprocessor;

/// <summary>
/// The frontend's ISO §7.3 directive outputs for one compilation group, as ONE record the binder consumes
/// (<c>CSharpEmitter.Bind</c> / <c>BinderDriver.Bind</c>): the <c>&gt;&gt;TURN</c> events (§7.3.25 — the group's
/// compile-time TurnState), the <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> events (§7.3.23), the <c>&gt;&gt;FLAG-02</c> /
/// <c>&gt;&gt;FLAG-14</c> events (§7.3.14 / §7.3.15), the <c>&gt;&gt;PROPAGATE</c> events (§7.3.21 — folded per
/// source element for automatic propagation, kb/Work PB1119), the <c>&gt;&gt;COBOL-WORDS</c> map (§7.3.10) and the
/// <c>&gt;&gt;LEAP-SECOND</c> events (§7.3.17 — folded per compilation unit, kb/Work PB1378), the
/// <c>&gt;&gt;DEFINE</c> compilation-variable timeline (§7.3.11.4 GR1 — read by <c>CONSTANT … FROM</c> at the
/// entry's own line, kb/Work PB1368), and the
/// POSITION-RULED directive sites (§7.3.20.3 SR4 / §7.3.22.3 SR4 / §7.3.25.3 SR5 — WHERE a TURN / PUSH / POP was
/// written, for the ONE lexical-containment predicate owner decision D20 requires — and the sites of every directive
/// whose row carries a placement rule). A directive that gains behavior adds a member here — never a new
/// positional parameter on Bind (kb/Work PB65).
/// <para>The event lists are <see cref="DirectiveTimeline{T}"/>s: each event carries the line of the
/// <c>&gt;&gt;POP</c> that revoked it (§7.3.20 / §7.3.22, kb/Work PB941), and the CobolWordsMap value already has
/// the PUSH/POP history applied. ⛔ A NEW member is directive state and must be claimed by a
/// <see cref="DirectiveStateRegistry"/> entry — <c>DirectiveStateStackTests</c> fails until it is, so PUSH/POP
/// cannot silently miss it.</para>
/// </summary>
public sealed record DirectiveResults(
    DirectiveTimeline<TurnEvent> TurnEvents,
    DirectiveTimeline<RefModZeroLengthEvent> RefModZeroLengthEvents,
    DirectiveTimeline<FlagEvent> FlagEvents,
    DirectiveTimeline<PropagateEvent> PropagateEvents,
    CobolWordsMap CobolWordsMap,
    DirectiveTimeline<LeapSecondEvent> LeapSecondEvents,
    DirectiveTimeline<CompilationVariableEvent> CompilationVariables,
    IReadOnlyList<DirectiveSite> DirectiveSites)
{
    /// <summary>No directives at all — the OFF/empty default for every member.</summary>
    public static readonly DirectiveResults None = new(DirectiveTimeline<TurnEvent>.Empty, DirectiveTimeline<RefModZeroLengthEvent>.Empty,
        DirectiveTimeline<FlagEvent>.Empty, DirectiveTimeline<PropagateEvent>.Empty, CobolWordsMap.Empty,
        DirectiveTimeline<LeapSecondEvent>.Empty, DirectiveTimeline<CompilationVariableEvent>.Empty, []);

    /// <summary>These results with <paramref name="ops"/> — PUSH/POP ops only a LATER phase can place — replayed
    /// into EVERY event timeline together with the written ones (<see cref="DirectiveTimeline{T}.WithStackOps"/>).
    /// The one caller is the front end's §14.9.28.4 GR14 implicit PUSH ALL / POP ALL around an exception-checking
    /// PERFORM's handlers (kb/Work PB1004, PB1066 — <c>Frontend.Parse</c>). ⛔ A new timeline member joins this method —
    /// <c>ExceptionPerformDirectiveScopeTests</c> fails for a <see cref="DirectiveTimeline{T}"/> member it does not
    /// replay. The group-prefix value needs nothing: COBOL-WORDS cannot be written inside a compilation unit
    /// (§7.3.10.3 SR1), so no PERFORM can bracket it.</summary>
    public DirectiveResults WithStackOps(IReadOnlyList<DirectiveStackOp> ops) =>
        ops.Count == 0 || !HasLineScopedEvents() ? this : this with
    {
        TurnEvents = TurnEvents.WithStackOps(ops),
        RefModZeroLengthEvents = RefModZeroLengthEvents.WithStackOps(ops),
        FlagEvents = FlagEvents.WithStackOps(ops),
        PropagateEvents = PropagateEvents.WithStackOps(ops),
        LeapSecondEvents = LeapSecondEvents.WithStackOps(ops),
        CompilationVariables = CompilationVariables.WithStackOps(ops),
    };

    /// <summary>True when any event timeline holds an event — with the conditional-compilation driver's own
    /// state changes, the front end's test for whether the GR14 implicit ops are worth a parse-tree walk (kb/Work
    /// PB1004, PB1066). ⛔ It names the same members as <see cref="WithStackOps"/>, and
    /// a new timeline member joins both.</summary>
    public bool HasLineScopedEvents() =>
        TurnEvents.Count + RefModZeroLengthEvents.Count + FlagEvents.Count + PropagateEvents.Count
        + LeapSecondEvents.Count + CompilationVariables.Count > 0;
}
