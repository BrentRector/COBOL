// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

public static partial class CobolEdit
{
    /// <summary>⭐ THE MASK'S DIGIT-POSITION FACTS — written ONCE (kb/Work PB2639). Which symbols of an edited
    /// PICTURE are digit positions is the question <see cref="Format"/> (the render), <see cref="TryFormat"/> (the
    /// size-error bound), <see cref="MaskCapacity"/>, <see cref="MaskScale"/> and <see cref="DeEdit"/> must all
    /// answer identically, and each used to re-derive it from the mask text: three of the five dropped the
    /// <see cref="EditRule"/> array, so a FLOATING extended editing sign control symbol — a digit position by
    /// §13.18.40.5 rule 6 — was a digit in the render and not in the bound or the scale.
    /// <para>The rule: 9, Z and * always; the currency symbol and the editing sign control symbols '+' and '-'
    /// when used as FLOATING insertion symbols (a string of at least two, rule 6); and a PICTURE EDITING
    /// character-1 whose <see cref="EditRule.Floating"/> bind decided. Rule 6's fourth paragraph — "The second
    /// floating symbol represents the leftmost limit of the numeric data that may be stored in the item" — makes
    /// ONE member of a floating string the symbol itself, so <see cref="Capacity"/> is the digit-position count
    /// less one whenever any floating string exists. The <paramref name="picture"/> may hold V and P: neither is
    /// counted. Taking <paramref name="Edits"/> is not optional, which is why <see cref="Of"/> has no overload
    /// without it.</para></summary>
    private readonly record struct MaskFacts(char CurrencyChar, int Plus, int Minus, int Currency, EditRule[]? Edits,
        int DigitSymbols = 0, bool FloatingChar1 = false)
    {
        /// <summary>A single '+' with no '-' (and the reverse) is FIXED insertion; two or more are a floating
        /// string (§13.18.40.5 rule 6: "a string of at least two identical floating insertion editing symbols").
        /// The pair of counts is the classification the pre-scan has always used, moved here unchanged.</summary>
        public bool FixedPlus => Plus == 1 && Minus == 0;

        /// <inheritdoc cref="FixedPlus"/>
        public bool FixedMinus => Minus == 1 && Plus == 0;

        /// <summary>One occurrence of the currency symbol is fixed insertion; two or more float.</summary>
        public bool FixedCurrency => Currency == 1;

        /// <summary>Does a floating insertion string of any kind exist in the mask?</summary>
        public bool HasFloating => Currency > 1 || Plus > 1 || Minus > 1 || FloatingChar1;

        /// <summary>The mask's digit-position capacity — the §14.7.5 size-error bound and the width of the
        /// rendered digit string.</summary>
        public int Capacity => HasFloating ? DigitSymbols - 1 : DigitSymbols;

        /// <summary>Is <paramref name="raw"/> (a symbol of the expanded mask) a digit position?</summary>
        public bool IsDigitPosition(char raw)
        {
            char p = char.ToUpperInvariant(raw);
            return p is '9' or 'Z' or '*'
                || (p == CurrencyChar && !FixedCurrency)
                || (p == '+' && !FixedPlus)
                || (p == '-' && !FixedMinus)
                || IsFloatingChar1(raw);
        }

        /// <summary>Is <paramref name="raw"/> a FLOATING PICTURE EDITING character-1 (a digit position by rule 6)?</summary>
        public bool IsFloatingChar1(char raw) => RuleFor(raw, Edits) is { Floating: true };

        /// <summary>Count the mask's symbols. <paramref name="currency"/> is the currency PICTURE SYMBOL (any
        /// case); <paramref name="picture"/> is dot-canonical (a comma-mode mask already swapped).</summary>
        public static MaskFacts Of(string picture, EditRule[]? edits, char currency = '$')
        {
            char cur = char.ToUpperInvariant(currency);
            int plus = 0, minus = 0, cs = 0;
            foreach (char raw in picture)
            {
                char p = char.ToUpperInvariant(raw);
                if (p == '+') plus++;
                else if (p == '-') minus++;
                else if (p == cur) cs++;
            }
            var counted = new MaskFacts(cur, plus, minus, cs, edits);
            int digits = 0;
            bool floatingChar1 = false;
            foreach (char raw in picture)
            {
                if (!counted.IsDigitPosition(raw)) continue;
                digits++;
                if (counted.IsFloatingChar1(raw)) floatingChar1 = true;
            }
            return counted with { DigitSymbols = digits, FloatingChar1 = floatingChar1 };
        }

        /// <summary>The mask's fraction scale — digit positions right of the point (<c>V</c> or <c>.</c>), the
        /// negative P-run scale of a trailing P, or P-count plus digit positions for a leading P. A FLOATING
        /// extended editing sign control symbol is a digit position on exactly the same footing as a floating
        /// currency symbol (rule 6 lists both among the floating insertion symbols), and §13.18.40.3 SR29 lets a
        /// floating string reach past the decimal point, so it counts in both branches (kb/Work PB491, PB2639).
        /// <paramref name="picture"/> is dot-canonical and still holds V and P.</summary>
        public int FractionDigits(string picture)
        {
            // PICTURE P scaling positions (§13.18.40.3): trailing P → a NEGATIVE mask scale (the value is a multiple
            // of 10^P — PIC ZZZPP aligns 900 to unscaled 9, NC124A PICTURE-TEST-30); leading P → every digit position
            // is fractional (scale = P-count + digit positions). P never coexists with V-fraction digits.
            // ⛔ The leading/trailing split anchors on EVERY digit position — 9/Z/* AND a FLOATING string's member
            // occurrences (§13.18.40.6 Table 10 puts 'P (left of decimal point)' beside floating cs and +/−) — never
            // on only the literal 9/Z/* (kb/Work PB155: `PIC $$$$PP` has no 9/Z/* at all, so its rightmost P run
            // read as LEADING and the mask scale came out +2 where the value is a multiple of 10^2, scale −2).
            // A FIXED single +/−/cs is not a digit position and must not anchor (a trailing fixed sign sits right
            // of a trailing P run: 99PPCR).
            int pCount = 0;
            foreach (char raw in picture) if (char.ToUpperInvariant(raw) == 'P') pCount++;
            if (pCount > 0)
            {
                string up = picture.ToUpperInvariant();
                int lastDigitPos = -1, digitPositions = 0;
                bool sawFloating = false;
                for (int i = 0; i < up.Length; i++)
                    if (IsDigitPosition(up[i]))
                    {
                        lastDigitPos = i;
                        // the LEFTMOST occurrence of a floating string is the sign/currency itself, not a digit
                        if (up[i] is not ('9' or 'Z' or '*') && !sawFloating) { sawFloating = true; continue; }
                        digitPositions++;
                    }
                if (lastDigitPos >= 0 && up.IndexOf('P', lastDigitPos) > lastDigitPos) return -pCount;
                return pCount + digitPositions;
            }
            int point = picture.IndexOf('V');
            if (point < 0) point = picture.IndexOf('.');
            if (point < 0) return 0;
            int n = 0;
            for (int i = point + 1; i < picture.Length; i++)
            {
                if (IsDigitPosition(picture[i])) n++;
                else if (char.ToUpperInvariant(picture[i]) is 'C' or 'D') break;   // CR/DB
            }
            return n;
        }
    }
}
