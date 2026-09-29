// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;

using CobolNet.Binding.Model;

using Core = CobolNet.Frontend.Generated.CobolParserCore;

namespace CobolNet.Binding;

/// <summary>The three general formats of the file description entry (ISO/IEC 1989:2023 §13.4.5.2, rendered from
/// the printed pages, folios 342-343) as a SET, so one table row can name every format that contains a clause.
/// §13.4.5.3 binds each entry to exactly one of them: SR5 "Format 1 is the file description entry for a sequential
/// file", SR7 "Format 2 is the file description entry for a relative file or an indexed file", SR8 "Format 3 is the
/// file description entry for a report file".</summary>
[Flags]
internal enum FileDescriptionFormat
{
    None = 0,

    /// <summary>Format 1 (sequential) — record sequential or line sequential organization, no REPORT clause.</summary>
    Sequential = 1,

    /// <summary>Format 2 (relative-or-indexed).</summary>
    RelativeOrIndexed = 2,

    /// <summary>Format 3 (report) — §9.1.22: "A report file is an output file having sequential organization whose
    /// file description entry contains a REPORT clause."</summary>
    Report = 4,

    All = Sequential | RelativeOrIndexed | Report,
}

/// <summary>One clause alternative of <c>fileDescriptionClause</c>: its parse-tree context, its name as a diagnostic
/// says it, the formats that contain it, and — for a clause that belongs to ANOTHER entry — which one.</summary>
/// <param name="Context">The generated context type of the alternative (the table's key).</param>
/// <param name="Name">The clause as the standard names it ("LINAGE clause").</param>
/// <param name="Formats">The §13.4.5.2 formats whose printed clause list contains it.</param>
/// <param name="HomeEntry">Null for a file description clause; otherwise the entry the clause belongs to, for a
/// clause the grammar parses in the FD position only so it can be refused BY NAME (the file control entry's
/// clauses: a user who writes <c>FD F ORGANIZATION IS RELATIVE</c> is told where the clause goes, not handed a
/// token error).</param>
/// <param name="DeletedAt2002">A COBOL-85 FD clause ISO/IEC 1989:2002 deleted (LABEL RECORDS, DATA RECORDS,
/// VALUE OF). It stood in every '85 FD format, so it is admitted in all three here and its edition is decided by
/// its removal gate; it is left out of the "admits only" sentence, which quotes the current §13.4.5.2.</param>
internal sealed record FileDescriptionClauseRow(
    Type Context, string Name, FileDescriptionFormat Formats, string? HomeEntry = null, bool DeletedAt2002 = false);

/// <summary>⛔ THE ONE PLACE THE FILE DESCRIPTION ENTRY'S FORMAT IS DECIDED AND ITS CLAUSE ADMISSIBILITY IS ASKED
/// (kb/Work PB1238, PB1081, PB865).
/// <para>Before it, no code decided which §13.4.5.2 format an FD was, so no rule keyed on the format existed:
/// BindFileSection bound every clause against every organization. LINAGE on a relative file bound a LINAGE page no
/// relative connector ever drives, CODE-SET on an indexed file compiled clean, LINAGE rode a report FD, and the
/// five file-control-entry alternatives the grammar carries in the FD position (ORGANIZATION, ACCESS MODE, RECORD
/// KEY, ALTERNATE RECORD KEY, FILE STATUS) had NO binder arm at all — <c>FD F ORGANIZATION IS RELATIVE</c> compiled
/// to a sequential connector and <c>FD F FILE STATUS IS FS</c> left FS unwired, silently.</para>
/// <para>The table is keyed by the parse-tree context TYPE of each <c>fileDescriptionClause</c> alternative, so it
/// is enumerable: <c>FileDescriptionFormatDriftTests</c> reflects over the generated parser and fails when a new
/// alternative lands with no row here — the next clause is decided by construction, never bound by default.</para>
/// </summary>
internal static class FileDescriptionFormats
{
    private const string FileControlEntry = "the file control entry (ISO §12.4.5.1)";

    /// <summary>Every <c>fileDescriptionClause</c> alternative except the <c>unrecognizedClause</c> error
    /// production (refused by <c>ClosedFormatPass</c>, COBOLNET1970), in the order §13.4.5.2 prints them. The
    /// Format columns are the rendered §13.4.5.2 lists.</summary>
    public static readonly IReadOnlyList<FileDescriptionClauseRow> Rows =
    [
        new(typeof(Core.FileGlobalExternalClauseContext), "IS EXTERNAL / IS GLOBAL clause", FileDescriptionFormat.All),
        new(typeof(Core.FormatClauseContext), "FORMAT clause", FileDescriptionFormat.Sequential),
        new(typeof(Core.BlockContainsClauseContext), "BLOCK CONTAINS clause", FileDescriptionFormat.All),
        new(typeof(Core.RecordClauseContext), "RECORD clause", FileDescriptionFormat.All),
        new(typeof(Core.LinageClauseContext), "LINAGE clause", FileDescriptionFormat.Sequential),
        new(typeof(Core.CodeSetClauseContext), "CODE-SET clause",
            FileDescriptionFormat.Sequential | FileDescriptionFormat.Report),
        new(typeof(Core.ReportClauseContext), "REPORT clause", FileDescriptionFormat.Report),
        new(typeof(Core.LabelRecordsClauseContext), "LABEL RECORDS clause", FileDescriptionFormat.All,
            DeletedAt2002: true),
        new(typeof(Core.DataRecordsClauseContext), "DATA RECORDS clause", FileDescriptionFormat.All,
            DeletedAt2002: true),
        new(typeof(Core.ValueOfClauseContext), "VALUE OF clause", FileDescriptionFormat.All, DeletedAt2002: true),
        new(typeof(Core.OrganizationClauseContext), "ORGANIZATION clause", FileDescriptionFormat.None,
            FileControlEntry),
        new(typeof(Core.AccessModeClauseContext), "ACCESS MODE clause", FileDescriptionFormat.None,
            FileControlEntry),
        new(typeof(Core.RecordKeyClauseContext), "RECORD KEY clause", FileDescriptionFormat.None, FileControlEntry),
        new(typeof(Core.AlternateKeyClauseContext), "ALTERNATE RECORD KEY clause", FileDescriptionFormat.None,
            FileControlEntry),
        new(typeof(Core.FileStatusClauseContext), "FILE STATUS clause", FileDescriptionFormat.None,
            FileControlEntry),
    ];

    /// <summary><see cref="Rows"/> by context type.</summary>
    public static readonly FrozenDictionary<Type, FileDescriptionClauseRow> ByContext =
        Rows.ToFrozenDictionary(r => r.Context);

    /// <summary>The format an FD is written in, decided ONCE from the file control entry's organization and
    /// whether the entry carries a REPORT clause (ISO §13.4.5.3 SR5/SR7/SR8). A relative or indexed file is
    /// Format 2 whatever else the FD says — §9.1.22 makes a report file sequential, so a REPORT clause on it is a
    /// Format 2 violation, not a Format 3 entry.</summary>
    public static FileDescriptionFormat Of(FileOrganization organization, bool hasReportClause) =>
        organization is FileOrganization.Relative or FileOrganization.Indexed ? FileDescriptionFormat.RelativeOrIndexed
        : hasReportClause ? FileDescriptionFormat.Report
        : FileDescriptionFormat.Sequential;

    /// <summary>The row for one written clause, or null for the <c>unrecognizedClause</c> error production (which
    /// its own pass refuses).</summary>
    public static FileDescriptionClauseRow? RowOf(Core.FileDescriptionClauseContext clause) =>
        clause.ChildCount > 0 && ByContext.TryGetValue(clause.GetChild(0).GetType(), out var row) ? row : null;

    /// <summary>How a diagnostic names a format: "Format 2 (relative-or-indexed)", as §13.4.5.2 captions it.</summary>
    public static string Caption(FileDescriptionFormat format) => format switch
    {
        FileDescriptionFormat.Sequential => "Format 1 (sequential)",
        FileDescriptionFormat.RelativeOrIndexed => "Format 2 (relative-or-indexed)",
        FileDescriptionFormat.Report => "Format 3 (report)",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "not a single §13.4.5.2 format"),
    };

    /// <summary>The §13.4.5.3 syntax rule that binds an entry to <paramref name="format"/>, quoted.</summary>
    public static string BindingRule(FileDescriptionFormat format) => format switch
    {
        FileDescriptionFormat.Sequential =>
            "ISO §13.4.5.3 SR5: \"Format 1 is the file description entry for a sequential file\"",
        FileDescriptionFormat.RelativeOrIndexed =>
            "ISO §13.4.5.3 SR7: \"Format 2 is the file description entry for a relative file or an indexed file\"",
        FileDescriptionFormat.Report =>
            "ISO §13.4.5.3 SR8: \"Format 3 is the file description entry for a report file\"",
        _ => throw new ArgumentOutOfRangeException(nameof(format), format, "not a single §13.4.5.2 format"),
    };

    /// <summary>The clauses <paramref name="format"/> admits, in the printed order, for the diagnostic's "admits
    /// only …" sentence — derived from the table, never restated.</summary>
    public static string Admitted(FileDescriptionFormat format) =>
        string.Join(", ", Rows.Where(r => !r.DeletedAt2002 && (r.Formats & format) != 0).Select(r => r.Name));
}
