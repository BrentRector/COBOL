// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using System.Text;

namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE ONE CASE FOLD OF A COBOL WORD — ISO Annex C, the "Mapping of uppercase letters to lowercase letters in the
/// COBOL character repertoire" (kb/Work PB1402). Every table keyed by a COBOL word, and every comparison of two
/// words, folds through here and through nothing else (<c>CobolNameFoldDriftTests</c>).
/// <para><b>The rule.</b> §8.1.3.2 GR3 b): "Equivalence of uppercase and lowercase basic letters is achieved by
/// folding from uppercase to lowercase in accordance with the case mapping described in Annex C", and GR4 b): "An
/// uppercase extended letter is treated as though it were folded to its corresponding lowercase extended letter, if
/// any, in accordance with the case mapping described in Annex C". A character Annex C.2 does not list folds to
/// nothing but itself — so a COBOL word is NOT compared by .NET's ordinal-ignore-case, which upper-cases by the host's
/// simple case mapping and disagrees with C.2 exactly on the extended letters: it makes final sigma (U+03C2) the same
/// letter as sigma and a lowercase Cherokee syllable the same as its capital (C.2 lists neither; Annex G.2 item 2
/// explains the Cherokee omission), and it keeps U+0130 and the Kelvin sign U+212A apart from <c>i</c> and <c>k</c>
/// (C.2 maps both).</para>
/// <para><b>The edition.</b> The tables are the COBOL 2023 Annex C; the earlier editions differ by Annex E's two
/// recorded changes: E.2 item 14 DELETED (0131,0069) and (03C2,03C3) at 2023, and E.3.3 item 6 ADDED 597 mappings.
/// Every added mapping pairs at least one character that the earlier editions' Annex B does not hold (measured by
/// <c>AnnexBCDriftTests</c>), so a word that could tell the two tables apart is refused by the Annex B screen before
/// any fold; the deleted pair is the ONLY observable difference, and <see cref="EditionSpelling"/> gives a word lexed
/// under an earlier edition the spelling that makes this one comparer answer for that edition.</para>
/// </summary>
public static partial class CobolNames
{
    /// <summary>The lookups, built from the generated lists on first use. A nested class, because the lists are static
    /// fields of the OTHER part of this partial class, and C# orders static initializers only within one file.</summary>
    private static class Tables
    {
        internal static readonly FrozenDictionary<int, int> Fold2023 =
            C2Mappings.ToFrozenDictionary(p => p.First, p => p.Second);
        internal static readonly FrozenDictionary<int, int> FoldedOnlyBefore2023 =
            E2Item14Deleted.ToFrozenDictionary(p => p.First, p => p.Second);
        internal static readonly FrozenSet<int> AddedIn2023 = E33Item6Added.Select(p => p.First).ToFrozenSet();
    }

    /// <summary>THE fold of one character: its Annex C.2 lowercase, or itself when C.2 lists no mapping for it.</summary>
    public static int Fold(int codePoint) =>
        codePoint < 0x80 ? (codePoint is >= 'A' and <= 'Z' ? codePoint | 0x20 : codePoint)
        : Tables.Fold2023.TryGetValue(codePoint, out int lower) ? lower : codePoint;

    /// <summary>The fold of one character under the Annex C of the edition <paramref name="dialectLevel"/> — the 2023
    /// table with Annex E's changes reversed below 2023 (a mapping E.3.3 item 6 added is absent, one E.2 item 14
    /// deleted is present).</summary>
    public static int Fold(int codePoint, int dialectLevel)
    {
        if (dialectLevel >= 2023 || codePoint < 0x80) return Fold(codePoint);
        if (Tables.FoldedOnlyBefore2023.TryGetValue(codePoint, out int lower)) return lower;
        return Tables.AddedIn2023.Contains(codePoint) ? codePoint : Fold(codePoint);
    }

    /// <summary>The folded form of a whole word — the KEY two equal words share. All-basic-letter words take the
    /// ASCII path and allocate only when a letter is uppercase.</summary>
    public static string Fold(string word)
    {
        int i = 0;
        while (i < word.Length && word[i] < 0x80 && word[i] is not (>= 'A' and <= 'Z')) i++;
        if (i == word.Length) return word;
        var sb = new StringBuilder(word.Length);
        sb.Append(word, 0, i);
        while (i < word.Length) AppendCodePoint(sb, NextFolded(word, ref i));
        return sb.ToString();
    }

    /// <summary>The folded code point at <paramref name="i"/>, advancing past it — one UTF-16 unit, two for a
    /// surrogate pair, and a lone surrogate is its own unit (left unfolded: no mapping names one). Every Annex C
    /// mapping keeps a character's UTF-16 width (a BMP letter folds to a BMP letter, a supplementary one to a
    /// supplementary one; <c>AnnexBCDriftTests</c> holds it), so a word and its fold are the same length and their
    /// code points line up — which is what lets the span operations below compare in place.</summary>
    private static int NextFolded(ReadOnlySpan<char> s, ref int i)
    {
        char c = s[i];
        if (c < 0x80) { i++; return c is >= 'A' and <= 'Z' ? c | 0x20 : c; }
        if (char.IsHighSurrogate(c) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
        {
            int cp = char.ConvertToUtf32(c, s[i + 1]);
            i += 2;
            return Fold(cp);
        }
        i++;
        return Fold((int)c);
    }

    private static void AppendCodePoint(StringBuilder sb, int codePoint)
    {
        if (codePoint > 0xFFFF) sb.Append(char.ConvertFromUtf32(codePoint));
        else sb.Append((char)codePoint);
    }

    /// <summary>Two COBOL words are the same word when their folds are equal. Two nulls are the same; a null and a
    /// word are not.</summary>
    public static bool Same(string? a, string? b) =>
        ReferenceEquals(a, b) || (a is not null && b is not null && Same(a.AsSpan(), b.AsSpan()));

    /// <summary><see cref="Same(string?, string?)"/> over spans of source text, allocation-free.</summary>
    public static bool Same(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0, j = 0; i < a.Length;)
            if (NextFolded(a, ref i) != NextFolded(b, ref j)) return false;
        return true;
    }

    /// <summary>The comparer every table keyed by a COBOL word uses (the replacement of
    /// <see cref="StringComparer.OrdinalIgnoreCase"/> wherever a COBOL word is the key).</summary>
    public static StringComparer Comparer { get; } = new FoldComparer();

    /// <summary>True when the fold of <paramref name="word"/> begins with the fold of <paramref name="prefix"/>.</summary>
    public static bool StartsWith(ReadOnlySpan<char> word, ReadOnlySpan<char> prefix) =>
        word.Length >= prefix.Length && Same(word[..prefix.Length], prefix);

    /// <summary>True when the fold of <paramref name="word"/> ends with the fold of <paramref name="suffix"/>.</summary>
    public static bool EndsWith(ReadOnlySpan<char> word, ReadOnlySpan<char> suffix) =>
        word.Length >= suffix.Length && Same(word[^suffix.Length..], suffix);

    /// <summary>True when the fold of <paramref name="text"/> contains the fold of <paramref name="part"/>.</summary>
    public static bool Contains(ReadOnlySpan<char> text, ReadOnlySpan<char> part)
    {
        for (int start = 0; start + part.Length <= text.Length; start++)
            if (Same(text.Slice(start, part.Length), part)) return true;
        return false;
    }

    /// <summary>The spelling under which the ONE (2023) comparer answers as the edition <paramref name="dialectLevel"/>'s
    /// Annex C would: each character whose fold differs between the two tables, and is not left as itself by the
    /// edition's, is written as the edition's fold (below 2023: dotless i U+0131 as <c>i</c>, final sigma U+03C2 as
    /// sigma U+03C3 — Annex E.2 item 14). The lexer applies it to every word it emits, so the word IS that edition's
    /// word from then on. At 2023 and for a word of basic characters it returns <paramref name="word"/> itself.</summary>
    public static string EditionSpelling(string word, int dialectLevel)
    {
        if (dialectLevel >= 2023) return word;
        int i = 0;
        while (i < word.Length && word[i] < 0x80) i++;
        if (i == word.Length) return word;
        StringBuilder? sb = null;
        for (int at = i; at < word.Length;)
        {
            int width = char.IsHighSurrogate(word[at]) && at + 1 < word.Length && char.IsLowSurrogate(word[at + 1]) ? 2 : 1;
            int cp = width == 2 ? char.ConvertToUtf32(word[at], word[at + 1]) : word[at];
            int edition = Fold(cp, dialectLevel);
            if (edition != cp && edition != Fold(cp))
            {
                sb ??= new StringBuilder(word.Length).Append(word, 0, at);
                AppendCodePoint(sb, edition);
            }
            else sb?.Append(word, at, width);
            at += width;
        }
        return sb?.ToString() ?? word;
    }

    /// <summary>The fold comparison, by code point, with a fast path for two basic characters.</summary>
    private sealed class FoldComparer : StringComparer
    {
        public override int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            int i = 0, j = 0;
            while (i < x.Length && j < y.Length)
            {
                int a = NextFolded(x, ref i), b = NextFolded(y, ref j);
                if (a != b) return a.CompareTo(b);
            }
            return (x.Length - i).CompareTo(y.Length - j);
        }

        public override bool Equals(string? x, string? y) => Compare(x, y) == 0;

        public override int GetHashCode(string obj)
        {
            var hash = new HashCode();
            for (int i = 0; i < obj.Length;) hash.Add(NextFolded(obj, ref i));
            return hash.ToHashCode();
        }
    }
}
