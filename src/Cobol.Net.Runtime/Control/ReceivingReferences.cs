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
/// (<see cref="StorageCell.Received(long, int)"/>), the boundary starts the record once the area is filled
/// (<see cref="ManagedPointer{T}.Watch"/>), and the write-back reads it here.</para>
/// <para>⛔ THE COORDINATES ARE THE FORMAL'S STORAGE, the positions of its area's cell: for a variable-length formal that
/// is its fixed run WITH each dynamic-capacity table's one-element reservation in place (<c>CobolVarGroup</c>'s
/// <c>Geometry.StorageAt</c>), never the fixed run alone (<c>Geometry.FixedChars</c>, which <c>StorageCell.RunOf</c> cuts
/// the reservations out of). A reader indexing this record by fixed-run offsets would read an occurrence shifted by every
/// reservation before it.</para>
/// <para>⛔ AN OCCURRENCE OF THE FORMAL'S OWN DYNAMIC-CAPACITY TABLE IS A SCOPE OF ITS OWN (kb/Work PB2690 branch review
/// round 1 of wave 1078 A, finding R1-1): its element lies in an element cell whose positions and component ordinals start
/// again at zero, so a fixed table nested in it (opposite a dynamic-capacity table nested in the argument's element) is
/// recorded in that occurrence's own record, keyed by the component ordinal and the occurrence number its store
/// reached (<see cref="StorageCell.Received(ReadOnlySpan{long}, long, int)"/>), and read back through
/// <see cref="Element"/>. The key is the SUBSCRIPT, never the element cell: an occurrence the callee's own receiving
/// reference creates is a new cell, and its references are recorded all the same.</para>
/// </summary>
public sealed class ReceivingReferences
{
    private readonly Scope _scope;
    private readonly int _base;

    /// <summary>The component ordinal of this record's first component in its scope, or <see cref="NoComponents"/> for a
    /// record narrowed to fixed material (<see cref="From"/>), which holds no component of its own.</summary>
    private readonly int _dynBase;

    private const int NoComponents = -1;

    private ReceivingReferences(Scope scope, int @base, int dynBase)
    {
        _scope = scope;
        _base = @base;
        _dynBase = dynBase;
    }

    /// <summary>The record of the formal whose storage starts at position <paramref name="offset"/> of the watched scope
    /// <paramref name="scope"/> and whose first component is the scope's component <paramref name="dynBase"/>.</summary>
    internal static ReceivingReferences Of(Scope scope, int offset, int dynBase) => new(scope, offset, dynBase);

    /// <summary>True when any position in [<paramref name="from"/>, <paramref name="to"/>) of the formal's storage, counted
    /// from where this record starts, was referenced as a receiving item.</summary>
    public bool Within(int from, int to) => _scope.Within(_base + from, _base + to);

    /// <summary>This record from storage position <paramref name="offset"/> of the formal on: the positions of one of its
    /// fixed-length tables or table elements, whose own storage starts there (fixed material: no component of its own).</summary>
    public ReceivingReferences From(int offset) => offset == 0 ? this : new(_scope, _base + offset, NoComponents);

    /// <summary>The record of occurrence <paramref name="occurrence"/> (one-based) of the formal's dynamic-capacity table
    /// that is its component <paramref name="component"/>: that element's own scope, or null when the activated element
    /// referenced nothing in it.</summary>
    public ReceivingReferences? Element(int component, int occurrence) =>
        _dynBase != NoComponents && _scope.ElementOf(_dynBase + component, occurrence) is { } element
            ? new(element, 0, 0)
            : null;

    /// <summary>The marks of one scope of a watched area — the area's cell, or one occurrence of a dynamic-capacity table
    /// in it — and the scopes of the occurrences referenced inside it, keyed by component ordinal and occurrence
    /// number.</summary>
    internal sealed class Scope
    {
        private bool[] _marks;
        private Dictionary<(long Ordinal, long Occurrence), Scope>? _elements;

        internal Scope(int width) => _marks = new bool[width];

        /// <summary>Record a receiving reference to the <paramref name="width"/> positions from <paramref name="at"/> of
        /// the scope <paramref name="path"/> leads to: pairs of (component ordinal, one-based occurrence number), outermost
        /// first; empty for this scope itself.</summary>
        internal void Mark(ReadOnlySpan<long> path, long at, int width)
        {
            var scope = this;
            for (int i = 0; i + 1 < path.Length; i += 2)
            {
                var key = (path[i], path[i + 1]);
                var elements = scope._elements ??= [];
                if (!elements.TryGetValue(key, out var inner)) elements[key] = inner = new Scope(0);
                scope = inner;
            }
            scope.Mark(at, width);
        }

        private void Mark(long at, int width)
        {
            long lo = Math.Max(0, at), hi = at + width;
            if (lo >= hi) return;
            if (hi > _marks.Length) Array.Resize(ref _marks, (int)hi);
            _marks.AsSpan((int)lo, (int)(hi - lo)).Fill(true);
        }

        internal bool Within(int from, int to)
        {
            int lo = Math.Max(0, from), hi = Math.Min(_marks.Length, to);
            return lo < hi && _marks.AsSpan(lo, hi - lo).Contains(true);
        }

        internal Scope? ElementOf(long ordinal, long occurrence) =>
            _elements is { } e && e.TryGetValue((ordinal, occurrence), out var inner) ? inner : null;
    }
}
