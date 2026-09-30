// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// A non-native ALPHANUMERIC program collating sequence (an <c>ALPHABET</c> literal phrase, ISO §12.3.7.4 GR7 k)
/// over the native alphanumeric character set — the 65,536 UTF-16 code units (implementor item 188). The rule
/// itself lives ONCE, in <see cref="LiteralPhraseCollation"/>, which the <c>FOR NATIONAL</c> twin
/// <see cref="NationalCollation"/> shares: §12.3.7.4 GR7 k governs both classes with one set of sub-rules, and
/// keeping two copies of it is what let the alphanumeric arm drift to a 256-entry Latin-1 block that masked every
/// operand with <c>&amp; 0xFF</c> (kb/Work PB770 leg f — <c>ALPHABET A IS 305 THRU 300</c> silently reversed
/// <c>'+'</c>…<c>'0'</c>).
/// <para>The compiler emits one instance per program as <c>__COLLATE</c>; CHAR (§15.15.4), ORD (§15.70.4 r1),
/// relations (§8.8.4.2.7), SORT/MERGE keys (§14.9.40.4 GR5), indexed keys (§12.4.5.7) and MAX/MIN read it
/// through the ONE <see cref="CobolCollation"/> carrier.</para>
/// </summary>
public sealed class AlphanumericCollation : LiteralPhraseCollation
{
    /// <param name="codes">The specified code units, sorted ascending by code.</param>
    /// <param name="positions">Parallel to <paramref name="codes"/>: each specified code's 0-based position.</param>
    /// <param name="repByPos">Per specified position: the FIRST character defined there (§15.15.4 r2 / GR7 k6).</param>
    /// <param name="nextFree">The first position after the specified block (§12.3.7.4 GR7 k3).</param>
    /// <param name="highValue">The sequence's HIGH-VALUE character (§12.3.7.4 GR8, binder-computed).</param>
    /// <param name="lowValue">The sequence's LOW-VALUE character (§12.3.7.4 GR9).</param>
    public AlphanumericCollation(ushort[] codes, ushort[] positions, ushort[] repByPos, int nextFree,
        char highValue = NativeCollatingSequence.HighValue, char lowValue = NativeCollatingSequence.LowValue)
        : base(codes, positions, repByPos, nextFree, highValue, lowValue) { }
}
