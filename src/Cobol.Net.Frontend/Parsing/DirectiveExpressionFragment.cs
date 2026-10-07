// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// The compile-time directive-expression re-parse (ISO/IEC 1989:2023 §7.3.6/§7.3.7/§7.3.8). The frontend
/// conditional-compilation stage isolates a directive's operand / constant-conditional-expression TEXT (a
/// <c>&gt;&gt;DEFINE</c> value, a <c>&gt;&gt;EVALUATE</c> subject / <c>&gt;&gt;WHEN</c> object, a <c>&gt;&gt;IF</c>
/// cce) and re-lexes it here with the lexer primed as a directive-expression region
/// (<see cref="CobolLexer.PrimeDirectiveExpr"/>: DEFINED is a token and every '(' groups), then parses it through
/// the isolated fragment entry rules — the SAME expression grammar the main parse uses, so there is no hand-rolled
/// tokenizer / condition parser (the <see cref="SubscriptExpressionFragment"/> precedent). The tree is evaluated by the ONE
/// shared <see cref="T:CobolNet.Frontend.Expressions.CompileTimeExpressionEvaluator"/>.
///
/// The DEFAULT lexer mode is used (no list region) — §7.3.6 has no argument juxtaposition, so <c>1 - 2</c>
/// is subtraction — and the <see cref="ZeroTokenRewriter"/> is applied so a figurative <c>ZERO</c> in an arithmetic
/// operand becomes <c>ZERO_ARITH</c> (which the evaluator then rejects under §7.3.3 SR10). The operand parses at the
/// newest edition (<see cref="EditionInfo.Latest"/>) — the whole-<c>&gt;&gt;</c>-facility introduction gate below
/// 2002 is tracked separately (ledger C15). Any syntax error becomes a loud <see langword="null"/> — a partial
/// parse is never evaluated.
/// </summary>
public static class DirectiveExpressionFragment
{
    /// <summary>Parse a compile-time OPERAND (a <c>&gt;&gt;DEFINE</c> value / <c>&gt;&gt;EVALUATE</c> subject /
    /// <c>&gt;&gt;WHEN</c> object), or <see langword="null"/> on any syntax error.</summary>
    public static CobolParserCore.CompileTimeOperandFragmentContext? ParseOperand(string text) =>
        Parse(text, static p => p.compileTimeOperandFragment());

    /// <summary>Parse a constant-conditional-expression (a <c>&gt;&gt;IF</c> operand / an <c>&gt;&gt;EVALUATE TRUE</c>
    /// <c>&gt;&gt;WHEN</c> operand), or <see langword="null"/> on any syntax error.</summary>
    public static CobolParserCore.ConstantConditionalExpressionFragmentContext? ParseCce(string text) =>
        Parse(text, static p => p.constantConditionalExpressionFragment());

    /// <summary>Parse the operand list of a <c>&gt;&gt;DISPLAY</c> directive (ISO §7.3.12.2) — the repeated
    /// operands and the UPON phrase's words — or <see langword="null"/> on any syntax error. Each operand is the SAME
    /// <c>compileTimeOperand</c> a DEFINE value is, or a <c>PARAMETER</c> phrase.</summary>
    public static CobolParserCore.DisplayDirectiveFragmentContext? ParseDisplay(string text) =>
        Parse(text, static p => p.displayDirectiveFragment());

    private static T? Parse<T>(string text, System.Func<CobolParserCore, T> rule) where T : class =>
        FragmentParse.Parse(text, EditionInfo.Latest, TokenRetypes.None, static l => l.PrimeDirectiveExpr(), rewriteZero: true, rule);
}
