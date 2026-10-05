// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Generic;
using System.Linq;
using CobolNet.Binding;

namespace CobolNet.Binding.Model;

/// <summary>
/// The ONE physical offset/width authority over a record tree (rearchitecture PHASE 05; DESIGN-data-model §2.6).
/// PHASE-FREE since P5 Step 8: leaf widths come from the DECLARED shape (<c>DataItem.ElementaryImageWidth</c>, a
/// pure Pic fact proven equal to the <see cref="StorageForm"/> width by the Step-2 corpus identity #3), so every
/// member here is callable at bind time AND emit time. Consolidates the formerly-duplicated geometry
/// (prove-then-delete: the Step-4/8 corpus asserts + the Sort/Keyed goldens gate each fold before Step 9 deletes
/// the copies): <c>DataItem.ImageWidth</c>'s recursion, <c>OdoModel.PhysicalWidth</c> (the ODO/GR8 physical
/// extent), the Sort geometry (<c>SortPhysicalWidth</c>/<c>SortOffsetInRecord</c>/<c>SortPlainOffset</c>), and the
/// Keyed geometry (<c>KeyedAreaOffset</c>/<c>KeyedKeyIndex</c> + the emitter's <c>KeyedImageOffset</c> twin).
/// <para><b>The Step-8 basis unification (a REAL fix, gated by the failing-first <c>KeyedOffsetSpecTests</c>):</b>
/// the legacy Keyed copies advanced by the redefined item's OWN <see cref="ImageWidth"/>, so a key sitting AFTER a
/// REDEFINES whose redefiner is WIDER than its target got a byte offset that disagreed with the emitted record
/// codec's physical layout — the runtime extracted the WRONG bytes and a same-byte-position operand from a sibling
/// record description failed to match its key. <see cref="OffsetOf"/> sits on the codec-correct
/// <see cref="PhysicalWidth"/> basis (ISO §12.4.5.12.4 GR4 — "the identical BYTE POSITIONS ... in any one record
/// description entry are implicitly referenced as keys for all other record description entries";
/// §14.9.41.3 SR6b — leftmost character position correspondence).</para>
/// </summary>
internal static class RecordLayout
{
    /// <summary>⛔ THE ONE WIDTH RULE of an item's character image — <c>DataItem.ImageWidth</c> IS this (kb/Work
    /// PB1665: the rule was written twice, as a mirror the StorageFormPass identity audit compared, and both copies
    /// counted a national leaf ONCE inside an alphanumeric group). Per THIS item's occurrence (a parent multiplies
    /// by this item's own OCCURS count): a leaf's declared image width (digits + separate-sign, or PIC length,
    /// national in POSITIONS); a group's sum over its NON-redefining children of (child contribution × the child's
    /// own fixed-OCCURS count) — every OCCURS position is part of the group image (ISO §14.9), and a REDEFINING
    /// child overlays its target and adds no storage (§13.18.44).
    /// <para>⛔ THE UNIT OF A GROUP'S WIDTH IS ITS OWN CLASS'S. A <c>GROUP-USAGE NATIONAL</c> group is as-if
    /// <c>PICTURE N(m)</c> (§13.18.29.4 GR2b), so its m is NATIONAL POSITIONS and every item beneath it is counted
    /// in them. Any other group is an alphanumeric group (§8.5.2.1: "an alphanumeric group item has class and
    /// category alphanumeric"), moved "exactly as if it were an alphanumeric to alphanumeric elementary move"
    /// (§14.9.25.4 GR4) — a string of STORAGE characters, in which a national position occupies
    /// <c>CobolBits.BytesPerNational</c> of them (§13.18.60.4 GR8, pinned by D-N1). Counting the national leaf once
    /// there made a group MOVE copy fewer characters than the sending group's image holds, so the trailing
    /// members were lost (kb/Work PB1665: <c>MOVE G5 TO G6</c> left <c>B</c> as spaces). Every consumer of a group's
    /// character-image width — MOVE, the receiving store, the boundary windows, ref-mod positions, LENGTH folds,
    /// the Tier-B strides — reads this one answer, so none restates the unit.</para></summary>
    public static int ImageWidth(DataItem item) =>
        item.IsElementary ? item.ElementaryImageWidth
        // D19/PB43 — a bit-bearing subtree is laid out by the ONE §8.5.1.6.3 walk, never summed.
        : item.HasBitDescendant ? BitLayout.Characters(BitLayout.ExtentBits(item))
        : item.Children.Where(c => c.RedefinesTargetName is null).Sum(c => ImageContribution(item, c) * (c.Occurs ?? 1));

    /// <summary>ONE occurrence of <paramref name="child"/> in the character positions of its enclosing
    /// <paramref name="group"/> — see <see cref="ImageWidth"/>: its own width, doubled to storage characters when
    /// the enclosing group is alphanumeric and the child is of class national.</summary>
    private static int ImageContribution(DataItem group, DataItem child)
    {
        int w = ImageWidth(child);
        return !InNationalGroup(group) && IsNationalClass(child) ? w * CobolNet.Runtime.CobolBits.BytesPerNational : w;
    }

    /// <summary>A national item is an elementary item of usage national or a national group
    /// (§13.18.29.4 GR2b treats the group as an elementary national item).</summary>
    private static bool IsNationalClass(DataItem item) =>
        item.IsElementary ? item.Pic?.Usage is Usage.National : InNationalGroup(item);

    /// <summary>The item is a GROUP-USAGE NATIONAL group or beneath one. Asked of the ancestor chain rather than of
    /// the group's own <see cref="DataItem.GroupUsage"/> alone, because usage is propagated to subordinate groups
    /// by a later bind walk (§13.16.4 GR2's implied clause) and a width is a pure function of the declared shape,
    /// callable before it runs.</summary>
    private static bool InNationalGroup(DataItem item)
    {
        for (var p = item; p is not null; p = p.Parent)
            if (p.IsGroup && p.GroupUsage is GroupUsage.National) return true;
        return false;
    }

    /// <summary>The tier-aware PHYSICAL image width of an item — the extent the emitted <c>AsImage()</c>/<c>FromImage()</c>
    /// codec spans (COBOLNET_DESIGN §4.2): a REDEFINING child overlays its target and contributes nothing; a Tier-B
    /// <see cref="RedefinesTier.StringCanonical"/> class contributes its ONE backing (class-max
    /// <see cref="RedefinesClass.Width"/>) once at the canonical member. For the class-free common subtree this
    /// equals <see cref="ImageWidth"/>. Mirrors <c>OdoModel.PhysicalWidth</c> / <c>SortPhysicalWidth</c> (the ODO
    /// §13.18.38 GR8 extent) — the raw <see cref="ImageWidth"/> would over- or under-count a group containing a
    /// redefines class whose redefiner is wider than the redefined item.</summary>
    public static int PhysicalWidth(DataItem item)
    {
        // ⛔ THE CODEC BASIS IS BYTES, NOT CARRIER POSITIONS (kb/Work PB327). This walk is the record/frame
        // geometry — §12.4.5.12.4 GR4 states key correspondence over "the identical BYTE POSITIONS", §14.9.30.4
        // GR14/GR15 state the short/long-record rules over "the number of BYTES in the record", and
        // §14.9.3.4 GR3 allocates "the number of BYTES required to hold an item". ByteWidth IS ImageWidth for
        // every leaf kind but NATIONAL, whose §13.18.60.4 GR8 size this implementation pins at two bytes per
        // position (D-N1) — so this read is byte-identical everywhere else and is the whole of the geometry
        // change that admits a national leaf to a file record. <see cref="ImageWidth"/> stays the CARRIER
        // authority (StorageFormPass's form widths, the §13.18.29.4 GR2b as-if PICTURE N(m) length).
        if (!item.IsGroup) return item.ByteWidth;
        // D19/PB43 — a bit-bearing subtree's extent comes from the §8.5.1.6.3 walk. A REDEFINES class inside such
        // a group is not reachable here: D-N2's byte≠char containment already refuses a sub-byte overlay, and
        // PB43 records the sub-byte redefiner as explicitly OUT of scope and loud rather than rounded.
        if (item.HasBitDescendant) return BitLayout.Characters(BitLayout.ExtentBits(item));
        int w = 0;
        foreach (var c in item.Children)
        {
            // The class-backing substitution applies ONLY where the child is a top-level class MEMBER (the
            // overlay root) — a SUBORDINATE of a member carries an inherited .Class link but occupies its own
            // positions inside the member's window (the P5.8 area-class find: skipping subordinates of a
            // multi-record FD's synthesized AREA class collapsed the record width to 0). The CANONICAL member —
            // which may itself be a REDEFINER (the classifier picks the class's storage owner, not necessarily
            // the target) — contributes the ONE backing at class-max width; other members contribute nothing.
            if (c.Class is { Tier: RedefinesTier.StringCanonical } cls && cls.Members.Contains(c))
            {
                if (c.IsCanonical) w += cls.Width;
                continue;
            }
            if (c.Class is { Tier: RedefinesTier.Alias } clsA && clsA.Members.Contains(c) && !c.IsCanonical)
                continue;   // a forwarded view
            w += PhysicalOccurrenceWidth(c) * (c.Occurs ?? 1);
        }
        return w;
    }

    /// <summary>ONE OCCURRENCE of <paramref name="item"/> on the PHYSICAL (codec, byte) basis — the per-child
    /// contribution <see cref="PhysicalWidth"/> multiplies by the child's OCCURS count, written once because three
    /// walks need it: <see cref="PhysicalWidth"/>'s sum, <see cref="OffsetOf"/>'s advance, and the §13.18.38.4 GR8
    /// occurs-depending extent (<c>OdoModel.WrapGroup</c>), whose per-occurrence STRIDE has to be in the same unit
    /// as the group image it slices. ⛔ That third reader used to take <c>table.ImageWidth</c> — CHARACTER
    /// positions, which a national leaf counts once where this basis counts it twice (§13.18.60.4 GR8; D-N1) —
    /// against a <see cref="PhysicalWidth"/> total in BYTES, so the fixed prefix came out as the difference and a
    /// zero-occurrence national table sent half its maximum image (kb/Work PB943).</summary>
    internal static int PhysicalOccurrenceWidth(DataItem item) =>
        item.IsGroup ? PhysicalWidth(item) : item.ByteWidth;

    /// <summary>The item's character offset within its record AREA on the PHYSICAL (codec) basis: the offset inside
    /// its own 01 root, which IS the area offset because every secondary 01 under an FD is a synthesized REDEFINES
    /// of the first, starting at position 0 (ISO §13.18.44 GR1). A REDEFINING child takes its TARGET's offset and
    /// contributes no width; the running position advances by each preceding sibling's PHYSICAL contribution — the
    /// class-max backing width at a Tier-B canonical (matching the emitted record layout), the plain image extent
    /// elsewhere. Null when the item or any ancestor carries OCCURS (no single fixed position — §12.4.5.12 SR1 /
    /// §14.9.40.3 SR6b ban OCCURS subjects; mirrors the Sort walk's bail).</summary>
    public static int? OffsetOf(DataItem item) => item.Occurs is null ? FirstOccurrenceOffsetOf(item) : null;

    /// <summary>The offset of <paramref name="item"/>'s FIRST occurrence within its record area — <see cref="OffsetOf"/>
    /// for an item that may itself carry the OCCURS clause, whose first byte position §13.18.38.3 SR20 measures from
    /// ("the first byte position defined by the data description entry containing the OCCURS clause"; kb/Work
    /// PB1261). Null when an ANCESTOR carries OCCURS.</summary>
    public static int? FirstOccurrenceOffsetOf(DataItem item)
    {
        for (var p = item.Parent; p is not null; p = p.Parent)
            if (p.Occurs is not null) return null;   // subject to OCCURS ⇒ no single fixed position
        DataItem root = item;
        while (root.Parent is { } parent) root = parent;

        int? found = null;
        var offsets = new Dictionary<DataItem, int>(ReferenceEqualityComparer.Instance);
        Walk(root, 0);
        return found;

        void Walk(DataItem node, int off)
        {
            if (found is not null) return;
            offsets[node] = off;
            if (ReferenceEquals(node, item)) { found = off; return; }
            int running = off;
            foreach (var c in node.Children)
            {
                int cOff = c.RedefinesTarget is { } t && offsets.TryGetValue(t, out int tOff) ? tOff : running;
                Walk(c, cOff);
                if (found is not null) return;
                // The advance mirrors PhysicalWidth's per-child contribution exactly (the codec layout):
                // a StringCanonical class MEMBER contributes the ONE backing (class-max) at the CANONICAL —
                // which may itself be a redefiner — and nothing at the other members; any other redefining
                // child overlays its target (no advance); everything else its physical extent.
                if (c.Class is { Tier: RedefinesTier.StringCanonical } cls && cls.Members.Contains(c))
                {
                    if (c.IsCanonical) running += cls.Width;
                    continue;
                }
                if (c.Class is { Tier: RedefinesTier.Alias } clsA && clsA.Members.Contains(c) && !c.IsCanonical)
                    continue;                                             // a forwarded view
                if (c.RedefinesTargetName is not null) continue;          // overlays its target — no advance
                running += PhysicalOccurrenceWidth(c) * (c.Occurs ?? 1);
            }
        }
    }

    /// <summary>The PHYSICAL width of a RECORD as its frame/area extent: a record that is itself a member of a
    /// REDEFINES class (a secondary 01 of a multi-record SD/FD — the synthesized area class) spans the ONE shared
    /// backing (class-max width, §9.1.2 records leftmost-aligned in one area); otherwise its own
    /// <see cref="PhysicalWidth"/>. (← <c>SortPhysicalWidth</c>'s item-level rule.)</summary>
    public static int AreaWidth(DataItem record) =>
        record.Class is { Tier: RedefinesTier.StringCanonical } cls && record.IsCanonical
            ? cls.Width
            : PhysicalWidth(record);

    /// <summary>The character offset of <paramref name="target"/> within <paramref name="root"/>'s PHYSICAL
    /// record image — the compile-time key window (ISO §14.9.40.3 SR6e: the same byte positions are the key in
    /// every record). An item inside a REDEFINES class sits at the class anchor's offset plus its
    /// <see cref="DataItem.ClassOffset"/> (a redefinition begins at the redefined item's first position,
    /// §13.18.44 GR1; covers keys under a redefining group and keys in a secondary 01 of a multi-01 SD/FD, whose
    /// synthesized class anchors at the first record). Null when the target does not live in
    /// <paramref name="root"/>'s area, or sits under an OCCURS (not a legal key, §14.9.40.3 SR6b/SR6f).
    /// (← <c>SortOffsetInRecord</c>/<c>SortPlainOffset</c>, on the ONE <see cref="OffsetOf"/> walk.)</summary>
    public static int? OffsetInRecord(DataItem root, DataItem target)
    {
        // ⛔ THE OCCURS BAIL IS ASKED OF THE DATA DESCRIPTION, AHEAD OF BOTH BRANCHES (kb/Work PB1173). It lived in
        // the plain walk (OffsetOf) only, so the class branch below — taken by EVERY 01 of a multi-record SD/FD,
        // which is a member of the synthesized REDEFINES class (PB836) — returned the FIRST occurrence's position
        // for an item under OCCURS and a key under a table was accepted with its occurrence discarded. An item
        // subject to OCCURS has no one position in the record whatever its storage form.
        if (target.IsTableElement) return null;   // §8.4.2.3.3 SR2's own-or-ancestor question, asked of DataItem (PB877)
        if (target.Class is { } cls)
        {
            if (ReferenceEquals(cls.Canonical, root)) return target.ClassOffset;
            if (ReferenceEquals(root.Class, cls)) return target.ClassOffset - root.ClassOffset;
            // The class anchors at its CANONICAL's plain position (the canonical itself carries the class link,
            // so it must resolve through the PLAIN walk — recursing the class branch would never terminate).
            return Plain(root, cls.Canonical) is { } a ? a + target.ClassOffset : null;
        }
        return Plain(root, target);

        static int? Plain(DataItem root, DataItem target)
        {
            for (DataItem? n = target; n is not null; n = n.Parent)
                if (ReferenceEquals(n, root))
                    return OffsetOf(target);   // under root (an 01 ⇒ area offset 0): the area offset IS the window
            return null;
        }
    }

    // ── A key's window in a VARIABLE-LENGTH record (kb/Work PB1025; docs/CONFORMANCE.md §3 D-KWV) ────────────────

    /// <summary>A key's byte window in its record (<see cref="KeyWindowOf"/>): <paramref name="Offset"/> — the
    /// record offset, or for a key a variable-length member precedes (<paramref name="FollowsVariable"/>) its
    /// FIXED-run offset; <paramref name="Bytes"/> — its byte extent; <paramref name="MaxEnd"/> — the furthest byte
    /// past its end in any record of the type, the quantity the minimum-record-size rules bound.</summary>
    public readonly record struct KeyWindow(int Offset, int Bytes, bool FollowsVariable, long MaxEnd);

    /// <summary>⛔ THE ONE ANSWER TO "WHERE IS THIS KEY IN THE RECORD, AND HOW FAR CAN IT REACH?" — for a SORT/MERGE
    /// key (§14.9.40.3 SR6 g) / §14.9.24.3 SR4 g)) and an indexed RECORD KEY / ALTERNATE RECORD KEY (§12.4.5.12.3
    /// SR4 / §12.4.5.6.3 SR5), whose rules are the same sentence: in a file of variable-length records every key
    /// "shall be contained within the first n bytes of the record, where n equals the minimum record size".
    /// <para>A variable-length record is sent and held as its CONTIGUOUS image (§8.5.1.11.2 — "behaves in all
    /// respects as though it were in fact contiguous with its neighbors"; determination D-FRA), so a key that a
    /// dynamic-length item PRECEDES has no one position (a dynamic-capacity table cannot be in a record —
    /// §8.5.1.9.1 3), COBOLNET1526; <see cref="FileModel.MaxDynamicExtent"/>): it starts at its offset in the
    /// record's FIXED run plus whatever those members hold in THIS record. Such a key is
    /// <see cref="KeyWindow.FollowsVariable"/>, its <see cref="KeyWindow.Offset"/> is the FIXED-run offset the
    /// record type's runtime layout (<c>CobolContiguousLayout.Position</c>) starts from, and its
    /// <see cref="KeyWindow.MaxEnd"/> charges every preceding member its MAXIMUM extent (§13.18.43.4 GR8 b)) — the
    /// furthest byte the key can occupy in any record, which is what "contained within the first n bytes" has to
    /// hold for. Every other key keeps <see cref="OffsetInRecord"/>'s fixed window, unchanged.</para>
    /// <para>Null when the key does not lie in <paramref name="root"/>'s area, or is subject to OCCURS.</para></summary>
    public static KeyWindow? KeyWindowOf(DataItem root, DataItem key, int bytes)
    {
        if (OffsetInRecord(root, key) is not { } off) return null;
        if (FileModel.IsVariableLengthRecord(root) && ContiguousPlaceOf(root, key) is { Preceding: > 0 } p)
            return new KeyWindow(p.FixedOffset, bytes, true, p.FixedOffset + p.PrecedingMaxBytes + bytes);
        return new KeyWindow(off, bytes, false, off + bytes);
    }

    /// <summary><see cref="KeyWindowOf"/> for a key of <paramref name="file"/>'s own file control entry — the form
    /// every rule that reads a RECORD KEY or ALTERNATE RECORD KEY asks (§12.4.5.12.3 SR4, §12.4.5.6.3 SR4 and SR5):
    /// the key's window in the record that holds it, null when <paramref name="key"/> is null, is not in a record of
    /// the file (its SR2 reports that), or is subject to OCCURS (its SR1 reports that).</summary>
    public static KeyWindow? KeyWindowInFile(FileModel file, DataItem? key)
    {
        if (key is null || !IsInRecordOfFile(file, key)) return null;
        var root = key;
        while (root.Parent is { } p) root = p;
        return KeyWindowOf(root, key, key.ByteWidth);
    }

    /// <summary>The walk behind <see cref="KeyWindowOf"/>: <paramref name="key"/>'s offset in
    /// <paramref name="root"/>'s FIXED run (every variable-length member contributing ZERO — the §8.5.1.12.3
    /// accounting the emitted layout's <c>FixedAt</c> uses, flattened through nested variable-length groups the
    /// same way), how many variable-length members precede it, and their summed MAXIMUM byte extents. The fixed
    /// members advance exactly as <see cref="OffsetOf"/> advances (the codec basis), so the offset is the one the
    /// layout's take step starts from.</summary>
    private static (int FixedOffset, int Preceding, long PrecedingMaxBytes)? ContiguousPlaceOf(DataItem root, DataItem key)
    {
        int preceding = 0;
        long maxBytes = 0;
        int? found = null;
        var offsets = new Dictionary<DataItem, int>(ReferenceEqualityComparer.Instance);
        Walk(root, 0);
        return found is { } f ? (f, preceding, maxBytes) : null;

        // Returns the FIXED-run offset just past node.
        int Walk(DataItem node, int start)
        {
            offsets[node] = start;
            if (ReferenceEquals(node, key)) { found = start; return start; }
            int running = start;
            foreach (var c in node.Children)
            {
                if (found is not null) return running;
                int cStart = c.RedefinesTarget is { } t && offsets.TryGetValue(t, out int tOff) ? tOff : running;
                if (c.IsDynamicLength)
                {
                    if (ReferenceEquals(c, key)) { found = cStart; return running; }
                    if (c.RedefinesTargetName is null) { preceding++; maxBytes += FileModel.MaxDynamicExtent(c); }
                    continue;   // §8.5.1.12.3 — contributes nothing to the fixed run
                }
                if (c.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(c) && c.RedefinesTargetName is null)
                {
                    running = Walk(c, cStart);   // a nested variable-length group flattens in place
                    continue;
                }
                Walk(c, cStart);
                if (found is not null) return running;
                if (c.Class is { Tier: RedefinesTier.StringCanonical } cls && cls.Members.Contains(c))
                {
                    if (c.IsCanonical) running += cls.Width;
                    continue;
                }
                if (c.Class is { Tier: RedefinesTier.Alias } clsA && clsA.Members.Contains(c) && !c.IsCanonical)
                    continue;
                if (c.RedefinesTargetName is not null) continue;
                running += PhysicalOccurrenceWidth(c) * (c.Occurs ?? 1);
            }
            return running;
        }
    }

    // ── Key-of-reference operand screens (ISO §14.9.41.3 SR6 · §14.9.30.3 SR11 · §12.4.5.12.4 GR4) ─────────────
    //
    // ⛔ TWO RULES, TWO ENTRY POINTS (kb/Work PB354). One method used to answer both, with the SR6 b) test —
    // "leftmost position coincides AND no longer than the key" — standing in for BOTH the "this IS a record key"
    // question READ asks and the "this is a legal GENERIC key" question START asks. That conflation is what let
    // §14.9.41.3 SR6 b) 2.'s class/category/usage condition sit unimplemented behind a doc comment (it has no
    // meaning for READ, so it could not be added to the shared body), and it made READ's §14.9.30.3 SR11 — which
    // carries NO generic-key arm at all — accept a short item where the rule names the key itself. The two rules
    // now have one method each, and each names the clause it enforces.

    /// <summary>ISO §14.9.41.3 SR6 a) / §14.9.30.3 SR11 — the operand IS a prime or alternate record key of
    /// <paramref name="file"/> (−1 = prime, i = the i-th alternate, null = it is not a key of this file): the
    /// declared key item itself, or the item occupying the IDENTICAL byte positions in another record
    /// description entry of the same file. §12.4.5.12.4 GR4 — <i>"the identical byte positions … in any one
    /// record description entry are implicitly referenced as keys for all other record description entries"</i>
    /// — is what makes the second form a key, and it is why the positional arm demands the SAME byte width
    /// rather than a smaller one: GR4 promotes the key's whole span, never a prefix of it. A prefix is SR6 b)'s
    /// generic key, which START admits and READ does not (see <see cref="GenericKeyIndex"/>).</summary>
    public static int? KeyIndexOfKeyItem(FileModel file, DataItem operand)
    {
        if (file.RecordKeyItem is { } prime && ReferenceEquals(prime, operand)) return -1;
        for (int i = 0; i < file.AlternateKeys.Count; i++)
            if (ReferenceEquals(file.AlternateKeys[i].Item, operand)) return i;
        // §12.4.5.12.4 GR4's implicit keys — the same byte window in ANOTHER record description of the file
        // (a REDEFINES of the key, or a same-position item in a secondary 01). The operand shall live in a
        // record description entry OF THIS FILE: GR4 speaks of the file's own record description entries, and
        // without that test offset 0 in ANY 01 anywhere — WORKING-STORAGE included — collided with offset 0 in
        // the record (kb/Work PB354 part 1).
        if (!IsInRecordOfFile(file, operand) || OffsetOf(operand) is not { } off) return null;
        return KeyIndexAtOffset(file, off, key => key.ByteWidth == operand.ByteWidth);
    }

    /// <summary>ISO §14.9.41.3 SR6 b) — START's GENERIC key: a data item that is not itself a record key but
    /// <b>b) 1.</b> whose leftmost character position <i>within a record of the file</i> corresponds to a record
    /// key's leftmost character position, <b>b) 2.</b> which has the same class, category and usage as that
    /// record key, and <b>b) 3.</b> whose length is not greater than that key's. All three conditions are
    /// conjunctive; b) 2. used to be omitted by an explicit doc-comment note, which let a
    /// <c>PIC 9(4) COMP-3</c> item stand in for an <c>X(6)</c> key and be compared on an incommensurable basis
    /// (kb/Work PB354 part 4; b) 2. and b) 3. are addressed by their printed path on <c>SR-14.9.41.3-6</c> since the
    /// PB698 transcription repair returned them to their printed level).
    /// <para>b) 1. also requires the key to be <i>"defined without the SOURCE phrase"</i>; the RECORD KEY /
    /// ALTERNATE RECORD KEY SOURCE phrase is not implemented (Annex A.3 item 40 — <c>FileModel</c> has no SOURCE
    /// carrier), so every key reaching here satisfies that half by construction.</para></summary>
    public static int? GenericKeyIndex(FileModel file, DataItem operand)
    {
        if (!IsInRecordOfFile(file, operand) || OffsetOf(operand) is not { } off) return null;   // b) 1.
        return KeyIndexAtOffset(file, off,
            key => SameClassCategoryUsage(operand, key)                                        // b) 2.
                && operand.ByteWidth <= key.ByteWidth);                                        // b) 3.
    }

    /// <summary>True when the item is <i>subject to an OCCURS clause</i> — it or any ancestor carries one. THE
    /// ONE predicate for that phrase (ISO §14.9.41.3 SR4 <i>"Data-name-1 or record-key-name-1 shall not be
    /// subject to any OCCURS clauses"</i>; §12.4.5.13.3 SR1, the RELATIVE KEY twin). Kept apart from
    /// <see cref="OffsetOf"/>'s own OCCURS bail, which answers a different question ("this item has no single
    /// fixed position") — folding the two is what made an SR4 violation report itself as an SR6 failure under a
    /// sentence that was FALSE of the operand (kb/Work PB354 part 2).</summary>
    public static bool IsSubjectToOccurs(DataItem item) => OccursSubjectOf(item) is not null;

    /// <summary>The entry whose OCCURS clause <paramref name="item"/> is subject to — the item itself or its nearest
    /// ancestor that carries one — or null. ANY format of the clause counts (§13.18.38: fixed, occurs-depending and
    /// Format-4 dynamic-capacity alike), so the test is <see cref="DataItem.IsTable"/>, never <c>Occurs</c> alone:
    /// <c>Occurs</c> is null for a dynamic-capacity table, and asking it let an item under OCCURS DYNAMIC pass every
    /// "subject to an OCCURS clause" rule (kb/Work PB1273's sweep; the CONTROL SR3 arm fell into the same trap).</summary>
    public static DataItem? OccursSubjectOf(DataItem item)
    {
        for (var p = item; p is not null; p = p.Parent)
            if (p.IsTable) return p;
        return null;
    }

    /// <summary>The item's 01 record description entry is one of <paramref name="file"/>'s records — ISO
    /// §14.9.41.3 SR6 b) 1.'s <i>"within a record of the file"</i>, and the same predicate the file control
    /// entry's own key rules are stated on: §12.4.5.12.3 SR2 and §12.4.5.6.3 SR2 REQUIRE it of a record key,
    /// §12.4.5.13.3 SR3 FORBIDS it of the relative key. ONE predicate for all four (<c>FileControlKeyRules</c>).
    /// <para>A null item is NOT in a record of the file — a data-name-1 that referenced nothing describable is a
    /// violation of the two SR2s, and answering "false" is what lets one row state that rule.</para></summary>
    public static bool IsInRecordOfFile(FileModel file, DataItem? item)
    {
        if (item is null) return false;
        DataItem root = item;
        while (root.Parent is { } p) root = p;
        return file.Records.Contains(root);
    }

    /// <summary>The index of the record key whose leftmost byte position is <paramref name="off"/> and which
    /// satisfies the caller's rule (−1 = prime, i = the i-th alternate, null = none). Positions are the PHYSICAL
    /// codec layout via <see cref="OffsetOf"/> — the basis §12.4.5.12.4 GR4 states the correspondence on.</summary>
    private static int? KeyIndexAtOffset(FileModel file, int off, Func<DataItem, bool> admits)
    {
        if (file.RecordKeyItem is { } prime && OffsetOf(prime) == off && admits(prime)) return -1;
        for (int i = 0; i < file.AlternateKeys.Count; i++)
        {
            var alt = file.AlternateKeys[i].Item;
            if (OffsetOf(alt) == off && admits(alt)) return i;
        }
        return null;
    }

    /// <summary>ISO §14.9.41.3 SR6 b) 2. — <i>"It has the same class, category, and usage as that record key."</i>
    /// Class comes from the ONE §8.5.2.1 Table-2 classifier (<see cref="IntrinsicArgumentRules.ClassOfItem"/>),
    /// never a second copy of that table. Where the model cannot tell two categories apart (it folds
    /// alphanumeric-edited into alphanumeric) the test passes — this screen exists to reject what the rule names,
    /// never what this compiler cannot classify.</summary>
    private static bool SameClassCategoryUsage(DataItem operand, DataItem key) =>
        IntrinsicArgumentRules.ClassOfItem(operand) == IntrinsicArgumentRules.ClassOfItem(key)
        && CategoryOfItem(operand) == CategoryOfItem(key)
        && UsageOfItem(operand) == UsageOfItem(key);

    /// <summary>The item's category: its PICTURE's (or a bit/national group's as-if PICTURE's), else — for an
    /// ordinary group, which has no PICTURE at all — category alphanumeric (§13.18.29.4 GR3, "an alphanumeric
    /// group item"). A group key with an elementary operand at its leftmost position is exactly the generic-key
    /// shape SR6 b) is written for, so the group arm may not answer "no category".
    /// <para>⚠ NOT <see cref="Place.CategoryOf"/>, which looks similar and answers a DIFFERENT question: that is
    /// §8.4.3.3.3 GR6's rule for the category a REFERENCE-MODIFIED view takes (numeric becomes alphanumeric, and
    /// so on). SR6 b) 2. compares the items' OWN categories, so folding the two would import a rule about
    /// reference modification into a rule about record keys.</para></summary>
    private static PicCategory CategoryOfItem(DataItem item) =>
        item.OperandPic?.Category ?? PicCategory.Alphanumeric;

    /// <summary>The item's USAGE (ISO §13.18.60), through THE ONE §8.5.2.1 usage reader
    /// (<see cref="ItemCategory.UsageOf"/>): the PICTURE's resolved usage for an elementary item, the as-if
    /// PICTURE's for a bit / national group, and DISPLAY for an alphanumeric group — "An alphanumeric group item
    /// is treated as though it had a usage of display" (kb/Work PB411's sibling sweep; the former
    /// <c>?? item.OwnUsage</c> gave <c>01 G USAGE COMP.</c> the answer BINARY, which §13.18.60.4 GR1 settles
    /// outright — "Unless the GROUP-USAGE clause is also specified or implied, the USAGE clause applies only to
    /// each elementary item in the group and not to the group itself"). The tail
    /// survives for the group kinds §3.11 excludes from "alphanumeric group item", where the standard states no
    /// usage and SR6 b) 2. still has to compare something: the entry's own keyword, else DISPLAY — GR2's
    /// standard data format, the usage an entry with no clause has.</summary>
    private static Usage UsageOfItem(DataItem item) =>
        ItemCategory.UsageOf(item) ?? item.OwnUsage ?? Usage.Display;
}
