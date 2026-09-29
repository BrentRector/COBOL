// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime;

/// <summary>
/// INSPECT TALLYING / REPLACING / CONVERTING over a field's character image (ISO/IEC 1989:2023 §14.9.22). The
/// emitter reads identifier-1's image once (GR4/GR6 — a signed numeric item is de-signed per GR4d and re-signed on
/// the store), calls <see cref="Tally"/> and/or <see cref="Replace"/>/<see cref="Convert"/>, and stores the result
/// back through the item's <c>Place</c>.
/// <para>TALLYING and REPLACING each execute as ONE shared left-to-right comparison cycle over the statement's
/// ordered operands (§14.9.22.4 GR8): at each position the operands are tried in source order (GR8a); the first
/// match tallies/replaces, the position advances past the matched characters, and the cycle restarts from the
/// first operand (GR8c); no match advances one position (GR8b); the cycle ends when the rightmost character has
/// participated or been considered (GR8d). CHARACTERS is an implied always-matching one-character operand (GR8e).
/// This shared cycle is why an earlier <c>ALL "A"</c> starves a later <c>LEADING "AH"</c> to zero — the leading
/// 'A' is consumed before the LEADING operand is ever tried.</para>
/// </summary>
public static class CobolInspect
{
    /// <summary>TALLYING operand kinds (§14.9.22.2 Format 1) — the binder's <c>InspectTallyKind</c> ordinals.</summary>
    public const int TallyAll = 0, TallyLeading = 1, TallyCharacters = 2;

    /// <summary>REPLACING operand kinds (§14.9.22.2 Format 2) — the binder's <c>InspectReplaceKind</c> ordinals.</summary>
    public const int ReplaceAll = 0, ReplaceFirst = 1, ReplaceLeading = 2, ReplaceCharacters = 3;

    // ── BACKWARD (2023; §14.9.22.4 GR3 NOTE 1/NOTE 2) ──
    // BACKWARD scans right-to-left, which is equivalent to scanning the REVERSED text left-to-right provided each
    // multi-character pattern/delimiter is also reversed (so "AB" read right-to-left matches "BA" forward).
    // BEFORE/AFTER keep their roles in the reversed-forward frame (the boundaries are established in scan
    // direction, GR3). Counts need no un-reversal; REPLACING/CONVERTING reverse the result buffer back.
    // CONVERTING's from/to sets are positional one-character maps and are NOT reversed (GR20 — each character
    // maps independently of scan direction).
    private static string ReverseText(string s)
    {
        var a = s.ToCharArray();
        Array.Reverse(a);
        return new string(a);
    }

    private static string?[] ReverseEach(string?[] arr)
    {
        var r = new string?[arr.Length];
        for (int i = 0; i < arr.Length; i++)
            r[i] = arr[i] is { Length: > 1 } s ? ReverseText(s) : arr[i];
        return r;
    }

    /// <summary>
    /// One operand's scan region [start, end) — fixed BEFORE the first comparison cycle (§14.9.22.4 GR9):
    /// AFTER → eligibility starts immediately past the FIRST occurrence of its delimiter; the delimiter absent ⇒
    /// the operand is NEVER eligible (GR9c — an empty region). BEFORE → eligibility ends at the first occurrence
    /// of its delimiter ENCOUNTERED FROM the AFTER-derived start; absent ⇒ as if BEFORE were not specified (GR9b
    /// — the asymmetry with AFTER is deliberate spec text).
    /// </summary>
    private static (int Start, int End) Region(string text, string? before, string? after)
    {
        int start = 0;
        int end = text.Length;
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
    /// The TALLYING comparison cycle (§14.9.22.4 GR8/GR12) over the statement's ordered operands (parallel arrays,
    /// source order across ALL counters — GR8a). Returns the per-operand match counts; the caller ADDS each count
    /// into its counter (GR11 — identifier-2 is not initialized by INSPECT). <paramref name="kinds"/> uses
    /// <see cref="TallyAll"/>/<see cref="TallyLeading"/>/<see cref="TallyCharacters"/>; a CHARACTERS operand has a
    /// null pattern. An ALL/LEADING match requires the WHOLE pattern inside the operand's region
    /// (<c>pos + len &lt;= regionEnd</c>), not merely starting inside it (GR9 — eligibility covers the cycle's
    /// compared characters).
    /// </summary>
    public static long[] Tally(
        string? text, int[] kinds, string?[] patterns, string?[] befores, string?[] afters, bool backward = false)
    {
        string t = text ?? "";
        if (backward)
        {
            t = ReverseText(t);
            patterns = ReverseEach(patterns);
            befores = ReverseEach(befores);
            afters = ReverseEach(afters);
        }
        int n = kinds.Length;
        var counts = new long[n];
        var regionStart = new int[n];
        var regionEnd = new int[n];
        var live = new bool[n];        // LEADING: the contiguous run is still extendable (GR12b)
        var expectedPos = new int[n];  // LEADING: the position the next contiguous match must occupy (-1 = not started)
        for (int k = 0; k < n; k++)
        {
            (regionStart[k], regionEnd[k]) = Region(t, befores[k], afters[k]);
            live[k] = true;
            expectedPos[k] = -1;
        }

        int pos = 0, len = t.Length;
        while (pos < len)
        {
            bool matched = false;
            OpenLeadingAnchors(pos, kinds, TallyLeading, regionStart, regionEnd, expectedPos);
            for (int k = 0; k < n; k++)
            {
                bool inRegion = pos >= regionStart[k] && pos < regionEnd[k];

                if (kinds[k] == TallyCharacters)
                {
                    if (!inRegion) continue;   // ineligible on this cycle ⇒ considered not to match (GR9b/c)
                    counts[k]++;               // GR12c — one per character matched in the GR8e sense
                    pos += 1;
                    matched = true;
                    break;
                }

                string pat = patterns[k] ?? "";
                if (pat.Length == 0) continue;
                bool fits = inRegion && pos + pat.Length <= regionEnd[k];
                bool isMatch = fits && t.AsSpan(pos, pat.Length).SequenceEqual(pat.AsSpan());

                if (kinds[k] == TallyLeading)
                {
                    // GR12b: count the first and each subsequent CONTIGUOUS occurrence, provided the first is at
                    // the point where comparison began in the FIRST cycle in which the operand was ELIGIBLE — an
                    // earlier operand consuming that point (the shared-cycle case) kills the run at zero.
                    if (!live[k] || expectedPos[k] < 0) continue;   // dead, or the region not reached yet — run not killed
                    if (pos == expectedPos[k] && isMatch)
                    {
                        counts[k]++;
                        pos += pat.Length;
                        expectedPos[k] = pos;
                        matched = true;
                        break;
                    }
                    live[k] = false;               // contiguity broken — the LEADING run ends
                    continue;
                }

                // TallyAll (GR12a — one per match).
                if (isMatch)
                {
                    counts[k]++;
                    pos += pat.Length;
                    matched = true;
                    break;
                }
            }
            if (!matched) pos += 1;   // GR8b — no operand matched: advance one position, restart the cycle
        }
        return counts;
    }

    /// <summary>⛔ THE ONE PLACE A LEADING RUN'S ANCHOR OPENS (kb/Work PB1124). §14.9.22.4 GR12 b) / GR17 c): the first
    /// LEADING occurrence must be "at the point where comparison began in the first comparison cycle in which
    /// literal-1 was eligible to participate" — so the anchor is the position of the first CYCLE that falls inside
    /// the operand's region, whichever operand that cycle then matches. Opened lazily inside the operand loop it
    /// slid downstream whenever an EARLIER operand matched at the region start (the shared cycle breaks on the first
    /// match, so the LEADING operand was never visited there): <c>TALLYING ... ALL 'A' ... LEADING 'B'</c> over
    /// "ABBC" counted the run at position 2. Tally and Replace both call this at the top of every cycle.</summary>
    private static void OpenLeadingAnchors(int pos, int[] kinds, int leadingKind, int[] regionStart, int[] regionEnd,
        int[] expectedPos)
    {
        for (int k = 0; k < kinds.Length; k++)
            if (kinds[k] == leadingKind && expectedPos[k] < 0 && pos >= regionStart[k] && pos < regionEnd[k])
                expectedPos[k] = pos;
    }

    /// <summary>
    /// The REPLACING comparison cycle (§14.9.22.4 GR8/GR17) over the statement's ordered operands; returns the
    /// replaced image (equal length — replacements are pattern-sized, so positions never shift). Matching always
    /// reads the ORIGINAL pre-modification text while writes go to a separate buffer, so an earlier replacement
    /// can never create or destroy a later match. A CHARACTERS operand (null pattern) replaces each matched
    /// character with its replacement's first character (GR17a); FIRST replaces only its leftmost match and each
    /// successive FIRST phrase independently replaces one occurrence regardless of its pattern value (GR17d);
    /// LEADING uses the same contiguity-from-first-eligibility rule as tallying (GR17c).
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
        if (backward)
        {
            t = ReverseText(t);
            patterns = ReverseEach(patterns);
            replacements = ReverseEach(replacements);
            befores = ReverseEach(befores);
            afters = ReverseEach(afters);
        }
        var chars = t.ToCharArray();

        int n = kinds.Length;
        var regionStart = new int[n];
        var regionEnd = new int[n];
        var live = new bool[n];        // FIRST/LEADING: still eligible (GR17c/d)
        var expectedPos = new int[n];  // LEADING: contiguous match position (-1 = not started)
        for (int k = 0; k < n; k++)
        {
            (regionStart[k], regionEnd[k]) = Region(t, befores[k], afters[k]);
            live[k] = true;
            expectedPos[k] = -1;
        }

        int pos = 0, len = t.Length;
        while (pos < len)
        {
            bool matched = false;
            OpenLeadingAnchors(pos, kinds, ReplaceLeading, regionStart, regionEnd, expectedPos);
            for (int k = 0; k < n; k++)
            {
                bool inRegion = pos >= regionStart[k] && pos < regionEnd[k];

                if (kinds[k] == ReplaceCharacters)
                {
                    if (!inRegion) continue;
                    string rep = replacements[k] ?? " ";
                    chars[pos] = rep.Length > 0 ? rep[0] : ' ';   // GR17a (GR15 unchecked: first character)
                    pos += 1;
                    matched = true;
                    break;
                }

                string pat = patterns[k] ?? "";
                string repl = replacements[k] ?? "";
                bool fill = figurative is not null && figurative[k];
                if (pat.Length == 0 || !fill && pat.Length != repl.Length) continue;   // GR14 unchecked: never matches
                bool fits = inRegion && pos + pat.Length <= regionEnd[k];
                bool isMatch = fits && t.AsSpan(pos, pat.Length).SequenceEqual(pat.AsSpan());

                if (kinds[k] == ReplaceFirst)
                {
                    if (!live[k]) continue;
                    if (isMatch)
                    {
                        Put(chars, pos, pat.Length, repl, fill);
                        live[k] = false;          // only the leftmost occurrence, per FIRST phrase (GR17d)
                        pos += pat.Length;
                        matched = true;
                        break;
                    }
                    continue;
                }

                if (kinds[k] == ReplaceLeading)
                {
                    if (!live[k] || expectedPos[k] < 0) continue;   // GR17c — same contiguity machinery as tallying GR12b
                    if (pos == expectedPos[k] && isMatch)
                    {
                        Put(chars, pos, pat.Length, repl, fill);
                        pos += pat.Length;
                        expectedPos[k] = pos;
                        matched = true;
                        break;
                    }
                    live[k] = false;
                    continue;
                }

                // ReplaceAll (GR17b — each match replaced).
                if (isMatch)
                {
                    Put(chars, pos, pat.Length, repl, fill);
                    pos += pat.Length;
                    matched = true;
                    break;
                }
            }
            if (!matched) pos += 1;
        }

        if (backward) Array.Reverse(chars);
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
    /// positional and direction-independent, so BACKWARD reverses only the text and delimiters.
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
        if (backward)
        {
            t = ReverseText(t);
            if (before is { Length: > 1 }) before = ReverseText(before);
            if (after is { Length: > 1 }) after = ReverseText(after);
        }
        var (start, end) = Region(t, before, after);
        int mapLen = toFigurative ? fromSet.Length : Math.Min(fromSet.Length, toSet.Length);
        var chars = t.ToCharArray();
        for (int i = start; i < end; i++)
        {
            int mapIdx = fromSet.IndexOf(chars[i]);   // first occurrence wins (GR23)
            if (mapIdx >= 0 && mapIdx < mapLen)
                chars[i] = toFigurative ? toSet[mapIdx % toSet.Length] : toSet[mapIdx];   // a figurative is never empty (Put)
        }
        if (backward) Array.Reverse(chars);
        return new string(chars);
    }
}
