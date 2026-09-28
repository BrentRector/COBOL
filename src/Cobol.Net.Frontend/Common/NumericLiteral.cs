// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Common;

/// <summary>Which decimal-separator rule a numeric literal violated, if any (ISO/IEC 1989:2023 §12.3.7 GR14a /
/// §8.3.3.3.2). Returned by <see cref="NumericLiteral.Normalize"/> so each caller can route the diagnostic to its
/// own channel with its own code — the algorithm stays a single pure function.</summary>
public enum NumericSeparatorIssue
{
    /// <summary>The separator matched the active mode — no violation.</summary>
    None,

    /// <summary>Under DECIMAL-POINT IS COMMA the literal used a '.' where the comma is the decimal separator.</summary>
    DecimalPointUnderCommaMode,

    /// <summary>Without DECIMAL-POINT IS COMMA the literal used a ',' decimal separator (only '.' is admitted).</summary>
    CommaWithoutCommaMode,
}

/// <summary>
/// The ONE numeric-literal normalizer (ISO/IEC 1989:2023 §12.3.7 GR14a: under DECIMAL-POINT IS COMMA "the
/// character written in numeric literals to represent the decimal separator shall be the comma"; §8.3.3.3.2
/// admits ONLY the decimal point as the separator in a fixed-point literal). It converts a numeric literal's
/// source text to the canonical dot-decimal form the whole decode pipeline consumes and reports — via the returned
/// <see cref="NumericSeparatorIssue"/>, not by emitting anything itself — whether the source used the wrong
/// separator for the active mode.
///
/// This is the algorithm that used to live inline in <c>DataBinder.NormalizeNumericLiteral</c>; it is lifted to
/// the Frontend so BOTH the compiler binder (which keeps emitting COBOLNET0895 with its own descriptor) AND the
/// compile-time expression evaluator (which routes the issue to a frontend code) share ONE normalization rule —
/// no second, drifting normalizer (singular-pattern).
/// </summary>
public static class NumericLiteral
{
    /// <summary>Normalize <paramref name="text"/> to canonical dot-decimal, reporting any §12.3.7 GR14a separator
    /// violation for the active <paramref name="decimalPointIsComma"/> mode through <paramref name="issue"/>. The
    /// returned text is always well-formed dot-decimal (a violation is normalized too, so downstream decode stays
    /// well-formed while the caller emits the diagnostic).</summary>
    public static string Normalize(string text, bool decimalPointIsComma, out NumericSeparatorIssue issue)
    {
        if (decimalPointIsComma)
        {
            issue = text.Contains('.') ? NumericSeparatorIssue.DecimalPointUnderCommaMode : NumericSeparatorIssue.None;
            return text.Replace(',', '.');
        }
        issue = text.Contains(',') ? NumericSeparatorIssue.CommaWithoutCommaMode : NumericSeparatorIssue.None;
        return text.Replace(',', '.');
    }

    /// <summary>⛔ THE ONE integer-literal FORM test — ISO §8.3.3.3.2: "An integer literal is a fixed-point numeric
    /// literal that contains no decimal point." It is the FORM, not the value: <c>1.0</c> is not an integer literal
    /// though its value is integral, and no §8.3.3.3.3 floating-point literal is one (its significand includes a
    /// decimal point). Either separator counts — the text may be as written (a ',' under DECIMAL-POINT IS COMMA) or
    /// canonical. Read by the compiler's integer classifier (<c>IntrinsicResultType.IsIntegerOperand</c>) and the
    /// compile-time boolean-shift count (kb/Work PB1413), so the two lanes answer §5.5 2) a) the same way.</summary>
    public static bool IsIntegerLiteralForm(string text) => !text.Contains('.') && !text.Contains(',');

    /// <summary>True when <paramref name="text"/> (canonical dot-decimal) has the FLOATING-POINT numeric literal form
    /// of ISO §8.3.3.3.3: a fixed-point significand (optional sign, digits, at most one decimal point), the letter E,
    /// and an optionally signed integer exponent — <c>1.5E+3</c>, <c>-2.5E-2</c>. Fixed-point text and any
    /// non-numeric operand return false. (Whether the significand carries the decimal point the standard requires is
    /// the lexer's question; a text of this shape only ever arrives from a numeric-literal token.)</summary>
    public static bool IsFloatingPointForm(string text)
    {
        string t = text.Trim();
        int e = t.IndexOfAny(['E', 'e']);
        if (e <= 0 || e == t.Length - 1) return false;
        return IsFixedPointForm(t[..e], allowPoint: true) && IsFixedPointForm(t[(e + 1)..], allowPoint: false);

        static bool IsFixedPointForm(string s, bool allowPoint)
        {
            int i = 0;
            if (i < s.Length && s[i] is '+' or '-') i++;
            bool digit = false, point = false;
            for (; i < s.Length; i++)
            {
                if (char.IsAsciiDigit(s[i])) { digit = true; continue; }
                if (s[i] == '.' && allowPoint && !point) { point = true; continue; }
                return false;
            }
            return digit;
        }
    }

    /// <summary>Which ISO §8.3.3.3.3 form rule a floating-point literal violates (kb/Work PB99), or None.</summary>
    public enum FloatingLiteralIssue
    {
        None,
        /// <summary>SR2: the significand shall be from 1 to 36 digits.</summary>
        SignificandDigits,
        /// <summary>SR3: the exponent shall have a maximum of four digits.</summary>
        ExponentDigits,
        /// <summary>SR4: a zero significand requires a zero exponent, and neither part may carry a negative sign.</summary>
        ZeroForm,
    }

    /// <summary>The ISO §8.3.3.3.3 SR2/SR3/SR4 form check of a canonical floating-point literal text (which shall
    /// satisfy <see cref="IsFloatingPointForm"/>). SR2's "shall include a decimal point" is the lexer's shape
    /// (a point-less <c>1E10</c> is a user-defined word, §8.3.2.1); the digit counts and the zero form are checked
    /// here — the ONE place, since both the expression funnel and the VALUE funnel normalize through it.</summary>
    public static FloatingLiteralIssue CheckFloatingPointForm(string text)
    {
        string t = text.Trim();
        int e = t.IndexOfAny(['E', 'e']);
        string sig = t[..e], exps = t[(e + 1)..];
        bool sigNeg = sig.StartsWith('-'), expNeg = exps.StartsWith('-');
        string sigDigits = sig.TrimStart('+', '-').Replace(".", "");
        string expDigits = exps.TrimStart('+', '-');
        if (sigDigits.Length is < 1 or > 36) return FloatingLiteralIssue.SignificandDigits;
        if (expDigits.Length > 4) return FloatingLiteralIssue.ExponentDigits;
        bool sigZero = sigDigits.All(c => c == '0'), expZero = expDigits.All(c => c == '0');
        if (sigZero && (!expZero || sigNeg || expNeg)) return FloatingLiteralIssue.ZeroForm;
        return FloatingLiteralIssue.None;
    }

    /// <summary>True when the numeric literal's value (fixed-point or floating-point form) is representable in the
    /// IEEE binary64 (<paramref name="single"/>: binary32) range — finite, and not a nonzero value that rounds to
    /// zero (below the smallest subnormal). The implementor-defined exponent range of ISO §8.3.3.3.3 r3 for a
    /// literal that evaluates in a binary floating-point form: a procedure-division floating-point literal (D16 —
    /// binary64) and a VALUE on a FLOAT-SHORT / FLOAT-LONG / FLOAT-BINARY-32/64 item (kb/Work PB99).</summary>
    public static bool FitsBinaryFloat(string text, bool single = false)
    {
        string t = text.Trim();
        var style = System.Globalization.NumberStyles.Float;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        bool zeroLiteral = t.TrimStart('+', '-').Split(['E', 'e'])[0].All(c => c is '0' or '.');
        if (single)
        {
            if (!float.TryParse(t, style, inv, out float f) || float.IsInfinity(f)) return false;
            return f != 0f || zeroLiteral;
        }
        if (!double.TryParse(t, style, inv, out double d) || double.IsInfinity(d)) return false;
        return d != 0d || zeroLiteral;
    }

    /// <summary>The EXACT value of a canonical numeric literal of EITHER form as a significand and a power of ten
    /// (ISO §8.3.3.3.3 GR5 for the floating form; a fixed-point literal is its unscaled digits at 10^−scale) — the ONE
    /// exact parser the binder's range checks, the standard-decimal literal operand and the VALUE initializer share
    /// (kb/Work PB99). False for any non-numeric text; a significand wider than Int128 (never a legal literal —
    /// 36 digits fit) also returns false.</summary>
    public static bool TryParseExact(string text, out Int128 sig, out int exp10)
    {
        sig = 0; exp10 = 0;
        string t = text.Trim();
        int e = t.IndexOfAny(['E', 'e']);
        string mant = e < 0 ? t : t[..e];
        string ex = e < 0 ? "0" : t[(e + 1)..];
        bool neg = mant.StartsWith('-');
        mant = mant.TrimStart('+', '-');
        int dot = mant.IndexOf('.');
        string digits = dot < 0 ? mant : mant.Remove(dot, 1);
        int scale = dot < 0 ? 0 : mant.Length - dot - 1;
        if (digits.Length == 0 || digits.Length > 38 || !digits.All(char.IsAsciiDigit)) return false;
        if (!Int128.TryParse(digits, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var u)) return false;
        if (!int.TryParse(ex, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out int exp)) return false;
        sig = neg ? -u : u;
        exp10 = exp - scale;
        return true;
    }

    /// <summary>The EXACT value of a floating-point numeric literal (ISO §8.3.3.3.3 GR5: "the algebraic product of
    /// the value of its significand and the quantity derived by raising ten to the power of the exponent") as a
    /// canonical fixed-point literal text — <c>1.5E+3</c> → <c>1500</c>, <c>-1.234E-5</c> → <c>-0.00001234</c>,
    /// <c>0.0E+0</c> → <c>0.0</c>. The ONE expansion the VALUE-clause pipeline applies when a floating-point literal
    /// seeds a FIXED-POINT numeric subject (§13.18.63.3 SR2 — a numeric literal whose value shall be representable
    /// exactly, checked downstream on the expanded text). <paramref name="text"/> shall satisfy
    /// <see cref="IsFloatingPointForm"/>.</summary>
    public static string ExpandFloatingPoint(string text)
    {
        string t = text.Trim();
        int e = t.IndexOfAny(['E', 'e']);
        string sig = t[..e], exps = t[(e + 1)..];
        bool neg = sig.StartsWith('-');
        sig = sig.TrimStart('+', '-');
        int exp = int.Parse(exps, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture);
        int dot = sig.IndexOf('.');
        string digits = dot < 0 ? sig : sig.Remove(dot, 1);
        int scale = dot < 0 ? 0 : sig.Length - dot - 1;
        int newScale = scale - exp;                       // digits × 10^-scale × 10^exp = digits × 10^-(scale - exp)
        string body;
        if (newScale <= 0) body = digits + new string('0', -newScale);
        else if (newScale >= digits.Length) body = "0." + new string('0', newScale - digits.Length) + digits;
        else body = digits[..^newScale] + "." + digits[^newScale..];
        // canonical: no superfluous leading integer zeros (keep one before a point or alone), no trailing zeros
        // stripped (the fraction digits are the literal's scale — a VALUE 1.50E+0 keeps its two decimals like 1.50)
        int ip = body.IndexOf('.') is >= 0 and var d ? d : body.Length;
        int lead = 0;
        while (lead < ip - 1 && body[lead] == '0') lead++;
        body = body[lead..];
        return neg ? "-" + body : body;
    }
}
