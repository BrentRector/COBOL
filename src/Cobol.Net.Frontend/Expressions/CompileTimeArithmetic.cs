// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Editions;
using CobolNet.Runtime;

namespace CobolNet.Frontend.Expressions;

/// <summary>
/// THE mode of arithmetic in which a compile-time arithmetic expression is evaluated (ISO/IEC 1989:2023 §7.3.6.3
/// GR2), SELECTED ONCE PER EDITION by <see cref="For"/> — the one selection every consumer of the shared
/// <see cref="CompileTimeExpressionEvaluator"/> (a constant entry's <c>AS</c>, <c>&gt;&gt;DEFINE</c>,
/// <c>&gt;&gt;IF</c>/<c>&gt;&gt;EVALUATE</c>) reaches, because the evaluator asks it and nothing else does
/// (kb/Work PB1592).
/// <para>⛔ TWO MODES, BECAUSE THE STANDARD HAS TWO RULES. Annex E.2 item 6 says the mode "is now explicitly
/// implementor defined" and that "The previous COBOL Standard required the use of an arithmetic mode that is no
/// longer supported"; the one mode 2023 removed is Standard Arithmetic (E.2 item 21). So at 2002 and 2014 the mode
/// is PRESCRIBED — <see cref="Standard"/>, the SDIDI decimal engine this compiler already runs
/// <c>ARITHMETIC IS STANDARD</c> on (34 digits, decimal128 range, <c>ArithmeticModes.IsDecimalEngine</c>) — and
/// from 2023 it is the implementor's documented choice, <see cref="SystemDecimal"/> (docs/CONFORMANCE.md
/// DOC-A.1-29). The edition boundary is written once, in <see cref="DialectBehaviors"/>
/// (<see cref="DialectBehavior.CompileTimeArithmeticImplementorDefined"/>). COBOL-85 has no compile-time arithmetic
/// expression (the §7.3 directives and the constant entry are 2002 introductions); an expression reached there only
/// after its introduction gate has refused it takes the pre-2023 mode.</para>
/// <para>Values travel in the mode-independent carrier (<see cref="CtNumeric"/>); a mode governs only what happens
/// to a value that ENTERS an arithmetic expression and each operation on such values.</para>
/// </summary>
public abstract class CompileTimeArithmetic
{
    private CompileTimeArithmetic() { }

    /// <summary>Standard arithmetic — the mode ISO/IEC 1989:2002 and 2014 prescribe for compile-time arithmetic
    /// expressions (Annex E.2 items 6 and 21), on the SDIDI decimal engine (§8.8.1.5: 34 significant digits, the
    /// decimal128 range) with the standard-decimal default intermediate rounding, NEAREST-AWAY-FROM-ZERO
    /// (§11.9.11.2 GR3 a) — a compile-time expression has no OPTIONS paragraph to name another).</summary>
    public static readonly CompileTimeArithmetic Standard = new StandardMode();

    /// <summary>The COBOL-2023 implementor-defined mode (§7.3.6.3 GR2; docs/CONFORMANCE.md DOC-A.1-29): .NET
    /// <see cref="decimal"/> — at most 28 digits after the point, magnitude below about 7.9×10²⁸, an inexact result
    /// rounded to nearest with a tie to the even digit.</summary>
    public static readonly CompileTimeArithmetic SystemDecimal = new SystemDecimalMode();

    /// <summary>⛔ THE ONE SELECTION: the mode compile-time arithmetic expressions use at <paramref name="edition"/>.</summary>
    public static CompileTimeArithmetic For(EditionInfo edition) =>
        DialectBehaviors.IsActive(DialectBehavior.CompileTimeArithmeticImplementorDefined, edition.Year)
            ? SystemDecimal : Standard;

    /// <summary>How a diagnostic names this mode's evaluation range, so a range message states the limit of the
    /// mode that actually refused the value.</summary>
    public abstract string RangeDescription { get; }

    /// <summary>A carrier value entering an arithmetic expression as an operand — an operand literal or a
    /// substituted name — in this mode's intermediate form, or <see langword="null"/> when it lies outside this
    /// mode's range.</summary>
    public abstract CobolDec? Enter(CobolDec value);

    /// <summary>One binary operation (<c>+ - * /</c>) on two values <see cref="Enter"/> produced, or
    /// <see langword="null"/> when the result leaves this mode's range. A zero divisor is the caller's §7.3.6.2 SR1c
    /// screen and never reaches here.</summary>
    public abstract CobolDec? Apply(char op, CobolDec left, CobolDec right);

    private sealed class StandardMode : CompileTimeArithmetic
    {
        private const CobolRounding Rounding = CobolRounding.NearestAwayFromZero;

        public override string RangeDescription =>
            "the standard-arithmetic range (the standard-decimal intermediate data item: 34 significant digits, "
            + "magnitude up to 9.99E+6144 — ISO §8.8.1.5.2, prescribed for compile-time arithmetic before COBOL-2023 "
            + "by Annex E.2 items 6 and 21)";

        public override CobolDec? Enter(CobolDec value) => Guard(() => CobolDec.FromParsed(value.Sig, value.Exp, Rounding));

        public override CobolDec? Apply(char op, CobolDec left, CobolDec right) => Guard(() => op switch
        {
            '+' => CobolDec.Add(left, right, Rounding),
            '-' => CobolDec.Sub(left, right, Rounding),
            '*' => CobolDec.Mul(left, right, Rounding),
            '/' => CobolDec.Div(left, right, Rounding),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, "not a compile-time arithmetic operator"),
        });

        /// <summary>The SDIDI's §8.8.1.5.2 r2 range check raises <see cref="CobolSizeError"/> (EC-SIZE-OVERFLOW /
        /// EC-SIZE-UNDERFLOW); at compile time that is the caller's range diagnostic, never a run-time condition.</summary>
        private static CobolDec? Guard(Func<CobolDec> operation)
        {
            try { return operation(); }
            catch (CobolSizeError) { return null; }
        }
    }

    private sealed class SystemDecimalMode : CompileTimeArithmetic
    {
        private const NumberStyles LiteralStyles =
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;

        public override string RangeDescription =>
            "the .NET decimal evaluation range (96-bit, 28–29 significant digits — the documented §7.3.6.2 SR2 "
            + "implementor limit)";

        public override CobolDec? Enter(CobolDec value) =>
            decimal.TryParse(CtNumeric.ToScientificText(value), LiteralStyles, CultureInfo.InvariantCulture, out decimal d)
                ? CtNumeric.FromDecimal(d) : null;

        public override CobolDec? Apply(char op, CobolDec left, CobolDec right)
        {
            decimal l = ToDecimal(left), r = ToDecimal(right);
            try
            {
                return CtNumeric.FromDecimal(op switch
                {
                    '+' => l + r,
                    '-' => l - r,
                    '*' => l * r,
                    '/' => l / r,
                    _ => throw new ArgumentOutOfRangeException(nameof(op), op, "not a compile-time arithmetic operator"),
                });
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        /// <summary>A value <see cref="Enter"/> or <see cref="Apply"/> produced back as the <see cref="decimal"/> it
        /// came from — exact, because it IS a decimal's coefficient and scale.</summary>
        private static decimal ToDecimal(CobolDec v)
        {
            Int128 magnitude = Int128.Abs(v.Sig);
            if (v.Exp > 0 || v.Exp < -28 || magnitude >> 96 != 0)
                throw new InvalidOperationException(
                    $"compile-time System.Decimal operand {CtNumeric.ToScientificText(v)} did not enter through Enter");
            return new decimal((int)(uint)magnitude, (int)(uint)(magnitude >> 32), (int)(uint)(magnitude >> 64),
                v.Sig < 0, (byte)-v.Exp);
        }
    }
}
