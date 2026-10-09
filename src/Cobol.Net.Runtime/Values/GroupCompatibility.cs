// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>The kind of one positional atom of a group's ISO §8.5.1.12 layout.</summary>
public enum GroupAtomKind
{
    /// <summary>An elementary item that is neither a table nor dynamic-length — plain material.</summary>
    Fixed,
    /// <summary>A dynamic-length elementary item (§8.5.1.10) — zero bytes for position purposes (§8.5.1.12.3).</summary>
    DynamicLength,
    /// <summary>A table with a fixed number of occurrences.</summary>
    Table,
    /// <summary>An OCCURS DEPENDING table, at its maximum length (§14.8.2.2: "the maximum length is used").</summary>
    OdoTable,
    /// <summary>A dynamic-capacity table (§8.5.1.9) — one element long for position purposes (§8.5.1.12.3).</summary>
    DynamicTable,
}

/// <summary>One positional atom of a group's §8.5.1.12 layout: the group FLATTENED left to right (REDEFINES subtrees
/// dropped, scalar subordinate groups flattened, a table kept whole), as the compiler's
/// <c>VariableLengthCompatibility</c> builds it from the data description and the generated code carries it across a
/// universal dispatch (<see cref="ActivationDescription.Atoms"/>).</summary>
/// <param name="Kind">Which of the §8.5.1.12 roles the atom plays.</param>
/// <param name="Bytes">Its contribution to the relative BYTE position under §8.5.1.12.3's conventions (a dynamic-length
/// item zero, a dynamic-capacity table one element) — the quantity §8.5.1.12.2's correspondence is stated in.</param>
/// <param name="Chars">Its contribution in CHARACTER positions — the carrier's geometry (<see cref="CobolVarGroup"/>), where
/// a national character is two bytes.</param>
/// <param name="ElementBytes">A table's one-occurrence byte length under §8.5.1.12.3's collapse conventions; zero
/// otherwise. §8.5.1.12.3 sentence 2 compares it directly only for an ELEMENTARY element: a group element's length for
/// the pair is measured by the walk over <paramref name="Element"/> (kb/Work PB2689).</param>
/// <param name="ElementChars">A table's one-occurrence character length; zero otherwise.</param>
/// <param name="Element">A table's element atoms when the element is a GROUP (§8.5.1.12.3's "their elements are
/// compatible" recurses into them); null for an elementary element and for every non-table atom.</param>
public sealed record GroupAtom(GroupAtomKind Kind, int Bytes, int Chars, int ElementBytes = 0, int ElementChars = 0,
    GroupAtom[]? Element = null)
{
    /// <summary>A table of any kind.</summary>
    public bool IsTable => Kind is GroupAtomKind.Table or GroupAtomKind.OdoTable or GroupAtomKind.DynamicTable;

    /// <summary>A variable-length data item (§8.5.1.11.1) — a component of the <see cref="CobolVarGroup"/> carrier;
    /// every other atom's characters lie in the carrier's fixed run.</summary>
    public bool IsComponent => Kind is GroupAtomKind.DynamicLength or GroupAtomKind.DynamicTable;
}

/// <summary>Why two groups are not compatible (§8.5.1.12.1 rules 1–3) — the facts a diagnostic names; the compiler
/// renders them with item names, the run time with positions.</summary>
public enum GroupMismatchKind
{
    /// <summary>A dynamic-length item with no dynamic-length item at its relative byte position in the other group
    /// (§8.5.1.12.1 rule 3 / §8.5.1.12.2).</summary>
    DynamicLengthUnpaired,
    /// <summary>A dynamic-capacity table opposite an atom that starts at its position but is not a table (§8.5.1.12.1
    /// rule 1 / §8.5.1.12.2).</summary>
    DynamicTableOppositeNonTable,
    /// <summary>A dynamic-capacity table at a position the other group has no atom starting at (§8.5.1.12.1 rule 1 /
    /// §8.5.1.12.2).</summary>
    DynamicTableUnpaired,
    /// <summary>Two corresponding tables whose element byte lengths, as the pair measures them, differ (§8.5.1.12.3
    /// sentence 2; <see cref="GroupMismatch.FirstLength"/> and <see cref="GroupMismatch.SecondLength"/>).</summary>
    ElementBytesDiffer,
    /// <summary>Two corresponding tables whose elements are not compatible (§8.5.1.12.3 sentence 2) — the reason is
    /// <see cref="GroupMismatch.Inner"/>.</summary>
    ElementsIncompatible,
    /// <summary>The inner reason of <see cref="ElementsIncompatible"/> when one element is elementary and the other a
    /// group.</summary>
    ElementShapeDiffers,
    /// <summary>A dynamic-length item beyond the shorter group's last byte (§8.5.1.12.2's last sentence grants the
    /// space-filled latitude to tables only).</summary>
    DynamicLengthBeyondEnd,
}

/// <summary>The reason two groups are not compatible.</summary>
/// <param name="Kind">The rule that failed.</param>
/// <param name="OnFirst">True when <paramref name="Atom"/> indexes the FIRST group's atoms.</param>
/// <param name="Atom">The atom the reason is about (for a table pair, the first group's table).</param>
/// <param name="OtherAtom">The other group's atom of a table pair; −1 otherwise.</param>
/// <param name="Position">The relative byte position the reason is stated at.</param>
/// <param name="Inner">For <see cref="GroupMismatchKind.ElementsIncompatible"/>, the elements' reason.</param>
/// <param name="FirstLength">For <see cref="GroupMismatchKind.ElementBytesDiffer"/>, the first table's element byte
/// length as the pair measures it (§8.5.1.12.3 — not its one-occurrence atom when the element holds a dynamic-capacity
/// table); zero otherwise.</param>
/// <param name="SecondLength">The other table's element byte length, likewise.</param>
public sealed record GroupMismatch(GroupMismatchKind Kind, bool OnFirst, int Atom, int OtherAtom, long Position,
    GroupMismatch? Inner = null, long FirstLength = 0, long SecondLength = 0);

/// <summary>The per-side character tally the walk accumulates — the pair-relative character lengths of two
/// groups (<c>VariableLengthCompatibility.PairCharWidths</c>).</summary>
public sealed class GroupCharTally
{
    /// <summary>The first group's pair-relative character length.</summary>
    public long First;
    /// <summary>The second group's pair-relative character length.</summary>
    public long Second;
}

/// <summary>
/// ⛔ THE ONE ISO §8.5.1.12 VARIABLE-LENGTH-GROUP COMPATIBILITY WALK (kb/Work PB204, PB480), over two groups' atom
/// layouts. The compiler asks it with the atoms it builds from two data descriptions (every bind-time §14.8.2.2 /
/// §14.8.3.2 / §14.9.25.3 SR9 / relation-condition question, through <c>VariableLengthCompatibility</c>); the run
/// time asks it with the atoms two independently compiled descriptions carry across a universal dispatch
/// (<see cref="ActivationRelations"/>) and to reshape one group's carrier into the other's
/// (<see cref="CobolVarGroup.Reshape"/>). One walk, so the two phases cannot disagree about which pair is compatible.
/// <para>THE THREE RULES are ONE positional walk because they are three statements about the SAME left-to-right byte
/// accounting:</para>
/// <list type="number">
/// <item>§8.5.1.12.1 rule 1 + §8.5.1.12.2 sentence 2 — for each dynamic-capacity table in either group there is a
/// corresponding table in the other: "Two tables correspond if at least one of them is a dynamic-capacity table and they
/// occupy the same relative byte positions within their groups".</item>
/// <item>§8.5.1.12.1 rule 2 + §8.5.1.12.3 sentence 2 — "Two corresponding tables match when the byte length of their
/// elements is equal and their elements are compatible" (the element compatibility recurses HERE).</item>
/// <item>§8.5.1.12.1 rule 3 + §8.5.1.12.2 sentence 1 — "Two dynamic-length elementary items correspond if they start at
/// the same relative byte positions within their groups"; §8.5.1.12.3 sentence 1 then makes them match "regardless of
/// their definitions".</item>
/// </list>
/// <para>The atoms carry §8.5.1.12.3's two collapse conventions already (<see cref="GroupAtom.Bytes"/>); the one the walk
/// applies itself is sentence 3 — "If one of the corresponding tables is not a dynamic-capacity table, that table is
/// treated as though it were a dynamic-capacity table" and the dynamic one is "considered to be the same length as the
/// corresponding table" — a fact about a PAIR, which no atom can hold. Fixed material between the variable-length items
/// may differ in shape and, after the last one, in length: the standard constrains the POSITIONS of the variable-length
/// items only.</para>
/// </summary>
public static class GroupCompatibility
{
    /// <summary>Null when <paramref name="a"/> and <paramref name="b"/> are compatible per §8.5.1.12, else the reason.
    /// <paramref name="pairs"/>, when not null, receives every CORRESPONDING pair the walk matched — dynamic-length
    /// items and tables — as (<paramref name="a"/> index, <paramref name="b"/> index), left to right;
    /// <paramref name="tally"/>, when not null, the pair-relative character lengths.</summary>
    public static GroupMismatch? Walk(GroupAtom[] a, GroupAtom[] b, List<(int First, int Second)>? pairs = null,
        GroupCharTally? tally = null) => Walk(a, b, pairs, tally, out _, out _);

    /// <summary>The walk itself. <paramref name="lengthA"/> / <paramref name="lengthB"/> receive the two groups'
    /// pair-relative BYTE lengths — each group's length under §8.5.1.12.3's conventions as they apply to THIS pair
    /// (sentences 3 and 4 decide a corresponding table's length from the pair, not from either table alone) — and are
    /// meaningful only when the walk returns null. They are what §8.5.1.12.3 sentence 2's "the byte length of their
    /// elements" means for two group elements (<see cref="Elements"/>).</summary>
    private static GroupMismatch? Walk(GroupAtom[] a, GroupAtom[] b, List<(int First, int Second)>? pairs,
        GroupCharTally? tally, out long lengthA, out long lengthB)
    {
        int ia = 0, ib = 0;
        long pa = 0, pb = 0;
        lengthA = lengthB = 0;
        while (ia < a.Length || ib < b.Length)
        {
            if (ia >= a.Length)
            {
                lengthA = pa;
                return Tail(b, ib, pb, pa, tally, first: false, out lengthB);
            }
            if (ib >= b.Length)
            {
                lengthB = pb;
                return Tail(a, ia, pa, pb, tally, first: true, out lengthA);
            }
            // The two sides are walked to the SAME relative byte position before any correspondence is decided:
            // §8.5.1.12.2 states BOTH correspondences as "the same relative byte positions within their groups".
            if (pa < pb)
            {
                if (tally is not null) tally.First += a[ia].Chars;
                if (Skip(a, ref ia, ref pa, first: true) is { } e) return e;
                continue;
            }
            if (pb < pa)
            {
                if (tally is not null) tally.Second += b[ib].Chars;
                if (Skip(b, ref ib, ref pb, first: false) is { } e) return e;
                continue;
            }

            var x = a[ia];
            var y = b[ib];
            // Rule 3: dynamic-length items correspond by position and then ALWAYS match.
            if (x.Kind is GroupAtomKind.DynamicLength || y.Kind is GroupAtomKind.DynamicLength)
            {
                if (x.Kind != y.Kind)
                {
                    bool loneFirst = x.Kind is GroupAtomKind.DynamicLength;
                    return new GroupMismatch(GroupMismatchKind.DynamicLengthUnpaired, loneFirst,
                        loneFirst ? ia : ib, -1, pa);
                }
                pairs?.Add((ia, ib));
                ia++; ib++;
                continue;
            }
            // Rules 1 and 2 (§8.5.1.12.2 sentence 2 + §8.5.1.12.3 sentences 2-4).
            bool xDyn = x.Kind is GroupAtomKind.DynamicTable, yDyn = y.Kind is GroupAtomKind.DynamicTable;
            if (xDyn || yDyn)
            {
                if (!x.IsTable || !y.IsTable)
                    return new GroupMismatch(GroupMismatchKind.DynamicTableOppositeNonTable, xDyn, xDyn ? ia : ib, -1, pa);
                // §8.5.1.12.3 sentence 2: "Two corresponding tables match when the byte length of their elements is
                // equal and their elements are compatible". The element lengths compared are the ones the element walk
                // MEASURES for the pair (kb/Work PB2689): an element holding a dynamic-capacity table has no static
                // length for this purpose, because sentence 3 makes that table "the same length as the corresponding
                // table" — so a dynamic table's element holding `OCCURS DYNAMIC` matches one holding `OCCURS 2` when
                // the rest agrees, although their one-element atoms are 1 and 2 bytes.
                var elementChars = tally is null ? null : new GroupCharTally();
                var inner = Elements(x, y, elementChars, out long ex, out long ey);
                if (inner is null or { Kind: GroupMismatchKind.ElementShapeDiffers } && ex != ey)
                    return new GroupMismatch(GroupMismatchKind.ElementBytesDiffer, true, ia, ib, pa,
                        FirstLength: ex, SecondLength: ey);
                if (inner is not null)
                    return new GroupMismatch(GroupMismatchKind.ElementsIncompatible, true, ia, ib, pa, inner);
                // §8.5.1.12.3 sentence 3: when only ONE of the pair is a dynamic-capacity table, the dynamic one "is
                // considered to be the same length as the corresponding table"; when BOTH are, sentence 4 makes each
                // "the length of a single element of that table" — the element length the pair just measured.
                bool both = xDyn && yDyn;
                long len = both ? ex : xDyn ? y.Bytes : x.Bytes;
                if (tally is not null)
                {
                    // The same two sentences in CHARACTER positions.
                    tally.First += both ? elementChars!.First : !xDyn ? x.Chars : y.Chars;
                    tally.Second += both ? elementChars!.Second : !yDyn ? y.Chars : x.Chars;
                }
                pairs?.Add((ia, ib));
                pa += len; pb += len; ia++; ib++;
                continue;
            }
            // Plain material on both sides: consume one atom and let the position compare above re-align.
            if (tally is not null) tally.First += x.Chars;
            pa += x.Bytes; ia++;
        }
        (lengthA, lengthB) = (pa, pb);
        return null;
    }

    /// <summary>Walk the lagging side forward over material the other side has already passed. A variable-length item
    /// found here sits at a byte position the other group has no counterpart at, so it fails rules 1/3.</summary>
    private static GroupMismatch? Skip(GroupAtom[] list, ref int i, ref long p, bool first)
    {
        var at = list[i];
        if (at.Kind is GroupAtomKind.DynamicLength)
            return new GroupMismatch(GroupMismatchKind.DynamicLengthUnpaired, first, i, -1, p);
        if (at.Kind is GroupAtomKind.DynamicTable)
            return new GroupMismatch(GroupMismatchKind.DynamicTableUnpaired, first, i, -1, p);
        p += at.Bytes;
        i++;
        return null;
    }

    /// <summary>The remaining atoms of the LONGER group, from relative byte position <paramref name="p"/>, once the
    /// other group's atoms are spent at <paramref name="shorter"/> bytes. An atom that still starts INSIDE the shorter
    /// group (<c>p &lt; shorter</c>: the shorter group's last atom covers it) is opposite material that started
    /// earlier, exactly as in <see cref="Skip"/>, so a variable-length item there fails rule 1 or 3. Past the shorter
    /// group's last byte §8.5.1.12.2's last sentence grants exactly one latitude — a dynamic-capacity table there "is
    /// treated as if it corresponds to a space-filled fixed-length table" — and grants it to TABLES ONLY: a trailing
    /// dynamic-LENGTH item still needs a real counterpart (rule 3), so it fails. <paramref name="length"/> receives
    /// the longer group's byte length.</summary>
    private static GroupMismatch? Tail(GroupAtom[] list, int i, long p, long shorter, GroupCharTally? tally,
        bool first, out long length)
    {
        length = 0;
        for (; i < list.Length; i++)
        {
            var at = list[i];
            if (tally is not null)
            {
                if (first) tally.First += at.Chars;
                else tally.Second += at.Chars;
            }
            if (p < shorter && at.IsComponent)
                return at.Kind is GroupAtomKind.DynamicLength
                    ? new GroupMismatch(GroupMismatchKind.DynamicLengthUnpaired, first, i, -1, p)
                    : new GroupMismatch(GroupMismatchKind.DynamicTableUnpaired, first, i, -1, p);
            if (at.Kind is GroupAtomKind.DynamicLength)
                return new GroupMismatch(GroupMismatchKind.DynamicLengthBeyondEnd, first, i, -1, p);
            p += at.Bytes;
        }
        length = p;
        return null;
    }

    /// <summary>§8.5.1.12.3 sentence 2 for a pair of corresponding tables: whether "their elements are compatible", and
    /// (<paramref name="xBytes"/> / <paramref name="yBytes"/>) "the byte length of their elements" the caller compares.
    /// A group element recurses into the SAME relation, which measures the two elements' lengths for the pair; an
    /// elementary element has no atoms of its own and is its one-occurrence length. <paramref name="chars"/>, when not
    /// null, receives the elements' pair-relative character lengths.</summary>
    private static GroupMismatch? Elements(GroupAtom x, GroupAtom y, GroupCharTally? chars, out long xBytes,
        out long yBytes)
    {
        if (x.Element is { } ex && y.Element is { } ey) return Walk(ex, ey, null, chars, out xBytes, out yBytes);
        (xBytes, yBytes) = (x.ElementBytes, y.ElementBytes);
        if (chars is not null)
        {
            chars.First += x.ElementChars;
            chars.Second += y.ElementChars;
        }
        return (x.Element is null) != (y.Element is null)
            ? new GroupMismatch(GroupMismatchKind.ElementShapeDiffers, x.Element is null, -1, -1, 0)
            : null;
    }

    /// <summary>⛔ THE GROUP'S §8.5.1.12 LAYOUT as the carrier reads it — flat <c>(kind, chars, elementChars)</c>
    /// triples in CHARACTER positions, the kinds being <see cref="CobolVarGroup"/>'s <c>Layout*</c> constants (kb/Work
    /// PB965): consecutive fixed material merged into one run, a fixed table its whole width, an OCCURS DEPENDING table
    /// its maximum, a dynamic-capacity table one element, a dynamic-length item nothing. Derived from the atoms, so the
    /// layout and the compatibility walk read one description of the group.</summary>
    public static int[] Layout(GroupAtom[] atoms)
    {
        var outp = new List<int>(atoms.Length * 3);
        int run = 0;
        foreach (var a in atoms)
        {
            if (a.Kind is GroupAtomKind.Fixed)
            {
                run += a.Chars;
                continue;
            }
            if (run != 0) outp.AddRange([CobolVarGroup.LayoutFixed, run, 0]);
            run = 0;
            outp.AddRange(a.Kind switch
            {
                GroupAtomKind.DynamicLength => [CobolVarGroup.LayoutDynamicLength, 0, 0],
                GroupAtomKind.DynamicTable => [CobolVarGroup.LayoutDynamicTable, a.Chars, a.ElementChars],
                GroupAtomKind.OdoTable => [CobolVarGroup.LayoutOdoTable, a.Chars, a.ElementChars],
                _ => [CobolVarGroup.LayoutTable, a.Chars, a.ElementChars],
            });
        }
        if (run != 0) outp.AddRange([CobolVarGroup.LayoutFixed, run, 0]);
        return [.. outp];
    }

    /// <summary>How a layout reads in a message: each <see cref="Layout"/> triple as its character count, with
    /// <c>D</c> for a dynamic-length item, <c>T</c> plus the element's characters for a dynamic-capacity table, and an
    /// <c>OCCURS</c> / <c>ODO</c> suffix on a fixed / occurs-depending table.</summary>
    public static string Describe(GroupAtom[] atoms)
    {
        int[] l = Layout(atoms);
        var parts = new List<string>(l.Length / 3);
        for (int k = 0; k < l.Length; k += 3)
            parts.Add(l[k] switch
            {
                CobolVarGroup.LayoutDynamicLength => "D",
                CobolVarGroup.LayoutDynamicTable => $"T{l[k + 2]}",
                CobolVarGroup.LayoutTable => $"{l[k + 1]} OCCURS",
                CobolVarGroup.LayoutOdoTable => $"{l[k + 1]} ODO",
                _ => $"{l[k + 1]}",
            });
        return string.Join(",", parts);
    }

    /// <summary>The atoms of a group that states no layout of its own — one run of plain material of
    /// <paramref name="positions"/> character positions (a group whose description carries no atoms: one with a USAGE
    /// BIT leaf, whose shared-byte runs make a character position non-positional, §8.5.1.6.3).</summary>
    public static GroupAtom[] FixedRun(int positions) => [new GroupAtom(GroupAtomKind.Fixed, positions, positions)];

    /// <summary>⛔ True when a description with atoms <paramref name="formal"/> can be LAID OVER storage described by
    /// <paramref name="area"/> (kb/Work PB2094): the formal's <see cref="Layout"/> is the area's, or a prefix of it whose
    /// last fixed run may be shorter — every component, every table and every fixed run before it at the same character
    /// position and of the same kind and extent — and every pair of tables has elements of the same storage, recursively
    /// (an elementary element is the one fixed run of its characters). That is the condition under which each component
    /// of the formal IS the area's component at the same ordinal, so §14.2.3 GR8's "the formal parameter occupies the same
    /// storage area as the argument" holds per component; a shorter formal simply describes fewer of the positions. Unlike
    /// <see cref="SameShape"/> it ignores how fixed material is divided into elementary items: storage, not description.</summary>
    public static bool LaysOver(GroupAtom[] formal, GroupAtom[] area) => Overlays(formal, area, prefix: true);

    private static bool Overlays(GroupAtom[] formal, GroupAtom[] area, bool prefix)
    {
        int[] f = Layout(formal), a = Layout(area);
        if (prefix ? f.Length > a.Length : f.Length != a.Length) return false;
        for (int k = 0; k < f.Length; k += 3)
        {
            bool shorterTail = prefix && k + 3 == f.Length && f[k] == CobolVarGroup.LayoutFixed && f[k + 1] <= a[k + 1];
            if (f[k] != a[k] || f[k + 2] != a[k + 2] || f[k + 1] != a[k + 1] && !shorterTail) return false;
        }
        var (tf, ta) = (formal.Where(x => x.IsTable).ToArray(), area.Where(x => x.IsTable).ToArray());
        for (int i = 0; i < tf.Length; i++)
            if (!Overlays(tf[i].Element ?? FixedRun(tf[i].ElementChars), ta[i].Element ?? FixedRun(ta[i].ElementChars), prefix: false))
                return false;
        return true;
    }

    /// <summary>True when <paramref name="atoms"/> hold a variable-length component — a dynamic-length item or a
    /// dynamic-capacity table, at any depth (§8.5.1.12.1's definition of a variable-length group).</summary>
    public static bool HasComponent(GroupAtom[] atoms) =>
        atoms.Any(x => x.IsComponent || x.Element is { } e && HasComponent(e));

    /// <summary>True when the two layouts are the same atom for atom (element atoms included) — a pair whose carriers
    /// need no reshaping (<see cref="CobolVarGroup.Reshape"/>).</summary>
    public static bool SameShape(GroupAtom[] a, GroupAtom[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            var (x, y) = (a[i], b[i]);
            if (x.Kind != y.Kind || x.Bytes != y.Bytes || x.Chars != y.Chars || x.ElementBytes != y.ElementBytes
                || x.ElementChars != y.ElementChars || (x.Element is null) != (y.Element is null)
                || (x.Element is { } ex && !SameShape(ex, y.Element!)))
                return false;
        }
        return true;
    }
}
