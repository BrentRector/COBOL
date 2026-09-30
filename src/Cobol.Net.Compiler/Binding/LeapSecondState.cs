// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Binding;

/// <summary>
/// The compile-time <c>&gt;&gt;LEAP-SECOND</c> resolution (ISO/IEC 1989:2023 §7.3.17; kb/Work PB65, PB1378): the
/// source-ordered ON/OFF directive toggles, folded at a compilation unit's first line to decide whether a formatted-time
/// argument's seconds subfield may be 60 (§15.3.3.3) and whether standard numeric time form is bounded at 86,401
/// (§7.3.17.4 GR4) for THAT unit. §7.3.4 GR5: "A compiler directive applies to all of the source text and library text
/// that follows", so a directive written between two sibling units — outside both, §7.3.17.3 SR1 — governs the units that
/// follow it and not the one before; §7.3.17.4 GR1: absent, OFF is implied.
/// </summary>
public sealed class LeapSecondState
{
    private readonly DirectiveTimeline<LeapSecondEvent> _events;

    /// <summary>The empty state — no directive; every unit is OFF (the §7.3.17.4 GR1 default).</summary>
    public static readonly LeapSecondState Empty = new(DirectiveTimeline<LeapSecondEvent>.Empty);

    private LeapSecondState(DirectiveTimeline<LeapSecondEvent> events) => _events = events;

    /// <summary>Build the state from the frontend's directive events. A <see cref="DirectiveTimeline{T}"/> carries the
    /// PUSH/POP history: a POP-revoked toggle is skipped (§7.3.20.4 GR1/GR3; kb/Work PB941).</summary>
    public static LeapSecondState Build(IReadOnlyList<LeapSecondEvent>? events)
        => events is null || events.Count == 0 ? Empty : new LeapSecondState(DirectiveTimeline<LeapSecondEvent>.Of(events));

    /// <summary>Is ON in effect for the compilation unit whose first line is <paramref name="unitLine"/>? The most
    /// recent toggle strictly before the unit wins; OFF when none precedes it.</summary>
    public bool IsOnAt(int unitLine) =>
        _events.TryLastInEffectBefore(unitLine, ev => ev.Line, out var last) && last.On;
}
