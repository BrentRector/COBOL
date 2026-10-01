// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;

namespace CobolNet.Binding.Model;

/// <summary>
/// ⛔ THE ONE ISO §8.5.1.12 VARIABLE-LENGTH-GROUP COMPATIBILITY RELATION (kb/Work PB204).
/// <para>§8.5.1.12.1: "a variable-length group is not equivalent to an alphanumeric data item and may not
/// undergo a comparison or a move operation, in either direction, explicitly or otherwise, unless the other
/// operand is a compatible group. Groups are compatible if all variable-length data items correspond and match
/// as specified below." Three named consumers import it verbatim — §14.8.2.2 ("If either the formal parameter or
/// the argument is a variable length group, the formal parameter and the argument shall be compatible, as
/// described in 8.5.1.12"), §14.8.3.2 (the same sentence over a RETURNING pair), and §14.9.4.3 SR25, which makes
/// both apply to a Format-2 CALL. Format 1 needs none of this: SR12 forbids the crossing outright
/// ("Identifier-2 shall not reference a variable-length group"), which is why the relation belongs at the
/// Format-2 / INVOKE boundary and nowhere else.</para>
/// <para>THE THREE RULES, implemented as ONE positional walk rather than three passes, because they are three
/// statements about the SAME left-to-right byte accounting:</para>
/// <list type="number">
/// <item>§8.5.1.12.1 rule 1 + §8.5.1.12.2 sentence 2 — for each dynamic-capacity table in either group there is
/// a corresponding table in the other: "at least one of them is a dynamic-capacity table and they occupy the
/// same relative byte positions within their groups".</item>
/// <item>§8.5.1.12.1 rule 2 + §8.5.1.12.3 sentence 2 — corresponding tables MATCH "when the byte length of
/// their elements is equal and their elements are compatible" (the element compatibility recurses HERE).</item>
/// <item>§8.5.1.12.1 rule 3 + §8.5.1.12.2 sentence 1 — for each dynamic-length elementary item in either group
/// there is a corresponding one in the other, correspondence being "start at the same relative byte positions
/// within their groups"; §8.5.1.12.3 sentence 1 then makes any two of them match "regardless of their
/// definitions".</item>
/// </list>
/// <para>THE BYTE ACCOUNTING IS NOT RE-DERIVED HERE — it is <see cref="DataItem.ByteWidth"/>, which already
/// satisfies §8.5.1.12.3's two collapse conventions BY CONSTRUCTION: a dynamic-length elementary item's
/// <c>ElementaryImageWidth</c> is 0 ("all dynamic-length elementary items are considered to be of zero length"),
/// and a dynamic-capacity table carries no <c>Occurs</c>, so a parent sums <c>ByteWidth * (Occurs ?? 1)</c> =
/// ONE element ("Dynamic-capacity tables that match each other are each considered to be the length of a single
/// element of that table"). The one convention the walk must apply itself is §8.5.1.12.3 sentence 3 — a
/// dynamic-capacity table corresponding to a table that is NOT one "is considered to be the same length as the
/// corresponding table" — which is a fact about a PAIR and therefore cannot live on either item.</para>
/// <para>NESTING IS TRANSPARENT: the relation is stated over relative byte positions within the group, never
/// over its declaration tree, so <see cref="Atoms"/> FLATTENS scalar subordinate groups. A subordinate entry
/// carrying REDEFINES is dropped with its whole subtree — §8.5.1.12.1: "In determining compatibility, any
/// subordinate data items that specify the REDEFINES clause, and all data items subordinate to those data
/// items, are ignored."</para>
/// </summary>
internal static class VariableLengthCompatibility
{
    /// <summary>True for a VARIABLE-LENGTH GROUP (§8.5.1.12.1 sentence 1: "a group item whose data description
    /// has at least one dynamic-length elementary item or dynamic-capacity table as a subordinate item"). The
    /// ONE spelling — <see cref="DataItem.IsImageCapable"/>'s dynamic axis answers the same question about a
    /// single item, and <c>ReferenceResolver.HasVariableLengthSubordinate</c> is the walk both share.</summary>
    public static bool IsVariableLength(DataItem item) =>
        item.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(item);

    /// <summary>True for a VARIABLE-LENGTH DATA ITEM (§8.5.1.11.1: "The term variable-length data item refers to
    /// either a dynamic-capacity table or a dynamic-length elementary item") — the ITEM, not a group containing one
    /// (that is <see cref="IsVariableLength"/>). §13.18.45.3 SR8 bars it from a RENAMES range.</summary>
    public static bool IsVariableLengthDataItem(DataItem item) =>
        item.IsDynamicTable || (item.IsDynamicLength && !item.IsGroup);

    /// <summary>⛔ THE ONE "dynamic-length elementary item or variable-length group" SCREEN (kb/Work PB1213). The
    /// standard bars that PAIR of shapes, in those words, from a family of operands and subjects — §13.18.5.3 SR2
    /// ("The subject of the entry shall not be a dynamic-length elementary item or a variable-length group"),
    /// §13.10.3 SR12 ("Data-name-1 and data-name-2 shall not be dynamic-length elementary items or variable-length
    /// groups"), §13.18.44.3 SR17 (REDEFINES, either side) and §12.4.5.8.3 SR3 (FILE STATUS). Each consumer used
    /// to spell its own walk, and the CONSTANT one asked about dynamic-capacity TABLES only, so a group made
    /// variable-length by a DYNAMIC LENGTH leaf passed. Returns the phrase naming the shape found — the clause of
    /// the consumer's diagnostic — or null when the item is neither.
    /// <para>The group half is <see cref="IsVariableLength"/>, §8.5.1.12.1's definition verbatim: "a group item
    /// whose data description has at least one dynamic-length elementary item or dynamic-capacity table as a
    /// subordinate item". An item that IS itself a dynamic-capacity table is neither shape (its elements have a
    /// fixed length), and an occurs-depending group is not a variable-length group — the standard names that
    /// shape separately wherever it means it.</para></summary>
    public static string? DynamicLengthOrVariableLengthGroup(DataItem item) =>
        item.IsDynamicLength && !item.IsGroup ? "a dynamic-length elementary item (ISO §8.5.1.10)"
        : IsVariableLength(item)
            ? "a variable-length group (ISO §8.5.1.12.1 — a dynamic-length elementary item or a dynamic-capacity "
              + "table is subordinate to it)"
        : null;

    /// <summary>The kind of one positional atom of a group's byte layout.</summary>
    private enum AtomKind
    {
        /// <summary>An elementary item that is neither a table nor dynamic-length — pure bytes.</summary>
        Fixed,
        /// <summary>A dynamic-length elementary item (§8.5.1.10) — zero bytes for position purposes.</summary>
        DynamicLength,
        /// <summary>A table (fixed OCCURS, OCCURS DEPENDING, or OCCURS DYNAMIC).</summary>
        Table,
    }

    /// <param name="Kind">Which of the three §8.5.1.12 roles this atom plays.</param>
    /// <param name="Item">The declaring item (its name goes into the diagnostic; a table's element description
    /// is this item, so element compatibility recurses through it).</param>
    /// <param name="Width">The atom's contribution to the relative byte position, under the §8.5.1.12.3
    /// conventions.</param>
    /// <param name="DynamicCapacity">True for an OCCURS DYNAMIC table (§8.5.1.9).</param>
    /// <param name="ElementBytes">One occurrence's byte length — the quantity §8.5.1.12.3 sentence 2 compares.</param>
    /// <param name="ImageChars">The atom's contribution in CHARACTER POSITIONS. Not used by the relation, which
    /// §8.5.1.12 states in bytes — only by <see cref="Signature"/>, whose consumer (the universal-dispatch
    /// descriptor) has to know the CARRIER layout too, and a NATIONAL member is 1 character per 2 bytes.</param>
    private readonly record struct Atom(
        AtomKind Kind, DataItem Item, int Width, bool DynamicCapacity, int ElementBytes, int ImageChars);

    /// <summary>The group's byte layout as a FLAT left-to-right atom sequence: REDEFINES subtrees dropped
    /// (§8.5.1.12.1), scalar subordinate groups flattened (relative byte position is nesting-blind), a table
    /// kept WHOLE (its element description is the recursion subject of §8.5.1.12.3 sentence 2, not something to
    /// flatten through).</summary>
    private static void Atoms(DataItem g, List<Atom> into)
    {
        foreach (var c in g.Children)
        {
            if (c.RedefinesTargetName is not null || !(c.IsGroup || c.IsElementary)) continue;
            if (c.IsDynamicTable)
                into.Add(new Atom(AtomKind.Table, c, c.ByteWidth, DynamicCapacity: true, c.ByteWidth, c.ImageWidth));
            else if (c.Occurs is { } n)
                // A fixed-OCCURS or OCCURS DEPENDING table takes its MAXIMUM length — §14.8.2.2's own sentence
                // for an occurs-depending group passed by reference, and §8.5.1.12.3 sentence 3's "its fixed
                // number of occurrences or the value of the DEPENDING operand, as applicable" resolved
                // statically (the DEPENDING operand's run-time value is not a compile-time quantity).
                into.Add(new Atom(AtomKind.Table, c, c.ByteWidth * n, DynamicCapacity: false, c.ByteWidth,
                    c.ImageWidth * n));
            else if (c.IsDynamicLength)
                into.Add(new Atom(AtomKind.DynamicLength, c, 0, DynamicCapacity: false, 0, 0));
            else if (c.IsGroup)
                Atoms(c, into);
            else
                into.Add(new Atom(AtomKind.Fixed, c, c.ByteWidth, DynamicCapacity: false, c.ByteWidth,
                    c.ImageWidth));
        }
    }

    /// <summary>The atoms of a level-66 THROUGH alias (kb/Work PB907). §13.18.45.4 GR2 makes the alias "an
    /// alphanumeric group item that includes all elementary items starting with data-name-2 … and concluding
    /// with data-name-3", so its layout is the record's storage window the resolver already tiled into
    /// <see cref="RenamesInfo.Span"/> — read HERE, never re-walked, so the alias's atoms and its carrier cannot
    /// disagree about where a byte lies. A WHOLE table leaf in the window is a <see cref="AtomKind.Table"/> atom
    /// (a fixed OCCURS table — §13.18.45.3 SR8 bars "a variable-length data item, or an occurs-depending table"
    /// from the range, so no dynamic atom can arise and the alias is always a FIXED-length group); every other
    /// part, including a single occurrence or a partial slice of one, is plain bytes, because a table only part
    /// of which lies in the window is not a table of the alias.</summary>
    private static void AliasAtoms(RenamesInfo ren, List<Atom> into)
    {
        foreach (var part in ren.Span)
        {
            var leaf = part.Leaf;
            if (part.IsWhole && leaf.Occurs is { } n)
            {
                into.Add(new Atom(AtomKind.Table, leaf, leaf.ByteWidth * n, DynamicCapacity: false, leaf.ByteWidth,
                    leaf.ImageWidth * n));
                continue;
            }
            // part.Length is in the leaf's own CHARACTER positions; the relation is stated in bytes (a national
            // leaf's character is two of them) — RenamesSpanPart.Bytes is the one conversion.
            int bytes = part.Bytes;
            into.Add(new Atom(AtomKind.Fixed, leaf, bytes, DynamicCapacity: false, bytes, part.Length));
        }
    }

    private static List<Atom> AtomsOf(DataItem g)
    {
        var list = new List<Atom>();
        if (g.Renames is { IsAlias: false } ren) AliasAtoms(ren, list);
        else Atoms(g, list);
        return list;
    }

    /// <summary>A CANONICAL rendering of the group's §8.5.1.12 atom layout: consecutive fixed material
    /// collapsed to one <c>bytes/chars</c> run, a dynamic-length item as <c>D</c>, a dynamic-capacity table as
    /// <c>T</c> plus its element's byte and character widths. Used by the OO UNIVERSAL-dispatch conformance
    /// DESCRIPTOR (§14.9.23.4 GR7c), which must decide conformance at RUN time from two independently compiled
    /// descriptions and therefore cannot run the relation itself.
    /// <para>⚠ EQUAL SIGNATURES IMPLY COMPATIBILITY, NOT THE CONVERSE — a DOCUMENTED STRICTNESS DELTA of the
    /// same family as the descriptor's existing one (a fixed group's <c>S:{width}</c> cannot express §14.8.2.2
    /// rule 1's prefix latitude either). Two shapes the relation accepts but the signature separates: a
    /// dynamic-capacity table corresponding to a FIXED table (§8.5.1.12.3 sentence 3), and fixed runs of
    /// unequal total length after the last variable-length component. Both raise EC-OO-UNIVERSAL through the
    /// universal path while the TYPED path — which runs <see cref="Mismatch"/> itself at bind — accepts
    /// them.</para></summary>
    public static string Signature(DataItem g)
    {
        var outp = new List<string>();
        long bytes = 0; long chars = 0;
        void Flush()
        {
            if (bytes == 0 && chars == 0) return;
            outp.Add($"{bytes}/{chars}");
            bytes = 0; chars = 0;
        }
        foreach (var a in AtomsOf(g))
        {
            if (a.Kind is AtomKind.DynamicLength) { Flush(); outp.Add("D"); }
            else if (a.Kind is AtomKind.Table && a.DynamicCapacity)
            {
                Flush();
                outp.Add($"T{a.ElementBytes}/{a.ImageChars}");
            }
            else { bytes += a.Width; chars += a.ImageChars; }
        }
        Flush();
        return string.Join(",", outp);
    }

    /// <summary>⛔ THE GROUP'S §8.5.1.12 LAYOUT as the runtime reads it — flat <c>(kind, chars, elementChars)</c>
    /// triples in CHARACTER positions, the kinds being <c>CobolVarGroup.Layout*</c> (kb/Work PB965). It is the
    /// compile-time half of <c>CobolVarGroup.CorrespondingSpans</c>, which pairs a FIXED group's layout with a
    /// VARIABLE-length group's and answers which of the fixed group's tables correspond (§8.5.1.12.2) — the
    /// spans <c>CobolVarGroup.FromFixedImage</c> lifts out so a fixed group can stand on the other side of an
    /// ISO §14.9.25.4 GR9 move, a §14.8.2.2 CALL argument/formal pair or a §14.8.3.2 returning pair.
    /// <para>It replaced <c>FlatTableSpans</c>, which answered "EVERY table of the fixed group" — a fact about
    /// ONE group, where §8.5.1.12.2's correspondence ("they occupy the same relative byte positions within
    /// their groups") is a fact about the PAIR. A fixed table opposite plain bytes of the variable-length group
    /// was lifted as a component anyway, every later component shifted one place, and the wrong table was
    /// moved (measured: `MOVE SRC TO VLG` with a leading two-occurrence table landed that table in the dynamic
    /// one). Across a CALL each side is compiled apart, so the layout TRAVELS (<c>CobolArg.Layout</c>) and the
    /// pair is decided where both are in hand.</para>
    /// <para>Fixed material is merged into one run; a fixed table carries its whole width, an OCCURS DEPENDING
    /// table its maximum (§14.8.2.2: "the maximum length is used"; §13.18.38.3 SR22 makes it the last atom), a
    /// dynamic-capacity table one element (§8.5.1.12.3 sentence 4), a dynamic-length item nothing.
    /// <see langword="null"/> for a non-group or a subtree with a USAGE BIT leaf (§8.5.1.6.3's shared-byte runs
    /// make a character position non-positional).</para></summary>
    public static int[]? Layout(DataItem g)
    {
        if (!ItemCategory.IsGroupItem(g) || g.HasBitDescendant) return null;
        var outp = new List<int>();
        int run = 0;
        void Flush()
        {
            if (run == 0) return;
            outp.AddRange([CobolNet.Runtime.CobolVarGroup.LayoutFixed, run, 0]);
            run = 0;
        }
        foreach (var a in AtomsOf(g))
        {
            switch (a.Kind)
            {
                case AtomKind.Fixed:
                    run += a.ImageChars;
                    break;
                case AtomKind.DynamicLength:
                    Flush();
                    outp.AddRange([CobolNet.Runtime.CobolVarGroup.LayoutDynamicLength, 0, 0]);
                    break;
                default:
                    Flush();
                    int kind = a.DynamicCapacity ? CobolNet.Runtime.CobolVarGroup.LayoutDynamicTable
                        : a.Item.OccursSpec is { DependingName: not null } ? CobolNet.Runtime.CobolVarGroup.LayoutOdoTable
                        : CobolNet.Runtime.CobolVarGroup.LayoutTable;
                    outp.AddRange([kind, a.ImageChars, a.Item.ImageWidth]);
                    break;
            }
        }
        Flush();
        return [.. outp];
    }

    /// <summary>True when a layout has anything but fixed material — a table or a variable-length member, the
    /// only atoms §8.5.1.12.2's correspondence can pair. A layout of fixed material alone states nothing beyond the
    /// group's length, which the runtime recovers from the carrier (<c>CobolVarGroup.FixedRun</c>), so the
    /// boundary does not emit it.</summary>
    public static bool HasTableOrVariable(int[] layout)
    {
        for (int k = 0; k < layout.Length; k += 3)
            if (layout[k] != CobolNet.Runtime.CobolVarGroup.LayoutFixed) return true;
        return false;
    }

    /// <summary>The spans of <paramref name="fixedGroup"/>'s tables that correspond to
    /// <paramref name="varGroup"/>'s dynamic-capacity tables — the compile-time call of the ONE correspondence
    /// walk (<c>CobolVarGroup.CorrespondingSpans</c>), for a pair both of whose descriptions are in hand (the
    /// §14.9.25.4 GR9 MOVE). Null when either layout cannot be stated or the pair does not correspond.</summary>
    public static int[]? CorrespondingSpans(DataItem fixedGroup, DataItem varGroup) =>
        Layout(fixedGroup) is { } f && Layout(varGroup) is { } v
            ? CobolNet.Runtime.CobolVarGroup.CorrespondingSpans(f, v)
            : null;

    /// <summary>⛔ ISO §8.5.1.12.1's PROHIBITION OVER AN OPERAND PAIR, for every operation it names — "a
    /// variable-length group is not equivalent to an alphanumeric data item and may not undergo a comparison or a
    /// move operation, in either direction, explicitly or otherwise, unless the other operand is a compatible
    /// group". Null when neither operand is a variable-length group or the pair is compatible; else the reason.
    /// A null operand means "not a plain data item" — a literal, a figurative constant, a function result, or a
    /// reference-modified operand (§8.4.3.3.4 GR6 makes it an ELEMENTARY alphanumeric item) — which is a violation,
    /// not a fall-through, because the rule is stated in terms of the OTHER operand. The MOVE (§14.9.25.3 SR9, via
    /// <c>MoveTable16</c>) and the relation condition (<c>StatementValidation.CheckRelationalOperands</c>, kb/Work
    /// PB1467) both ask it, so the two operations cannot read the one sentence differently.</summary>
    /// <param name="aRole">/<paramref name="bRole"/> name each operand in the reason ("sending", "receiving",
    /// "first", …); <paramref name="operation"/> completes "a variable-length group …" for this operation.</param>
    public static string? PairRefusal(DataItem? a, DataItem? b, string aRole, string bRole, string operation)
    {
        bool engaged = (a is not null && IsVariableLength(a)) || (b is not null && IsVariableLength(b));
        return !engaged ? null
            : a is null || b is null
                ? $"the {(b is null ? bRole : aRole)} operand is not a group item: a variable-length group "
                  + $"{operation} (ISO §8.5.1.12.1)"
                : Mismatch(a, b);
    }

    /// <summary>Null when <paramref name="one"/> and <paramref name="other"/> are COMPATIBLE per §8.5.1.12,
    /// else the reason, worded for a diagnostic. Two FIXED-length groups are compatible outright (§8.5.1.12.1:
    /// "Two fixed-length groups are always compatible, unless they are strongly typed and have different type
    /// definitions" — the strong-typing half is §14.8.2.2's own separate sentence and is checked by the caller,
    /// so this returns null for that pair rather than restating a rule that lives elsewhere).</summary>
    public static string? Mismatch(DataItem one, DataItem other)
    {
        if (!IsVariableLength(one) && !IsVariableLength(other)) return null;
        // §8.5.1.12.1: compatibility is a relation between GROUPS ("unless the other operand is a compatible
        // group"). An elementary counterpart — which §14.8.2.2 rule 1 would otherwise admit for a fixed-length
        // group — has no atom sequence to correspond with.
        // ⛔ The CATEGORY question, never the structural IsGroup: a level-66 THROUGH alias is a group item with
        // no subordinates (§13.18.45.4 GR2 — kb/Work PB907), and its atoms are its span (AliasAtoms).
        bool oneGroup = ItemCategory.IsGroupItem(one), otherGroup = ItemCategory.IsGroupItem(other);
        if (!oneGroup || !otherGroup)
            return $"'{(oneGroup ? other : one).CobolName}' is not a group: a variable-length group is "
                + "compatible only with a group (ISO §8.5.1.12.1)";
        return Walk(AtomsOf(one), AtomsOf(other), one, other, null);
    }

    /// <summary>⛔ THE PAIR-RELATIVE LENGTHS of two groups at least one of which is a variable-length group, in
    /// CHARACTER positions — the only lengths a size rule may compare for such a pair (kb/Work PB965). A
    /// variable-length group has no length of its own: its collapsed <see cref="DataItem.ImageWidth"/> counts a
    /// dynamic-capacity table as one element, which is a convention for a pair of MATCHING dynamic-capacity
    /// tables only (§8.5.1.12.3 sentence 4). When the corresponding table is NOT a dynamic-capacity table,
    /// §8.5.1.12.3 sentence 3 decides instead — "For purposes of determining compatibility, the dynamic-capacity
    /// table is considered to be the same length as the corresponding table" — a fact about the PAIR, so it is
    /// answered by the SAME walk that decides the correspondence, never by either item alone. Comparing the
    /// collapsed width is what refused a compatible variable-length argument BY REFERENCE into a fixed-length
    /// formal ("the formal (7 character positions) exceeds the argument (5)"). Material past the shorter group's
    /// last character counts on its own side only. Null when the pair is not compatible (<see cref="Mismatch"/>
    /// says why) or either side is not a group.</summary>
    public static (long One, long Other)? PairCharWidths(DataItem one, DataItem other)
    {
        if (!ItemCategory.IsGroupItem(one) || !ItemCategory.IsGroupItem(other)) return null;
        var tally = new CharTally();
        return Walk(AtomsOf(one), AtomsOf(other), one, other, tally) is null ? (tally.A, tally.B) : null;
    }

    /// <summary>The per-side character-position accumulator <see cref="PairCharWidths"/> threads through the
    /// walk; null for a bare compatibility question.</summary>
    private sealed class CharTally
    {
        public long A, B;
    }

    private static string? Walk(List<Atom> a, List<Atom> b, DataItem ga, DataItem gb, CharTally? tally)
    {
        int ia = 0, ib = 0;
        long pa = 0, pb = 0;
        while (ia < a.Count || ib < b.Count)
        {
            if (ia >= a.Count) return Tail(b, ib, gb, ga, tally, sideA: false);
            if (ib >= b.Count) return Tail(a, ia, ga, gb, tally, sideA: true);
            // The two sides are walked to the SAME relative byte position before any correspondence is decided:
            // §8.5.1.12.2 states BOTH correspondences as "the same relative byte positions within their groups".
            if (pa < pb)
            {
                if (tally is not null) tally.A += a[ia].ImageChars;
                if (Skip(a, ref ia, ref pa, ga) is { } e) return e;
                continue;
            }
            if (pb < pa)
            {
                if (tally is not null) tally.B += b[ib].ImageChars;
                if (Skip(b, ref ib, ref pb, gb) is { } e) return e;
                continue;
            }

            var x = a[ia];
            var y = b[ib];
            // Rule 3 (§8.5.1.12.2 sentence 1 + §8.5.1.12.3 sentence 1): dynamic-length items correspond by
            // position and then ALWAYS match, "regardless of their definitions".
            if (x.Kind is AtomKind.DynamicLength || y.Kind is AtomKind.DynamicLength)
            {
                if (x.Kind != y.Kind)
                {
                    var lone = x.Kind is AtomKind.DynamicLength ? x : y;
                    var host = x.Kind is AtomKind.DynamicLength ? ga : gb;
                    return $"the dynamic-length item '{lone.Item.CobolName}' of '{host.CobolName}' starts at "
                        + $"relative byte position {pa} and the other group has no dynamic-length item there "
                        + "(ISO §8.5.1.12.1 rule 3 / §8.5.1.12.2)";
                }
                ia++; ib++;
                continue;
            }
            // Rules 1 and 2 (§8.5.1.12.2 sentence 2 + §8.5.1.12.3 sentences 2-4).
            if ((x.Kind is AtomKind.Table && x.DynamicCapacity) || (y.Kind is AtomKind.Table && y.DynamicCapacity))
            {
                if (x.Kind is not AtomKind.Table || y.Kind is not AtomKind.Table)
                {
                    var lone = x.DynamicCapacity ? x : y;
                    var host = x.DynamicCapacity ? ga : gb;
                    return $"the dynamic-capacity table '{lone.Item.CobolName}' of '{host.CobolName}' occupies "
                        + $"relative byte position {pa} and the other group has no table there "
                        + "(ISO §8.5.1.12.1 rule 1 / §8.5.1.12.2)";
                }
                if (x.ElementBytes != y.ElementBytes)
                    return $"corresponding tables '{x.Item.CobolName}' and '{y.Item.CobolName}' do not match: "
                        + $"their elements are {x.ElementBytes} and {y.ElementBytes} bytes "
                        + "(ISO §8.5.1.12.3 — the byte length of their elements shall be equal)";
                if (Elements(x.Item, y.Item) is { } inner)
                    return $"corresponding tables '{x.Item.CobolName}' and '{y.Item.CobolName}' do not match: "
                        + $"their elements are not compatible — {inner} (ISO §8.5.1.12.3)";
                // §8.5.1.12.3 sentence 3: when only ONE of the pair is a dynamic-capacity table, the dynamic one
                // "is considered to be the same length as the corresponding table"; when BOTH are, sentence 4
                // makes each one element long — which is the Width each atom already carries.
                long len = x.DynamicCapacity && y.DynamicCapacity ? x.ElementBytes
                    : x.DynamicCapacity ? y.Width : x.Width;
                if (tally is not null)
                {
                    // The same two sentences in CHARACTER positions: sentence 4 leaves each matching
                    // dynamic-capacity table one element long; sentence 3 gives both the fixed table's length.
                    bool both = x.DynamicCapacity && y.DynamicCapacity;
                    tally.A += both || !x.DynamicCapacity ? x.ImageChars : y.ImageChars;
                    tally.B += both || !y.DynamicCapacity ? y.ImageChars : x.ImageChars;
                }
                pa += len; pb += len; ia++; ib++;
                continue;
            }
            // Plain bytes on both sides: consume one atom and let the position compare above re-align. Widths
            // may differ freely — the standard constrains the POSITIONS of the variable-length items, not the
            // shape of the fixed material between them.
            if (tally is not null) tally.A += x.ImageChars;
            pa += x.Width; ia++;
        }
        return null;
    }

    /// <summary>Walk the lagging side forward over material the other side has already passed. A dynamic item
    /// found here sits at a byte position the other group has no counterpart at, so it fails rules 1/3.</summary>
    private static string? Skip(List<Atom> list, ref int i, ref long p, DataItem host)
    {
        var at = list[i];
        if (at.Kind is AtomKind.DynamicLength)
            return $"the dynamic-length item '{at.Item.CobolName}' of '{host.CobolName}' starts at relative "
                + $"byte position {p} and the other group has no dynamic-length item there "
                + "(ISO §8.5.1.12.1 rule 3 / §8.5.1.12.2)";
        if (at.DynamicCapacity)
            return $"the dynamic-capacity table '{at.Item.CobolName}' of '{host.CobolName}' occupies relative "
                + $"byte position {p} and the other group has no corresponding table there "
                + "(ISO §8.5.1.12.1 rule 1 / §8.5.1.12.2)";
        p += at.Width;
        i++;
        return null;
    }

    /// <summary>The atoms of the LONGER group beyond the shorter group's last byte. §8.5.1.12.2's last sentence
    /// grants exactly one latitude here — "where the relative byte position of a dynamic capacity table in the
    /// longer group is beyond the last character of the shorter group, the dynamic capacity table is treated as
    /// if it corresponds to a space-filled fixed-length table" — and grants it to TABLES ONLY. A trailing
    /// dynamic-LENGTH item still needs a real counterpart (rule 3), so it fails.</summary>
    private static string? Tail(List<Atom> list, int i, DataItem host, DataItem other, CharTally? tally, bool sideA)
    {
        for (; i < list.Count; i++)
        {
            if (tally is not null)
            {
                if (sideA) tally.A += list[i].ImageChars;
                else tally.B += list[i].ImageChars;
            }
            if (list[i].Kind is AtomKind.DynamicLength)
                return $"the dynamic-length item '{list[i].Item.CobolName}' of '{host.CobolName}' lies beyond "
                    + $"the last byte of '{other.CobolName}', which therefore has no corresponding "
                    + "dynamic-length item (ISO §8.5.1.12.1 rule 3 / §8.5.1.12.2)";
        }
        return null;
    }

    /// <summary>§8.5.1.12.3 sentence 2's second conjunct — "their elements are compatible". A group element
    /// recurses into the SAME relation (a table of variable-length groups is exactly the shape rule 2 exists
    /// for); an elementary element has no atoms of its own and its byte length was compared by the caller.</summary>
    private static string? Elements(DataItem x, DataItem y) =>
        x.IsGroup && y.IsGroup ? Walk(AtomsOf(x), AtomsOf(y), x, y, null)
        : x.IsGroup != y.IsGroup
            ? $"'{(x.IsGroup ? y : x).CobolName}' is elementary and '{(x.IsGroup ? x : y).CobolName}' is a group"
            : null;
}
