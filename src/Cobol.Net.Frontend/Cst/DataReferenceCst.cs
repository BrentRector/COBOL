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
/// <para>The suffixes (qualification, the subscript list <see cref="Core.SubscriptPartContext"/> and the reference
/// modifier <see cref="Core.RefModPartContext"/>) are not on the façade: they are parse nodes since D10 removed the
/// SUBSCRIPT lexer mode (kb/Work PB2113), and the binder's <c>ReferenceResolver.ReadWritten</c> reads them off the
/// raw context as the reference AS WRITTEN. The façade's former "has no suffix" test was deleted (kb/Work PB2193):
/// <c>dataReferenceSuffix</c> carries qualification as well as subscripts, so that test could not tell a legal
/// qualified reference from a subscripted one (kb/Work PB457).</para>
/// </summary>
public readonly struct DataReferenceCst(Core.DataReferenceContext ctx)
{
    /// <summary>The special-register kind this reference names, else <see cref="SpecialRegister.None"/>.</summary>
    public SpecialRegister Register =>
          ctx.LINAGE_COUNTER() is not null ? SpecialRegister.LinageCounter
        : ctx.LINE_COUNTER()   is not null ? SpecialRegister.LineCounter
        : ctx.PAGE_COUNTER()   is not null ? SpecialRegister.PageCounter
        : SpecialRegister.None;

    /// <summary>The base data-name text (the leading <c>cobolWord</c>), or <see langword="null"/> for a bare
    /// special register (whose <c>cobolWord</c>, if present, is a qualifier — see the register early-returns).</summary>
    public string? BaseName => ctx.cobolWord()?.GetText();

    /// <summary>Non-invasive adoption: an existing call site passes the raw context unchanged.</summary>
    public static implicit operator DataReferenceCst(Core.DataReferenceContext c) => new(c);
}
