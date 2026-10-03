// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>
/// The Report Writer half of the Roslyn backend (ISO/IEC 1989:2023 §13.14–§13.18 / §14.9.16/.21/.46;
/// COBOLNET_REPORT_WRITER_DESIGN §4; P7 Step 9f — a real collaborator over the per-unit
/// <see cref="EmitContext"/>): per report an engine instance field (<c>__RPT_n</c>, a <c>CobolReport</c>),
/// constructed in <c>__Activate</c> alongside the file registration; per report LINE one generated COMPOSE
/// method invoked by the engine at presentation time. Every printable item renders through the orchestrator's
/// ONE MOVE conversion (<c>ConvertSource</c>) — which IS §13.18.53.4 GR1's "SOURCE specifies the sending operand
/// of an implicit MOVE statement to the printable item" (PIC-governed alignment/editing; the legacy's byte-copy
/// content bugs cannot recur by construction). No byte plans, no registration kinds — the typed-native singular
/// pattern.
/// </summary>
internal sealed class ReportWriterEmitter(
    EmitContext ctx, NumericRenderer num, ReferenceResolver refs, MoveEmitter move, ConditionRenderer cond,
    DispatchState dispatch)
{
    /// <summary>The DATA DIVISION emitter of THIS unit, built once on first use — the report lane reaches it for
    /// exactly one thing: <see cref="DataEmitter.ValueImageOf"/>, the ONE §13.18.63 VALUE recipe a format-4
    /// operand is initialized by (kb/Work PB506). Lazy and cached because a compose method asks once per
    /// printable item per repetition, and a DataEmitter builds a PhysicalModel and its slicer/codec collaborators.</summary>
    private DataEmitter? _data;
    private DataEmitter Data => _data ??= new DataEmitter(ctx);

    /// <summary>Emit the per-report class members: the engine field, the NumProfile statics of the numeric
    /// printable items (synthetic items live outside the storage forest, so <c>FieldEmitter.EmitProfiles</c>
    /// never sees them), and the per-line compose methods.</summary>
    public void EmitReportMembers(CodeWriter w)
    {
        var reports = ctx.Data.Reports;
        if (reports.Count == 0) return;
        w.Line();
        foreach (var r in reports)
            w.Line($"private CobolReport __RPT_{r.CsIndex} = null!;   // RD {r.Name} (ISO §13.14) — constructed in __Activate");
        // The NumProfile of each SUM counter's implicitly-defined register (ISO §13.18.54.4 GR1 — "a conceptual
        // data item that behaves as a data item of the category numeric"). Like a printable item, the register
        // lives OUTSIDE the storage forest, so FieldEmitter.EmitProfiles never sees it — and unlike a printable
        // item it is a RECEIVER too (GR12), so the store needs its profile emitted here (kb/Work PB840).
        // ONE profile per SUM ENTRY: a repeating entry's occurrences share the one register (kb/Work PB1271).
        foreach (var r in reports)
            foreach (var family in r.SumFamilies)
                if (family.Register.Pic is { } rpic)
                    w.Line($"private static readonly NumProfile {family.Register.ProfileName} = {rpic.ProfileInitializer(ctx.SignEncoding)};"
                        + $"   // {r.Name} sum counter {family.BaseId}{(family.Count > 1 ? $"..{family.BaseId + family.Count - 1}" : "")}"
                        + $"{(family.Name is null ? "" : $" '{family.Name}'")} (ISO §13.18.54.4 GR1)");
        // The NumProfile of each report's PAGE-COUNTER register (ISO §8.4.3.15.4 GR1 — a temporary unsigned
        // integer data item maintained per report). Same reason as the sum counter above: the register lives
        // outside the storage forest and §8.4.3.15.3 SR1 makes it a RECEIVER, so its store needs a profile
        // (kb/Work PB429).
        foreach (var r in reports)
            if (r.PageCounterRegister.Pic is { } ppic)
                w.Line($"private static readonly NumProfile {r.PageCounterRegister.ProfileName} = {ppic.ProfileInitializer(ctx.SignEncoding)};"
                    + $"   // {r.Name} PAGE-COUNTER (ISO §8.4.3.15.4 GR1)");
        foreach (var r in reports)
            foreach (var (group, gi) in r.Groups.Select((g, i) => (g, i)))
            {
                EmitPresenceProbe(r, group, gi, w);
                foreach (var (line, li) in group.Lines.Select((l, i) => (l, i)))
                {
                    // Profiles first (declaration order is irrelevant for statics, but keep them adjacent).
                    foreach (var f in line.Fields)
                        if (f.PrintItem.Pic is { Category: PicCategory.Numeric, IsFloat: false } pic)
                            w.Line($"private static readonly NumProfile {f.PrintItem.ProfileName} = {pic.ProfileInitializer(ctx.SignEncoding)};");
                    EmitCompose(r, group, gi, line, li, w);
                }
            }
    }

    /// <summary>⛔ ONE PRESENCE SNAPSHOT PER GROUP PRESENTATION (kb/Work PB1272) — the slots of one report group's
    /// conditioned entries, in a fixed order: each conditioned LINE (its PRESENT WHEN chain AND its enclosing
    /// OCCURS … DEPENDING counts, <see cref="LinePresent"/>), each conditioned printable item (its field-local
    /// chain AND its repetition guards), each conditioned SUM entry the group prints (its FULL chain). ISO
    /// §13.18.41.4 GR2 evaluates every condition-1 of the group "before the processing of any LINE clauses for the
    /// report group" and §13.18.38.4 GR13 evaluates data-name-1 "just before the processing for the first LINE
    /// clause", so the engine runs the probe ONCE, before the page-fit test, and the placement, the compose and the
    /// sum reset all read its answers. The compose used to evaluate an item's chain itself — after the page advance
    /// the group's own fit test had caused — and the engine re-ran a SUM entry's chain at the end of the group.
    /// A GROUP INDICATE condition is NOT a slot: §13.18.28.4 GR1 makes it the engine's own state, which this
    /// group's page advance legitimately re-arms (kb/Work PB1244).</summary>
    private sealed class PresencePlan
    {
        public List<string> Tests { get; } = [];
        public Dictionary<ReportLineModel, int> Lines { get; } = new(ReferenceEqualityComparer.Instance);
        public Dictionary<ReportFieldModel, int> Fields { get; } = new(ReferenceEqualityComparer.Instance);
        public Dictionary<ReportSumModel, int> Sums { get; } = new(ReferenceEqualityComparer.Instance);

        /// <summary>The slot of a conditioned entry, −1 for an unconditional one.</summary>
        public static int SlotOf<T>(Dictionary<T, int> slots, T entry) where T : notnull =>
            slots.TryGetValue(entry, out int k) ? k : -1;
    }

    private readonly Dictionary<ReportGroupModel, PresencePlan> _plans = new(ReferenceEqualityComparer.Instance);

    /// <summary>The group's <see cref="PresencePlan"/>, built once and shared by the probe, the compose methods
    /// and the construction, so the three agree on every slot number.</summary>
    private PresencePlan PlanOf(ReportModel r, ReportGroupModel group)
    {
        if (_plans.TryGetValue(group, out var plan)) return plan;
        plan = new PresencePlan();
        foreach (var l in group.Lines)
        {
            if (LinePresent(l) is { } lp) { plan.Lines[l] = plan.Tests.Count; plan.Tests.Add(lp); }
            foreach (var f in l.Fields)
                if (f.PresentWhen.Count > 0 || f.RepetitionGuards.Count > 0)
                {
                    plan.Fields[f] = plan.Tests.Count;
                    plan.Tests.Add(string.Join(" && ", [.. f.PresentWhen.Select(c => $"({cond.Render(c)})"),
                                                         .. f.RepetitionGuards.Select(RepetitionTest)]));
                }
        }
        // A SUM counter's presence is BOTH halves of §13.18.54.4 GR10 — its PRESENT WHEN chain and its occurrence's
        // OCCURS … DEPENDING tests (kb/Work PB1271): an absent occurrence's counter is neither printed nor reset.
        foreach (var s in r.Sums)
            if (ReferenceEquals(s.PrintedIn, group) && (s.PresentWhen.Count > 0 || s.RepetitionGuards.Count > 0))
            {
                plan.Sums[s] = plan.Tests.Count;
                List<string> terms = [.. s.PresentWhen.Count > 0 ? [PresentExpr(s.PresentWhen)] : (string[])[],
                                      .. s.RepetitionGuards.Select(RepetitionTest)];
                plan.Tests.Add(string.Join(" && ", terms));
            }
        _plans[group] = plan;
        return plan;
    }

    /// <summary>The per-group presence probe the engine runs once per presentation (see <see cref="PresencePlan"/>);
    /// nothing for a group with no conditioned entry.</summary>
    private void EmitPresenceProbe(ReportModel r, ReportGroupModel group, int gi, CodeWriter w)
    {
        var plan = PlanOf(r, group);
        if (plan.Tests.Count == 0) return;
        using (w.Block($"private void __RPT_P_{r.CsIndex}_{gi}(bool[] __p)   // {r.Name} {group.Kind} presence (ISO §13.18.41.4 GR2 / §13.18.38.4 GR13)"))
            for (int k = 0; k < plan.Tests.Count; k++)
                w.Line($"__p[{k}] = {plan.Tests[k]};");
    }

    /// <summary>Emit one report line's compose method: a space-filled buffer of the report's line width, each
    /// printable item placed at its COLUMN (§13.18.14) with the value the §13.18.53.4 GR1/GR3 implicit MOVE
    /// produces — evaluated when the ENGINE invokes the method, i.e. at presentation time, after LINE-COUNTER
    /// was set to this line's number (§13.18.35.4 GR6). A field-local PRESENT WHEN chain guards its placements
    /// (§13.18.41.4 GR2b — an absent item places nothing and never advances the horizontal counter, GR3f);
    /// a multiple COLUMN entry places once per operand (§13.18.14.4 GR12) with its VARYING counters stepped
    /// per repetition (§13.18.64.4 GR3); a relative (PLUS) operand places against the line's horizontal
    /// counter (§13.18.14.4 GR7/GR8/GR9).</summary>
    private void EmitCompose(ReportModel r, ReportGroupModel group, int gi, ReportLineModel line, int li, CodeWriter w)
    {
        using (w.Block($"private string __RPT_C_{r.CsIndex}_{gi}_{li}()   // {r.Name} {group.Kind} line {li + 1}"))
        {
            w.Line($"var __ln = {RuntimeApi.ReportNewLine(r.CsIndex)};");
            // The horizontal counter (§13.18.14.4 GR7 — the rightmost occupied column, 0 at line start) exists
            // only when some operand is relative; every placed item then updates it (GR9).
            bool needsHc = line.Fields.Any(f => f.Columns.Any(c => c.Relative));
            if (needsHc) w.Line("int __hc = 0;   // the §13.18.14.4 GR7 horizontal counter");
            // The step anchors of this line's repeating entries (§13.18.38.4 GR12): each holds the base column of
            // ONE printable placement, written by its first repetition and read (never rewritten) by the rest, so
            // the displacement Σ ordinal × integer-3 lands on the column the item occupies in repetition 0.
            foreach (var a in line.Fields.SelectMany(f => f.Columns)
                         .Where(c => c.Kind == ReportColumnKindModel.AnchorSeed)
                         .Select(c => c.AnchorId).Distinct().Order())
                w.Line($"int __ra{a} = 0;   // §13.18.38.4 GR12 — a repeating entry's step anchor");
            foreach (var f in line.Fields)
                EmitFieldPlacements(r, gi, PresencePlan.SlotOf(PlanOf(r, group).Fields, f), f, needsHc, w);
            w.Line("return __ln.ToString();");
        }
    }

    /// <summary>Emit one printable entry's placements into the compose body (see <see cref="EmitCompose"/>).
    /// The COBOL-85 shape — one absolute operand, unconditional, no VARYING, in an all-absolute line — keeps
    /// its exact single-statement emission (the characterization-pinned text).</summary>
    /// <param name="gi">The group's index in its report — the engine's <c>ReportGroup.Index</c>.</param>
    /// <param name="presentSlot">The item's slot in the group's presence snapshot (<see cref="PresencePlan"/>),
    /// −1 when it carries neither a PRESENT WHEN chain nor a repetition guard.</param>
    private void EmitFieldPlacements(ReportModel r, int gi, int presentSlot, ReportFieldModel f, bool needsHc, CodeWriter w)
    {
        if (!needsHc && f.Columns.Count == 1 && presentSlot < 0 && !f.GroupIndicate && f.Varyings.Count == 0)
        {
            w.Line($"{RuntimeApi.ReportPlace(r.CsIndex, "__ln", f.Columns[0].AbsoluteLeftmost(f.PrintItem.DisplayTextWidth), FieldImage(r, f, 0))};");
            return;
        }
        // The placement's presence — ALL THREE suppressors §13.18.63.4 GR22 names ("a GROUP INDICATE, PRESENT
        // WHEN, or OCCURS clause with the DEPENDING phrase may suppress the appearance of the item"), in one test:
        // the PRESENT WHEN chain (§13.18.41.4 GR2b) and every enclosing repeating entry's OCCURS … DEPENDING test
        // (§13.18.38.4 GR13) as the group's presence SNAPSHOT recorded them before any LINE clause was processed
        // (kb/Work PB1272); and the GROUP INDICATE condition, which §13.18.28.4 GR1 makes "the same effect as a
        // PRESENT WHEN clause" whose condition the engine owns (GroupIndicatePresent, per detail group — kb/Work
        // PB1244). An absent item places nothing and never advances the horizontal counter, so an indicated item
        // with a relative COLUMN operand needs nothing of its own.
        string[] tests = [.. presentSlot >= 0 ? [RuntimeApi.ReportIsPresent(r.CsIndex, gi, presentSlot)] : (string[])[],
                          .. f.GroupIndicate ? [$"__RPT_{r.CsIndex}.GroupIndicatePresent"] : (string[])[]];
        using IDisposable? guard = tests.Length > 0
            ? w.Block($"if ({string.Join(" && ", tests)})   // presence (§13.18.41.4 GR2b / §13.18.28.4 GR1 / §13.18.38.4 GR13)")
            : null;
        // VARYING counters (§13.18.64.4 GR3): the first occurrence takes FROM (default 1) and "for the second and
        // subsequent occurrences, the value of arithmetic-expression-2 is added" — so occurrence n holds
        // FROM + n × BY. It is written as that CLOSED FORM over the repetition ordinal, not as an accumulator,
        // because an entry made repeating by an OCCURS clause (§13.18.38 Format 3) is REPLAYED into one field per
        // repetition and an accumulator local to a field could not span them. The two forms are equal, not
        // approximately: GR3 adds arithmetic-expression-2 itself, and both operands are landed as integers ONCE
        // (VaryValue — a noninteger value is GR5's EC-REPORT-VARYING).
        // ⛔ THE COUNTER IS AN Int128 (kb/Work PB1305). GR1: "an independent temporary integer data item that shall
        // be large enough to contain the maximum expected value" — the expression's own range, which the numeric
        // renderer widens to Int128 for any operator. The locals were `long`, so `FROM 2 * 3 - 5` was a CS0266
        // backend crash on conforming source.
        for (int k = 0; k < f.Varyings.Count; k++)
        {
            w.Line($"Int128 {VaryName(f, k)} = {VaryValue(r, f.Varyings[k], f.Varyings[k].From, "FROM")};   // VARYING {f.Varyings[k].Name} FROM (§13.18.64.4 GR3a)");
            w.Line($"Int128 {VaryName(f, k)}b = {VaryValue(r, f.Varyings[k], f.Varyings[k].By, "BY")};   // … BY (§13.18.64.4 GR3b)");
        }
        for (int rep = 0; rep < f.Columns.Count; rep++)
        {
            for (int k = 0; k < f.Varyings.Count; k++)
                w.Line($"Int128 {VaryName(f, k)}_{rep} = {VaryName(f, k)} + {f.RepetitionOrdinal + rep} * {VaryName(f, k)}b;"
                    + $"   // occurrence {f.RepetitionOrdinal + rep + 1} (§13.18.64.4 GR3)");
            var spec = f.Columns[rep];
            // The operand this repetition takes (§13.18.63.4 GR23 / §13.18.53.4 GR4 — the ONE cycling reader is
            // ReportFieldModel.SourceAt). The index is the repetition ORDINAL, so a PRESENT WHEN that suppresses
            // the item does not shift the assignment (GR23's last sentence).
            string image = FieldImage(r, f, rep);
            switch (spec.Kind)
            {
                case ReportColumnKindModel.Absolute:
                    // GR6 b)-d): LEFT, RIGHT or CENTER fixes the leftmost column from integer-1 and the printable-size
                    // (ReportColumnSpec.AbsoluteLeftmost — the ONE computation the bind-time width walk uses too).
                    w.Line($"{RuntimeApi.ReportPlace(r.CsIndex, "__ln", spec.AbsoluteLeftmost(f.PrintItem.DisplayTextWidth), image)};");
                    if (needsHc) w.Line($"__hc = {spec.AbsoluteRightmost(f.PrintItem.DisplayTextWidth)};   // §13.18.14.4 GR9");
                    break;
                case ReportColumnKindModel.Relative:
                    w.Line($"__hc += {spec.Value};   // §13.18.14.4 GR8 — leftmost = horizontal counter + integer-2");
                    w.Line($"{RuntimeApi.ReportPlace(r.CsIndex, "__ln", "__hc", image)};");
                    w.Line($"__hc += {f.PrintItem.DisplayTextWidth - 1};   // §13.18.14.4 GR9");
                    break;
                case ReportColumnKindModel.AnchorSeed:
                    // Repetition 0 of a STEP'd repeating entry whose COLUMN operand is relative: place as GR8
                    // says AND remember the column, because §13.18.38.4 GR12 measures the later repetitions from
                    // the column this one occupies, not from the horizontal counter (which holds its RIGHTMOST).
                    w.Line($"__ra{spec.AnchorId} = __hc + {spec.Value};   // §13.18.14.4 GR8 + §13.18.38.4 GR12");
                    w.Line($"{RuntimeApi.ReportPlace(r.CsIndex, "__ln", $"__ra{spec.AnchorId}", image)};");
                    w.Line($"__hc = __ra{spec.AnchorId} + {f.PrintItem.DisplayTextWidth - 1};   // §13.18.14.4 GR9");
                    break;
                default:
                    w.Line($"{RuntimeApi.ReportPlace(r.CsIndex, "__ln", $"__ra{spec.AnchorId} + {spec.Value}", image)};"
                        + $"   // §13.18.38.4 GR12 — {spec.Value} columns right of repetition 0");
                    if (needsHc)
                        w.Line($"__hc = __ra{spec.AnchorId} + {spec.Value + f.PrintItem.DisplayTextWidth - 1};   // §13.18.14.4 GR9");
                    break;
            }
        }
    }

    /// <summary>ONE repetition's OCCURS … DEPENDING presence test as a C# boolean (ISO §13.18.38.4 GR13 with
    /// §13.18.63.4 GR22). GR13: the repetition count is data-name-1 when its value lies in integer-1 through
    /// (integer-2 − 1), and integer-2 otherwise ("the report group is processed as though the OCCURS clause had
    /// been written without the TO and DEPENDING phrases"), so repetition <c>Ordinal</c> appears exactly when it
    /// is below that count. The test is one term of the group's presence probe (<see cref="PresencePlan"/>), so
    /// data-name-1 is read once per presentation, at the point GR13 names — "just before the processing for the
    /// first LINE clause of the report group" — and every placement of the group sees that one value.</summary>
    private string RepetitionTest(ReportRepetitionGuard g)
    {
        if (g.Spec.DependingItem is not { } dn || refs.ResolveItem(dn) is not { } place) return "true";
        // The GR13 count through the ONE occurrence-count renderer (AllCount.ReportDepending), which a table(ALL)
        // argument over a repeating sum counter reads too (kb/Work PB1271).
        return $"{g.Ordinal} < {PlaceRenderer.OccurrenceCount(new AllCount.ReportDepending(place, g.Spec.Min, g.Spec.Max))}";
    }

    /// <summary>ONE presence delegate per report line, carrying BOTH suppressors §13.18.63.4 GR22 names that
    /// can absent a whole line: the §13.18.41 PRESENT WHEN chain and the §13.18.38.4 GR13 count of every
    /// enclosing repeating entry with a DEPENDING phrase. Composing them here rather than in two delegates is
    /// what keeps §13.18.35.4 GR4c's "these clauses are taken into account in computing the trial sum" true of
    /// both at once — the line's slot of the group's presence snapshot (<see cref="PresencePlan"/>) holds the one
    /// answer the placement and the page-fit test both read. Null when the line is unconditional (the
    /// characterization-pinned three-argument construction).</summary>
    private string? LinePresent(ReportLineModel l)
    {
        if (l.PresentWhen.Count == 0 && l.RepetitionGuards.Count == 0) return null;
        List<string> terms = [.. l.PresentWhen.Count > 0 ? [PresentExpr(l.PresentWhen)] : (string[])[],
                              .. l.RepetitionGuards.Select(RepetitionTest)];
        return string.Join(" && ", terms);
    }

    /// <summary>The compose-local name of a VARYING counter (§13.18.64) — keyed by the synthetic print item's
    /// uid + the counter's index within the entry's VARYING clause.</summary>
    private static string VaryName(ReportFieldModel f, int k) => $"__rv{f.PrintItem.Uid}_{k}";

    /// <summary>A VARYING FROM/BY value as an <see cref="Int128"/> integer C# expression (ISO §13.18.64.4 GR3a/GR3b;
    /// absent ⇒ 1). The value lands in its OWN lane — fixed point unscaled at its scale, binary64, or a
    /// standard-decimal intermediate — through <c>CobolReport.VaryingInteger</c>, which applies GR5: "If the
    /// evaluation of arithmetic-expression-1 or arithmetic-expression-2 produces a noninteger value and the VARYING
    /// clause was specified in a report description entry, the EC-REPORT-VARYING exception condition is set to
    /// exist". A scale-0 fixed-point value is an integer by construction and needs no test.</summary>
    private string VaryValue(ReportModel r, ReportVaryingModel v, BoundExpr? e, string phrase)
    {
        if (e is null) return "(Int128)1";
        NumX x = num.Render(e, ReceiverContext.None);
        string detail = CsLiteral($"report {r.Name}: VARYING {v.Name} {phrase} evaluated to a noninteger value "
            + "(ISO §13.18.64.4 GR5)");
        if (x.Real || x.Dec) return RuntimeApi.ReportVaryingInteger($"{x.Expr}, {detail}");
        string unscaled = NumericRenderer.Align(x, x.Scale);   // the unsigned-wide lane lands in Int128 here
        return x.Scale == 0 && !x.U
            ? $"(Int128)({unscaled})"
            : RuntimeApi.ReportVaryingInteger($"{unscaled}, {x.Scale}, {detail}");
    }

    /// <summary>A PRESENT WHEN chain as ONE C# boolean expression — the AND of the chain (ISO §13.18.41.4 GR2b:
    /// an absent ancestor absents every subordinate, so presence = every condition true).</summary>
    private string PresentExpr(IReadOnlyList<BoundCondition> conds) =>
        string.Join(" && ", conds.Select(c => $"({cond.Render(c)})"));

    /// <summary>The C# expression of one printable item's image at REPETITION <paramref name="rep"/> — the
    /// operand §13.18.63.4 GR23 / §13.18.53.4 GR4 assign to that repetition, rendered by the rule its OWN clause
    /// carries.
    /// <para>⛔ TWO CLAUSES, TWO RULES, AND THEY ARE NOT THE SAME RULE (kb/Work PB506). A SOURCE operand is "the
    /// sending operand of an implicit MOVE statement in which the data item referenced by identifier-1 is moved
    /// to the printable item" (§13.18.53.4 GR1), so it goes through the orchestrator's ONE MOVE conversion
    /// (<c>ConvertSource</c> — PIC-governed alignment and editing, JUSTIFIED honoured). A VALUE operand is an
    /// INITIALIZATION: §13.18.63.4 GR21 imports GR7 ("aligned … except that initialization is not affected by a
    /// JUSTIFIED clause and no editing takes place") and GR8, and §13.18.63.3 SR34 imports SR11 (an
    /// alphanumeric-edited or national-edited picture's editing characters "do not cause editing of the initial
    /// value"), so it goes through the ONE VALUE recipe the working-storage lane uses
    /// (<see cref="ValueInitializer.InitializerFrom"/>). Routing a VALUE through the MOVE applied all three
    /// excluded transforms and printed three different wrong answers.</para>
    /// <para>The synthetic print item is StoreAsImage for numerics, so both paths yield the printable CHARACTER
    /// image for every category (the display format / edit-mask / string-store renders).</para></summary>
    /// <param name="rep">The PLACEMENT index within this field (one per COLUMN operand). The entry-wide
    /// repetition ORDINAL — what §13.18.63.4 GR23 counts when it assigns "successive operands to successive
    /// repeating printable items", and what §13.18.38's replay makes span the OCCURS repetitions too — is
    /// <see cref="ReportFieldModel.RepetitionOrdinal"/> + this index (kb/Work PB506 × PB565).</param>
    private string FieldImage(ReportModel r, ReportFieldModel f, int rep)
    {
        BoundOperand source;
        // The transfer's ROUNDING MODE (§14.7.4). Only a report value clause's own ROUNDED phrase sets it:
        // §13.18.53.4 GR2 for a SOURCE operand and §13.18.54.4 GR4 for a SUM counter's printable face. With no
        // phrase the transfer is §13.18.53.4 GR1's MOVE / GR4's MOVE, which truncates (kb/Work PB852).
        var rounding = CobolRounding.Truncation;
        switch (f.SourceAt(f.RepetitionOrdinal + rep))
        {
            case FieldValueSource v:
                // §13.18.63 — an initialization, NOT the §13.18.53.4 GR1 implicit MOVE (see the remarks above).
                return Data.ValueImageOf(f.PrintItem, v.Raw);
            case FieldCounterSource c:
                // SOURCE LINE-COUNTER / PAGE-COUNTER (§8.4.3.15 SR1) — composed at presentation time, AFTER the
                // §13.18.35.4 GR6 counter update, so a PH line's LINE-COUNTER prints the PH's own line number.
                source = new BoundComputedOperand(new BoundReportCounterRef(r, c.IsPage));
                break;
            case FieldSumSource s:
                // The SUM counter is the printable entry's source item (§13.18.54.4 GR4 — "the content of the
                // sum counter is moved, according to the general rules of the MOVE statement, to the printable
                // item"), reached through the ONE sum-counter place the procedure division also reads and writes
                // (kb/Work PB840): one identity, one accessor, one conversion. The counter is indexed by its
                // ENTRY ORDINAL (GR1) — `First(Id == name)` used to pick whichever entry spelled its data-name
                // first (kb/Work PB882).
                var sum = r.Sums[s.CounterId];
                // §13.18.54.4 GR4 — with the clause's ROUNDED phrase "the content of the sum counter is computed
                // according to the general rules for the COMPUTE statement with the ROUNDED phrase".
                string moved = move.ConvertSource(
                    new BoundFieldOperand(new ReportSumCounterPlace(r.CsIndex, s.CounterId, sum.Family.Register)),
                    f.PrintItem, rounding: sum.Rounding);
                // ⛔ …but only while the counter's SIZE ERROR INDICATOR is unset (GR4: "If the associated size error
                // indicator is set, an EC-REPORT-SUM-SIZE exception condition is set to exist and the printable item
                // is filled with spaces" — kb/Work PB1130). The engine answers the indicator and raises the
                // condition; the fill is unconditional, so it is decided here and not by the raise.
                return $"({RuntimeApi.ReportSumPresentable(r.CsIndex, s.CounterId)} "
                    + $"? {moved} : {CsLiteral(new string(' ', f.PrintItem.DisplayTextWidth))})";
            case FieldComputeSource { Value: { } expr } cs:
                // §13.18.53.4 GR2 — "Arithmetic-expression-1 specifies the operand of an implicit COMPUTE
                // statement that is executed implicitly whenever the associated item is printed. If the ROUNDED
                // phrase is specified, the implicit COMPUTE statement has the corresponding ROUNDED phrase."
                // The COMPUTE's receiving operand is the printable item, so the transfer IS the one conversion
                // every other operand takes — with the clause's rounding mode (kb/Work PB852).
                source = new BoundComputedOperand(expr);
                rounding = cs.Rounding;
                break;
            case FieldComputeSource bad:
                return LoudValue("string",
                    $"report {r.Name}: SOURCE '{bad.Written}' was rejected at bind (ISO §13.18.53.3 SR4)");
            case FieldVaryingSource v:
                // The entry's own VARYING counter as the source item (§13.18.64.4 GR4 NOTE) — the compose-local
                // counter, re-read at each repetition's placement.
                source = new BoundComputedOperand(new BoundReportVaryingRef($"{VaryName(f, v.Index)}_{rep}"));
                break;
            case FieldDataSource d when d.Item is { } item && refs.ResolveItem(item) is { } place:
                source = new BoundFieldOperand(place);
                break;
            default:
                return LoudValue("string",
                    $"report {r.Name}: SOURCE operand not resolvable to storage (ISO §13.18.53.3 SR4)");
        }
        return move.ConvertSource(source, f.PrintItem, rounding: rounding);
    }

    // ⛔ `ValueOperand(string raw)` IS GONE (kb/Work PB506), and this comment stands where it was so it is not
    // re-added. It turned a format-4 VALUE operand's raw text into a BoundOperand — a quoted literal, an ALL
    // literal, a figurative, else a numeric literal — purely so the operand could be pushed through
    // `move.ConvertSource`, i.e. through §13.18.53.4 GR1's implicit MOVE. §13.18.63.4 GR21/GR7/GR8 and
    // §13.18.63.3 SR34/SR11 say a VALUE is an INITIALIZATION and exclude exactly the transforms a MOVE applies,
    // so the whole raw-text→BoundOperand recognition chain was a SECOND, DIVERGENT copy of the decode
    // `ValueInitializer` already owns (its figurative/ALL/edited arms, the CCVS alphanumeric-on-numeric
    // leniency, the LOCALE compose). `FieldImage` now calls that one recipe.

    /// <summary>Emit the per-instance report-engine construction (called inside <c>__Activate</c>'s
    /// once-per-instance block, right after the file registration — hazard: the report FD must be
    /// registered BEFORE the engine's first write, COBOLNET_REPORT_WRITER_DESIGN §4): the engine with its
    /// §13.18.39.4 geometry, each group with its compose table, the CONTROL get/set delegates (§13.18.16), the
    /// SUM counters (§13.18.54), and the USE BEFORE REPORTING hooks (§14.9.49 Format 2 GR8).</summary>
    public void EmitReportConstruction(CodeWriter w)
    {
        var reports = ctx.Data.Reports;
        if (reports.Count == 0) return;
        foreach (var r in reports)
        {
            if (r.File is null) continue;   // diagnosed at bind (§13.18.46) — compile already failed
            w.Line($"__RPT_{r.CsIndex} = new CobolReport({CsLiteral(r.Name)}, {FileKeyExpr(r.File)}, "
                + $"{r.LineWidth}, {r.PageWidth}, {(r.Paged ? "true" : "false")}, {r.PageLimit}, {r.Heading}, {r.FirstDetail}, "
                + $"{r.LastControlHeading}, {r.LastDetail}, {r.Footing});");
            // The CODE clause (§13.18.12): the characters every logical record of this report begins with (GR1). A
            // literal is a constant; an identifier is READ by the engine at each body group (GR3 — the value is used
            // until the next evaluation), through the one string-carrier read the CONTROL operands use.
            if (r.Code is { } code)
            {
                if (code.Literal is { } literal)
                    w.Line($"__RPT_{r.CsIndex}.SetCode(static () => {CsLiteral(literal)});   // CODE literal-1 (§13.18.12.4 GR1)");
                else if (code.Item is { } codeItem && refs.ResolveItem(codeItem) is { } codePlace)
                    w.Line($"__RPT_{r.CsIndex}.SetCode(() => {CallEmitter.CallStringRead(codePlace)});   // CODE identifier-1 (§13.18.12.4 GR3)");
                else
                    w.Line(LoudStmt($"report {r.Name}: CODE identifier '{code.Written}' does not resolve to a place this "
                        + "backend can read (ISO §13.18.12.4 GR3)"));
            }
            foreach (var (group, gi) in r.Groups.Select((g, i) => (g, i)))
            {
                // A conditioned line carries its slot in the group's presence snapshot, which the engine takes
                // once per presentation, BEFORE any LINE processing (§13.18.41.4 GR2; kb/Work PB1272);
                // unconditional lines keep the three-argument construction (the characterization-pinned text). A
                // line that seeds or steps from a §13.18.38.4 GR12c/GR12d step anchor adds the anchor triple,
                // which then forces the slot argument to be written out even when it is −1. A first line whose
                // LINE clause carries the NEXT PAGE phrase (§13.18.35.2 Format 1; kb/Work PB1001) adds it as a
                // NAMED argument, so every phrase-less line keeps its text.
                var plan = PlanOf(r, group);
                string lines = group.Lines.Count == 0
                    ? "System.Array.Empty<ReportGroupLine>()"
                    : "new[] { " + string.Join(", ", group.Lines.Select((l, li) =>
                        $"new ReportGroupLine(ReportLineKind.{l.Kind}, {l.Value}, __RPT_C_{r.CsIndex}_{gi}_{li}"
                        + (plan.Lines.TryGetValue(l, out int ls) ? $", {ls}" : l.Anchor > 0 ? ", -1" : "")
                        + (l.Anchor > 0 ? $", {l.Anchor}, {l.RelativeBase}, {l.TrialInterval}" : "")
                        + (l.NextPage ? ", nextPage: true" : "") + ")")) + " }";
                w.Line($"var __rg{r.CsIndex}_{gi} = new ReportGroup(ReportGroupKind.{group.Kind}, "
                    + $"{CsLiteral(group.Name ?? "")}, {group.ControlLevel}, {lines});");
                if (plan.Tests.Count > 0)
                    w.Line($"__rg{r.CsIndex}_{gi}.SetPresence({plan.Tests.Count}, __RPT_P_{r.CsIndex}_{gi});");
                // The OR PAGE phrase of a control heading (§13.18.57.2; kb/Work PB1298) — the engine reprints the
                // heading after each page advance (§13.18.57.4 GR6 c)).
                if (group.OrPage)
                    w.Line($"__rg{r.CsIndex}_{gi}.OrPage = true;");
                // The NEXT GROUP clause (§13.18.37; kb/Work PB957) — the bound runtime record, written verbatim; the
                // engine applies it after the group's last line (GR2).
                if (group.NextGroup is { } ng)
                    w.Line($"__rg{r.CsIndex}_{gi}.NextGroup = new ReportNextGroup(ReportNextGroupKind.{ng.Kind}, "
                        + $"{ng.Value}, {(ng.Reset ? "true" : "false")});");
                w.Line($"__RPT_{r.CsIndex}.AddGroup(__rg{r.CsIndex}_{gi});");
            }
            // CONTROL hierarchy (§13.18.16), major→minor: get/set image delegates over the typed storage — the
            // CALL boundary's one string-carrier pair (CallStringRead/CallStringWrite), reused verbatim.
            foreach (var ctl in r.Controls)
            {
                if (ctl.IsFinal)
                {
                    w.Line($"__RPT_{r.CsIndex}.AddControl(true, static () => \"\", static __v => {{ }});   // FINAL (§13.18.16.4 GR2 — never breaks)");
                    continue;
                }
                // ⛔ CITATION REPAIRED (kb/Work PB177 arm C): this said "ISO §13.18.16.3 SR3", but SR3 is
                // "Data-name-1 shall not be subject to any OCCURS clauses" — a real clause answering a
                // different question. The §13.18.16.3 SHAPE rules (SR3/SR5/SR7) and the §13.18.60.3 SR10 INDEX
                // rule are REJECTED AT BIND TIME by DataBinder.ControlOperandShapeViolation, so an INDEX operand
                // never reaches this loop. What survives here is ONE implementation limit, labelled as one: an
                // unresolved operand — the shapes ReferenceResolver returns null for, which today are the
                // dynamic-capacity table entry and a leaf beneath one (both ALSO rejected at bind, by the IsTable
                // arm), so this limb is a backstop for a resolver shape not yet enumerated.
                // ⛔ A REFERENCE-MODIFIED OPERAND IS SAVED AND COMPARED AS ITS SLICE, NOT AS THE WHOLE ITEM
                // (kb/Work PB205). §13.18.16.3 SR4 expressly permits the ref-mod and §13.18.16.4 GR3 then defines
                // the prior control as having "the same data description as the corresponding data item" — which
                // for a reference-modified operand is the §8.4.3.3.4 GR5 unique data item, the slice. The
                // positions are integer literals (SR4), so the view needs no expression machinery: it is the ONE
                // ref-mod view builder, reached by item instead of by parse context.
                if (ctl.Item is not { } item
                    || (ctl.Operand?.RefModStart is { } start
                            ? refs.ResolveItemRefMod(item, start, ctl.Operand.RefModLength)
                            : refs.ResolveItem(item)) is not { } place)
                {
                    w.Line(LoudStmt($"report {r.Name}: CONTROL operand '{ctl.Display}' does not resolve to a place "
                        + "this backend can save and restore as the prior control value (ISO §13.18.16.4 GR3)"));
                    continue;
                }
                // ⛔ A FLOATING-POINT CONTROL ITEM SAVES AND RESTORES ITS BIT PATTERN (kb/Work PB1234). No syntax
                // rule of §13.18.16.3 excludes a floating-point data-name-1, and GR3's prior control has "the same
                // data description as the corresponding data item", so GR4 a)'s store and restore are same-usage
                // copies. The character-image channel below has no float restore (a string→double assignment),
                // and a DISPLAY image is a ROUNDED rendering besides, so the item was a run-time loud at program
                // activation. The float arm reads the item on its OWN carrier (the same-usage MOVE read), keys it
                // by its bits (CobolReport.FloatControlKey), writes it back through the item's own encoding, and
                // breaks on a VALUE change (CobolReport.FloatControlEqual). A reference-modified operand denotes a
                // character slice, not the item (§8.4.3.3.4 GR5/GR6 — Place.DenotedItem is null for it), and keeps
                // the character arm.
                if (place.DenotedItem is not null && place.Item.Pic is { IsFloat: true } fpic)
                {
                    string carrier = NumericRenderer.FloatCarrierRead(place, SendingRef.SameUsageMove);
                    string restored = RuntimeApi.ReportFloatControlValue("__v", fpic.IsSingle);
                    string store = place.Item.StoreAsImage ? NumericRenderer.ImageOfCarrier(restored, place.Item) : restored;
                    w.Line($"__RPT_{r.CsIndex}.AddControl(false, () => {RuntimeApi.ReportFloatControlKey(carrier)}, "
                        + $"__v => {{ {PlaceRenderer.Write(place, store)} }}, {RuntimeApi.ReportFloatControlEqual});"
                        + "   // floating-point CONTROL (§13.18.16.4 GR3/GR4)");
                    continue;
                }
                // The prior-control save/compare/restore key is the item's CHARACTER IMAGE (§13.18.16.4 GR3 —
                // representation-faithful for every category): read via the one string-carrier read; the
                // restore decodes through StoreDisplay for a native numeric leaf (the NumericImagePlace shape),
                // or splices the image for string-carried storage.
                string set = CallEmitter.CallPlaceIsString(place)
                    ? CallEmitter.CallStringWrite(place, "__v")
                    : PlaceRenderer.Write(place, RuntimeApi.NumStoreDisplay("__v", place.Item.ProfileName, PlaceRenderer.Read(place)));
                // ⛔ THE BREAK TEST IS THE PROGRAM'S COMPARISON (kb/Work PB1131). §12.3.6.4 GR11 c): the program
                // collating sequences "are used to determine the truth value of any alphanumeric comparisons and
                // national comparisons … Implicitly specified by the presence of a CONTROL clause", and §13.18.16.4
                // GR3's test is one "for equality with the corresponding prior control" — an operand of the SAME
                // data description, so the comparison class is the item's own against itself, asked of the ONE
                // class rule a relation condition asks (CollateArgFor over the operand's category — a reference-
                // modified operand answers the slice's category, §8.4.3.3.4 GR6 c)). No sequence ⇒ no argument:
                // the engine's code-unit compare of two equal-length images IS that comparison.
                var cat = CollatingSelection.OperandCategory(new BoundFieldOperand(place));
                string collate = ctx.CollateArgFor(cat, cat);
                string equal = collate.Length == 0 ? "" : $", static (__a, __b) => {RuntimeApi.StrCompare("__a", "__b", collate)} == 0";
                w.Line($"__RPT_{r.CsIndex}.AddControl(false, () => {CallEmitter.CallStringRead(place)}, __v => {{ {set} }}{equal});");
            }
            // SUM counters (§13.18.54): the addend delegate yields the addends' total at the counter's scale
            // (GR3 — ADD-consistent accumulation; GR9 — multiple addends sum together).
            // ⛔ ONE CARRIER, NEVER A NARROWING CAST (kb/Work PB1509/PB1560/PB1666). The counter, every addend and
            // the term's total are Int128 end to end — the carrier the compiler gives every 19–38-digit numeric
            // item — and GR1's digit count (up to the 31 digit positions a numeric PICTURE may describe,
            // §13.18.40.3 SR14) is the engine's capacity. The delegate used to be `() => (long)(…)`, so a 20-digit
            // addend wrapped modulo 2^64 into a wrong printed total with no size error. An alignment or a term
            // total past Int128 is past every counter's capacity: the checked Align raises CobolSizeError and the
            // checked term sum OverflowException, and the engine takes either as GR3's size error.
            // The arithmetic MODE (§11.9.5.2 GR1 NATIVE / GR3 STANDARD-DECIMAL — both name the SUM clause) selects
            // how each ADDEND is evaluated: an arithmetic-expression addend renders through the unit's own lane
            // (a CobolDec SDIDI under STANDARD-DECIMAL, landed by NumericRenderer.Align's Dec arm), exactly as the
            // same expression in a COMPUTE would. The ACCUMULATION itself is one aligned addition of two
            // fixed-point values ≤ 31 digits each, whose ≤ 32-digit exact result both engines represent without
            // rounding (§8.8.1.5.2 — a 34-digit SDIDI never rounds a ≤ 34-digit value), so the Int128 addition IS
            // the standard-decimal result as well as the native one.
            foreach (var sum in r.Sums)
            {
                int printedGi = r.Groups.IndexOf(sum.PrintedIn);
                // A conditioned SUM entry passes its slot in its group's presence snapshot — absent at a
                // presentation suppresses the end-of-group reset (§13.18.41.4 GR3g / §13.18.54.4 GR10), and the
                // printable face reads the same snapshot, so one answer governs both halves (kb/Work PB1272).
                string sumPresent = PresencePlan.SlotOf(PlanOf(r, sum.PrintedIn).Sums, sum) is var ss and >= 0 ? $", {ss}" : "";
                // GR1's digit count rides the registration: it is the counter's capacity, and an addition past it is
                // the GR3 size error that sets the entry's indicator (kb/Work PB1130).
                w.Line($"__RPT_{r.CsIndex}.AddSum({sum.Id}, {sum.ResetLevel}, "
                    + $"__rg{r.CsIndex}_{printedGi}{sumPresent});");
                // ONE TERM PER `SUM … [UPON …]` GROUP (§13.18.54.3 SR1 + §13.18.54.4 GR1/GR7c2 — kb/Work
                // PB482): the counter belongs to the ENTRY, the UPON filter belongs to its own group, and GR9
                // sums a group's addends together. Each addend is the bound identifier's value — subscripts and
                // all — rendered through the ONE numeric renderer, so a table addend is an ordinary indexed read
                // rather than the run-time loud it used to be.
                foreach (var term in sum.Terms)
                {
                    // ⛔ THE ADDITION IS THE ADD STATEMENT'S, EVALUATED BY THE ADD STATEMENT'S MACHINERY (kb/Work
                    // PB1686). §13.18.54.4 GR3: "The adding is consistent with the general rules of the ADD statement
                    // with the ON SIZE ERROR phrase or, in the case of an arithmetic expression, the COMPUTE
                    // statement with the ON SIZE ERROR phrase", and GR9 sums a group's addends together — so ONE
                    // addition is `ADD addend-1 … addend-n TO counter`: the addends' sum is the one initial
                    // evaluation (ArithmeticEmitter.EmitInPlace's Fold), it is combined with the counter's content
                    // at the WIDER of the two scales, and the result is stored ONCE at the counter's scale with its
                    // capacity, its ROUNDED mode and its size-error test (the receiver's profile — GR1). The counter's
                    // content arrives as the closure's argument and the stored content is the closure's value. The
                    // former form aligned EACH addend to the counter's scale (and rounded it there) BEFORE adding, so
                    // an addend finer than the counter lost its extra digits one addend at a time: a 9V99 counter fed
                    // 1.000 then −0.005 held 1.00, where ADD of the same two values gives 0.99 (1.000 − 0.005 =
                    // 0.995, truncated). The SUM clause's own rounded-phrase (§13.18.54.2) is the mode of that store (kb/Work PB852's determination; GR4 speaks of the SOURCE clause's phrase).
                    var family = sum.Family;
                    var rcv = new ReceiverContext(family.Scale, Real: false, sum.Rounding, InSizeError: true,
                        IntegerDigits: Math.Max(0, (family.Register.Pic?.DigitPositions ?? 0) - family.Scale));
                    var addendExprs = term.Addends.Where(a => a.Value is not null).Select(a => a.Value!).ToList();
                    string apply = term.Addends.Any(a => a.Value is null)
                        ? LoudValue("Int128", $"report {r.Name}: SUM addend was rejected at bind (ISO §13.18.54.3 SR5)")
                        : addendExprs.Count == 0
                            ? "__c"
                            : NumericRenderer.StoreExpr(
                                num.Combine(new NumX("__c", family.Scale), "+", num.Fold(addendExprs, rcv), rcv),
                                family.Scale, family.Register.ProfileName, sum.Rounding, raiseOnSizeError: true);
                    string addend = $"(Int128 __c) => {apply}";
                    // null = no UPON phrase (GR7 c) 1) — every GENERATE for this report). An UPON phrase whose
                    // operands were ALL rejected emits the EMPTY filter instead, so a suppressed COBOLNET2046
                    // accumulates on NOTHING rather than on everything: the absence of the phrase and the
                    // failure to resolve it are opposite answers, and the fallback has to be the narrow one.
                    var upon = term.Upon.Where(d => d.Detail is not null).ToList();
                    string uponArg = term.Upon.Count == 0
                        ? "null"
                        : upon.Count == 0
                            ? "System.Array.Empty<string>()"
                            : "new[] { " + string.Join(", ", upon.Select(d => CsLiteral(d.Detail!.Name!))) + " }";
                    w.Line($"__RPT_{r.CsIndex}.AddSumTerm({sum.Id}, {addend}, {uponArg});");
                }
            }
        }
        // ⛔ NO USE BEFORE REPORTING HOOK IS INSTALLED HERE (kb/Work PB369). The declarative run before a group is
        // produced is selected per STATEMENT (§14.9.49.4 GR4 — "FORMATS 1 AND 2"), so it travels with the GENERATE /
        // TERMINATE call as the selector EmitBeforeReportingSelectors writes; an engine-wide hook installed by the
        // declaring program would run the declaring program's NON-global declaratives for a contained program's
        // GENERATE of a GLOBAL report, and could never run the contained program's own.
    }

    // ── The §14.9.49.4 GR4 Format-2 selector (kb/Work PB369) ─────────────────────────────────────────────────

    /// <summary>The per-report selector member a unit emits, named by the report's compilation-unique
    /// <see cref="ReportModel.Uid"/> so a contained program and its containers agree on it.</summary>
    private static string SelectorName(ReportModel r) => $"__BeforeReporting_{r.Uid}";

    /// <summary>The cached delegate over <see cref="SelectorName"/> handed to the engine (one allocation per
    /// program instance, not per GENERATE).</summary>
    private static string SelectorField(ReportModel r) => $"__brSel_{r.Uid}";

    /// <summary>Does a qualifying USE BEFORE REPORTING declarative for a group of <paramref name="r"/> exist in
    /// <paramref name="unit"/> (any, when <paramref name="globalOnly"/> is false; only a GLOBAL one otherwise) or,
    /// as a GLOBAL one, in a container between it and the program that declares the report? §14.9.49.4 GR4: a) the
    /// source element containing the statement, then b) "a qualifying declarative with the GLOBAL attribute in the
    /// next inclusive directly containing source element", repeated outward. The walk stops at the declaring
    /// program: no program outside it can name the report's groups (§13.18.27.4 GR2).</summary>
    public static bool ChainSelects(BoundUnit? unit, ReportModel r, bool globalOnly)
    {
        for (; unit is not null && unit.Data.VisibleReports.Contains(r); unit = unit.Parent, globalOnly = true)
        {
            if (unit.Bound.Declaratives is { } ds
                && ds.Any(d => d.ReportGroup is { } g && r.Groups.Contains(g) && (!globalOnly || d.Global)))
                return true;
            if (unit.Data.ReportDepth(r) == 0) break;   // the declaring program — GR4 b)'s walk ends here
        }
        return false;
    }

    /// <summary>Emit, for every report <paramref name="unit"/> can see whose GR4 chain has a qualifying
    /// declarative, the selector the engine asks just before producing each group (§14.9.49.4 GR8): the unit's
    /// own declarative for the group — any, when the statement is this unit's (GR4 a)); only a GLOBAL one when
    /// the walk arrived from a contained program (GR4 b)) — run in THIS instance (the declaring program's data,
    /// §8.4.6.2), else the next container outward. Records the reports whose statements pass it in
    /// <see cref="DispatchState.BeforeReportingSelectors"/>.</summary>
    public void EmitBeforeReportingSelectors(BoundUnit unit, CodeWriter w)
    {
        dispatch.BeforeReportingSelectors.Clear();
        EmitSelectors(unit.Bound.Declaratives ?? [], unit.Data.VisibleReports,
            r => ChainSelects(unit, r, globalOnly: false),
            r => unit.Data.ReportDepth(r) > 0 && ChainSelects(unit.Parent, r, globalOnly: true)
                ? $"return __outer.{SelectorName(r)}(__gi, true);   // GR4 b) — the next directly containing source element"
                : "return false;   // no qualifying declarative — the group is produced with none (GR4 b) exhausted)",
            asLocal: false, w);
    }

    /// <summary>The METHOD's half of §14.9.49.4 GR4 (kb/Work PB1044): a method is a source element of its own
    /// (§14.2.2 SR10 admits USE declaratives in a method definition; design SSOT §9.10), so its GENERATE and
    /// TERMINATE statements select over ITS declaratives — GR4 a) — and, being contained in no source element with a
    /// procedure division (§14.2.2 SR12/SR13), GR4 b)'s outward walk has nowhere to go. The selectors are LOCAL
    /// FUNCTIONS of the method (they call its local <c>__RunUse</c> and capture its data), over the reports its
    /// class half owns. The caller restores <see cref="DispatchState.BeforeReportingSelectors"/> after the body.</summary>
    public void EmitMethodBeforeReportingSelectors(IReadOnlyList<BoundDeclarative> decls, CodeWriter w)
    {
        dispatch.BeforeReportingSelectors.Clear();
        EmitSelectors(decls, ctx.Data.VisibleReports,
            r => decls.Any(d => d.ReportGroup is { } g && r.Groups.Contains(g)),
            _ => "return false;   // a method has no containing source element (GR4 b) exhausted)",
            asLocal: true, w);
    }

    /// <summary>The ONE selector emitter (programs: members; methods: local functions). Emits, for every report of
    /// <paramref name="reports"/> that <paramref name="selects"/>, the cached-delegate slot and the
    /// <c>__BeforeReporting_*</c> switch over <paramref name="decls"/>, ending in <paramref name="tail"/>.</summary>
    private void EmitSelectors(IReadOnlyList<BoundDeclarative> decls, IEnumerable<ReportModel> reports,
        Func<ReportModel, bool> selects, Func<ReportModel, string> tail, bool asLocal, CodeWriter w)
    {
        foreach (var r in reports)
        {
            if (!selects(r)) continue;
            dispatch.BeforeReportingSelectors.Add(r);
            w.Line();
            w.Line($"{(asLocal ? "" : "private ")}System.Func<int, bool>? {SelectorField(r)}{(asLocal ? " = null" : "")};   // RD {r.Name}: the cached selector (ISO §14.9.49.4 GR4)");
            using (w.Block($"{(asLocal ? "" : "public ")}bool {SelectorName(r)}(int __gi, bool __globalOnly)   // RD {r.Name} — ISO §14.9.49.4 GR4 / GR8"))
            {
                var cases = new List<string>();
                var seen = new HashSet<int>();
                for (int i = 0; i < decls.Count; i++)
                    if (decls[i].ReportGroup is { } g && r.Groups.IndexOf(g) is var gi and >= 0 && seen.Add(gi))
                        cases.Add(decls[i].Global
                            ? $"case {gi}: {dispatch.RunUseCall(i, decls[i].Range)}; return true;   // USE GLOBAL BEFORE REPORTING {g.Name}"
                            : $"case {gi}: if (!__globalOnly) {{ {dispatch.RunUseCall(i, decls[i].Range)}; return true; }} break;   // USE BEFORE REPORTING {g.Name} (GR4 a) only)");
                if (cases.Count > 0)
                    using (w.Block("switch (__gi)"))
                        foreach (string c in cases) w.Line(c);
                w.Line(tail(r));
            }
        }
    }

    /// <summary>The engine argument a GENERATE / TERMINATE passes: this unit's cached selector for
    /// <paramref name="r"/>, or nothing when no declarative in its GR4 chain names a group of the report.</summary>
    private string SelectorArgument(ReportModel r) =>
        dispatch.BeforeReportingSelectors.Contains(r)
            ? $"{SelectorField(r)} ??= __gi => {SelectorName(r)}(__gi, false)"
            : "";

    /// <summary>The engine of <paramref name="r"/> as THIS unit reaches it — its own field, or the declaring
    /// container's through the <c>__outer</c> chain for an inherited GLOBAL report (§13.18.27.4 GR2).</summary>
    private string Engine(ReportModel r) => RuntimeApi.ReportEngine(r.CsIndex, ctx.Data.ReportDepth(r));

    // ── Verb emission (ISO §14.9.21 / §14.9.16 / §14.9.46) ───────────────────────────────────────────────────

    /// <summary>INITIATE: one engine call per report, in written order (§14.9.21.4 GR5).</summary>
    public void EmitInitiate(BoundInitiate s)
    {
        foreach (var r in s.Reports)
            ctx.Writer.Line($"{Engine(r)}.Initiate();");
    }

    /// <summary>GENERATE: detail reporting names the detail group; summary reporting (the report-name form,
    /// §14.9.16.4 GR2) passes null.</summary>
    public void EmitGenerate(BoundGenerate s)
    {
        if (s.Detail is { } det && det.Name is null)
        {
            // A GENERATE-able detail always has a data-name (§13.16.3 SR7) — unreachable unless the binder let
            // an unnamed group through; loud, never a silent wrong-group generate (§1.4).
            ctx.Writer.Line(LoudStmt($"GENERATE of an unnamed detail group of report {s.Report.Name}"));
            return;
        }
        string sel = SelectorArgument(s.Report);
        ctx.Writer.Line($"{Engine(s.Report)}.Generate({(s.Detail is { } d ? CsLiteral(d.Name!) : "null")}"
            + $"{(sel.Length > 0 ? ", " + sel : "")});");
    }

    /// <summary>TERMINATE: one engine call per report, in written order (§14.9.46.4 GR4).</summary>
    public void EmitTerminate(BoundTerminate s)
    {
        foreach (var r in s.Reports)
            ctx.Writer.Line($"{Engine(r)}.Terminate({SelectorArgument(r)});");
    }

    /// <summary>SUPPRESS PRINTING (§14.9.45): name the group of the enclosing USE BEFORE REPORTING procedure
    /// (resolved at bind, <see cref="BoundSuppress.Group"/>) to its report's engine, which inhibits the CURRENT
    /// instance of that group only when the statement executes during that group's own hook (GR1/GR2; kb/Work
    /// PB1186) — printing, page advance, NEXT GROUP and LINE-COUNTER changes, but NOT the end-of-group sum reset.
    /// The argument is the group's ordinal in its report description: the <c>ReportGroup.Index</c> that
    /// <see cref="EmitReportConstruction"/>'s AddGroup order assigns and the §14.9.49.4 GR4 selector switches on.</summary>
    public void EmitSuppress(BoundSuppress s) =>
        ctx.Writer.Line($"{Engine(s.Report)}.SuppressPrinting({s.Report.Groups.IndexOf(s.Group)});   "
            + $"// §14.9.45.4 GR1 — {s.Group.Name ?? "(unnamed group)"}");
}
