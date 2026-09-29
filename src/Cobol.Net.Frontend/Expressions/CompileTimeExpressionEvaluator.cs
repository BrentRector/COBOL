// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Common;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Frontend.Expressions;

using Core = CobolParserCore;

/// <summary>
/// The ONE compile-time expression evaluator (ISO/IEC 1989:2023 §7.3.6 arithmetic; §7.3.7/§8.8.2 boolean is added
/// with the frontend directive rewire). It walks a parsed <c>arithmeticExpression</c> and returns its value,
/// reachable by BOTH consumers of a compile-time expression (the singular-pattern requirement, ledger C2): the
/// frontend conditional-compilation stage (which fragment-parses a directive operand) and the CONSTANT-entry
/// binder (§13.10.4 GR4, which already has the parse tree). It reads NO consumer state — everything specific is
/// injected: how a name resolves to a numeric value, where diagnostics go (code-preserving), the per-consumer
/// operand wording/citation, and the DECIMAL-POINT IS COMMA mode.
///
/// The §7.3.11.4 GR5 reclassification (a single numeric literal stays a literal, keeping its fractional value) and
/// the §7.3.6.3 GR3 truncation (an arithmetic EXPRESSION's final result is truncated to its integer part) are
/// applied HERE, at the public <see cref="EvaluateArithmeticOperand"/> boundary — the raw-value recursion stays
/// private so intermediate results are correctly un-truncated (§7.3.6.3 GR1), and no consumer re-implements the
/// probe/truncate rule.
///
/// The MODE of arithmetic (§7.3.6.3 GR2) is the edition's: the constructor selects it through
/// <see cref="CompileTimeArithmetic.For"/> — standard arithmetic at 2002/2014, where Annex E.2 6) says the previous
/// standard prescribed it, and the documented System.Decimal mode from 2023 (kb/Work PB1592). Values travel in the
/// mode-independent <see cref="CtNumeric"/> carrier, so a consumer never sees which mode produced one.
/// </summary>
public sealed class CompileTimeExpressionEvaluator
{
    private readonly Func<string, CtValue?> _resolveName;
    private readonly ICtDiagnostics _diag;
    private readonly CtOperandVocabulary _vocab;
    private readonly bool _decimalPointIsComma;
    private readonly CompileTimeArithmetic _arithmetic;
    private readonly int _literalDigits;

    /// <param name="edition">The targeted edition — it selects the §7.3.6.3 GR2 arithmetic mode
    /// (<see cref="CompileTimeArithmetic.For"/>) and the §8.3.3.3.2 fixed-point literal capacity
    /// (<see cref="EditionInfo.MaxDigits"/>) every literal operand and every §7.3.6.3 GR3 result is held to.</param>
    /// <param name="resolveName">A bare (unqualified, unsubscripted) name → its bound <see cref="CtValue"/> if it is
    /// a currently-defined constant/compilation-variable, else <see langword="null"/>. An arithmetic operand uses
    /// only the NUMERIC case (§7.3.6.2 SR1b — a non-numeric or undefined name is rejected); a boolean operand uses
    /// the BOOLEAN case (§7.3.7 substitution); a defined-condition uses mere presence (§7.3.8.4.4).</param>
    /// <param name="diag">The code-preserving diagnostic sink (§5.2 of the design).</param>
    /// <param name="vocab">Per-consumer operand wording + citation.</param>
    /// <param name="decimalPointIsComma">The active §12.3.7 GR14a mode (binder: the real SPECIAL-NAMES setting;
    /// frontend: false — a directive operand is processed before SPECIAL-NAMES is bound, so it is dot-decimal).</param>
    public CompileTimeExpressionEvaluator(
        EditionInfo edition, Func<string, CtValue?> resolveName, ICtDiagnostics diag, CtOperandVocabulary vocab,
        bool decimalPointIsComma)
    {
        _arithmetic = CompileTimeArithmetic.For(edition);
        _literalDigits = edition.MaxDigits;
        _resolveName = resolveName;
        _diag = diag;
        _vocab = vocab;
        _decimalPointIsComma = decimalPointIsComma;
    }

    /// <summary>The final value of one compile-time arithmetic operand (§7.3.6). <paramref name="WasSingleLiteral"/>
    /// is true when the operand was a single numeric literal (§7.3.11.4 GR5 / §13.10.3 SR1 — treated as a literal,
    /// NOT truncated). <paramref name="Value"/> is the value in the <see cref="CtNumeric"/> carrier.
    /// <paramref name="Literal"/> is the literal the operand STANDS FOR, as source text — the substitution form a
    /// consumer stores: a single literal EXACTLY AS WRITTEN (its sign and its decimal separator included — §13.10.4
    /// GR1, "as if literal-1 … were written where constant-name-1 is written", kb/Work PB1230), or an expression's
    /// §7.3.6.3 GR3 integer literal. It is never a normalized form: a consumer that needs the value reads
    /// <paramref name="Value"/>, and one that re-binds the literal takes it through its own literal chokepoint in the
    /// active DECIMAL-POINT mode, exactly as it would the written literal. <paramref name="IsInteger"/> is true for
    /// an integer literal (fixed-point, no decimal separator) and for every expression result (GR3).</summary>
    public readonly record struct CtNumber(bool WasSingleLiteral, CobolDec Value, string Literal, bool IsInteger);

    /// <summary>Evaluate one compile-time arithmetic operand (ISO §7.3.6), applying §7.3.11.4 GR5 (single-literal
    /// reclassification) and §7.3.6.3 GR3 (integer truncation of an expression's final result) at this boundary.
    /// <see langword="null"/> (already reported) on any §7.3.6.2 violation.</summary>
    public CtNumber? EvaluateArithmeticOperand(Core.ArithmeticExpressionContext expr, string where)
    {
        // ⛔ THE §8.8.1.2 TABLE 3 FORMATION SCREEN RUNS FIRST, BEFORE THE SINGLE-LITERAL PROBE BELOW — and that
        // ORDER is still the point (kb/Work PB158, narrowed by PB400). The screen and the probe below now share
        // ONE contiguity test — ArithmeticFormationRules.SignIsPartOfLiteral — so `- - 5` is neither a
        // permissible (unary, literal) pair nor a single literal; what the ORDER buys is that the violation is
        // reported here instead of falling silently through to EvalArith. This method's recursion, the
        // compiler's ExpressionBinder and that probe are three evaluating arms of one rule, which is exactly why
        // the rule is one shared frontend screen invoked per tree rather than a gate copied into each walker.
        if (FormationViolation(expr, where)) return null;
        // §7.3.11.4 GR5 / §13.10.3 SR1 — a single (possibly signed) numeric literal is a LITERAL, not an
        // expression, so it keeps its value (AS 0.25 stays 0.25) and is NOT truncated. As a literal it may be of
        // ANY numeric class, so a floating-point (E-form) literal is valid here — unlike a §7.3.6.2 SR1b
        // arithmetic-EXPRESSION operand, which must be fixed-point (see ParseLiteral). No arithmetic mode applies to
        // it (kb/Work PB1592): the carrier holds every valid literal exactly, so the only limits are the literal's
        // own — its fixed-point digit capacity (§8.3.3.3.2) and the decimal128 exponent range this compiler gives a
        // floating-point literal in every arithmetic mode (§8.3.3.3.3 r3) — each rejected LOUDLY (never a silent
        // null — the boundary's "null means already reported" contract).
        if (SoleNumericLiteral(expr) is { } lit)
        {
            string text = CobolNet.Common.NumericLiteral.Normalize(lit, _decimalPointIsComma, out var issue);
            ReportSeparator(issue, lit);
            bool floating = CobolNet.Common.NumericLiteral.IsFloatingPointForm(text);
            if (!floating && !WithinLiteralCapacity(text, lit, where)) return null;
            if (CtNumeric.TryParseLiteral(text, out var v))
                return new CtNumber(true, v, lit, IsInteger: CobolNet.Common.NumericLiteral.IsIntegerLiteralForm(text));
            _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: the numeric literal '{lit}' lies outside the "
                + "decimal128 range this compiler gives a numeric literal, about 1E-6176 to 9.99E+6144 (ISO "
                + "§8.3.3.3.3 r3; §8.8.1.5.2 r2; CONFORMANCE.md §7)");
            return null;
        }
        // An arithmetic EXPRESSION: evaluate the raw value in the edition's mode (intermediates un-truncated,
        // §7.3.6.3 GR1), then truncate the FINAL result to its integer part (§7.3.6.3 GR3 / INTEGER-PART §15.49).
        // GR3 also makes that value "an integer numeric literal", so it is held to the literal's digit capacity
        // (§8.3.3.3.2) — reachable only under standard arithmetic, whose 34-digit intermediates can carry a
        // product of two 31-digit operands that no literal of this edition can spell.
        if (EvalArith(expr, where) is not { } result) return null;
        CobolDec truncated = CtNumeric.IntegerPart(result);
        int digits = CtNumeric.IntegerDigits(truncated);
        if (digits > _literalDigits)
        {
            _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: the final result of the compile-time arithmetic "
                + $"expression has {digits} digits, but it is an integer numeric literal (ISO §7.3.6.3 GR3) and a "
                + $"fixed-point numeric literal has at most {_literalDigits} digits (ISO §8.3.3.3.2)");
            return null;
        }
        return new CtNumber(false, truncated, CtNumeric.ToIntegerText(truncated), IsInteger: true);
    }

    /// <summary>The §8.3.3.3.2 fixed-point literal capacity (<see cref="EditionInfo.MaxDigits"/>), counted the way
    /// every other literal screen counts it — digit positions — reported through the evaluator's sink.</summary>
    private bool WithinLiteralCapacity(string canonicalText, string written, string where)
    {
        int digits = canonicalText.Count(char.IsAsciiDigit);
        if (digits <= _literalDigits) return true;
        _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: the numeric literal '{written}' has {digits} digit "
            + $"positions; a fixed-point numeric literal has at most {_literalDigits} (ISO §8.3.3.3.2)");
        return false;
    }

    // ── The §7.3.6 raw-value recursion (lifted from the CONSTANT binder's battery-tested EvalConstExpr) ──────────

    /// <summary>Evaluate a compile-time arithmetic expression to its raw (un-truncated) value (ISO §7.3.6):
    /// operands are fixed-point numeric literals (§7.3.6.2 SR1b) or previously-defined numeric names substituting
    /// them; exponentiation is rejected (SR1a); division by zero is rejected (SR1c); every operand enters, and every
    /// operation runs in, the edition's <see cref="CompileTimeArithmetic"/> mode (§7.3.6.3 GR2), whose range bounds
    /// the intermediate results. <see langword="null"/> (already reported) on any violation.</summary>
    private CobolDec? EvalArith(IParseTree node, string where)
    {
        switch (node)
        {
            case Core.ArithmeticExpressionContext a:
                return EvalArith(a.GetChild(0), where);
            case Core.AdditiveExpressionContext or Core.MultiplicativeExpressionContext:
            {
                CobolDec? acc = null;
                char op = '+';
                for (int i = 0; i < node.ChildCount; i++)
                {
                    var c = node.GetChild(i);
                    if (c is Core.AddOpContext or Core.MulOpContext) { op = c.GetText()[0]; continue; }
                    if (EvalArith(c, where) is not { } v) return null;
                    if (acc is not { } left) { acc = v; continue; }
                    if (op == '/' && v.Sig == 0)
                    {
                        _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: the compile-time arithmetic "
                            + "expression divides by zero — the expression shall be specified in such a "
                            + "way that a division by zero cannot occur (ISO §7.3.6.2 SR1c)");
                        return null;
                    }
                    acc = _arithmetic.Apply(op, left, v);
                    if (acc is null)
                    {
                        _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: an intermediate result of the compile-time "
                            + $"arithmetic expression exceeds {_arithmetic.RangeDescription}");
                        return null;
                    }
                }
                return acc;
            }
            case Core.PowerExpressionContext p:
            {
                var bases = p.unaryExpression();
                if (bases.Length > 1)
                {
                    _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: the exponentiation operator shall not be "
                        + "specified in a compile-time arithmetic expression (ISO §7.3.6.2 SR1a)");
                    return null;
                }
                return EvalArith(bases[0], where);
            }
            case Core.UnaryExpressionContext u:
            {
                if (u.primaryExpression() is { } pr) return EvalArith(pr, where);
                var inner = EvalArith(u.unaryExpression(), where);
                return inner is not { } value ? null
                    : u.addOp().GetText() == "-" ? new CobolDec(-value.Sig, value.Exp)   // negation is exact
                    : value;
            }
            case Core.PrimaryExpressionContext pe:
            {
                if (pe.numericLiteral() is { } num) return ParseLiteral(num.GetText(), where);
                if (pe.ZERO_ARITH() is not null) return new CobolDec(0, 0);
                if (pe.arithmeticExpression() is { } paren) return EvalArith(paren, where);
                if (pe.dataReference() is { } dref)
                {
                    // A name operand substitutes its literal (§7.3.6.2 SR1b) — only a BARE (unqualified,
                    // unsubscripted) NUMERIC constant/compilation-variable is a valid operand, and its value
                    // enters the expression through the edition's mode like any literal operand.
                    if (dref.dataReferenceSuffix().Length == 0 && dref.cobolWord() is { } w
                        && _resolveName(w.GetText()) is { Category: CtCategory.Numeric } cv)
                        return EnterOperand(cv.Number, w.GetText(), where);
                    _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: '{dref.GetText()}' — all operands of the "
                        + $"compile-time arithmetic expression shall be fixed-point numeric literals or {_vocab.OperandSource} "
                        + $"({_vocab.GoverningCitation})");
                    return null;
                }
                _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: '{pe.GetText()}' is not a valid compile-time "
                    + $"arithmetic operand — operands shall be fixed-point numeric literals ({_vocab.GoverningCitation})");
                return null;
            }
            default:
                _diag.Report(CtDiagCode.ArithmeticRule,
                    $"{where}: unsupported compile-time arithmetic expression shape (ISO §7.3.6)");
                return null;
        }
    }

    /// <summary>Parse one fixed-point numeric literal operand (§7.3.6.2 SR1b): dot-decimal after the §12.3.7 GR14a
    /// normalization; a floating-point (E-form) literal is NOT fixed-point and rejects; a literal past the
    /// §8.3.3.3.2 digit capacity rejects; the value then enters the edition's mode (<see cref="EnterOperand"/>),
    /// whose range is the §7.3.6.2 SR2 limit — a separate rule from SR1b, reported as such.</summary>
    private CobolDec? ParseLiteral(string text, string where)
    {
        string norm = CobolNet.Common.NumericLiteral.Normalize(text, _decimalPointIsComma, out var issue);
        ReportSeparator(issue, text);
        bool fixedPoint = !CobolNet.Common.NumericLiteral.IsFloatingPointForm(norm);
        if (fixedPoint && !WithinLiteralCapacity(norm, text, where)) return null;
        if (!fixedPoint || !CtNumeric.TryParseLiteral(norm, out var v))
        {
            _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: '{text}' — all operands of a compile-time arithmetic "
                + "expression shall be fixed-point numeric literals (ISO §7.3.6.2 SR1b)");
            return null;
        }
        return EnterOperand(v, text, where);
    }

    /// <summary>An operand value entering the expression in the edition's mode (§7.3.6.3 GR2), or
    /// <see langword="null"/> (reported) when it lies outside the mode's range (§7.3.6.2 SR2).</summary>
    private CobolDec? EnterOperand(CobolDec value, string written, string where)
    {
        if (_arithmetic.Enter(value) is { } entered) return entered;
        _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: the operand '{written}' of the compile-time arithmetic "
            + $"expression exceeds {_arithmetic.RangeDescription}");
        return null;
    }

    /// <summary>Report a §12.3.7 GR14a decimal-separator violation through the code-preserving sink (the consumer
    /// routes <see cref="CtDiagCode.NumericSeparator"/> to COBOLNET0895 / its own code). The message matches the
    /// binder's historical wording so its diagnostic is unchanged.</summary>
    private void ReportSeparator(NumericSeparatorIssue issue, string text)
    {
        switch (issue)
        {
            case NumericSeparatorIssue.DecimalPointUnderCommaMode:
                _diag.Report(CtDiagCode.NumericSeparator, $"numeric literal '{text}': under DECIMAL-POINT IS COMMA "
                    + "the decimal separator is the comma (ISO §12.3.7.4 GR14a); '.' is not valid in a numeric literal");
                break;
            case NumericSeparatorIssue.CommaWithoutCommaMode:
                _diag.Report(CtDiagCode.NumericSeparator, $"numeric literal '{text}': a comma decimal separator "
                    + "requires DECIMAL-POINT IS COMMA (ISO §12.3.7.4 GR14a; §8.3.3.3.2 admits only '.' as the decimal point)");
                break;
        }
    }

    /// <summary>The single (possibly signed) numeric literal an arithmetic expression consists of, AS WRITTEN, or
    /// null — the §13.10.3 SR1 / §7.3.11.4 GR5 re-classification probe ("if the operand consists of a single numeric
    /// literal, that operand is treated as a literal, not as an arithmetic-expression").
    /// <para>⛔ THE LITERAL IS RETURNED AS WRITTEN — its '+' is NOT dropped (kb/Work PB1230). Canonicalizing
    /// <c>+5</c> to <c>5</c> here made <c>CONSTANT AS +5</c> substitute <c>5</c>, so the constant DISPLAYed '5' where
    /// the literal +5 displays '+5' — not §13.10.4 GR1's "as if literal-1 … were written". Equal values are
    /// <see cref="CtNumber.Value"/>'s question, never the text's.</para>
    /// <para>⛔ THE DESCENT IS <see cref="SoleOperand.NumericLiteral"/>, NOT A LOCAL COPY (kb/Work PB400). This
    /// used to walk its own spine and TOGGLE the sign through a stacked unary chain, which reclassified
    /// <c>- -5</c> — an operand that does not CONSIST OF a single literal — as the literal 5. The shared descent
    /// applies §8.3.3.3.2 rule 2's contiguity test instead, so exactly one ADJACENT sign is part of the literal
    /// and everything else is an arithmetic-expression operand (and is therefore §7.3.6.3 GR3-truncated, which
    /// the toggling version silently skipped).</para></summary>
    private static string? SoleNumericLiteral(Core.ArithmeticExpressionContext expr) => SoleOperand.NumericLiteral(expr);

    // ══ Directive-context operand dispatch (§7.3.3 SR10 master constraint) ═══════════════════════════════════════

    /// <summary>Evaluate one compile-time operand (a <c>&gt;&gt;DEFINE</c> value / <c>&gt;&gt;EVALUATE</c> subject /
    /// <c>&gt;&gt;WHEN</c> object) to a category-tagged <see cref="CtValue"/> (ISO §7.3). Enforces the §7.3.3 SR10
    /// master constraint — no floating-point literal, no figurative constant, no concatenation expression in a
    /// compiler directive — which is why a directive numeric operand is NOT the same as a CONSTANT data-entry
    /// operand (§13.10.3 admits a floating-point literal; a directive does not). <see langword="null"/> (already
    /// reported) on any violation.</summary>
    public CtValue? EvaluateOperand(Core.CompileTimeOperandContext op, string where)
    {
        if (op.booleanExpression() is { } be)
            return EvaluateBoolean(be, where) is { } b ? CtValue.Boolean(b) : null;
        if (op.arithmeticExpression() is { } ae)
        {
            // A bare (unqualified, unsubscripted) NAME substitutes its stored value — of ANY category (numeric /
            // alphanumeric / national / boolean; §7.3.11.4 GR6-GR8). Only a genuine arithmetic EXPRESSION (or a
            // numeric literal) runs through the §7.3.6 numeric core.
            if (SoleDataRef(ae) is { } dref && dref.dataReferenceSuffix().Length == 0 && dref.cobolWord() is { } w)
            {
                if (_resolveName(w.GetText()) is { } cv) return cv;
                ReportDirective(where, $"'{w.GetText()}' is not a previously-defined compilation variable (ISO §7.3.11 / §13.10.3)");
                return null;
            }
            return EvaluateDirectiveArithmetic(ae, where) is { } n ? CtValue.Numeric(n.Value, n.Literal) : null;
        }
        if (op.nonNumericLiteral() is { } nn)
            return NonNumericOperand(nn, where);
        ReportDirective(where, "an empty compile-time operand");
        return null;
    }

    /// <summary>A directive arithmetic operand: the §7.3.3 SR10 float/figurative bar applied BEFORE the shared
    /// §7.3.6 arithmetic core (which stays consumer-agnostic — the CONSTANT binder keeps float acceptance).</summary>
    private CtNumber? EvaluateDirectiveArithmetic(Core.ArithmeticExpressionContext expr, string where)
    {
        if (ContainsToken(expr, Core.FLOATLIT) || ContainsToken(expr, Core.COMMA_FLOATLIT))
        { ReportDirective(where, "a floating-point numeric literal shall not appear in a compiler directive (ISO §7.3.3 SR10)"); return null; }
        if (ContainsToken(expr, Core.ZERO_ARITH))
        { ReportDirective(where, "a figurative constant shall not appear in a compiler directive (ISO §7.3.3 SR10)"); return null; }
        return EvaluateArithmeticOperand(expr, where);
    }

    private CtValue? NonNumericOperand(Core.NonNumericLiteralContext nn, string where)
    {
        if (nn.concatenationExpression() is not null)
        { ReportDirective(where, "a concatenation expression shall not appear in a compiler directive (ISO §7.3.3 SR10)"); return null; }
        if (nn.figurativeConstant() is not null)
        { ReportDirective(where, "a figurative constant shall not appear in a compiler directive (ISO §7.3.3 SR10)"); return null; }
        if (LiteralRuleViolated(nn, where)) return null;
        if (nn.STRINGLIT() is { } s) return CtValue.Alphanumeric(CobolLiteral.Decode(s.GetText()));
        if (nn.HEXLIT() is { } h) return CtValue.Alphanumeric(CobolLiteral.Decode(h.GetText()));   // X"…" — category alphanumeric
        if (nn.NATLIT() is { } nat) return CtValue.National(CobolLiteral.Decode(nat.GetText()));
        if (nn.BOOLLIT() is { } bl) return CtValue.Boolean(BitString.Of(CobolLiteral.Decode(bl.GetText())));
        ReportDirective(where, $"'{nn.GetText()}' is not a supported compile-time literal operand");
        return null;
    }

    // ══ Boolean fold (§7.3.7 → §8.8.2) via the ONE shared BooleanExpressionResolver ═════════════════════════════

    /// <summary>Evaluate one compile-time boolean operand (ISO §7.3.7 → §8.8.2) to its bit string via the ONE
    /// shared <see cref="BooleanExpressionResolver"/> — the same precedence/grouping the runtime binder uses,
    /// including the context-inherited rule-7b shift precedence a context-free grammar cannot express. Compile-time
    /// operands are boolean LITERALS (§7.3.7.2 SR1) and previously-defined boolean compilation-variable
    /// substitutions; §7.3.3 SR10 bars the figurative constants (<c>ZERO</c> / <c>ALL "literal"</c>) that the
    /// runtime §8.8.2 admits. <see langword="null"/> (already reported) on any violation, propagated through the
    /// fold so an errored sub-expression never yields a value.</summary>
    public BitString? EvaluateBoolean(Core.BooleanExpressionContext expr, string where) =>
        // §8.8.2 Table 4's (B-NOT, B-NOT) cell — the boolean twin of the arithmetic screen above, from the same
        // shared rule so the two tables are enforced by ONE mechanism (kb/Work PB158).
        FormationViolation(expr, where) ? null
        : BooleanExpressionResolver.Resolve<BitString?>(
            expr,
            leaf: vo => BooleanLeaf(vo, where),
            not: b => b?.Not(),
            binary: (l, op, r) => l is null || r is null ? null : BitString.Combine(l, op, r),
            shift: (b, suf) => BooleanShift(b, suf, where));

    /// <summary>Resolve a boolean-expression leaf operand (§8.8.2 operand list) to its bit string. §7.3.7.2 SR1
    /// admits ONLY a boolean literal (or a substituted previously-defined boolean compilation variable); §7.3.3
    /// SR10 bars a figurative constant / concatenation; a non-boolean literal is not a boolean operand.</summary>
    private BitString? BooleanLeaf(Core.ValueOperandContext vo, string where)
    {
        if (vo.nonNumericLiteral() is { } nn)
        {
            if (nn.BOOLLIT() is { } bl) return LiteralRuleViolated(nn, where) ? null : BitString.Of(CobolLiteral.Decode(bl.GetText()));
            if (nn.concatenationExpression() is not null)
            { ReportDirective(where, "a concatenation expression shall not appear in a compiler directive (ISO §7.3.3 SR10)"); return null; }
            if (nn.figurativeConstant() is not null)
            { ReportDirective(where, "a figurative constant shall not appear in a compile-time boolean expression (ISO §7.3.3 SR10 / §7.3.7.2 SR1 — boolean literals only)"); return null; }
            ReportDirective(where, $"'{nn.GetText()}' is not a boolean operand — a compile-time boolean expression admits boolean literals only (ISO §7.3.7.2 SR1 / §8.8.2)");
            return null;
        }
        // A bare data-name substitutes a previously-defined BOOLEAN compilation variable (§7.3.7 — its value is a
        // boolean literal). Any other arithmetic operand is not a boolean operand.
        if (vo.arithmeticExpression() is { } expr && SoleDataRef(expr) is { } dref
            && dref.dataReferenceSuffix().Length == 0 && dref.cobolWord() is { } w)
        {
            if (_resolveName(w.GetText()) is { Category: CtCategory.Boolean, Bits: { } b }) return b;
            ReportDirective(where, $"'{w.GetText()}' is not a previously-defined boolean compilation variable (ISO §7.3.7.2 SR1)");
            return null;
        }
        ReportDirective(where, $"'{vo.GetText()}' is not a valid compile-time boolean operand (ISO §7.3.7.2 SR1 / §8.8.2)");
        return null;
    }

    /// <summary>Apply one boolean shift suffix (<c>(B-SHIFT-L|R|LC|RC) integer</c>, §8.8.2 rule 8). The second
    /// operand is an INTEGER operand (rule 5). Its SHAPE — a single identifier or literal (Table 4) — was already
    /// screened by <see cref="FormationViolation"/> through the shared <see cref="ArithmeticFormationRules"/>, so it
    /// is one literal or one compilation-variable name here (kb/Work PB1370). Either way the operand the rule sees
    /// is a LITERAL (a name is used "where a literal of the category associated with the name is permitted",
    /// §7.3.11.4 GR1, and stands for its value), and §5.5 2) a) requires an INTEGER literal —
    /// a FORM test (<see cref="CobolNet.Common.NumericLiteral.IsIntegerLiteralForm"/>), so <c>1.0</c> is refused
    /// though its value is integral, and a name holding 1.5 is refused rather than truncated. A negative count is
    /// rejected (the spec defines only counts ≥ 1). Result length = the first operand's length.</summary>
    private BitString? BooleanShift(BitString? operand, Core.BooleanShiftSuffixContext suf, string where)
    {
        if (operand is null) return null;   // already reported
        bool circular = suf.B_SHIFT_LC() is not null || suf.B_SHIFT_RC() is not null;
        bool left = suf.B_SHIFT_L() is not null || suf.B_SHIFT_LC() is not null;
        if (ShiftCount(suf.arithmeticExpression(), where) is not { } count) return null;
        if (!count.IsInteger)
        {
            ReportDirective(where, $"the second operand of a boolean shift shall be an integer operand (ISO §8.8.2 "
                + $"rule 5) — '{count.Literal}' is not an integer literal: \"An integer literal is a fixed-point numeric "
                + "literal that contains no decimal point\" (ISO §8.3.3.3.2; §5.5 2) a))");
            return null;
        }
        if (count.Value.Sig < 0)
        { ReportDirective(where, "a boolean shift count shall not be negative (ISO §8.8.2 rule 8)"); return null; }
        // Reduce the count to a small equivalent BEFORE the (long) cast so an astronomically large literal count
        // cannot overflow the cast: a LOGICAL shift by ≥ the length is all boolean zeros (cap at the length), and a
        // CIRCULAR shift is periodic in the length (mod). A zero-length operand shifts to itself. The count is an
        // integer of at most the edition's literal capacity (31 digits — the boundary above enforced it), so it
        // fits an Int128.
        int n = operand.Length;
        Int128 c = CtNumeric.ToInt128(count.Value);
        long k = n == 0 ? 0
               : circular ? (long)(c % n)
               : c > n ? n : (long)c;
        return operand.Shift(k, circular, left);
    }

    /// <summary>The literal a well-formed shift count stands for: a numeric literal as written, or the value of a
    /// previously-defined NUMERIC compilation variable (§7.3.11.4 GR1 — the name stands for its literal, whose
    /// text <see cref="CtValue.Text"/> keeps; an expression-valued variable's text is its §7.3.6.3 GR3 integer), its
    /// <see cref="CtNumber.IsInteger"/> the ONE integer-literal form test over that text. <see langword="null"/>
    /// (reported) otherwise — a name is never GR3-truncated here, which is what let a variable holding 1.5 shift
    /// by 1.</summary>
    private CtNumber? ShiftCount(Core.ArithmeticExpressionContext count, string where)
    {
        if (SoleDataRef(count) is not { } dref) return EvaluateDirectiveArithmetic(count, where);
        if (dref.dataReferenceSuffix().Length == 0 && dref.cobolWord() is { } w
            && _resolveName(w.GetText()) is { Category: CtCategory.Numeric } cv)
            return new CtNumber(true, cv.Number, cv.Text,
                IsInteger: CobolNet.Common.NumericLiteral.IsIntegerLiteralForm(cv.Text));
        ReportDirective(where, $"'{dref.GetText()}' — the second operand of a boolean shift shall be an integer "
            + "operand (ISO §8.8.2 rule 5): an integer literal or a previously-defined numeric compilation variable "
            + "holding one (§7.3.11.4 GR1)");
        return null;
    }

    // ══ Constant-conditional-expression (§7.3.8) over the ANTLR tree ═════════════════════════════════════════════

    /// <summary>Evaluate a constant-conditional-expression (ISO §7.3.8) — true/false, or <see langword="null"/>
    /// when a formation rule is violated (already reported). Per §8.8.4.13 the VALUE may short-circuit, but a
    /// FORMATION error is reportable regardless of branch, so every AND/XOR/OR operand is evaluated; the frontend
    /// treats a null result as false for line selection.</summary>
    public bool? EvaluateCce(Core.ConstantConditionalExpressionContext cce, string where) => EvalCceOr(cce.cceOr(), where);

    private bool? EvalCceOr(Core.CceOrContext o, string where)
    {
        bool result = false, ok = true;
        foreach (var x in o.cceXor())
        {
            var v = EvalCceXor(x, where);
            if (v is null) ok = false; else result |= v.Value;
        }
        return ok ? result : (bool?)null;
    }

    /// <summary>The exclusive-or tier (§8.8.4.9 — "true if one but not both of the included conditions is true";
    /// precedence between AND and OR, §8.8.4.11.3). Both operands are always evaluated, as for AND and OR — and XOR
    /// has no short circuit to take in any case. Its 2023 introduction gate is the conditional-compilation stage's
    /// (<see cref="LogicalOperatorGate"/>), asked of the whole fragment before it is evaluated (kb/Work PB1371).</summary>
    private bool? EvalCceXor(Core.CceXorContext x, string where)
    {
        bool result = false, ok = true;
        foreach (var a in x.cceAnd())
        {
            var v = EvalCceAnd(a, where);
            if (v is null) ok = false; else result ^= v.Value;
        }
        return ok ? result : (bool?)null;
    }

    private bool? EvalCceAnd(Core.CceAndContext a, string where)
    {
        bool result = true, ok = true;
        foreach (var n in a.cceNot())
        {
            var v = EvalCceNot(n, where);
            if (v is null) ok = false; else result &= v.Value;
        }
        return ok ? result : (bool?)null;
    }

    /// <summary>§8.8.4.10's negation — one NOT at most (Table 5: 'NOT NOT' is not permissible; the grammar's
    /// <c>NOT? ccePrimary</c> makes a second one a malformed fragment).</summary>
    private bool? EvalCceNot(Core.CceNotContext n, string where) =>
        EvalCcePrimary(n.ccePrimary(), where) is { } v ? (n.NOT() is not null ? !v : v) : (bool?)null;

    private bool? EvalCcePrimary(Core.CcePrimaryContext p, string where)
    {
        if (p.constantConditionalExpression() is { } inner) return EvaluateCce(inner, where);   // ( … )
        if (p.definedCondition() is { } d) return EvalDefined(d);
        return EvalRelationOrBoolean(p.cceRelationOrBoolean(), where);
    }

    /// <summary>A defined-condition (§7.3.8.4.4): <c>name IS [NOT] DEFINED</c> — true iff the compilation variable
    /// is currently defined (a name in scope resolves to a non-null value), negated by NOT.</summary>
    private bool EvalDefined(Core.DefinedConditionContext d)
    {
        bool defined = _resolveName(d.cobolWord().GetText()) is not null;
        return d.NOT() is not null ? !defined : defined;
    }

    private bool? EvalRelationOrBoolean(Core.CceRelationOrBooleanContext r, string where)
    {
        // The bare simple-boolean-condition alt (§8.8.4.3) — a genuine boolean expression used as a condition.
        if (r.booleanExpression() is { } be) return SimpleBoolean(EvaluateBoolean(be, where), where);
        var operands = r.compileTimeOperand();
        var left = EvaluateOperand(operands[0], where);
        if (r.comparisonOperator() is not { } opCtx)
        {
            // A bare operand as a cce primary — only a BOOLEAN operand (a length-1 boolean literal) is a valid
            // simple boolean condition (§8.8.4.3); any other bare operand is not a condition.
            if (left is null) return null;
            if (left.Category == CtCategory.Boolean) return SimpleBoolean(left.Bits, where);
            ReportDirective(where, $"'{operands[0].GetText()}' is not a valid constant-conditional-expression (ISO §7.3.8)");
            return null;
        }
        var right = operands.Length > 1 ? EvaluateOperand(operands[1], where) : null;
        if (left is null || right is null) return null;
        return CceRelation(left, right, opCtx, r.NOT() is not null, where);
    }

    /// <summary>A simple boolean condition (§8.8.4.3): SR1 — the value shall be of length 1; GR1 — true iff the
    /// bit is 1.</summary>
    private bool? SimpleBoolean(BitString? bits, string where)
    {
        if (bits is null) return null;
        if (bits.Length != 1)
        { ReportDirective(where, "a simple boolean condition shall reference a boolean value of length 1 (ISO §8.8.4.3.3 SR1)"); return null; }
        return bits.IsTrue;
    }

    /// <summary>A constant-conditional relation (§7.3.8.2 SR1a / §7.3.8.3 GR2): SR1a.1 both operands same category;
    /// SR1a.2 non-numeric operands admit only equal/unequal; numeric compared by value; non-numeric compared by
    /// binary character/bit value, LENGTH-SENSITIVE (unequal length ⇒ unequal, no collating). The relation-level
    /// <c>NOT</c> negates the result.</summary>
    private bool? CceRelation(CtValue left, CtValue right, Core.ComparisonOperatorContext opCtx, bool negate, string where)
    {
        if (left.Category != right.Category)
        { ReportDirective(where, $"the operands of a constant-conditional relation shall be of the same category ('{left.Category}' vs '{right.Category}', ISO §7.3.8.2 SR1a.1)"); return null; }
        string op = MapOperator(opCtx.GetText());
        if (negate) op = NegateOp(op);
        if (left.Category == CtCategory.Numeric)
        {
            int cmp = CobolDec.Compare(left.Number, right.Number);
            return op switch { "==" => cmp == 0, "!=" => cmp != 0, "<" => cmp < 0, ">" => cmp > 0, "<=" => cmp <= 0, ">=" => cmp >= 0, _ => false };
        }
        if (op is not ("==" or "!="))
        { ReportDirective(where, "a non-numeric constant-conditional relation admits only IS EQUAL / IS NOT EQUAL (ISO §7.3.8.2 SR1a.2)"); return null; }
        // §7.3.8.3 GR2 — Alphanumeric/National binary + LENGTH-sensitive; §8.8.4.2.8 — Boolean right-zero-extends
        // the shorter operand (GR2's length-sensitivity is for operands "not numeric or boolean").
        bool eq = left.RelationalEquals(right);
        return op == "==" ? eq : !eq;
    }

    // ── shared small helpers ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Report a compiler-directive expression formation violation through the code-preserving sink (the
    /// frontend routes <see cref="CtDiagCode.DirectiveRule"/> to COBOLNET1619).</summary>
    private void ReportDirective(string where, string message) => _diag.Report(CtDiagCode.DirectiveRule, $"{where}: {message}");

    /// <summary>A directive operand's literal asks the literal's own §8.3.3 rules — content repertoire, hexadecimal
    /// grouping, length — through <see cref="CobolLiteral.SyntaxViolation"/>, exactly as <c>LiteralScreenPass</c> asks
    /// them of the unit's tokens (kb/Work PB1441). The directive fragment is lexed apart from the unit, so no other screen
    /// ever sees this token, and a literal the rules refuse has no value: the decoders answer "" for it, which this
    /// evaluator must not take as the operand's value. True (reported) when a rule is violated.</summary>
    private bool LiteralRuleViolated(Core.NonNumericLiteralContext nn, string where)
    {
        if (CobolLiteral.SyntaxViolation(nn.GetText()) is not { } v) return false;
        ReportDirective(where, $"the literal {CobolLiteral.Abbreviated(nn.GetText())} {v.Message}");
        return true;
    }

    /// <summary>The frontend's invocation of the SHARED expression-formation rule (ISO §8.8.1.2 Table 3 /
    /// §8.8.2 Table 4) — <see cref="ArithmeticFormationRules"/>, the same rule the compiler's
    /// <c>ExpressionFormationPass</c> applies to a whole parse tree. Routed to
    /// <see cref="CtDiagCode.ArithmeticRule"/>, the kind this consumer already owns for §7.3.6 formation
    /// violations. Returns true when at least one violation was reported, so the caller returns its
    /// "null means already reported" result rather than evaluating a shape the standard does not admit.</summary>
    private bool FormationViolation(IParseTree expr, string where)
    {
        bool any = false;
        ArithmeticFormationRules.Check(expr, (_, message) =>
        {
            any = true;
            _diag.Report(CtDiagCode.ArithmeticRule, $"{where}: {message}");
        });
        return any;
    }

    /// <summary>The sole (unqualified, unsubscripted) data reference an arithmetic expression consists of, or null
    /// — through <see cref="SoleOperand"/>, THE ONE single-child descent (kb/Work PB224). It was the last
    /// surviving copy of that walk and it survived the PB172 collapse only because the other five lived in
    /// <c>Cobol.Net.Compiler</c>, which this assembly cannot reference; the shared body therefore moved HERE,
    /// where the parse trees are defined.</summary>
    private static Core.DataReferenceContext? SoleDataRef(Core.ArithmeticExpressionContext expr) =>
        SoleOperand.DataRef(expr);

    /// <summary>True when the subtree contains a terminal of the given token <paramref name="type"/> — the §7.3.3
    /// SR10 float/figurative scan over a directive arithmetic operand.</summary>
    private static bool ContainsToken(IParseTree t, int type)
    {
        if (t is ITerminalNode term) return term.Symbol.Type == type;
        for (int i = 0; i < t.ChildCount; i++) if (ContainsToken(t.GetChild(i), type)) return true;
        return false;
    }

    /// <summary>Normalize a <c>comparisonOperator</c>'s concatenated text to <c>==</c>/<c>!=</c>/<c>&lt;</c>/<c>&gt;</c>/
    /// <c>&lt;=</c>/<c>&gt;=</c> (the §8.8.4.2 relational-operator set). Mirrors the runtime binder's mapping — a small
    /// CFG-independent pure function duplicated across the Frontend↔Compiler layer boundary.</summary>
    private static string MapOperator(string raw)
    {
        string t = raw.ToUpperInvariant().Replace("IS", "").Replace("THAN", "").Replace("TO", "");
        if (t.Contains("<>")) return "!=";
        bool not = t.Contains("NOT");
        bool orEqual = t.Contains(">=") || t.Contains("<=") || t.Contains("OREQUAL");
        string baseOp =
            t.Contains('>') || t.Contains("GREATER") ? (orEqual ? ">=" : ">")
            : t.Contains('<') || t.Contains("LESS") ? (orEqual ? "<=" : "<")
            : "==";
        return not ? NegateOp(baseOp) : baseOp;
    }

    private static string NegateOp(string op) => op switch
    { ">" => "<=", ">=" => "<", "<" => ">=", "<=" => ">", "==" => "!=", _ => "==" };
}
