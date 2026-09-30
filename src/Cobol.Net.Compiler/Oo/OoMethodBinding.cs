// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;

namespace CobolNet.Compiler.Oo;

/// <summary>
/// The AFTER-DATA-BIND half of a method's description (P9 R7 — phase-explicit): the resolved formal list,
/// RETURNING item, data roots, and (stamped later still, at class-BODY bind) the pc range. Attached to
/// <see cref="OoMethodSymbol.Binding"/> by <c>DataBinder.OoBindMethodData</c> once the method's data has
/// bound — reading it earlier is a null-deref (a TYPE-level ordering fact), never a silent <c>-1</c>
/// sentinel mis-read. (<c>OverrideOf</c> stays on the SYMBOL: it is a pass-1 identity fact —
/// <c>OoClassTable.Build</c> marks it and the CsName adoption depends on it before any data binds.)
/// </summary>
public sealed class OoMethodBinding
{
    /// <summary>The ordered PD USING formals (§14.9.23.4 GR3 — positional correspondence; every formal is
    /// BY REFERENCE, the header BY VALUE phrase being an unparsed grammar extension).</summary>
    public List<OoFormal> Formals { get; } = [];

    /// <summary>The PD RETURNING item (a LINKAGE 01/77 — §14.2.3 GR6: callee-allocated; the method's C# return
    /// value delivers it, §14.9.23.4 GR8), or null for a void method.</summary>
    public DataItem? Returning { get; set; }

    /// <summary>The method's LINKAGE roots (ALL of them — formals, the RETURNING item, and any unattached
    /// entry): each becomes a capturable C# LOCAL of the emitted method.</summary>
    public List<DataItem> LinkageRoots { get; } = [];

    /// <summary>LOCAL-STORAGE roots → C# locals, re-initialized on every activation (§8.6.4).</summary>
    public List<DataItem> LocalRoots { get; } = [];

    /// <summary>The method's contiguous pc range in its class's one dispatch space — assigned by
    /// <c>StatementBinder.BindClassBody</c> (the exit-bounded range IS the fall-through guard: running past
    /// the last paragraph returns from the method, never into a sibling's paragraphs — the legacy trap #4).
    /// Entry &gt; End ⇔ an empty method body.</summary>
    public int EntryPc { get; set; } = -1;
    public int EndPc { get; set; } = -2;   // Entry > End ⇔ an empty method body

    /// <summary>The first pc of the method's slice: its declarative sections occupy
    /// <c>[DeclStartPc .. EntryPc−1]</c> (empty — DeclStartPc == EntryPc — for a method with none), reached only
    /// through the method's USE selection or an explicit PERFORM (ISO §14.9.49.3 SR4; §14.2.3 GR1). The emitted
    /// <c>__MDispatch</c> carries cases for <c>[DeclStartPc .. EndPc]</c>; the activation runs
    /// <c>[EntryPc .. EndPc]</c> (kb/Work PB1010).</summary>
    public int DeclStartPc { get; set; } = -1;

    /// <summary>The method's own USE declaratives in source order (kb/Work PB1010; ISO §14.2.2 SR10 admits
    /// Format 1 in a method definition). §14.9.49.4 GR3 selects over "the USE statements in the source element",
    /// and GR4 a) makes the method the source element for its statements; a method is contained in no source
    /// element with a procedure division of its own (§14.2.2 SR12/SR13), so no GR4 b) GLOBAL walk exists.</summary>
    public IReadOnlyList<CobolNet.Binding.Bound.BoundDeclarative> Declaratives { get; set; } = [];

    /// <summary>The method's contiguous sub-range of the class's appended Format-3 (exception-checking) PERFORM
    /// handler pc-space (ISO §14.9.28.4 GR17; design SSOT §9.10). The class's handler pcs
    /// <c>[F3HandlerBasePc .. F3HandlerBasePc+H−1]</c> partition into per-method sub-ranges in method order — a
    /// method's handlers are all appended while that method's paragraphs bind (<c>StatementBinder.BindMethodRoster</c>'s
    /// pc-order second loop). <see cref="HandlerCount"/> == 0 for a method with no F3 PERFORM (the byte-identical
    /// default); the emitter includes <c>[HandlerStartPc .. HandlerStartPc+HandlerCount−1]</c> as extra cases in this
    /// method's <c>__MDispatch</c> and emits the method-local F3 machinery. Stamped by <c>BindMethodRoster</c>.</summary>
    public int HandlerStartPc { get; set; }
    public int HandlerCount { get; set; }
}
