// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// INSPECT TALLYING / REPLACING / CONVERTING over a field's character image (ISO/IEC 1989:2023 §14.9.22). The
/// emitter reads identifier-1's image once (GR4/GR6 — a signed numeric item is de-signed per GR4d and re-signed on
/// the store), calls <see cref="Tally"/> and/or <see cref="Replace"/>/<see cref="Convert"/>, and stores the result
/// back through the item's <c>Place</c>.
/// <para>TALLYING and REPLACING each execute as ONE shared comparison cycle over the statement's ordered operands
/// (§14.9.22.4 GR8), and the two share ONE engine (<see cref="RunCycle"/>): at each position the operands are tried
/// in source order (GR8a); the first match tallies/replaces, the scan moves on (GR8c) and the cycle restarts from
/// the first operand; no match moves the scan one position (GR8b); the cycle ends when the last character has
/// participated or been considered (GR8d). CHARACTERS is an implied always-matching one-character operand (GR8e).
/// This shared cycle is why an earlier <c>ALL "A"</c> starves a later <c>LEADING "AH"</c> to zero — the leading
/// 'A' is consumed before the LEADING operand is ever tried.</para>
/// <para>⛔ BACKWARD IS A DIRECTION OF THE SCAN, NEVER OF THE MATCHING (§14.9.22.4 GR8 NOTE 2, kb/Work PB1152).
/// The current position walks from the rightmost character to the leftmost, and at each one a literal is compared
/// LEFT-TO-RIGHT starting at that position — exactly as forward. The texts and the patterns are never reversed,
/// because a reversed pattern matches the literal ENDING at the current position, which is the wrong rule (Annex
/// D.25 EXAMPLE 5 row 1 prints COUNT-1 = 1 where the reversed scan gave 0). Forward and backward differ in exactly
/// three places, all here: where the scan starts, how far a match moves it (GR8c — past the matched characters
/// forward, one position left of the match's leftmost character backward), and where a BEFORE/AFTER delimiter is
/// first ENCOUNTERED (<see cref="Region"/>).</para>
/// </summary>
public static class CobolInspect
{
    /// <summary>TALLYING operand kinds (§14.9.22.2 Format 1) — the binder's <c>InspectTallyKind</c> ordinals.</summary>
    public const int TallyAll = 0, TallyLeading = 1, TallyCharacters = 2;

    /// <summary>REPLACING operand kinds (§14.9.22.2 Format 2) — the binder's <c>InspectReplaceKind</c> ordinals.</summary>
    public const int ReplaceAll = 0, ReplaceFirst = 1, ReplaceLeading = 2, ReplaceCharacters = 3;

    /// <summary>The adjective an operand carries in the ONE comparison cycle — TALLYING's and REPLACING's kind
    /// ordinals both normalize to it, so the cycle is written once.</summary>
    private enum Adjective { All, First, Leading, Characters }

    private static Adjective[] TallyAdjectives(int[] kinds) =>
        Array.ConvertAll(kinds, k => k switch
        {
            TallyAll => Adjective.All,
            TallyLeading => Adjective.Leading,
            _ => Adjective.Characters,
        });

    private static Adjective[] ReplaceAdjectives(int[] kinds) =>
        Array.ConvertAll(kinds, k => k switch
        {
            ReplaceAll => Adjective.All,
            ReplaceFirst => Adjective.First,
            ReplaceLeading => Adjective.Leading,
            _ => Adjective.Characters,
        });

    /// <summary>A matched operand at a position: which operand, where its leftmost matched character is, how many
    /// characters matched (1 for CHARACTERS).</summary>
    private delegate void MatchSink(int operand, int position, int length);

    /// <summary>
    /// One operand's eligible region [Start, End) in the text's OWN coordinates — fixed BEFORE the first comparison
    /// cycle (§14.9.22.4 GR9), and a match [p, p + length) is eligible only when it lies wholly inside it.
    /// <para>FORWARD — AFTER: eligibility starts immediately past the FIRST occurrence of its delimiter; absent ⇒
    /// the operand is NEVER eligible (GR9c — an empty region). BEFORE: eligibility ends at the first occurrence of
    /// its delimiter ENCOUNTERED FROM the AFTER-derived start; absent ⇒ as if BEFORE were not specified (GR9b — the
    /// asymmetry with AFTER is deliberate spec text).</para>
    /// <para>BACKWARD — the same two rules with "first occurrence encountered" read in the direction of the scan
    /// (GR3 NOTE 1; GR9b, GR9c 1): the delimiter is compared left-to-right at each position (GR8 NOTE 2), so its
    /// first occurrence encountered is the RIGHTMOST one. AFTER: eligibility runs from the position immediately
    /// left of that occurrence down to the leftmost position — the region is [0, p). BEFORE: from the first
    /// eligible position (the AFTER-derived right edge) down to, not including, the rightmost occurrence lying
    /// wholly inside the remainder — the region is [p + length, AFTER's edge).</para>
    /// </summary>
    private static (int Start, int End) Region(string text, string? before, string? after, bool backward)
    {
        int start = 0;
        int end = text.Length;
        if (backward)
        {
            if (after is { Length: > 0 })
            {
                int idx = text.AsSpan().LastIndexOf(after.AsSpan());   // ordinal; the rightmost occurrence
                if (idx < 0) return (0, 0);                 // not found ⇒ never eligible (GR9c)
                end = idx;
            }
            if (before is { Length: > 0 })
            {
                int idx = text.AsSpan(0, end).LastIndexOf(before.AsSpan());   // wholly inside the AFTER-derived remainder
                if (idx >= 0) start = idx + before.Length;      // not found ⇒ whole remainder (GR9b)
            }
            return (start, end);
        }
        if (after is { Length: > 0 })
        {
            int idx = text.IndexOf(after, StringComparison.Ordinal);
            start = idx >= 0 ? idx + after.Length : end;   // not found ⇒ never eligible (GR9c)
        }
        if (before is { Length: > 0 })
        {
            int idx = text.IndexOf(before, start, StringComparison.Ordinal);
            if (idx >= 0) end = idx;                        // not found ⇒ whole remainder (GR9b)
        }
        return start > end ? (end, end) : (start, end);
    }

    /// <summary>
    /// ⛔ THE ONE COMPARISON CYCLE (§14.9.22.4 GR8/GR12/GR17), shared by TALLYING and REPLACING, forward and
    /// backward. <paramref name="sink"/> is told each hit; comparison always reads the ORIGINAL text
    /// <paramref name="t"/> (every BEFORE/AFTER region is fixed from it before the first cycle too — GR9), so what the
    /// sink writes can never create or destroy a later hit. BACKWARD a later comparison can overlap an earlier hit
    /// (GR8c resumes one position left of the hit's leftmost character and NOTE 2 compares rightward from there);
    /// both read the original and the LATER write wins the shared characters — Annex D.25 EXAMPLE 5 row 3 prints
    /// exactly that (BCABCABD → BCABCXYD: LEADING "B" writes position 6, then ALL "AB" at 5 overwrites it). An
    /// ALL/FIRST/LEADING match requires the WHOLE
    /// pattern inside the operand's region (GR9 — eligibility covers the cycle's compared characters), a
    /// CHARACTERS operand only the current position. <paramref name="never"/>[k] marks an operand that cannot
    /// match at all (a zero-length pattern; a REPLACING pair the runtime must treat as never matching — GR14).
    /// </summary>
    private static void RunCycle(
        string t, bool backward, Adjective[] adjectives, string?[] patterns, string?[] befores, string?[] afters,
        bool[] never, MatchSink sink)
    {
        int n = adjectives.Length;
        var regionStart = new int[n];
        var regionEnd = new int[n];
        var live = new bool[n];        // FIRST/LEADING: still eligible (GR12b, GR17c/d)
        var expectedPos = new int[n];  // LEADING: the position the next contiguous match must occupy (-1 = not started)
        for (int k = 0; k < n; k++)
        {
            (regionStart[k], regionEnd[k]) = Region(t, befores[k], afters[k], backward);
            live[k] = true;
            expectedPos[k] = -1;
        }

        int pos = backward ? t.Length - 1 : 0;
        while (backward ? pos >= 0 : pos < t.Length)
        {
            bool matched = false;
            OpenLeadingAnchors(pos, adjectives, regionStart, regionEnd, expectedPos);
            for (int k = 0; k < n; k++)
            {
                bool inRegion = pos >= regionStart[k] && pos < regionEnd[k];

                if (adjectives[k] == Adjective.Characters)
                {
                    if (!inRegion) continue;   // ineligible on this cycle ⇒ considered not to match (GR9b/c)
                    sink(k, pos, 1);           // GR12c/GR17a — one per character matched in the GR8e sense
                    pos += backward ? -1 : 1;
                    matched = true;
                    break;
                }

                if (never[k]) continue;
                string pat = patterns[k]!;
                bool isMatch = inRegion && pos + pat.Length <= regionEnd[k]
                    && t.AsSpan(pos, pat.Length).SequenceEqual(pat.AsSpan());

                if (adjectives[k] == Adjective.First)
                {
                    if (!live[k] || !isMatch) continue;
                    live[k] = false;           // only the first occurrence ENCOUNTERED, per FIRST phrase (GR17d)
                }
                else if (adjectives[k] == Adjective.Leading)
                {
                    // GR12b/GR17c: the first and each subsequent CONTIGUOUS occurrence, provided the first is at the
                    // point where comparison began in the FIRST cycle in which the operand was ELIGIBLE — an earlier
                    // operand consuming that point (the shared-cycle case) kills the run at zero.
                    if (!live[k] || expectedPos[k] < 0) continue;   // dead, or the region not reached yet — run not killed
                    if (pos != expectedPos[k] || !isMatch) { live[k] = false; continue; }   // contiguity broken
                }
                else if (!isMatch)
                    continue;

                sink(k, pos, pat.Length);
                pos += backward ? -1 : pat.Length;   // GR8c — past the match forward; left of its leftmost character backward
                if (adjectives[k] == Adjective.Leading) expectedPos[k] = pos;
                matched = true;
                break;
            }
            if (!matched) pos += backward ? -1 : 1;   // GR8b — no operand matched: move one position, restart the cycle
        }
    }

    /// <summary>
    /// The TALLYING comparison cycle (§14.9.22.4 GR8/GR12) over the statement's ordered operands (parallel arrays,
    /// source order across ALL counters — GR8a). Returns the per-operand match counts; the caller ADDS each count
    /// into its counter (GR11 — identifier-2 is not initialized by INSPECT). <paramref name="kinds"/> uses
    /// <see cref="TallyAll"/>/<see cref="TallyLeading"/>/<see cref="TallyCharacters"/>; a CHARACTERS operand has a
    /// null pattern.
    /// </summary>
    public static long[] Tally(
        string? text, int[] kinds, string?[] patterns, string?[] befores, string?[] afters, bool backward = false)
    {
        var counts = new long[kinds.Length];
        var never = new bool[kinds.Length];
        for (int k = 0; k < kinds.Length; k++)
            never[k] = (patterns[k] ?? "").Length == 0;
        RunCycle(text ?? "", backward, TallyAdjectives(kinds), patterns, befores, afters, never,
            (k, _, _) => counts[k]++);
        return counts;
    }

    /// <summary>⛔ THE ONE PLACE A LEADING RUN'S ANCHOR OPENS (kb/Work PB1124). §14.9.22.4 GR12 b) / GR17 c): the first
    /// LEADING occurrence must be "at the point where comparison began in the first comparison cycle in which
    /// literal-1 was eligible to participate" — so the anchor is the position of the first CYCLE that falls inside
    /// the operand's region, whichever operand that cycle then matches. Opened lazily inside the operand loop it
    /// slid downstream whenever an EARLIER operand matched at the region start (the shared cycle breaks on the first
    /// match, so the LEADING operand was never visited there): <c>TALLYING ... ALL 'A' ... LEADING 'B'</c> over
    /// "ABBC" counted the run at position 2. Called at the top of every cycle.</summary>
    private static void OpenLeadingAnchors(int pos, Adjective[] adjectives, int[] regionStart, int[] regionEnd,
        int[] expectedPos)
    {
        for (int k = 0; k < adjectives.Length; k++)
            if (adjectives[k] == Adjective.Leading && expectedPos[k] < 0 && pos >= regionStart[k] && pos < regionEnd[k])
                expectedPos[k] = pos;
    }

    /// <summary>
    /// The REPLACING comparison cycle (§14.9.22.4 GR8/GR17) over the statement's ordered operands; returns the
    /// replaced image (equal length — replacements are pattern-sized, so positions never shift). Matching reads
    /// the ORIGINAL content while writes go to a separate buffer (see <see cref="RunCycle"/>). A CHARACTERS operand (null pattern) replaces each matched
    /// character with its replacement's first character (GR17a); FIRST replaces only its first occurrence
    /// encountered — the leftmost, or with BACKWARD the rightmost — and each successive FIRST phrase independently
    /// replaces one occurrence regardless of its pattern value (GR17d); LEADING uses the same
    /// contiguity-from-first-eligibility rule as tallying (GR17c).
    /// <para>A FIGURATIVE replacement (<paramref name="figurative"/>[k]) has the size of its pattern (GR14 — "the
    /// size of the figurative constant is equal to the size of literal-1 or the size of the data item referenced by
    /// identifier-3"), so it is filled to the pattern's RUN-TIME size here: the binder cannot know that size for a
    /// function-identifier or a dynamic-length identifier-3 (kb/Work PB1126).</para>
    /// <para>A pattern/replacement size mismatch (GR14 — only an identifier can produce one; two literals are SR6's
    /// compile-time error) and a CHARACTERS replacement that is not one character (GR15) set EC-RANGE-INSPECT-SIZE,
    /// checked for the whole statement BEFORE the first cycle. With checking enabled the fatal condition ends the
    /// statement there; with it off, "the results of the execution of the INSPECT statement are undefined" and the
    /// deterministic outcome is: the mis-sized operand never matches, and a CHARACTERS replacement contributes its
    /// first character.</para>
    /// </summary>
    public static string Replace(
        string? text, int[] kinds, string?[] patterns, string?[] replacements,
        string?[] befores, string?[] afters, bool backward = false, bool[]? figurative = null)
    {
        string t = text ?? "";
        CheckReplacingSizes(kinds, patterns, replacements, figurative);
        var chars = t.ToCharArray();

        var never = new bool[kinds.Length];
        for (int k = 0; k < kinds.Length; k++)
        {
            string pat = patterns[k] ?? "";
            bool fill = figurative is not null && figurative[k];
            never[k] = pat.Length == 0 || !fill && pat.Length != (replacements[k] ?? "").Length;   // GR14 unchecked: never matches
        }
        RunCycle(t, backward, ReplaceAdjectives(kinds), patterns, befores, afters, never, (k, pos, size) =>
        {
            string repl = replacements[k] ?? "";
            if (kinds[k] == ReplaceCharacters)
                chars[pos] = repl.Length > 0 ? repl[0] : ' ';   // GR17a (GR15 unchecked: first character)
            else
                Put(chars, pos, size, repl, figurative is not null && figurative[k]);
        });
        return new string(chars);
    }

    /// <summary>Write one replacement over <paramref name="size"/> matched positions: the replacement itself, or,
    /// for a figurative (<paramref name="fill"/>), its value repeated to the matched size (GR14's "equal to the size
    /// of literal-1"). No allocation: the fill is written in place.</summary>
    private static void Put(char[] chars, int pos, int size, string repl, bool fill)
    {
        if (!fill) { repl.CopyTo(0, chars, pos, size); return; }
        // A figurative is never zero-length: §8.3.3.6.3 SR2's zero-length ALL literal-1 is a compile-time error
        // (COBOLNET1648) and every other figurative is one character — so there is no empty case to invent a value for.
        for (int i = 0; i < size; i++) chars[pos + i] = repl[i % repl.Length];
    }

    /// <summary>§14.9.22.4 GR14 / GR15, asked of the whole statement before the first comparison cycle: every
    /// non-figurative pattern/replacement pair of unequal size, and every CHARACTERS replacement that is not one
    /// character, sets EC-RANGE-INSPECT-SIZE (Table 13 fatal). GR14 compares the two SIZES and names no exemption,
    /// so a zero-length identifier-3 beside a non-empty replacement is a mismatch too.</summary>
    private static void CheckReplacingSizes(int[] kinds, string?[] patterns, string?[] replacements, bool[]? figurative)
    {
        for (int k = 0; k < kinds.Length; k++)
        {
            if (figurative is not null && figurative[k]) continue;
            int repl = replacements[k]?.Length ?? 0;
            if (kinds[k] == ReplaceCharacters)
            {
                if (repl != 1)
                    ExceptionState.RangeInspectSizeError(
                        $"INSPECT REPLACING CHARACTERS BY a {repl}-character replacement; ISO §14.9.22.4 GR15 requires one character");
                continue;
            }
            int pat = patterns[k]?.Length ?? 0;
            if (pat != repl)
                ExceptionState.RangeInspectSizeError(
                    $"INSPECT REPLACING operand {k + 1}: a {pat}-character pattern replaced by a {repl}-character "
                    + "replacement; ISO §14.9.22.4 GR14 requires equal sizes");
        }
    }

    /// <summary>
    /// CONVERTING (§14.9.22.4 GR20): equivalent to a REPLACING with one <c>ALL c BY d</c> per character of
    /// <paramref name="fromSet"/> (positional correspondence with <paramref name="toSet"/>) — since every operand
    /// is one character, this degenerates to a per-character map over the ONE region. A character duplicated in
    /// <paramref name="fromSet"/> maps by its FIRST occurrence (GR23 — <c>IndexOf</c>). The from/to maps are
    /// positional and direction-independent, so BACKWARD changes only where the BEFORE/AFTER delimiters are first
    /// encountered (<see cref="Region"/>).
    /// <para>GR22: a FIGURATIVE literal-5 (<paramref name="toFigurative"/>) has the size of the from-set, so the
    /// to-set is its value repeated to that RUN-TIME size (an identifier-6 function-identifier's size is known only
    /// here, kb/Work PB1126; an ALL literal repeats per §8.3.3.6.4 GR2). A non-figurative to-set of another size
    /// sets EC-RANGE-INSPECT-SIZE (Table 13 fatal) before any character is converted; with checking off the results
    /// are undefined and the deterministic outcome maps only the common prefix.</para>
    /// </summary>
    public static string Convert(
        string? text, string fromSet, string toSet, string? before, string? after, bool backward = false,
        bool toFigurative = false)
    {
        string t = text ?? "";
        if (!toFigurative && fromSet.Length != toSet.Length)
            ExceptionState.RangeInspectSizeError(
                $"INSPECT CONVERTING a {fromSet.Length}-character set to a {toSet.Length}-character set; ISO "
                + "§14.9.22.4 GR22 requires equal sizes");
        var (start, end) = Region(t, before, after, backward);
        int mapLen = toFigurative ? fromSet.Length : Math.Min(fromSet.Length, toSet.Length);
        var chars = t.ToCharArray();
        for (int i = start; i < end; i++)
        {
            int mapIdx = fromSet.IndexOf(chars[i]);   // first occurrence wins (GR23)
            if (mapIdx >= 0 && mapIdx < mapLen)
                chars[i] = toFigurative ? toSet[mapIdx % toSet.Length] : toSet[mapIdx];   // a figurative is never empty (Put)
        }
        return new string(chars);
    }
}
