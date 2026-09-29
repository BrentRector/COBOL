// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Binding;

/// <summary>
/// The compile-time <c>&gt;&gt;PROPAGATE</c> resolution (ISO/IEC 1989:2023 §7.3.21; kb/Work PB1119): the
/// source-ordered ON/OFF directive toggles, folded at a source element's first line to decide whether AUTOMATIC
/// PROPAGATION of exception conditions is enabled for that function, method or program. §7.3.21.4 GR1: "When the ON
/// phrase is specified or implied, automatic propagation of exception conditions becomes enabled for functions,
/// methods, and programs that follow in the compilation group"; GR3 disables it the same way for OFF; GR4: "The
/// default for a compilation group is PROPAGATE OFF." What an enabled element DOES with it is the emitter's
/// (<c>EcEmitter.EmitSelection</c> — §14.6.13.1.3 6)).
/// </summary>
public sealed class PropagateState
{
    private readonly DirectiveTimeline<PropagateEvent> _events;

    /// <summary>The empty state — no directive; every element is OFF (the §7.3.21.4 GR4 default).</summary>
    public static readonly PropagateState Empty = new(DirectiveTimeline<PropagateEvent>.Empty);

    private PropagateState(DirectiveTimeline<PropagateEvent> events) => _events = events;

    /// <summary>Build the state from the frontend's directive events (introduction-gated and operand-checked at the
    /// directive-recognition point). A <see cref="DirectiveTimeline{T}"/> carries the PUSH/POP history: a
    /// POP-revoked toggle is skipped (§7.3.20.4 GR1/GR3; kb/Work PB941).</summary>
    public static PropagateState Build(IReadOnlyList<PropagateEvent>? events)
        => events is null || events.Count == 0 ? Empty : new PropagateState(DirectiveTimeline<PropagateEvent>.Of(events));

    /// <summary>Is automatic propagation enabled for the source element whose first line is
    /// <paramref name="elementLine"/>? The most recent toggle strictly before the element wins; OFF when none
    /// precedes it.</summary>
    public bool IsOnAt(int elementLine) =>
        _events.TryLastInEffectBefore(elementLine, ev => ev.Line, out var last) && last.On;
}
