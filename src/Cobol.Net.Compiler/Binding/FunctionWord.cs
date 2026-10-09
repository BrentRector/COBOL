// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Parsing;
using CobolNet.Runtime;

namespace CobolNet.Binding;

/// <summary>
/// ⛔ A FUNCTION-NAME WORD AS THE <c>&gt;&gt;COBOL-WORDS</c> DIRECTIVE LEFT IT, RESOLVED EXACTLY ONCE (ISO §7.3.10.4
/// GR2/GR3/GR4; kb/Work PB1372). The binder classifies a name by what the directive made of it — a synonym denotes its
/// canonical intrinsic, a de-reserved word denotes none — and the lexer ALREADY applied the directive to every token it
/// could reach (<see cref="CobolWordsRewriter.Plan"/>: a synonym of a keyword token arrives respelled canonically, a
/// de-reserved keyword arrives as an <c>IDENTIFIER</c>). Resolving the text of a token a second time reads a
/// SUBSTITUTE'd literal-4 — canonical AND de-reserved at once — as "removed", so
/// <c>SUBSTITUTE "SUM" BY "TOTAL"</c> made <c>FUNCTION TOTAL(1 2 3)</c> "not an intrinsic function". The two factories
/// are the only ways to make one, so every consumer asks the SAME once-only question:
/// <see cref="OfToken"/> for a word read off a token (the token-aware <see cref="CobolWordsRewriter.CanonicalWordOf"/>
/// knows which tokens were resolved already) and <see cref="OfWrittenWord"/> for a word that is source text no lexer
/// token carries (a SUBSCRIPT-mode capture).
/// </summary>
/// <param name="Written">The word's text as the parser saw it (a respelled synonym of a keyword token reads canonically).</param>
/// <param name="Canonical">The canonical word the directive makes of it, or null when the directive REMOVED it (UNDEFINE
/// literal-3 / SUBSTITUTE literal-4): it is no longer an intrinsic-function-name.</param>
internal readonly record struct FunctionWord(string Written, string? Canonical)
{
    /// <summary>True when the directive removed the word: it names no intrinsic function in this group.</summary>
    public bool RemovedByDirective => Canonical is null;

    /// <summary>The name to look up: the canonical word, or the written one when the directive removed it (a
    /// REPOSITORY-declared user function may still carry that name).</summary>
    public string Name => Canonical ?? Written;

    /// <summary>The word a lexed token denotes under <paramref name="map"/>. Without a directive the text is kept as
    /// written (the zero-overhead path).</summary>
    public static FunctionWord OfToken(IToken tok, CobolWordsMap map) =>
        map.IsEmpty ? new(tok.Text, tok.Text) : new(tok.Text, CobolWordsRewriter.CanonicalWordOf(tok, map));

    /// <summary>The word as WRITTEN in source text under <paramref name="map"/> — the directive applied once, here.</summary>
    public static FunctionWord OfWrittenWord(string written, CobolWordsMap map) =>
        map.IsEmpty ? new(written, written) : new(written, map.Resolve(CobolNames.UpperFold(written)));
}
