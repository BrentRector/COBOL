// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.CodeGen.Emit;
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.CodeGen;

using static CobolNet.CodeGen.Emit.EmitText;

/// <summary>The control-flow verb emitter (P7 Step 9i — a real collaborator over the per-unit
/// <see cref="EmitContext"/>): IF, inline/out-of-line PERFORM (TIMES/UNTIL/VARYING), serial SEARCH, and
/// GO TO … DEPENDING. The out-of-line PERFORM's bounded dispatch reads <see cref="DispatchState.DispatchName"/>;
/// VARYING/SEARCH index advances ride the ONE SET-target store pair on <see cref="SetEmitter"/>.</summary>
internal sealed class ControlFlowEmitter(EmitContext ctx, NumericRenderer num, ConditionRenderer cond,
    DispatchState dispatch, SetEmitter set)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (IF branches,
    /// inline-PERFORM bodies, and SEARCH arms nest arbitrary statement lists — a cyclic edge no ctor order
    /// can satisfy).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>The EC raise-site dispatcher (property-wired by <see cref="UnitEmitters"/>, the same cyclic edge as
    /// <see cref="Statements"/>) — a SEARCH with EC-RANGE checking ON but NO AT END phrase dispatches the raised
    /// range EC to an applicable USE declarative / Format-3 WHEN via <see cref="EcEmitter.EcDispatchExpr"/>
    /// (ISO §14.9.37.4 GR1b2; CA36).</summary>
    internal EcEmitter Ec { get; set; } = null!;

    /// <summary>Emit <c>GO TO … DEPENDING ON sel</c> (ISO §14.9.17 Format 2; GR2): a 1-based selector picks a pc;
    /// an out-of-range value transfers nowhere and falls through to the next statement. (The clause number was
    /// §14.9.20 here and at the selector read below — that is the INITIALIZE statement; GO TO is §14.9.17.)</summary>
    public void EmitGoToDepending(BoundGoToDepending d)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextDep();
        // The selector "shall reference a numeric elementary data item that is an integer" (§14.9.17.3 SR1) — read
        // through the ONE integer landing so a P-scaled or
        // unsigned-wide item selects by VALUE (kb/Work PB86's sweep of raw integer-identifier reads) — and narrowed through
        // the SATURATING HostInt32, so a value past `int` stays outside 1..n and falls through (GR2; kb/Work PB1033: a
        // cast wrapped 4294967297 to 1 and went to procedure-name-1).
        w.Line($"int __dep{id} = {RuntimeApi.HostInt32(NumericRenderer.Align(num.AsNum(d.Selector, ReceiverContext.None), 0))};");
        // X3.23-1985 USE FOR DEBUGGING (VCR 7.17): an in-range GO TO … DEPENDING transfer is DEBUG-CONTENTS SPACES,
        // DEBUG-LINE the GO TO DEPENDING statement's own line.
        string cause = dispatch.DebugActive ? $" __dbgCause = DebugCause.Transfer; __dbgLine = {d.SourceLine};" : "";
        using (w.Block($"switch (__dep{id})"))
            for (int k = 0; k < d.Targets.Count; k++)
                w.Line($"case {k + 1}:{cause} __pc = {d.Targets[k]}; break;");   // this `break` is the SELECTOR switch's own
        // In range ⇒ transfer. The jump is the dispatcher-transfer idiom, not a `break`: the statement's own
        // selector switch is itself a C# breakable, so a `break` here left the SELECTOR (or, inside an inline
        // PERFORM, that loop) and the transfer was silently discarded (kb/Work PB405).
        w.Line($"if (__dep{id} >= 1 && __dep{id} <= {d.Targets.Count}) {dispatch.TransferJump()}   // in range → transfer (ISO §14.9.17.4 GR2)");
    }

    // DISPLAY lives on AcceptDisplayEmitter since Step 9c (the ACCEPT/DISPLAY collaborator).


    public void EmitIf(BoundIf iff)
    {
        var w = ctx.Writer;
        using (w.Block($"if ({cond.Render(iff.Condition)})"))
            Statements.EmitStatementList(iff.Then);
        if (iff.Else.Count > 0)
            using (w.Block("else"))
                Statements.EmitStatementList(iff.Else);
    }

    // EmitPerform(inline:true) brackets the body with a fresh F3Region.Inline(pid) + the __pcont/__pexit labels, so
    // an EXIT PERFORM here targets THIS loop and a nested inline PERFORM (setting its OWN Inline id) targets the inner
    // loop — §14.9.14.4 GR5a "the most closely preceding, and as yet unterminated, inline PERFORM". No manual region reset needed.
    public void EmitInlinePerform(BoundInlinePerform p) =>
        EmitPerform(p.Control, () => Statements.EmitStatementList(p.Body), inline: true);

    /// <summary>Emit a Format-3 (exception-checking) PERFORM (ISO §14.9.28 Format 3) via the pc-RANGE interceptor
    /// (design SSOT §9.3.3). imperative-statement-1 is emitted INLINE inside a try (under the bind-time GR14
    /// overlay); a raise site within it consults the installed <c>PerformFrame</c> (GR17). The frame's matcher does
    /// tier-ordered WHEN selection (§14.9.49.4 GR3c-g) and runs the matching imp-2 (+ WHEN COMMON imp-4) as bounded
    /// pc-ranges via <c>__RunF3</c>/<c>__RunUse</c>. FINALLY (imp-5) is the INLINE trailing block, reached on every
    /// NON-fatal exit path (normal fall-off / imp-1 EXIT-PERFORM goto / a handler EXIT-PERFORM caught here); a fatal,
    /// unresumed EC throws PAST it (the chosen standard-defect default, §9.6 Q5). The frame pops in the finally
    /// BEFORE FINALLY so imp-5 behaves as if in a Format-2 PERFORM (GR21).</summary>
    public void EmitExceptionPerform(BoundExceptionPerform p)
    {
        var w = ctx.Writer;
        int n = p.PerformId;

        // Install the interceptor frame — the tier-ordered WHEN matcher closure (pure match arithmetic + __RunF3;
        // no goto/RESUME/EXIT inside it, so C#'s no-goto-out-of-a-lambda restriction never bites).
        w.Line("ExceptionState.PushPerformFrame(new PerformFrame { Matcher = (__ec, __f) =>");
        w.Line("{");
        w.Indent();
        EmitMatcherArms(p, w);
        w.Outdent();
        w.Line("}});");

        void EmitImp1()
        {
            var s = dispatch.SetF3Region(F3Region.Imp1, n);
            Statements.EmitStatementList(p.Imp1);   // inline, bound under the GR14 overlay
            dispatch.RestoreF3Region(s);
        }
        using (w.Block("try"))
        {
            // The handler EXIT-PERFORM catch (only when a handler contains EXIT PERFORM) sits in a NESTED try so it
            // is inside the finally that pops the frame; without it, imp-1 goes directly under the outer try.
            if (p.HandlerHasExit)
            {
                using (w.Block("try")) EmitImp1();
                w.Line($"catch (ExitPerformSignal __eps{n}) when (__eps{n}.Id == {n}) {{ }}   // handler EXIT PERFORM → §14.9.14.4 GR4");
            }
            else EmitImp1();
        }
        w.Line("finally { ExceptionState.PopPerformFrame(); }");
        w.Line($"__f3fin{n}: ;   // implicit CONTINUE preceding FINALLY (§14.9.14.4 GR4 / §14.9.28.4 GR16)");
        if (p.FinallyBody is { } fb)
        {
            var s = dispatch.SetF3Region(F3Region.Finally, n);
            // §14.9.28.4 GR14 covers imp-5 too: the implicit POP ALL sits "immediately preceding the END PERFORM
            // phrase", so FINALLY runs inside the TURN OFF ALL window just as the WHEN bodies do. The binder bound
            // imp-5 under that window, and the run-time half is the nested-list checking scope every statement list
            // opens (EcEmitter.EnterNestedStatements — kb/Work PB891): its statements set only their own flags, and
            // a standing guard's flags are re-based to all-off around them. A `goto` out of that scope to __f3end is
            // legal C#, so EXIT PERFORM in imp-5 (§14.9.28.4 GR16) still reaches the implicit CONTINUE.
            Statements.EmitStatementList(fb);   // imp-5 inline; skipped on the fatal-throw path
            dispatch.RestoreF3Region(s);
        }
        w.Line($"__f3end{n}: ;   // end of PERFORM");
    }

    /// <summary>Emit the F3 matcher body: one <c>if (test) return __RunF3(...)</c> per WHEN operand, SORTED into the
    /// §14.9.49.4 GR3c-g tiers (file+L3 → file+L2 / bare-file → L3 → L2 → L1/EC-ALL; source order only within a
    /// tier, GR17), then WHEN OTHER (imp-3, GR18) as the unconditional fallback, else <c>NoMatch</c> (fall to the USE
    /// declaratives). A tier-4 EC-ALL operand is the catch-all (its <c>true</c> test makes a following OTHER/NoMatch
    /// unreachable, so it terminates the body). Every handler runs with its WHEN COMMON (imp-4) via <c>__RunF3</c>.</summary>
    private void EmitMatcherArms(BoundExceptionPerform p, CodeWriter w)
    {
        var arms = new List<(int Tier, int W, int O, string Test, int Imp2Pc)>();
        for (int wi = 0; wi < p.Whens.Count; wi++)
        {
            var m = p.Whens[wi];
            if (m.OpenMode is { } mode)
            {
                // WHEN EXCEPTION INPUT | OUTPUT | I-O | EXTEND — GR3b open-mode scope (tier 1): an EC-I-O whose file
                // is CURRENTLY OPEN in that mode. (An OPEN-failure's mode is best-effort — the connector reports its
                // mode only once open; §9.7.) __f is null for a non-I-O condition or a file-less EC-I-O RAISE.
                arms.Add((1, wi, 0,
                    $"ExceptionCatalog.IsIoName(__ec) && __f is not null && {RuntimeApi.FileOpenModeOf("__f")} == {ModeOrdinal(mode)}",
                    m.Imp2Pc));
                continue;
            }
            for (int oi = 0; oi < m.Operands.Count; oi++)
            {
                var (tier, test) = ClassifyOperand(m.Operands[oi]);
                arms.Add((tier, wi, oi, test, m.Imp2Pc));
            }
        }
        arms.Sort((a, b) => a.Tier != b.Tier ? a.Tier.CompareTo(b.Tier)
            : a.W != b.W ? a.W.CompareTo(b.W) : a.O.CompareTo(b.O));

        (int cu, int cpc) = p.CommonPc is int cp ? (HandlerUseId(cp), cp) : (-1, -1);
        bool catchAll = false;
        foreach (var arm in arms)
        {
            string call = $"__RunF3({HandlerUseId(arm.Imp2Pc)}, {arm.Imp2Pc}, {cu}, {cpc})";
            if (arm.Test == "true") { w.Line($"return {call};   // WHEN EC-ALL (§14.9.49.4 GR3g)"); catchAll = true; break; }
            w.Line($"if ({arm.Test}) return {call};");
        }
        if (catchAll) return;
        if (p.OtherPc is int opc)
            w.Line($"return __RunF3({HandlerUseId(opc)}, {opc}, {cu}, {cpc});   // WHEN OTHER (imp-3, §14.9.28.4 GR18)");
        else
            w.Line("return PerformFrame.NoMatch;   // no WHEN/OTHER selects → fall to __EcDispatch (USE), GR17 tail");
    }

    /// <summary>Classify one WHEN operand into its §14.9.49.4 GR3a-g tier + the runtime match test (the EC-name
    /// tiers mirror <c>__EcDispatch</c>'s per-tier tests so the two never drift). GR17 selects across the WHEN
    /// operands by GR3's a→g priority: bare file-name (GR3a) > open-mode (GR3b, in the caller) > file+L3 (GR3c) >
    /// file+L2 (GR3d) > L3 (GR3e) > L2 (GR3f) > L1/EC-ALL (GR3g); source order only WITHIN a tier.</summary>
    private static (int Tier, string Test) ClassifyOperand(BoundWhenOperand op)
    {
        if (op.Ec is null)   // bare file-name — GR3a (any EC-I-O associated with the file), the HIGHEST tier
            return (0, $"ExceptionCatalog.IsIoName(__ec) && __f == {FileKeyExpr(op.File!)}");
        int level = ExceptionCatalog.TryGet(op.Ec, out var info) ? info.Level : 3;
        if (op.File is not null)
            return level == 3
                ? (2, $"__f == {FileKeyExpr(op.File)} && __ec == {CsLiteral(op.Ec)}")                             // GR3c
                : (3, $"__f == {FileKeyExpr(op.File)} && ExceptionCatalog.UnderLevel2(__ec, {CsLiteral(op.Ec)})"); // GR3d
        return level switch
        {
            3 => (4, $"__ec == {CsLiteral(op.Ec)}"),                                                              // GR3e
            2 => (5, $"ExceptionCatalog.UnderLevel2(__ec, {CsLiteral(op.Ec)})"),                                  // GR3f
            _ => (6, "true"),   // level 1 = EC-ALL                                                               // GR3g
        };
    }

    /// <summary>The <c>FileOpenMode</c> ordinal of a WHEN EXCEPTION open-mode keyword (INPUT/OUTPUT/I-O/EXTEND) —
    /// matches the binder's `useOnTarget` mode mapping and the runtime enum.</summary>
    private static int ModeOrdinal(string mode) => mode switch
    {
        "INPUT" => (int)CobolNet.Runtime.IO.FileOpenMode.Input,
        "OUTPUT" => (int)CobolNet.Runtime.IO.FileOpenMode.Output,
        "EXTEND" => (int)CobolNet.Runtime.IO.FileOpenMode.Extend,
        "I-O" => (int)CobolNet.Runtime.IO.FileOpenMode.IO,
        _ => -1,
    };

    /// <summary>The <c>__useActive</c> re-entrancy-array id of an appended handler pc — its slot sits above the
    /// declarative slots (§9.1): <c>DeclCount + (pc − F3HandlerBasePc)</c>.</summary>
    private int HandlerUseId(int pc) => dispatch.DeclCount + (pc - dispatch.F3HandlerBasePc!.Value);

    /// <summary>An out-of-line PERFORM is a recursive bounded <c>Dispatch(start, end)</c> over the target pc range
    /// (the C# call stack is the return-address stack, COBOLNET_DESIGN §5.4). X3.23-1985 USE FOR DEBUGGING (VCR 7.17):
    /// the FIRST entry into the range is a plain-PERFORM transfer (DEBUG-CONTENTS SPACES); every subsequent loop
    /// iteration is DEBUG-CONTENTS "PERFORM LOOP" — a per-PERFORM first-iteration flag carries that (any loop form).</summary>
    public void EmitOutOfLinePerform(BoundOutOfLinePerform p)
    {
        var w = ctx.Writer;
        // ⛔ THE SCAFFOLD IS THE STATEMENT; THE RANGE IS ONLY THE BODY (ISO §14.9.28.4 GR4 — "an inline PERFORM
        // statement and an out-of-line PERFORM statement function identically"). An EMPTY specified set (a
        // zero-paragraph section, §14.4.2) therefore emits the SAME loop — GR13 a)'s induction-variable
        // initialization, GR9's once-only count, GR10's condition tests, GR13's augments and the return — around a
        // body that does nothing, and no transfer of control takes place (GR5). It must NOT reach the dispatcher:
        // the return test cannot fire on an empty range (kb/Work PB440), which is why DispatchCall refuses one.
        if (p.Range.IsEmpty)
        {
            EmitPerform(p.Control, static () => { }, inline: false);
            return;
        }
        if (dispatch.DebugActive)
        {
            // DEBUG-LINE for a PERFORM/iteration trigger is the PERFORM statement's own line on EVERY iteration
            // (DB101A PERF-ITERATION-TEST :611-617); DEBUG-CONTENTS is SPACES on entry, "PERFORM LOOP" on re-iter.
            int fid = ctx.Names.NextLoop();
            w.Line($"bool __dbgFirst{fid} = true;");
            EmitPerform(p.Control, () =>
            {
                w.Line($"__dbgCause = __dbgFirst{fid} ? DebugCause.Transfer : DebugCause.PerformLoop; __dbgFirst{fid} = false; __dbgLine = {p.SourceLine};");
                EmitPerformedRange(p);
            }, inline: false);
        }
        else
            EmitPerform(p.Control, () => EmitPerformedRange(p), inline: false);
    }

    /// <summary>One execution of an out-of-line PERFORM's range — the bounded dispatch, and, for a PERFORM that
    /// ENTERS A DECLARATIVE from the nondeclarative portion in a unit that has RESUME, the RESUME landing ISO
    /// §14.9.33.4 GR2 b) gives that PERFORM:
    /// <list type="bullet">
    ///   <item>RESUME AT NEXT STATEMENT — "the implicit CONTINUE statement immediately follows the last statement
    ///         of the terminating procedure referenced in that PERFORM statement": the range's own end, so the
    ///         landing simply completes this execution of the range and the PERFORM's control phrase goes on;</item>
    ///   <item>RESUME AT procedure-name — GR3, "as if a GO TO procedure-name-1 were executed": the transfer every
    ///         other resume landing takes (<see cref="DispatchState.ResumeTransfer"/>).</item>
    /// </list>
    /// ⛔ THE SAME SIGNAL HAS TWO ENTRIES AND EACH NEEDS ITS LANDING (kb/Work PB892). A declarative is entered by
    /// an exception (§14.9.49 — <c>__RunUse</c> catches the <c>ResumeSignal</c> and returns the action to the raise
    /// site) or by this PERFORM (SR4); only the first had a landing, so a performed declarative's RESUME escaped
    /// every frame and killed the run unit with an unhandled .NET exception.</summary>
    private void EmitPerformedRange(BoundOutOfLinePerform p)
    {
        if (!(p.EntersDeclarative && dispatch.UnitHasResume))
        {
            Statements.EmitProcedureRange(p.Range);
            return;
        }
        var w = ctx.Writer;
        int id = ctx.Names.NextEc();
        using (w.Block("try"))
            Statements.EmitProcedureRange(p.Range);
        w.Line($"catch (ResumeSignal __rp{id}) {{ {dispatch.ResumeTransfer($"__rp{id}.TargetPc", "")} }}"
            + "   // §14.9.33.4 GR2 b) NEXT STATEMENT → after the range's last statement; GR3 procedure-name → GO TO");
    }


    /// <summary>Emit a PERFORM's loop scaffold. For an inline PERFORM this wrapper brackets the body with the
    /// per-PERFORM EXIT-PERFORM machinery: a fresh <see cref="F3Region.Inline"/> region, a <c>__pcont{id}</c> label at
    /// the loop-control boundary (target of EXIT PERFORM CYCLE — falls through to the VARYING augment + re-test), and a
    /// <c>__pexit{id}</c> label just past the loop (target of EXIT PERFORM — leaves EVERY nested VARYING level).
    /// §14.9.14.4 GR5/GR6 require leaving/continuing the WHOLE inline PERFORM, which a bare C# break/continue cannot do
    /// across the nested loops a multi-level VARYING emits (CA31/CA32). An out-of-line PERFORM never contains its own
    /// EXIT PERFORM (SR8), so it takes the bare loop with no region/labels.</summary>
    private void EmitPerform(BoundPerformControl control, Action body, bool inline)
    {
        if (!inline) { EmitPerformLoop(control, body, inline: false); return; }
        var w = ctx.Writer;
        int pid = ctx.Names.NextLoop();
        var saved = dispatch.SetF3Region(F3Region.Inline, pid);
        EmitPerformLoop(control, () => { body(); w.Line($"__pcont{pid}: ;"); }, inline: true);
        dispatch.RestoreF3Region(saved);
        w.Line($"__pexit{pid}: ;");
    }

    private void EmitPerformLoop(BoundPerformControl control, Action body, bool inline)
    {
        var w = ctx.Writer;
        switch (control)
        {
            case PerformTimes t:
                // The TIMES count is determined ONCE at the start of the PERFORM (ISO §14.9.28 GR7) — the body
                // modifying the count item must not change the iteration count (NC102A PFM-TEST-F2-6); a zero or
                // negative count runs the body zero times.
                int id = ctx.Names.NextLoop();
                w.Line($"long __n{id} = {CountExpr(t.Count)};");
                using (w.Block($"for (long __i{id} = 0; __i{id} < __n{id}; __i{id}++)")) body();
                break;
            case PerformUntil u when u.TestAfter:
                using (w.Block("do")) body();
                w.Line($"while (!({cond.Render(u.Until)}));");
                break;
            case PerformUntil u:
                using (w.Block($"while (!({cond.Render(u.Until)}))")) body();
                break;
            case PerformVarying v:
                EmitVarying(v, body);
                break;
            case PerformForever:
                // UNTIL EXIT (§14.9.28.4 GR11, 2023): the condition never becomes true. An inline EXIT PERFORM leaves
                // via `goto __pexit` (StatementEmitter.Visit(BoundExitPerform)); an out-of-line loop escapes only via GOBACK/STOP.
                using (w.Block("while (true)")) body();
                break;
            default:   // PerformOnce — an inline body runs once via do/while(false); an out-of-line call is unconditional
                if (inline) { using (w.Block("do")) body(); w.Line("while (false);"); }
                else body();
                break;
        }
    }

    /// <summary>PERFORM VARYING … [AFTER …] (ISO §14.9.28.4 GR13), leftmost level outermost.
    /// <para>TEST BEFORE (GR13 a/d/e) — the nested <c>while(!cond)</c> nest IS GR13 e)'s current-condition machine,
    /// one level per condition: e) 1 and e) 2's "evaluate the current condition" are the <c>while</c> tests; e) 2's
    /// FALSE arm a. ("another AFTER phrase to the right … becomes the current condition") is entering the next inner
    /// <c>while</c>; its FALSE arm b. (body, then augment the current condition's variable) is the innermost level's
    /// loop body; and e) 2's TRUE arm is the code that runs when an inner <c>while</c> exits — <b>a. the variable of
    /// the condition that went true is set to its INITIALIZATION VALUE, and only THEN b./c. the condition to its
    /// LEFT becomes current and THAT variable is augmented.</b> That order is the whole rule: because GR12 does item
    /// identification for the FROM operand "each time … is used in a setting … operation" and GR13's closing
    /// paragraph gives every change "immediate effect", <c>AFTER B FROM A</c> resets B from the <b>pre-augment</b> A.
    /// GR13 a) is the one-shot left-to-right initialization before the outermost test.</para>
    /// <para>TEST AFTER (GR13 b/c): body-first loops — the innermost tests after the body (false → augment, repeat);
    /// when true the next level out tests (false → augment it, REINITIALIZE the inner variable, run again). GR13 c) 4
    /// is the ONE sub-step of GR13 that states the increment BEFORE the reset ("the induction variable associated
    /// with that condition is incremented …, all induction variables to the right of the false condition are set to
    /// their initialization values"), and this arm implements exactly it — the two arms are deliberately NOT
    /// symmetric, and each is written to its own sub-step.</para>
    /// <para>FROM/BY render inline so each set/augment re-reads their current contents (GR12).</para>
    /// <para>⚠ NIST NC201A PFM-TEST-F4-23 ("ORDER OF INITIALISATION OF VARYING IDENTIFIERS") asserts 6 executions
    /// for the TEST BEFORE <c>AFTER B FROM A</c> nest; GR13 e) 2 gives 8. The ISO text is the oracle and the CCVS
    /// corpus is a regression net (CLAUDE.md rule 1), so NC201A is a recorded ISO-vs-CCVS divergence —
    /// <c>tests/nist/corpus.tsv</c> row, the pin in <c>SpecPinnedNistTests</c>, and the determination in
    /// <c>docs/CONFORMANCE.md</c> §3. kb/Work PB436.</para></summary>
    private void EmitVarying(PerformVarying v, Action body)
    {
        var w = ctx.Writer;
        var levels = v.Levels;
        if (!v.TestAfter)
        {
            foreach (var lv in levels) InitVaryingTarget(v, lv);   // GR13a: left-to-right init
            EmitBefore(0);
            void EmitBefore(int k)
            {
                using (w.Block($"while (!({cond.Render(levels[k].Until)}))"))
                {
                    if (k == levels.Count - 1)
                    {
                        body();
                        set.AugmentSetTarget(levels[k].Var, down: false, RenderPerEvaluation(levels[k].By), "PERFORM VARYING");
                    }
                    else
                    {
                        EmitBefore(k + 1);
                        // The inner while exited ⇒ the condition at k+1 is the current condition and it is TRUE, so
                        // ISO §14.9.28.4 GR13 e) 2's true-branch runs, IN ITS ORDER:
                        //   a. "the induction variable associated with the current condition is set to its
                        //      initialization value"   — level k+1 RESETS FIRST, reading its FROM operand NOW, and
                        //   b. "the condition to the left of the current condition becomes the current condition"
                        //   c. "the induction variable associated with the new current condition is incremented by
                        //      its associated augment value"   — level k augments SECOND.
                        // So `AFTER B FROM A` resets B from the PRE-augment A (GR12 re-identifies the FROM operand
                        // on every setting operation; GR13's closing paragraph gives the change immediate effect).
                        // ⛔ Do NOT swap these two: the augment-then-reset order is NIST NC201A PFM-TEST-F4-23's
                        // CCVS-85 expectation, which GR13 e) 2 contradicts — see the method doc-comment, the
                        // docs/CONFORMANCE.md §3 determination and kb/Work PB436.
                        InitVaryingTarget(v, levels[k + 1]);
                        set.AugmentSetTarget(levels[k].Var, down: false, RenderPerEvaluation(levels[k].By), "PERFORM VARYING");
                    }
                }
            }
        }
        else
        {
            InitVaryingTarget(v, levels[0]);
            EmitAfter(0);
            void EmitAfter(int k)
            {
                using (w.Block("while (true)"))
                {
                    if (k == levels.Count - 1) body();
                    else
                    {
                        // GR13 c) 4: on re-entry from the augment below, "all induction variables to the RIGHT of
                        // the false condition are set to their initialization values" — so the augment of level k
                        // (bottom of this block) happens BEFORE this reset of level k+1, the OPPOSITE order to the
                        // TEST BEFORE arm's GR13 e) 2 a–c. Deliberate: each arm is written to its own sub-step.
                        InitVaryingTarget(v, levels[k + 1]);
                        EmitAfter(k + 1);
                    }
                    w.Line($"if ({cond.Render(levels[k].Until)}) break;");
                    set.AugmentSetTarget(levels[k].Var, down: false, RenderPerEvaluation(levels[k].By), "PERFORM VARYING");
                }
            }
        }
    }

    /// <summary>Initialize a PERFORM VARYING (or AFTER) level's target from its FROM operand (GR13).
    /// <para>An INDEX-NAME target takes the FROM value through THE ONE SET-family amount landing
    /// (<see cref="SetEmitter.LandAmount"/>) — §13.18.38.4 GR2 names PERFORM VARYING beside SET and SEARCH as a
    /// statement that "creates a value for the index", and makes a value outside the implementor's index range the
    /// EC-RANGE-INDEX case. The former bare <c>(long)(Align(…, 0))</c> narrowing WRAPPED such a value silently, and
    /// §14.9.28.3 SR4 a) admits an integer data item of up to 31 digits as the FROM operand, so the wrap is
    /// reachable from conforming source (kb/Work PB459). The landing's own integrality test is satisfied by that
    /// same SR4 a)/b), so on legal source it never fires; the range test is the live one.</para>
    /// <para>Inside that success leg, when EC-RANGE-PERFORM-VARYING checking is enabled (§14.9.28.4 GR3) and the
    /// FROM operand is a data item, the fatal EC is raised for a non-positive value (the runtime tests the
    /// DATA-ITEM value, GR3 — the throw is caught by the FatalAmbientGates guard for USE-F3 dispatch). GR3 governs
    /// FROM initialization only, so the BY/augment sites are unaffected; a literal FROM (BoundNumLiteral) and an
    /// index-name FROM (BoundIndexRef) are out of GR3 scope.</para>
    /// <para>A NUMERIC induction variable (§14.9.28.3 SR2 — the only other legal VARYING identifier) is a PICTURE
    /// store with its own §14.7 size rules and has no index range, so it keeps the plain store.</para></summary>
    private void InitVaryingTarget(PerformVarying v, VaryingLevel lv)
    {
        NumX from = RenderPerEvaluation(lv.From);
        if (lv.Var is not SetIndexTarget)
        {
            set.StoreSetTarget(lv.Var, from);
            return;
        }
        string guard = set.LandAmount(from, SetAmountRule.IndexTo, "PERFORM VARYING … FROM", out string tmp, "pv");
        using (ctx.Writer.Block($"if ({guard})"))
        {
            // ⛔ THE GUARD ASKS THE RULE'S QUESTION, NOT THE NODE'S C# TYPE (kb/Work PB439). GR3's premise is
            // "an identifier is specified in the associated FROM phrase", and §8.4.3.1.2 prints THREE identifier
            // formats — a qualified-and-subscripted data-name, a function-identifier, and a reference-modified
            // identifier. This test used to be `lv.From is BoundNumRef`, which is the FIRST of those and nothing
            // else, so `FROM FUNCTION INTEGER(WS-Z)` over a zero silently took the unchecked path: the FATAL
            // EC-RANGE-PERFORM-VARYING was never set to exist, no USE declarative ran, and the index was
            // initialized to occurrence 0 and used to subscript the table. The binder now decides the kind once
            // (VaryingOperandKind), so the next identifier shape is covered by being an identifier.
            if (v.CheckIndexRange && lv.FromKind is VaryingOperandKind.Identifier)
                ctx.Writer.Line($"ExceptionState.PerformVaryingIndexError({tmp}, "
                    + $"{EmitText.CsLiteral("PERFORM VARYING index-name initialized from a non-positive item (ISO 14.9.28.4 GR3)")});");
            set.StoreSetTarget(lv.Var, new NumX(tmp, 0));
        }
    }

    /// <summary>Render a varying-phrase operand AT the setting or augmenting operation that reads it, emitting
    /// any function activations it carries as STATEMENTS immediately before that operation — ISO §14.9.28.4 GR12:
    /// "Item identification for identifier-3, identifier-4, identifier-6, identifier-7, index-name-2, and
    /// index-name-4 is done each time the content of the data item referenced by the identifier or the index
    /// referenced by the index-name is used in a setting or augmenting operation", and §8.4.3.2.4 GR1/GR6a makes
    /// a function-identifier's value "determined when the function is referenced at runtime". kb/Work PB437.
    /// <para>STATEMENTS, NOT AN IIFE. The condition twin (<c>ConditionRenderer.Visit(BoundUdfEvaluated)</c>) must
    /// render an immediately-invoked <c>Func&lt;bool&gt;</c> because a condition sits in a C# loop HEADER where no
    /// statement can precede it per iteration; every site that calls THIS method is a statement position, so the
    /// activations are emitted directly. Both go through the ONE statement emitter, and in both a condition an
    /// activation propagates leaves through the PERFORM's <c>BoundActivationSite</c> (kb/Work PB892). Each of the three call
    /// sites (the GR13 a)/b) initialization, the GR13 e) 2 a. / c) 4 re-initialization, and the augment) emits
    /// the activations exactly once per operation, because each renders the operand exactly once.</para>
    /// <para>Every other expression renders unchanged, so a varying phrase with no function reference produces
    /// byte-identical output.</para></summary>
    private NumX RenderPerEvaluation(BoundExpr e)
    {
        if (e is not BoundUdfEvaluatedExpr w) return num.RenderOperandLike(e);
        foreach (var activation in w.Activations) Statements.EmitStatement(activation);
        return num.RenderOperandLike(w.Inner);
    }

    /// <summary>The TIMES count as a C# <c>long</c> (§14.9.28.4 GR7 — determined once), narrowed only through the
    /// saturating <c>RuntimeApi.HostInt64</c> / <c>HostInt64Literal</c> (kb/Work PB1033 — a cast wrapped 2^64 + 1 to ONE
    /// iteration, and a 20-digit literal was CS1021): a literal folded at compile time; an
    /// error operand loud; every other operand — an integer data item (a P-scaled or unsigned-wide read included),
    /// a function-identifier's result (kb/Work PB86: it used to fall to a `_ => "1"` default and run the body
    /// ONCE) — through the ONE integer landing, <see cref="NumericRenderer.Align"/> at scale 0.</summary>
    private string CountExpr(BoundOperand count) => count switch
    {
        BoundNumericLiteral n => RuntimeApi.HostInt64Literal(n.Text),
        BoundOperandError e => LoudValue("long", e.Feature),
        _ => RuntimeApi.HostInt64(NumericRenderer.Align(num.AsNum(count, ReceiverContext.None), 0)),
    };


    /// <summary>SEARCH (ISO §14.9.37.4) — ONE frame, TWO lowerings, because the standard writes the scan twice.
    /// Format 1 is the serial search of GR3/GR4; Format 2 (SEARCH ALL) is the implementor-chosen search of GR9,
    /// whose index is bounded where Format 1's is not (see <see cref="EmitSerialScan"/> / <see cref="EmitAllScan"/>
    /// — kb/Work PB447). Emitted as a LABEL loop — not a C# while — so a GO TO inside a WHEN/AT END body
    /// (`__pc = k; break;`) breaks the DISPATCHER case, not a search loop (GR1 a)/b)1.: "If the execution of a
    /// procedure branching or conditional statement results in an explicit transfer of control, control is
    /// transferred in accordance with the rules for that statement"); a body that runs to completion jumps past
    /// the search.</summary>
    public void EmitSearch(BoundSearch s)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextSearch();
        // An OCCURS DYNAMIC table brackets the scan with EnterSearch/ExitSearch so a SET Format 14 on that same
        // table WHILE searching raises EC-FLOW-SEARCH (ISO §14.9.39 GR31; data-model D9). A try/finally is required
        // because the WHEN/AT-END arms `goto __searchEnd` OUT of the scan — ExitSearch must run on every exit path.
        if (s.DynTable is { } dt)
        {
            w.Line($"{dt}.EnterSearch();");
            using (w.Block("try")) EmitSearchScan(s, id);
            w.Line($"finally {{ {dt}.ExitSearch(); }}");
        }
        else EmitSearchScan(s, id);
    }

    /// <summary>The SEARCH statement's shared frame (ISO §14.9.37.4): the scan BOUND, the EC-raise plumbing, the
    /// per-format scan, and the ONE AT-END emission every unsuccessful exit funnels into (GR1 b) — an ALL FORMATS
    /// rule, which is why it is shared).
    /// <para>⛔ THE SCAN ITSELF IS NOT SHARED, AND MUST NOT BE RE-MERGED (kb/Work PB447). The standard writes the
    /// search twice — GR4 for Format 1 and GR9 for Format 2 — and the two texts DISAGREE about where the search
    /// index may be left: GR4 forms the new value first and repeats "unless the new value for the search index
    /// corresponds to a table element outside the permissible range of occurrence values", so an unsuccessful
    /// serial search ends with the index one past the table, while GR9 says of the Format-2 index "At no time is
    /// it set to a value that exceeds the value that corresponds to the last element of the table or is less than
    /// the value that corresponds to the first element of the table". One advance-then-test loop serving both gave
    /// SEARCH ALL the overshoot only Format 1 is licensed for, and the AT END phrase — part of this statement's own
    /// execution (GR1 b) 1.) — could then read outside the table through the search index.</para>
    /// <para>The bound is the table's MAXIMUM occurrence count — or an occurs-depending table's CURRENT count, or
    /// (D9) a dynamic table's current <c>Capacity</c> (§13.18.38 GR7/§8.5.1.9.1). Extracted so a dynamic table can
    /// wrap it in an EnterSearch/ExitSearch try/finally.</para></summary>
    private void EmitSearchScan(BoundSearch s, int id)
    {
        var w = ctx.Writer;
        string bound = s.DynTable is { } dt ? $"{dt}.Capacity"
            : s.DependItem is { } dp ? PlaceRenderer.CountRead(dp)
            : $"{s.Count}L";
        // CA36 (ISO §14.9.37.4 GR1b2): when the AT END phrase is ABSENT and EC-RANGE checking is ON, a raised
        // EC-RANGE-SEARCH-INDEX/-NO-MATCH must transfer to an applicable exception-processing statement (a USE
        // AFTER EXCEPTION CONDITION declarative / Format-3 PERFORM WHEN) and, if control returns, to the end of the
        // SEARCH. Track which range EC was raised so the shared AT-END funnel can DISPATCH it (mirror
        // EcEmitter.EmitOverflow's no-phrase dispatch). Emitted ONLY in that niche, so every other SEARCH is
        // byte-identical.
        bool dispatchEc = s.AtEnd is null && (s.CheckSearchIndex || s.CheckSearchNoMatch);
        string ecVar = $"__searchEc{id}";
        if (dispatchEc) w.Line($"string {ecVar} = null;");
        // ⛔ ONE EMISSION OF "THE SEARCH OPERATION IS UNSUCCESSFUL" — the test, the exception condition set under
        // its OWN checking gate, and the transfer to the AT-END funnel (GR1 b), an ALL FORMATS rule) travel
        // together. Each format's rule reaches it twice, with a different condition and a different EC, and
        // writing the three steps out per site is how one of them comes to be forgotten at a fifth.
        void Unsuccessful(string when, string ecName, bool checking)
        {
            using (w.Block($"if ({when})"))
            {
                if (checking)
                {
                    Ec.EmitConditionSet(ecName, "the search operation is unsuccessful (§14.9.37.4 GR1 b))");
                    if (dispatchEc) w.Line($"{ecVar} = \"{ecName}\";");
                }
                w.Line($"goto __searchAtEnd{id};");
            }
        }
        if (s.IsAll) EmitAllScan(s, id, bound, Unsuccessful);
        else EmitSerialScan(s, id, bound, Unsuccessful);
        // the shared AT-END emission — every unsuccessful exit reaches it (emitted ONCE), then the search-end label.
        w.Line($"__searchAtEnd{id}: ;");
        // GR1b2: AT END absent + checking on → dispatch the raised range EC to an applicable USE declarative / F3
        // WHEN; >=0 = RESUME AT a procedure (the dispatcher transfer idiom — the `break` it used to emit was
        // captured by an inline PERFORM around the SEARCH, kb/Work PB405); -1/-2/-3 (declarative ran / RESUME
        // NEXT / no handler) fall through to the end of the SEARCH (nonfatal — §14.6.13.1.4 #3/#4).
        if (dispatchEc)
            using (w.Block($"if ({ecVar} != null)"))
                Ec.EmitSelection(ecVar, terminate: null);   // EC-RANGE-SEARCH-INDEX / -NO-MATCH: both nonfatal (Table 13)
        bool terminated = s.AtEnd is { } at && Statements.EmitStatementList(at);
        if (!terminated) w.Line($"goto __searchEnd{id};");
        w.Line($"__searchEnd{id}: ;");
    }

    /// <summary>FORMAT 1 — the serial search, ISO §14.9.37.4 GR4, with the GR3 index/VARYING augmentation.
    /// <list type="number">
    /// <item>The INITIAL setting decides admission: "If, at the start of the execution, the search index contains
    /// a value that corresponds to an occurrence number that is negative, zero, or greater than the highest
    /// permissible occurrence number for identifier-1, the search operation is unsuccessful, the
    /// EC-RANGE-SEARCH-INDEX exception condition is set to exist". The <c>&lt; 1</c> half is emitted
    /// UNCONDITIONALLY (a zero/negative index would otherwise read a phantom scratch occurrence); only the EC
    /// <c>Set</c> is checking-gated.</item>
    /// <item>None of the conditions satisfied → "the search index is incremented by one occurrence number", and
    /// with it (GR3 b)/c)) the VARYING operand: an index data item "incremented by the same amount as, and at the
    /// same time as, the search index", an integer data item "incremented by the value one at the same time".
    /// GR3 c) 1. — VARYING an index OF THIS TABLE — makes that index the search index itself, so it is the
    /// <c>IndexField</c> here and no separate augment exists.</item>
    /// <item>ADVANCE-THEN-TEST is the rule's own order and is licensed HERE ONLY: "The process is then repeated
    /// using the new index setting unless the new value for the search index corresponds to a table element
    /// outside the permissible range of occurrence values, in which case the search operation is unsuccessful,
    /// the EC-RANGE-SEARCH-NO-MATCH exception condition is set to exist". The new value is formed before it is
    /// judged, so an unsuccessful serial search leaves the index (and the in-step item) one past the table — a
    /// state GR4 contemplates in as many words. ⛔ Format 2 forbids exactly this; see
    /// <see cref="EmitAllScan"/>.</item></list></summary>
    private void EmitSerialScan(BoundSearch s, int id, string bound, Action<string, string, bool> unsuccessful)
    {
        var w = ctx.Writer;
        unsuccessful($"{s.IndexField} < 1 || {s.IndexField} > {bound}", "EC-RANGE-SEARCH-INDEX", s.CheckSearchIndex);
        w.Line($"__search{id}:");
        EmitSearchWhens(s, id);
        w.Line($"{s.IndexField} += 1;");
        if (s.AlsoVaried is { } also) set.AugmentSetTarget(also, down: false, new NumX("1", 0), "SEARCH VARYING");
        unsuccessful($"{s.IndexField} > {bound}", "EC-RANGE-SEARCH-NO-MATCH", s.CheckSearchNoMatch);
        w.Line($"goto __search{id};");
    }

    /// <summary>FORMAT 2 (<c>SEARCH ALL</c>) — ISO §14.9.37.4 GR9, the search whose index the standard BOUNDS.
    /// <list type="number">
    /// <item>"The initial setting of the search index is ignored" — the scan's first probe is occurrence 1
    /// whatever the program left there, so a SET before the statement cannot change the outcome.</item>
    /// <item>"Its setting is varied during the search operation in a manner specified by the implementor" — the
    /// TECHNIQUE is latitude (GR9 opens "A non serial type of search operation may take place", a permission, not
    /// a requirement), and this implementation probes the occurrences in order. The technique rests on no syntax
    /// rule and on no ordering guarantee: an unsequenced table is GR6's case, and a scan that finds a present
    /// element lands inside GR6 a) 1. (kb/Work PB445, <c>SearchBinder.BindSearchAll</c>).</item>
    /// <item>⛔ THE RANGE BOUND IS NOT LATITUDE. "At no time is it set to a value that exceeds the value that
    /// corresponds to the last element of the table or is less than the value that corresponds to the first
    /// element of the table" — unconditional, and binding on the unsuccessful exit too, since GR1 b) 1. makes the
    /// AT END phrase part of this statement's execution and a subscript written there reads through the search
    /// index. So the bound is tested BEFORE the advance: the index moves only onto an occurrence that exists.
    /// GR9's later "the final setting of the search index is undefined" forbids a PROGRAM to rely on which
    /// in-range occurrence is left — it does not license leaving the table. (kb/Work PB447: an
    /// advance-then-test loop shared with Format 1 parked a five-occurrence table's index at 6, and
    /// <c>K(KX)</c> under AT END then read storage outside the table.)</item>
    /// <item>An EMPTY table (an occurs-depending or DYNAMIC current count of 0) has no permissible setting at all,
    /// so no probe is made: the search is unsuccessful at once with the index at 1, and GR9's bound — worded over
    /// a first and a last element — has no occurrence to name.</item></list>
    /// Format 2 has no VARYING phrase (§14.9.37.2 Format 2), so <c>AlsoVaried</c> is null by construction and
    /// nothing is varied in step; Format 2 also cannot raise EC-RANGE-SEARCH-INDEX, because GR9 leaves it no
    /// initial setting to reject.</summary>
    private void EmitAllScan(BoundSearch s, int id, string bound, Action<string, string, bool> unsuccessful)
    {
        var w = ctx.Writer;
        w.Line($"{s.IndexField} = 1;");
        unsuccessful($"{s.IndexField} > {bound}", "EC-RANGE-SEARCH-NO-MATCH", s.CheckSearchNoMatch);
        w.Line($"__search{id}:");
        EmitSearchWhens(s, id);
        unsuccessful($"{s.IndexField} >= {bound}", "EC-RANGE-SEARCH-NO-MATCH", s.CheckSearchNoMatch);
        w.Line($"{s.IndexField} += 1;");
        w.Line($"goto __search{id};");
    }

    /// <summary>The WHEN arms of one probe — the ALL FORMATS half of the statement, so ONE emission serves both
    /// lowerings. ISO §14.9.37.4 GR1: the SEARCH statement "tests conditions specified in WHEN phrases … to
    /// determine whether a table element satisfies these conditions", and "Any subscripting specified in a WHEN
    /// phrase is evaluated each time the conditions in that WHEN phrase are evaluated" — hence the conditions are
    /// rendered INSIDE the loop, once per probe. First true wins and the search is successful (GR1 a): the index
    /// "remains set at the occurrence number that caused a WHEN condition to be satisfied", which is exactly the
    /// value the loop holds, and a body that runs to completion jumps to the end of the SEARCH statement.</summary>
    private void EmitSearchWhens(BoundSearch s, int id)
    {
        var w = ctx.Writer;
        foreach (var when in s.Whens)
            using (w.Block($"if ({cond.Render(when.Condition)})"))
            {
                if (!Statements.EmitStatementList(when.Statements)) w.Line($"goto __searchEnd{id};");
            }
    }

}
