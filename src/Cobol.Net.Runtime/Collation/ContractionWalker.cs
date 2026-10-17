// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Collation;

/// <summary>
/// The ONE implementation of UTS #10 S2.1 over a (canonically ordered) UTF-16 text: the code point cursor, the
/// LONGEST contiguous contraction match starting at a code point, and the DISCONTIGUOUS extension over the following
/// unblocked non-starters (S2.1.1–S2.1.3), with the marks a discontiguous match consumed skipped when the cursor
/// reaches them. It decides only WHICH mapping each step of the text takes; what that mapping's elements are is the
/// caller's (S2.2). Two callers, one walk: <see cref="CollationElementIterator"/> walks a built
/// <see cref="CollationTable"/>'s contractions, and the CLDR tailoring builder walks the mappings its rules have built
/// so far (ICU's CollationBuilder reads a reset, extension or prefix string the same way: <c>getCEs</c> over the NFD
/// string on the data built so far). An unpaired surrogate is walked as its own code unit, so ill-formed text still
/// orders. Allocation-free except in the discontiguous case, where the consumed positions are remembered.
/// </summary>
internal ref struct ContractionWalker
{
    private readonly ReadOnlySpan<char> _text;
    private readonly CollationTable _unicode;   // the combining classes (every table shares its Unicode data)
    private int _pos;
    // Char indices consumed early by a discontiguous contraction match (rare; lazily allocated).
    private List<int>? _removed;

    /// <param name="text">The text, already in NFD when it holds a non-starter (this type never normalizes).</param>
    /// <param name="unicode">Any table: only its canonical combining classes are read.</param>
    public ContractionWalker(ReadOnlySpan<char> text, CollationTable unicode)
    {
        _text = text;
        _unicode = unicode;
    }

    /// <summary>Decode the code point at the cursor (skipping any position a discontiguous match already consumed)
    /// and advance past it. <paramref name="after"/> is the char index following it.</summary>
    public bool NextCodePoint(out int codePoint, out int after)
    {
        while (_pos < _text.Length)
        {
            int start = _pos;
            int cp = DecodeAt(start, out int len);
            _pos = start + len;
            if (_removed is { Count: > 0 } && _removed.Remove(start)) continue;   // consumed by an earlier discontiguous match
            codePoint = cp;
            after = _pos;
            return true;
        }
        codePoint = 0;
        after = _pos;
        return false;
    }

    /// <summary>UTS #10 S2.1: the LONGEST contiguous contraction among <paramref name="candidates"/> (the contractions
    /// that begin with the code point just returned by <see cref="NextCodePoint"/>, longest first) starting at
    /// <paramref name="after"/>, then S2.1.1–S2.1.3: extend it with each following unblocked non-starter for which a
    /// longer contraction exists (a non-starter is blocked when an intervening non-starter shares its combining
    /// class). On a match the cursor moves past the contiguous part, and a non-starter consumed discontiguously is
    /// skipped when the cursor reaches it.</summary>
    public bool TryMatchContraction(int after, CollationTable.Contraction[] candidates, out CollationTable.Contraction match)
    {
        // Contiguous, longest first (the candidate array is sorted longest-first).
        bool matched = false;
        match = default;
        int end = after;
        foreach (var cand in candidates)
        {
            if (MatchesAt(after, cand.Rest, out int matchEnd))
            {
                matched = true;
                match = cand;
                end = matchEnd;
                break;
            }
        }
        // Discontiguous extension over the following non-starters.
        int scan = end;
        Span<bool> seen = stackalloc bool[256];
        while (scan < _text.Length)
        {
            int cp = DecodeAt(scan, out int len);
            int here = scan;
            scan += len;
            if (_removed is { Count: > 0 } && _removed.Contains(here)) continue;
            int ccc = _unicode.CombiningClass(cp);
            if (ccc == 0) break;
            if (seen[ccc]) continue;                                   // blocked (S2.1.2 note)
            var grown = FindContraction(candidates, matched ? match.Rest : null, matched ? match.Rest.Length : 0, cp);
            if (grown is { } g)
            {
                matched = true;
                match = g;
                (_removed ??= new List<int>(2)).Add(here);
                // The consumed mark no longer blocks anything; its class is NOT recorded.
            }
            else seen[ccc] = true;
        }
        if (!matched) return false;
        _pos = end;      // past the contiguous part; discontiguously consumed marks are skipped when the cursor reaches them
        return true;
    }

    private int DecodeAt(int index, out int length)
    {
        char c = _text[index];
        if (char.IsHighSurrogate(c) && index + 1 < _text.Length && char.IsLowSurrogate(_text[index + 1]))
        {
            length = 2;
            return char.ConvertToUtf32(c, _text[index + 1]);
        }
        length = 1;
        return c;   // a BMP character — or an unpaired surrogate walked as its own code unit
    }

    /// <summary>The candidate whose Rest is <paramref name="prefix"/> (length <paramref name="prefixLength"/>) followed
    /// by <paramref name="next"/>, or null.</summary>
    private static CollationTable.Contraction? FindContraction(CollationTable.Contraction[] candidates, int[]? prefix, int prefixLength, int next)
    {
        foreach (var cand in candidates)
        {
            if (cand.Rest.Length != prefixLength + 1 || cand.Rest[prefixLength] != next) continue;
            bool same = true;
            for (int i = 0; i < prefixLength && same; i++) same = cand.Rest[i] == prefix![i];
            if (same) return cand;
        }
        return null;
    }

    /// <summary>Do the code points starting at <paramref name="index"/> (skipping consumed positions) spell
    /// <paramref name="rest"/>? <paramref name="end"/> is the char index just past the match.</summary>
    private bool MatchesAt(int index, int[] rest, out int end)
    {
        int i = index;
        foreach (int expected in rest)
        {
            while (true)
            {
                if (i >= _text.Length) { end = i; return false; }
                int cp = DecodeAt(i, out int len);
                int here = i;
                i += len;
                if (_removed is { Count: > 0 } && _removed.Contains(here)) continue;
                if (cp != expected) { end = i; return false; }
                break;
            }
        }
        end = i;
        return true;
    }
}
