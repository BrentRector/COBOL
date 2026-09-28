// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The ISO/IEC 1989:2023 §8.8.4.4.4 GR3 class questions asked of a COMPUTED numeric operand — a LINAGE-COUNTER,
/// LINE-COUNTER or PAGE-COUNTER register, or a numeric / integer function-identifier (§8.5.2.12 items 3–7): a
/// category-numeric data item that has no storage of its own, only a VALUE (kb/Work PB1401). The operand arrives
/// lifted into the one decimal form every non-float carrier lifts to exactly (<see cref="CobolDec.From"/>); a
/// binary64 function result is asked on its raw bits by <see cref="CobolFloatClass"/> instead, the float twin of
/// this class.
/// <para>Each question takes the operand's value ONCE: the caller evaluates the function or reads the register a
/// single time and every comparison is made here, so a function with an effect (RANDOM's seed, an
/// EC-ARGUMENT-FUNCTION raise) is evaluated exactly as often as it is written.</para>
/// </summary>
public static class CobolValueClass
{
    /// <summary>GR3 n) 1. c. — NUMERIC over a computed numeric operand: "the content … consists entirely of a valid
    /// representation for the usage". A computed operand's content IS a value its carrier holds — there is no
    /// stored image that could hold anything else, and no PICTURE clause whose range it could exceed — so the
    /// answer is TRUE once the operand has been evaluated; the argument is taken so it is.</summary>
    public static bool IsNumeric(CobolDec value) => true;

    /// <summary>GR3 g) / m) — FARTHEST-FROM-ZERO / NEAREST-TO-ZERO over a computed operand: the value is one of the
    /// description's extremes (<paramref name="positive"/> and, when the description can hold a sign,
    /// <paramref name="negative"/>), compared algebraically in the one decimal form.</summary>
    public static bool IsExtreme(CobolDec value, CobolDec positive, CobolDec? negative) =>
        CobolDec.Compare(value, positive) == 0 || negative is { } n && CobolDec.Compare(value, n) == 0;

    /// <summary>GR3 l) — IN-ARITHMETIC-RANGE over a computed operand: "the numeric content … is neither farther
    /// from zero nor closer to zero than is permitted for the form of an intermediate data item appropriate to the
    /// mode of arithmetic in effect". <paramref name="farthest"/> and <paramref name="nearest"/> are that form's
    /// extreme MAGNITUDES; zero is permitted (the intermediate holds zero exactly, so "closer to zero than is
    /// permitted" cannot describe zero itself).</summary>
    public static bool IsInArithmeticRange(CobolDec value, CobolDec farthest, CobolDec nearest)
    {
        if (value.Sig == 0) return true;
        var magnitude = value with { Sig = Int128.Abs(value.Sig) };
        return CobolDec.Compare(magnitude, farthest) <= 0 && CobolDec.Compare(magnitude, nearest) >= 0;
    }
}
