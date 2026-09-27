// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ISO §14.9.6.4 GR2 and GR3 as a checked STRUCTURE (kb/Work PB235). GR2 partitions every physical file into
/// four categories and GR3's Table 14 is indexed by them, so two things have to hold and neither used to be
/// expressible: every supported connector kind is placed in exactly one category, and every (format, category)
/// pair names the symbol set the standard prints.
///
/// <para>⛔ THIS IS THE DRIFT TEST THE C# COMPILER CANNOT BE. <c>Table14.Cell</c> needs a default arm for
/// out-of-range enum casts (CS8524), and that arm would silently swallow a NEWLY ADDED
/// <see cref="CloseFormat"/> or <see cref="PhysicalFileCategory"/> member as a runtime throw instead of a build
/// error. <see cref="Cell_CoversEveryFormatByCategoryPair"/> enumerates both enums and asserts their member
/// counts, so a new member fails HERE, at the point where its four (or five) missing cells have to be
/// derived from the printed table.</para>
/// </summary>
public sealed class CloseTable14Tests
{
    /// <summary>The printed table, transcribed once — rows in Table 14's own order, cells left to right under
    /// Non-unit · Sequential single-unit · Sequential multi-unit · Non-sequential single/multi-unit.</summary>
    public static TheoryData<CloseFormat, PhysicalFileCategory, CloseSymbol> PrintedCells() => new()
    {
        // | CLOSE                  | c     | c,f   | a,c,f   | c   |
        { CloseFormat.Normal, PhysicalFileCategory.NonUnit, CloseSymbol.CloseFile },
        { CloseFormat.Normal, PhysicalFileCategory.SequentialSingleUnit, CloseSymbol.CloseFile | CloseSymbol.Rewind },
        { CloseFormat.Normal, PhysicalFileCategory.SequentialMultiUnit, CloseSymbol.PreviousUnits | CloseSymbol.CloseFile | CloseSymbol.Rewind },
        { CloseFormat.Normal, PhysicalFileCategory.NonSequential, CloseSymbol.CloseFile },
        // | CLOSE WITH NO REWIND   | c,g   | b,c   | a,b,c   | N/A |
        { CloseFormat.NoRewind, PhysicalFileCategory.NonUnit, CloseSymbol.CloseFile | CloseSymbol.PhrasesIgnored },
        { CloseFormat.NoRewind, PhysicalFileCategory.SequentialSingleUnit, CloseSymbol.NoRewindCurrentReel | CloseSymbol.CloseFile },
        { CloseFormat.NoRewind, PhysicalFileCategory.SequentialMultiUnit, CloseSymbol.PreviousUnits | CloseSymbol.NoRewindCurrentReel | CloseSymbol.CloseFile },
        { CloseFormat.NoRewind, PhysicalFileCategory.NonSequential, CloseSymbol.NotApplicable },
        // | CLOSE UNIT             | e     | e,f   | e,f     | N/A |
        { CloseFormat.Unit, PhysicalFileCategory.NonUnit, CloseSymbol.CloseUnit },
        { CloseFormat.Unit, PhysicalFileCategory.SequentialSingleUnit, CloseSymbol.CloseUnit | CloseSymbol.Rewind },
        { CloseFormat.Unit, PhysicalFileCategory.SequentialMultiUnit, CloseSymbol.CloseUnit | CloseSymbol.Rewind },
        { CloseFormat.Unit, PhysicalFileCategory.NonSequential, CloseSymbol.NotApplicable },
        // | CLOSE UNIT FOR REMOVAL | e     | d,e,f | d,e,f   | N/A |
        { CloseFormat.UnitForRemoval, PhysicalFileCategory.NonUnit, CloseSymbol.CloseUnit },
        { CloseFormat.UnitForRemoval, PhysicalFileCategory.SequentialSingleUnit, CloseSymbol.UnitRemoval | CloseSymbol.CloseUnit | CloseSymbol.Rewind },
        { CloseFormat.UnitForRemoval, PhysicalFileCategory.SequentialMultiUnit, CloseSymbol.UnitRemoval | CloseSymbol.CloseUnit | CloseSymbol.Rewind },
        { CloseFormat.UnitForRemoval, PhysicalFileCategory.NonSequential, CloseSymbol.NotApplicable },
    };

    /// <summary>Every cell equals what Table 14 prints for it.</summary>
    [Theory]
    [MemberData(nameof(PrintedCells))]
    public void Cell_TranscribesTheStandardsTable(CloseFormat format, PhysicalFileCategory category, CloseSymbol expected) =>
        Assert.Equal(expected, Table14.Cell(format, category));

    /// <summary>The table is TOTAL over both enums, and both enums still have exactly the members the standard
    /// names — four written CLOSE forms (§14.9.6.4 GR3's four rows) and four categories (GR2 a–d). Adding a
    /// member without deriving its cells fails here rather than at run time.</summary>
    [Fact]
    public void Cell_CoversEveryFormatByCategoryPair()
    {
        var formats = Enum.GetValues<CloseFormat>();
        var categories = Enum.GetValues<PhysicalFileCategory>();
        Assert.Equal(4, formats.Length);       // Table 14's four rows
        Assert.Equal(4, categories.Length);    // §14.9.6.4 GR2 a) b) c) d)
        int cells = 0;
        foreach (var f in formats)
            foreach (var c in categories)
            {
                var cell = Table14.Cell(f, c);
                Assert.True(cell != CloseSymbol.None, $"Table 14 cell ({f}, {c}) is empty — every printed cell is a symbol set or N/A");
                cells++;
            }
        Assert.Equal(16, cells);
        // The transcription theory must cover the same 16 pairs — a cell dropped from PrintedCells would
        // otherwise leave the table unverified while this fact still passed.
        Assert.Equal(cells, PrintedCells().Count());
    }

    /// <summary>N/A is exactly the non-sequential × phrase combinations — §14.9.6.3 SR1, <i>"The NO REWIND,
    /// REEL, and UNIT phrases may be used only with files that are of sequential organization"</i>. A plain
    /// CLOSE is applicable in every column.</summary>
    [Fact]
    public void NotApplicable_IsExactlyThePhraseOnANonSequentialFile()
    {
        foreach (var f in Enum.GetValues<CloseFormat>())
            foreach (var c in Enum.GetValues<PhysicalFileCategory>())
            {
                bool na = Table14.Cell(f, c).HasFlag(CloseSymbol.NotApplicable);
                Assert.Equal(c == PhysicalFileCategory.NonSequential && f != CloseFormat.Normal, na);
            }
    }

    /// <summary>⛔ WiseOwl COBOL'S MEDIUM DETERMINATION, proved through the behaviour Table 14 assigns rather than
    /// through a getter. §14.9.6.4 GR2 requires every supported physical file to be in exactly one category;
    /// the sequential connector is (a) Non-unit and the two keyed connectors are (d), and NOTHING is (b) or
    /// (c) — which is what makes Table 14's two unit columns vacuous rather than unimplemented (documented at
    /// docs/CONFORMANCE.md §7, A.1 item 24).
    ///
    /// <para>Why the CLOSE UNIT observation PLACES the sequential file: the Non-unit cell is symbol e alone,
    /// so the statement succeeds, the file stays OPEN and the status is '07'. Category (b) or (c) would make
    /// the cell e,f, and symbol f (rewind the reel) needs a reel/unit-structured medium the dispatch refuses —
    /// so '07'-and-still-open is reachable from exactly one column. The keyed connectors take the N/A column:
    /// §14.9.6.3 SR1 rejects the phrase at bind time, and the runtime arm behind it is loud.</para></summary>
    [Fact]
    public void Category_PlacesEverySupportedConnectorKind()
    {
        var reg = new FileRegistry();
        string dir = Path.Combine(Path.GetTempPath(), $"pb235-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            reg.Register("S", Path.Combine(dir, "s.dat"), 8, lineSequential: false, optional: false, -1, -1);
            reg.OpenStatic("S", FileOpenMode.Output);

            reg.CloseReelUnit("S");                     // Non-unit × CLOSE UNIT = e
            Assert.Equal(FileStatusCode.PhraseOnNonReelMedium, reg.Status("S"));
            reg.CloseReelUnitForRemoval("S");           // Non-unit × CLOSE UNIT FOR REMOVAL = e (still open)
            Assert.Equal(FileStatusCode.PhraseOnNonReelMedium, reg.Status("S"));
            reg.CloseNoRewind("S");                     // Non-unit × CLOSE WITH NO REWIND = c,g — closed, '07'
            Assert.Equal(FileStatusCode.PhraseOnNonReelMedium, reg.Status("S"));
            reg.Close("S");                             // §14.9.6.4 GR1 — no longer open
            Assert.Equal(FileStatusCode.FileNotOpen, reg.Status("S"));

            // (d) Non-sequential: every phrase cell is N/A.
            reg.RegisterRelative("R", Path.Combine(dir, "r.dat"), 8, false, 0, 4, -1, -1);
            reg.RegisterIndexed("I", Path.Combine(dir, "i.dat"), 8, false, 0, 0, 4, -1, -1);
            Assert.Throws<InvalidOperationException>(() => reg.CloseReelUnit("R"));
            Assert.Throws<InvalidOperationException>(() => reg.CloseReelUnitForRemoval("R"));
            Assert.Throws<InvalidOperationException>(() => reg.CloseNoRewind("I"));
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { } }
    }

    /// <summary>The SAME medium determination read from the OPEN side (kb/Work PB317). §14.9.27.4 GR11 —
    /// <i>"The NO REWIND phrase will be ignored if it does not apply to the storage medium on which the file
    /// resides. If the NO REWIND phrase is ignored, the OPEN statement is successful and the I-O status
    /// associated with file-name-1 is set to '07'."</i> — and §14.9.27.4 GR12, its complement for a medium that
    /// permits rewinding, partition the media between them, so the '07' the CLOSE arm reports and the '07' the
    /// OPEN arm reports are the SAME determination and cannot be allowed to drift apart.
    ///
    /// <para>⛔ THIS IS THE DRIFT TEST FOR GR12'S VACUITY. GR12 is unreachable only while every connector a
    /// sequential OPEN can name answers (a) <see cref="PhysicalFileCategory.NonUnit"/>. The moment one answers
    /// (b) or (c) — a medium that DOES permit rewinding — the first assertion below throws instead of reporting
    /// '07', because <c>FileRegistry.NoRewindPhraseEffect</c> refuses a category it has no rule for; the row
    /// must then be re-adjudicated rather than inherit its closure, exactly as
    /// <c>DRV-GR-14.9.6.4-L2.1</c> says for the CLOSE twin.</para></summary>
    [Fact]
    public void OpenNoRewind_IsTheCloseArmsTwinOnTheSameMedium()
    {
        var reg = new FileRegistry();
        string dir = Path.Combine(Path.GetTempPath(), $"pb317-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            reg.Register("S", Path.Combine(dir, "s.dat"), 8, lineSequential: false, optional: false, -1, -1);

            // (a) Non-unit × OPEN … WITH NO REWIND = §14.9.27.4 GR11: successful, and '07' (§9.1.13.2 item 6).
            reg.OpenNoRewindStatic("S", FileOpenMode.Output);
            Assert.Equal(FileStatusCode.PhraseOnNonReelMedium, reg.Status("S"));

            // §14.9.27.4 GR25 a) — an UNSUCCESSFUL open keeps the value naming why it failed; GR11's warning is
            // an overlay on a successful open, never a replacement for a diagnosis. Here the connector is
            // already open, so GR2 owes '41' and the phrase must not overwrite it.
            reg.OpenNoRewindStatic("S", FileOpenMode.Input);
            Assert.Equal(FileStatusCode.FileAlreadyOpen, reg.Status("S"));
            reg.Close("S");

            // (d) Non-sequential: §14.9.27.3 SR5 rejects the phrase at bind time (COBOLNET1802), so the runtime
            // arm behind it is loud — the OPEN twin of the CLOSE N/A cells asserted above.
            reg.RegisterRelative("R", Path.Combine(dir, "r.dat"), 8, false, 0, 4, -1, -1);
            Assert.Throws<InvalidOperationException>(() => reg.OpenNoRewindStatic("R", FileOpenMode.Input));
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { } }
    }

    /// <summary>The determination's consequence, stated as the invariant the dispatch relies on: no category a
    /// connector can report carries a symbol that needs a reel/unit-structured medium (a, b, d or f), so
    /// <c>FileRegistry.CloseByFormat</c>'s loud arm for those is genuinely unreachable today.</summary>
    [Fact]
    public void ReachableCategories_NeedNoUnitStructuredSymbol()
    {
        PhysicalFileCategory[] reachable = [PhysicalFileCategory.NonUnit, PhysicalFileCategory.NonSequential];
        foreach (var f in Enum.GetValues<CloseFormat>())
            foreach (var c in reachable)
                Assert.Equal(CloseSymbol.None, Table14.Cell(f, c) & Table14.UnitStructuredOnly);
    }
}
