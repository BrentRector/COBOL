// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.IO;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ISO §14.9.27.4 <b>Table 19</b> as a checked STRUCTURE (kb/Work PB321) — the OPEN sibling of
/// <see cref="CloseTable14Tests"/>. Three things have to hold and none of them used to be expressible:
/// every one of the 35 printed cells is transcribed, the arbitration is TOTAL over all 144
/// (sharing, mode)² connector pairs, and the two prose readings of the same rule agree with the table.
///
/// <para>⛔ WHAT THE OLD SHAPE LET THROUGH. <c>FileRegistry.Conflicts</c> was a four-test predicate chain whose
/// final <c>return false;</c> carried the comment <c>// (e) ALL OTHER</c> — the letter of a §9.1.13.9 sub-case
/// that had NO implementation — and the theory guarding it had SIX rows against 35 cells, with no row whose
/// incoming mode was OUTPUT. Five combinations were wrong and every one of them let an incoming OPEN OUTPUT
/// truncate a file another connector held open. <see cref="Conflicts_MatchesTheTableForEveryConnectorPair"/>
/// is the row-count-independent replacement: it enumerates the whole 144 and cannot have a hole.</para>
/// </summary>
public sealed class OpenTable19Tests
{
    private const OpenSharingOutcome U = OpenSharingOutcome.UnsuccessfulOpen;
    private const OpenSharingOutcome K = OpenSharingOutcome.NormalOpen;

    /// <summary>The printed table, transcribed once — rows in Table 19's own order, cells left to right under
    /// <i>sharing with no other</i> · <i>read only: extend I-O output</i> · <i>read only: input</i> ·
    /// <i>all other: extend I-O output</i> · <i>all other: input</i>. Verified against the PDF page, not only
    /// against the Markdown transcription (kb/Work PB321).</summary>
    public static TheoryData<OpenRequestRow, ExistingSharingColumn, OpenSharingOutcome> PrintedCells()
    {
        var data = new TheoryData<OpenRequestRow, ExistingSharingColumn, OpenSharingOutcome>();
        var columns = new[]
        {
            ExistingSharingColumn.NoOtherAnyMode, ExistingSharingColumn.ReadOnlyNonInput,
            ExistingSharingColumn.ReadOnlyInput, ExistingSharingColumn.AllOtherNonInput,
            ExistingSharingColumn.AllOtherInput,
        };
        var rows = new (OpenRequestRow Row, OpenSharingOutcome[] Cells)[]
        {
            // | SHARING WITH NO OTHER  | EXTEND I-O INPUT OUTPUT | U | U | U | U | U |
            (OpenRequestRow.NoOtherAnyMode,   [U, U, U, U, U]),
            // | SHARING WITH READ ONLY | EXTEND I-O              | U | U | U | U | N |
            (OpenRequestRow.ReadOnlyExtendIO, [U, U, U, U, K]),
            // |                        | INPUT                   | U | U | N | U | N |
            (OpenRequestRow.ReadOnlyInput,    [U, U, K, U, K]),
            // |                        | OUTPUT                  | U | U | U | U | U |
            (OpenRequestRow.ReadOnlyOutput,   [U, U, U, U, U]),
            // | SHARING WITH ALL OTHER | EXTEND I-O              | U | U | U | N | N |
            (OpenRequestRow.AllOtherExtendIO, [U, U, U, K, K]),
            // |                        | INPUT                   | U | N | N | N | N |
            (OpenRequestRow.AllOtherInput,    [U, K, K, K, K]),
            // |                        | OUTPUT                  | U | U | U | U | U |
            (OpenRequestRow.AllOtherOutput,   [U, U, U, U, U]),
        };
        foreach (var (row, cells) in rows)
            for (int i = 0; i < columns.Length; i++)
                data.Add(row, columns[i], cells[i]);
        return data;
    }

    /// <summary>Every cell equals what Table 19 prints for it.</summary>
    [Theory]
    [MemberData(nameof(PrintedCells))]
    public void Cell_TranscribesTheStandardsTable(OpenRequestRow row, ExistingSharingColumn column,
        OpenSharingOutcome expected) => Assert.Equal(expected, Table19.Cell(row, column));

    /// <summary>The table is TOTAL over both enums, and both still have exactly the members the standard prints
    /// — seven <i>Open request</i> rows and five <i>Most restrictive existing sharing mode and open mode</i>
    /// columns. Adding a member without deriving its cells fails HERE rather than at run time, where
    /// <c>Table19.Cell</c>'s CS8524 default arm would have swallowed it.</summary>
    [Fact]
    public void Cell_CoversEveryRowByColumnPair()
    {
        var rows = Enum.GetValues<OpenRequestRow>();
        var columns = Enum.GetValues<ExistingSharingColumn>();
        Assert.Equal(7, rows.Length);
        Assert.Equal(5, columns.Length);
        int cells = 0;
        foreach (var r in rows)
            foreach (var c in columns) { _ = Table19.Cell(r, c); cells++; }
        Assert.Equal(35, cells);
        // The transcription theory must cover the same 35 pairs — a dropped cell would otherwise leave the
        // table unverified while this fact still passed.
        Assert.Equal(cells, PrintedCells().Count());
    }

    /// <summary>Every (sharing, mode) pair lands in a row and in a column, and the groupings are the printed
    /// ones. ⛔ The OUTPUT placement is the load-bearing half: on the REQUEST side OUTPUT is its own row under
    /// READ ONLY and ALL OTHER, while on the EXISTING side it groups with <c>extend I-O</c>. Getting that
    /// backwards is exactly the reading §9.1.13.9's sub-cases (c) and (d) invite.</summary>
    [Fact]
    public void RowAndColumn_GroupTheModesTheTablePrints()
    {
        foreach (var mode in Enum.GetValues<FileOpenMode>())
        {
            Assert.Equal(OpenRequestRow.NoOtherAnyMode, Table19.Row(FileSharing.NoOther, mode));
            Assert.Equal(ExistingSharingColumn.NoOtherAnyMode, Table19.Column(FileSharing.NoOther, mode));
            foreach (var sharing in new[] { FileSharing.ReadOnly, FileSharing.AllOther })
            {
                bool readOnly = sharing == FileSharing.ReadOnly;
                Assert.Equal(
                    mode == FileOpenMode.Input
                        ? (readOnly ? ExistingSharingColumn.ReadOnlyInput : ExistingSharingColumn.AllOtherInput)
                        : (readOnly ? ExistingSharingColumn.ReadOnlyNonInput : ExistingSharingColumn.AllOtherNonInput),
                    Table19.Column(sharing, mode));
                Assert.Equal(
                    mode switch
                    {
                        FileOpenMode.Input => readOnly ? OpenRequestRow.ReadOnlyInput : OpenRequestRow.AllOtherInput,
                        FileOpenMode.Output => readOnly ? OpenRequestRow.ReadOnlyOutput : OpenRequestRow.AllOtherOutput,
                        _ => readOnly ? OpenRequestRow.ReadOnlyExtendIO : OpenRequestRow.AllOtherExtendIO,
                    },
                    Table19.Row(sharing, mode));
            }
        }
    }

    /// <summary>⛔ THE 144-COMBINATION DRIFT TEST. <see cref="Table19.Conflicts"/> must answer "conflict"
    /// for exactly the (existing, incoming) connector pairs whose Table 19 cell is <i>Unsuccessful open</i> —
    /// every one of the 12 × 12 pairs, so a missing arm cannot be green for want of a row.</summary>
    [Fact]
    public void Conflicts_MatchesTheTableForEveryConnectorPair()
    {
        foreach (var (exS, exM) in EveryConnectorState())
            foreach (var (incS, incM) in EveryConnectorState())
            {
                bool expected = Table19.Cell(incS, incM, exS, exM) == U;
                Assert.Equal(expected, Table19.Conflicts((incS, incM), (exS, exM)));
            }
    }

    /// <summary>⛔ THE OTHER READING OF THE SAME RULE, pinned against the table. §9.1.15's three sharing-mode
    /// paragraphs plus §9.1.13.9 1) e) are the PROSE form of Table 19, and they reproduce all 144 cells exactly.
    /// The five §9.1.13.9 sub-cases read literally do NOT: (c) and (d) say <i>"I-O or extend"</i> where the
    /// table's existing-side column groups are <c>extend I-O output</c>, so they under-enumerate the table by
    /// four combinations. This test is what keeps the code on the reading that agrees with the printed table,
    /// and it FAILS if the transcription above is ever edited to the sub-cases' shape.</summary>
    [Fact]
    public void NineOneFifteenRules_ReproduceTheWholeTable()
    {
        int subcaseDisagreements = 0;
        foreach (var (exS, exM) in EveryConnectorState())
            foreach (var (incS, incM) in EveryConnectorState())
            {
                bool table = Table19.Cell(incS, incM, exS, exM) == U;

                // §9.1.15 1) "Associating this file connector with the physical file will be unsuccessful if the
                //             physical file is currently open through other file connectors." / "… subsequent
                //             requests to open the physical file through other file connectors … unsuccessful."
                // §9.1.15 2) "… unsuccessful if the physical file is associated with another file connector
                //             whose open mode is other than input." / "… subsequent requests … in a mode other
                //             than input … will be unsuccessful."
                // §9.1.13.9 1) e) "An attempt is made to open a physical file in the output mode and the
                //             physical file is currently open by another file connector."
                bool prose = exS == FileSharing.NoOther
                          || incS == FileSharing.NoOther
                          || (exS == FileSharing.ReadOnly && incM != FileOpenMode.Input)
                          || (incS == FileSharing.ReadOnly && exM != FileOpenMode.Input)
                          || incM == FileOpenMode.Output;
                Assert.True(table == prose,
                    $"§9.1.15 disagrees with Table 19 at existing=({exS},{exM}) incoming=({incS},{incM})");

                // The five §9.1.13.9 sub-cases read LITERALLY — (c)/(d) restricted to "I-O or extend".
                bool subcases = exS == FileSharing.NoOther
                             || incS == FileSharing.NoOther
                             || (incM is FileOpenMode.IO or FileOpenMode.Extend && exS == FileSharing.ReadOnly)
                             || (incS == FileSharing.ReadOnly && exM is FileOpenMode.IO or FileOpenMode.Extend)
                             || incM == FileOpenMode.Output;
                if (subcases != table) subcaseDisagreements++;
            }
        // Exactly four, all of them an EXISTING connector open in the OUTPUT mode against an incoming SHARING
        // WITH READ ONLY request. Asserted rather than described so the finding cannot rot into prose.
        Assert.Equal(4, subcaseDisagreements);
    }

    /// <summary>⛔ THE IMPLEMENTOR DEFAULT (Annex A.1 items 77 and 131, kb/Work PB322). A connector that writes
    /// neither a SHARING clause nor an OPEN SHARING phrase is given one of §9.1.15's own modes by its open mode:
    /// INPUT establishes SHARING WITH READ ONLY and OUTPUT, I-O and EXTEND establish SHARING WITH NO OTHER
    /// (<see cref="FileRegistry.ImplementorDefaultSharing"/>). Two facts follow and both are asserted, because
    /// they are what <c>docs/CONFORMANCE.md</c> DOC-A.1-131 promises a user:
    /// <list type="number">
    /// <item>Two clause-less connectors on one physical file can be open together only when both are INPUT.</item>
    /// <item>A clause-less connector in any mode other than INPUT is refused by, and refuses, every other
    /// connector — whatever mode or sharing the other one carries.</item>
    /// </list></summary>
    [Fact]
    public void ImplementorDefault_IsAFunctionOfTheOpenMode()
    {
        foreach (var mode in Enum.GetValues<FileOpenMode>())
            Assert.Equal(mode == FileOpenMode.Input ? FileSharing.ReadOnly : FileSharing.NoOther,
                         FileRegistry.ImplementorDefaultSharing(mode));

        foreach (var exM in Enum.GetValues<FileOpenMode>())
            foreach (var incM in Enum.GetValues<FileOpenMode>())
                Assert.Equal(!(exM == FileOpenMode.Input && incM == FileOpenMode.Input),
                    Table19.Conflicts((FileRegistry.ImplementorDefaultSharing(incM), incM),
                                      (FileRegistry.ImplementorDefaultSharing(exM), exM)));

        foreach (var (otherS, otherM) in EveryConnectorState())
            foreach (var mode in Enum.GetValues<FileOpenMode>().Where(m => m != FileOpenMode.Input))
            {
                var dflt = (FileRegistry.ImplementorDefaultSharing(mode), mode);
                Assert.True(Table19.Conflicts(dflt, (otherS, otherM)));   // refused by every existing connector
                Assert.True(Table19.Conflicts((otherS, otherM), dflt));   // and refuses every incoming one
            }
    }

    /// <summary>The 12 states a file connector can be open in — the three §9.1.15 sharing modes crossed with the
    /// four open modes. Table 19 collapses them into 7 rows and 5 columns; the arbitration must be right for all
    /// 12, which is why the drift tests enumerate these and not the printed groups.</summary>
    private static IEnumerable<(FileSharing Sharing, FileOpenMode Mode)> EveryConnectorState()
    {
        foreach (var s in Table19.StandardModes)
            foreach (var m in Enum.GetValues<FileOpenMode>())
                yield return (s, m);
    }
}
