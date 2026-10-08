// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Runtime;

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
/// <para>THE THREE RULES are ONE positional walk rather than three passes, because they are three statements about
/// the SAME left-to-right byte accounting — and that walk is the RUNTIME's <see cref="GroupCompatibility.Walk"/>
/// (kb/Work PB480), over the atoms this class builds from a data description, so the bind-time screens here and the
/// run-time relation a universal dispatch asks of two descriptions compiled apart are one copy of the rule. This class
/// owns the atoms (<see cref="GroupAtoms"/>) and words the walk's reasons with item names:</para>
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

    /// <summary>A group's §8.5.1.12 atoms (<see cref="GroupAtom"/>, the runtime's model, which the ONE walk
    /// <see cref="GroupCompatibility.Walk"/> reads) with the declaring item of each — a diagnostic names it, and a
    /// table atom's item is the subject of §8.5.1.12.3 sentence 2's element recursion.</summary>
    private readonly record struct Laid(GroupAtom[] Atoms, DataItem[] Items);

    /// <summary>A table atom of <paramref name="c"/>: one occurrence's byte and character lengths, and — when the
    /// element is a group — the element's own atoms, which §8.5.1.12.3's "their elements are compatible" recurses
    /// into.</summary>
    private static GroupAtom TableAtom(DataItem c, GroupAtomKind kind, int occurrences) =>
        new(kind, c.ByteWidth * occurrences, c.ImageWidth * occurrences, c.ByteWidth, c.ImageWidth,
            c.IsGroup ? AtomsOf(c).Atoms : null);

    /// <summary>The group's byte layout as a FLAT left-to-right atom sequence: REDEFINES subtrees dropped
    /// (§8.5.1.12.1), scalar subordinate groups flattened (relative byte position is nesting-blind), a table
    /// kept WHOLE (its element description is the recursion subject of §8.5.1.12.3 sentence 2, not something to
    /// flatten through) — except a FIXED-OCCURS table whose element is itself a variable-length group, which is
    /// unrolled occurrence by occurrence (each occurrence's dynamic-length items are items of the group).
    /// <para>THE BYTE ACCOUNTING IS NOT RE-DERIVED HERE — it is <see cref="DataItem.ByteWidth"/>, which already
    /// satisfies §8.5.1.12.3's two collapse conventions BY CONSTRUCTION: a dynamic-length elementary item's
    /// <c>ElementaryImageWidth</c> is 0, and a dynamic-capacity table carries no <c>Occurs</c>, so its atom is ONE
    /// element.</para></summary>
    private static void Atoms(DataItem g, List<GroupAtom> into, List<DataItem> items)
    {
        foreach (var c in g.Children)
        {
            if (c.RedefinesTargetName is not null || !(c.IsGroup || c.IsElementary)) continue;
            if (c.IsDynamicTable)
            {
                into.Add(TableAtom(c, GroupAtomKind.DynamicTable, 1));
                items.Add(c);
            }
            else if (c.Occurs is { } times && c.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(c))
                // A FIXED-OCCURS or OCCURS DEPENDING table whose element is a variable-length group (kb/Work
                // PB244): every occurrence holds its own dynamic-length items "at the same relative byte positions"
                // (§8.5.1.12.2), so the table is not an atom — it is `times` flattened copies of its element
                // (an OCCURS DEPENDING table at its MAXIMUM, as the plain one below is: §14.8.2.2 and §8.5.1.12.3
                // sentence 3 resolved statically), exactly as the emitted carrier flattens them (GroupImageCodec
                // VarPartKind.NestedTable / OdoTable).
                for (int i = 0; i < times; i++) Atoms(c, into, items);
            else if (c.Occurs is { } n)
            {
                // A fixed-OCCURS or OCCURS DEPENDING table takes its MAXIMUM length — §14.8.2.2's own sentence
                // for an occurs-depending group passed by reference, and §8.5.1.12.3 sentence 3's "its fixed
                // number of occurrences or the value of the DEPENDING operand, as applicable" resolved
                // statically (the DEPENDING operand's run-time value is not a compile-time quantity).
                into.Add(TableAtom(c, c.OccursSpec?.DependingName is null ? GroupAtomKind.Table : GroupAtomKind.OdoTable, n));
                items.Add(c);
            }
            else if (c.IsDynamicLength)
            {
                into.Add(new GroupAtom(GroupAtomKind.DynamicLength, 0, 0));
                items.Add(c);
            }
            else if (c.IsGroup)
                Atoms(c, into, items);
            else
            {
                into.Add(new GroupAtom(GroupAtomKind.Fixed, c.ByteWidth, c.ImageWidth));
                items.Add(c);
            }
        }
    }

    /// <summary>The atoms of a level-66 THROUGH alias (kb/Work PB907). §13.18.45.4 GR2 makes the alias "an
    /// alphanumeric group item that includes all elementary items starting with data-name-2 … and concluding
    /// with data-name-3", so its layout is the record's storage window the resolver already tiled into
    /// <see cref="RenamesInfo.Span"/> — read HERE, never re-walked, so the alias's atoms and its carrier cannot
    /// disagree about where a byte lies. A WHOLE table leaf in the window is a table atom (a fixed OCCURS table —
    /// §13.18.45.3 SR8 bars "a variable-length data item, or an occurs-depending table" from the range, so no dynamic
    /// atom can arise and the alias is always a FIXED-length group); every other part, including a single occurrence
    /// or a partial slice of one, is plain bytes, because a table only part of which lies in the window is not a table
    /// of the alias.</summary>
    private static void AliasAtoms(RenamesInfo ren, List<GroupAtom> into, List<DataItem> items)
    {
        foreach (var part in ren.Span)
        {
            var leaf = part.Leaf;
            items.Add(leaf);
            if (part.IsWhole && leaf.Occurs is { } n)
            {
                into.Add(TableAtom(leaf, GroupAtomKind.Table, n));
                continue;
            }
            // The part is kept in storage BYTES, the unit the relation is stated in; its CHARACTER positions (a
            // national leaf's character is two bytes) are RenamesSpanPart.Positions, the one conversion.
            into.Add(new GroupAtom(GroupAtomKind.Fixed, part.LengthBytes, part.Positions));
        }
    }

    private static Laid AtomsOf(DataItem g)
    {
        var atoms = new List<GroupAtom>();
        var items = new List<DataItem>();
        if (g.Renames is { IsAlias: false } ren) AliasAtoms(ren, atoms, items);
        else Atoms(g, atoms, items);
        return new Laid([.. atoms], [.. items]);
    }

    /// <summary>⛔ THE GROUP'S §8.5.1.12 ATOMS as the run time reads them (<see cref="GroupAtom"/>) — what a universal
    /// dispatch carries in an <see cref="ActivationDescription"/> so the ONE walk can decide a pair compiled apart
    /// (kb/Work PB480). <see langword="null"/> for a non-group or a subtree with a USAGE BIT leaf (§8.5.1.6.3's
    /// shared-byte runs make a character position non-positional).</summary>
    public static GroupAtom[]? GroupAtoms(DataItem g) =>
        !ItemCategory.IsGroupItem(g) || g.HasBitDescendant ? null : AtomsOf(g).Atoms;

    /// <summary>⛔ THE GROUP'S §8.5.1.12 LAYOUT as the runtime reads it — flat <c>(kind, chars, elementChars)</c>
    /// triples in CHARACTER positions (<see cref="GroupCompatibility.Layout"/>, derived from the same atoms the walk
    /// reads; kb/Work PB965). It is the compile-time half of <c>CobolVarGroup.CorrespondingSpans</c>, which pairs a
    /// FIXED group's layout with a VARIABLE-length group's and answers which of the fixed group's tables correspond
    /// (§8.5.1.12.2) — the spans <c>CobolVarGroup.FromFixedImage</c> lifts out so a fixed group can stand on the other
    /// side of an ISO §14.9.25.4 GR9 move. Across a CALL each side is compiled apart, so the ATOMS travel
    /// (<c>CobolArg.Atoms</c>, <see cref="GroupAtoms"/>; kb/Work PB2280) and the runtime derives this layout from them.
    /// <see langword="null"/> for a non-group or a subtree with a USAGE BIT leaf.</summary>
    private static int[]? Layout(DataItem g) => GroupAtoms(g) is { } atoms ? GroupCompatibility.Layout(atoms) : null;

    /// <summary>True when a group's atoms have anything but fixed material — a table or a variable-length member, the
    /// only atoms §8.5.1.12.2's correspondence can pair. Fixed material alone states nothing beyond the group's length,
    /// which the runtime recovers from the carrier (<c>GroupCompatibility.FixedRun</c>), so the boundary does not emit
    /// it.</summary>
    public static bool HasTableOrVariable(GroupAtom[] atoms)
    {
        foreach (var a in atoms)
            if (a.Kind != GroupAtomKind.Fixed) return true;
        return false;
    }

    /// <summary>The spans of <paramref name="fixedGroup"/>'s tables that correspond to
    /// <paramref name="varGroup"/>'s dynamic-capacity tables — the compile-time call of the ONE correspondence
    /// walk (<c>CobolVarGroup.CorrespondingSpans</c>), for a pair both of whose descriptions are in hand at an INVOKE
    /// boundary (<c>OoEmitter</c>'s fixed-group arms; a statement — the §14.9.25.4 GR9 MOVE, the §8.8.4.2.17 relation —
    /// pairs the two shapes through <c>CobolVarGroup.Reshape</c> / <c>Compare</c> instead, kb/Work PB2496). Null when
    /// either layout cannot be stated or the pair does not correspond.</summary>
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
        Laid a = AtomsOf(one), b = AtomsOf(other);
        return GroupCompatibility.Walk(a.Atoms, b.Atoms) is { } why ? Reason(why, a, b, one, other) : null;
    }

    /// <summary>The CORRESPONDING TABLE PAIRS of a compatible group pair, in left-to-right order — each pair
    /// §8.5.1.12.2 sentence 2 makes correspond ("at least one of them is a dynamic-capacity table and they occupy
    /// the same relative byte positions within their groups"), as (<paramref name="one"/>'s table,
    /// <paramref name="other"/>'s table). It is the SAME walk that decides compatibility, so a pair is listed exactly
    /// when the relation matched it. A table beyond the shorter group's last character corresponds only to "a
    /// space-filled fixed-length table" (§8.5.1.12.2's last sentence) — no table of the other group — and is not
    /// listed. Consumer: the §14.9.25.4 GR9 MOVE, whose §14.6.9.2 element moves are per corresponding pair (kb/Work
    /// PB1144). Empty when the pair is not compatible or either side is not a group.</summary>
    public static IReadOnlyList<(DataItem One, DataItem Other)> CorrespondingTables(DataItem one, DataItem other)
    {
        if (!ItemCategory.IsGroupItem(one) || !ItemCategory.IsGroupItem(other)) return [];
        Laid a = AtomsOf(one), b = AtomsOf(other);
        var pairs = new List<(int First, int Second)>();
        if (GroupCompatibility.Walk(a.Atoms, b.Atoms, pairs) is not null) return [];
        return [.. pairs.Where(p => a.Atoms[p.First].IsTable).Select(p => (a.Items[p.First], b.Items[p.Second]))];
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
        var tally = new GroupCharTally();
        return GroupCompatibility.Walk(AtomsOf(one).Atoms, AtomsOf(other).Atoms, tally: tally) is null
            ? (tally.First, tally.Second)
            : null;
    }

    /// <summary>A §8.5.1.12 reason (<see cref="GroupMismatch"/>) worded for a diagnostic, naming the items its atom
    /// indices denote in <paramref name="a"/> / <paramref name="b"/> (the groups <paramref name="ga"/> /
    /// <paramref name="gb"/>).</summary>
    private static string Reason(GroupMismatch m, Laid a, Laid b, DataItem ga, DataItem gb)
    {
        string? Item(bool first, int i) => (first ? a : b).Items[i].CobolName;
        string? Host(bool first) => (first ? ga : gb).CobolName;
        switch (m.Kind)
        {
            case GroupMismatchKind.DynamicLengthUnpaired:
                return $"the dynamic-length item '{Item(m.OnFirst, m.Atom)}' of '{Host(m.OnFirst)}' starts at "
                    + $"relative byte position {m.Position} and the other group has no dynamic-length item there "
                    + "(ISO §8.5.1.12.1 rule 3 / §8.5.1.12.2)";
            case GroupMismatchKind.DynamicLengthBeyondEnd:
                return $"the dynamic-length item '{Item(m.OnFirst, m.Atom)}' of '{Host(m.OnFirst)}' lies beyond "
                    + $"the last byte of '{Host(!m.OnFirst)}', which therefore has no corresponding "
                    + "dynamic-length item (ISO §8.5.1.12.1 rule 3 / §8.5.1.12.2)";
            case GroupMismatchKind.DynamicTableOppositeNonTable:
                return $"the dynamic-capacity table '{Item(m.OnFirst, m.Atom)}' of '{Host(m.OnFirst)}' occupies "
                    + $"relative byte position {m.Position} and the other group has no table there "
                    + "(ISO §8.5.1.12.1 rule 1 / §8.5.1.12.2)";
            case GroupMismatchKind.DynamicTableUnpaired:
                return $"the dynamic-capacity table '{Item(m.OnFirst, m.Atom)}' of '{Host(m.OnFirst)}' occupies relative "
                    + $"byte position {m.Position} and the other group has no corresponding table there "
                    + "(ISO §8.5.1.12.1 rule 1 / §8.5.1.12.2)";
        }
        DataItem x = a.Items[m.Atom], y = b.Items[m.OtherAtom];
        if (m.Kind is GroupMismatchKind.ElementBytesDiffer)
            return $"corresponding tables '{x.CobolName}' and '{y.CobolName}' do not match: "
                + $"their elements are {a.Atoms[m.Atom].ElementBytes} and {b.Atoms[m.OtherAtom].ElementBytes} bytes "
                + "(ISO §8.5.1.12.3 — the byte length of their elements shall be equal)";
        // §8.5.1.12.3 sentence 2's second conjunct — "their elements are compatible": a group element recursed into
        // the same walk (over the tables' own atoms); an elementary element opposite a group one does not.
        var inner = m.Inner!;
        string innerText = inner.Kind is GroupMismatchKind.ElementShapeDiffers
            ? $"'{(inner.OnFirst ? x : y).CobolName}' is elementary and '{(inner.OnFirst ? y : x).CobolName}' is a group"
            : Reason(inner, AtomsOf(x), AtomsOf(y), x, y);
        return $"corresponding tables '{x.CobolName}' and '{y.CobolName}' do not match: "
            + $"their elements are not compatible — {innerText} (ISO §8.5.1.12.3)";
    }
}
