// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The OVER-PUNCH CONVENTION a compiled program uses for the operational sign of a USAGE DISPLAY item whose sign
/// is NOT written <c>SEPARATE CHARACTER</c> — the compile option <c>--sign-encoding</c>, and this implementation's
/// answer to Annex A.1 items <b>177</b> (representation when the picture contains 'S' and no SIGN clause applies)
/// and <b>178</b> (what constitutes a valid sign when SEPARATE CHARACTER is absent). kb/Work PB803, owner decision
/// 2026-09-09: <i>"Keep both behind an option with the default being IBM and Micro Focus compatibility."</i>
/// <para>ISO/IEC 1989:2023 §13.18.52.4 GR4 — "Neither the representation nor the position of that operational sign
/// is specified by the symbol 'S'. The implementor shall specify the position and representation of the operational
/// sign." — and GR5 b) — "The implementor defines what constitutes valid signs for data items." SEPARATE CHARACTER
/// carries NO latitude (GR6 b) pins '+' and '-'), so no member of this enum reaches a SEPARATE item.</para>
/// <para>⛔ The convention is a property of the COMPILED PROGRAM, never of the process: it is baked into every
/// emitted <see cref="NumProfile"/> and into the class-condition call the emitter renders, so two programs of one
/// run unit compiled under different options interoperate exactly as two vendors' programs do — each reads storage
/// by its own convention. A data file's convention is whatever wrote it.</para>
/// </summary>
public enum SignEncoding
{
    /// <summary>THE DEFAULT. The IBM / Micro Focus zoned-decimal convention as it lands on an ASCII host: the
    /// positive digits 0–9 fuse to <c>{ A B C D E F G H I</c> and the negative digits to
    /// <c>} J K L M N O P Q R</c>. What IBM Enterprise COBOL, Micro Focus and GnuCOBOL's <c>-fsign=ebcdic</c> all
    /// write, so a data file interchanges with them. (The name is <c>Ibm</c>, not "EBCDIC": nothing about the
    /// characters is EBCDIC-encoded on an ASCII host — they are what an EBCDIC-descended CONVENTION produces after
    /// translation, and calling it EBCDIC is the misnomer that would make the option unreadable.)</summary>
    Ibm = 0,

    /// <summary>The pure-ASCII convention — GnuCOBOL's <c>-fsign=ascii</c>, its default on a PC host: a POSITIVE
    /// digit is left alone (<c>0</c>–<c>9</c>) and a NEGATIVE digit is <c>digit + 0x40</c> (<c>p</c>–<c>y</c>). So
    /// <c>PIC S9(4)</c> holding −1234 is <c>123t</c>, and under SIGN LEADING <c>q234</c>.</summary>
    Ascii = 1,
}

/// <summary>One convention's two sign tables: the character each digit 0–9 becomes when the item's operational sign
/// is positive, and when it is negative. Positions are the DIGIT, so <c>Positive[d]</c> / <c>Negative[d]</c> is the
/// punched character for digit <paramref name="d"/>. Both strings are exactly 10 characters.</summary>
/// <param name="Positive">The ten characters for +0 … +9.</param>
/// <param name="Negative">The ten characters for −0 … −9.</param>
public readonly record struct SignPunchTable(string Positive, string Negative);

/// <summary>
/// ⛔ THE ONE PLACE EITHER OVER-PUNCH TABLE IS WRITTEN (kb/Work PB803). The encode (<see cref="Punch"/>), the decode
/// (<see cref="TryUnpunch"/>) and the NUMERIC class condition's valid-sign set (<see cref="IsSignCharacter"/>) are all
/// derived from <see cref="Table"/>, so they cannot disagree — which the previous shape could not promise: the tables
/// lived in <c>CobolNum</c> while the class test's valid set was a separate hand-written character-range pattern in
/// <c>CobolClass</c>, a second copy of the same rule.
/// <para><b>Adding a third convention is a ROW plus an enum member.</b> Nothing below names a convention; every
/// operation indexes <see cref="Tables"/> by the enum, and <c>ZonedSignTableDriftTests</c> iterates
/// <see cref="System.Enum.GetValues{TEnum}"/> so a new member is checked for completeness, distinctness and
/// digit-disjointness the moment it exists.</para>
/// </summary>
public static class ZonedSign
{
    /// <summary>The tables, INDEXED BY <see cref="SignEncoding"/>. Order is the enum's, and the drift test pins
    /// the length against the enum so a member added without a row fails the build rather than throwing at run
    /// time on the one program that selects it.</summary>
    private static readonly SignPunchTable[] Tables =
    [
        // SignEncoding.Ibm — A.1 item 177/178 default. NIST-verified and matched
        // against GnuCOBOL's `DISPLAY: Sign EBCDIC` expectation `{ABCDEFGHI}JKLMNOPQR`.
        new("{ABCDEFGHI", "}JKLMNOPQR"),
        // SignEncoding.Ascii — GnuCOBOL's `DISPLAY: Sign ASCII (2)` expectation `0123456789pqrstuvwxy`:
        // the positive digit is untouched, the negative digit is digit + 0x40.
        new("0123456789", "pqrstuvwxy"),
    ];

    /// <summary>How many conventions exist — the drift test's cross-check against the enum.</summary>
    internal static int TableCount => Tables.Length;

    /// <summary>The two tables of <paramref name="encoding"/>.</summary>
    public static SignPunchTable Table(SignEncoding encoding) => Tables[(int)encoding];

    /// <summary>The character that digit <paramref name="digit"/> (0–9) becomes when it carries the operational
    /// sign — §13.18.52.4 GR4 / GR5 a): the sign fuses ONTO an existing digit position and adds no character
    /// position. Callers guarantee the range; a non-digit never reaches here.</summary>
    public static char Punch(SignEncoding encoding, int digit, bool negative)
    {
        var t = Tables[(int)encoding];
        return (negative ? t.Negative : t.Positive)[digit];
    }

    /// <summary>The inverse of <see cref="Punch"/>: recover the digit and the sign a punched character carries.
    /// Returns false for a character that is not in EITHER table of <paramref name="encoding"/> — including a plain
    /// digit under a convention whose positive table is not the digits (the <see cref="SignEncoding.Ibm"/> case),
    /// which the caller treats as an unpunched positive digit exactly as it always did.
    /// <para>The POSITIVE table is searched first, so under a convention whose positive table IS the digits
    /// (<see cref="SignEncoding.Ascii"/>) a plain digit decodes as positive by the table rather than by a fallback —
    /// one path, not two.</para></summary>
    public static bool TryUnpunch(SignEncoding encoding, char c, out int digit, out bool negative)
    {
        var t = Tables[(int)encoding];
        int p = t.Positive.IndexOf(c);
        if (p >= 0) { digit = p; negative = false; return true; }
        int n = t.Negative.IndexOf(c);
        if (n >= 0) { digit = n; negative = true; return true; }
        digit = 0;
        negative = false;
        return false;
    }

    /// <summary>§13.18.52.4 GR5 b) — "The implementor defines what constitutes valid signs for data items", the set
    /// §8.8.4.4.4 GR3 n)1.a's closing sentence delegates here ("Valid operational signs are defined in 13.18.52,
    /// SIGN clause"): true when <paramref name="c"/> may stand at the SIGN POSITION of an unseparated signed DISPLAY
    /// item under <paramref name="encoding"/> — a plain digit (an item whose sign was never punched, e.g. one filled
    /// by a group MOVE), or any character of either table. Every OTHER character position must be a plain digit,
    /// which is the caller's half of n)1.a.</summary>
    public static bool IsSignCharacter(SignEncoding encoding, char c) =>
        c is >= '0' and <= '9' || TryUnpunch(encoding, c, out _, out _);

    /// <summary>The accepted spellings of <c>--sign-encoding</c>, in enum order — DERIVED from the enum's own member
    /// names lower-cased, so a convention added to <see cref="SignEncoding"/> brings its spelling, its help text and
    /// its acceptance with it rather than needing three hand-kept lists.</summary>
    public static IReadOnlyList<string> OptionSpellings { get; } = BuildSpellings();

    private static string[] BuildSpellings()
    {
        string[] names = Enum.GetNames<SignEncoding>();
        var spellings = new string[names.Length];
        for (int i = 0; i < names.Length; i++) spellings[i] = names[i].ToLowerInvariant();
        return spellings;
    }

    /// <summary>⛔ THE ONE MAPPING between an option's TEXT and a convention (kb/Work PB803) — used by the CLI's
    /// <c>--sign-encoding</c> validator, by its resolver, and by the conformance corpus's per-golden
    /// <c>*&gt; options:</c> header, so those three can never disagree about what <c>ascii</c> means.
    /// Case-insensitive; a numeric spelling is REFUSED (<c>Enum.TryParse</c> alone would accept <c>"1"</c>, which
    /// is an internal ordinal and not part of the option's contract).</summary>
    public static bool TryParseOption(string? text, out SignEncoding encoding)
    {
        encoding = SignEncoding.Ibm;
        if (text is null) return false;
        string[] names = Enum.GetNames<SignEncoding>();
        SignEncoding[] values = Enum.GetValues<SignEncoding>();
        for (int i = 0; i < names.Length; i++)
        {
            if (!string.Equals(names[i], text, StringComparison.OrdinalIgnoreCase)) continue;
            encoding = values[i];
            return true;
        }
        return false;
    }
}
