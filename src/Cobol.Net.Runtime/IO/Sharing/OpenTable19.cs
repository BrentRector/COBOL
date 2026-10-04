// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>The two outcomes Table 19 (§14.9.27.4) prints in its cells. A cell is a single value, not a set —
/// unlike Table 14's symbol sets — so this is a plain enum.</summary>
public enum OpenSharingOutcome
{
    /// <summary><i>"Normal open"</i> — the request is allowed against the connectors already open.</summary>
    NormalOpen,
    /// <summary><i>"Unsuccessful open"</i> — the file sharing conflict condition of §9.1.13.9 item 1 exists, so
    /// the I-O status is '61' and §14.9.27.4 GR25 leaves the file <i>"not affected"</i>.</summary>
    UnsuccessfulOpen,
}

/// <summary>The SEVEN printed rows of Table 19 — the <i>"Open request"</i> stub. A row is a sharing mode crossed
/// with a SET of open modes, exactly as the table prints them, which is why the members are named for the cell
/// group rather than for one mode: the standard collapses EXTEND and I-O everywhere, and collapses all four
/// modes under SHARING WITH NO OTHER.</summary>
public enum OpenRequestRow
{
    /// <summary>SHARING WITH NO OTHER × EXTEND / I-O / INPUT / OUTPUT.</summary>
    NoOtherAnyMode,
    /// <summary>SHARING WITH READ ONLY × EXTEND / I-O.</summary>
    ReadOnlyExtendIO,
    /// <summary>SHARING WITH READ ONLY × INPUT.</summary>
    ReadOnlyInput,
    /// <summary>SHARING WITH READ ONLY × OUTPUT.</summary>
    ReadOnlyOutput,
    /// <summary>SHARING WITH ALL OTHER × EXTEND / I-O.</summary>
    AllOtherExtendIO,
    /// <summary>SHARING WITH ALL OTHER × INPUT.</summary>
    AllOtherInput,
    /// <summary>SHARING WITH ALL OTHER × OUTPUT.</summary>
    AllOtherOutput,
}

/// <summary>The FIVE printed columns of Table 19 — <i>"Most restrictive existing sharing mode and open mode"</i>.
/// ⛔ Note where OUTPUT sits: the existing side groups <c>extend I-O output</c> together under both READ ONLY and
/// ALL OTHER, so an existing connector open in the OUTPUT mode is arbitrated exactly like one open in I-O or
/// EXTEND. §9.1.13.9's sub-cases (c) and (d) name only <i>"I-O or extend"</i> and so under-enumerate the printed
/// table by four combinations; §9.1.15 rules 1)–3), which say <i>"other than input"</i>, agree with the table
/// exactly. See <see cref="Table19"/>.</summary>
public enum ExistingSharingColumn
{
    /// <summary>sharing with no other × extend / I-O / input / output.</summary>
    NoOtherAnyMode,
    /// <summary>sharing with read only × extend / I-O / output.</summary>
    ReadOnlyNonInput,
    /// <summary>sharing with read only × input.</summary>
    ReadOnlyInput,
    /// <summary>sharing with all other × extend / I-O / output.</summary>
    AllOtherNonInput,
    /// <summary>sharing with all other × input.</summary>
    AllOtherInput,
}

/// <summary>
/// ISO §14.9.27.4 Table 19 — <i>"Table 19, Opening available shared files that are currently open by another file
/// connector, shows the results of opening available files that are currently open by another file connector,
/// including those implicitly opened by the SORT and MERGE statements."</i> — as a STRUCTURE the OPEN arbitration
/// reads, rather than as a hand-written predicate chain (kb/Work PB321; the sibling of <see cref="Table14"/>,
/// which did the same for CLOSE under kb/Work PB235).
/// </summary>
/// <remarks>
/// <para>⛔ WHY THE TABLE AND NOT THE FIVE SUB-CASES. §9.1.13.9 item 1 enumerates five <i>"possible violations"</i>
/// (a)–(e) that give I-O status '61'. Sub-cases (c) and (d) are written over <i>"I-O or extend"</i>, but Table 19's
/// existing-side column groups are <c>extend I-O output</c> — so a literal reading of the five sub-cases answers
/// <i>Normal open</i> in FOUR combinations the printed table marks <i>Unsuccessful open</i> (incoming SHARING WITH
/// READ ONLY in the EXTEND/I-O or INPUT mode against an existing connector open in the OUTPUT mode). §9.1.15's
/// rules resolve the disagreement in the table's favour in so many words — rule 2) is <i>"unsuccessful if the
/// physical file is associated with another file connector whose open mode is other than input"</i> and
/// <i>"subsequent requests to open the physical file through other file connectors in a mode other than input …
/// will be unsuccessful"</i> — and OUTPUT is a mode other than input. The transcription below is therefore the
/// arbiter, and <c>OpenTable19Tests</c> pins BOTH readings against it: the 35 cells cell-by-cell, and the
/// §9.1.15 rule-set over all 144 (sharing, mode)² combinations.</para>
/// <para>⛔ THE SWITCH IS EXHAUSTIVE ON PURPOSE. Both operands are enums with no default arm, so a new row or a
/// new column cannot be added without the compiler naming the cells it owes. That is the property the previous
/// shape lacked: the predicate chain's final <c>return false;</c> carried the comment <c>// (e) ALL OTHER</c> —
/// the letter of the one sub-case it had NEVER implemented, attached to the fall-through that swallowed it, so
/// every incoming OPEN OUTPUT was allowed to truncate a file another connector held open (kb/Work PB321).</para>
/// <para>The <i>"most restrictive"</i> in the column heading is realized by the caller, not here: the arbitration
/// tests the incoming request against EVERY connector currently open on the physical file and is unsuccessful if
/// any one of them yields <see cref="OpenSharingOutcome.UnsuccessfulOpen"/>, which is the same answer as looking
/// up the single most restrictive one (a request the most restrictive connector permits is permitted by every
/// less restrictive one).</para>
/// </remarks>
public static class Table19
{
    /// <summary>The printed row an OPEN request falls in (§14.9.27.4 Table 19, the <i>"Open request"</i> stub).
    /// Total by construction: both operands are enums and every pair is named.</summary>
    public static OpenRequestRow Row(FileSharing sharing, FileOpenMode mode) => (sharing, mode) switch
    {
        (FileSharing.NoOther, _) => OpenRequestRow.NoOtherAnyMode,
        (FileSharing.ReadOnly, FileOpenMode.Extend or FileOpenMode.IO) => OpenRequestRow.ReadOnlyExtendIO,
        (FileSharing.ReadOnly, FileOpenMode.Input) => OpenRequestRow.ReadOnlyInput,
        (FileSharing.ReadOnly, FileOpenMode.Output) => OpenRequestRow.ReadOnlyOutput,
        (FileSharing.AllOther, FileOpenMode.Extend or FileOpenMode.IO) => OpenRequestRow.AllOtherExtendIO,
        (FileSharing.AllOther, FileOpenMode.Input) => OpenRequestRow.AllOtherInput,
        (FileSharing.AllOther, FileOpenMode.Output) => OpenRequestRow.AllOtherOutput,
        _ => throw new ArgumentOutOfRangeException(nameof(sharing),
                 $"({sharing}, {mode}) is not an ISO §14.9.27.4 Table 19 open-request row"),
    };

    /// <summary>The printed column an already-open connector falls in (Table 19's <i>"Most restrictive existing
    /// sharing mode and open mode"</i> heading). ⛔ OUTPUT groups with EXTEND and I-O here — see
    /// <see cref="ExistingSharingColumn"/>.</summary>
    public static ExistingSharingColumn Column(FileSharing sharing, FileOpenMode mode) => (sharing, mode) switch
    {
        (FileSharing.NoOther, _) => ExistingSharingColumn.NoOtherAnyMode,
        (FileSharing.ReadOnly, FileOpenMode.Input) => ExistingSharingColumn.ReadOnlyInput,
        (FileSharing.ReadOnly, _) => ExistingSharingColumn.ReadOnlyNonInput,
        (FileSharing.AllOther, FileOpenMode.Input) => ExistingSharingColumn.AllOtherInput,
        (FileSharing.AllOther, _) => ExistingSharingColumn.AllOtherNonInput,
        _ => throw new ArgumentOutOfRangeException(nameof(sharing),
                 $"({sharing}, {mode}) is not an ISO §14.9.27.4 Table 19 existing-connector column"),
    };

    /// <summary>The cell at (<paramref name="row"/>, <paramref name="column"/>) — exactly the outcome the
    /// standard prints in Table 19.</summary>
    public static OpenSharingOutcome Cell(OpenRequestRow row, ExistingSharingColumn column) => (row, column) switch
    {
        //                                                     no other | read only        | all other
        //                                                              | non-in | input   | non-in | input
        // Row "SHARING WITH NO OTHER / EXTEND I-O INPUT OUTPUT":  U     |  U     |  U      |  U     |  U
        (OpenRequestRow.NoOtherAnyMode, ExistingSharingColumn.NoOtherAnyMode) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.NoOtherAnyMode, ExistingSharingColumn.ReadOnlyNonInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.NoOtherAnyMode, ExistingSharingColumn.ReadOnlyInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.NoOtherAnyMode, ExistingSharingColumn.AllOtherNonInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.NoOtherAnyMode, ExistingSharingColumn.AllOtherInput) => OpenSharingOutcome.UnsuccessfulOpen,

        // Row "SHARING WITH READ ONLY / EXTEND I-O":              U     |  U     |  U      |  U     |  Normal
        (OpenRequestRow.ReadOnlyExtendIO, ExistingSharingColumn.NoOtherAnyMode) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.ReadOnlyExtendIO, ExistingSharingColumn.ReadOnlyNonInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.ReadOnlyExtendIO, ExistingSharingColumn.ReadOnlyInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.ReadOnlyExtendIO, ExistingSharingColumn.AllOtherNonInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.ReadOnlyExtendIO, ExistingSharingColumn.AllOtherInput) => OpenSharingOutcome.NormalOpen,

        // Row "SHARING WITH READ ONLY / INPUT":                   U     |  U     |  Normal |  U     |  Normal
        (OpenRequestRow.ReadOnlyInput, ExistingSharingColumn.NoOtherAnyMode) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.ReadOnlyInput, ExistingSharingColumn.ReadOnlyNonInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.ReadOnlyInput, ExistingSharingColumn.ReadOnlyInput) => OpenSharingOutcome.NormalOpen,
        (OpenRequestRow.ReadOnlyInput, ExistingSharingColumn.AllOtherNonInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.ReadOnlyInput, ExistingSharingColumn.AllOtherInput) => OpenSharingOutcome.NormalOpen,

        // Row "SHARING WITH READ ONLY / OUTPUT":                  U     |  U     |  U      |  U     |  U
        (OpenRequestRow.ReadOnlyOutput, ExistingSharingColumn.NoOtherAnyMode) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.ReadOnlyOutput, ExistingSharingColumn.ReadOnlyNonInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.ReadOnlyOutput, ExistingSharingColumn.ReadOnlyInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.ReadOnlyOutput, ExistingSharingColumn.AllOtherNonInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.ReadOnlyOutput, ExistingSharingColumn.AllOtherInput) => OpenSharingOutcome.UnsuccessfulOpen,

        // Row "SHARING WITH ALL OTHER / EXTEND I-O":              U     |  U     |  U      |  Normal|  Normal
        (OpenRequestRow.AllOtherExtendIO, ExistingSharingColumn.NoOtherAnyMode) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.AllOtherExtendIO, ExistingSharingColumn.ReadOnlyNonInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.AllOtherExtendIO, ExistingSharingColumn.ReadOnlyInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.AllOtherExtendIO, ExistingSharingColumn.AllOtherNonInput) => OpenSharingOutcome.NormalOpen,
        (OpenRequestRow.AllOtherExtendIO, ExistingSharingColumn.AllOtherInput) => OpenSharingOutcome.NormalOpen,

        // Row "SHARING WITH ALL OTHER / INPUT":                   U     |  Normal|  Normal |  Normal|  Normal
        (OpenRequestRow.AllOtherInput, ExistingSharingColumn.NoOtherAnyMode) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.AllOtherInput, ExistingSharingColumn.ReadOnlyNonInput) => OpenSharingOutcome.NormalOpen,
        (OpenRequestRow.AllOtherInput, ExistingSharingColumn.ReadOnlyInput) => OpenSharingOutcome.NormalOpen,
        (OpenRequestRow.AllOtherInput, ExistingSharingColumn.AllOtherNonInput) => OpenSharingOutcome.NormalOpen,
        (OpenRequestRow.AllOtherInput, ExistingSharingColumn.AllOtherInput) => OpenSharingOutcome.NormalOpen,

        // Row "SHARING WITH ALL OTHER / OUTPUT":                  U     |  U     |  U      |  U     |  U
        // (§9.1.13.9 1) e): "An attempt is made to open a physical file in the output mode and the physical file
        // is currently open by another file connector" — the sub-case that had no implementation at all.)
        (OpenRequestRow.AllOtherOutput, ExistingSharingColumn.NoOtherAnyMode) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.AllOtherOutput, ExistingSharingColumn.ReadOnlyNonInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.AllOtherOutput, ExistingSharingColumn.ReadOnlyInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.AllOtherOutput, ExistingSharingColumn.AllOtherNonInput) => OpenSharingOutcome.UnsuccessfulOpen,
        (OpenRequestRow.AllOtherOutput, ExistingSharingColumn.AllOtherInput) => OpenSharingOutcome.UnsuccessfulOpen,

        // Only an out-of-range CAST reaches here — every NAMED pair above is a printed Table 19 cell. C# requires
        // this arm (CS8524); `OpenTable19Tests.Cell_CoversEveryRowByColumnPair` enumerates both enums so a new
        // member that silently landed here fails instead.
        _ => throw new ArgumentOutOfRangeException(nameof(row),
                 $"({row}, {column}) is not a cell of ISO §14.9.27.4 Table 19"),
    };

    /// <summary>The Table 19 outcome of opening with (<paramref name="requestSharing"/>,
    /// <paramref name="requestMode"/>) against a connector already open with
    /// (<paramref name="existingSharing"/>, <paramref name="existingMode"/>).</summary>
    public static OpenSharingOutcome Cell(FileSharing requestSharing, FileOpenMode requestMode,
                                          FileSharing existingSharing, FileOpenMode existingMode) =>
        Cell(Row(requestSharing, requestMode), Column(existingSharing, existingMode));

    /// <summary>⛔ THE ONE ARBITRATION QUESTION: is an OPEN request unsuccessful (§9.1.13.9 item 1, I-O status
    /// '61') against ONE connector already open on the same physical file? A plain lookup of
    /// <see cref="Cell(FileSharing, FileOpenMode, FileSharing, FileOpenMode)"/>: every connector, declared or
    /// defaulted, carries one of §9.1.15's three modes (<see cref="FileRegistry.ImplementorDefaultSharing"/> gives
    /// the clause-less ones theirs from the open mode), so there is no undetermined side to quantify over
    /// (kb/Work PB322). §9.1.13.9 item 1 e) — <i>"An attempt is made to open a physical file in the output mode
    /// and the physical file is currently open by another file connector"</i> — is the Output rows of the table.</summary>
    public static bool Conflicts((FileSharing Sharing, FileOpenMode Mode) request,
                                 (FileSharing Sharing, FileOpenMode Mode) existing) =>
        Cell(request.Sharing, request.Mode, existing.Sharing, existing.Mode) == OpenSharingOutcome.UnsuccessfulOpen;

    /// <summary>The three sharing modes §9.1.15 specifies, in the order the standard lists them — the universe
    /// the table and its drift tests enumerate.</summary>
    public static readonly FileSharing[] StandardModes =
        [FileSharing.NoOther, FileSharing.ReadOnly, FileSharing.AllOther];
}
