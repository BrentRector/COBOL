// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.CodeGen;

// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════
//  The emitter's mutable state model (P7 Step 9b; the phase doc's AS-BUILT PLAN). The cross-partial mutable
//  fields formerly scattered over the CSharpEmitter partials become three cohesive per-scope objects so the
//  Step-9 collaborator emitters receive their shared state EXPLICITLY (ctor-threaded), never through a god
//  class's private fields. All three are RUN-UNIT lifetime (fields of ProgramEmitter since Step 9n, like
//  NameAllocator); the "per unit" / "per statement" designations below document the MUTATION discipline —
//  which emitter writes the field, and when — preserved exactly from the pre-split partials (byte-exactness
//  gate: the 32 characterization snapshots).
// ═══════════════════════════════════════════════════════════════════════════════════════════════════════════════

/// <summary>The lexical region governing what an <c>EXIT PERFORM</c> compiles to (ISO §14.9.14.4 GR4/GR5 /
/// §14.9.28.4 GR16). For a Format-3 (exception-checking) PERFORM: a plain <c>goto</c> to the PERFORM's
/// implicit-CONTINUE-before-FINALLY label in imperative-statement-1 (<see cref="Imp1"/>), a thrown
/// <see cref="Runtime.Exceptions.ExitPerformSignal"/> from a handler pc-range (<see cref="Handler"/> — imp-2/3/4,
/// which runs in a nested dispatcher a goto cannot leave), or a <c>goto</c> to the end label in FINALLY
/// (<see cref="Finally"/> — imp-5). For an ORDINARY inline PERFORM (<see cref="Inline"/>): a <c>goto</c> to the
/// per-PERFORM <c>__pexit</c> label just past the loop (EXIT PERFORM) or the <c>__pcont</c> label at the
/// loop-control boundary (EXIT PERFORM CYCLE) — a bare C# <c>break</c>/<c>continue</c> cannot express GR5a/GR6
/// when a multi-level VARYING is emitted as nested loops (it would leave/cycle only the innermost). Every inline
/// PERFORM sets its OWN <see cref="Inline"/> region with a fresh id, so a nested inline PERFORM's EXIT PERFORM
/// targets the innermost loop (§14.9.14.4 GR5a "the most closely preceding, and as yet unterminated, inline PERFORM").
/// <see cref="None"/> is a defensive fallback the binder never reaches for a valid EXIT PERFORM (SR8 permits it
/// only inside an inline/F3 PERFORM).</summary>
internal enum F3Region { None, Imp1, Handler, Finally, Inline }

/// <summary>The PC-dispatcher state the statement emitters cooperate over (COBOLNET_DESIGN §5): which paragraph
/// is being emitted, the NEXT SENTENCE label, the dispatch-method name, the ONE way a statement leaves the
/// current paragraph body (<see cref="TransferOut(string,string)"/>), and the USE-declaratives hooks.</summary>
internal sealed class DispatchState
{
    /// <summary>The paragraph index being emitted (for EXIT PARAGRAPH / fall-through). Written per pc case by
    /// the dispatch-method emission.</summary>
    public int CurrentPc { get; set; }

    /// <summary>The current Format-3 PERFORM region + its <c>PerformId</c> (see <see cref="F3Region"/>) — read by
    /// <c>BoundExitPerform</c>'s emit. Default <see cref="F3Region.None"/> (an ordinary loop EXIT PERFORM). Set by
    /// <c>EmitExceptionPerform</c> (Imp1/Finally), by the dispatcher around a handler <c>case</c> (Handler), and
    /// reset to None around a nested inline/out-of-line PERFORM body.</summary>
    public (F3Region Region, int Id) F3Cur { get; private set; } = (F3Region.None, 0);

    /// <summary>Set <see cref="F3Cur"/>, returning the previous value for a later <see cref="RestoreF3Region"/>
    /// (the save/restore idiom around nested statement bodies).</summary>
    public (F3Region Region, int Id) SetF3Region(F3Region region, int id)
    {
        var saved = F3Cur;
        F3Cur = (region, id);
        return saved;
    }

    /// <summary>Restore <see cref="F3Cur"/> to a value captured by <see cref="SetF3Region"/>.</summary>
    public void RestoreF3Region((F3Region Region, int Id) saved) => F3Cur = saved;

    /// <summary>The unit's declarative count and the first appended Format-3 handler pc — the emitter derives a
    /// handler's <c>__useActive</c> id as <c>DeclCount + (pc − F3HandlerBasePc)</c> (the handler pc-ranges reuse
    /// <c>__RunUse</c>'s re-entrancy array above the declarative slots). Set per unit by the dispatcher emission;
    /// <see cref="F3HandlerBasePc"/> is null for a non-F3 unit.</summary>
    public int DeclCount { get; set; }
    public int? F3HandlerBasePc { get; set; }

    /// <summary>The unit contains a RESUME statement (<c>EcFeatures.HasResume</c>) — so a <c>ResumeSignal</c> can
    /// be thrown at all, and a PERFORM that enters a declarative needs its §14.9.33.4 GR2 b) landing. Set per unit
    /// beside <see cref="DeclCount"/>; a unit without RESUME emits byte-identical PERFORMs.</summary>
    public bool UnitHasResume { get; set; }

    /// <summary>The <c>__useActive</c> ids of this unit's <c>USE … GLOBAL</c> declaratives — empty when it
    /// declares none. Set per unit beside <see cref="DeclCount"/>, and cleared around an OO method body (a
    /// method has no USE declaratives of its own).</summary>
    public IReadOnlyList<int> GlobalDeclIds { get; set; } = [];

    /// <summary>ISO §14.9.18.4 GR6's run-time question, rendered — "is one of THIS program's GLOBAL declarative
    /// procedures currently activated and not yet returned", i.e. is the executing statement within its RANGE —
    /// or null when the unit declares no GLOBAL declarative and the test can never be true.
    /// <para>⛔ IT IS THE SAME ARRAY §14.9.49.4 GR2's re-entrancy guard uses, and that is the point (kb/Work
    /// PB409): "within the range of a declarative procedure" is a DYNAMIC relation — a declarative may PERFORM
    /// arbitrary procedures — so no bind-time "is this paragraph inside a declarative" test can answer it, and
    /// <c>__RunUse</c> already maintains exactly the activated-and-not-yet-returned flag the rule asks for.
    /// "Specified in the same program as the GOBACK statement" comes free: the array is per program instance,
    /// and a container's GLOBAL declarative selected on behalf of a contained program (GR4 b)) runs in the
    /// CONTAINER's instance.</para></summary>
    public string? InGlobalDeclarativeRangeTest =>
        GlobalDeclIds.Count == 0 ? null : string.Join(" || ", GlobalDeclIds.Select(i => $"__useActive[{i}]"));

    /// <summary>The goto target NEXT SENTENCE jumps to (null in the last sentence). Written per sentence by the
    /// paragraph-body emission.</summary>
    public string? SentenceEndLabel { get; set; }

    /// <summary>The dispatch-method NAME the statement emitters call for a bounded range (out-of-line PERFORM,
    /// SORT/MERGE procedures): <c>__Dispatch</c> for a program's instance method; <c>__MDispatch</c> while a
    /// COBOL-class METHOD body emits — its dispatcher is a LOCAL FUNCTION of the emitted method, so the
    /// method's LINKAGE/LOCAL-STORAGE locals are capturable (OO deep-dive D3/D6, slice 2). Saved/swapped/
    /// restored around each OO method body.</summary>
    public string DispatchName { get; set; } = "__Dispatch";

    /// <summary>Render the bounded dispatch call for ONE resolved procedure range — the single place a
    /// <see cref="PcRange"/> becomes a <c>__Dispatch(start, end)</c> statement (out-of-line PERFORM, SORT/MERGE
    /// INPUT/OUTPUT PROCEDURE). <paramref name="comment"/> is appended as a trailing <c>//</c> note.
    /// <para>⛔ An EMPTY range (a zero-paragraph section, ISO §14.4.2) has no first statement to transfer to
    /// (§14.9.28.4 GR4/GR5), and the dispatcher's return test (<c>__atExit &amp;&amp; __pc == __exitPc + 1</c>) can never
    /// fire on it — it would run from the range start to the END OF THE PC SPACE, executing the following
    /// sections once per iteration of whatever control phrase was written. Callers must therefore emit NOTHING
    /// for an empty range; asking for the call is an emitter bug and throws here rather than shipping that
    /// program (kb/Work PB440). Note that a LEGAL INVERTED range (GR6) also has <c>End &lt; Start</c>, so this
    /// is <see cref="PcRange.IsEmpty"/> and never an arithmetic test.</para></summary>
    public string DispatchCall(PcRange range, string comment = "") =>
        range.IsEmpty
            ? throw new InvalidOperationException(
                $"internal: {DispatchName}{range} requested for an EMPTY procedure range — an empty specified "
                + $"set of statements has no first statement to transfer to (ISO §14.9.28.4 GR4/GR5)")
            : $"{DispatchName}({range.Start}, {range.End});{comment}";

    /// <summary>Render the invocation of ONE selected declarative's use procedure — the single place a
    /// <see cref="BoundDeclarative"/> becomes a <c>__RunUse(id, start, end)</c> expression, for every selection
    /// path there is (<c>__IoCheck</c>, <c>__IoCheckEc</c>, <c>__EcDispatch</c>, <c>__EcObjDispatch</c>,
    /// <c>__RunGlobalUse</c> and the report engine's BEFORE REPORTING hook). The pair handed to
    /// <c>__RunUse</c> is the DECLARATIVE SECTION'S OWN RANGE and nothing else: ISO §14.9.49.3 SR1 makes the use
    /// procedure "the remainder of the section", and §14.9.14.4 GR7's NOTE puts the USE return mechanism after
    /// that section's last paragraph. Deriving a shorter end from paragraph shape ran the selected declarative
    /// only in part (kb/Work PB367), and the way that stayed invisible was that SIX call sites each spelled the
    /// pair out themselves — so the pair is spelled ONCE, here.
    /// <para>The exception-checking (Format-3) PERFORM's <c>imp-2</c>/<c>imp-3</c>/<c>imp-4</c> handlers
    /// (<c>EcEmitter.EmitPerformInterceptor</c>) also go through the generated <c>__RunUse</c>, and they do NOT
    /// come through here — deliberately. They are not declaratives: they are single-pc SYNTHETIC ranges appended
    /// above the pc space (<see cref="F3HandlerBasePc"/>) and selected by §14.9.28.4 GR17, not §14.9.49.4 GR3, and
    /// their pcs are runtime values of the interceptor rather than a bound node's. What this renderer owns is the
    /// question "what is a DECLARATIVE's range", which is the question PB367 was about.</para>
    /// <para>⛔ An EMPTY range cannot reach here. A declarative section with zero paragraphs is legal
    /// (§14.4.2 / §14.9.49.3 SR1 "zero, one, or more procedural paragraphs"), but the binder gives it ONE no-op
    /// pc precisely so the bounded dispatch has a range to run — because the selector must still STOP at it
    /// (§14.9.49.4 GR3: "The first declarative that satisfies the selection criteria is executed and no other
    /// declaratives are executed"), which is not the same thing as emitting nothing the way
    /// <see cref="DispatchCall"/>'s callers must. If that invariant is ever relaxed, the selector arms — not this
    /// renderer — are what must learn to say "selected, ran nothing".</para></summary>
    public string RunUseCall(int id, PcRange range) =>
        range.IsEmpty
            ? throw new InvalidOperationException(
                $"internal: __RunUse({id}, …) requested for an EMPTY declarative range — a declarative section "
                + "always carries at least one pc so its bounded dispatch can run (ISO §14.9.49.3 SR1)")
            : $"__RunUse({id}, {range.Start}, {range.End})";

    // ── Leaving the paragraph body: ONE idiom, one label (kb/Work PB405/PB414) ────────────────────────────────
    //  ⛔ A transfer of control OUT of the current paragraph is spelled `__pc = <target>; goto <TransferLabel>;`
    //  and NOWHERE spelled as a bare C# `break;`. C# binds `break` to the innermost enclosing breakable
    //  statement, and the emitter lowers COBOL containers to real C# breakables — an inline PERFORM is a
    //  for/while/do (ControlFlowEmitter.EmitPerformLoop), GO TO … DEPENDING is a switch — so a `break` written
    //  for the dispatcher's `switch (__pc)` was CAPTURED by any such container the statement happened to sit in:
    //  the transfer was silently discarded and the paragraph's fall-through epilogue then overwrote the target
    //  (§14.9.14.4 GR6/GR7 and §14.9.19.4 GR4/GR6 all measured wrong that way). A `goto` cannot be captured, so
    //  the NEXT container the emitter learns to lower is correct by construction rather than by review; the
    //  DispatcherTransferIdiomDriftTests pin that. The same reasoning already governed EXIT PERFORM, which has
    //  used planted __pexit/__pcont labels since CA31/CA32 — this is that reasoning applied to the statements
    //  that leave the DISPATCHER instead of a PERFORM.

    /// <summary>The label planted immediately after the dispatcher's <c>switch (__pc)</c> (before the at-exit
    /// return test) — the ONE landing point for every transfer of control out of a paragraph body. Scoped per
    /// dispatch method by <see cref="BeginTransferScope"/> so a program's <c>__Dispatch</c> and an OO method's
    /// local-function <c>__MDispatch</c> each own theirs.</summary>
    public string TransferLabel { get; private set; } = "__xfer";

    /// <summary>Whether the dispatch method currently being emitted rendered at least one
    /// <see cref="TransferOut(string,string)"/> — the label is planted only then, so a program with no transfers
    /// emits no unreferenced label (and its generated source is unchanged).</summary>
    public bool TransferUsed { get; private set; }

    /// <summary>Open a transfer scope for one dispatch method, returning the previous scope for
    /// <see cref="EndTransferScope"/> (the save/restore idiom of <see cref="SetF3Region"/>).</summary>
    public (string Label, bool Used) BeginTransferScope(string label)
    {
        var saved = (TransferLabel, TransferUsed);
        TransferLabel = label;
        TransferUsed = false;
        return saved;
    }

    /// <summary>Close a transfer scope opened by <see cref="BeginTransferScope"/>.</summary>
    public void EndTransferScope((string Label, bool Used) saved) => (TransferLabel, TransferUsed) = saved;

    /// <summary>Render "leave the current paragraph body and re-dispatch at <paramref name="pcExpr"/>" — the
    /// single place a transfer of control out of a paragraph becomes C# (GO TO and its alterable/DEPENDING forms,
    /// EXIT PARAGRAPH §14.9.14.4 GR6, EXIT SECTION GR7, NEXT SENTENCE in a last sentence §14.9.19.4 GR4/GR6, and
    /// every RESUME AT landing §14.9.33.4 GR3). <paramref name="comment"/> is appended as a trailing <c>//</c>
    /// note. Marks <see cref="TransferUsed"/> so the dispatch emission plants the label.</summary>
    public string TransferOut(string pcExpr, string comment = "") => $"__pc = {pcExpr}; {TransferJump()}{comment}";

    /// <summary>The JUMP half of <see cref="TransferOut(string,string)"/>, for the one statement that must run
    /// something between setting <c>__pc</c> and leaving: EXIT SECTION fires the section's own return mechanism
    /// first (§14.9.14.4 GR7 "preceding any return mechanisms for that section").</summary>
    public string TransferJump()
    {
        // ⛔ AN OPERAND-EVALUATION STEP NEVER LEAVES BY `goto` (kb/Work PB1432): it may be running inside a
        // per-evaluation window's lambda, where C# forbids the jump (CS0159 — the backend rejected legal COBOL),
        // and hoisted before its statement the jump would be right only by accident. Its only transfer is the
        // RESUME landing, which ResumeTransfer already unwinds; anything else reaching here is an emitter bug,
        // reported as one rather than as a C# compile error against a generated path.
        if (InOperandEvaluation)
            throw new InvalidOperationException(
                "internal: a statement-level transfer was requested while emitting an operand-evaluation step "
                + "(BoundStatement.OperandEvaluation) — only the RESUME landing may leave one, and it unwinds");
        TransferUsed = true;
        return $"goto {TransferLabel};";
    }

    /// <summary>The <see cref="TransferOut(string,string)"/> of a compile-time pc.</summary>
    public string TransferOut(int pc, string comment = "") => TransferOut(pc.ToString(), comment);

    /// <summary>Render the RESUME landing of one dispatch result: a USE declarative / Format-3 handler that
    /// completed with RESUME AT procedure-name returns that paragraph's pc, and the raise site transfers there
    /// (ISO §14.9.33.4 GR3); a negative result is "no transfer" (normal completion, RESUME NEXT STATEMENT, or no
    /// qualifying declarative) and falls through. Written ONCE here because every raise site there is — I/O,
    /// CALL, pointer, SEARCH, RAISE, size-error, CONTINUE AFTER — lands the same way, and each site that spelled
    /// it itself spelled the transfer as a capturable <c>break</c> (kb/Work PB405).</summary>
    /// <para>⛔ INSIDE AN OPERAND-EVALUATION STEP (<see cref="InOperandEvaluation"/>) THE SAME CALL UNWINDS
    /// (kb/Work PB892, PB1432): see <see cref="OperandEvaluationResume"/>. Every raise site of such a step — a
    /// function activation's propagation pickup, a §15.4 subscript temporary store's size-error selection, any
    /// raise a sending-value temp's MOVE makes — lands through this one method, so none of them decides the landing
    /// itself and a new raise site inherits the right one.</para>
    public string ResumeTransfer(string resultVar, string comment = "   // RESUME AT procedure-name (§14.9.33.4 GR3)")
        => InOperandEvaluation
            ? OperandEvaluationResume(resultVar)
            : $"if ({resultVar} >= 0) {{ {TransferOut(resultVar)} }}{comment}";

    /// <summary>True while the statement emitter is emitting an operand-evaluation step
    /// (<see cref="Binding.Bound.BoundStatement.OperandEvaluation"/>) — opened by
    /// <c>StatementEmitter.EmitStatement</c>, the one place every step (hoisted, lambda-captured or
    /// per-operation) is emitted through.</summary>
    public bool InOperandEvaluation { get; private set; }

    /// <summary>Enter the operand-evaluation scope, returning the previous state for
    /// <see cref="ExitOperandEvaluation"/> (the save/restore idiom of <see cref="BeginTransferScope"/>).</summary>
    public bool EnterOperandEvaluation()
    {
        bool saved = InOperandEvaluation;
        InOperandEvaluation = true;
        return saved;
    }

    /// <summary>Close a scope opened by <see cref="EnterOperandEvaluation"/>.</summary>
    public void ExitOperandEvaluation(bool saved) => InOperandEvaluation = saved;

    /// <summary>The RESUME landing of a dispatch result selected for a condition an OPERAND-EVALUATION step raised
    /// or propagated (<see cref="Binding.Bound.BoundStatement.OperandEvaluation"/> — a function reference, an inline
    /// method invocation, an object-property accessor, a §15.4 subscript temporary store; kb/Work PB892, PB1432).
    /// ISO §14.9.33.4 GR2 a) 2.: "for an inline invocation or a function invocation, it [the applicable statement]
    /// is the statement in which the inline invocation or function invocation was specified", and GR2 a) 1. makes
    /// the statement a store was evaluated for "the one in which the exception condition was raised" — so RESUME
    /// AT NEXT STATEMENT leaves THAT statement, and GR3's RESUME AT procedure-name leaves it too. Neither can be
    /// written here: the step may be running
    /// inside a C# expression (a per-evaluation window's immediately-invoked lambda), where no <c>goto</c> can
    /// leave, and even a hoisted one sits BEFORE the statement, so <see cref="ResumeTransfer"/>'s fall-through
    /// would re-enter the very statement GR2 says to abandon. Both actions therefore unwind as
    /// <c>RaiseResumeSignal</c> to the carrying statement's <c>BoundActivationSite</c>, which lands them with
    /// <see cref="ResumeTransfer"/>. A declarative that completed normally (-1) or no qualifying declarative (-3)
    /// leaves the activation's result in place and the statement continues (§14.9.18.4 GR1 b) "execution continues
    /// … as specified in the rules for the activating statement after the result … is returned").</summary>
    private static string OperandEvaluationResume(string resultVar) =>
        $"if ({resultVar} >= 0 || {resultVar} == ResumeSignal.NextStatement) throw new RaiseResumeSignal({resultVar});"
        + "   // RESUME leaves the statement the activation was specified in (§14.9.33.4 GR2 a) 2.)";

    /// <summary>The program being emitted declares USE procedures (drives the <c>__IoCheck</c> hooks). Set per
    /// unit by the dispatcher emission; cleared by the OO class-unit emission (a class owns no USE
    /// declaratives).</summary>
    public bool UseDecls { get; set; }

    /// <summary>A CONTAINING program has USE … GLOBAL declaratives (ISO §14.9.49.4 GR4b — the child's
    /// <c>__IoCheck</c> walks outward). Set per unit by the program-class emission.</summary>
    public bool OuterGlobalUse { get; set; }

    /// <summary>The reports whose GENERATE / TERMINATE statements in the unit being emitted pass a USE BEFORE
    /// REPORTING selector (ISO §14.9.49.4 GR4 / GR8; kb/Work PB369) — the reports the unit emitted a selector
    /// member for. Written per unit by <c>ReportWriterEmitter.EmitBeforeReportingSelectors</c> before any statement
    /// of the unit is emitted; cleared by the OO class-unit emission.</summary>
    public HashSet<Binding.ReportModel> BeforeReportingSelectors { get; } = new(ReferenceEqualityComparer.Instance);

    /// <summary>The program being emitted has an ACTIVE X3.23-1985 USE FOR DEBUGGING procedure-trigger facility
    /// (WITH DEBUGGING MODE + a procedure-subject debugging declarative; VCR Table 7 row 7.17). Gates the debug
    /// scaffolding — the <c>__dbgItem</c>/<c>__dbgCause</c> fields, the <c>__RunDebug</c> helper, the per-subject
    /// entry triggers, and the DEBUG-CONTENTS cause assignments threaded through every transfer of control — so a
    /// non-debug program's generated source is byte-identical (the zero-scaffolding invariant). Set per unit by the
    /// dispatcher emission; a class unit owns no debug facility.</summary>
    public bool DebugActive { get; set; }

    /// <summary>The debug trigger subjects keyed by their nondeclarative pc (empty unless <see cref="DebugActive"/>):
    /// the dispatch-method emission injects a <c>__RunDebug(...)</c> at each subject case's entry.</summary>
    public IReadOnlyDictionary<int, BoundDebugSubject> DebugByPc { get; set; } =
        new Dictionary<int, BoundDebugSubject>();

    /// <summary>Emit a DEBUG-CONTENTS cause + DEBUG-LINE assignment at a transfer of control, but ONLY when the
    /// debug facility is active (else nothing — the zero-scaffolding gate). <paramref name="cause"/> is a
    /// <c>DebugCause</c> enumerand name (the emitted file has <c>using CobolNet.Runtime;</c>);
    /// <paramref name="causingLine"/> is the source line of the CAUSING (transferring) statement — the X3.23-1985
    /// DEBUG-LINE (VCR 7.17). A non-positive line is omitted (e.g. START PROGRAM, whose DEBUG-LINE is the subject's
    /// own first statement, applied in __RunDebug).</summary>
    public void EmitDebugCause(CodeWriter w, string cause, int causingLine = 0)
    {
        if (!DebugActive) return;
        w.Line(causingLine > 0
            ? $"__dbgCause = DebugCause.{cause}; __dbgLine = {causingLine};"
            : $"__dbgCause = DebugCause.{cause};");
    }
}

/// <summary>The exception-condition emission state (ISO §14.6.13 / §7.3.25) shared by the EC wrappers, the
/// arithmetic size-error emission, the I/O hooks, CALL propagation, and pointer checks.</summary>
internal sealed class EcState
{
    /// <summary>Group-level: ANY EC feature in use (gates every machinery emission). Restored once per run unit
    /// from the immutable <c>BoundCompilation</c>.</summary>
    public bool Active { get; set; }

    /// <summary>The program class being emitted has F3 declaratives (→ <c>__EcDispatch</c> exists). Set per
    /// unit.</summary>
    public bool UnitHasF3 { get; set; }

    /// <summary>The program class being emitted has an exception-checking (Format-3) PERFORM (§14.9.28) → the funnel
    /// emits <c>__EcPerform</c> (consult the ambient F3-frame stack before the USE dispatch) and the pc-range
    /// handler machinery is installed, EVEN when the unit declares no F3 USE declaratives (the <c>UnitHasF3</c>
    /// gate alone is insufficient — §9.5). Set per unit from <c>EcFeatures.HasF3Perform</c>; false for OO methods
    /// (F3-in-method is loud-rejected). A non-F3 unit emits byte-identical source.</summary>
    public bool UnitHasF3Perform { get; set; }

    /// <summary>… has F4 (EXCEPTION OBJECT) declaratives (→ <c>__EcObjDispatch</c> exists). Set per unit.</summary>
    public bool UnitHasF4 { get; set; }

    /// <summary>The group's class table — restored once per run unit with <see cref="Active"/>; the emitter resolves
    /// an element's PROCEDURE DIVISION header RAISING classes and interfaces through it (kb/Work PB1119).</summary>
    public Compiler.Oo.OoClassTable? OoClasses { get; set; }

    /// <summary>The source element being emitted runs with AUTOMATIC PROPAGATION enabled (ISO §7.3.21.4 GR1 — a
    /// <c>&gt;&gt;PROPAGATE ON</c> precedes it; kb/Work PB1119), or null when it does not (GR4's OFF default). Set per
    /// program unit and per METHOD, restored after each; read only by <c>EcEmitter</c>, where the fatal default is
    /// decided (§14.6.13.1.3 6)) and where an activator disposes of an exception object (§14.6.13.1.5).</summary>
    public AutomaticPropagation? Propagation { get; set; }

    /// <summary>The wrapper context of the statement being emitted (else null) — statement-scoped, saved/restored
    /// around each <c>BoundEcChecked</c> body.</summary>
    public EcStatementInfo? Info { get; set; }

    /// <summary>True while the code being emitted runs with some statement guard's run-time checking flags
    /// STANDING — inside an <c>EcEmitter.OpenGateFlags</c> scope and not yet re-based by an
    /// <c>EcEmitter.EnterCheckingBaseline</c> one (kb/Work PB891). Read only by <c>EnterCheckingBaseline</c>, so
    /// the all-off scope around other source statements is emitted exactly where a guard's flags could leak into
    /// them, and a statement list no flag guard encloses emits nothing.</summary>
    public bool FlagsStanding { get; set; }

    /// <summary>True while the statement being emitted has any EC-SIZE-* condition enabled (kb/Work PB91): the
    /// receiver-less numeric renders inside it — a relation operand, a function argument, a subscript, a SET
    /// amount — take the CHECKED kernels (MulChecked / AddChecked / SubChecked / DivideOrThrow, the checked NUMVAL
    /// landing), so an intermediate that overflows the Int128 carrier or a zero divisor raises CobolSizeError for
    /// the ambient EC-SIZE guard to dispatch (§14.7.5 no-phrase rule 3 + §14.6.13.1.3) instead of wrapping.</summary>
    public bool SizeChecking =>
        Info?.Enabled.Any(p => p.Ec.StartsWith("EC-SIZE-", StringComparison.Ordinal)) == true;

    /// <summary>True while the statement being emitted has EC-SIZE-TRUNCATION specifically enabled (ISO
    /// §7.3.25's TURN directive; §14.6.13.1.1 — "if checking for an exception condition is enabled", which is
    /// per level-3 NAME, not per family). The narrower sibling of <see cref="SizeChecking"/>, read by the ONE
    /// store shape that has no arithmetic statement to latch a flag in and therefore RAISES instead: the
    /// §14.2.3 GR9/GR10 argument crossing performed by the ACTIVATING element, on both the CALL lane
    /// (<c>CallEmitter.LandedForFormal</c>) and the INVOKE lane (<c>OoEmitter</c>'s BY CONTENT arms) — kb/Work
    /// PB640. It lives HERE rather than in either emitter because those two lanes are one rule, and a
    /// per-emitter copy of the enablement test is how they would come to disagree.</summary>
    public bool SizeTruncationChecking =>
        Info?.Enabled.Any(p => p.Ec == "EC-SIZE-TRUNCATION") == true;

    /// <summary>The current <c>__sizeErr</c> flag while emitting a checked arithmetic body (else null) —
    /// statement-scoped scratch set/cleared by the ON SIZE ERROR two-phase wrapper and read by the checked
    /// arithmetic stores (the EC↔arithmetic interlock).</summary>
    public string? SizeErrVar { get; set; }

    /// <summary>The current EC-SIZE name local while emitting a checked arithmetic body (else null) — the
    /// <c>&gt;&gt;TURN EC-SIZE</c> half of the interlock.</summary>
    public string? SizeErrEcVar { get; set; }
}

/// <summary>The inter-program emission state of the unit being emitted (COBOLNET_INTERPROGRAM_DESIGN D1–D5).</summary>
internal sealed class CallUnitState
{
    /// <summary>The emitted unit's qualified program path — the CALL/CANCEL calling-path argument (§8.4.6.3).
    /// Set per unit (a method body names its class).</summary>
    public string SelfPath { get; set; } = "";

    /// <summary>The LINKAGE RETURNING item's place (null when none) — the EXIT PROGRAM / GOBACK result store.
    /// Set per unit (methods deliver results via slice-2 RETURNING, never the program ABI).</summary>
    public Place? ReturningPlace { get; set; }

    /// <summary>This unit's PROCEDURE DIVISION USING formals (ISO §14.2.3 GR2), so a CALL site can recognize
    /// an argument that IS a whole formal parameter and forward its ARGUMENT CARRIER rather than a freshly
    /// built one (kb/Work PB165). That recognition is what realizes §8.8.4.8.4 GR1c — "the omitted-argument
    /// condition is true … if the argument corresponding to data-name-1 is itself a formal parameter for which
    /// the omitted-argument condition is true" — and §14.9.4.4 GR12's "except as an argument" exemption: a
    /// rebuilt carrier answers <c>IsNull</c> false, so the next callee saw a PRESENT argument, and a rebuilt
    /// BY CONTENT snapshot READ the omitted formal and raised EC-PROGRAM-ARG-OMITTED where the exemption
    /// applies. ⛔ The recognition is STRUCTURAL (identity against these items), never a <c>__lnk</c> name
    /// match: the name match only ever saw a CARRIER-RESIDENT formal, which is exactly why a GROUP formal —
    /// whose carrier is a copy-in field, not the caller's storage — lost its omission.
    /// Set per unit alongside <see cref="ReturningPlace"/>.</summary>
    public IReadOnlyList<LinkageFormal> Formals { get; set; } = [];

    /// <summary>The formals of the METHOD whose body is being emitted (empty outside a method body) — the method
    /// arm of the same recognition (kb/Work PB757): a method formal forwarded as a CALL or INVOKE argument carries
    /// its omitted-presence flag on, per §8.8.4.8.4 GR1c. Set and cleared by <c>OoEmitter.EmitMethod</c>.</summary>
    public IReadOnlyList<CobolNet.Compiler.Oo.OoFormal> MethodFormals { get; set; } = [];

    /// <summary>⛔ THE ONE "IS THIS ARGUMENT A WHOLE FORMAL PARAMETER?" RECOGNITION, over BOTH activation ABIs
    /// (§8.8.4.8.4 GR1c; kb/Work PB165 program arm, PB757 method arm): the presence fact to carry on, or null
    /// when the place is not a whole formal. A REFERENCE-MODIFIED view is never the formal itself (GR1c speaks
    /// of an argument that "is itself a formal parameter"); identity against the level-01/77 item is the
    /// whole-item test, since a subitem resolves to its own item and a level-01 entry carries no OCCURS.</summary>
    public OmittedProbe? WholeFormalProbe(Place p)
    {
        if (p is RefModPlace) return null;
        foreach (var f in Formals)
            if (ReferenceEquals(f.Item, p.Item)) return f.Probe;
        foreach (var f in MethodFormals)
            if (ReferenceEquals(f.Item, p.Item)) return f.Probe;
        return null;
    }

    /// <summary>For each GLOBAL file INHERITED from a container (ISO §13.18.30), the place of the OWNER's FILE
    /// STATUS item reached through the <c>__outer</c> instance chain. §12.4.5.8.4 GR1 NOTE 1: "In the case where
    /// a file-name is global and data-name-1 is not, data-name-1 is updated by references to file-name in
    /// contained programs even though data-name-1 is a local name" — the contained program's after-verb status
    /// store must write the OWNER's storage although the NAME is not visible to it. Rebuilt per emitted unit
    /// (nearest container first); consumed by the after-verb FILE STATUS store.</summary>
    public Dictionary<FileModel, Place> InheritedStatusPlace { get; } = [];
}

/// <summary>How an element compiled under <c>&gt;&gt;PROPAGATE ON</c> performs the "as if a GOBACK statement with the
/// RAISING LAST EXCEPTION phrase were executed" of ISO §14.6.13.1.3 6) / §14.6.13.1.5 (kb/Work PB1119).</summary>
/// <param name="InMethod">The element is a METHOD: GOBACK returns through <c>MethodReturn</c> and there is always an
/// activator. Otherwise a program or function: <c>ProgramReturn</c>, and only while <c>__asCalled</c> — §7.3.21.1
/// propagates "to the activating runtime element", and a run unit's main program has none (§14.9.18.4 GR3 makes
/// its GOBACK a STOP that ignores RAISING), so there the §14.6.13.1.3 7) termination stands.</param>
/// <param name="RaisingObjectCsTypes">The C# types an exception object must be an instance of to be "an applicable
/// class or interface … specified in the RAISING phrase of the procedure division header" of this element
/// (§14.6.13.1.5 item 3) — the classes' own types (FACTORY OF: the factory type) and each interface's implementing
/// types.</param>
internal sealed record AutomaticPropagation(bool InMethod, IReadOnlyList<string> RaisingObjectCsTypes)
{
    /// <summary>The element's automatic propagation, or null when it is OFF; <paramref name="raising"/> is its
    /// PROCEDURE DIVISION header RAISING phrase, resolved through <paramref name="table"/>.</summary>
    public static AutomaticPropagation? Of(bool on, bool inMethod, IEnumerable<Binding.Model.RaisingTarget> raising,
        Compiler.Oo.OoClassTable? table) =>
        !on ? null : new(inMethod, [.. raising.SelectMany(t => t.Kind switch
        {
            Binding.Model.RaisingTargetKind.ObjectClass when table?.Find(t.Name) is { } c =>
                (IEnumerable<string>)[t.Factory ? c.FactoryCsName : c.CsName],
            Binding.Model.RaisingTargetKind.Interface when table?.FindInterface(t.Name) is { } i => i.ImplementedCsTypes,
            _ => [],
        }).Distinct(StringComparer.Ordinal)]);
}
