// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// One shared, aliasable character-storage cell (the Tier-B string-canonical backing lifted onto the heap —
/// never a byte substrate): EXTERNAL records, ADDRESS-OF-taken items, and ALLOCATEd areas all live in one of
/// these; <see cref="Ref"/> is a FIELD so the generated <c>ref</c>-returning bridge property can alias it.
/// </summary>
public sealed class StorageCell
{
    /// <summary>The storage's character image (its full width; every view windows it).</summary>
    public string Ref = "";

    /// <summary>True for a cell obtained by ALLOCATE (ISO §14.9.3) — the only cells FREE releases (§14.9.15.4 GR1a).</summary>
    public bool Allocated;

    /// <summary>⛔ THE ONE LIFETIME STATE OF A CELL (kb/Work PB1216): how many lives of this storage have ENDED. A data
    /// pointer (<see cref="CellPointer"/>) records the generation it was taken in, and ISO §13.18.5.4 GR4 makes a
    /// reference through one whose generation is no longer the cell's current one a reference to "an address [that is]
    /// not a valid address of storage" — EC-BOUND-PTR. A counter rather than a flag because a STATIC cell is
    /// re-seeded IN PLACE at CANCEL (<see cref="Reinitialize"/>): a flag would come back to 'live' and resurrect every
    /// pointer taken before the CANCEL, while the counter keeps those dead and lets a pointer taken afterwards
    /// live.</summary>
    public int Generation { get; private set; }

    /// <summary>Why the most recent life ended — <see cref="StorageEnd.None"/> until <see cref="End"/> has run.</summary>
    public StorageEnd LastEnd { get; private set; }

    /// <summary>End the cell's current life for <paramref name="reason"/> — the ONE way storage stops existing. FREE
    /// (§14.9.15.4 GR1a: "the contents of any data items located within the released storage area become undefined")
    /// also releases the image and the managed slots, which are one storage area with it; the activation and CANCEL
    /// ends (§8.6.4) leave the image to its owner, who re-seeds or drops it. Every pointer taken before this call
    /// now fails its dereference loud (<c>CobolPtr.Deref</c>).</summary>
    public void End(StorageEnd reason)
    {
        Generation++;
        LastEnd = reason;
        if (reason == StorageEnd.Freed)
        {
            Ref = "";
            ClearSlots();   // GR1a — the managed slots are the SAME storage area (kb/Work PB231)
        }
    }

    /// <summary>⛔ THE MANAGED SLOTS OF THE SAME STORAGE AREA, keyed by the slot's BYTE OFFSET within it
    /// (kb/Work PB231 — the pointer third). A data item of class pointer or class object holds a managed
    /// REFERENCE, which is not a byte sequence and therefore has no image in <see cref="Ref"/>; its bytes
    /// there are reserved placeholder positions so that §14.9.3.4 GR3's "the amount of storage to be
    /// allocated is the number of bytes required to hold an item as described by data-name-1" — and every
    /// following member's offset — stay exactly what a byte-addressed area says they are.
    /// <para>⛔ THE SLOTS BELONG TO THE CELL, NOT TO THE ITEM, and that is the whole reason they are here:
    /// EXTERNAL sharing, ADDRESS OF aliasing and <c>SET ADDRESS OF</c> re-pointing all mean "two descriptions
    /// of ONE storage area", so a pointer member re-pointed through one description must be visible through
    /// every other. A per-instance field could not do that.</para>
    /// <para>⛔ AN UNWRITTEN SLOT READS NULL, AND THAT *IS* §14.9.3.4 GR9 — "data items of class object or
    /// class pointer in the allocated storage are initialized to null" — realized by construction rather
    /// than by a seeding loop that a future allocation path could forget. The table is allocated lazily, so
    /// a cell with no managed member costs one null field.</para></summary>
    private Dictionary<int, object?>? _slots;

    /// <summary>The managed value at <paramref name="byteOffset"/>, or <see langword="null"/> when nothing
    /// has been stored there (ISO §14.9.3.4 GR9's null initial state — see <see cref="_slots"/>).</summary>
    public object? SlotAt(int byteOffset) =>
        _slots is { } m && m.TryGetValue(byteOffset, out object? v) ? v : null;

    /// <summary>Store a managed value at <paramref name="byteOffset"/> — the receiving twin of
    /// <see cref="SlotAt"/>.</summary>
    public void SetSlotAt(int byteOffset, object? value) => (_slots ??= [])[byteOffset] = value;

    // ── THE DYNAMIC-LENGTH HALF OF THE SAME AREA (kb/Work PB1026) ───────────────────────────────────────────────
    //
    // A dynamic-length elementary item has no fixed character positions: ISO §8.5.1.10.3 — "Dynamic-length
    // elementary items may be physically located in memory within the record they are subordinate to, or they may
    // be located elsewhere in the computer's memory". Inside a CELL-BACKED area (an EXTERNAL record, an EXTERNAL
    // file's out-of-line record, an ADDRESS-OF-taken record) the "elsewhere" is a managed slot of the SAME cell,
    // so every description that shares the cell shares the item's content too (§13.18.22.4 GR1 / GR4 b)). It
    // occupies ZERO positions of Ref — the area's FIXED RUN, exactly the CobolVarGroup.Fixed accounting
    // (§8.5.1.12.3: "all dynamic-length elementary items are considered to be of zero length") — and its content
    // rides the slot keyed by its ordinal among the area's dynamic-length items. The keys are negative, so they can
    // never meet a pointer slot, which is keyed by a byte offset (≥ 0).

    private static int DynKey(int ordinal) => -1 - ordinal;

    /// <summary>The current content of the area's <paramref name="ordinal"/>-th dynamic-length item — empty
    /// until one is stored — ISO §8.6.4: "If no VALUE clause is specified, the length of that item in its initial state is zero".</summary>
    public string DynAt(int ordinal) => SlotAt(DynKey(ordinal)) as string ?? "";

    /// <summary>Store the <paramref name="ordinal"/>-th dynamic-length item's new content. The caller has already
    /// applied §8.5.1.10.4's receiving rule (<c>CobolDynString.Store</c>), exactly as it does for a declared
    /// field.</summary>
    public void SetDynAt(int ordinal, string content) => SetSlotAt(DynKey(ordinal), content);

    /// <summary>Seed a dynamic-length item's INITIAL content (its VALUE clause, §8.6.4) and return this cell — the
    /// fluent form the generated cell initializers chain onto <c>new StorageCell { Ref = … }</c> and
    /// <see cref="Reinitialize"/>.</summary>
    public StorageCell SeedDyn(int ordinal, string content)
    {
        SetDynAt(ordinal, content);
        return this;
    }

    /// <summary>A variable-length group of this area as its §8.5.1.12 component carrier: the group's fixed run
    /// (<paramref name="fixedWidth"/> positions of <see cref="Ref"/> from <paramref name="fixedAt"/>) and its
    /// <paramref name="dynCount"/> dynamic-length items from ordinal <paramref name="dynBase"/> — the same carrier a
    /// declared group's generated <c>AsVarImage()</c> builds.</summary>
    public CobolVarGroup VarGroupAt(int fixedAt, int fixedWidth, int dynBase, int dynCount)
    {
        var dyn = new string[dynCount];
        for (int k = 0; k < dynCount; k++) dyn[k] = DynAt(dynBase + k);
        return new CobolVarGroup(FixedRun(fixedAt, fixedWidth), dyn);
    }

    /// <summary>Distribute a component carrier into the group — the receiving twin of <see cref="VarGroupAt"/>,
    /// and the twin of a declared group's <c>FromVarImage</c>: the fixed run is stored at its width (padded or
    /// truncated, §14.9.25.4 GR9's fixed part), and each dynamic-length item takes its component as its new
    /// content, truncated on the right at its maximum size (§8.5.1.10.4).</summary>
    public void StoreVarGroupAt(int fixedAt, int fixedWidth, int dynBase, ReadOnlySpan<int> dynMax, CobolVarGroup v)
    {
        Ref = CobolString.WindowInto(Ref, fixedAt + 1, fixedWidth, CobolString.Store(v.Fixed, fixedWidth));
        for (int k = 0; k < dynMax.Length; k++)
            SetDynAt(dynBase + k, CobolDynString.Store(v.Dyn(k), dynMax[k]));
    }

    /// <summary>A variable-length group of this area as its CONTIGUOUS image at its current extent — ISO
    /// §8.5.1.11.2: "a variable-length data item behaves in all respects as though it were in fact contiguous with
    /// its neighbors whenever a procedural operation is applied to a group containing it". Each dynamic-length item
    /// sits at its fixed-run position <paramref name="dynFixedAt"/> (relative to the group) — the declared group's
    /// <c>CurrentImage()</c>, composed from the cell.</summary>
    public string ContiguousAt(int fixedAt, int fixedWidth, int dynBase, ReadOnlySpan<int> dynFixedAt)
    {
        string run = FixedRun(fixedAt, fixedWidth);
        var sb = new System.Text.StringBuilder(fixedWidth);
        int at = 0;
        for (int k = 0; k < dynFixedAt.Length; k++)
        {
            sb.Append(run, at, dynFixedAt[k] - at);
            at = dynFixedAt[k];
            sb.Append(DynAt(dynBase + k));
        }
        sb.Append(run, at, run.Length - at);
        return sb.ToString();
    }

    /// <summary>The EXTENT TABLE of that contiguous image (determination D-FRA (v); kb/Work PB1053): each
    /// dynamic-length item's fixed-run position <paramref name="dynFixedAt"/> and its current length — what a WRITE /
    /// REWRITE / RELEASE of the group sends beside <see cref="ContiguousAt"/>, the declared group's
    /// <c>CurrentExtents()</c> composed from the cell.</summary>
    public RecordExtents ContiguousExtentsAt(int fixedWidth, int dynBase, ReadOnlySpan<int> dynFixedAt,
                                             ReadOnlySpan<int> dynMax)
    {
        var lengths = new int[dynFixedAt.Length];
        for (int k = 0; k < lengths.Length; k++) lengths[k] = DynAt(dynBase + k).Length;
        return new RecordExtents(dynFixedAt.ToArray(), lengths, LayoutOf(fixedWidth, dynFixedAt, dynMax));
    }

    /// <summary>The contiguous layout of a cell-backed variable-length group: unit 1 per component — a
    /// dynamic-length item contributes its content character for character, the same unit a declared group's
    /// generated layout gives it (<c>GroupImageCodec.ContiguousLayout</c>).</summary>
    private static CobolContiguousLayout LayoutOf(int fixedWidth, ReadOnlySpan<int> dynFixedAt, ReadOnlySpan<int> dynMax)
    {
        var ones = new int[dynFixedAt.Length];
        var max = new long[dynMax.Length];
        for (int k = 0; k < ones.Length; k++) { ones[k] = 1; max[k] = dynMax[k]; }
        return new CobolContiguousLayout(fixedWidth, dynFixedAt.ToArray(), ones, max);
    }

    /// <summary>Make a contiguous image the group's content — the inverse of <see cref="ContiguousAt"/>, through the
    /// ONE decomposition (<see cref="CobolContiguousLayout.Decompose"/>, determination D-FRA): by the image's own
    /// <paramref name="extents"/> when they describe it (a record read back with its extent table — D-FRA (v),
    /// kb/Work PB1053), otherwise by the take step, each dynamic-length item taking as many characters as the image
    /// holds beyond the fixed material still to come, up to its maximum size.</summary>
    public void StoreContiguousAt(int fixedAt, int fixedWidth, int dynBase, ReadOnlySpan<int> dynFixedAt,
                                  ReadOnlySpan<int> dynMax, string image, RecordExtents? extents = null,
                                  bool fixedForm = false)
    {
        var layout = LayoutOf(fixedWidth, dynFixedAt, dynMax);
        StoreVarGroupAt(fixedAt, fixedWidth, dynBase, dynMax, layout.Decompose(image ?? "", extents, fixedForm));
    }

    private string FixedRun(int fixedAt, int fixedWidth) =>
        CobolString.Store(fixedAt >= Ref.Length ? "" : Ref.Substring(fixedAt, Math.Min(fixedWidth, Ref.Length - fixedAt)),
            fixedWidth);

    /// <summary>Drop every managed slot — FREE (§14.9.15.4 GR1a, "the contents of any data items located
    /// within the released storage area become undefined"): the byte image and the managed slots are one
    /// storage area and are released together, so a released cell cannot keep a dangling pointer alive.</summary>
    internal void ClearSlots() => _slots = null;

    /// <summary>Return a STATIC cell to its initial state IN PLACE (ISO §14.6.2.3.2 action 2 — kb/Work PB234):
    /// the byte image becomes <paramref name="image"/> (the declaration's own VALUE-honoring seed) and every
    /// managed slot reads null again, which is §13.18.63's initial state for a pointer or object member with no
    /// VALUE. IN PLACE, never a fresh cell, because the cell IS the storage a <c>ManagedPointer.At</c> window
    /// aliases: a RECURSIVE unit's WORKING-STORAGE is ONE static copy (§13.5.4 GR1) and the window keeps naming it —
    /// but the window's generation (<see cref="Generation"/>) no longer matches, so a pointer taken BEFORE the CANCEL
    /// is an invalid address (§8.6.5, §13.18.5.4 GR4) and one taken after is live.</summary>
    public StorageCell Reinitialize(string image)
    {
        End(StorageEnd.Cancelled);   // §8.6.4: the static item persisted "to … the execution of a CANCEL statement" — every pointer taken into the old life is dead (kb/Work PB1216)
        Ref = image;
        _slots = null;
        return this;   // a dynamic-length item's VALUE is re-seeded by a chained SeedDyn (kb/Work PB1026)
    }
}
