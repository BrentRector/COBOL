// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace CobolNet.Runtime.Globalization;

/// <summary>
/// The ONE LC_TIME model (ISO/IEC 1989:2023 §8.2.1 — LC_TIME through ISO/IEC 9945:2009 clause 7; DESIGN-locale-facility
/// §4.7/§8; kb/Work PB2764): the resolved snapshot of one locale's date and time formatting, and THE renderer of
/// <c>d_fmt</c> and <c>t_fmt</c>, so that every LC_TIME string a COBOL result carries — the patterns, the date and time
/// separators, the AM/PM designators, the era, the day and month names, the decimal separator of a seconds fraction —
/// has passed through <see cref="LocaleFacts.NormalizeLocaleText"/> exactly once, here, before it is concatenated
/// (DETERMINATION L12: Unicode Cf removed, U+00A0/U+202F/U+2009 mapped to the plain space). It is the LC_TIME twin of
/// <see cref="MonetaryFacts"/>; <c>CobolLocale</c> (LOCALE-DATE, LOCALE-TIME, LOCALE-TIME-FROM-SECONDS) formats only
/// through it and never reads a <see cref="DateTimeFormatInfo"/> or <see cref="NumberFormatInfo"/> itself
/// (<c>LocaleTextNormalizationDriftTests</c> keeps that true).
/// <para>⚖ Why normalizing the PATTERN was not enough (kb/Work PB2764): .NET formats the pattern's <c>/</c> and
/// <c>:</c> with the culture's own <see cref="DateTimeFormatInfo.DateSeparator"/> / <see cref="DateTimeFormatInfo.TimeSeparator"/>
/// and its <c>tt</c> with the raw AM/PM designators. ICU puts U+200F RIGHT-TO-LEFT MARK in every ar-* date separator
/// (28 cultures) and U+00A0 inside <c>a. m.</c> / <c>p. m.</c> of nine es-* / yrl-* cultures, so a pattern that
/// <see cref="LocaleFacts.NormalizeSpacing"/> had already cleaned still produced those bytes through the separator and
/// the designator — the host-varying, character-position-consuming text DETERMINATION L10/L12 exist to remove.</para>
/// <para>⚖ DETERMINATION L13 (CONFORMANCE.md §4 item 5): the DATE rendered is the Gregorian date argument-1 names
/// (§15.52.3 r2: "a date in the same format as the year, month, and day returned in character positions 1 through 8
/// by the CURRENT-DATE function"; §15.52.4 r2: <c>d_fmt</c> is a FORMAT of that date, ISO/IEC 9945 strftime, which
/// formats a Gregorian broken-down time — its alternative-era formats are the separate <c>era_d_fmt</c>), never the
/// culture's own calendar: .NET's default calendar for fa-*, ps-*, ckb-IR, lrc, mzn, uz-Arab (Persian), th-* (Thai
/// Buddhist) and ar-SA (Umm al-Qura) made <c>LOCALE-DATE("20261008", fa-IR)</c> read 1405/7/16, a different date.
/// The renderer takes the year, month and day from the Gregorian date directly, and the day, month and era NAMES from
/// a Gregorian-calendar clone of the culture's format data.</para>
/// </summary>
internal sealed class TimeFacts
{
    /// <summary>The letters <see cref="Render"/> treats as DATE tokens. Every unquoted ASCII letter of every
    /// predefined culture's short date pattern is one of them (measured over the 889 cultures of .NET 10, and held
    /// there by <c>LocaleTextNormalizationDriftTests</c>), so no letter reaches a COBOL result as an unrendered
    /// literal.</summary>
    internal const string DateTokenLetters = "dMyg";

    /// <summary>The letters <see cref="Render"/> treats as TIME tokens (long time pattern; same census).</summary>
    internal const string TimeTokenLetters = "Hhmst";

    private readonly string _dateFormat;
    private readonly string _timeFormat;
    private readonly string _dateSeparator;
    private readonly string _timeSeparator;
    private readonly string _amDesignator;
    private readonly string _pmDesignator;
    private readonly string _decimalPoint;
    private readonly string _era;
    private readonly string[] _dayNames;
    private readonly string[] _abbreviatedDayNames;
    private readonly string[] _monthNames;
    private readonly string[] _abbreviatedMonthNames;

    private TimeFacts(LocaleFacts facts)
    {
        // The Gregorian clone: names and the era in the Gregorian calendar (see the L13 paragraph above). A culture
        // whose optional calendars omit Gregorian keeps its own — the setter then throws and the clone is unchanged.
        var dtf = (DateTimeFormatInfo)facts.DateTimeFormat.Clone();
        try { dtf.Calendar = new GregorianCalendar(); }
        catch (ArgumentOutOfRangeException) { }
        _dateFormat = facts.DateFormat;
        _timeFormat = facts.TimeFormat;
        _dateSeparator = LocaleFacts.NormalizeLocaleText(dtf.DateSeparator);
        _timeSeparator = LocaleFacts.NormalizeLocaleText(dtf.TimeSeparator);
        _amDesignator = LocaleFacts.NormalizeLocaleText(dtf.AMDesignator);
        _pmDesignator = LocaleFacts.NormalizeLocaleText(dtf.PMDesignator);
        _decimalPoint = LocaleFacts.NormalizeLocaleText(facts.NumberFormat.NumberDecimalSeparator);
        _era = LocaleFacts.NormalizeLocaleText(dtf.GetEraName(1));
        _dayNames = Normalize(dtf.DayNames);
        _abbreviatedDayNames = Normalize(dtf.AbbreviatedDayNames);
        _monthNames = Normalize(dtf.MonthNames);
        _abbreviatedMonthNames = Normalize(dtf.AbbreviatedMonthNames);
    }

    private static string[] Normalize(string[] names)
    {
        var result = new string[names.Length];
        for (int i = 0; i < names.Length; i++) result[i] = LocaleFacts.NormalizeLocaleText(names[i]);
        return result;
    }

    private static readonly ConditionalWeakTable<LocaleFacts, TimeFacts> s_cache = new();

    /// <summary>The LC_TIME snapshot of one locale's facts (cached per <see cref="LocaleFacts"/> instance).</summary>
    public static TimeFacts Of(LocaleFacts facts) => s_cache.GetValue(facts, static f => new TimeFacts(f));

    /// <summary>The date <paramref name="date"/> (a Gregorian date) rendered in <c>d_fmt</c> (§15.52.4 r2; L10 + L13).</summary>
    public string FormatDate(DateTime date) => FormatDate(_dateFormat, date);

    /// <summary>Render <paramref name="pattern"/> over <paramref name="date"/> with this locale's separators, names
    /// and era — the whole token vocabulary, including the name tokens no predefined culture's short date pattern
    /// uses today (the unit tests drive them here, so a future ICU release that starts using one finds it rendered).</summary>
    internal string FormatDate(string pattern, DateTime date) => Render(pattern, new Moment(date, 0, 0, 0, null, HasTime: false));

    /// <summary>The time of day rendered in <c>t_fmt</c> (§15.53.4 r2, §15.54.4 r2): hours, minutes and seconds, plus a
    /// <paramref name="fraction"/> of the seconds (the digits after the decimal point; null for none). Done over the
    /// pattern's tokens rather than through <see cref="DateTime"/> because the standard's values are WIDER than a
    /// DateTime can hold (§15.53.3 r3: hour 24, seconds up to 99 — a leap-second or end-of-day value renders as the
    /// number it is).</summary>
    public string FormatTime(int hh, int mm, int ss, string? fraction) =>
        Render(_timeFormat, new Moment(default, hh, mm, ss, fraction, HasTime: true));

    /// <summary>What a pattern renders: a date, a time of day, or (never both at once in the locale data) either.</summary>
    private readonly record struct Moment(DateTime Date, int Hour, int Minute, int Second, string? Fraction, bool HasTime);

    /// <summary>Render <paramref name="pattern"/> over its tokens. Date tokens: <c>d</c>/<c>dd</c> day of month,
    /// <c>ddd</c>/<c>dddd</c> weekday name, <c>M</c>/<c>MM</c> month number, <c>MMM</c>/<c>MMMM</c> month name,
    /// <c>y</c>… year, <c>g</c> the era. Time tokens: <c>H</c>/<c>HH</c> 0–24 hour, <c>h</c>/<c>hh</c> 12-hour clock (12
    /// for 0 and 12; hour 24 → 12 with the AM designator of the day's end), <c>m</c>/<c>mm</c>, <c>s</c>/<c>ss</c> (the
    /// fraction, when any, follows the seconds with the locale's decimal separator), <c>t</c>/<c>tt</c> the AM/PM
    /// designator. <c>/</c> is the date separator, <c>:</c> the time separator, quoted and escaped literals verbatim; a
    /// token whose field the moment does not carry (a time token in a date pattern) is a literal.</summary>
    private string Render(string pattern, in Moment m)
    {
        var sb = new StringBuilder(pattern.Length + 8);
        bool pm = m.Hour >= 12 && m.Hour < 24;
        int h12 = m.Hour % 12 == 0 ? 12 : m.Hour % 12;
        for (int i = 0; i < pattern.Length;)
        {
            char c = pattern[i];
            int run = 1;
            while (i + run < pattern.Length && pattern[i + run] == c) run++;
            switch (c)
            {
                case 'd' when !m.HasTime:
                    sb.Append(run switch
                    {
                        1 => m.Date.Day.ToString(CultureInfo.InvariantCulture),
                        2 => m.Date.Day.ToString("00", CultureInfo.InvariantCulture),
                        3 => _abbreviatedDayNames[(int)m.Date.DayOfWeek],
                        _ => _dayNames[(int)m.Date.DayOfWeek],
                    });
                    break;
                case 'M' when !m.HasTime:
                    sb.Append(run switch
                    {
                        1 => m.Date.Month.ToString(CultureInfo.InvariantCulture),
                        2 => m.Date.Month.ToString("00", CultureInfo.InvariantCulture),
                        3 => _abbreviatedMonthNames[m.Date.Month - 1],
                        _ => _monthNames[m.Date.Month - 1],
                    });
                    break;
                case 'y' when !m.HasTime:
                    sb.Append(run switch
                    {
                        1 => (m.Date.Year % 100).ToString(CultureInfo.InvariantCulture),
                        2 => (m.Date.Year % 100).ToString("00", CultureInfo.InvariantCulture),
                        _ => m.Date.Year.ToString(CultureInfo.InvariantCulture).PadLeft(run, '0'),
                    });
                    break;
                case 'g' when !m.HasTime: sb.Append(_era); break;
                case 'H' when m.HasTime: sb.Append(run >= 2 ? m.Hour.ToString("00", CultureInfo.InvariantCulture) : m.Hour.ToString(CultureInfo.InvariantCulture)); break;
                case 'h' when m.HasTime: sb.Append(run >= 2 ? h12.ToString("00", CultureInfo.InvariantCulture) : h12.ToString(CultureInfo.InvariantCulture)); break;
                case 'm' when m.HasTime: sb.Append(run >= 2 ? m.Minute.ToString("00", CultureInfo.InvariantCulture) : m.Minute.ToString(CultureInfo.InvariantCulture)); break;
                case 's' when m.HasTime:
                    sb.Append(run >= 2 ? m.Second.ToString("00", CultureInfo.InvariantCulture) : m.Second.ToString(CultureInfo.InvariantCulture));
                    if (m.Fraction is not null) sb.Append(_decimalPoint).Append(m.Fraction);
                    break;
                case 't' when m.HasTime:
                {
                    string des = pm ? _pmDesignator : _amDesignator;
                    sb.Append(run >= 2 ? des : des.Length > 0 ? des[..1] : "");
                    break;
                }
                case '/': sb.Append(_dateSeparator); run = 1; break;
                case ':': sb.Append(_timeSeparator); run = 1; break;
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
}
