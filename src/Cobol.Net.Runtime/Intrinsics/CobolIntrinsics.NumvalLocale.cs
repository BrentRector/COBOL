// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Runtime.InteropServices;
using CobolNet.Runtime.Globalization;

namespace CobolNet.Runtime;

/// <summary>
/// NUMVAL-C / TEST-NUMVAL-C — the LOCALE arm (ISO/IEC 1989:2023 §15.68.3 rule 5, imported whole into
/// TEST-NUMVAL-C by §15.94.3 r1; DESIGN-locale-facility §4.6; kb/Work PB64 T6). ⛔ §15.68.3 rule 4 opens "If the
/// LOCALE keyword is not specified", so NOTHING of the r4 arm is inherited here: the two fixed formats, CR/DB as
/// sign indicators, the period/comma roles and their DECIMAL-POINT IS COMMA swap, and the invariant ANYCASE fold
/// are all r4-only — the admissible language is generated from the locale's LC_MONETARY snapshot
/// (<see cref="MonetaryFacts"/>, the SAME model PICTURE format-2 editing renders from) and the ONE scan serves
/// both functions, the TEST- twin a pure projection, so they can never disagree about what conforms.
/// <para>The admissible token orders come from the locale's two placement conventions
/// (<see cref="MonetaryPlacement"/>): each convention's layout with the currency string present (either
/// <c>currency_symbol</c> or the first three characters of <c>int_curr_symbol</c> — r5b.3) or absent (r5b.3 "may
/// contain"), the sign present or absent (r5b.4 "may contain" — absent ⇒ nonnegative, §15.68.4 r3), spaces
/// admitted at every token adjacency (a superset of the three <c>sep_by_space</c> values — the field ISO 1989
/// never names; §8.2.1's ISO 9945 import) plus leading/trailing (r5b.7). The only OBLIGATION is one digit
/// (r5b.7). Grouping separators stand between digits, never right of <c>mon_decimal_point</c> (§8.2.2 /
/// §15.68.4 r2), and where <c>mon_grouping</c> puts them (r5b.6 names the field — kb/Work PB835): none at all, or
/// every one (⚖ DETERMINATION, <see cref="MonetaryFacts.GroupingAdmits"/>). ⚖ Documented determinations: the
/// fraction-digit count is not constrained by <c>frac_digits</c> — §15.68.1 names "the grouping separator and
/// the decimal separator permitted" and r5b.5 is a permission;
/// argument-1 is compared with the snapshot under the ONE L12 equivalence (<see cref="LocaleFacts.TryFoldLocaleChar"/>:
/// a Cf character is ignorable, U+00A0/U+202F/U+2009 is a space wherever a space is admitted or matched — a typed
/// U+00A0 matches fr-FR's normalized separator, the U+200F an ar-* culture's own formatted string carries after its
/// currency symbol matches the symbol the snapshot stripped it from), positions reported in the original argument;
/// the negative convention is tried before the positive one (an empty positive sign matches only by
/// absence); when both currency candidates match, the longer wins.</para>
/// </summary>
public static partial class CobolIntrinsics
{
    /// <summary>NUMVAL-C LOCALE's §15.68.4 r1 value as an SDIDI, exact at the parsed scale (the same lift as
    /// <see cref="NumvalCDec"/>) — the projection EVERY arithmetic mode uses (kb/Work PB251); the omitted
    /// <paramref name="digitCap"/> is the family's one convention, the NATIVE §15.67.3 r3 cap.</summary>
    public static CobolDec NumvalCLocaleDec(string text, string? localeTag, bool anycase = false, int digitCap = 31)
    {
        NvParse p = NvScanLocale(text, localeTag, anycase, digitCap, "NUMVAL-C");
        if (p.ErrPos != 0) return CobolDec.From(NumvalCLocaleReject(p, text, digitCap), 0);
        return CobolDec.From(p.Neg ? -p.Unscaled : p.Unscaled, p.Frac);
    }

    /// <summary>TEST-NUMVAL-C with the LOCALE keyword (§15.94.4 over the §15.68.3 r5 language): 0, the first
    /// error position (r1 b — under a template ALTERNATION the reported position is 1 + the longest prefix any
    /// admissible template consumes, which realizes r1 b.1's "first non-space character following the spaces"
    /// example verbatim), or LENGTH+1 (r1 c — every character admissible but the scan ran off the end).
    /// Never raises EC-ARGUMENT-FUNCTION for content (reporting it is the function's purpose); the locale
    /// conditions still gate.</summary>
    public static long TestNumvalCLocale(string text, string? localeTag, bool anycase = false, int digitCap = 31)
        => NvScanLocale(text, localeTag, anycase, digitCap, "TEST-NUMVAL-C").ErrPos;

    private static Int128 NumvalCLocaleReject(NvParse p, string text, int digitCap) =>
        p.CapHit
            ? DigitCapExceeded("NUMVAL-C", digitCap + 1, digitCap, "ISO §15.68.3 rules 6-7")
            : Exceptions.ExceptionState.ArgumentError(
                $"NUMVAL-C argument-1 \"{text}\" is not in a format consistent with locale category LC_MONETARY "
                + $"in the locale in use (ISO §15.68.3 rule 5 b; first character in error at position {p.ErrPos}; "
                + "§15.94 TEST-NUMVAL-C reports the same position)");

    /// <summary>One template's token kinds — the layout order comes from the convention, spaces are admitted at
    /// every adjacency.</summary>
    private enum LocTok { Open, Close, Cs, Sign, Num }

    private readonly record struct LocTemplate(LocTok[] Tokens, bool Negative, string Sign, string? Cs);

    /// <summary>The ONE §15.68.3 r5 scan — validates positionally AND accumulates the value in the same pass over
    /// the argument folded by DETERMINATION L12 (<see cref="LocaleFacts.NormalizeLocaleText(string, out int[])"/>,
    /// the snapshot's own equivalence; every position is mapped back to its ordinal in the ORIGINAL argument). The
    /// ANYCASE fold happens only at the currency-comparison site, never by rewriting the string — §15.57.4 r5's
    /// non-1:1 warning. Template set: the negative
    /// convention first when it is distinguishable (parentheses or a nonempty negative_sign), then the positive,
    /// then the unsigned reduction; each × currency-present (both r5b.3 candidates, longer preferred) /
    /// currency-absent.</summary>
    private static NvParse NvScanLocale(string text, string? localeTag, bool anycase, int digitCap, string fn)
    {
        string tag = localeTag ?? RunUnit.Current.Locale.Current(LocaleCategory.Monetary);
        var lf = LocaleFacts.For(tag).Require(LocaleCategory.Monetary,
            $"FUNCTION {fn}{(localeTag is not null ? " (LOCALE locale-name-1)" : " (LOCALE)")}",
            "ISO §15.68.3 r5a") ?? LocaleFacts.Root;
        if (anycase)
            // r5b.3 ¶2 — the fold is "as specified in the rules for the LOWER-CASE function with LOCALE
            // locale-name-1 specified": LC_CTYPE of the SAME locale, so the §8.2.1 gate covers it too.
            lf.Require(LocaleCategory.Ctype, $"FUNCTION {fn} ANYCASE", "ISO §15.68.3 r5b.3 / §15.57.4 r2");
        var f = MonetaryFacts.Of(lf);
        var ti = anycase && lf.HasCultureData ? lf.TextInfo : null;   // ANYCASE with no culture data folds invariant

        // Degenerate locale content makes the model ambiguous — §8.2.1 "invalid or incomplete".
        if (f.ThousandsSep.Length > 0 && f.ThousandsSep == f.DecimalPoint)
            Exceptions.ExceptionState.LocaleInvalidError(
                $"FUNCTION {fn}: the locale '{lf.Collate}' defines mon_thousands_sep and mon_decimal_point as "
                + "the same string, which makes the monetary format ambiguous (ISO §8.2.1)");

        var templates = new List<LocTemplate>();
        void Add(MonetaryConvention c, bool negative, string sign)
        {
            LocTok[] Order()
            {
                if (c.SignPosn == 0)
                    return c.CsPrecedes
                        ? [LocTok.Open, LocTok.Cs, LocTok.Num, LocTok.Close]
                        : [LocTok.Open, LocTok.Num, LocTok.Cs, LocTok.Close];
                return (c.SignPosn, c.CsPrecedes) switch
                {
                    (1, true) or (3, true) => [LocTok.Sign, LocTok.Cs, LocTok.Num],
                    (1, false) => [LocTok.Sign, LocTok.Num, LocTok.Cs],
                    (3, false) => [LocTok.Num, LocTok.Sign, LocTok.Cs],
                    (4, true) => [LocTok.Cs, LocTok.Sign, LocTok.Num],
                    (2, true) => [LocTok.Cs, LocTok.Num, LocTok.Sign],
                    _ => [LocTok.Num, LocTok.Cs, LocTok.Sign],        // posn 2/4, cs succeeding
                };
            }
            var order = Order();
            // Currency present — both r5b.3 candidates, LONGER first — then the r5b.3 "may contain" reduction.
            string? int3 = f.IntCurrencySymbol is { Length: >= 3 } ics ? ics[..3] : null;
            var candidates = new List<string>();
            if (f.CurrencySymbol.Length > 0) candidates.Add(f.CurrencySymbol);
            if (int3 is not null && int3 != f.CurrencySymbol) candidates.Add(int3);
            candidates.Sort(static (a, b) => b.Length - a.Length);
            foreach (string cs in candidates) templates.Add(new(order, negative, sign, cs));
            templates.Add(new(Array.FindAll(order, static k => k != LocTok.Cs), negative, sign, null));
        }
        bool negDistinct = f.Negative.SignPosn == 0 || f.NegativeSign.Length > 0;
        if (negDistinct) Add(f.Negative, negative: true, f.Negative.SignPosn == 0 ? "" : f.NegativeSign);
        if (f.PositiveSign.Length > 0 && f.Positive.SignPosn != 0)
            Add(f.Positive, negative: false, f.PositiveSign);
        // The unsigned reduction (r5b.4 "may contain" — enumerated once, nonnegative per §15.68.4 r3):
        // the nonnegative layout with no sign token.
        LocTok[] unsignedOrder = f.Positive.CsPrecedes
            ? [LocTok.Cs, LocTok.Num]
            : [LocTok.Num, LocTok.Cs];
        if (f.CurrencySymbol.Length > 0) templates.Add(new(unsignedOrder, false, "", f.CurrencySymbol));
        if (f.IntCurrencySymbol is { Length: >= 3 } i3 && i3[..3] != f.CurrencySymbol)
            templates.Add(new(unsignedOrder, false, "", i3[..3]));
        templates.Add(new([LocTok.Num], false, "", null));

        // DETERMINATION L12: the snapshot's strings are folded (MonetaryFacts), so argument-1 is folded by the SAME
        // per-character equivalence before it is scanned — "a format consistent with the specifications of locale
        // category LC_MONETARY" (r5 b) is judged on one side of one equivalence. A removed Cf character shifts what
        // follows it, so every position the scan finds is mapped back to its ordinal in the ORIGINAL argument.
        string scan = LocaleFacts.NormalizeLocaleText(text, out int[]? origins);
        int n = scan.Length;
        int bestConsumed = -1;
        bool bestCapHit = false;
        long bestCapPos = 0;
        foreach (var t in templates)
        {
            var a = TryLocTemplate(scan, t, f, ti, anycase, digitCap);
            if (a.Accepted) return new(0, false, t.Negative, a.Unscaled, a.Frac);
            long capPos = a.CapHit ? Ordinal((int)a.CapPos - 1) : 0;
            if (a.CapHit && (!bestCapHit || capPos < bestCapPos)) { bestCapHit = true; bestCapPos = capPos; }
            if (a.Consumed > bestConsumed) bestConsumed = a.Consumed;
        }
        long structuralPos = Ordinal(bestConsumed);                                     // r1 b / r1 c
        if (bestCapHit && bestCapPos <= structuralPos)
            return new(bestCapPos, true, false, 0, 0);                // r1 b.2/3/4 — the (cap+1)-th digit,
                                                                      //  "if no prior error is found"
        return new(structuralPos, false, false, 0, 0);

        // The 1-based position in the ORIGINAL argument of the folded text's character k, or LENGTH + 1 when the
        // scan ran off the end (r1 c — a trailing removed Cf character is admissible, so it does not move that).
        long Ordinal(int k) => k >= n ? text.Length + 1 : (origins is null ? k : origins[k]) + 1;
    }

    private readonly record struct LocAttempt(bool Accepted, int Consumed, bool CapHit, long CapPos,
        Int128 Unscaled, int Frac);

    private static LocAttempt TryLocTemplate(string text, in LocTemplate t, MonetaryFacts f,
        System.Globalization.TextInfo? fold, bool anycase, int digitCap)
    {
        int n = text.Length, i = 0;
        Int128 unscaled = 0;
        int frac = -1, digits = 0;
        bool anyDigit = false;
        void Sp() { while (i < n && text[i] == ' ') i++; }

        Sp();
        foreach (var tok in t.Tokens)
        {
            switch (tok)
            {
                case LocTok.Open:
                    if (i >= n || text[i] != '(') return Fail();
                    i++;
                    break;
                case LocTok.Close:
                    if (i >= n || text[i] != ')') return Fail();
                    i++;
                    break;
                case LocTok.Sign:
                    if (!MatchAt(text, i, t.Sign, fold: null)) return Fail();   // sign matching: never folded (r5b.1)
                    i += t.Sign.Length;
                    break;
                case LocTok.Cs:
                    if (t.Cs is not { } cs || !MatchAt(text, i, cs, fold, anycase)) return Fail();
                    i += cs.Length;
                    break;
                case LocTok.Num:
                {
                    // NUM ::= digits [sep digits]… [dec [digits]] | dec digits — a separator is digit-flanked, left
                    // of the decimal (§15.68.4 r2), AND stands where mon_grouping puts one (r5b.6 "in accordance
                    // with locale fields mon_thousands_sep and mon_grouping" — kb/Work PB835): the integer runs
                    // are checked as a PREFIX at every character, so the first character no grouped completion
                    // admits is the §15.94.4 r1 b) position, and as a WHOLE where the integer part ends (a valid
                    // but incomplete grouping ends at the next character, or at LENGTH + 1 — r1 c).
                    // ⛔ no commaMode, no CR/DB (§15.68.3 r4 is not inherited).
                    int start = i;
                    bool sawDec = false;
                    List<int>? groups = null;                              // integer runs, once a separator appears
                    while (i < n)
                    {
                        char c = text[i];
                        if (char.IsAsciiDigit(c))
                        {
                            anyDigit = true;
                            if (++digits > digitCap)
                                return new(false, i, true, i + 1, 0, 0);   // the (cap+1)-th digit's ordinal
                            if (groups is not null && !sawDec)
                            {
                                groups[^1]++;
                                if (!f.GroupingAdmits(CollectionsMarshal.AsSpan(groups), complete: false))
                                    return Fail();                         // this digit overfills its group
                            }
                            unscaled = unscaled * 10 + (c - '0');
                            if (sawDec) frac++;
                            i++;
                            continue;
                        }
                        if (!sawDec && anyDigit && f.ThousandsSep.Length > 0
                            && MatchAt(text, i, f.ThousandsSep, fold: null)
                            && i + f.ThousandsSep.Length < n && char.IsAsciiDigit(text[i + f.ThousandsSep.Length]))
                        {
                            (groups ??= [digits]).Add(0);                  // digits so far = the leftmost run
                            if (!f.GroupingAdmits(CollectionsMarshal.AsSpan(groups), complete: false))
                                return Fail();                             // no group boundary can fall here
                            i += f.ThousandsSep.Length;
                            continue;
                        }
                        if (!sawDec && f.DecimalPoint.Length > 0 && MatchAt(text, i, f.DecimalPoint, fold: null))
                        {
                            if (!GroupedWhole()) return Fail();            // the integer part ends short
                            sawDec = true;
                            frac = 0;
                            i += f.DecimalPoint.Length;
                            continue;
                        }
                        break;
                    }
                    if (i == start) return Fail();                         // the slot consumed nothing
                    if (!sawDec && !GroupedWhole()) return Fail();
                    break;

                    bool GroupedWhole() =>
                        groups is null || f.GroupingAdmits(CollectionsMarshal.AsSpan(groups), complete: true);
                }
            }
            Sp();
        }
        if (i != n || !anyDigit) return Fail();                            // r5b.7 — at least one digit
        return new(true, n, false, 0, unscaled, frac < 0 ? 0 : frac);

        LocAttempt Fail() => new(false, i, false, 0, 0, 0);
    }

    /// <summary>Compare <paramref name="candidate"/> against <paramref name="text"/> at <paramref name="at"/>, per
    /// character: both sides are already L12-folded (the snapshot by <see cref="MonetaryFacts"/>, the argument by
    /// <see cref="NvScanLocale"/>), so the comparison is ordinal unless <paramref name="anycase"/> asks for the
    /// ANYCASE currency comparison (§15.68.3 r5b.1/3) — the locale's simple LC_CTYPE lowercase correspondence
    /// through <paramref name="fold"/> (L9; length-preserving, so positions stay ordinal).</summary>
    private static bool MatchAt(string text, int at, string candidate, System.Globalization.TextInfo? fold,
        bool anycase = false)
    {
        if (candidate.Length == 0 || at + candidate.Length > text.Length) return false;
        for (int j = 0; j < candidate.Length; j++)
        {
            char a = text[at + j], b = candidate[j];
            if (anycase)
            {
                a = fold?.ToLower(a) ?? char.ToLowerInvariant(a);
                b = fold?.ToLower(b) ?? char.ToLowerInvariant(b);
            }
            if (a != b) return false;
        }
        return true;
    }
}
