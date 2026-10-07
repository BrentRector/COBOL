// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// The D18 subscript / reference-modifier expression re-parse (fix-queue PB17; ISO §8.4.2.3.2 admits
/// <c>arithmetic-expression-1</c> as a subscript, §8.4.3.3.3 SR4 as a leftmost-position/length). A subscript
/// SEGMENT arrives at the binder as the tokens of one parsed subscript item (kb/Work PB2113), which
/// <c>ReferenceResolver.RenderSegment</c> renders to C# text directly — a token renderer that handles literals,
/// data-names, index-names, the operators and parentheses, and nothing else. A segment it cannot render (today a
/// function-identifier) is re-lexed HERE from its verbatim source text and parsed through the ONE
/// <c>arithmeticExpression</c> rule, so it binds through <c>ExpressionBinder.BindExpr</c> — the documented entry
/// for subscripts — instead of growing a third hand-written expression compiler inside the renderer.
///
/// The DEFAULT lexer mode is used (the <see cref="DirectiveExpressionFragment"/> posture): the text is ONE subscript
/// the main parse already delimited, so no list region is open in it and the §8.3.3.3.2 sign-adjacent literal twins
/// do not fire. The fragment parses at the CALLER's edition so a
/// function referenced in a subscript meets the same D8 introduction window it would meet anywhere else.
/// Any syntax error is a loud <see langword="null"/> — the caller keeps its existing not-implemented posture
/// rather than binding a partial parse.
/// </summary>
public static class SubscriptExpressionFragment
{
    /// <summary>Parse one subscript / reference-modifier segment's source text (verbatim from the char stream so
    /// spacing survives) to the fragment CST, or <see langword="null"/> on any syntax error.</summary>
    public static CobolParserCore.SubscriptExpressionFragmentContext? Parse(string text, EditionInfo edition, TokenRetypes retypes) =>
        FragmentParse.Parse(text, edition, retypes, prime: null, rewriteZero: true,
            static p => p.subscriptExpressionFragment());
}
