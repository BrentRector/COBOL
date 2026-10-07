// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The STRING (ISO/IEC 1989:2023 §14.9.43) and UNSTRING (§14.9.48) runtime kernels over the typed-native string
/// substrate: values come in and go out as .NET <see cref="string"/>s; the POINTER item travels as a 1-based
/// <c>ref long</c>. One <see cref="StringTransfer"/> call per STRING sending operand; one
/// <see cref="UnstringExtract"/> call per UNSTRING receiving area — the emitter unrolls the operand lists and
/// stores the receivers back through the compiler's established store paths.
/// </summary>
public static class CobolStringOps
{
    /// <summary>
    /// Transfer ONE STRING sending operand into the receiver image (§14.9.43.4 GR3–GR8). The receiver travels as
    /// its full character image and only the touched positions change — no space filling is ever applied (GR3a /
    /// GR7: every untouched position keeps its prior content). A <see langword="null"/> or zero-length
    /// <paramref name="delimiter"/> means DELIMITED BY SIZE (SR9 / GR3c — a zero-length delimiter item behaves as
    /// SIZE); otherwise the transfer stops BEFORE the first occurrence of the delimiter in the sender, and the
    /// delimiter characters are not transferred (GR3b; a delimiter that never occurs — including one longer than
    /// the sender — moves the whole sender). Characters move one at a time at position <paramref name="pointer"/>,
    /// which increments after each character and is changed by nothing else (GR6); before EACH move, a pointer
    /// &lt; 1 or &gt; the receiver size sets <paramref name="overflow"/> and stops all transfer (GR8a/b — note the
    /// &lt; 1 arm: a zero/negative POINTER overflows without writing). An
    /// already-set <paramref name="overflow"/> short-circuits: GR8a terminates transfer for the REST of the
    /// statement, not just the current operand. A sending operand with nothing to move performs no pointer check
    /// (GR8 guards each character move — zero moves, zero checks, no overflow).
    /// </summary>
    public static string StringTransfer(string? dest, string? source, string? delimiter, ref long pointer, ref bool overflow)
    {
        dest ??= "";
        source ??= "";
        if (overflow) return dest;                                   // GR8a — no further data is transferred

        int take = source.Length;                                    // SIZE / no delimiter: the whole sender (GR3c)
        if (delimiter is { Length: > 0 })
        {
            int cut = source.IndexOf(delimiter, StringComparison.Ordinal);
            if (cut >= 0) take = cut;                                // GR3b — stop at, and exclude, the delimiter
        }
        if (take == 0) return dest;

        char[] chars = dest.ToCharArray();                           // GR7 — only referenced positions change
        for (int j = 0; j < take; j++)
        {
            if (pointer < 1 || pointer > dest.Length) { overflow = true; break; }   // GR8 — before each move
            chars[(int)pointer - 1] = source[j];
            pointer++;                                               // GR6 — incremented per character moved
        }
        return new string(chars);
    }

    /// <summary>The position in <paramref name="delimiters"/> of the delimiter an examination matched, given the
    /// occurrence <see cref="UnstringExtract"/> returned in its <c>delimiter</c> out-argument; −1 when that is empty
    /// (the examination ended at the end of the sender, so nothing was matched). Exact, not a guess: the kernel
    /// takes the earliest match and, on a same-position tie, the first listed, so an earlier entry holding the same
    /// text would have matched at the same position and been chosen instead — the first entry equal to the matched
    /// text IS the matched entry. Asked of the array the examination ran over, before anything overwrites it
    /// (GR18, docs/CONFORMANCE.md D-UNS2).</summary>
    public static int MatchedDelimiterIndex(string[] delimiters, string matched)
    {
        if (matched.Length == 0) return -1;
        for (int d = 0; d < delimiters.Length; d++)
            if (string.Equals(delimiters[d], matched, StringComparison.Ordinal)) return d;
        return -1;
    }

    /// <summary>
    /// Examine the UNSTRING sending field for ONE receiving area (§14.9.48.4 GR11) starting at the 1-based
    /// <paramref name="pointer"/> (GR11a; the caller has already established pointer ≥ 1 — the initiation
    /// range check is GR15a/GR16a, performed once per statement before any receiver). Returns the number of
    /// characters examined excluding delimiter characters (the COUNT IN value, GR11e / GR4), with the examined
    /// characters in <paramref name="field"/> (GR11c) and one occurrence of the matched delimiter in
    /// <paramref name="delimiter"/> (GR11d; empty when the delimiting condition was the end of the sender — the
    /// caller space-fills via the MOVE). Returns −1 when the sender is already exhausted: this receiver is NOT
    /// acted upon (GR11g ends the repetition; exhaustion alone is not an overflow, GR15) — the caller leaves the
    /// receiver, its DELIMITER IN / COUNT IN, and the tally untouched.
    /// <para>Delimiter selection (GR9/GR10): a zero-length delimiter is ignored; every delimiter is applied in
    /// statement order and the EARLIEST match in the sender wins, a same-position tie going to the first listed;
    /// a multi-character delimiter matches only contiguous, in-order characters; when ALL delimiters are
    /// zero-length the statement behaves as if DELIMITED were absent. With the ALL phrase, contiguous repeats of
    /// the matched delimiter are consumed as one delimiting occurrence (GR7). With no (effective) delimiters,
    /// exactly <paramref name="receiverSize"/> characters are examined — the receiving area's size in character
    /// positions, one less when its sign occupies a separate position (GR11b, computed by the binder) — or fewer
    /// when the sender ends first.</para>
    /// <para>On return <paramref name="pointer"/> has advanced by every character examined INCLUDING the consumed
    /// delimiter characters (GR13).</para>
    /// </summary>
    public static int UnstringExtract(
        string? source, string[] delimiters, bool[] allFlags, int receiverSize,
        ref long pointer, out string field, out string delimiter)
    {
        source ??= "";
        field = "";
        delimiter = "";
        int pos = (int)(pointer - 1);
        if (pos >= source.Length) return -1;                         // GR11g — sender exhausted: not acted upon

        bool anyDelim = false;                                       // GR9 — all zero-length ⇒ as if DELIMITED absent
        for (int d = 0; d < delimiters.Length; d++)
            if (delimiters[d].Length > 0) { anyDelim = true; break; }

        int extractLen;
        int consumed = 0;
        if (!anyDelim)
        {
            int size = receiverSize < 0 ? 0 : receiverSize;
            extractLen = Math.Min(size, source.Length - pos);        // GR11b — size-bounded examination
        }
        else
        {
            int best = -1, bestIdx = -1;
            for (int d = 0; d < delimiters.Length; d++)
            {
                string del = delimiters[d];
                if (del.Length == 0) continue;                       // GR9 — zero-length delimiter ignored
                int found = source.IndexOf(del, pos, StringComparison.Ordinal);
                if (found >= 0 && (best < 0 || found < best)) { best = found; bestIdx = d; }   // GR10 — earliest
            }                                                        // wins; strict < keeps the first listed on a tie
            if (best >= 0)
            {
                extractLen = best - pos;
                string del = delimiters[bestIdx];
                delimiter = del;                                     // ONE occurrence for DELIMITER IN (GR7 / GR11d)
                consumed = del.Length;
                if (bestIdx < allFlags.Length && allFlags[bestIdx])
                {
                    int skip = best + del.Length;                    // GR7 ALL — contiguous repeats act as one
                    while (skip + del.Length <= source.Length
                           && string.CompareOrdinal(source, skip, del, 0, del.Length) == 0)
                        skip += del.Length;
                    consumed = skip - best;
                }
            }
            else
                extractLen = source.Length - pos;                    // GR11b — sender end ends the examination
        }

        field = source.Substring(pos, extractLen);
        pointer = pos + extractLen + consumed + 1;                    // GR13 — per character examined, delimiters included
        return extractLen;                                            // GR11e / GR4 — delimiter characters excluded
    }
}
