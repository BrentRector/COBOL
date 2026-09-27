// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;

namespace CobolNet.Frontend.Expressions;

/// <summary>
/// An immutable compile-time boolean value — a string of '0'/'1' positions (ISO/IEC 1989:2023 §8.8.2; the D-B1
/// substrate the runtime <c>CobolString</c>/<c>CobolBool</c> store category-boolean items on). VALUE equality is
/// <b>length-sensitive</b> (§7.3.8.3 GR2 constant-conditional comparison + §7.3.11.3 SR2 redefinition — two
/// boolean values are the same iff same length AND same bits), distinct from the runtime §8.8.4.2.2 relation
/// which right-extends the shorter operand.
///
/// The fold operators mirror the runtime <see cref="T:CobolNet.Runtime.CobolBool"/> kernel EXACTLY (the proven
/// §8.8.2 implementation; the ALGORITHM — not the code — is shared, written before the Frontend referenced the
/// Runtime assembly): a binary op combines positionwise left-to-right, right-zero-extending the shorter operand, result
/// length = the larger operand (rules 9/10); zero-length combines to zero-length (rule 9 NOTE 2); <see cref="Not"/>
/// preserves length; a shift preserves the first operand's length (rule 8).
///
/// A compile-time (directive) boolean operand is ALWAYS a concrete boolean LITERAL — §7.3.7.2 SR1 admits only
/// boolean literals, and §7.3.3 SR10 bars the figurative constants (<c>ZERO</c> / <c>ALL "literal"</c>) that the
/// runtime §8.8.2 admits — so there is no positionless (figurative) case here; a figurative operand is a formation
/// error the evaluator rejects (COBOLNET1619), never a value.
/// </summary>
public sealed class BitString : IEquatable<BitString>
{
    /// <summary>The '0'/'1' positions (empty for a zero-length value).</summary>
    public string Bits { get; }

    private BitString(string bits) => Bits = bits ?? "";

    /// <summary>A concrete boolean value of the given fixed width.</summary>
    public static BitString Of(string bits) => new(bits);

    public int Length => Bits.Length;

    private static char Bit(string s, int i) => i < s.Length && s[i] == '1' ? '1' : '0';

    /// <summary>Unary <c>B-NOT</c> (§8.8.2 rule 7b, 1st) — complement every position, length preserved.</summary>
    public BitString Not()
    {
        var arr = Bits.ToCharArray();
        for (int i = 0; i < arr.Length; i++) arr[i] = arr[i] == '1' ? '0' : '1';
        return new BitString(new string(arr));
    }

    /// <summary>A binary boolean op (<c>'&amp;'</c> B-AND / <c>'^'</c> B-XOR / <c>'|'</c> B-OR, §8.8.2 rules 9/10)
    /// combining <paramref name="left"/> and <paramref name="right"/> positionwise, the shorter right-zero-extended;
    /// result length = the larger operand; two zero-length operands ⇒ zero-length (rule 9 NOTE 2).</summary>
    public static BitString Combine(BitString left, char op, BitString right)
    {
        string a = left.Bits, b = right.Bits;
        int n = Math.Max(a.Length, b.Length);
        if (n == 0) return Of("");
        var arr = new char[n];
        for (int i = 0; i < n; i++)
        {
            char x = Bit(a, i), y = Bit(b, i);
            bool bit = op switch { '&' => x == '1' && y == '1', '^' => x != y, _ => x == '1' || y == '1' };
            arr[i] = bit ? '1' : '0';
        }
        return Of(new string(arr));
    }

    /// <summary>A boolean shift/rotate (§8.8.2 rule 8): <paramref name="count"/> positions, logical (zero-fill) or
    /// <paramref name="circular"/> (rotate), <paramref name="left"/> or right. Result length = this value's length.
    /// A count == 0 is identity; a logical shift by ≥ the length yields all zeros; a circular shift is periodic in
    /// the length. Mirrors <c>CobolBool.Shift</c>.</summary>
    public BitString Shift(long count, bool circular, bool left)
    {
        string v = Bits;
        int n = v.Length;
        if (n == 0 || count <= 0) return Of(v);
        if (circular) { count %= n; if (count == 0) return Of(v); }
        else if (count >= n) return Of(new string('0', n));
        var arr = v.ToCharArray();
        for (long it = 0; it < count; it++)
        {
            if (left)
            {
                char first = arr[0];
                for (int i = 0; i < n - 1; i++) arr[i] = arr[i + 1];
                arr[n - 1] = circular ? first : '0';
            }
            else
            {
                char last = arr[n - 1];
                for (int i = n - 1; i > 0; i--) arr[i] = arr[i - 1];
                arr[0] = circular ? last : '0';
            }
        }
        return Of(new string(arr));
    }

    /// <summary>The truth value of a boolean value used as a simple boolean condition (§8.8.4.3 GR1 — true iff the
    /// value is boolean 1). SR1 (length 1) is checked by the caller; this defensively tests position 1.</summary>
    public bool IsTrue => Bits.Length > 0 && Bits[0] == '1';

    /// <summary>Boolean-RELATION equality (§8.8.4.2.8 rule 2): a positionwise value comparison, the shorter operand
    /// right-extended with boolean zeros; two zero-length values are equal. This is the equality a cce / EVALUATE
    /// boolean relation uses (§7.3.8.3 GR2's unequal-length⇒unequal length-sensitivity is for operands that are
    /// "not numeric or boolean") — distinct from <see cref="Equals(BitString)"/>. Mirrors <c>CobolBool.Equal</c>.</summary>
    public static bool EqualExtended(BitString a, BitString b)
    {
        int n = Math.Max(a.Length, b.Length);
        for (int i = 0; i < n; i++) if (Bit(a.Bits, i) != Bit(b.Bits, i)) return false;
        return true;
    }

    // Length-sensitive value equality (§7.3.11.3 SR2 redefinition — two DEFINE values are "the same" iff same
    // length AND same bits): unequal length ⇒ not equal. (A boolean RELATION uses EqualExtended instead.)
    public bool Equals(BitString? other) =>
        other is not null && string.Equals(Bits, other.Bits, StringComparison.Ordinal);
    public override bool Equals(object? obj) => Equals(obj as BitString);
    public override int GetHashCode() => Bits.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => "B\"" + Bits + "\"";
}
