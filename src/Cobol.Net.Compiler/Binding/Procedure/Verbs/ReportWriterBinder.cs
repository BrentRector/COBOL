// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The Report Writer half of the statement binder (ISO/IEC 1989:2023 §14.9.21 INITIATE / §14.9.16 GENERATE /
/// §14.9.46 TERMINATE; COBOLNET_REPORT_WRITER_DESIGN §5): the three verb binders over the bound
/// <see cref="ReportModel"/>s, the LINE-COUNTER / PAGE-COUNTER reference interception (§8.4.3.15 — the
/// registers are RWCS state, never storage; the <c>BoundLinageCounterRef</c> precedent), and the report-section
/// PRESENT WHEN / VARYING expression binding (§13.18.41/§13.18.64 — parse contexts captured at data bind,
/// bound HERE through the host's ONE condition/expression binders). P7 Step 10f collaborator; the host is
/// consumed only by <see cref="BindReportGroupClauses"/> (the standard (ctx, host) collaborator shape). The
/// receiving-side counter guard (<c>ResolveReceiving</c>) did NOT ride along — it is the shared receiving
/// spine (5 host pipelines consume it) and hoisted to the core, final home <c>ExpressionBinder</c> at 10q.
/// </summary>
internal sealed class ReportWriterBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind every report-section PRESENT WHEN condition (ISO §13.18.41 Format 1) and VARYING FROM/BY
    /// expression (§13.18.64) captured on this unit's report models — once per unit bind, through the ONE
    /// <c>ConditionBinder</c> / expression binder. Each DISTINCT condition context binds exactly once (an
    /// entry's condition appears in every subordinate chain — the memo keeps diagnostics single-shot).</summary>
    public void BindReportGroupClauses()
    {
        var memo = new Dictionary<Core.ConditionContext, BoundCondition>(ReferenceEqualityComparer.Instance);
        BoundCondition Bind(Core.ConditionContext c)
        {
            if (!memo.TryGetValue(c, out var b)) memo[c] = b = host.Cond.BindCondition(c);
            return b;
        }
        try
        {
            foreach (var r in ctx.Data.Reports)
                BindReportClauses(r, Bind);
        }
        finally
        {
            ctx.Refs.VaryingScope = null;
            ctx.Refs.ReportScope = null;
        }
    }

    /// <summary>One report's clause expressions, bound with the scope the REPORT SECTION gives them: the RD in whose
    /// description they are written (§8.4.2.2.3 SR9/SR10 — "In the report section, an unqualified reference to the
    /// PAGE-COUNTER is qualified implicitly by the name of the report in whose report description entry the reference
    /// is made"; §13.18.53.3 SR4 — a sum counter "defined in the current report") and the VARYING counters of the
    /// entry being bound (§13.18.64.3 SR2 — "only within the current entry or a subordinate entry").</summary>
    private void BindReportClauses(ReportModel r, Func<Core.ConditionContext, BoundCondition> Bind)
    {
        ctx.Refs.ReportScope = r;
        ctx.Refs.VaryingScope = null;
        // The RD CODE clause's identifier-1 (§13.18.12.2): the place the engine reads at each body group (GR3), through
        // the same ONE sending-operand resolution a SOURCE identifier takes.
        if (r.Code is { Reference: { } codeRef } code)
        {
            using var at = ctx.Edition.At(codeRef);
            if (host.Expr.FieldOperand(codeRef) is BoundFieldOperand { Place: { } codePlace }) code.Value = codePlace;
        }
        foreach (var g in r.Groups)
            foreach (var ln in g.Lines)
            {
                foreach (var c in ln.PresentWhenCtxs) ln.PresentWhen.Add(Bind(c));
                foreach (var f in ln.Fields)
                {
                    foreach (var c in f.PresentWhenCtxs) f.PresentWhen.Add(Bind(c));
                    // §13.18.64.2 writes FROM/BY as plain arithmetic-expression-1/-2, and the RW VARYING is on NEITHER
                    // index clause's list (§13.18.38.3 r7 names only PERFORM's and SEARCH's VARYING; §13.18.60.3 SR10
                    // names no VARYING) — so both screens apply (kb/Work PB215). A counter's expressions may name the
                    // counters of the entries above it, and its BY the counters of its own entry (§13.18.64.3 SR3),
                    // so each binds with exactly those in scope — once, whichever field reaches the counter first.
                    foreach (var use in f.Varyings)
                    {
                        var v = use.Counter;
                        if (v.Bound) continue;
                        v.Bound = true;
                        using var at = ctx.Edition.At(v.Entry);
                        // (A FROM naming a counter of its own clause is §13.18.64.3 SR3's prohibition, reported ONCE per
                        // written entry by ScreenReportVaryingClauses; it binds in the same scope so the refusal is
                        // not followed by a second, misleading "not defined".)
                        ctx.Refs.VaryingScope = ScopeOf(v.Enclosing.Concat(v.Group));
                        if (v.FromCtx is { } fc) v.From = host.Expr.BindExpr(fc);
                        if (v.ByCtx is { } bc) v.By = host.Expr.BindExpr(bc);
                    }
                    // The field's operands see every counter in scope at the field (SR2).
                    ctx.Refs.VaryingScope = ScopeOf(f.Varyings.Select(u => u.Counter));
                    // A SOURCE identifier-1 is §13.18.53.4 GR1's implicit MOVE sender: bound HERE through the ONE operand
                    // binder a MOVE's sending operand takes, which is what knows a subscript, a reference modification,
                    // a counter of this or another report, a sum counter of the current report, a VARYING counter and a
                    // constant-name (kb/Work PB1292 × PB1306 × PB1316 × PB1456).
                    foreach (var rs in f.Sources.OfType<FieldReferenceSource>())
                    {
                        if (rs.Rejected) continue;
                        using var at = ctx.Edition.At(rs.Ref);
                        var operand = host.Expr.FieldOperand(rs.Ref);
                        // identifier-1 is a data item, so an index-name is no sender (§13.18.53.3 SR2 — a MOVE shall be
                        // valid, and §13.18.38.3 r7 admits an index-name in no MOVE).
                        if (host.Expr.ScreenIndexNameOperand(operand, rs.Written, $"RD '{r.Name}': SOURCE")) rs.Rejected = true;
                        else rs.Value = operand;
                    }
                    // A SOURCE operand written as arithmetic-expression-1, or as identifier-1 under the
                    // clause's ROUNDED phrase (§13.18.53.3 SR5) — §13.18.53.4 GR2's implicit COMPUTE. The
                    // expression binds HERE through the same BindExpr a procedure-division reference takes
                    // (the kb/Work PB482 argument: a subscript may be an index-name or an expression and has
                    // no value at data bind), and the ROUNDED phrase resolves through the ONE §14.7.4
                    // rounding-mode reader (kb/Work PB852).
                    foreach (var cs in f.Sources.OfType<FieldComputeSource>())
                    {
                        if (cs.Rejected) continue;
                        using var at = ctx.Edition.At(cs.Ctx);
                        cs.Value = host.Expr.BindExpr(cs.Ctx);
                        cs.Rounding = host.Expr.RoundingOf(cs.Rounded);
                    }
                    ctx.Refs.VaryingScope = null;
                }
            }
        foreach (var s in r.Sums)
        {
            foreach (var c in s.PresentWhenCtxs) s.PresentWhen.Add(Bind(c));
            // SUM addends (§13.18.54.3 SR5 — kb/Work PB482). An addend written as identifier-1 is an
            // ORDINARY IDENTIFIER (§8.4.3.1.2 Format 2, qualified-data-name-with-subscripts), so its VALUE
            // is bound HERE, through the same `BindExpr` a procedure-division reference takes — which is
            // what makes `SUM WS-CELL(2)`, `SUM WS-CELL(IX)` and `SUM WS-CELL(IX + 1)` resolve at all: a
            // subscript may be an arithmetic expression or an index-name, and neither has a value at data
            // bind. A REJECTED addend is skipped: its rule has already been named, and re-binding it would
            // report the same words twice under a second clause.
            foreach (var t in s.Terms)
                foreach (var a in t.Addends)
                    if (!a.Rejected)
                    {
                        using var at = ctx.Edition.At(a.Ctx);
                        a.Value = host.Expr.BindExpr(a.Ctx);
                    }
            // The SUM clause's own ROUNDED phrase (§13.18.54.2's trailing rounded-phrase) — §13.18.54.4 GR4
            // computes the counter's delivery to the printable item "according to the general rules for the
            // COMPUTE statement with the ROUNDED phrase". Same §14.7.4 reader as SOURCE's (kb/Work PB852).
            s.Rounding = host.Expr.RoundingOf(s.Rounded);
        }
    }

    /// <summary>The scope a report entry's clause expression binds in (<see cref="ReferenceResolver.VaryingScope"/>).</summary>
    private static Dictionary<string, ReportVaryingModel> ScopeOf(IEnumerable<ReportVaryingModel> counters)
    {
        var scope = new Dictionary<string, ReportVaryingModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in counters) scope[c.Name] = c;
        return scope;
    }

    /// <summary>A bare reference to a VARYING counter in scope (ISO §13.18.64.4 GR3/GR4 — "this allows data-name-1 to be
    /// used as a source data item, as a subscript to a source data item …"): the compose-local integer the placement
    /// declares. Null for any other reference, and always outside a report entry's clause (§13.18.64.3 SR2 — a counter
    /// may be referenced only within its entry or a subordinate entry). Asked by the operand and expression binders
    /// right after the report counters, ahead of name resolution: a counter is "not defined elsewhere in the source
    /// element" (SR2), so no ordinary name can answer the word.</summary>
    public BoundExpr? VaryingExpr(Core.DataReferenceContext dref) =>
        ctx.Refs.VaryingScope is { } scope && dref.dataReferenceSuffix().Length == 0
            && dref.cobolWord() is { } w && scope.TryGetValue(w.GetText(), out var counter)
            ? new BoundReportVaryingRef(counter.CsName) : null;

    /// <summary><c>INITIATE report-name…</c> (ISO §14.9.21): each name shall be an RD entry (SR1); a multi-name
    /// statement IS a separate INITIATE statement per report-name in written order (§14.9.21.4 GR5) — one
    /// <see cref="BoundInitiate"/> per name inside a <see cref="BoundImplicitSeries"/>, so GR5's second sentence
    /// ("processing resumes at the next implicit INITIATE statement, if any") has a boundary to land on.</summary>
    public BoundStatement BindInitiate(Core.InitiateStatementContext stmt)
    {
        if (RejectInBeforeReporting("INITIATE") is { } refused) return refused;
        var members = new List<BoundStatement>();
        foreach (var rn in stmt.reportName())
        {
            if (RwFindReport(rn.GetText()) is not { } r)
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule, $"INITIATE '{rn.GetText()}': report-name-1 shall be defined by a report "
                    + "description entry in the report section (ISO §14.9.21.3 SR1)");
            if (RejectContainedReportFile(r, $"INITIATE {r.Name}", "ISO §14.9.21.3 SR2") is { } notGlobal) return notGlobal;
            members.Add(new BoundInitiate([r]));
        }
        return BoundImplicitSeries.Of(members);
    }

    /// <summary><c>GENERATE {data-name | report-name}</c> (ISO §14.9.16): a detail report group (SR1 — detail
    /// reporting) or a report-name whose RD has a CONTROL clause (SR2 — summary reporting, GR2).</summary>
    public BoundStatement BindGenerate(Core.GenerateStatementContext stmt)
    {
        if (RejectInBeforeReporting("GENERATE") is { } refused) return refused;
        var (name, qualifier) = ReportGroupResolution.Parts(stmt.reportGroupReference());
        // The report-name form (SR2, summary reporting) has NO qualifier — §8.4.2.2 gives a report-name no
        // qualifier at all — so a qualified operand can only be the data-name form and skips this arm.
        if (qualifier is null && RwFindReport(name) is { } summary)
        {
            if (summary.Controls.Count == 0)
                ctx.Edition.Error(DiagnosticCatalog.ReportGenerateNeedsControl, $"GENERATE {name}: the report-name form requires a CONTROL "
                    + "clause in the report description entry (ISO §14.9.16.3 SR2)");
            if (RejectContainedReportFile(summary, $"GENERATE {name}", "ISO §14.9.16.3 SR4") is { } notGlobal) return notGlobal;
            return new BoundGenerate(summary, null);   // summary reporting (GR2)
        }
        // SR1's data-name form, through the ONE funnel: it owns the qualified spelling ("It may be qualified by
        // a report-name") AND the §8.4.2.2.3 SR1 ambiguity this loop used to resolve by writing order (PB365).
        string where = $"GENERATE {name}{(qualifier is null ? "" : $" OF {qualifier}")}";
        if (ReportGroupResolution.Resolve(ctx.Edition, ctx.Data.VisibleReports, name, qualifier, where,
                out var report, out var group, ctx.Data) != ReportGroupResolution.Match.None)
        {
            if (group!.Kind != ReportGroupKindModel.Detail)
                ctx.Edition.Error(DiagnosticCatalog.ReportGenerateNotDetail, $"{where}: the named report group is not a "
                    + "DETAIL group (ISO §14.9.16.3 SR1)");
            // SR3's REPORT half ("the report description entry in which data-name-1 is specified ... shall contain a
            // GLOBAL clause") holds by construction — a container's non-GLOBAL report is not visible here at all,
            // so its groups resolve to nothing (§13.18.27.4 GR2). The FILE half is asked here.
            if (RejectContainedReportFile(report!, where, "ISO §14.9.16.3 SR3") is { } notGlobal) return notGlobal;
            return new BoundGenerate(report!, group);
        }
        // Match.None is UNDIAGNOSED by contract ("leaves the 'this is not a report group' diagnostic to the
        // caller") — so this is the report, never a deferral (kb/Work PB909).
        return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule, $"{where}: '{name}' names neither a detail report group (ISO §14.9.16.3 SR1) "
            + "nor a report description entry (SR2)");
    }

    /// <summary><c>TERMINATE report-name…</c> (ISO §14.9.46 SR1; §14.9.46.4 GR4) — one <see cref="BoundTerminate"/> per
    /// report-name inside a <see cref="BoundImplicitSeries"/>, GR4's "as though a separate TERMINATE statement had
    /// been executed for each report-name-1" and its per-implicit-statement resumption point.</summary>
    public BoundStatement BindTerminate(Core.TerminateStatementContext stmt)
    {
        if (RejectInBeforeReporting("TERMINATE") is { } refused) return refused;
        var members = new List<BoundStatement>();
        foreach (var rn in stmt.reportName())
        {
            if (RwFindReport(rn.GetText()) is not { } r)
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementOperandRule, $"TERMINATE '{rn.GetText()}': report-name-1 shall be defined by a report "
                    + "description entry in the report section (ISO §14.9.46.3 SR1)");
            if (RejectContainedReportFile(r, $"TERMINATE {r.Name}", "ISO §14.9.46.3 SR2") is { } notGlobal) return notGlobal;
            members.Add(new BoundTerminate([r]));
        }
        return BoundImplicitSeries.Of(members);
    }

    /// <summary><c>SUPPRESS PRINTING</c> (ISO §14.9.45): inhibit the current instance's printing of the report
    /// group named by the USE BEFORE REPORTING procedure in which this SUPPRESS lexically appears. §14.9.45.4 GR1
    /// makes that group a STATIC property of the enclosing declarative, so it is resolved HERE at bind time: the
    /// declarative whose pc range covers the current bind cursor (a Format-2 <c>ReportGroup</c> scope) fixes the
    /// group, and its owning report drives the emitted engine call. §14.9.45.3 SR1 — a SUPPRESS outside any USE
    /// BEFORE REPORTING procedure has no group to inhibit and is rejected (the per-instance suppression itself is
    /// the runtime half, GR2 — the report engine's one-shot flag).</summary>
    public BoundStatement BindSuppress(Core.SuppressStatementContext stmt)
    {
        // The containing declarative comes from the ONE bind-position probe (kb/Work PB403) — SUPPRESS's
        // §14.9.45.3 SR1 is a placement rule like §14.9.14.3 SR2 and §14.9.33.3 SR1/SR2, and each of them used to
        // walk ctx.Table.Declaratives itself. Ranges are disjoint, so "the declarative containing the cursor,
        // if it is a USE BEFORE REPORTING one" is the same set as the old "first declarative that has a report
        // group AND contains the cursor".
        if (ctx.Enclosing.Declarative?.ReportGroup is not { } group)
        {
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ReportSuppressContext,
                "SUPPRESS PRINTING may appear only in a USE BEFORE REPORTING procedure (ISO §14.9.45.3 SR1)");
        }
        var report = ctx.Data.VisibleReports.First(r => r.Groups.Contains(group));
        return new BoundSuppress(report, group);
    }

    /// <summary>⛔ THE ONE CHECK of the rule the three report-driving verbs each restate (kb/Work PB369): "If
    /// report-name-1 is defined in a containing program, the file description entry associated with report-name-1
    /// shall contain a GLOBAL clause" — §14.9.21.3 SR2 (INITIATE), §14.9.46.3 SR2 (TERMINATE), §14.9.16.3 SR4
    /// (GENERATE report-name) and the file half of §14.9.16.3 SR3 (GENERATE data-name). The report itself is
    /// visible only because it is GLOBAL (<see cref="DataBinder.VisibleReports"/>); the statement drives output to
    /// its FILE, which a contained program may reference only when the FD is GLOBAL too (§13.18.27.3 SR3). Null
    /// when the statement may proceed — the report is this unit's own, or its file is GLOBAL.</summary>
    private BoundStatement? RejectContainedReportFile(ReportModel report, string where, string rule) =>
        ctx.Data.ReportDepth(report) > 0 && report.File is { IsGlobal: false } file
            ? BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ContainedReportFileNotGlobal,
                $"{where}: report '{report.Name}' is defined in a containing program and its file description entry "
                + $"'{file.CobolName}' does not contain a GLOBAL clause; the file description entry associated with a "
                + $"containing program's report shall contain a GLOBAL clause ({rule})")
            : null;

    /// <summary>ISO §14.9.49.3 SR10 (kb/Work PB363): "The GENERATE, INITIATE, or TERMINATE statements shall not
    /// appear in a paragraph within a USE BEFORE REPORTING procedure." ONE guard read by all three verbs, over the
    /// ONE bind-position probe SUPPRESS's converse rule (§14.9.45.3 SR1) reads, so the three cannot drift apart.
    /// The rule is lexical ("appear in a paragraph"); the same question asked of a statement REACHED through a
    /// PERFORM at run time is §14.9.49.4 GR10's EC-FLOW-REPORT (kb/Work PB326). Null when the statement may
    /// proceed.</summary>
    private BoundStatement? RejectInBeforeReporting(string verb) =>
        ctx.Enclosing.InBeforeReportingProcedure
            ? BoundRejected.Report(ctx.Edition, DiagnosticCatalog.UseBeforeReportingRestriction,
                $"{verb}: this statement is in USE BEFORE REPORTING declarative section "
                + $"'{ctx.Enclosing.Declarative!.SectionName}' — \"The GENERATE, INITIATE, or TERMINATE statements "
                + "shall not appear in a paragraph within a USE BEFORE REPORTING procedure\" (ISO §14.9.49.3 SR10)")
            : null;

    /// <summary>ISO §14.9.49.3 SR11 (kb/Work PB363): "A USE BEFORE REPORTING procedure shall not alter the value of
    /// any control data item." Asked of one BOUND statement, through the ONE store classification
    /// (<see cref="BoundStores.StoresInto"/>), by <c>StatementBinder.BindStatement</c> for every statement bound in a
    /// USE BEFORE REPORTING procedure — so every verb that can store is covered by the classification's
    /// totality, never by a per-verb list here.
    /// <para>"Alter the value" is read over the storage the control data item occupies: a store into the item, into
    /// a group that contains it, or into an item subordinate to it (a MOVE to the record that holds the control
    /// field alters the field). "Any control data item" is literal — the control data items of every report of
    /// the source element, not only the report whose group the procedure names.</para></summary>
    /// <returns>True HAVING REPORTED a violation.</returns>
    public bool CheckControlDataStores(BoundStatement core)
    {
        List<DataItem>? controls = null;
        foreach (var r in ctx.Data.VisibleReports)
            foreach (var c in r.Controls)
                if (c.Item is { } item) (controls ??= []).Add(item);
        if (controls is null) return false;

        DataItem? hit = null;
        if (!BoundStores.StoresInto(core, stored =>
            {
                foreach (var c in controls)
                    if (SameStorageLine(stored, c)) { hit = c; return true; }
                return false;
            }))
            return false;
        ctx.Edition.Error(DiagnosticCatalog.UseBeforeReportingRestriction,
            $"this statement alters control data item '{hit!.CobolName}' in USE BEFORE REPORTING declarative section "
            + $"'{ctx.Enclosing.Declarative!.SectionName}' — \"A USE BEFORE REPORTING procedure shall not alter the "
            + "value of any control data item\" (ISO §14.9.49.3 SR11)");
        return true;
    }

    /// <summary>Is one of the two items the other or an ancestor of it — the hierarchy in which a store into one
    /// alters the other's value?</summary>
    private static bool SameStorageLine(DataItem a, DataItem b)
    {
        for (var x = b; x is not null; x = x.Parent) if (ReferenceEquals(x, a)) return true;
        for (var x = a.Parent; x is not null; x = x.Parent) if (ReferenceEquals(x, b)) return true;
        return false;
    }

    private ReportModel? RwFindReport(string name) =>
        ctx.Data.VisibleReports.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Intercept a LINE-COUNTER / PAGE-COUNTER data reference (ISO §8.4.3.15) ahead of normal name
    /// resolution (the LINAGE-COUNTER idiom in <c>FieldOperand</c>/<c>RefExpr</c>). Returns null when the
    /// reference is NOT a counter; a <see cref="BoundReportCounterRef"/> when it resolves (the OF/IN
    /// <c>cobolWord</c> is the report-name qualifier, SR2/§8.4.2.2 — unqualified resolves only against a sole
    /// report); a <see cref="BoundExprError"/> (with a bind diagnostic) for a counter that cannot resolve.</summary>
    public BoundExpr? CounterExpr(Core.DataReferenceContext dref)
    {
        bool isPage = dref.PAGE_COUNTER() is not null;
        if (!isPage && dref.LINE_COUNTER() is null) return null;
        string reg = isPage ? "PAGE-COUNTER" : "LINE-COUNTER";
        return CounterReportOf(dref, reg) is { } report
            ? new BoundReportCounterRef(report, isPage, ctx.Data.ReportDepth(report))
            : BoundExprError.Refused(ctx.Edition, $"{reg} reference '{DataBinder.WrittenText(dref)}'");
    }

    /// <summary>The <see cref="ReportPageCounterPlace"/> for a PAGE-COUNTER RECEIVING reference (ISO §8.4.3.15.3
    /// SR1 — "In the procedure division, PAGE-COUNTER and LINE-COUNTER may be referenced in any context where an
    /// integer data item may appear"; SR3 removes LINE-COUNTER, and only LINE-COUNTER, from the receiving side,
    /// which <c>ExpressionBinder.ResolveReceiving</c> screens before it reaches here). Null when the reference
    /// does not resolve to a report — the qualification diagnostic has then already been raised, exactly as on
    /// the sending side, because BOTH directions ask <see cref="CounterReportOf"/> (kb/Work PB429).</summary>
    public Place? CounterPlace(Core.DataReferenceContext dref) =>
        CounterReportOf(dref, "PAGE-COUNTER") is { } report
            ? new ReportPageCounterPlace(report.CsIndex, report.PageCounterRegister, ctx.Data.ReportDepth(report))
            : null;

    /// <summary>⛔ THE ONE RESOLUTION OF A COUNTER REFERENCE TO ITS REPORT, read by the SENDING
    /// (<see cref="CounterExpr"/>) and RECEIVING (<see cref="CounterPlace"/>) directions alike: the OF/IN
    /// <c>cobolWord</c> is the report-name qualifier (ISO §8.4.3.15 SR2 / §8.4.2.2), and an unqualified counter
    /// resolves only when the program has exactly one report. Null — with the diagnostic already raised — when
    /// the qualifier names no report, when no report description entry exists, or when an unqualified reference
    /// is ambiguous. Written once so a program cannot be told the qualification rule on one side of a statement
    /// and a different one on the other (kb/Work PB429).</summary>
    private ReportModel? CounterReportOf(Core.DataReferenceContext dref, string reg)
    {
        if (dref.cobolWord() is { } q)   // qualified: COUNTER OF/IN report-name
        {
            if (RwFindReport(q.GetText()) is { } named) return named;
            ctx.Edition.Error(DiagnosticCatalog.ReportCounterQualification, $"{reg} OF '{q.GetText()}': the qualifier shall name a report "
                + $"description entry — a counter of a DIFFERENT report is referenced \"qualified explicitly by the report-name "
                + $"associated with the different report\" (ISO {CounterQualificationRule(reg)}; the qualification requirements "
                + "for the counters are those of §8.4.2.2, §8.4.3.15.3 SR2)");
            return null;
        }
        // §8.4.2.2.3 SR9/SR10 — "In the report section, an unqualified reference to the [LINE-COUNTER | PAGE-COUNTER] is
        // qualified implicitly by the name of the report in whose report description entry the reference is made": the
        // enclosing RD answers, whatever number of reports the program has (kb/Work PB1456).
        if (ctx.Refs.ReportScope is { } enclosing) return enclosing;
        // §8.4.6.2.5 — the counters of a GLOBAL report are global; §8.4.6.2.1 rule 3 — the nearest declaring
        // source element's reports hide a container's, so "exactly one report" is asked of the nearest ones.
        var nearest = ctx.Data.NearestInScope(ctx.Data.VisibleReports, r => r);
        if (nearest.Count == 1) return nearest[0];
        ctx.Edition.Error(DiagnosticCatalog.ReportCounterQualification, nearest.Count == 0
            ? $"{reg} referenced, but the program has no report description entry (ISO §8.4.3.15.1 — the "
              + "counters are generated per report)"
            : $"unqualified {reg} with more than one report description entry: {reg} shall be qualified each time it is "
              + $"referenced in the procedure division if more than one report description entry is specified in the "
              + $"source element (ISO {CounterQualificationRule(reg)})");
        return null;
    }

    /// <summary>The syntax rule that qualifies the register <paramref name="reg"/> — one rule per counter, written as
    /// the standard writes it (§8.4.2.2.3 SR9 for LINE-COUNTER, SR10 for PAGE-COUNTER).</summary>
    private static string CounterQualificationRule(string reg) =>
        reg == "PAGE-COUNTER" ? "§8.4.2.2.3 SR10" : "§8.4.2.2.3 SR9";
}
