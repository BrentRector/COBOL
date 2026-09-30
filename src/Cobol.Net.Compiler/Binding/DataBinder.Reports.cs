// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding;

using Core = CobolParserCore;
using CobolNet.Compiler.Oo;
using ReportNextGroup = CobolNet.Runtime.IO.ReportNextGroup;
using ReportNextGroupKind = CobolNet.Runtime.IO.ReportNextGroupKind;

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

    /// <summary>The report line width: the FD's fixed RECORD CONTAINS when present, else the widest field extent
    /// (column + image width − 1) over the report (the §13.18.39.4 GR5 page-width default 999 is a MAXIMUM, not
    /// a record length). Computed post-build.</summary>
    public int LineWidth { get; set; } = 1;

    /// <summary>The CONTROL hierarchy in major→minor order (ISO §13.18.16.4 GR1; FINAL, if present, first — GR2).</summary>
    public List<ReportControlModel> Controls { get; } = [];

    /// <summary>The report groups in declaration order.</summary>
    public List<ReportGroupModel> Groups { get; } = [];

    /// <summary>The SUM counters of this report (ISO §13.18.54), keyed by counter id.</summary>
    public List<ReportSumModel> Sums { get; } = [];

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
        if (!Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase)) return false;
        if (RefModStart != other.RefModStart || RefModLength != other.RefModLength) return false;
        if (Qualifiers.Count != other.Qualifiers.Count) return false;
        for (int i = 0; i < Qualifiers.Count; i++)
            if (!Qualifiers[i].Equals(other.Qualifiers[i], StringComparison.OrdinalIgnoreCase)) return false;
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

    /// <summary>The CH/CF control operand as written (§13.18.57.3 SR10 — qualifiable AND reference-modifiable);
    /// null when omitted (legal only with a one-operand CONTROL clause, SR11), for FINAL, or for a non-control
    /// group.</summary>
    public ReportControlRef? ControlOperand { get; set; }
    public bool ControlFinal { get; set; }

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

/// <summary>One placement of a printable item: a COLUMN clause operand (ISO §13.18.14 Format 1) or, for a
/// repetition of a STEP'd repeating entry, its step-anchor placement (§13.18.38.4 GR12).</summary>
/// <param name="Kind">Which datum fixes the leftmost column.</param>
/// <param name="Value">integer-1 / integer-2 / integer-3, per <paramref name="Kind"/>.</param>
/// <param name="AnchorId">The step anchor's compose-local register id; 0 for the two COLUMN-clause kinds.</param>
public readonly record struct ReportColumnSpec(ReportColumnKindModel Kind, int Value, int AnchorId = 0)
{
    /// <summary>A COLUMN clause operand as written: absolute, or relative (PLUS).</summary>
    public ReportColumnSpec(bool relative, int value)
        : this(relative ? ReportColumnKindModel.Relative : ReportColumnKindModel.Absolute, value) { }

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

/// <summary>One report VARYING counter (ISO §13.18.64): the counter name, the FROM/BY expressions as captured
/// parse contexts (bound to <see cref="From"/>/<see cref="By"/> in the procedure phase; null = the GR3 default 1),
/// stepping once per repetition of the entry's multiple COLUMN clause (GR3a/GR3b).</summary>
public sealed class ReportVaryingModel
{
    public required string Name { get; init; }
    public CobolParserCore.ArithmeticExpressionContext? FromCtx { get; init; }
    public CobolParserCore.ArithmeticExpressionContext? ByCtx { get; init; }
    public BoundExpr? From { get; set; }
    public BoundExpr? By { get; set; }
}

/// <summary>One PRINTABLE item (an entry with a COLUMN clause, ISO §13.18.14): its column operands (one per
/// repetition — a multiple COLUMN clause is a repeating entry, §13.15.4 GR3), the synthetic
/// <see cref="DataItem"/> carrying its PICTURE/JUSTIFIED/BLANK WHEN ZERO, its value-source OPERAND LIST, the
/// GROUP INDICATE flag (§13.18.28), its field-local PRESENT WHEN chain (the conditions BELOW the line entry —
/// the line's own chain already gates the whole line), and the entry's VARYING counters (§13.18.64).</summary>
public sealed class ReportFieldModel
{
    public required IReadOnlyList<ReportColumnSpec> Columns { get; init; }
    public required DataItem PrintItem { get; init; }

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

    /// <summary>The first column operand's value — the single-absolute fast path and diagnostics anchor.</summary>
    public int Column => Columns[0].Value;

    /// <summary>PRESENT WHEN conditions strictly below the line entry, down to this entry (§13.18.41.4 GR2b).</summary>
    public List<CobolParserCore.ConditionContext> PresentWhenCtxs { get; } = [];
    public List<BoundCondition> PresentWhen { get; } = [];

    /// <summary>The entry's VARYING counters (§13.18.64) — empty for a non-VARYING entry.</summary>
    public List<ReportVaryingModel> Varyings { get; } = [];

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

/// <summary>A SOURCE clause data reference (ISO §13.18.53), captured as base word + IN/OF qualifiers (the FILE
/// STATUS capture pattern) and resolved post-build to <see cref="Item"/>.</summary>
public sealed record FieldDataSource(string Name, IReadOnlyList<string> Qualifiers) : ReportFieldSource
{
    public DataItem? Item { get; set; }
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

/// <summary>A <c>SOURCE IS LINE-COUNTER / PAGE-COUNTER</c> reference (ISO §8.4.3.15 SR1 — referable in the
/// report section only in SOURCE). The counter is the OWN report's (a report-name qualifier naming another
/// report is staged loud — no corpus surface).</summary>
public sealed record FieldCounterSource(bool IsPage) : ReportFieldSource;

/// <summary>The printable face of a SUM entry (ISO §13.18.54.4 GR4 — the sum counter acts as the source item).
/// <paramref name="CounterId"/> is the counter's ENTRY ORDINAL (GR1; <see cref="ReportSumModel.Id"/>), never a
/// data-name — two entries may legally carry the same one (kb/Work PB882).</summary>
public sealed record FieldSumSource(int CounterId) : ReportFieldSource;

/// <summary>A SOURCE naming the entry's own VARYING counter (ISO §13.18.64.4 GR4 NOTE — the counter is usable as
/// a source data item); <paramref name="Index"/> indexes <see cref="ReportFieldModel.Varyings"/>. Renders as the
/// compose-local counter, re-read per repetition.</summary>
public sealed record FieldVaryingSource(int Index) : ReportFieldSource;

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

    /// <summary>The operand as written — the form every diagnostic about it quotes.</summary>
    public override string ToString() => Qualifier is null ? Name : $"{Name} OF {Qualifier}";
}

/// <summary>One SUM counter (ISO §13.18.54): its identity (GR1 — see <see cref="Id"/>), the counter scale
/// (GR1 — derived from the entry's PICTURE), the addend TERMS (SR5 — items OUTSIDE the report section;
/// report-section addends/rolled totals are staged loud), each carrying its own UPON detail names (GR7c2), and
/// the RESET operand (GR2).</summary>
public sealed class ReportSumModel
{
    /// <summary>⛔ THE COUNTER'S IDENTITY IS THE ENTRY, NEVER ITS SPELLING (kb/Work PB882). ISO §13.18.54.4
    /// GR1: "Each entry containing a SUM clause establishes an independent sum counter and size error
    /// indicator." This is the entry's ORDINAL within its report description — the index of this model in
    /// <see cref="ReportModel.Sums"/> and, at run time, of its counter in the engine's list. It used to be
    /// <see cref="Name"/> (the entry's data-name, else a synthesized string), which made two entries that
    /// legally share a data-name share ONE counter: the second registration overwrote the first and both
    /// printable faces rendered the second total.</summary>
    public required int Id { get; init; }

    /// <summary>The data-name that NAMES THIS COUNTER (ISO §13.18.54.4 GR5 — "If a data-name immediately
    /// follows the level number in the entry containing the SUM clause, the data-name is the name of the sum
    /// counter, not the name of the associated printable item, if any"), or null for an unnamed entry. It is
    /// what a procedure division statement writes to read or alter the counter (GR12) — <b>not</b> its
    /// identity, which is <see cref="Id"/>.</summary>
    public string? Name { get; init; }

    /// <summary>The IMPLICITLY-DEFINED register this counter is (ISO §13.18.54.4 GR1 — "a conceptual data item
    /// that behaves as a data item of the category numeric"), carrying the GR1 profile
    /// (<see cref="PicInfo.SumCounterItem"/>). It is engine state, not storage — kept off
    /// <c>DataBinder.ByName</c>/<c>Roots</c> and reachable only through <c>DataBinder.SumCounters</c>, the
    /// resolver hook that builds a <see cref="Model.ReportSumCounterPlace"/> (the CAPACITY-register pattern).
    /// It is what GR5's data-name names and what GR12 permits the procedure division to alter.</summary>
    public required DataItem Register { get; init; }

    public int Scale { get; init; }

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

    /// <summary>The SUM entry's FULL PRESENT WHEN chain (01 → entry). When any condition is false at a group
    /// presentation the counter is neither printed nor reset for that instance (ISO §13.18.41.4 GR3g /
    /// §13.18.54.4 GR10) — the engine consults the AND of these per presentation.</summary>
    public List<CobolParserCore.ConditionContext> PresentWhenCtxs { get; } = [];
    public List<BoundCondition> PresentWhen { get; } = [];
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
        if (VisibleReports.Any(r => r.Name.Equals(report.Name, StringComparison.OrdinalIgnoreCase))) return false;
        if (_inheritedReports.Count == 0) _visibleReports.AddRange(_reports);
        _visibleReports.Add(report);
        _inheritedReports[report] = depth;
        foreach (var sum in report.Sums)
        {
            // A sum counter is a data-name subordinate to the report (GR1). A LOCAL declaration of the same name
            // hides it; a same-named counter of this unit's own reports is a homonym the report-name qualifier
            // resolves, exactly as between two reports of one program (kb/Work PB882).
            if (sum.Name is not { } sn || ByName.ContainsKey(sn)) continue;
            if (!_sumCounters.TryGetValue(sn, out var homonyms)) _sumCounters[sn] = homonyms = [];
            homonyms.Add((report, sum));
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
    private void ScreenReportUsage(Usage usage, ReportModel model, string? entryName, string? written)
    {
        if (usage is Usage.Display or Usage.National) return;
        Edition.Error(DiagnosticCatalog.ReportUsageNotDisplayOrNational, $"RD '{model.Name}' entry "
            + $"'{entryName ?? "FILLER"}'{(written is null ? "" : $" ({written})")}: usage {usage} is not "
            + "admitted here — only the DISPLAY or NATIONAL phrase may be specified in any USAGE clause "
            + "associated with a report group item (ISO §13.18.60.3 SR7)");
    }

    /// <summary>Bind one RD entry's description clauses: PAGE geometry (§13.18.39) with the GR3 defaults,
    /// CONTROL (§13.18.16), GLOBAL (§13.18.27 on an RD); CODE (§13.18.12) stages loud.</summary>
    private void BindReportDescriptionClauses(Core.ReportDescriptionEntryContext rd, ReportModel model)
    {
        bool heading = false, firstDetail = false, lastDetail = false, footing = false;
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
            else if (clause.reportCodeClause() is not null)
                Edition.Error(DiagnosticCatalog.ReportCodeClause, $"RD '{model.Name}': the CODE clause (ISO §13.18.12) is not yet "
                    + "implemented");
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
                model.Paged = true;
                model.PageLimit = CobolNet.Validation.IntegerOperandRules.HostValue(page.integerLiteral());
                // §13.18.39.2 prints each phrase in its own bracket with no ellipsis; SR4 licenses any ORDER,
                // never a repeat — `HEADING 1 HEADING 2` used to keep the last value silently (COBOLNET2423).
                var subs = page.reportPageSubclause();
                UnrepeatedElements.AtMostOnce(Edition, subs.Count(s => s.HEADING() is not null), rdWhere,
                    "the PAGE clause's HEADING phrase", "13.18.39.2");
                UnrepeatedElements.AtMostOnce(Edition, subs.Count(s => s.FIRST() is not null), rdWhere,
                    "the PAGE clause's FIRST DETAIL phrase", "13.18.39.2");
                UnrepeatedElements.AtMostOnce(Edition, subs.Count(s => s.LAST() is not null), rdWhere,
                    "the PAGE clause's LAST DETAIL phrase", "13.18.39.2");
                UnrepeatedElements.AtMostOnce(Edition, subs.Count(s => s.FOOTING() is not null), rdWhere,
                    "the PAGE clause's FOOTING phrase", "13.18.39.2");
                foreach (var sub in subs)
                {
                    int v = CobolNet.Validation.IntegerOperandRules.HostValue(sub.integerLiteral());
                    if (sub.HEADING() is not null) { model.Heading = v; heading = true; }
                    else if (sub.FIRST() is not null) { model.FirstDetail = v; firstDetail = true; }
                    else if (sub.LAST() is not null) { model.LastDetail = v; lastDetail = true; }
                    else if (sub.FOOTING() is not null) { model.Footing = v; footing = true; }
                }
            }
        }
        if (!model.Paged) return;
        // The §13.18.39.4 GR3 defaults (the grammar has no LAST CONTROL HEADING phrase, so GR3c always defaults):
        if (!heading) model.Heading = 1;                                              // GR3a
        if (!firstDetail) model.FirstDetail = model.Heading;                          // GR3b
        if (!lastDetail) model.LastDetail = footing ? model.Footing : model.PageLimit;   // GR3d
        model.LastControlHeading = lastDetail ? model.LastDetail
            : footing ? model.Footing : model.PageLimit;                              // GR3c
        if (!footing) model.Footing = lastDetail ? model.LastDetail : model.PageLimit;   // GR3e
    }

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
        ScreenReportLineNesting(entries, model);
        ScreenReportLineClauses(entries, model);
        ScreenReportEntryClausePresence(entries, model);
        BindReportEntries(entries, 0, entries.Length, model, new ReportGroupBuild());
        BindNextGroupClauses(entries, model);
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
                        + $"'{ge.reportGroupName()?.GetText() ?? "FILLER"}' is a level {level} entry; the NEXT GROUP "
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
                    CobolNet.Validation.IntegerOperandRules.HostValue(ngc.integerLiteral()));
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
                Violation($"{(absolute ? "integer-1" : "integer-2")} {ngc.integerLiteral().GetText()} exceeds {model.VerticalLimitWords}"
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
    /// when every line can be absent.</summary>
    private static int? MinimumLastLine(ReportGroupModel g, ReportModel model)
    {
        int? pos = null;
        var anchors = new Dictionary<int, int>();
        foreach (var l in g.Lines)
        {
            if (l.PresentWhenCtxs.Count > 0 || l.RepetitionGuards.Count > 0) continue;   // may be absent
            int relative = l.Kind == ReportLineKindModel.Step ? l.RelativeBase : l.Value;
            int target = l.Kind == ReportLineKindModel.Absolute ? l.Value
                : pos is null ? (g.Kind == ReportGroupKindModel.ReportHeading ? model.Heading + relative - 1
                                                                              : model.Footing + relative)
                : l.Kind == ReportLineKindModel.Step && anchors.TryGetValue(l.Anchor, out int a) ? a + l.Value
                : pos.Value + relative;
            if (l.Anchor != 0 && (l.Kind != ReportLineKindModel.Step || !anchors.ContainsKey(l.Anchor)))
                anchors[l.Anchor] = target - (l.Kind == ReportLineKindModel.Step ? l.Value : 0);
            pos = target;
        }
        return pos;
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
        bool firstLineClause = true;
        foreach (var ge in entries)
        {
            if (int.TryParse(ge.levelNumber().GetText(), out int level) && level == 1)
            {
                kind = WrittenGroupKind(ge);
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
                    bool absolute = op.integerLiteral() is not null && op.reportRelativeSign() is null;
                    if (op.integerLiteral() is { } lit)
                    {
                        if (CobolNet.Validation.IntegerOperandRules.HostValue(lit) > model.VerticalLimit)
                            Violation($"{(absolute ? "integer-1" : "integer-2")} {lit.GetText()} exceeds {model.VerticalLimitWords}"
                                + "; neither integer-1 nor integer-2 shall exceed the page limit, or 9999 if the report "
                                + "is not divided into pages (ISO §13.18.35.3 SR3)");
                    }
                    if (!model.Paged && (absolute || op.NEXT() is not null))
                        Violation($"the report is not divided into pages, so all its LINE clauses shall be relative — "
                            + (absolute ? $"LINE {op.integerLiteral()!.GetText()}{(op.NEXT() is not null ? " ON NEXT PAGE" : "")} is absolute"
                                        : "the ON NEXT PAGE operand is not the relative form {PLUS|+} integer-2")
                            + " (ISO §13.18.35.3 SR5)");
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
    /// </list>
    /// SR8 is <see cref="ScreenReportLineNesting"/> (its §13.18.35.3 SR4 twin), SR9 is the binder's
    /// <c>ReportColumnWithoutLine</c> arm, and SR12/SR14 are the PICTURE arm, which needs the analysed picture.
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
                justifiedOrBwz = false;
            foreach (var c in ge.reportGroupClause())
            {
                column |= c.reportColumnClause() is not null;
                source |= c.reportSourceClause() is not null;
                value |= c.valueClause() is not null;
                sum |= c.reportSumClause() is not null;
                picture |= c.pictureClause() is not null;
                groupIndicate |= c.reportGroupIndicateClause() is not null;
                justifiedOrBwz |= c.justifiedClause() is not null || c.blankWhenZeroClause() is not null;
            }
            string where = $"RD '{model.Name}' entry '{ge.reportGroupName()?.GetText() ?? "FILLER"}'";
            using var _ = Edition.At(ge);
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
        public readonly List<(int Level, Core.ConditionContext? Cond, int Reps, string? Usage)> Chain = [];
        /// <summary>Stack frames whose conditions the CURRENT line already carries.</summary>
        public int LineChainDepth;
        /// <summary>The repeating entries enclosing the entry being bound, outermost first (§13.18.38 Format 3).</summary>
        public readonly List<ReportRepetitionFrame> Repetitions = [];
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

            if (ReportRepetitionOf(ge, entries, i, subtreeEnd, model, st) is { } occurs)
            {
                var frame = new ReportRepetitionFrame(occurs);
                st.Repetitions.Add(frame);
                for (int rep = 0; rep < occurs.Max; rep++)
                {
                    frame.Ordinal = rep;
                    BindReportEntry(ge, entries[i], model, st, occurs, rep);
                    BindReportEntries(entries, i + 1, subtreeEnd, model, st);
                }
                st.Repetitions.RemoveAt(st.Repetitions.Count - 1);
                i = subtreeEnd - 1;
                continue;
            }
            BindReportEntry(ge, ge, model, st);
        }
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
            if (ops[k].integerLiteral() is not { } lit) continue;   // a bare `ON NEXT PAGE` operand
            if (ops[k].reportRelativeSign() is not null) { relativeSeen = true; continue; }
            // b) "All absolute operands, if present, shall precede all relative operands, if present."
            if (relativeSeen)
            {
                Edition.Error(DiagnosticCatalog.ReportLineClauseRule, $"RD '{model.Name}': in a multiple LINE clause all "
                    + "absolute operands shall precede all relative operands (ISO §13.18.35.3 SR10b)");
                continue;
            }
            // c) "The occurrences of integer-1, if present, shall be in ascending numerical order."
            int v = CobolNet.Validation.IntegerOperandRules.HostValue(lit);
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
        string name = ge.reportGroupName()?.GetText() ?? "FILLER";
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

        var bounds = oc.occursBound();
        bool hasTo = oc.TO() is not null;
        var depending = oc.dataReference();
        int? step = oc.occursStepPhrase() is { } sp ? CobolNet.Validation.IntegerOperandRules.HostValue(sp.integerLiteral()) : null;
        int max = OccursBoundValue(bounds[^1], where) ?? 0;
        int min = hasTo ? OccursBoundValue(bounds[0], where) ?? 0 : 0;

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
        if (spec.DependingItem.Pic is not { Category: PicCategory.Numeric, IsFloat: false, Scale: 0 })
            Edition.Error(DiagnosticCatalog.ReportOccursFormat3Rule, $"RD '{model.Name}': OCCURS … DEPENDING ON "
                + $"'{spec.DependingName}' — data-name-1 shall describe an integer (ISO §13.18.38.3 SR17)");
    }

    /// <summary>The display width of one report group entry's PICTURE, or null when it has none (a group entry).
    /// The §13.18.38.3 SR26 overlap test measures the repeated item against integer-3.</summary>
    private int? ReportEntryPictureWidth(Core.ReportGroupEntryContext ge, ReportModel model)
    {
        foreach (var clause in ge.reportGroupClause())
            if (clause.pictureClause()?.PIC_STRING() is { } pic)
                // The SAME character-position count the print columns use (§13.18.14.4 GR9), through the ONE rule.
                return PictureAnalyzer.Analyze(pic.GetText(), Usage.Display, Edition,
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
        Core.ReportGroupEntryContext ge, Core.ReportGroupEntryContext anchorKey, ReportModel model,
        ReportGroupBuild st, ReportOccursSpec? ownOccurs = null, int ordinal = 0)
    {
        {
            using var _ = Edition.At(ge);
            int.TryParse(ge.levelNumber().GetText(), out int level);
            string? entryName = ge.reportGroupName()?.GetText();
            if (level == 1)
            {
                st.Group = new ReportGroupModel { Name = entryName };
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
            // ISO §13.18.60.4 GR1 — the usage written on an enclosing report group entry "applies only to each
            // elementary item in the group", so the innermost surviving frame carries the usage this entry
            // inherits when it writes no USAGE clause of its own (kb/Work PB541).
            string? inheritedUsage = chain.Count > 0 ? chain[^1].Usage : null;

            // Clause capture for THIS entry (clauses may appear in any order within the entry — RW104A).
            string? picText = null, usageText = null;
            // The VALUE clause's operand list (§13.18.63.2 format 4) and the count WRITTEN — the SR35 check
            // reads the written count so a rejected operand cannot make an illegal clause look legal.
            var valueRaws = new List<string>();
            int valueOpsWritten = 0;
            List<EditingPhraseSpec>? reportEditing = null;   // PICTURE EDITING phrases (§13.18.40.2)
            LocaleEditSpec? reportLocale = null;             // PICTURE format 2 — the LOCALE phrase (PB113 / PB64 T6)
            SignSpec? ownSign = null;
            bool justified = false, blankWhenZero = false, groupIndicate = false, repeatingEntry = false;
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
            // ⛔ A LIST, NOT A SLOT (kb/Work PB482): ISO §13.18.54.3 SR1 — "The whole clause is referred to as a
            // SUM clause even though the SUM keyword may appear more than once", and §13.18.54.4 GR1 gives the
            // ENTRY one counter. A single slot silently DISCARDED every group but the last:
            // `SUM WS-A UPON DET SUM WS-B UPON DET2` totalled WS-B alone.
            var sumClauses = new List<Core.ReportSumClauseContext>();
            Core.ConditionContext? ownCond = null;
            var varyings = new List<ReportVaryingModel>();

            foreach (var clause in ge.reportGroupClause())
            {
                if (clause.reportTypeClause()?.reportGroupType() is { } t)
                    BindGroupType(t, group, model);
                else if (clause.reportLineClause() is { } lc)
                {
                    // The multiple LINE clause (§13.18.35.3 SR10) is a §13.15.4 GR3 repetition VEHICLE, read by
                    // ReportRepetitionOf before this entry is bound: §13.18.35.4 GR9 makes it "functionally
                    // equivalent to a LINE clause with a single operand, together with a simple OCCURS clause
                    // whose integer is equal to the number of operands", so this replay takes the operand its
                    // own ORDINAL names and the entry opens one report line per repetition. The modulo is the
                    // SR10d recovery path only (an entry carrying BOTH vehicles is diagnosed, not guessed at).
                    var ops = lc.reportLineOperand();
                    if (ops.Length > 1) repeatingEntry = true;
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
                    foreach (var op in cc.reportColumnOperand())
                        columns.Add(new ReportColumnSpec(op.reportRelativeSign() is not null, CobolNet.Validation.IntegerOperandRules.HostValue(op.integerLiteral())));
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
                    if (sc.roundedPhrase() is not null || sops.Any(o => BareReferenceOf(o) is null))
                        sourceNeedsNumericEntry = true;
                    foreach (var op in sops)
                        if (BindSourceOperand(op, sc.roundedPhrase(), model) is { } so) sourceOps.Add(so);
                }
                else if (clause.reportSumClause() is { } sm)
                    sumClauses.Add(sm);
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
                else if (clause.reportVaryingClause() is { } vy)
                    foreach (var spec in vy.reportVaryingSpec())
                        varyings.Add(new ReportVaryingModel
                        {
                            Name = spec.cobolWord().GetText(),
                            FromCtx = spec.FROM() is not null ? spec.arithmeticExpression(0) : null,
                            ByCtx = spec.BY() is not null
                                ? spec.arithmeticExpression(spec.FROM() is not null ? 1 : 0) : null,
                        });
                else if (clause.pictureClause()?.PIC_STRING() is { } pic)
                {
                    picText = pic.GetText();
                    reportEditing = BuildEditingSpecs(clause.pictureClause(),
                        $"report group entry '{entryName ?? "FILLER"}'");
                    // PICTURE format 2 in a REPORT GROUP entry (§13.15.4 GR2 imports the PICTURE clause's own
                    // rules, so format 2 is LEGAL here; kb/Work PB113 — this arm used to ignore the phrase and
                    // analyze the picture as format 1: a silent wrong answer). One analyzer, three callers. ⛔ Do
                    // NOT pair it with a SIGN check: §13.15.3 carries no twin of §13.16.3 SR19 — the pair is legal here.
                    if (clause.pictureClause()!.pictureLocalePhrase() is { } rlp)
                    {
                        var localeName = rlp.cobolWord();   // locale-name-1; LOCALE is the formatWord (kb/Work PB764)
                        var locale = LocaleRef.Current;
                        if (localeName is not null)
                        {
                            var sym = ResolveLocaleName(localeName.GetText(),
                                $"RD '{model.Name}' entry '{entryName ?? "FILLER"}' PICTURE … LOCALE {localeName.GetText()}",
                                "ISO §13.18.40.3 SR37 — locale-name-1 shall be specified in the LOCALE clause in the SPECIAL-NAMES paragraph");
                            if (sym is not null) locale = new LocaleRef(sym);
                        }
                        int size = Math.Max(1, CobolNet.Validation.IntegerOperandRules.HostValue(rlp.integerLiteral()));
                        reportLocale = new LocaleEditSpec(locale, size, "");
                    }
                }
                else if (clause.usageClause() is { } usage)
                {
                    usageText = UsageKeyword(usage);
                    // ⛔ §13.18.60.3 SR7 IS ABOUT THE CLAUSE, NOT ABOUT THE PRINTABLE LEAF (kb/Work PB541):
                    // "Only the DISPLAY or NATIONAL phrase may be specified in any USAGE clause associated with
                    // a report group item." A USAGE clause on a GROUP entry is associated with the report group
                    // items under it (GR1), so it is screened HERE, where every entry's clause passes — before
                    // this, a group entry's `USAGE COMP` was captured and then silently discarded, and the only
                    // usage screen in the compiler was a staged-loud on the leaf's analyzed picture.
                    ScreenReportUsage(PictureAnalyzer.ParseUsage(usageText, Edition,
                            $"RD '{model.Name}' entry '{entryName ?? "FILLER"}'"),
                        model, entryName, usageText);
                }
                else if (clause.signClause() is { } sign)
                    ownSign = new SignSpec(sign.LEADING() is not null, sign.SEPARATE() is not null);
                else if (clause.justifiedClause() is not null)
                    justified = true;
                else if (clause.blankWhenZeroClause() is not null)
                    blankWhenZero = true;
                else if (clause.occursClause() is not null)
                {
                    // The repeating entry itself (ISO §13.18.38 Format 3) is read by ReportOccursOf BEFORE this
                    // entry is bound — it drives the REPLAY, so there is nothing to capture per repetition.
                    // `repeatingEntry` records the §13.18.64.3 SR1 vehicle either way; a clause ReportOccursOf
                    // REFUSED (a syntax rule, or the still-staged vertical axis) leaves ownOccurs null and the
                    // entry's repetition count unknowable.
                    repeatingEntry = true;
                    if (ownOccurs is null) staysLoud = true;
                }
                else if (clause.valueClause() is { } value)
                {
                    // Format 4 (report-section), ISO §13.18.63.2 — `{literal-1}…`, an operand LIST; the same ONE
                    // literal-position reader as Format 1 per operand, so a non-literal operand is reported here
                    // too (kb/Work PB732: an undefined word used to be written into the report as its own
                    // spelling, exit 0) and the operands are never glued (kb/Work PB506).
                    CheckValueConnective(value, pairedConnective: true,
                        $"RD '{model.Name}' entry '{entryName ?? "FILLER"}'");
                    valueOpsWritten += value.valueItem().FirstOrDefault()?.valueClauseOperand().Length ?? 0;
                    if (ExtractValueOperandList(value, $"RD '{model.Name}' entry '{entryName ?? "FILLER"}'") is { } raws)
                        valueRaws.AddRange(raws);
                }
            }

            // GROUP INDICATE shall not share an entry with PRESENT WHEN (ISO §13.15.3 SR17 — GROUP INDICATE IS
            // a fixed-condition PRESENT WHEN, §13.18.28.4 GR1).
            if (groupIndicate && ownCond is not null)
                Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}' entry '{entryName ?? "FILLER"}': the GROUP "
                    + "INDICATE clause shall not be specified in an entry in which the PRESENT WHEN clause is "
                    + "specified (ISO §13.15.3 SR17)");

            // VARYING (§13.18.64): SR1 — the entry shall also contain OCCURS or a multiple LINE or multiple
            // COLUMN clause. All three vehicles set `repeatingEntry`/`columns`: OCCURS is LIVE on the horizontal
            // axis (§13.18.38 Format 3, the replay), the multiple LINE clause still stages loud.
            if (varyings.Count > 0)
            {
                if (!repeatingEntry && columns.Count <= 1)
                    Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}' entry '{entryName ?? "FILLER"}': a VARYING "
                        + "clause requires the entry to also contain an OCCURS clause or a multiple LINE or "
                        + "multiple COLUMN clause (ISO §13.18.64.3 SR1)");
                foreach (var v in varyings)
                {
                    // SR3: data-name-1 shall not be referenced in arithmetic-expression-1 of the same clause.
                    if (v.FromCtx is not null && varyings.Any(o => HasWord(v.FromCtx, o.Name)))
                        Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}' VARYING '{v.Name}': the counter shall "
                            + "not be referenced in the FROM expression of the same VARYING clause (ISO "
                            + "§13.18.64.3 SR3)");
                    // SR3 permits the counter in arithmetic-expression-2 — that leg is staged loud (the
                    // expression would need to bind against the compose-local counter).
                    if (v.ByCtx is not null && varyings.Any(o => HasWord(v.ByCtx, o.Name)))
                        Edition.Error(DiagnosticCatalog.ReportVaryingCounterInExpression, $"RD '{model.Name}' VARYING '{v.Name}': a "
                            + "VARYING counter referenced inside the BY expression (ISO §13.18.64.3 SR3) is not "
                            + "yet implemented");
                }
            }

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
                && !usageText.Equals(inheritedUsage, StringComparison.OrdinalIgnoreCase))
                Edition.Error(DiagnosticCatalog.ReportUsageNotDisplayOrNational, $"RD '{model.Name}' entry "
                    + $"'{entryName ?? "FILLER"}': USAGE {usageText} contradicts the USAGE {inheritedUsage} "
                    + "written on the group entry above it — when a USAGE clause is written on a group item and "
                    + "on an entry subordinate to it, the same usage shall be specified in both (ISO "
                    + "§13.18.60.3 SR2, imported into the report group description entry by §13.15.4 GR2)");

            if (lineOperand is { } lop)
                opened = RepeatedLine(lop, lineOperandIndex, group.Lines.Count == 0, anchorKey, st);
            if (opened is not null)
            {
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

            // A SUM entry establishes a counter whether or not it is printable (§13.18.54.4 GR1/GR3); its FULL
            // chain governs the GR10 print/reset suppression (§13.18.41.4 GR3g).
            ReportSumModel? sum = null;
            if (sumClauses.Count > 0)
            {
                sum = BindSumClause(sumClauses, entryName, picText, group, model, columns.Count > 0);
                foreach (var (_, c, _, _) in chain) if (c is not null) sum.PresentWhenCtxs.Add(c);
                if (ownCond is not null) sum.PresentWhenCtxs.Add(ownCond);
            }

            if (columns.Count > 0)
            {
                int col = columns[0].Value;
                if (line is null)
                {
                    Edition.Error(DiagnosticCatalog.ReportColumnWithoutLine, $"RD '{model.Name}': a COLUMN clause with no LINE clause in "
                        + "effect (ISO §13.18.14 — a printable item belongs to a report line)");
                    chain.Add((level, ownCond, EntryRepetitions(columns, ownOccurs), usageText ?? inheritedUsage));
                    return;
                }
                // ⛔ NO OPERAND, NO PRINTABLE ITEM (kb/Work PB853). An entry with a COLUMN clause and no SOURCE,
                // VALUE or SUM operand is either an ISO §13.15.3 SR10 violation ScreenReportEntryClausePresence
                // already refused, or one whose written operands were each refused at their own clause. A
                // figurative SPACE sender used to be FABRICATED here in place of the missing operand — the
                // compiler inventing source the programmer did not write — which printed `000` under PIC 999.
                if (sumClauses.Count == 0 && sourceOps.Count == 0 && valueRaws.Count == 0)
                {
                    chain.Add((level, ownCond, EntryRepetitions(columns, ownOccurs), usageText ?? inheritedUsage));
                    return;
                }
                // The printable item (§13.18.14): a SYNTHETIC DataItem carrying the PICTURE so the emitter's ONE
                // MOVE conversion path renders the §13.18.53.4 GR1 implicit MOVE. A printable item is a
                // USAGE-DISPLAY elementary item; its numeric face stores its character IMAGE (StoreAsImage).
                string itemWhere = $"RD '{model.Name}' printable item '{entryName ?? "FILLER"}'";
                Usage itemUsage = PictureAnalyzer.ParseUsage(usageText ?? inheritedUsage, Edition, itemWhere);
                var pic = picText is not null
                    ? PictureAnalyzer.Analyze(picText, itemUsage, Edition,
                        itemWhere, ownSign, currencies: CurrencySigns, blankWhenZero: blankWhenZero, editing: reportEditing,
                        localeFormat2: reportLocale, decimalPointIsComma: DecimalPointIsComma)
                    // ⛔ ISO §13.15.3 SR14 — THE REPORT GROUP ENTRY'S OWN VALUE-IMPLIED PICTURE, word for word the
                    // §13.16.3 SR9 rule this compiler synthesizes for a data description entry: "The PICTURE clause
                    // may be omitted for an elementary item when an alphanumeric, boolean or national literal that
                    // is not a zero-length literal is specified in the VALUE clause.  A PICTURE clause is implied as
                    // follows: a) If the literal is alphanumeric, 'PICTURE X(length)' b) If the literal is boolean,
                    // 'PICTURE 1(length)' c) If the literal is national, 'PICTURE N(length)' where length is the
                    // length of the literal as specified in 8.3.3, Literals."
                    // ONE classifier for both formats (DataBinder.ImpliedPicture.cs) — the two-arm shape is this
                    // repo's most reproducible defect, and this WAS that shape: `02 COLUMN 1 VALUE "HELLO".` is
                    // legal source the report binder REJECTED, while its data-division twin now compiles.
                    // The CONTEXT §8.3.3.6.4 GR1/GR4 ask about is this entry's own usage — a report group entry
                    // is not subject to §13.18.60.4 GR1 inheritance from a data-division group, and §13.18.60.3
                    // SR7 admits only DISPLAY or NATIONAL here anyway, so GR4's boolean context cannot arise.
                    // ⛔ THE SINGULAR "the literal": SR14 fixes `length` from THE literal, so the implied
                    // PICTURE exists only for a clause that supplies exactly ONE operand. A multi-operand
                    // format-4 VALUE clause (§13.18.63.2, kb/Work PB506) implies none — the standard names no
                    // rule for which of several literals fixes the ONE description all the repetitions share,
                    // and taking the first would silently truncate every longer one. Such an entry reaches the
                    // SR12 diagnostic below, never a guess (DETERMINATION, kb/Work PB504 × PB506).
                    : valueRaws is [{ } reportValue]
                        && Sr9ImpliedFor(reportValue, itemUsage) is { } implied
                    ? ImpliedReportPicture(implied, itemUsage, usageText is not null, ownSign, itemWhere)
                    : null;
                if (pic is null)
                {
                    Edition.Error(DiagnosticCatalog.ReportItemMissingPicture, $"RD '{model.Name}': printable item at COLUMN {col} has no "
                        + "PICTURE clause — one shall be specified in every elementary entry that has a SOURCE or "
                        + "SUM clause (ISO §13.15.3 SR12), and SR14 implies one only from a VALUE clause supplying "
                        + "an alphanumeric, boolean or national literal that is not a zero-length literal");
                    chain.Add((level, ownCond, EntryRepetitions(columns, ownOccurs), usageText ?? inheritedUsage));
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
                    ScreenReportUsage(pic.Usage, model, entryName, picText is null ? null : $"PICTURE {picText}");
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
                List<ReportFieldSource> srcs =
                    sum is not null ? [new FieldSumSource(sum.Id)]
                    : sourceOps.Count > 0 ? sourceOps
                    : [.. valueRaws.Select(r => (ReportFieldSource)new FieldValueSource(r))];
                // SOURCE naming the entry's own VARYING counter (§13.18.64.4 GR4 NOTE — a counter is a source
                // item). Per OPERAND: a multi-operand SOURCE may name a counter in any of its positions.
                for (int si = 0; si < srcs.Count; si++)
                    if (srcs[si] is FieldDataSource { Qualifiers.Count: 0 } fd
                        && varyings.FindIndex(v => v.Name.Equals(fd.Name, StringComparison.OrdinalIgnoreCase)) is var vi and >= 0)
                        srcs[si] = new FieldVaryingSource(vi);
                var field = new ReportFieldModel
                {
                    Columns = RepeatedPlacements(columns, anchorKey, st),
                    PrintItem = item,
                    Sources = srcs,
                    GroupIndicate = groupIndicate,
                    RepetitionOrdinal = st.Placements.GetValueOrDefault(anchorKey),
                };
                st.Placements[anchorKey] = field.RepetitionOrdinal + field.Columns.Count;
                // The field-local chain: conditions strictly BELOW the line entry (its own chain gates the line).
                for (int ci = Math.Min(st.LineChainDepth, chain.Count); ci < chain.Count; ci++)
                    if (chain[ci].Cond is { } c) field.PresentWhenCtxs.Add(c);
                if (ownCond is not null && opened is null) field.PresentWhenCtxs.Add(ownCond);
                field.Varyings.AddRange(varyings);
                field.RepetitionGuards.AddRange(st.GuardsHere());
                line.Fields.Add(field);
            }

            chain.Add((level, ownCond, EntryRepetitions(columns, ownOccurs), usageText ?? inheritedUsage));
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
    private static ReportLineModel RepeatedLine(
        Core.ReportLineOperandContext op, int operand, bool groupFirstLine, Core.ReportGroupEntryContext anchorKey,
        ReportGroupBuild st)
    {
        bool nextPage = groupFirstLine && op.NEXT() is not null;
        bool relative = op.reportRelativeSign() is not null || op.integerLiteral() is null;
        int value = op.integerLiteral() is { } lit ? CobolNet.Validation.IntegerOperandRules.HostValue(lit) : BareNextPageInterval;
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
                    && lc.reportLineOperand().Any(o => o.reportRelativeSign() is null && o.integerLiteral() is not null))
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
    private static int ReportEntryLineSpan(Core.ReportGroupEntryContext[] entries, int self, int subtreeEnd)
    {
        int relativeSpan = 1, absoluteLow = int.MaxValue, absoluteHigh = int.MinValue, lines = 0;
        bool anyRelative = false, anyAbsolute = false;
        for (int k = self; k < subtreeEnd; k++)
            foreach (var clause in entries[k].reportGroupClause())
            {
                if (clause.reportLineClause() is not { } lc) continue;
                foreach (var op in lc.reportLineOperand())
                {
                    if (op.integerLiteral() is not { } lit) continue;
                    int v = CobolNet.Validation.IntegerOperandRules.HostValue(lit);
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
            return t.GetText().Equals(word, StringComparison.OrdinalIgnoreCase);
        for (int i = 0; i < tree.ChildCount; i++)
            if (HasWord(tree.GetChild(i), word)) return true;
        return false;
    }

    /// <summary>THE ONE TYPE READER: the report group type a TYPE clause names (ISO §13.18.57 Format 2 + the SR9
    /// abbreviations). <see cref="BindGroupType"/> and the flat-entry screens that need the TYPE before the group is
    /// built (<see cref="ScreenReportLineClauses"/>) both read it here.</summary>
    private static ReportGroupKindModel GroupKindOf(Core.ReportGroupTypeContext t) =>
        t.RH() is not null || (t.REPORT() is not null && t.HEADING() is not null) ? ReportGroupKindModel.ReportHeading
        : t.PH() is not null || (t.PAGE() is not null && t.HEADING() is not null) ? ReportGroupKindModel.PageHeading
        : t.CH() is not null || (t.CONTROL() is not null && t.HEADING() is not null) ? ReportGroupKindModel.ControlHeading
        : t.DE() is not null || t.DETAIL() is not null ? ReportGroupKindModel.Detail
        : t.CF() is not null || (t.CONTROL() is not null && t.FOOTING() is not null) ? ReportGroupKindModel.ControlFooting
        : t.PF() is not null || (t.PAGE() is not null && t.FOOTING() is not null) ? ReportGroupKindModel.PageFooting
        : ReportGroupKindModel.ReportFooting;

    /// <summary>The group type a level-01 entry's written TYPE clause names, for the flat-entry screens that run
    /// before the group is built (<see cref="ScreenReportLineClauses"/>, <see cref="ScreenReportEntryClausePresence"/>):
    /// through <see cref="GroupKindOf"/>, and <see cref="ReportGroupKindModel.Detail"/> — the group model's own
    /// default — when the entry writes none.</summary>
    private static ReportGroupKindModel WrittenGroupKind(Core.ReportGroupEntryContext ge) =>
        ge.reportGroupClause().Select(c => c.reportTypeClause()?.reportGroupType())
            .LastOrDefault(t => t is not null) is { } t ? GroupKindOf(t) : ReportGroupKindModel.Detail;

    /// <summary>Map a TYPE clause (ISO §13.18.57 Format 2 + the SR9 abbreviations) onto the group model,
    /// capturing the CH/CF control operand.</summary>
    private void BindGroupType(Core.ReportGroupTypeContext t, ReportGroupModel group, ReportModel model)
    {
        group.Kind = GroupKindOf(t);

        if (group.Kind is ReportGroupKindModel.ControlHeading or ReportGroupKindModel.ControlFooting)
        {
            if (t.FINAL() is not null) group.ControlFinal = true;
            else if (t.dataReference() is { } dref)
                group.ControlOperand = ControlOperandRef(dref, DiagnosticCatalog.ReportControlTypeOperand,
                    $"RD '{model.Name}': TYPE {(group.Kind == ReportGroupKindModel.ControlHeading ? "CH" : "CF")} operand",
                    "ISO §13.18.57.3 SR10");
            // Omitted operand: legal only with a one-operand CONTROL clause (§13.18.57.3 SR11) — resolved later.
        }
        // PH/PF require a PAGE clause (§13.18.57.3 SR12).
        if (group.Kind is ReportGroupKindModel.PageHeading or ReportGroupKindModel.PageFooting && !model.Paged)
            Edition.Error(DiagnosticCatalog.ReportPageTypeRequiresPage, $"RD '{model.Name}': TYPE {group.Kind} requires a PAGE clause that "
                + "defines the page limit (ISO §13.18.57.3 SR12)");
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
        List<(int Level, Core.ConditionContext? Cond, int Reps, string? Usage)> chain)
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
        if (!ops.Any(o => BareReferenceOf(o) is null)) return;   // every operand is identifier-1 — SR7 is silent
        foreach (var o in ops.Where(o => !IsParenthesized(o)))
            Edition.Error(DiagnosticCatalog.ReportSourceOperandParens, $"{where}: the SOURCE clause has "
                + $"{ops.Length} operands of which at least one is an arithmetic-expression, so each operand "
                + $"shall be enclosed in parentheses (ISO §13.18.53.3 SR7); '{o.GetText()}' is not.");
    }

    /// <summary>Bind ONE SOURCE clause operand (ISO §13.18.53.2 — the clause writes
    /// `{ identifier-1 | arithmetic-expression-1 } …`): a LINE-COUNTER/PAGE-COUNTER register (§8.4.3.15 SR1 — the
    /// only report-section reference position), a data reference captured as base + qualifiers (GR1's implicit
    /// MOVE), or — for an expression operand, or an identifier under the clause's ROUNDED phrase (SR5) — a
    /// <see cref="FieldComputeSource"/> carrying GR2's implicit COMPUTE. Subscripted / reference-modified
    /// IDENTIFIER operands stage loud (no corpus surface); inside an expression they are ordinary identifiers and
    /// the one expression binder resolves them.</summary>
    private ReportFieldSource? BindSourceOperand(Core.ReportValueOperandContext op,
        Core.RoundedPhraseContext? rounded, ReportModel model)
    {
        // §13.18.53.3 SR5 makes an identifier-1 written WITH the ROUNDED phrase an arithmetic-expression, so the
        // two forms merge here and GR2's COMPUTE governs both (kb/Work PB852).
        if (BareReferenceOf(op) is not { } dref || rounded is not null)
            return new FieldComputeSource(op, AsWritten(op)) { Rounded = rounded };
        return BindSourceReference(dref, model);
    }

    /// <summary>The identifier-1 arm of <see cref="BindSourceOperand"/> — §13.18.53.4 GR1's implicit MOVE.</summary>
    private ReportFieldSource? BindSourceReference(Core.DataReferenceContext dref, ReportModel model)
    {
        if (dref.LINE_COUNTER() is not null || dref.PAGE_COUNTER() is not null)
        {
            // A report-name qualifier naming a DIFFERENT report's counter is legal (§8.4.3.15 SR2) — staged.
            if (dref.cobolWord() is { } q && !q.GetText().Equals(model.Name, StringComparison.OrdinalIgnoreCase))
                Edition.Error(DiagnosticCatalog.ReportSourceOtherReportCounter, $"RD '{model.Name}': SOURCE {DataBinder.WrittenText(dref)} — a counter of "
                    + "another report (ISO §8.4.3.15.3 SR2) is not yet implemented");
            return new FieldCounterSource(dref.PAGE_COUNTER() is not null);
        }
        foreach (var sfx in dref.dataReferenceSuffix())
            if (sfx.subscriptPart() is not null || sfx.refModPart() is not null)
            {
                Edition.Error(DiagnosticCatalog.ReportSourceSubscripted, $"RD '{model.Name}': SOURCE {DataBinder.WrittenText(dref)} — a subscripted or "
                    + "reference-modified SOURCE operand (ISO §13.18.53) is not yet implemented");
                return null;
            }
        var (b, qls) = KeyReference(dref);
        return new FieldDataSource(b, qls);
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

    /// <summary>Bind ONE ENTRY's SUM clause (ISO §13.18.54) into a <see cref="ReportSumModel"/>: the counter id
    /// (the entry's data-name, GR5, else synthesized), the addend TERMS, their UPON operands, and the RESET
    /// operand. The counter's scale derives from the entry's PICTURE (GR1).
    /// <para>⛔ IT TAKES EVERY <c>SUM …</c> GROUP OF THE ENTRY, not one (kb/Work PB482). §13.18.54.3 SR1 — "The
    /// whole clause is referred to as a SUM clause even though the SUM keyword may appear more than once" — and
    /// §13.18.54.4 GR1 establishes ONE counter per ENTRY, so the groups are terms of a single counter and each
    /// keeps its OWN UPON list (GR7c2 attaches the phrase to its group).</para></summary>
    private ReportSumModel BindSumClause(IReadOnlyList<Core.ReportSumClauseContext> clauses,
        string? entryName, string? picText, ReportGroupModel group, ReportModel model, bool hasColumn)
    {
        // Scale-derivation analysis (GR1) — threads the edition + the program currency symbol like every other
        // Analyze site (a custom §12.3.7 currency symbol in a SUM counter's PICTURE must classify, not error).
        string sumWhere = $"RD '{model.Name}' SUM counter '{entryName ?? "FILLER"}'";
        var pic = picText is not null
            ? PictureAnalyzer.Analyze(picText, Usage.Display, Edition, sumWhere, currencies: CurrencySigns,
                decimalPointIsComma: DecimalPointIsComma)
            : null;
        // ⛔ THE COUNTER'S SCALE IS THE ENTRY PICTURE'S RECEIVER SCALE, NOT `pic.Scale` (kb/Work PB1685). §13.18.54.4 GR1
        // derives the counter's digits — integral AND fractional — from the entry's PICTURE excluding insertion editing
        // characters, and a NUMERIC-EDITED PicInfo carries Scale 0 (its fraction lives in the mask), so `PIC 99.99 SUM
        // WS-F` registered a scale-0 counter and truncated every addend to an integer (2.75 twice printed 04.00 for the
        // 05.50 owed). The one rule every store already asks (PicInfo.ReceiverScale) answers it.
        int sumScale = pic?.ReceiverScale(DecimalPointIsComma) ?? 0;
        var sum = new ReportSumModel
        {
            // §13.18.54.4 GR1 — one counter per ENTRY: the identity is this entry's ordinal in the report
            // description, and GR5's data-name rides alongside as the counter's NAME (kb/Work PB882).
            Id = model.Sums.Count,
            Name = entryName,
            Scale = sumScale,
            PrintedIn = group,
            // The counter AS A DATA ITEM (GR1) — the implicitly-defined register a procedure division reference
            // resolves to (GR5 names it, GR12 permits altering it). Off ByName/Roots, exactly like the OCCURS
            // DYNAMIC CAPACITY register: its value IS the engine's, so it allocates no storage.
            Register = new DataItem
            {
                Level = 49,
                DeclaredAt = Edition.Cursor,
                CobolName = entryName,
                CsName = NamingConvention.SumCounterName(model.Name, model.Sums.Count),
                Pic = PicInfo.SumCounterItem(pic is null ? 18 : SumCounterDigits(pic), sumScale),
                Uid = _uidCounter++,
            },
            // Preserve a floating-point-edited / national-edited PICTURE gate for the post-bind GateData report-Sums
            // walk (this PicInfo is otherwise discarded — only Scale is used — so the 0900 would drop; DEVLOG 740).
            SkeletonGate = pic is null ? null : CobolNet.Validation.VersionConformancePass.PictureConstructId(pic),
            SkeletonWhere = sumWhere,
        };
        bool resetSeen = false, roundedSeen = false;
        foreach (var sm in clauses)
        {
            var term = new ReportSumTerm();
            foreach (var op in sm.reportValueOperand())
                term.Addends.Add(SumAddendRef(op, model));
            // UPON data-name-2 (SR7) — the WHOLE written reference: the one qualifier the rule allows is a
            // report-name, and §8.4.3.3.3 SR5's NOTE bars a ref-mod wherever a general format writes
            // data-name-n. Resolution waits for ResolveReports (a detail may be described after this entry).
            foreach (var up in sm.dataReference())
                if (UponDetailRef(up, model) is { } det) term.Upon.Add(det);
            sum.Terms.Add(term);
            // §13.18.54.2 — the rounded-phrase sits OUTSIDE the repeated SUM … UPON group (PDF p487 rendered),
            // so an entry has at most one, however many times the SUM keyword appears (SR1). It governs
            // §13.18.54.4 GR4's delivery of the counter to the printable item, which is why SR3 requires the
            // COLUMN clause that defines that item (kb/Work PB852's sibling sweep).
            if (sm.roundedPhrase() is { } rnd)
            {
                if (roundedSeen)
                    Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}': the SUM clause of "
                        + $"'{entryName ?? "FILLER"}' writes more than one ROUNDED phrase; the general format admits "
                        + "one rounded-phrase for the whole clause (ISO §13.18.54.2)");
                else if (!hasColumn)
                    Edition.Error(DiagnosticCatalog.ReportSumRoundedWithoutColumn, $"RD '{model.Name}' entry "
                        + $"'{entryName ?? "FILLER"}': the SUM clause writes a ROUNDED phrase, which is permitted "
                        + "only if the COLUMN clause is specified for the subject of the entry (ISO §13.18.54.3 "
                        + "SR3) — the phrase governs §13.18.54.4 GR4's transfer of the sum counter to the "
                        + "printable item, and this entry defines none.");
                else
                    sum.Rounded = rnd;
                roundedSeen = true;
            }
            if (sm.reportSumReset() is not { } reset) continue;
            // §13.18.54.2 — the RESET phrase sits OUTSIDE the repeated SUM … UPON group, so an entry has at
            // most one. (Reachable only once the SUM keyword repeats, which SR1 permits.)
            if (resetSeen)
            {
                Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}': the SUM clause of '{entryName ?? "FILLER"}' "
                    + "writes more than one RESET phrase; the general format admits one RESET phrase for the "
                    + "whole clause (ISO §13.18.54.2)");
                continue;
            }
            resetSeen = true;
            if (reset.FINAL() is not null) sum.ResetFinal = true;
            else if (reset.dataReference() is { } rref)
                sum.ResetOperand = ControlOperandRef(rref, DiagnosticCatalog.ReportResetNotControlOperand,
                    $"RD '{model.Name}': SUM … RESET ON operand", "ISO §13.18.54.3 SR8");
        }
        model.Sums.Add(sum);
        // §13.18.54.4 GR5 — a data-name immediately after the level number names THE COUNTER. Publish it into
        // the source element's name space so GR12's permission to read or alter it can be exercised; the entry
        // keeps its own counter whether or not another entry spells its name the same way (GR1, kb/Work PB882).
        if (entryName is not null)
        {
            if (!_sumCounters.TryGetValue(entryName, out var homonyms))
                _sumCounters[entryName] = homonyms = [];
            homonyms.Add((model, sum));
        }
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
        if (BareReferenceOf(op) is not { } dref)
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

    /// <summary>Post-build resolution for every report (the <c>ResolveFiles</c> pattern — runs after the storage
    /// forest is complete): the owning FILE (§13.18.46), SOURCE / CONTROL / SUM-addend data items, CH/CF control
    /// levels (§13.18.57.3 SR10/SR11), RESET levels, and the report's line width.</summary>
    internal void ResolveReports()
    {
        foreach (var model in Reports)
        {
            // The owning file: the FD whose REPORT(S) clause names this report (ISO §13.18.46.4 GR1; §13.14
            // requires each report-name be named in exactly one REPORT clause of an FD).
            model.File = Files.FirstOrDefault(f =>
                f.ReportNames.Any(rn => rn.Equals(model.Name, StringComparison.OrdinalIgnoreCase)));
            if (model.File is null)
                Edition.Error(DiagnosticCatalog.ReportNotInFile, $"RD '{model.Name}' is not named in any file description entry's "
                    + "REPORT clause (ISO §13.18.46 / §13.14)");
            else if (model.File.ReportNames.Count > 1)
                Edition.Error(DiagnosticCatalog.ReportMultipleOnFile, $"file '{model.File.CobolName}': multiple reports on one file "
                    + "(REPORTS ARE …, ISO §13.18.46) are not yet implemented");

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
                    // The BOUNDS are deliberately NOT screened here: §8.4.3.3.4 item 5c defines an out-of-range
                    // slice as the EC-BOUND-REF-MOD condition, and the emitted RefModPlace raises it — a general
                    // rule, not a syntax rule, so a compile-time rejection would be this compiler's invention.
                    else if (op.RefModStart is not null && ctl.Item is not null
                             && ReferenceResolver.RefModExclusion(ctl.Item) is { } why)
                        Edition.Error(DiagnosticCatalog.RefModIdentifierNotPermitted, $"RD '{model.Name}': CONTROL operand '{op}': reference "
                            + $"modification of {why} is not permitted (ISO §8.4.3.3.3 SR1)");
                }

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
                                case FieldDataSource ds:
                                    ds.Item = LookupQualified(ds.Name, ds.Qualifiers, $"RD '{model.Name}': SOURCE", out bool srcAmbiguous);
                                    if (ds.Item is null && !srcAmbiguous)
                                        Edition.Error(DiagnosticCatalog.ReportSourceOperandUnresolved, $"RD '{model.Name}': SOURCE '{ds.Name}' does not "
                                            + "resolve to a data item (ISO §13.18.53.3 SR4)");
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

            foreach (var sum in model.Sums)
            {
                foreach (var term in sum.Terms)
                {
                    foreach (var addend in term.Addends) ResolveSumAddend(addend, model);
                    foreach (var det in term.Upon) ResolveUponDetail(det, model);
                }
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

            // VARYING SR2 (§13.18.64.3): data-name-1 shall not be defined elsewhere in the source element.
            foreach (var g in model.Groups)
                foreach (var ln in g.Lines)
                    foreach (var f in ln.Fields)
                        foreach (var v in f.Varyings)
                            if (ByName.ContainsKey(v.Name))
                                Edition.Error(DiagnosticCatalog.ReportGroupClauseRule, $"RD '{model.Name}' VARYING '{v.Name}': the counter "
                                    + "data-name shall not be defined elsewhere in the source element (ISO "
                                    + "§13.18.64.3 SR2)");

            // Line width: the FD's fixed RECORD CONTAINS, else the widest NOMINAL field extent — absolute
            // operands at column + width − 1; relative (PLUS) operands walked against the line's horizontal
            // counter with every item present (§13.18.14.4 GR7–GR9; presentation-time absence only SHRINKS the
            // occupied extent, so the all-present walk is the width bound).
            int widest = 1;
            foreach (var g in model.Groups)
                foreach (var ln in g.Lines)
                {
                    int hc = 0;
                    // The step anchors of a repeating entry's placements (§13.18.38.4 GR12) — the same registers
                    // the compose method keeps, walked here so the width bound sees every repetition.
                    var anchors = new Dictionary<int, int>();
                    foreach (var f in ln.Fields)
                        foreach (var spec in f.Columns)
                        {
                            int left = spec.Kind switch
                            {
                                ReportColumnKindModel.Absolute => spec.Value,
                                ReportColumnKindModel.Relative => hc + spec.Value,
                                ReportColumnKindModel.AnchorSeed => anchors[spec.AnchorId] = hc + spec.Value,
                                _ => anchors.GetValueOrDefault(spec.AnchorId) + spec.Value,
                            };
                            hc = left + f.PrintItem.DisplayTextWidth - 1;   // GR9 — the rightmost column becomes the counter
                            widest = Math.Max(widest, hc);
                        }
                }
            model.LineWidth = model.File?.RecordContains ?? widest;
        }
    }

    /// <summary>⛔ THE ONE ARM CHOICE FOR A SUM ADDEND (kb/Work PB482). ISO §13.18.54.3 SR1 admits three addend
    /// forms and the arms differ by WHERE the operand is defined, so the choice is made once, here, after the
    /// whole storage forest and every report description exist:
    /// <list type="number">
    /// <item>SR4's <c>data-name-1</c> — "the name of a numeric data item IN THE REPORT SECTION" (a rolled total,
    /// §13.18.54.4 GR6). The accumulation chain it needs is staged loud, never silently dropped.</item>
    /// <item>SR4 g)'s cross-report form — the operand qualified by a REPORT-name; also staged.</item>
    /// <item>SR5's <c>identifier-1</c> — "it shall specify a numeric data item NOT defined in the report
    /// section". Both halves of that sentence are screened: resolution against ordinary storage (report-section
    /// names never enter <see cref="DataBinder.ByName"/>) and the CATEGORY, which nothing checked before —
    /// <c>SUM WS-TXT</c> over a <c>PIC X(6)</c> holding "123456" totalled 123456 per GENERATE, silently.</item>
    /// </list>
    /// The SUBSCRIPT is deliberately absent from this method: the arm choice is about the base name, and the
    /// subscript is evaluated by <c>ExpressionBinder</c> in the procedure phase off <see cref="ReportSumAddend.Ctx"/>.</summary>
    private void ResolveSumAddend(ReportSumAddend addend, ReportModel model)
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
        // SR4's data-name-1 — a report-section item. `IsReportSectionOnlyName` is the SAME set §13.18.16.3 SR2
        // and §13.15.3 SR16 use (built once, kb/Work PB205): a name ALSO declared in ordinary storage resolves
        // THERE (§8.4.2.1) and is an SR5 identifier-1, not an SR4 data-name-1.
        if (IsReportSectionOnlyName(addend.Name))
        {
            Edition.Error(DiagnosticCatalog.ReportSumRolledTotal, $"RD '{model.Name}': SUM addend '{addend.Written}' names a report "
                + "section data item (data-name-1 — a rolled total, ISO §13.18.54.3 SR4 / §13.18.54.4 GR6) — "
                + "not yet implemented");
            return;
        }
        // SR4 g) — "If data-name-1 specifies an entry in a different report description". The qualifier that
        // says so is a REPORT-name (§8.4.2.2.2 Format 1), and `dataReference` swallows it as an ordinary IN/OF
        // qualification, so the spelling is recognised HERE rather than at `sumOperand`'s own `OF reportName`
        // alternative, which the qualification tail makes unreachable.
        if (addend.Qualifiers.Count == 1
            && Reports.Any(r => r.Name.Equals(addend.Qualifiers[0], StringComparison.OrdinalIgnoreCase)))
        {
            Edition.Error(DiagnosticCatalog.ReportSumCrossReport, $"RD '{model.Name}': SUM addend '{addend.Written}' names an entry of "
                + $"report '{addend.Qualifiers[0]}' (a cross-report sum, ISO §13.18.54.3 SR4 g) — not yet "
                + "implemented");
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
        // A detail of ANOTHER report: GR7 c) 2) accumulates on a GENERATE of that detail, which runs on the
        // other report's engine — a cross-report wiring this backend does not have. Staged with the family.
        if (!ReferenceEquals(owner, model))
        {
            Edition.Error(DiagnosticCatalog.ReportSumUponCrossReport, $"{where} names a detail of report '{owner!.Name}' (ISO "
                + "§13.18.54.4 GR7 c) 2) — a cross-report UPON) — not yet implemented");
            return;
        }
        det.Detail = group;
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
    /// names). Token-level scan over each distinct captured condition context.</summary>
    private void CheckConditionOperands(ReportModel model)
    {
        var conds = new HashSet<Core.ConditionContext>(ReferenceEqualityComparer.Instance);
        foreach (var g in model.Groups)
            foreach (var ln in g.Lines)
            {
                foreach (var c in ln.PresentWhenCtxs) conds.Add(c);
                foreach (var f in ln.Fields)
                    foreach (var c in f.PresentWhenCtxs) conds.Add(c);
            }
        foreach (var s in model.Sums)
            foreach (var c in s.PresentWhenCtxs) conds.Add(c);
        if (conds.Count == 0) return;

        // A name also declared in ordinary storage resolves THERE (never to the report item), so it is not an
        // SR16 reference — only report-section-exclusive names are scanned (no textual false positives). The SAME
        // set answers §13.18.16.3 SR2 for a CONTROL operand, so it is built in ONE place (kb/Work PB205).
        var names = ReportSectionOnlyNames(model);

        foreach (var cond in conds)
        {
            if (HasToken(cond, CobolLexer.LINE_COUNTER) || HasToken(cond, CobolLexer.PAGE_COUNTER))
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
                if (n.Equals(name, StringComparison.OrdinalIgnoreCase)) return true;
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
        if (node is Core.DataReferenceContext dref)
        {
            if (dref.LINE_COUNTER() is not null || dref.PAGE_COUNTER() is not null)
                // A report counter is admissible only in a SOURCE clause, and only the CURRENT report's
                // (§8.4.3.15.3 SR1/SR2 — an unqualified counter is the own report's).
                return countersOf is not null
                    && (dref.cobolWord() is not { } q
                        || q.GetText().Equals(countersOf.Name, StringComparison.OrdinalIgnoreCase))
                    ? null : dref.GetText();
            if (dref.cobolWord()?.GetText() is { } w && IsReportSectionOnlyName(w))
            {
                // A sum counter of the CURRENT report is SR4's other admitted report-section item.
                bool ownCounter = countersOf is not null
                    && countersOf.Sums.Any(s => s.Name is { } sn && sn.Equals(w, StringComparison.OrdinalIgnoreCase));
                return ownCounter ? null : dref.GetText();
            }
            return null;
        }
        for (int i = 0; i < node.ChildCount; i++)
            if (ReportSectionNameIn(node.GetChild(i), countersOf) is { } bad) return bad;
        return null;
    }

    /// <summary>The report-section-exclusive names of one report: its group names, its printable entries' names
    /// and its sum counters, each only when <see cref="ByName"/> does not also carry it (see
    /// <see cref="IsReportSectionOnlyName"/> for why).</summary>
    private List<string> ReportSectionOnlyNames(ReportModel model)
    {
        var names = new List<string>();
        foreach (var g in model.Groups)
        {
            if (g.Name is { } gn && !ByName.ContainsKey(gn)) names.Add(gn);
            foreach (var ln in g.Lines)
                foreach (var f in ln.Fields)
                    if (f.PrintItem.CobolName is { } fn && !ByName.ContainsKey(fn)) names.Add(fn);
        }
        // A sum counter contributes the name GR5 gives it (its identity is the ENTRY — kb/Work PB882 — but this
        // set is about NAMES: the §13.18.16.3 SR2 / §13.15.3 SR16 "declared in the report section" question).
        foreach (var s in model.Sums) if (s.Name is { } sn && !ByName.ContainsKey(sn)) names.Add(sn);
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
