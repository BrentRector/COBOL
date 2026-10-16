// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Procedure;

namespace CobolNet.Binding;

/// <summary>⛔ ONE ROW PER §8.4.2.2.2 QUALIFIED-NAME FORMAT — the resolution each routes through, so the uniqueness
/// obligation ISO §8.4.2.2.3 SR1 places on EVERY user-defined name ("For each non unique user-defined name that is
/// explicitly referenced, uniqueness shall be established through a sequence of qualifiers that precludes any
/// ambiguity of reference") has a named enforcement site per name class, or a named reason there is none
/// (kb/Work PB919).
/// <para>WHY A REGISTER. The rule was enforced class by class, and one class was never done: index-names had no
/// candidate set at all, so two tables' <c>INDEXED BY IX</c> silently shared one cell and the Format-3 spelling
/// that disambiguates them was "not defined". A name class added by a later edition must not be able to repeat
/// that: <c>QualifiedNameClassDriftTests</c> reads the format list out of the standard's §8.4.2.2.2 text and fails
/// when a format has no row here, when a row names a format the standard does not print, or when a row's
/// resolution member does not exist.</para></summary>
internal static class QualifiedNameClasses
{
    /// <summary>How a format's references are judged.</summary>
    internal enum Disposition
    {
        /// <summary>References are resolved and counted at the named member.</summary>
        Resolved,
        /// <summary>No reference can reach resolution: the facility that declares the name is refused by name at the
        /// named member (a DECLINED optional module or processor-dependent element).</summary>
        Declined,
    }

    /// <summary>One §8.4.2.2.2 format: its printed name, the member that owns its resolution (or decline), and
    /// the rule that member enforces.</summary>
    internal sealed record Row(string Format, Disposition Disposition, Type Owner, string Member, string Rule);

    internal static readonly IReadOnlyList<Row> All =
    [
        new("qualified-data-name", Disposition.Resolved, typeof(DataBinder), "QualifiedCandidates",
            "§8.4.2.2.3 SR1 + SR4 — counted by DataBinder.UniqueOrReportAmbiguous (data division) and "
            + "ReferenceResolver.ReportUnidentified (procedure division)"),
        new("qualified-condition-name", Disposition.Resolved, typeof(ConditionBinder), "ConditionOf",
            "§8.4.2.2.3 SR1 + SR5"),
        new("qualified-index-name", Disposition.Resolved, typeof(ReferenceResolver), "ResolveIndexName",
            "§8.4.2.2.3 SR1 + SR6 — counted by DataBinder.UniqueOrReportAmbiguous (kb/Work PB919)"),
        new("qualified-procedure-name", Disposition.Resolved, typeof(ProcedureTableBuilder), "ResolveProcedureWord",
            "§8.4.2.2.3 SR1 + SR7"),
        new("qualified-screen-name", Disposition.Declined, typeof(ScreenFacility), "ReportSection",
            "the SCREEN SECTION is the declined Annex A.4.2 module (COBOLNET1560) — no screen-name is declared"),
        new("qualified-record-key-name", Disposition.Resolved, typeof(Model.FileModel), "KeyIndexOfRecordKeyName",
            "§8.4.2.2.3 SR1 — a record-key-name is asked of the file its statement or clause names, and Format 6's "
            + "one file-name qualifier must name that file (kb/Work PB1099)"),
        new("qualified-linage-counter", Disposition.Resolved, typeof(ExpressionBinder), "LinageFileOf",
            "§8.4.2.2.3 SR8"),
        new("qualified-report-counter", Disposition.Resolved, typeof(ReportWriterBinder), "CounterReportOf",
            "§8.4.2.2.3 SR9 + SR10"),
    ];
}
