// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Expressions;

using Core = CobolParserCore;

/// <summary>
/// The ONE screen for the two EXPRESSION FORMATION TABLES — ISO/IEC 1989:2023 §8.8.1.2 Table 3 (arithmetic) and
/// §8.8.2 Table 4 (boolean). Both tables state which ordered PAIRS of adjacent symbols are permissible; a '—' cell
/// is an invalid pair. Most cells are excluded STRUCTURALLY by the expression tiers, and this screen owns exactly
/// the cells the tiers still admit.
///
/// <para><b>Which cells those are was MEASURED, not deduced</b> (2026-09-02, one probe program per cell, parsed
/// through the real compiler — kb/Work PB158). Of Table 3's thirteen invalid cells, ten are already hard parse
/// errors: (ident,ident) <c>A B</c>, (binop,binop) <c>A + * B</c>, (binop,')') <c>(A + )</c>, (unary,binop)
/// <c>- * A</c>, (unary,')') <c>( - )</c>, ('(',binop) <c>( * A )</c>, ('(',')') <c>( )</c>, (')' ,ident)
/// <c>(A) B</c>, (')' ,unary) and (')','(') <c>(A) (B)</c>. Two more — (ident,'(') and its (')' ,'(') sibling —
/// are not producible as an arithmetic PAIR at all: COBOL's own reference syntax reads a '(' after an identifier
/// as a subscript or reference-modifier, so the juxtaposition never forms. Screening any of those twelve would be
/// dead code. What remains is ONE cell: <b>(unary, unary)</b>. Table 4's counterparts are <b>(B-NOT, B-NOT)</b>,
/// admitted by <c>booleanFactor</c>'s self-recursion, and the <b>boolean-shift-operator row</b> — whose only
/// permissible second symbol is an identifier or literal (rule 5's integer operand), admitted wider by
/// <c>booleanShiftSuffix</c>'s <c>arithmeticExpression</c> count (<see cref="ShiftCountNotSoleOperand"/>,
/// kb/Work PB1370 / PB1413).</para>
///
/// <para><b>Why a screen and not a grammar tier.</b> §8.8.4.11.3's Table 5 excludes its identical cell
/// structurally, because <c>unaryLogicalExpression</c> was written non-self-recursive, and converging Table 3 on
/// that shape was the first candidate. It does not work, and the reason is a MEASURED property of the lexer:
/// §8.3.3.3.2 rule 2 makes a leading sign part of a numeric literal ("A fixed-point numeric literal is a
/// character-string … If a sign is used, it shall appear as the leftmost character of the literal"), and a
/// character-string is contiguous — so <c>- -2</c> is (unary, LITERAL) = <b>P, permissible</b>, while <c>- - 2</c>
/// is (unary, unary) = invalid. In the DEFAULT lexer mode both spellings produce the same token sequence
/// (MINUS MINUS INTEGERLIT); the SIGNED_INTEGERLIT / SIGNED_DECIMALLIT twins that encode the adjacency exist only
/// in the FUNCTION-argument and SUBSCRIPT regions. A grammar tier therefore cannot separate the legal spelling
/// from the illegal one, and a tier that rejected both would reject legal source — the worse failure. Token
/// POSITIONS do carry the distinction, so the rule is applied here, over the parse tree, with the adjacency read
/// off the token stream.
/// </para>
///
/// <para><b>Why it is shared and consumer-generic</b> (the <see cref="BooleanExpressionResolver"/> pattern): the
/// same rule has THREE evaluating arms — <c>ExpressionBinder.BindExprCore</c> in the compiler, and
/// <c>CompileTimeExpressionEvaluator.EvalArith</c> AND its <c>SoleNumericLiteral</c> literal-reclassification
/// probe in the frontend. A gate copied into each is the anti-pattern, and the third arm is the one a two-arm fix
/// misses: <c>SoleNumericLiteral</c> toggles the sign through a stacked unary chain and would happily reclassify
/// <c>- - 5</c> as the literal 5. This class lives in the FRONTEND because the compile-time evaluator runs during
/// compiler-directive processing, before any compiler pass exists; it reports through a caller-supplied sink so
/// each consumer keeps its own diagnostic code.</para>
/// </summary>
public static class ArithmeticFormationRules
{
    /// <summary>The §8.8.1.2 Table 3 message — quotes the table and names the two symbols of the invalid pair.</summary>
    public const string StackedUnaryMessage =
        "ISO §8.8.1.2 Table 3 (combinations of symbols in arithmetic expressions): the pair "
        + "(unary '+' or '−', unary '+' or '−') is an invalid pair — a unary operator may not be immediately "
        + "followed by another unary operator. Note that a sign written ADJACENT to the digits is part of the "
        + "numeric literal (§8.3.3.3.2 rule 2), so '- -2' is the permissible (unary, literal) pair; only a "
        + "SEPARATED second sign is a second unary operator";

    /// <summary>The §8.8.2 Table 4 message — the boolean counterpart of the same cell.</summary>
    public const string StackedNotMessage =
        "ISO §8.8.2 Table 4 (combination of symbols in boolean expressions): the pair (B-NOT, B-NOT) is an "
        + "invalid pair — B-NOT may not be immediately followed by another B-NOT. (§8.8.4.11.3's Table 5 states "
        + "the same restriction for conditions outright: \"the pair 'NOT NOT' is not permissible\".)";

    /// <summary>Screen every expression under <paramref name="tree"/> with a plain recursive walk. This is the
    /// convenience entry for a consumer that has no traversal of its own — the frontend's compile-time evaluator,
    /// which is handed one expression at a time. The COMPILER consumer does NOT use it: it overrides the two
    /// visit methods of its own <c>CursorFollowingVisitor</c> walk so the diagnostic cursor keeps following the
    /// nodes (one positioning mechanism, not two), and calls <see cref="StackedUnarySign"/> /
    /// <see cref="StackedNot"/> directly. The RULE is those two per-node tests; this is only a way to reach them.</summary>
    /// <param name="tree">The parse tree to screen; null is a no-op.</param>
    /// <param name="report">Receives the offending token and the message. The CODE is the caller's.</param>
    public static void Check(IParseTree? tree, Action<IToken, string> report)
    {
        if (tree is null) return;
        switch (tree)
        {
            case Core.UnaryExpressionContext u when StackedUnarySign(u) is { } sign:
                report(sign, StackedUnaryMessage);
                break;
            case Core.BooleanFactorContext bf when StackedNot(bf) is { } not:
                report(not, StackedNotMessage);
                break;
            case Core.BooleanShiftSuffixContext shift when ShiftCountNotSoleOperand(shift) is { } count:
                report(count, ShiftCountMessage(shift));
                break;
        }
        for (int i = 0; i < tree.ChildCount; i++) Check(tree.GetChild(i), report);
    }

    /// <summary>The §8.8.2 Table 4 (B-NOT, B-NOT) test. <c>booleanFactor : B_NOT booleanFactor | …</c>
    /// self-recurses, so <c>B-NOT B-NOT x</c> parses; Table 4's B-NOT row gives its B-NOT column '—'. Returns the
    /// SECOND B-NOT's token, else null. There is no literal-sign subtlety here — B-NOT is a reserved word and can
    /// never be part of an operand — so the nesting alone decides.</summary>
    public static IToken? StackedNot(Core.BooleanFactorContext bf) =>
        bf.B_NOT() is not null && bf.booleanFactor()?.B_NOT() is { } second ? second.Symbol : null;

    /// <summary>The §8.8.2 Table 4 shift-operator row, with rule 2's ending and rule 5's operand (kb/Work PB1370,
    /// PB1413). Table 4 permits exactly ONE symbol after B-SHIFT-L / B-SHIFT-R / B-SHIFT-LC / B-SHIFT-RC —
    /// "Identifier or literal" — and rule 5 makes it "an integer operand"; rule 2 then requires the expression to
    /// END there (or continue with a boolean operator / ')'). The grammar parses the count as a whole
    /// <c>arithmeticExpression</c>, so a parenthesized count <c>(1)</c> (the invalid (shift, '(') cell), a signed
    /// operator <c>- 1</c> (a unary operator is no Table 4 symbol at all) and a compound count <c>1 + 1</c> (an
    /// arithmetic operator where rule 2 requires the end) all parse. Returns the count's first token when the count
    /// is not a single identifier or literal, else null.
    /// <para><b>Why a screen and not a narrower grammar rule</b> — the same measured lexer fact as
    /// <see cref="StackedUnarySign"/>: a signed integer literal <c>-1</c> is an integer literal (§5.5 2) a) routes
    /// to §8.3.3.3.2, which admits the leftmost sign) while <c>- 1</c> is a unary operator, and the default-mode
    /// lexer emits MINUS INTEGERLIT for both. Only the token POSITIONS separate them, through
    /// <see cref="SoleOperand.NumericLiteral"/>'s contiguity test — so the one rule that must read positions owns
    /// the whole cell rather than splitting it between a grammar tier and a screen. Whether the single operand is
    /// an INTEGER (an integer literal, an integer data item, an integer function — §5.5 2)) is a binding fact
    /// each lane asks after this: the runtime binder through the ONE integer classifier, the compile-time
    /// evaluator through the literal's form.</para></summary>
    public static IToken? ShiftCountNotSoleOperand(Core.BooleanShiftSuffixContext shift)
    {
        var count = shift.arithmeticExpression();
        if (count is null) return null;                                   // an already-errored parse
        if (SoleOperand.NumericLiteral(count) is not null) return null;   // a literal, its sign contiguous (§8.3.3.3.2 2))
        // An identifier — a data reference, a function-identifier or an inline method invocation (§8.4.3.1.2) —
        // or the figurative ZERO the token rewriter hands an arithmetic slot. A PARENTHESIZED primary is the
        // invalid (shift, '(') pair.
        return SoleOperand.Primary(count) is { } primary && primary.arithmeticExpression() is null ? null : count.Start;
    }

    /// <summary>The §8.8.2 Table 4 / rule 5 message for a shift count that is not a single identifier or literal.</summary>
    public static string ShiftCountMessage(Core.BooleanShiftSuffixContext shift) =>
        $"ISO §8.8.2 rule 5 and Table 4 (combination of symbols in boolean expressions): the second operand of "
        + $"{shift.GetChild(0).GetText()} shall be an integer operand, and the only symbol Table 4 permits after a "
        + $"boolean shift operator is a single identifier or literal — '{Parsing.WrittenSource.Of(shift.arithmeticExpression())}' is "
        + "an arithmetic expression. A parenthesized count or a separated sign is an invalid pair, and an arithmetic "
        + "operator cannot follow the count (rule 2: the expression ends with the integer operand, ')' or a boolean "
        + "operator)";

    /// <summary>The §8.8.1.2 Table 3 (unary, unary) test. Returns the SECOND sign's token when this
    /// <c>unaryExpression</c> is a unary operator immediately followed by another unary operator, else null.
    ///
    /// <para>The whole subtlety is the second sign: <c>unaryExpression : addOp unaryExpression |
    /// primaryExpression</c> nests, so a nested <c>addOp</c> is present for BOTH <c>- - 2</c> (two unary
    /// operators — invalid) and <c>- -2</c> (a unary operator then a signed numeric literal — permissible,
    /// row 'Unary + or −' × column 'Identifier or literal' = P). The trees are identical because the default-mode
    /// lexer emits MINUS MINUS INTEGERLIT for both. §8.3.3.3.2 rule 2 decides between them, and its criterion is
    /// CONTIGUITY — the literal is "a character-string", and a space is a separator (§8.3.5) — so the test is
    /// whether the second sign abuts a numeric literal that is the whole operand.</para></summary>
    public static IToken? StackedUnarySign(Core.UnaryExpressionContext u)
    {
        if (u.addOp() is null) return null;                       // not a unary operator at all
        if (u.unaryExpression() is not { } inner) return null;    // operand is a primary — nothing stacked
        if (inner.addOp() is not { } innerSign) return null;      // operand is not itself signed
        return SignIsPartOfLiteral(innerSign.Stop, inner.unaryExpression()) ? null : innerSign.Start;
    }

    /// <summary>⛔ THE ONE §8.3.3.3.2 rule-2 TEST — "A literal shall not contain more than one sign character.
    /// If a sign is used, it shall appear as the leftmost character of the literal." The sign is part of the
    /// numeric literal exactly when the literal is one contiguous character-string — the sign's last character
    /// immediately precedes the literal's first, on the same line — AND the operand is that literal ALONE (an
    /// operator, a second sign, or a parenthesis anywhere in the operand means the sign governs an EXPRESSION,
    /// not a literal, and is therefore a unary operator).
    /// <para>Two rules consult it, and they are the two halves of the same fact. <see cref="StackedUnarySign"/>
    /// asks it to separate Table 3's permissible <c>- -2</c> (unary, LITERAL) from its invalid <c>- - 2</c>
    /// (unary, unary); <see cref="SoleOperand.NumericLiteral"/> asks it to decide whether an operand "consists
    /// of a single literal" — the question §7.3.11.4 GR5 (DEFINE), §13.10.3 SR1 (CONSTANT) and §14.9.13.4 GR1
    /// (EVALUATE) each ask in their own words. A second copy of the contiguity test is how a signed literal
    /// becomes an arithmetic expression on one path and a literal on another (kb/Work PB400).</para></summary>
    public static bool SignIsPartOfLiteral(IToken sign, Core.UnaryExpressionContext? operand)
    {
        if (operand?.primaryExpression() is not { } primary) return false;
        IToken first = primary.Start;
        if (first.Line != sign.Line || first.StartIndex != sign.StopIndex + 1) return false;   // separated ⇒ unary
        // The operand must be the bare literal: walk the sole-child spine and require a numeric literal at the
        // end. `- -2 * 3` reaches here with primary = 2 only, so the spine test is over `primary` itself.
        IParseTree n = primary;
        while (n.ChildCount == 1) n = n.GetChild(0);
        return n is Core.NumericLiteralCoreContext or ITerminalNode { Parent: Core.NumericLiteralCoreContext };
    }
}
