// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Runtime;

/// <summary>
/// The ACCEPT system sources (ISO/IEC 1989:2023 §14.9.1): the Format 1 hardware-device read (an 80-character
/// card-image transfer from standard input, GR1–GR5) and the Format 2 temporal readings (DATE / DAY /
/// DAY-OF-WEEK / TIME as the conceptual unsigned-integer values of GR7–GR12). The COMPILED program calls these;
/// the emitter then stores the result per the statement's rules (device = aligned-left by size, GR3/GR4;
/// temporal = the MOVE rules, GR6).
/// </summary>
public static class AcceptSource
{
    /// <summary>The Format 1 transfer size (ISO §14.9.1.4 GR2 — implementor-defined per device): one
    /// 80-character card-image record, the NIST-proven convention of the legacy engine.</summary>
    public const int RecordSize = 80;

    /// <summary>The ONE clock read every temporal source makes (once per ACCEPT statement): the run unit's
    /// injectable <see cref="IClock"/> (DESIGN-runtime-library §2.7 — the former process-global mutable
    /// <c>Func&lt;DateTime&gt; Now</c> seam is deleted). The default <see cref="SystemClock"/> consults the
    /// <c>COBOLNET_CLOCK</c> environment variable so a test run can pin the clock ACROSS PROCESSES — the
    /// deterministic-clock path — falling back to the local system clock (§14.9.1.4 GR7: "the hardware clock
    /// provides the current date and time"). An in-process test sets <c>RunUnit.Current.Clock</c> instead.</summary>
    private static DateTime Now() => RunUnit.Current.Clock.Now().DateTime;

    /// <summary>DATE — the conceptual 6-digit unsigned integer YYMMDD (ISO §14.9.1.4 GR7).</summary>
    public static long Date()
    {
        DateTime n = Now();
        return n.Year % 100 * 10000L + n.Month * 100L + n.Day;
    }

    /// <summary>DATE YYYYMMDD — the conceptual 8-digit unsigned integer with a four-digit year
    /// (ISO §14.9.1.4 GR8; COBOL-2002+, edition-gated at bind time).</summary>
    public static long DateYYYYMMDD()
    {
        DateTime n = Now();
        return n.Year * 10000L + n.Month * 100L + n.Day;
    }

    /// <summary>DAY — the conceptual 5-digit unsigned integer YYDDD, DDD the day of year 001–366
    /// (ISO §14.9.1.4 GR9 — the Julian-style date).</summary>
    public static long Day()
    {
        DateTime n = Now();
        return n.Year % 100 * 1000L + n.DayOfYear;
    }

    /// <summary>DAY YYYYDDD — the conceptual 7-digit unsigned integer with a four-digit year
    /// (ISO §14.9.1.4 GR10; COBOL-2002+, edition-gated at bind time).</summary>
    public static long DayYYYYDDD()
    {
        DateTime n = Now();
        return n.Year * 1000L + n.DayOfYear;
    }

    /// <summary>TIME — the conceptual 8-digit unsigned integer HHMMSScc on the 24-hour clock: hours 00–23,
    /// minutes, seconds, and HUNDREDTHS of a second (ISO §14.9.1.4 GR11 — a system unable to provide fractional
    /// seconds would supply 00; this one derives them from the millisecond reading).</summary>
    public static long Time()
    {
        DateTime n = Now();
        return n.Hour * 1000000L + n.Minute * 10000L + n.Second * 100L + n.Millisecond / 10;
    }

    /// <summary>DAY-OF-WEEK — the conceptual 1-digit unsigned integer where 1 IS MONDAY … 7 is Sunday
    /// (ISO §14.9.1.4 GR12); .NET's <c>DayOfWeek</c> starts at Sunday=0, hence the +6 mod 7 remap.</summary>
    public static long DayOfWeek() => ((int)Now().DayOfWeek + 6) % 7 + 1;

    /// <summary>
    /// The Format 1 device transfer (ISO §14.9.1.4 GR1–GR5): read 80-character card-image records from standard
    /// input until <paramref name="width"/> characters are available, and return EXACTLY <paramref name="width"/>
    /// characters. An input line shorter than 80 characters is space-padded to the full record (the card image);
    /// a line LONGER than 80 characters is consecutive records, each padded (GR2 — the transfer size is one
    /// record, never one oversized line). A receiver no larger than one record consumes exactly ONE record
    /// (GR3 / GR4b — only the leftmost characters that fit are kept, the rest ignored); a larger receiver
    /// requests ADDITIONAL records until filled (GR4a — stored aligned left, the next transfer landing in the
    /// unoccupied positions); end-of-input leaves the unfilled remainder as spaces. A zero-length receiver
    /// still consumes one record and ignores ALL of its characters (GR4b's closing sentence).
    /// The standard input IS the device for both the FROM-omitted default (GR5) and the CONSOLE / SYSIN
    /// mnemonics (§12.3.7 — the implementor's device set; stdin redirection is the process-level seam).
    /// </summary>
    public static string Device(int width)
    {
        if (width <= 0)
        {
            // GR4b's closing sentence: "If identifier-1 references a zero-length item, all the characters of
            // the transferred data are ignored" — the transfer HAPPENS (one record is consumed) and every
            // character of it is ignored. The old early-return never read, leaving the record for the next
            // ACCEPT — a wrong answer one statement later.
            Console.In.ReadLine();
            return "";
        }
        var sb = new StringBuilder(Math.Max(width, RecordSize));
        while (sb.Length < width)
        {
            string? line = Console.In.ReadLine();
            if (line is null) break;                                       // EOF — the tail space-fills (GR4a)
            // GR2: one transfer is EXACTLY one 80-character card-image record. A shorter input line pads to
            // the record; a LONGER line is CONSECUTIVE records, each padded — never one oversized transfer
            // (the old whole-line append put line characters 81+ where record 2's padding belongs). Records
            // of this line beyond the receiver's remaining need are ignored (GR4b — no push-back exists).
            int records = Math.Max(1, (line.Length + RecordSize - 1) / RecordSize);
            for (int r = 0; r < records && sb.Length < width; r++)
                sb.Append(line.Substring(r * RecordSize, Math.Min(RecordSize, line.Length - r * RecordSize)).PadRight(RecordSize));
        }
        return sb.Length >= width ? sb.ToString(0, width) : sb.ToString().PadRight(width);
    }

    /// <summary>The Format 1 device transfer into a FLOATING-POINT receiver (COMP-1 / COMP-2 / FLOAT-*) — the
    /// ISO §14.9.1.4 GR1 conversion ("Any conversion of data required between the device and the data item
    /// referenced by identifier-1 is defined by the implementor"), documented as CONFORMANCE.md §7 DOC-A.1-1
    /// (kb/Work PB887). A float item has no character positions to size a window by (§13.16.3 SR8 — the PICTURE clause
    /// shall not be specified for a float-short / float-long / float-extended item), so the transfer is EXACTLY ONE record (GR2 — DOC-A.1-5, <see cref="RecordSize"/>), and its
    /// characters are read as the INVERSE of the DISPLAY conversion (DOC-A.1-56 — the invariant-culture
    /// shortest-round-trip image), which is the §15.69 NUMVAL-F argument format: optional sign, digits with an
    /// optional period decimal point, an optional <c>E</c>±exponent, leading/trailing spaces ignored. A record
    /// that does not conform (TEST-NUMVAL-F non-zero — an all-space record included) converts to ZERO and sets
    /// no exception condition, the float twin of the fixed-point arm's "a non-digit contributes no digit". The
    /// value is converted ONCE into the receiver's own format (ISO §14.6.8.3 rule 2, kb/Work PB1110): the correctly
    /// rounded binary64 <see cref="CobolIntrinsics.NumvalFDouble"/> returns, or for a binary32 receiver
    /// (<paramref name="single"/>) the correctly rounded binary32 <see cref="CobolIntrinsics.NumvalFSingle"/> returns,
    /// widened exactly so the emitter's <c>(float)</c> cast is not a second rounding.</summary>
    public static double DeviceFloat(bool single)
    {
        string record = Device(RecordSize);
        if (CobolIntrinsics.TestNumvalF(record) != 0) return 0d;
        return single ? CobolIntrinsics.NumvalFSingle(record) : CobolIntrinsics.NumvalFDouble(record);
    }

    /// <summary>The Format 1 device transfer into a BOOLEAN receiver (ISO §14.9.1.4 GR1 — conversion between
    /// the device and the data item is implementor-defined): each transferred character <c>'1'</c> converts to
    /// boolean one and EVERY other character (a <c>'0'</c>, a pad space, any other device character) to
    /// boolean zero, so the receiver's §13.18.40.4 GR14 <c>'0'</c>/<c>'1'</c> representation invariant holds
    /// for any input. SR1 does not exclude a boolean receiver, so this path is conforming source.</summary>
    public static string DeviceBoolean(int width)
    {
        string s = Device(width);
        return string.Create(s.Length, s, static (span, src) =>
        {
            for (int i = 0; i < span.Length; i++) span[i] = src[i] == '1' ? '1' : '0';
        });
    }
}
