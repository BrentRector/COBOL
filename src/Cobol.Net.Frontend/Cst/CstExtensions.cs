// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Cst;

using Core = CobolParserCore;

/// <summary>The small stringly-typed helpers the binder repeats over the highest-churn leaf rules
/// (<c>cobolWord</c>, <c>integerLiteral</c>) — the named replacements for the raw <c>GetText()</c> /
/// <c>int.Parse(GetText())</c> idioms (rearchitecture PHASE 04, Group C).</summary>
public static class CstExtensions
{
    /// <summary>The user-defined-word text of a <c>cobolWord</c> (a data-name, qualifier, index-name, …).</summary>
    public static string Name(this Core.CobolWordContext ctx) => ctx.GetText();

    /// <summary>The name an entry-name clause (§13.18.20) GIVES its entry, or <see langword="null"/> when the entry
    /// is unnamed: the clause omitted (§13.18.20.3 SR2, "the word FILLER is assumed") or its filler format
    /// written (§13.18.20.3 SR3, §13.18.20.4 GR1 — FILLER names the item but is never a name that can be referred
    /// to). The ONE reading of the shared <c>dataName</c> rule a report group description entry's name slot uses
    /// (kb/Work PB1226); a caller that needs the FILLER stand-in for a message writes <c>?? "FILLER"</c>.</summary>
    public static string? NameOrNull(this Core.DataNameContext? ctx) =>
        ctx is null || ctx.FILLER() is not null ? null : ctx.GetText();
}
