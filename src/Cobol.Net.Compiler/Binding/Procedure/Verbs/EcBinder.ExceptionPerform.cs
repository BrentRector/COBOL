// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime.Tree;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>
/// The exception-checking (Format-3) PERFORM binder (ISO §14.9.28 Format 3, COBOL-2023). Produces a
/// <see cref="BoundExceptionPerform"/> and emits the Format-3 syntax-rule + cross-statement-ban diagnostics
/// (COBOLNET1597-1617; see <c>docs/rearchitecture/evidence/PHASE-13-c5-perform-format3-DESIGN.md</c>). The version
/// gate (COBOLNET0900, below 2023) is the VersionConformancePass's <c>VisitPerformStatement</c> arm — not here.
/// </summary>
internal sealed partial class EcBinder
{
    /// <summary>Bind an inline PERFORM that carries a WHEN / FINALLY phrase or a LOCATION head (§14.9.28 Format 3).
    /// imp-1 binds under the GR14 implicit-TURN overlay (the WHEN-named ECs enabled over its extent); imp-2/3/4
    /// bind inside an <see cref="EnclosingConstruct.PerformWhen"/> frame (RESUME NEXT STATEMENT is legal there,
    /// §14.9.33.3 SR1); imp-5 (FINALLY) binds inside a <see cref="EnclosingConstruct.PerformFinally"/> frame
    /// instead, because RESUME is not admitted in a FINALLY phrase. imp-2..5 bind against the BASE TurnState
    /// (GR21).
    /// <para>The whole statement — imp-1, every handler and the FINALLY phrase — binds inside ONE
    /// <see cref="EnclosingConstruct.ExceptionCheckingPerform"/> frame, which is what §14.9.14.3 SR8 and
    /// §14.9.14.4 GR4 ask about: an EXIT PERFORM written anywhere in it exits THIS PERFORM (kb/Work PB403).</para></summary>
    public BoundStatement EcBindExceptionPerform(Core.PerformStatementContext p)
    {
        ctx.EcState.F3Perform = true;   // this unit installs the F3-frame interceptor (emitter gate — §14.9.28)
        using var f3Frame = ctx.EnterConstruct(EnclosingConstruct.ExceptionCheckingPerform);

        var whenPhrases = p.performWhenPhrase();
        bool withLocation = p.performInlineHead()?.performLocationPhrase()?.LOCATION() is not null;

        // PF3-STRUCT-WHEN-REQUIRED (COBOLNET1597): ≥1 ordinary WHEN — the outer brace is a required group in the
        // §14.9.28.2 Format-3 general format (PDF-resolved). WHEN OTHER / WHEN COMMON / FINALLY alone is not admitted.
        if (whenPhrases.Length == 0)
            ctx.Edition.Error("COBOLNET1597", "an exception-checking (Format-3) PERFORM requires at least one WHEN "
                + "phrase (ISO §14.9.28.2 Format 3)");

        // Resolve each WHEN's operands (mode / exception-name / file) → the descriptor header, the GR14 overlay
        // enables, and the SR14/SR15 operand census.
        var headers = new List<(string? Mode, List<BoundWhenOperand> Ops)>();
        var overlay = new List<(string Ec, string? File)>();
        var census = new List<(string? Ec, string? File)>();
        foreach (var w in whenPhrases)
            headers.Add(ResolveWhenOperands(w, overlay, census));

        CheckSr14Sr15(census);

        // GR14 overlay, part 1 of 2: imp-1 binds with the WHEN-named ECs implicitly enabled over its extent
        // (WITH LOCATION iff the PERFORM specifies LOCATION). Popped after imp-1 — and then REPLACED by the
        // TURN OFF ALL floor below, which is GR14's other half. imp-2..5 do NOT bind against the base state.
        var savedTurn = ctx.EcState.Turn;
        // imp-1's line ≈ the PERFORM statement's line (imp-1's statements are at ≥ this line, pre-PERFORM
        // directives at < it) — the GR14 synthetic is placed here so a pre-PERFORM >>TURN OFF loses to it.
        ctx.EcState.Turn = savedTurn.WithImplicitEnable(overlay, withLocation, p.Start.Line);
        var imp1 = host.BindBlocks([p.statementBlock()]);
        ctx.EcState.Turn = savedTurn;

        CheckCrossStatementBans(p);   // parse-subtree walks — order-independent; run once for both paths below

        // An F3 PERFORM inside an OO method binds its handler pc-ranges EXACTLY like a program's (design SSOT §9.10):
        // AddF3Handler records ctx.CurrentMethodScope so StatementBinder.BindMethodRoster stamps each method's
        // contiguous handler sub-range, and OoEmitter emits the method-LOCAL __RunUse/__RunF3 + the two-range
        // __MDispatch + the entry frame FLOOR. (Formerly rejected loud COBOLNET0899 — the F3StagedInMethodStub gap,
        // lifted here.)

        // imp-2/3/4 (WHEN / OTHER / COMMON bodies) bind IN LEXICAL CONTEXT with InF3When (RESUME-NEXT relaxation)
        // and are REDIRECTED into synthetic pc-range paragraphs (the pc-RANGE interceptor, §9.1-B) run by the
        // reused __RunUse; imp-5 (FINALLY) stays inline.
        //
        // ⛔ THEY BIND UNDER TURN OFF ALL, not under the base state. §14.9.28.4 GR14: "An implicit PUSH ALL
        // followed by TURN OFF ALL is assumed at the END of imperative-statement-1. Immediately preceding the END
        // PERFORM phrase, there is an implicit POP ALL …" — and imp-2..5 all run between those two points, so no
        // checking is in effect inside them (§14.6.13.1.1: "if checking for an exception that occurs is not
        // enabled, no exception condition is raised"). This code previously restored the BASE state here and cited
        // GR21, which says only that an exception raised in imp-2..5 does not transfer control back into them —
        // re-entry, not checking. A pre-PERFORM `>>TURN ec ON` therefore leaked into every handler body and
        // EcWrap wrapped handler statements in BoundEcChecked that the standard says cannot fire.
        //
        // The floor is spliced at the FIRST HANDLER's line (the END-PERFORM line when there is no WHEN), so a real
        // `>>TURN` written INSIDE a handler still sorts after it and still wins — GR14 disables, it does not
        // freeze. Restored to savedTurn after imp-5 so GR22 governs what survives the PERFORM.
        int performId = ctx.EcState.NextF3PerformId();
        int line = p.Start.Line;
        int handlerLine = whenPhrases.Length > 0 ? whenPhrases[0].Start.Line : p.Stop.Line;
        ctx.EcState.Turn = savedTurn.WithAllDisabledFrom(handlerLine);
        var whens = new List<BoundExceptionMatch>();
        for (int i = 0; i < whenPhrases.Length; i++)
        {
            int pc = BindHandler([whenPhrases[i].statementBlock()], performId, line);
            whens.Add(new BoundExceptionMatch(headers[i].Mode, headers[i].Ops, pc));
        }
        int? otherPc = p.performWhenOther() is { } o ? BindHandler([o.statementBlock()], performId, line) : null;
        int? commonPc = p.performWhenCommon() is { } c ? BindHandler([c.statementBlock()], performId, line) : null;
        IReadOnlyList<BoundStatement>? final = null;
        if (p.performFinally() is { } f)
        {
            using var finallyFrame = ctx.EnterConstruct(EnclosingConstruct.PerformFinally);
            final = host.BindBlocks([f.statementBlock()]);
        }
        ctx.EcState.Turn = savedTurn;   // GR14's implicit POP ALL precedes END-PERFORM; GR22 governs from here

        bool handlerHasExit = HandlerBodiesContainExitPerform(p);
        return new BoundExceptionPerform(imp1, whens, otherPc, commonPc, final, withLocation, performId, handlerHasExit);
    }

    /// <summary>Bind ONE handler body (imp-2/3/4) inside a <see cref="EnclosingConstruct.PerformWhen"/> frame and
    /// register it as a synthetic pc-range paragraph. The frame is what §14.9.33.3 SR1 / §14.9.14.3 SR6 /
    /// §14.9.18.3 SR5 mean by "a WHEN phrase"; ONE method, so the three handler kinds cannot acquire three
    /// different answers to it.</summary>
    private int BindHandler(Core.StatementBlockContext[] body, int performId, int line)
    {
        using var whenFrame = ctx.EnterConstruct(EnclosingConstruct.PerformWhen);
        return ctx.Table.AddF3Handler(host.BindBlocks(body), performId, line);
    }

    /// <summary>True when any handler body (imp-2/3/4 — the WHEN / WHEN OTHER / WHEN COMMON statement blocks) contains
    /// a plain <c>EXIT PERFORM</c> (recursion stops at a nested inline <c>performStatement</c>, whose EXIT PERFORM
    /// refers to that inner loop, §14.9.14.4 GR5a). Gates the emitted <c>catch (ExitPerformSignal)</c> boundary so a
    /// PERFORM whose handlers never EXIT-PERFORM emits no catch.</summary>
    private static bool HandlerBodiesContainExitPerform(Core.PerformStatementContext p)
    {
        var bodies = p.performWhenPhrase().Select(w => (IParseTree)w.statementBlock())
            .Concat(p.performWhenOther() is { } o ? [o.statementBlock()] : (IParseTree[])[])
            .Concat(p.performWhenCommon() is { } c ? [c.statementBlock()] : (IParseTree[])[]);
        return bodies.Any(ExitPerformOfThisPerform);
    }

    // A plain EXIT PERFORM (no CYCLE) whose nearest enclosing inline PERFORM is THIS Format-3 PERFORM (recursion
    // stops at a nested performStatement, which owns its own EXIT PERFORM — §14.9.14.4 GR5a).
    private static bool ExitPerformOfThisPerform(IParseTree node)
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is Core.ExitStatementContext ex && ex.PERFORM() is not null && ex.CYCLE() is null)
                return true;
            if (child is Core.PerformStatementContext) continue;   // a nested inline PERFORM owns its own EXIT PERFORM
            if (ExitPerformOfThisPerform(child)) return true;
        }
        return false;
    }

    /// <summary>Resolve ONE WHEN phrase's operands. The mode form (WHEN EXCEPTION INPUT|OUTPUT|I-O|EXTEND) is a
    /// STAGED runtime match (COBOLNET0899, §5.4-1). The name/file forms resolve each exception-name against the
    /// §14.6.13.1 catalog at ANY level (the USE §14.9.49.4 GR3a-3g tiers select by level — NOT the RAISE level-3-only rule),
    /// enforce SR16 (a FILE-paired name shall begin EC-I-O), and the per-name edition window. Contributes GR14
    /// overlay enables and the SR14/SR15 census.</summary>
    private (string? Mode, List<BoundWhenOperand> Ops) ResolveWhenOperands(
        Core.PerformWhenPhraseContext w, List<(string, string?)> overlay, List<(string?, string?)> census)
    {
        var ops = new List<BoundWhenOperand>();

        // Alt 1: WHEN EXCEPTION { INPUT | OUTPUT | I-O | EXTEND | file-name-1… }
        if (w.performWhenModeList() is { } ml)
        {
            string? mode = ml.INPUT() is not null ? "INPUT" : ml.OUTPUT() is not null ? "OUTPUT"
                : ml.I_O() is not null ? "I-O" : ml.EXTEND() is not null ? "EXTEND" : null;
            if (mode is not null)
            {
                // The open-mode operand form is IMPLEMENTED, and this comment used to say otherwise (kb/Work
                // PB595's second finding, re-probed 2026-09-21): it called the runtime match a staged sub-GAP
                // "covered by the one COBOLNET0899 raised at the top of EcBindExceptionPerform", and no
                // COBOLNET0899 is raised anywhere on the Format-3 path any more — the F3-in-a-method staged
                // reject that used to raise it was lifted (see above). The match itself lives where §14.9.28.4
                // GR17 puts it, at the raise site, through the USE tiers ("The rules for determining a match are
                // specified in General rules 3a to 3g of the USE statement"): ControlFlowEmitter emits this form
                // as the GR3b tier-1 arm, `IsIoName(__ec) && __f is not null && OpenModeOf(__f) == <mode>`.
                // Measured: a READ past end-of-file inside `PERFORM … WHEN EXCEPTION INPUT` runs the handler.
                // What this arm contributes at BIND time is the GR14 overlay enable for EC-I-O over imp-1.
                overlay.Add(("EC-I-O", null));   // enable EC-I-O checking over imp-1 for the eventual runtime
                return (mode, ops);
            }
            foreach (var fn in ml.fileName())
                if (ResolveWhenFile(fn.GetText()) is { } f)
                {
                    ops.Add(new BoundWhenOperand(null, f));
                    overlay.Add(("EC-I-O", f.CobolName));
                    census.Add((null, f.CobolName));
                }
            return (null, ops);
        }

        // Alt 2: WHEN { exception-name-1 | exception-name-2 FILE file-name-2… }…
        foreach (var item in w.performWhenEcList()!.performWhenEcItem())
        {
            string raw = item.cobolWord().GetText();
            // Resolution + the 0711/0878/1636 diagnostics live in the ONE funnel (kb/Work R05).
            if (!EcNameResolution.TryResolve(ctx.Edition, raw, $"WHEN '{raw}'", out var info)) continue;
            var files = item.fileName();
            if (files.Length > 0 && !ExceptionCatalog.IsIoName(info.Name))
            {
                // PF3-SR16 (COBOLNET1601): if file-name-2 is specified, exception-name-2 shall begin 'EC-I-O'.
                ctx.Edition.Error("COBOLNET1601", $"WHEN exception-name '{info.Name}' is paired with FILE but does "
                    + "not begin with the COBOL characters 'EC-I-O' (ISO §14.9.28.3 SR16)");
                continue;
            }
            if (files.Length == 0)
            {
                ops.Add(new BoundWhenOperand(info.Name, null));
                overlay.Add((info.Name, null));
                census.Add((info.Name, null));
                continue;
            }
            foreach (var fn in files)
                if (ResolveWhenFile(fn.GetText()) is { } f)
                {
                    ops.Add(new BoundWhenOperand(info.Name, f));
                    overlay.Add((info.Name, f.CobolName));
                    census.Add((info.Name, f.CobolName));
                }
        }
        return (null, ops);
    }

    private FileModel? ResolveWhenFile(string name)
    {
        if (ctx.Data.FilesByName.TryGetValue(name, out var file)) return file;
        ctx.Edition.Error("COBOLNET0897", $"WHEN names unknown file '{name}' (ISO §14.9.28.2 Format 3)");
        return null;
    }

    /// <summary>PF3-SR14 (COBOLNET1599) — a file-name shall not appear more than once across the WHEN phrases
    /// unless every instance is paired with an exception-name (§14.9.28.3 SR14). PF3-SR15 (COBOLNET1600) — an
    /// exception-name shall appear only once unless each occurrence pairs a DIFFERENT file-name (§14.9.28.3
    /// SR15).</summary>
    private void CheckSr14Sr15(List<(string? Ec, string? File)> census)
    {
        // SR14: group by file-name; a file that appears >1 time with ANY bare (unpaired) instance is illegal.
        foreach (var g in census.Where(c => c.File is not null).GroupBy(c => c.File!, CobolNames.Comparer))
            if (g.Count() > 1 && g.Any(c => c.Ec is null))
                ctx.Edition.Error("COBOLNET1599", $"file-name '{g.Key.ToUpperInvariant()}' is specified in more than one WHEN phrase "
                    + "without an exception-name pairing (ISO §14.9.28.3 SR14)");

        // SR15: an exception-name may repeat ONLY "in conjunction with different file-names". A repeat is illegal
        // if ANY occurrence is bare (no file — not "in conjunction with a file-name"), or two occurrences share a
        // file. A bare occurrence's null is NOT a licensing "distinct file-name" (so bare+FILE-paired same name is
        // rejected, not just bare+bare).
        foreach (var g in census.Where(c => c.Ec is not null).GroupBy(c => c.Ec!, CobolNames.Comparer))
        {
            if (g.Count() <= 1) continue;
            var files = g.Select(c => c.File).ToList();
            if (files.Any(f => f is null) || files.Select(f => f!).Distinct(CobolNames.Comparer).Count() != files.Count)
                ctx.Edition.Error("COBOLNET1600", $"exception-name '{g.Key.ToUpperInvariant()}' is specified more than once without a "
                    + "distinct file-name (ISO §14.9.28.3 SR15)");
        }
    }

    /// <summary>The cross-statement bans (§14.9.28 + the cited sibling-statement SRs), by lexical region over the
    /// PARSE subtree of the Format-3 PERFORM. Region A = imperative-statement-1; region B = the whole PERFORM;
    /// region C = the WHEN phrases (imp-2/3/4); region D = imperative-statement 2..5.</summary>
    private void CheckCrossStatementBans(Core.PerformStatementContext p)
    {
        // Region content (statement-block subtrees).
        var regionA = new IParseTree[] { p.statementBlock() };
        var whenBodies = p.performWhenPhrase().Select(w => (IParseTree)w.statementBlock())
            .Concat(p.performWhenOther() is { } o ? [o.statementBlock()] : (IParseTree[])[])
            .Concat(p.performWhenCommon() is { } c ? [c.statementBlock()] : (IParseTree[])[]).ToList();   // region C
        var finallyBody = p.performFinally() is { } f ? new IParseTree[] { f.statementBlock() } : [];
        var regionD = whenBodies.Concat(finallyBody).ToList();
        var regionB = regionA.Concat(regionD).ToList();

        // ── Region B (whole PERFORM) ──
        // ⛔ XS-EXIT-PERFORM-CYCLE (COBOLNET1604, §14.9.14.3 SR8 sentence 2) IS NO LONGER CHECKED HERE, and its
        // ExitCyclesOfThisPerform walker is gone (kb/Work PB403). SR8 is ONE syntax rule about the EXIT PERFORM
        // statement — "may be specified only in an inline or exception-checking PERFORM statement. The CYCLE
        // phrase shall not be specified within an exception-checking PERFORM statement" — and it is now enforced
        // WHOLE at the one site that binds that statement, ControlFlowBinder.BindExit, against the enclosing
        // construct stack (ctx.Enclosing.InExceptionCheckingPerform). Splitting it left sentence 1 with no home
        // at all: an EXIT PERFORM outside every PERFORM compiled clean and emitted a bare `break;` that spun the
        // pc dispatcher forever. A rule written down in two places is how one of the two ends up empty.
        foreach (var init in regionB.SelectMany(Descendants<Core.InitiateStatementContext>).Where(s => s.reportName().Length > 1))
            ctx.Edition.Error("COBOLNET1605", "an INITIATE naming more than one report-name shall not appear within "
                + "an exception-checking PERFORM (ISO §14.9.21.3 SR3)");
        foreach (var term in regionB.SelectMany(Descendants<Core.TerminateStatementContext>).Where(s => s.reportName().Length > 1))
            ctx.Edition.Error("COBOLNET1606", "a TERMINATE naming more than one report-name shall not appear within "
                + "an exception-checking PERFORM (ISO §14.9.46.3 SR3)");
        foreach (var val in regionB.SelectMany(Descendants<Core.ValidateFacilityStatementContext>).Where(s => s.dataReference().Length > 1))
            ctx.Edition.Error("COBOLNET1607", "a VALIDATE naming more than one identifier shall not appear within an "
                + "exception-checking PERFORM (ISO §14.9.50.3 SR6)");

        // ── Region A (imperative-statement-1 only) ──
        // XS-DELETE-FILE-MULTI (§14.9.10.3 SR4) went LIVE with the Format-2 grammar (kb/Work PB134).
        foreach (var dfm in regionA.SelectMany(Descendants<Core.DeleteFileStatementContext>).Where(s => s.fileName().Length > 1))
            ctx.Edition.Error("COBOLNET1613", "a DELETE FILE naming more than one file-name shall not appear in "
                + "imperative-statement-1 of an exception-checking PERFORM (ISO §14.9.10.3 SR4)");
        foreach (var cl in regionA.SelectMany(Descendants<Core.CloseStatementContext>).Where(s => s.closeFilePhrase().Length > 1))
            ctx.Edition.Error("COBOLNET1612", "a multi-file CLOSE shall not appear in imperative-statement-1 of an "
                + "exception-checking PERFORM (ISO §14.9.6.3 SR3)");
        // ⛔ §14.9.20.3 SR2 AND §14.9.27.3 SR3 COUNT THE METAVARIABLE'S OCCURRENCES, NOT REPEATED VALUES
        // (kb/Work PB330, PB417). "Identifier-1 shall be specified only once" and "An OPEN statement that
        // specifies file-name-1 more than once" speak of the FORMAT TERM, which the general format repeats with
        // its ellipsis — the statement's own general rules use the same words for the multi-operand form:
        // §14.9.20.4 GR3 "If more than one identifier-1 is specified in an INITIALIZE statement" and §14.9.27.4
        // GR20 "If more than one file-name is specified in an OPEN statement", each expanding into separate
        // implicit statements. That is the family's shape — CLOSE (§14.9.6.3 SR3), DELETE FILE (§14.9.10.3 SR4),
        // INITIATE, TERMINATE and VALIDATE all ban the MULTI-OPERAND statement — and its purpose: an unsuccessful
        // implicit statement transfers control to the WHEN phrase and abandons the rest. Both checks used to
        // group the operands' parse TEXT for duplicates, so `INITIALIZE N M` and `OPEN OUTPUT F1 F2` compiled,
        // and one item spelled two ways (`N OF G` / `N IN G`) slipped past even the duplicate reading. A
        // duplicate is still rejected: it is one case of "more than once".
        foreach (var ini in regionA.SelectMany(Descendants<Core.InitializeStatementContext>)
                     .Where(s => s.initializeOperandList().dataReference().Length > 1))
            ctx.Edition.Error("COBOLNET1614", "an INITIALIZE naming more than one identifier-1 shall not appear in "
                + "imperative-statement-1 of an exception-checking PERFORM (ISO §14.9.20.3 SR2)");
        foreach (var _ in regionA.SelectMany(Descendants<Core.MergeStatementContext>))
            ctx.Edition.Error("COBOLNET1615", "MERGE shall not appear in imperative-statement-1 of an "
                + "exception-checking PERFORM (ISO §14.9.24.3 SR1)");
        // Counted over EVERY open-mode group: the outer ellipsis repeats the whole group, so file-name-1 is
        // specified more than once whenever the statement names two file-names in any groups (see above).
        foreach (var _ in regionA.SelectMany(Descendants<Core.OpenStatementContext>)
                     .Where(s => s.openClause().Sum(oc => oc.openFileSpec().Length) > 1))
            ctx.Edition.Error("COBOLNET1616", "an OPEN naming more than one file-name shall not appear in "
                + "imperative-statement-1 of an exception-checking PERFORM (ISO §14.9.27.3 SR3)");
        // ⛔ SR3 IS PRINTED UNDER THE `FORMAT 1` HEADING (§14.9.40.3: SR3-SR12 are Format 1, SR13-SR15 Format 2), so the
        // ban is on the FILE-format SORT; a table SORT is legal here and no other rule restricts where it may appear.
        // The parse node is shared by both formats and the split is semantic (SortBinder.IsFileFormat, kb/Work PB1139).
        foreach (var _ in regionA.SelectMany(Descendants<Core.SortStatementContext>).Where(s => SortBinder.IsFileFormat(ctx, s)))
            ctx.Edition.Error("COBOLNET1617", "SORT shall not appear in imperative-statement-1 of an "
                + "exception-checking PERFORM (ISO §14.9.40.3 SR3)");

        // ── Region C (WHEN phrases) ──
        foreach (var _ in whenBodies.SelectMany(Descendants<Core.GoToStatementContext>))
            ctx.Edition.Error("COBOLNET1608", "GO TO shall not appear in a WHEN phrase of an exception-checking "
                + "PERFORM (ISO §14.9.17.3 SR3)");

        // ── Region D (imperative-statement 2..5) ──
        foreach (var _ in regionD.SelectMany(Descendants<Core.RaiseStatementContext>))
            ctx.Edition.Error("COBOLNET1611", "RAISE shall appear only in imperative-statement-1 of an "
                + "exception-checking PERFORM (ISO §14.9.29.3 SR4)");

        // ── Region E (imperative-statement-5, the FINALLY phrase) ──
        // §14.9.28.4 GR16: "There shall be no statements that include a transfer of control out of the PERFORM
        // statement within imperative-statement-5" (kb/Work PB434). Every such statement is statically visible in
        // the subtree: a GO TO (a procedure-name never lies inside a statement), an EXIT naming a paragraph,
        // section, program, method or function, a GOBACK, a NEXT SENTENCE, and a STOP RUN — "control is transferred
        // to the operating system" (§14.9.42.4 GR6); §14.6.3 names STOP beside EXIT PROGRAM and GOBACK as the
        // statements that transfer control outside the source element, and a GOBACK in a main program "operates as
        // if executing a STOP statement" (§14.9.18.4 GR3), so the two cannot take different verdicts. EXIT PERFORM
        // stays admitted — GR16's third sentence gives it the implicit CONTINUE after END-PERFORM.
        foreach (var transfer in finallyBody.SelectMany(TransfersOut))
        {
            using var at = ctx.Edition.At(new DiagnosticCursor(transfer.Start.Line, transfer.Start.Column + 1));   // the statement, not the PERFORM
            ctx.Edition.Error(DiagnosticCatalog.ExceptionPerformFinallyTransferOut,
                $"'{DataBinder.WrittenText(transfer)}' transfers control out of the PERFORM statement, and it is written "
                + "in the FINALLY phrase (imperative-statement-5) of an exception-checking PERFORM (ISO §14.9.28.4 GR16)");
        }

        CheckDirectiveBans(p);   // region B, by LINE — a directive is not in the parse tree
    }

    /// <summary>The three COMPILER-DIRECTIVE bans on region B (the whole PERFORM), as ONE lexical-containment
    /// test — owner decision D20 (2026-07-19), which settled both the reading and the shape:
    /// <list type="bullet">
    /// <item>ISO §7.3.25.3 SR5 — "A TURN directive shall not be specified within an exception processing PERFORM
    /// statement."</item>
    /// <item>ISO §7.3.22.3 SR4 — "The PUSH directive shall not be specified within an exception checking PERFORM
    /// statement."</item>
    /// <item>ISO §7.3.20.3 SR4 — "The POP directive shall not be specified within an exception checking PERFORM
    /// statement."</item>
    /// </list>
    /// <para><b>FLAT BAN, SUPPRESSIBLE WARNING</b> (D20). The ban covers the WHOLE statement —
    /// imperative-statement-1 included, not only the handler phrases — because Annex D.16.4 uses "exception-
    /// processing PERFORM statement" for the PUSH/POP ban that is stated normatively as "exception checking", so
    /// the two phrasings are drafting synonyms and the narrow reading's sole tie-breaker fails. The program still
    /// COMPILES: §4.2.2 requires only "a warning mechanism that optionally may be invoked by the user at compile
    /// time to indicate violations of the general formats" for a syntax rule of this kind, and §14.9.28.4 GR14's
    /// semantics are implemented for the accepted case.</para>
    /// <para>⛔ BY LINE, AND THAT IS NOT A SHORTCUT. A compiler directive is removed by the preprocessor and is
    /// in no parse tree, so "lexically within this statement" is a question about POSITIONS — which is why the
    /// sites are recorded on the FINAL text (<c>DirectiveSiteProcessor</c>), the one frame where a directive line
    /// and a token line are the same number. ONE predicate, three directive words: a fourth directive with a
    /// position rule is one entry in <c>DirectiveSiteProcessor.PositionRuled</c> plus one row below.</para></summary>
    private void CheckDirectiveBans(Core.PerformStatementContext p)
    {
        if (ctx.EcState.DirectiveSites.Count == 0) return;
        int first = p.Start.Line, last = p.Stop.Line;
        foreach (var site in ctx.EcState.DirectiveSites)
        {
            if (site.Line < first || site.Line > last) continue;
            var ban = Array.Find(DirectiveBans,
                b => CobolNames.Same(b.Word, site.Word));
            if (ban.Word is null) continue;   // a position-ruled word this statement's rules do not ban
            // Nested Format-3 PERFORMs contain the same directive line; the rule is about the DIRECTIVE, so it
            // is one violation and one warning, reported by whichever bind reaches it first.
            if (!ctx.EcState.ReportedDirectiveBans.Add(site.Line)) continue;
            ctx.Edition.Warning(DiagnosticCatalog.DirectiveInExceptionCheckingPerform,
                $"the >>{site.Word} directive on line {site.Line} is written within an exception-checking "
                + $"(Format-3) PERFORM statement — \"{ban.Text}\" (ISO {ban.Clause} {ban.Rule}). The program "
                + "still compiles (ISO §4.2.2 requires a compile-time warning mechanism for a violation of the "
                + "syntax rules, not rejection). The checking state inside this statement is not the state the "
                + "surrounding text reads as: §14.9.28.4 GR14 assumes \"an implicit PUSH ALL followed by TURN "
                + "OFF ALL … at the end of imperative-statement-1\" and \"immediately preceding the END PERFORM "
                + "phrase … an implicit POP ALL\", so a directive written in a WHEN or FINALLY phrase is "
                + "discarded by that POP ALL instead of enabling checking \"for the procedure division "
                + "statements … that follow in the compilation group\" (§7.3.25.4 GR6), and one written in "
                + "imperative-statement-1 competes with the implicit TURN directives GR14 assumes around it. "
                + "Write it before the PERFORM, or after END-PERFORM.");
        }
    }

    /// <summary>⛔ ONE ROW PER BANNED DIRECTIVE — the three syntax rules as DATA, each with its own clause and
    /// its own words, so the diagnostic quotes the rule the programmer broke rather than an intersection of the
    /// three (the CorrespondingOperandRule discipline). D20 requires one predicate for all three; it does not
    /// require one sentence for all three.</summary>
    private static readonly (string Word, string Clause, string Rule, string Text)[] DirectiveBans =
    [
        ("TURN", "§7.3.25.3", "SR5",
            "A TURN directive shall not be specified within an exception processing PERFORM statement"),
        ("PUSH", "§7.3.22.3", "SR4",
            "The PUSH directive shall not be specified within an exception checking PERFORM statement"),
        ("POP", "§7.3.20.3", "SR4",
            "The POP directive shall not be specified within an exception checking PERFORM statement"),
    ];

    /// <summary>The statements in <paramref name="node"/>'s subtree that transfer control out of an enclosing PERFORM
    /// statement (§14.9.28.4 GR16's family): GO TO, EXIT PARAGRAPH / SECTION / PROGRAM / METHOD / FUNCTION, GOBACK,
    /// NEXT SENTENCE and STOP RUN (§14.9.42.4 GR6). A bare EXIT (Format 1, no operation), EXIT PERFORM and the
    /// obsolete STOP literal (which resumes at the next statement) are not members.</summary>
    private static IEnumerable<Antlr4.Runtime.ParserRuleContext> TransfersOut(IParseTree node) =>
        Descendants<Antlr4.Runtime.ParserRuleContext>(node).Where(s => s switch
        {
            Core.GoToStatementContext or Core.GobackStatementContext or Core.NextSentenceStatementContext => true,
            Core.StopStatementContext stop => stop.RUN() is not null,
            Core.ExitStatementContext e => e.PARAGRAPH() is not null || e.SECTION() is not null
                || e.PROGRAM() is not null || e.METHOD() is not null || e.FUNCTION() is not null,
            _ => false,
        });

    private static IEnumerable<T> Descendants<T>(IParseTree node) where T : class
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChild(i);
            if (child is T t) yield return t;
            foreach (var d in Descendants<T>(child)) yield return d;
        }
    }
}
