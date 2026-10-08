// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Numerics;
using System.Text;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// The FLOATING-POINT NUMERIC-EDITED form (ISO/IEC 1989:2023 §13.18.40.4 GR13 b — a PICTURE whose two parts are
/// separated by the symbol <c>E</c>: a significand that is a numeric or fixed numeric-edited string with no
/// floating insertion and no zero suppression, and an exponent <c>+9</c>…<c>+9999</c>) — data-model design D21,
/// kb/Work PB66. The item's storage is its character image (category numeric-edited, class alphanumeric); its
/// VALUE channel is EXACT DECIMAL: a store normalizes the sending value by integer arithmetic so the significand's
/// most significant digit is nonzero (§14.6.8.4 GR1) and edits it into the mask (Table 7: simple, special and
/// fixed insertion for the significand, none for the exponent; rule 8 for zero), and a de-editing read
/// (§14.9.25.4 GR5) yields a <see cref="CobolDec"/> — a significand and its own power of ten — never a
/// <c>double</c> (a 36-digit significand, §13.18.40.3 SR15, does not round-trip binary64).
/// </summary>
public static partial class CobolEdit
{
    /// <summary>The parsed two-part structure of a floating-point numeric-edited PICTURE (dot-canonical: a
    /// DECIMAL-POINT IS COMMA mask is swapped on the way in and its image swapped back on the way out).</summary>
    /// <param name="SigPattern">The significand's expanded character-string, sign symbol included, in dot-canonical form.</param>
    /// <param name="SigDigits">The '9' positions of the significand (§13.18.40.3 SR15: 1..36).</param>
    /// <param name="SigScale">The '9' positions right of the significand's '.' (0 when there is none).</param>
    /// <param name="SigSign">The significand's fixed-insertion sign symbol ('+', '-') or '\0' when unsigned.</param>
    /// <param name="ExpDigits">The '9' positions of the exponent (§13.18.40.4 GR13 b: 1..4).</param>
    /// <param name="Edits">The item's PICTURE EDITING rules — only the IS form can reach this form (§13.18.40.3
    /// SR12 bars the FOR form's extended editing sign control symbols from a floating-point edited item), and an
    /// IS-form character-1 is a SIMPLE INSERTION symbol (§13.18.40.5 rule 3), which Table 7 admits in the
    /// significand. In comma mode the literals are held PRE-SWAPPED, so the whole-image separator swap on the way
    /// out restores them (§13.18.40.2 SR13 exchanges the SYMBOLS' roles, never a literal's characters; kb/Work
    /// PB866). Null when the clause has no EDITING phrase.</param>
    public readonly record struct FloatMask(string SigPattern, int SigDigits, int SigScale, char SigSign, int ExpDigits,
        EditRule[]? Edits = null)
    {
        /// <summary>The largest exponent magnitude the mask can hold (10^ExpDigits − 1).</summary>
        public int MaxExp => Pow10Int(ExpDigits) - 1;

        /// <summary>The whole item's character length: the significand's positions (each IS-form character-1
        /// counted at its literal-1's width — §13.18.40.4 GR14 'es', "the size of literal-1 is counted in the size
        /// of the item") + 'E' + the exponent's sign + digits.</summary>
        public int Length
        {
            get
            {
                int n = 0;
                foreach (char c in SigPattern) n += RuleFor(c, Edits) is { } r ? r.Width : 1;
                return n + 1 + 1 + ExpDigits;
            }
        }

        /// <summary>Parse an EXPANDED floating-point numeric-edited picture (repeats unrolled, uppercased — the
        /// analyzer's <c>EditMask</c>). The analyzer has already validated the form (§13.18.40.4 GR13 b, Table 10 row
        /// E); this reads the structure it guaranteed.</summary>
        public static FloatMask Parse(string picture, bool commaMode = false, EditRule[]? edits = null)
        {
            if (commaMode)
            {
                picture = SwapSeparators(picture);
                if (edits is not null)
                    edits = Array.ConvertAll(edits, r => r with { Neg = SwapSeparators(r.Neg), Pos = SwapSeparators(r.Pos) });
            }
            int e = picture.IndexOf('E');
            if (e < 0) throw new ArgumentException($"not a floating-point numeric-edited picture: {picture}", nameof(picture));
            string sig = picture[..e];
            string exp = picture[(e + 1)..];
            char sign = sig.Length > 0 && sig[0] is '+' or '-' ? sig[0] : '\0';
            int digits = 0, scale = 0; bool afterPoint = false;
            foreach (char c in sig)
            {
                if (c == '9') { digits++; if (afterPoint) scale++; }
                else if (c == '.') afterPoint = true;
            }
            int expDigits = exp.Count(c => c == '9');
            return new FloatMask(sig, digits, scale, sign, expDigits, edits is { Length: > 0 } ? edits : null);
        }
    }

    /// <summary>The disposition of a floating-point edited store — the caller (MOVE vs arithmetic) decides what
    /// each means (§14.9.25.4 GR6 item 4 vs §14.7.5 cases 3/4). <see cref="Inexact"/> is §14.7.4.3 rule 7's own
    /// outcome, reported only for <see cref="CobolRounding.Prohibited"/> (kb/Work PB2638): the significand cannot
    /// be represented exactly in the mask's digits, which is a size error and not a rounding.</summary>
    public enum FloatStoreOutcome { Ok, Overflow, Underflow, Inexact }

    /// <summary>The MOVE store (ISO §14.9.25.4 GR6 item 4): a value farther from zero than the mask permits sets
    /// EC-DATA-OVERFLOW (fatal when the statement has it enabled) and — the content being "undefined" — stores the
    /// PINNED saturated image (all-nines significand at the maximum exponent, the value's sign; docs/CONFORMANCE.md);
    /// a value nearer to zero than the smallest nonzero the mask can hold "is treated as zero" — the rule-8 zero image,
    /// no exception. <paramref name="value"/> × 10^−<paramref name="valueScale"/> is the sending value.
    /// <para>⛔ <paramref name="mode"/> is the LANDING's, named by the caller (kb/Work PB2638): a MOVE passes
    /// <see cref="CobolRounding.Truncation"/> (§14.6.8.4 rule 2 aligns and truncates), and the no-phrase
    /// ARITHMETIC store passes the receiver's own §14.7.4.3 mode — ROUNDED applies to a floating-point edited
    /// resultant exactly as to any other, at the significand's digit count; PROHIBITED lands truncated here, as
    /// <c>CONFORMANCE.md</c> DOC-A.1-70 gives every unchecked store, and is raised only by <see cref="TryFormatFloat(Int128, int, string, out string, CobolRounding, bool, bool, EditRule[])"/>.</para></summary>
    public static string FormatFloatStore(Int128 value, int valueScale, string picture, CobolRounding mode, bool blankWhenZero = false, bool commaMode = false, EditRule[]? edits = null)
        => FormatFloatStoreCore((BigInteger)value, -valueScale, picture, mode, blankWhenZero, commaMode, edits);

    /// <summary>The unsigned-wide lane of <see cref="FormatFloatStore(Int128, int, string, CobolRounding, bool, bool, EditRule[])"/>
    /// (kb/Work R10's <see cref="UInt128"/> carrier, a 16-byte unsigned COMP-5 item's full container value), through
    /// <see cref="UnsignedWideSignificand"/>. ⛔ Distinctly named, never an overload: an <c>int</c> constant converts
    /// implicitly to both wide types (<c>CobolNum.StoreUOrRaise</c>).</summary>
    public static string FormatFloatStoreU(UInt128 value, int valueScale, string picture, CobolRounding mode, bool blankWhenZero = false, bool commaMode = false, EditRule[]? edits = null)
    {
        var (sig, scale) = UnsignedWideSignificand(value, valueScale);
        return FormatFloatStore(sig, scale, picture, mode, blankWhenZero, commaMode, edits);
    }

    /// <summary>An unsigned-wide value as the <see cref="Int128"/> significand the fixed lane takes: exact when it fits;
    /// past <see cref="Int128.MaxValue"/> (only a 39-digit container value) one decimal digit is folded
    /// ROUND-TO-ODD: the 38-digit quotient keeps an ODD last digit whenever the dropped digit is nonzero. A floating-point
    /// edited significand has far fewer than 37 digits, so every §14.7.4.3 mode, and PROHIBITED's exactness test
    /// (rule 7), rounds the folded value exactly as it would the exact one: the same lowering argument the
    /// standard-decimal intermediate's round-to-odd rests on (docs/CONFORMANCE.md DOC-A.1-123).</summary>
    private static (Int128 Sig, int Scale) UnsignedWideSignificand(UInt128 value, int valueScale)
    {
        if (value <= (UInt128)Int128.MaxValue) return ((Int128)value, valueScale);
        UInt128 q = value / 10;
        if (value % 10 != 0 && q % 2 == 0) q++;
        return ((Int128)q, valueScale - 1);
    }

    /// <summary>The store of a <see cref="CobolDec"/>-carried sender (a standard-decimal intermediate or another
    /// floating-point edited item's de-edited value).</summary>
    public static string FormatFloatStore(CobolDec value, string picture, CobolRounding mode, bool blankWhenZero = false, bool commaMode = false, EditRule[]? edits = null)
        => FormatFloatStoreCore((BigInteger)value.Sig, value.Exp, picture, mode, blankWhenZero, commaMode, edits);

    /// <summary>The store of a binary64 sender — through the shortest round-trip decimal (<see cref="CobolDec.FromDouble"/>).</summary>
    public static string FormatFloatStore(double value, string picture, CobolRounding mode, bool blankWhenZero = false, bool commaMode = false, EditRule[]? edits = null)
        => FormatFloatStore(CobolDec.FromDouble(value), picture, mode, blankWhenZero, commaMode, edits);

    private static string FormatFloatStoreCore(BigInteger sig, int exp10, string picture, CobolRounding mode, bool blankWhenZero, bool commaMode, EditRule[]? edits)
    {
        var m = FloatMask.Parse(picture, commaMode, edits);
        // An unchecked store never acts on Inexact: PROHIBITED is a request to raise, and this one cannot; the
        // rounding kernel lands it truncated (DOC-A.1-70), the same as every unchecked fixed-point store.
        string image = FormatFloatCore(sig, exp10, m, mode, blankWhenZero, out var outcome);
        if (outcome == FloatStoreOutcome.Overflow)
            ExceptionState.FloatOverflowError($"the value {sig}E{exp10} is farther from zero than the picture {picture} permits");
        return commaMode ? SwapSeparators(image) : image;
    }

    /// <summary>The ARITHMETIC store (ISO §14.7.5 cases 3 and 4 — both the size error condition, receiver unchanged):
    /// false when the value is farther from zero OR nearer to zero than the mask permits, or when
    /// <paramref name="mode"/> is <see cref="CobolRounding.Prohibited"/> and the significand cannot be represented
    /// exactly (§14.7.4.3 rule 7: the size error condition exists and the resultant is unchanged) — the caller
    /// raises the size error and leaves the receiver alone — else the edited image in <paramref name="image"/>,
    /// the significand rounded to the mask's digits by <paramref name="mode"/> (§14.7.4.3 rules 3 to 10).</summary>
    public static bool TryFormatFloat(Int128 value, int valueScale, string picture, out string image, CobolRounding mode, bool blankWhenZero = false, bool commaMode = false, EditRule[]? edits = null)
        => TryFormatFloatCore((BigInteger)value, -valueScale, picture, out image, mode, blankWhenZero, commaMode, edits);

    /// <summary>The unsigned-wide lane of <see cref="TryFormatFloat(Int128, int, string, out string, CobolRounding, bool, bool, EditRule[])"/>
    /// (distinctly named — see <see cref="FormatFloatStoreU"/>).</summary>
    public static bool TryFormatFloatU(UInt128 value, int valueScale, string picture, out string image, CobolRounding mode, bool blankWhenZero = false, bool commaMode = false, EditRule[]? edits = null)
    {
        var (sig, scale) = UnsignedWideSignificand(value, valueScale);
        return TryFormatFloat(sig, scale, picture, out image, mode, blankWhenZero, commaMode, edits);
    }

    /// <inheritdoc cref="TryFormatFloat(Int128, int, string, out string, CobolRounding, bool, bool, EditRule[])"/>
    public static bool TryFormatFloat(CobolDec value, string picture, out string image, CobolRounding mode, bool blankWhenZero = false, bool commaMode = false, EditRule[]? edits = null)
        => TryFormatFloatCore((BigInteger)value.Sig, value.Exp, picture, out image, mode, blankWhenZero, commaMode, edits);

    /// <inheritdoc cref="TryFormatFloat(Int128, int, string, out string, CobolRounding, bool, bool, EditRule[])"/>
    public static bool TryFormatFloat(double value, string picture, out string image, CobolRounding mode, bool blankWhenZero = false, bool commaMode = false, EditRule[]? edits = null)
        => TryFormatFloat(CobolDec.FromDouble(value), picture, out image, mode, blankWhenZero, commaMode, edits);

    private static bool TryFormatFloatCore(BigInteger sig, int exp10, string picture, out string image, CobolRounding mode, bool blankWhenZero, bool commaMode, EditRule[]? edits)
    {
        var m = FloatMask.Parse(picture, commaMode, edits);
        string img = FormatFloatCore(sig, exp10, m, mode, blankWhenZero, out var outcome);
        image = commaMode ? SwapSeparators(img) : img;
        return outcome == FloatStoreOutcome.Ok;
    }

    /// <summary>The image of the mask's positive extreme (all-nines significand at the maximum exponent) — the value
    /// FUNCTION HIGHEST-ALGEBRAIC names (§15.43.4 r2) and the pinned overflow content.</summary>
    public static string FloatExtremeImage(string picture, bool negative, bool commaMode = false, EditRule[]? edits = null)
    {
        var m = FloatMask.Parse(picture, commaMode, edits);
        string img = RenderFloat(m, negative, new string('9', m.SigDigits), m.MaxExp);
        return commaMode ? SwapSeparators(img) : img;
    }

    /// <summary>The core: normalize (§14.6.8.4 GR1) by exact integer arithmetic to the mask's significand digit
    /// count, round the dropped digits by <paramref name="mode"/> through the ONE rounding kernel
    /// (<see cref="CobolNum.RoundDiv{T}"/>), decide the outcome against the exponent's capacity, render
    /// (§13.18.40.5 Table 7 + rule 8).
    /// <para>⛔ THE ROUNDING PRECEDES THE RANGE TEST (kb/Work PB2638). §14.7.5 case 3 is "after radix point
    /// alignment and any applicable rounding specifications", so a significand that rounds up past its last digit
    /// (<c>9.995</c> into <c>+9.99E+99</c> is <c>+1.00E+01</c>) is renormalized and its exponent re-checked against
    /// the mask's capacity, and a value just under the underflow bound that rounds up to the smallest
    /// representable nonzero is that value and not a size error. A MOVE passes TRUNCATION (§14.6.8.4 rule 2) and
    /// never carries.</para></summary>
    private static string FormatFloatCore(BigInteger sig, int exp10, in FloatMask m, CobolRounding mode, bool blankWhenZero, out FloatStoreOutcome outcome)
    {
        outcome = FloatStoreOutcome.Ok;
        // BLANK WHEN ZERO (§13.18.8.4 GR1) tests THE VALUE BEING STORED, here and at the underflow arm below —
        // the only two ways this form stores zero, since normalization leaves the significand's leading digit
        // nonzero and its truncation therefore cannot produce a zero (kb/Work PB566 sweep). Otherwise rule 8's
        // zero image: all significand and exponent digits zero, both signs positive.
        if (sig.IsZero)
            return blankWhenZero ? new string(' ', m.Length) : RenderFloat(m, negative: false, new string('0', m.SigDigits), 0);
        bool negative = sig.Sign < 0, inexact = false;
        BigInteger a = BigInteger.Abs(sig);
        int d = DigitCount(a);
        // S = a × 10^k with exactly SigDigits digits (leading digit nonzero by construction; dropped digits round
        // by `mode` — TRUNCATION for a MOVE, §14.6.8.4 GR2 → §13.18.40's alignment / truncation rules); the
        // exponent that keeps the value:
        // value = a × 10^exp10 = (S × 10^−SigScale) × 10^E  ⇒  E = d + exp10 + SigScale − SigDigits.
        int k = m.SigDigits - d;
        int e = d + exp10 + m.SigScale - m.SigDigits;
        var tenK = BigInteger.Pow(10, Math.Abs(k));
        // k < 0 drops digits: the SIGNED value goes to the kernel, because TOWARD-GREATER and TOWARD-LESSER depend
        // on the sign and the kernel takes the away-from-zero step from it (§14.7.4.3 rules 3, 8 and 9).
        var s = k >= 0 ? a * tenK : BigInteger.Abs(CobolNum.RoundDiv(sig, tenK, mode));
        if (k < 0)
        {
            // §14.7.4.3 rule 7: PROHIBITED and a nonzero dropped tail — the kernel lands such a value truncated
            // (the unchecked disposition), the checked caller turns this outcome into the size error.
            if (mode == CobolRounding.Prohibited && !(a % tenK).IsZero) inexact = true;
            if (DigitCount(s) > m.SigDigits)     // rounded up past the last digit: 9.99|5 → 10.00
            {
                s /= 10;
                e++;
            }
        }
        int maxExp = m.MaxExp;
        if (e > maxExp)
        {
            outcome = FloatStoreOutcome.Overflow;
            return RenderFloat(m, negative, new string('9', m.SigDigits), maxExp);   // the pinned saturated image
        }
        if (e < -maxExp)
        {
            outcome = FloatStoreOutcome.Underflow;
            // §14.9.25.4 GR6 d) 4 b: a value nearer to zero than the receiver permits "is treated as zero" — so
            // the value BEING STORED is zero and §13.18.8.4 GR1 blanks.
            return blankWhenZero ? new string(' ', m.Length) : RenderFloat(m, negative: false, new string('0', m.SigDigits), 0);
        }
        if (inexact) outcome = FloatStoreOutcome.Inexact;
        return RenderFloat(m, negative, s.ToString().PadLeft(m.SigDigits, '0'), e);
    }

    /// <summary>Render a normalized significand digit string and exponent into the mask (dot-canonical).</summary>
    private static string RenderFloat(in FloatMask m, bool negative, string sigDigits, int e)
    {
        var sb = new StringBuilder(m.Length);
        int next = 0;
        foreach (char c in m.SigPattern)
        {
            switch (c)
            {
                case '9': sb.Append(sigDigits[next++]); break;
                case '+': sb.Append(negative ? '-' : '+'); break;   // Table 8 fixed insertion
                case '-': sb.Append(negative ? '-' : ' '); break;
                // SIMPLE INSERTION from the ONE set (§13.18.40.5 rule 3, Table 7's "Simple insertion … for the
                // significand part") — 'B' inserts a space, '0' '/' ',' insert themselves, and an IS-form PICTURE
                // EDITING character-1 inserts its WHOLE literal-1 (rule 3: "if literal-1 is specified, character-1"
                // is a simple insertion symbol; GR14 'es' counts literal-1's size — kb/Work PB866). The FOR form
                // never reaches here: §13.18.40.3 SR12 bars extended editing sign control symbols from this form
                // (PictureAnalyzer.AnalyzeFloatEdited, COBOLNET1658). '.' is SPECIAL insertion (rule 4) and falls to
                // the default, which copies every remaining mask character verbatim.
                default:
                    if (RuleFor(c, m.Edits) is { SimpleInsertion: true } r) sb.Append(r.Pos);
                    else sb.Append(TrySimpleInsertion(c, null, out char ins) ? ins : c);
                    break;
            }
        }
        sb.Append('E');
        sb.Append(e < 0 ? '-' : '+');
        sb.Append(Math.Abs(e).ToString().PadLeft(m.ExpDigits, '0'));
        return sb.ToString();
    }

    /// <summary>DE-EDIT a floating-point numeric-edited image (ISO §14.9.25.4 GR5): the significand's digit positions
    /// and sign, the 'E', the exponent's sign and digits, back to the exact value <c>±S × 10^(E − SigScale)</c>.
    /// An ALL-SPACES image of an item with BLANK WHEN ZERO (<paramref name="blankWhenZero"/>) is that item's zero
    /// (§13.18.8.4 GR3: the sending data item's "value … is considered to be zero"). Under EC-DATA-INCOMPATIBLE
    /// checking (§14.6.13.2 rule 4: content "not a possible result for any editing operation in that data item"),
    /// the image is verified to be an editing result by the fixed-point form's own test — <see cref="FormatFloatCore"/>
    /// of the de-edited value, with the item's BLANK WHEN ZERO, must reproduce it, so a significand that §14.6.8.4 GR1
    /// would have normalized (a leading zero on a nonzero value), a nonzero exponent on a zero value and every
    /// misplaced character are one and the same mismatch — and the fatal exception is raised before any receiver is
    /// written. With checking off a non-digit contributes zero (the tolerant direction the zoned/packed decoders take).</summary>
    public static CobolDec DeEditFloat(string image, string picture, bool commaMode = false, EditRule[]? edits = null,
        bool blankWhenZero = false)
    {
        var m = FloatMask.Parse(picture, commaMode, edits);
        if (blankWhenZero && IsBlanked(image)) return new CobolDec(0, 0);   // §13.18.8.4 GR3
        if (commaMode) image = SwapSeparators(image);
        bool negative = false;
        BigInteger s = BigInteger.Zero;
        int i = 0;
        char At() => i < image.Length ? image[i] : (char)0;
        foreach (char c in m.SigPattern)
        {
            // An IS-form character-1 stands for its whole literal-1 (GR14 'es'): it holds no digit and no sign.
            if (RuleFor(c, m.Edits) is { SimpleInsertion: true } r)
            {
                i += r.Width;
                continue;
            }
            char ch = At(); i++;
            switch (c)
            {
                case '9': s = s * 10 + (char.IsAsciiDigit(ch) ? ch - '0' : 0); break;   // a non-digit contributes zero at its position
                case '+' or '-': if (ch == '-') negative = true; break;
            }
        }
        i++;   // the 'E'
        bool expNeg = At() == '-'; i++;
        int e = 0;
        for (int k = 0; k < m.ExpDigits; k++)
        {
            char ch = At(); i++;
            if (char.IsAsciiDigit(ch)) e = e * 10 + (ch - '0');
        }
        if (expNeg) e = -e;
        if (ExceptionState.DataIncompatibleChecking)
        {
            // The image's own digits fit the mask, so the round trip drops none and the mode is moot.
            string expected = FormatFloatCore(negative ? -s : s, e - m.SigScale, m, CobolRounding.Truncation, blankWhenZero, out _);
            if (expected != image)
                ExceptionState.DataIncompatibleError(
                    $"the content '{image}' is not a possible result of editing into {picture} (a de-editing MOVE, ISO 14.6.13.2 rule 4)");
        }
        // The significand's digits fit Int128 (≤ 36 digits, SR15).
        Int128 sig = (Int128)s;
        return new CobolDec(negative ? -sig : sig, e - m.SigScale);
    }

    private static int DigitCount(BigInteger a) => a.IsZero ? 1 : a.ToString().Length;

    private static int Pow10Int(int n) { int r = 1; for (int i = 0; i < n; i++) r *= 10; return r; }
}
