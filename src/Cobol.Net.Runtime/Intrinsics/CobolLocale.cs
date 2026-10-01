// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Text;
using CobolNet.Runtime.Collation;
using CobolNet.Runtime.Exceptions;
using CobolNet.Runtime.Globalization;

namespace CobolNet.Runtime;

/// <summary>
/// The LOCALE intrinsic functions of ISO/IEC 1989:2023 Annex A.4.9 items 2–5 (DESIGN-locale-facility §4.7/§4.8;
/// kb/Work PB64 T4): <see cref="Compare"/> (§15.51 LOCALE-COMPARE), <see cref="Date"/> (§15.52 LOCALE-DATE),
/// <see cref="Time"/> (§15.53 LOCALE-TIME), <see cref="TimeFromSeconds"/> (§15.54 LOCALE-TIME-FROM-SECONDS). Each
/// takes the bound <c>localeTag</c> of an optional locale-name-1 (null = the locale CURRENT for the category —
/// §14.6.6 r7 LC_COLLATE for the comparison, r8 LC_TIME for the three time functions) and resolves it at USE:
/// an unavailable named locale is EC-LOCALE-MISSING (§15.51.4 r3 / §15.52.4 r1 / §15.53.4 r1 / §15.54.4 r1;
/// checking-gated per §14.6.13.1.1, the root locale's answer standing when checking is off); an available locale
/// whose LC_TIME content this environment lacks (no .NET culture data — <see cref="LocaleFacts.HasCultureData"/>)
/// is EC-LOCALE-INVALID (§8.2.1 "invalid or incomplete", the invariant format standing).
/// <para>LOCALE-COMPARE IS the locale-based relation comparison (<see cref="LocaleCollation"/> — §15.51.4 r2's
/// trailing-space truncation is §8.8.4.2.11 sentence 1 verbatim; r4 the cultural ordering) plus the sign→character
/// map of r5; it is deliberately NOT a second comparison implementation.</para>
/// </summary>
public static class CobolLocale
{
    /// <summary>⛔ THE SUBSTITUTED RESULT OF A REJECTED <c>LOCALE-DATE</c> / <c>LOCALE-TIME</c> /
    /// <c>LOCALE-TIME-FROM-SECONDS</c> IS <b>ONE SPACE</b>, NOT A ZERO-LENGTH VALUE (kb/Work PB470) — the number
    /// of character positions the three §15.3 rule 14 guards below hand back when EC-ARGUMENT-FUNCTION checking
    /// is off. The class and the length are decided HERE, once, and the guards cite this member.
    /// <para>⚖ The derivation, from docs/CONFORMANCE.md row <c>DOC-A.1-90</c>, which settles the class by asking
    /// what determines the returned item's LENGTH. For these three functions §15.52.4 / §15.53.4 / §15.54.4 rule
    /// 3 answer it identically — "The length of the returned value depends on the format indicated in the
    /// locale" — so the length derives from the LOCALE, which rejecting argument-1 leaves entirely intact. The
    /// row's zero-length class is every function whose returned length derives from the REJECTED argument, and
    /// by its own words it therefore does not reach here; the row's general clause does, and for an alphanumeric
    /// result (§15.52.1 / §15.53.1 / §15.54.1: "The function type is alphanumeric") that is SPACES.</para>
    /// <para>⚖ Why ONE space rather than the width the locale would have produced: DETERMINATION L10 renders
    /// d_fmt / t_fmt as the culture's own patterns (<c>LocaleFacts.DateFormat</c>, <c>LocaleFacts.TimeFormat</c>),
    /// whose rendered width varies with the VALUE as well as with the format — under <c>fr-FR</c>'s
    /// <c>dd/MM/yyyy</c> it is 10, under <c>en-US</c>'s <c>M/d/yyyy</c> it is 8 for 1 February and 10 for
    /// 31 December — so once the value is rejected the format alone fixes no width to fill. The standard answers
    /// exactly that shape itself, at §15.30.3 rule 1 for EXCEPTION-LOCATION — an alphanumeric function whose
    /// length is "based on its contents" (rule 2) returns "one alphanumeric space character" when the contents
    /// are absent — and the row adopts that answer here.</para>
    /// <para>⛔ Do NOT "simplify" this back to <c>return "";</c>. Zero length and one space are INDISTINGUISHABLE
    /// through a MOVE (§14.6.8.5 space-fills the receiver from a zero-length sender), which is why the defect
    /// survived from PB64 T4 to PB470 behind a green golden; they differ under DISPLAY (§14.9.11.4 GR1 transfers
    /// nothing for a zero-length operand) and under FUNCTION LENGTH (§15.50.4 r3), and
    /// <c>2014/pb470_locale_argument_substitute</c> measures both.</para></summary>
    private const int LocaleSubstitutePositions = 1;

    /// <summary>LOCALE-COMPARE (§15.51): '&lt;' / '=' / '&gt;' (r5; length 1, r6) for argument-1 against argument-2
    /// under the named (else current LC_COLLATE) locale's cultural ordering (r3/r4). Both arguments are UTF-16 text
    /// — r1's national conversion is the repertoire identity here.</summary>
    public static string Compare(string? a, string? b, string? localeTag)
    {
        var sequence = localeTag is null ? LocaleCollation.Current : Sequence(localeTag, "LOCALE-COMPARE");
        int c = sequence.Compare(a, b);   // §8.8.4.2.11 trimming + EC-LOCALE-INCOMPATIBLE live in the ONE carrier
        return c < 0 ? "<" : c > 0 ? ">" : "=";
    }

    /// <summary>LOCALE-DATE (§15.52): argument-1 — a date in CURRENT-DATE positions 1–8 form (YYYYMMDD, r1/r2; valid
    /// per §15.21's definition: a Gregorian date of year 1601 through 9999) — formatted per the locale's
    /// <c>d_fmt</c> (r2; L10: the culture's short date pattern). An invalid argument is EC-ARGUMENT-FUNCTION, and
    /// checking off the substituted result is ONE SPACE — see <see cref="LocaleSubstitutePositions"/>. The length
    /// depends on the locale (r3).</summary>
    public static string Date(string? argument, string? localeTag)
    {
        string s = (argument ?? "").TrimEnd(' ');
        if (s.Length != 8 || !IsDigits(s)
            || !TryDate(int.Parse(s.AsSpan(0, 4), CultureInfo.InvariantCulture), int.Parse(s.AsSpan(4, 2), CultureInfo.InvariantCulture),
                int.Parse(s.AsSpan(6, 2), CultureInfo.InvariantCulture), out var date))
        {
            return ExceptionState.ArgumentErrorSpaces(
                $"LOCALE-DATE argument-1 '{argument}' is not a valid date in CURRENT-DATE positions 1-8 form, YYYYMMDD (ISO §15.52.3 r1/r2)",
                LocaleSubstitutePositions);
        }
        var facts = Facts(localeTag, LocaleCategory.Time, "LOCALE-DATE");
        return date.ToString(facts.DateFormat, facts.DateTimeFormat);
    }

    /// <summary>LOCALE-TIME (§15.53): argument-1 — a time in CURRENT-DATE positions 9–14 form (HHMMSS, r1/r2) with
    /// THIS clause's own ranges (r3: hours 00–24, seconds 00–99 — wider than CURRENT-DATE's, so not its validator)
    /// — formatted per the locale's <c>t_fmt</c> (r2; L10: the culture's long time pattern, hours + minutes +
    /// seconds). An invalid argument is EC-ARGUMENT-FUNCTION, and checking off the substituted result is ONE SPACE
    /// — see <see cref="LocaleSubstitutePositions"/>.</summary>
    public static string Time(string? argument, string? localeTag)
    {
        string s = (argument ?? "").TrimEnd(' ');
        if (s.Length != 6 || !IsDigits(s))
        {
            return ExceptionState.ArgumentErrorSpaces(
                $"LOCALE-TIME argument-1 '{argument}' is not a time in CURRENT-DATE positions 9-14 form, HHMMSS (ISO §15.53.3 r1/r2)",
                LocaleSubstitutePositions);
        }
        int hh = int.Parse(s.AsSpan(0, 2), CultureInfo.InvariantCulture), mm = int.Parse(s.AsSpan(2, 2), CultureInfo.InvariantCulture), ss = int.Parse(s.AsSpan(4, 2), CultureInfo.InvariantCulture);
        if (hh > 24 || mm > 59 || ss > 99)
        {
            return ExceptionState.ArgumentErrorSpaces(
                $"LOCALE-TIME argument-1 '{argument}': hours shall be 00 through 24, minutes 00 through 59, seconds 00 through 99 (ISO §15.53.3 r3)",
                LocaleSubstitutePositions);
        }
        var facts = Facts(localeTag, LocaleCategory.Time, "LOCALE-TIME");
        return FormatTime(facts, hh, mm, ss, fraction: null);
    }

    /// <summary>LOCALE-TIME-FROM-SECONDS (§15.54): argument-1 — seconds past midnight in standard numeric time form
    /// (r1; §7.3.17's range, the same exact Int128 screen FORMATTED-TIME uses), as an unscaled value and scale —
    /// formatted per the locale's <c>t_fmt</c>. A nonzero scale carries its fraction into the seconds (Annex D.31.4.5's
    /// nanosecond note — a determination, D being informative), trimmed to the argument's scale.</summary>
    public static string TimeFromSeconds(Int128 secUnscaled, int secScale, string? localeTag, bool leapSecond = false)
    {
        // The predicate has already raised EC-ARGUMENT-FUNCTION (§15.54.3 r1 through §7.3.17.4); this arm owes only
        // the substituted result, and it reads the CLASS rather than spelling a literal (see LocaleSubstitutePositions).
        if (CobolDate.SecondsOutOfStandardFormFor("LOCALE-TIME-FROM-SECONDS", secUnscaled, secScale, leapSecond))
            return ArgumentSubstitute.Spaces(LocaleSubstitutePositions);
        Int128 pow = Pow10.AsWide(secScale);
        long whole = (long)(secUnscaled / pow);
        Int128 frac = secUnscaled % pow;
        // §15.54.4 r2 — "a character-string containing hours, minutes, and seconds of the time specified by
        // argument-1" (§15.3.3.3: the seconds subfield is up to 60 under LEAP-SECOND ON): the ONE reading of a
        // whole-seconds value as a time of day, which under LEAP-SECOND ON makes 86 400 the leap second 23:59:60
        // (kb/Work PB1379 — it was 24:00:00, a time the day does not have, rendered "12:00:00 AM" in en-US).
        var (hh, mm, ss) = CobolDate.TimeOfDay(whole, leapSecond);
        string? fraction = secScale > 0 ? frac.ToString().PadLeft(secScale, '0') : null;
        var facts = Facts(localeTag, LocaleCategory.Time, "LOCALE-TIME-FROM-SECONDS");
        return FormatTime(facts, hh, mm, ss, fraction);
    }

    // ── UPPER-CASE / LOWER-CASE (§15.97 / §15.57; A.4.9 items 13 / 6; T5) ───────────────────────────────────────

    /// <summary>UPPER-CASE with a LOCALE phrase (§15.97.4 r2 — "the correspondence of lowercase to uppercase letters is
    /// determined from locale category LC_CTYPE in the locale associated with locale-name-1"): the named locale's
    /// culture case mapping. ⚖ DETERMINATION L9: the mapping is SIMPLE (one code unit to one — <see cref="TextInfo"/>),
    /// which §15.97.4 r5 admits and ISO 9945's LC_CTYPE toupper is; a letter without an uppercase correspondence is
    /// unchanged (r6). EC-LOCALE-MISSING / EC-LOCALE-INVALID as the other LOCALE functions.</summary>
    public static string UpperCase(string? s, string localeTag) => UpperCase(s, Facts(localeTag, LocaleCategory.Ctype, "UPPER-CASE"));

    /// <summary>LOWER-CASE with a LOCALE phrase (§15.57.4 r2) — the named locale's LC_CTYPE; L9 simple mapping; r6.</summary>
    public static string LowerCase(string? s, string localeTag) => LowerCase(s, Facts(localeTag, LocaleCategory.Ctype, "LOWER-CASE"));

    /// <summary>UPPER-CASE under the module's CHARACTER CLASSIFICATION (§15.97.4 r3 — a locale in effect for character
    /// classification; §12.3.6.4 GR7a): <paramref name="facts"/> null is the coded character set's correspondence —
    /// the implementor's (r4), <see cref="string.ToUpperInvariant"/> — exactly what the function without any locale
    /// did before T5.</summary>
    public static string UpperCase(string? s, LocaleFacts? facts)
    {
        facts = facts?.Require(LocaleCategory.Ctype, "FUNCTION UPPER-CASE under the CHARACTER CLASSIFICATION", "ISO §15.97.4 r3 / §12.3.6.4 GR7a");
        return facts is null || !facts.HasCultureData ? (s ?? "").ToUpperInvariant() : facts.TextInfo.ToUpper(s ?? "");
    }

    /// <summary>LOWER-CASE under the module's CHARACTER CLASSIFICATION (§15.57.4 r3; r4 when none).</summary>
    public static string LowerCase(string? s, LocaleFacts? facts)
    {
        facts = facts?.Require(LocaleCategory.Ctype, "FUNCTION LOWER-CASE under the CHARACTER CLASSIFICATION", "ISO §15.57.4 r3 / §12.3.6.4 GR7a");
        return facts is null || !facts.HasCultureData ? (s ?? "").ToLowerInvariant() : facts.TextInfo.ToLower(s ?? "");
    }

    // ── resolution ─────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The named locale's collating sequence (§15.51.4 r3): EC-LOCALE-MISSING when unavailable — raised by
    /// the sequence itself at use (<see cref="LocaleCollation.Resolve"/>).</summary>
    private static LocaleCollation Sequence(string localeTag, string fn) => new(localeTag);

    /// <summary>The facts of the named (else current) locale for <paramref name="category"/>: EC-LOCALE-MISSING for an
    /// unavailable named locale (the root's facts stand when checking is off); EC-LOCALE-INVALID when the locale has
    /// no culture data for the category (the invariant facts stand).</summary>
    /// <summary>The facts of a function's locale — locale-name-1's when given, else the locale CURRENT for the category
    /// (§14.6.6 r7/r8) — through the ONE §8.2.1 gate (<see cref="LocaleFacts.Require"/>): EC-LOCALE-MISSING for an
    /// unavailable named locale (the rule each function states — §15.51.4 r3, §15.52.4 r1, §15.53.4 r1, §15.54.4 r1;
    /// §15.97.4 r2 / §15.57.4 r2 name the locale without restating the condition), the ROOT's answer standing when
    /// checking is off; EC-LOCALE-INVALID for incomplete content.</summary>
    private static LocaleFacts Facts(string? localeTag, LocaleCategory category, string fn)
    {
        string tag = localeTag ?? RunUnit.Current.Locale.Current(category);
        string rule = fn switch
        {
            "LOCALE-COMPARE" => "ISO §15.51.4 r3",
            "LOCALE-DATE" => "ISO §15.52.4 r1",
            "LOCALE-TIME" => "ISO §15.53.4 r1",
            "LOCALE-TIME-FROM-SECONDS" => "ISO §15.54.4 r1",
            "UPPER-CASE" => "ISO §15.97.4 r2",
            "LOWER-CASE" => "ISO §15.57.4 r2",
            _ => "ISO §8.2.1",
        };
        return LocaleFacts.For(tag).Require(category, $"FUNCTION {fn}{(localeTag is not null ? " (locale-name-1)" : "")}", rule) ?? LocaleFacts.Root;
    }

    // ── t_fmt rendering ────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Render hours/minutes/seconds per the culture's <c>t_fmt</c> (the long time pattern). Done over the
    /// pattern's tokens rather than through <see cref="DateTime"/> because the standard's values are WIDER than a
    /// DateTime can hold (§15.53.3 r3: hour 24, seconds up to 99 — a leap-second or end-of-day value renders as
    /// the number it is). Tokens: <c>H</c>/<c>HH</c> 0–24 hour, <c>h</c>/<c>hh</c> 12-hour clock (12 for 0 and 12;
    /// hour 24 → 12 with the AM designator of the day's end), <c>m</c>/<c>mm</c>, <c>s</c>/<c>ss</c> (the fraction,
    /// when any, follows the seconds with the culture's decimal separator), <c>t</c>/<c>tt</c> the AM/PM designator,
    /// <c>:</c> the culture's time separator, quoted and escaped literals verbatim.</summary>
    internal static string FormatTime(LocaleFacts facts, int hh, int mm, int ss, string? fraction)
    {
        var dtf = facts.DateTimeFormat;
        string pattern = facts.TimeFormat;
        var sb = new StringBuilder(pattern.Length + 8);
        bool pm = hh >= 12 && hh < 24;
        int h12 = hh % 12 == 0 ? 12 : hh % 12;
        for (int i = 0; i < pattern.Length;)
        {
            char c = pattern[i];
            int run = 1;
            while (i + run < pattern.Length && pattern[i + run] == c) run++;
            switch (c)
            {
                case 'H': sb.Append(run >= 2 ? hh.ToString("00", CultureInfo.InvariantCulture) : hh.ToString(CultureInfo.InvariantCulture)); break;
                case 'h': sb.Append(run >= 2 ? h12.ToString("00", CultureInfo.InvariantCulture) : h12.ToString(CultureInfo.InvariantCulture)); break;
                case 'm': sb.Append(run >= 2 ? mm.ToString("00", CultureInfo.InvariantCulture) : mm.ToString(CultureInfo.InvariantCulture)); break;
                case 's':
                    sb.Append(run >= 2 ? ss.ToString("00", CultureInfo.InvariantCulture) : ss.ToString(CultureInfo.InvariantCulture));
                    if (fraction is not null) sb.Append(facts.NumberFormat.NumberDecimalSeparator).Append(fraction);
                    break;
                case 't':
                {
                    string des = pm ? dtf.PMDesignator : dtf.AMDesignator;
                    sb.Append(run >= 2 ? des : des.Length > 0 ? des[..1] : "");
                    break;
                }
                case ':': sb.Append(dtf.TimeSeparator); run = 1; break;
                case '\'':
                case '"':
                {
                    int close = pattern.IndexOf(c, i + 1);
                    if (close < 0) close = pattern.Length;
                    sb.Append(pattern, i + 1, close - i - 1);
                    i = close + 1;
                    continue;
                }
                case '\\':
                    if (i + 1 < pattern.Length) sb.Append(pattern[i + 1]);
                    i += 2;
                    continue;
                default:
                    sb.Append(c, run);
                    break;
            }
            i += run;
        }
        return sb.ToString();
    }

    private static bool IsDigits(string s)
    {
        foreach (char c in s) if (c is < '0' or > '9') return false;
        return true;
    }

    private static bool TryDate(int year, int month, int day, out DateTime date)
    {
        date = default;
        if (year is < 1601 or > 9999 || month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)) return false;
        date = new DateTime(year, month, day);
        return true;
    }
}
