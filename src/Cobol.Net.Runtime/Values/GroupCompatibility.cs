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
/// <param name="ElementBytes">A table's one-occurrence byte length (§8.5.1.12.3 sentence 2 compares it); zero
/// otherwise.</param>
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
    /// <summary>Two corresponding tables whose element byte lengths differ (§8.5.1.12.3 sentence 2).</summary>
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
public sealed record GroupMismatch(GroupMismatchKind Kind, bool OnFirst, int Atom, int OtherAtom, long Position,
    GroupMismatch? Inner = null);

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
        GroupCharTally? tally = null)
    {
        int ia = 0, ib = 0;
        long pa = 0, pb = 0;
        while (ia < a.Length || ib < b.Length)
        {
            if (ia >= a.Length) return Tail(b, ib, tally, first: false);
            if (ib >= b.Length) return Tail(a, ia, tally, first: true);
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
                if (x.ElementBytes != y.ElementBytes)
                    return new GroupMismatch(GroupMismatchKind.ElementBytesDiffer, true, ia, ib, pa);
                if (Elements(x, y) is { } inner)
                    return new GroupMismatch(GroupMismatchKind.ElementsIncompatible, true, ia, ib, pa, inner);
                // §8.5.1.12.3 sentence 3: when only ONE of the pair is a dynamic-capacity table, the dynamic one "is
                // considered to be the same length as the corresponding table"; when BOTH are, sentence 4 makes each
                // one element long — which is the Bytes each atom already carries.
                long len = xDyn && yDyn ? x.ElementBytes : xDyn ? y.Bytes : x.Bytes;
                if (tally is not null)
                {
                    // The same two sentences in CHARACTER positions.
                    bool both = xDyn && yDyn;
                    tally.First += both || !xDyn ? x.Chars : y.Chars;
                    tally.Second += both || !yDyn ? y.Chars : x.Chars;
                }
                pairs?.Add((ia, ib));
                pa += len; pb += len; ia++; ib++;
                continue;
            }
            // Plain material on both sides: consume one atom and let the position compare above re-align.
            if (tally is not null) tally.First += x.Chars;
            pa += x.Bytes; ia++;
        }
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

    /// <summary>The atoms of the LONGER group beyond the shorter group's last byte. §8.5.1.12.2's last sentence grants
    /// exactly one latitude here — a dynamic-capacity table there "is treated as if it corresponds to a space-filled
    /// fixed-length table" — and grants it to TABLES ONLY. A trailing dynamic-LENGTH item still needs a real
    /// counterpart (rule 3), so it fails.</summary>
    private static GroupMismatch? Tail(GroupAtom[] list, int i, GroupCharTally? tally, bool first)
    {
        for (; i < list.Length; i++)
        {
            if (tally is not null)
            {
                if (first) tally.First += list[i].Chars;
                else tally.Second += list[i].Chars;
            }
            if (list[i].Kind is GroupAtomKind.DynamicLength)
                return new GroupMismatch(GroupMismatchKind.DynamicLengthBeyondEnd, first, i, -1, 0);
        }
        return null;
    }

    /// <summary>§8.5.1.12.3 sentence 2's second conjunct — "their elements are compatible". A group element recurses
    /// into the SAME relation; an elementary element has no atoms of its own and its byte length was compared by the
    /// caller.</summary>
    private static GroupMismatch? Elements(GroupAtom x, GroupAtom y) =>
        x.Element is { } ex && y.Element is { } ey ? Walk(ex, ey)
        : (x.Element is null) != (y.Element is null)
            ? new GroupMismatch(GroupMismatchKind.ElementShapeDiffers, x.Element is null, -1, -1, 0)
            : null;

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
