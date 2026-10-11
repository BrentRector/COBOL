// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using Core = CobolParserCore;
using CobolNet.Compiler.Oo;
using ReportNextGroup = CobolNet.Runtime.IO.ReportNextGroup;
using ReportNextGroupKind = CobolNet.Runtime.IO.ReportNextGroupKind;
using CobolNet.Runtime;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  REPORT SECTION binding (ISO/IEC 1989:2023 §13.6 report section / §13.14 report description / §13.15 report
//  group description; COBOLNET_REPORT_WRITER_DESIGN §3). Each RD becomes a ReportModel — page geometry with the
//  §13.18.39.4 GR3 defaults applied, the CONTROL hierarchy, and the report groups as LINE-clause-built line
//  lists of printable fields. A printable item is carried as a SYNTHETIC DataItem (PicInfo + flags, never added
//  to the storage forest — report groups are not data storage): the emitter then renders each SOURCE/VALUE
//  through the ONE MOVE conversion path (CSharpEmitter.ConvertSource), which IS §13.18.53.4 GR1's implicit MOVE.
//  Legal-but-unimplemented clauses stage LOUD here (Edition.Error, COBOLNET0899) — never silently dropped (§1.4).
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>One report description entry (ISO §13.14): identity, owning file (resolved post-build from the FD's
/// REPORT clause, §13.18.46), the §13.18.39.4 page regions (GR3 defaults applied), the §13.18.16 control
/// hierarchy (major→minor; FINAL first when present), the report groups, and the SUM counters.</summary>
public sealed class ReportModel
{
    public required string Name { get; init; }

    /// <summary>The report file whose FD names this report in a REPORT(S) clause (ISO §13.18.46), resolved
    /// post-build; a report named by NO file description entry is a bind error.</summary>
    public FileModel? File { get; set; }

    /// <summary>True when the RD has a PAGE clause (§13.18.39.4 GR2a — absent ⇒ one page of indefinite length;
    /// the page-fit/advance machinery is then inert).</summary>
    public bool Paged { get; set; }

    // The §13.18.39.4 GR2 page regions (GR3 defaults applied by the binder; meaningful only when Paged).
    public int PageLimit { get; set; }

    /// <summary>⛔ THE ONE VERTICAL OPERAND LIMIT — "the page limit, or 9999 if the report is not divided into
    /// pages". The standard writes that sentence for the LINE clause (§13.18.35.3 SR3) and again for the NEXT
    /// GROUP clause (§13.18.37.3 SR1); both screens read it here, with <see cref="VerticalLimitWords"/> naming it
    /// in their diagnostics.</summary>
    public int VerticalLimit => Paged ? PageLimit : 9999;

    /// <summary>What <see cref="VerticalLimit"/> is, in a diagnostic's words.</summary>
    public string VerticalLimitWords => Paged ? $"the page limit {PageLimit}"
        : "9999, the limit for a report that is not divided into pages";
    public int Heading { get; set; }
    public int FirstDetail { get; set; }
    public int LastControlHeading { get; set; }
    public int LastDetail { get; set; }
    public int Footing { get; set; }

    /// <summary>The page width — integer-2 of the PAGE clause (ISO §13.18.39.4 GR2b, "the maximum number of print
    /// columns that may be accommodated in any line of the report"), or 999 when it is omitted (GR5, "a value of
    /// 999 is assumed for the page width"). It is the bound of the COLUMN clause's own operands (§13.18.14.3 SR6,
    /// COBOLNET2710) and of the line the engine places printable items in (§13.18.14.4 GR5, EC-REPORT-PAGE-WIDTH).
    /// Independent of <see cref="Paged"/>: a PAGE clause may give the width alone.</summary>
    public int PageWidth { get; set; } = DefaultPageWidth;

    /// <summary>The PAGE clause wrote integer-2 (so <see cref="PageWidth"/> is the program's, not GR5's default).</summary>
    public bool PageWidthWritten { get; set; }

    /// <summary>§13.18.39.4 GR5's assumed page width.</summary>
    public const int DefaultPageWidth = 999;

    /// <summary>The report line width: the record size the FD's RECORD clause establishes in any of its three formats
    /// (<see cref="FileModel.RecordClause"/>, upper operand) less the CODE characters, else the widest field extent
    /// (column + image width − 1) over the report (the §13.18.39.4 GR5 page-width default 999 is a MAXIMUM, not
    /// a record length). A line wider than this is cut when recorded. Computed post-build.</summary>
    public int LineWidth { get; set; } = 1;

    /// <summary>The RD's CODE clause (ISO §13.18.12), or null — the characters every logical record this report
    /// writes begins with (GR1).</summary>
    public ReportCodeModel? Code { get; set; }

    /// <summary>The RD wrote a CODE clause — true even when its operand was refused, so §13.18.12.3 SR3's "specified
    /// for each report" is asked of what was WRITTEN and never double-reports a refused operand.</summary>
    public bool CodeWritten { get; set; }

    /// <summary>The CONTROL hierarchy in major→minor order (ISO §13.18.16.4 GR1; FINAL, if present, first — GR2).</summary>
    public List<ReportControlModel> Controls { get; } = [];

    /// <summary>The report group description entries as WRITTEN, in source order (§13.15) — every entry the RD
    /// is followed by, whether or not it produced a line, a printable field or a counter. The screens whose rule
    /// is about what was written (§13.15.3 SR16's PRESENT WHEN operands, the report-section name set) read this
    /// and never the bound products, which exist only for the entries that make one (kb/Work PB1289).</summary>
    public IReadOnlyList<Core.ReportGroupEntryContext> WrittenEntries { get; init; } = [];

    /// <summary>The report groups in declaration order.</summary>
    public List<ReportGroupModel> Groups { get; } = [];

    /// <summary>The SUM counters of this report (ISO §13.18.54), one per OCCURRENCE, in counter-id order (the
    /// index of a counter in this list IS its <see cref="ReportSumModel.Id"/> once the description is bound —
    /// <c>DataBinder.SealSumCounters</c>).</summary>
    public List<ReportSumModel> Sums { get; } = [];

    /// <summary>The SUM ENTRIES of this report, one <see cref="ReportSumFamily"/> per entry containing a SUM clause
    /// (ISO §13.18.54.4 GR1), in the order their counter-id blocks were reserved.</summary>
    public List<ReportSumFamily> SumFamilies { get; } = [];

    /// <summary>EVERY entry of this report that carries a value a rolled total can add (ISO §13.18.54.4 GR6): the
    /// SUM entries (<see cref="SumFamilies"/>' families, the same objects) and the entries with a SOURCE or VALUE
    /// clause, printable or not. The one list SUM data-name-1 (§13.18.54.3 SR4) is resolved against.</summary>
    public List<ReportEntryFamily> EntryFamilies { get; } = [];

    /// <summary>The occurrences of those entries, report-wide in binding order — the rows the procedure-phase binder
    /// gives a value and the emitter a presence slot (<see cref="ReportItemOccurrence"/>).</summary>
    public List<ReportItemOccurrence> ItemOccurrences { get; } = [];

    /// <summary>The data-names the report's VARYING clauses define (§13.18.64.3 SR2), once per WRITTEN entry — a
    /// repeating entry's replays share one declaration. Filled by <c>ScreenReportVaryingClauses</c>, which asks SR2's
    /// "not defined elsewhere in the source element" of each name once the whole source element is described;
    /// <see cref="HasVarying"/> is what the FLAG-02 pass asks.</summary>
    public List<string> VaryingNames { get; } = [];

    /// <summary>The report writes a VARYING clause somewhere (§13.18.64) — including a group entry's, which no
    /// printable field of its own carries.</summary>
    public bool HasVarying => VaryingNames.Count > 0;

    /// <summary>This report's index within its program unit — backs the emitted engine field name
    /// (<c>__RPT_{CsIndex}</c>).</summary>
    public int CsIndex { get; set; }

    /// <summary>A compilation-unique identity (drawn from the declaring unit's disjoint uid band). It names the
    /// members every program that can see this report emits for it — the §14.9.49.4 GR4 USE BEFORE REPORTING
    /// selector in particular, whose name must agree between a contained program and its containers.</summary>
    public required int Uid { get; init; }

    /// <summary>The RD entry specifies the GLOBAL clause (ISO §13.18.27.3 SR1 e)): the report-name is a global
    /// name, and so is every data-name subordinate to it (§13.18.27.4 GR1) — its report groups and its sum
    /// counters — visible to every program the declaring program contains, directly or indirectly (GR2).</summary>
    public bool IsGlobal { get; set; }

    /// <summary>This report's PAGE-COUNTER AS A DATA ITEM (ISO §8.4.3.15.4 GR1 — "temporary unsigned integer
    /// data items of class and category numeric, which are maintained for each report"): the implicitly-defined
    /// register a procedure division RECEIVING reference resolves to (§8.4.3.15.3 SR1, kb/Work PB429). Off
    /// ByName/Roots like the SUM counter's register and the OCCURS DYNAMIC CAPACITY register — its value IS the
    /// engine's, so it allocates no storage. LINE-COUNTER has no such register: SR3 bars it from the receiving
    /// side, and the sending side of both counters is <c>BoundReportCounterRef</c>.</summary>
    public required DataItem PageCounterRegister { get; init; }
}

/// <summary>The RD CODE clause (ISO §13.18.12.2 — <c>CODE IS {literal-1 | identifier-1}</c>): the characters "automatically
/// placed in the first characters of each logical record written to the report file for this report" (GR1), which are
/// "not included in the descriptions of the lines in the report, but are included in the logical record size" (GR2).
/// Exactly one of <see cref="Literal"/> (an alphanumeric literal's characters) and <see cref="Operand"/> (an
/// identifier, evaluated by the engine at each body group — GR3) is set.</summary>
public sealed class ReportCodeModel
{
    /// <summary>literal-1's characters, decoded (concatenation folded, hexadecimal format decoded); null for identifier-1.</summary>
    public string? Literal { get; init; }

    /// <summary>identifier-1 AS WRITTEN — base word, qualifiers, subscripts and reference modification (§13.18.12.2) —
    /// the SOURCE clause's own written-reference form (kb/Work PB1129 × PB1292); null for literal-1. The BASE item it
    /// names is <see cref="Item"/> (the data-phase SR2 screen's subject) and the value the engine reads is
    /// <see cref="Value"/> (procedure phase, the ONE sending-operand resolution).</summary>
    public Core.DataReferenceContext? Reference { get; init; }

    /// <summary>The clause's operand as written, for diagnostics.</summary>
    public required string Written { get; init; }

    /// <summary>How many character positions the code occupies at the start of every logical record — literal-1's
    /// length, or identifier-1's (fixed: SR2 excludes every variable-length shape). 0 until resolved.</summary>
    public int Length { get; set; }

    /// <summary>The resolved base item of identifier-1 (data bind); null for literal-1 or an unresolved operand.</summary>
    public DataItem? Item { get; set; }

    /// <summary>identifier-1 as the place the engine reads at each evaluation (§13.18.12.4 GR3), resolved in the
    /// procedure phase; null for literal-1 or a refused operand.</summary>
    public Place? Value { get; set; }
}

/// <summary>One CONTROL clause operand (ISO §13.18.16): FINAL or a (possibly qualified) data-name resolved
/// post-build to its item.</summary>
public sealed class ReportControlModel
{
    public bool IsFinal { get; init; }
    public ReportControlRef? Operand { get; init; }
    public DataItem? Item { get; set; }

    /// <summary>The operand as written, for diagnostics and emitted comments (FINAL has no data reference).</summary>
    public string Display => IsFinal ? "FINAL" : Operand?.ToString() ?? "?";
}

/// <summary>
/// ONE operand of the CONTROL clause AS WRITTEN (ISO §13.18.16.3): the data-name, its IN/OF qualifier words in
/// written order (§8.4.2.2), and — SR4 — the optional reference modification, whose leftmost-position and length
/// are integer literals.
/// <para>⛔ THE SAME WRITTEN REFERENCE IS HOW TWO OTHER CLAUSES NAME A CONTROL LEVEL, so the "is this one of the
/// CONTROL clause's operands" test is written down ONCE, here (kb/Work PB205): §13.18.57.3 SR10 — "Data-name-1
/// and data-name-2 may be qualified and reference-modified. If data-name-1 or data-name-2 is reference-modified,
/// leftmost-position and length shall be integer literals. Each data-name-1, data-name-2, and FINAL, if
/// specified, shall be the same as one of the operands of the CONTROL clause of the corresponding report
/// description entry." — and §13.18.54.3 SR8, the same sentence for SUM … RESET ON data-name-3. All three sites
/// previously kept only the base word (TYPE, RESET) or the base word plus qualifiers (CONTROL) and compared by
/// NAME, so <c>CX(1:3)</c> and <c>CX(4:3)</c> were the same operand and the minor control footing silently bound
/// to the major level — the three-arm form of this repo's two-arm-dispatch defect.</para>
/// <para>Equality is over the WRITTEN reference, which is what SR6 makes unique ("Data-name-1 shall be unique in
/// any given CONTROL clause") — and unique it must be TEXTUALLY, because SR6's own second sentence permits two
/// operands to "refer to the same physical data item or to overlapping data items", so the referenced item
/// cannot be what distinguishes them.</para>
/// </summary>
/// <param name="Name">The base data-name.</param>
/// <param name="Qualifiers">The IN/OF qualifier words, innermost first (§8.4.2.2).</param>
/// <param name="RefModStart">The reference modification's leftmost-position integer literal; null when the
/// operand is not reference-modified.</param>
/// <param name="RefModLength">Its length integer literal; null both when there is no ref-mod and for
/// §8.4.3.3.2's omitted, bracketed length (the slice then runs to the rightmost position, §8.4.3.3.4 GR5c).</param>
public sealed record ReportControlRef(
    string Name, IReadOnlyList<string> Qualifiers, int? RefModStart, int? RefModLength)
{
    /// <summary>The identity ISO §13.18.57.3 SR10 requires — "the same as one of the operands of the CONTROL
    /// clause" — which §13.18.54.3 SR8 asks of its own operand in its own words ("Data-name-3 or FINAL shall be
    /// an operand of the CONTROL clause of the current report description"). The record's synthesized equality
    /// cannot serve — <see cref="Qualifiers"/> is a list, compared by reference — and COBOL words are
    /// case-insensitive (§8.2), so the test is spelled out.</summary>
    public bool SameOperandAs(ReportControlRef other)
    {
        if (!CobolNames.Same(Name, other.Name)) return false;
        if (RefModStart != other.RefModStart || RefModLength != other.RefModLength) return false;
        if (Qualifiers.Count != other.Qualifiers.Count) return false;
        for (int i = 0; i < Qualifiers.Count; i++)
            if (!CobolNames.Same(Qualifiers[i], other.Qualifiers[i])) return false;
        return true;
    }

    /// <summary>The operand as written — the form every diagnostic about it quotes.</summary>
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder(Name);
        foreach (var q in Qualifiers) sb.Append(" OF ").Append(q);
        if (RefModStart is { } s) sb.Append('(').Append(s).Append(':').Append(RefModLength?.ToString() ?? "").Append(')');
        return sb.ToString();
    }
}

/// <summary>A report group's TYPE (ISO §13.18.57 Format 2).</summary>
public enum ReportGroupKindModel { ReportHeading, PageHeading, ControlHeading, Detail, ControlFooting, PageFooting, ReportFooting }

/// <summary>One report group description (ISO §13.15): its 01-level name (a detail's GENERATE handle,
/// §14.9.16 SR1), TYPE, CH/CF control association, and the LINE-built report lines.</summary>
public sealed class ReportGroupModel
{
    public string? Name { get; set; }
    public ReportGroupKindModel Kind { get; set; } = ReportGroupKindModel.Detail;

    /// <summary>The level 1 entry that opened the group — where a diagnostic about the group as a whole stands
    /// (§13.18.57.3 SR13–SR15, <c>ScreenReportGroupCensus</c>).</summary>
    public CobolParserCore.ReportGroupEntryContext? Entry { get; init; }

    /// <summary>The CH/CF control operand as written (§13.18.57.3 SR10 — qualifiable AND reference-modifiable);
    /// null when omitted (legal only with a one-operand CONTROL clause, SR11), for FINAL, or for a non-control
    /// group.</summary>
    public ReportControlRef? ControlOperand { get; set; }
    public bool ControlFinal { get; set; }

    /// <summary>A control heading written with the OR PAGE phrase (§13.18.57.2): it is printed "in addition after each
    /// page advance, following any page heading" (§13.18.57.4 GR6 c)) — the engine's <c>ReportGroup.OrPage</c>.</summary>
    public bool OrPage { get; set; }

    /// <summary>The resolved control LEVEL (the index into <see cref="ReportModel.Controls"/>); −1 until
    /// resolved / for non-control groups.</summary>
    public int ControlLevel { get; set; } = -1;

    public List<ReportLineModel> Lines { get; } = [];

    /// <summary>The group's NEXT GROUP clause as written on its level 1 entry (ISO §13.18.37; §13.15.3 SR6), null
    /// when none — captured during the entry walk because the TYPE clause the syntax rules depend on may follow it
    /// in the same entry, and bound by <c>BindNextGroupClauses</c> once the group is complete.</summary>
    public CobolParserCore.ReportNextGroupClauseContext? NextGroupClause { get; set; }

    /// <summary>The bound NEXT GROUP clause — the runtime's own record, which the emitter writes verbatim, so the
    /// engine and the model cannot disagree about its shape (ISO §13.18.37.2).</summary>
    public CobolNet.Runtime.IO.ReportNextGroup? NextGroup { get; set; }
}

/// <summary>The LINE clause form of one report line (ISO §13.18.35; the NEXT PAGE phrase is a flag on the line,
/// <see cref="ReportLineModel.NextPage"/>, not a kind) — plus the STEP placement a later occurrence of a VERTICALLY repeating entry takes (§13.18.38.4 GR12c/GR12d).
/// The names and the meanings are the runtime <c>ReportLineKind</c>'s; the model is what the emitter copies.</summary>
public enum ReportLineKindModel { Absolute, Relative, Step }

/// <summary>One report line: its LINE clause, its printable fields in declaration order, and its effective
/// PRESENT WHEN chain (ISO §13.18.41 Format 1) — every condition on the entry that opened the line AND on its
/// ancestors up to the 01 (GR2b: an absent ancestor makes every subordinate absent, so the line is present iff
/// ALL chain conditions are true). Conditions are captured as parse contexts at data bind and bound through the
/// ONE <c>ConditionBinder</c> when the procedure phase runs (<c>ReportWriterBinder.BindReportGroupClauses</c>).</summary>
public sealed class ReportLineModel(ReportLineKindModel kind, int value)
{
    public ReportLineKindModel Kind { get; } = kind;
    public int Value { get; } = value;
    public List<ReportFieldModel> Fields { get; } = [];

    /// <summary>The WRITTEN report group description entry whose LINE clause opened this line — the same entry for
    /// every replay of a repeating entry, so the line-set rules of §13.18.35.3 SR6 report once per pair of entries.
    /// Null only before <c>BindReportEntry</c> has stamped the line it made.</summary>
    public CobolParserCore.ReportGroupEntryContext? Entry { get; set; }

    /// <summary>This line's step-anchor slot (ISO §13.18.38.4 GR12c/GR12d), 0 when the line neither seeds one
    /// nor steps from one. A non-Step line with an anchor SEEDS it with the page line it lands on; a
    /// <see cref="ReportLineKindModel.Step"/> line places at anchor + <see cref="Value"/>.</summary>
    public int Anchor { get; init; }

    /// <summary>A Step line's own written integer-2 — the fallback when the first occurrence of this line was
    /// absent under a PRESENT WHEN clause, so its anchor was never seeded (§13.18.41.4 GR2b).</summary>
    public int RelativeBase { get; init; }

    /// <summary>A Step line's §13.18.35.4 GR4c page-fit contribution: integer-3 when this line OPENS an
    /// occurrence, 0 otherwise — "the vertical interval between successive occurrences is added into the trial
    /// sum once for each occurrence beyond the first". Absolute and relative lines compute their own.</summary>
    public int TrialInterval { get; init; }

    /// <summary>The line's LINE clause carries the NEXT PAGE phrase (ISO §13.18.35.2 Format 1 — <c>integer-1 ON
    /// NEXT PAGE</c>, or the bare <c>ON NEXT PAGE</c> operand). Set on the FIRST occurrence only: §13.18.35.3 SR7
    /// allows the phrase only in the group's first LINE clause, and SR10a only with a multiple LINE clause's first
    /// operand, so a later occurrence of the same clause places as the phrase-less form. The engine reads it on
    /// the group's first PRESENT line (§13.18.35.4 GR4a / GR5a).</summary>
    public bool NextPage { get; init; }

    /// <summary>The PRESENT WHEN condition chain (01 → line entry) as captured parse contexts (§13.18.41).</summary>
    public List<CobolParserCore.ConditionContext> PresentWhenCtxs { get; } = [];
    /// <summary>The bound chain (AND-composed by the emitter); parallel to <see cref="PresentWhenCtxs"/>.</summary>
    public List<BoundCondition> PresentWhen { get; } = [];

    /// <summary>The OCCURS … DEPENDING presence tests this LINE inherits (ISO §13.18.38.4 GR13), outermost
    /// repeating entry first — empty unless the line lies inside a VERTICALLY repeating entry with the
    /// DEPENDING phrase. GR13 makes such an OCCURS "have the same effect as an OCCURS clause with no TO or
    /// DEPENDING phrases and with an integer-2 equal to the current value of data-name-1", so a repetition past
    /// that count does not EXIST — on the vertical axis that means its whole report line is absent, not blank.
    /// The emitter composes it into the same delegate the PRESENT WHEN chain feeds, which is what makes
    /// §13.18.35.4 GR4c ("If any of the LINE clauses used in computing the trial sum are subject to a PRESENT
    /// WHEN clause or to an OCCURS clause with the DEPENDING phrase, these clauses are taken into account")
    /// true for both suppressors at once.</summary>
    public List<ReportRepetitionGuard> RepetitionGuards { get; } = [];
}

/// <summary>How ONE placement of a printable item fixes its leftmost column.</summary>
public enum ReportColumnKindModel
{
    /// <summary>COLUMN NUMBER integer-1 — the leftmost character stands in that column (ISO §13.18.14.4 GR1/GR2).</summary>
    Absolute,
    /// <summary>COLUMN PLUS integer-2 — integer-2 columns right of the line's horizontal counter (GR8).</summary>
    Relative,
    /// <summary>The FIRST repetition of a STEP'd repeating entry (ISO §13.18.38.4 GR12): placed like
    /// <see cref="Relative"/>, and its leftmost column is REMEMBERED in the entry's step anchor.</summary>
    AnchorSeed,
    /// <summary>A later repetition of a STEP'd repeating entry: "integer-3 columns to the right of the column
    /// they occupy in the preceding occurrence" (ISO §13.18.38.4 GR12b) — the anchor advanced by integer-3. The
    /// anchor, not the horizontal counter, is the datum, because GR12 measures from the PRECEDING OCCURRENCE'S
    /// LEFTMOST while the counter holds the last placed item's RIGHTMOST (§13.18.14.4 GR9).</summary>
    AnchorStep,
}

/// <summary>The COLUMN clause's alignment word (ISO §13.18.14.2 Format 1, §13.18.14.4 GR6 b)–d)): which edge of the
/// printable item integer-1 names. LEFT is assumed when none is written and an operand is absolute (§13.18.14.3 SR9).</summary>
public enum ReportColumnAlignment { Left, Center, Right }

/// <summary>One placement of a printable item: a COLUMN clause operand (ISO §13.18.14 Format 1) or, for a
/// repetition of a STEP'd repeating entry, its step-anchor placement (§13.18.38.4 GR12).</summary>
/// <param name="Kind">Which datum fixes the leftmost column.</param>
/// <param name="Value">integer-1 / integer-2 / integer-3, per <paramref name="Kind"/>.</param>
/// <param name="AnchorId">The step anchor's compose-local register id; 0 for the two COLUMN-clause kinds.</param>
/// <param name="Alignment">Which edge of the item <paramref name="Value"/> names (§13.18.14.4 GR6 b)–d)); only an
/// <see cref="ReportColumnKindModel.Absolute"/> operand can carry anything but the default (§13.18.14.3 SR9).</param>
public readonly record struct ReportColumnSpec(
    ReportColumnKindModel Kind, int Value, int AnchorId = 0, ReportColumnAlignment Alignment = ReportColumnAlignment.Left)
{
    /// <summary>A COLUMN clause operand as written: absolute, or relative (PLUS), with the clause's alignment word
    /// (<see cref="ReportColumnAlignment.Left"/> when none is written — §13.18.14.3 SR9).</summary>
    public ReportColumnSpec(bool relative, int value, ReportColumnAlignment alignment = ReportColumnAlignment.Left)
        : this(relative ? ReportColumnKindModel.Relative : ReportColumnKindModel.Absolute, value, 0, alignment) { }

    /// <summary>⛔ THE ONE LEFTMOST-COLUMN COMPUTATION of an ABSOLUTE operand (ISO §13.18.14.4 GR6 b)–d)), read by
    /// the compose emitter's placement and by the bind-time line-width walk, so the two cannot place an item in
    /// different columns (kb/Work PB1220). LEFT: integer-1. RIGHT: integer-1 − printable-size + 1. CENTER, odd
    /// printable-size: integer-1 − ((printable-size − 1) / 2); even: (integer-1 − (printable-size / 2)) + 1 — both
    /// are integer-1 − (printable-size − 1) / 2 in truncating integer arithmetic.</summary>
    public int AbsoluteLeftmost(int printableSize) => Alignment switch
    {
        ReportColumnAlignment.Right => Value - printableSize + 1,
        ReportColumnAlignment.Center => Value - (printableSize - 1) / 2,
        _ => Value,
    };

    /// <summary>The rightmost column of an ABSOLUTE operand — GR6 b) integer-1 + printable-size − 1 for LEFT,
    /// integer-1 for RIGHT, integer-1 + (printable-size / 2) for CENTER — which GR9 makes the horizontal counter.
    /// It is the leftmost column plus the item's extent in every case (checked against GR6 d) 1. and 2.).</summary>
    public int AbsoluteRightmost(int printableSize) => AbsoluteLeftmost(printableSize) + printableSize - 1;

    /// <summary>True when the placement is NOT a fixed column number — i.e. the line needs the §13.18.14.4 GR7
    /// horizontal counter. <see cref="ReportColumnKindModel.AnchorSeed"/> reads the counter; its
    /// <see cref="ReportColumnKindModel.AnchorStep"/> siblings read the anchor the seed wrote, and a step can
    /// never appear on a line without its seed, so grouping all three here is exact.</summary>
    public bool Relative => Kind is not ReportColumnKindModel.Absolute;
}

/// <summary>
/// A report group description entry's OCCURS clause — ISO §13.18.38 FORMAT 3 (report-writer):
/// <c>OCCURS [ integer-1 TO ] integer-2 TIMES [ DEPENDING ON data-name-1 ] [ STEP integer-3 ]</c>.
/// <para>The entry is a REPEATING ENTRY: §13.18.38.4 GR10 makes it "define integer-2 distinct report items",
/// GR11 gives every repetition the same PICTURE/VALUE/JUSTIFIED/BLANK WHEN ZERO/GROUP INDICATE effect, and GR12
/// makes the STEP phrase the interval between successive occurrences. The binder REPLAYS the entry's subtree
/// once per repetition (COBOLNET_REPORT_WRITER_DESIGN §3.4), which is what §13.18.63.4 GR21's import of GR9
/// ("causes every occurrence of the associated data item to be assigned the specified value") asks for.</para>
/// </summary>
/// <param name="Min">integer-1 (the DEPENDING minimum); 0 when the TO phrase is absent.</param>
/// <param name="Max">integer-2 — the number of repetitions the entry defines.</param>
/// <param name="DependingName">data-name-1 of the DEPENDING phrase, else null.</param>
/// <param name="DependingQualifiers">its IN/OF qualifier words (§8.4.2.2).</param>
/// <param name="Step">integer-3 of the STEP phrase, else null.</param>
public sealed record ReportOccursSpec(
    int Min, int Max, string? DependingName, IReadOnlyList<string> DependingQualifiers, int? Step)
{
    /// <summary>data-name-1, resolved post-build (the SOURCE-operand pattern) — the §13.18.38.4 GR13 count.</summary>
    public DataItem? DependingItem { get; set; }

    /// <summary>⛔ THE AXIS DECIDES WHICH HALF OF GR10 AND GR12 APPLIES, AND A STEP DISPLACES ON THAT AXIS ONLY.
    /// §13.18.38.4 GR10a/GR10b and GR12a/GR12b are the COLUMN (horizontal) arms; GR10c/GR10d and GR12c/GR12d are
    /// the LINE (vertical) arms. An entry that contains, or has subordinate to it, a LINE clause repeats
    /// VERTICALLY; every other repeating entry repeats horizontally. Writing it down once is what keeps a
    /// vertical entry's integer-3 out of the horizontal displacement and vice versa.</summary>
    public ReportRepetitionAxis Axis { get; init; } = ReportRepetitionAxis.Horizontal;

    /// <summary>Where data-name-1 was written — its post-build resolution reports there.</summary>
    public Editions.DiagnosticCursor DependingAt { get; init; }
}

/// <summary>The axis a report-group repeating entry repeats on (ISO §13.18.38.4 GR10/GR12; see
/// <see cref="ReportOccursSpec.Axis"/>).</summary>
public enum ReportRepetitionAxis { Horizontal, Vertical }

/// <summary>
/// ONE repetition's presence test for a repeating entry with the DEPENDING phrase (ISO §13.18.38.4 GR13 with
/// §13.18.63.4 GR22 — "The value of literal-1 is used for the content of the printable item whenever it is
/// printed, except that a GROUP INDICATE, PRESENT WHEN, or OCCURS clause with the DEPENDING phrase may suppress
/// the appearance of the item").
/// <para>GR13: data-name-1 "is evaluated just before the processing for the first LINE clause of the report
/// group. If the value of data-name-1 is not in the range integer-1 to (integer-2 - 1), the report group is
/// processed as though the OCCURS clause had been written without the TO and DEPENDING phrases" — i.e. all
/// integer-2 repetitions appear — "If the value of data-name-1 is in the range integer-1 to (integer-2 - 1), the
/// OCCURS clause has the same effect as an OCCURS clause with no TO or DEPENDING phrases and with an integer-2
/// equal to the current value of data-name-1" — i.e. that many appear. So repetition <paramref name="Ordinal"/>
/// is present exactly when <c>Ordinal &lt; (inRange ? data-name-1 : integer-2)</c>.</para>
/// <para>⛔ Suppression here is NOT absence for VALUE purposes: §13.18.63.4 GR23's last sentence — "If any of the
/// printable items are suppressed as a result of a PRESENT WHEN clause or an OCCURS clause with a DEPENDING
/// phrase, VALUE operands are nevertheless assigned to them, even though they are not printed" — which the
/// bind-time replay satisfies by construction: every repetition is bound, only its PLACEMENT is guarded.</para>
/// </summary>
/// <param name="Spec">The repeating entry's OCCURS clause.</param>
/// <param name="Ordinal">This repetition's zero-based ordinal within that entry.</param>
public sealed record ReportRepetitionGuard(ReportOccursSpec Spec, int Ordinal);

/// <summary>One report VARYING counter (ISO §13.18.64) — the temporary integer data item an ENTRY containing a
/// VARYING clause establishes (GR1). It is declared by its ENTRY, not by the printable leaf that happens to sit
/// under it, so a group entry's counter exists and a subordinate entry can name it (§13.18.64.3 SR2: "This
/// definition of data-name-1 may be referenced only within the current entry or a subordinate entry"; SR3 lets a
/// subordinate entry's FROM and BY name it too). One instance is made per WRITTEN entry per enclosing repetition,
/// because GR3's "first occurrence" restarts with every occurrence of an enclosing repeating entry
/// (kb/Work PB1306): its occurrences are the repetitions of the declaring entry within ONE enclosing occurrence.
/// <para>The counter name, the FROM/BY expressions as captured parse contexts (bound to <see cref="From"/>/
/// <see cref="By"/> in the procedure phase through the ONE expression binder, with <see cref="Enclosing"/> and
/// <see cref="Group"/> in scope; null = the GR3 default 1).</para></summary>
public sealed class ReportVaryingModel
{
    public required string Name { get; init; }
    public CobolParserCore.ArithmeticExpressionContext? FromCtx { get; init; }
    public CobolParserCore.ArithmeticExpressionContext? ByCtx { get; init; }
    public BoundExpr? From { get; set; }
    public BoundExpr? By { get; set; }

    /// <summary>The compilation-unique identity that names the compose-local variable holding the counter.</summary>
    public required int Uid { get; init; }

    /// <summary>The compose-local variable (<see cref="BoundReportVaryingRef.CsName"/>) a reference to this counter reads.</summary>
    public string CsName => $"__kv{Uid}";

    /// <summary>The report group description entry whose VARYING clause declares the counter.</summary>
    public required CobolParserCore.ReportGroupEntryContext Entry { get; init; }

    /// <summary>Every counter of the declaring entry, in clause order (§13.18.64.2 — one clause names several).</summary>
    public IReadOnlyList<ReportVaryingModel> Group { get; set; } = [];

    /// <summary>The counters of the entries ENCLOSING the declaring entry, outermost first — the names §13.18.64.3 SR3
    /// lets this counter's FROM and BY reference ("arithmetic-expression-1 or arithmetic-expression-2 of a VARYING
    /// clause in a subordinate entry").</summary>
    public IReadOnlyList<ReportVaryingModel> Enclosing { get; init; } = [];

    /// <summary>The BY expression names a counter of the SAME entry (§13.18.64.3 SR3: "may be referenced in
    /// arithmetic-expression-2 of the same VARYING clause"), so occurrence n is not FROM + n × BY: each step adds a
    /// BY evaluated against the counters as the previous step left them (GR3 b), a running recurrence.</summary>
    public bool Recurrent { get; init; }

    /// <summary>The FROM/BY expressions have been bound (they are shared by every field the counter reaches).</summary>
    public bool Bound { get; set; }
}

/// <summary>A VARYING counter as one printable field sees it: the counter, and the occurrence number of its DECLARING
/// entry this field's placement lies in (§13.18.64.4 GR3 — the first occurrence takes FROM, each later one adds BY).
/// <paramref name="PerPlacement"/> is set when the field's OWN entry declares the counter and repeats through a
/// multiple COLUMN clause: placement <c>j</c> of the field is then occurrence <c>Ordinal + j</c>. Every other use is
/// one fixed occurrence — the field lies inside ONE occurrence of the declaring entry.</summary>
public sealed record ReportVaryingUse(ReportVaryingModel Counter, int Ordinal, bool PerPlacement);

/// <summary>One PRINTABLE item (an entry with a COLUMN clause, ISO §13.18.14): its column operands (one per
/// repetition — a multiple COLUMN clause is a repeating entry, §13.15.4 GR3), the synthetic
/// <see cref="DataItem"/> carrying its PICTURE/JUSTIFIED/BLANK WHEN ZERO, its value-source OPERAND LIST, the
/// GROUP INDICATE flag (§13.18.28), its field-local PRESENT WHEN chain (the conditions BELOW the line entry —
/// the line's own chain already gates the whole line), and the entry's VARYING counters (§13.18.64).</summary>
public sealed class ReportFieldModel
{
    public required IReadOnlyList<ReportColumnSpec> Columns { get; init; }
    public required DataItem PrintItem { get; init; }

    /// <summary>The WRITTEN report group description entry this field is a placement of — one entry for every
    /// replay of a repeating entry. The arrangement rules of the line (§13.18.14.3 SR7, SR8) are about entries as
    /// written, so they report once per pair of entries and skip two placements of the same one.</summary>
    public CobolParserCore.ReportGroupEntryContext? Entry { get; init; }

    /// <summary>⛔ THE VALUE / SOURCE OPERAND LIST — ONE ENTRY PER WRITTEN OPERAND, NEVER A SCALAR (kb/Work
    /// PB506). ISO §13.18.63.2 format 4 is <c>{ literal-1 } …</c> and §13.18.53.2 is
    /// <c>{ identifier-1 / arithmetic-expression-1 } …</c>: BOTH clauses take an operand LIST, and the standard
    /// then writes the same two rules twice — §13.18.63.3 SR35 / §13.18.53.3 SR6 (a multi-operand clause
    /// requires a repeating entry and an operand count that matches its repetitions) and §13.18.63.4 GR23 /
    /// §13.18.53.4 GR4 ("successive operands are assigned to successive repeating printable items, horizontally
    /// and then vertically, as applicable, in that hierarchy. If no further operands remain, assignment begins
    /// again from the first operand"). A single-operand clause is a one-element list, so the cycling rule below
    /// is the ONE reader for both shapes and the model cannot answer "which operand prints in repetition n"
    /// two different ways.</summary>
    public required IReadOnlyList<ReportFieldSource> Sources { get; init; }

    /// <summary>The entry carries a GROUP INDICATE clause — ISO §13.18.28.4 GR1: "the same effect as a PRESENT
    /// WHEN clause" whose condition is the report engine's per-detail-group state, so the emitter ANDs it into
    /// this item's presence test beside <see cref="PresentWhen"/> (kb/Work PB1244).</summary>
    public bool GroupIndicate { get; init; }

    /// <summary>The operand that supplies repetition <paramref name="rep"/> (0-based) — ISO §13.18.63.4 GR23 /
    /// §13.18.53.4 GR4: successive operands to successive repeating printable items, wrapping to the first when
    /// no further operands remain. GR23's last sentence ("If any of the printable items are suppressed … VALUE
    /// operands are nevertheless assigned to them") is honoured BY CONSTRUCTION: the index is the repetition
    /// ORDINAL, never a count of the items actually placed, so a PRESENT WHEN that suppresses the entry cannot
    /// shift the assignment.</summary>
    public ReportFieldSource SourceAt(int rep) => Sources[rep % Sources.Count];

    /// <summary>PRESENT WHEN conditions strictly below the line entry, down to this entry (§13.18.41.4 GR2b).</summary>
    public List<CobolParserCore.ConditionContext> PresentWhenCtxs { get; } = [];
    public List<BoundCondition> PresentWhen { get; } = [];

    /// <summary>The VARYING counters in scope at this field (§13.18.64.3 SR2) — those of its own entry and of every
    /// entry above it, outermost first, each with the occurrence of its declaring entry the field lies in. Empty
    /// outside any VARYING entry.</summary>
    public List<ReportVaryingUse> Varyings { get; } = [];

    /// <summary>The OCCURS … DEPENDING presence tests this placement inherits, outermost repeating entry first
    /// (ISO §13.18.38.4 GR13 / §13.18.63.4 GR22) — empty unless the item lies inside a repeating entry with the
    /// DEPENDING phrase. A nested repetition contributes one guard per enclosing level; all shall hold.</summary>
    public List<ReportRepetitionGuard> RepetitionGuards { get; } = [];

    /// <summary>
    /// How many PLACEMENTS of this entry precede this field's first one — the ordinal of its first repeating
    /// printable item, so placement <c>j</c> of this field is repetition <c>RepetitionOrdinal + j</c> of the
    /// entry. An entry repeats through a multiple COLUMN clause (the operands of ONE field) and/or through an
    /// OCCURS clause (§13.18.38 Format 3 — a REPLAY producing one field per repetition), and the two compose, so
    /// the ordinal has to be counted per entry rather than read off either vehicle.
    /// <para>It is what ISO §13.18.64.4 GR3 counts ("For the first occurrence, the value of
    /// arithmetic-expression-1 is moved to data-name-1 … For the second and subsequent occurrences, the value of
    /// arithmetic-expression-2 is added"), and what §13.18.63.4 GR23 counts when a VALUE clause has more than one
    /// operand ("successive operands are assigned to successive repeating printable items").</para>
    /// </summary>
    public int RepetitionOrdinal { get; init; }
}

/// <summary>A printable item's value source, by clause kind.</summary>
public abstract record ReportFieldSource;

/// <summary>ONE format-4 VALUE clause operand (raw operand text — figurative word or literal; ISO §13.18.63.2
/// format 4). A multi-operand clause contributes one of these PER OPERAND to
/// <see cref="ReportFieldModel.Sources"/> — the raw text of the operands is never glued together.</summary>
public sealed record FieldValueSource(string Raw) : ReportFieldSource;

/// <summary>A SOURCE clause <b>identifier-1</b> (ISO §13.18.53.2), kept as the WRITTEN REFERENCE and bound in the
/// PROCEDURE phase through the ONE operand binder a MOVE's sending operand takes (<c>ExpressionBinder.FieldOperand</c>)
/// — which is §13.18.53.4 GR1's "sending operand of an implicit MOVE statement" said as code. That one binder is the
/// only place that knows every shape an identifier-1 can have: a subscript, a reference-modification, a LINE-COUNTER
/// or PAGE-COUNTER of this or another report (§8.4.2.2.3 SR9/SR10), a sum counter of the current report (§13.18.53.3
/// SR4), a VARYING counter in scope (§13.18.64.3 SR2) and a constant-name (§13.10.3 SR2). (kb/Work PB1292 × PB1306 ×
/// PB1316 × PB1456 — before it the identifier arm captured name + qualifiers at data bind and looked the name up in
/// ordinary storage, so each of those shapes was a separate staged refusal of legal source.)
/// <para><see cref="Value"/> is null until that bind; a screen that already named its rule sets
/// <see cref="Rejected"/>, so the rejected words are never bound or reported twice (the
/// <see cref="FieldComputeSource.Rejected"/> discipline).</para></summary>
public sealed record FieldReferenceSource(Core.DataReferenceContext Ref, string Written) : ReportFieldSource
{
    /// <summary>The bound sending operand (procedure phase).</summary>
    public BoundOperand? Value { get; set; }

    /// <summary>A screen has already rejected this operand and named its rule.</summary>
    public bool Rejected { get; set; }
}

/// <summary>A SOURCE clause operand that is an <b>arithmetic-expression-1</b> (ISO §13.18.53.2), or an
/// identifier-1 written WITH the ROUNDED phrase — §13.18.53.3 SR5: "If identifier-1 is specified with the
/// ROUNDED phrase, it is considered to be an arithmetic-expression." Its rule is §13.18.53.4 GR2:
/// "Arithmetic-expression-1 specifies the operand of an implicit COMPUTE statement that is executed implicitly
/// whenever the associated item is printed. If the ROUNDED phrase is specified, the implicit COMPUTE statement
/// has the corresponding ROUNDED phrase." (kb/Work PB852.)
/// <para>The operand is kept as its PARSE TREE and bound in the PROCEDURE phase through the ONE expression
/// binder — the same route the SUM addend takes since kb/Work PB482, and for the same reason: a subscript may
/// be an index-name or an expression and has no value at data bind.</para></summary>
public sealed record FieldComputeSource(Core.ReportValueOperandContext Ctx, string Written) : ReportFieldSource
{
    /// <summary>The clause's ROUNDED phrase (§13.18.53.2's trailing <c>[ rounded-phrase ]</c>, §14.7.4), or null
    /// — in which case GR2's implicit COMPUTE has no ROUNDED phrase and the transfer truncates (§14.7.4.3 r2).</summary>
    public Core.RoundedPhraseContext? Rounded { get; init; }

    /// <summary>The bound expression (procedure phase); null when a screen rejected the operand.</summary>
    public BoundExpr? Value { get; set; }

    /// <summary>A screen has already rejected this operand and named its rule: no binding, no emission, and no
    /// second diagnostic about the same words (the <c>ReportSumAddend.Rejected</c> discipline).</summary>
    public bool Rejected { get; set; }

    /// <summary>The rounding mode the ROUNDED phrase selects (§14.7.4.3), resolved in the procedure phase
    /// through the ONE <c>ExpressionBinder.RoundingOf</c>.</summary>
    public CobolNet.Runtime.CobolRounding Rounding { get; set; } = CobolNet.Runtime.CobolRounding.Truncation;
}

/// <summary>The printable face of a SUM entry (ISO §13.18.54.4 GR4 — the sum counter acts as the source item).
/// <paramref name="CounterId"/> is the counter's ENTRY ORDINAL (GR1; <see cref="ReportSumModel.Id"/>), never a
/// data-name — two entries may legally carry the same one (kb/Work PB882).</summary>
public sealed record FieldSumSource(int CounterId) : ReportFieldSource;

/// <summary>ONE SUM addend written as <c>identifier-1</c> (ISO §13.18.54.3 SR1 — "Each data-name-1,
/// identifier-1 or arithmetic-expression-1 is an addend"), kept as the WRITTEN REFERENCE rather than a resolved
/// item. §8.4.3.1.2 Format 2 makes an identifier a <i>qualified-data-name-with-subscripts</i>, so the subscript
/// is part of the reference and can be evaluated only where the ordinary identifier machinery lives — the
/// procedure phase. <see cref="Ctx"/> is bound there through the ONE expression binder into <see cref="Value"/>;
/// <see cref="Name"/>/<see cref="Qualifiers"/> answer the DATA-phase question (which arm of SR4/SR5 this is)
/// and <see cref="Item"/> is what that lookup found.
/// <para>⛔ Before kb/Work PB482 the addend was captured by <c>KeyReference</c> — base word + qualifiers, with
/// the subscript and the reference modification dropped on the floor — so <c>SUM WS-CELL(2)</c> compiled and
/// then ABORTED at run time in the addend delegate, and <c>SUM WS-TXT(1:2)</c> silently summed the whole
/// item.</para></summary>
public sealed class ReportSumAddend
{
    /// <summary>The operand's parse tree — a <c>reportValueOperand</c>, i.e. an arithmetic expression whose
    /// degenerate case is the bare identifier form. Bound through the ONE <c>ExpressionBinder.BindExpr</c>.</summary>
    public required CobolParserCore.ReportValueOperandContext Ctx { get; init; }

    /// <summary>The bare <c>dataReference</c> this operand IS, when it is written as data-name-1 / identifier-1
    /// (ISO §13.18.54.3 SR1's first two addend forms); null when the operand is an arithmetic-expression-1, whose
    /// rules are SR6's rather than SR4/SR5's (kb/Work PB883).</summary>
    public CobolParserCore.DataReferenceContext? Reference { get; init; }

    /// <summary>True when the addend is written as arithmetic-expression-1 (SR1's third form) — the
    /// §13.18.54.4 GR3 COMPUTE-with-ON-SIZE-ERROR accumulation rather than GR3's ADD.</summary>
    public bool IsExpression => Reference is null;

    public required string Name { get; init; }
    public required IReadOnlyList<string> Qualifiers { get; init; }
    /// <summary>The operand exactly as written — what every diagnostic about it quotes.</summary>
    public required string Written { get; init; }
    /// <summary>The item the base name resolves to (the SR5 screen's subject); null when the addend was
    /// rejected or staged, in which case <see cref="Value"/> stays null and the emitter stays loud.</summary>
    public DataItem? Item { get; set; }
    /// <summary>A screen has already rejected this operand and named its rule: no resolution, no binding, no
    /// emission, and above all NO SECOND DIAGNOSTIC about the same words.</summary>
    public bool Rejected { get; set; }
    /// <summary>The addend's value, bound in the procedure phase through <c>ExpressionBinder.BindExpr</c> — the
    /// same route a procedure-division identifier takes, so subscripts (literal, index-name or expression) and
    /// qualification are resolved by the ONE machinery. Null when a screen already rejected the operand.</summary>
    public BoundExpr? Value { get; set; }

    /// <summary>⛔ THIS ADDEND IS DATA-NAME-1 — A ROLLED TOTAL (ISO §13.18.54.3 SR4, kb/Work PB1294): the name of an
    /// entry in the report section, whose value §13.18.54.4 GR6 adds when ITS group is processed (GR7 a)/b)) rather
    /// than when a GENERATE is executed, so it has no <see cref="Value"/> of its own to bind and the emitter's
    /// GENERATE-driven addition leaves it out. Set by the one resolution (<c>ResolveSumAddend</c>).</summary>
    public bool Rolled { get; set; }

    /// <summary>The occurrences of data-name-1 this addend adds into THE COUNTER OCCURRENCE THAT OWNS IT — §13.18.54.4
    /// GR8: "each occurrence of the addend is added into the corresponding occurrence of the sum counter" when both
    /// have the same number of levels of repetition, and when the addend has more, "forming the total of a complete
    /// table of occurrences of the addend at one or more levels into each occurrence of the sum counter". One
    /// addition per occurrence listed; a non-repeating addend is the one occurrence of every counter occurrence.</summary>
    public List<ReportItemOccurrence> RolledFrom { get; } = [];
}

/// <summary>ONE <c>SUM OF addend… [UPON data-name-2…]</c> group of a SUM clause (ISO §13.18.54.2 — the general
/// format's outer brace repeats, and §13.18.54.3 SR1 says so: "The whole clause is referred to as a SUM clause
/// even though the SUM keyword may appear more than once"). The UPON phrase belongs to ITS group: §13.18.54.4
/// GR7c2 adds an addend "whenever any GENERATE statement is executed for a detail referenced by the UPON
/// phrase", so two groups with different UPON lists accumulate on different GENERATEs into the ONE counter
/// §13.18.54.4 GR1 gives the entry.</summary>
public sealed class ReportSumTerm
{
    public List<ReportSumAddend> Addends { get; } = [];
    /// <summary>The group's UPON operands (GR7c2); empty = no UPON phrase (GR7c1 — every GENERATE).</summary>
    public List<ReportDetailRef> Upon { get; } = [];
}

/// <summary>An <c>UPON data-name-2</c> operand as written (ISO §13.18.54.3 SR7: "Data-name-2 shall be the name
/// of a detail. It may be qualified only by a report-name"). The qualifier is a REPORT-name — §8.4.2.2.2
/// Format 1's file-report-qualifier, the same one <c>GENERATE data-name OF report-name</c> writes — so the
/// operand resolves through the ONE report-group funnel, <see cref="ReportGroupResolution"/>, never by a bare
/// name scan. <see cref="Detail"/> is null until that resolution succeeds; a rejected operand stays null and
/// contributes no run-time filter entry.</summary>
public sealed record ReportDetailRef(string Name, string? Qualifier)
{
    public ReportGroupModel? Detail { get; set; }

    /// <summary>The report description the resolved <see cref="Detail"/> belongs to — the RD of the SUM entry itself
    /// for an unqualified operand, ANOTHER report's when SR7's report-name qualifier says so (§13.18.54.4 GR7 c) 2):
    /// the GENERATE that adds is then executed against that report's engine).</summary>
    public ReportModel? Owner { get; set; }

    /// <summary>The operand as written — the form every diagnostic about it quotes.</summary>
    public override string ToString() => Qualifier is null ? Name : $"{Name} OF {Qualifier}";
}

/// <summary>
/// ⛔ ONE WRITTEN REPORT ENTRY THAT CARRIES A VALUE, AND THE GEOMETRY OF ITS REPETITION (kb/Work PB1294). ISO
/// §13.18.54.4 GR6 gives the three kinds of entry a SUM clause's data-name-1 can name one value each — an entry that
/// contains a SUM clause is "the corresponding sum counter", and one that has a SOURCE or VALUE clause is "the operand
/// of the SOURCE or VALUE clause" — so the entry is the unit a rolled total is resolved against, whether it prints
/// (a COLUMN clause) or not (§13.18.53.4 GR3: "the SOURCE clause causes no action, except where the entry is referred
/// to by means of a SUM clause").
/// <para>Its REPETITION geometry is the one a sum counter always had (<see cref="ReportSumFamily"/>, kb/Work PB1271):
/// §13.15.4 GR3's three vehicles — an OCCURS clause, a multiple LINE clause (§13.18.35.4 GR9) and a multiple COLUMN
/// clause (§13.18.14.4 GR12) — are all OCCURS levels, so an entry's occurrences form a table of
/// <see cref="Extents"/>, outermost level first. §13.18.54.4 GR8 maps an addend's occurrences onto a counter's by
/// those levels, which is why both kinds of entry carry them.</para>
/// </summary>
public abstract class ReportEntryFamily
{
    /// <summary>The entry's data-name — for a SUM entry GR5's name of the counter, "not the name of the associated
    /// printable item, if any" — or null for an unnamed entry (such an entry can be named by no clause).</summary>
    public string? Name { get; init; }

    /// <summary>The report description the entry is written in.</summary>
    public required ReportModel Report { get; init; }

    /// <summary>The report group description (the 01-level entry and everything under it) the entry is part of —
    /// §13.18.54.3 SR4 b)/c) ask whether data-name-1 and the subject share one, and SR4 f) asks its TYPE.</summary>
    public required ReportGroupModel Group { get; init; }

    /// <summary>The enclosing repeating entries' OCCURS clauses, outermost first (§13.18.38 Format 3, or a multiple
    /// LINE clause's §13.18.35.4 GR9 equivalent) — one table level each. They also answer what a table(ALL)
    /// argument asks of a level's CURRENT range: §15.3, "If the ALL subscript is associated with a data item
    /// described with an OCCURS DEPENDING ON clause, the range of values is determined by the object of the OCCURS
    /// DEPENDING ON clause", read by §13.18.38.4 GR13's own count (<see cref="Model.AllCount.ReportDepending"/>).</summary>
    public required IReadOnlyList<ReportOccursSpec> Repetitions { get; init; }

    /// <summary>The operand count of the entry's own multiple COLUMN clause — the innermost level — or 1 when the
    /// entry has no multiple COLUMN clause (§13.18.14.3 SR10 a) keeps an OCCURS clause out of such an entry).</summary>
    public required int Columns { get; init; }

    /// <summary>The occurrence count of each level, outermost first: each of <see cref="Repetitions"/>' integer-2,
    /// then <see cref="Columns"/> when it is a level. Empty for a non-repeating entry: one occurrence. Derived, so the
    /// levels, a sum register's synthetic OCCURS chain and the occurrence arithmetic read one answer.</summary>
    public IReadOnlyList<int> Extents =>
        _extents ??= [.. Repetitions.Select(r => r.Max), .. Columns > 1 ? [Columns] : (int[])[]];
    private IReadOnlyList<int>? _extents;

    /// <summary>How many occurrences the entry has — the product of <see cref="Extents"/> (1 for none).</summary>
    public int Count => Extents.Aggregate(1, (n, e) => n * e);

    /// <summary>⛔ THE ENTRY'S QUALIFICATION HIERARCHY (kb/Work PB1454): the data-names of the report group
    /// description entries it is subordinate to, INNERMOST FIRST and ending at the 01 report group, a null for an
    /// entry without a data-name (FILLER). ISO §8.4.2.2.3 SR4 — "Each data-name-2 shall be the name associated with a
    /// level number to which the item being qualified is subordinate" — so an entry is qualifiable by every named
    /// level above it, and by its REPORT as the outermost container (§8.4.2.2.2 Format 1's file-report-qualifier).
    /// Read by <see cref="DataBinder.QualifierWalk"/>, the ONE qualifier walk a data item's own ancestors go through.</summary>
    public IReadOnlyList<string?> Qualification { get; init; } = [];

    /// <summary>The entry's occurrences, one per replay of it and per operand of its own multiple COLUMN clause, in
    /// binding order (which is NOT row-major order: the replay binds siblings between repetitions).</summary>
    public List<ReportItemOccurrence> Occurrences { get; } = [];

    /// <summary>The row-major linear coordinate of <paramref name="coordinates"/> (zero-based, one per
    /// <see cref="Extents"/> level, outermost first) — the order a subscript list is written in (§8.4.2.3.3 SR3,
    /// "successively less inclusive dimensions"), and the arithmetic the engine's <c>CobolReport.SumOccurrence</c>
    /// does for a run-time subscript.</summary>
    public int Linear(IReadOnlyList<int> coordinates)
    {
        int linear = 0;
        for (int k = 0; k < Extents.Count; k++) linear = linear * Extents[k] + coordinates[k];
        return linear;
    }

    /// <summary>The inverse of <see cref="Linear"/>: the zero-based coordinates of occurrence <paramref name="linear"/>.</summary>
    public int[] CoordinatesOf(int linear)
    {
        var coordinates = new int[Extents.Count];
        for (int k = Extents.Count - 1; k >= 0; k--)
        {
            coordinates[k] = linear % Extents[k];
            linear /= Extents[k];
        }
        return coordinates;
    }
}

/// <summary>An entry that carries a SOURCE or VALUE clause and no SUM clause (ISO §13.18.54.4 GR6 — "the value added
/// is that of the operand of the SOURCE or VALUE clause"), printable or not.
/// <see cref="Category"/> is its PICTURE's category, which SR4's "numeric data item" asks.</summary>
public sealed class ReportSourceFamily : ReportEntryFamily
{
    /// <summary>The category of the entry's PICTURE; null when it has none or none that analyzed.</summary>
    public PicCategory? Category { get; set; }
}

/// <summary>
/// ⛔ ONE OCCURRENCE OF A VALUE-CARRYING REPORT ENTRY — what one rolled addition adds (kb/Work PB1294). It is the
/// entry's counter occurrence (<see cref="Sum"/>), or the operand of its SOURCE or VALUE clause for this occurrence
/// (<see cref="Source"/> — §13.18.63.4 GR23 / §13.18.53.4 GR4: successive operands to successive repeating items, so
/// occurrence n takes operand n mod count), and it carries the FULL presence chain §13.18.54.4 GR11 asks ("declared
/// to be absent as a result of a PRESENT WHEN clause or an OCCURS clause with the DEPENDING phrase"): every condition
/// from the 01 entry down to the entry, and this replay's DEPENDING tests.
/// </summary>
public sealed class ReportItemOccurrence
{
    public required ReportEntryFamily Family { get; init; }

    /// <summary>Zero-based, one per <see cref="ReportEntryFamily.Extents"/> level, outermost first.</summary>
    public required IReadOnlyList<int> Coordinates { get; init; }

    /// <summary>The counter occurrence, when the entry contains a SUM clause.</summary>
    public ReportSumModel? Sum { get; init; }

    /// <summary>The operand of the entry's SOURCE or VALUE clause for this occurrence, bound in the procedure phase
    /// with the rest of the report's clause operands; null for a SUM entry.</summary>
    public ReportFieldSource? Source { get; init; }

    /// <summary>The VARYING counters in scope at the entry (§13.18.64.3 SR2) — what its SOURCE operand may name.</summary>
    public List<ReportVaryingUse> Varyings { get; } = [];

    /// <summary>The SOURCE operand names one of those counters — whose value is the entry's own occurrence's
    /// (§13.18.64.4 GR3: FROM for the first, BY added for each later one), a compose-local when the entry prints. The
    /// emitter gives a rolled addition of such an operand the same locals for the occurrence it adds.</summary>
    public bool VaryingDependent { get; set; }

    // A SUM entry's chain IS its counter occurrence's (the same lists, so the engine's reset and a rolled addition
    // read one answer); a SOURCE / VALUE entry owns its own.
    private readonly List<CobolParserCore.ConditionContext> _presentWhenCtxs = [];
    private readonly List<BoundCondition> _presentWhen = [];
    private readonly List<ReportRepetitionGuard> _guards = [];

    /// <summary>The PRESENT WHEN conditions 01 → entry as captured parse contexts (§13.18.41.4 GR2b).</summary>
    public List<CobolParserCore.ConditionContext> PresentWhenCtxs => Sum?.PresentWhenCtxs ?? _presentWhenCtxs;

    /// <summary>The same chain bound in the procedure phase.</summary>
    public List<BoundCondition> PresentWhen => Sum?.PresentWhen ?? _presentWhen;

    /// <summary>This replay's OCCURS … DEPENDING presence tests (§13.18.38.4 GR13), outermost repeating entry first.</summary>
    public List<ReportRepetitionGuard> RepetitionGuards => Sum?.RepetitionGuards ?? _guards;

    /// <summary>The value one rolled addition adds, bound in the procedure phase: the counter occurrence's content, or
    /// the SOURCE / VALUE operand as a number. Null when the operand was refused (reported where it was).</summary>
    public BoundExpr? Value { get; set; }
}

/// <summary>
/// ⛔ ONE SUM ENTRY'S COUNTERS, AND THEY ARE A TABLE (kb/Work PB1271). ISO §13.18.54.4 GR1 gives "each entry
/// containing a SUM clause" an independent sum counter, and when that entry is a REPEATING entry the counter
/// repeats with it: GR8 a) adds each occurrence of a repeating addend "into the corresponding occurrence of the sum
/// counter", and GR10 suppresses the print and the reset of "the corresponding sum counter" of an absent
/// occurrence. The repetition vehicles are §13.15.4 GR3's three — an OCCURS clause (§13.18.38 Format 3), a multiple
/// LINE clause (§13.18.35.4 GR9: "functionally equivalent to … a simple OCCURS clause") and a multiple COLUMN clause
/// (§13.18.14.4 GR12: the same sentence) — and every one of them is an OCCURS level, so the counter's occurrences
/// form a table of <see cref="Extents"/>, one level per enclosing repetition, outermost first, plus, innermost,
/// the entry's own multiple COLUMN clause.
/// <para>So a procedure division reference subscripts it like any table element (§8.4.2.3.3 SR3 — one subscript
/// per OCCURS level; SR5 — "Each table element reference shall be subscripted"), which is why the family's
/// <see cref="Register"/> carries a synthetic ancestor chain with one <see cref="DataItem.Occurs"/> per extent: the
/// ordinary arity screen, <see cref="DataItem.SubscriptLevels"/> and the table(ALL) argument read it unchanged.
/// The single-register model this replaced shared ONE counter among a multiple COLUMN entry's printable items and
/// refused every subscripted reference "not defined".</para>
/// <para>The counter ids of a family are one CONTIGUOUS block, row-major over <see cref="Extents"/>, reserved when
/// the family is first seen: an occurrence's id is <see cref="BaseId"/> + its linear coordinate. Contiguity is
/// what lets a run-time subscript select the counter by arithmetic — the replay binds sibling entries between the
/// repetitions (A0 B0 A1 B1), so the ids could not be contiguous in binding order.</para>
/// </summary>
public sealed class ReportSumFamily : ReportEntryFamily
{
    /// <summary>The id of occurrence (1, 1, …): the first counter of this family's block.</summary>
    public required int BaseId { get; init; }

    /// <summary>The IMPLICITLY-DEFINED data item the counter is (ISO §13.18.54.4 GR1 — "a conceptual data item
    /// that behaves as a data item of the category numeric"), carrying the GR1 profile
    /// (<see cref="PicInfo.SumCounterItem"/>) and, for a repeating entry, the synthetic OCCURS chain of
    /// <see cref="Extents"/>. It is engine state, not storage — kept off <c>DataBinder.ByName</c>/<c>Roots</c> and
    /// reachable only through <c>DataBinder.SumCounters</c>, the resolver hook that builds a
    /// <see cref="Model.ReportSumCounterPlace"/> (the CAPACITY-register pattern). Every occurrence shares it: they
    /// are occurrences of ONE description.</summary>
    public required DataItem Register { get; init; }

    /// <summary>The counter's scale — the fractional digit count GR1 derives from the entry's PICTURE.</summary>
    public int Scale { get; init; }

    /// <summary>The COBOL-2002 PICTURE-shape introduction gate (a <c>Constructs.*</c> id) this counter's PICTURE
    /// carries — a floating-point numeric-edited (symbol E) or national-edited picture, from the ONE
    /// <c>VersionConformancePass.PictureConstructId</c>. The SUM-counter scale-derivation <c>Analyze</c> (GR1) is a
    /// DISTINCT call off <c>ConformanceForest</c>, so this preserves its gate for the post-bind
    /// <c>VersionConformancePass</c> GateData report-Sums walk (DEVLOG 740; else the 0900 below 2002 is dropped on
    /// this error path). Null when the picture is version-invariant (the normal numeric case).</summary>
    public string? SkeletonGate { get; init; }
    /// <summary>The exact where-string the SUM-counter <c>Analyze</c> used (<c>RD '…' SUM counter '…'</c>) — replayed
    /// verbatim by GateData when <see cref="SkeletonGate"/> fires, so the 0900 is byte-identical to the former site.</summary>
    public string SkeletonWhere { get; init; } = "";

    /// <summary>The id of the occurrence at <paramref name="coordinates"/> (zero-based, outermost first, one per
    /// <see cref="Extents"/> level): <see cref="BaseId"/> + the row-major linear coordinate — the same order a
    /// subscript list is written in (§8.4.2.3.3 SR3, "successively less inclusive dimensions"), and the same
    /// arithmetic the engine's <c>CobolReport.SumOccurrence</c> does for a run-time subscript.</summary>
    public int IdAt(IReadOnlyList<int> coordinates) => BaseId + Linear(coordinates);
}

/// <summary>One SUM counter — one OCCURRENCE of a <see cref="ReportSumFamily"/> (ISO §13.18.54): its identity
/// (GR1 — see <see cref="Id"/>), the addend TERMS (SR5's items OUTSIDE the report section, and SR4's data-name-1
/// rolled totals, which register with their addend's own group — <see cref="ReportSumAddend.RolledFrom"/>), each
/// carrying its own UPON detail names (GR7c2), the RESET operand
/// (GR2), and the presence tests that suppress its print and reset (GR10).</summary>
public sealed class ReportSumModel
{
    /// <summary>⛔ THE COUNTER'S IDENTITY IS THE ENTRY'S OCCURRENCE, NEVER ITS SPELLING (kb/Work PB882, PB1271).
    /// ISO §13.18.54.4 GR1: "Each entry containing a SUM clause establishes an independent sum counter and size
    /// error indicator", and a repeating entry establishes one per occurrence (<see cref="ReportSumFamily"/>). This
    /// is the occurrence's id within its report description — <see cref="ReportSumFamily.IdAt"/>, the index of this
    /// model in <see cref="ReportModel.Sums"/> and, at run time, of its counter in the engine's list. It used to be
    /// the entry's data-name, which made two entries that legally share a data-name share ONE counter: the second
    /// registration overwrote the first and both printable faces rendered the second total.</summary>
    public required int Id { get; init; }

    /// <summary>The SUM ENTRY this counter is an occurrence of — its name, its register, its scale and its
    /// qualification hierarchy are the entry's, shared by every occurrence.</summary>
    public required ReportSumFamily Family { get; init; }

    /// <summary>The clause's <c>SUM … [UPON …]</c> groups in written order — ONE counter per ENTRY (GR1),
    /// however many times the SUM keyword appears (SR1).</summary>
    public List<ReportSumTerm> Terms { get; } = [];

    /// <summary>The RESET ON operand as written (§13.18.54.3 SR8 — data-name-3 "may be qualified and
    /// reference-modified"; it "shall be an operand of the CONTROL clause of the current report description").
    /// Null for RESET ON FINAL and for no RESET phrase.</summary>
    public ReportControlRef? ResetOperand { get; set; }
    public bool ResetFinal { get; set; }

    /// <summary>The clause's ROUNDED phrase (§13.18.54.2's trailing <c>[ rounded-phrase ]</c>, §14.7.4), or null.
    /// §13.18.54.4 GR4: "If the ROUNDED phrase is specified …, the content of the sum counter is computed
    /// according to the general rules for the COMPUTE statement with the ROUNDED phrase" — the phrase governs the
    /// counter's delivery to the printable item, which is why §13.18.54.3 SR3 admits it only with a COLUMN
    /// clause. Null (no phrase) ⇒ the transfer truncates (§14.7.4.3 r2).</summary>
    public Core.RoundedPhraseContext? Rounded { get; set; }

    /// <summary>The rounding mode <see cref="Rounded"/> selects (§14.7.4.3), resolved in the procedure phase
    /// through the ONE <c>ExpressionBinder.RoundingOf</c>.</summary>
    public CobolNet.Runtime.CobolRounding Rounding { get; set; } = CobolNet.Runtime.CobolRounding.Truncation;
    /// <summary>The resolved RESET control level; −1 = no RESET phrase (reset where printed, GR2).</summary>
    public int ResetLevel { get; set; } = -1;
    /// <summary>The group whose processing end resets the counter when no RESET phrase is given (GR2).</summary>
    public required ReportGroupModel PrintedIn { get; init; }

    /// <summary>The SUM entry's FULL PRESENT WHEN chain (01 → entry). When any condition is false at a group
    /// presentation the counter is neither printed nor reset for that instance (ISO §13.18.41.4 GR3g /
    /// §13.18.54.4 GR10) — the engine consults the AND of these per presentation.</summary>
    public List<CobolParserCore.ConditionContext> PresentWhenCtxs { get; } = [];
    public List<BoundCondition> PresentWhen { get; } = [];

    /// <summary>⛔ GR10's OTHER HALF — the OCCURS … DEPENDING presence tests of THIS OCCURRENCE (ISO §13.18.38.4
    /// GR13), outermost repeating entry first. §13.18.54.4 GR10: "If the entry is associated with an absent data item
    /// as a result of a PRESENT WHEN clause or an OCCURS clause with the DEPENDING phrase, the corresponding sum
    /// counter is not printed and is not reset to zero for the current instance of the report group." The PRESENT
    /// WHEN half is <see cref="PresentWhen"/>; an occurrence the DEPENDING count excludes is absent exactly as its
    /// printable item is (<see cref="ReportFieldModel.RepetitionGuards"/>), and the two are ANDed into the one
    /// presence slot the engine's reset reads (kb/Work PB1271).</summary>
    public List<ReportRepetitionGuard> RepetitionGuards { get; } = [];
}

public sealed partial class DataBinder
{
    /// <summary>The program unit's report description entries, in source order (ISO §13.6 REPORT SECTION).
    /// (READ-ONLY view — P6 Step 5.)</summary>
    public IReadOnlyList<ReportModel> Reports => _reports;
    private readonly List<ReportModel> _reports = [];

    /// <summary>⛔ THE REPORTS A PROCEDURE DIVISION REFERENCE CAN NAME — this unit's own report description
    /// entries, then the GLOBAL ones of its containers, nearest container first (ISO §13.18.27.4 GR1/GR2; kb/Work
    /// PB369). Every NAME resolution (a report-name, a report group, a sum counter, a LINE-/PAGE-COUNTER
    /// qualifier, a USE BEFORE REPORTING operand) reads THIS list; <see cref="Reports"/> stays the list of reports
    /// this unit DECLARES — the ones it constructs, owns the storage of, and validates. A container's report is
    /// never re-declared here: the entry is the container's own <see cref="ReportModel"/>, reached at run time
    /// through the <c>__outer</c> chain <see cref="ReportDepth"/> measures.</summary>
    public IReadOnlyList<ReportModel> VisibleReports => _inheritedReports.Count == 0 ? _reports : _visibleReports;
    private readonly List<ReportModel> _visibleReports = [];
    private readonly Dictionary<ReportModel, int> _inheritedReports = new(ReferenceEqualityComparer.Instance);

    /// <summary>How many containment levels out the storage of <paramref name="report"/> lives: 0 for a report this
    /// unit declares, n for a GLOBAL report inherited from its n-th container (the length of the <c>__outer</c>
    /// chain a reference renders through).</summary>
    public int ReportDepth(ReportModel report) => _inheritedReports.TryGetValue(report, out int depth) ? depth : 0;

    /// <summary>ISO §8.4.6.2.1 rule 3 — when a name is declared both in this source element and as a global name
    /// of a container (or in two containers), "the item in source element B is the referenced item", else the one
    /// in the nearest containing element: of <paramref name="candidates"/> keep those whose report lives at the
    /// smallest <see cref="ReportDepth"/>. The ONE application of the rule to a report-scoped name — a report
    /// group (<c>ReportGroupResolution.Resolve</c>), a sum counter (<c>ReferenceResolver.SumCounterFor</c>) and an
    /// unqualified LINE-/PAGE-COUNTER (<c>ReportWriterBinder.CounterReportOf</c>) — so ambiguity (§8.4.2.2.3 SR1)
    /// is asked only among candidates of one source element.</summary>
    internal List<T> NearestInScope<T>(IEnumerable<T> candidates, Func<T, ReportModel> reportOf)
    {
        var nearest = new List<T>();
        int best = int.MaxValue;
        foreach (var c in candidates)
        {
            int depth = ReportDepth(reportOf(c));
            if (depth < best) { nearest.Clear(); best = depth; }
            if (depth == best) nearest.Add(c);
        }
        return nearest;
    }

    /// <summary>Make a container's GLOBAL report visible here (ISO §13.18.27.4 GR1/GR2): its report-name, its
    /// report groups (reached through the report), and its sum counters (GR1 — "All data-names subordinate to a
    /// global name are global names"). Called by <c>BinderDriver.BindUnitData</c> AFTER this unit's own data
    /// division has bound, nearest container first, so a name this unit declares — or a nearer container already
    /// supplied — hides the farther one (§8.4.6.2). Returns false when the report-name is hidden.</summary>
    internal bool InheritGlobalReport(ReportModel report, int depth)
    {
        if (VisibleReports.Any(r => CobolNames.Same(r.Name, report.Name))) return false;
        if (_inheritedReports.Count == 0) _visibleReports.AddRange(_reports);
        _visibleReports.Add(report);
        _inheritedReports[report] = depth;
        foreach (var family in report.SumFamilies)
        {
            // A sum counter is a data-name subordinate to the report (GR1). A LOCAL declaration of the same name
            // hides it; a same-named counter of this unit's own reports is a homonym the report-name qualifier
            // resolves, exactly as between two reports of one program (kb/Work PB882).
            if (family.Name is not { } sn || ByName.ContainsKey(sn)) continue;
            if (!_sumCounters.TryGetValue(sn, out var homonyms)) _sumCounters[sn] = homonyms = [];
            homonyms.Add((report, family));
        }
        return true;
    }

    /// <summary>Bind the REPORT SECTION's RD entries into <see cref="Reports"/> (ISO §13.14/§13.15). Runs after
    /// <c>BindFileControl</c>/<c>BindFileSection</c> (the FD REPORT clauses are captured there); SOURCE/CONTROL
    /// data references resolve post-build in <see cref="ResolveReports"/> (the FILE STATUS pattern — one
    /// canonical resolution point).</summary>
    private void BindReportSection(Core.ProgramUnitContext program)
    {
        var rs = program.dataDivision()?.reportSection();
        if (rs is null) return;
        foreach (var rd in rs.reportDescriptionEntry())
        {
            using var _ = Edition.At(rd);
            if (rd.reportName()?.GetText() is not { } name) continue;
            DeclareUserWord(name, UserWordKind.ReportName);   // §8.3.2.2 — the one declaration funnel (kb/Work PB1083)
            var model = new ReportModel
            {
                Name = name,
                WrittenEntries = rd.reportGroupEntry(),
                CsIndex = Reports.Count,
                Uid = _uidCounter++,
                // §8.4.3.15.4 GR1 — the counter exists per REPORT, so its register is built here, once, with the
                // RD (kb/Work PB429). The SUM counter's Register is built the same way a few hundred lines below.
                PageCounterRegister = new DataItem
                {
                    Level = 49,
                    DeclaredAt = Edition.Cursor,
                    CobolName = "PAGE-COUNTER",
                    CsName = $"__pagectr_{Reports.Count}",
                    Pic = PicInfo.ReportCounterItem(),
                    Uid = _uidCounter++,
                },
            };
            BindReportDescriptionClauses(rd, model);
            BindReportGroups(rd, model);
            _reports.Add(model);
        }
    }

    /// <summary>⛔ THE ONE SCREEN OF ISO §13.18.60.3 SR7 — "Only the DISPLAY or NATIONAL phrase may be
    /// specified in any USAGE clause associated with a report group item" (kb/Work PB541). It is asked at the
    /// two points a usage becomes known and nowhere else: of the USAGE CLAUSE as it is captured (which is the
    /// rule's own subject, and the only place a GROUP entry's clause is visible), and of the usage a printable
    /// item SETTLES on, which may have been implied by its picture character-string (§13.18.60.4 GR7/GR8) and so
    /// never passed a clause. One rule, one message, two positions — never two spellings of the rule.
    /// <para>The gate this replaced tested <c>Usage is not Display</c>, one alternative narrower than the rule,
    /// and refused every legal NATIONAL report item at every edition under a §13.15 citation that says nothing
    /// of the kind; the nearest real text, §13.18.14.4 GR3, is about the column/character correspondence.</para>
    /// </summary>
    private void ScreenReportUsage(Usage usage, string reportName, string? entryName, string? written)
    {
        if (usage is Usage.Display or Usage.National) return;
        Edition.Error(DiagnosticCatalog.ReportUsageNotDisplayOrNational, $"RD '{reportName}' entry "
            + $"'{entryName ?? "FILLER"}'{(written is null ? "" : $" ({written})")}: usage {usage} is not "
            + "admitted here — only the DISPLAY or NATIONAL phrase may be specified in any USAGE clause "
            + "associated with a report group item (ISO §13.18.60.3 SR7)");
    }

    /// <summary>Bind one RD entry's description clauses: PAGE geometry (§13.18.39) with the GR3 defaults,
    /// CONTROL (§13.18.16), GLOBAL (§13.18.27 on an RD); CODE (§13.18.12) stages loud.</summary>
    private void BindReportDescriptionClauses(Core.ReportDescriptionEntryContext rd, ReportModel model)
    {
        bool heading = false, firstDetail = false, lastControlHeading = false, lastDetail = false, footing = false;
        // §13.14.2 prints each clause in its own bracket with no ellipsis; §13.14.3 SR2 licenses any ORDER, never a
        // repeat (COBOLNET2423 — the grammar's `reportDescriptionClause*` admits both).
        var rdClauses = rd.reportDescriptionClause();
        string rdWhere = $"RD '{model.Name}'";
        UnrepeatedElements.AtMostOnce(Edition, rdClauses.Count(c => c.reportGlobalClause() is not null), rdWhere,
            "the GLOBAL clause", "13.14.2");
        UnrepeatedElements.AtMostOnce(Edition, rdClauses.Count(c => c.reportCodeClause() is not null), rdWhere,
            "the CODE clause", "13.14.2");
        UnrepeatedElements.AtMostOnce(Edition, rdClauses.Count(c => c.reportControlClause() is not null), rdWhere,
            "the CONTROL clause", "13.14.2");
        UnrepeatedElements.AtMostOnce(Edition, rdClauses.Count(c => c.reportPageClause() is not null), rdWhere,
            "the PAGE clause", "13.14.2");
        foreach (var clause in rdClauses)
        {
            // §13.18.27.3 SR1 e) — the report-name is a global name. The containment half (visibility in contained
            // programs, §13.18.27.4 GR1/GR2, and the declarative a contained GENERATE selects, §14.9.49.4 GR4) is
            // BinderDriver's inheritance walk and the emitter's GR4 selector (kb/Work PB369).
            if (clause.reportGlobalClause() is not null)
                model.IsGlobal = true;
            else if (clause.reportCodeClause() is { } code)
            {
                model.CodeWritten = true;
                model.Code = BindCodeClause(code, model);
            }
            else if (clause.reportControlClause() is { } ctl)
            {
                // Operand order IS the hierarchy, major→minor (§13.18.16.4 GR1); FINAL is the highest level (GR2).
                int clauseStart = model.Controls.Count;   // a repeated CONTROL clause is COBOLNET2423 above
                for (int i = 0; i < ctl.ChildCount; i++)
                    switch (ctl.GetChild(i))
                    {
                        case Antlr4.Runtime.Tree.ITerminalNode t when t.Symbol.Type == CobolLexer.FINAL:
                            // ⛔ THE GENERAL FORMAT, not a syntax rule (kb/Work PB483): §13.18.16.2 prints
                            // `FINAL [ data-name-1 ] …` — FINAL once, FIRST, the ellipsis on the data-name bracket
                            // alone (§5.2.7). The grammar's `(FINAL | dataReference)+` admits FINAL anywhere and
                            // any number of times; before this screen `CONTROLS ARE FINAL FINAL CX` built a second
                            // FINAL level that could never break, and `CONTROL CX FINAL` made FINAL the MINOR
                            // level — contradicting GR2 — so TERMINATE printed CF FINAL before CF CX.
                            if (model.Controls.Count > clauseStart)
                                Edition.Error(DiagnosticCatalog.ReportControlFinalPlacement, $"RD '{model.Name}': "
                                    + (model.Controls.Skip(clauseStart).Any(c => c.IsFinal)
                                        ? "FINAL is written more than once in the CONTROL clause"
                                        : $"FINAL is written after the data-name '{model.Controls[^1].Display}'")
                                    + " — ISO §13.18.16.2 admits FINAL once, as the first operand (`FINAL "
                                    + "[ data-name-1 ] …`, §5.2.7), and §13.18.16.4 GR2 makes it the highest level");
                            model.Controls.Add(new ReportControlModel { IsFinal = true });
                            break;
                        case Core.DataReferenceContext dref:
                            model.Controls.Add(new ReportControlModel
                            {
                                Operand = ControlOperandRef(dref, DiagnosticCatalog.ReportControlOperandShape,
                                    $"RD '{model.Name}': CONTROL operand", "ISO §13.18.16.3 SR4"),
                            });
                            break;
                    }
                // §13.18.16.3 SR6 — "Data-name-1 shall be unique in any given CONTROL clause." Uniqueness is of
                // the WRITTEN reference: SR6's own second sentence permits two operands to "refer to the same
                // physical data item or to overlapping data items", so the referenced ITEM cannot be what
                // distinguishes them, and SR10/SR8 could not name a level unambiguously if it were.
                for (int i = 1; i < model.Controls.Count; i++)
                    if (model.Controls[i].Operand is { } later)
                        for (int j = 0; j < i; j++)
                            if (model.Controls[j].Operand is { } earlier && earlier.SameOperandAs(later))
                            {
                                Edition.Error(DiagnosticCatalog.ReportControlOperandShape, $"RD '{model.Name}': CONTROL operand "
                                    + $"'{later}' appears twice: data-name-1 shall be unique in any given CONTROL "
                                    + "clause (ISO §13.18.16.3 SR6). Two operands may refer to the same item or to "
                                    + "overlapping data — write them as distinct references (a qualification or a "
                                    + "reference modification), never as the same one twice.");
                                break;
                            }
            }
            else if (clause.reportPageClause() is { } page)
            {
                // §13.18.39.4 GR2a — integer-1 is the page limit, and "If integer-1 is not specified, the report
                // consists of a single page of indefinite length": a PAGE clause that gives only integer-2 (the page
                // width, GR2b) leaves the report UNPAGED. GR5 — "If integer-2 is omitted, a value of 999 is assumed
                // for the page width": ReportModel.PageWidth already holds that default.
                // Every integer of the clause is a literal position, so an integer constant-name stands there
                // (§13.10.3 SR2) — read through the ONE reader (kb/Work PB1947); a refused constant reads as 1, the
                // recovery value no range rule below trips on (the compile has already failed).
                int? writtenLimit = null;   // null: not written, or refused (the compile has already failed)
                if (page.integerOperand() is { } limit)
                {
                    model.Paged = true;
                    writtenLimit = IntegerOperandValue(limit, rdWhere);
                    model.PageLimit = writtenLimit ?? RecoveredIntegerOperand;
                }
                if (page.reportPageWidth() is { } width)
                {
                    model.PageWidth = IntegerOperandValue(width.integerOperand(), rdWhere) ?? RecoveredIntegerOperand;
                    model.PageWidthWritten = true;
                }
                // §13.18.39.2 prints each phrase in its own bracket with no ellipsis; SR4 licenses any ORDER,
                // never a repeat (COBOLNET2423).
                var subs = page.reportPageSubclause();
                foreach (var phrase in Enum.GetValues<PagePhrase>())
                    UnrepeatedElements.AtMostOnce(Edition, subs.Count(s => PagePhraseOf(s) == phrase), rdWhere,
                        $"the PAGE clause's {PagePhraseWords(phrase)} phrase", "13.18.39.2");
                // §13.18.39.3 SR3 — "The HEADING, FIRST DETAIL, LAST CONTROL HEADING, LAST DETAIL, or FOOTING
                // phrase may be specified only if integer-1 is specified." Each phrase subdivides the page
                // (GR2c–GR2g) and a report with no page limit has no page to subdivide.
                if (page.integerOperand() is null && subs.Length > 0)
                    Edition.Error(DiagnosticCatalog.ReportPagePhraseWithoutLimit, $"{rdWhere}: the PAGE clause "
                        + $"specifies {string.Join(", ", subs.Select(s => PagePhraseWords(PagePhraseOf(s))).Distinct())} "
                        + "but no integer-1 (the page limit): those phrases may be specified only if integer-1 is "
                        + "specified (ISO §13.18.39.3 SR3)");
                var writtenPhrases = new List<(PagePhrase Phrase, int Value, Core.ReportPageSubclauseContext Ctx)>();
                foreach (var sub in subs)
                {
                    int? written = IntegerOperandValue(sub.integerOperand(), rdWhere);
                    int v = written ?? RecoveredIntegerOperand;
                    if (written is { } w) writtenPhrases.Add((PagePhraseOf(sub), w, sub));
                    switch (PagePhraseOf(sub))
                    {
                        case PagePhrase.Heading: model.Heading = v; heading = true; break;
                        case PagePhrase.FirstDetail: model.FirstDetail = v; firstDetail = true; break;
                        case PagePhrase.LastControlHeading: model.LastControlHeading = v; lastControlHeading = true; break;
                        case PagePhrase.LastDetail: model.LastDetail = v; lastDetail = true; break;
                        case PagePhrase.Footing: model.Footing = v; footing = true; break;
                    }
                }
                ScreenReportPageIntegers(model, page, writtenLimit, writtenPhrases);
            }
        }
        if (!model.Paged) return;
        // The §13.18.39.4 GR3 defaults:
        if (!heading) model.Heading = 1;                                              // GR3a
        if (!firstDetail) model.FirstDetail = model.Heading;                          // GR3b
        if (!lastDetail) model.LastDetail = footing ? model.Footing : model.PageLimit;   // GR3d
        if (!lastControlHeading)                                                      // GR3c
            model.LastControlHeading = lastDetail ? model.LastDetail
                : footing ? model.Footing : model.PageLimit;
        if (!footing) model.Footing = lastDetail ? model.LastDetail : model.PageLimit;   // GR3e
    }

    /// <summary>⛔ THE PAGE CLAUSE'S INTEGER RULES (ISO §13.18.39.3 SR5, SR6; kb/Work PB1270), asked of the WRITTEN
    /// integers before the §13.18.39.4 GR3 defaults exist — a default is the programmer's integer copied, so asking
    /// after it would report one fault twice, and SR6 says "wherever specified".
    /// <list type="bullet">
    /// <item>SR5 — "Integer-1 shall not exceed 9999."</item>
    /// <item>SR6, second sentence — "Integer-3, integer-4, integer-5, integer-6, integer-7, and integer-1 shall be
    /// greater than zero. Wherever specified, they shall be in ascending order, with equality allowed." The order is
    /// the HEADING, FIRST DETAIL, LAST CONTROL HEADING, LAST DETAIL, FOOTING integers and then the page limit
    /// (<see cref="PagePhrase"/>'s declaration order), compared pairwise over the ones written, so an omitted phrase
    /// is skipped and never invents a bound. (The "greater than zero" half is IntegerOperandPass, COBOLNET2386.)</item>
    /// </list>
    /// An operand a rule has already refused (a constant-name that names no integer) is not in the list, so a
    /// recovered value never trips an ordering rule.</summary>
    private void ScreenReportPageIntegers(ReportModel model, Core.ReportPageClauseContext page, int? limit,
        List<(PagePhrase Phrase, int Value, Core.ReportPageSubclauseContext Ctx)> phrases)
    {
        if (limit is > MaxPageLimit && page.integerOperand() is { } limitCtx)
        {
            using var at = Edition.At(limitCtx);
            Edition.Error(DiagnosticCatalog.ReportPageClauseRule, $"RD '{model.Name}': the page limit {limit} exceeds "
                + $"{MaxPageLimit}; integer-1 shall not exceed {MaxPageLimit} (ISO §13.18.39.3 SR5)");
        }
        phrases.Sort((a, b) => a.Phrase.CompareTo(b.Phrase));
        for (int k = 0; k < phrases.Count; k++)
        {
            var (phrase, value, ctx) = phrases[k];
            // The next written integer up the order: the following phrase, or the page limit after the last one.
            string? nextWords = null;
            int next = 0;
            if (k + 1 < phrases.Count) { nextWords = $"the {PagePhraseWords(phrases[k + 1].Phrase)} integer"; next = phrases[k + 1].Value; }
            else if (limit is { } l) { nextWords = "the page limit"; next = l; }
            if (nextWords is null || value <= next) continue;
            using var at = Edition.At(ctx);
            Edition.Error(DiagnosticCatalog.ReportPageClauseRule, $"RD '{model.Name}': the {PagePhraseWords(phrase)} integer "
                + $"{value} is greater than {nextWords} {next}; the PAGE clause's integers shall be in ascending order, "
                + "HEADING, FIRST DETAIL, LAST CONTROL HEADING, LAST DETAIL, FOOTING, then the page limit, with equality "
                + "allowed (ISO §13.18.39.3 SR6)");
        }
    }

    /// <summary>ISO §13.18.39.3 SR5 — "Integer-1 shall not exceed 9999."</summary>
    private const int MaxPageLimit = 9999;

    /// <summary>Capture the RD's CODE clause (ISO §13.18.12.2 — <c>CODE IS {literal-1 | identifier-1}</c>; kb/Work
    /// PB1129). literal-1 is screened here (SR1 — "Literal-1 shall be an alphanumeric literal": a numeric, national or
    /// boolean literal and a figurative constant are not one) and decoded through the ONE literal readers
    /// (<see cref="OperandLiteralClass"/> / <see cref="LiteralCharsOf"/> — a §8.8.3.3 concatenation folds, a
    /// hexadecimal format decodes). identifier-1 is kept as the WRITTEN reference, exactly as the SOURCE clause's
    /// identifier operand is (<see cref="FieldReferenceSource"/>): its base item is screened post-build with SR2's shape
    /// rules (<see cref="ResolveReportCode"/>) and its value is bound in the procedure phase. Returns null for an
    /// operand that was refused, so no code is emitted for it.</summary>
    private ReportCodeModel? BindCodeClause(Core.ReportCodeClauseContext cc, ReportModel model)
    {
        string where = $"RD '{model.Name}': the CODE clause";
        if (cc.literal() is { } lit)
        {
            if (OperandLiteralClass(lit) != LiteralClass.Alphanumeric)
            {
                Edition.Error(DiagnosticCatalog.ReportCodeClauseRule, $"{where} names {lit.GetText()}, which is not an "
                    + "alphanumeric literal: literal-1 shall be an alphanumeric literal (ISO §13.18.12.3 SR1)");
                return null;
            }
            return new ReportCodeModel { Literal = LiteralCharsOf(lit, sr11: false), Written = lit.GetText() };
        }
        var dref = cc.dataReference();
        if (dref.LINE_COUNTER() is not null || dref.PAGE_COUNTER() is not null)
        {
            // LINE-COUNTER / PAGE-COUNTER — the report's registers, numeric: never an alphanumeric data item.
            Edition.Error(DiagnosticCatalog.ReportCodeClauseRule, $"{where} names {DataBinder.WrittenText(dref)}, a report "
                + "counter, which is numeric: identifier-1 shall reference an alphanumeric data item (ISO §13.18.12.3 SR2)");
            return null;
        }
        // The WHOLE written reference rides on the model — a subscript and a reference modification are part of an
        // identifier (§8.4.3.1.2) and bind in the procedure phase, exactly as the SOURCE clause's identifier-1 does.
        return new ReportCodeModel { Reference = dref, Written = DataBinder.WrittenText(dref) };
    }

    /// <summary>Resolve the CODE clause's identifier-1 to its item and screen §13.18.12.3 SR2 over it — "Identifier-1 shall
    /// reference an alphanumeric data item that shall not be an occurs-depending-on group item, a variable-length group,
    /// or a dynamic-length elementary item" — then fix the code's length (the item's character positions, which SR2's
    /// exclusions make FIXED). literal-1's length is its characters'. Either way the length is what GR2 charges to the
    /// logical record size, so the report's line width gives it up (<c>RECORD CONTAINS</c>, in <see cref="ResolveReports"/>).</summary>
    private void ResolveReportCode(ReportModel model)
    {
        if (model.Code is not { } code) return;
        if (code.Literal is { } literal)
        {
            code.Length = literal.Length;
            return;
        }
        // ⛔ THE BASE ITEM is looked up here (the SR2 screen is over the item's DESCRIPTION), and nothing else of the written
        // reference is dropped: a subscript and a reference modification are part of identifier-1 (§8.4.3.1.2) and bind
        // in the procedure phase through the ONE sending-operand resolution (`ReportCodeModel.Value`), where the
        // resolver also asks §8.4.2.3.3 SR3's subscript count — so an unsubscripted reference to a table element is
        // refused THERE (SR5), and a subscripted one is legal source (kb/Work PB1292).
        var (codeName, codeQuals) = KeyReference(code.Reference!);
        string where = $"RD '{model.Name}': the CODE clause";
        code.Item = LookupQualified(codeName, codeQuals, where, out bool ambiguous);
        if (code.Item is not { } item)
        {
            if (!ambiguous)
                Edition.Error(DiagnosticCatalog.ReportCodeClauseRule, $"{where} names '{code.Written}', which does not resolve "
                    + "to a data item (ISO §8.4.2.1; identifier-1 of §13.18.12.2)");
            return;
        }
        // A reference-modified identifier-1 references the §8.4.3.3.4 GR5 unique data item, which GR6 c) makes alphanumeric
        // whatever the base's category — so the CLASS half of SR2 is asked of the base item only when no
        // reference-modification is written, and the length is the slice's, which the logical record size needs FIXED
        // (GR2) — an integer-literal position and length, as the CONTROL operand's ref-mod requires.
        var codeSfx = ReferenceResolver.ReadOperandSuffixes(code.Reference!);
        bool codeRefMod = codeSfx.RefMods > 0;
        var itemClass = IntrinsicArgumentRules.ClassOfItem(item);
        string? why = !codeRefMod && (itemClass is not { } cls || IntrinsicArgumentRules.TableTwoClass(cls) != CobolClass.Alphanumeric)
                ? $"is not an alphanumeric data item (it is of {itemClass?.ToString().ToLowerInvariant() ?? "unknown"} class)"
            : codeRefMod && (codeSfx.RefMods > 1 || codeSfx.NonLiteral || codeSfx.BeyondHostLimit || codeSfx.Start is null)
                ? "is reference-modified with a leftmost-position or length that is not a single integer literal, so the "
                  + "characters it occupies in every logical record (§13.18.12.4 GR2) are not a fixed number"
            : OdoModel.TableUnder(item) is { } odo
                ? $"is a group with the occurs-depending-on table '{odo.CobolName ?? odo.CsName}' subordinate to it"
            : item.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(item)
                ? "is a variable-length group"
            : item.IsDynamicLength || item.IsDynamicTable
                ? "is a dynamic-length elementary item"
            : null;
        if (why is not null)
        {
            Edition.Error(DiagnosticCatalog.ReportCodeClauseRule, $"{where} names '{code.Written}', which {why}: identifier-1 "
                + "shall reference an alphanumeric data item that is not an occurs-depending-on group item, a "
                + "variable-length group, or a dynamic-length elementary item (ISO §13.18.12.3 SR2)");
            return;
        }
        code.Length = codeRefMod
            ? Math.Max(0, codeSfx.Length ?? item.DisplayTextWidth - codeSfx.Start!.Value + 1)
            : item.DisplayTextWidth;
    }

    /// <summary>The five trailing phrases of the PAGE clause (§13.18.39.2), each in its own bracket — what
    /// <see cref="PagePhraseOf"/> decides, so the once-only screen, the SR3 screen and the binding read one
    /// classification and the next phrase is one more member here.</summary>
    private enum PagePhrase { Heading, FirstDetail, LastControlHeading, LastDetail, Footing }

    /// <summary>Which §13.18.39.2 phrase a <c>reportPageSubclause</c> is. Read from the phrase's first token and,
    /// for LAST, the word after it — never from <c>HEADING()</c>, which is also a terminal of LAST CONTROL HEADING
    /// (§13.18.39.3 SR1 makes the abbreviations CH and DE synonyms, so the second word may be either spelling).</summary>
    private static PagePhrase PagePhraseOf(Core.ReportPageSubclauseContext sub) => sub.Start.Type switch
    {
        CobolLexer.HEADING => PagePhrase.Heading,
        CobolLexer.FIRST => PagePhrase.FirstDetail,
        CobolLexer.FOOTING => PagePhrase.Footing,
        _ => sub.CONTROL() is not null || sub.CH() is not null ? PagePhrase.LastControlHeading : PagePhrase.LastDetail,
    };

    private static string PagePhraseWords(PagePhrase phrase) => phrase switch
    {
        PagePhrase.Heading => "HEADING",
        PagePhrase.FirstDetail => "FIRST DETAIL",
        PagePhrase.LastControlHeading => "LAST CONTROL HEADING",
        PagePhrase.LastDetail => "LAST DETAIL",
        _ => "FOOTING",
    };

    /// <summary>Build one RD's report groups from its (flat, level-numbered) group entries. The line-building
    /// rule (ISO §13.15 / COBOLNET_REPORT_WRITER_DESIGN §3.3): walking the entries in declaration order, a
    /// 01-level entry opens a new GROUP; an entry whose clauses include a LINE clause OPENS a new report line
    /// (LINE is legal at ANY level — RW101A puts <c>LINE PLUS 1</c> on an 03); an entry with a COLUMN clause
    /// appends a printable field to the CURRENT line. PRESENT WHEN conditions (§13.18.41 Format 1) accumulate
    /// down a level-number stack — a line carries the chain 01→line-entry, a field the chain strictly below the
    /// line entry, a SUM entry the full chain (GR2b: an absent ancestor absents every subordinate,
    /// "irrespective of any PRESENT WHEN clauses they may also contain" — the AND of independent conditions).
    /// Legal-but-unimplemented clauses stage loud (§1.4).</summary>
    private void BindReportGroups(Core.ReportDescriptionEntryContext rd, ReportModel model)
    {
        var entries = rd.reportGroupEntry();
        var runs = ReportEntryRuns(rd);
        ScreenReportDescriptionHasGroup(entries, model);
        // The flat screens read the level hierarchy of the WRITTEN entries, so each runs over one run between constant
        // entries: a constant ends the report group before it (kb/Work PB1954, see ReportEntryRuns), and an entry
        // after it is subordinate to nothing before it.
        foreach (var (start, end, _) in runs)
        {
            var run = entries[start..end];
            ScreenReportLevelNumbers(run, model);
            ScreenReportLineNesting(run, model);
            ScreenReportLineClauses(run, model);
            ScreenReportColumnClauses(run, model);
            ScreenReportEntryClausePresence(run, model);
            ScreenReportVaryingClauses(run, model);
        }
        BindReportSectionEntries(entries, runs, model);
        BindNextGroupClauses(entries, model);
    }

    /// <summary>⛔ THE REPORT SECTION'S ENTRIES FOR ONE RD, CUT INTO RUNS AT ITS CONSTANT ENTRIES — the one place the
    /// cut is made, for the flat screens and for <see cref="BindReportSectionEntries"/> alike. ISO §13.8.2 prints the
    /// entries after a report description entry as <c>{ constant-entry | report-group-description-entry } …</c>, and a
    /// constant is a level-01 entry (§13.10.2), so it ends the report group before it. Each run is the index range
    /// [Start, End) of <c>rd.reportGroupEntry()</c> and the constant entry that ends it (null for the last run).
    /// Before the cut the screens read across a constant: `03 LINE PLUS 1. 01 K CONSTANT AS 3. 05 LINE PLUS 1.` drew a
    /// spurious §13.18.35.3 SR4 diagnostic, as if the second LINE entry were subordinate to the first.</summary>
    private static List<(int Start, int End, Core.ConstantEntryContext? Constant)> ReportEntryRuns(
        Core.ReportDescriptionEntryContext rd)
    {
        var runs = new List<(int Start, int End, Core.ConstantEntryContext? Constant)>();
        int start = 0, seen = 0;
        foreach (var child in rd.children)
        {
            if (child is Core.ReportGroupEntryContext) { seen++; continue; }
            if (child is not Core.ConstantEntryContext constant) continue;
            runs.Add((start, seen, constant));
            start = seen;
        }
        runs.Add((start, seen, null));
        return runs;
    }

    /// <summary>ISO §8.5.1.3.2's equal-sibling rule for report group description entries (kb/Work PB1954): "All
    /// items that are immediately subordinate to a given group item shall be described using numerically equal
    /// level-numbers greater than the level-number used to describe that group item." §13.18.33.1 gives level-numbers
    /// 1 through 49 one meaning in a data description entry and a report group description entry, so the rule the
    /// data arm asks in <see cref="BindEntries"/> is asked here too, through the same
    /// <see cref="ScreenImmediateMemberLevel"/>. Screened ONCE per WRITTEN entry over one run of the flat entry array
    /// (the <see cref="ScreenReportLineNesting"/> shape): a §13.18.38 Format 3 subtree replay in
    /// <c>BindReportEntry</c> would report it once per repetition (kb/Work PB884, PB1306). A first entry that is not
    /// level 1 is <see cref="DiagnosticCatalog.ReportGroupBefore01"/>'s, reported by the walk. An entry whose
    /// level-number is outside 1 through 49 (an 88, 66 or 77 written in an RD) is no report group description entry
    /// (§13.18.33.3 SR4, COBOLNET1746 from <c>LevelNumberPass</c>), so it takes no place in the hierarchy: counting it
    /// made `01 DT TYPE DETAIL. 88 D-ON VALUE "X". 05 LINE 1.` report the 05 as unequal to the 88.</summary>
    private void ScreenReportLevelNumbers(Core.ReportGroupEntryContext[] run, ReportModel model)
    {
        var open = new List<(int Level, string Name, int? MemberLevel)>();
        foreach (var ge in run)
        {
            if (!int.TryParse(ge.levelNumber().GetText(), out int level) || level is < 1 or > 49) continue;
            while (open.Count > 0 && open[^1].Level >= level) open.RemoveAt(open.Count - 1);
            string name = ge.dataName().NameOrNull() ?? "FILLER";
            if (open.Count > 0)
            {
                var group = open[^1];
                if (group.MemberLevel is null) open[^1] = group with { MemberLevel = level };
                using var _ = Edition.At(ge);
                ScreenImmediateMemberLevel(group.MemberLevel, level, $"RD '{model.Name}' entry '{name}'",
                    $"the report group entry '{group.Name}'");
            }
            open.Add((level, name, null));
        }
    }

    /// <summary>⛔ THE VARYING CLAUSE'S SYNTAX RULES, asked ONCE per WRITTEN entry over the flat entry array (kb/Work
    /// PB1306) — the <see cref="ScreenReportLineNesting"/> shape, so a §13.18.38 subtree replay cannot multiply them.
    /// <list type="bullet">
    /// <item>§13.18.64.3 SR1 — "The entry containing the VARYING clause shall also contain an OCCURS clause or, if the
    /// VARYING clause appears in a report group description entry, a multiple LINE or multiple COLUMN clause."</item>
    /// <item>SR2 — "Data-name-1 shall not be defined elsewhere in the source element, except as data-name-1 in another
    /// VARYING clause of an entry not subordinate to the subject of the current entry." Three arms: a name given twice
    /// in ONE entry; a name an ENCLOSING entry's VARYING clause already defines (that entry is not "not subordinate": the
    /// current entry is subordinate to it); and a name defined by anything else — a data item, or a report section
    /// entry or sum counter (the latter arm asked once the source element is complete, in <c>ResolveReports</c>, from
    /// <see cref="ReportModel.VaryingNames"/>). A reuse in a sibling or unrelated entry is legal and "refers to a
    /// completely independent data item".</item>
    /// <item>SR3 — "Data-name-1 shall not be referenced in arithmetic-expression-1 of the same VARYING clause, but may
    /// be referenced in arithmetic-expression-2 of the same VARYING clause or in arithmetic-expression-1 or
    /// arithmetic-expression-2 of a VARYING clause in a subordinate entry." Only the first half is a prohibition: a
    /// FROM naming a counter the same entry defines.</item>
    /// </list></summary>
    private void ScreenReportVaryingClauses(Core.ReportGroupEntryContext[] entries, ReportModel model)
    {
        var enclosing = new List<(int Level, string Name)>();
        foreach (var ge in entries)
        {
            int.TryParse(ge.levelNumber().GetText(), out int level);
            enclosing.RemoveAll(c => c.Level >= level);
            var specs = ge.reportGroupClause().Select(c => c.reportVaryingClause()).OfType<Core.ReportVaryingClauseContext>()
                .SelectMany(vc => vc.reportVaryingSpec()).ToList();
            if (specs.Count == 0) continue;
            using var _ = Edition.At(ge);
            string where = $"RD '{model.Name}' entry '{ge.dataName().NameOrNull() ?? "FILLER"}'";
            bool repeating = ge.reportGroupClause().Any(c => c.occursClause() is not null
                || (c.reportLineClause()?.reportLineOperand().Length ?? 0) > 1
                || (c.reportColumnClause()?.reportColumnOperand().Length ?? 0) > 1);
            if (!repeating)
                Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"{where}: a VARYING clause requires the entry to also "
                    + "contain an OCCURS clause or a multiple LINE or multiple COLUMN clause (ISO §13.18.64.3 SR1)");
            var own = new List<string>();
            foreach (var spec in specs)
            {
                string name = spec.cobolWord().GetText();
                if (own.Contains(name, CobolNames.Comparer))
                    Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"{where} VARYING '{name}': the counter is defined twice in "
                        + "this entry; data-name-1 shall not be defined elsewhere in the source element, except as "
                        + "data-name-1 in another VARYING clause of an entry not subordinate to the subject of the "
                        + "current entry (ISO §13.18.64.3 SR2)");
                else if (enclosing.Any(c => CobolNames.Same(c.Name, name)))
                    Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"{where} VARYING '{name}': an entry above this one already "
                        + "defines the counter in its VARYING clause; a reuse of the name is permitted only in an entry "
                        + "not subordinate to the subject of the current entry (ISO §13.18.64.3 SR2)");
                else if (!model.VaryingNames.Contains(name, CobolNames.Comparer))
                    model.VaryingNames.Add(name);
                own.Add(name);
            }
            foreach (var spec in specs)
                if (spec.FROM() is not null && own.FirstOrDefault(n => HasWord(spec.arithmeticExpression(0), n)) is { } hit)
                    Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"{where} VARYING '{spec.cobolWord().GetText()}': the "
                        + $"counter '{hit}' shall not be referenced in arithmetic-expression-1 of the same VARYING clause (ISO "
                        + "§13.18.64.3 SR3)");
            enclosing.AddRange(own.Select(n => (level, n)));
        }
    }

    /// <summary>⛔ ISO §13.8.4 — "An RD entry shall be followed by one or more report group description entries"
    /// (kb/Work PB1226). §13.8.2 prints the entries after a report description entry as the brace group
    /// <c>{ constant-entry | report-group-description-entry } …</c>, and §13.8.4 narrows the one-or-more to the
    /// report group kind: constant entries alone describe no report. The grammar admits the list empty (a
    /// bind-time rule, like the sort-merge twin's COBOLNET1837, so the diagnostic names the rule rather than a
    /// parse position).</summary>
    private void ScreenReportDescriptionHasGroup(Core.ReportGroupEntryContext[] entries, ReportModel model)
    {
        if (entries.Length > 0) return;
        Edition.Error(DiagnosticCatalog.ReportDescriptionWithoutGroup, $"RD '{model.Name}' is followed by no report "
            + "group description entry (ISO §13.8.4: an RD entry shall be followed by one or more report group "
            + "description entries)");
    }

    /// <summary>⛔ THE REPORT SECTION'S ENTRIES IN SOURCE ORDER (ISO §13.8.2: <c>{ constant-entry |
    /// report-group-description-entry } …</c>; kb/Work PB1226). A constant entry binds into the compile-time
    /// constant table at the point it stands, unless a reference bound it earlier on demand (a reference may precede
    /// the entry it names, kb/Work PB1231, <see cref="FindConstant"/>); the report group entries between two constants bind as
    /// one run (<see cref="ReportEntryRuns"/>) through <see cref="BindReportEntries"/>, which takes an index range for
    /// exactly this. A constant is a level-01 entry (§13.10.2), so it ends the group before it: the entries after it
    /// have no 01 entry to belong to, and the builder's group is dropped so
    /// <see cref="DiagnosticCatalog.ReportGroupBefore01"/> says so instead of attaching them to the group the constant
    /// interrupted.</summary>
    private void BindReportSectionEntries(Core.ReportGroupEntryContext[] entries,
        List<(int Start, int End, Core.ConstantEntryContext? Constant)> runs, ReportModel model)
    {
        var st = new ReportGroupBuild();
        foreach (var (start, end, constant) in runs)
        {
            BindReportEntries(entries, start, end, model, st);
            if (constant is null) continue;
            using var _ = Edition.At(constant);
            BindConstantEntry(constant, constant.levelNumber(), constant.dataName(), constant.constantEntryBody());
            st.Group = null;
        }
        SealSumCounters(model);
    }

    /// <summary>Put the report's counters in COUNTER-ID order once every entry is bound (kb/Work PB1271). Each SUM
    /// entry reserved its block of ids when it was first seen (<see cref="SumFamilyOf"/>), but the replay binds its
    /// occurrences interleaved with the sibling entries' (A0 B0 A1 B1), so binding order is not id order; the engine
    /// registers the counters in list order and requires the list index to BE the id (<c>CobolReport.AddSum</c>).
    /// Every repetition of every entry is bound (§13.18.38.4 GR10 — "integer-2 distinct report items"; the
    /// repetitions §13.18.63.4 GR23 lets a DEPENDING phrase suppress are bound too), so the reserved blocks are
    /// exactly filled; a gap is a binder defect,
    /// never a property of the source.</summary>
    private static void SealSumCounters(ReportModel model)
    {
        model.Sums.Sort((a, b) => a.Id.CompareTo(b.Id));
        int reserved = model.SumFamilies.Sum(f => f.Count);
        for (int i = 0; i < reserved; i++)
            if (i >= model.Sums.Count || model.Sums[i].Id != i)
                throw new InvalidOperationException($"RD '{model.Name}': reserved sum counter id {i} has no counter "
                    + "— a counter block was not filled by its entry's occurrences (kb/Work PB1271)");
    }

    /// <summary>⛔ THE NEXT GROUP CLAUSE (ISO §13.18.37; kb/Work PB957), bound once per RD after every group is
    /// complete — its syntax rules read the group's TYPE (which may follow the clause in the entry) and, for SR6a,
    /// SR6c and SR7, the group's lines. The binding is the runtime's own
    /// <see cref="CobolNet.Runtime.IO.ReportNextGroup"/>; the engine applies it after the group's last line is
    /// printed (§13.18.37.4 GR2).
    /// <list type="bullet">
    /// <item>§13.15.3 SR6 — "The NEXT GROUP clause may be specified only in a level 1 entry." Screened over the flat
    /// entry array, so a §13.18.38 Format 3 replay cannot report it once per repetition.</item>
    /// <item>§13.18.37.3 SR1 — "Integer-1 and integer-2 shall not exceed the page limit, or 9999 if the report is
    /// not divided into pages." (Unsigned: the grammar's integerLiteral carries no sign, and SR2's PLUS/+ is the
    /// relative operator, not a sign.)</item>
    /// <item>SR3 — "If the report is not divided into pages, only the relative form of the clause may be
    /// specified."</item>
    /// <item>SR4 — "The NEXT GROUP clause shall not be specified in a page heading or report footing."</item>
    /// <item>SR5 — "The NEXT PAGE phrase shall not be specified in a page footing."</item>
    /// <item>SR6 a/b/c and SR7 a/b — the absolute and relative integers against the report heading, body group and
    /// page footing regions. "The minimum last line number of the report group" is the last line the group
    /// prints when every line that can be absent IS absent (a PRESENT WHEN chain or an OCCURS DEPENDING guard):
    /// an absent line can only leave the last line where an earlier line put it, so the unconditional lines
    /// alone bound it from below. A group with no unconditional line has no such bound and the comparison with
    /// it is not made.</item>
    /// </list></summary>
    private void BindNextGroupClauses(Core.ReportGroupEntryContext[] entries, ReportModel model)
    {
        foreach (var ge in entries)
        {
            if (int.TryParse(ge.levelNumber().GetText(), out int level) && level == 1) continue;
            foreach (var c in ge.reportGroupClause())
                if (c.reportNextGroupClause() is { } misplaced)
                {
                    using var _ = Edition.At(misplaced);
                    Edition.Error(DiagnosticCatalog.ReportNextGroupClauseRule, $"RD '{model.Name}' entry "
                        + $"'{ge.dataName().NameOrNull() ?? "FILLER"}' is a level {level} entry; the NEXT GROUP "
                        + "clause may be specified only in a level 1 entry (ISO §13.15.3 SR6)");
                }
        }
        foreach (var g in model.Groups)
        {
            if (g.NextGroupClause is not { } ngc) continue;
            using var _ = Edition.At(ngc);
            var ng = ngc.PAGE() is not null
                ? new ReportNextGroup(ReportNextGroupKind.NextPage, 0, ngc.RESET() is not null)
                : new ReportNextGroup(ngc.reportRelativeSign() is not null
                    ? ReportNextGroupKind.Relative : ReportNextGroupKind.Absolute,
                    IntegerOperandValue(ngc.integerOperand(), $"RD '{model.Name}' group '{g.Name ?? "FILLER"}'") ?? RecoveredIntegerOperand);
            g.NextGroup = ng;
            string where = $"RD '{model.Name}' group '{g.Name ?? "FILLER"}' ({ReportGroupTypeWords(g.Kind)})";
            void Violation(string rule) =>
                Edition.Error(DiagnosticCatalog.ReportNextGroupClauseRule, $"{where}: {rule}");

            if (g.Kind is ReportGroupKindModel.PageHeading or ReportGroupKindModel.ReportFooting)
            {
                Violation("the NEXT GROUP clause shall not be specified in a page heading or report footing (ISO "
                    + "§13.18.37.3 SR4)");
                continue;
            }
            if (ng.Kind == ReportNextGroupKind.NextPage)
            {
                if (g.Kind == ReportGroupKindModel.PageFooting)
                    Violation("the NEXT PAGE phrase shall not be specified in a page footing (ISO §13.18.37.3 SR5)");
                if (!model.Paged)
                    Violation("the report is not divided into pages, so only the relative form of the NEXT GROUP "
                        + "clause may be specified (ISO §13.18.37.3 SR3)");
                continue;
            }
            bool absolute = ng.Kind == ReportNextGroupKind.Absolute;
            if (ng.Value > model.VerticalLimit)
                Violation($"{(absolute ? "integer-1" : "integer-2")} {ngc.integerOperand().GetText()} exceeds {model.VerticalLimitWords}"
                    + "; integer-1 and integer-2 shall not exceed the page limit, or 9999 if the report is not "
                    + "divided into pages (ISO §13.18.37.3 SR1)");
            if (!model.Paged)
            {
                if (absolute)
                    Violation("the report is not divided into pages, so only the relative form of the NEXT GROUP "
                        + "clause may be specified (ISO §13.18.37.3 SR3)");
                continue;   // SR6/SR7 are stated against the page regions, which an unpaged report has none of
            }
            int? minLast = g.Kind is ReportGroupKindModel.ReportHeading or ReportGroupKindModel.PageFooting
                ? MinimumLastLine(g, model) : null;
            switch (g.Kind, absolute)
            {
                case (ReportGroupKindModel.ReportHeading, true)
                    when ng.Value >= model.FirstDetail || (minLast is { } m && ng.Value <= m):
                    Violation($"integer-1 {ng.Value} shall be greater than the minimum last line number of the report "
                        + $"heading ({minLast?.ToString() ?? "none"}) and less than the FIRST DETAIL integer "
                        + $"{model.FirstDetail} (ISO §13.18.37.3 SR6a)");
                    break;
                case (ReportGroupKindModel.ControlHeading or ReportGroupKindModel.Detail
                      or ReportGroupKindModel.ControlFooting, true)
                    when ng.Value < model.FirstDetail || ng.Value > model.Footing:
                    Violation($"integer-1 {ng.Value} shall lie between the FIRST DETAIL integer {model.FirstDetail} and "
                        + $"the FOOTING integer {model.Footing}, inclusive (ISO §13.18.37.3 SR6b)");
                    break;
                case (ReportGroupKindModel.PageFooting, true) when minLast is { } m && ng.Value <= m:
                    Violation($"integer-1 {ng.Value} shall be greater than the minimum last line number of the page "
                        + $"footing ({m}) (ISO §13.18.37.3 SR6c)");
                    break;
                case (ReportGroupKindModel.ReportHeading, false) when minLast is { } m && m + ng.Value >= model.FirstDetail:
                    Violation($"the minimum last line number of the report heading ({m}) plus integer-2 {ng.Value} shall "
                        + $"be less than the FIRST DETAIL integer {model.FirstDetail} (ISO §13.18.37.3 SR7a)");
                    break;
                case (ReportGroupKindModel.PageFooting, false) when minLast is { } m && m + ng.Value > model.PageLimit:
                    Violation($"the minimum last line number of the page footing ({m}) plus integer-2 {ng.Value} shall "
                        + $"not exceed the page limit {model.PageLimit} (ISO §13.18.37.3 SR7b)");
                    break;
            }
        }
    }

    /// <summary>The MINIMUM LAST LINE NUMBER of a report heading or page footing (ISO §13.18.37.3 SR6a/SR6c/SR7):
    /// the §13.18.35.4 GR5/GR7 placement walked over the group's UNCONDITIONAL lines only — first line absolute →
    /// integer-1, relative → HEADING + integer-2 − 1 (GR5b1, report heading) or FOOTING + integer-2 (GR5b4, page
    /// footing); later lines absolute → integer-1, relative → the previous line + integer-2, and a §13.18.38.4
    /// GR12c/GR12d STEP line → its anchor + its displacement, the anchors seeded as the engine seeds them. Null
    /// when every line can be absent. <paramref name="present"/> names the lines to walk instead — the §13.18.35.3 SR6 d)
    /// screen asks it for a presentation in which a chosen set of conditional lines is present (null: the
    /// unconditional lines, the floor no presentation can lift). <paramref name="seed"/> is the last line of the group
    /// printed BEFORE this one in the same run of groups, when there is one (a control heading below an OR PAGE heading
    /// follows it, §13.18.57.4 GR7 d) 2.): the walk starts from that line, so a relative first line is placed after it
    /// instead of at FIRST DETAIL, and with no walked line the seed is the answer.</summary>
    private static int? MinimumLastLine(ReportGroupModel g, ReportModel model, Func<ReportLineModel, bool>? present = null, int? seed = null)
    {
        int? pos = seed;
        var anchors = new Dictionary<int, int>();
        foreach (var l in g.Lines)
        {
            if (present is null ? l.PresentWhenCtxs.Count > 0 || l.RepetitionGuards.Count > 0 : !present(l)) continue;   // may be absent
            int relative = l.Kind == ReportLineKindModel.Step ? l.RelativeBase : l.Value;
            int target = l.Kind == ReportLineKindModel.Absolute ? l.Value
                // The bare ON NEXT PAGE of a report footing starts the footing on a page by itself, at the upper limit
                // §13.18.57.4 GR7 f) gives it, the HEADING integer (the run-time engine's determination, docs/CONFORMANCE.md).
                : pos is null && g.Kind == ReportGroupKindModel.ReportFooting && l.NextPage ? model.Heading
                : pos is null ? g.Kind switch
                {
                    ReportGroupKindModel.ReportHeading or ReportGroupKindModel.PageHeading => model.Heading + relative - 1,   // GR5b1, GR5b2
                    ReportGroupKindModel.PageFooting or ReportGroupKindModel.ReportFooting => model.Footing + relative,       // GR5b4, GR5b5
                    _ => model.FirstDetail,   // GR5b3 — the first body group on a page, whatever integer-2 says
                }
                : l.Kind == ReportLineKindModel.Step && anchors.TryGetValue(l.Anchor, out int a) ? a + l.Value
                : pos.Value + relative;
            if (l.Anchor != 0 && (l.Kind != ReportLineKindModel.Step || !anchors.ContainsKey(l.Anchor)))
                anchors[l.Anchor] = target - (l.Kind == ReportLineKindModel.Step ? l.Value : 0);
            pos = target;
        }
        return pos;
    }

    /// <summary>⛔ THE UPPER AND LOWER LIMIT OF A REPORT GROUP (ISO §13.18.57.4 GR7 and GR8), computed once the whole
    /// report is bound — the compile-time consumer of §13.18.35.3 SR6 c), and the same numbers the run-time page-fit
    /// test reads for a body group (<c>CobolReport.LowerLimit</c>, GR8 d)–f)). GR7: "the uppermost permitted line on
    /// the page that may be occupied by the report group's first line"; GR8: "the lowermost permitted line … that
    /// may be occupied by the report group's last line".
    /// <list type="bullet">
    /// <item>UPPER. a) report heading, and page heading where no report heading shares the page: the HEADING integer —
    /// on page 1 a page heading follows the report heading (b), on every later page it does not, so the HEADING integer
    /// is its widest. c) a body group with no OR PAGE control heading: FIRST DETAIL. d) with one: a control heading at
    /// the highest OR PAGE level or above, FIRST DETAIL; a lower control heading, the line after the next higher-level
    /// control heading; a detail, the line after the lowest-level OR PAGE control heading; a control footing, the line
    /// after the lowest OR PAGE heading at its own level or above, else FIRST DETAIL. e) page footing: FOOTING + 1.
    /// f)/g) report footing: on a page by itself (its first LINE clause has the NEXT PAGE phrase, §13.18.35.4 GR5 a))
    /// the HEADING integer, else the line after the page footing's last line, or FOOTING + 1 with none.</item>
    /// <item>LOWER. a)/b) report heading: on a page by itself (its NEXT GROUP clause is NEXT PAGE, §13.18.37.4 GR3 c)
    /// — the first body group takes no page-fit test, so nothing else separates it) the page limit; else the line before
    /// the page heading's first line, or FIRST DETAIL − 1 with no page heading. c) page heading: FIRST DETAIL − 1.
    /// d) control heading: LAST CONTROL HEADING. e) detail: LAST DETAIL. f) control footing: FOOTING. g) page footing
    /// and report footing: the page limit.</item>
    /// </list>
    /// <para>⚠ DETERMINATION (docs/CONFORMANCE.md, the report group limits block): a limit that depends on how far a
    /// heading REACHES — "the line after the last line of" an OR PAGE control heading or the page footing — is taken
    /// at that group's MINIMUM reach (<see cref="MinimumLastLine"/>: its unconditional lines, placed as the engine
    /// places them), the widest region the group's own PRESENT WHEN clauses leave open. The screen is a syntax-rule
    /// screen: a line outside the widest region is outside every presentation's. A heading's last line is walked
    /// from the last line of the nearest OR PAGE heading ABOVE it (every page advance prints those first, GR6 c)),
    /// so the d) 2./3./4. limits of a group below two OR PAGE headings sit below both of them (kb/Work PB2513).</para></summary>
    private static (int Upper, int Lower) GroupLimits(ReportGroupModel g, ReportModel model)
    {
        var controlHeadings = model.Groups.Where(x => x.Kind == ReportGroupKindModel.ControlHeading && x.ControlLevel >= 0).ToList();
        var orPage = controlHeadings.Where(x => x.OrPage).ToList();
        // The last line of a control heading at its UPPERMOST: every OR PAGE heading above it is printed before it on
        // every page advance (§13.18.57.4 GR6 c)), so its first line follows the nearest such heading's last line
        // instead of standing at FIRST DETAIL (§13.18.35.4 GR5 b) 3. — the first body group on a page), and so on up the
        // chain (the d) 2./d) 3. limits "the line following the last line of" a heading below another one).
        int? HeadingReach(ReportGroupModel h)
        {
            var above = orPage.Where(x => x.ControlLevel < h.ControlLevel).OrderByDescending(x => x.ControlLevel).FirstOrDefault();
            return MinimumLastLine(h, model, null, above is null ? null : HeadingReach(above));
        }
        int AfterHeading(ReportGroupModel h) => HeadingReach(h) is { } last ? last + 1 : model.FirstDetail;
        // GR7 d) 1.–4.: the OR PAGE headings push the body groups below them down; absent any, GR7 c) is FIRST DETAIL.
        int BodyUpper()
        {
            if (orPage.Count == 0 || g.ControlLevel < 0 && g.Kind != ReportGroupKindModel.Detail) return model.FirstDetail;
            switch (g.Kind)
            {
                case ReportGroupKindModel.ControlHeading:
                    if (g.ControlLevel <= orPage.Min(x => x.ControlLevel)) return model.FirstDetail;
                    var higher = controlHeadings.Where(x => x.ControlLevel < g.ControlLevel)
                        .OrderByDescending(x => x.ControlLevel).FirstOrDefault();
                    return higher is null ? model.FirstDetail : AfterHeading(higher);
                case ReportGroupKindModel.Detail:
                    return AfterHeading(orPage.OrderByDescending(x => x.ControlLevel).First());
                default:   // control footing
                    var atOrAbove = orPage.Where(x => x.ControlLevel <= g.ControlLevel)
                        .OrderByDescending(x => x.ControlLevel).FirstOrDefault();
                    return atOrAbove is null ? model.FirstDetail : AfterHeading(atOrAbove);
            }
        }
        switch (g.Kind)
        {
            case ReportGroupKindModel.ReportHeading:
            {
                if (g.NextGroup?.Kind == CobolNet.Runtime.IO.ReportNextGroupKind.NextPage) return (model.Heading, model.PageLimit);
                var pageHeading = model.Groups.FirstOrDefault(x => x.Kind == ReportGroupKindModel.PageHeading);
                bool absoluteFirst = pageHeading is { Lines: [{ Kind: ReportLineKindModel.Absolute } first, ..] }
                    && first.PresentWhenCtxs.Count == 0;
                return (model.Heading, absoluteFirst ? pageHeading!.Lines[0].Value - 1 : model.FirstDetail - 1);
            }
            case ReportGroupKindModel.PageHeading: return (model.Heading, model.FirstDetail - 1);
            case ReportGroupKindModel.ControlHeading: return (BodyUpper(), model.LastControlHeading);
            case ReportGroupKindModel.Detail: return (BodyUpper(), model.LastDetail);
            case ReportGroupKindModel.ControlFooting: return (BodyUpper(), model.Footing);
            case ReportGroupKindModel.PageFooting: return (model.Footing + 1, model.PageLimit);
            default:   // report footing
            {
                if (g.Lines is [{ NextPage: true }, ..]) return (model.Heading, model.PageLimit);
                var pageFooting = model.Groups.FirstOrDefault(x => x.Kind == ReportGroupKindModel.PageFooting);
                int below = pageFooting is not null && MinimumLastLine(pageFooting, model) is { } last ? last + 1 : model.Footing + 1;
                return (below, model.PageLimit);
            }
        }
    }

    /// <summary>⛔ THE CENSUS OF A REPORT'S GROUPS (ISO §13.18.57.3 SR13–SR15; kb/Work PB1299), asked once every CH/CF
    /// has its control level (<see cref="ControlLevelOf"/>, SR10/SR11):
    /// <list type="bullet">
    /// <item>SR13 — "REPORT HEADING, PAGE HEADING, REPORT FOOTING, and PAGE FOOTING may each appear no more than once
    /// in any given report description."</item>
    /// <item>SR14 — "At most one CONTROL HEADING and at most one CONTROL FOOTING may be defined for each control data
    /// item or FINAL of the CONTROL clause for any given report." Counted per RESOLVED level, so an omitted operand
    /// (SR11) and a written one naming the same control collide. A CH or CF whose operand SR10/SR11 refused has no
    /// level and is not counted twice.</item>
    /// <item>SR15 — "Each report description shall include at least one body group", a DETAIL, CONTROL HEADING or
    /// CONTROL FOOTING (SR15's own definition). SR16 only relaxes it to a CONTROL group alone (a summary report,
    /// GENERATE report-name), so any one body group meets it.</item>
    /// </list>
    /// The run-time <c>CobolReport.AddGroup</c> keeps one slot per type (per control level), so an unscreened second
    /// group silently REPLACED the first and its lines were never printed.</summary>
    private void ScreenReportGroupCensus(ReportModel model)
    {
        var first = new Dictionary<(ReportGroupKindModel Kind, int Level), ReportGroupModel>();
        foreach (var g in model.Groups)
        {
            if (g.Kind == ReportGroupKindModel.Detail) continue;
            bool control = g.Kind is ReportGroupKindModel.ControlHeading or ReportGroupKindModel.ControlFooting;
            if (control && g.ControlLevel < 0) continue;
            if (first.TryAdd((g.Kind, control ? g.ControlLevel : -1), g)) continue;
            using var at = Edition.At(g.Entry);
            Edition.Error(DiagnosticCatalog.ReportGroupSetRule, $"RD '{model.Name}': report group '{g.Name ?? "FILLER"}' is a second "
                + (control
                    ? $"{ReportGroupTypeWords(g.Kind)} for control '{model.Controls[g.ControlLevel].Display}' ("
                        + $"'{first[(g.Kind, g.ControlLevel)].Name ?? "FILLER"}' is the first); at most one CONTROL HEADING and at "
                        + "most one CONTROL FOOTING may be defined for each control data item or FINAL of the CONTROL clause (ISO "
                        + "§13.18.57.3 SR14)"
                    : $"{ReportGroupTypeWords(g.Kind)} ('{first[(g.Kind, -1)].Name ?? "FILLER"}' is the first); REPORT HEADING, "
                        + "PAGE HEADING, REPORT FOOTING, and PAGE FOOTING may each appear no more than once in any given report "
                        + "description (ISO §13.18.57.3 SR13)"));
        }
        if (model.Groups.Count > 0 && !model.Groups.Exists(g => g.Kind is ReportGroupKindModel.Detail
                or ReportGroupKindModel.ControlHeading or ReportGroupKindModel.ControlFooting))
        {
            using var at = Edition.At(model.Groups[0].Entry);
            Edition.Error(DiagnosticCatalog.ReportGroupSetRule, $"RD '{model.Name}' describes no body group: each report "
                + "description shall include at least one body group, a DETAIL, CONTROL HEADING or CONTROL FOOTING group "
                + "(ISO §13.18.57.3 SR15)");
        }
    }

    /// <summary>The upper limit a RELATIVE first line is held to (§13.18.57.4 GR7 a), b), e), f)): the HEADING integer for
    /// a heading (and for a report footing on a page by itself); FOOTING + 1 for a page footing and a report footing
    /// with no page footing before it. A report footing printed after the page footing has the longer reach — the line
    /// after the page footing's last — but its first line is then that last line plus integer-2, so the question
    /// "does integer-2 put the first line above the limit" is answered by the same difference against the lower seed
    /// (<see cref="MinimumLastLine"/>), which <see cref="GroupLimits"/>'s reach-dependent upper limit would mis-state.</summary>
    private static int RelativeFirstLineFloor(ReportGroupModel g, ReportModel model) =>
        g.Kind is ReportGroupKindModel.ReportHeading or ReportGroupKindModel.PageHeading
        || g.Lines is [{ NextPage: true }, ..] ? model.Heading : model.Footing + 1;

    /// <summary>⛔ THE LINE SET OF EACH REPORT GROUP (ISO §13.18.35.3 SR6 a)–e); kb/Work PB1222, PB1270), asked once
    /// the group is complete and the page regions are known:
    /// <list type="bullet">
    /// <item>a) "If any two or more absolute lines are defined using line numbers that are not in increasing numerical
    /// order, they shall each be subject to a different PRESENT WHEN clause."</item>
    /// <item>b) "If any two or more lines, or groups of lines, overlap each other, they shall each be subject to a
    /// different PRESENT WHEN clause." Two absolute lines with the same number. A relative line overlaps nothing the
    /// description fixes: its position is its predecessor's plus integer-2, and §13.18.35.4 GR3 excepts the integer-2
    /// of zero ("the non-space characters of a relative line specified with an integer-2 of zero will overwrite the
    /// corresponding characters of the preceding line") from the overlap rule altogether.</item>
    /// <item>c) "Any absolute report lines shall be defined in such a way that no line appears above the upper limit
    /// or below the lower limit allowed for the report group" — <see cref="GroupLimits"/>; only in a report divided
    /// into pages, the only report with page regions (§13.18.39.4 GR2).</item>
    /// <item>d) "If the report group consists of one or more absolute lines, not subject to any PRESENT WHEN clause,
    /// and ends in a set of relative lines, or groups of relative lines, they shall not cause the report group's lower
    /// limit to be exceeded unless each of them is subject to a different PRESENT WHEN clause, in which case this rule
    /// applies only to the vertically largest of them." The tail is every line after the group's last absolute one
    /// (itself unconditional), judged whole unless <see cref="EachDifferentPresentWhen"/>, and then by the one that
    /// ends lowest when it alone of the tail is present; the placement is <see cref="MinimumLastLine"/>, the one walk,
    /// asked for a chosen set of present lines.</item>
    /// <item>e) "If the description of any absolute line appears later than that of a relative line, they shall each
    /// be subject to a different PRESENT WHEN clause."</item>
    /// </list>
    /// The pairs of a), b) and e) are over WRITTEN entries — each entry's first line, in description order — so the
    /// repetitions of a repeating entry (§13.18.38.3 SR26's overlap rule) and the operands of a multiple LINE clause
    /// (SR10 c)) are not compared with each other, and a replay never reports one fault once per repetition.</summary>
    private void ScreenReportGroupLines(ReportModel model)
    {
        foreach (var g in model.Groups)
        {
            var firstLines = new List<ReportLineModel>();
            foreach (var l in g.Lines)
                if (l.Entry is not null && !firstLines.Exists(f => ReferenceEquals(f.Entry, l.Entry))) firstLines.Add(l);

            void Violation(ReportLineModel l, string rule) =>
                Edition.Error(DiagnosticCatalog.ReportLineClauseRule, $"RD '{model.Name}': {rule}");

            string LineWords(ReportLineModel l) => l.Kind == ReportLineKindModel.Absolute ? $"LINE {l.Value}" : $"LINE PLUS {l.Value}";

            for (int j = 0; j < firstLines.Count; j++)
                for (int i = 0; i < j; i++)
                {
                    var li = firstLines[i];
                    var lj = firstLines[j];
                    if (DifferentPresentWhen([.. li.PresentWhenCtxs], [.. lj.PresentWhenCtxs])) continue;
                    using var at = Edition.At(lj.Entry);
                    bool iAbsolute = li.Kind == ReportLineKindModel.Absolute, jAbsolute = lj.Kind == ReportLineKindModel.Absolute;
                    if (iAbsolute && jAbsolute && lj.Value < li.Value)
                        Violation(lj, $"the absolute lines {LineWords(li)} and {LineWords(lj)} of one report group are not in "
                            + "increasing numerical order and are not each subject to a different PRESENT WHEN clause "
                            + "(ISO §13.18.35.3 SR6 a))");
                    else if (iAbsolute && jAbsolute && lj.Value == li.Value)
                        Violation(lj, $"the lines {LineWords(li)} and {LineWords(lj)} of one report group overlap and are not "
                            + "each subject to a different PRESENT WHEN clause (ISO §13.18.35.3 SR6 b))");
                    else if (!iAbsolute && jAbsolute)
                        Violation(lj, $"the absolute line {LineWords(lj)} is described after the relative line {LineWords(li)} "
                            + "of one report group, and they are not each subject to a different PRESENT WHEN clause "
                            + "(ISO §13.18.35.3 SR6 e))");
                }

            if (!model.Paged) continue;
            var (upper, lower) = GroupLimits(g, model);
            var reportedEntries = new HashSet<Core.ReportGroupEntryContext?>(ReferenceEqualityComparer.Instance);
            foreach (var l in g.Lines)
            {
                if (l.Kind != ReportLineKindModel.Absolute || (l.Value >= upper && l.Value <= lower)) continue;
                if (!reportedEntries.Add(l.Entry)) continue;
                using var at = Edition.At(l.Entry);
                Violation(l, $"the absolute line {LineWords(l)} of {ReportGroupTypeWords(g.Kind).ToLowerInvariant()} report group "
                    + $"'{g.Name ?? "FILLER"}' lies "
                    + (l.Value < upper ? $"above the upper limit {upper}" : $"below the lower limit {lower}")
                    + " the report group may occupy (ISO §13.18.35.3 SR6 c); limits per §13.18.57.4 GR7/GR8 from the PAGE "
                    + "clause, §13.18.39.4)");
            }

            // §13.18.39.4 GR1/GR2 — a heading or footing group is confined to its region. A group of RELATIVE lines
            // only has no absolute line for SR6 c) to bound, but where §13.18.35.4 GR5 b puts its first line is a
            // number the description fixes: HEADING + integer-2 − 1 (a report heading, a page heading with no report
            // heading before it), FOOTING + integer-2 (a page footing, a report footing with no page footing before
            // it) — the seeds MinimumLastLine walks, from below only: a page heading after a report heading, a report
            // footing after a page footing, start LOWER (the previous line's number plus integer-2). So each of the two
            // questions is answered on every page the group is printed on:
            //   - its LAST line against the lower limit — GR2 d) "any report heading (when not on a page by itself) or
            //     page heading shall be defined so that it terminates before" FIRST DETAIL; GR2 a) "No report line will
            //     appear below" the page limit, which is the lower limit of a page or report footing (GR8 g);
            //   - its FIRST line against the upper limit (RelativeFirstLine) — GR2 c) "No report line will appear
            //     higher than" the HEADING integer; GR2 g) "Any page footing or report footing (when not on a page by
            //     itself) shall be defined so that it begins after" the FOOTING integer.
            // A body group is confined by the page-fit test it takes at run time (GR4), so it is not asked here.
            if (g.Kind is ReportGroupKindModel.ReportHeading or ReportGroupKindModel.PageHeading
                    or ReportGroupKindModel.PageFooting or ReportGroupKindModel.ReportFooting)
            {
                if (!g.Lines.Exists(l => l.Kind == ReportLineKindModel.Absolute)
                    && MinimumLastLine(g, model) is { } relativeEnd && relativeEnd > lower)
                {
                    var last = g.Lines[^1];
                    using var at = Edition.At(last.Entry);
                    Violation(last, $"the relative lines of {ReportGroupTypeWords(g.Kind).ToLowerInvariant()} report group "
                        + $"'{g.Name ?? "FILLER"}' reach line {relativeEnd}, past the group's lower limit {lower}, on every page "
                        + (g.Kind is ReportGroupKindModel.PageFooting or ReportGroupKindModel.ReportFooting
                            ? "(ISO §13.18.39.4 GR1, GR2 a): no report line will appear below the page limit"
                            : "(ISO §13.18.39.4 GR1, GR2 d): each report group shall be confined within its region of the page")
                        + "; limits per §13.18.57.4 GR8)");
                }

                // The candidates for the group's first line: a relative line is first when every line above it may be
                // absent (PRESENT WHEN, or a repetition guard), so the walk stops at the first unconditional line.
                foreach (var l in g.Lines)
                {
                    if (l.Kind != ReportLineKindModel.Absolute && !l.NextPage
                        && MinimumLastLine(g, model, only => ReferenceEquals(only, l)) is { } first
                        && first < RelativeFirstLineFloor(g, model))
                    {
                        using var at = Edition.At(l.Entry);
                        bool heading = g.Kind is ReportGroupKindModel.ReportHeading or ReportGroupKindModel.PageHeading;
                        Violation(l, $"the relative {LineWords(l)} can be the first line of "
                            + $"{ReportGroupTypeWords(g.Kind).ToLowerInvariant()} report group '{g.Name ?? "FILLER"}', and "
                            + $"§13.18.35.4 GR5 b) prints it on line {first}, "
                            + (heading ? $"above the HEADING integer {model.Heading}, the first line position on which a heading "
                                + "may be printed (ISO §13.18.39.4 GR2 c))"
                                : $"not after the FOOTING integer {model.Footing}, after which a footing shall begin (ISO "
                                + "§13.18.39.4 GR2 g))")
                            + "; limits per §13.18.57.4 GR7)");
                    }
                    if (l.PresentWhenCtxs.Count == 0 && l.RepetitionGuards.Count == 0) break;
                }
            }

            // d) — a group of unconditional absolute lines that ends in relative lines. The tail is every line after
            // the group's last absolute one; the rule is asked of all of it, unless each tail line carries a PRESENT
            // WHEN clause the others do not, and then of the vertically largest alone — each in turn, with the
            // unconditional lines, as the only one present.
            int lastAbsolute = g.Lines.FindLastIndex(l => l.Kind == ReportLineKindModel.Absolute);
            if (lastAbsolute >= 0 && lastAbsolute < g.Lines.Count - 1
                && g.Lines[lastAbsolute].PresentWhenCtxs.Count == 0 && g.Lines[lastAbsolute].RepetitionGuards.Count == 0)
            {
                var tail = g.Lines.Skip(lastAbsolute + 1).ToList();
                var upToAbsolute = new HashSet<ReportLineModel>(g.Lines.Take(lastAbsolute + 1), ReferenceEqualityComparer.Instance);
                var tailClauses = tail.Select(l => (IReadOnlyList<object>)[.. l.PresentWhenCtxs]).ToList();
                int? extent = EachDifferentPresentWhen(tailClauses)
                    ? tail.Select(only => MinimumLastLine(g, model, l => upToAbsolute.Contains(l) || ReferenceEquals(l, only))).Max()
                    : MinimumLastLine(g, model, _ => true);
                if (extent is { } last && last > lower)
                {
                    using var at = Edition.At(tail[^1].Entry);
                    Violation(tail[^1], $"the relative lines at the end of {ReportGroupTypeWords(g.Kind).ToLowerInvariant()} "
                        + $"report group '{g.Name ?? "FILLER"}' carry its last line to line {last}, past the group's lower limit "
                        + $"{lower}, and are not each subject to a different PRESENT WHEN clause (ISO §13.18.35.3 SR6 d))");
                }
            }
        }
    }

    /// <summary>⛔ THE LINE CLAUSE'S OPERAND RULES (ISO §13.18.35.3 SR3, SR5, SR7, SR8; kb/Work PB1001 + PB1002),
    /// screened ONCE per WRITTEN clause over the flat entry array — the <see cref="ScreenReportLineNesting"/>
    /// shape, so a §13.18.38 Format 3 subtree replay cannot report one clause once per repetition. They read the
    /// RD's page model (bound before the groups — <c>BindReportDescriptionClauses</c>) and the group's TYPE, which
    /// the level 1 entry carries (<see cref="GroupKindOf"/>, the one TYPE reader).
    /// <list type="bullet">
    /// <item>SR3 — "Neither integer-1 nor integer-2 shall exceed the page limit, or 9999 if the report is not
    /// divided into pages." The NEXT GROUP clause's SR1 is the same sentence and reads the same two limits.</item>
    /// <item>SR5 — "If the report is not divided into pages, all its LINE clauses shall be relative." ⚠ The bare
    /// <c>ON NEXT PAGE</c> operand is not the relative FORM (<c>{PLUS|+} integer-2</c>, SR3's "Integer-2 specifies
    /// a relative line number"), exactly as §13.18.37.3 SR3 — "only the relative form of the clause may be
    /// specified" — excludes NEXT GROUP NEXT PAGE from an unpaged report; a report of one page of indefinite
    /// length (§13.18.39.4 GR2a) has no next page to begin (docs/CONFORMANCE.md, the LINE NEXT PAGE block).</item>
    /// <item>SR7 — "Within a given report group description, a NEXT PAGE phrase, if present, shall be specified
    /// only in the first LINE clause." (A multiple LINE clause's later operands are SR10a's, in
    /// <see cref="MultipleLineOperands"/>, and are not reported twice.)</item>
    /// <item>SR8 — "The NEXT PAGE phrase may appear only in the description of a body group or a report
    /// footing."</item>
    /// </list></summary>
    private void ScreenReportLineClauses(Core.ReportGroupEntryContext[] entries, ReportModel model)
    {
        var kind = ReportGroupKindModel.Detail;
        bool firstLineClause = true, orPageHeading = false;
        foreach (var ge in entries)
        {
            if (int.TryParse(ge.levelNumber().GetText(), out int level) && level == 1)
            {
                kind = WrittenGroupKind(ge);
                orPageHeading = kind == ReportGroupKindModel.ControlHeading && WrittenGroupType(ge)?.OR() is not null;
                firstLineClause = true;
            }
            foreach (var clause in ge.reportGroupClause())
            {
                if (clause.reportLineClause() is not { } lc) continue;
                using var _ = Edition.At(lc);
                void Violation(string rule) =>
                    Edition.Error(DiagnosticCatalog.ReportLineClauseRule, $"RD '{model.Name}': {rule}");
                var ops = lc.reportLineOperand();
                for (int k = 0; k < ops.Length; k++)
                {
                    var op = ops[k];
                    bool absolute = op.integerOperand() is not null && op.reportRelativeSign() is null;
                    if (op.integerOperand() is { } lit)
                    {
                        if ((IntegerOperandValue(lit, $"RD '{model.Name}' entry") ?? RecoveredIntegerOperand) > model.VerticalLimit)
                            Violation($"{(absolute ? "integer-1" : "integer-2")} {lit.GetText()} exceeds {model.VerticalLimitWords}"
                                + "; neither integer-1 nor integer-2 shall exceed the page limit, or 9999 if the report "
                                + "is not divided into pages (ISO §13.18.35.3 SR3)");
                    }
                    if (!model.Paged && (absolute || op.NEXT() is not null))
                        Violation($"the report is not divided into pages, so all its LINE clauses shall be relative — "
                            + (absolute ? $"LINE {op.integerOperand()!.GetText()}{(op.NEXT() is not null ? " ON NEXT PAGE" : "")} is absolute"
                                        : "the ON NEXT PAGE operand is not the relative form {PLUS|+} integer-2")
                            + " (ISO §13.18.35.3 SR5)");
                    // SR9 — "If the current report group is a control heading with the OR PAGE phrase, all the LINE
                    // clauses in the report group description shall be relative." A bare NEXT PAGE operand is no
                    // relative form either, exactly as SR5 reads it (kb/Work PB1248).
                    if (orPageHeading && (absolute || op.NEXT() is not null))
                        Violation("a control heading with the OR PAGE phrase shall have only relative LINE clauses — "
                            + (absolute ? $"LINE {op.integerOperand()!.GetText()}{(op.NEXT() is not null ? " ON NEXT PAGE" : "")} is absolute"
                                        : "the ON NEXT PAGE operand is not the relative form {PLUS|+} integer-2")
                            + " (ISO §13.18.35.3 SR9)");
                    if (op.NEXT() is null) continue;
                    if (k == 0 && !firstLineClause)
                        Violation("a NEXT PAGE phrase, if present, shall be specified only in the first LINE clause of "
                            + "the report group description (ISO §13.18.35.3 SR7)");
                    if (kind is not (ReportGroupKindModel.ControlHeading or ReportGroupKindModel.Detail
                        or ReportGroupKindModel.ControlFooting or ReportGroupKindModel.ReportFooting))
                        Violation($"the NEXT PAGE phrase may appear only in the description of a body group or a report "
                            + $"footing, not a {ReportGroupTypeWords(kind)} (ISO §13.18.35.3 SR8)");
                }
                firstLineClause = false;
            }
        }
    }

    /// <summary>⛔ THE MULTIPLE COLUMN CLAUSE'S RULES (ISO §13.18.14.3 SR10; kb/Work PB1222), screened ONCE per WRITTEN
    /// clause over the flat entry array — the <see cref="ScreenReportLineClauses"/> shape, and the COLUMN arm of the
    /// two-arm rule whose LINE arm (§13.18.35.3 SR10 c)/d)) <see cref="MultipleLineOperands"/> already screens. "If more
    /// than one integer-1 or integer-2 operand is specified the clause is referred to as a multiple COLUMN clause and
    /// the following additional rules apply: a) No OCCURS clause shall be specified in the same entry. b) All the
    /// occurrences of integer-1 shall be in increasing order of magnitude." Neither has a PRESENT WHEN excuse.</summary>
    private void ScreenReportColumnClauses(Core.ReportGroupEntryContext[] entries, ReportModel model)
    {
        foreach (var ge in entries)
            foreach (var clause in ge.reportGroupClause())
            {
                if (clause.reportColumnClause() is not { } cc || cc.reportColumnOperand().Length <= 1) continue;
                using var _ = Edition.At(cc);
                string where = $"RD '{model.Name}' entry '{ge.dataName().NameOrNull() ?? "FILLER"}'";
                if (ge.reportGroupClause().Any(c => c.occursClause() is not null))
                    Edition.Error(DiagnosticCatalog.ReportColumnClauseRule, $"{where}: a multiple COLUMN clause and an OCCURS "
                        + "clause may not both be present in the same report group description entry (ISO §13.18.14.3 SR10 a))");
                int? last = null;
                foreach (var op in cc.reportColumnOperand())
                {
                    if (op.reportRelativeSign() is not null || op.integerOperand() is not { } lit) continue;
                    if (IntegerOperandValue(lit, where) is not { } v) continue;
                    if (last is { } previous && v <= previous)
                        Edition.Error(DiagnosticCatalog.ReportColumnClauseRule, $"{where}: in a multiple COLUMN clause all the "
                            + $"occurrences of integer-1 shall be in increasing order of magnitude — {v} follows {previous} "
                            + "(ISO §13.18.14.3 SR10 b))");
                    last = v;
                }
            }
    }

    /// <summary>ISO §13.18.35.3 SR4 — "Within a given report group description entry, an entry that contains a
    /// LINE clause shall not have a subordinate entry that also contains a LINE clause." Screened over the FLAT
    /// entry array once per RD, before the walk, so a subtree REPLAY (§13.18.38.4 GR10) cannot report the same
    /// violation once per repetition. The rule is what makes the §13.18.38.4 GR10c/GR10d split exhaustive: a
    /// vertically repeating entry either carries the LINE clause itself or has them below it, never both.</summary>
    private void ScreenReportLineNesting(Core.ReportGroupEntryContext[] entries, ReportModel model)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (!entries[i].reportGroupClause().Any(c => c.reportLineClause() is not null)) continue;
            if (!int.TryParse(entries[i].levelNumber().GetText(), out int level)) continue;
            for (int k = i + 1; k < entries.Length; k++)
            {
                if (!int.TryParse(entries[k].levelNumber().GetText(), out int sub) || sub <= level) break;
                if (!entries[k].reportGroupClause().Any(c => c.reportLineClause() is not null)) continue;
                using var _ = Edition.At(entries[k]);
                Edition.Error(DiagnosticCatalog.ReportLineClauseRule, $"RD '{model.Name}': an entry that contains a LINE "
                    + "clause shall not have a subordinate entry that also contains a LINE clause (ISO "
                    + "§13.18.35.3 SR4)");
                break;
            }
        }
    }

    /// <summary>⛔ THE §13.15.3 CLAUSE-PRESENCE RULES, screened ONCE per WRITTEN entry over the flat entry array
    /// (kb/Work PB853) — the <see cref="ScreenReportLineNesting"/> shape, and for the same reason: they are
    /// properties of the entry as written, and the §13.18.38 Format 3 subtree replay binds OCCURRENCES of it, so a
    /// screen inside <c>BindReportEntry</c> would report once per repetition (kb/Work PB884).
    /// <list type="bullet">
    /// <item>SR10 — "Every elementary entry with a COLUMN clause shall also contain either a SOURCE, VALUE or SUM
    /// clause." It had NO site, and the binder FABRICATED the missing operand: a figurative SPACE sender stood in
    /// for the clause the programmer never wrote, so <c>03 COLUMN 1 PIC 999.</c> printed <c>000</c> (and at one
    /// point aborted the run unit), while the PICTURE-less <c>03 COLUMN 1.</c> was refused only by accident, under
    /// the SR12 PICTURE rule. The fabrication is deleted; this screen is what makes that sound.</item>
    /// <item>SR11 — "The PICTURE, COLUMN, SOURCE, VALUE, SUM, and GROUP INDICATE clauses may be written only in
    /// an elementary entry." A group entry's PICTURE and VALUE were silently discarded.</item>
    /// <item>SR13 — "A COLUMN clause shall be specified in each elementary entry that has a VALUE clause." A
    /// column-less VALUE entry printed nothing, in silence.</item>
    /// <item>SR15 — "If BLANK WHEN ZERO or JUSTIFIED is specified, a COLUMN clause shall also be specified."</item>
    /// <item>§13.18.28.3 SR1 — "The GROUP INDICATE clause may be specified only within a detail report group
    /// description, in an elementary entry that also contains a COLUMN clause and a SOURCE or VALUE clause." Its
    /// elementary half is SR11 above; the detail-group, COLUMN and SOURCE-or-VALUE halves had NO site, so the
    /// clause in a control heading, or on an entry that prints nothing, compiled in silence (kb/Work PB1245).
    /// The group's TYPE is read from its level-01 entry, which precedes every subordinate entry.</item>
    /// <item>SR5 — "The TYPE clause may be specified only in a level 1 entry and shall be specified in every level 1
    /// entry." (kb/Work PB1288)</item>
    /// <item>SR9 — "Every elementary entry with a COLUMN clause but no LINE clause shall be subordinate to an entry
    /// with a LINE clause", read off the level hierarchy (<see cref="SubordinateToLineClause"/>), not off the line the
    /// build has open (kb/Work PB1224).</item>
    /// <item>SR12 — a PICTURE clause in every elementary entry that has a SOURCE or SUM clause, printable or not.</item>
    /// </list>
    /// SR8 is <see cref="ScreenReportLineNesting"/> (its §13.18.35.3 SR4 twin); the VALUE-implied picture of §13.15.3
    /// SR14 is the binder's PICTURE arm, which needs the analysed picture.
    /// An entry is ELEMENTARY when the entry after it is not subordinate to it (§13.15.4 GR1: "The report group is
    /// defined by this entry and all its subordinate entries").</summary>
    private void ScreenReportEntryClausePresence(Core.ReportGroupEntryContext[] entries, ReportModel model)
    {
        var groupKind = ReportGroupKindModel.Detail;
        for (int i = 0; i < entries.Length; i++)
        {
            var ge = entries[i];
            int.TryParse(ge.levelNumber().GetText(), out int level);
            if (level == 1) groupKind = WrittenGroupKind(ge);
            bool elementary = i + 1 >= entries.Length
                || !int.TryParse(entries[i + 1].levelNumber().GetText(), out int next) || next <= level;
            bool column = false, source = false, value = false, sum = false, picture = false, groupIndicate = false,
                justifiedOrBwz = false, type = false, line = false;
            foreach (var c in ge.reportGroupClause())
            {
                type |= c.reportTypeClause() is not null;
                line |= c.reportLineClause() is not null;
                column |= c.reportColumnClause() is not null;
                source |= c.reportSourceClause() is not null;
                value |= c.valueClause() is not null;
                sum |= c.reportSumClause() is not null;
                picture |= c.pictureClause() is not null;
                groupIndicate |= c.reportGroupIndicateClause() is not null;
                justifiedOrBwz |= c.justifiedClause() is not null || c.blankWhenZeroClause() is not null;
            }
            string where = $"RD '{model.Name}' entry '{ge.dataName().NameOrNull() ?? "FILLER"}'";
            using var _ = Edition.At(ge);
            // SR5 — "The TYPE clause may be specified only in a level 1 entry and shall be specified in every level 1
            // entry." Both halves are properties of the WRITTEN entry. The binder used to honour a TYPE clause at any
            // level (retyping the enclosing group) and to make a TYPE-less level 1 entry a detail group by default
            // (kb/Work PB1288).
            if (level == 1 && !type)
                Edition.Error(DiagnosticCatalog.ReportEntryClausePresence, $"{where} is a level 1 entry with no TYPE clause; "
                    + "the TYPE clause shall be specified in every level 1 entry (ISO §13.15.3 SR5)");
            else if (level != 1 && type)
                Edition.Error(DiagnosticCatalog.ReportEntryClausePresence, $"{where} writes a TYPE clause at level {level}; "
                    + "the TYPE clause may be specified only in a level 1 entry (ISO §13.15.3 SR5)");
            // SR9 — "Every elementary entry with a COLUMN clause but no LINE clause shall be subordinate to an entry
            // with a LINE clause" (and §13.18.14.3 SR3, the same rule from the COLUMN clause's side). It is a rule
            // about the entry's ANCESTRY, so it reads the level hierarchy — never the line the build happens to have
            // opened last, which a COLUMN entry that follows a LINE entry's subtree without being subordinate to it
            // would wrongly inherit (kb/Work PB1224).
            if (elementary && column && !line && !SubordinateToLineClause(entries, i))
                Edition.Error(DiagnosticCatalog.ReportEntryClausePresence, $"{where} has a COLUMN clause but neither a LINE "
                    + "clause of its own nor an entry above it that has one; every elementary entry with a COLUMN clause "
                    + "but no LINE clause shall be subordinate to an entry with a LINE clause (ISO §13.15.3 SR9, "
                    + "§13.18.14.3 SR3)");
            // SR12 — "A PICTURE clause shall be specified in every elementary entry that has a SOURCE or SUM clause."
            // The rule has no COLUMN condition: an unprintable SOURCE entry and a SUM entry that prints nothing need
            // one too (§13.18.54.4 GR1 sizes the counter from it). SR14's VALUE-implied picture is the VALUE
            // clause's alone.
            if (elementary && (source || sum) && !picture)
                Edition.Error(DiagnosticCatalog.ReportEntryClausePresence, $"{where} has a {(sum ? "SUM" : "SOURCE")} clause "
                    + "but no PICTURE clause; a PICTURE clause shall be specified in every elementary entry that has a "
                    + "SOURCE or SUM clause (ISO §13.15.3 SR12)");
            // §13.18.54.2 — the SUM clause is the repeated `SUM OF … [UPON …]` group followed by ONE RESET phrase and ONE
            // rounded-phrase; the grammar's reportSumClause takes the groups together, so a SECOND clause in an entry
            // means a RESET or ROUNDED phrase (or another clause) was written BETWEEN two SUM groups (kb/Work PB1295).
            if (ge.reportGroupClause().Count(c => c.reportSumClause() is not null) > 1)
                Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"{where} writes its SUM clause in more than one "
                    + "piece: the RESET phrase and the rounded-phrase come once, after the whole repeated SUM … UPON "
                    + "group, so neither of them, nor any other clause, may be written between two SUM groups (ISO §13.18.54.2)");
            if (!elementary)
            {
                var written = new List<string>();
                if (picture) written.Add("PICTURE");
                if (column) written.Add("COLUMN");
                if (source) written.Add("SOURCE");
                if (value) written.Add("VALUE");
                if (sum) written.Add("SUM");
                if (groupIndicate) written.Add("GROUP INDICATE");
                if (written.Count > 0)
                    Edition.Error(DiagnosticCatalog.ReportEntryClausePresence, $"{where} is a group entry and writes "
                        + $"{string.Join(", ", written)}; the PICTURE, COLUMN, SOURCE, VALUE, SUM, and GROUP INDICATE "
                        + "clauses may be written only in an elementary entry (ISO §13.15.3 SR11)");
            }
            else
            {
                if (column && !source && !value && !sum)
                    Edition.Error(DiagnosticCatalog.ReportEntryClausePresence, $"{where} has a COLUMN clause but no "
                        + "SOURCE, VALUE or SUM clause; every elementary entry with a COLUMN clause shall also "
                        + "contain either a SOURCE, VALUE or SUM clause (ISO §13.15.3 SR10)");
                if (value && !column)
                    Edition.Error(DiagnosticCatalog.ReportEntryClausePresence, $"{where} has a VALUE clause but no "
                        + "COLUMN clause; a COLUMN clause shall be specified in each elementary entry that has a "
                        + "VALUE clause (ISO §13.15.3 SR13)");
            }
            if (justifiedOrBwz && !column)
                Edition.Error(DiagnosticCatalog.ReportEntryClausePresence, $"{where} specifies BLANK WHEN ZERO or "
                    + "JUSTIFIED without a COLUMN clause; if BLANK WHEN ZERO or JUSTIFIED is specified, a COLUMN "
                    + "clause shall also be specified (ISO §13.15.3 SR15)");
            if (groupIndicate)
            {
                // §13.18.28.3 SR1 — the three placement conditions the elementary rule (§13.15.3 SR11, above)
                // leaves open. The COLUMN and SOURCE-or-VALUE halves are asked of an elementary entry only: a group
                // entry writing the clause is already the §13.15.3 error, and naming its missing COLUMN too would
                // report one fault twice.
                var faults = new List<string>();
                if (groupKind != ReportGroupKindModel.Detail)
                    faults.Add($"is in a {ReportGroupTypeWords(groupKind)} report group");
                if (elementary && !column) faults.Add("has no COLUMN clause");
                if (elementary && !source && !value) faults.Add("has neither a SOURCE nor a VALUE clause");
                if (faults.Count > 0)
                    Edition.Error(DiagnosticCatalog.ReportGroupIndicatePlacement, $"{where} specifies GROUP INDICATE "
                        + $"but {string.Join(" and ", faults)}; the GROUP INDICATE clause may be specified only within "
                        + "a detail report group description, in an elementary entry that also contains a COLUMN "
                        + "clause and a SOURCE or VALUE clause (ISO §13.18.28.3 SR1)");
            }
        }
    }

    /// <summary>
    /// The mutable state of one RD's group build — what the flat entry walk carries from entry to entry, and what
    /// a REPEATING ENTRY's replay (ISO §13.18.38 Format 3) therefore has to carry across its repetitions.
    /// </summary>
    private sealed class ReportGroupBuild
    {
        public ReportGroupModel? Group;
        public ReportLineModel? Line;
        /// <summary>The entry scope stack: one frame per entry on the current level path. It carries the
        /// PRESENT WHEN condition (§13.18.41.4 GR2b) AND the frame entry's REPETITION COUNT (§13.15.4 GR3), so
        /// §13.18.63.3 SR35 / §13.18.53.3 SR6 can read "the repeating entry, and any number of successive
        /// repeating entries at higher levels" off the stack instead of a hand-maintained special case
        /// (kb/Work PB506).</summary>
        /// <para>The frame also carries the entry's EFFECTIVE usage (ISO §13.18.60.4 GR1 — "If the USAGE
        /// clause is specified or implied at a group level, it applies only to each elementary item in the
        /// group"), so a printable item inherits the usage written on a group entry above it instead of the
        /// clause being discarded (kb/Work PB541).</para>
        public readonly List<(int Level, Core.ConditionContext? Cond, int Reps, string? Name)> Chain = [];
        /// <summary>Stack frames whose conditions the CURRENT line already carries.</summary>
        public int LineChainDepth;
        /// <summary>The repeating entries enclosing the entry being bound, outermost first (§13.18.38 Format 3).</summary>
        public readonly List<ReportRepetitionFrame> Repetitions = [];
        /// <summary>The VARYING declarations in scope for the entry being bound (§13.18.64.3 SR2 — "only within the
        /// current entry or a subordinate entry"), outermost first: the declaring entry, its counters, and the
        /// repetition frame whose ordinal is the occurrence being bound (null for a leaf that repeats only through
        /// a multiple COLUMN clause, whose occurrences are its placements).</summary>
        public readonly List<(Core.ReportGroupEntryContext Entry, IReadOnlyList<ReportVaryingModel> Counters,
            ReportRepetitionFrame? Frame)> Varying = [];
        /// <summary>The step-anchor register ids, keyed by (entry, COLUMN-operand index) — one per PRINTABLE
        /// PLACEMENT of a repeating entry whose base column is relative (see <see cref="ReportColumnKindModel"/>).</summary>
        public readonly Dictionary<(Core.ReportGroupEntryContext Entry, int Operand, string Undisplaced), int> Anchors = [];
        /// <summary>The VERTICAL twin, keyed the same way over LINE operands — one slot per report LINE of a
        /// STEP'd vertically repeating entry whose line is relative (§13.18.38.4 GR12c/GR12d). Separate from
        /// <see cref="Anchors"/> because the two live in different storage: a column anchor is compose-local to
        /// ONE line, a line anchor spans the whole group presentation and belongs to the engine.</summary>
        public readonly Dictionary<(Core.ReportGroupEntryContext Entry, int Operand, string Undisplaced), int> LineAnchors = [];
        /// <summary>Placements of each entry bound so far — <see cref="ReportFieldModel.RepetitionOrdinal"/>.</summary>
        public readonly Dictionary<Core.ReportGroupEntryContext, int> Placements = [];
        /// <summary>The <see cref="ReportSumFamily"/> of each SUM entry, created at the entry's FIRST replay (when its
        /// counter-id block is reserved) and found again by every later one (kb/Work PB1271).</summary>
        public readonly Dictionary<Core.ReportGroupEntryContext, ReportSumFamily> SumFamilies = [];
        /// <summary>The <see cref="ReportSourceFamily"/> of each SOURCE / VALUE entry, found again by every replay
        /// (kb/Work PB1294) — the twin of <see cref="SumFamilies"/> for the entries a rolled total may name.</summary>
        public readonly Dictionary<Core.ReportGroupEntryContext, ReportSourceFamily> SourceFamilies = [];
        /// <summary>The bind-time EXPECTED vertical offset of the last relative line placed in the group under
        /// construction, measured from the group's own start. It is the §13.18.35.4 GR4c trial sum read
        /// forwards: each line's <see cref="ReportLineModel.TrialInterval"/> is its expected offset minus this
        /// cursor, so Σ intervals is exactly "the expected position of the last line of the report group" for an
        /// all-relative group, whatever mixture of plain repetition and STEP displacement produced it.</summary>
        public int VerticalCursor;
        /// <summary>Each line anchor's expected offset at the moment it was seeded — the datum a Step line's
        /// interval is computed from.</summary>
        public readonly Dictionary<int, int> AnchorOffset = [];

        /// <summary>Σ ordinalⱼ × integer-3ⱼ over the enclosing repeating entries THAT REPEAT ON
        /// <paramref name="axis"/> — the displacement §13.18.38.4 GR12 gives this repetition ("integer-3 columns
        /// to the right of the column they occupy in the preceding occurrence" horizontally, GR12a/GR12b;
        /// "integer-3 lines vertically beneath the line they occupy in the preceding occurrence" vertically,
        /// GR12c/GR12d), which is additive across nested repeating entries of the same axis. A frame on the OTHER
        /// axis contributes nothing: its integer-3 is an interval in the other dimension entirely.</summary>
        public int Shift(ReportRepetitionAxis axis)
        {
            int shift = 0;
            foreach (var rep in Repetitions) shift += rep.Ordinal * StepOn(rep, axis);
            return shift;
        }

        /// <summary>The ordinals of the enclosing repeating entries that do NOT displace on
        /// <paramref name="axis"/> — the rest of a step anchor's key, so genuinely distinct items (produced by a
        /// repetition that steps on the other axis, or not at all) never share one datum.</summary>
        public string Undisplaced(ReportRepetitionAxis axis) =>
            string.Join(".", Repetitions.Where(r => StepOn(r, axis) == 0).Select(r => r.Ordinal));

        /// <summary>One frame's integer-3 as it acts on <paramref name="axis"/>: its STEP when the entry repeats
        /// on that axis, else 0 (ISO §13.18.38.4 GR12).</summary>
        private static int StepOn(ReportRepetitionFrame f, ReportRepetitionAxis axis) =>
            f.Spec.Axis == axis ? f.Spec.Step ?? 0 : 0;

        /// <summary>The §13.18.38.4 GR13 presence tests this placement inherits (one per enclosing repeating
        /// entry with a DEPENDING phrase), outermost first.</summary>
        public List<ReportRepetitionGuard> GuardsHere()
        {
            var guards = new List<ReportRepetitionGuard>();
            foreach (var rep in Repetitions)
                if (rep.Spec.DependingName is not null) guards.Add(new ReportRepetitionGuard(rep.Spec, rep.Ordinal));
            return guards;
        }
    }

    /// <summary>One enclosing repeating entry during the replay: its OCCURS clause and the ordinal being bound.</summary>
    private sealed class ReportRepetitionFrame(ReportOccursSpec spec)
    {
        public ReportOccursSpec Spec { get; } = spec;
        public int Ordinal { get; set; }
    }

    /// <summary>
    /// Bind the report group entries in <c>[start, end)</c>. A REPEATING ENTRY (ISO §13.18.38 Format 3) binds its
    /// own subtree once per repetition — "it causes the entry to define integer-2 distinct report items"
    /// (§13.18.38.4 GR10) — which is the ONE place repetition is expressed, so every clause of every repeated
    /// entry gets GR11's "same effect on each repetition as they would on a single data item without the OCCURS
    /// clause" for free, §13.18.63.4 GR21's import of GR9 included.
    /// </summary>
    private void BindReportEntries(
        Core.ReportGroupEntryContext[] entries, int start, int end, ReportModel model, ReportGroupBuild st)
    {
        for (int i = start; i < end; i++)
        {
            var ge = entries[i];
            int.TryParse(ge.levelNumber().GetText(), out int entryLevel);
            // ⛔ §13.18.63.3 SR30 — "Condition-name and content-validation formats shall not be specified in
            // the report section" — NEEDS NO SCREEN OF ITS OWN HERE, and this comment is why (kb/Work PB558).
            // Both formats it names are level-88 formats: §13.18.63.3 SR33, "Formats 3 and 5 may be specified
            // only when the level-number of the subject of the entry is 88." And §13.18.33.3 SR4 already bounds
            // this section's level-numbers — "Report group description entries that are subordinate to an RD
            // entry shall have level-numbers with the values 1 through 49" — enforced over the whole parse tree
            // by LevelNumberPass, COBOLNET1746. The conjunction is exact: a format-3 or format-5 VALUE clause in
            // the report section requires a level-number this section does not admit, so SR30 is a consequence
            // of two rules each screened at the one place it belongs, not a third rule to write down here.
            // Pinned by tests/conformance/negative/pb558-condition-name-in-report-section, all four editions.
            //
            // The finding this replaced WAS real when it was measured: the walk dropped an 88 entry in silence,
            // and a later `IF condition-name` compiled and then aborted at RUN time. PB485 (f37da577b,
            // 2026-09-05) gave the level-number a domain on both its axes and closed it — measured again here
            // before writing this, not assumed.
            // The entry's SUBTREE: itself plus every following entry of a higher level number (§13.15 — the
            // level-number hierarchy). It is what a repeating entry replays.
            int subtreeEnd = i + 1;
            while (subtreeEnd < end && int.TryParse(entries[subtreeEnd].levelNumber().GetText(), out int lv)
                   && lv > entryLevel) subtreeEnd++;

            // The entry's VARYING counters (§13.18.64.1): made ONCE for this entry within the enclosing occurrence being
            // bound, and in scope for the entry and its subtree — which a repeating entry's replay encloses, so
            // every repetition shares the counter and GR3's occurrence number is the repetition's ordinal.
            var counters = VaryingCountersOf(ge, st);
            if (ReportRepetitionOf(ge, entries, i, subtreeEnd, model, st) is { } occurs)
            {
                var frame = new ReportRepetitionFrame(occurs);
                st.Repetitions.Add(frame);
                if (counters.Count > 0) st.Varying.Add((ge, counters, frame));
                for (int rep = 0; rep < occurs.Max; rep++)
                {
                    frame.Ordinal = rep;
                    BindReportEntry(new ReportEntryLocation(entries, i, model.Name), entries[i], model, st, occurs, rep);
                    BindReportEntries(entries, i + 1, subtreeEnd, model, st);
                }
                if (counters.Count > 0) st.Varying.RemoveAt(st.Varying.Count - 1);
                st.Repetitions.RemoveAt(st.Repetitions.Count - 1);
                i = subtreeEnd - 1;
                continue;
            }
            if (counters.Count > 0) st.Varying.Add((ge, counters, null));
            BindReportEntry(new ReportEntryLocation(entries, i, model.Name), ge, model, st);
            if (counters.Count > 0) st.Varying.RemoveAt(st.Varying.Count - 1);
        }
    }

    /// <summary>The VARYING counters the entry <paramref name="ge"/> declares (ISO §13.18.64.2 — <c>VARYING { data-name-1
    /// [FROM arithmetic-expression-1] [BY arithmetic-expression-2] } …</c>), empty for an entry with no VARYING
    /// clause. The rules over the WRITTEN clause (SR1–SR3) are asked once per written entry by
    /// <see cref="ScreenReportVaryingClauses"/>, not here, so a replay cannot multiply them.</summary>
    private List<ReportVaryingModel> VaryingCountersOf(Core.ReportGroupEntryContext ge, ReportGroupBuild st)
    {
        var specs = ge.reportGroupClause().Select(c => c.reportVaryingClause()).OfType<Core.ReportVaryingClauseContext>()
            .SelectMany(vc => vc.reportVaryingSpec()).ToList();
        if (specs.Count == 0) return [];
        var enclosing = st.Varying.SelectMany(v => v.Counters).ToList();
        var names = specs.Select(s => s.cobolWord().GetText()).ToList();
        var group = new List<ReportVaryingModel>(specs.Count);
        foreach (var spec in specs)
        {
            var byCtx = spec.BY() is not null ? spec.arithmeticExpression(spec.FROM() is not null ? 1 : 0) : null;
            group.Add(new ReportVaryingModel
            {
                Name = spec.cobolWord().GetText(),
                FromCtx = spec.FROM() is not null ? spec.arithmeticExpression(0) : null,
                ByCtx = byCtx,
                Uid = _uidCounter++,
                Entry = ge,
                Enclosing = enclosing,
                Recurrent = byCtx is not null && names.Any(n => HasWord(byCtx, n)),
            });
        }
        foreach (var v in group) v.Group = group;
        return group;
    }

    /// <summary>
    /// ⛔ THE ONE REPETITION VEHICLE OF A REPORT GROUP DESCRIPTION ENTRY. ISO §13.15.4 GR3 names three — "An
    /// entry that contains either an OCCURS clause or a LINE or COLUMN clause with more than one operand is said
    /// to be a repeating entry" — and two of them drive the SUBTREE REPLAY: the OCCURS clause (§13.18.38 Format
    /// 3) and the multiple LINE clause. §13.18.35.4 GR9 is what makes the second one the first: "A multiple LINE
    /// clause is functionally equivalent to a LINE clause with a single operand, together with a simple OCCURS
    /// clause whose integer is equal to the number of operands of the LINE clause, except that the multiple LINE
    /// clause allows the report lines to be defined at unequal vertical intervals" — so it IS a simple,
    /// STEP-less, vertical OCCURS whose per-repetition operand the LINE clause supplies, and modelling it as one
    /// is the standard's own reduction rather than a second copy of repetition.
    /// <para>The third vehicle, the multiple COLUMN clause, is an operand LIST on ONE printable item rather than
    /// a replay (§13.18.14.3 SR10), and <see cref="ReportFieldModel.Columns"/> carries it.</para>
    /// </summary>
    private ReportOccursSpec? ReportRepetitionOf(
        Core.ReportGroupEntryContext ge, Core.ReportGroupEntryContext[] entries, int self, int subtreeEnd,
        ReportModel model, ReportGroupBuild st)
    {
        var occurs = ReportOccursOf(ge, entries, self, subtreeEnd, model, st);
        using var _ = Edition.At(ge);   // every rule below is the ENTRY's, so it is reported at the entry
        var multi = MultipleLineOperands(ge, model);
        if (multi is not { } ops) return occurs;
        // SR10 d) "An OCCURS clause shall not also be present in the same entry."
        if (ge.reportGroupClause().Any(c => c.occursClause() is not null))
        {
            Edition.Error(DiagnosticCatalog.ReportLineClauseRule, $"RD '{model.Name}': a multiple LINE clause and an "
                + "OCCURS clause may not both be present in the same report group description entry (ISO "
                + "§13.18.35.3 SR10d)");
            return occurs;
        }
        // §13.18.35.4 GR9's equivalence, written as the spec writes it: a simple OCCURS of `ops` repetitions on
        // the VERTICAL axis. No STEP — GR9's "unequal vertical intervals" ARE the several LINE operands.
        return new ReportOccursSpec(0, ops, null, [], null) { Axis = ReportRepetitionAxis.Vertical };
    }

    /// <summary>The operand count of this entry's MULTIPLE LINE clause (ISO §13.18.35.3 SR10) with SR10 a/b/c
    /// enforced, or null when the entry has no LINE clause or a single-operand one.</summary>
    private int? MultipleLineOperands(Core.ReportGroupEntryContext ge, ReportModel model)
    {
        Core.ReportLineClauseContext? lc = null;
        foreach (var clause in ge.reportGroupClause())
            if (clause.reportLineClause() is { } found) lc = found;
        if (lc is null) return null;
        var ops = lc.reportLineOperand();
        if (ops.Length <= 1) return null;
        int lastAbsolute = int.MinValue;
        bool relativeSeen = false;
        for (int k = 0; k < ops.Length; k++)
        {
            // a) "The NEXT PAGE phrase, if specified, shall appear only with the first operand."
            if (k > 0 && ops[k].NEXT() is not null)
                Edition.Error(DiagnosticCatalog.ReportLineClauseRule, $"RD '{model.Name}': in a multiple LINE clause the "
                    + "NEXT PAGE phrase shall appear only with the first operand (ISO §13.18.35.3 SR10a)");
            if (ops[k].integerOperand() is not { } lit) continue;   // a bare `ON NEXT PAGE` operand
            if (ops[k].reportRelativeSign() is not null) { relativeSeen = true; continue; }
            // b) "All absolute operands, if present, shall precede all relative operands, if present."
            if (relativeSeen)
            {
                Edition.Error(DiagnosticCatalog.ReportLineClauseRule, $"RD '{model.Name}': in a multiple LINE clause all "
                    + "absolute operands shall precede all relative operands (ISO §13.18.35.3 SR10b)");
                continue;
            }
            // c) "The occurrences of integer-1, if present, shall be in ascending numerical order."
            int v = IntegerOperandValue(lit, $"RD '{model.Name}' entry") ?? RecoveredIntegerOperand;
            if (lastAbsolute > int.MinValue && v <= lastAbsolute)
                Edition.Error(DiagnosticCatalog.ReportLineClauseRule, $"RD '{model.Name}': in a multiple LINE clause the "
                    + $"occurrences of integer-1 shall be in ascending numerical order — {v} follows "
                    + $"{lastAbsolute} (ISO §13.18.35.3 SR10c)");
            lastAbsolute = v;
        }
        return ops.Length;
    }

    /// <summary>
    /// The OCCURS clause of one report group description entry, as ISO §13.18.38 FORMAT 3 — the report-writer
    /// format, <c>OCCURS [ integer-1 TO ] integer-2 TIMES [ DEPENDING ON data-name-1 ] [ STEP integer-3 ]</c> —
    /// with its syntax rules enforced; null when the entry is not a repeating entry or a syntax rule rejected it.
    /// </summary>
    private ReportOccursSpec? ReportOccursOf(
        Core.ReportGroupEntryContext ge, Core.ReportGroupEntryContext[] entries, int self, int subtreeEnd,
        ReportModel model, ReportGroupBuild st)
    {
        Core.OccursClauseContext? oc = null;
        foreach (var clause in ge.reportGroupClause())
            if (clause.occursClause() is { } found) oc = found;
        if (oc is null) return null;

        using var _ = Edition.At(ge);
        string name = ge.dataName().NameOrNull() ?? "FILLER";
        string where = $"RD '{model.Name}' entry '{name}'";
        int.TryParse(ge.levelNumber().GetText(), out int level);

        // The report-writer format has NO DYNAMIC, KEY or INDEXED BY phrase (§13.18.38.2 Format 3, verified
        // against the printed general-format diagram); §13.18.38.3 SR3/SR7/SR8 place those in Formats 1, 2 and 4.
        if (oc.DYNAMIC() is not null || oc.occursKeyClause().Length > 0 || oc.INDEXED() is not null)
        {
            Edition.Error(DiagnosticCatalog.ReportOccursFormat3Rule, $"{where}: an OCCURS clause in a report group description "
                + "entry is the report-writer format — OCCURS [integer-1 TO] integer-2 TIMES [DEPENDING ON "
                + "data-name-1] [STEP integer-3]; the DYNAMIC, ASCENDING/DESCENDING KEY and INDEXED BY phrases "
                + "belong to formats 1, 2 and 4 (ISO §13.18.38.2)");
            return null;
        }
        // SR1a — "The OCCURS clause shall not be specified in a data description entry that … has a level-number
        // of 01, 66, 77, or 88". A report group description entry's level-number is 1 through 49 (§13.15.3 SR3).
        if (level == 1)
        {
            Edition.Error(DiagnosticCatalog.ReportOccursFormat3Rule, $"{where}: the OCCURS clause shall not be specified in an "
                + "entry that has a level-number of 01 (ISO §13.18.38.3 SR1a)");
            return null;
        }

        var bounds = oc.integerOperand();
        bool hasTo = oc.TO() is not null;
        var depending = oc.dataReference();
        int? step = oc.occursStepPhrase() is { } sp ? IntegerOperandValue(sp.integerOperand(), where) ?? RecoveredIntegerOperand : null;
        int max = IntegerOperandValue(bounds[^1], where) ?? 0;
        int min = hasTo ? IntegerOperandValue(bounds[0], where) ?? 0 : 0;

        // SR24 — "The TO and DEPENDING phrases shall either be both absent or both present."
        if (hasTo != (depending is not null))
        {
            Edition.Error(DiagnosticCatalog.ReportOccursFormat3Rule, $"{where}: the TO and DEPENDING phrases shall either be "
                + "both absent or both present (ISO §13.18.38.3 SR24)");
            return null;
        }
        // SR16 (formats 2 and 3) — "Integer-1 shall be greater than or equal to zero and integer-2 shall be
        // greater than integer-1."
        if (hasTo && (min < 0 || max <= min))
        {
            Edition.Error(DiagnosticCatalog.ReportOccursFormat3Rule, $"{where}: OCCURS {min} TO {max} — integer-1 shall be greater "
                + "than or equal to zero and integer-2 shall be greater than integer-1 (ISO §13.18.38.3 SR16)");
            return null;
        }
        if (max <= 0)
        {
            Edition.Error(DiagnosticCatalog.ReportOccursFormat3Rule, $"{where}: OCCURS {max} TIMES — integer-2 is the number of "
                + "repetitions the entry defines and shall be positive (ISO §13.18.38.4 GR10)");
            return null;
        }
        // SR27 — "A report group description entry that contains an OCCURS clause with a DEPENDING phrase may be
        // followed within that report group only by report group description entries that are subordinate to it."
        if (depending is not null && subtreeEnd < entries.Length
            && int.TryParse(entries[subtreeEnd].levelNumber().GetText(), out int nextLevel) && nextLevel > 1)
        {
            Edition.Error(DiagnosticCatalog.ReportOccursFormat3Rule, $"{where}: an entry with an OCCURS … DEPENDING clause may be "
                + "followed within that report group only by entries subordinate to it (ISO §13.18.38.3 SR27)");
            return null;
        }
        // SR10 (formats 1 and 3) — an OCCURS may nest only when the DEPENDING phrase is absent.
        if (depending is not null && st.Repetitions.Count > 0)
        {
            Edition.Error(DiagnosticCatalog.ReportOccursFormat3Rule, $"{where}: an OCCURS clause may be subordinate to an entry "
                + "containing another OCCURS clause only if the DEPENDING ON phrase is not specified (ISO "
                + "§13.18.38.3 SR10)");
            return null;
        }

        // ⛔ THE REPETITION AXIS (§13.18.38.4 GR10/GR12). GR10c/GR10d and GR12c/GR12d are the LINE arms and
        // GR10a/GR10b and GR12a/GR12b the COLUMN arms, so the SAME integer-3 is a vertical interval on one
        // entry and a horizontal one on another — which arm applies is fixed HERE, once, by the clause the
        // entry (or its subtree) carries: "If the entry contains a LINE clause, each successive occurrence is
        // positioned integer-3 lines vertically beneath the preceding occurrence" (GR12c) and "If the entry is
        // a group entry having subordinate entries with LINE clauses, report lines in successive occurrences
        // are positioned integer-3 lines vertically beneath the line they occupy in the preceding occurrence"
        // (GR12d). Anything else repeats horizontally.
        var axis = ReportRepetitionAxis.Horizontal;
        for (int k = self; k < subtreeEnd; k++)
            if (entries[k].reportGroupClause().Any(c => c.reportLineClause() is not null))
            {
                axis = ReportRepetitionAxis.Vertical;
                break;
            }
        // ⛔ SR25's FOUR LEGS ARE THE FOUR GR12 LEGS, AND TWO OF THEM ARE ABOUT THE ENTRY ITSELF — NOT ITS
        // SUBTREE. "The STEP phrase shall be specified if the entry: a) contains an absolute LINE clause, or
        // b) has an entry with an absolute LINE clause subordinate to it, or c) contains an absolute COLUMN
        // clause, or d) is subordinate to an entry with a LINE clause and has an entry with an absolute COLUMN
        // clause subordinate to it." a) ∪ b) IS "an absolute LINE clause anywhere in the subtree", so those two
        // collapse; c) and d) do NOT — c) asks about the entry's OWN clause and d) adds the GR12b qualifier
        // "being itself subordinate to an entry with a LINE clause". Reading c/d as one subtree scan REFUSED
        // `03 LINE PLUS 1 OCCURS 3 TIMES.` over a subordinate `05 COLUMN 1` — a vertically repeating entry
        // whose repetitions are spread by the LINE clause and whose column is the same in each, which is
        // exactly GR10c, and conforming source (kb/Work PB565).
        if (step is null)
        {
            if (AbsoluteLineClauseIn(entries, self, subtreeEnd))
            {
                Edition.Error(DiagnosticCatalog.ReportOccursFormat3Rule, $"{where}: the STEP phrase shall be specified when a "
                    + "repeating entry contains, or has subordinate to it, an absolute LINE clause "
                    + "(ISO §13.18.38.3 SR25a/SR25b) — without it every repetition would print on the "
                    + "same line");
                return null;
            }
            if (AbsoluteColumnClauseIn(entries, self, self + 1)
                || (SubordinateToLineClause(entries, self) && AbsoluteColumnClauseIn(entries, self, subtreeEnd)))
            {
                Edition.Error(DiagnosticCatalog.ReportOccursFormat3Rule, $"{where}: the STEP phrase shall be specified when a "
                    + "repeating entry contains an absolute COLUMN clause, or is subordinate to an entry with a "
                    + "LINE clause and has one subordinate to it (ISO §13.18.38.3 SR25c/SR25d) — without it "
                    + "every repetition would print in the same column");
                return null;
            }
        }
        // SR26 — "The value of integer-3 shall be sufficient to prevent the overlapping of any line (in the case
        // of vertical repetition) or column (in the case of horizontal repetition) of any two consecutive
        // repetitions of the associated report item." The rule names BOTH axes, so it is measured on the axis
        // this entry repeats on: horizontally the repeated item's printed width, vertically the number of lines
        // one occurrence occupies.
        if (step is { } sv)
        {
            int span = 0;
            string unit = axis == ReportRepetitionAxis.Vertical ? "lines" : "columns";
            if (axis == ReportRepetitionAxis.Vertical)
                span = ReportEntryLineSpan(entries, self, subtreeEnd);
            else
                for (int k = self; k < subtreeEnd; k++)
                    if (ReportEntryPictureWidth(entries[k], model) is { } w) span += w;
            if (span > 0 && sv < span)
                Edition.Error(DiagnosticCatalog.ReportOccursFormat3Rule, $"{where}: STEP {sv} is not sufficient to prevent the "
                    + $"overlapping of two consecutive repetitions — the repeated report item occupies {span} "
                    + unit + " (ISO §13.18.38.3 SR26)");
        }

        // ⛔ data-name-1 IS A QUALIFIED-DATA-NAME, SCREENED (kb/Work PB885). ISO §13.18.38.3 SR2 (all formats):
        // "Data-name-1 and data-name-2 shall not be subscripted." The grammar's shared dataReference admits a
        // subscript, and the bare KeyReference capture used here DROPPED it in silence — `DEPENDING ON WS-TE (2)`
        // bound to the unsubscripted name and printed the wrong number of repetitions. ClauseDataName is the ONE
        // data-name-n capture the data-division OCCURS clause, the FD clauses and the file-control keys share.
        string? dn = null;
        IReadOnlyList<string> dq = [];
        Editions.DiagnosticCursor dAt = default;
        if (depending is not null)
        {
            (dn, dq) = ClauseDataName(depending, $"{where}: OCCURS … DEPENDING ON");
            using var _d = Edition.At(depending);
            dAt = Edition.Cursor;
        }
        return new ReportOccursSpec(min, max, dn, dq, step) { Axis = axis, DependingAt = dAt };
    }

    /// <summary>Resolve one repeating entry's <c>DEPENDING ON data-name-1</c> (ISO §13.18.38 Format 3) and apply
    /// §13.18.38.3 SR17 — "Data-name-1 shall describe an integer". Reported once per repeating entry.</summary>
    private void ResolveReportOccursDepending(ReportOccursSpec spec, ReportModel model, HashSet<ReportOccursSpec> seen)
    {
        if (spec.DependingName is null || !seen.Add(spec)) return;
        // The ONE clause-operand resolver (§8.4.2.2 uniqueness; silent for an operand the capture refused).
        spec.DependingItem = ResolveClauseOperand(spec.DependingName, spec.DependingQualifiers,
            $"RD '{model.Name}': OCCURS … DEPENDING ON", spec.DependingAt);
        if (spec.DependingItem is null) return;
        // SR17 read exactly as the data-division OCCURS reads it (an index item is NOT an integer data item).
        if (spec.DependingItem.Pic is not { IsIntegerDescription: true })
            Edition.Error(DiagnosticCatalog.ReportOccursFormat3Rule, $"RD '{model.Name}': OCCURS … DEPENDING ON "
                + $"'{spec.DependingName}' — data-name-1 shall describe an integer (ISO §13.18.38.3 SR17)");
        // SR18 is a FORMATS 2 AND 3 rule (kb/Work PB1261): a report group entry is subordinate to its report
        // description entry, which §13.18.27.3 SR1 e) lets carry the GLOBAL clause — the data-division twin is
        // DataBinder.Odo.cs#DependingAttributeFault, over the same global-name answer (IsGlobalRecord).
        if (model.IsGlobal && !IsGlobalRecord(spec.DependingItem.Root))
        {
            using var _ = Editions.DiagnosticCursorExtensions.At(Edition, spec.DependingAt);
            Edition.Error(DiagnosticCatalog.OccursDependingAttributeMismatch, $"RD '{model.Name}': OCCURS … DEPENDING ON "
                + $"'{spec.DependingName}' — the report is global and data-name-1 is not a global name: \"If the OCCURS "
                + "clause is specified in an entry subordinate to one containing the GLOBAL clause, data-name-1, if "
                + "specified, shall be a global name\" (ISO §13.18.38.3 SR18)");
        }
    }

    /// <summary>The display width of one report group entry's PICTURE, or null when it has none (a group entry).
    /// The §13.18.38.3 SR26 overlap test measures the repeated item against integer-3.</summary>
    private int? ReportEntryPictureWidth(Core.ReportGroupEntryContext ge, ReportModel model)
    {
        foreach (var clause in ge.reportGroupClause())
            if (clause.pictureClause()?.PIC_STRING() is { } pic)
                // The SAME character-position count the print columns use (§13.18.14.4 GR9), through the ONE rule.
                return PictureAnalyzer.Analyze(
                    UnitHasConstants ? ExpandPicConstants(pic.GetText(), "", diagnose: false) : pic.GetText(),
                    Usage.Display, Edition,
                    $"RD '{model.Name}' repeating entry", null, currencies: CurrencySigns,
                    decimalPointIsComma: DecimalPointIsComma) is { } p ? DataItem.DisplayTextWidthOf(p) : null;
        return null;
    }

    /// <summary>Bind ONE report group description entry into the build state (see <see cref="BindReportEntries"/>
    /// for the walk and <see cref="BindReportGroups"/> for the line-building rule).</summary>
    /// <param name="ge">The entry.</param>
    /// <param name="anchorKey">The entry whose identity keys this placement's step anchors — the same context,
    /// except that a repeating entry's own replayed copies must share one anchor per placement.</param>
    /// <param name="ownOccurs">This entry's OWN §13.15.4 GR3 repetition vehicle when it is a live repeating
    /// entry — the report-writer OCCURS clause (§13.18.38 Format 3) or the multiple LINE clause's §13.18.35.4
    /// GR9 equivalent — carrying its repetition count and the evidence that the vehicle was not refused, which
    /// is what the §13.18.63.3 SR35 / §13.18.53.3 SR6 operand-count screen needs.</param>
    /// <param name="ordinal">This repetition's zero-based ordinal within <paramref name="ownOccurs"/> — what
    /// selects the LINE operand of a multiple LINE clause (§13.18.35.4 GR9).</param>
    private void BindReportEntry(
        ReportEntryLocation at, Core.ReportGroupEntryContext anchorKey, ReportModel model,
        ReportGroupBuild st, ReportOccursSpec? ownOccurs = null, int ordinal = 0)
    {
        {
            var ge = at.Entry;
            using var _ = Edition.At(ge);
            int.TryParse(ge.levelNumber().GetText(), out int level);
            string? entryName = ge.dataName().NameOrNull();
            if (level == 1)
            {
                st.Group = new ReportGroupModel { Name = entryName, Entry = ge };
                model.Groups.Add(st.Group);
                st.Line = null;
                st.VerticalCursor = 0;   // the §13.18.35.4 GR4c trial sum is measured per report group
            }
            var group = st.Group;
            if (group is null)
            {
                Edition.Error(DiagnosticCatalog.ReportGroupBefore01, $"RD '{model.Name}': report group entry before any 01-level entry");
                return;
            }
            var chain = st.Chain;
            while (chain.Count > 0 && chain[^1].Level >= level) chain.RemoveAt(chain.Count - 1);

            // The clauses that DESCRIBE the entry's item — PICTURE, USAGE (its own, and the one §13.18.60.4 GR1 hands
            // down from an enclosing entry), SIGN, JUSTIFIED, BLANK WHEN ZERO and the VALUE literals — decoded once per
            // entry by the reader a constant's length phrase also asks (DataBinder.ReportItemDescription.cs, kb/Work
            // PB1941). The rest of the entry's clauses are the walk's, captured below (clauses may appear in any order
            // within the entry — RW104A).
            var described = DescribeReportItem(at);
            string? picText = described.PicText, usageText = described.UsageText;
            string? inheritedUsage = described.InheritedUsage;
            // The VALUE clause's operand list (§13.18.63.2 format 4) and the count WRITTEN — the SR35 check
            // reads the written count so a rejected operand cannot make an illegal clause look legal.
            var valueRaws = new List<string>(described.ValueRaws);
            int valueOpsWritten = 0;
            List<EditingPhraseSpec>? reportEditing = described.Editing;   // PICTURE EDITING phrases (§13.18.40.2)
            LocaleEditSpec? reportLocale = described.Locale;   // PICTURE format 2 — the LOCALE phrase (PB113 / PB64 T6)
            SignSpec? ownSign = described.OwnSign;
            bool justified = described.Justified, blankWhenZero = described.BlankWhenZero, groupIndicate = false;
            // A repetition VEHICLE that was REFUSED (an OCCURS clause a §13.18.38.3 syntax rule rejected):
            // the entry's §13.15.4 GR3 repetition count is then not knowable, so the operand-count screen
            // below stands down rather than emitting a second diagnostic about it (kb/Work PB506).
            bool staysLoud = false;
            var columns = new List<ReportColumnSpec>();
            // The SOURCE clause's operand list (§13.18.53.2 — one entry per written identifier-1); empty when
            // the entry carries no SOURCE clause.
            var sourceOps = new List<ReportFieldSource>();
            int sourceOpsWritten = 0;   // operands WRITTEN (a staged/unresolvable one adds none to sourceOps)
            // §13.18.53.3 SR3 — set when the clause writes an arithmetic-expression operand or a ROUNDED phrase.
            bool sourceNeedsNumericEntry = false;
            ReportLineModel? opened = null;
            // The LINE clause operand this repetition places by, and its index within the clause (§13.18.35.3
            // SR10 / §13.18.35.4 GR9) — resolved after the clause loop, where the enclosing repetitions' STEP
            // displacement is known.
            Core.ReportLineOperandContext? lineOperand = null;
            int lineOperandIndex = 0;
            // ⛔ ONE CLAUSE, MANY GROUPS (kb/Work PB482, PB1295): ISO §13.18.54.3 SR1 — "The whole clause is referred to as
            // a SUM clause even though the SUM keyword may appear more than once", and §13.18.54.4 GR1 gives the
            // ENTRY one counter. The grammar's reportSumClause holds every `SUM … [UPON …]` group of the clause
            // (reportSumGroup) and its one RESET and one rounded-phrase, so each group becomes a term of the counter;
            // keeping a single group silently DISCARDED the rest: `SUM WS-A UPON DET SUM WS-B UPON DET2` totalled
            // WS-B alone.
            Core.ReportSumClauseContext? sumClause = null;
            Core.ConditionContext? ownCond = null;

            foreach (var clause in ge.reportGroupClause())
            {
                if (clause.reportTypeClause()?.reportGroupType() is { } t)
                {
                    // §13.15.3 SR5 — the TYPE clause belongs to the level 1 entry alone (ScreenReportEntryClausePresence
                    // refuses it anywhere else), so a stray one never retypes the group it is written inside.
                    if (level == 1) BindGroupType(t, group, model);
                }
                else if (clause.reportLineClause() is { } lc)
                {
                    // The multiple LINE clause (§13.18.35.3 SR10) is a §13.15.4 GR3 repetition VEHICLE, read by
                    // ReportRepetitionOf before this entry is bound: §13.18.35.4 GR9 makes it "functionally
                    // equivalent to a LINE clause with a single operand, together with a simple OCCURS clause
                    // whose integer is equal to the number of operands", so this replay takes the operand its
                    // own ORDINAL names and the entry opens one report line per repetition. The modulo is the
                    // SR10d recovery path only (an entry carrying BOTH vehicles is diagnosed, not guessed at).
                    var ops = lc.reportLineOperand();
                    lineOperand = ops.Length > 1 ? ops[ordinal % ops.Length] : ops[0];
                    lineOperandIndex = ops.Length > 1 ? ordinal % ops.Length : 0;
                }
                else if (clause.reportNextGroupClause() is { } ngc)
                {
                    // Captured on the level 1 entry's group; its syntax rules (and §13.15.3 SR6 for any other
                    // level) are screened once the group is complete — BindNextGroupClauses (kb/Work PB957).
                    if (level == 1) group.NextGroupClause = ngc;
                }
                else if (clause.reportColumnClause() is { } cc)
                {
                    // §13.18.14.3 SR9 — the alignment word (LEFT assumed when none is written) and its absolute-only
                    // rule: "If LEFT, CENTER, or RIGHT is specified, all the operands shall be absolute." The word
                    // belongs to the clause, so every operand of a multiple COLUMN clause carries it.
                    var written = cc.reportColumnAlignment();
                    var alignment = written?.CENTER() is not null ? ReportColumnAlignment.Center
                        : written?.RIGHT() is not null ? ReportColumnAlignment.Right : ReportColumnAlignment.Left;
                    if (written is not null && cc.reportColumnOperand().Any(o => o.reportRelativeSign() is not null))
                        Edition.Error(DiagnosticCatalog.ReportColumnAlignmentNotAbsolute, $"RD '{model.Name}' entry "
                            + $"'{entryName ?? "FILLER"}': the COLUMN clause writes "
                            + $"{written.GetText().ToUpperInvariant()} and a relative (PLUS) operand; "
                            + "if LEFT, CENTER, or RIGHT is specified, all the operands shall be absolute (ISO §13.18.14.3 SR9)");
                    foreach (var op in cc.reportColumnOperand())
                    {
                        bool relative = op.reportRelativeSign() is not null;
                        int column = IntegerOperandValue(op.integerOperand(), $"RD '{model.Name}' entry '{entryName ?? "FILLER"}'")
                            ?? RecoveredIntegerOperand;
                        // §13.18.14.3 SR6 — "Neither integer-1 nor integer-2 shall exceed the page width": the
                        // absolute column and the relative offset are both written integers, so the rule is
                        // decidable here against ReportModel.PageWidth (the PAGE clause's integer-2, else GR5's 999).
                        if (column > model.PageWidth)
                            Edition.Error(DiagnosticCatalog.ReportColumnBeyondPageWidth, $"RD '{model.Name}' entry "
                                + $"'{entryName ?? "FILLER"}': the COLUMN clause's {(relative ? "relative offset" : "column")} "
                                + $"{column} exceeds the page width {model.PageWidth}"
                                + (model.PageWidthWritten ? "" : " (assumed 999, §13.18.39.4 GR5)")
                                + " (ISO §13.18.14.3 SR6)");
                        columns.Add(new ReportColumnSpec(relative, column, alignment));
                    }
                }
                else if (clause.reportSourceClause() is { } sc)
                {
                    // Every written operand of the clause (§13.18.53.2's ellipsis) — the SR6 count below reads
                    // the WRITTEN count, so a staged operand (a subscripted reference, another report's
                    // counter) cannot make an illegal clause look legal.
                    var sops = sc.reportValueOperand();
                    sourceOpsWritten += sops.Length;
                    ScreenSourceOperandParens(sops, $"RD '{model.Name}' entry '{entryName ?? "FILLER"}'");   // SR7
                    // §13.18.53.3 SR3 — an arithmetic-expression operand, or the ROUNDED phrase, requires the
                    // ENTRY to define a numeric or numeric-edited item. The entry's PICTURE is analyzed below
                    // (it needs the COLUMN block's usage/editing context), so the obligation is recorded here
                    // and discharged there, where `pic` exists.
                    if (sc.roundedPhrase() is not null || sops.Any(o => IdentifierOperandOf(o) is null))
                        sourceNeedsNumericEntry = true;
                    foreach (var op in sops) sourceOps.Add(BindSourceOperand(op, sc.roundedPhrase()));
                }
                else if (clause.reportSumClause() is { } sm)
                    sumClause ??= sm;   // a second one is refused once per written entry (ScreenReportEntryClausePresence)
                else if (clause.reportGroupIndicateClause() is not null)
                    groupIndicate = true;
                else if (clause.reportPresentWhenClause() is { } pw)
                {
                    ownCond = pw.condition();
                    // A FUNCTION inside condition-1 would need the UDF activation-hoist protocol, which is a
                    // statement-context mechanism — staged loud, never a silent mis-hoist.
                    if (HasToken(ownCond, CobolLexer.FUNCTION))
                    {
                        Edition.Error(DiagnosticCatalog.ReportConditionFunction, $"RD '{model.Name}': a FUNCTION reference inside a "
                            + "PRESENT WHEN condition (ISO §13.18.41) is not yet implemented");
                        ownCond = null;
                    }
                }
                else if (clause.reportVaryingClause() is not null)
                {
                    // The counters are made by the walk (VaryingCountersOf), once per entry per enclosing occurrence,
                    // and reach this entry's printable item through ReportGroupBuild.Varying (§13.18.64.3 SR2).
                }
                else if (clause.pictureClause() is not null || clause.usageClause() is not null
                         || clause.signClause() is not null || clause.justifiedClause() is not null
                         || clause.blankWhenZeroClause() is not null)
                {
                    // The item's description — decoded once per entry by DescribeReportItem, above.
                }
                else if (clause.occursClause() is not null)
                {
                    // The repeating entry itself (ISO §13.18.38 Format 3) is read by ReportOccursOf BEFORE this
                    // entry is bound — it drives the REPLAY, so there is nothing to capture per repetition.
                    // A clause ReportOccursOf REFUSED (a syntax rule) leaves ownOccurs null and the entry's
                    // repetition count unknowable.
                    if (ownOccurs is null) staysLoud = true;
                }
                else if (clause.valueClause() is { } value)
                {
                    // Format 4 (report-section), ISO §13.18.63.2 — `{literal-1}…`: its literals are the description's
                    // (DescribeReportItem); the connective and the WRITTEN count are the walk's.
                    CheckValueConnective(value, pairedConnective: true,
                        $"RD '{model.Name}' entry '{entryName ?? "FILLER"}'");
                    valueOpsWritten += value.valueItem().FirstOrDefault()?.valueClauseOperand().Length ?? 0;
                }
            }

            // GROUP INDICATE shall not share an entry with PRESENT WHEN (ISO §13.15.3 SR17 — GROUP INDICATE IS
            // a fixed-condition PRESENT WHEN, §13.18.28.4 GR1).
            if (groupIndicate && ownCond is not null)
                Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}' entry '{entryName ?? "FILLER"}': the GROUP "
                    + "INDICATE clause shall not be specified in an entry in which the PRESENT WHEN clause is "
                    + "specified (ISO §13.15.3 SR17)");

            // ⛔ THE MULTI-OPERAND REPETITION RULE, ONE READER FOR BOTH CLAUSES (kb/Work PB506). ISO
            // §13.18.63.3 SR35 (VALUE) and §13.18.53.3 SR6 (SOURCE) are the SAME rule written twice, and their
            // general rules (§13.18.63.4 GR23 / §13.18.53.4 GR4) are likewise twins — so the screen is written
            // once and each clause supplies its own operand count, diagnostic and citation. Skipped when a
            // repetition vehicle was REFUSED (staysLoud): the entry's §13.15.4 GR3 count is then not knowable
            // here and a second diagnostic would be noise.
            if (!staysLoud)
            {
                var repChain = RepetitionChain(columns, ownOccurs, chain);
                ScreenRepeatingOperandCount(valueOpsWritten, repChain, $"RD '{model.Name}' entry '{entryName ?? "FILLER"}'",
                    "VALUE", DiagnosticCatalog.ReportValueOperandCount, "ISO §13.18.63.3 SR35");
                ScreenRepeatingOperandCount(sourceOpsWritten, repChain, $"RD '{model.Name}' entry '{entryName ?? "FILLER"}'",
                    "SOURCE", DiagnosticCatalog.ReportSourceOperandCount, "ISO §13.18.53.3 SR6");
            }

            // ISO §13.18.60.3 SR2 — "If the USAGE clause is written in the data description entry for a group
            // item, it may also be written in the data description entry for any subordinate elementary item or
            // group item, but the same usage shall be specified in both entries." §13.15.4 GR2 imports the data
            // description entry's clause rules into the report group description entry, so the pair is a rule
            // here too, and it is the only thing that makes GR1's inheritance unambiguous (kb/Work PB541).
            if (usageText is not null && inheritedUsage is not null
                && !CobolNames.Same(usageText, inheritedUsage))
                Edition.Error(DiagnosticCatalog.ReportUsageNotDisplayOrNational, $"RD '{model.Name}' entry "
                    + $"'{entryName ?? "FILLER"}': USAGE {usageText} contradicts the USAGE {inheritedUsage} "
                    + "written on the group entry above it — when a USAGE clause is written on a group item and "
                    + "on an entry subordinate to it, the same usage shall be specified in both (ISO "
                    + "§13.18.60.3 SR2, imported into the report group description entry by §13.15.4 GR2)");

            if (lineOperand is { } lop)
                opened = RepeatedLine(lop, lineOperandIndex, group.Lines.Count == 0, anchorKey, st);
            if (opened is not null)
            {
                opened.Entry = ge;
                st.Line = opened;
                group.Lines.Add(opened);
                // The line's PRESENT WHEN chain: every ancestor condition + this entry's own (§13.18.41.4 GR2b).
                foreach (var (_, c, _, _) in chain) if (c is not null) opened.PresentWhenCtxs.Add(c);
                if (ownCond is not null) opened.PresentWhenCtxs.Add(ownCond);
                // §13.18.38.4 GR13 on the VERTICAL axis: a repetition the DEPENDING count excludes has no line.
                opened.RepetitionGuards.AddRange(st.GuardsHere());
                st.LineChainDepth = chain.Count + 1;   // this entry's frame is pushed below
            }
            var line = st.Line;

            // A SUM entry establishes a counter whether or not it is printable (§13.18.54.4 GR1/GR3) — ONE PER
            // OCCURRENCE of a repeating entry (kb/Work PB1271): this replay's counters are the occurrences its
            // ordinals select, one per COLUMN operand of a multiple COLUMN clause (§13.18.14.4 GR12 makes those
            // operands a simple OCCURS level, so its printable items are occurrences too). Each counter's FULL
            // chain and this replay's OCCURS … DEPENDING tests govern the GR10 print/reset suppression
            // (§13.18.41.4 GR3g).
            var sums = new List<ReportSumModel>();
            if (sumClause is not null)
            {
                var family = SumFamilyOf(ge, entryName, picText, columns.Count, chain, model, st);
                int perReplay = columns.Count > 1 ? columns.Count : 1;
                var coordinates = new List<int>(st.Repetitions.Count + 1);
                foreach (var frame in st.Repetitions) coordinates.Add(frame.Ordinal);
                if (perReplay > 1) coordinates.Add(0);
                for (int c = 0; c < perReplay; c++)
                {
                    if (perReplay > 1) coordinates[^1] = c;
                    var sum = BindSumClause(sumClause, family, family.IdAt(coordinates), entryName, group, model,
                        columns.Count > 0);
                    foreach (var (_, cond, _, _) in chain) if (cond is not null) sum.PresentWhenCtxs.Add(cond);
                    if (ownCond is not null) sum.PresentWhenCtxs.Add(ownCond);
                    sum.RepetitionGuards.AddRange(st.GuardsHere());
                    sums.Add(sum);
                    // §13.18.54.4 GR6 — the counter occurrence is what a SUM clause's data-name-1 adds when it names
                    // this entry; its presence chain IS the counter's (kb/Work PB1294).
                    var counterOccurrence = new ReportItemOccurrence
                    {
                        Family = family, Coordinates = family.CoordinatesOf(sum.Id - family.BaseId), Sum = sum,
                    };
                    family.Occurrences.Add(counterOccurrence);
                    model.ItemOccurrences.Add(counterOccurrence);
                }
            }

            // ⛔ AN UNPRINTABLE SOURCE ENTRY IS STILL AN ENTRY (kb/Work PB1294). §13.18.53.4 GR3: "If the entry
            // containing the SOURCE clause contains no COLUMN clause and therefore defines an unprintable item, the
            // SOURCE clause causes no action, except where the entry is referred to by means of a SUM clause" — so
            // it prints nothing and has no printable field, but §13.18.54.4 GR6 gives a SUM clause's data-name-1 "the
            // operand of the SOURCE … clause" of it to add. (A printable entry's occurrences are recorded where its
            // field is made, below, beside the operands that field cycles through.)
            if (columns.Count == 0 && sums.Count == 0 && sourceOps.Count > 0)
            {
                var sourceFamily = SourceFamilyOf(ge, entryName, 0, chain, model, st);
                sourceFamily.Category ??= UnprintablePictureCategory(ge, picText, usageText ?? inheritedUsage, ownSign,
                    reportEditing, reportLocale, $"RD '{model.Name}' entry '{entryName ?? "FILLER"}'");
                RecordSourceOccurrences(sourceFamily, sourceOps, 1, ownCond, st, VaryingUsesOf(ge, 0, st), model);
            }

            if (columns.Count > 0)
            {
                int col = columns[0].Value;
                if (line is null)
                {
                    // No report line has been opened in this group, so no entry above this one has a LINE clause
                    // and ScreenReportEntryClausePresence has refused the COLUMN entry (§13.15.3 SR9) — said once,
                    // at the written entry, never here per replay. (The converse is not what this test means: a
                    // line opened by an EARLIER entry says nothing about this entry's ancestry, kb/Work PB1224.)
                    chain.Add((level, ownCond, EntryRepetitions(columns, ownOccurs), entryName));
                    return;
                }
                // ⛔ NO OPERAND, NO PRINTABLE ITEM (kb/Work PB853). An entry with a COLUMN clause and no SOURCE,
                // VALUE or SUM operand is either an ISO §13.15.3 SR10 violation ScreenReportEntryClausePresence
                // already refused, or one whose written operands were each refused at their own clause. A
                // figurative SPACE sender used to be FABRICATED here in place of the missing operand — the
                // compiler inventing source the programmer did not write — which printed `000` under PIC 999.
                if (sumClause is null && sourceOps.Count == 0 && valueRaws.Count == 0)
                {
                    chain.Add((level, ownCond, EntryRepetitions(columns, ownOccurs), entryName));
                    return;
                }
                // The printable item (§13.18.14): a SYNTHETIC DataItem carrying the PICTURE so the emitter's ONE
                // MOVE conversion path renders the §13.18.53.4 GR1 implicit MOVE. A printable item is a
                // USAGE-DISPLAY elementary item; its numeric face stores its character IMAGE (StoreAsImage).
                string itemWhere = $"RD '{model.Name}' printable item '{entryName ?? "FILLER"}'";
                // The one analysis a constant's length phrase measures the item by, too (kb/Work PB1941).
                var pic = ReportPrintablePicture(described, model.Name, entryName);
                ScreenReportEntryPicture(ge, pic, picText, itemWhere, summed: sumClause is not null);   // once per written entry
                if (pic is null)
                {
                    // A SOURCE or SUM entry with no PICTURE is SR12's, refused once per written entry by
                    // ScreenReportEntryClausePresence; what is left here is the VALUE-only entry, whose PICTURE SR14
                    // implies only from an alphanumeric, boolean or national literal that is not zero-length.
                    if (sourceOpsWritten == 0 && sumClause is null)
                        Edition.Error(DiagnosticCatalog.ReportEntryClausePresence, $"RD '{model.Name}': printable item at COLUMN {col} "
                            + "has a VALUE clause but no PICTURE clause, and none is implied: the PICTURE clause may be "
                            + "omitted only when an alphanumeric, boolean or national literal that is not a zero-length "
                            + "literal is specified in the VALUE clause (ISO §13.15.3 SR14)");
                    chain.Add((level, ownCond, EntryRepetitions(columns, ownOccurs), entryName));
                    return;
                }
                // §13.18.60.3 SR7 over a usage NO clause ever stated — one IMPLIED by the picture
                // character-string (§13.18.60.4 GR7/GR8: "The implicit or explicit USAGE DISPLAY clause…", "The
                // implicit or explicit USAGE NATIONAL clause…"). A usage this entry or an enclosing group entry
                // WROTE was already screened at its clause, which is the rule's own subject, so re-asking here
                // would report one violation twice. Same rule, same message, one text: ScreenReportUsage. The
                // gate this replaced admitted DISPLAY alone and refused the NATIONAL half of the rule under a
                // §13.15 citation that does not say it (kb/Work PB541).
                if (usageText is null && inheritedUsage is null)
                    ScreenReportUsage(pic.Usage, model.Name, entryName, picText is null ? null : $"PICTURE {picText}");
                // §13.18.52.3 SR1's report bullet — "a numeric report group description entry whose picture
                // character-string contains the symbol 'S'" — and SR2, through the ONE elementary-subject test the
                // data description entry reads (kb/Work PB537: this arm had no screen at all).
                if (ownSign is not null
                    && SignClauseElementaryDefect(pic, picText, "report group description entry") is { } signDefect)
                {
                    Edition.Error(DiagnosticCatalog.SignClauseSubject,
                        $"RD '{model.Name}' entry '{entryName ?? "FILLER"}': {signDefect}");
                    ownSign = null;
                }
                // ⛔ §13.15.4 GR2 — "the USAGE, PICTURE, BLANK WHEN ZERO and JUSTIFIED clauses are the same clauses as
                // those that are described under the general format for a data description entry and shall obey the
                // syntax rules and general rules defined for each clause." The SUBJECT rules of the last two (what the
                // elementary item they are written on may BE: §13.18.8.3 SR1/SR2, §13.18.32.3 SR3) are the data
                // division's CheckClauseSubjects, and these are its predicates, not a second copy of them (kb/Work
                // PB507, PB1288). The clause is cleared on a violation, as the data division clears it, so nothing
                // downstream applies it to an item it may not describe.
                if (blankWhenZero && BlankWhenZeroViolation(pic) is { } bwzFault)
                {
                    Edition.Error(DiagnosticCatalog.ClauseSubjectCategory,
                        $"RD '{model.Name}' entry '{entryName ?? "FILLER"}': {bwzFault}");
                    blankWhenZero = false;
                }
                if (justified && JustifiedViolation(pic) is { } justifiedFault)
                {
                    Edition.Error(DiagnosticCatalog.ClauseSubjectCategory,
                        $"RD '{model.Name}' entry '{entryName ?? "FILLER"}': {justifiedFault}");
                    justified = false;
                }
                // ⛔ §13.18.63.3 SR6 NAMES FORMAT 4 — "literals in formats 1, 2, and 4 of the VALUE clause may be
                // numeric" — so a report-section printable item's numeric literal rides the SAME COBOL-2023
                // introduction (Annex E.3.3 item 43) as its format-1 and format-2 siblings. It did not: the
                // report entry's VALUE operands never pass through the data-division literal funnel (they are
                // collected by ExtractValueOperandList and stored as FieldValueSource), so
                // `10 COLUMN 1 PIC ZZ9.99 VALUE 10.` compiled clean at --std 85 and PRINTED the 2023 edited image
                // ` 10.00`, while `01 X PIC ZZ9.99 VALUE 10.` was refused there (kb/Work PB921, the third arm).
                // ⛔ AND IT IS NOT THE ONLY ALL-FORMATS RULE THE REPORT ARM SKIPPED (kb/Work PB586's sibling sweep):
                // §13.18.63.3 SRs 1-9 open "ALL FORMATS", so SR2's range ("all literals in the VALUE clause shall
                // be numeric and shall be permissible values within the range indicated by the PICTURE clause"),
                // SR3's sign and SR4/SR5's class bind a format-4 literal exactly as a format-1 one. MEASURED
                // before: `03 COLUMN 1 PIC 9(2) VALUE 12345.` printed `45` and `PIC 9(2) VALUE -3` printed `03` —
                // silent truncation at every edition — and `PIC 9(2) VALUE "AB"` leaked Roslyn CS0103 against the
                // generated C#. So each operand now takes THE funnel every other format's literal takes (SR6's
                // edition gate included, which is why the narrower call that stood here is gone), and the text it
                // returns — the --permissive rewrite, or a fixed-point subject's expansion of a floating-point
                // literal — is what the printable item stores. The subject is an ELEMENTARY item: a printable item
                // is one (§13.18.14), and its size is its PICTURE's, written or §13.15.3 SR14-implied.
                // Per operand, once the picture is settled — a multi-operand format-4 clause (§13.18.63.3 SR35)
                // screens each of its literals.
                for (int vr = 0; vr < valueRaws.Count; vr++)
                    valueRaws[vr] = ScreenValueLiteral(pic, valueRaws[vr],
                        $"RD '{model.Name}' entry '{entryName ?? "FILLER"}'",
                        ValueSubject.ForElementary(pic, isDynamicLength: false, isAnyLength: false,
                            implicitPicture: picText is null));
                // §13.18.53.3 SR3 — "If arithmetic-expression-1 or the ROUNDED phrase is specified, the entry
                // shall define either a numeric data item or a numeric-edited data item." The receiving operand
                // of GR2's implicit COMPUTE is this printable item (kb/Work PB852).
                if (sourceNeedsNumericEntry && pic.Category is not (PicCategory.Numeric or PicCategory.NumericEdited))
                    Edition.Error(DiagnosticCatalog.ReportSourceExpressionNotNumeric, $"RD '{model.Name}' entry "
                        + $"'{entryName ?? "FILLER"}': the SOURCE clause specifies an arithmetic-expression or the "
                        + $"ROUNDED phrase, so the entry shall define a numeric or numeric-edited data item (ISO "
                        + $"§13.18.53.3 SR3); its PICTURE '{picText}' describes a {pic.Category} item.");
                var item = new DataItem
                {
                    Level = level,
                    DeclaredAt = Edition.Cursor,
                    CobolName = entryName,
                    CsName = "_rptItem" + _uidCounter,
                    Pic = pic,
                    OwnSign = ownSign,
                    Justified = justified,
                    BlankWhenZero = blankWhenZero,
                };
                item.Uid = _uidCounter++;
                if (pic is { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display })
                    MarkImageForced(item);      // the collected image fact — compose wants the printable CHARACTER image
                // THE OPERAND LIST (§13.18.63.2 format 4 / §13.18.53.2 — both clauses write `{ operand } …`).
                // SUM wins the entry (§13.18.54.4 GR4 — the sum counter acts as the source item); then the
                // SOURCE operands; then the VALUE operands. There is no fourth arm: §13.15.3 SR10 forbids the
                // operand-less entry, and the guard above returns before one reaches here (kb/Work PB853).
                // A multiple COLUMN SUM entry supplies one counter PER printable item (kb/Work PB1271), paired with
                // its repetition by the same SourceAt reader a multi-operand SOURCE uses.
                List<ReportFieldSource> srcs =
                    sums.Count > 0 ? [.. sums.Select(s => (ReportFieldSource)new FieldSumSource(s.Id))]
                    : sourceOps.Count > 0 ? sourceOps
                    : [.. valueRaws.Select(r => (ReportFieldSource)new FieldValueSource(r))];
                var field = new ReportFieldModel
                {
                    Columns = RepeatedPlacements(columns, anchorKey, st),
                    PrintItem = item,
                    Entry = ge,
                    Sources = srcs,
                    GroupIndicate = groupIndicate,
                    RepetitionOrdinal = st.Placements.GetValueOrDefault(anchorKey),
                };
                st.Placements[anchorKey] = field.RepetitionOrdinal + field.Columns.Count;
                // The field-local chain: conditions strictly BELOW the line entry (its own chain gates the line).
                for (int ci = Math.Min(st.LineChainDepth, chain.Count); ci < chain.Count; ci++)
                    if (chain[ci].Cond is { } c) field.PresentWhenCtxs.Add(c);
                if (ownCond is not null && opened is null) field.PresentWhenCtxs.Add(ownCond);
                // The VARYING counters in scope (§13.18.64.3 SR2): this entry's own and every enclosing entry's, each with the
                // occurrence of its declaring entry this placement lies in (GR3). An enclosing entry's occurrence is its
                // replay's ordinal; this entry's own, when it repeats through a multiple COLUMN clause as well, is
                // counted per placement (the emitter adds the placement index), after the replay ordinal's columns.
                field.Varyings.AddRange(VaryingUsesOf(ge, columns.Count, st));
                field.RepetitionGuards.AddRange(st.GuardsHere());
                line.Fields.Add(field);
                // The entry's occurrences as a rolled total's addends (§13.18.54.4 GR6 — the operand of its SOURCE or
                // VALUE clause), one per placement, each taking the operand the field's own cycling gives it. A SUM
                // entry's occurrences were recorded with its counters above.
                if (sums.Count == 0)
                {
                    var sourceFamily = SourceFamilyOf(ge, entryName, columns.Count, chain, model, st);
                    sourceFamily.Category ??= pic.Category;
                    RecordSourceOccurrences(sourceFamily, srcs, columns.Count, ownCond, st, field.Varyings, model,
                        field.RepetitionOrdinal);
                }
            }

            chain.Add((level, ownCond, EntryRepetitions(columns, ownOccurs), entryName));
        }
    }

    /// <summary>
    /// This repetition's placements for one entry's COLUMN operands (ISO §13.18.38.4 GR12 — "The STEP phrase, if
    /// specified, defines the vertical or horizontal interval between successive occurrences of the associated
    /// report item after the first occurrence … each successive occurrence is printed at a horizontal distance
    /// integer-3 columns to the right of the preceding occurrence").
    /// <para>The displacement Σ ordinal × integer-3 is ADDITIVE over the enclosing repeating entries, so an
    /// ABSOLUTE operand simply moves by it. A RELATIVE (PLUS) operand has no compile-time column at all, so its
    /// FIRST repetition seeds a compose-local anchor with the column it lands on and every later repetition
    /// places at anchor + displacement — the anchor is the item's base column, never mutated.</para>
    /// <para>With NO step phrase there is nothing to displace: GR12's closing sentence — "If no STEP phrase is
    /// specified, the vertical or horizontal interval between successive occurrences is defined by the relative
    /// LINE or COLUMN numbers, respectively, specified in the corresponding report section entries" — and the
    /// replayed relative operand reproduces exactly that against the horizontal counter.</para>
    /// </summary>
    private static IReadOnlyList<ReportColumnSpec> RepeatedPlacements(
        List<ReportColumnSpec> columns, Core.ReportGroupEntryContext anchorKey, ReportGroupBuild st)
    {
        int shift = st.Shift(ReportRepetitionAxis.Horizontal);
        // Nothing displaces horizontally — neither a STEP-less repetition (GR12's closing sentence) nor a
        // repetition whose integer-3 is a VERTICAL interval (GR12c/GR12d).
        if (shift == 0 && st.Repetitions.TrueForAll(
                r => r.Spec.Axis != ReportRepetitionAxis.Horizontal || r.Spec.Step is null))
            return columns;
        // A step anchor identifies ONE printable placement across the repetitions that displace it, so its key
        // holds the operand AND the ordinals of the enclosing repeating entries that do NOT displace (those
        // produce genuinely distinct items whose base columns the horizontal counter fixes independently).
        string undisplaced = st.Undisplaced(ReportRepetitionAxis.Horizontal);
        var placed = new List<ReportColumnSpec>(columns.Count);
        for (int op = 0; op < columns.Count; op++)
        {
            var spec = columns[op];
            if (spec.Kind == ReportColumnKindModel.Absolute) { placed.Add(spec with { Value = spec.Value + shift }); continue; }
            var key = (anchorKey, op, undisplaced);
            if (!st.Anchors.TryGetValue(key, out int anchor)) st.Anchors[key] = anchor = st.Anchors.Count + 1;
            placed.Add(shift == 0
                ? new ReportColumnSpec(ReportColumnKindModel.AnchorSeed, spec.Value, anchor)
                : new ReportColumnSpec(ReportColumnKindModel.AnchorStep, shift, anchor));
        }
        return placed;
    }

    /// <summary>
    /// ⛔ THE VERTICAL TWIN OF <see cref="RepeatedPlacements"/> — this repetition's placement for one LINE
    /// clause operand (ISO §13.18.38.4 GR12c/GR12d: "If the entry contains a LINE clause, each successive
    /// occurrence is positioned integer-3 lines vertically beneath the preceding occurrence" / "If the entry is
    /// a group entry having subordinate entries with LINE clauses, report lines in successive occurrences are
    /// positioned integer-3 lines vertically beneath the line they occupy in the preceding occurrence").
    /// <para>The displacement Σ ordinal × integer-3 is additive over the enclosing VERTICALLY repeating entries,
    /// so an ABSOLUTE line simply moves by it and stays a compile-time constant. A RELATIVE line has no
    /// compile-time page line at all, so its FIRST occurrence seeds an engine-held anchor with the line it lands
    /// on and every later occurrence places at anchor + displacement — the anchor is that line's base position,
    /// never mutated, because GR12d measures from the PRECEDING OCCURRENCE'S line while LINE-COUNTER holds the
    /// last line PRINTED (§13.18.35.4 GR1), and the lines between them belong to other occurrences.</para>
    /// <para>With NO step phrase there is nothing to displace: GR12's closing sentence — "If no STEP phrase is
    /// specified, the vertical or horizontal interval between successive occurrences is defined by the relative
    /// LINE or COLUMN numbers … specified in the corresponding report section entries" — and the replayed
    /// relative line reproduces exactly that against LINE-COUNTER. That is also the whole of a multiple LINE
    /// clause's §13.18.35.4 GR9 equivalence, whose "unequal vertical intervals" are its several operands.</para>
    /// </summary>
    /// <para>THE NEXT PAGE PHRASE (ISO §13.18.35.2 Format 1; kb/Work PB1001) is a FLAG on the group's first line,
    /// never a kind: <c>integer-1 ON NEXT PAGE</c> is an absolute line and the bare <c>ON NEXT PAGE</c> operand a
    /// relative one, so every placement rule below and in the engine stays the phrase-less rule, and the phrase
    /// adds exactly what §13.18.35.4 GR4a ("no page fit test takes place and the page fit is declared
    /// unsuccessful") and GR5a ("the first line is printed beginning on a new page" — a report footing) say.
    /// <paramref name="groupFirstLine"/> is true only for the group's first report line — the only one SR7 lets
    /// carry the phrase — so a later occurrence of a repeated clause places as its phrase-less twin.</para>
    /// <para>⚠ DETERMINATION (docs/CONFORMANCE.md, the LINE NEXT PAGE block): the bare operand writes no
    /// integer. §13.18.35.4 GR4c's "relative … without the NEXT PAGE phrase" names it a relative clause, and its
    /// integer-2 is never read where the phrase applies — GR5b3 places the first body group on a page at the
    /// FIRST DETAIL integer whatever integer-2 says, and a report footing on a page by itself starts at its
    /// §13.18.57.4 GR7f upper limit, the HEADING integer. The only place an integer-2 IS read is a LATER
    /// occurrence of a repeated bare clause, which advances one line (<see cref="BareNextPageInterval"/>).</para>
    private ReportLineModel RepeatedLine(
        Core.ReportLineOperandContext op, int operand, bool groupFirstLine, Core.ReportGroupEntryContext anchorKey,
        ReportGroupBuild st)
    {
        bool nextPage = groupFirstLine && op.NEXT() is not null;
        bool relative = op.reportRelativeSign() is not null || op.integerOperand() is null;
        int value = op.integerOperand() is { } lit ? IntegerOperandValue(lit, "a report group description entry") ?? RecoveredIntegerOperand : BareNextPageInterval;
        int shift = st.Shift(ReportRepetitionAxis.Vertical);
        bool anchored = st.Repetitions.Exists(
            r => r.Spec.Axis == ReportRepetitionAxis.Vertical && r.Spec.Step is not null);
        if (!relative)
            return new ReportLineModel(ReportLineKindModel.Absolute, value + shift) { NextPage = nextPage };
        if (!anchored)
        {
            st.VerticalCursor += value;      // GR4c: "incremented by integer-2 for each subsequent LINE clause"
            return new ReportLineModel(ReportLineKindModel.Relative, value) { NextPage = nextPage };
        }
        var key = (anchorKey, operand, st.Undisplaced(ReportRepetitionAxis.Vertical));
        if (!st.LineAnchors.TryGetValue(key, out int anchor)) st.LineAnchors[key] = anchor = st.LineAnchors.Count + 1;
        if (shift == 0)
        {
            st.AnchorOffset[anchor] = st.VerticalCursor += value;
            return new ReportLineModel(ReportLineKindModel.Relative, value) { Anchor = anchor, NextPage = nextPage };
        }
        // The GR4c contribution is this line's expected offset minus the cursor, so Σ over the group is exactly
        // "the expected position of the last line of the report group" however the repetitions interleave.
        int expected = st.AnchorOffset.GetValueOrDefault(anchor) + shift;
        int interval = Math.Max(0, expected - st.VerticalCursor);
        st.VerticalCursor = Math.Max(st.VerticalCursor, expected);
        return new ReportLineModel(ReportLineKindModel.Step, shift)
        {
            Anchor = anchor,
            RelativeBase = value,
            TrialInterval = interval,
        };
    }

    /// <summary>The integer-2 a LATER occurrence of a repeated bare <c>ON NEXT PAGE</c> operand places by — the
    /// next line (see the determination on <see cref="RepeatedLine"/>). The first occurrence never reads it.</summary>
    private const int BareNextPageInterval = 1;

    /// <summary>True when any entry in <c>[start, end)</c> carries a LINE clause with an ABSOLUTE operand
    /// (ISO §13.18.38.3 SR25a/SR25b).</summary>
    private static bool AbsoluteLineClauseIn(Core.ReportGroupEntryContext[] entries, int start, int end)
    {
        for (int k = start; k < end; k++)
            foreach (var clause in entries[k].reportGroupClause())
                if (clause.reportLineClause() is { } lc
                    && lc.reportLineOperand().Any(o => o.reportRelativeSign() is null && o.integerOperand() is not null))
                    return true;
        return false;
    }

    /// <summary>True when any entry in <c>[start, end)</c> carries a COLUMN clause with an ABSOLUTE operand
    /// (ISO §13.18.38.3 SR25c/SR25d).</summary>
    private static bool AbsoluteColumnClauseIn(Core.ReportGroupEntryContext[] entries, int start, int end)
    {
        for (int k = start; k < end; k++)
            foreach (var clause in entries[k].reportGroupClause())
                if (clause.reportColumnClause() is { } cc && cc.reportColumnOperand().Any(o => o.reportRelativeSign() is null))
                    return true;
        return false;
    }

    /// <summary>True when <paramref name="self"/> "is subordinate to an entry with a LINE clause" — the
    /// qualifier ISO §13.18.38.3 SR25d and §13.18.38.4 GR10b/GR12b attach to the COLUMN legs. An ANCESTOR is a
    /// preceding entry with a strictly lower level-number, taken innermost-first (the §13.15 level hierarchy).</summary>
    private static bool SubordinateToLineClause(Core.ReportGroupEntryContext[] entries, int self)
    {
        if (!int.TryParse(entries[self].levelNumber().GetText(), out int level)) return false;
        for (int k = self - 1; k >= 0; k--)
        {
            if (!int.TryParse(entries[k].levelNumber().GetText(), out int lv) || lv >= level) continue;
            level = lv;
            if (entries[k].reportGroupClause().Any(c => c.reportLineClause() is not null)) return true;
            if (lv == 1) break;
        }
        return false;
    }

    /// <summary>The number of report LINES one occurrence of a vertically repeating entry occupies — the span
    /// ISO §13.18.38.3 SR26 measures ("sufficient to prevent the overlapping of any line (in the case of
    /// vertical repetition) … of any two consecutive repetitions"). All-relative lines span
    /// 1 + Σ integer-2 over every line but the first; all-absolute lines span max − min + 1. A MIXED entry
    /// returns 0 (not measurable at bind): §13.18.35.3 SR6e already confines that shape to lines under
    /// different PRESENT WHEN clauses, whose overlap the standard leaves to GR3's EC-REPORT-LINE-OVERLAP.</summary>
    private int ReportEntryLineSpan(Core.ReportGroupEntryContext[] entries, int self, int subtreeEnd)
    {
        int relativeSpan = 1, absoluteLow = int.MaxValue, absoluteHigh = int.MinValue, lines = 0;
        bool anyRelative = false, anyAbsolute = false;
        for (int k = self; k < subtreeEnd; k++)
            foreach (var clause in entries[k].reportGroupClause())
            {
                if (clause.reportLineClause() is not { } lc) continue;
                foreach (var op in lc.reportLineOperand())
                {
                    if (op.integerOperand() is not { } lit) continue;
                    int v = IntegerOperandValue(lit, "a report group description entry") ?? RecoveredIntegerOperand;
                    if (op.reportRelativeSign() is not null)
                    {
                        anyRelative = true;
                        if (lines > 0) relativeSpan += v;
                    }
                    else
                    {
                        anyAbsolute = true;
                        absoluteLow = Math.Min(absoluteLow, v);
                        absoluteHigh = Math.Max(absoluteHigh, v);
                    }
                    lines++;
                }
            }
        if (anyRelative && anyAbsolute) return 0;
        if (anyAbsolute) return absoluteHigh - absoluteLow + 1;
        return anyRelative ? relativeSpan : 0;
    }

    /// <summary>True when <paramref name="tree"/> contains a terminal of <paramref name="tokenType"/>.</summary>
    private static bool HasToken(Antlr4.Runtime.Tree.IParseTree tree, int tokenType)
    {
        if (tree is Antlr4.Runtime.Tree.ITerminalNode t) return t.Symbol.Type == tokenType;
        for (int i = 0; i < tree.ChildCount; i++)
            if (HasToken(tree.GetChild(i), tokenType)) return true;
        return false;
    }

    /// <summary>True when <paramref name="tree"/> contains a word terminal spelled <paramref name="word"/>
    /// (case-insensitive) — the VARYING-counter / report-section-name reference scans.</summary>
    private static bool HasWord(Antlr4.Runtime.Tree.IParseTree tree, string word)
    {
        if (tree is Antlr4.Runtime.Tree.ITerminalNode t)
            return CobolNames.Same(t.GetText(), word);
        for (int i = 0; i < tree.ChildCount; i++)
            if (HasWord(tree.GetChild(i), word)) return true;
        return false;
    }

    /// <summary>THE ONE TYPE READER: the report group type a TYPE clause names (ISO §13.18.57 Format 2 + the SR9
    /// abbreviations). <see cref="BindGroupType"/> and the flat-entry screens that need the TYPE before the group is
    /// built (<see cref="ScreenReportLineClauses"/>) both read it here.</summary>
    private static ReportGroupKindModel GroupKindOf(Core.ReportGroupTypeContext t) => t.Start.Type switch
    {
        // Decided by the phrase's FIRST token (and the word after REPORT / PAGE / CONTROL): `PAGE` is also a
        // terminal of a control heading's OR PAGE phrase (§13.18.57.2), so a test on `t.PAGE()` would classify
        // `CONTROL HEADING CX OR PAGE` as a page heading.
        CobolLexer.RH => ReportGroupKindModel.ReportHeading,
        CobolLexer.PH => ReportGroupKindModel.PageHeading,
        CobolLexer.CH => ReportGroupKindModel.ControlHeading,
        CobolLexer.DE or CobolLexer.DETAIL => ReportGroupKindModel.Detail,
        CobolLexer.CF => ReportGroupKindModel.ControlFooting,
        CobolLexer.PF => ReportGroupKindModel.PageFooting,
        CobolLexer.REPORT => t.HEADING() is not null ? ReportGroupKindModel.ReportHeading : ReportGroupKindModel.ReportFooting,
        CobolLexer.PAGE => t.HEADING() is not null ? ReportGroupKindModel.PageHeading : ReportGroupKindModel.PageFooting,
        CobolLexer.CONTROL => t.HEADING() is not null ? ReportGroupKindModel.ControlHeading : ReportGroupKindModel.ControlFooting,
        _ => ReportGroupKindModel.ReportFooting,   // RF — the last of the seven alternatives
    };

    /// <summary>The group type a level-01 entry's written TYPE clause names, for the flat-entry screens that run
    /// before the group is built (<see cref="ScreenReportLineClauses"/>, <see cref="ScreenReportEntryClausePresence"/>):
    /// through <see cref="GroupKindOf"/>, and <see cref="ReportGroupKindModel.Detail"/> — the group model's own
    /// default — when the entry writes none.</summary>
    private static ReportGroupKindModel WrittenGroupKind(Core.ReportGroupEntryContext ge) =>
        WrittenGroupType(ge) is { } t ? GroupKindOf(t) : ReportGroupKindModel.Detail;

    /// <summary>The TYPE phrase an entry writes (the last one when a repeated clause was written — §13.18.57 has no
    /// ellipsis and <see cref="UnrepeatedElements"/> diagnoses it elsewhere), or null.</summary>
    private static Core.ReportGroupTypeContext? WrittenGroupType(Core.ReportGroupEntryContext ge) =>
        ge.reportGroupClause().Select(c => c.reportTypeClause()?.reportGroupType()).LastOrDefault(t => t is not null);

    /// <summary>Map a TYPE clause (ISO §13.18.57 Format 2 + the SR9 abbreviations) onto the group model,
    /// capturing the CH/CF control operand.</summary>
    private void BindGroupType(Core.ReportGroupTypeContext t, ReportGroupModel group, ReportModel model)
    {
        group.Kind = GroupKindOf(t);

        if (group.Kind is ReportGroupKindModel.ControlHeading or ReportGroupKindModel.ControlFooting)
        {
            if (t.reportControlName() is { } name)
            {
                if (name.FINAL() is not null) group.ControlFinal = true;
                else if (name.dataReference() is { } dref)
                    group.ControlOperand = ControlOperandRef(dref, DiagnosticCatalog.ReportControlTypeOperand,
                        $"RD '{model.Name}': TYPE {(group.Kind == ReportGroupKindModel.ControlHeading ? "CH" : "CF")} operand",
                        "ISO §13.18.57.3 SR10");
            }
            // Omitted operand: legal only with a one-operand CONTROL clause (§13.18.57.3 SR11) — resolved later.
            // §13.18.57.2: the OR PAGE phrase belongs to the control HEADING (the grammar writes it nowhere else).
            group.OrPage = t.OR() is not null;
        }
        // PH/PF and the OR PAGE phrase require a PAGE clause that defines the page limit (§13.18.57.3 SR12).
        if ((group.Kind is ReportGroupKindModel.PageHeading or ReportGroupKindModel.PageFooting || group.OrPage)
            && !model.Paged)
            Edition.Error(DiagnosticCatalog.ReportPageTypeRequiresPage, $"RD '{model.Name}': "
                + (group.OrPage ? "the OR PAGE phrase of TYPE CONTROL HEADING" : $"TYPE {group.Kind}")
                + " requires a PAGE clause that defines the page limit (ISO §13.18.57.3 SR12)");
    }

    /// <summary>THE §13.15.4 GR3 REPETITION COUNT of one report group description entry: "An entry that contains
    /// either an OCCURS clause or a LINE or COLUMN clause with more than one operand is said to be a repeating
    /// entry, and the number of repetitions is defined to be integer-2 of the OCCURS clause or the number of
    /// operands of the LINE or COLUMN clause, whichever is applicable. The number of repetitions of an entry that
    /// is not a repeating entry is defined to be 1."
    /// <para>ALL THREE of GR3's vehicles are LIVE (kb/Work PB565): the multiple COLUMN clause as an operand
    /// list on one printable item, and the report-writer OCCURS clause (§13.18.38 Format 3) and the multiple
    /// LINE clause (§13.18.35.4 GR9's "simple OCCURS clause") as the subtree replay, on either axis. An entry
    /// whose vehicle a syntax rule REFUSED never reaches the operand-count screen. An entry carrying both an
    /// OCCURS and a multiple COLUMN clause defines integer-2 × operands distinct report items — the replay
    /// produces exactly that — so GR3's "whichever is applicable" is their product here; an OCCURS and a
    /// multiple LINE clause cannot share an entry (§13.18.35.3 SR10d).</para></summary>
    private static int EntryRepetitions(List<ReportColumnSpec> columns, ReportOccursSpec? occurs = null)
        => (columns.Count > 1 ? columns.Count : 1) * (occurs?.Max ?? 1);

    /// <summary>The repetition counts governing an entry, INNERMOST FIRST: the entry's own (§13.15.4 GR3) then
    /// each enclosing entry's, so §13.18.63.3 SR35 / §13.18.53.3 SR6 read "the repeating entry … multiplied by
    /// the number of repetitions of any number of successive repeating entries at higher levels" straight off
    /// the scope stack.</summary>
    private static List<int> RepetitionChain(List<ReportColumnSpec> columns, ReportOccursSpec? occurs,
        List<(int Level, Core.ConditionContext? Cond, int Reps, string? Name)> chain)
    {
        var reps = new List<int>(chain.Count + 1) { EntryRepetitions(columns, occurs) };
        for (int i = chain.Count - 1; i >= 0; i--) reps.Add(chain[i].Reps);
        return reps;
    }

    /// <summary>⛔ THE MULTI-OPERAND REPETITION RULE — ONE READER, TWO CLAUSES (kb/Work PB506). ISO §13.18.63.3
    /// SR35 (VALUE) and §13.18.53.3 SR6 (SOURCE) are the same rule written twice: "If the … clause has more than
    /// one operand, the entry shall be a repeating entry or shall be subordinate to a repeating entry. The number
    /// of operands … shall be equal to the number of repetitions of the repeating entry or the same number
    /// multiplied by the number of repetitions of any number of successive repeating entries at higher levels
    /// than the repeating entry." The admissible counts are therefore the PREFIX PRODUCTS of
    /// <paramref name="repChain"/> — a non-repeating frame contributes a factor of 1, so the set is the same
    /// whether or not the non-repeating frames are filtered out — and sentence 1 is exactly "the full product is
    /// greater than 1". A single-operand clause is unconstrained by either sentence.</summary>
    private void ScreenRepeatingOperandCount(int written, List<int> repChain, string where, string clause,
        DiagnosticDescriptor code, string rule)
    {
        if (written <= 1) return;
        int product = 1;
        var admissible = new List<int>(repChain.Count);
        foreach (int r in repChain) { product *= r; admissible.Add(product); }
        if (product <= 1)
        {
            Edition.Error(code, $"{where}: a {clause} clause with more than one operand ({written} written) requires "
                + $"the entry to be a repeating entry or to be subordinate to a repeating entry ({rule}); this entry "
                + "has no repetition — a multiple COLUMN clause, a multiple LINE clause or an OCCURS clause "
                + "(ISO §13.15.4 GR3)");
            return;
        }
        if (!admissible.Contains(written))
            Edition.Error(code, $"{where}: a {clause} clause with more than one operand shall have as many operands "
                + $"as the entry has repetitions, or that number multiplied by the repetitions of successive higher "
                + $"repeating entries ({rule}) — {written} operands against "
                + $"{string.Join(" / ", admissible.Distinct())} admissible");
    }

    /// <summary>⛔ THE ONE CLASSIFIER OF A REPORT VALUE-CLAUSE OPERAND (kb/Work PB852 × PB883). §13.18.53.2 and
    /// §13.18.54.2 both print `{ identifier-1 | arithmetic-expression-1 }` (SUM adds data-name-1, also an
    /// identifier), and the grammar gives both clauses ONE production, so the form is decided HERE for both:
    /// returns the operand's bare <c>dataReference</c> when the whole expression is nothing but that reference —
    /// no operator, no parentheses, no function — else null, meaning arithmetic-expression-1. Writing the test
    /// once is the point: SOURCE's SR4/SR5 and SUM's SR4/SR5/SR6 all key on which form was written, and two
    /// copies of this walk would be the two-arm dispatch this repo keeps rediscovering.</summary>
    internal static Core.DataReferenceContext? BareReferenceOf(Core.ReportValueOperandContext op)
    {
        var add = op.arithmeticExpression()?.additiveExpression();
        if (add is null || add.addOp().Length > 0) return null;
        var mul = add.multiplicativeExpression();
        if (mul.Length != 1 || mul[0].mulOp().Length > 0) return null;
        var pow = mul[0].powerExpression();
        if (pow.Length != 1 || pow[0].POWER().Length > 0) return null;
        var un = pow[0].unaryExpression();
        if (un.Length != 1) return null;
        // `unaryExpression : addOp unaryExpression | primaryExpression` — a signed operand is an expression.
        return un[0].primaryExpression()?.dataReference();
    }

    /// <summary>True when the operand is written enclosed in parentheses — the shape §13.18.53.3 SR7 requires of
    /// every operand of a multi-operand SOURCE clause that contains an arithmetic-expression. The parenthesized
    /// form is <c>primaryExpression : LPAREN arithmeticExpression RPAREN</c> reached with no operator above it,
    /// so it is the same walk as <see cref="BareReferenceOf"/> ending one alternative over.</summary>
    internal static bool IsParenthesized(Core.ReportValueOperandContext op)
    {
        var add = op.arithmeticExpression()?.additiveExpression();
        if (add is null || add.addOp().Length > 0) return false;
        var mul = add.multiplicativeExpression();
        if (mul.Length != 1 || mul[0].mulOp().Length > 0) return false;
        var pow = mul[0].powerExpression();
        if (pow.Length != 1 || pow[0].POWER().Length > 0) return false;
        var un = pow[0].unaryExpression();
        // GROUPING-PAREN-ONLY: §13.18.53.3 SR7 asks whether the operand is "enclosed in parentheses", and the
        // only paren that encloses an OPERAND is `primaryExpression : LPAREN arithmeticExpression RPAREN`. A
        // FUNCTION argument list's paren (FNARG_LPAREN, §8.4.3.2.3 SR6) belongs to `functionCall`, a different
        // alternative of the same rule, and does not enclose the operand — `SOURCES ARE FUNCTION MAX(A B) (C)`
        // leaves the first operand unparenthesized, which is exactly what SR7 refuses.
        return un.Length == 1 && un[0].primaryExpression()?.LPAREN() is not null;
    }

    /// <summary>ISO §13.18.53.3 SR7 — "If the SOURCE clause has more than one operand of which at least one is an
    /// arithmetic-expression, each operand shall be enclosed in parentheses." ENFORCED, not assumed from the
    /// grammar's shape: operands are separated by nothing but a space, so without the parentheses the standard's
    /// own general format would not say where one operand ends. EVERY operand takes them, including the bare
    /// identifiers. (kb/Work PB852.)</summary>
    private void ScreenSourceOperandParens(Core.ReportValueOperandContext[] ops, string where)
    {
        if (ops.Length <= 1) return;
        if (!ops.Any(o => IdentifierOperandOf(o) is null)) return;   // every operand is identifier-1 — SR7 is silent
        foreach (var o in ops.Where(o => !IsParenthesized(o)))
            Edition.Error(DiagnosticCatalog.ReportSourceOperandParens, $"{where}: the SOURCE clause has "
                + $"{ops.Length} operands of which at least one is an arithmetic-expression, so each operand "
                + $"shall be enclosed in parentheses (ISO §13.18.53.3 SR7); '{o.GetText()}' is not.");
    }

    /// <summary>⛔ THE ONE FORM CLASSIFIER OF A REPORT VALUE-CLAUSE OPERAND (kb/Work PB852 × PB883 × PB1316): the bare
    /// <c>dataReference</c> an operand IS when it is written as identifier-1 / data-name-1 — else null, meaning
    /// arithmetic-expression-1. <see cref="BareReferenceOf"/> is the SYNTACTIC walk; this adds the one semantic fact
    /// the syntax cannot say: a bare word that names a CONSTANT is no identifier. §13.10.3 SR2 lets a constant-name
    /// "be used anywhere that a format specifies a literal of the class and category of constant-name-1", the
    /// operand brace of §13.18.53.2 / §13.18.54.2 specifies an arithmetic-expression, and an arithmetic-expression's
    /// operand may be a numeric literal — so `SOURCE KC` is arithmetic-expression-1 whose one operand is the constant
    /// (SR3 then asks the entry for a numeric item, a non-numeric constant is §8.8.1.1's refusal), never an
    /// identifier that fails to resolve in storage. SOURCE and SUM both ask it here, so neither can drift.</summary>
    private Core.DataReferenceContext? IdentifierOperandOf(Core.ReportValueOperandContext op) =>
        BareReferenceOf(op) is { } dref && ConstantOf(dref) is null ? dref : null;

    /// <summary>Bind ONE SOURCE clause operand (ISO §13.18.53.2 — the clause writes
    /// `{ identifier-1 | arithmetic-expression-1 } …`). identifier-1 is §13.18.53.4 GR1's implicit MOVE sender; an
    /// arithmetic-expression-1 operand, an identifier under the clause's ROUNDED phrase (SR5) and a constant-name
    /// are <see cref="FieldComputeSource"/>, GR2's implicit COMPUTE. BOTH arms keep the operand as its parse tree
    /// and bind it in the PROCEDURE phase through the ONE host binder (the kb/Work PB482 argument: a subscript may
    /// be an index-name or an expression and has no value at data bind), so every shape an operand can take — a
    /// subscript, a reference-modification, a counter of this or another report, a sum counter of the current
    /// report, a VARYING counter in scope — is bound by the machinery that already knows it.</summary>
    private ReportFieldSource BindSourceOperand(Core.ReportValueOperandContext op, Core.RoundedPhraseContext? rounded)
    {
        // §13.18.53.3 SR5 makes an identifier-1 written WITH the ROUNDED phrase an arithmetic-expression, so the
        // two forms merge here and GR2's COMPUTE governs both (kb/Work PB852).
        if (IdentifierOperandOf(op) is not { } dref || rounded is not null)
            return new FieldComputeSource(op, AsWritten(op)) { Rounded = rounded };
        return new FieldReferenceSource(dref, AsWritten(op));
    }

    /// <summary>⛔ ISO §13.18.54.4 GR1 — the sum counter's digit count, "derived from the corresponding number of
    /// digits, excluding insertion editing characters, in the PICTURE clause of the entry containing the SUM
    /// clause". It is the counter's CAPACITY: an addition past it is the GR3 size error, and a set size error
    /// indicator prints the item as spaces (GR4), so an undercount is a wrong answer on every report (kb/Work
    /// PB1296). The count is per category, over the categories §13.18.54.3 SR2 admits (a receiving operand of a
    /// numeric MOVE):
    /// <list type="bullet">
    /// <item>numeric — the '9' positions (<see cref="PicInfo.Digits"/>, the digits the item stores);</item>
    /// <item>numeric-edited — every DIGIT POSITION (<see cref="PicInfo.DigitPositions"/>: '9', 'Z', '*' and the
    /// floating-insertion positions), not <see cref="PicInfo.Digits"/>, which counts only the '9's — PIC ZZ9 is a
    /// three-digit counter, and reading it as a one-digit one made every total past 9 print as spaces;</item>
    /// <item>alphanumeric / national, edited or not — each character position except an insertion editing
    /// character ('B', '0', '/' — the simple insertion of §13.18.40.5 GR1): the MOVE of an unsigned integer into such an
    /// item places one digit per position, so X(4) holds a four-digit counter. The clamp this replaced read the
    /// category's zero '9' count as ONE digit, printing "4" for a total of 24 (or spaces once the size error
    /// indicator existed).</item>
    /// </list></summary>
    private static int SumCounterDigits(PicInfo pic) => pic.Category switch
    {
        PicCategory.Numeric => pic.Digits,
        PicCategory.NumericEdited => pic.DigitPositions,
        _ => pic.EditMask is { } mask
            ? mask.Count(c => c is not ('B' or '0' or '/'))
            : DataItem.DisplayTextWidthOf(pic),
    };

    /// <summary>
    /// The <see cref="ReportSumFamily"/> of SUM entry <paramref name="ge"/> — created at the entry's FIRST replay and
    /// found again by every later one (kb/Work PB1271). Creating it RESERVES the family's counter-id block (the next
    /// <see cref="ReportSumFamily.Count"/> ids after the previous family's), builds the register the counter is as
    /// a data item (GR1), and publishes the counter's name (GR5).
    /// <para><b>The extents</b> are §13.15.4 GR3's repetition vehicles, outermost first: one per enclosing
    /// repeating entry (<see cref="ReportGroupBuild.Repetitions"/> — an OCCURS clause, or a multiple LINE clause by
    /// §13.18.35.4 GR9), then the entry's own multiple COLUMN clause by §13.18.14.4 GR12 ("functionally equivalent to
    /// a COLUMN clause with a single operand, together with a simple OCCURS clause whose integer is equal to the
    /// number of operands"). §13.18.14.3 SR10 a) forbids an OCCURS clause in the same entry as a multiple COLUMN
    /// clause, so the entry's own vehicle is at most one of the two.</para>
    /// <para><b>The register</b> is the counter's ONE description, shared by every occurrence: the GR1 profile
    /// (<see cref="PicInfo.SumCounterItem"/>), and for a repeating entry a synthetic ancestor per enclosing
    /// repetition with <see cref="DataItem.Occurs"/> = its extent, the register itself carrying the COLUMN extent —
    /// so §8.4.2.3.3 SR3's count (<see cref="DataItem.SubscriptArity"/>) and order
    /// (<see cref="DataItem.SubscriptLevels"/>) are the ordinary ones.</para>
    /// </summary>
    private ReportSumFamily SumFamilyOf(Core.ReportGroupEntryContext ge, string? entryName, string? picText,
        int columnCount, List<(int Level, Core.ConditionContext? Cond, int Reps, string? Name)> chain,
        ReportModel model, ReportGroupBuild st)
    {
        if (st.SumFamilies.TryGetValue(ge, out var known)) return known;
        // Scale-derivation analysis (GR1) — threads the edition + the program currency symbol like every other
        // Analyze site (a custom §12.3.7 currency symbol in a SUM counter's PICTURE must classify, not error).
        string sumWhere = $"RD '{model.Name}' SUM counter '{entryName ?? "FILLER"}'";
        var pic = picText is not null
            ? PictureAnalyzer.Analyze(picText, Usage.Display, Edition, sumWhere, currencies: CurrencySigns,
                decimalPointIsComma: DecimalPointIsComma)
            : null;
        ScreenReportEntryPicture(ge, pic, picText, sumWhere, summed: true);   // §13.18.40.3 SR14, §13.18.54.3 SR2
        // ⛔ THE COUNTER'S SCALE IS THE ENTRY PICTURE'S RECEIVER SCALE, NOT `pic.Scale` (kb/Work PB1685). §13.18.54.4 GR1
        // derives the counter's digits — integral AND fractional — from the entry's PICTURE excluding insertion editing
        // characters, and a NUMERIC-EDITED PicInfo carries Scale 0 (its fraction lives in the mask), so `PIC 99.99 SUM
        // WS-F` registered a scale-0 counter and truncated every addend to an integer (2.75 twice printed 04.00 for the
        // 05.50 owed). The one rule every store already asks (PicInfo.ReceiverScale) answers it.
        int sumScale = pic?.ReceiverScale() ?? 0;
        int baseId = model.SumFamilies.Count == 0 ? 0 : model.SumFamilies[^1].BaseId + model.SumFamilies[^1].Count;
        // The synthetic OCCURS chain: one ancestor per enclosing repetition (outermost first), each an implicit
        // table level of the counter — never storage, never in ByName, reachable only through the register.
        DataItem? parent = null;
        for (int k = 0; k < st.Repetitions.Count; k++)
        {
            var level = new DataItem
            {
                Level = 48,
                DeclaredAt = Edition.Cursor,
                CsName = $"{NamingConvention.SumCounterName(model.Name, baseId)}_occ{k}",
                Occurs = st.Repetitions[k].Spec.Max,
                Parent = parent,
            };
            level.Uid = _uidCounter++;
            parent = level;
        }
        var family = new ReportSumFamily
        {
            Name = entryName,
            Report = model,
            Group = st.Group!,
            BaseId = baseId,
            Repetitions = [.. st.Repetitions.Select(f => f.Spec)],
            Columns = Math.Max(1, columnCount),
            Scale = sumScale,
            // §8.4.2.2.3 SR4 — the counter is subordinate to every level above its entry (PB1454).
            Qualification = [.. Enumerable.Reverse(chain).Select(f => f.Name)],
            // The counter AS A DATA ITEM (GR1) — the implicitly-defined register a procedure division reference
            // resolves to (GR5 names it, GR12 permits altering it). Off ByName/Roots, exactly like the OCCURS
            // DYNAMIC CAPACITY register: its value IS the engine's, so it allocates no storage.
            Register = new DataItem
            {
                Level = 49,
                DeclaredAt = Edition.Cursor,
                CobolName = entryName,
                CsName = NamingConvention.SumCounterName(model.Name, baseId),
                Pic = PicInfo.SumCounterItem(pic is null ? 18 : SumCounterDigits(pic), sumScale),
                Occurs = columnCount > 1 ? columnCount : null,
                Parent = parent,
                Uid = _uidCounter++,
            },
            // Preserve a floating-point-edited / national-edited PICTURE gate for the post-bind GateData report-Sums
            // walk (this PicInfo is otherwise discarded — only Scale is used — so the 0900 would drop; DEVLOG 740).
            SkeletonGate = pic is null ? null : CobolNet.Validation.VersionConformancePass.PictureConstructId(pic),
            SkeletonWhere = sumWhere,
        };
        st.SumFamilies[ge] = family;
        model.SumFamilies.Add(family);
        model.EntryFamilies.Add(family);
        // §13.18.54.4 GR5 — a data-name immediately after the level number names THE COUNTER. Publish it into
        // the source element's name space so GR12's permission to read or alter it can be exercised; the entry
        // keeps its own counter whether or not another entry spells its name the same way (GR1, kb/Work PB882).
        if (entryName is not null)
        {
            if (!_sumCounters.TryGetValue(entryName, out var homonyms))
                _sumCounters[entryName] = homonyms = [];
            homonyms.Add((model, family));
        }
        return family;
    }

    /// <summary>The <see cref="ReportSourceFamily"/> of SOURCE / VALUE entry <paramref name="ge"/> — created at the
    /// entry's FIRST replay and found again by every later one (kb/Work PB1294), the twin of
    /// <see cref="SumFamilyOf"/>: the same repetition geometry, so a rolled total maps an addend's occurrences onto a
    /// counter's by one arithmetic (§13.18.54.4 GR8).</summary>
    private ReportSourceFamily SourceFamilyOf(Core.ReportGroupEntryContext ge, string? entryName, int columnCount,
        List<(int Level, Core.ConditionContext? Cond, int Reps, string? Name)> chain,
        ReportModel model, ReportGroupBuild st)
    {
        if (st.SourceFamilies.TryGetValue(ge, out var known)) return known;
        var family = new ReportSourceFamily
        {
            Name = entryName,
            Report = model,
            Group = st.Group!,
            Repetitions = [.. st.Repetitions.Select(f => f.Spec)],
            Columns = Math.Max(1, columnCount),
            // §8.4.2.2.3 SR4 — the entry is subordinate to every level above it (kb/Work PB1454).
            Qualification = [.. Enumerable.Reverse(chain).Select(f => f.Name)],
        };
        st.SourceFamilies[ge] = family;
        model.EntryFamilies.Add(family);
        return family;
    }

    /// <summary>Record the occurrences of a SOURCE / VALUE entry for THIS replay (kb/Work PB1294): <paramref name="placements"/>
    /// of them — one per COLUMN operand of a multiple COLUMN clause, else one — each taking operand
    /// <c>(first ordinal + j) mod count</c> of <paramref name="operands"/> (§13.18.53.4 GR4 / §13.18.63.4 GR23:
    /// "successive operands are assigned to successive repeating printable items … If no further operands remain,
    /// assignment begins again from the first operand"), and each carrying the FULL presence chain §13.18.54.4 GR11
    /// asks of data-name-1 — the conditions of every enclosing entry and of its own, and this replay's OCCURS …
    /// DEPENDING tests — exactly as a SUM entry's counter carries them (GR10).</summary>
    private void RecordSourceOccurrences(ReportSourceFamily family, IReadOnlyList<ReportFieldSource> operands,
        int placements, Core.ConditionContext? ownCond, ReportGroupBuild st, IReadOnlyList<ReportVaryingUse> varyings,
        ReportModel model, int? firstOrdinal = null)
    {
        for (int j = 0; j < Math.Max(1, placements); j++)
        {
            var coordinates = new List<int>(st.Repetitions.Count + 1);
            foreach (var frame in st.Repetitions) coordinates.Add(frame.Ordinal);
            if (family.Columns > 1) coordinates.Add(j);
            int ordinal = firstOrdinal is { } first ? first + j : family.Linear(coordinates);
            var occurrence = new ReportItemOccurrence
            {
                Family = family, Coordinates = coordinates, Source = operands[ordinal % operands.Count],
            };
            foreach (var (_, cond, _, _) in st.Chain) if (cond is not null) occurrence.PresentWhenCtxs.Add(cond);
            if (ownCond is not null) occurrence.PresentWhenCtxs.Add(ownCond);
            occurrence.RepetitionGuards.AddRange(st.GuardsHere());
            occurrence.Varyings.AddRange(varyings);
            occurrence.VaryingDependent = occurrence.Source switch
            {
                FieldReferenceSource rs => varyings.Any(u => HasWord(rs.Ref, u.Counter.Name)),
                FieldComputeSource cs => varyings.Any(u => HasWord(cs.Ctx, u.Counter.Name)),
                _ => false,
            };
            family.Occurrences.Add(occurrence);
            model.ItemOccurrences.Add(occurrence);
        }
    }

    /// <summary>The VARYING counters in scope at entry <paramref name="ge"/> (§13.18.64.3 SR2): its own and every
    /// enclosing entry's, outermost first, each with the occurrence of its declaring entry the placement lies in
    /// (GR3). An enclosing entry's occurrence is its replay's ordinal; this entry's own, when it repeats through a
    /// multiple COLUMN clause as well, is counted per placement (the emitter adds the placement index), after the
    /// replay ordinal's columns. The ONE reader a printable field and an unprintable SOURCE entry share.</summary>
    private static List<ReportVaryingUse> VaryingUsesOf(Core.ReportGroupEntryContext ge, int columnCount, ReportGroupBuild st)
    {
        var uses = new List<ReportVaryingUse>();
        foreach (var (declaring, counters, frame) in st.Varying)
        {
            bool own = ReferenceEquals(declaring, ge);
            int perReplay = own ? Math.Max(columnCount, 1) : 1;
            foreach (var counter in counters)
                uses.Add(new ReportVaryingUse(counter, (frame?.Ordinal ?? 0) * perReplay,
                    PerPlacement: own && columnCount > 1));
        }
        return uses;
    }

    /// <summary>The category of an UNPRINTABLE SOURCE entry's PICTURE — what §13.18.54.3 SR4's "numeric data item"
    /// asks of data-name-1 when no printable item was analysed for it (kb/Work PB1294). Null for an entry with no
    /// PICTURE. The analysis is the printable item's own, so an entry's PICTURE is judged by one analyzer; it is run
    /// ONLY for the unprintable entry, whose printable twin does not exist, so no diagnostic is raised twice.</summary>
    private PicCategory? UnprintablePictureCategory(Core.ReportGroupEntryContext ge, string? picText, string? usageText,
        SignSpec? ownSign, List<EditingPhraseSpec>? editing, LocaleEditSpec? locale, string where)
    {
        if (picText is null) return null;
        var usage = PictureAnalyzer.ParseUsage(usageText, Edition, where);
        var pic = PictureAnalyzer.Analyze(picText, usage, Edition, where, ownSign, currencies: CurrencySigns,
            editing: editing, localeFormat2: locale, decimalPointIsComma: DecimalPointIsComma);
        ScreenReportEntryPicture(ge, pic, picText, where, summed: false);
        return pic?.Category;
    }

    /// <summary>The report entries whose analysed PICTURE has been screened by <see cref="ScreenReportEntryPicture"/>: a
    /// repeating entry's replayed copies, and the entry's printable item and SUM counter (two analyses of one
    /// PICTURE), report a fault once, at the written entry.</summary>
    private readonly HashSet<Core.ReportGroupEntryContext> _reportPicturesScreened = [];

    /// <summary>⛔ THE RULES THAT NEED A REPORT ENTRY'S ANALYSED PICTURE, asked once per written entry from every site
    /// that analyses one (the printable item, the SUM counter, the unprintable SOURCE entry):
    /// <list type="bullet">
    /// <item>ISO §13.18.40.3 SR14 (kb/Work PB1687): §13.15.4 GR2 makes the PICTURE of a report group description entry
    /// "the same clause" as a data description entry's, "obeying the syntax rules and general rules defined for" it,
    /// so the digit-position limit — 31, and COBOL-85's 18 — is asked through the data division's own
    /// <see cref="ScreenPictureDigitCapacity"/>.</item>
    /// <item>ISO §13.18.54.3 SR2 (kb/Work PB1295), for an entry with a SUM clause (<paramref name="summed"/>): "The
    /// category of the subject of the entry shall be valid as the category of a receiving operand in a MOVE
    /// statement for a sending operand of the category numeric" — asked of the one MOVE validity chain
    /// (<see cref="MoveTable16.NumericSenderRefusal"/>).</item>
    /// </list></summary>
    private void ScreenReportEntryPicture(Core.ReportGroupEntryContext ge, PicInfo? pic, string? picText, string where, bool summed)
    {
        if (picText is null || pic is null || !_reportPicturesScreened.Add(ge)) return;
        ScreenPictureDigitCapacity(pic, $"{where} (PICTURE {picText})");
        if (summed && MoveTable16.NumericSenderRefusal(Table16Operand.Of(pic)) is { } why)
            Edition.Error(DiagnosticCatalog.ReportSumEntryCategory, $"{where}: its PICTURE {picText} is of a category that cannot "
                + $"receive a numeric sum, and the entry contains a SUM clause: {why}. The category of the subject of the entry "
                + "shall be valid as the category of a receiving operand in a MOVE statement for a sending operand of the "
                + "category numeric (ISO §13.18.54.3 SR2)");
    }

    /// <summary>Bind ONE OCCURRENCE of an entry's SUM clause (ISO §13.18.54) into the <see cref="ReportSumModel"/>
    /// whose id is <paramref name="id"/> (<see cref="ReportSumFamily.IdAt"/>): the addend TERMS, their UPON
    /// operands, and the RESET operand. The counter's name, register and scale are the entry's
    /// (<paramref name="family"/>).
    /// <para>⛔ IT TAKES EVERY <c>SUM …</c> GROUP OF THE CLAUSE, not one (kb/Work PB482). §13.18.54.3 SR1 — "The
    /// whole clause is referred to as a SUM clause even though the SUM keyword may appear more than once" — and
    /// §13.18.54.4 GR1 establishes ONE counter per ENTRY, so the groups are terms of a single counter and each
    /// keeps its OWN UPON list (GR7c2 attaches the phrase to its group). The clause's one RESET phrase and one
    /// rounded-phrase follow the groups (§13.18.54.2; kb/Work PB1295), and the grammar says so.</para></summary>
    private ReportSumModel BindSumClause(Core.ReportSumClauseContext clause, ReportSumFamily family,
        int id, string? entryName, ReportGroupModel group, ReportModel model, bool hasColumn)
    {
        var sum = new ReportSumModel
        {
            // §13.18.54.4 GR1 — one counter per ENTRY OCCURRENCE: the identity is the occurrence's id within the
            // report description, and GR5's data-name rides on the family as the counter's NAME (kb/Work PB882).
            Id = id,
            Family = family,
            PrintedIn = group,
        };
        foreach (var sg in clause.reportSumGroup())
        {
            var term = new ReportSumTerm();
            foreach (var op in sg.reportValueOperand())
                term.Addends.Add(SumAddendRef(op, model));
            // UPON data-name-2 (SR7) — the WHOLE written reference: the one qualifier the rule allows is a
            // report-name, and §8.4.3.3.3 SR5's NOTE bars a ref-mod wherever a general format writes
            // data-name-n. Resolution waits for ResolveReports (a detail may be described after this entry).
            foreach (var up in sg.dataReference())
                if (UponDetailRef(up, model) is { } det) term.Upon.Add(det);
            sum.Terms.Add(term);
        }
        // §13.18.54.2 — the rounded-phrase sits OUTSIDE the repeated SUM … UPON group (PDF p487 rendered), so the
        // clause has at most one. It governs §13.18.54.4 GR4's delivery of the counter to the printable item, which
        // is why SR3 requires the COLUMN clause that defines that item (kb/Work PB852's sibling sweep).
        if (clause.roundedPhrase() is { } rnd)
        {
            if (!hasColumn)
                Edition.Error(DiagnosticCatalog.ReportSumRoundedWithoutColumn, $"RD '{model.Name}' entry "
                    + $"'{entryName ?? "FILLER"}': the SUM clause writes a ROUNDED phrase, which is permitted "
                    + "only if the COLUMN clause is specified for the subject of the entry (ISO §13.18.54.3 "
                    + "SR3) — the phrase governs §13.18.54.4 GR4's transfer of the sum counter to the "
                    + "printable item, and this entry defines none.");
            else
                sum.Rounded = rnd;
        }
        // §13.18.54.2 — the RESET phrase sits OUTSIDE the repeated SUM … UPON group, so the clause has at most one.
        if (clause.reportSumReset() is { } reset)
        {
            if (reset.FINAL() is not null) sum.ResetFinal = true;
            else if (reset.dataReference() is { } rref)
                sum.ResetOperand = ControlOperandRef(rref, DiagnosticCatalog.ReportResetNotControlOperand,
                    $"RD '{model.Name}': SUM … RESET ON operand", "ISO §13.18.54.3 SR8");
        }
        model.Sums.Add(sum);   // in binding order; SealSumCounters puts the list in id order
        return sum;
    }

    /// <summary>⛔ THE ONE CAPTURE OF A SUM ADDEND (kb/Work PB482). An addend written as <c>identifier-1</c> is an
    /// ORDINARY IDENTIFIER — §8.4.3.1.2 Format 2, <i>qualified-data-name-with-subscripts</i> — so the whole
    /// written reference is kept and the VALUE is bound in the procedure phase through the one expression binder.
    /// Only the shape a syntax rule forbids is screened here, lexically:
    /// <para>A REFERENCE-MODIFIED addend is rejected. §13.18.54.3 SR5 requires identifier-1 to "specify a numeric
    /// data item", and §8.4.3.3.4 GR6 c) makes the unique data item reference modification creates "class and
    /// category alphanumeric" unless the usage is national — never numeric — so no reference-modified spelling
    /// can satisfy SR5. It was being DROPPED silently: <c>SUM WS-TXT(1:2)</c> summed the whole item.</para>
    /// <para>The SUBSCRIPT is NOT screened — it is the legal spelling this helper exists to carry (§8.4.2.3
    /// subscripting an identifier), and it reaches the emitter as a bound expression.</para>
    /// <para>⛔ THE THIRD ADDEND FORM (kb/Work PB883). §13.18.54.3 SR1 — "Each data-name-1, identifier-1 or
    /// arithmetic-expression-1 is an addend" — and an EXPRESSION addend is not a reference at all: its rule is
    /// SR6 ("any identifiers it contains may reference entries in any section of the data division other than
    /// the report section"), never SR4/SR5's, and §13.18.54.4 GR3 gives it the COMPUTE-with-ON-SIZE-ERROR
    /// accumulation in place of the ADD. It carries no base name to screen here; the whole tree goes to the ONE
    /// expression binder, and the SR6 screen runs at resolution where the report-section name set is known.</para></summary>
    private ReportSumAddend SumAddendRef(Core.ReportValueOperandContext op, ReportModel model)
    {
        string written = AsWritten(op);
        if (IdentifierOperandOf(op) is not { } dref)
            return new ReportSumAddend { Ctx = op, Reference = null, Name = "", Qualifiers = [], Written = written };
        var (name, quals) = KeyReference(dref);
        var addend = new ReportSumAddend
        {
            Ctx = op, Reference = dref, Name = name, Qualifiers = quals, Written = written,
        };
        var sfx = ReferenceResolver.ReadOperandSuffixes(dref);
        if (sfx.RefMods > 0)
        {
            Edition.Error(DiagnosticCatalog.ReportSumAddendNotNumeric, $"RD '{model.Name}': SUM addend '{written}' is reference-modified. "
                + "The addend shall specify a numeric data item (ISO §13.18.54.3 SR5), and reference "
                + "modification creates a data item of class and category alphanumeric unless the usage is "
                + "national (§8.4.3.3.4 GR6 c), so a reference-modified addend is never numeric.");
            addend.Rejected = true;
        }
        return addend;
    }

    /// <summary>⛔ THE ONE CAPTURE OF AN <c>UPON data-name-2</c> OPERAND (kb/Work PB482). ISO §13.18.54.3 SR7:
    /// "Data-name-2 shall be the name of a detail. It may be qualified only by a report-name." The operand is
    /// therefore a report-group reference exactly as <c>GENERATE data-name-1</c> writes one, and it resolves
    /// through the ONE funnel (<see cref="ReportGroupResolution"/>) in <see cref="ResolveReports"/>, once every
    /// group is described. Here only the SHAPE is screened — a data-name-n position admits no reference
    /// modification (§8.4.3.3.3 SR5 NOTE) and no subscript (§8.4.2.3.3 SR2 permits one only for an item that has
    /// an OCCURS clause, which a report group never does), and SR7 allows at most ONE qualifier.
    /// <para>Before this, the operand was reduced to <c>up.cobolWord()?.GetText()</c> — the first word — so the
    /// report-name qualifier was dropped and nothing checked that the name was a detail at all: <c>UPON CFT</c>
    /// (a control footing) and <c>UPON NOSUCH</c> both compiled clean and silently totalled nothing.</para>
    /// Returns null when the operand is rejected, so it contributes no run-time filter entry.</summary>
    private ReportDetailRef? UponDetailRef(Core.DataReferenceContext dref, ReportModel model)
    {
        var (name, quals) = KeyReference(dref);
        string written = AsWritten(dref);
        var sfx = ReferenceResolver.ReadOperandSuffixes(dref);
        if (sfx.Subscripts > 0 || sfx.RefMods > 0)
        {
            Edition.Error(DiagnosticCatalog.ReportSumUponNotDetail, $"RD '{model.Name}': the UPON operand '{written}' is "
                + $"{(sfx.RefMods > 0 ? "reference-modified" : "subscripted")}. Data-name-2 shall be the name of "
                + "a detail (ISO §13.18.54.3 SR7) — a report group is named, never indexed, and where a general "
                + "format writes data-name-n reference modification is not permitted (§8.4.3.3.3 SR5 NOTE).");
            return null;
        }
        if (quals.Count > 1)
        {
            Edition.Error(DiagnosticCatalog.ReportSumUponNotDetail, $"RD '{model.Name}': the UPON operand '{written}' carries "
                + $"{quals.Count} qualifiers; data-name-2 \"may be qualified only by a report-name\" (ISO "
                + "§13.18.54.3 SR7), which is one qualifier (§8.4.2.2.2 Format 1).");
            return null;
        }
        return new ReportDetailRef(name, quals.Count == 1 ? quals[0] : null);
    }

    /// <summary>⛔ THE FD SIDE OF THE report-name ↔ REPORT-clause CORRESPONDENCE (kb/Work PB1285). The binding below walks
    /// the RDs and takes the first FD that names each, so the rules about the FDs' own REPORT clauses were never asked:
    /// §13.18.46.3 SR1 — "Each report-name-1 shall be the subject of a report description entry in the report
    /// section of the same source element" (a REPORT clause naming no RD), and §13.18.46.3 SR2 — "Each report-name-1
    /// may appear in only one REPORT clause" (with §13.14.3 SR1, "one and only one REPORT clause specifying
    /// report-name-1": a name repeated inside one clause is the same violation). The RD named by NO clause is the
    /// third arm of §13.14.3 SR1 and is reported where the file is resolved.</summary>
    private void ScreenReportClauseNames()
    {
        var rdNames = new HashSet<string>(Reports.Select(r => r.Name), CobolNames.Comparer);
        var firstClause = new Dictionary<string, FileModel>(CobolNames.Comparer);
        foreach (var file in Files)
            foreach (var name in file.ReportNames)
            {
                if (!rdNames.Contains(name))
                    Edition.Error(DiagnosticCatalog.ReportClauseNameRule, $"file '{file.CobolName}': the REPORT clause names "
                        + $"'{name}', which is not the subject of a report description entry in the report section: each "
                        + "report-name-1 shall be (ISO §13.18.46.3 SR1)");
                else if (firstClause.TryGetValue(name, out var first))
                    Edition.Error(DiagnosticCatalog.ReportClauseNameRule, $"file '{file.CobolName}': the REPORT clause names "
                        + $"'{name}', which is already named by the REPORT clause of file '{first.CobolName}': each "
                        + "report-name-1 may appear in only one REPORT clause (ISO §13.18.46.3 SR2; §13.14.3 SR1)");
                else
                    firstClause[name] = file;
            }
    }

    /// <summary>ISO §13.18.12.3 SR3 — "If the CODE clause is specified for any report, it shall be specified for each
    /// report associated with the same report file." The code is what tells one report's records from another's on a
    /// shared file (§13.18.12.1), so a file whose reports disagree about having one has records that cannot be told
    /// apart. Asked of the reports each FD's REPORT clause names (§13.18.46), over what was WRITTEN.</summary>
    private void ScreenReportCodeAgreement()
    {
        foreach (var file in Files)
        {
            if (file.ReportNames.Count < 2) continue;
            var members = Reports.Where(r => file.ReportNames.Any(n => CobolNames.Same(n, r.Name))).ToList();
            if (members.Any(r => r.CodeWritten) && members.Any(r => !r.CodeWritten))
                Edition.Error(DiagnosticCatalog.ReportCodeClauseRule, $"file '{file.CobolName}': the CODE clause is specified for "
                    + $"{string.Join(", ", members.Where(r => r.CodeWritten).Select(r => $"'{r.Name}'"))} but not for "
                    + $"{string.Join(", ", members.Where(r => !r.CodeWritten).Select(r => $"'{r.Name}'"))}: if the CODE clause is "
                    + "specified for any report, it shall be specified for each report associated with the same report file "
                    + "(ISO §13.18.12.3 SR3)");
        }
    }

    /// <summary>Post-build resolution for every report (the <c>ResolveFiles</c> pattern — runs after the storage
    /// forest is complete): the owning FILE (§13.18.46), SOURCE / CONTROL / SUM-addend data items, CH/CF control
    /// levels (§13.18.57.3 SR10/SR11), RESET levels, and the report's line width.</summary>
    internal void ResolveReports()
    {
        ScreenReportClauseNames();
        ScreenReportCodeAgreement();
        foreach (var model in Reports)
        {
            // The owning file: the FD whose REPORT(S) clause names this report (§13.14.3 SR1 — one and only one such
            // clause; a name in none is reported here, a name in several by ScreenReportClauseNames). A report may
            // share its file with others (§13.18.46.4 GR1 — each has its own engine over the one connector).
            model.File = Files.FirstOrDefault(f =>
                f.ReportNames.Any(rn => CobolNames.Same(rn, model.Name)));
            if (model.File is null)
                Edition.Error(DiagnosticCatalog.ReportClauseNameRule, $"RD '{model.Name}' is not named in any file description "
                    + "entry's REPORT clause: there shall be one and only one REPORT clause specifying report-name-1 "
                    + "(ISO §13.14.3 SR1)");

            foreach (var ctl in model.Controls)
                if (!ctl.IsFinal && ctl.Operand is { } op)
                {
                    ctl.Item = LookupQualified(op.Name, op.Qualifiers, $"RD '{model.Name}': CONTROL operand", out bool ctlAmbiguous);
                    if (ctl.Item is null && !ctlAmbiguous)
                    {
                        // §13.18.16.3 SR2 — "Data-name-1 shall not be defined in the report section." The report
                        // section's own names never enter ByName, so such an operand FAILS RESOLUTION: the right
                        // verdict under the wrong rule, and it would change meaning the day those names join
                        // ByName. This arm gives SR2 its own rejection, asked only AFTER resolution failed —
                        // a name declared in ordinary storage TOO resolves there (§8.4.2.1) and is not an SR2
                        // reference, which is the §13.15.3 SR16 scan's own reasoning.
                        if (IsReportSectionOnlyName(op.Name))
                            Edition.Error(DiagnosticCatalog.ReportControlOperandShape, $"RD '{model.Name}': CONTROL operand '{op}' names a "
                                + "report section data item: data-name-1 shall not be defined in the report "
                                + "section (ISO §13.18.16.3 SR2)");
                        else
                            Edition.Error(DiagnosticCatalog.ReportControlOperandUnresolved, $"RD '{model.Name}': CONTROL operand '{op}' does not "
                                + "resolve to a data item (ISO §8.4.2.1)");
                    }
                    else if (ctl.Item is { } ctlItem && ControlOperandShapeViolation(ctlItem) is { } shape)
                        Edition.Error(shape.Code, $"RD '{model.Name}': CONTROL operand '{op}' {shape.Clause}");
                    // §8.4.3.3.3 SR1 governs a ref-modded operand here exactly as in the procedure division, and
                    // it is read through the ONE exclusion test so the two paths cannot drift (kb/Work PB205).
                    else if (op.RefModStart is not null && ctl.Item is not null
                             && ReferenceResolver.RefModExclusion(ctl.Item) is { } why)
                        Edition.Error(DiagnosticCatalog.RefModIdentifierNotPermitted, $"RD '{model.Name}': CONTROL operand '{op}': reference "
                            + $"modification of {why} is not permitted (ISO §8.4.3.3.3 SR1)");
                    // The literal BOUNDS are screened by the same one screen the procedure division uses (kb/Work
                    // PB1707 part 1): a CONTROL operand's positions are integer literals by SR4 and no statement
                    // exists here for an EC-BOUND-REF-MOD checking directive to govern, so a violation is the
                    // error. A zero length is not judged (REF-MOD-ZERO-LENGTH is a per-line directive this data
                    // clause cannot be folded against); the run-time test still owns it.
                    else if (op.RefModStart is { } ctlStart && ctl.Item is { } ctlRmItem
                             && ReferenceResolver.LiteralRefModRangeViolation(ctlRmItem, ctlStart, op.RefModLength,
                                 omittedLength: op.RefModLength is null, allowZeroLength: true) is { } outOfRange)
                        Edition.Error(DiagnosticCatalog.RefModLiteralOutOfRange,
                            $"RD '{model.Name}': CONTROL operand '{op}': {outOfRange}");
                }

            ResolveReportCode(model);   // §13.18.12.3 SR2 / GR2 — the CODE clause's identifier and length

            var seenOccurs = new HashSet<ReportOccursSpec>(ReferenceEqualityComparer.Instance);
            foreach (var group in model.Groups)
            {
                // CH/CF control level (§13.18.57.3 SR10/SR11): match the operand against the CONTROL hierarchy;
                // an omitted operand selects the sole control.
                if (group.Kind is ReportGroupKindModel.ControlHeading or ReportGroupKindModel.ControlFooting)
                {
                    group.ControlLevel = group.ControlFinal
                        ? model.Controls.FindIndex(c => c.IsFinal)
                        : group.ControlOperand is { } gcn
                            ? ControlLevelOf(model, gcn)
                            : model.Controls.Count == 1 ? 0 : -1;
                    if (group.ControlLevel < 0)
                        Edition.Error(DiagnosticCatalog.ReportControlTypeOperand, $"RD '{model.Name}': the TYPE C{(group.Kind == ReportGroupKindModel.ControlHeading ? "H" : "F")} operand "
                            + $"{(group.ControlOperand is { } d ? $"'{d}' " : "")}shall be the same as one of the "
                            + "operands of the CONTROL clause (ISO §13.18.57.3 SR10/SR11)");
                }
                foreach (var ln in group.Lines)
                    foreach (var f in ln.Fields)
                    {
                        foreach (var fs in f.Sources)   // EVERY operand of a multi-operand SOURCE clause (§13.18.53.2)
                            switch (fs)
                            {
                                // §13.18.53.3 SR4 — "If identifier-1 specifies a report section item, it shall be a report
                                // counter identifier or a sum counter defined in the current report." identifier-1 is
                                // screened by the SAME walk the expression arm is (below), and resolved by the same
                                // host binder in the procedure phase (kb/Work PB1292).
                                case FieldReferenceSource rs when ReportSectionNameIn(rs.Ref, model) is { } badRef:
                                    Edition.Error(DiagnosticCatalog.ReportExpressionOperandSection, $"RD '{model.Name}': SOURCE "
                                        + $"'{rs.Written}' names the report section item '{badRef}', which is neither a "
                                        + "report counter nor a sum counter of this report. If identifier-1 specifies a "
                                        + "report section item, it shall be a report counter identifier or a sum counter "
                                        + "defined in the current report (ISO §13.18.53.3 SR4).");
                                    rs.Rejected = true;
                                    break;
                                // §13.18.53.3 SR4, last sentence — "This same Syntax rule applies to any
                                // identifier appearing in arithmetic-expression-1": a report-section identifier
                                // inside the expression shall be a report counter or a sum counter OF THIS
                                // REPORT. The identifiers themselves are resolved by the ONE expression binder
                                // in the procedure phase (kb/Work PB852).
                                case FieldComputeSource cs when ReportSectionNameIn(cs.Ctx, model) is { } bad:
                                    Edition.Error(DiagnosticCatalog.ReportExpressionOperandSection, $"RD '{model.Name}': SOURCE "
                                        + $"'{cs.Written}' contains '{bad}', which names a report section item that "
                                        + "is neither a report counter nor a sum counter of this report. An "
                                        + "identifier of a SOURCE clause — including any identifier inside "
                                        + "arithmetic-expression-1 — may name a report section item only in those "
                                        + "two shapes (ISO §13.18.53.3 SR4).");
                                    cs.Rejected = true;
                                    break;
                            }
                        // OCCURS … DEPENDING ON data-name-1 (§13.18.38 Format 3): resolved on the SOURCE operand's
                        // pattern, ONCE per repeating entry (every repetition's guard shares its one spec).
                        foreach (var g in f.RepetitionGuards)
                            ResolveReportOccursDepending(g.Spec, model, seenOccurs);
                    }
            }

            // §13.18.35.3 SR6 — the line set of each group, asked once every group's control level is resolved (the
            // §13.18.57.4 GR7 d) limits of a body group read the OR PAGE control headings by level).
            ScreenReportGroupCensus(model);   // §13.18.57.3 SR13–SR15, over the resolved control levels
            ScreenReportGroupLines(model);

            foreach (var sum in model.Sums)
            {
                foreach (var term in sum.Terms)
                {
                    foreach (var addend in term.Addends) ResolveSumAddend(addend, term, sum, model);
                    foreach (var det in term.Upon) ResolveUponDetail(det, model);
                }
                // A SUM entry with no printable item still carries its occurrence's DEPENDING tests (§13.18.54.4
                // GR10 — the reset of an absent occurrence is suppressed), so its specs resolve here too; the
                // seen-set keeps it once per repeating entry.
                foreach (var g in sum.RepetitionGuards)
                    ResolveReportOccursDepending(g.Spec, model, seenOccurs);
                if (sum.ResetFinal)
                    sum.ResetLevel = model.Controls.FindIndex(c => c.IsFinal);
                else if (sum.ResetOperand is { } rn)
                    sum.ResetLevel = ControlLevelOf(model, rn);
                if ((sum.ResetFinal || sum.ResetOperand is not null) && sum.ResetLevel < 0)
                    Edition.Error(DiagnosticCatalog.ReportResetNotControlOperand, $"RD '{model.Name}': RESET ON '{sum.ResetOperand?.ToString() ?? "FINAL"}' is "
                        + "not an operand of the CONTROL clause (ISO §13.18.54.3 SR8)");
            }

            // PRESENT WHEN SR16 (§13.15.3): condition-1 shall not reference a sum counter, LINE-COUNTER,
            // PAGE-COUNTER, or another report section data item. Scanned over each DISTINCT captured condition
            // (an entry's condition appears in every subordinate chain) against this RD's report-section names.
            CheckConditionOperands(model);

            // §8.4.2.3.3 SR8 — no sum counter, LINE-COUNTER or PAGE-COUNTER as a report section subscript, asked once
            // every report is described (a sum counter may belong to a later report description).
            ScreenReportSubscripts(model);

            // VARYING SR2 (§13.18.64.3): data-name-1 shall not be defined elsewhere in the source element — the arm over
            // what ELSE defines the name (the two over other VARYING clauses are ScreenReportVaryingClauses'). A data
            // item or constant is in ByName; a report group entry, printable item or sum counter is a report section
            // name of some report of the source element, all of which are described by now.
            foreach (var name in model.VaryingNames)
                if (ByName.ContainsKey(name) || IsReportSectionOnlyName(name))
                    Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}' VARYING '{name}': the counter "
                        + "data-name shall not be defined elsewhere in the source element (ISO §13.18.64.3 SR2)");

            // Line width: the FD's fixed RECORD CONTAINS, else the widest NOMINAL field extent — absolute
            // operands at column + width − 1; relative (PLUS) operands walked against the line's horizontal
            // counter with every item present (§13.18.14.4 GR7–GR9; presentation-time absence only SHRINKS the
            // occupied extent, so the all-present walk is the width bound).
            int widest = 1;
            var arrangementReported = new HashSet<(Core.ReportGroupEntryContext?, Core.ReportGroupEntryContext?, int)>();
            foreach (var g in model.Groups)
                foreach (var ln in g.Lines)
                {
                    foreach (var p in NominalPlacements(ln))
                    {
                        // GR6 c)/d): integer-1 and the printable-size fix the leftmost column, and a line has no
                        // column before 1 — the standard states no outcome for it, so it is refused here (COBOLNET2712).
                        if (p.Left < 1 && p.Spec.Kind == ReportColumnKindModel.Absolute && p.Spec.Alignment != ReportColumnAlignment.Left)
                            Edition.Error(DiagnosticCatalog.ReportColumnLeftOfLine, $"RD '{model.Name}' entry "
                                + $"'{p.Field.PrintItem.CobolName ?? "FILLER"}': COLUMN {p.Spec.Alignment.ToString().ToUpperInvariant()} "
                                + $"{p.Spec.Value} with a printable-size of {p.Size} puts the item's leftmost column at {p.Left}, "
                                + "before column 1 (ISO §13.18.14.4 GR6 c)/d))");
                        widest = Math.Max(widest, p.Right);
                    }
                    ScreenReportColumnArrangement(model, ln, arrangementReported);
                }
            // §13.18.12.4 GR2 — the CODE characters "are not included in the descriptions of the lines in the report,
            // but are included in the logical record size": the record is the code PLUS the line. The record's size is
            // the maximum the RECORD clause establishes (§14.9.30.4 GR6) in ANY of its three formats (§13.18.43.4 GR6
            // integer-1, GR7 integer-3, GR18 integer-5; kb/Work PB2519), the one reading FileModel.RecordClause gives
            // every consumer.
            // A column past it is cut from the record (the Annex A.1 159) latitude, docs/CONFORMANCE.md DOC-A.1-159) —
            // the occupancy GR4 reads is NOT cut with it (ReportLineImage), kb/Work PB1934.
            model.LineWidth = model.File?.RecordClause?.Upper is { } record
                ? Math.Max(1, record - (model.Code?.Length ?? 0)) : widest;
        }
        // SR4 e) is asked of the references of EVERY report at once: a chain may leave one report description and
        // come back (SR4 g) lets data-name-1 name an entry of a different one).
        ScreenRolledChains();
    }

    /// <summary>One PLACEMENT of a printable item on a report line, in the line's NOMINAL walk: the field, which of
    /// its COLUMN operands it is, the operand, its printable-size and the leftmost column the walk puts it in.</summary>
    private readonly record struct NominalPlacement(
        ReportFieldModel Field, int Operand, ReportColumnSpec Spec, int Size, int Left)
    {
        /// <summary>The rightmost column — the value that becomes the horizontal counter (§13.18.14.4 GR9).</summary>
        public int Right => Left + Size - 1;
    }

    /// <summary>⛔ THE ONE NOMINAL HORIZONTAL WALK OF A REPORT LINE (ISO §13.18.14.4 GR6–GR9), read by the line-width
    /// computation and by <see cref="ScreenReportColumnArrangement"/>, so the two cannot place an item in different
    /// columns: absolute operands at the leftmost column <see cref="ReportColumnSpec.AbsoluteLeftmost"/> gives,
    /// relative (PLUS) operands against the line's horizontal counter, a step anchor's later repetitions at the
    /// anchor plus their displacement (§13.18.38.4 GR12). Every field of <paramref name="ln"/> is present unless
    /// <paramref name="included"/> says otherwise — presentation-time absence only shrinks what is occupied, so the
    /// all-present walk is the bound, and the walk over the unconditional fields alone is the floor.</summary>
    private static List<NominalPlacement> NominalPlacements(ReportLineModel ln, Func<ReportFieldModel, bool>? included = null)
    {
        var placements = new List<NominalPlacement>();
        int hc = 0;
        var anchors = new Dictionary<int, int>();
        foreach (var f in ln.Fields)
        {
            if (included is not null && !included(f)) continue;
            int size = f.PrintItem.DisplayTextWidth;   // the printable-size (§13.18.14.4 GR3)
            for (int op = 0; op < f.Columns.Count; op++)
            {
                var spec = f.Columns[op];
                int left = spec.Kind switch
                {
                    ReportColumnKindModel.Absolute => spec.AbsoluteLeftmost(size),   // GR6 b)-d), the ONE computation
                    ReportColumnKindModel.Relative => hc + spec.Value,
                    ReportColumnKindModel.AnchorSeed => anchors[spec.AnchorId] = hc + spec.Value,
                    _ => anchors.GetValueOrDefault(spec.AnchorId) + spec.Value,
                };
                var placement = new NominalPlacement(f, op, spec, size, left);
                placements.Add(placement);
                hc = placement.Right;   // GR9 — the rightmost column becomes the counter
            }
        }
        return placements;
    }

    /// <summary>The PRESENT WHEN clauses a printable item is subject to BELOW its report line — the line's own chain
    /// gates every item on it alike and so tells two of them apart never (§13.18.41.4 GR2b). A GROUP INDICATE clause
    /// "has the same effect as a PRESENT WHEN clause" (§13.18.28.4 GR1): its entry stands as that clause.</summary>
    private static List<object> PresentWhenClausesOf(ReportFieldModel f)
    {
        var clauses = new List<object>(f.PresentWhenCtxs);
        if (f.GroupIndicate) clauses.Add((object?)f.Entry ?? f);
        return clauses;
    }

    /// <summary>"Each subject to a different PRESENT WHEN clause" (§13.18.14.3 SR7, SR8 a) and §13.18.35.3 SR6 a)–e)):
    /// each of the two carries a clause the other does not. Two items under the very same clause — or one whose
    /// clauses the other also has — are present together, which is what the rule exists to refuse.</summary>
    private static bool DifferentPresentWhen(IReadOnlyList<object> a, IReadOnlyList<object> b) =>
        a.Any(x => !b.Contains(x)) && b.Any(x => !a.Contains(x));

    /// <summary>"Unless each of them is subject to a different PRESENT WHEN clause" (§13.18.14.3 SR8 c),
    /// §13.18.35.3 SR6 d)): every item carries a clause and every pair differs (<see cref="DifferentPresentWhen"/>).
    /// An item subject to no clause is never excused, so one such item makes the answer no.</summary>
    private static bool EachDifferentPresentWhen(IReadOnlyList<IReadOnlyList<object>> clauses)
    {
        for (int i = 0; i < clauses.Count; i++)
        {
            if (clauses[i].Count == 0) return false;
            for (int j = 0; j < i; j++)
                if (!DifferentPresentWhen(clauses[i], clauses[j])) return false;
        }
        return true;
    }

    /// <summary>⛔ THE PRINTABLE ITEMS OF ONE REPORT LINE, AS A SET (ISO §13.18.14.3 SR7, SR8; kb/Work PB1222), asked
    /// of each line after the whole source element is described, because the printable-size is a picture's. The
    /// walk is the NOMINAL one (<see cref="NominalPlacements"/>), so a rule is judged on what the entries as
    /// written place, never on one presentation's presence pattern:
    /// <list type="bullet">
    /// <item>SR7 — "Within a given report line, any two or more absolute items defined using column numbers that are
    /// not in increasing numerical order shall be subject to a different PRESENT WHEN clause."</item>
    /// <item>SR8 a) — "If any two or more items overlap each other, they shall each be subject to a different
    /// PRESENT WHEN clause." Two placements of ONE entry are never compared (the repetitions of an OCCURS entry belong to
    /// the STEP rule of §13.18.38.3 SR26, the out-of-order operands of a multiple COLUMN clause to the SR10 b) screen of
    /// §13.18.14.3); the operands of one multiple COLUMN clause that overlap are, having one clause between them.</item>
    /// <item>SR8 b) — "The rightmost column positions of all absolute items shall not exceed the page width."</item>
    /// <item>SR8 c) — "If the report line ends in a set of relative printable items or consists only of such, they shall
    /// not cause the page width to be exceeded unless each of them is subject to a different PRESENT WHEN clause, in
    /// which case this rule applies only to the largest of them." The tail is the placements after the last absolute
    /// one, judged whole (every item present) unless <see cref="EachDifferentPresentWhen"/>, and then by the one that
    /// ends furthest right when it alone of the tail is present. Items under different clauses may all be present at
    /// run time, which is what the run-time EC-REPORT-PAGE-WIDTH (§13.18.14.4 GR5) and EC-REPORT-COLUMN-OVERLAP (GR4)
    /// are left to report.</item>
    /// </list>
    /// Reported once per pair of WRITTEN entries (<paramref name="reported"/>), however many lines and repetitions
    /// the entries are bound into.</summary>
    private void ScreenReportColumnArrangement(ReportModel model, ReportLineModel ln,
        HashSet<(Core.ReportGroupEntryContext?, Core.ReportGroupEntryContext?, int)> reported)
    {
        var all = NominalPlacements(ln);
        if (all.Count == 0) return;
        const int Sr7 = 7, Sr8a = 81, Sr8b = 82, Sr8c = 83;
        string EntryName(ReportFieldModel f) => f.PrintItem.CobolName ?? "FILLER";
        bool Report(NominalPlacement a, NominalPlacement? b, int rule, string message)
        {
            if (!reported.Add((a.Field.Entry, b?.Field.Entry, rule))) return false;
            using var at = Edition.At(a.Field.Entry);
            Edition.Error(DiagnosticCatalog.ReportColumnClauseRule, $"RD '{model.Name}': {message}");
            return true;
        }

        for (int j = 0; j < all.Count; j++)
        {
            var pj = all[j];
            // SR8 b)
            if (pj.Spec.Kind == ReportColumnKindModel.Absolute && pj.Right > model.PageWidth)
                Report(pj, null, Sr8b, $"the absolute item '{EntryName(pj.Field)}' at COLUMN {pj.Spec.Value} ends in column "
                    + $"{pj.Right}, past the page width {model.PageWidth}"
                    + (model.PageWidthWritten ? "" : " (assumed 999, §13.18.39.4 GR5)")
                    + "; the rightmost column positions of all absolute items shall not exceed the page width "
                    + "(ISO §13.18.14.3 SR8 b))");
            for (int i = 0; i < j; i++)
            {
                var pi = all[i];
                bool sameEntry = pi.Field.Entry is not null && ReferenceEquals(pi.Field.Entry, pj.Field.Entry);
                if (sameEntry && !ReferenceEquals(pi.Field, pj.Field)) continue;   // two repetitions of one entry
                // Two operands of one multiple COLUMN clause that are not in increasing order are SR10 b)'s, said once
                // by ScreenReportColumnClauses; only increasing operands that overlap are left for SR8 a).
                if (sameEntry && pi.Spec.Kind == ReportColumnKindModel.Absolute && pj.Spec.Kind == ReportColumnKindModel.Absolute
                    && pj.Spec.Value <= pi.Spec.Value) continue;
                var clausesI = PresentWhenClausesOf(pi.Field);
                var clausesJ = PresentWhenClausesOf(pj.Field);
                if (DifferentPresentWhen(clausesI, clausesJ)) continue;
                // SR7 — absolute items by their written column numbers, in the order they are written.
                if (!sameEntry && pi.Spec.Kind == ReportColumnKindModel.Absolute && pj.Spec.Kind == ReportColumnKindModel.Absolute
                    && pj.Spec.Value <= pi.Spec.Value)
                {
                    Report(pj, pi, Sr7, $"the absolute items '{EntryName(pi.Field)}' (COLUMN {pi.Spec.Value}) and "
                        + $"'{EntryName(pj.Field)}' (COLUMN {pj.Spec.Value}) are not in increasing numerical order and are not "
                        + "each subject to a different PRESENT WHEN clause (ISO §13.18.14.3 SR7)");
                    continue;
                }
                // SR8 a)
                if (pj.Left <= pi.Right && pi.Left <= pj.Right)
                    Report(pj, pi, Sr8a, $"the items '{EntryName(pi.Field)}' (columns {pi.Left}-{pi.Right}) and "
                        + $"'{EntryName(pj.Field)}' (columns {pj.Left}-{pj.Right}) overlap and are not each subject to a "
                        + "different PRESENT WHEN clause (ISO §13.18.14.3 SR8 a))");
            }
        }

        // SR8 c) — the relative tail: the placements after the last absolute one, judged as a whole unless each carries
        // a different PRESENT WHEN clause, and then by the largest alone: the item that, with every item before the
        // tail present and no other tail item, ends furthest right.
        int lastAbsolute = all.FindLastIndex(p => p.Spec.Kind == ReportColumnKindModel.Absolute);
        if (lastAbsolute == all.Count - 1) return;
        var tail = all.Skip(lastAbsolute + 1).ToList();
        int prefixEnd = lastAbsolute >= 0 ? all[lastAbsolute].Right : 0;
        int extent = EachDifferentPresentWhen(tail.Select(p => (IReadOnlyList<object>)PresentWhenClausesOf(p.Field)).ToList())
            ? tail.Max(p => p.Spec.Kind is ReportColumnKindModel.Relative or ReportColumnKindModel.AnchorSeed
                ? prefixEnd + p.Spec.Value + p.Size - 1 : p.Right)
            : tail.Max(p => p.Right);
        if (extent > model.PageWidth)
        {
            var widest = tail.OrderByDescending(p => p.Right).First();
            Report(widest, null, Sr8c, $"the report line ends in relative items, and they reach column {extent}, past the page "
                + $"width {model.PageWidth}" + (model.PageWidthWritten ? "" : " (assumed 999, §13.18.39.4 GR5)")
                + " although they are not each subject to a different PRESENT WHEN clause, or the largest of those that "
                + "are is itself too wide; relative printable items at the end of a report line shall not cause the "
                + "page width to be exceeded (ISO §13.18.14.3 SR8 c))");
        }
    }

    /// <summary>⛔ THE ONE ARM CHOICE FOR A SUM ADDEND (kb/Work PB482). ISO §13.18.54.3 SR1 admits three addend
    /// forms and the arms differ by WHERE the operand is defined, so the choice is made once, here, after the
    /// whole storage forest and every report description exist:
    /// <list type="number">
    /// <item>SR4's <c>data-name-1</c> — "the name of a numeric data item IN THE REPORT SECTION" (a rolled total,
    /// §13.18.54.4 GR6), resolved by <see cref="ResolveRolledAddend"/>.</item>
    /// <item>SR4 g)'s cross-report form — the operand qualified by a REPORT-name: the same arm, the report-name being
    /// the outermost qualifier of the entry (§8.4.2.2.2 Format 1).</item>
    /// <item>SR5's <c>identifier-1</c> — "it shall specify a numeric data item NOT defined in the report
    /// section". Both halves of that sentence are screened: resolution against ordinary storage (report-section
    /// names never enter <see cref="DataBinder.ByName"/>) and the CATEGORY, which nothing checked before —
    /// <c>SUM WS-TXT</c> over a <c>PIC X(6)</c> holding "123456" totalled 123456 per GENERATE, silently.</item>
    /// </list>
    /// The SUBSCRIPT is deliberately absent from this method: the arm choice is about the base name, and the
    /// subscript is evaluated by <c>ExpressionBinder</c> in the procedure phase off <see cref="ReportSumAddend.Ctx"/>.</summary>
    private void ResolveSumAddend(ReportSumAddend addend, ReportSumTerm term, ReportSumModel sum, ReportModel model)
    {
        if (addend.Rejected) return;
        addend.Rejected = true;   // cleared only by the one success path at the end
        // ⛔ AN ARITHMETIC-EXPRESSION ADDEND TAKES SR6, NOT SR4/SR5 (ISO §13.18.54.3 SR6 — "If the addend is
        // arithmetic-expression-1, any identifiers it contains may reference entries in any section of the data
        // division other than the report section"; kb/Work PB883). There is no base name to look up: the whole
        // tree binds through the ONE expression binder in the procedure phase, and the only DATA-phase question
        // is SR6's — does any identifier in it name a report-section item?
        if (addend.IsExpression)
        {
            if (ReportSectionNameIn(addend.Ctx) is { } rsName)
            {
                Edition.Error(DiagnosticCatalog.ReportExpressionOperandSection, $"RD '{model.Name}': SUM addend "
                    + $"'{addend.Written}' contains '{rsName}', which names a report section item. If the addend "
                    + "is arithmetic-expression-1, any identifiers it contains may reference entries in any "
                    + "section of the data division OTHER THAN the report section (ISO §13.18.54.3 SR6).");
                return;
            }
            addend.Rejected = false;
            return;
        }
        // §8.4.3.14.3 SR1 — a bare LINAGE-COUNTER addend names no data item, so the lookup below would answer "does
        // not resolve" under SR5, a sentence about a different rule (kb/Work PB1431). The expression forms bind through
        // ExpressionBinder.LinageFileOf, which asks the same rule by binding context.
        if (addend.Reference?.LINAGE_COUNTER() is not null)
        {
            ReportLinageCounterOutsideProcedure(Edition, addend.Ctx, $"RD '{model.Name}': SUM addend", addend.Written);
            return;
        }
        // SR4's data-name-1 — a report-section item: a name ALSO declared in ordinary storage resolves THERE
        // (§8.4.2.1) and is an SR5 identifier-1 (`IsRolledAddendName`).
        // SR4 g) — "If data-name-1 specifies an entry in a different report description". The qualifier that
        // says so is a REPORT-name (§8.4.2.2.2 Format 1), and `dataReference` swallows it as an ordinary IN/OF
        // qualification, so the spelling is recognised HERE rather than at `sumOperand`'s own `OF reportName`
        // alternative, which the qualification tail makes unreachable — it is the same arm, the report-name being
        // the outermost qualifier of the entry.
        if (IsRolledAddendName(addend))
        {
            ResolveRolledAddend(addend, term, sum, model);
            return;
        }
        if (LookupQualified(addend.Name, addend.Qualifiers, $"RD '{model.Name}': SUM addend", out bool sumAmbiguous) is not { } item)
        {
            if (sumAmbiguous) return;
            Edition.Error(DiagnosticCatalog.ReportSumAddendUnresolved, $"RD '{model.Name}': SUM addend '{addend.Written}' does not resolve "
                + "to a data item outside the report section (ISO §13.18.54.3 SR5)");
            return;
        }
        // SR5's category half. A group item and every non-numeric category fail it — the addend is added into
        // the counter by §13.18.54.4 GR3's ADD, which has no meaning for a non-numeric sending operand.
        // A USAGE INDEX item is class INDEX, never numeric (§8.5.2.1 Table 2), but its storage PicInfo carries category
        // Numeric — the R27/PB640 trap the INSPECT tally counter (PB1127) had (kb/Work PB1830).
        if (item.Pic is not { Category: PicCategory.Numeric, Usage: not Usage.Index })
        {
            Edition.Error(DiagnosticCatalog.ReportSumAddendNotNumeric, $"RD '{model.Name}': SUM addend '{addend.Written}' is "
                + $"{(item.IsGroup ? "a group item" : $"of category {item.Pic?.Category.ToString().ToLowerInvariant() ?? "unknown"}")}; "
                + "the addend shall specify a numeric data item (ISO §13.18.54.3 SR5) — its content is added "
                + "into the sum counter by an implicit ADD (§13.18.54.4 GR3).");
            return;
        }
        addend.Item = item;
        addend.Rejected = false;
    }

    /// <summary>Is this addend SR4's <c>data-name-1</c> (a report section item) rather than SR5's identifier-1? A name
    /// declared in the report section AND NOWHERE ELSE is (<see cref="IsReportSectionOnlyName"/> — the set §13.18.16.3
    /// SR2 and §13.15.3 SR16 use: a name ALSO declared in ordinary storage resolves THERE, §8.4.2.1); so is one a
    /// REPORT-name qualifier points at an entry of (SR4 g) — the report-name says which report description's entry
    /// is meant, whatever else the word names).</summary>
    private bool IsRolledAddendName(ReportSumAddend addend) =>
        IsReportSectionOnlyName(addend.Name)
        || (addend.Qualifiers.Any(q => Reports.Any(r => CobolNames.Same(r.Name, q)))
            && Reports.Any(r => r.EntryFamilies.Any(f => CobolNames.Same(f.Name, addend.Name))));

    /// <summary>Per WRITTEN addend (its parse node), the entry its data-name-1 names — null when it was refused. A
    /// repeating SUM entry binds its clause once per occurrence of the counter, and each occurrence meets the same
    /// written addend, so the rules over the WRITTEN clause are asked and reported once.</summary>
    private readonly Dictionary<Core.ReportValueOperandContext, ReportEntryFamily?> _rolledAddendEntries =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>⛔ SR4's DATA-NAME-1 — A ROLLED TOTAL (ISO §13.18.54.3 SR4; kb/Work PB1294). The addend names an entry
    /// of the report section whose VALUE §13.18.54.4 GR6 adds: the sum counter of an entry that contains a SUM clause,
    /// else "the operand of the SOURCE or VALUE clause". Resolved once per written addend against every entry of every
    /// report description of the source element (a report-name qualifier picks the description, SR4 g)), then screened
    /// by the rules that govern it —
    /// <list type="bullet">
    /// <item>SR4: a NUMERIC data item with a value, "specified without the subscripting normally required";</item>
    /// <item>SR4 a): no UPON phrase in the SUM clause;</item>
    /// <item>SR4 b)/c)/d): the repetition levels — see <see cref="RolledRepetitionFault"/>;</item>
    /// <item>SR4 f): the combination of report group TYPEs, within one report description (g lifts it across two);</item>
    /// <item>SR4 e): the chain of references terminates — <see cref="ScreenRolledChains"/>, once every report is resolved.</item>
    /// </list>
    /// and finally mapped per COUNTER OCCURRENCE (§13.18.54.4 GR8): this addend object belongs to ONE occurrence of the
    /// counter, and takes every occurrence of data-name-1 that corresponds to it. The additions themselves are
    /// registered with the engine whose group contains data-name-1 (GR7 a)/b)), which is why the addend carries
    /// OCCURRENCES and not a value.</summary>
    private void ResolveRolledAddend(ReportSumAddend addend, ReportSumTerm term, ReportSumModel sum, ReportModel model)
    {
        var target = sum.Family;
        if (!_rolledAddendEntries.TryGetValue(addend.Ctx, out var data))
        {
            using var at = Edition.At(addend.Ctx);
            _rolledAddendEntries[addend.Ctx] = data = ScreenRolledAddend(addend, term, target, model);
        }
        if (data is null) return;   // refused, and said so once
        var targetCoordinates = target.CoordinatesOf(sum.Id - target.BaseId);
        int levels = data.Extents.Count, targetLevels = targetCoordinates.Length;
        foreach (var occurrence in data.Occurrences)
        {
            // GR8 a)/b): the levels correspond "in order beginning with the lowest level of nesting" (SR4 d) — the
            // INNERMOST levels of the two tables are the same levels — and the addend's remaining, outer levels are
            // the ones "the total of a complete table of occurrences" is formed over. A non-repeating addend (c)
            // is the one addend of every occurrence of the counter.
            bool corresponds = true;
            for (int k = 0; k < targetLevels && levels > 0; k++)
                corresponds &= occurrence.Coordinates[levels - 1 - k] == targetCoordinates[targetLevels - 1 - k];
            if (corresponds) addend.RolledFrom.Add(occurrence);
        }
        addend.Rolled = true;
        addend.Rejected = false;
    }

    /// <summary>The rules over a WRITTEN rolled addend (see <see cref="ResolveRolledAddend"/>) — null after reporting
    /// the first that fails, else the entry data-name-1 names. First-failure, like every other screen of this
    /// clause: one wrong word is one diagnostic.</summary>
    private ReportEntryFamily? ScreenRolledAddend(ReportSumAddend addend, ReportSumTerm term, ReportSumFamily target, ReportModel model)
    {
        string where = $"RD '{model.Name}': SUM addend '{addend.Written}'";
        // The entry the word names — the qualifiers are consumed against each entry's own hierarchy, the REPORT-name
        // last (§8.4.2.2.3 SR4, §8.4.2.2.2 Format 1), exactly as a sum counter's reference is (kb/Work PB1454).
        var candidates = Reports.SelectMany(r => r.EntryFamilies)
            .Where(f => CobolNames.Same(f.Name, addend.Name)
                && QualifierWalk(f.Qualification, addend.Qualifiers,
                    q => CobolNames.Same(q, f.Report.Name)))
            .ToList();
        if (candidates.Count > 1)
        {
            Edition.Error(DiagnosticCatalog.ReportSumDataNameRule, $"{where} is ambiguous — {candidates.Count} entries of the report "
                + "section carry that name. A reference shall uniquely identify one resource (ISO §8.4.2.2.1); qualify it "
                + "by report group entry or report-name (§8.4.2.2.3 SR4, §8.4.2.2.2 Format 1).");
            return null;
        }
        if (candidates.Count == 0)
        {
            // (A report GROUP, an entry with no SUM, SOURCE or VALUE clause and a name no entry carries are one answer
            // here: none is an elementary entry with a value, and telling them apart would search the groups by name
            // outside the one funnel, `ReportGroupResolution` — kb/Work PB365.)
            Edition.Error(DiagnosticCatalog.ReportSumDataNameRule, $"{where} names no entry of the report section that carries "
                + "a value (a report group is not an elementary item, and an entry with no SUM, SOURCE or VALUE clause has "
                + "nothing to add): data-name-1 shall be the name of a numeric data item in the report section (ISO §13.18.54.3 SR4), and "
                + "§13.18.54.4 GR6 adds the sum counter of an entry containing a SUM clause or the operand of its SOURCE or "
                + "VALUE clause — an entry with none of them has no value to add.");
            return null;
        }
        var data = candidates[0];
        // SR4 — "a numeric data item". A SUM entry's counter is one by definition (§13.18.54.4 GR1: "a conceptual data
        // item that behaves as a data item of the category numeric"); a SOURCE / VALUE entry's PICTURE says.
        if (data is ReportSourceFamily { Category: not PicCategory.Numeric } source)
        {
            Edition.Error(DiagnosticCatalog.ReportSumDataNameRule, $"{where} names the entry '{data.Name}', whose PICTURE is "
                + $"{(source.Category is { } cat ? $"of category {cat.ToString().ToLowerInvariant()}" : "absent")}: "
                + "data-name-1 shall be the name of a numeric data item in the report section (ISO §13.18.54.3 SR4).");
            return null;
        }
        // SR4 — "If it is associated with an OCCURS clause, it shall be specified without the subscripting normally
        // required": the reference names the WHOLE repeating item, and GR8 says how its occurrences are added.
        if (addend.Reference is { } reference && ReferenceResolver.ReadOperandSuffixes(reference).Subscripts > 0)
        {
            Edition.Error(DiagnosticCatalog.ReportSumDataNameRule, $"{where} is subscripted: if data-name-1 is associated with an "
                + "OCCURS clause, it shall be specified without the subscripting normally required (ISO §13.18.54.3 SR4) — "
                + "§13.18.54.4 GR8 adds each occurrence of a repeating addend into the corresponding occurrence of the counter.");
            return null;
        }
        // SR4 a)
        if (term.Upon.Count > 0)
        {
            Edition.Error(DiagnosticCatalog.ReportSumDataNameRule, $"{where} is data-name-1, and the SUM clause also writes an UPON "
                + "phrase: when data-name-1 is specified the UPON phrase shall not be specified (ISO §13.18.54.3 SR4 a)) — a "
                + "rolled total is added when the group that contains it is processed, not on a GENERATE.");
            return null;
        }
        if (RolledRepetitionFault(data, target) is { } fault)
        {
            Edition.Error(DiagnosticCatalog.ReportSumDataNameRule, $"{where} names '{data.Name}': {fault}");
            return null;
        }
        // SR4 f)/g)
        if (ReferenceEquals(data.Report, target.Report) && !ReferenceEquals(data.Group, target.Group)
            && !GroupCombinationPermitted(target.Group, data.Group))
        {
            Edition.Error(DiagnosticCatalog.ReportSumDataNameGroups, $"{where} names the entry '{data.Name}' of a "
                + $"{ReportGroupTypeWords(data.Group.Kind)} report group, and the SUM entry is in a "
                + $"{ReportGroupTypeWords(target.Group.Kind)} report group: that combination of report types is not one SR4 f) permits "
                + "(a control footing: a detail or a control footing of a lower level of control; a detail: a different detail; "
                + "a report footing: any other group but the report heading; a page footing: any body group — ISO §13.18.54.3 SR4 f)).");
            return null;
        }
        return data;
    }

    /// <summary>SR4 b)/c)/d) — what a rolled addend's repetition may be against its counter's, or null when it fits.
    /// "Levels of repetition" are <see cref="ReportEntryFamily.Extents"/> (§13.15.4 GR3's three vehicles, all OCCURS
    /// levels). b) data-name-1 in the SAME report group description as the subject "shall be a repeating item … and
    /// subject to at least one more level of repetition than the subject of the entry"; c) in a DIFFERENT one it
    /// "either shall not reference a repeating item or shall reference a repeating item that is subject to at least
    /// the same number of levels of repetition as the subject of the entry"; d) "The maximum number of repetitions of
    /// data-name-1 and the subject of the entry shall be equal at each corresponding level taken in order beginning
    /// with the lowest level of nesting. Levels superordinate to those corresponding levels may specify any number of
    /// repetitions."</summary>
    private static string? RolledRepetitionFault(ReportEntryFamily data, ReportSumFamily target)
    {
        int d = data.Extents.Count, t = target.Extents.Count;
        if (ReferenceEquals(data.Group, target.Group))
        {
            if (d == 0)
                return "it is in the same report group description as the SUM entry, so it shall be a repeating item "
                    + "(ISO §13.18.54.3 SR4 b))";
            if (d < t + 1)
                return $"it is in the same report group description as the SUM entry, so it shall be subject to at least one more "
                    + $"level of repetition than the subject of the entry ({d} against {t}) (ISO §13.18.54.3 SR4 b))";
        }
        else if (d > 0 && d < t)
            return $"it is in a different report group description and is a repeating item, so it shall be subject to at least "
                + $"the same number of levels of repetition as the subject of the entry ({d} against {t}) (ISO §13.18.54.3 SR4 c))";
        for (int k = 0; k < Math.Min(d, t); k++)
            if (data.Extents[d - 1 - k] != target.Extents[t - 1 - k])
                return $"its maximum number of repetitions at level {k + 1} counted from the innermost "
                    + $"({data.Extents[d - 1 - k]}) differs from the subject's ({target.Extents[t - 1 - k]}): the maximum number of "
                    + "repetitions shall be equal at each corresponding level taken in order beginning with the lowest level of "
                    + "nesting (ISO §13.18.54.3 SR4 d))";
        return null;
    }

    /// <summary>SR4 f) — within ONE report description, the report types of the SUM entry's group (<paramref name="current"/>)
    /// and of the group that defines data-name-1 (<paramref name="defining"/>) that are permitted together.
    /// "Associated with a lower level of control" is a LARGER index in the control hierarchy, which runs major → minor
    /// (§13.18.16.4 GR1); a body group is a detail, a control heading or a control footing (§13.18.57.3 SR15).</summary>
    private static bool GroupCombinationPermitted(ReportGroupModel current, ReportGroupModel defining) => current.Kind switch
    {
        ReportGroupKindModel.ControlFooting => defining.Kind == ReportGroupKindModel.Detail
            || (defining.Kind == ReportGroupKindModel.ControlFooting && defining.ControlLevel > current.ControlLevel),
        ReportGroupKindModel.Detail => defining.Kind == ReportGroupKindModel.Detail,
        ReportGroupKindModel.ReportFooting => defining.Kind != ReportGroupKindModel.ReportHeading,
        ReportGroupKindModel.PageFooting => defining.Kind is ReportGroupKindModel.Detail
            or ReportGroupKindModel.ControlHeading or ReportGroupKindModel.ControlFooting,
        _ => false,
    };

    /// <summary>⛔ SR4 e) — "Any chain of reference shall terminate at an entry that does not contain a SUM clause
    /// referring to a data-name-1 defined in the report section." A SUM entry that rolls up a counter that rolls up
    /// the first is a cycle no addition order resolves (GR6 asks "the additions necessary to compute its value are
    /// completed before the adding of the operand"), so it is refused, once per cycle, naming the entries on it.
    /// Edges are only those of counter → counter: a SOURCE or VALUE entry contains no SUM clause, so it ends a chain.</summary>
    private void ScreenRolledChains()
    {
        var edges = new Dictionary<ReportSumFamily, List<ReportSumFamily>>(ReferenceEqualityComparer.Instance);
        foreach (var r in Reports)
            foreach (var sum in r.Sums)
                foreach (var a in sum.Terms.SelectMany(t => t.Addends).Where(a => a.Rolled))
                    foreach (var o in a.RolledFrom)
                        if (o.Sum is { } s)
                        {
                            if (!edges.TryGetValue(sum.Family, out var outgoing)) edges[sum.Family] = outgoing = [];
                            if (!outgoing.Contains(s.Family)) outgoing.Add(s.Family);
                        }
        var done = new HashSet<ReportSumFamily>(ReferenceEqualityComparer.Instance);
        var path = new List<ReportSumFamily>();
        void Visit(ReportSumFamily f)
        {
            int at = path.IndexOf(f);
            if (at >= 0)
            {
                var cycle = path.Skip(at).Append(f).Select(x => $"'{x.Name ?? "FILLER"}' of report '{x.Report.Name}'");
                Edition.Error(DiagnosticCatalog.ReportSumDataNameRule, "a chain of SUM data-name-1 references never terminates: "
                    + $"{string.Join(" → ", cycle)} — any chain of reference shall terminate at an entry that does not contain a SUM "
                    + "clause referring to a data-name-1 defined in the report section (ISO §13.18.54.3 SR4 e))");
                return;
            }
            if (!done.Add(f)) return;
            path.Add(f);
            if (edges.TryGetValue(f, out var next)) foreach (var n in next) Visit(n);
            path.RemoveAt(path.Count - 1);
        }
        foreach (var f in edges.Keys.ToList()) Visit(f);
    }

    /// <summary>Resolve one <c>UPON data-name-2</c> operand (ISO §13.18.54.3 SR7 — "Data-name-2 shall be the name
    /// of a detail. It may be qualified only by a report-name") through the ONE report-group funnel, which owns
    /// the §8.4.2.2.3 SR1 ambiguity rule as well (kb/Work PB365). The TYPE test is SR7's own sentence: a
    /// control footing or a report heading is not a detail, and §13.18.54.4 GR7 c) 2) can only fire for a detail,
    /// because only a detail is GENERATE-able (§14.9.16.3 SR1).</summary>
    private void ResolveUponDetail(ReportDetailRef det, ReportModel model)
    {
        string where = $"RD '{model.Name}': the UPON operand '{det}'";
        if (ReportGroupResolution.Resolve(Edition, Reports, det.Name, det.Qualifier, where,
                out var owner, out var group) == ReportGroupResolution.Match.None)
        {
            Edition.Error(DiagnosticCatalog.ReportSumUponNotDetail, $"{where} does not name a report group. Data-name-2 shall be "
                + "the name of a detail (ISO §13.18.54.3 SR7).");
            return;
        }
        if (group!.Kind is not ReportGroupKindModel.Detail)
        {
            Edition.Error(DiagnosticCatalog.ReportSumUponNotDetail, $"{where} names a report group of TYPE "
                + $"{ReportGroupTypeWords(group.Kind)}; data-name-2 shall be the name of a DETAIL (ISO "
                + "§13.18.54.3 SR7) — only a detail is the operand of a GENERATE statement (§14.9.16.3 SR1), "
                + "which is the event §13.18.54.4 GR7 c) 2) accumulates on.");
            return;
        }
        // A detail of ANOTHER report (SR7's report-name qualifier): GR7 c) 2) accumulates on a GENERATE of that detail,
        // which runs on THAT report's engine, so the emitter registers the addition with it (`AddGenerateTrigger`) —
        // the owner travels with the operand (kb/Work PB1294).
        det.Detail = group;
        det.Owner = owner;
    }

    /// <summary>A report group's TYPE in the standard's own words, for a diagnostic (ISO §13.18.57.2).</summary>
    private static string ReportGroupTypeWords(ReportGroupKindModel kind) => kind switch
    {
        ReportGroupKindModel.ReportHeading => "REPORT HEADING",
        ReportGroupKindModel.PageHeading => "PAGE HEADING",
        ReportGroupKindModel.ControlHeading => "CONTROL HEADING",
        ReportGroupKindModel.ControlFooting => "CONTROL FOOTING",
        ReportGroupKindModel.PageFooting => "PAGE FOOTING",
        ReportGroupKindModel.ReportFooting => "REPORT FOOTING",
        _ => "DETAIL",
    };

    /// <summary>The §13.15.3 SR16 scan: no PRESENT WHEN condition of <paramref name="model"/> may reference
    /// LINE-COUNTER, PAGE-COUNTER, a sum counter, or another report section data item (group / printable-entry
    /// names). Token-level scan over each PRESENT WHEN condition the report WRITES — read off the written entries
    /// (<see cref="ReportModel.WrittenEntries"/>), the <see cref="ScreenReportEntryClausePresence"/> shape, because
    /// the rule is about the clause as written: a condition on an entry that produces no line, no printable field
    /// and no counter (a body group with no LINE clause, say) is a PRESENT WHEN clause all the same (kb/Work
    /// PB1289). The names it may not reference are those of EVERY report description of the source element
    /// ("other report section data item"), not this RD's alone.</summary>
    private void CheckConditionOperands(ReportModel model)
    {
        var conds = new List<Core.ConditionContext>();
        foreach (var ge in model.WrittenEntries)
            foreach (var clause in ge.reportGroupClause())
                if (clause.reportPresentWhenClause()?.condition() is { } cond) conds.Add(cond);
        if (conds.Count == 0) return;

        // A name also declared in ordinary storage resolves THERE (never to the report item), so it is not an
        // SR16 reference — only report-section-exclusive names are scanned (no textual false positives). The SAME
        // set answers §13.18.16.3 SR2 for a CONTROL operand, so it is built in ONE place (kb/Work PB205).
        var names = Reports.SelectMany(r => ReportSectionOnlyNames(r)).Distinct(CobolNames.Comparer).ToList();

        foreach (var cond in conds)
        {
            using var _ = Edition.At(cond);
            // The register is a LINE_COUNTER / PAGE_COUNTER token as an operand but a SUBSCRIPT-mode word inside the
            // parentheses of a subscript (`TE(PAGE-COUNTER)`), which a token-type test never saw (kb/Work PB1474) —
            // so the word is asked, which both spellings share.
            if (HasWord(cond, "LINE-COUNTER") || HasWord(cond, "PAGE-COUNTER"))
                Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}': a PRESENT WHEN condition shall not "
                    + "reference LINE-COUNTER or PAGE-COUNTER (ISO §13.15.3 SR16)");
            foreach (var n in names)
                if (HasWord(cond, n))
                {
                    Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}': a PRESENT WHEN condition shall not "
                        + $"reference the report section data item '{n}' (ISO §13.15.3 SR16)");
                    break;
                }
        }
    }

    /// <summary>⛔ THE ONE "is this one of the CONTROL clause's operands" TEST (kb/Work PB205) — the level of the
    /// CONTROL operand <paramref name="operand"/> names, or −1. Both clauses that name a control level ask this
    /// question in the same words: §13.18.57.3 SR10 ("shall be the same as one of the operands of the CONTROL
    /// clause of the corresponding report description entry") and §13.18.54.3 SR8 ("shall be an operand of the
    /// CONTROL clause of the current report description"), so one comparison serves both — over the WRITTEN
    /// reference, ref-mod included, which is what §13.18.16.3 SR6 makes unique.</summary>
    private static int ControlLevelOf(ReportModel model, ReportControlRef operand)
    {
        for (int i = 0; i < model.Controls.Count; i++)
            if (model.Controls[i].Operand is { } c && c.SameOperandAs(operand)) return i;
        return -1;
    }

    /// <summary>True when <paramref name="name"/> is defined in the report section AND NOWHERE ELSE — the test
    /// §13.18.16.3 SR2 needs of a CONTROL operand and §13.15.3 SR16 needs of a PRESENT WHEN condition. A name
    /// also declared in ordinary storage resolves THERE (§8.4.2.1), so it is not a report-section reference; the
    /// set is therefore "report-section-exclusive", never "every report-section name".</summary>
    private bool IsReportSectionOnlyName(string name)
    {
        foreach (var r in Reports)
            foreach (var n in ReportSectionOnlyNames(r))
                if (CobolNames.Same(n, name)) return true;
        return false;
    }

    /// <summary>⛔ THE ONE SECTION SCREEN OVER A REPORT VALUE CLAUSE'S ARITHMETIC-EXPRESSION OPERAND (kb/Work
    /// PB852 × PB883). Both clauses constrain which items the identifiers INSIDE the expression may name, and
    /// they draw the line in different places — so one walk, one caller-supplied policy:
    /// <list type="bullet">
    /// <item>SUM, ISO §13.18.54.3 SR6: "If the addend is arithmetic-expression-1, any identifiers it contains may
    /// reference entries in any section of the data division other than the report section." Nothing of the
    /// report section is admitted — not a sum counter, and (by §8.4.3.15.3 SR1, "In the report section,
    /// PAGE-COUNTER and LINE-COUNTER may be referenced only in a SOURCE clause") not a report counter either.
    /// Pass <paramref name="countersOf"/> = null.</item>
    /// <item>SOURCE, ISO §13.18.53.3 SR4: "If identifier-1 specifies a report section item, it shall be a report
    /// counter identifier or a sum counter defined in the current report. This same Syntax rule applies to any
    /// identifier appearing in arithmetic-expression-1." Pass the CURRENT report.</item>
    /// </list>
    /// Returns the first offending identifier as written, or null when every identifier is admissible.</summary>
    private string? ReportSectionNameIn(Antlr4.Runtime.Tree.IParseTree node, ReportModel? countersOf = null)
    {
        // A subscript list's names are read by SubscriptWordsOfReference below, which leaves §8.4.2.3.3 SR8's counters
        // to ScreenReportSubscripts; walking into the list as well would judge a counter subscript a second time.
        if (node is Core.SubscriptPartContext) return null;
        if (node is Core.DataReferenceContext dref)
        {
            if (dref.LINE_COUNTER() is not null || dref.PAGE_COUNTER() is not null)
            {
                // A report counter is admissible only in a SOURCE clause (§8.4.3.15.3 SR1), and — "report counter
                // identifier", SR4's first admitted item — of ANY report: §8.4.2.2.3 SR9/SR10's third sentence says
                // how the counter of a DIFFERENT report is referenced ("shall be qualified explicitly by the
                // report-name associated with the different report"), so that spelling is legal. A qualifier that
                // names no report is the qualification rule's own error, reported where the counter is bound
                // (kb/Work PB1456); it is not a report section NAME and is not flagged here.
                if (countersOf is null) return dref.GetText();
                return null;
            }
            if (dref.cobolWord()?.GetText() is { } w && IsReportSectionOnlyName(w))
            {
                // A sum counter of the CURRENT report is SR4's other admitted report-section item.
                bool ownCounter = countersOf is not null
                    && countersOf.SumFamilies.Any(s => s.Name is { } sn && CobolNames.Same(sn, w));
                return ownCounter ? null : dref.GetText();
            }
            // identifier-1 is a qualified-data-name-with-subscripts (§8.4.3.1.2): a name inside a SUBSCRIPT or a
            // reference-modification is an identifier of the clause too (SR4's "any identifier appearing in…"). A
            // subscript's words are read here (kb/Work PB1295): a report section item other than a sum counter or a
            // counter register — those are §8.4.2.3.3 SR8's, said once by ScreenReportSubscripts — is a report section
            // name like any other.
            foreach (var subscripted in SubscriptWordsOfReference(dref))
                if (!IsReportSubscriptCounter(subscripted) && IsReportSectionOnlyName(subscripted)) return subscripted;
            for (int i = 0; i < dref.ChildCount; i++)
                if (ReportSectionNameIn(dref.GetChild(i), countersOf) is { } inner) return inner;
            return null;
        }
        for (int i = 0; i < node.ChildCount; i++)
            if (ReportSectionNameIn(node.GetChild(i), countersOf) is { } bad) return bad;
        return null;
    }

    /// <summary>The words of the SUBSCRIPTS of one written reference (kb/Work PB1295, PB1474): every name written in
    /// its subscript lists (<c>subscriptPart</c>). The reference counts only when its base word is a data item or a
    /// report section entry — a keyword-omitted function call has the same parenthesised shape, and its arguments
    /// are not subscripts (§8.4.3.2.3 SR2) — and a reference modification (<c>refModPart</c>) has none.</summary>
    private IEnumerable<string> SubscriptWordsOfReference(Core.DataReferenceContext dref)
    {
        if (dref.cobolWord()?.GetText() is not { } baseWord || !(ByName.ContainsKey(baseWord) || IsReportSectionOnlyName(baseWord)))
            yield break;
        foreach (var suffix in dref.dataReferenceSuffix())
        {
            var parts = suffix.subscriptPart() is { } direct ? [direct]
                : suffix.qualification()?.subscriptPart() ?? [];
            foreach (var part in parts)
                foreach (var word in SubscriptIdentifiers(part)) yield return word;
        }
    }

    private static IEnumerable<string> SubscriptIdentifiers(Antlr4.Runtime.Tree.IParseTree node)
    {
        if (node is Antlr4.Runtime.Tree.ITerminalNode t)
        {
            if (ReferenceResolver.IsNameToken(t.Symbol) || t.Symbol.Type is Core.LINE_COUNTER or Core.PAGE_COUNTER)
                yield return t.GetText();
            yield break;
        }
        for (int i = 0; i < node.ChildCount; i++)
            foreach (var w in SubscriptIdentifiers(node.GetChild(i))) yield return w;
    }

    /// <summary>Is <paramref name="word"/> one of the identifiers ISO §8.4.2.3.3 SR8 bars as a report section subscript:
    /// the LINE-COUNTER or PAGE-COUNTER register, or a sum counter — the name of a report section entry (and of no
    /// ordinary-storage item, §8.4.2.1) that contains a SUM clause.</summary>
    private bool IsReportSubscriptCounter(string word) =>
        CobolNames.Same(word, "LINE-COUNTER")
        || CobolNames.Same(word, "PAGE-COUNTER")
        || (!ByName.ContainsKey(word) && Reports.Any(r => r.WrittenEntries.Any(ge =>
            ge.dataName().NameOrNull() is { } n && CobolNames.Same(n, word)
            && ge.reportGroupClause().Any(c => c.reportSumClause() is not null))));

    /// <summary>⛔ ISO §8.4.2.3.3 SR8 (kb/Work PB1474) — "In the report section, neither a sum counter nor the LINE-COUNTER
    /// and PAGE-COUNTER identifiers may be used as a subscript." Asked ONCE per written entry, over every clause that
    /// can carry a subscripted reference — SOURCE, SUM, VARYING, OCCURS … DEPENDING ON — because the rule is about the
    /// SUBSCRIPT wherever it is written, not about one clause's operand: three arms each resolved the base name and
    /// evaluated the subscript in the procedure phase, and none asked it. A PRESENT WHEN condition is §13.15.3 SR16's
    /// (it refuses every sum counter, register and report section item the condition references, subscript or not),
    /// so it is not asked twice. A report section item that is neither is the clause's own section rule
    /// (<see cref="ReportSectionNameIn"/>, §13.18.53.3 SR4 / §13.18.54.3 SR6).</summary>
    private void ScreenReportSubscripts(ReportModel model)
    {
        foreach (var ge in model.WrittenEntries)
            foreach (var clause in ge.reportGroupClause())
            {
                if (clause.reportPresentWhenClause() is not null) continue;
                var seen = new HashSet<string>(CobolNames.Comparer);
                foreach (var word in SubscriptWordsOf(clause).Where(IsReportSubscriptCounter))
                    if (seen.Add(word))
                    {
                        using var at = Edition.At(clause);
                        Edition.Error(DiagnosticCatalog.ReportSubscriptCounter, $"RD '{model.Name}' entry "
                            + $"'{ge.dataName().NameOrNull() ?? "FILLER"}': '{word}' is used as a subscript. In the report "
                            + "section, neither a sum counter nor the LINE-COUNTER and PAGE-COUNTER identifiers may be used "
                            + "as a subscript (ISO §8.4.2.3.3 SR8)");
                    }
            }
    }

    private IEnumerable<string> SubscriptWordsOf(Antlr4.Runtime.Tree.IParseTree node)
    {
        if (node is Core.DataReferenceContext dref)
            foreach (var w in SubscriptWordsOfReference(dref)) yield return w;
        for (int i = 0; i < node.ChildCount; i++)
            foreach (var w in SubscriptWordsOf(node.GetChild(i))) yield return w;
    }

    /// <summary>The report-section-exclusive names of one report: the data-name of EVERY report group description
    /// entry it writes — the group's own, an intermediate group entry's, a printable entry's, a sum counter's, an
    /// unprintable SOURCE entry's — each only when <see cref="ByName"/> does not also carry it (see
    /// <see cref="IsReportSectionOnlyName"/> for why). Read off the WRITTEN entries (<see cref="ReportModel.WrittenEntries"/>):
    /// the set is about NAMES the programmer declared, and the bound products (lines, fields, counters) exist only
    /// for the entries that make one, so a name on any other entry was invisible to the §13.18.16.3 SR2 and
    /// §13.15.3 SR16 questions (kb/Work PB1289). A sum counter's name is its entry's (kb/Work PB882).</summary>
    private List<string> ReportSectionOnlyNames(ReportModel model)
    {
        var names = new List<string>();
        foreach (var ge in model.WrittenEntries)
            if (ge.dataName().NameOrNull() is { } name && !ByName.ContainsKey(name)
                && !names.Contains(name, CobolNames.Comparer))
                names.Add(name);
        return names;
    }

    /// <summary>⛔ THE ONE CAPTURE OF A CONTROL-CLAUSE OPERAND REFERENCE, shared by the THREE clauses that write
    /// one (kb/Work PB205): the CONTROL clause itself (§13.18.16.3), a TYPE CH/CF operand (§13.18.57.3 SR10) and a
    /// SUM … RESET ON operand (§13.18.54.3 SR8). It keeps the WHOLE written reference — base word, IN/OF
    /// qualifiers AND reference modification — because all three clauses expressly permit the ref-mod and all
    /// three identify a control level BY the written form. Before this, all three kept only part of it
    /// (<c>KeyReference</c> drops <c>refModPart</c>; TYPE and RESET kept the bare <c>cobolWord</c>) and compared
    /// by NAME, so <c>CX(1:3)</c> and <c>CX(4:3)</c> were one operand.
    /// <para>The rule the three clauses SHARE is screened here, once — "If [data-name-1] is reference-modified,
    /// leftmost-position and length shall be integer literals" (§13.18.16.3 SR4, and word-for-word in §13.18.54.3 SR8
    /// and §13.18.57.3 SR10) — with the CALLING clause's own <paramref name="code"/> and <paramref name="rule"/> in the message,
    /// so each site keeps its own citation. Two ref-mods are §8.4.3.3.3 SR3 (the existing COBOLNET1630). A
    /// SUBSCRIPT is rejected outright: §13.18.16.3 SR3 bars an operand subject to an OCCURS clause and
    /// §8.4.2.3.3 SR2 bars a subscript on an item that has none, so no legal subscripted spelling exists — and it
    /// was being dropped on the floor exactly as the ref-mod was.</para></summary>
    private ReportControlRef ControlOperandRef(
        Core.DataReferenceContext dref, DiagnosticDescriptor code, string where, string rule)
    {
        var (name, quals) = KeyReference(dref);
        var sfx = ReferenceResolver.ReadOperandSuffixes(dref);
        if (sfx.Subscripts > 0)
            Edition.Error(code, $"{where} '{DataBinder.WrittenText(dref)}' is subscripted. A control operand may not be subject "
                + "to an OCCURS clause (ISO §13.18.16.3 SR3), and a subscript may be written only for an item that "
                + "has one (§8.4.2.3.3 SR2), so a subscripted control operand is never legal.");
        if (sfx.RefMods > 1)
            Edition.Error(DiagnosticCatalog.RefModOfRefMod, $"{where} '{DataBinder.WrittenText(dref)}' carries {sfx.RefMods} reference "
                + "modifications; a reference-modified item cannot itself be reference-modified (ISO §8.4.3.3.3 "
                + "SR3). Compose the positions into one modifier instead.");
        else if (sfx.NonLiteral)
            Edition.Error(code, $"{where} '{DataBinder.WrittenText(dref)}' is reference-modified with a leftmost-position or "
                + $"length that is not an integer literal. The operand may be reference-modified, but if it is, "
                + $"leftmost-position and length shall be integer literals ({rule}) — the prior control has the "
                + "same data description as the slice (§13.18.16.4 GR3), so its extent is fixed at compile time.");
        else if (sfx.BeyondHostLimit)
            // kb/Work PB1579: the literals ARE integer literals (the rule above holds), but one lies beyond the host
            // range, and the slice is laid out as the prior control's own description — the limit a written
            // integer-n that sizes or positions something meets (COBOLNET2427), never a saturated bound allocated.
            Edition.Error(DiagnosticCatalog.IntegerOperandBeyondLimit,
                CobolNet.Validation.IntegerOperandRules.BeyondLimitMessage(where,
                    $"the reference-modification of '{DataBinder.WrittenText(dref)}', a leftmost-position or length that"));
        return new ReportControlRef(name, quals, sfx.Start, sfx.Length);
    }

    /// <summary>⛔ THE ONE SHAPE SCREEN FOR A CONTROL OPERAND — the syntax rules over data-name-1, one arm per
    /// rule (kb/Work PB177 arm C). Returns the diagnostic to raise and the clause of the message naming the rule
    /// violated, or null when the operand is legal. Each arm carries its OWN descriptor, so a rule from outside
    /// §13.18.16.3 keeps its own citation instead of being folded into a clause list it does not belong to.
    /// <para>Every arm was MISSING and each was measured before it was written: SR3 and SR5 compiled and RAN
    /// silently; SR7 and the INDEX shape compiled and then staged a RUNTIME loud — a syntax rule demands a
    /// compile-time rejection, and the emitter's loud is a backstop, not the verdict. Note the SR5/SR7 pair is
    /// exactly the trap <c>feedback_validate_the_premise_not_only_the_rule</c> names: an occurs-DEPENDING table
    /// does NOT make a group "variable-length" (§8.5.1.12.1 defines that term over dynamic-length elementary
    /// items and dynamic-CAPACITY tables), so SR7 does not reach the ODO shape and SR5 exists precisely because
    /// it does not — two rules, two arms, and a screen written for only one of them would leave the other
    /// open.</para>
    /// <para>⛔ THE SR3 ARM RECOGNISES A TABLE WITH <see cref="DataItem.IsTable"/>, NOT <c>Occurs is not null</c>
    /// (kb/Work PB177 arm C follow-up). <see cref="DataItem.Occurs"/> is the FIXED physical capacity and is NULL
    /// for a Format-4 dynamic-capacity table, so the first spelling missed BOTH dynamic shapes — the operand that
    /// IS the dynamic table and the operand subordinate to one — and SR7 structurally cannot cover the first of
    /// them (§8.5.1.12.1 defines "variable-length group" over items SUBORDINATE to the group, so the table entry
    /// itself is never one). Measured: both compiled clean and reached ReportWriterEmitter's runtime loud. This is
    /// a table-RECOGNITION site, which is exactly what <c>IsTable</c>'s own doc-comment says it is for.</para></summary>
    private static (DiagnosticDescriptor Code, string Clause)? ControlOperandShapeViolation(DataItem item)
    {
        // "Subject to an OCCURS clause" is §8.4.2.3.3 SR2's question under another name, so it is asked through
        // THE one walk (DataItem.SubscriptLevels, outermost first — kb/Work PB877); the message names the
        // INNERMOST level, which is the one the hand-written self→parent loop used to report.
        if (item.SubscriptLevels() is [.., var innermost])
            return (DiagnosticCatalog.ReportControlOperandShape,
                $"is subject to the OCCURS clause on '{innermost.CobolName ?? innermost.CsName}': data-name-1 shall "
                + "not be subject to any OCCURS clauses (ISO §13.18.16.3 SR3)");
        if (OdoModel.TableUnder(item) is { } odo)
            return (DiagnosticCatalog.ReportControlOperandShape,
                $"has the occurs-depending table '{odo.CobolName ?? odo.CsName}' subordinate to it: the "
                + "entry specified by data-name-1 shall not have an occurs-depending table subordinate to it "
                + "(ISO §13.18.16.3 SR5)");
        if (item.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(item))
            return (DiagnosticCatalog.ReportControlOperandShape,
                "is a variable-length group (ISO §8.5.1.12.1 — a dynamic-length elementary item or a "
                + "dynamic-capacity table is subordinate to it): data-name-1 shall not reference a "
                + "variable-length group (ISO §13.18.16.3 SR7)");
        // §13.18.60.3 SR10 closes the set of contexts in which an index data item may be referenced EXPLICITLY,
        // and §8.4.5 makes a data-division clause naming a data item exactly such an explicit reference: "a
        // specification in the environment or data division may specify the name of a data item as an explicit
        // reference in order to identify those data items that are to be referenced implicitly in procedure
        // division statements related to such specifications." The CONTROL clause is not on SR10's list.
        if (item.Pic is { Usage: Usage.Index })
            return (DiagnosticCatalog.ReportControlOperandIndex,
                "is an index data item: an index data item may be referenced explicitly only in a SEARCH or SET "
                + "statement, a relation condition, an intrinsic function argument, an inline method invocation "
                + "argument, the USING phrase of a procedure division header, or the USING phrase of a CALL or "
                + "INVOKE statement (ISO §13.18.60.3 SR10; a CONTROL clause naming it is an explicit reference, "
                + "§8.4.5)");
        return null;
    }

    /// <summary>Resolve a report clause's (possibly IN/OF-qualified) data-name — CONTROL, SOURCE, SUM — through the
    /// ONE §8.4.2.2 resolver (<see cref="QualifiedCandidates"/>) and the ONE ambiguity verdict
    /// (<see cref="UniqueOrReportAmbiguous"/>). <paramref name="ambiguous"/> says the null was reported here as
    /// §8.4.2.2.3 SR1's ambiguity; any other null names nothing, which the caller's own clause rule reports.
    /// <para>⛔ THIS WAS A PRIVATE FIRST-MATCH TWIN (kb/Work PB978): an unqualified name took <c>ByName[n][0]</c>
    /// and a qualified one the first candidate whose chain matched, so <c>RD RPT CONTROL IS K OF G</c> with both
    /// <c>A.G.K</c> and <c>B.G.K</c> declared compiled clean and broke control on whichever K came first. Its
    /// qualifier walk also knew nothing of the file-name qualifier (§8.4.2.2.2 Format 1).</para></summary>
    private DataItem? LookupQualified(string name, IReadOnlyList<string> qualifiers, string face, out bool ambiguous) =>
        UniqueOrReportAmbiguous(QualifiedCandidates(name, qualifiers, Model.Scope.Program), face,
            WrittenQualified(name, qualifiers), out ambiguous);
}
