// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;
using CobolNet.Runtime;
using CobolNet.Runtime.Globalization;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The LC_MONETARY model (kb/Work PB64 T6; DESIGN-locale-facility §8, test plan T-C 3): the runtime-DERIVED
/// pattern→convention tables, the mon_grouping conversion, the L12 normalization, and the drift oracle that
/// asserts, for EVERY specific culture the host exposes, that WiseOwl COBOL's format-2 edit produces the same
/// PLACEMENT SHAPE as <c>value.ToString("C", culture)</c> — the "make the next case automatic" guarantee: a
/// future ICU release that adds a currency pattern fails here instead of silently mis-editing.
/// </summary>
public sealed class MonetaryFactsTests
{
    // ── The derived tables (MonetaryPlacement) ─────────────────────────────────────────────────────────────────

    [Fact]
    public void DerivedRange_EqualsTheRuntimesAcceptedRange_NeverTheDocumentedOne()
    {
        // The documented CurrencyNegativePattern range (0..15) is FALSE on this runtime — it accepts 16 (the
        // culture luy-KE uses it). The table must cover exactly what the runtime accepts: re-discover the
        // accepted maximum independently and assert the derived table's length matches. This is the assertion
        // that catches a future pattern 17.
        var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        int maxNeg = -1, maxPos = -1;
        for (int v = 0; ; v++) { try { nfi.CurrencyNegativePattern = v; maxNeg = v; } catch (ArgumentOutOfRangeException) { break; } }
        for (int v = 0; ; v++) { try { nfi.CurrencyPositivePattern = v; maxPos = v; } catch (ArgumentOutOfRangeException) { break; } }
        Assert.Equal(maxNeg + 1, MonetaryPlacement.NegativeByPattern.Length);
        Assert.Equal(maxPos + 1, MonetaryPlacement.PositiveByPattern.Length);
        Assert.True(MonetaryPlacement.NegativeByPattern.Length >= 16, "the runtime lost negative patterns it had");
    }

    [Fact]
    public void CanonicalTriples_ArePinned()
    {
        // The canonical search order (sign_posn asc, sep_by_space asc, cs_precedes true-first) makes the
        // aliased layouts resolve deterministically; these rows pin it. A different order silently changes
        // which convention recognition prefers.
        Assert.Equal(new MonetaryConvention(true, 0, 0), MonetaryPlacement.NegativeByPattern[0]);    // ($n)
        Assert.Equal(new MonetaryConvention(true, 0, 1), MonetaryPlacement.NegativeByPattern[1]);    // -$n
        Assert.Equal(new MonetaryConvention(true, 2, 4), MonetaryPlacement.NegativeByPattern[12]);   // $ -n
        Assert.Equal(new MonetaryConvention(false, 2, 3), MonetaryPlacement.NegativeByPattern[13]);  // n- $
        if (MonetaryPlacement.NegativeByPattern.Length > 16)
            Assert.Equal(new MonetaryConvention(true, 1, 4), MonetaryPlacement.NegativeByPattern[16]);   // $- n
        Assert.All(MonetaryPlacement.PositiveByPattern, c => Assert.Equal(1, c.SignPosn));   // the determination
    }

    [Fact]
    public void Render_SepBySpace1And2_AreDistinct()
    {
        // The single distinction separating .NET pattern 16 ($- n) from pattern 12 ($ -n): with sign_posn 4 the
        // sign glues to the currency string, and sep 1 spaces the UNIT from the value while sep 2 spaces the
        // JUNCTION. A model without the sep axis cannot express both.
        Assert.Equal("QN 1", MonetaryPlacement.Render(new(true, 1, 4), "Q", "N", "1"));
        Assert.Equal("Q N1", MonetaryPlacement.Render(new(true, 2, 4), "Q", "N", "1"));
        // sign_posn 0: the parentheses ARE the sign; the sign string is unused.
        Assert.Equal("(Q1)", MonetaryPlacement.Render(new(true, 0, 0), "Q", "NEVER", "1"));
        // An ABSENT sign suppresses the sep-2 junction space (no sign string, no space).
        Assert.Equal("Q1", MonetaryPlacement.Render(new(true, 2, 1), "Q", "", "1"));
    }

    [Fact]
    public void EveryDerivedConvention_RoundTripsItsPattern()
    {
        // The derivation's own proof obligation, re-run as a test: each accepted pattern's probe rendering is
        // reproduced by its derived convention. (The static initializer THROWS if not — this makes the
        // invariant visible and keeps it covered when that code changes.)
        var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        nfi.CurrencySymbol = "Q";
        nfi.NegativeSign = "N";
        nfi.CurrencyDecimalDigits = 0;
        nfi.CurrencyGroupSizes = [0];
        for (int v = 0; v < MonetaryPlacement.NegativeByPattern.Length; v++)
        {
            nfi.CurrencyNegativePattern = v;
            Assert.Equal((-1m).ToString("C", nfi),
                MonetaryPlacement.Render(MonetaryPlacement.NegativeByPattern[v], "Q", "N", "1"));
        }
        for (int v = 0; v < MonetaryPlacement.PositiveByPattern.Length; v++)
        {
            nfi.CurrencyPositivePattern = v;
            Assert.Equal((1m).ToString("C", nfi),
                MonetaryPlacement.Render(MonetaryPlacement.PositiveByPattern[v], "Q", "", "1"));
        }
    }

    // ── The snapshot (MonetaryFacts) ───────────────────────────────────────────────────────────────────────────

    [Fact]
    public void InvariantSnapshot_IsStableByDefinition()
    {
        var f = MonetaryFacts.Of(LocaleFacts.For(""));
        Assert.Equal("¤", f.CurrencySymbol);
        Assert.Null(f.IntCurrencySymbol);            // no region for the invariant culture — a determination
        Assert.Equal(".", f.DecimalPoint);
        Assert.Equal(",", f.ThousandsSep);
        Assert.Equal(new[] { 3 }, f.GroupSizes);
        Assert.False(f.GroupStops);
        Assert.Equal(2, f.FracDigits);
        Assert.Equal(new MonetaryConvention(true, 0, 0), f.Negative);    // ($n) — parentheses, sign unused
        Assert.Equal(1, f.Positive.SignPosn);
    }

    [Fact]
    public void L12Normalization_StripsCfAndMapsSpacingVariants()
    {
        // DETERMINATION L12 — the ONE normalization of locale-sourced text: Cf removed, the three CLDR spacing
        // variants become the plain space, U+2212 kept (a real character, not a spacing artifact).
        Assert.Equal("a b c d", LocaleFacts.NormalizeLocaleText("a\u00A0b\u202Fc\u2009d"));
        Assert.Equal("$-", LocaleFacts.NormalizeLocaleText("$\u200F-\u200E"));          // Cf marks stripped
        Assert.Equal("\u2212", LocaleFacts.NormalizeLocaleText("\u2212"));               // U+2212 survives
        Assert.Equal("\u061C-".Length - 1, LocaleFacts.NormalizeLocaleText("\u061C-").Length);   // ALM stripped
        Assert.Same("plain", LocaleFacts.NormalizeLocaleText("plain"));                 // clean text: no copy
    }

    [Fact]
    public void GroupingConversion_ThreeShapes()
    {
        // .NET semantics measured: {3} repeats; {3,0} = 3 then STOP; {0} = no grouping. Exercised through the
        // edit itself so the conversion and its consumer are tested together — synthesized via the derived
        // GroupBoundaries path with real cultures where they exist, and pinned structurally here.
        var inv = MonetaryFacts.Of(LocaleFacts.For(""));
        Assert.False(inv.GroupStops);
        // hi-IN carries {3,2} — the Indian grouping. (Structure only: the edit-level pin lives in the drift
        // oracle below, which covers every host culture including this one.)
        var hi = MonetaryFacts.Of(LocaleFacts.For("hi-IN"));
        if (hi.GroupSizes.Length >= 2) Assert.Equal(3, hi.GroupSizes[0]);
    }

    [Fact]
    public void GroupingAdmits_PinnedShapes_ThreeThenTwoAndThrees()
    {
        // kb/Work PB835 — ISO §15.68.3 r5b.6 "in accordance with locale fields mon_thousands_sep and mon_grouping".
        var us = MonetaryFacts.Of(LocaleFacts.For("en-US"));                // {3} repeating
        Assert.True(us.GroupingAdmits([1, 3, 3], complete: true));          // 1,234,567
        Assert.True(us.GroupingAdmits([7], complete: true));                // no separator — r5b.6 "may"
        Assert.False(us.GroupingAdmits([2, 2], complete: true));            // 12,34 — short last group
        Assert.True(us.GroupingAdmits([2, 2], complete: false));            // … but "12,345" completes it
        Assert.False(us.GroupingAdmits([4, 0], complete: false));           // 1234, — no completion exists
        Assert.False(us.GroupingAdmits([1, 4], complete: false));           // 1,2345 — overfull group
        Assert.False(us.GroupingIsExact([7]));                              // an edit always writes them
        Assert.True(us.GroupingIsExact([3]));
        var inv = MonetaryFacts.Of(LocaleFacts.For(""));
        Assert.Equal(3, inv.GroupSize(0));
        Assert.Equal(3, inv.GroupSize(9));                                  // the last size repeats
    }

    [Fact]
    public void EveryHostCulture_GroupingWalk_AgreesWithDotNetsOwnGroupSizeRendering()
    {
        // The drift oracle for the ONE mon_grouping reading (kb/Work PB835): .NET's "N0" formatter applied with the
        // culture's CurrencyGroupSizes is an INDEPENDENT implementation of the same size-list semantics (repeat
        // the last size; a trailing 0 stops). For every culture and every integer width 1..16, the runs it
        // writes must be exactly grouped (the de-edit's test), admitted whole and at every prefix (the NUMVAL-C
        // LOCALE scan's tests), and one separator moved one digit right must be refused.
        int examined = 0;
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            var f = MonetaryFacts.Of(LocaleFacts.For(culture.Name));
            if (f.ThousandsSep.Length == 0) continue;
            var nfi = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            nfi.NumberGroupSizes = culture.NumberFormat.CurrencyGroupSizes;
            nfi.NumberGroupSeparator = "|";
            for (int width = 1; width <= 16; width++)
            {
                long v = long.Parse("1234567890123456"[..width], CultureInfo.InvariantCulture);
                string text = v.ToString("N0", nfi);
                int[] groups = Array.ConvertAll(text.Split('|'), static s => s.Length);
                Assert.True(f.GroupingIsExact(groups), $"{culture.Name}: {text} not exact");
                Assert.True(f.GroupingAdmits(groups, complete: true), $"{culture.Name}: {text} not admitted");
                for (int len = 1; len <= groups[^1]; len++)
                {
                    int[] prefix = [.. groups];
                    prefix[^1] = len;
                    Assert.True(f.GroupingAdmits(prefix, complete: false), $"{culture.Name}: prefix of {text}");
                }
                if (groups.Length >= 2 && groups[^1] > 1)
                {
                    int[] shifted = [.. groups];
                    shifted[^2]++; shifted[^1]--;
                    Assert.False(f.GroupingAdmits(shifted, complete: true), $"{culture.Name}: shifted {text}");
                }
            }
            examined++;
        }
        Assert.True(examined > 100, $"only {examined} cultures examined — the host lost its culture data");
    }

    // ── The drift oracle (T-C 3) — every host culture vs ToString("C") ─────────────────────────────────────────

    [Fact]
    public void EveryHostCulture_EditsWithTheSamePlacementShape_AsToStringC()
    {
        int examined = 0;
        var patternsSeen = new HashSet<(int, int)>();
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
        {
            if (culture.Name.Length == 0) continue;
            var lf = LocaleFacts.For(culture.Name);
            if (!lf.HasCultureData) continue;
            var f = MonetaryFacts.Of(lf);
            int d = culture.NumberFormat.CurrencyDecimalDigits;
            if (d is < 0 or > 6) continue;
            // Build the picture FROM the culture so the two digit strings are identical and only PLACEMENT can
            // differ. ⚠ TEST CONSTRUCTION, not a rule: §13.18.40.5 r12 hands the locale the separators and
            // group sizes, never the fraction width — the width here is chosen to match ToString's.
            string frac = "891234"[..d];
            string picPos = "$" + new string('9', 7) + (d > 0 ? "." + new string('9', d) : "");
            string picNeg = "+" + picPos;
            Int128 unscaled = Int128.Parse("1234567" + frac, CultureInfo.InvariantCulture);
            decimal value = decimal.Parse("1234567" + (d > 0 ? "." + frac : ""), CultureInfo.InvariantCulture);

            string oursPos = CobolLocaleEdit.Format(unscaled, d, picPos, culture.Name, 80).TrimStart(' ');
            string oursNeg = CobolLocaleEdit.Format(-unscaled, d, picNeg, culture.Name, 80).TrimStart(' ');
            string oraclePos = LocaleFacts.NormalizeLocaleText(value.ToString("C", culture)).TrimStart(' ');
            string oracleNeg = LocaleFacts.NormalizeLocaleText((-value).ToString("C", culture)).TrimStart(' ');

            Assert.True(Sigma(oursPos, f) == Sigma(oraclePos, f),
                $"{culture.Name} positive: ours '{oursPos}' vs .NET '{oraclePos}'");
            Assert.True(Sigma(oursNeg, f) == Sigma(oracleNeg, f),
                $"{culture.Name} negative: ours '{oursNeg}' vs .NET '{oracleNeg}'");
            examined++;
            patternsSeen.Add((culture.NumberFormat.CurrencyPositivePattern, culture.NumberFormat.CurrencyNegativePattern));
        }
        // A filtered or empty run must FAIL, not pass — the population is the evidence.
        Assert.True(examined >= 400, $"only {examined} cultures examined — the oracle did not run over the host's population");
        Assert.True(patternsSeen.Count >= 5, $"only {patternsSeen.Count} pattern pairs exercised");
    }

    [Fact]
    public void TheDriftComparison_CanFail()
    {
        // The positive control (a gate must fail once before its silence is evidence): two cultures whose
        // shapes genuinely differ must produce different sigmas.
        var en = MonetaryFacts.Of(LocaleFacts.For("en-US"));
        string a = Sigma(CobolLocaleEdit.Format(123456, 2, "$9999.99", "en-US", 40).TrimStart(' '), en);
        var fr = MonetaryFacts.Of(LocaleFacts.For("fr-FR"));
        string b = Sigma(CobolLocaleEdit.Format(123456, 2, "$9999.99", "fr-FR", 40).TrimStart(' '), fr);
        Assert.NotEqual(a, b);
    }

    /// <summary>The placement-shape reduction: tokenize with anchored longest-match — the currency string first
    /// (it may contain the locale's own separators), then the sign, parens, separators, digits, the space — and
    /// FAIL on anything unclassified (an unknown character means the field model missed something).</summary>
    private static string Sigma(string s, MonetaryFacts f)
    {
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length;)
        {
            if (f.CurrencySymbol.Length > 0 && Starts(s, i, f.CurrencySymbol)) { sb.Append('¤'); i += f.CurrencySymbol.Length; continue; }
            if (f.NegativeSign.Length > 0 && Starts(s, i, f.NegativeSign)) { sb.Append('±'); i += f.NegativeSign.Length; continue; }
            if (f.PositiveSign.Length > 0 && Starts(s, i, f.PositiveSign)) { sb.Append('±'); i += f.PositiveSign.Length; continue; }
            char c = s[i];
            if (c is '(' or ')') { sb.Append(c); i++; continue; }
            if (f.ThousandsSep.Length > 0 && Starts(s, i, f.ThousandsSep)) { sb.Append(','); i += f.ThousandsSep.Length; continue; }
            if (f.DecimalPoint.Length > 0 && Starts(s, i, f.DecimalPoint)) { sb.Append('.'); i += f.DecimalPoint.Length; continue; }
            if (char.IsAsciiDigit(c)) { sb.Append('9'); i++; continue; }
            if (c == ' ') { sb.Append('_'); i++; continue; }
            Assert.Fail($"unclassified character U+{(int)c:X4} in '{s}' — the LC_MONETARY field model missed something");
            return "";
        }
        return sb.ToString();

        static bool Starts(string s, int i, string tok) =>
            i + tok.Length <= s.Length && string.CompareOrdinal(s, i, tok, 0, tok.Length) == 0;
    }
}
