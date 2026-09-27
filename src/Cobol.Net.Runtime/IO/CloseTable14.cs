// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ISO §14.9.6.4 GR2 — the partition every physical file falls into, and the ONE place WiseOwl COBOL's medium
/// determination is written down.
///
/// <para>GR2: <i>"For the purpose of showing the effect of various types of CLOSE statements as applied to
/// various storage media, all files are divided into the following categories, where the term 'file' means the
/// physical file"</i>. The four members below ARE those categories, and they are the column headings of Table 14
/// (§14.9.6.4 GR3), so a conforming implementation must place every physical file it supports in exactly one —
/// which is what makes this an enum on the connector rather than a sentence in a comment. The determination
/// used to live in four unrelated doc comments and `docs/CONFORMANCE.md` §2 contradicted them (kb/Work
/// PB235).</para>
///
/// <para>⛔ WiseOwl COBOL'S DETERMINATION IS NOT FREE — IT IS FORCED BY THE STATUS THE COMPILER ALREADY REPORTS.
/// §9.1.13.2 item 6 defines '07' as <i>"An OPEN or CLOSE statement is successfully executed but a CLOSE
/// statement with the NO REWIND, REEL/UNIT, or FOR REMOVAL phrase or an OPEN statement with the NO REWIND
/// phrase references a physical file on a non-reel/unit medium"</i> — '07' EXISTS ONLY on a non-reel/unit
/// medium. `CLOSE … WITH NO REWIND` reports '07' (Table 14 symbol g, the <see cref="NonUnit"/> column's cell
/// alone — the Sequential-single-unit cell is b,c and carries no g), and so does `CLOSE … REEL/UNIT`. Every
/// sequential file this implementation supports is therefore category (a), and that is recorded for users in
/// `docs/CONFORMANCE.md` §7's A.1 item 24 row (Annex A.1 item 24 makes GR3's symbol c a REQUIRED documented
/// implementor item).</para>
/// </summary>
public enum PhysicalFileCategory
{
    /// <summary>(a) Non-unit — <i>"A file whose input or output medium is such that the concepts of rewind and
    /// units have no meaning."</i> (§14.9.6.4 GR2 a). Every <see cref="SequentialConnector"/> file: a host path
    /// on mass storage, with no reel, unit or volume behind it.</summary>
    NonUnit,

    /// <summary>(b) Sequential single-unit — <i>"A sequential file that is entirely contained on one unit."</i>
    /// (§14.9.6.4 GR2 b). ⛔ NO SUPPORTED MEDIUM IS IN THIS CATEGORY (see the type remarks); the arm exists so
    /// that adding one is a compile-time obligation rather than a silent omission.</summary>
    SequentialSingleUnit,

    /// <summary>(c) Sequential multi-unit — <i>"A sequential file that is contained on more than one unit."</i>
    /// (§14.9.6.4 GR2 c). ⛔ NO SUPPORTED MEDIUM IS IN THIS CATEGORY.</summary>
    SequentialMultiUnit,

    /// <summary>(d) Non-sequential single/multi-unit — <i>"A file with organization other than sequential, that
    /// resides on a mass storage device."</i> (§14.9.6.4 GR2 d). Every <see cref="RelativeConnector"/> and
    /// <see cref="IndexedConnector"/> file.</summary>
    NonSequential,
}

/// <summary>The four rows of Table 14 (§14.9.6.4 GR3) — the written forms of the CLOSE statement whose effect
/// the table is indexed by. <c>WITH LOCK</c> is deliberately absent: it is not a Table 14 row, it is the plain
/// <see cref="Normal"/> form plus §14.9.6.4's reopen prohibition, and <see cref="FileRegistry.CloseWithLock"/>
/// layers it over this dispatch.</summary>
public enum CloseFormat
{
    /// <summary><c>CLOSE file-name</c>.</summary>
    Normal,
    /// <summary><c>CLOSE file-name WITH NO REWIND</c>.</summary>
    NoRewind,
    /// <summary><c>CLOSE file-name REEL</c> / <c>UNIT</c> (§14.9.6.3 SR2 makes the two words equivalent).</summary>
    Unit,
    /// <summary><c>CLOSE file-name REEL/UNIT FOR REMOVAL</c> — its own Table 14 row, which is why it is its own
    /// member: the <see cref="PhysicalFileCategory.NonUnit"/> cell happens to equal <see cref="Unit"/>'s, but
    /// the (b)/(c) cells add symbol d and folding the two forms together would lose that.</summary>
    UnitForRemoval,
}

/// <summary>The symbols Table 14's cells are written in (§14.9.6.4 GR3, <i>"The definitions of the symbols in
/// Table 14 … are given below"</i>). A cell is a SET of them, so this is a flags enum and
/// <see cref="Table14.Cell"/> returns the set the standard prints.</summary>
[Flags]
public enum CloseSymbol
{
    /// <summary>No symbol — never a Table 14 cell; the zero member a flags enum owes.</summary>
    None = 0,
    /// <summary>a) Effect on previous units — all units prior to the current one are closed.</summary>
    PreviousUnits = 1 << 0,
    /// <summary>b) No rewind of current reel — <i>"The current unit is left in its current position."</i></summary>
    NoRewindCurrentReel = 1 << 1,
    /// <summary>c) Close file — <i>"Closing operations specified by the implementor are executed."</i>
    /// (Annex A.1 item 24: those operations are a REQUIRED documented implementor item; WiseOwl COBOL's are
    /// stated in `docs/CONFORMANCE.md` §7's A.1 item 24 row.)</summary>
    CloseFile = 1 << 2,
    /// <summary>d) Unit removal — <i>"The current unit is rewound, when applicable, and the unit is logically
    /// removed from the run unit"</i>.</summary>
    UnitRemoval = 1 << 3,
    /// <summary>e) Close unit — three branches by medium; on non-unit media <i>"Execution of this statement is
    /// considered successful. The file remains in the open mode, the file position indicator is unchanged, the
    /// I-O status indicator for the file connector is set to '07', and no other action takes place."</i></summary>
    CloseUnit = 1 << 4,
    /// <summary>f) Rewind — <i>"The current reel or analogous device is positioned at its physical
    /// beginning."</i></summary>
    Rewind = 1 << 5,
    /// <summary>g) Optional phrases ignored — <i>"The CLOSE statement is executed as if none of the optional
    /// phrases were present. The I-O status indicator for the file connector is set to '07'."</i></summary>
    PhrasesIgnored = 1 << 6,
    /// <summary>Table 14's <i>'N/A'</i> — <i>"The notation 'N/A' means that the combination is not
    /// applicable."</i> Not a symbol; the cell has no effect to execute because the combination cannot be
    /// written (§14.9.6.3 SR1 restricts the phrases to sequential organization).</summary>
    NotApplicable = 1 << 7,
}

/// <summary>
/// ISO §14.9.6.4 Table 14 — <i>"The results of executing each type of CLOSE for each category of physical file
/// are summarized in Table 14, Relationship of categories of physical files and the format of the CLOSE
/// statement."</i> — as a STRUCTURE the CLOSE dispatch reads, rather than as behaviour hand-copied into one
/// method per written form (kb/Work PB235).
/// </summary>
/// <remarks>
/// ⛔ THE SWITCH IS EXHAUSTIVE ON PURPOSE. Both operands are enums with no default arm, so a new
/// <see cref="PhysicalFileCategory"/> or a new <see cref="CloseFormat"/> cannot be added without the compiler
/// naming the four cells it owes. That is the property the previous shape lacked: the (b)/(c) columns had no
/// representation at all, so the two Table 14 columns they head were unreachable AND unmentioned.
/// <c>CloseTable14Tests</c> pins the transcription cell by cell against the printed table.
/// </remarks>
public static class Table14
{
    /// <summary>The cell at (<paramref name="format"/>, <paramref name="category"/>) — exactly the symbol set
    /// the standard prints in Table 14.</summary>
    public static CloseSymbol Cell(CloseFormat format, PhysicalFileCategory category) => (format, category) switch
    {
        // Row "CLOSE":                       c | c,f | a,c,f | c
        (CloseFormat.Normal, PhysicalFileCategory.NonUnit) => CloseSymbol.CloseFile,
        (CloseFormat.Normal, PhysicalFileCategory.SequentialSingleUnit) => CloseSymbol.CloseFile | CloseSymbol.Rewind,
        (CloseFormat.Normal, PhysicalFileCategory.SequentialMultiUnit) => CloseSymbol.PreviousUnits | CloseSymbol.CloseFile | CloseSymbol.Rewind,
        (CloseFormat.Normal, PhysicalFileCategory.NonSequential) => CloseSymbol.CloseFile,

        // Row "CLOSE WITH NO REWIND":        c,g | b,c | a,b,c | N/A
        (CloseFormat.NoRewind, PhysicalFileCategory.NonUnit) => CloseSymbol.CloseFile | CloseSymbol.PhrasesIgnored,
        (CloseFormat.NoRewind, PhysicalFileCategory.SequentialSingleUnit) => CloseSymbol.NoRewindCurrentReel | CloseSymbol.CloseFile,
        (CloseFormat.NoRewind, PhysicalFileCategory.SequentialMultiUnit) => CloseSymbol.PreviousUnits | CloseSymbol.NoRewindCurrentReel | CloseSymbol.CloseFile,
        (CloseFormat.NoRewind, PhysicalFileCategory.NonSequential) => CloseSymbol.NotApplicable,

        // Row "CLOSE UNIT":                  e | e,f | e,f | N/A
        (CloseFormat.Unit, PhysicalFileCategory.NonUnit) => CloseSymbol.CloseUnit,
        (CloseFormat.Unit, PhysicalFileCategory.SequentialSingleUnit) => CloseSymbol.CloseUnit | CloseSymbol.Rewind,
        (CloseFormat.Unit, PhysicalFileCategory.SequentialMultiUnit) => CloseSymbol.CloseUnit | CloseSymbol.Rewind,
        (CloseFormat.Unit, PhysicalFileCategory.NonSequential) => CloseSymbol.NotApplicable,

        // Row "CLOSE UNIT FOR REMOVAL":      e | d,e,f | d,e,f | N/A
        (CloseFormat.UnitForRemoval, PhysicalFileCategory.NonUnit) => CloseSymbol.CloseUnit,
        (CloseFormat.UnitForRemoval, PhysicalFileCategory.SequentialSingleUnit) => CloseSymbol.UnitRemoval | CloseSymbol.CloseUnit | CloseSymbol.Rewind,
        (CloseFormat.UnitForRemoval, PhysicalFileCategory.SequentialMultiUnit) => CloseSymbol.UnitRemoval | CloseSymbol.CloseUnit | CloseSymbol.Rewind,
        (CloseFormat.UnitForRemoval, PhysicalFileCategory.NonSequential) => CloseSymbol.NotApplicable,

        // Only an out-of-range CAST reaches here — every NAMED pair above is a printed Table 14 cell. C#
        // requires this arm (CS8524) and it is the reason the exhaustiveness is ALSO pinned by a test rather
        // than by the compiler alone: `CloseTable14Tests.Cell_CoversEveryFormatByCategoryPair` enumerates both
        // enums and would fail on a new member that silently landed here.
        _ => throw new ArgumentOutOfRangeException(nameof(format),
                 $"({format}, {category}) is not a cell of ISO §14.9.6.4 Table 14"),
    };

    /// <summary>The symbols that can only be performed on a REEL/UNIT-STRUCTURED medium — a, b, d and f, every
    /// one of which manipulates a unit, a volume pointer or a reel position. §14.9.6.4 GR2 puts no supported
    /// WiseOwl COBOL medium in category (b) or (c) (see <see cref="PhysicalFileCategory"/>), so no cell this
    /// dispatch can reach contains one; <see cref="FileRegistry"/> treats their appearance as a compiler defect
    /// rather than silently performing a subset of the cell.</summary>
    public const CloseSymbol UnitStructuredOnly =
        CloseSymbol.PreviousUnits | CloseSymbol.NoRewindCurrentReel | CloseSymbol.UnitRemoval | CloseSymbol.Rewind;
}
