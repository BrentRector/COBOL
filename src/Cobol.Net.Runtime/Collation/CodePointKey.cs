// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.Collation;

/// <summary>
/// A code point SEQUENCE as a set or dictionary key — one code point, or a contraction — with value equality over
/// the sequence. The one key shape a tailoring is looked up by: a <c>.tailor</c> file's entries
/// (<see cref="TailoringRules"/>), the entries a <see cref="TailoringPlan"/> defines, and the canonical-closure keys
/// <see cref="CollationTable.Rebuild"/> derives from them, so "does the tailoring define this sequence" has one
/// answer everywhere.
/// </summary>
internal readonly struct CodePointKey : IEquatable<CodePointKey>
{
    private readonly int[] _codePoints;
    private readonly int _hash;

    public CodePointKey(ReadOnlySpan<int> codePoints)
    {
        _codePoints = codePoints.ToArray();
        var h = new HashCode();
        foreach (int cp in codePoints) h.Add(cp);
        _hash = h.ToHashCode();
    }

    /// <summary>The sequence.</summary>
    public ReadOnlySpan<int> CodePoints => _codePoints;

    public bool Equals(CodePointKey other) => _hash == other._hash && _codePoints.AsSpan().SequenceEqual(other._codePoints);

    public override bool Equals(object? obj) => obj is CodePointKey other && Equals(other);

    public override int GetHashCode() => _hash;

    public override string ToString() => string.Join(" ", (_codePoints ?? []).Select(cp => $"U+{cp:X4}"));
}
