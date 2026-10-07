// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Cst;

using Core = CobolParserCore;

/// <summary>The special-register kind a <c>dataReference</c> names, or <see cref="None"/> for an ordinary
/// (optionally qualified/subscripted) data-name reference. These registers are runtime-sourced, never a storage
/// place (ISO §8.4.3.14/§8.4.3.15).</summary>
public enum SpecialRegister { None, LinageCounter, LineCounter, PageCounter }

/// <summary>
/// Typed façade over <see cref="Core.DataReferenceContext"/> (rearchitecture PHASE 04, Group C) — the narrow
/// surface the binder's <c>ReferenceResolver</c> reads instead of raw <c>GetText()</c>/positional walks. Thin and
/// 1:1 with the <c>dataReference</c> grammar rule: it holds the context and names the accessors, it computes NO
/// semantic state (that belongs to the binder). A grammar-rule rename now breaks THIS file (a compile error)
/// instead of drifting silently across the ~336 raw <c>GetText()</c> sites (P7 migrates the rest).
/// <para>The subscript list (<see cref="Core.SubscriptPartContext"/>) and the reference modifier
/// (<see cref="Core.RefModPartContext"/>) are reached through <see cref="Context"/>: both are parse nodes since D10
/// removed the SUBSCRIPT lexer mode (kb/Work PB2113), read by the binder's <c>ReferenceResolver.ReadWritten</c>.</para>
/// </summary>
public readonly struct DataReferenceCst(Core.DataReferenceContext ctx)
{
    /// <summary>The wrapped raw context — the escape hatch for reads not yet lifted onto the façade (the suffix
    /// classify walk; P7).</summary>
    public Core.DataReferenceContext Context => ctx;

    /// <summary>The special-register kind this reference names, else <see cref="SpecialRegister.None"/>.</summary>
    public SpecialRegister Register =>
          ctx.LINAGE_COUNTER() is not null ? SpecialRegister.LinageCounter
        : ctx.LINE_COUNTER()   is not null ? SpecialRegister.LineCounter
        : ctx.PAGE_COUNTER()   is not null ? SpecialRegister.PageCounter
        : SpecialRegister.None;

    /// <summary>The base data-name text (the leading <c>cobolWord</c>), or <see langword="null"/> for a bare
    /// special register (whose <c>cobolWord</c>, if present, is a qualifier — see the register early-returns).</summary>
    public string? BaseName => ctx.cobolWord()?.GetText();

    /// <summary>True when the reference carries NO suffix (no subscript / ref-mod / qualification) — the
    /// no-side-effect OCCURS-DYNAMIC CAPACITY-register peek relies on this exact shape (data-model D9).</summary>
    public bool HasNoSuffix => ctx.dataReferenceSuffix().Length == 0;

    public SourceSpan Span => SourceSpan.Of(ctx);

    /// <summary>Non-invasive adoption: an existing call site passes the raw context unchanged.</summary>
    public static implicit operator DataReferenceCst(Core.DataReferenceContext c) => new(c);
}
