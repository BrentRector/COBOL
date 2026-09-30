// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// A non-native NATIONAL collating sequence (an <c>ALPHABET … FOR NATIONAL</c> literal phrase, ISO §12.3.7.4
/// GR7 k) over the native national character set — the 65,536 UTF-16 code units, one per national position
/// (D-N1; §8.5.1.4 makes each code element its own character position). The rule itself lives ONCE, in
/// <see cref="LiteralPhraseCollation"/>, shared with the alphanumeric twin <see cref="AlphanumericCollation"/>:
/// §12.3.7.4 GR7 k governs both classes with one set of sub-rules (the b-series and the c-series of
/// §12.3.7.3 SR14 differ only in which literal class each operand shall be).
/// <para>The compiler emits one instance per program as <c>__COLLATE_NAT</c> (the national twin of the
/// alphanumeric <c>__COLLATE</c> weights); it drives national relation/condition-name comparisons
/// (§12.3.6 GR11 / §8.8.4.2.9), CHAR-NATIONAL (§15.16.4), and ORD over a national argument (§15.70.4 r2).</para>
/// </summary>
public sealed class NationalCollation : LiteralPhraseCollation
{
    /// <param name="codes">The specified code units, sorted ascending by code.</param>
    /// <param name="positions">Parallel to <paramref name="codes"/>: each specified code's 0-based position.</param>
    /// <param name="repByPos">Per specified position: the FIRST character defined there (§15.16.4 r2 / GR7 k6).</param>
    /// <param name="nextFree">The first position after the specified block (§12.3.7.4 GR7 k3).</param>
    /// <param name="highValue">The sequence's national HIGH-VALUE character (§12.3.7.4 GR8, binder-computed).</param>
    /// <param name="lowValue">The sequence's national LOW-VALUE character (§12.3.7.4 GR9).</param>
    public NationalCollation(ushort[] codes, ushort[] positions, ushort[] repByPos, int nextFree,
        char highValue = NativeCollatingSequence.HighValue, char lowValue = NativeCollatingSequence.LowValue)
        : base(codes, positions, repByPos, nextFree, highValue, lowValue) { }
}
