// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime;

namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ THE ONE BIT-GRANULAR LAYOUT (ISO §8.5.1.6.3 "Alignment of data items of usage bit"; design D19, fix-queue
/// PB43). A <c>USAGE BIT</c> item occupies BITS — <b>§13.18.60.4 GR5</b>: "The USAGE BIT clause specifies that
/// bits shall be used to represent a boolean data item … The alignment of a data item described with USAGE BIT is
/// specified in 8.5.1.6.3." A boolean item WITHOUT a USAGE clause is a different question and is NOT handled here:
/// §13.18.60.3 SR13(b) implies USAGE DISPLAY and §13.18.60.4 GR7 makes DISPLAY "an alphanumeric coded character
/// set", so one character per boolean position is REQUIRED for it (D-B1's surviving half).
///
/// <para><b>Why a separate walk instead of teaching <c>ImageWidth</c> to add bits.</b> Every other width in this
/// model is a SUM of per-child widths, which works because each child starts where the previous one ended. Bit
/// items break that: two same-level bit items SHARE a byte, and a bit item after anything else skips to the next
/// byte. Position therefore depends on the PREVIOUS sibling's kind and level, which a sum cannot express — so the
/// layout is a cursor walk, and the character widths derive FROM it rather than the other way round.</para>
///
/// <para>⛔ <b>ONLY reached for a subtree that actually contains a USAGE BIT leaf</b>
/// (<see cref="DataItem.HasBitDescendant"/>). Without one there are no sub-byte runs, so this walk and the plain
/// character sum agree BY CONSTRUCTION — gating on it makes the change provably byte-identical for every program
/// that writes no <c>USAGE BIT</c>, which is the entire pre-existing corpus. Same discipline as PB41's scale-0
/// fast path: the new machinery must not perturb the text it is not needed for.</para>
/// </summary>
internal static class BitLayout
{
    /// <summary>Bits per character position. §8.1.2 makes this implementor-specified; WiseOwl COBOL pins <b>8</b>,
    /// consistent with <see cref="DataItem.ByteWidth"/>'s "DISPLAY = 1 byte per character position" and recorded
    /// in <c>docs/CONFORMANCE.md</c> §4.2.16.</summary>
    public const int BitsPerCharacter = 8;

    /// <summary>True when this leaf occupies BITS — a boolean item whose usage is explicitly BIT.</summary>
    public static bool IsBitLeaf(DataItem item) =>
        item.IsElementary && item.Pic is { Category: PicCategory.Boolean, Usage: Usage.Bit };

    /// <summary>True for a bit item in §8.5.1.6.3's sense — "an elementary bit data item or bit group item": a bit
    /// leaf, or a GROUP-USAGE BIT group (§13.18.29.4 GR1a — "a bit group and also a bit data item"; D20/PB79).</summary>
    public static bool IsBitItem(DataItem item) => IsBitLeaf(item) || item.GroupUsage is GroupUsage.Bit;

    /// <summary>
    /// ⛔ THE ONE PLACEMENT PREDICATE: does <paramref name="c"/> continue its predecessor's byte, or start a new
    /// one? True is §8.5.1.6.3's ONLY byte-sharing case; false is "the first bit position of the first available
    /// byte", whether the reason is rule 2 (a different-level or non-bit predecessor, or no predecessor at all),
    /// rule 3 (a non-bit item advancing to its natural boundary) or §13.18.1.4 GR1 (an ALIGNED subject).
    ///
    /// <para>⛔ It is a METHOD because the test was previously written out TWICE — once in
    /// <see cref="ExtentBits"/> and once in <see cref="StartBitWithin"/> — which is this repository's most
    /// reproducible defect shape: a dispatch with two arms and only one of them fixed. Adding the ALIGNED case
    /// to one copy and not the other would have made an item's OFFSET disagree with the group's EXTENT, a
    /// silently wrong layout with no diagnostic anywhere (kb/Work PB487).</para>
    ///
    /// <para><b>§13.18.1.4 GR1</b>: "An ALIGNED clause causes the subject of the entry to be aligned on the first
    /// bit of the first available byte boundary. Implicit filler bits may be generated to complete the assignment
    /// of bits as described in 8.5.1.6.3." So an ALIGNED item NEVER shares a byte with its predecessor — one
    /// conjunct, and the round-up both callers already perform does the rest. <b>GR3</b> ("when an ALIGNED clause
    /// is not specified, bit data items are aligned in accordance with 8.5.1.6.3") is the false arm, unchanged.</para>
    ///
    /// <para><b>§13.18.57.4 GR2 d)</b>: a TYPE subject whose type describes a group "is aligned as though it were a
    /// level 1 item", and §8.5.1.6.3 puts "a level 1 bit group … at the first bit of a byte" — the second conjunct
    /// (<see cref="DataItem.AlignedAsLevelOne"/>; kb/Work PB1569).</para>
    /// </summary>
    public static bool SharesByteWith(DataItem? prev, DataItem c) =>
        !c.IsAligned                                            // §13.18.1.4 GR1 — an ALIGNED subject never shares
        && !c.AlignedAsLevelOne                                 // §13.18.57.4 GR2 d) — nor does a group-typed subject
        && IsBitItem(c) && prev is not null && IsBitItem(prev)   // §8.5.1.6.3 rules 1 and 2 — both sides bit items…
        && prev.Level == c.Level;                                // …"of the same level"

    /// <summary>⛔ THE ONE ANSWER TO "which of these siblings SHARE bytes" (ISO §8.5.1.6.3): a RUN is a maximal
    /// stretch of consecutive same-level bit members — "an elementary bit data item immediately following an
    /// elementary bit data item or bit group item of the same level" — and it is what makes a run's members
    /// occupy ONE image slice instead of one each.
    /// <para>⛔ IT LIVES HERE BECAUSE IT HAS TWO READERS, and it had two SPELLINGS: <c>PhysicalModel</c>'s
    /// physical-field walk (the record-struct / <c>AsImage()</c> lane) computed it, and the compile-time image
    /// SEED — the only composer a Tier-B REDEFINES group ever reaches — did not, so a MIXED bit/character group's
    /// seed padded each bit member to its own byte and every following member shifted (kb/Work PB584). One law,
    /// one computation, two composers.</para>
    /// <para>A REDEFINING sibling is never a run member: it overlays storage its target already placed.</para>
    /// <para>⛔ A run CONTINUES only where <see cref="SharesByteWith"/> says the member shares its predecessor's
    /// byte — the same predicate the extent and offset walks read. The run's image packs its members' carriers
    /// back to back, so a member the placement walks start on a fresh byte (an ALIGNED one, §13.18.1.4 GR1; a
    /// group-typed TYPE subject, §13.18.57.4 GR2 d)) must LEAD a new run: continuing the run on a same-level test
    /// alone packed <c>05 F PIC 1 USAGE BIT. 05 G PIC 1 USAGE BIT ALIGNED.</c> into ONE byte while
    /// <c>FUNCTION BYTE-LENGTH</c> — the extent walk — counted two (kb/Work PB1569's sibling sweep).</para></summary>
    public static BitRunMap RunsOf(IEnumerable<DataItem> siblings)
    {
        var list = siblings as IList<DataItem> ?? siblings.ToList();
        // ⛔ THE NO-BIT-MEMBER FAST PATH, and it is the same discipline the class header states for the whole
        // walk: without a USAGE BIT member there are no sub-byte runs, so the map is empty BY CONSTRUCTION and
        // the composers behave exactly as they did before runs existed. Both callers run over EVERY sibling list
        // of EVERY program (the physical-field walk and the image seed), and the overwhelming majority of them
        // hold no bit item at all — allocating a dictionary and a set to say so is a cost with nothing to show.
        int first = -1;
        for (int i = 0; i < list.Count; i++)
            if (RunMember(list[i]) && list[i].RedefinesTargetName is null) { first = i; break; }
        if (first < 0) return BitRunMap.Empty;
        var start = new Dictionary<DataItem, IReadOnlyList<DataItem>>(ReferenceEqualityComparer.Instance);
        var inRun = new HashSet<DataItem>(ReferenceEqualityComparer.Instance);
        for (int i = first; i < list.Count; i++)
        {
            if (!RunMember(list[i]) || list[i].RedefinesTargetName is not null || inRun.Contains(list[i])) continue;
            var run = new List<DataItem> { list[i] };
            for (int j = i + 1; j < list.Count && RunMember(list[j])
                                && list[j].RedefinesTargetName is null
                                && SharesByteWith(list[j - 1], list[j]); j++)
                run.Add(list[j]);
            foreach (var m in run) inRun.Add(m);
            start[list[i]] = run;
        }
        return new BitRunMap(start, inRun);
    }

    /// <summary>A §8.5.1.6.3 RUN member — "an elementary bit data item or bit group item of the same level"
    /// (D20/PB79): a bit LEAF, or a GROUP-USAGE BIT group, which contributes its <c>AsBits()</c> carrier and its
    /// exact extent. Only siblings WITHIN a group form runs — level-01 records never share a byte.</summary>
    private static bool RunMember(DataItem x) =>
        IsBitLeaf(x) || (x.GroupUsage is GroupUsage.Bit && x.Parent is not null);

    /// <summary>The bit positions a §8.5.1.6.3 run MEMBER contributes — a bit leaf's declared boolean positions, a
    /// bit group's exact extent (its as-if PICTURE 1(m) length), times its OCCURS (D20/PB79).
    /// <para>⛔ Expressed through <see cref="WidthBits"/> rather than re-reading <c>Pic</c>/<c>AsIfPic</c>: for a
    /// bit item the two spellings are the SAME number by construction (that method's first arm is the bit leaf's
    /// PICTURE length and its group arm is <see cref="ExtentBits"/>, which is exactly what
    /// <see cref="DataItem.AsIfPic"/> reports as §13.18.29.4 GR1b's <c>m</c>), and one spelling means the
    /// per-occurrence extent used as a WIDTH (the Tier-B bit window, kb/Work PB203) and the one used as a STRIDE
    /// (a subscripted bit member) cannot drift apart.</para></summary>
    public static int RunBits(DataItem m) =>
        m.Occurs is not { } n || n <= 1 ? WidthBits(m)
        : StrideBits(m) * (n - 1) + WidthBits(m);

    /// <summary>The bit extent of one item PER OCCURRENCE — a bit leaf's declared boolean-position count, else the
    /// item's byte extent expressed in bits. A group defers to <see cref="ExtentBits"/> so a nested bit run is laid
    /// out by the same rules (§8.5.1.6.3 applies "within that group" at every level).
    /// <para>⛔ This is a WIDTH, not a STRIDE. For an ALIGNED multiple-occurrence item the two differ — use
    /// <see cref="StrideBits"/> wherever the number multiplies a subscript.</para></summary>
    public static int WidthBits(DataItem item) =>
        IsBitLeaf(item) ? item.Pic!.Length
        : item.IsElementary ? item.ElementaryByteWidth * BitsPerCharacter
        : ExtentBits(item);

    /// <summary>
    /// ⛔ THE ONE SUBSCRIPT STRIDE in bits — the distance from one occurrence's first bit to the next
    /// occurrence's first bit. Equal to <see cref="WidthBits"/> for everything except an ALIGNED item, which is
    /// exactly what <b>§13.18.1.4 GR2</b> requires: "An ALIGNED clause specified for a multiple-occurrence data
    /// item applies to each occurrence of that item" — so EVERY occurrence, not just the first, begins at the
    /// first bit of a byte, and the stride is the per-occurrence width rounded up to a byte.
    ///
    /// <para>Because the two coincide whenever <c>IsAligned</c> is false, converting a call site from
    /// <see cref="WidthBits"/> to this method is provably byte-identical for every program that writes no ALIGNED
    /// clause — the whole pre-PB487 corpus — which is the same discipline the class header records for gating the
    /// bit walk on <c>HasBitDescendant</c>.</para>
    ///
    /// <para>⚠ The trailing filler of the LAST occurrence is NOT part of the item's extent: §8.5.1.6.3 generates
    /// implicit filler "as needed to advance alignment to a required natural boundary for the NEXT item", so a
    /// same-level bit item following an ALIGNED table still starts at the next BIT position after the final
    /// occurrence. <see cref="RunBits"/> therefore counts <c>stride × (n−1) + width</c>, not <c>stride × n</c>;
    /// the group's own tail filler is rule 4's round-up, applied once by <see cref="ExtentBits"/>.</para>
    /// </summary>
    public static int StrideBits(DataItem item) =>
        item.IsAligned ? RoundUpToByte(WidthBits(item)) : WidthBits(item);

    /// <summary>
    /// The total bit extent of <paramref name="group"/> — the §8.5.1.6.3 cursor walk over its NON-redefining
    /// children (a redefining child overlays its target and adds no storage, §13.18.44).
    ///
    /// <para>The four placement rules, in the order the standard states them:</para>
    /// <list type="number">
    ///   <item>A bit item <b>immediately following an elementary bit data item or bit group item OF THE SAME
    ///         LEVEL</b> goes at the next bit position — the ONLY case that shares a byte.</item>
    ///   <item>Any OTHER bit item is aligned "at the first bit position of the first available byte" — after a
    ///         character item, after a bit item of a DIFFERENT level, or as the group's first item.</item>
    ///   <item>A non-bit item advances to its natural boundary first; the skipped bits are §8.5.1.6.3's implicit
    ///         filler "as needed to advance alignment to a required natural boundary for the next item".</item>
    ///   <item>A trailing partial byte is filled: "Following a bit data item that is the last data item in a
    ///         record that is an alphanumeric group or strongly-typed group item, as needed to increase the number
    ///         of bits to fill an integral number of characters." §13.18.29.4 GR3 makes every group without a
    ///         GROUP-USAGE clause an alphanumeric group item, so this fires for all of them.</item>
    /// </list>
    /// <para>⚠ Every filler bit counted above is REQUIRED to be counted by <b>§15.50.4 r5</b> ("the returned
    /// length shall include the number of implicit FILLER positions"). It is counted by construction — the cursor
    /// advances through filler rather than skipping it.</para>
    /// <para>⚠ The NOTE under the trailing-filler rule excludes "the end of a record that is entirely a bit group,
    /// the end of a level 77 item, or the end of a level 1 elementary item" — which is why an ELEMENTARY bit item
    /// never comes through here and keeps its exact bit count (see <see cref="WidthBits"/>).</para>
    /// </summary>
    public static int ExtentBits(DataItem group)
    {
        int cursor = Walk(group, null);
        // Rule 4 — the trailing filler: stated for "a record that is an alphanumeric group or strongly-typed group
        // item" (§13.18.29.4 GR3 makes every group WITHOUT a GROUP-USAGE clause alphanumeric), and its NOTE excludes
        // "the end of a record that is entirely a bit group" — so a GROUP-USAGE BIT group (D20/PB79) keeps its EXACT
        // bit extent (its PICTURE 1(m) length, §15.50.4 r1); its character OCCUPANCY is still Characters(extent).
        return group.GroupUsage is GroupUsage.Bit ? cursor : RoundUpToByte(cursor);
    }

    /// <summary>The start-bit offset of DIRECT child <paramref name="child"/> within <paramref name="group"/>,
    /// by the same four placement rules as <see cref="ExtentBits"/> — one walk, one law (kb/Work PB132;
    /// §14.9.4.3 SR6/SR8 ask whether a BY REFERENCE bit argument "is aligned on a byte boundary"). A
    /// redefining child overlays its target (§13.18.44), so its offset IS the target's — resolved here by
    /// name. Returns -1 when the child is not found (an unmodelled overlay chain — callers must not reject
    /// on -1).</summary>
    public static int StartBitWithin(DataItem group, DataItem child)
    {
        var children = group.Children;
        for (int i = 0; i < children.Count; i++)
            if (ReferenceEquals(children[i], child)) return ChildStarts(group)[i];
        return -1;
    }

    /// <summary>⛔ EVERY DIRECT CHILD'S start bit RELATIVE to <paramref name="group"/>, in <c>group.Children</c>
    /// order, from <see cref="Walk"/> — the one §8.5.1.6.3 placement. A redefining child's entry is its target's
    /// (§13.18.44.4 GR1); -1 marks an unresolvable overlay chain, exactly as <see cref="StartBitWithin"/> reports
    /// it. The REDEFINES-class walk (<c>DataBinder.AssignClassOffsets</c>) adds the group's own absolute bit
    /// offset to each — it must not run a cursor of its own (kb/Work PB1572).</summary>
    public static int[] ChildStarts(DataItem group)
    {
        var starts = new int[group.Children.Count];
        Walk(group, starts);
        return starts;
    }

    /// <summary>
    /// ⛔ THE ONE §8.5.1.6.3 CURSOR WALK over a group's DIRECT children — <see cref="ExtentBits"/>,
    /// <see cref="StartBitWithin"/> and <see cref="ChildStarts"/> all read it, so a placement is written once. It had
    /// THREE copies of the loop (kb/Work PB1572): the third, in <c>DataBinder.AssignClassOffsets</c>, started its
    /// cursor at the group's ABSOLUTE bit offset and rounded THAT up to a byte before the first child, so a bit group
    /// placed at bit 1 of a byte (§8.5.1.6.3 — "a bit group item immediately following … elementary bit data item …
    /// of the same level") put its first member at bit 8 instead of bit 1, against "the alignment of the start of a
    /// group item and the alignment of the first item within that group, when the first item is a bit data item, are
    /// at the same bit position in storage". The cursor here is RELATIVE to the group's start, so the first child is
    /// at 0 whatever the group's own offset; callers add their origin.
    /// <para>When <paramref name="starts"/> is non-null it receives each child's start bit relative to the group;
    /// a child that REDEFINES a sibling takes its target's start, or -1 when the target is not among the siblings.
    /// Returns the cursor after the last non-redefining child, before rule 4's trailing filler.</para></summary>
    private static int Walk(DataItem group, int[]? starts)
    {
        var children = group.Children;
        int cursor = 0;
        DataItem? prev = null;
        for (int i = 0; i < children.Count; i++)
        {
            var c = children[i];
            if (c.RedefinesTargetName is not null) continue;   // overlays its target — no advance (§13.18.44)

            // Rule 1 vs 2 (and §13.18.1.4 GR1) — ONE predicate.
            if (!SharesByteWith(prev, c)) cursor = RoundUpToByte(cursor);   // rules 2 and 3 — same advance, different reasons
            if (starts is not null) starts[i] = cursor;
            cursor += RunBits(c);   // width, or the ALIGNED per-occurrence stride × (n−1) + width (GR2)
            prev = c;
        }
        if (starts is null) return cursor;
        // A redefining child begins at its target's first position (§13.18.44.4 GR1). §13.18.44.3 SR17 bars a
        // dynamic item under REDEFINES, so chasing the overlay chain terminates.
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i].RedefinesTargetName is null) continue;
            int at = -1;
            var c = children[i];
            for (int hops = 0; hops < 64 && c.RedefinesTargetName is { } name; hops++)
            {
                int t = IndexOfNamed(children, name);
                if (t < 0) break;
                c = children[t];
                if (c.RedefinesTargetName is null) at = starts[t];
            }
            starts[i] = at;
        }
        return cursor;
    }

    private static int IndexOfNamed(IReadOnlyList<DataItem> siblings, string name)
    {
        for (int i = 0; i < siblings.Count; i++)
            if (CobolNames.Same(siblings[i].CobolName, name)) return i;
        return -1;
    }

    /// <summary>The next byte boundary at or after <paramref name="bits"/> — "the first bit position of the first
    /// available byte" (§8.5.1.6.3), and equally the implicit-filler advance to a natural boundary. Already
    /// byte-aligned input is returned unchanged, so no phantom filler byte is generated.</summary>
    public static int RoundUpToByte(int bits) =>
        (bits + BitsPerCharacter - 1) / BitsPerCharacter * BitsPerCharacter;

    /// <summary>The bit offset of <paramref name="item"/> within <paramref name="ancestor"/> — the sum of each
    /// intervening level's <see cref="StartBitWithin"/> placement, so a DEEP descendant is located by the same four
    /// rules as a direct child. Returns -1 when any level cannot be resolved (an unmodelled overlay chain, or
    /// <paramref name="item"/> not being a descendant); callers must not compute a layout from -1.
    /// <para>⛔ This is the honest "fixed prefix" for an OCCURS DEPENDING extent over a bit-bearing subtree
    /// (§13.18.38.4 GR8 read through §8.5.1.6.3), and it is NOT <c>ExtentBits(ancestor) − elem × max</c>: rule 4's
    /// trailing filler belongs AFTER the variable tail, so subtracting the tail from a byte-rounded total charges
    /// the filler to the prefix. The group's own filler is recovered by taking the CEILING of the current extent
    /// (<see cref="Characters"/>), which is what rule 4 says it is.</para></summary>
    public static int StartBitOf(DataItem ancestor, DataItem item)
    {
        int off = 0;
        for (DataItem? n = item; !ReferenceEquals(n, ancestor); n = n.Parent)
        {
            if (n?.Parent is not { } parent) return -1;
            int at = StartBitWithin(parent, n);
            if (at < 0) return -1;
            off += at;
        }
        return off;
    }

    /// <summary>The character positions a bit extent occupies — the ceiling, which is also the shape
    /// <b>§15.50.4 r9</b> requires ("if argument-1 does not occupy an integral number of positions, the returned
    /// value is rounded to the next larger integer value"). With rule 4 above the extent is already integral for a
    /// group; the ceiling is what makes an ELEMENTARY bit item's character occupancy right.</summary>
    public static int Characters(int bits) => (bits + BitsPerCharacter - 1) / BitsPerCharacter;
}

/// <summary>The §8.5.1.6.3 bit RUNS of one sibling list — see <see cref="BitLayout.RunsOf"/>. A run is keyed by
/// the member that LEADS it; every member of a run (leader included) answers true to
/// <see cref="IsInRun"/>, so a composer emits the run's whole slice at the leader and nothing at the
/// continuations, exactly as <c>PhysicalModel.Physical.BitRun</c> / <c>Width 0</c> express it.</summary>
internal sealed class BitRunMap(IReadOnlyDictionary<DataItem, IReadOnlyList<DataItem>> start, IReadOnlySet<DataItem> inRun)
{
    /// <summary>The map of a sibling list with no <c>USAGE BIT</c> member — shared, because that is nearly every
    /// sibling list in nearly every program and the answer carries no state.</summary>
    public static readonly BitRunMap Empty =
        new(new Dictionary<DataItem, IReadOnlyList<DataItem>>(), new HashSet<DataItem>());

    /// <summary>The run this item LEADS, or null when it leads none (a continuation, or not a bit member).</summary>
    public IReadOnlyList<DataItem>? RunLedBy(DataItem item) => start.TryGetValue(item, out var r) ? r : null;

    /// <summary>True when this item belongs to a run — as its leader or as a continuation.</summary>
    public bool IsInRun(DataItem item) => inRun.Contains(item);
}
