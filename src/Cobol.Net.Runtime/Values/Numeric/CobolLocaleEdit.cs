// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.Globalization;

namespace CobolNet.Runtime;

/// <summary>
/// PICTURE format 2 — LOCALE editing and de-editing (ISO/IEC 1989:2023 §13.18.40.2 format 2, §13.18.40.4
/// GR16–GR18, §13.18.40.5 rules 9–15; DESIGN-locale-facility §4.6; kb/Work PB64 T6). Sits BESIDE
/// <see cref="CobolEdit"/>, deliberately not inside it: format 1's mask is a 1:1 character map and locale editing
/// has no fixed mask — the currency string, separators and sign are the LOCALE's, multi-character, and placed by
/// the locale's conventions (<see cref="MonetaryPlacement"/>), so the item's rendering is runtime data.
/// <para>The PICTURE contributes exactly TWO presence booleans and the digit shape (§13.18.40.4 GR18): a currency
/// symbol means "the item is to include a currency string in accordance with the specifications in the locale", a
/// '+' means "the item is to be signed in accordance with the specifications in the locale" (absent ⇒ the item is
/// UNSIGNED and the absolute value is edited — no exception, the standard provides none). Everything else —
/// position, length, characters of the currency string (r9), separators and group sizes (r12), the sign
/// convention (r13) — is LC_MONETARY's (<see cref="MonetaryFacts"/>). ⛔ Table 11's written order ('+' then cs
/// then digits) is a SYNTAX constraint on character-string-1, never an output ordering: a locale with
/// n_sign_posn 2 emits <c>$12.50-</c> and the invariant locale's n_sign_posn 0 emits <c>(¤12.50)</c>.</para>
/// <para>The locale is resolved AT EACH edit and de-edit (r11 named-else-current; §14.6.6 r6 "the locale current
/// at the time"), never cached on the item; <see cref="MonetaryFacts.Require"/> is the §8.2.1 gate
/// (EC-LOCALE-MISSING / EC-LOCALE-INVALID). The ONE EC-LOCALE-SIZE raise site in the compiler is
/// <see cref="Format"/>'s r14 b) branch.</para>
/// </summary>
public static class CobolLocaleEdit
{
    /// <summary>The compile-time shape of a format-2 picture (parsed from the canonical character-string —
    /// currency symbols canonicalized to <c>$</c>, symbols <c>+ $ Z 9 .</c> only, repeats expanded).</summary>
    private readonly record struct Shape(bool HasPlus, bool HasCs, bool HasDot, int DigitsLeft, int DigitsRight, int ZRun)
    {
        public int Digits => DigitsLeft + DigitsRight;
        /// <summary>r15 b) second sentence's trigger: every digit position is a 'Z' (no '9' anywhere).</summary>
        public bool AllZ => ZRun == Digits;
    }

    private static Shape Parse(string picture)
    {
        bool plus = false, cs = false, dot = false;
        int left = 0, right = 0, zrun = 0;
        bool zOpen = true;
        foreach (char c in picture)
        {
            switch (c)
            {
                case '+': plus = true; break;
                case '$': cs = true; break;
                case '.': dot = true; break;
                case 'Z' or 'z':
                    if (zOpen) zrun++;
                    if (dot) right++; else left++;
                    break;
                case '9':
                    zOpen = false;   // Table 11: no 9 precedes any Z, so the Z's are one leading run
                    if (dot) right++; else left++;
                    break;
            }
        }
        return new Shape(plus, cs, dot, left, right, zrun);
    }

    /// <summary>Edit a fixed-point value into a format-2 item — §13.18.40.5 rules 9–15: the §13.18.40.4 GR18 sign decision
    /// (no '+' ⇒ the absolute value, unsigned); r14's decimal-point alignment (zero fill or SILENT truncation on
    /// either end — EC-LOCALE-SIZE belongs only to the final move), which reads the PICTURE alone and so runs
    /// FIRST, because the two zero short-circuits below are both about the value BEING STORED and that is the
    /// ALIGNED value (§13.18.8.4 GR1, r15 b; kb/Work PB566); the r10 BLANK WHEN ZERO short-circuit (precedence
    /// over locale editing — still no locale consulted, so an unavailable locale still stores blanks for a stored
    /// zero); the §8.2.1 locale resolution (r11 named-else-current, at THIS moment); r15 b's all-Z zero; the
    /// hypothetical data item at FULL width (r9/r12/r13 — grouping, separators, currency string and sign per the
    /// locale, through the ONE <see cref="MonetaryPlacement.Render(MonetaryConvention, string, string, string, out int)"/>);
    /// r15 zero suppression (the all-Z-and-zero case blanks ALL <paramref name="size"/> positions — no separator,
    /// no currency string, no sign — and otherwise the window stops at the first nonzero digit, the first
    /// non-suppressible position, or the decimal point, a grouping separator inside the suppressed run becoming a
    /// space too); then r14 a)/b) — right-justify with space fill, or LEFT-truncate with the ONE EC-LOCALE-SIZE
    /// raise when a truncated character is neither a zero nor a space caused by a suppressed zero (CHARACTER-based:
    /// a truncated currency character, separator or sign raises; Table 13's "digits were truncated" gloss is
    /// narrower and is not what r14 b says). Never reads <c>frac_digits</c> — the fraction width is the
    /// picture's (r12 hands the locale only the separators and group sizes).</summary>
    /// <param name="unscaled">The sending value, unscaled.</param>
    /// <param name="valueScale">The sending value's scale (decimal digits carried in <paramref name="unscaled"/>).</param>
    /// <param name="picture">The canonical expanded format-2 character-string (<c>+ $ Z 9 .</c>).</param>
    /// <param name="localeTag">locale-name-1's L1-normalized tag, or null for the current LC_MONETARY locale.</param>
    /// <param name="size">integer-1 — the item's character positions (§13.18.40.4 GR17).</param>
    /// <param name="blankWhenZero">The BLANK WHEN ZERO clause (r10).</param>
    public static string Format(Int128 unscaled, int valueScale, string picture, string? localeTag, int size,
        bool blankWhenZero = false)
    {
        var p = Parse(picture);

        bool negative = p.HasPlus && unscaled < 0;                          // GR18 '+' — absent ⇒ unsigned, |v|
        UInt128 mag = CobolDec.UAbs(unscaled);

        // r14 sentence 1 — align on the decimal point position, zero fill or SILENT truncation on either end.
        // Done over the digit STRING so a wide rescale cannot overflow the carrier. ⛔ HOISTED ABOVE r10 and
        // r15 b), because both ask what is BEING STORED (§13.18.8.4 GR1 "the value being stored"; r15 b) "the
        // value of the data to be stored") and after r14's truncation that is not always the SENDING value — 0.4
        // into a format-2 item with no fraction digit positions stores zero (kb/Work PB566). The alignment reads
        // only the PICTURE, never the locale, so r10 below still decides before any locale is consulted.
        string digits = mag.ToString();
        if (digits.Length <= valueScale) digits = new string('0', valueScale - digits.Length + 1) + digits;
        string intPart = digits[..^valueScale] is { Length: > 0 } ip ? ip : "0";
        string fracPart = valueScale > 0 ? digits[^valueScale..] : "";
        string I = intPart.Length >= p.DigitsLeft
            ? intPart[^p.DigitsLeft..]                                      // silent high-order truncation
            : new string('0', p.DigitsLeft - intPart.Length) + intPart;     // zero fill
        if (p.DigitsLeft == 0) I = "";
        string F = fracPart.Length >= p.DigitsRight
            ? fracPart[..p.DigitsRight]                                     // silent low-order truncation
            : fracPart + new string('0', p.DigitsRight - fracPart.Length);
        // The value the item is left holding — what §13.18.8.4 GR1 and r15 b) both test (kb/Work PB566).
        bool storesZero = I.AsSpan().IndexOfAnyExcept('0') < 0 && F.AsSpan().IndexOfAnyExcept('0') < 0;

        if (blankWhenZero && storesZero) return new string(' ', size);      // r10 — before any locale consult

        var facts = MonetaryFacts.Require(localeTag, "PICTURE format 2 locale editing", "ISO §13.18.40.5 r9/r11");

        if (p.AllZ && storesZero) return new string(' ', size);             // r15 b) — "all character positions
                                                                            //  of the ITEM" are spaces

        // The hypothetical data item at FULL width, with per-position roles for r15/r14 b).
        // roleDigit[i] = the 1-based integer-digit index at position i (0 = not an integer digit);
        // roleSepAfter[i] = for a grouping separator, the integer-digit index to its LEFT (0 otherwise).
        var num = new System.Text.StringBuilder(I.Length * 2 + 1 + F.Length);
        var numDigit = new List<int>();                                     // parallel to num: digit index or 0
        var numSepAfter = new List<int>();                                  // parallel: left-digit index or 0
        int[] boundaries = GroupBoundaries(facts, p.DigitsLeft);            // digit indexes AFTER which a separator sits
        int b = 0;
        for (int j = 1; j <= I.Length; j++)
        {
            num.Append(I[j - 1]);
            numDigit.Add(j);
            numSepAfter.Add(0);
            if (b < boundaries.Length && boundaries[b] == j && j < I.Length)
            {
                foreach (char sc in facts.ThousandsSep) { num.Append(sc); numDigit.Add(0); numSepAfter.Add(j); }
                b++;
            }
        }
        if (p.HasDot)
        {
            foreach (char sc in facts.DecimalPoint) { num.Append(sc); numDigit.Add(0); numSepAfter.Add(0); }
            foreach (char fc in F) { num.Append(fc); numDigit.Add(0); numSepAfter.Add(0); }
        }

        var conv = negative ? facts.Negative : facts.Positive;              // !HasPlus ⇒ negative is false ⇒ the
        string sign = !p.HasPlus ? ""                                       //  nonnegative layout with no glyph
            : conv.SignPosn == 0 ? ""                                       // posn 0: the parentheses ARE the sign
            : negative ? facts.NegativeSign : facts.PositiveSign;
        string cs = p.HasCs ? facts.CurrencySymbol : "";
        string H = MonetaryPlacement.Render(conv, cs, sign, num.ToString(), out int valStart);

        // r15 — zero suppression with replacement (space). The window stops at min(zRun, digitsLeft): it never
        // crosses the decimal point (r15 a's third stop / r15 b's nonzero arm).
        var suppressed = new bool[H.Length];
        int stopDigit = Math.Min(p.ZRun, p.DigitsLeft);
        int k = stopDigit + 1;                                              // first non-suppressed integer digit
        for (int j = 1; j <= stopDigit; j++)
            if (I[j - 1] != '0') { k = j; break; }
        var h = H.ToCharArray();
        for (int i = 0; i < numDigit.Count; i++)
        {
            int pos = valStart + i;
            int dj = numDigit[i], sj = numSepAfter[i];
            // r15 a: every character position preceding the first non-suppressed digit becomes a space — a
            // grouping separator whose LEFT digit is still inside the suppressed run (sj < k) included.
            if ((dj > 0 && dj < k) || (sj > 0 && sj < k)) { h[pos] = ' '; suppressed[pos] = true; }
        }

        // r14 a)/b) — the move into the SIZE-declared item. Right-justify / LEFT-truncate: the OPPOSITE of the
        // ordinary alphanumeric move.
        if (size >= h.Length) return new string(' ', size - h.Length) + new string(h);
        int cut = h.Length - size;
        for (int i = 0; i < cut; i++)
            if (!(h[i] == '0' || suppressed[i]))
            {
                ExceptionState.LocaleSizeError(
                    $"PICTURE format 2: editing produced {h.Length} character positions and the item's SIZE is "
                    + $"{size}; the truncated character '{h[i]}' is neither a zero nor a space caused by a "
                    + "suppressed zero (ISO §13.18.40.5 r14 b)");
                break;                                                      // checking off: store the remainder
            }
        return new string(h, cut, size);
    }

    /// <summary>The ARITHMETIC store's capacity-checked twin (§14.7.5 — the size error condition): false, with the
    /// receiver's content untouched, when the aligned |value| carries more integer digits than the picture's
    /// integer digit positions; otherwise the <see cref="Format"/> result. ⛔ The size error condition (digit
    /// CAPACITY — EC-SIZE-TRUNCATION under EC checking) is DISTINCT from EC-LOCALE-SIZE (§13.18.40.5 r14 b's
    /// CHARACTER truncation in the layout's move into the SIZE-declared item): both can arise on one store, and a
    /// capacity-clean value can still lose a currency character to a small SIZE — Format raises that one.</summary>
    public static bool TryFormat(Int128 unscaled, int valueScale, string picture, string? localeTag, int size,
        out string edited, bool blankWhenZero = false)
    {
        var p = Parse(picture);
        UInt128 mag = CobolDec.UAbs(unscaled);
        int magDigits = mag == 0 ? 1 : mag.ToString().Length;
        if (magDigits - valueScale > p.DigitsLeft)
        {
            edited = "";
            return false;
        }
        edited = Format(unscaled, valueScale, picture, localeTag, size, blankWhenZero);
        return true;
    }

    /// <summary>The integer-digit indexes (1-based, LEFT-to-right over <paramref name="digitsLeft"/> digits) after
    /// which a grouping separator sits — mon_grouping applied RIGHT-to-left from the decimal delimiter
    /// (§13.18.40.5 r12; the size list is read only through <see cref="MonetaryFacts.GroupSize"/> — the last size
    /// repeats unless the list terminates).</summary>
    private static int[] GroupBoundaries(MonetaryFacts f, int digitsLeft)
    {
        var cuts = new List<int>();
        int fromRight = 0;
        for (int g = 0; ; g++)
        {
            int sz = f.GroupSize(g);
            if (sz <= 0) break;                                             // no further grouping
            fromRight += sz;
            if (fromRight >= digitsLeft) break;
            cuts.Add(digitsLeft - fromRight);                               // separator after this left-index digit
        }
        cuts.Sort();
        return [.. cuts];
    }

    /// <summary>De-edit a format-2 item's content back to its numeric value (§3.56; §14.9.25.4 GR5/GR6 d;
    /// §14.6.13.2 r4 — the accepted language is EXACTLY the image of <see cref="Format"/> under the SAME picture
    /// and the locale current NOW; content that is "not a possible result for any editing operation in that data
    /// item" is EC-DATA-INCOMPATIBLE with an undefined result). Deliberately NOT the NUMVAL-C LOCALE scanner —
    /// §15.68.3 r5b is laxer (optional currency, arbitrary spaces, no fixed fraction width), and a shared scanner
    /// would silently swallow the r4 condition. The returned value is unscaled at the PICTURE's scale
    /// (digits right of '.'), which the caller knows statically; "which may be signed" (GR6 d 1) — negative only
    /// when the picture has a '+', since <see cref="Format"/> can never have written a negative layout otherwise.</summary>
    public static Int128 DeEdit(string content, string picture, string? localeTag, bool blankWhenZero = false)
    {
        var p = Parse(picture);
        string s = content ?? "";

        bool allSpace = true;
        foreach (char c in s)
            if (c != ' ') { allSpace = false; break; }
        if (allSpace)
        {
            if (blankWhenZero || p.AllZ) return 0;                          // r10 / r15 b) — possible results
            ExceptionState.DataIncompatibleError(
                "PICTURE format 2 de-editing: the item is all spaces, which is not a possible result for any "
                + "editing operation in that data item (ISO §14.6.13.2 r4)");
            return 0;
        }

        var facts = MonetaryFacts.Require(localeTag, "PICTURE format 2 locale de-editing", "ISO §13.18.40.5 r11");
        string t = s.TrimStart(' ');                                        // undo r14 a)'s left space fill

        // Undo the layout: try the conventions Format could have written, negative first (an empty positive
        // sign matches only by absence, so the negative layout must get first refusal). A negative convention
        // with NO distinguishing mark (empty negative_sign and no parentheses) writes the same characters as the
        // nonnegative one — Format lost the sign there, and the only coherent de-edit is nonnegative.
        string cs = p.HasCs ? facts.CurrencySymbol : "";
        bool negDistinct = facts.Negative.SignPosn == 0 || facts.NegativeSign.Length > 0;
        if (p.HasPlus && negDistinct && TryUnwrap(t, facts.Negative,
                facts.Negative.SignPosn == 0 ? "" : facts.NegativeSign, cs, out string? numNeg))
            return DeEditNum(numNeg!, p, facts, negative: true, s);
        string posSign = p.HasPlus && facts.Positive.SignPosn != 0 ? facts.PositiveSign : "";
        if (TryUnwrap(t, facts.Positive, posSign, cs, out string? numPos))
            return DeEditNum(numPos!, p, facts, negative: false, s);
        Incompatible(s);
        return 0;
    }

    /// <summary>Match one convention's fixed frame around the numeric body — the frame strings come from the ONE
    /// <see cref="MonetaryPlacement.Render(MonetaryConvention, string, string, string, out int)"/> so the matcher
    /// and the renderer cannot disagree about a layout.</summary>
    private static bool TryUnwrap(string t, MonetaryConvention conv, string sign, string cs, out string? num)
    {
        string marker = ((char)0xFFFF).ToString();                          // a noncharacter: never locale content
        string frame = MonetaryPlacement.Render(conv, cs, sign, marker, out _);
        int at = frame.IndexOf(marker, StringComparison.Ordinal);
        string prefix = frame[..at], suffix = frame[(at + 1)..];
        num = null;
        if (t.Length < prefix.Length + suffix.Length) return false;
        if (!t.StartsWith(prefix, StringComparison.Ordinal)) return false;
        if (!t.EndsWith(suffix, StringComparison.Ordinal)) return false;
        num = t[prefix.Length..^suffix.Length];
        return num.Length > 0;
    }

    /// <summary>The numeric body's inverse of Format's steps 4a/5: the LEADING spaces are the suppressed
    /// positions (digits and separators alike — r15 replaces both) and contribute NOTHING, grouping separators
    /// are skipped left of the decimal separator, the decimal separator appears once when the picture has a '.',
    /// and the fraction width must be exactly the picture's. An interior space AFTER the first digit is not a
    /// possible editing result (suppression is a leading run). The integer part may be SHORTER than digitsLeft —
    /// r14 b) may have left-truncated it. The separators among the VISIBLE integer digits must stand exactly where
    /// Format's <see cref="GroupBoundaries"/> put them — every mon_grouping position, measured from the decimal
    /// delimiter, carries one (a separator elsewhere, or a missing one, is not a possible editing result — kb/Work
    /// PB835); a separator LEFT of the first visible digit is the r14 b) truncation's remainder and is skipped.</summary>
    private static Int128 DeEditNum(string num, in Shape p, MonetaryFacts facts, bool negative, string original)
    {
        Int128 value = 0;
        int fracSeen = -1, intDigits = 0;
        // The visible integer runs, left to right (at most one more than the separators, so num.Length + 1 bounds it).
        Span<int> groups = num.Length < 64 ? stackalloc int[num.Length + 1] : new int[num.Length + 1];
        int ng = 1;
        for (int i = 0; i < num.Length;)
        {
            char c = num[i];
            if (char.IsAsciiDigit(c))
            {
                value = value * 10 + (c - '0');
                if (fracSeen >= 0) fracSeen++;
                else { intDigits++; groups[ng - 1]++; }
                i++;
                continue;
            }
            if (c == ' ' && intDigits == 0 && fracSeen < 0) { i++; continue; }   // a suppressed leading position
            if (fracSeen < 0 && facts.ThousandsSep.Length > 0
                && string.CompareOrdinal(num, i, facts.ThousandsSep, 0, facts.ThousandsSep.Length) == 0)
            {
                if (intDigits > 0) groups[ng++] = 0;
                i += facts.ThousandsSep.Length;
                continue;
            }
            if (fracSeen < 0 && p.HasDot
                && string.CompareOrdinal(num, i, facts.DecimalPoint, 0, facts.DecimalPoint.Length) == 0)
            { fracSeen = 0; i += facts.DecimalPoint.Length; continue; }
            Incompatible(original);
            return 0;
        }
        if (p.HasDot && fracSeen != p.DigitsRight) { Incompatible(original); return 0; }
        if (!p.HasDot && fracSeen >= 0) { Incompatible(original); return 0; }
        if (intDigits > p.DigitsLeft) { Incompatible(original); return 0; }
        if (!facts.GroupingIsExact(groups[..ng])) { Incompatible(original); return 0; }
        return negative ? -value : value;
    }

    private static void Incompatible(string content) =>
        ExceptionState.DataIncompatibleError(
            $"PICTURE format 2 de-editing: the item's content \"{content}\" is not a possible result for any "
            + "editing operation in that data item under the locale in effect (ISO §14.6.13.2 r4)");
}
