// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Runtime.Collation;

/// <summary>
/// Canonical decomposition (Unicode Normalization Form D) computed from the collation table's OWN data — the
/// canonical decomposition mappings and combining classes of the UCD version the table was generated from — so the
/// engine's notion of canonical equivalence never depends on the Unicode version of the host's ICU. (That dependence
/// is not hypothetical: a Windows-bundled ICU that predates Unicode 16 leaves U+1ADB and U+10D6A unordered, and the
/// CLDR conformance test then fails on this host while passing on a newer one.)
/// <para>The engine normalizes only when it must (UTS #10 S1.1): a text with a NON-STARTER may need canonical
/// REORDERING and its precomposed bases must decompose so their marks take part in it; at
/// <see cref="CollationStrength.Identical"/> the tie-break compares NFD forms, so any decomposable character counts.
/// Everything else is walked as-is — the derived table's explicit mapping of a precomposed character equals its
/// decomposition's element sequence by construction (the CLDR/UCA data is canonically closed, and
/// <see cref="CollationTable.Rebuild"/> closes every tailored table, whichever front-end built it).</para>
/// <para>NFD here means: replace every code point by its full canonical decomposition (Hangul syllables through the
/// arithmetic L V T mapping), then stable-sort every maximal run of non-starters by combining class (The Unicode
/// Standard §3.11, Canonical Ordering Algorithm). No composition step — collation never needs NFC.</para>
/// </summary>
internal static class Normalizer
{
    /// <summary>Does <paramref name="text"/> need normalizing before it is walked? True when it holds a non-starter
    /// (reordering may apply); with <paramref name="forIdentical"/> also when it holds any decomposable code point.</summary>
    public static bool NeedsNfd(ReadOnlySpan<char> text, CollationTable table, bool forIdentical)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c < (char)0xC0) continue;                                   // below U+00C0 nothing decomposes or combines
            int cp = c;
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                cp = char.ConvertToUtf32(c, text[++i]);
            if (table.IsNonStarter(cp)) return true;
            if (forIdentical && (CollationTable.IsHangulSyllable(cp) || table.TryGetCanonicalDecomposition(cp, out _))) return true;
        }
        return false;
    }

    /// <summary>The NFD of <paramref name="text"/> under the table's Unicode data. Unpaired surrogates pass through
    /// as themselves.</summary>
    public static string ToNfd(ReadOnlySpan<char> text, CollationTable table)
    {
        var cps = new List<int>(text.Length + 8);
        var ccc = new List<byte>(text.Length + 8);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            int cp = c;
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                cp = char.ConvertToUtf32(c, text[++i]);
            if (CollationTable.IsHangulSyllable(cp))
            {
                int n = CollationTable.DecomposeHangul(cp, out int l, out int v, out int t);
                cps.Add(l); ccc.Add(0);
                cps.Add(v); ccc.Add(0);
                if (n == 3) { cps.Add(t); ccc.Add(0); }
            }
            else if (table.TryGetCanonicalDecomposition(cp, out var d))
            {
                foreach (int x in d) { cps.Add(x); ccc.Add((byte)table.CombiningClass(x)); }
            }
            else
            {
                cps.Add(cp);
                ccc.Add((byte)table.CombiningClass(cp));
            }
        }
        // Canonical Ordering Algorithm: stable insertion sort of every run of non-starters by combining class.
        for (int i = 1; i < cps.Count; i++)
        {
            byte ci = ccc[i];
            if (ci == 0) continue;
            int j = i;
            while (j > 0 && ccc[j - 1] > ci)
            {
                (cps[j], cps[j - 1]) = (cps[j - 1], cps[j]);
                (ccc[j], ccc[j - 1]) = (ccc[j - 1], ccc[j]);
                j--;
            }
        }
        var sb = new StringBuilder(cps.Count + 4);
        foreach (int cp in cps)
        {
            if (cp is >= 0xD800 and <= 0xDFFF) sb.Append((char)cp);      // an unpaired surrogate stays what it was
            else sb.Append(char.ConvertFromUtf32(cp));
        }
        return sb.ToString();
    }
}
