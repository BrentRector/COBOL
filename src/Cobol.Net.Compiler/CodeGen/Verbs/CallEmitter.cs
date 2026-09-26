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
        c = HoistOnceOnlyIdentification(c, w);
        // An EC-active group's CALL site consumes a callee-staged RAISING propagation itself (the pickup below
        // runs the §14.9.49 F3 selection and honors RESUME); an EC-free site emits none, and the staging — which
        // names THIS activation (kb/Work PB892 Arm B) — is then never raised anywhere (§14.9.18.4 GR1 b)).
        // §14.9.4.4 GR3d's ACTIVATING half (kb/Work PB133 wave C2b): this statement's TURN state.
        string invocation = InvocationText(c,
            argMismatchChecking: EnabledProgramNames().Contains("EC-PROGRAM-ARG-MISMATCH"));

        var ecProg = EnabledProgramNames();
        // The ACTIVATING half of §14.8.4.1's both-elements rule: this CALL statement's enabled EC-EXTERNAL-*
        // set becomes the pending site mask the activation boundary latches for the activated element's
        // Describe gate (§14.9.4.4 GR3e — "enabled ... in both the activated program and activating runtime
        // element"). Zero-scaffolding: an EC-free site emits nothing (the boundary re-zeroes after every call).
        int siteExternalMask = ecProg.Sum(ExternalBit);
        if (siteExternalMask != 0)
            w.Line($"ExceptionState.ExternalCheckMask = {siteExternalMask};   // §14.8.4.1 — this CALL's EC-EXTERNAL enablement (the activating element)");
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
        if (ecProg.Count > 0) EmitCallEcCatch(ecProg, byPhrase: hasOn, flag, c.InExpression);
        if (ecOther.Count > 0) EmitCallEcCatch(ecOther, byPhrase: false, flag, c.InExpression);
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

    /// <summary>§14.9.4.4 GR3a's once-only identification (kb/Work PB133 wave B): rewrite the ALIASING
    /// operands' places so every non-constant table subscript and ref-mod position is a hoisted local. The
    /// value operands (BY CONTENT / BY VALUE snapshots) already read once when the args array is built.</summary>
    private BoundCallProgram HoistOnceOnlyIdentification(BoundCallProgram c, CodeWriter w)
    {
        bool needed = (c.Returning is { } r0 && HasVariableIndex(r0))
            || c.Args.Any(a => a.Mode == CobolPassMode.Reference && a.Place is { } p0 && HasVariableIndex(p0));
        if (!needed) return c;
        var args = c.Args
            .Select(a => a.Mode == CobolPassMode.Reference && a.Place is { } p ? a with { Place = HoistPlace(p, w) } : a)
            .ToList();
        var ret = c.Returning is { } rp && HasVariableIndex(rp) ? HoistPlace(rp, w) : c.Returning;
        return c with { Args = args, Returning = ret };
    }

    private static bool HasVariableIndex(Place p) => p switch
    {
        RefModPlace rm => !IsConstIndex(rm.Start) || (rm.Length is { } l && !IsConstIndex(l)) || HasVariableIndex(rm.Inner),
        PlaceDecorator d => HasVariableIndex(d.Inner),
        MemberPlace mp => PathHasVariableIndex(mp.Path),
        DynTablePlace dp => PathHasVariableIndex(dp.Path),
        _ => false,
    };

    private static bool PathHasVariableIndex(AccessPath path) => path.Segments.Any(s => s switch
    {
        FixedTableSegment ft => !IsConstIndex(ft.OneBasedIndex),
        DynTableSegment dt => !IsConstIndex(dt.OneBasedIndex),
        _ => false,
    });

    private static bool IsConstIndex(string rendered) => long.TryParse(rendered.Trim(), out _);

    private int _gr3aSeq;

    private Place HoistPlace(Place p, CodeWriter w)
    {
        switch (p)
        {
            case RefModPlace rm:
                return rm with
                {
                    Inner = HoistPlace(rm.Inner, w),
                    Start = HoistIndex(rm.Start, w),
                    Length = rm.Length is { } l ? HoistIndex(l, w) : null,
                };
            case MemberPlace mp when PathHasVariableIndex(mp.Path):
                return mp with { Path = HoistPath(mp.Path, w) };
            case DynTablePlace dp when PathHasVariableIndex(dp.Path):
                return dp with { Path = HoistPath(dp.Path, w) };
            default:
                return p;   // constant or index-free — nothing to pin
        }
    }

    private AccessPath HoistPath(AccessPath path, CodeWriter w)
    {
        var segs = new List<AccessSegment>(path.Segments.Count);
        foreach (var s in path.Segments)
            segs.Add(s switch
            {
                FixedTableSegment ft when !IsConstIndex(ft.OneBasedIndex) => new FixedTableSegment(HoistIndex(ft.OneBasedIndex, w)),
                DynTableSegment dt when !IsConstIndex(dt.OneBasedIndex) => new DynTableSegment(HoistIndex(dt.OneBasedIndex, w)),
                _ => s,
            });
        return new AccessPath(segs);
    }

    private string HoistIndex(string rendered, CodeWriter w)
    {
        if (IsConstIndex(rendered)) return rendered;
        string local = $"__ci{_gr3aSeq++}";
        w.Line($"var {local} = {rendered};   // §14.9.4.4 GR3a — identified once, at the CALL's start");
        return local;
    }

    /// <summary>The <c>CobolArg[]</c> expression of one bound call's arguments — the ONE argument-array text of
    /// <see cref="EmitCall"/>, which renders every activation, statement-position or operand (kb/Work PB892).</summary>
    private string ArgsArrayText(BoundCallProgram c) => c.Args.Count == 0
        ? "System.Array.Empty<CobolArg>()"
        : $"new CobolArg[] {{ {string.Join(", ", c.Args.Select(ArgText))} }}";

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
        string head = $"{CsLiteral(callState.SelfPath)}, {ArgsArrayText(c)}, "
            + $"{(c.Returning is { } rp ? ReturningArgText(rp) : "null")}";
        if (c.IsPointerTarget && c.DynamicName is BoundFieldOperand pf)
            return c.IsFunction
                ? $"ProgramRegistry.CallFunctionPointer({PlaceRenderer.Read(pf.Place)}, {head});"
                : $"ProgramRegistry.CallPointer({PlaceRenderer.Read(pf.Place)}, {head});";
        string nameExpr = c.LiteralName is { } literal
            ? CsLiteral(literal)
            : $"({OperandText.AsString(c.DynamicName!, num)}).Trim()";   // GR3b — the identifier's value at CALL time (GR3a: read once)
        return $"ProgramRegistry.CallProgram({nameExpr}, {head}"
            + $"{(c.IsFunction ? ", notFoundEc: \"EC-FUNCTION-NOT-FOUND\"" : "")}"   // §8.4.3.2.4 GR6b
            + $"{(argMismatchChecking ? ", siteArgMismatchChecking: true" : "")});";
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
    /// compile-time split and the emitted runtime filter cannot drift apart. Also the source of this CALL's
    /// §14.8.4.1 EC-EXTERNAL site mask and of GR3d's ACTIVATING-half argument-checking flag.</summary>
    private List<string> EnabledProgramNames() =>
        EnabledCallNames().Where(RuntimeApi.CallEcIsProgramOrExternal).ToList();

    /// <summary>The complement: enabled carriable names NOT in GR3h item 1's two families (today only
    /// EC-FUNCTION-NOT-FOUND — §8.4.3.2.4 GR6b, a user-defined-function locate miss). These take ISO §14.9.4.4
    /// GR3h item 2's SECOND disjunct — "or if the exception condition is not one of the EC-PROGRAM exception
    /// conditions, any applicable exception processing statements are executed" — with NO ON EXCEPTION escape,
    /// which is why they need an arm of their own rather than a share of the family arm.</summary>
    private List<string> EnabledOtherCallNames() =>
        EnabledCallNames().Where(n => !RuntimeApi.CallEcIsProgramOrExternal(n)).ToList();

    /// <summary>The <see cref="ExternalChecks"/> bit of one EC-EXTERNAL level-3 name (0 for any other name) —
    /// the emitted CALL-site mask is the OR over the statement's enabled set.</summary>
    private static int ExternalBit(string ec) => ec switch
    {
        "EC-EXTERNAL-FORMAT-CONFLICT" => (int)ExternalChecks.FormatConflict,
        "EC-EXTERNAL-DATA-MISMATCH" => (int)ExternalChecks.DataMismatch,
        "EC-EXTERNAL-FILE-MISMATCH" => (int)ExternalChecks.FileMismatch,
        _ => 0,
    };

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
    private void EmitCallEcCatch(List<string> ecNames, bool byPhrase, string? phraseFlag, bool inExpression = false)
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
                    EcEmitter.FatalTermination($"__ce{id}.EcName", $"__ce{id}.Message"),   // every name here is fatal (Table 13)
                    r => Resume(inExpression, r));
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
            if (raised is not null) w.Line($"{raised} = true;");
            w.Line($"ExceptionState.SetObject(__po{id});   // GR1b2 — the current exception object HERE (the activator)");
            w.Line($"int __or{id} = {ec.ObjDispatchExpr($"__po{id}")};   // rule 2 — USE AFTER EXCEPTION OBJECT (GR14)");
            w.Line(Resume(site.InExpression, $"__or{id}", "   // RESUME AT procedure-name"));
            using (w.Block($"if (__or{id} == -3)   // rule 3 PROPAGATE ON: directive not implemented (residue); rule 4 —"))
            {
                // As if EXCEPTION EC-OO-EXCEPTION (:24608): the name enters the F3 tiers; Table 13 makes it fatal.
                ec.EmitConditionSet("EC-OO-EXCEPTION", "as if EXCEPTION EC-OO-EXCEPTION (:24608)");
                ec.EmitSelection("\"EC-OO-EXCEPTION\"",
                    EcEmitter.FatalTermination("\"EC-OO-EXCEPTION\"",
                        "\"an exception object was not handled (ISO 14.6.13.1.5; Table 13 - fatal)\""),
                    r => Resume(site.InExpression, r, ""));
            }
            w.Line("// -1/-2: declarative completed / RESUME NEXT — normal continuation (:24604)");
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
                r => Resume(site.InExpression, r),
                fatalWhen: $"__pf{id}");
        }
        return raised;
    }

    /// <summary>The RESUME landing of one activation site's dispatch result — the ONE place the two kinds of
    /// activation part: a CALL / INVOKE STATEMENT is itself §14.9.33.4 GR2 a) 2.'s applicable statement, so its
    /// transfer is the ordinary <see cref="DispatchState.ResumeTransfer"/>; an OPERAND activation's applicable
    /// statement is the one it was written in, which it leaves through
    /// <see cref="DispatchState.OperandActivationResume"/> (kb/Work PB892).</summary>
    private string Resume(bool inExpression, string resultVar,
        string comment = "   // RESUME AT procedure-name (§14.9.33.4 GR3)") =>
        inExpression ? dispatch.OperandActivationResume(resultVar) : dispatch.ResumeTransfer(resultVar, comment);

    /// <summary>The C# <c>CobolArg</c> expression for one bound CALL argument (caller side; design D1/D2).
    /// BY REFERENCE builds an accessor carrier over the caller's storage (§14.2.3 GR8); BY CONTENT/BY VALUE
    /// snapshot the value into a cell AT CALL INITIATION — which also realizes the §14.9.4.4 GR3a once-only
    /// evaluation for those modes. (A BY REFERENCE accessor over a SUBSCRIPTED operand re-evaluates the
    /// subscript inside the closure — the GR3a capture-into-locals refinement is a known follow-up.)</summary>
    public string ArgText(BoundCallArg a) => LandedForFormal(a, ArgCarrierText(a));

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
    /// <para>Only BY CONTENT / BY VALUE, and only a FIXED-POINT NUMERIC formal: BY REFERENCE is GR8's storage
    /// aliasing with no crossing conversion; a group, index, pointer, object or edited formal takes GR9's
    /// MOVE leg; a formal "of class index, object, or pointer" takes GR9's SET leg — and USAGE INDEX is why the
    /// guard is <c>PicInfo.IsClassNumericFixedPoint</c> and not a bare category test: an index item's storage
    /// description carries category Numeric with ZERO digits, so landing it through a numeric profile stored
    /// <c>value % 10^0</c> = 0 and <c>BY CONTENT</c> an index of 3 crossed as 0 (measured); and a floating-point
    /// formal has no digit capacity to overflow (§14.6.8.3 GR1 — the IEEE receiver takes the algebraic value). <c>a.Formal</c> is null for exactly GR9's FIRST branch, whose
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
        && f.Pic is { IsClassNumericFixedPoint: true } fp
            ? RuntimeApi.ArgLandForFormal(built, fp.ProfileInitializer(ctx.SignEncoding), $"{fp.Scale}",
                                          fp.ClrType, ecState.SizeTruncationChecking)
            : built;

    /// <summary>⛔ THE DESCRIPTION a place's storage carries across the activation boundary, as the trailing
    /// <c>CobolArg</c> constructor arguments (kb/Work PB873 + PB965): an elementary NUMERIC item's whole
    /// <c>NumProfile</c> (Place.DenotedItem — a reference-modified view denotes no item and is character storage;
    /// a USAGE INDEX item's storage description has no digit positions for a profile to state), and a GROUP's
    /// §8.5.1.12 layout when it has a table or a variable-length member (<c>VariableLengthCompatibility.Layout</c>
    /// — the fact §8.5.1.12.2's correspondence needs from each side, since the two sides are compiled apart). ONE
    /// renderer for the argument and the RETURNING item, because both are storage the activating element owns
    /// and the activated element reaches through a carrier.</summary>
    private string PlaceDescription(Place p)
    {
        string meta = p.DenotedItem is { Pic: { Category: PicCategory.Numeric } pp } && pp.Usage is not Usage.Index
            ? pp.ProfileInitializer(ctx.SignEncoding)
            : "null";
        return BoundaryLayout(p) is { } layout ? $"{meta}, {layout}" : meta;
    }

    /// <summary>The emitted §8.5.1.12 layout of a group place that has a table or a variable-length member, or
    /// null when it has neither (a null layout answers "no correspondence" on the runtime side, which is right
    /// for such a group). A reference-modified or redefinition view is character storage, never a group.</summary>
    internal static string? BoundaryLayout(Place p) =>
        p is not RedefViewPlace and not RefModPlace
        // A bit / national group crosses as its ELEMENTARY value, which has no image layout (kb/Work PB1166).
        && p.DenotedItem is { IsAsIfElementary: false } item
        && VariableLengthCompatibility.Layout(item) is { } layout
        && VariableLengthCompatibility.HasTableOrVariable(layout)
            ? LayoutArray(layout)
            : null;

    /// <summary>The C# array literal of a §8.5.1.12 layout.</summary>
    internal static string LayoutArray(int[] layout) => $"new int[] {{ {string.Join(", ", layout)} }}";

    /// <summary>The RETURNING item's <c>CobolArg</c> (kb/Work PB962/PB965): the same BY REFERENCE carrier an
    /// argument gets (§14.2.3 GR6 NOTE 1 — "the storage for the returning item is allocated in the activating
    /// source unit") plus the same description, so the activated element's delivery can place the result by
    /// the RECEIVER's shape.</summary>
    private string ReturningArgText(Place rp) =>
        $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Reference)}, {RefCarrier(rp)}, {PlaceDescription(rp)})";

    /// <summary>The C# <c>CobolArg</c> expression for one bound CALL argument BEFORE the §14.2.3 GR9/GR10
    /// landing <see cref="LandedForFormal"/> wraps around it.</summary>
    private string ArgCarrierText(BoundCallArg a)
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
            string meta = PlaceDescription(p);
            // ⛔ V59 RESIDUE FIX: the predicate is IsImageCapable, not the pre-V59 IsCharacterImage. A group whose
            // only non-character leaf is BINARY/PACKED now HAS a whole-group image — V59 gave those leaves their
            // pinned bytes — and `RecordStructEmitter` emits AsImage()/FromImage() for exactly IsImageCapable
            // items. Guarding on the stricter predicate therefore loud-staged a CALL whose codec had actually been
            // generated: `01 G. 05 N PIC S9(4) COMP. 05 A PIC X(3).` answered BYTE-LENGTH(G) = 5 and then threw
            // "no whole-group character image" on `CALL "SUB" USING G`. That claim was false, and refusing the
            // CALL rejected conforming source — §14.2.3 GR8 (`cite.py`-verified): "If the argument is passed by
            // reference, the activated runtime element operates as if the formal parameter occupies the same
            // storage area as the argument", which COBOL.NET realizes through the very image round-trip that
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
                // through is both the presence fact and the aliasing.
                if (fwd is { CarrierResident: true } rf)
                    return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Reference)}, "
                        + $"{rf.CarrierField}, {meta})";
                // A NON-resident formal (a group, or a REDEFINED elementary one) keeps a callee-local field
                // that round-trips the caller's image at the activation boundary — so the carrier to pass on IS
                // a fresh view over that field, and only the PRESENCE has to be taken from the incoming
                // carrier. Rebuilding it unconditionally is what made an omitted group formal arrive at the
                // next callee as PRESENT (measured: `CALL "S8" AS NESTED USING OMITTED` → the inner
                // `LH IS OMITTED` test answered false).
                return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Reference)}, "
                    + $"{Forwarded(probe, RefCarrier(p))}, {meta})";
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
                CallCrossing.VarGroup => RuntimeApi.VarGroupCell(PlaceRenderer.VarGroupImage(p, "CALL argument")),
                CallCrossing.Text => $"ManagedPointer<string>.Cell({CallContentRead(p)})",
                // Native and Managed both snapshot the storage's own value into a detached cell of its own
                // carrier — §14.2.3 GR9/GR10's allocated record, whose filling is "a COMPUTE statement without
                // the ROUNDED phrase" for a numeric formal and "a SET statement" for one of class object or
                // pointer. A SET between two items of the same category IS this copy (kb/Work PB663).
                _ => $"ManagedPointer<{CallCellCarrier(p)}>.Cell({PlaceRenderer.Read(p)})",
            };
            return $"new CobolArg({RuntimeApi.PassModeText(a.Mode)}, {Forwarded(probe, snapshot)}, {meta})";
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
            // ⛔ A FIGURATIVE CONSTANT / ALL LITERAL FILLS THE FORMAL (kb/Work PB1418). §14.8.2.3.3 rule 2d makes a
            // BY CONTENT crossing into a non-numeric formal "the same as for a MOVE statement", and a figurative
            // constant "in association with a fixed-length data item" is "repeated character by character" to the
            // item's character positions, then truncated from the right (§8.3.3.6.4 GR2). The
            // crossing used to carry ONE occurrence, which the callee's formal then space-padded: `CALL … AS NESTED
            // USING BY CONTENT ALL "*"` into a PIC X(4) formal arrived as "*   ", ZERO as "0   " (measured). The
            // formal is recorded on every Format-2 and function argument (BoundCallArg.Formal), so the fill width is
            // its character image width; with no fixed-length formal known (a numeric formal, whose COMPUTE-rule
            // crossing reads the one digit, or an ANY LENGTH / dynamic-length one) one occurrence is the value.
            case BoundAllLiteral all:
                return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Content)}, ManagedPointer<string>.Cell({CsLiteral(RepeatToWidth(all.Literal, FigurativeFillWidth(a.Formal)))}), null)";
            case BoundFigurative fig:
                return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Content)}, ManagedPointer<string>.Cell(new string({FigurativeConstants.Fill(fig.Kind, ctx.Data.Collating, a.Formal?.Pic?.Category)}, {FigurativeFillWidth(a.Formal) ?? 1})), null)";
            default:
                return $"new CobolArg({RuntimeApi.PassModeText(CobolPassMode.Content)}, ManagedPointer<string>.Cell("
                    + LoudValue("string", "CALL USING argument form") + "), null)";
        }
    }

    /// <summary>The character width a figurative-constant / ALL-literal argument fills (§14.8.2.3.3 rule 2d's MOVE
    /// into the corresponding formal) — the formal's character positions when it is a fixed-length, non-numeric
    /// ELEMENTARY item; null when no such width is known (no recorded formal, a numeric formal, an ANY LENGTH or
    /// DYNAMIC LENGTH one). ⚠ A GROUP formal still receives one occurrence — a named residue: its fill width is the
    /// group's current extent, which for a variable-length group is a run-time fact this compile-time fill cannot
    /// read (kb/Work PB204's BoundaryImageCapable carrier), so it is not guessed here.</summary>
    private static int? FigurativeFillWidth(DataItem? formal) =>
        formal is { IsElementary: true, IsAnyLength: false, IsDynamicLength: false }
        && formal.Pic is { Category: not PicCategory.Numeric }
        && formal.ImageWidth > 0
            ? formal.ImageWidth : null;

    /// <summary>An ALL literal's value at <paramref name="width"/> characters — §8.3.3.6.4 GR9 ("all or part of the
    /// string generated by successive concatenations of the characters comprising literal-1"), sized by GR2 (repeated
    /// until it reaches the associated item's character positions, then truncated from the right); one occurrence
    /// (GR3 c) when no width is known.</summary>
    private static string RepeatToWidth(string literal, int? width)
    {
        if (width is not { } w || literal.Length == 0) return literal;
        var sb = new System.Text.StringBuilder(w + literal.Length);
        while (sb.Length < w) sb.Append(literal);
        return sb.ToString(0, w);
    }

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
            PlaceRenderer.VarGroupImage(p, "CALL argument"),
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
    /// <para>A <see cref="RedefViewPlace"/> or a <see cref="RefModPlace"/> is excluded by construction and the
    /// exclusion is stated rather than relied on: §13.18.44.3 SR12 ("The REDEFINES clause shall not be
    /// specified for a data item of class object, message-tag, or pointer …") and SR14 (the same list for
    /// data-name-2) bar both ends of a redefinition, and §8.4.3.3.3 SR1's list admits reference modification
    /// only for character-class and display/national numeric operands — so neither place shape can stand over
    /// such an item on conforming source, and a nonconforming one keeps the character arm's existing loud
    /// rather than acquiring a slot it has no storage for.</para></summary>
    internal static bool CallPlaceIsManaged(Place p) =>
        p is not RedefViewPlace and not RefModPlace && SlotWindow.CarriedBySlot(p.Item);

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
    /// see <see cref="CallStringRead"/>). A redefinition view over one is character storage, never the group.</summary>
    private static bool IsAsIfGroupCrossing(Place p) => p is not RedefViewPlace && p.Item.IsAsIfElementary;

    /// <summary>The full-allocation place of a boundary operand (§14.2.3 GR8): an occurs-depending wrapper unwrapped.</summary>
    private static Place FullAllocation(Place p) => p is OdoGroupPlace o ? o.Inner : p;

    /// <summary>The character width of a formal's TEXT crossing — the unit <see cref="CallStringRead"/> measures it
    /// in: a bit / national group's as-if position count (kb/Work PB1166), else its record image width. ONE
    /// answer for the CALL callee's carrier window and the INVOKE argument / copy-out windows.</summary>
    internal static int BoundaryImageWidth(DataItem item) =>
        item.IsAsIfElementary ? item.AsIfPic!.Length : item.ImageWidth;

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
            string nameExpr = literal is { } l ? CsLiteral(l) : $"({OperandText.AsString(dynamic!, num)}).Trim()";
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
            w.Line($"ExceptionState.SetPropagatingLast({names}{loc});"
                + "   // RAISING LAST EXCEPTION (§14.9.18.4 GR1b3a — the PD-header RAISING list is GR1b3a's operand)");
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
