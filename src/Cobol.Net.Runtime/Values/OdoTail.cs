// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>
/// The OCCURS DEPENDING table a variable-length group holds as its TRAILING storage (kb/Work PB244, PB2497): ISO
/// §13.18.38.3 SR22 lets the table's subject be followed within its record only by entries subordinate to it, so the
/// table is the last thing of every group that contains it. ONE geometry serves both readers of it:
/// <list type="bullet">
/// <item>a CELL-BACKED group's window, which reserves the table at its MAXIMUM (<see cref="Max"/> elements of
/// <see cref="Elem"/> positions) — the cell's group helpers (<see cref="StorageCell.VarGroupAt"/> and its siblings)
/// take it beside their component layout and cut the run to the operand's count;</item>
/// <item>the CONTIGUOUS layout of a record (<see cref="CobolContiguousLayout"/>, a declared group's generated
/// <c>__contiguous</c> and a cell's alike), which must know the table to split a record read back: a table of
/// elements of a FIXED image (<see cref="Comps"/> zero) is the layout's last component, one element per unit, so the
/// record's own length says how many occurrences it holds; a table of VARIABLE-LENGTH elements (<see cref="Comps"/>
/// above zero) contributes <see cref="Comps"/> components per occurrence, and the record's extent table says how many
/// occurrences it holds by how many components it describes (<see cref="CountFrom"/>).</item>
/// </list>
/// <c>default</c> (<see cref="Present"/> false) is a group that holds no such table.
/// </summary>
/// <param name="Elem">One occurrence's width OF THE GROUP'S FIXED RUN in the table's unit: character positions (a
/// dynamic-capacity table inside an element reserves its one-element extent in the window and none in the run; a
/// dynamic-length item occupies none), or BITS for a table counted in bits (<paramref name="PositionsPerCharacter"/>).</param>
/// <param name="Max">The table's maximum occurrence count (OCCURS … TO integer-2).</param>
/// <param name="Comps">How many variable-length COMPONENTS (dynamic-length items, dynamic-capacity tables) one
/// occurrence holds - zero for an element of a fixed image (kb/Work PB244). A table's components are numbered per
/// occurrence (<c>CellComponents</c>; <c>GroupImageCodec.VarParts</c>' flattening), and the table is the LAST thing of
/// the group, so they are its last <c>Max * Comps</c> components and the occurrences beyond the count contribute none
/// of them.</param>
/// <param name="Lead">For a table counted in bits: how many bits of the fixed material before the table share the
/// character that holds the table's first bit. Zero for a table counted in characters.</param>
/// <param name="PositionsPerCharacter">The table's unit: 1 when <paramref name="Elem"/> counts characters, 8 when it
/// counts BITS — a table of fixed-image elements under which a USAGE BIT item lies (kb/Work PB3030): ISO §8.5.1.6.3
/// places a bit item "at the next bit position in storage", so an occurrence may share a byte and the table's
/// characters are the CEILING of its bits (§15.50.4 rule 9). The compiler's <c>OdoModel.FixedImageTail</c> is the one
/// producer of a tail of this shape, from the one geometry <c>OdoModel.GeometryOf</c>.</param>
/// <param name="Trailing">The filler, in the table's unit, that pads the LAST occurrence out to the stride
/// <paramref name="Elem"/> — zero but for a table counted in bits whose occurrences stride whole bytes (an ALIGNED subject
/// or member, ISO §13.18.1.4 GR2 "applies to each occurrence of that item"; kb/Work PB3156, PB3167). §8.5.1.6.3 generates
/// filler only "as needed to advance alignment to a required natural boundary for the next item", and the last
/// occurrence has no next one, so the table ends <paramref name="Trailing"/> units short of its stride count. A table
/// that begins inside a character (<paramref name="Lead"/> above zero) then ends a character earlier than the stride count
/// says (kb/Work PB3167 review R1-1).</param>
public readonly record struct OdoTail(int Elem, int Max, int Comps = 0, int Lead = 0, int PositionsPerCharacter = 1,
                                      int Trailing = 0)
{
    /// <summary>True when the group holds the table.</summary>
    public bool Present => Max > 0;

    /// <summary>The table's width at its maximum, in characters — what the group's window reserves.</summary>
    public int Width => CharactersAt(Max);

    /// <summary>The characters <paramref name="count"/> occurrences occupy from the character that holds the table's
    /// first position (the ceiling of their bits for a table counted in bits): the ONE extent rule
    /// <see cref="CobolTable.TrimmedExtent"/> the OCCURS DEPENDING extent channels read too, so the last occurrence's
    /// <see cref="Trailing"/> filler is given back before the ceiling exactly as theirs is.</summary>
    private int CharactersAt(int count) =>
        (CobolTable.TrimmedExtent(count, Lead, Elem, Trailing) + PerCharacter - 1) / PerCharacter;

    /// <summary><see cref="PositionsPerCharacter"/>, 1 for <c>default</c> (a group that holds no table).</summary>
    private int PerCharacter => PositionsPerCharacter > 1 ? PositionsPerCharacter : 1;

    /// <summary>How many trailing characters of the group's MAXIMUM image lie beyond <paramref name="count"/>
    /// occurrences — the current extent of the group is its maximum image less this suffix (ISO §13.18.38.4 GR8:
    /// only the part the DEPENDING item names is used). A count outside 0..<see cref="Max"/> is clamped, as the
    /// declared group's <c>__odo</c> argument already is (<c>TableOdoExtent</c>, EC-BOUND-ODO).</summary>
    public int CutAt(int count) => Present ? Width - CharactersAt(Math.Clamp(count, 0, Max)) : 0;

    /// <summary>The table's unit as the LAST component of a record layout (<see cref="CobolContiguousLayout"/>'s
    /// <c>Odo</c>; determination D-FRA (ix)): one element per unit for a table counted in characters, so the record's
    /// length says its count; one CHARACTER per unit for a table counted in bits, whose occurrences have no character
    /// boundary of their own.</summary>
    public int RecordUnit => PerCharacter == 1 ? Elem : 1;

    /// <summary>The most <see cref="RecordUnit"/>s the table occupies in a record.</summary>
    public int RecordMaxUnits => PerCharacter == 1 ? Max : Width;

    /// <summary>A receiving store's run when only <paramref name="count"/> occurrences are used (ISO §13.18.38.4 GR8 a:
    /// "only that part of the table area that is specified by the value of the data item referenced by data-name-1 at
    /// the start of the operation will be used"): <paramref name="received"/> up to the end of the first
    /// <paramref name="count"/> occurrences, <paramref name="current"/> from there on, so the remaining occurrences
    /// keep their content. Both strings are the same run, whose trailing <see cref="Width"/> characters hold the table
    /// (§13.18.38.3 SR22). A table counted in bits is spliced at the bit (<see cref="CobolBits.WriteWindow"/>): its
    /// first unused occurrence may start inside a character.</summary>
    public string KeepUnused(string received, string current, int count)
    {
        if (!Present || count >= Max) return received;
        if (PerCharacter == 1)
        {
            int cut = CutAt(count);
            return received[..^cut] + current[^cut..];
        }
        int from = (current.Length - Width) * PerCharacter + Lead + Math.Max(0, count) * Elem;
        return CobolBits.WriteWindow(received, from,
            CobolBits.ReadWindow(current, from, current.Length * PerCharacter - from));
    }

    /// <summary>How many of the group's trailing COMPONENTS belong to the occurrences beyond <paramref name="count"/>
    /// - the components the current extent drops with them (<see cref="CutAt"/> drops their fixed run). Zero for a
    /// table whose elements hold none.</summary>
    public int CutComponents(int count) => Present ? (Max - Math.Clamp(count, 0, Max)) * Comps : 0;

    /// <summary>The occurrence count a record holds when it carries <paramref name="components"/> of the
    /// <paramref name="total"/> components the group has at the table's maximum — the inverse of
    /// <see cref="CutComponents"/> (kb/Work PB2497): every occurrence the record holds brought its <see cref="Comps"/>
    /// components, the ones beyond it none. Null when no count gives that many (the record was sent through another
    /// description) or when the elements hold no component, where the record's LENGTH says the count instead.</summary>
    public int? CountFrom(int components, int total)
    {
        if (Comps <= 0) return null;
        int beyond = total - components;
        return beyond >= 0 && beyond % Comps == 0 && beyond / Comps <= Max ? Max - beyond / Comps : null;
    }
}
