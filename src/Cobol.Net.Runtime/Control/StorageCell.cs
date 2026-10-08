// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

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
    /// REFERENCE, which is not a byte sequence and therefore has no VALUE in <see cref="Ref"/>; its bytes
    /// there are reserved positions (a pointer's hold its storage image, which <c>CobolPtr.SlotWrite</c> keeps equal
    /// to the slot's value, kb/Work PB1071; an object's are placeholders) so that §14.9.3.4 GR3's "the amount of storage to be
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

    /// <summary>⛔ A DETACHED COPY OF THE AREA <paramref name="width"/> positions from <paramref name="offset"/> (kb/Work
    /// PB1940): a new cell holding those characters of <see cref="Ref"/> and the managed slots keyed inside them, each
    /// shifted to the same position of the copy. It is ISO §14.2.3 GR9's record "allocated by the activating runtime
    /// element" for a group whose values ride the slots (a strongly-typed group with an object-reference or pointer
    /// leaf): the copy keeps every reference, which a character image would drop, and "does not occupy the same storage
    /// area as the argument". Only byte-offset slots are copied — the area is a group with no variable-length component
    /// (such a group crosses on its §8.5.1.12 carrier).</summary>
    public StorageCell CopyArea(long offset, int width)
    {
        int at = checked((int)offset);
        var copy = new StorageCell { Ref = Ref.Substring(at, width) };
        if (_slots is { } m)
            foreach (var (key, value) in m)
                if (key >= at && key < at + width) copy.SetSlotAt(key - at, value);
        return copy;
    }

    /// <summary>The receiving twin of <see cref="CopyArea"/> (kb/Work PB1940): make the positions of this cell from
    /// <paramref name="offset"/> hold <paramref name="source"/> — its characters, and its managed slots in place of the
    /// ones keyed inside those positions here.</summary>
    public void StoreArea(long offset, StorageCell source)
    {
        int at = checked((int)offset), width = source.Ref.Length;
        Ref = string.Concat(Ref.AsSpan(0, at), source.Ref, Ref.AsSpan(at + width));
        if (_slots is { } m)
            foreach (int key in m.Keys.Where(k => k >= at && k < at + width).ToList()) m.Remove(key);
        if (source._slots is { } s)
            foreach (var (key, value) in s)
                if (key >= 0) SetSlotAt(key + at, value);
    }

    // ── THE VARIABLE-LENGTH HALF OF THE SAME AREA (kb/Work PB1026, PB1042) ──────────────────────────────────────
    //
    // A cell-backed area (an EXTERNAL record, an EXTERNAL file's out-of-line record, an ADDRESS-OF-taken record) may
    // hold VARIABLE-LENGTH COMPONENTS, and neither kind has fixed character positions:
    //  * a dynamic-length elementary item — ISO §8.5.1.10.3: "Dynamic-length elementary items may be physically
    //    located in memory within the record they are subordinate to, or they may be located elsewhere in the
    //    computer's memory";
    //  * a dynamic-capacity table — ISO §8.5.1.9.1 3): "it may be defined in any place, other than the file section,
    //    in which a fixed-capacity table may be defined", and an EXTERNAL or ADDRESS-OF-taken record is such a place.
    // Each rides a managed slot of the SAME cell, so every description that shares the cell shares the component too
    // (§13.18.22.4 GR1 / GR4 b)). Components are numbered once per OCCURRENCE in storage order (the compiler's
    // ordinal; a component under a fixed table has one per occurrence). A dynamic-length item's slot holds its
    // content; a dynamic-capacity table's slot holds a CobolDynTable whose every occurrence is an ELEMENT CELL — the
    // element's fixed run in its Ref and the element's own components in its own slots, so a table inside a table
    // element, or a dynamic-length item inside one, is the same mechanism one level down. The two key families are
    // disjoint from each other and from the pointer slots (byte offsets ≥ 0), so a description that asks a slot of
    // the other kind reads its empty state instead of mis-casting.
    // In the fixed run a dynamic-length item occupies ZERO positions (its ByteWidth) and a table RESERVES its
    // one-element extent (its ByteWidth) as placeholder positions — the D-SLOT reason a pointer reserves its bytes:
    // every following member's offset stays what the byte-addressed walk says. The group composers below cut the
    // reservations out, which is §8.5.1.12.3's accounting ("all dynamic-length elementary items are considered to be
    // of zero length") applied to a table, whose CURRENT occurrences are the component.

    private static int DynKey(int ordinal) => -1 - ordinal;

    private static int TableKey(int ordinal) => int.MinValue + ordinal;

    /// <summary>The current content of the area's <paramref name="ordinal"/>-th component, a dynamic-length item —
    /// empty until one is stored — ISO §8.6.4: "If no VALUE clause is specified, the length of that item in its initial state is zero".</summary>
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

    /// <summary>The area's <paramref name="ordinal"/>-th component, a dynamic-capacity table (kb/Work PB1042): one
    /// <see cref="StorageCell"/> per occurrence. Every reference to the table and to its elements goes through it —
    /// the element accessors (<c>RefSending</c> / <c>RefReceiving</c>, §8.5.1.9.2 / §8.5.1.9.3), the CAPACITY
    /// register, SET Format 14 — so the table's capacity rules are <see cref="CobolDynTable{T}"/>'s, written once.
    /// <para>⛔ ISO §14.6.13.2 rule 6 IS ASKED HERE: "When the internal format of a dynamic-capacity table, as defined by
    /// the implementor, is not correctly formed or does not agree with the corresponding OCCURS clause an
    /// EC-DATA-INCOMPATIBLE exception condition is set to exist." A shared area is the one place a table is read
    /// through a description other than the one that built it (two programs describing one EXTERNAL record), so the
    /// REFERENCING description's FROM minimum <paramref name="min"/> and element storage width
    /// <paramref name="elementWidth"/> arrive with every reference — the twin of <c>CobolDynString.Agree</c>, rule 5.
    /// The table disagrees when its current capacity is below the minimum or its elements are another width, and is
    /// not correctly formed when the area holds no table at this component at all; with checking enabled the fatal
    /// condition is raised, with it off the result is undefined (§14.6.13.1.1) and the reference proceeds — on a table
    /// built from this description when the area had none.</para></summary>
    public CobolDynTable<StorageCell> DynTableAt(int ordinal, int min, int elementWidth)
    {
        if (SlotAt(TableKey(ordinal)) is CobolDynTable<StorageCell> table)
        {
            if (ExceptionState.DataIncompatibleChecking && Disagreement(table, min, elementWidth) is { } why)
                ExceptionState.DataIncompatibleError($"a dynamic-capacity table {why} (ISO §14.6.13.2 rule 6)");
            return table;
        }
        if (ExceptionState.DataIncompatibleChecking)
            ExceptionState.DataIncompatibleError(
                $"the storage area holds no dynamic-capacity table at component {ordinal}: the area was created by a "
                + "description that does not agree with this one (ISO §14.6.13.2 rule 6)");
        var formed = new CobolDynTable<StorageCell>(() => new StorageCell { Ref = new string(' ', elementWidth) }, min, null, null);
        SetSlotAt(TableKey(ordinal), formed);
        return formed;
    }

    /// <summary>Why <paramref name="table"/> does not agree with an OCCURS clause of minimum <paramref name="min"/> and
    /// element width <paramref name="elementWidth"/>, or null when it agrees.</summary>
    private static string? Disagreement(CobolDynTable<StorageCell> table, int min, int elementWidth) =>
        table.Capacity < min ? $"has a current capacity of {table.Capacity}, below the OCCURS clause's minimum {min}"
        : table.Capacity > 0 && table.RefSending(1).Ref.Length != elementWidth
            ? $"has elements of {table.RefSending(1).Ref.Length} positions, and the OCCURS clause's element is {elementWidth}"
        : null;

    /// <summary>Seed the area's <paramref name="ordinal"/>-th component with its dynamic-capacity table and return
    /// this cell — the table's initial state (§8.5.1.9.1: the FROM capacity, each occurrence its element's initial
    /// image), chained onto the cell initializer exactly as <see cref="SeedDyn"/> is.</summary>
    public StorageCell SeedDynTable(int ordinal, CobolDynTable<StorageCell> table)
    {
        SetSlotAt(TableKey(ordinal), table);
        return this;
    }

    /// <summary>An element cell's character image — the element's fixed run. An element that has a component of its
    /// own (a table of variable-length ELEMENTS, kb/Work PB244) is composed with its
    /// <see cref="CellGroupShape"/> instead (<see cref="ComponentAt"/>).</summary>
    private static readonly Func<StorageCell, string> ElementImage = static e => e.Ref;

    /// <summary>Distribute one occurrence's image into a freshly seeded element cell.</summary>
    private static readonly Func<StorageCell, string, StorageCell> ElementStore = static (e, x) =>
    {
        e.Ref = x;
        return e;
    };

    /// <summary>The current content of component <paramref name="ordinal"/>: a dynamic-length item's content, or a
    /// table's occurrences at its current capacity (<paramref name="tableWidth"/> &gt; 0 is its element width) —
    /// §14.9.11.4 GR7's "every occurrence at its current capacity", the declared group's <c>CurrentImage</c> arm.
    /// A table whose ELEMENTS are variable-length groups (<paramref name="element"/>, kb/Work PB244) contributes each
    /// occurrence at ITS current extent - the element cell composed with the element's own shape, exactly the
    /// declared table's <c>CurrentImage(static __e =&gt; __e.CurrentImage())</c>.</summary>
    private string ComponentAt(int ordinal, int tableWidth, CellGroupShape? element = null) =>
        tableWidth > 0
            ? DynTableAt(ordinal, 0, tableWidth).CurrentImage(element is null ? ElementImage : e => e.ContiguousAt(element))
            : DynAt(ordinal);

    /// <summary>The CONTIGUOUS image of the variable-length group an element cell holds, by its own
    /// <paramref name="shape"/> (no OCCURS DEPENDING table lies inside a dynamic-capacity table's element:
    /// <c>DataItem.CurrentExtentComposes</c>).</summary>
    private string ContiguousAt(CellGroupShape shape) =>
        ContiguousAt(0, shape.Width, 0, shape.DynFixedAt, shape.DynTable, default, 0, shape.Elems);

    /// <summary>Make the <paramref name="k"/>-th carried component the content of component <paramref name="ordinal"/>
    /// — the declared group's <c>FromVarImage</c> arm for arm: a dynamic-length item takes it truncated on the right at
    /// its maximum size (§8.5.1.10.4); a table is RECREATED from it (§14.6.9.2) when it was carried and space-filled
    /// at its unaffected capacity (§14.6.9.4) when the sender had no corresponding component. A table whose ELEMENTS are
    /// variable-length groups (<paramref name="element"/>, kb/Work PB2496) is recreated from its carried occurrence
    /// carriers, each stored into its element cell by the element's own shape — §14.6.9.2's "Correspondingly numbered
    /// elements are moved according to the rules of the MOVE statement". <paramref name="storage"/> is the formal
    /// copy-in that keeps every dynamic-length item's whole content (kb/Work PB1937), one level down too.</summary>
    private void StoreComponentAt(int ordinal, int tableWidth, int max, CobolVarGroup v, int k,
                                  CellGroupShape? element = null, bool storage = false)
    {
        if (tableWidth == 0)
        {
            SetDynAt(ordinal, CobolDynString.Store(v.Dyn(k), max));
            return;
        }
        var table = DynTableAt(ordinal, 0, tableWidth);
        if (element is { } shape)
        {
            StorageCell Store(StorageCell e, CobolVarGroup x)
            {
                e.StoreVarGroupAt(0, shape.Width, 0, shape.DynFixedAt, storage ? Unlimited(shape.DynMax) : shape.DynMax,
                    shape.DynTable, default, int.MaxValue, x, shape.Elems, storage);
                return e;
            }
            if (v.HasDyn(k)) table.Recreate(v.ElementCarriersAt(k), CobolVarGroup.Empty, Store);
            else table.SpaceFillElements(CobolVarGroup.Empty, Store);
            return;
        }
        if (v.HasDyn(k)) table.FromCurrentImage(v.Dyn(k), tableWidth, ElementStore);
        else table.SpaceFillElements(new string(' ', tableWidth), ElementStore);
    }

    /// <summary>The maximum sizes of a formal's copy-in (kb/Work PB1937): no member LIMIT applies to that store.</summary>
    private static int[] Unlimited(int[] dynMax) => Array.ConvertAll(dynMax, static _ => int.MaxValue);

    /// <summary>Component <paramref name="ordinal"/> — a dynamic-capacity table whose elements are variable-length groups
    /// of <paramref name="shape"/> (kb/Work PB2496) — as its occurrences' own §8.5.1.12 carriers, up to its current
    /// capacity: the nested component of <see cref="CobolVarGroup.Elements"/>, each element cell read by the element's
    /// shape (an element cell is a scope of its own, its slots numbered from zero).</summary>
    private CobolVarGroup[] ElementCarriersAt(int ordinal, int tableWidth, CellGroupShape shape) =>
        DynTableAt(ordinal, 0, tableWidth).CurrentOccurrencesAs(
            e => e.VarGroupAt(0, shape.Width, 0, shape.DynFixedAt, shape.DynTable, default, 0, shape.Elems));

    /// <summary>A variable-length group of this area as its §8.5.1.12 component carrier: the group's fixed run (its
    /// <paramref name="width"/> positions of <see cref="Ref"/> from <paramref name="fixedAt"/>, each table's
    /// reservation cut out) and its components from ordinal <paramref name="dynBase"/>, sitting at
    /// <paramref name="dynFixedAt"/> (relative to the group; <paramref name="dynTable"/> is each one's table element
    /// width, 0 for a dynamic-length item) — the same carrier a declared group's generated <c>AsVarImage()</c>
    /// builds. A group that holds an OCCURS DEPENDING table (<paramref name="tail"/>, kb/Work PB244) has it cut to
    /// <paramref name="count"/> occurrences: the table is the run's trailing storage (§13.18.38.3 SR22), and the
    /// activation boundary passes the table's maximum count (§14.8.2.2); the other helpers below take the same pair.
    /// <paramref name="elems"/> is each table component's element shape when its elements are variable-length groups
    /// (kb/Work PB2496): that component is carried nested, as its occurrences' own carriers.</summary>
    public CobolVarGroup VarGroupAt(int fixedAt, int width, int dynBase, ReadOnlySpan<int> dynFixedAt,
                                    ReadOnlySpan<int> dynTable, CellOdoTail tail, int count, CellGroupShape?[]? elems = null)
    {
        // The occurrences beyond the OCCURS DEPENDING count carry no components (kb/Work PB244): the table is the
        // group's trailing storage, so they are its last ones - the carrier a declared group builds
        // (CobolTable.ConcatVarImages) holds the first `count` occurrences' only.
        var dyn = new string[dynFixedAt.Length - tail.CutComponents(count)];
        CobolVarGroup[]?[]? nested = null;
        for (int k = 0; k < dyn.Length; k++)
        {
            // A table of variable-length ELEMENTS is a nested component (kb/Work PB2496): its occurrences' carriers.
            if (elems?[k] is { } shape)
            {
                (nested ??= new CobolVarGroup[]?[dyn.Length])[k] = ElementCarriersAt(dynBase + k, dynTable[k], shape);
                dyn[k] = "";
            }
            else dyn[k] = ComponentAt(dynBase + k, dynTable[k]);
        }
        string run = RunOf(fixedAt, width, dynFixedAt, dynTable);
        return new CobolVarGroup(tail.Present ? run[..^tail.CutAt(count)] : run, dyn, nested);
    }

    /// <summary>Distribute a component carrier into the group — the receiving twin of <see cref="VarGroupAt"/>,
    /// and the twin of a declared group's <c>FromVarImage</c>: the fixed run is stored at its width (padded or
    /// truncated, §14.9.25.4 GR9's fixed part) around the tables' reservations, and each component takes its
    /// carried part (<see cref="StoreComponentAt"/>).
    /// <para>A group that holds an OCCURS DEPENDING table (<paramref name="tail"/>, kb/Work PB244) uses only
    /// <paramref name="count"/> of its occurrences (ISO §13.18.38.4 GR8 a: "only that part of the table area that is
    /// specified by the value of the data item referenced by data-name-1 at the start of the operation will be
    /// used"): the table's remaining occurrences keep their fixed run AND their components. The maximum count - GR8 b)
    /// for a depending item inside the group, a record read back, the activation boundary - stores them all.</para>
    /// <para><paramref name="elems"/> is each table component's element shape when its elements are variable-length
    /// groups (kb/Work PB2496; the component is then nested, <see cref="CobolVarGroup.Elements"/>), and
    /// <paramref name="storage"/> marks a formal's copy-in, whose dynamic-length items keep their whole content at every
    /// level (kb/Work PB1937).</para></summary>
    public void StoreVarGroupAt(int fixedAt, int width, int dynBase, ReadOnlySpan<int> dynFixedAt,
                                ReadOnlySpan<int> dynMax, ReadOnlySpan<int> dynTable, CellOdoTail tail, int count,
                                CobolVarGroup v, CellGroupShape?[]? elems = null, bool storage = false)
    {
        string run = v.Fixed;
        if (tail.CutAt(count) is > 0 and var cut)
        {
            string current = RunOf(fixedAt, width, dynFixedAt, dynTable);
            run = CobolString.Store(run, current.Length)[..^cut] + current[^cut..];
        }
        StoreRun(fixedAt, width, dynFixedAt, dynTable, run);
        for (int k = 0, used = dynFixedAt.Length - tail.CutComponents(count); k < used; k++)
            StoreComponentAt(dynBase + k, dynTable[k], dynMax[k], v, k, elems?[k], storage);
    }

    /// <summary>A variable-length group of this area as its CONTIGUOUS image at its current extent — ISO
    /// §8.5.1.11.2: "a variable-length data item behaves in all respects as though it were in fact contiguous with
    /// its neighbors whenever a procedural operation is applied to a group containing it". Each component sits at its
    /// fixed-run position — the declared group's <c>CurrentImage()</c>, composed from the cell.</summary>
    public string ContiguousAt(int fixedAt, int width, int dynBase, ReadOnlySpan<int> dynFixedAt,
                               ReadOnlySpan<int> dynTable, CellOdoTail tail, int count,
                               CellGroupShape?[]? elems = null)
    {
        string run = RunOf(fixedAt, width, dynFixedAt, dynTable);
        var pos = RunPositions(dynFixedAt, dynTable);
        var sb = new System.Text.StringBuilder(run.Length);
        int at = 0;
        // The occurrences beyond the OCCURS DEPENDING count drop their COMPONENTS with their fixed run (kb/Work PB244):
        // the table is the group's trailing storage, so they are its last ones.
        for (int k = 0, keep = pos.Length - tail.CutComponents(count); k < keep; k++)
        {
            sb.Append(run, at, pos[k] - at);
            at = pos[k];
            sb.Append(ComponentAt(dynBase + k, dynTable[k], elems?[k]));
        }
        // The OCCURS DEPENDING table is the TRAILING storage (§13.18.38.3 SR22) of the run, so the group's current
        // extent is its maximum image less the occurrences beyond the count (§13.18.38.4 GR8).
        sb.Append(run, at, run.Length - at - tail.CutAt(count));
        return sb.ToString();
    }

    /// <summary>The EXTENT TABLE of that contiguous image (determination D-FRA (v); kb/Work PB1053): each
    /// component's fixed-run position and its current length — what a WRITE / REWRITE / RELEASE of the group sends
    /// beside <see cref="ContiguousAt"/>, the declared group's <c>CurrentExtents()</c> composed from the cell.</summary>
    public RecordExtents ContiguousExtentsAt(int width, int dynBase, ReadOnlySpan<int> dynFixedAt,
                                             ReadOnlySpan<int> dynMax, ReadOnlySpan<int> dynStructure,
                                             ReadOnlySpan<int> dynTable, CellOdoTail tail, int count)
    {
        var pos = RunPositions(dynFixedAt, dynTable);
        var layout = LayoutOf(width - Reserved(dynTable), pos, dynMax, dynStructure, dynTable, tail);
        // The OCCURS DEPENDING table is the layout's LAST component (CobolContiguousLayout.OdoTail): the record's
        // own length says how many occurrences it holds, so its extent is the count's share of the table.
        int comps = pos.Length;
        var lengths = new int[comps + (tail.Present ? 1 : 0)];
        for (int k = 0; k < comps; k++) lengths[k] = ComponentAt(dynBase + k, dynTable[k]).Length;
        if (tail.Present) lengths[comps] = tail.Width - tail.CutAt(count);
        return layout.ExtentsFrom(lengths);
    }

    /// <summary>The contiguous layout of a cell-backed variable-length group over its fixed run of
    /// <paramref name="runWidth"/> positions: a dynamic-length item contributes its content character for character
    /// (unit 1), a table whole occurrences (unit = its element width, maximum = its maximum capacity) — the units a
    /// declared group's generated layout gives the same members (<c>GroupImageCodec.ContiguousLayout</c>).
    /// <paramref name="dynStructure"/> is each component's <see cref="CobolDynStructure.Code"/> (0 = none; ISO
    /// §12.3.7.4 GR18/GR19; kb/Work PB1094) — constant data the compiler read off the same item, so this layout and a
    /// declared group's cannot disagree. A group that holds an OCCURS DEPENDING table (<paramref name="tail"/>) has it
    /// as the LAST component (§13.18.38.3 SR22; <see cref="CobolContiguousLayout"/>'s <c>OdoTail</c>), taken out of
    /// the fixed run: <paramref name="runWidth"/> includes it at its maximum, the layout's fixed total does not.</summary>
    private static CobolContiguousLayout LayoutOf(int runWidth, int[] pos, ReadOnlySpan<int> dynMax,
                                                  ReadOnlySpan<int> dynStructure, ReadOnlySpan<int> dynTable,
                                                  CellOdoTail tail)
    {
        int comps = pos.Length, n = comps + (tail.Present ? 1 : 0);
        var at = new int[n];
        var units = new int[n];
        var max = new long[n];
        for (int k = 0; k < comps; k++) { at[k] = pos[k]; units[k] = dynTable[k] > 0 ? dynTable[k] : 1; max[k] = dynMax[k]; }
        if (tail.Present) { at[comps] = runWidth - tail.Width; units[comps] = tail.Elem; max[comps] = tail.Max; }
        int[]? structure = null;
        if (dynStructure.ContainsAnyExcept(0))
        {
            structure = new int[n];
            dynStructure.CopyTo(structure);
        }
        return new CobolContiguousLayout(tail.Present ? runWidth - tail.Width : runWidth, at, units, max, structure,
            OdoTail: tail.Present);
    }

    /// <summary>Make a contiguous image the group's content — the inverse of <see cref="ContiguousAt"/>, through the
    /// ONE decomposition (<see cref="CobolContiguousLayout.Decompose"/>, determination D-FRA): by the image's own
    /// <paramref name="extents"/> when they describe it (a record read back with its extent table — D-FRA (v),
    /// kb/Work PB1053), otherwise by the take step, each component taking as many characters as the image holds
    /// beyond the fixed material still to come, up to its maximum size.</summary>
    public void StoreContiguousAt(int fixedAt, int width, int dynBase, ReadOnlySpan<int> dynFixedAt,
                                  ReadOnlySpan<int> dynMax, ReadOnlySpan<int> dynStructure, ReadOnlySpan<int> dynTable,
                                  CellOdoTail tail, string image, RecordExtents? extents = null, bool fixedForm = false)
    {
        var layout = LayoutOf(width - Reserved(dynTable), RunPositions(dynFixedAt, dynTable), dynMax, dynStructure, dynTable, tail);
        StoreVarGroupAt(fixedAt, width, dynBase, dynFixedAt, dynMax, dynTable, tail, int.MaxValue,
            layout.Decompose(image ?? "", extents, fixedForm));
    }

    /// <summary>The positions the tables reserve in a group's window — the window width less its fixed run.</summary>
    private static int Reserved(ReadOnlySpan<int> dynTable)
    {
        int sum = 0;
        foreach (int w in dynTable) sum += w;
        return sum;
    }

    /// <summary>Each component's position in the group's FIXED RUN: its position in the window less the
    /// reservations of the tables before it.</summary>
    private static int[] RunPositions(ReadOnlySpan<int> dynFixedAt, ReadOnlySpan<int> dynTable)
    {
        var pos = new int[dynFixedAt.Length];
        int cut = 0;
        for (int k = 0; k < pos.Length; k++)
        {
            pos[k] = dynFixedAt[k] - cut;
            cut += dynTable[k];
        }
        return pos;
    }

    /// <summary>The group's FIXED RUN: its <paramref name="width"/>-position window with each table's reservation
    /// cut out.</summary>
    private string RunOf(int fixedAt, int width, ReadOnlySpan<int> dynFixedAt, ReadOnlySpan<int> dynTable)
    {
        string window = FixedRun(fixedAt, width);
        if (!dynTable.ContainsAnyExcept(0)) return window;
        var sb = new System.Text.StringBuilder(window.Length);
        int at = 0;
        for (int k = 0; k < dynFixedAt.Length; k++)
        {
            if (dynTable[k] == 0) continue;
            sb.Append(window, at, dynFixedAt[k] - at);
            at = dynFixedAt[k] + dynTable[k];
        }
        sb.Append(window, at, window.Length - at);
        return sb.ToString();
    }

    /// <summary>Store <paramref name="run"/> as the group's FIXED RUN — the receiving twin of <see cref="RunOf"/>:
    /// padded or truncated to the run's width and laid around the tables' reservations, which keep their
    /// placeholder positions.</summary>
    private void StoreRun(int fixedAt, int width, ReadOnlySpan<int> dynFixedAt, ReadOnlySpan<int> dynTable, string run)
    {
        run = CobolString.Store(run, width - Reserved(dynTable));
        if (dynTable.ContainsAnyExcept(0))
        {
            string window = FixedRun(fixedAt, width);
            var sb = new System.Text.StringBuilder(width);
            int at = 0, r = 0;
            for (int k = 0; k < dynFixedAt.Length; k++)
            {
                if (dynTable[k] == 0) continue;
                int len = dynFixedAt[k] - at;
                sb.Append(run, r, len).Append(window, dynFixedAt[k], dynTable[k]);
                r += len;
                at = dynFixedAt[k] + dynTable[k];
            }
            run = sb.Append(run, r, run.Length - r).ToString();
        }
        Ref = CobolString.WindowInto(Ref, fixedAt + 1, width, run);
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
