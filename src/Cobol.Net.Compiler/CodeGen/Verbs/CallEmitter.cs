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

/// <summary>⛔ THE FOUR CROSSING FORMS of one operand's storage at an activation boundary (kb/Work PB663), and
/// the ONE vocabulary both sides of it speak: the ACTIVATING element builds its argument carrier in this shape
/// (<see cref="CallEmitter.RefCarrier"/>) and the ACTIVATED element declares and adopts its formal's carrier in
/// the same one (<c>ProgramEmitter</c>'s formal loop). They used to be two independent formulations of one
/// rule — a caller chain of three predicates and a callee <c>bool isNum</c> — and the callee's had no arm for a
/// managed-reference item at all, so a <c>USAGE POINTER</c> formal was declared as a space-filled
/// <c>ManagedPointer&lt;string&gt;</c> and the generated C# did not compile.
/// <para>The four are exhaustive over what storage a COBOL item can BE in this model: a native scalar field, a
/// fixed-width character image, the §8.5.1.12 variable-length component carrier, and a managed slot holding a
/// reference that has no byte image (<c>SlotWindow.CarriedBySlot</c>). <c>LinkageCarrierDriftTests</c> derives
/// its population from <see cref="PicCategory"/> itself, so a category added to the model is a RED TEST rather
/// than a silent fall into the character arm.</para></summary>
internal enum CallCrossing
{
    /// <summary>The item's own native carrier cell — a fixed-point or floating-point numeric leaf
    /// (<c>ManagedPointer&lt;long|ulong|Int128|UInt128|double|float&gt;</c>; kb/Work R12 + PB238).</summary>
    Native,

    /// <summary>The fixed-width CHARACTER IMAGE (<c>ManagedPointer&lt;string&gt;</c>) — alphanumeric, national,
    /// boolean, numeric-edited, a zoned-image numeric leaf, a Tier-B window, and any fixed-length group.</summary>
    Text,

    /// <summary>The §8.5.1.12 variable-length group carrier (<c>ManagedPointer&lt;CobolVarGroup&gt;</c>;
    /// kb/Work PB204).</summary>
    VarGroup,

    /// <summary>A MANAGED SLOT — class pointer (data / program / function) or class object-reference, whose
    /// value is a managed reference with no byte image (<c>ManagedPointer&lt;ManagedPointer|ProgramPointer|
    /// FunctionPointer|«class»?&gt;</c>; kb/Work PB663 + PB231).</summary>
    Managed,
}

/// <summary>The CALL / CANCEL / GOBACK / EXIT PROGRAM verb emitter (P7 Step 9m, BATCH-3a — a real collaborator
/// over the per-unit <see cref="EmitContext"/>; ISO §14.9.4 / §14.9.5 / §14.9.14 / §14.9.18): the activation
/// call with its BY REFERENCE/CONTENT/VALUE carriers, the EC-PROGRAM catch + RAISING propagation pickup, and
/// the ONE CALL-boundary string-carrier trio (<see cref="CallPlaceIsString"/>/<see cref="CallStringRead"/>/
/// <see cref="CallStringWrite"/>) Report Writer and the program-class emission reuse.</summary>
internal sealed class CallEmitter(EmitContext ctx, NumericRenderer num, EcState ecState, CallUnitState callState,
    EcEmitter ec, MoveEmitter move, DispatchState dispatch, PtrEmitter ptr)
{
    /// <summary>The statement dispatcher — property-wired by <see cref="UnitEmitters"/> (the ON/NOT-ON
    /// EXCEPTION phrase bodies nest arbitrary statement lists, a cyclic edge no ctor order can satisfy).</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    internal static string CallBool(bool b) => b ? "true" : "false";

    // ── Statement emitters: CALL / CANCEL / GOBACK ──────────────────────────────────────────────────────────

    /// <summary>Emit one CALL (ISO §14.9.4.4). With no exception phrase, a CALL failure (not found / recursive
    /// re-entry) propagates and terminates the run unit loudly (the 85 abnormal-termination surface; the
    /// EC-PROGRAM model is the §11 subsystem). With a phrase, the failure runs the ON imperative and control
    /// falls to the end of the CALL (GR3h); NOT ON runs only on a successful return (GR3i).</summary>
    public bool EmitCall(BoundCallProgram c)
    {
        var w = ctx.Writer;
        // §14.9.4.4 GR3a (kb/Work PB133 wave B): "item identification is done for identifier-3 at the
        // beginning of the execution of the CALL statement" — and 14.2.3 GR8 fixes each BY REFERENCE
        // argument's STORAGE AREA at the same point. The aliasing carriers re-render their subscript and
        // ref-mod expressions on every access, so a callee that reaches the caller's index item through
        // another BY REFERENCE argument could re-aim them mid-call; each variable index is hoisted into a
        // statement-local evaluated here, once.
        c = HoistOnceOnlyIdentification(c);
        // An EC-active group's CALL site consumes a callee-staged RAISING propagation itself (the pickup below
        // runs the §14.9.49 F3 selection and honors RESUME); an EC-free site emits none, and the staging — which
        // names THIS activation (kb/Work PB892 Arm B) — is then never raised anywhere (§14.9.18.4 GR1 b)).
        // §14.9.4.4 GR3d's ACTIVATING half (kb/Work PB133 wave C2b): this statement's TURN state.
        string invocation = InvocationText(c,
            argMismatchChecking: EnabledProgramNames().Contains("EC-PROGRAM-ARG-MISMATCH"));

        var ecProg = EnabledProgramNames();
        // The ACTIVATING half of §14.8.4.1's both-elements rule is NOT emitted here: this CALL statement's enabled
        // EC-EXTERNAL-* names are statement-guard checking flags (EcEmitter.FatalAmbientGates), and the activation
        // boundary (ProgramTable.CallProgram) reads them before the activated element's checking scope opens — the
        // ONE handshake the CALL, a function activation and an INVOKE's method activation share (kb/Work PB1138).
        // ── §14.9.4.4 GR3h/GR3i: the CALL statement's exception partition (kb/Work PB233) ────────────────────
        // ON EXCEPTION is the ONLY phrase that diverts a failed activation. GR3h item 1 names it explicitly,
        // and §14.6.13.1.3 #1 admits only "a conditional phrase WITHOUT the NOT phrase" — so a CALL written
        // with only NOT ON EXCEPTION behaves exactly like a CALL with no phrase at all (item 2 or item 3
        // governs). Keying the catch on "either phrase" let a NOT-ON-only CALL SWALLOW a failed activation.
        bool hasOn = c.OnException is not null;
        bool hasPhrase = hasOn || c.NotOnException is not null;
        var ecOther = EnabledOtherCallNames();
        if (!hasOn && ecProg.Count == 0 && ecOther.Count == 0)
        {
            // Nothing catches: the condition leaves the statement and takes §14.6.13.1 (item 3 → #8, this
            // implementation's loud abnormal termination). A NOT ON phrase can only be reached by a normal
            // return, so it needs no guard here — GR3i.
            w.Line(invocation);
            string? propagatedBare = EmitPropagationPickup(c, reportRaised: c.NotOnException is not null);
            if (c.NotOnException is { } notBare) EmitNotOnException(notBare, callErr: null, propagatedBare);
            return false;
        }
        int id = ctx.Names.NextCall();
        if (hasPhrase) w.Line($"bool __callErr{id} = false;");
        using (w.Block("try"))
            w.Line(invocation);
        // The arms, in the ONE order that keeps each reachable (a narrower filter must precede a broader one):
        //   1. enabled EC-PROGRAM-*/EC-EXTERNAL-*  → status set, then the phrase (item 1) or the declaratives (item 2)
        //   2. enabled non-EC-PROGRAM carriers     → status set, then the declaratives ALWAYS (item 2, 2nd disjunct)
        //   3. UNenabled EC-PROGRAM-*/EC-EXTERNAL-* → the phrase only (item 1 carries no checking-enabled qualifier),
        //      with NO status set (§14.6.13.1.1 sets an indicator only when checking is enabled).
        // Anything else — a name no arm claims, or ANY condition that escaped the CALLED program's execution —
        // falls through to §14.6.13.1, because §14.9.4.4 GR3i says that once the program "was successfully
        // called" the ON EXCEPTION phrase is ignored.
        string? flag = hasPhrase ? $"__callErr{id}" : null;
        if (ecProg.Count > 0) EmitCallEcCatch(ecProg, byPhrase: hasOn, flag);
        if (ecOther.Count > 0) EmitCallEcCatch(ecOther, byPhrase: false, flag);
        if (hasOn)
        {
            int pid = ctx.Names.NextEc();
            w.Line($"catch (CobolCallException __cp{pid}) when (!__cp{pid}.ControlTransferred "
                + $"&& {RuntimeApi.CallEcIsProgramOrExternalText($"__cp{pid}.EcName")}) {{ {flag} = true; }}"
                + "   // §14.9.4.4 GR3h item 1 (checking not enabled → no status is set) / GR3i");
        }
        // §14.9.4.4 GR3i's two outcomes of a SUCCESSFUL call are ALTERNATIVES (kb/Work PB606): "If an exception
        // condition is propagated from the called program, execution continues as specified in 14.6.13.1 …;
        // otherwise, control is transferred … to imperative-statement-2". So the propagation is picked up FIRST
        // — before either phrase body, which also keeps an activation inside a phrase body from consuming THIS
        // CALL's staging — and NOT ON EXCEPTION runs only when the pickup raised nothing. A failed activation
        // stages nothing, so the pickup is a no-op on the ON EXCEPTION path.
        string? propagated = EmitPropagationPickup(c, reportRaised: c.NotOnException is not null);
        if (c.OnException is { } on)
            using (w.Block($"if (__callErr{id})")) Statements.EmitStatementList(on);
        if (c.NotOnException is { } not)
            EmitNotOnException(not, callErr: $"__callErr{id}", propagated);
        return false;
    }

    /// <summary>The CALL statement's NOT ON EXCEPTION arm — ISO §14.9.4.4 GR3i: imperative-statement-2 runs only
    /// when the program was successfully called (<paramref name="callErr"/> is false, or null when nothing can
    /// fail the activation catchably) AND no exception condition was propagated from it
    /// (<paramref name="propagated"/>, the pickup's raised flag, or null when the group emits no pickup).
    /// §14.6.13.1.4 3) says the same from the other side: a nonfatal condition whose declarative completes
    /// normally leaves "the imperative-statement in that phrase … not executed".</summary>
    private void EmitNotOnException(IReadOnlyList<BoundStatement> not, string? callErr, string? propagated)
    {
        var guards = new[] { callErr, propagated }.OfType<string>().Select(g => "!" + g).ToList();
        if (guards.Count == 0) { Statements.EmitStatementList(not); return; }
        using (ctx.Writer.Block($"if ({string.Join(" && ", guards)})   // §14.9.4.4 GR3i — a successful call that propagated nothing"))
            Statements.EmitStatementList(not);
    }

    /// <summary>§14.9.4.4 GR3a's once-only identification (kb/Work PB133 wave B; PB1123): "item identification is done
    /// for identifier-3 at the beginning of the execution of the CALL statement" and §14.2.3 GR8 fixes each BY
    /// REFERENCE argument's storage area at the same point, so the ALIASING operands' places — each BY REFERENCE
    /// argument and the RETURNING identifier — have every run-time address fragment (subscript, reference-modifier
    /// position, ODO extent window) frozen into a statement-local by THE one mechanism,
    /// <see cref="PlaceIdentification"/>, shared with INSPECT, STRING, UNSTRING and INVOKE. The value operands
    /// (BY CONTENT / BY VALUE snapshots) already read once when the args array is built.</summary>
    private BoundCallProgram HoistOnceOnlyIdentification(BoundCallProgram c)
    {
        var hoist = PlaceIdentification.Hoister(ctx);
        var args = c.Args
            .Select(a => a.Mode == CobolPassMode.Reference && a.Place is { } p ? a with { Place = PlaceIdentification.Freeze(p, hoist) } : a)
            .ToList();
        var ret = c.Returning is { } rp ? PlaceIdentification.Freeze(rp, hoist) : c.Returning;
        return c with { Args = args, Returning = ret };
    }

    /// <summary>⛔ WHETHER THE ACTIVATED PROGRAM IS UNKNOWN TO THIS ACTIVATING ELEMENT — the lane §14.8.2.3.2 / §14.8.2.3.3 rule 1
    /// governs (kb/Work PB165): "a program for which there is no program-specifier in the REPOSITORY paragraph of the activating
    /// element and there is no NESTED phrase specified on the CALL statement", where "the formal parameter shall be of the same
    /// length as the corresponding argument" and nothing is converted. A user-defined function, a method, a NESTED call and a
    /// program-specifier's program take rule 2 (the PICTURE-level identity, or a COMPUTE/MOVE conversion that relates no
    /// lengths), which the binder checks where the callee is known; the length rule would refuse legal source there, so a
    /// call on those lanes describes no argument. A Format-1 CALL whose target is not a literal cannot tell which program it
    /// will reach, so it is on this lane only when the element writes no program-specifier at all — the one answer it can
    /// give soundly.</summary>
    private bool IsDynamicLane(BoundCallProgram c) =>
        !c.IsFunction
        && (ctx.Data.ProgramSpecifiers.Count == 0
            || (c.LiteralName is { } literal
                && !ctx.Data.ProgramSpecifiers.Values.Any(s => ExternalizedNames.Same(s.ExternalizedName, literal))));

    /// <summary>The <c>CobolArg[]</c> expression of one bound call's arguments — the ONE argument-array text of
    /// <see cref="EmitCall"/>, which renders every activation, statement-position or operand (kb/Work PB892).</summary>
    private string ArgsArrayText(BoundCallProgram c, bool describeArguments) => c.Args.Count == 0
        ? "System.Array.Empty<CobolArg>()"
        : $"new CobolArg[] {{ {string.Join(", ", c.Args.Select(a => ArgText(a, describeArguments)))} }}";

    /// <summary>⛔ THE ONE ACTIVATION-INVOCATION RENDERER — <see cref="EmitCall"/> renders every activation through it —
    /// statement-position and operand (kb/Work PB892) — so the three activation
    /// targets cannot be taught to one site and not the other (the two-arm dispatch, kb/Work PB847: the
    /// per-evaluation site read <c>c.LiteralName!</c> and had no pointer arm at all).
    /// <list type="bullet">
    ///   <item>a PROGRAM-POINTER CALL target (§14.9.4.3 SR1; P10 Step 7) — <c>ProgramRegistry.CallPointer</c>;</item>
    ///   <item>a FUNCTION-POINTER function-identifier (§8.4.3.2.4 GR4/GR6c) — <c>ProgramRegistry.CallFunctionPointer</c>,
    ///         whose NULL raise is EC-FUNCTION-PTR-NULL and whose locate miss is GR6b's EC-FUNCTION-NOT-FOUND;</item>
    ///   <item>a name — a literal, an identifier's value at CALL time (GR3b, read once per GR3a), or a
    ///         function-prototype's externalized name — <c>ProgramRegistry.CallProgram</c>.</item>
    /// </list>
    /// A pointer's carrier goes straight to the registry, never a name-string read.</summary>
    private string InvocationText(BoundCallProgram c, bool argMismatchChecking)
    {
        string head = $"{CsLiteral(callState.SelfPath)}, {ArgsArrayText(c, describeArguments: argMismatchChecking && IsDynamicLane(c))}, "
            + $"{(c.Returning is { } rp ? ReturningArgText(rp) : "null")}";
        // ⛔ GR3d's activating half rides EVERY arm (kb/Work PB1040's sweep): the two pointer arms used to drop it, so
        // a CALL through a program-pointer or a function-pointer never raised the argument-count or RETURNING
        // mismatch that the same CALL by name did.
        string site = argMismatchChecking ? ", siteArgMismatchChecking: true" : "";
        if (c.IsPointerTarget && c.DynamicName is BoundFieldOperand pf)
            return c.IsFunction
                ? $"ProgramRegistry.CallFunctionPointer({PlaceRenderer.Read(pf.Place)}, {head}{site});"
                : $"ProgramRegistry.CallPointer({PlaceRenderer.Read(pf.Place)}, {head}{site});";
        string nameExpr = c.LiteralName is { } literal
            ? CsLiteral(literal)
            : OperandText.AsString(c.DynamicName!, num);   // GR3b — the identifier's value at CALL time (GR3a: read once); ProgramTable forms the name
        return $"ProgramRegistry.CallProgram({nameExpr}, {head}"
            + $"{(c.IsFunction ? ", notFoundEc: \"EC-FUNCTION-NOT-FOUND\"" : "")}"   // §8.4.3.2.4 GR6b
            + $"{site});";
    }

    /// <summary>The current statement's enabled level-3 names that a <see cref="CobolCallException"/> can
    /// actually carry (empty when none / no wrapper). ONE filter, asked once and split two ways below: an
    /// enabled name outside <see cref="CobolCallException.CarriedNames"/> — EC-PROGRAM-RESOURCES and
    /// EC-PROGRAM-ARG-OMITTED are the live examples, the latter having left this carrier at kb/Work PB133 —
    /// has no raise site to match, so naming it in a catch filter emits a disjunct that can never be true, and
    /// a <c>&gt;&gt;TURN EC-ALL CHECKING ON</c> unit would emit a two-hundred-way one on every CALL.</summary>
    private List<string> EnabledCallNames() =>
        ecState.Info?.Enabled.Select(p => p.Ec).Where(RuntimeApi.CallEcIsCarried).ToList() ?? [];

    /// <summary>The enabled EC-PROGRAM-* / EC-EXTERNAL-* names of the current statement — the two families a
    /// CALL raises through <see cref="CobolCallException"/> (ISO §14.9.4.4 GR3b–f: locate/recursion/argument
    /// failures; GR3e: the §14.8.4 external-conformance trio). All are Table 13 Fatal and GR3h item 1 gives the
    /// ON EXCEPTION phrase both families, so they share one catch arm. The partition itself is
    /// <see cref="CobolCallException.IsProgramOrExternal"/> — written down ONCE, next to the carrier, so this
    /// compile-time split and the emitted runtime filter cannot drift apart. Also the source of GR3d's
    /// ACTIVATING-half argument-checking flag.</summary>
    private List<string> EnabledProgramNames() =>
        EnabledCallNames().Where(RuntimeApi.CallEcIsProgramOrExternal).ToList();

    /// <summary>The complement: enabled carriable names NOT in GR3h item 1's two families (today only
    /// EC-FUNCTION-NOT-FOUND — §8.4.3.2.4 GR6b, a user-defined-function locate miss). These take ISO §14.9.4.4
    /// GR3h item 2's SECOND disjunct — "or if the exception condition is not one of the EC-PROGRAM exception
    /// conditions, any applicable exception processing statements are executed" — with NO ON EXCEPTION escape,
    /// which is why they need an arm of their own rather than a share of the family arm.</summary>
    private List<string> EnabledOtherCallNames() =>
        EnabledCallNames().Where(n => !RuntimeApi.CallEcIsProgramOrExternal(n)).ToList();

    /// <summary>Emit ONE name-filtered <c>catch (CobolCallException)</c> arm of a CALL under enabled checking
    /// (§9.1.13-style bridge for the inter-program family: the runtime latched the Table 13 level-3 name in
    /// <see cref="CobolCallException.EcName"/>): set the last exception status (§14.6.13.1.1), flag the
    /// statement as failed (so GR3i's NOT ON phrase cannot run over a failed activation), then either leave it
    /// to the statement's own ON EXCEPTION phrase — <paramref name="byPhrase"/>, §14.6.13.1.3 #1 / §14.9.4.4
    /// GR3h item 1 — or run the §14.9.49 F3 selection with the fatal default (every name reachable here is
    /// Table 13 Fatal: the EC-PROGRAM-*/EC-EXTERNAL-* families and EC-FUNCTION-NOT-FOUND).
    /// <para><c>!ControlTransferred</c> is the GR3h/GR3i boundary: GR3h speaks only of a program that "was not
    /// successfully called", so an exception raised INSIDE the called program's execution is none of this
    /// statement's business (GR3i) and must fall through to §14.6.13.1. A CobolCallException whose name is not
    /// enabled likewise falls through to the next arm / propagates — the checking-off behavior unchanged.</para>
    /// </summary>
    private void EmitCallEcCatch(List<string> ecNames, bool byPhrase, string? phraseFlag)
    {
        var w = ctx.Writer;
        int id = ctx.Names.NextEc();
        string nameTest = string.Join(" || ", ecNames.Select(n => $"__ce{id}.EcName == {CsLiteral(n)}"));
        using (w.Block($"catch (CobolCallException __ce{id}) when (!__ce{id}.ControlTransferred && ({nameTest}))"))
        {
            // The §15.32.3 r2 pair rides the CALL statement's ambient context (kb/Work R14) — the callee's own
            // contexts were restored on unwind, so this Set attributes the CALL, not the callee's last statement.
            w.Line($"ExceptionState.Set(__ce{id}.EcName, true);   // §14.6.13.1.1 — every name reachable here is fatal (Table 13)");
            if (phraseFlag is not null)
                w.Line($"{phraseFlag} = true;   // the activation failed — GR3i's NOT ON phrase shall not run");
            if (byPhrase)
                w.Line("// the statement's ON EXCEPTION phrase handles it (§14.6.13.1.3 #1; §14.9.4.4 GR3h item 1)");
            else
                ec.EmitSelection($"__ce{id}.EcName",
                    EcEmitter.FatalTermination($"__ce{id}.EcName", $"__ce{id}.Message"));   // every name here is fatal (Table 13)
        }
    }

    /// <summary>Emit the activator-side pickup of a callee-staged <c>GOBACK / EXIT PROGRAM / method-return …
    /// RAISING</c> exception condition — ISO §14.9.18.4 GR1 b), RAISED HERE, in the activating runtime element,
    /// "as if a RAISE statement" at the end of the activating statement (§14.6.13.1.3 #6): test the ACTIVATOR's
    /// checking state for the propagated name, and when it is enabled set the last exception status
    /// (§14.6.13.1.1), run the §14.9.49 Format-3 selection over the DYNAMIC name, honor RESUME and apply the
    /// fatal default.
    /// <para>Whether the pickup is EMITTED still gates on the group's EC participation (<c>EcState.Active</c>) —
    /// zero scaffolding for a group that uses no EC feature. Whether it RAISES gates on
    /// <paramref name="site"/>'s own <see cref="CobolNet.Runtime.Exceptions.EcCheckingProfile"/>, which answers
    /// the per-name question for a name chosen at run time. Before kb/Work PB408 the group gate was the ONLY
    /// gate and the per-name test was taken in the callee, so this site raised conditions the activating element
    /// had turned off and skipped ones it had turned on.</para></summary>
    /// <returns>With <paramref name="reportRaised"/>, the name of a <c>bool</c> local that is true once a
    /// propagated condition (a name or an object) was RAISED here — the §14.9.4.4 GR3i "propagated" test the
    /// CALL's NOT ON EXCEPTION phrase keys on (kb/Work PB606). Null when no pickup is emitted (nothing can be
    /// raised) or no caller asked.</returns>
    public string? EmitPropagationPickup(IActivatingStatement site, bool reportRaised = false)
    {
        if (!ecState.Active) return null;
        var w = ctx.Writer;
        int id = ctx.Names.NextEc();
        string? raised = reportRaised ? $"__praised{id}" : null;
        if (raised is not null) w.Line($"bool {raised} = false;");
        using (w.Block($"if (ExceptionState.TakePropagatedObject(out var __po{id}))   // §14.6.13.1.5 — an exception OBJECT propagated"))
        {
            w.Line($"ExceptionState.SetObject(__po{id});   // GR1b2 — the current exception object HERE (the activator)");
            w.Line($"int __or{id} = {ec.ObjDispatchExpr($"__po{id}")};   // rule 2 — USE AFTER EXCEPTION OBJECT (GR14)");
            w.Line(dispatch.ResumeTransfer($"__or{id}", "   // RESUME AT procedure-name"));
            // The object was RAISED here iff a USE AFTER EXCEPTION OBJECT declarative took it. One that no declarative
            // took becomes the NAMED EC-OO-EXCEPTION below, and THAT is raised (and reported) only by the named arm's
            // activator-checking gate (§14.9.4.4 GR3i: the NOT ON EXCEPTION phrase keys on a condition "propagated"
            // and raised, the reading the named arm already follows — kb/Work PB606, PB1121).
            if (raised is not null)
                w.Line($"if (__or{id} != DispatchResult.NoHandler) {raised} = true;");
            using (w.Block($"if (__or{id} == DispatchResult.NoHandler)   // no declarative took it: item 3 (PROPAGATE ON), else item 4"))
            {
                // Item 3 — under >>PROPAGATE ON the activator re-propagates it (kb/Work PB1119); it returns, so what
                // follows is item 4 for an element without the directive (or a main program, which has no activator).
                if (ec.ObjectPropagationReturn($"__po{id}") is { } propagate)
                    w.Line(propagate + "   // §14.6.13.1.5 item 3");
                // Item 4 — "as if EXCEPTION EC-OO-EXCEPTION were specified in the RAISING phrase, instead of an
                // exception object": re-stage it as that NAMED propagation, which the named arm below takes through
                // §14.9.18.4 GR1 b) — raised HERE only if EC-OO-EXCEPTION checking is enabled HERE, then the F3
                // selection and Table 13's fatal default. With checking off nothing is raised and the activation
                // completes (§14.6.13.1.1: "By default, checking is not enabled for any exception condition").
                w.Line("ExceptionState.ConvertObjectPropagationToNamed();   // §14.6.13.1.5 item 4");
            }
            w.Line("// Normal/ResumeNext: declarative completed / RESUME NEXT — normal continuation (:24604)");
        }
        using (w.Block($"if (ExceptionState.TakeRaisedPropagation({CsLiteral(site.ActivatorChecking.Encoded)}, "
            + $"out var __pn{id}, out var __pf{id}))   // §14.9.18.4 GR1b — raised HERE iff checking is enabled HERE"))
        {
            if (raised is not null) w.Line($"{raised} = true;");
            // The propagated name — and so its fatality — is chosen at run time: the staged flag gates the default.
            ec.EmitSelection($"__pn{id}",
                EcEmitter.FatalTermination($"__pn{id}",
                    "\"exception condition propagated by GOBACK/EXIT PROGRAM RAISING and not resumed "
                    + "(ISO 14.9.18; 14.6.13.1.3 #6/#7)\""),
                fatalWhen: $"__pf{id}");
        }
        return raised;
    }

    /// <summary>The C# <c>CobolArg</c> expression for one bound CALL argument (caller side; design D1/D2).
    /// BY REFERENCE builds an accessor carrier over the caller's storage (§14.2.3 GR8); BY CONTENT/BY VALUE
    /// snapshot the value into a cell AT CALL INITIATION — which also realizes the §14.9.4.4 GR3a once-only
    /// evaluation for those modes. (A BY REFERENCE accessor over a SUBSCRIPTED operand re-evaluates the
    /// subscript inside the closure — the GR3a capture-into-locals refinement is a known follow-up.)</summary>
    public string ArgText(BoundCallArg a, bool describe) => LandedForFormal(a, ArgCarrierText(a, describe));

    /// <summary>⛔ THE ACTIVATING ELEMENT'S §14.2.3 GR9/GR10 COMPUTE (kb/Work PB640) — wrapped around EVERY
    /// argument carrier shape <see cref="ArgCarrierText"/> builds, which is why it is a wrapper and not a
    /// branch inside one of them.
    /// <para>GR9's second branch and GR10 both say the linkage record is "allocated by the activating runtime
    /// element during the process of initiating the activation" and make the argument the sending operand of
    /// "a COMPUTE statement without the ROUNDED phrase" into it. This implementation used to perform that
    /// COMPUTE entirely CALLEE-side (<c>CobolArgAdapt.NumValue</c> / <c>Num</c>), where the activating
    /// element's <c>&gt;&gt;TURN EC-SIZE CHECKING</c> state, its USE declaratives and §14.9.4.4 GR3g's
    /// "control is transferred to the called program" have all already been left behind — so a BY CONTENT /
    /// BY VALUE argument overflowing its formal's description under checking stored DOC-A.1-70's low-order
    /// digits silently where §14.7.5's no-phrase rule 4 sets EC-SIZE-TRUNCATION to exist.</para>
    /// <para>The raise needs no new machinery: EC-SIZE-TRUNCATION is a FATAL ambient gate
    /// (<c>EcEmitter.FatalAmbientGates</c>) and a CALL is not an <c>IArithmeticStatement</c>, so a CALL
    /// compiled under EC-SIZE checking already carries the try/catch that sets the last exception status, runs
    /// the §14.9.49 F3 selection and honours RESUME. The landing is emitted INSIDE the argument expression, so
    /// the throw happens while the <c>CobolArg[]</c> is being built — before <c>ProgramRegistry.CallProgram</c>
    /// is entered, which is exactly GR3g's ordering — and it works in an EXPRESSION-position activation (a
    /// user-defined function reference inside a per-evaluation condition window) as well as at statement
    /// position.</para>
    /// <para>Only BY CONTENT / BY VALUE, and only a NUMERIC formal: BY REFERENCE is GR8's storage
    /// aliasing with no crossing conversion; a group, index, pointer, object or edited formal takes GR9's
    /// MOVE leg; a formal "of class index, object, or pointer" takes GR9's SET leg — and USAGE INDEX is why the
    /// guard is <c>PicInfo.IsClassNumeric</c> and not a bare category test: an index item's storage
    /// description carries category Numeric with ZERO digits, so landing it through a numeric profile stored
    /// <c>value % 10^0</c> = 0 and <c>BY CONTENT</c> an index of 3 crossed as 0 (measured). A FLOATING-POINT
    /// formal is a numeric receiver of the same COMPUTE (§14.8.2.3.3 2) a) names no exemption — kb/Work PB1114:
    /// the activator's COMPUTE covered the fixed-point formals only, so a fixed-point argument BY CONTENT to a
    /// FLOAT-LONG formal of a NESTED activation arrived as 0): it lands through <c>FloatResultant</c> rather
    /// than <c>CobolNum.Store</c>, because a float description has no digit positions to reduce to (§14.6.8.3
    /// GR1 — the IEEE receiver takes the algebraic value, rounded as the no-ROUNDED COMPUTE rounds it).
    /// <c>a.Formal</c> is null for exactly GR9's FIRST branch, whose
    /// record is moved "without conversion" — see <see cref="BoundCallArg.Formal"/>.</para>
    /// <para>⛔ The carrier is <c>PicInfo.ClrType</c>, NOT <c>DataItem.ElementType</c>: an IMAGE-STORED numeric
    /// formal (a REDEFINED elementary one, a Tier-B window) answers <c>"string"</c> for its field type, which
    /// the landing's <c>where T : struct, INumberBase&lt;T&gt;</c> constraint cannot take — generated C# that
    /// does not compile. The allocated record of GR9/GR10 is a data item of the FORMAL'S DESCRIPTION, and that
    /// description's value carrier is its PICTURE's; the callee's image-carried adapters
    /// (<c>CobolArgAdapt.Text</c> / <c>TextValue</c>) read a native numeric cell through the
    /// <c>(Digits, Scale)</c> meta, which the landing has just set to the formal's own.</para>
    /// <para>The kernel selection is COMPILE-time (<c>EcState.SizeTruncationChecking</c>), like the arithmetic
    /// store's <c>checkedLanding</c>: a unit with checking off emits the unchecked landing and the §14.7.5
    /// no-phrase disposition documented in <c>CONFORMANCE.md</c> DOC-A.1-70 stands.</para></summary>
    private string LandedForFormal(BoundCallArg a, string built) =>
        !a.Omitted
        && a.Mode is CobolPassMode.Content or CobolPassMode.Value
        && a.Formal is { } f
        && f.Pic is { IsClassNumeric: true } fp
            ? RuntimeApi.ArgLandForFormal(built, fp.ProfileInitializer(ctx.SignEncoding), $"{fp.Scale}",
                                          fp.ClrType, ecState.SizeTruncationChecking)
            : built;

    /// <summary>⛔ THE DESCRIPTION a place's storage carries across the activation boundary, as the trailing
    /// <c>CobolArg</c> constructor arguments (kb/Work PB873 + PB965): an elementary NUMERIC item's whole
    /// <c>NumProfile</c> (Place.DenotedItem — a reference-modified view denotes no item and is character storage;
    /// a USAGE INDEX item's storage description has no digit positions for a profile to state), and a GROUP's
    /// §8.5.1.12 atoms when it has a table or a variable-length member (<see cref="BoundaryAtoms"/> — the fact
    /// §8.5.1.12.2's correspondence needs from each side, since the two sides are compiled apart). ONE
    /// renderer for the argument and the RETURNING item, because both are storage the activating element owns
    /// and the activated element reaches through a carrier.</summary>
    private string PlaceDescription(Place p)
    {
        string meta = BoundaryProfile(p, ctx.SignEncoding) ?? "null";
        return BoundaryAtoms(p) is { } atoms ? $"{meta}, {atoms}" : meta;
    }

    /// <summary>The emitted <c>NumProfile</c> of an elementary NUMERIC place's storage, or null when it has none
    /// (see <see cref="PlaceDescription"/>) — the ONE place that decides which items state a numeric profile, for the
    /// activating element's <c>CobolArg</c> and the activated unit's registration alike (kb/Work PB1040).</summary>
    private static string? BoundaryProfile(Place p, SignEncoding signEncoding) =>
        p.DenotedItem is { Pic: { Category: PicCategory.Numeric } pp } && pp.Usage is not Usage.Index
            ? pp.ProfileInitializer(signEncoding)
            : null;

    /// <summary>⛔ THE REGISTERED DESCRIPTION of a unit's RETURNING item (<c>BoundaryItem</c>, kb/Work PB1040): the same
    /// two facts the activating element's <c>CobolArg</c> states for its receiver — <see cref="BoundaryProfile"/> and
    /// <see cref="BoundaryLength"/> — emitted at the unit's <c>ProgramRegistry.Register</c>, so
    /// §14.9.4.4 GR3 d) can compare the pair at call initiation. Null when it states neither.</summary>
    internal static string? RegisteredReturning(Place p, SignEncoding signEncoding) =>
        RegisteredBoundary(p, signEncoding, BoundaryLength(p));

    /// <summary>⛔ THE REGISTERED DESCRIPTION of one FORMAL (<c>BoundaryItem</c>, kb/Work PB165): what the activated unit
    /// states for a formal so that §14.8.2 has something to compare an argument with at a dynamic CALL. It is the same
    /// description the activating element states for the argument (<see cref="ArgumentFacts"/>), measured by the same
    /// functions, so the two sides cannot disagree about what a "length" counts. Never null: an element that states nothing
    /// is the unstated <c>new BoundaryItem(null)</c>, so the array stays positional.</summary>
    internal static string RegisteredFormal(Place p, SignEncoding signEncoding) =>
        RegisteredBoundary(p, signEncoding, ArgumentLength(p)) ?? "new BoundaryItem(null)";

    private static string? RegisteredBoundary(Place p, SignEncoding signEncoding, int length)
    {
        string? profile = BoundaryProfile(p, signEncoding);
        var cls = BoundaryClassOf(p);
        return profile is null && length == RuntimeApi.UnstatedBoundaryLength && cls == BoundaryClass.Other ? null
            : $"new BoundaryItem({profile ?? "null"}"
              + $"{(length == RuntimeApi.UnstatedBoundaryLength ? "" : $", {length}")}"
              + $"{(cls == BoundaryClass.Other ? "" : $"{(length == RuntimeApi.UnstatedBoundaryLength ? ", Class: " : ", ")}BoundaryClass.{cls}")})";
    }

    /// <summary>The class facts of a place's item that §14.8.2.2 / §14.8.3.2 hang a rule on (<see cref="BoundaryClass"/>;
    /// kb/Work PB165): an alphanumeric group (not strongly typed, not variable-length), an elementary item of category
    /// alphanumeric — a reference-modified operand is one (§8.4.3.3.4 GR6) — and the exempt strongly-typed and
    /// variable-length groups.</summary>
    internal static BoundaryClass BoundaryClassOf(Place p)
    {
        if (p is RefModPlace) return BoundaryClass.Alphanumeric;
        if (p.DenotedItem is not { } item) return BoundaryClass.Other;
        if (item.IsAsIfElementary) return BoundaryClass.Other;   // a bit / national group is the elementary item it is treated as
        if (item.IsGroup)
            return StrongTypeModel.IsStronglyTyped(item) || CrossingOf(p) is CallCrossing.VarGroup
                ? BoundaryClass.Exempt : BoundaryClass.Group;
        return item.Pic is { Category: PicCategory.Alphanumeric } ? BoundaryClass.Alphanumeric : BoundaryClass.Other;
    }

    /// <summary>The statement of an ARGUMENT's description beyond its numeric profile — its storage length and class — as
    /// the trailing named <c>CobolArg</c> constructor arguments (kb/Work PB165), or empty when it states neither. Emitted
    /// only at a CALL site that checks EC-PROGRAM-ARG-MISMATCH: that is the one site where the activated unit's registered
    /// formals are compared with it (§14.9.4.4 GR3 d)), so an unchecked CALL's text is what it always was.</summary>
    private static string ArgumentFacts(Place p) => NamedFacts(ArgumentLength(p), BoundaryClassOf(p));

    /// <summary>The trailing named <c>CobolArg</c> arguments of a stated length and class (empty for neither) — the ONE
    /// spelling of them, for an argument (<see cref="ArgumentFacts"/>) and a RETURNING receiver
    /// (<see cref="ReturningArgText"/>) alike.</summary>
    private static string NamedFacts(int length, BoundaryClass cls) =>
        $"{(length == RuntimeApi.UnstatedBoundaryLength ? "" : $", Length: {length}")}"
        + $"{(cls == BoundaryClass.Other ? "" : $", Class: BoundaryClass.{cls}")}";

    /// <summary>⛔ THE STORAGE LENGTH an item states for §14.8.2.3.2 / §14.8.2.3.3 rule 1's "same length" (kb/Work PB165):
    /// <see cref="BoundaryLength"/> for a text-carried item, and the item's BYTE-LENGTH (<c>DataItem.ImageWidth</c>, the one
    /// width authority) for a fixed-point or floating-point native cell — so a <c>PIC 9(4)</c> argument and a <c>PIC X(4)</c>
    /// formal are both four long. A USAGE INDEX item, a pointer or object reference and a variable-length group state none
    /// (their conformance is another rule's); ONLY a RETURNING item's FIT uses <see cref="BoundaryLength"/> alone, because a
    /// native cell is not fitted.</summary>
    internal static int ArgumentLength(Place p) =>
        BoundaryLength(p) is var text && text != RuntimeApi.UnstatedBoundaryLength ? text
        : CrossingOf(p) is CallCrossing.Native
          && p.DenotedItem is { IsGroup: false, IsAnyLength: false, Pic: { } pic } item && pic.Usage is not Usage.Index
          && item.ImageWidth is > 0 and var width
            ? width
            : RuntimeApi.UnstatedBoundaryLength;

    /// <summary>The emitted §8.5.1.12 ATOMS of a group place that has a table or a variable-length member
    /// (<c>CobolArg.Atoms</c>; kb/Work PB2280), or null when it has neither (its one §8.5.1.12 fact is then its length,
    /// which the runtime reads off the carrier). A reference-modified view is character storage, never a group.</summary>
    internal static string? BoundaryAtoms(Place p) =>
        // A redefinition or cell VIEW of a group is that group's storage and states its atoms like any other (the
        // group a CALL claims onto a cell is one — kb/Work PB2087); a reference-modified one denotes no item.
        // A bit / national group crosses as its ELEMENTARY value, which has no image layout (kb/Work PB1166).
        p.DenotedItem is { IsAsIfElementary: false } item
        && VariableLengthCompatibility.GroupAtoms(item) is { } atoms
        && VariableLengthCompatibility.HasTableOrVariable(atoms)
            ? RuntimeApi.GroupAtomsNew(atoms)
            : null;

    /// <summary>⛔ THE STORAGE AREA OF A BY REFERENCE ARGUMENT (kb/Work PB2087) — the C# expression of the cell area it
    /// occupies, or null when its storage is not a cell. ISO §14.2.3 GR8: "If the argument is passed by reference,
    /// the activated runtime element operates as if the formal parameter occupies the same storage area as the argument."
    /// An argument whose class lives in a <c>StorageCell</c> — a group a CALL claims (<c>DataBinder.PtrBindBasedAndAddressables</c>),
    /// an EXTERNAL, BASED or ADDRESS-OF-taken record, an area formal passed on — states the cell and the offset it begins
    /// at, the same pair <c>ADDRESS OF</c> renders (<c>PtrEmitter.AddressOfText</c>), and an area formal is laid over
    /// exactly those positions. A reference-modified operand is "a subset of the data item referenced by identifier-1" (§8.4.3.3.4 GR5) beginning at its leftmost-position — the inner
    /// view's offset displaced by the checked zero-based start, as ADDRESS OF displaces it. A CALL states it as the
    /// <c>CobolArg</c>'s <c>Area</c> (<see cref="AreaArgument"/>) and an INVOKE passes it as the method formal's area
    /// parameter; a whole formal of the current source element answers through <see cref="ArgumentArea"/>.</summary>
    internal static string? AreaOf(Place p)
    {
        string? start = null;
        if (p is RefModPlace rm)
        {
            if (rm.Inner is not RedefViewPlace) return null;
            start = PositionRenderer.Render(rm.Start);
            p = rm.Inner;
        }
        // A bit item's positions are bits and a national one's two bytes each, so only an identity-coded view's start is
        // a character offset a reference-modified slice can be displaced by.
        if (FullAllocation(p) is not RedefViewPlace { Cell: { } cell } view
            || start is not null && view.Coding is not null) return null;
        // ⛔ A BIT ITEM HAS AN AREA TOO (kb/Work PB2095): its window is stated in BIT positions of the cell
        // (BitWindow.Offset, absolute in the cell), and §14.9.4.3 SR6 — "identifier-2 shall be described such that
        // it is aligned on a byte boundary" — makes a BY REFERENCE bit item's first bit a whole number of characters
        // into the cell, so its area begins at that bit offset over the bits a character holds. The activated element's
        // bit-group area formal windows its bits at BitsPerCharacter × the area's offset (PlaceRenderer), the same unit.
        string at = view.Bit is { } bit
            ? $"({PositionRenderer.Render(bit.Offset)}) / {BitLayout.BitsPerCharacter}"
            : PositionRenderer.Render(view.Offset);
        string offset = start is null ? at : $"({at}) + ({start}) - 1";
        // ⛔ A VARIABLE-LENGTH GROUP'S AREA ALSO BEGINS AT A COMPONENT (kb/Work PB2094): its dynamic-length items and
        // dynamic-capacity tables are slots of the cell numbered from the group's first component ordinal
        // (VarGroupWindow.DynBase), so the area states that ordinal and the group's §8.5.1.12 atoms, and a formal of the
        // same storage numbers its own components from there (CobolArgAdapt.Area). A group whose atoms cannot be stated
        // (a USAGE BIT leaf makes a character position non-positional) states no area: no description could be shown
        // to share its storage, so the formal holds a copy.
        if (view.Coding is VarGroupWindow g)
            return view.DenotedItem is { } group && VariableLengthCompatibility.GroupAtoms(group) is { } atoms
                ? RuntimeApi.ArgArea(PlaceRenderer.RenderPath(cell, AccessDir.Sending), offset, PositionRenderer.Render(g.DynBase), RuntimeApi.GroupAtomsNew(atoms))
                : null;
        return RuntimeApi.ArgArea(PlaceRenderer.RenderPath(cell, AccessDir.Sending), offset);
    }

    /// <summary>The area expression of a BY REFERENCE argument for any activation lane: a whole formal of the current
    /// source element passes on the area it occupies (<c>CallUnitState.WholeFormalArea</c>), any other operand its own
    /// cell area (<see cref="AreaOf"/>); "null" when it has none.</summary>
    internal string ArgumentArea(Place p) => callState.WholeFormalArea(p) ?? AreaOf(p) ?? "null";

    /// <summary>The trailing named <c>Area</c> argument of a BY REFERENCE <c>CobolArg</c>, or empty when it has none.</summary>
    private string AreaArgument(Place p) => ArgumentArea(p) is var area && area != "null" ? $", Area: {area}" : "";

    /// <summary>⛔ THE BY CONTENT RECORD OF A GROUP WHOSE VALUES RIDE MANAGED SLOTS (kb/Work PB1940): ISO §14.2.3 GR9 —
    /// "the activated runtime element operates as if the record in the linkage section were allocated by the activating
    /// runtime element during the process of initiating the activation and as if this record does not occupy the same
    /// storage area as the argument". A strongly-typed group with an object-reference or pointer leaf has no character
    /// image to snapshot, so its record is a DETACHED COPY of its area — the characters and every reference
    /// (<c>CobolArgAdapt.ContentRecord</c>) — stated as the argument's <c>Area</c>, which the formal (of the same type,
    /// §14.8.2.2) is laid over: GR9's MOVE between two groups of one type, with the references set. Empty for every
    /// other BY CONTENT argument, which crosses as its value.</summary>
    private string ContentRecordArgument(BoundCallArg a, Place p) =>
        a.Mode is CobolPassMode.Content && CobolNet.Compiler.Oo.OoClassTable.LeafCarried(p.Item)
        && ArgumentArea(p) is var area && area != "null"
            ? $", Area: {RuntimeApi.ArgAdaptContentRecord(area, p.Item.ByteWidth)}"
            : "";

    /// <summary>The C# array literal of a §8.5.1.12 layout.</summary>
    internal static string LayoutArray(int[] layout) => $"new int[] {{ {string.Join(", ", layout)} }}";

    /// <summary>The RETURNING item's <c>CobolArg</c> (kb/Work PB962/PB965): the same BY REFERENCE carrier an
    /// argument gets (§14.2.3 GR6 NOTE 1 — "the storage for the returning item is allocated in the activating
    /// source unit") plus the same description, so the activated element's delivery can place the result by
    /// the RECEIVER's shape.
    /// <para>⛔ It also states the receiver's fixed CHARACTER LENGTH (<see cref="BoundaryLength"/>, kb/Work PB1040): the
    /// delivery fits the result to it and the activation checks it against the callee's, §14.8.3.3's same-PICTURE
    /// rule for the one boundary fact a string carrier does not carry itself.</para></summary>
    private string ReturningArgText(Place rp) =>
        $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Reference)}, {RefCarrier(rp)}, {PlaceDescription(rp)}"
        + $"{NamedFacts(BoundaryLength(rp), BoundaryClassOf(rp))}"
        // A receiver whose values ride managed slots states its AREA, the storage §14.6.5's delivery places the result
        // in (§14.2.3 GR6 NOTE 1 — "the storage for the returning item is allocated in the activating source unit";
        // kb/Work PB1940): its references cannot cross as characters.
        + $"{(rp.DenotedItem is { } g && CobolNet.Compiler.Oo.OoClassTable.LeafCarried(g) ? AreaArgument(rp) : "")})";

    /// <summary>⛔ THE FIXED CHARACTER LENGTH a place's text-carried storage has across the activation boundary
    /// (<see cref="CobolArg.Length"/>; kb/Work PB1040), <see cref="CobolArg.Unstated"/> when it has none. ONE
    /// answer for both ends of a RETURNING crossing — the activating element's receiver
    /// (<see cref="ReturningArgText"/>) and the activated unit's registration (<c>ProgramEmitter</c>) — measured in
    /// the unit <see cref="CallStringRead"/> measures the image in (<see cref="BoundaryImageWidth"/>), so the two
    /// sides cannot disagree about what a "length" counts. Only the TEXT crossing has one: a native cell, a
    /// managed slot and a variable-length group carry their own description. A DYNAMIC LENGTH item's length is a
    /// run-time value and an ANY LENGTH item takes the other side's (§14.8.3.3 rules 4 and 5).</summary>
    internal static int BoundaryLength(Place p) =>
        // DenotedItem is null for a reference-modified view, which is an alphanumeric slice with no item length of
        // its own (§8.4.3.3.4 GR5/GR6); the identity question is the model's, never re-derived here (kb/Work PB602).
        CrossingOf(p) is CallCrossing.Text
        && p.DenotedItem is { IsDynamicLength: false, IsAnyLength: false } item
        && BoundaryImageWidth(item) is > 0 and var width
            ? width
            : RuntimeApi.UnstatedBoundaryLength;

    /// <summary>The C# <c>CobolArg</c> expression for one bound CALL argument BEFORE the §14.2.3 GR9/GR10
    /// landing <see cref="LandedForFormal"/> wraps around it.</summary>
    private string ArgCarrierText(BoundCallArg a, bool describe)
    {
        // §14.9.4.4 GR11 (kb/Work PB133 wave C): the omitted argument crosses as the NULL carrier —
        // CobolArgAdapt.Present answers false, the formal's adapters hand out the GR12 checked-raise carrier,
        // and a forwarded omitted formal stays omitted (GR1c) because IsNull rides the carrier itself.
        if (a.Omitted)
            return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Reference)}, ManagedPointer.Null, null)";
        // §14.9.4.2 Format 2's boolean-expression-1 (kb/Work PB238) — FIRST, because a boolean value is
        // string-CARRIED like an alphanumeric one and the Place/Value arms below read a place or an operand
        // this argument does not have. §8.8.2 rule 10 fixes the value's length at the largest boolean ITEM
        // referenced (0 = literals only, which carry no item width, so the callee's own store fits it) — the
        // same width §14.9.8.4 GR3 states for a boolean COMPUTE, applied here exactly as EmitCompute and
        // OoEmitter's INVOKE twin apply it. Digits/Scale are 0: this is character storage, not numeric meta.
        if (a.ContentBool is { } cb)
        {
            string bv = BooleanRenderer.RenderAtItemWidth(cb, num);
            return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<string>.Cell({bv}), null)";
        }
        // ⛔ AN ADDRESS-IDENTIFIER CROSSES AS A DETACHED POINTER VALUE IN EVERY MODE (kb/Work PB239). §14.9.4.3
        // SR4 makes it a SENDING operand even BY REFERENCE and SR5 withholds the receiving role, so the carrier is
        // a cell holding "the unique data item of class pointer" §8.4.3.11.4 GR1 / §8.4.3.13.4 GR1 create — the
        // callee's pointer formal adopts it through the Managed crossing like any pointer argument, and a store
        // into that formal reaches the cell, never the program's storage (Annex D.6.5.6.4: "it will never be
        // updated even when passed by reference"). The mode still rides the wire for the callee's adapters.
        if (a.DataAddress is { } da)
            return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<ManagedPointer>.Cell({ptr.AddressOfText(da)}), null)";
        if (a.ProgramAddress is { } pa)
            return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<ProgramPointer>.Cell("
                + $"{ptr.ProgramAddressText(pa, callArgument: true)}), null)";
        if (a.Place is { } p)
        {
            // THE CARRIED DESCRIPTION (kb/Work PB873): the argument's WHOLE numeric profile — sign, sign
            // position and byte form, not just (digits, scale) — because a formal that sees this storage as
            // characters sees its representation (§14.2.3 GR8 / GR9's first branch). Only an elementary NUMERIC
            // item has one (Place.DenotedItem — a reference-modified view denotes no item and is character storage), and a USAGE INDEX item's storage
            // description has no digit positions for a profile to state.
            // ⛔ …and a GROUP's §8.5.1.12 layout (kb/Work PB965) rides beside it: the one description a
            // variable-length group formal needs to meet a fixed-length group argument (§14.8.2.2).
            // ⛔ …and, at a site that checks EC-PROGRAM-ARG-MISMATCH (`describe`), its storage LENGTH and CLASS (kb/Work
            // PB165): the facts §14.8.2.3.2 / §14.8.2.3.3 rule 1 and §14.8.2.2 compare with the activated unit's registered
            // formal at a dynamic CALL. A forwarded formal states its OWN description, which is the same place.
            string meta = PlaceDescription(p) + (describe && a.Formal is null ? ArgumentFacts(p) : "");
            // ⛔ V59 RESIDUE FIX: the predicate is IsImageCapable, not the pre-V59 IsCharacterImage. A group whose
            // only non-character leaf is BINARY/PACKED now HAS a whole-group image — V59 gave those leaves their
            // pinned bytes — and `RecordStructEmitter` emits AsImage()/FromImage() for exactly IsImageCapable
            // items. Guarding on the stricter predicate therefore loud-staged a CALL whose codec had actually been
            // generated: `01 G. 05 N PIC S9(4) COMP. 05 A PIC X(3).` answered BYTE-LENGTH(G) = 5 and then threw
            // "no whole-group character image" on `CALL "SUB" USING G`. That claim was false, and refusing the
            // CALL rejected conforming source — §14.2.3 GR8 (`cite.py`-verified): "If the argument is passed by
            // reference, the activated runtime element operates as if the formal parameter occupies the same
            // storage area as the argument", which WiseOwl COBOL realizes through the very image round-trip that
            // exists. Only a variable-length group or a group with a pointer/object-class leaf is still
            // genuinely imageless and stays loud (every NUMERIC leaf kind joined the image across kb/Work
            // PB164 waves 1–2 + the R40 INDEX pin) — the wording matches the predicate actually tested.
            // ⛔ THE PREDICATE IS BoundaryImageCapable, NOT IsImageCapable (kb/Work PB204). §14.8.2.2 admits a
            // VARIABLE-LENGTH group across a Format-2 boundary "subject to compatibility as described in
            // 8.5.1.12" — an admission, checked at bind by OoConformance.DescriptionMismatch — so staging it
            // loud here refused conforming source. Only a group with NO boundary image at all (a
            // pointer/object-class leaf, or a variable-length shape outside the current-extent gate) is loud.
            // Asked of the OPERAND (Place.BoundaryImageCapable — a subscripted dynamic-table element has an image
            // although its entry does not, kb/Work PB189).
            if (p.Item.IsGroup && !p.BoundaryImageCapable && p is not RedefViewPlace)
                return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<string>.Cell("
                    + LoudValue("string", TierCIsland.Reason(p.Item, "CALL USING group"))
                    + "), null)";
            // ⛔ FORWARDING A FORMAL PARAMETER AS AN ARGUMENT — ISO §8.8.4.8.4 GR1c and §14.9.4.4 GR12, for
            // EVERY passing mode and EVERY residency (kb/Work PB165; PB133 wave C landed only the
            // BY REFERENCE + carrier-resident corner). GR1c: the omitted-argument condition is true "if the
            // argument corresponding to data-name-1 is itself a formal parameter for which the omitted-argument
            // condition is true" — so omission is TRANSITIVE through any number of forwardings. GR12 exempts
            // exactly this reference form ("except as an argument"), so the forward must not read the formal
            // either. `WholeFormal` recognizes the case STRUCTURALLY, so a SUBITEM or subscripted reference
            // keeps the ordinary build below and still raises inside an omitted formal, as GR12 requires.
            var fwd = WholeFormal(p);
            var probe = callState.WholeFormalProbe(p);
            if (a.Mode == CobolPassMode.Reference)
            {
                // A CARRIER-RESIDENT formal's carrier IS the caller's storage (§14.2.3 GR8), so passing it
                // through is both the presence fact and the aliasing — and its argument's AREA rides along, so an area
                // formal of the next activation is laid over the same positions (kb/Work PB2089).
                if (fwd is { CarrierResident: true } rf)
                    return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Reference)}, "
                        + $"{rf.CarrierField}, {meta}{AreaArgument(p)})";
                // A NON-resident formal (an AREA formal — a group, a REDEFINED or an addressed one — or a
                // variable-length group's image) is passed on as a fresh view over its storage, and only the
                // PRESENCE has to be taken from the incoming carrier. Rebuilding it unconditionally is what made an
                // omitted group formal arrive at the next callee as PRESENT (measured: `CALL "S8" AS NESTED USING
                // OMITTED` → the inner `LH IS OMITTED` test answered false). An area formal's AREA is its carrier
                // field itself — the argument's own cell area it was laid over (kb/Work PB2087) — read WITHOUT
                // dereferencing it, because GR12 exempts this reference form even when the formal is omitted.
                // A method formal forwarded to a CALL passes the area it occupies the same way (kb/Work PB2087).
                return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Reference)}, "
                    + $"{Forwarded(probe, RefCarrier(p))}, {meta}{AreaArgument(p)})";
            }
            // BY CONTENT — "a record … allocated by the activating element" (§14.2.3 GR9) — and BY VALUE with
            // an identifier argument (a UDF BY VALUE formal, §8.4.3.2.4 GR5c): both are value snapshots at
            // call initiation; the mode rides the wire so the arg is honest about which rule produced it
            // (the BY VALUE callee re-conforms through its own NumValue cell, GR10).
            // ⛔ The snapshot READS the operand, so when the operand is a forwarded formal the read has to be
            // guarded: a reference to an omitted formal raises its *-ARG-OMITTED (kb/Work PB971), and GR12 says this
            // reference form does not. The guard also carries the omission on, per GR1c.
            string snapshot = CrossingOf(p) switch
            {
                CallCrossing.VarGroup => RuntimeApi.VarGroupCell(PlaceRenderer.VarGroupBoundaryImage(p, "CALL argument")),
                CallCrossing.Text => $"ManagedPointer<string>.Cell({CallContentRead(p)})",
                // Native and Managed both snapshot the storage's own value into a detached cell of its own
                // carrier — §14.2.3 GR9/GR10's allocated record, whose filling is "a COMPUTE statement without
                // the ROUNDED phrase" for a numeric formal and "a SET statement" for one of class object or
                // pointer. A SET between two items of the same category IS this copy (kb/Work PB663).
                _ => $"ManagedPointer<{CallCellCarrier(p)}>.Cell({PlaceRenderer.Read(p)})",
            };
            return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, {Forwarded(probe, snapshot)}, {meta}{ContentRecordArgument(a, p)})";
        }
        switch (a.Value)
        {
            case BoundStringLiteral s:
                return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<string>.Cell({CsLiteral(s.Value)}), null)";
            // ⛔ ONE NUMERIC-ARGUMENT FUNNEL for both non-place arms (kb/Work PB263 + PB264). A numeric literal
            // reaches this switch in EITHER bound shape — BY CONTENT and a bare Format-2 argument bind it as
            // BoundNumericLiteral, while BY VALUE binds it as a BoundComputedOperand wrapping a BoundNumLiteral
            // (CallBinder's byValue arm goes through BindByValueExpr) — and the two arms used to derive a
            // carrier and a scale EACH. They disagreed, so ONE rule ("a numeric literal argument crosses with
            // its exact value") produced three different wrong answers depending on how it was spelled.
            // ⛔ …EXCEPT INTO A KNOWN ELEMENTARY FORMAL OF ANOTHER CATEGORY (kb/Work PB1113). §14.2.3 GR9 fills the
            // formal's allocated record by "a MOVE statement" when the formal is not numeric, so the literal crosses
            // as that record — the receiving category's ONE MOVE store, the INVOKE lane's twin — and not as a numeric
            // cell the callee's adapter would read as digits: `12.5` into PIC ZZ9.99 printed "125   " and `-5` into
            // PIC X(4) the sign-overpunched "N   " (measured), where the MOVE edits and drops the sign (§14.9.25.4 GR6).
            // A GROUP formal is §14.9.25.4 GR4's non-elementary move — "as if it were an alphanumeric to
            // alphanumeric elementary move … no conversion": the literal's own characters, as the written
            // `MOVE -12 TO G` and the INVOKE lane store them (the numeric cell crossed as the overpunched "1K").
            case BoundNumericLiteral n when a.Formal is { IsGroup: true }:
                return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<string>.Cell({CsLiteral(n.Text)}), null)";
            case BoundNumericLiteral n when a.Formal is { Pic: { } fp }
                                            && fp.Category is not PicCategory.Numeric && SlotWindow.CarriedBySlot(a.Formal) is false:
                return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<string>.Cell({move.ConvertSource(n, a.Formal)}), null)";
            case BoundNumericLiteral n:
                return NumericArgText(a.Mode, n.Text, ctx.SignEncoding);
            case BoundComputedOperand ce when Gr8ArgumentLiteral.NumericText(ce.Expr) is { } ct:
                return NumericArgText(a.Mode, ct, ctx.SignEncoding);
            // ⛔ A CHARACTER-VALUED INTRINSIC FUNCTION-IDENTIFIER CROSSES ON THE STRING CHANNEL (kb/Work PB1418).
            // `FUNCTION F(FUNCTION UPPER-CASE(X))` binds its argument as a computed operand over an alphanumeric
            // BoundIntrinsicCall, and the arithmetic arm below rendered it as a NUMBER — the callee's PIC X(4)
            // formal received "0000" for "ABCD" (measured). §15.4 puts the returned value in "a temporary
            // elementary data item" of the function's category, so it crosses exactly as a nonnumeric literal of
            // that category does: its character image, through the ONE string channel (OperandText.AsString).
            case BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Alphanumeric or PicCategory.National or PicCategory.Boolean } } sc:
                return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<string>.Cell({OperandText.AsString(sc, num)}), null)";
            // ⛔ AN ARITHMETIC EXPRESSION INTO A KNOWN ELEMENTARY FORMAL OF ANOTHER CATEGORY (kb/Work PB1946, verdict
            // PB1936) crosses as that formal's allocated record, exactly as the numeric literal above does: §14.2.3 GR9
            // fills it by "a MOVE statement" when the formal is not numeric, and §14.8.2.3.3 rule 2 d) takes the
            // expression's VALUE as the MOVE's sending operand. The receiving category's ONE MOVE store edits it into a
            // numeric-edited mask; a numeric cell would be read by the callee's adapter as digits ("12.50" as "125").
            case BoundComputedOperand moved when a.Formal is { IsGroup: false, Pic: { } mp }
                                                 && mp.Category is not PicCategory.Numeric
                                                 && SlotWindow.CarriedBySlot(a.Formal) is false:
                return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<string>.Cell({move.ConvertSource(moved, a.Formal)}), null)";
            case BoundComputedOperand expr:
            {
                // A GENUINE runtime expression snapshots its computed value (§14.2.3 GR9/GR10 — the CALL BY
                // VALUE grammar leg binds Mode=Value; a UDF expression argument to a BY REFERENCE formal binds
                // Mode=Content per §8.4.3.2.4 GR5b — the mode is bound, not assumed here).
                // An unsigned-wide result (a HIGHEST-ALGEBRAIC fold literal — kb/Work R10) funnels through the
                // same DeU rule as every arithmetic consumer: loud beyond the Int128 intermediate, never a wrap.
                // An SDIDI intermediate (a STANDARD-DECIMAL expression; a native integer power — kb/Work PB69) lands
                // through the ONE landing at the receiver-less working scale (kb/Work PB84 — `(long)(CobolDec)` was
                // a Roslyn error on `CALL … BY VALUE A ** 2`).
                NumX x = num.Landed(NumericRenderer.DeU(num.Render(expr.Expr, ReceiverContext.None)), ReceiverContext.None);
                // ⛔ THE FLOAT LANE CROSSES AS A FLOAT (kb/Work PB238). `Landed` documents its own contract:
                // "A float under NATIVE arithmetic stays binary64 — the consumer's own float arm applies", and
                // this consumer had none, so `(Int128)(…)` TRUNCATED the fraction away: `01 F FLOAT-LONG
                // VALUE 1.5` reached a `PIC S9(3)V99` BY VALUE formal as 001.00 (measured), where §14.2.3 GR10
                // makes the crossing "a COMPUTE statement without the ROUNDED phrase" ⇒ 001.50. The FIX IS THE
                // LANE'S OWN CARRIER, not a pre-rounding at some working scale the receiver never chose — the
                // same answer PB264 gave the 19+-digit case (widen to Int128 rather than check the narrowing)
                // and PB201 gave the position operand ("a position operand's CARRIER, not its class"). No
                // Digits/Scale meta rides with it: a binary floating-point item has neither, and
                // CobolArgAdapt's float arm reads the value itself.
                if (x.Real)
                    return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<double>.Cell((double)({x.Expr})), null)";
                // ⛔ THE CELL IS Int128, NOT long, AND THE CONVERSION IS WIDENING (kb/Work PB264). This used to
                // be `ManagedPointer<long>.Cell((long)(x.Expr))` — an UNCHECKED narrowing of a value that the
                // DeU/Landed funnel above delivers on the Int128 lane, so an argument beyond 18 digits crossed
                // as its MODULAR LOW-ORDER BITS: a silent wrong value, in the one direction the callee cannot
                // detect. Widening to the lane's own carrier removes the narrowing rather than checking it —
                // there is no value on the Int128 lane that an Int128 cell cannot hold — and every carrier the
                // ABI accepts is read back through CobolArgAdapt's ReadNumericCell (kb/Work R12).
                return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, ManagedPointer<Int128>.Cell((Int128)({x.Expr})), {ValueMeta(ctx.SignEncoding, 38, x.Scale, signed: true)})";
            }
            // ⛔ NULL IS AN IDENTIFIER, NOT A FILL (kb/Work PB1630). §8.4.3.1.2 Format 8 (predefined-address) and
            // §8.4.3.10.3 SR1 a) admit it "as an argument in a program-prototype format CALL statement, a
            // function-prototype format function activation", and §14.8.2.3.3 hands it to a pointer / object-reference
            // formal by a SET. This arm used to fall into the figurative fill below and cross as a one-character
            // string, which every slot adapter refused at run time (EC-PROGRAM-ARG-MISMATCH on legal source). It
            // crosses in its OWN mode — BY CONTENT or BY VALUE (§14.9.4.3 SR22 admits class pointer and object) — as
            // the storage-free NULL carrier whose value the formal's own slot adapter supplies, so the crossing is
            // the same whether or not this activating element knows the formal (§12.3.8.4 GR10 c)).
            case BoundPredefinedNull:
                return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, {RuntimeApi.PredefinedNullArgumentCarrier}, null)";
            // ⛔ A FIGURATIVE CONSTANT / ALL LITERAL FILLS THE FORMAL'S ALLOCATED RECORD (kb/Work PB1418 + PB1617) —
            // through the ONE fill the INVOKE lane shares (FigurativeArgumentImage).
            case BoundAllLiteral or BoundFigurative:
                return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Content)}, ManagedPointer<string>.Cell({FigurativeArgumentImage(a.Value, a.Formal, ctx.Data)}), null)";
            default:
                return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Content)}, ManagedPointer<string>.Cell("
                    + LoudValue("string", "CALL USING argument form") + "), null)";
        }
    }

    /// <summary>⛔ THE ONE VALUE OF A FIGURATIVE-CONSTANT / ALL-LITERAL ARGUMENT (kb/Work PB1418 + PB1617) — the C#
    /// string expression every activation that knows its formal crosses: the Format-2 CALL and the user-defined
    /// function (<see cref="ArgText"/>) and the INVOKE (<c>OoEmitter</c>'s content-fill arm).
    /// <para>The rule is §14.2.3 GR9's second branch: for "a program and the NESTED phrase", a prototyped program, "a
    /// method" or "a function", the allocated record is "a data item with the same description and the same number
    /// of bytes as the formal parameter, where the maximum length is used if the formal parameter is described as a
    /// variable-occurrence data item", and the argument is moved into it by "a MOVE statement" unless the formal is
    /// numeric (then "a COMPUTE statement"). A figurative constant moved to "a fixed-length data item" is "repeated
    /// character by character" to that item's character positions and truncated from the right (§8.3.3.6.4 GR2) —
    /// so the value that crosses is the fill at the RECORD's width (<see cref="FigurativeFillWidth"/>), in the
    /// formal's own category for HIGH-/LOW-VALUE (a national formal reads the national sequence, §8.3.3.6.4 GR1).
    /// Where no fixed width exists — no formal known (GR9's FIRST branch, whose record is "of the same length as the
    /// argument"), a numeric formal (the COMPUTE reads the value), an ANY LENGTH formal (whose record takes "the
    /// same … length as the argument") or a dynamic-length one (not a fixed-length item) — the figurative's length
    /// is the one §8.3.3.6.4 GR3 b)/c) gives it: one character, or one occurrence of literal-1.</para>
    /// <para>Before PB1617 the width was stated for an ELEMENTARY formal only, so a GROUP formal received one
    /// occurrence that the callee space-padded (measured: <c>CALL … AS NESTED USING BY CONTENT ALL "*"</c> into
    /// <c>01 G. 05 A PIC X(2). 05 B PIC X(2).</c> displayed <c>*   </c>), and the INVOKE lane had no arm at all.</para></summary>
    internal static string FigurativeArgumentImage(BoundOperand fill, DataItem? formal, DataBinder data)
    {
        int? width = FigurativeFillWidth(formal);
        return fill switch
        {
            // §8.3.3.6.4 GR2's repetition is the ONE runtime rule EmitText.RepeatToWidth folds (kb/Work PB297).
            BoundAllLiteral all => CsLiteral(width is { } w ? RepeatToWidth(all.Literal, w) : all.Literal),
            BoundFigurative fig =>
                $"new string({FigurativeConstants.Fill(fig.Kind, data.Collating, formal?.OperandPic?.Category, data.NationalCollating)}, {width ?? 1})",
            _ => throw new ArgumentException(
                $"{fill.GetType().Name} is not a figurative constant or ALL literal", nameof(fill)),
        };
    }

    /// <summary>The character positions of §14.2.3 GR9's allocated record for a figurative-constant / ALL-literal
    /// argument — the width <see cref="FigurativeArgumentImage"/> fills to — or null when the record has no fixed
    /// character width to fill (see there). It is the formal's TEXT-crossing window
    /// (<see cref="BoundaryImageWidth"/>, the width the callee's carrier and the INVOKE argument window use), so the
    /// fill and the window cannot disagree: an alphanumeric group's record image width, a bit / national group's
    /// as-if position count (§14.8.2.1 NOTE: "A bit group or national group is treated as an elementary item"), a
    /// non-numeric elementary item's character positions.
    /// <para>An OCCURS DEPENDING group needs no run-time extent: GR9 allocates the record at the MAXIMUM length, which
    /// is the record image width. A VARIABLE-LENGTH group (§8.5.1.12) has no fixed record at all — and needs none,
    /// because §8.5.1.12.1 bars any move into it from an operand that is not a compatible group, which
    /// <c>ParameterConformance</c> reports at bind — so it answers null.</para></summary>
    private static int? FigurativeFillWidth(DataItem? formal) =>
        formal is null || formal.IsAnyLength || formal.IsDynamicLength ? null
        : formal.IsElementary
            ? formal.Pic is { Category: not PicCategory.Numeric } && formal.ImageWidth > 0 ? formal.ImageWidth : null
        : VariableLengthCompatibility.IsVariableLength(formal) ? null
        : BoundaryImageWidth(formal);

    /// <summary>The PROCEDURE DIVISION USING formal this argument place denotes AS A WHOLE, or null
    /// (ISO §8.8.4.8.4 GR1c / §14.9.4.4 GR12 — kb/Work PB165).
    /// <para>⛔ THE TEST IS IDENTITY AGAINST THE UNIT'S FORMAL LIST, not a <c>__lnk</c> prefix match on the
    /// emitted field name. The name match could only ever see a CARRIER-RESIDENT formal — <c>DataBinder</c>
    /// rewrites just those to <c>__lnk{Uid}.Value</c> — so a GROUP formal, whose carrier is a copy-in field with
    /// an ordinary name, fell through and lost its omitted state on every forward. Identity sees both, and it
    /// keeps seeing both when a future residency rule changes.</para>
    /// <para>Identity against the formal's own <c>DataItem</c> is itself the whole-item test: a SUBITEM
    /// resolves to the subordinate item, never to the level-01 root, and a level-01 entry cannot carry OCCURS,
    /// so no subscripted place has a formal root as its item. A REFERENCE-MODIFIED view is excluded
    /// explicitly — §8.8.4.8.4 GR1c speaks of an argument that "is itself a formal parameter", and a window
    /// into one is not; referencing INSIDE an omitted formal is exactly the error GR12 states.</para></summary>
    private LinkageFormal? WholeFormal(Place p) =>
        p is RefModPlace ? null : callState.Formals.FirstOrDefault(f => ReferenceEquals(f.Item, p.Item));

    /// <summary>⛔ THE ONE RENDERING OF THE PRESENCE FACT (§8.8.4.8.4 GR1; kb/Work PB757): a C# boolean that is
    /// true when the formal's argument was omitted. The program arm tests its null carrier; the method arm reads
    /// its presence parameter. The §8.8.4.8 condition, the CALL forward and the INVOKE forward all render it
    /// here, so no consumer spells either arm itself.</summary>
    public static string OmittedTest(OmittedProbe probe) => probe switch
    {
        OmittedProbe.Carrier c => $"{c.CarrierField}.IsNull",
        OmittedProbe.MethodFlag m => m.FlagParam,
        _ => throw new InvalidOperationException($"unmodeled omitted-argument probe {probe}"),
    };

    /// <summary>Guard a freshly built argument carrier with the FORWARDED formal's presence (ISO §8.8.4.8.4
    /// GR1c — omission is transitive; §14.9.4.4 GR12 — a reference "as an argument" is exempt, so the built
    /// carrier's read must not happen at all when the formal is omitted). <paramref name="built"/> is returned
    /// unguarded when the argument is not a formal forward.
    /// <para>The guard is a C# conditional, not a runtime helper taking a factory: the omitted case allocates
    /// nothing and the present case allocates exactly what it allocated before — a <c>Func&lt;T&gt;</c> closure
    /// per argument per CALL would be a new allocation on the hot path for a rule that needs none.</para></summary>
    private static string Forwarded(OmittedProbe? formal, string built) =>
        formal is null ? built : $"({OmittedTest(formal)} ? ManagedPointer.Null : {built})";

    // The COMPILE-TIME numeric-literal text of an argument expression is `Gr8ArgumentLiteral.NumericText`
    // (Binding/Bound/BoundCall.cs). ⛔ It used to be a PRIVATE COPY here, and the binder's §14.8.2.3.3
    // conformance screen needed the same answer (kb/Work PB165) — the emitted carrier and the conformance
    // verdict must agree about what §14.9.4.4 GR8 says an argument IS, so the reduction has exactly one home.
    // Its negate arm is why: at a LITERAL position the sign is part of the token, but inside an ARITHMETIC
    // EXPRESSION — which is what a BY VALUE argument binds as — a leading '−' before the FLOATING-POINT form
    // is taken by `unaryExpression` first, so `BY VALUE -1.234E-5` arrives as BoundNegate(BoundNumLiteral)
    // while `BY VALUE -0.00001234` arrives bare. Matching only the bare shape truncated the signed spelling
    // at the receiver-less working scale (−0.00001234 crossed as −0.000012 — measured).

    /// <summary>⛔ THE ONE numeric-LITERAL argument carrier build, for every notation and every pass mode
    /// (kb/Work PB263 + PB264). A numeric literal argument crosses as the <c>(unscaled value, scale)</c> pair
    /// that <see cref="EmitText.TryUnscaledParts"/> derives EXACTLY from the literal — its §8.3.3.3.2 rule-4
    /// value for the fixed-point form, its §8.3.3.3.3 rule-5 value ("the algebraic product of the value of its
    /// significand and the quantity derived by raising ten to the power of the exponent") for the
    /// floating-point form — so the two notations of one value cross identically, and the callee's own
    /// conformance (<c>CobolArgAdapt.Num</c> for §14.2.3 GR9, <c>NumValue</c> for GR10's "COMPUTE statement
    /// without the ROUNDED phrase") receives the value the program actually wrote.
    /// <para>THE CARRIER AND THE DIGIT META COME FROM THE RENDERED VALUE, never from the source text. The digit
    /// count used to be <c>Text.Count(char.IsAsciiDigit)</c>, which counts a floating-point literal's EXPONENT
    /// digits as significand digits, and the cell type was re-derived from that miscount — so
    /// <c>BY CONTENT 1.5E+3</c> asked for a <c>long</c> cell and got a <c>double</c> expression, a raw Roslyn
    /// CS1503 on conforming source with no COBOL diagnostic at all (PB263). <c>IntLiteralCore</c> now decides
    /// the rendering and its carrier together, so they cannot disagree.</para></summary>
    private static string NumericArgText(CobolPassMode mode, string literalText, SignEncoding signEncoding)
    {
        // ONE decomposition, then ONE carrier decision over it. ⛔ Deliberately NOT `UnscaledLit` here: that
        // would decompose the literal a SECOND time and take only half of the result, leaving the rendered
        // expression and the cell type derived independently again — which is the precise shape of the defect
        // this method exists to remove.
        if (!TryUnscaledParts(literalText, out string unscaled, out int scale))
            // Not a canonical numeric literal — the binder has already diagnosed it (COBOLNET1661 for an
            // out-of-range exponent, the §8.3.3.3.3 SR2/SR3 form checks otherwise). Stage loud rather than
            // emit a cell whose type cannot be derived; never a silent value.
            return $"new CobolArg({RuntimeApi.PassModeText(mode)}, ManagedPointer<string>.Cell("
                + LoudValue("string", $"CALL USING numeric literal '{literalText}'") + "), null)";
        var (cell, _, carrier) = IntLiteralCore(unscaled);
        int digits = unscaled.Count(char.IsAsciiDigit);
        return $"new CobolArg({RuntimeApi.PassModeText(mode)}, "
            + $"ManagedPointer<{carrier}>.Cell({cell}), {ValueMeta(signEncoding, digits, scale, signed: unscaled.Contains('-'))})";
    }

    /// <summary>The carried description of a numeric argument with NO data item behind it — a literal or a
    /// computed expression (kb/Work PB873): a DISPLAY item of the value's own digit count and scale, signed when
    /// the value can be negative. §14.2.3 GR9's first branch allocates such an argument's record "of the same
    /// length as the argument" and moves it "without conversion", so its image is that DISPLAY form; the pair
    /// this replaced (digits, scale) spelled only the unsigned digit run and so could not carry a negative
    /// literal's sign to a formal that reads the argument as characters.</summary>
    private static string ValueMeta(SignEncoding signEncoding, int digits, int scale, bool signed) =>
        $"new NumProfile {{ Digits = {digits}, FractionDigits = {scale}, Signed = {(signed ? "true" : "false")}, "
        + "SignKind = NumericSign.TrailingOverpunch, Truncation = NumericTruncation.DigitCount, "
        + "ByteForm = NumericByteForm.Zoned"
        + $"{(signEncoding is SignEncoding.Ibm ? "" : $", SignEncoding = SignEncoding.{signEncoding}")} }}";

    /// <summary>An accessor carrier over a caller place — the BY REFERENCE / RETURNING aliasing form (design D1:
    /// <c>OverField</c> over the native field; a whole group crosses as its character image, distributed back
    /// through <c>FromImage</c> — the deep-dive group round-trip). One arm per <see cref="CallCrossing"/>, so
    /// the ACTIVATING element's carrier and the ACTIVATED element's formal are built from the ONE
    /// classification and cannot drift apart (kb/Work PB663).</summary>
    public string RefCarrier(Place p) => CrossingOf(p) switch
    {
        // §14.8.2.2's variable-length sentence, realized (kb/Work PB204): the carrier is the group's
        // current-extent components, aliased through the SAME OverField shape every other form uses.
        CallCrossing.VarGroup => RuntimeApi.VarGroupOverField(
            PlaceRenderer.VarGroupBoundaryImage(p, "CALL argument"),
            PlaceRenderer.WriteVarGroupImage(p, "__v", "CALL boundary copy into")),
        CallCrossing.Text =>
            $"ManagedPointer<string>.OverField(() => {CallStringRead(p)}, __v => {{ {CallStringWrite(p, "__v")} }})",
        // Native AND Managed share one rendering — the item's own carrier over its own field — because both
        // ARE their storage rather than an image of it (§14.2.3 GR8's "same storage area"). They are two arms
        // rather than one so the callee's matching arms have something to be matched against.
        _ => $"ManagedPointer<{CallCellCarrier(p)}>.OverField(() => {PlaceRenderer.Read(p)}, __v => {{ {PlaceRenderer.Write(p, "__v")} }})",
    };

    /// <summary>True when a place's storage crosses the CALL boundary as a character image (string carrier):
    /// groups, Tier-B windows, zoned-image leaves, alphanumeric / numeric-edited items. EVERY native
    /// fixed-point leaf crosses as its own CARRIER (<c>long</c> / <c>ulong</c> / <c>Int128</c> /
    /// <c>UInt128</c> — kb/Work R12): the former <c>Digits &gt; 18</c> leg routed the wide tiers onto a string
    /// crossing whose write half was NEVER implemented (the generated C# assigned a string to the native field
    /// and did not compile) and whose read half was the picture-digit image (lossy for a BinaryCapacity item's
    /// beyond-picture container values), while the CALLEE side built a <c>ManagedPointer&lt;long&gt;</c> cell
    /// its own carrier-typed reads could not use. One predicate, both sides, native and value-exact.
    /// <para>⛔ AND THE FLOAT LEAF JOINED THEM (kb/Work PB238). R12's argument was about the four FIXED-POINT
    /// carriers and left <c>p.Item.Pic is { IsFloat: true }</c> on the character route, where a binary64 value
    /// decoded through the RECEIVER's zoned profile (<c>CobolArgAdapt.NumValue</c>'s string arm calls
    /// <c>CobolNum.ParseDisplay</c>) — a numeric item read as digit characters it was never written as. The
    /// float lane's own carrier is <c>double</c>/<c>float</c>, <c>CallCellCarrier</c> already answers with it
    /// (<c>DataItem.ElementType</c>), and <c>CobolArgAdapt.ReadRealCell</c> is the callee half — so the same
    /// R12 sentence now covers all six carriers rather than four. A float reaching a CHARACTER formal has no
    /// arm on either side and takes the OMITTED (loud) carrier: §14.8.2.3.2 requires the same category and
    /// usage for a BY REFERENCE pairing and §14.8.2.3.3's MOVE rules give a float sender no alphanumeric
    /// receiver, so that pairing is a conformance violation to report, never a crossing to invent.</para>
    /// </summary>
    internal static bool CallPlaceIsString(Place p) =>
        p is RedefViewPlace || p.Item.IsGroup || p.Item.StoreAsImage
        || p.Item.Pic?.Category is PicCategory.Alphanumeric or PicCategory.NumericEdited
            or PicCategory.National or PicCategory.Boolean;   // string-stored (D-N1/D-B1): both ABI sides are C# strings, char-correct

    /// <summary>True when a place crosses the activation boundary as the §8.5.1.12 VARIABLE-LENGTH carrier
    /// (kb/Work PB204) — the THIRD crossing form beside the native cell and the flat character image. A
    /// variable-length group has no fixed record window, so <see cref="CallStringRead"/>'s flat image is not
    /// invertible for it; <c>CobolVarGroup</c> carries the fixed run and the ordered variable-length components
    /// instead. Deliberately NARROWER than "is a variable-length group": it also demands
    /// <see cref="DataItem.CurrentExtentImageCapable"/>, so a shape outside that gate (an OCCURS DEPENDING
    /// member, an in-element runtime length) still takes the ordinary arms and still stages the documented
    /// Tier-C loud — a residue keeps its loud rather than acquiring a half-built crossing.
    /// <para>⛔ <see cref="CallPlaceIsString"/> deliberately still answers TRUE for such a place. Its consumers
    /// that this mechanism did NOT convert (ReportWriterEmitter's CONTROL restore) therefore keep routing
    /// through <see cref="CallStringRead"/>, whose group arm stages the Tier-C loud — the SAFE fallback. Making
    /// it answer false would have sent those sites down the NATIVE arm instead, which is the wrong answer
    /// rather than a loud one.</para></summary>
    internal static bool CallPlaceIsVarGroup(Place p) =>
        // CurrentExtentImageCapable ALREADY implies both `IsGroup` and `!IsImageCapable` — a variable-length
        // group has a dynamic child whose own IsImageCapable is false, so the group's is too. The conjuncts
        // are left out rather than restated: a redundant conjunct is a claim that can rot.
        // A cell-backed variable-length group view (kb/Work PB1026) IS such a group: its carrier is the cell's.
        (p is not RedefViewPlace and not RefModPlace || p is RedefViewPlace { Coding: VarGroupWindow })
        && p.Item.CurrentExtentImageCapable;

    /// <summary>True when a place crosses the activation boundary as a MANAGED SLOT — the FOURTH crossing form
    /// (kb/Work PB663). It is exactly <c>SlotWindow.CarriedBySlot</c>, the ONE test the data model already uses
    /// for "does this item's value ride the area's managed slots rather than its bytes?" (kb/Work PB231), so
    /// the boundary and the storage cannot disagree about the population: class pointer (data / program /
    /// function) and class object-reference.
    /// <para>⛔ The test is asked of the place's CODING, never of its shape (kb/Work PB1632). A cell-backed area —
    /// BASED, EXTERNAL, or one whose ADDRESS OF is taken — renders EVERY member as a <see cref="RedefViewPlace"/>,
    /// and a managed member's view carries the <see cref="SlotWindow"/> coding: that view IS the item's storage,
    /// not a redefinition of it. Excluding every RedefViewPlace sent <c>SET WP TO ADDRESS OF WP</c> +
    /// <c>CALL … BY CONTENT WP</c> to the character arm, where the COMPILER threw for want of a pointer's image,
    /// although §14.8.2.3.3 2) makes a pointer argument conform "as if a SET statement were performed". The
    /// same rule <see cref="CallPlaceIsVarGroup"/> applies to its own cell coding.</para>
    /// <para>Any OTHER <see cref="RedefViewPlace"/> or a <see cref="RefModPlace"/> stays excluded, and the exclusion
    /// is stated rather than relied on: §13.18.44.3 SR12 ("The REDEFINES clause shall not be specified for a data
    /// item of class object, message-tag, or pointer …") and SR14 (the same list for data-name-2) bar both ends
    /// of a redefinition, and §8.4.3.3.3 SR1's list admits reference modification only for character-class and
    /// display/national numeric operands — so neither shape can stand over such an item on conforming source,
    /// and a nonconforming one keeps the character arm's existing loud rather than acquiring a slot it has no
    /// storage for.</para></summary>
    internal static bool CallPlaceIsManaged(Place p) =>
        (p is not RedefViewPlace and not RefModPlace || p is RedefViewPlace { Coding: SlotWindow })
        && SlotWindow.CarriedBySlot(p.Item);

    /// <summary>⛔ THE ONE classification of a place's crossing form, in priority order, for BOTH sides of the
    /// boundary (kb/Work PB663). Managed first — a pointer item is neither a group nor a character shape, so
    /// the order only makes the intent legible; VarGroup before Text because <see cref="CallPlaceIsString"/>
    /// deliberately still answers true for a variable-length group (see its own remark).</summary>
    internal static CallCrossing CrossingOf(Place p) =>
        CallPlaceIsManaged(p) ? CallCrossing.Managed
        : CallPlaceIsVarGroup(p) ? CallCrossing.VarGroup
        : CallPlaceIsString(p) ? CallCrossing.Text
        : CallCrossing.Native;

    /// <summary>The C# carrier type of a place that crosses in its OWN storage type rather than as an image —
    /// <see cref="CallCrossing.Native"/> and <see cref="CallCrossing.Managed"/> alike. It is the item's own
    /// <c>ElementType</c> (kb/Work R12: the cell type IS the field type, so the aliasing lambdas and the
    /// callee's carrier-typed reads compile and carry the full container range by construction), which for a
    /// managed item is its <c>PicInfo.ClrType</c> — <c>ManagedPointer</c>, <c>ProgramPointer</c>,
    /// <c>FunctionPointer</c> or the object reference's own class type. §14.8.2.3.2 forces the two sides to the
    /// same category and the same object class, so the same <c>T</c> arrives on both ends by construction.</summary>
    internal static string CallCellCarrier(Place p) => p.Item.ElementType;

    /// <summary>The string image a place contributes ACROSS THE CALL BOUNDARY. An occurs-depending group reads
    /// its FULL maximum-allocation image here, never the ODO window: BY REFERENCE "operates as if the [formal]
    /// occupies the same storage area as the argument" (ISO §14.2.3 GR8 — the STORAGE is the maximum allocation)
    /// and a BY CONTENT copy is of the whole record (GR9); the current-extent window of §13.18.38 GR8 is a
    /// SENDING-OPERAND rule for MOVE/compare/INSPECT, not a storage-aliasing rule (IC207A: CALL … USING TABLE-01
    /// with DN3=3 must still carry all 15 character positions in, and carry the callee's full table back out).
    /// Every call site of this helper (the BY REFERENCE carrier, BY CONTENT snapshot, callee copy-out, and
    /// RETURNING delivery) is such a boundary.
    /// <para>⛔ A GROUP CROSSES AS ITS STORAGE IMAGE, NOT AS ITS OPERAND VALUE (kb/Work PB173 — measured, and
    /// PRE-EXISTING at 876d8ab0: `01 G GROUP-USAGE BIT. 05 B1 PIC 1(4). 05 B2 PIC 1(4).` holding 11001010 and
    /// passed BY REFERENCE arrived in the callee as 00110001 and came home as 00110000). §14.2.3 GR8 makes the
    /// formal occupy "the same storage area as the argument", so the carrier is the group's character IMAGE —
    /// the exact inverse of the write half's <c>FromImage</c>. Routing through <c>OperandText.FieldImage</c>
    /// instead delivered a BIT group's OPERAND value (§13.18.29.4 GR1b's m boolean positions, <c>AsBits</c>)
    /// into a <c>FromImage</c> that reads ceil(m/8) PACKED characters — two alphabets, one carrier, silent
    /// argument corruption on legal source that §14.9.4.3 SR6 explicitly admits ("If the BY REFERENCE phrase is
    /// specified or implied for an identifier-2 that is a bit data item, identifier-2 shall be described such
    /// that it is aligned on a byte boundary …", which a level-01 bit group satisfies by construction).
    /// <c>PlaceRenderer.GroupImage</c> is THE ONE reader and already owns all four arms — the Tier-B window, the
    /// <c>OdoGroupPlace</c> unwrap to the FULL allocation, the capability guard and the struct image — so this
    /// is now the exact mirror of <see cref="CallStringWrite"/>'s group arm, arm for arm.</para></summary>
    internal static string CallStringRead(Place p) =>
        // ⛔ A REFERENCE-MODIFIED OPERAND IS AN ELEMENTARY ALPHANUMERIC ITEM OVER THE SLICE (§8.4.3.3.4 GR6),
        // whatever the inner item is — it must NOT take the group arm below. Its own substrate wrap
        // (GroupImagePlace / BitImagePlace / NumericImagePlace) is already inside the RefModPlace, so
        // `PlaceRenderer.Read` gives the slice and `PlaceRenderer.Write` splices it back: an exact pair. This
        // arm is stated FIRST and explicitly on BOTH halves because the alternative is invisible — widening the
        // read half's group test to `p.Item.IsGroup` (RefModPlace.Item forwards to the INNER item, so a
        // ref-modded group answers true) rendered `CobolStr.RefMod(G.AsImage(),1,3).AsImage()`, a backend
        // CS1061 on `string`, and the write half had been emitting the `.FromImage(` half of exactly that pair
        // since before this change (measured at 876d8ab0: `CALL "S" USING G(1:3)` = one CS1061; with the group
        // test widened, two).
        p is RefModPlace ? OperandText.FieldImage(p)
        // ⛔ A BIT / NATIONAL GROUP CROSSES AS THE ELEMENTARY ITEM IT IS TREATED AS (kb/Work PB1166). §14.8.2.1 /
        // §14.8.3.1 NOTE: "A bit group or national group is treated as an elementary item", and §14.8.2.3.2
        // "Additionally" b)/c) and §14.8.3.3 "Additionally" 2)/3) pair it with an elementary bit / national item of
        // the same position count — so both ends of every such crossing must speak the ELEMENTARY alphabet, the
        // m boolean or m national positions of its §13.18.29.4 GR1b/GR2b as-if picture. Its storage image speaks
        // another (ceil(m/8) packed characters; two bytes per national position), and while the comparator
        // refused every group ⇄ elementary pairing that difference was invisible; admitting the pairing on the
        // image made `CALL … RETURNING GN` deliver "ABCDE" as NUL-interleaved bytes. The FULL allocation, like
        // every boundary read here (§14.2.3 GR8), so an occurs-depending wrapper is unwrapped.
        : IsAsIfGroupCrossing(p) ? PlaceRenderer.SendingGroupValue(FullAllocation(p))
        : p.Item.IsGroup
            ? PlaceRenderer.GroupImage(p)   // the FULL image (GR8 is a sending-operand rule, not a boundary one) — window or struct (kb/Work PB80)
        // ⛔ AN IMAGE-CARRIED NUMERIC LEAF CROSSES AS ITS STORAGE BYTES (kb/Work PB970), for the same reason the
        // group arm above does: §14.2.3 GR8 — "the activated runtime element operates as if the formal parameter
        // occupies the same storage area as the argument" — and GR9's first branch moves the argument "without
        // conversion". The window already HOLDS those bytes (zoned digits, radix-2, BCD, IEEE), so the read is the
        // window itself. It used to be `OperandText.FieldImage` — the OPERAND (DISPLAY) text — while the other
        // half of the same channel, `CobolArgAdapt.Text`'s native-cell arm, delivers the STORAGE image (kb/Work
        // PB873): one carrier, two alphabets, so `PIC S9(5) COMP-3` −42 reached a redefined COMP-3 formal as
        // bytes 00 00 0C and came home as garbage. The predicate is exactly the one the write half keys on.
        : IsImageCarriedNumeric(p)
            ? PlaceRenderer.Read(p)
            : OperandText.FieldImage(p);

    /// <summary>True when an ELEMENTARY numeric place's storage is a character window holding its record image
    /// (an image-stored leaf, or a Tier-B REDEFINES view) — the numeric population of the <see cref="CallCrossing.Text"/>
    /// form, whose boundary image is its STORAGE (kb/Work PB970). ONE predicate for the read and the write half.
    /// The identity question — is this reference the item itself, not a reference-modified view — is
    /// <see cref="Place.DenotedItem"/>'s (kb/Work PB602).</summary>
    private static bool IsImageCarriedNumeric(Place p) =>
        p.DenotedItem is { IsGroup: false, Pic.Category: PicCategory.Numeric } item
        && (item.StoreAsImage || p is RedefViewPlace);

    /// <summary>⛔ THE BY CONTENT READ of a character-image argument — <see cref="CallStringRead"/>, except that an
    /// OCCURS DEPENDING group sends only its current extent. ISO §14.8.2.2 states the two lengths apart: "For an
    /// argument or formal parameter that is described as an occurs-depending group item passed by reference, the
    /// maximum length is used. For an occurs-depending group item passed by content, the length of the argument
    /// is determined by the rules of the OCCURS clause for a sending data item" — §13.18.38.4 GR8's current-count
    /// part, which <see cref="PlaceRenderer.SendingGroupImage"/> owns. Both the CALL and the INVOKE BY CONTENT
    /// arms read through here (kb/Work PB965 finisher — measured: `CALL … USING BY CONTENT OG` with the ODO count
    /// at 2 of 5 delivered all five occurrences).</summary>
    internal static string CallContentRead(Place p) =>
        // SendingGroupValue is the ONE sending dispatch: an alphanumeric group's current-extent image, a bit /
        // national group's current-extent boolean / national positions (the elementary alphabet — kb/Work PB1166).
        p is OdoGroupPlace ? PlaceRenderer.SendingGroupValue(p, "CALL BY CONTENT argument") : CallStringRead(p);

    /// <summary>A place that crosses the boundary as a bit / national group's ELEMENTARY value (kb/Work PB1166 —
    /// see <see cref="CallStringRead"/>). A redefinition view over one crosses the same way (kb/Work PB1653): its
    /// window's coding IS the elementary alphabet (<see cref="BitWindow"/> boolean positions, <see cref="NationalWindow"/>
    /// national positions), so the view's value is exactly what <c>PlaceRenderer.SendingGroupValue</c> /
    /// <c>PlaceRenderer.Write</c> read and store — excluding it sent a national group's 2m storage bytes against a
    /// formal that counts m positions.</summary>
    private static bool IsAsIfGroupCrossing(Place p) => p.Item.IsAsIfElementary;

    /// <summary>The full-allocation place of a boundary operand (§14.2.3 GR8): an occurs-depending wrapper unwrapped.</summary>
    private static Place FullAllocation(Place p) => p is OdoGroupPlace o ? o.Inner : p;

    /// <summary>The character width of a formal's TEXT crossing — the unit <see cref="CallStringRead"/> measures it
    /// in: a bit / national group's as-if position count (kb/Work PB1166), else its record image width. ONE
    /// answer for the CALL callee's carrier window and the INVOKE argument / copy-out windows.</summary>
    internal static int BoundaryImageWidth(DataItem item) =>
        item.IsAsIfElementary ? item.AsIfPic!.Length : item.ImageWidth;

    /// <summary>The character window of an ELEMENTARY string-carried formal — its carrier's width: the PICTURE's
    /// length for every character category, and for an image-stored NUMERIC formal its storage image
    /// (<see cref="DataItem.ImageWidth"/>, the one width authority), which for a BINARY / PACKED formal is its BYTES,
    /// not its digit count (kb/Work PB1466: a promoted <c>USAGE BINARY-DOUBLE</c> formal is 8 positions, not 19). ONE
    /// answer for the CALL callee's resident carrier and the INVOKE argument / copy-out windows.</summary>
    internal static int ElementaryFormalWindow(DataItem formal) =>
        Math.Max(1, formal.StoreAsImage ? formal.ImageWidth : formal.Pic!.Length);

    internal static string CallStringWrite(Place p, string value) =>
        // The boundary WRITE half of the §14.2.3 GR8/GR9 full-allocation rule above: a group (including an
        // occurs-depending group — OdoGroupPlace.Write delegates to the full-width struct) distributes the whole
        // image through FromImage, never the GR8a current-extent splice.
        // ⛔ NO `&& p.Item.IsImageCapable` HERE — THE ONE WRITER OWNS THE GUARD (kb/Work PB177 arm B, the EIGHTH
        // two-arm-dispatch instance in this repo). This arm used to carry the capability test itself and an
        // imageless group therefore FELL THROUGH to the raw `PlaceRenderer.Write(p, value)` at the bottom, which
        // for a group MemberPlace renders `_G = <string>;` — a backend CS0029 (measured: a sub-program whose
        // `PROCEDURE DIVISION USING G` names a group with a USAGE POINTER leaf, compiled ALONE, so the
        // caller-side ArgText screen above gives it no cover). Its READ twin `CallStringRead` correctly staged
        // the Tier-C loud through `OperandText.FieldImage`, and the comment right here claimed the two were
        // "kept in lockstep deliberately" while they were not. Routing EVERY non-RedefViewPlace group to
        // `WriteFullGroupImage` makes the lockstep a STRUCTURAL fact — `WriteGroupImage`'s own arm order stages
        // the same loud — instead of something a drift test has to assert. All FIVE live callers inherit it with
        // no edit: ProgramEmitter's callee formal copy-in (ProgramEmitter.cs), the CALL BY REFERENCE cell
        // (CallEmitter.cs), the two INVOKE argument write-backs (OoEmitter.cs — the BY REFERENCE copy-out and
        // the RETURNING delivery), and ReportWriterEmitter's `CONTROL IS <group>` restore. (⚠ this sentence used
        // to say "three", which under-counted the two OO sites — the enumeration is now the grep.)
        // The RECEIVING twin of CallStringRead's first arm: a ref-modded operand splices its slice back
        // (§8.4.3.3.4 GR6 — an elementary alphanumeric item over the slice), and takes NEITHER the group image
        // store NOR the numeric decode/re-encode below, whose predicates both read through to the INNER item.
        p is RefModPlace ? PlaceRenderer.Write(p, value)
        // The receiving twin of CallStringRead's bit / national group arm (kb/Work PB1166): the ONE as-if writer
        // (PlaceRenderer.Write's as-if arm — FromBits / FromNat) distributes the elementary value to the subordinates.
        : IsAsIfGroupCrossing(p) ? PlaceRenderer.Write(FullAllocation(p), value)
        : p.Item.IsGroup && p is not RedefViewPlace
            ? PlaceRenderer.WriteFullGroupImage(p, value, "CALL boundary copy")   // the FULL image — an ODO wrapper is unwrapped (kb/Work PB80)
        // ⛔ AN IMAGE-CARRIED NUMERIC LEAF: the boundary text IS its storage image, of EVERY byte form (kb/Work
        // PB970 — the mirror of CallStringRead's arm). §14.6.5 delivers "the content of the data item" and
        // §14.2.3 GR8 makes a BY REFERENCE formal "occupy the same storage area as the argument" — content in,
        // content out — so the text is stored as it stands, fitted to the item's character positions: no decode,
        // and so no value conversion to lose content that is not a valid representation (a RETURNING item
        // holding spaces came home as 000 — kb/Work PB962). This used to be TWO arms: the zoned one stored the
        // text, and a BINARY/PACKED one decoded it as DISPLAY digits and re-encoded it (kb/Work PB181) — right
        // only while the channel carried operand text, and wrong for every native argument, whose adapter has
        // delivered the storage image since kb/Work PB873. A float leaf fell to the raw write below with no arm.
        : IsImageCarriedNumeric(p)
            ? PlaceRenderer.Write(p, RuntimeApi.StrStore(value, $"{p.Item.ImageWidth}"))
            : PlaceRenderer.Write(p, value);

    /// <summary>Emit CANCEL (ISO §14.9.5): one registry call per target, left to right (GR2). Under enabled
    /// EC-PROGRAM checking (>>TURN, §7.3.25) each target's <see cref="CobolCallException"/> runs the
    /// §14.6.13.1.3 sequence (status, F3 selection, fatal default) instead of crashing raw.</summary>
    public void EmitCancel(BoundCancel c)
    {
        var w = ctx.Writer;
        var ecProg = EnabledProgramNames();
        foreach (var (literal, dynamic) in c.Targets)
        {
            string nameExpr = literal is { } l ? CsLiteral(l) : OperandText.AsString(dynamic!, num);
            string call = $"ProgramRegistry.Cancel({nameExpr}, {CsLiteral(callState.SelfPath)});";
            if (ecProg.Count == 0)
            {
                w.Line(call);
                continue;
            }
            using (w.Block("try"))
                w.Line(call);
            // §14.9.5.2 gives CANCEL no conditional phrase at all, so the arm is always the §14.6.13.1.3
            // sequence; and every name it can raise (EC-PROGRAM-CANCEL-ACTIVE) is raised OUTSIDE any
            // activation, so the shared arm's !ControlTransferred filter is vacuously true here.
            EmitCallEcCatch(ecProg, byPhrase: false, phraseFlag: null);
        }
    }

    /// <summary>Emit GOBACK (ISO §14.9.18): move the RETURNING source into the header RETURNING item (GR2 — the
    /// activation result), stage a RAISING exception condition for the activator (the EC model — picked up at
    /// the activating statement's pickup, and never raised when the activator emitted none — kb/Work PB892), then raise <see cref="ProgramReturn"/> —
    /// caught at THIS program's activation entry, returning control to the activator (called program) or ending
    /// the run unit (main program, GR3).</summary>
    public bool EmitGoback(BoundGoback g)
    {
        var w = ctx.Writer;
        // §14.9.18.4 GR6 — "If a GOBACK statement is executed within the range of a declarative procedure whose
        // USE statement contains the GLOBAL phrase and that USE statement is specified in the same program as
        // the GOBACK statement, the EC-FLOW-GLOBAL-GOBACK exception condition is set to exist." This is the
        // RUN-TIME half of the rule pair whose syntax half (§14.9.18.3 SR1) the binder refuses; GR6 governs
        // LEGAL source — a GOBACK in an ordinary paragraph a global declarative PERFORMs — so it cannot be
        // decided at bind time (kb/Work PB409). The raise is FIRST: the condition exists when the statement is
        // executed, and with checking enabled and no applicable handler §14.6.13.1.3 #7 terminates the run unit
        // before the return happens. With checking off the helper returns and the GOBACK proceeds.
        // ⛔ CHECKING-GATED at EMIT, like every other raise site: with the name not enabled at this statement the
        // condition is not raised at all (§14.6.13.1.1) and a declarative-bearing program that never names it
        // keeps byte-identical generated source — which also keeps the raise out of an EC-FREE group, whose
        // generated file carries no ExceptionState using.
        if (dispatch.InGlobalDeclarativeRangeTest is { } inGlobalRange && ec.EnabledHere("EC-FLOW-GLOBAL-GOBACK"))
            w.Line($"if ({inGlobalRange}) ExceptionState.FlowGlobalGobackError(\"a GOBACK statement executed "
                + "within the range of a USE ... GLOBAL declarative procedure of the same program "
                + "(ISO 14.9.18.4 GR6)\");");
        if (g.ReturningSource is not null)
        {
            // The move was BOUND by CallBinder.BindGoback (kb/Work PB880) into the same RETURNING item.
            if (g.ReturningMove is { } rm)
                move.Emit(rm);
            else
                w.Line(LoudStmt("GOBACK RETURNING without a PROCEDURE DIVISION RETURNING item (ISO §14.9.18.4 GR2)"));
        }
        if (g.Raising is { } r)
            // §14.9.18.4 GR3 (the P13 review C3 fix): in a program NOT under the control of a calling runtime
            // element, GOBACK operates as STOP and "a RAISING phrase, if specified, is ignored" — so the staging
            // (including the checking-off fatal termination arm) is __asCalled-gated, exactly like EmitExitProgram.
            using (w.Block("if (__asCalled)   // §14.9.18.4 GR1b/GR3 — a main-program GOBACK ignores RAISING"))
                EmitRaisingStage(r, "GOBACK");
        // GOBACK … WITH {NORMAL|ERROR} STATUS [value] (§14.9.18.4 GR10): the status reaches the OS ONLY in a main
        // program (GR3 — a called-program GOBACK returns to the activator, GR2, so its status phrase is inert);
        // guard on __asCalled, the same activation flag EmitExitProgram uses.
        if (g.Status is { } st)
            w.Line($"if (!__asCalled) {RuntimeApi.SetExitStatus(num.ExitStatus(st))};   // §14.9.18.4 GR3/GR10 — a main program passes the status");
        w.Line("throw new ProgramReturn();   // return to the activator; in a main program ≡ STOP (ISO §14.9.18.4 GR2/GR3)");
        return true;
    }

    /// <summary>Emit EXIT PROGRAM [RAISING …] (ISO §14.9.14 Format 2): GR2 — in a program NOT under the control
    /// of a calling runtime element the statement is CONTINUE and "no exception condition is raised even if the
    /// RAISING phrase is specified", so BOTH the staging and the return are <c>__asCalled</c>-gated; GR3 — in a
    /// called program it returns per the GOBACK rules, staging the RAISING condition for the activator.</summary>
    public void EmitExitProgram(BoundExitProgram ep)
    {
        var w = ctx.Writer;
        if (ep.Raising is null)
        {
            w.Line("if (__asCalled) throw new ProgramReturn();   // ISO §14.9.14.4 GR2: CONTINUE in a non-called program; GR3: return in a called one");
            return;
        }
        using (w.Block("if (__asCalled)   // GR2 — a non-called program raises nothing, even with RAISING"))
        {
            EmitRaisingStage(ep.Raising, "EXIT PROGRAM");
            w.Line("throw new ProgramReturn();   // return to the activator (ISO §14.9.14.4 GR3)");
        }
    }

    /// <summary>Stage a <c>RAISING</c> phrase's exception condition for the ACTIVATOR (ISO §14.9.18.4 GR1 b) /
    /// §14.9.14.4 GR3 — consumed by the activating statement's pickup; the staging names the activating
    /// ACTIVATION, so when that activator emitted no pickup nothing else can take it — kb/Work PB892 Arm B).
    /// Staging is UNCONDITIONAL and raises nothing here.
    /// <para>⛔ IT USED TO BRANCH ON A BIND-TIME <c>Enabled</c> FLAG, AND THAT FLAG WAS THIS ELEMENT'S OWN
    /// <c>&gt;&gt;TURN</c> STATE (kb/Work PB408). GR1 b) names one element and it is the other one: "an exception
    /// condition is raised in the activating runtime element if checking for that exception condition is enabled
    /// in the activating runtime element". So a declarative in an activator that had enabled the condition never
    /// ran when the callee had it off, and one in an activator that had DISABLED it ran when the callee had it
    /// on — measured in both directions inside a single compilation group, which §7.3.25.4 GR6/GR8 make possible
    /// because a TURN directive scopes to the statements that FOLLOW IT IN THE COMPILATION GROUP. The disabled +
    /// fatal arm additionally terminated the run unit from inside the CALLEE citing §14.6.13.1.3 #8; that rule's
    /// latitude governs a fatal condition that already EXISTS, and GR1 b) stops one coming into existence in an
    /// unchecked activator at all — the identical misapplication <c>ProgramTable.ApplyPropagationDefault</c> had
    /// already had removed on the runtime side.</para></summary>
    public void EmitRaisingStage(BoundRaising r, string verb)
    {
        var w = ctx.Writer;
        if (r.ObjectSource is { } os)
        {
            // The exception-OBJECT leg (§14.9.18.4 GR1b2; the EC-OO wave): objects are not TURN-gated
            // (§7.3.25 takes names only); the activator's §14.6.13.1.5 rules decide.
            w.Line($"ExceptionState.SetPropagatingObject({RuntimeApi.AsExceptionObject(PlaceRenderer.Read(os))});   // {verb} RAISING identifier-1 — staged for the activator");
            return;
        }
        if (r.IsLast)
        {
            // §14.9.18.4 GR1b3: the name is the run-unit last exception status (GR1b3b — a clear status stages
            // nothing), and GR1b3a substitutes EC-RAISING-NOT-SPECIFIED for a level-3 EC-USER condition the
            // containing element's PD-header RAISING phrase does not name. That list crosses to the RUNTIME
            // because only the runtime knows which name is being propagated; the membership test is written
            // ONCE, there, over the names themselves.
            string names = r.PdRaising is { Count: > 0 } pdr
                ? $"new[] {{ {string.Join(", ", pdr.Select(CsLiteral))} }}"
                : "null";
            string loc = r.WithLocation
                ? $", {CsLiteral(r.StatementName!)}, {CsLiteral(r.Location!)}"
                : "";
            // An OBJECT status is admitted only when the header's RAISING phrase names its class or an interface it
            // implements (§14.6.13.1.5 item 1 — the one census, EcState.PdRaisingObjectCsTypes); otherwise the GOBACK
            // is "as if EXCEPTION EC-OO-EXCEPTION were specified" (kb/Work PB1121).
            w.Line($"ExceptionState.SetPropagatingLast({ec.ObjectApplicableTest("ExceptionState.ExceptionObject")}, {names}{loc});"
                + "   // RAISING LAST EXCEPTION (§14.9.18.4 GR1b3a — the PD-header RAISING list is GR1b3a's operand; §14.6.13.1.5 item 1 for an object)");
            return;
        }
        // kb/Work R07: the §15.32.3 r2 / §15.30.3 r2 operands travel WITH the staged condition and are applied by
        // the activator-side raise, when THIS name's TURN said WITH LOCATION. §7.3.25.4 GR7 keys them on the
        // directive governing the SOURCE STATEMENT ("all information necessary to identify a source statement …
        // is made available to the run unit"), and that statement is this GOBACK/EXIT — so the RAISING element's
        // own fold is the right one here even though the RAISE happens in the activator. Without LOCATION the
        // two-arg call stages null-null and the activator's Set falls back to ITS ambient statement context,
        // which is the §14.6.13.1.3 #6 reading: the condition is raised as if by a RAISE at the end of the
        // activating statement.
        w.Line(r.WithLocation
            ? $"ExceptionState.SetPropagating({CsLiteral(r.EcName!)}, {(r.Fatal ? "true" : "false")}, "
              + $"{CsLiteral(r.StatementName!)}, {CsLiteral(r.Location!)});   // staged for the activator (§14.9.18.4 GR1b)"
            : $"ExceptionState.SetPropagating({CsLiteral(r.EcName!)}, {(r.Fatal ? "true" : "false")});   // staged for the activator (§14.9.18.4 GR1b)");
    }
}
