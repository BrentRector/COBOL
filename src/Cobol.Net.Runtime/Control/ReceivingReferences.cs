// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// ⛔ THE CHARACTER POSITIONS OF A FORMAL'S STORAGE THE ACTIVATED ELEMENT REFERENCED AS A RECEIVING ITEM (kb/Work PB2690).
/// <para>ISO §8.5.1.9.3: "When a data item in a dynamic-capacity table is referenced as a receiving item and the value of
/// the subscript exceeds the current capacity of the table, a new element is automatically created and the capacity of the
/// table is increased to the value given by the subscript." A BY REFERENCE formal whose table has a FIXED occurrence count
/// opposite the argument's dynamic-capacity table occupies the argument's storage (§14.2.3 GR8: "the activated runtime
/// element operates as if the formal parameter occupies the same storage area as the argument"), so a store through its
/// occurrence past the argument's capacity is exactly that receiving reference. When the pair's shapes differ the formal
/// occupies a fresh area filled from the argument and stored back at return (<c>CobolArgAdapt.Area</c>), and the image
/// stored back cannot tell a store of exactly the image the formal was shown from no store at all. So the area records
/// the reference itself: every store through a view of a pointer-located class notes its window on the area's cell
/// (<see cref="StorageCell.Received"/>), the boundary starts the record once the area is filled
/// (<see cref="ManagedPointer{T}.Watch"/>), and the write-back reads it here, in the positions of the formal's own image
/// (its fixed run, for a variable-length formal).</para>
/// </summary>
public sealed class ReceivingReferences
{
    private readonly bool[] _marks;
    private readonly int _base;

    internal ReceivingReferences(bool[] marks, int @base)
    {
        _marks = marks;
        _base = @base;
    }

    /// <summary>True when any position in [<paramref name="from"/>, <paramref name="to"/>) of the formal's image — the
    /// positions this record starts at — was referenced as a receiving item.</summary>
    public bool Within(int from, int to)
    {
        int lo = Math.Max(0, _base + from), hi = Math.Min(_marks.Length, _base + to);
        return lo < hi && _marks.AsSpan(lo, hi - lo).Contains(true);
    }

    /// <summary>This record from position <paramref name="offset"/> of the formal's image on — the positions of one of its
    /// fixed-length table elements, whose own image starts there.</summary>
    public ReceivingReferences From(int offset) => offset == 0 ? this : new(_marks, _base + offset);
}
