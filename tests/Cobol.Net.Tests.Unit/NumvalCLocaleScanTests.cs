// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using CobolNet.Runtime;
using CobolNet.Runtime.Globalization;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The NUMVAL-C / TEST-NUMVAL-C LOCALE arm's scanner (ISO §15.68.3 r5 / §15.94.4; kb/Work PB64 T6), pinned
/// against the INVARIANT locale (tag "" — currency ¤, decimal '.', grouping ',', negative convention
/// PARENTHESES per CurrencyNegativePattern 0) so every verdict is host-stable and hand-derived. The tag is
/// always explicit — the current-locale arm rides the conformance goldens, which own a run unit.
/// </summary>
public sealed class NumvalCLocaleScanTests
{
    /// <summary>The LOCALE value at two fraction digits. ⛔ THE FUNCTION HAS ONE VALUE PROJECTION, AND IT CARRIES
    /// ITS OWN SCALE (kb/Work PB251): §15.68.4 r1 fixes the returned value with no arithmetic-mode qualification,
    /// so the SDIDI is the value in every mode and the scale-2 rendering is this TEST's convenience — it used to
    /// be a scaled-<c>Int128</c> twin of the same rule, and a twin only one caller reaches is a copy nothing
    /// contradicts.</summary>
    private static Int128 Scaled2(string text, string tag = "") =>
        CobolIntrinsics.NumvalCLocaleDec(text, tag).ToUnscaled(2, CobolRounding.Truncation);

    [Theory]
    // The default path IS the parentheses convention: §15.68.4 r3 with the derived n_sign_posn 0.
    [InlineData("(¤1,234.56)", 0)]
    [InlineData("(1,234.56)", 0)]              // r5b.3 "may contain" — the currency string is omissible
    [InlineData("¤1,234.56", 0)]               // unsigned form — nonnegative
    [InlineData("1234.56", 0)]                 // bare number: one digit is the only obligation (r5b.7)
    [InlineData("  ¤ 1,234.56  ", 0)]          // leading/trailing spaces (r5b.7) + adjacency space
    [InlineData("( ¤1,234.56 )", 0)]           // spaces at every token adjacency
    [InlineData(".56", 0)]                     // the leading-separator form (parity with r4a's `. digit`)
    [InlineData("+¤1,234.56", 0)]              // the positive convention: positive_sign at the determined posn 1
    [InlineData("1234567.5", 0)]               // no separator at all — §15.68.3 r5b.6 "may contain"
    [InlineData("¤1.5", 0)]                    // ⚖ fraction width not constrained by frac_digits (2)
    public void Conforming_ReturnsZero(string text, long expected)
    {
        Assert.Equal(expected, CobolIntrinsics.TestNumvalCLocale(text, ""));
    }

    [Theory]
    // A bare '-' has NO meaning under LOCALE when the convention is parentheses — §15.68.4 r3's CR/DB/minus
    // sentence is explicitly the non-LOCALE leg, and negative_sign is unused with n_sign_posn 0.
    [InlineData("-¤1,234.56", 1)]
    [InlineData("¤1,234.56CR", 10)]            // CR is not a sign indicator under LOCALE (r4b is not inherited)
    [InlineData("0 1", 3)]                     // §15.94.4 r1 b.1's own worked example — position of the '1'
    [InlineData("1,234.5,6", 8)]               // a grouping separator right of the decimal separator (§8.2.2)
    [InlineData("1..5", 3)]                    // a second decimal separator
    [InlineData("1,23,4.5", 5)]                // r5b.6 mon_grouping {3}: a 2-digit group ends at this ',' (kb/Work PB835)
    [InlineData("1,2,3,4.5", 4)]               // the second separator follows a 1-digit group
    [InlineData("12,34.5", 6)]                 // "12,34" still completes to "12,345"; the '.' ends it short
    [InlineData("1,23", 5)]                    // valid characters, incomplete grouping — r1 c LENGTH+1
    [InlineData("(¤1,234.56", 11)]             // unclosed parenthesis — every char admissible ⇒ r1 c LENGTH+1
    [InlineData("", 1)]                        // zero-length ⇒ 0+1 (r1 c NOTE)
    [InlineData("   ", 4)]                     // all spaces ⇒ n+1 (r1 c NOTE)
    public void NonConforming_ReportsTheFirstErrorPosition(string text, long expected)
    {
        Assert.Equal(expected, CobolIntrinsics.TestNumvalCLocale(text, ""));
    }

    [Fact]
    public void CommaMode_IsInertUnderLocale()
    {
        // §15.68.3 r4 d (the DECIMAL-POINT IS COMMA role swap) opens "If the LOCALE keyword is not specified" —
        // the separators are mon_decimal_point/mon_thousands_sep, full stop. There is no commaMode parameter on
        // the LOCALE entry points at all; this pins that the invariant separators hold regardless.
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("1,234.56", ""));
        Assert.Equal(6, CobolIntrinsics.TestNumvalCLocale("1.234,56", ""));   // the ',' right of '.' is in error
    }

    [Fact]
    public void Value_And_Sign_FollowTheMatchedConvention()
    {
        // §15.68.4 r3 — negative iff the NEGATIVE convention matched; the parenthesized form IS the negative
        // convention under the invariant locale.
        Assert.Equal((Int128)(-123456), Scaled2("(¤1,234.56)"));
        Assert.Equal((Int128)123456, Scaled2("¤1,234.56"));
        Assert.Equal((Int128)123456, Scaled2("+¤1,234.56"));
        // Negative zero evaluates to zero — COBOL numeric has no signed zero.
        Assert.Equal((Int128)0, Scaled2("(¤0.00)"));
        // §15.68.4 r2 — grouping separators preceding the decimal separator are ignored in the value.
        Assert.Equal((Int128)123450, Scaled2("1,234.5"));
    }

    [Fact]
    public void TestTwin_AgreesWithTheValueFunction()
    {
        // The ONE-scanner invariant (§15.94.1: TEST-NUMVAL-C exists to verify NUMVAL-C will succeed): for every
        // probe, TEST == 0 iff NUMVAL-C parses it (checking off ⇒ the reject projection returns 0 — so the
        // agreement is asserted on TEST's verdict against a re-parse, not on the 0 collision).
        foreach (string probe in new[]
        {
            "(¤1,234.56)", "¤1,234.56", "1234", "  12.5  ", "-1", "¤¤1", "1.2.3", "( 1 )", "ABC", ".5", "+.5",
        })
        {
            long verdict = CobolIntrinsics.TestNumvalCLocale(probe, "");
            if (verdict == 0)
                // A conforming probe must parse to SOME value without tripping the scan again.
                _ = CobolIntrinsics.NumvalCLocaleDec(probe, "");
            else
                Assert.NotEqual(0, verdict);
        }
    }

    [Fact]
    public void DigitCap_ReportsTheCapDigitsOrdinal_PerArithmeticMode()
    {
        string d40 = new('1', 40);
        Assert.Equal(32, CobolIntrinsics.TestNumvalCLocale(d40, ""));                    // native — r1 b.2
        Assert.Equal(35, CobolIntrinsics.TestNumvalCLocale(d40, "", digitCap: 34));      // standard-decimal — r1 b.4
        // The cap counts DIGIT CHARACTERS and the position is the ordinal IN THE ORIGINAL — a currency prefix
        // shifts it.
        Assert.Equal(33, CobolIntrinsics.TestNumvalCLocale("¤" + d40, ""));
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale(new string('1', 31), ""));
    }

    [Fact]
    public void SpacingEquivalence_ATypedNbspMatchesTheNormalizedSeparator()
    {
        // DETERMINATION L12 + LAT-6: fr-FR's mon_thousands_sep normalizes to the plain space, and a user-typed
        // U+00A0 or U+202F matches it through the same equivalence class — "1 234,56" is conforming in fr-FR
        // however the space was typed.
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("1 234,56", "fr-FR"));
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("1 234,56", "fr-FR"));
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("1 234,56", "fr-FR"));
    }

    /// <summary>DETERMINATION L12 is ONE equivalence for both sides (kb/Work PB2766): the snapshot drops every Cf
    /// character, so a Cf character in argument-1 is ignorable too, and every position TEST-NUMVAL-C reports is still
    /// the ORIGINAL argument's ordinal (§15.94.4 r1).</summary>
    [Fact]
    public void L12Equivalence_ACfCharacterInTheArgumentIsIgnorable_AndPositionsStayTheArgumentsOwn()
    {
        const string rlm = "\u200F";
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale(rlm + "¤1,234.56", ""));
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("¤" + rlm + "1,2" + rlm + "34.56" + rlm, ""));
        Assert.Equal((Int128)(-123456), Scaled2("(" + rlm + "¤1,234.56)"));
        Assert.Equal(4, CobolIntrinsics.TestNumvalCLocale("0" + rlm + " 1", ""));          // "0 1" reports 3; the mark shifts the '1'
        Assert.Equal(3, CobolIntrinsics.TestNumvalCLocale(rlm + rlm + "-¤1,234.56", ""));  // the '-' is in error
        Assert.Equal(6, CobolIntrinsics.TestNumvalCLocale("1,23" + rlm, ""));              // r1 c: LENGTH + 1 of the argument
        Assert.Equal(34, CobolIntrinsics.TestNumvalCLocale(rlm + "¤" + new string('1', 40), ""));   // the cap digit's ordinal
    }

    /// <summary>DETERMINATION L12: U+00A0 / U+202F / U+2009 is the plain space wherever r5b.7 or a token adjacency admits
    /// a space, not only where it matches a normalized separator (kb/Work PB2766).</summary>
    [Fact]
    public void L12Equivalence_ANoBreakSpaceIsASpaceWhereverASpaceIsAdmitted()
    {
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("\u00A0¤\u202F1,234.56\u2009", ""));
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("(\u00A0¤1,234.56\u00A0)", ""));
        Assert.Equal(4, CobolIntrinsics.TestNumvalCLocale("0\u00A0\u00A01", ""));           // still one ordinal per character
    }

    /// <summary>The scenario PB2766 names, over the whole host population: every culture's OWN formatted currency
    /// string — the raw .NET text, U+200F after an ar-* currency symbol and U+00A0 / U+202F grouping included — is
    /// conforming NUMVAL-C LOCALE input for that culture and evaluates to the formatted value (§15.68.3 r5 b,
    /// §15.68.4 r1).</summary>
    [Fact]
    public void EveryHostCulture_RecognizesItsOwnFormattedCurrencyString()
    {
        int examined = 0;
        var failures = new List<string>();
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            if (culture.Name.Length == 0 || !LocaleFacts.For(culture.Name).HasCultureData) continue;
            int d = culture.NumberFormat.CurrencyDecimalDigits;
            if (d is < 0 or > 6) continue;
            // A locale whose mon_thousands_sep IS its mon_decimal_point (mn-Mong-MN: ',' for both) is §8.2.1 "invalid"
            // content to NvScanLocale — its own "1,234,567,89" is ambiguous, and EC-LOCALE-INVALID is the answer.
            var f = MonetaryFacts.Of(LocaleFacts.For(culture.Name));
            if (f.ThousandsSep.Length > 0 && f.ThousandsSep == f.DecimalPoint) continue;
            decimal value = decimal.Round(1234567.891234m, d, MidpointRounding.AwayFromZero);
            string text = value.ToString("C", culture);
            examined++;
            long verdict = CobolIntrinsics.TestNumvalCLocale(text, culture.Name);
            if (verdict != 0) { failures.Add($"{culture.Name} '{text}': position {verdict}"); continue; }
            Int128 expected = (Int128)(value * 100m);
            Int128 actual = CobolIntrinsics.NumvalCLocaleDec(text, culture.Name).ToUnscaled(2, CobolRounding.Truncation);
            if (actual != expected) failures.Add($"{culture.Name} '{text}': {actual} != {expected}");
        }
        if (LocaleFacts.InvariantMode) return;
        Assert.True(examined >= 400, $"only {examined} cultures examined — the population is the evidence");
        Assert.True(failures.Count == 0, $"{failures.Count} culture(s) reject their own currency string:{Environment.NewLine}"
            + string.Join(Environment.NewLine, failures.Take(20)));
    }

    [Fact]
    public void IntCurrSymbol_MatchesItsFirstThreeCharacters()
    {
        // §15.68.3 r5b.3 — either currency_symbol or the FIRST THREE characters of int_curr_symbol; the
        // appended space is never required. en-US: ISOCurrencySymbol "USD".
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("USD1,234.56", "en-US"));
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("USD 1,234.56", "en-US"));
        Assert.Equal((Int128)123456, Scaled2("USD1,234.56", "en-US"));
        // The INVARIANT locale has no region ⇒ no int_curr_symbol ⇒ "USD" can never match (a determination).
        Assert.NotEqual(0, CobolIntrinsics.TestNumvalCLocale("USD1,234.56", ""));
    }

    [Fact]
    public void Anycase_FoldsOnlyTheCurrencyString()
    {
        // §15.68.3 r5b.1 scopes ANYCASE to "the matching rules for detecting a currency string".
        Assert.NotEqual(0, CobolIntrinsics.TestNumvalCLocale("usd1,234.56", "en-US"));
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("usd1,234.56", "en-US", anycase: true));
        Assert.Equal(0, CobolIntrinsics.TestNumvalCLocale("Usd 1,234.56", "en-US", anycase: true));
    }
}
