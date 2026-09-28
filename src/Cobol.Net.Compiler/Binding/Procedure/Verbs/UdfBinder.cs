// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// User-defined function invocation (ISO §9.4 / §8.4.3.2; M2-UDF-1 — the PHASE4_RECONCILIATION
/// decision-complete design). A <c>FUNCTION user-name(args)</c> reference LOWERS at bind time onto the
/// program-activation machinery the group already has: a caller-side result temporary cloned from the
/// callee's RETURNING description (§8.4.3.2.4 GR1 :6963), a <see cref="BoundCallProgram"/> = CALL
/// "name" USING «args» RETURNING «temp» registered on a statement-scoped pending list, and the reading
/// expression/operand over the temp. <see cref="UdfWrapCalls"/> drains the list at the BindStatement
/// chokepoint into a <see cref="BoundSequence"/> that HOISTS each activation before the carrying
/// statement — always a PRE-op, because a function-identifier is never a receiving operand
/// (§8.4.3.2.3 SR1), so no store-polarity classification is needed (unlike property references).
/// Argument evaluation order (§8.4.3.2.4 GR2 — left to right, nested function-identifiers allowed) falls
/// out of registration order: a nested call registers while its consumer's arguments bind, so it
/// precedes the consumer in the sequence. A hoist is EXACT only where the reference is unconditionally
/// evaluated exactly once per statement execution; every conditionally- or repeatedly-evaluated CONDITION
/// window (a PERFORM UNTIL/VARYING UNTIL condition, a SEARCH WHEN, an EVALUATE object term, a non-first
/// AND/OR operand) instead drains its suffix into a per-evaluation <see cref="BoundUdfEvaluated"/> wrapper
/// (<see cref="UdfAttachPerEvaluation"/> — §8.8.4.13 r2 "if and when the conditions containing them are
/// evaluated"); every repeatedly-evaluated OPERAND window — a PERFORM VARYING BY operand and an AFTER level's
/// FROM operand, §14.9.28.4 GR12's "each time … is used in a setting or augmenting operation" — drains its
/// suffix into the expression twin <see cref="BoundUdfEvaluatedExpr"/> (kb/Work PB437). No window stages
/// loud any more: an EVALUATE selection subject binds once for the statement — a <b>value</b> subject's value
/// materialized into §14.9.25.4 GR1's intermediate result item (kb/Work PB394), a <b>condition</b> subject's
/// TRUTH value into a one-position boolean intermediate (§14.9.13.4 GR3 e, kb/Work PB842 / PB912), and a
/// partial-expression object splices that one value in rather than re-binding the subject — so the statement
/// hoist is EXACT for every subject, and the narrowed COBOLNET1509 residue stage was deleted with its last
/// two callers.
/// Emission is
/// 100% existing surface:
/// <c>CallEmitCall</c> → <c>ProgramRegistry.CallProgram</c>; FUNCTION-ID units already emit as callable
/// program classes with the RETURNING carrier.
/// P7 Step 10k: a real collaborator over <see cref="BinderContext"/>, landed TOGETHER with
/// <see cref="IntrinsicBinder"/> (the argument bind reaches back into its <c>BindArgOperand</c>). The
/// host.UserFunctions/host.UdfSelfName injection surface STAYS on the StatementBinder host (BinderDriver's
/// object-initializer contract — re-homed at 10t); the statement-scoped <c>_udfPendingCalls</c> mark/drain
/// suffix protocol is exposed through <see cref="Mark"/> for the host's BindStatement /
/// BindFlatSequence chokepoints. The line-65 <c>ConstructRegistry.Check</c> stays THE documented bind-time
/// gate exception (fires on RECOGNITION, pre-hoist), moved VERBATIM.</summary>
internal sealed class UdfBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>THIS statement's not-yet-hoisted PRE-ops (statement-scoped: BindStatement marks the count on entry
    /// and drains only its own suffix — the property-op discipline).
    /// <para>⛔ IT IS THE SHARED <see cref="DataBinder.PendingPreOps"/>, NOT A LIST OWNED HERE. A D18
    /// function-bearing subscript registers its §15.4 temp store on the same list while this binder's arguments
    /// bind (fix-queue PB17), and only ONE list in registration order sequences the store before the activation
    /// that consumes it. Every member below is therefore written against <see cref="BoundStatement"/>, not
    /// <see cref="BoundCallProgram"/>.</para></summary>
    private List<BoundStatement> Pending => ctx.Data.PendingPreOps;

    /// <summary>The mark of BOTH statement-scoped pending lists at one instant — the pre-op list
    /// (<see cref="DataBinder.PendingPreOps"/>: function activations, inline method invocations, function-bearing
    /// subscript temps) and the object-property op list (<see cref="DataBinder.OoPendingPropertyOps"/>).</summary>
    internal readonly record struct PendingMark(int PreOps, int PropertyOps);

    /// <summary>The pending-list mark for the host chokepoints (the suffix-drain protocol). ⛔ IT MARKS BOTH LISTS
    /// (kb/Work PB987): a per-evaluation window drains everything that activates while it binds — a function, an
    /// inline invocation AND an object-property GET — so none of the three can be hoisted out of a condition that
    /// is evaluated repeatedly or conditionally. It marked only the pre-op list until PB987, and a property
    /// reference in <c>PERFORM UNTIL P OF S &gt; 3</c> was fetched ONCE for the whole loop.</summary>
    internal PendingMark Mark => new(Pending.Count, ctx.Data.OoPendingPropertyOps.Count);

    /// <summary>Bind one user-function reference (the <see cref="BindIntrinsicCore"/> dispatch target for a
    /// REPOSITORY-declared name, which per §12.3.8.2 GR12 refers to the user function and never a same-named
    /// intrinsic): resolve the signature, bind the arguments in the §8.4.3.2.4 GR5 manner, synthesize the
    /// result temporary, register the hoisted activation, and return the temp-reading expression.</summary>
    internal BoundExpr UdfBindCall(string name, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        // INTRODUCTION gate: user-defined functions are COBOL-2002+ (§9.4 / §12.3.8; 0900 below 2002). It fires on
        // RECOGNITION — a below-2002 UDF reference is an edition violation independent of whether the function is
        // DEFINED (a locate miss is 1505 / EC-FUNCTION-NOT-FOUND). A bound-arm gate on the hoisted BoundCallProgram
        // loses it when the reference errors before the hoist (UdfInvocationTests.BinderGate_0900_At85), so it stays
        // BIND-TIME here until Step 14h moves ALL introduction gates to the presence-based post-bind parse-arm
        // (CI-red fix, 2026-07-09).
        ConstructRegistry.Check(ctx.Edition.Edition, ctx.Edition, Constructs.UserFunctionInvocation2002,
            $"FUNCTION {name.ToUpperInvariant()}");

        if (host.UserFunctions is null || !host.UserFunctions.TryGetValue(name, out var fn))
        {
            ctx.Edition.Error("COBOLNET1505",
                $"FUNCTION {name.ToUpperInvariant()} is declared in the REPOSITORY paragraph but the compilation "
                + "group contains neither a FUNCTION-ID definition nor a FUNCTION-ID … IS PROTOTYPE for it — "
                + "declare a function prototype (ISO §11.5 / §12.3.8.3 SR10) so its signature is available for a "
                + "separately-compiled target, or provide the definition in this group");
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {name}");
        }
        return UdfActivate(name, fn, argCtxs, pointer: null);
    }

    /// <summary>Bind one function-identifier written with FUNCTION-POINTER-NAME-1 (ISO §8.4.3.2; kb/Work PB847 —
    /// the consumer SET Format 8 landed without). §8.4.3.2.4 GR4: "the function prototype specified in the TO
    /// phrase of the USAGE clause in the definition of function-pointer-name-1 is used to determine the
    /// characteristics of the activated element and the function to be activated", and GR1 takes the result's
    /// description from that prototype's RETURNING item — so this is the prototype activation of
    /// <see cref="UdfBindCall"/> with ONE difference: the activated function is whichever one the pointer holds
    /// at run time (GR6c), NULL raising EC-FUNCTION-PTR-NULL. <paramref name="pointer"/> is the resolved
    /// function-pointer item; the §8.4.3.2.3 SR4 category test was made by the caller.</summary>
    internal BoundExpr UdfBindPointerCall(Place pointer, IReadOnlyList<Core.FunctionArgumentContext> argCtxs)
    {
        string name = pointer.Item.CobolName ?? "function-pointer";
        // The same RECOGNITION gate as the prototype form (the FUNCTION-POINTER usage is itself 2014-gated at its
        // declaration, so below 2002 this reference is already one edition violation deep).
        ConstructRegistry.Check(ctx.Edition.Edition, ctx.Edition, Constructs.UserFunctionInvocation2002,
            $"FUNCTION {name.ToUpperInvariant()}");
        // The mandatory `[TO] function-prototype-name-1` operand (§13.18.60.2, unbracketed) is screened at the
        // declaration (COBOLNET1958); an item missing it has already been reported, so there is no prototype to
        // take GR4's characteristics from and nothing further to say here.
        if (pointer.Item.Pic?.RestrictedPrototypeName is not { } proto)
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {name}");
        if (host.UserFunctions is null || !host.UserFunctions.TryGetValue(proto, out var fn))
        {
            ctx.Edition.Error("COBOLNET1505",
                $"FUNCTION {name.ToUpperInvariant()}: function-pointer '{name}' is restricted to function-prototype "
                + $"'{proto.ToUpperInvariant()}', but the compilation group contains neither a FUNCTION-ID "
                + "definition nor a FUNCTION-ID … IS PROTOTYPE for it — the prototype supplies the characteristics "
                + "of the activated function (ISO §8.4.3.2.4 GR4) and the result's description (GR1)");
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {name}");
        }
        return UdfActivate(name, fn, argCtxs, new BoundFieldOperand(pointer));
    }

    /// <summary>⛔ THE ONE PROTOTYPE ACTIVATION — both function-identifier forms that name a prototype
    /// (function-prototype-name-1 and, through its USAGE TO phrase, function-pointer-name-1) bind their
    /// arguments, result temporary and hoisted activation HERE, so the §14.8.2 / §14.8.3 conformance and the
    /// GR5 argument manner cannot drift between them. <paramref name="pointer"/> null activates the prototype's
    /// own externalized function (GR3); non-null activates the function the pointer holds (GR6c).</summary>
    private BoundExpr UdfActivate(string name, UserFunctionSignature fn,
        IReadOnlyList<Core.FunctionArgumentContext> argCtxs, BoundFieldOperand? pointer)
    {
        if (fn.Returning is null)
            // Ill-formed function definition — COBOLNET1507 already reported once at the unit.
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {name} RETURNING");

        // The category-carrying result channel (§8.4.3.2.4 GR1 — the temp's "description, class, and
        // category" ARE the RETURNING item's; §14.2.2 SR5 places NO category restriction on a function's
        // RETURNING item): elementary fixed-point numeric, alphanumeric/alphabetic, numeric-edited, and
        // national results — and a character-form GROUP (its full subtree cloned into the temp, the image
        // crossing the boundary via AsImage/FromImage) — are carried end-to-end: the temp clones the full
        // description, every operand chokepoint maps the read to a BoundFieldOperand (category drives
        // MOVE Table-16 legality, relation class dispatch, DISPLAY rendering, and the LENGTH fold), and
        // the CALL-ABI RETURNING delivery ships strings through CobolArgAdapt.StoreReturn(string). The
        // remaining shapes stay STAGED by name (§1.4 — loud, never silently wrong): see UdfReturningResidue.
        if (UdfReturningResidue(fn.Returning) is { } residue)
        {
            ctx.Edition.Error("COBOLNET1510",
                $"FUNCTION {name.ToUpperInvariant()}: {residue} (the result temporary's category channel, "
                + "ISO §8.4.3.2.4 GR1 / §14.8.3)");
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {name} RETURNING category");
        }

        // Arguments: one typed operand per argument parse tree, through the SAME BindArgOperand the intrinsic
        // path uses (the ONE argument pipeline). NO table(ALL) expansion here — §9.4 (:12529): "arguments and
        // returned values for user-defined functions may not use the word ALL as a subscript" (an ALL subscript
        // fails resolution and stays a loud named operand). An OMITTED argument (kb/Work PB757 — the third arm of
        // the omitted-argument model, beside CALL's and INVOKE's) is a null operand here, checked against
        // §8.4.3.2.3 SR9 below where the formal is known.
        var operands = new List<BoundOperand?>();
        foreach (var a in argCtxs)
            operands.Add(a.OMITTED() is not null ? null : host.Intrinsic.BindArgOperand(a, nullAdmitting: true));

        // Positional correspondence (§14.8.2.1): one argument per USING formal, "with the exception of trailing
        // formal parameters that are specified with an OPTIONAL phrase in the procedure division header of the
        // activated element and omitted from the list of arguments" — the callee's program-ABI adapters answer a
        // missing trailing slot as the omitted carrier (§8.4.3.2.4 GR7), exactly as they do for CALL.
        if (operands.Count > fn.Formals.Count || fn.Formals.Skip(operands.Count).Any(f => !f.Optional))
        {
            ctx.Edition.Error("COBOLNET1506",
                $"FUNCTION {name.ToUpperInvariant()} takes {fn.Formals.Count} argument(s); {operands.Count} "
                + "given — arguments correspond positionally to the function's PROCEDURE DIVISION USING "
                + "formals, and only trailing OPTIONAL formals may be omitted (ISO §14.8.2.1)");
            return BoundExprError.Refused(ctx.Edition, $"FUNCTION {name} arity");
        }

        // ⛔ §8.4.3.2.3 SR13 — "If function-prototype-name-1 or function-pointer-name-1 is specified, the rules for
        // conformance specified in 14.8.2, Parameters and 14.8.3, Returning items, apply" — asked of THE ONE argument
        // half of §14.8.2 the Format-2 CALL asks too (ParameterConformance; kb/Work PB1418 / PB1115). This binder
        // used to re-implement CALL's argument binding privately and skip it, so a PIC X(4) identifier aliased a
        // PIC 9(4) BY REFERENCE formal and an OBJECT REFERENCE C1 ONLY argument reached an OBJECT REFERENCE C1
        // formal with no diagnostic. §14.8.3 needs no screen here: the result temporary IS a clone of the
        // RETURNING item (GR1), so the returning pair conforms by construction.
        var site = new ActivationSite($"FUNCTION {name.ToUpperInvariant()}", "§8.4.3.2.3 SR13",
            DiagnosticCatalog.FunctionArgumentConformance);
        var callArgs = new List<BoundCallArg>(operands.Count);
        for (int i = 0; i < operands.Count; i++)
        {
            if (operands[i] is not { } operand)
            {
                // §8.4.3.2.3 SR9: "If the word OMITTED is specified, the OPTIONAL phrase shall be specified for the
                // corresponding formal parameter." The argument then crosses as the null carrier (§14.9.4.4 GR11's
                // shape, which CallEmitter renders for every Omitted BoundCallArg) and GR7 holds in the function.
                if (!fn.Formals[i].Optional)
                {
                    ctx.Edition.Error(DiagnosticCatalog.FunctionOmittedNeedsOptional,
                        $"FUNCTION {name.ToUpperInvariant()} argument {i + 1}: OMITTED corresponds to formal "
                        + $"parameter '{fn.Formals[i].Item.CobolName}', which the function's procedure division "
                        + "header does not describe with the OPTIONAL phrase (ISO §8.4.3.2.3 SR9)");
                    return BoundExprError.Refused(ctx.Edition, $"FUNCTION {name} argument {i + 1} OMITTED");
                }
                callArgs.Add(new BoundCallArg(CobolPassMode.Reference, null, null, Omitted: true) { Formal = fn.Formals[i].Item });
                continue;
            }
            var formal = fn.Formals[i];
            // §8.4.3.2.3 SR10: when the formal corresponding to argument-1 is specified with a BY VALUE phrase,
            // argument-1 shall be of class numeric, object, or pointer. Checked HERE (the one place the
            // formal↔argument pairing exists); the header side's SR2 already restricted the FORMAL. The class is
            // the ONE answer CALL's §14.9.4.3 SR22 screen asks (ParameterConformance.ValueArgumentClass) — this
            // screen used to test the item's CATEGORY, and a USAGE INDEX item (category numeric in the storage
            // model, class index by §13.18.60.4 GR10) passed it (kb/Work PB1418). Fail-open on an undecidable
            // class, exactly as the CALL screen does.
            if (formal.ByValue && ParameterConformance.ValueArgumentClass(operand)
                    is { } cls and not (CobolClass.Numeric or CobolClass.Object or CobolClass.Pointer))
            {
                ctx.Edition.Error("COBOLNET1554",
                    $"FUNCTION {name.ToUpperInvariant()} argument {i + 1} is of class "
                    + $"{cls.ToString().ToLowerInvariant()}; an argument passed to a BY VALUE formal parameter "
                    + "shall be of class numeric, object, or pointer (ISO §8.4.3.2.3 SR10)");
                return BoundExprError.Refused(ctx.Edition, $"FUNCTION {name} argument {i + 1} BY VALUE class");
            }
            if (UdfArg(operand, argCtxs[i], formal) is not { } arg)
            {
                // The operand refused to bind (the segment parser or resolver already classified it — an
                // unresolvable name, an ALL subscript) — name that ACTUAL shape, never a message claiming a legal
                // form is illegal. Every argument FORM §8.4.3.2.3 SR8 names has an arm in UdfArg.
                string what = operand is BoundOperandError err
                    ? err.Feature
                    : "this argument form (ISO §8.4.3.2.3 SR8 admits an identifier, a literal, a boolean "
                      + "expression, or an arithmetic expression)";
                ctx.Edition.Error("COBOLNET1506",
                    $"FUNCTION {name.ToUpperInvariant()} argument {i + 1}: {what} is not yet supported for "
                    + "user-defined function activation");
                return BoundExprError.Refused(ctx.Edition, $"FUNCTION {name} argument {i + 1}");
            }
            // §8.4.3.2.3 SR14: with a BY REFERENCE formal and a bit data item argument, the argument "shall be
            // described such that it is aligned on a byte boundary", with literal-only subscripts and ref-mod
            // leftmost position — the SAME proof CALL's §14.9.4.3 SR6 asks, so it is the same method.
            if (!formal.ByValue && operand is BoundFieldOperand { Place: var bitPlace } && BitLayout.IsBitItem(bitPlace.Item))
                host.Params.ScreenBitAlignment(bitPlace, DiagnosticCatalog.FunctionBitAlignment,
                    $"FUNCTION {name.ToUpperInvariant()} argument {i + 1}", "§8.4.3.2.3 SR14");
            host.Params.CheckArgument(formal.Item, arg, i + 1, site);
            callArgs.Add(arg);
        }

        // The caller-side result temporary (§8.4.3.2.4 GR1 :6963 — "the description, class, and category of
        // the temporary data item is that specified by the description in the linkage section of the item
        // specified in the RETURNING phrase"), declared like any other item via the Roots pipeline. A GROUP
        // model deep-clones its subtree (CreateCompilerTemp's group leg) so the FieldEmitter declares the
        // struct with its AsImage/FromImage codec — the CALL boundary's group crossing form.
        var temp = ctx.Data.CreateCompilerTemp(fn.Returning, "__FNRES-", "__fnres", name);
        if (ctx.Refs.ResolveItem(temp) is not { } tempPlace)
            return BoundExprError.Unbuilt(ctx.Edition, $"FUNCTION {name} result temporary");

        // The activation names the callee by its EXTERNALIZED name — the key ProgramRegistry holds it under
        // (§8.3.2.2 2); ordinarily identical to fn.Name, and the AS literal when FUNCTION-ID wrote one, PB303).
        // Through a function-pointer the callee is whatever the pointer HOLDS when the activation runs (GR6c):
        // the pointer operand rides DynamicName and the emitter's pointer arm reads its carrier.
        Pending.Add(pointer is null
            ? new BoundCallProgram(fn.Externalized, null, callArgs, tempPlace, null, null) { IsFunction = true }
            : new BoundCallProgram(null, pointer, callArgs, tempPlace, null, null)
                { IsFunction = true, IsPointerTarget = true });
        // The reading expression: a BoundNumRef over the temp's Place. Every general-operand chokepoint
        // (MOVE source, DISPLAY, relation operands, function arguments — IntrinsicBinder.OperandOf) maps it
        // to a BoundFieldOperand, whose Place.Item carries the cloned category into Table-16 legality, the
        // relation class dispatch, and the LENGTH fold. In a genuinely NUMERIC context (COMPUTE/arithmetic)
        // the temp behaves exactly like a same-category data item — the §14.9.25.4 GR6 unsigned-integer
        // decode for alphanumeric/national, the GR5 de-edit for numeric-edited (NumericRenderer.FieldNum).
        return new BoundNumRef(tempPlace);
    }

    /// <summary>The staged-loud RETURNING shapes (COBOLNET1510) — null when the category-carrying result
    /// channel handles the description end-to-end. ISO §14.2.2 SR5 places NO category restriction on a
    /// function's RETURNING item (any level-01/77 LINKAGE entry without BASED/REDEFINES), and §8.4.3.2.4 GR1
    /// clones its description into the caller temp — so every shape named here is an IMPLEMENTATION residue
    /// staged loud (§1.4), never a conformance rule. Supported: elementary fixed-point numeric,
    /// alphanumeric/alphabetic, numeric-edited, national; image-form groups (every leaf
    /// <see cref="DataItem.ElementImageCapable"/> — character-stored, or any pinned numeric byte form:
    /// zoned DISPLAY, binary, packed, COMP-5, IEEE float, INDEX). Staged: FLOAT
    /// (the CALL-boundary string carrier has no float write half — CallEmitter.CallStringWrite renders a raw
    /// string store into the double field), BOOLEAN (the §8.8.2 boolean-expression channel has no
    /// function-result arm — admitting it would half-wire: MOVE/relations would work while IF f(x) and
    /// COMPUTE Format 2 would not), pointer/object/index classes (§8.8.4-restricted reference sets), and the
    /// group residues (strongly-typed identity, internal REDEFINES, variable-length, and a pointer- or
    /// object-class LEAF — the only leaf form with no character image under either axis). ⛔ A byte-form
    /// numeric leaf (binary/packed/COMP-5/float/INDEX) is NOT a residue: it is carried by the
    /// <c>ElementImageCapable</c> arm above, and this sentence said otherwise until kb/Work PB199.</summary>
    private static string? UdfReturningResidue(DataItem ret)
    {
        if (ret.IsGroup)
        {
            for (var stack = new Stack<DataItem>([ret]); stack.Count > 0;)
            {
                var d = stack.Pop();
                if (d.StrongType)
                    return "a strongly-typed group RETURNING item is not yet carried — the §8.5.3.3 same-type "
                        + "identity does not survive the caller-side temp clone (ISO §14.8.3.2; a named residue)";
                if (!ReferenceEquals(d, ret) && d.RedefinesTargetName is not null)
                    return "a group RETURNING item containing an internal REDEFINES is not yet carried — the "
                        + "temp clone precedes no REDEFINES re-classification (ISO §13.18.44; a named residue)";
                if (d.OccursSpec is { } os && (os.DependingName is not null || os.IsDynamic))
                    return "a variable-length (OCCURS DEPENDING/DYNAMIC CAPACITY) group RETURNING item is not "
                        + "yet carried (ISO §8.5.1.12 / §14.8.3.2 variable-length-group conformance; a named residue)";
                // ⛔ THE DERIVED IMAGE PREDICATE, never a hand-rolled usage union. This screen asks exactly one
                // question — does the leaf have a character image the group codec can carry across the
                // activation boundary? — and DataItem.ElementImageCapable is where that question is answered
                // for every consumer (the group codec, the REDEFINES backing, the record window). It read
                // `{ Numeric, IsFloat: false, Usage: Display }` — the DISPLAY-only union from before V59 — so it
                // went on rejecting binary / packed / COMP-5 / float / INDEX group leaves years after those
                // forms got their pinned bytes (V59, kb/Work PB164 waves 1–2, R40). What is genuinely left is
                // the pointer/object class, which has no character image under EITHER axis.
                if (d.IsElementary && !d.ElementImageCapable)
                    return $"a group RETURNING item with a pointer- or object-class leaf ('{d.CobolName ?? "FILLER"}') "
                        + "is not yet carried — a data-pointer, program-pointer, function-pointer or "
                        + "object-reference leaf has no character image to cross the activation boundary "
                        + "(ISO §8.5.2.6 / §13.18.60.4 GR23 reference restrictions; a named residue)";
                foreach (var c in d.Children) stack.Push(c);
            }
            return null;   // a character-form group — the implemented group leg
        }
        return ret.Pic switch
        {
            null => "an undescribed RETURNING item",   // childless, PICTURE-less — already diagnosed at the entry
            { Category: PicCategory.Numeric, IsFloat: true } =>
                "a FLOAT RETURNING item (COMP-1/COMP-2/FLOAT-SHORT/-LONG/-EXTENDED) is not yet carried — the "
                + "CALL-boundary string carrier has no float write half (ISO §14.8.3.3 usage-identical "
                + "conformance; the float-RETURNING descriptor, a named residue)",
            { Category: PicCategory.Numeric, Usage: Usage.Index } =>
                "an index RETURNING item is not a carried result category (ISO §13.18.60.3 SR10 — only a "
                + "SEARCH or SET statement, a relation condition, an intrinsic-function or inline-method "
                + "argument, or a procedure-division / CALL / INVOKE USING phrase references class index; "
                + "a named residue)",
            { Category: PicCategory.Numeric } => null,                 // the original fixed-point leg
            { Category: PicCategory.Alphanumeric } => null,           // alphanumeric + alphabetic (§8.4.2 class alphanumeric)
            { Category: PicCategory.NumericEdited } => null,          // the edited image carries as its mask'd string
            { Category: PicCategory.National } => null,               // rides the Step-5 national category channel
            { Category: PicCategory.Boolean } =>
                "a BOOLEAN RETURNING item is not yet carried — the §8.8.2 boolean-expression channel "
                + "(IF simple-boolean-condition, COMPUTE Format 2) has no function-result arm; carrying only "
                + "the MOVE/relation legs would half-wire the category (a named residue)",
            { Category: PicCategory.Pointer } =>
                "a data-pointer RETURNING item is not yet carried across the function-activation boundary "
                + "(ISO §8.5.2.6 / §13.18.60.4 GR23 reference restrictions; a named residue)",
            { Category: PicCategory.ObjectReference } =>
                "an object-reference RETURNING item for a FUNCTION-ID is not yet carried (the §14.8.3.3 "
                + "object-reference conformance legs; a named residue)",
            _ => "this RETURNING item's category is not yet carried (a named residue)",
        };
    }

    /// <summary>One bound argument in its ISO §8.4.3.2.4 GR5 manner, for every argument FORM §8.4.3.2.3 SR8 names:
    /// <list type="bullet">
    /// <item>c) a formal specified BY VALUE ⇒ BY VALUE for every shape — the caller snapshots the value and the
    /// callee adopts the §14.2.3 GR10 detached copy (SR10 has already screened the class).</item>
    /// <item>a) an identifier "that is permitted as a receiving operand, other than an object property or object
    /// data item" (<see cref="IsReferenceIdentifier"/>) ⇒ BY REFERENCE over the caller's storage.</item>
    /// <item>b) "a literal, an arithmetic expression, a boolean expression, an object property, object data item, or
    /// any identifier that is not permitted as a receiving operand" ⇒ BY CONTENT: a private-copy cell (the runtime
    /// <c>CobolArgAdapt</c> profile adaptation realizes the §14.2.3 GR9 copy-in conformance to the formal). A
    /// CONSTANT RECORD item is such an identifier (§13.18.15.3 SR2), so the function's stores into its formal no
    /// longer overwrite the structured constant (kb/Work PB1418). A boolean expression rides the same BY CONTENT
    /// boolean value channel as CALL's boolean-expression-1 (<see cref="BoundCallArg.ContentBool"/>); a figurative
    /// constant or ALL literal is a literal wherever 'literal' appears in a rule (§8.3.3.6.3 SR1), and its crossing
    /// fills the formal (<c>CallEmitter.ArgText</c>, §8.3.3.6.4 GR2).</item>
    /// </list>
    /// Null = the operand itself refused to bind (the caller reports).</summary>
    /// <remarks>Every arm records <see cref="BoundCallArg.Formal"/>: §14.2.3 GR9's second branch names "a
    /// function" outright, so a UDF argument's crossing into a numeric formal is the ACTIVATING element's
    /// COMPUTE (kb/Work PB640), and this binder has always had the formal in hand.</remarks>
    private BoundCallArg? UdfArg(BoundOperand op, Core.FunctionArgumentContext a, LinkageFormal formal)
    {
        BoundCallArg? arg = op switch
        {
            BoundFieldOperand f => new BoundCallArg(
                formal.ByValue ? CobolPassMode.Value
                : IsReferenceIdentifier(a, f.Place) ? CobolPassMode.Reference
                : CobolPassMode.Content, f.Place, null),
            // A class-boolean value has no BY VALUE crossing (SR10 refused it above).
            BoundBoolOperand b => new BoundCallArg(CobolPassMode.Content, null, null) { ContentBool = b.Expr },
            // ZERO into a BY VALUE formal is the numeric value 0 (§8.3.3.6.4 GR4) — the value channel's form.
            BoundFigurative { Kind: 'Z' } when formal.ByValue
                => new BoundCallArg(CobolPassMode.Value, null, new BoundNumericLiteral("0")),
            BoundNumericLiteral or BoundStringLiteral or BoundFigurative or BoundAllLiteral or BoundComputedOperand
                => new BoundCallArg(formal.ByValue ? CobolPassMode.Value : CobolPassMode.Content, null, op),
            // §8.4.3.10.3 SR1 a): the predefined NULL "as an argument in … a function-prototype format function
            // activation" — it references no storage-section item, so it crosses BY CONTENT (BY VALUE to a BY VALUE
            // formal) as the storage-free NULL carrier CallEmitter renders for every lane (kb/Work PB1630, PB1427).
            BoundPredefinedNull => new BoundCallArg(formal.ByValue ? CobolPassMode.Value : CobolPassMode.Content, null, op),
            _ => null,
        };
        return arg is null ? null : arg with { Formal = formal.Item };
    }

    /// <summary>§8.4.3.2.4 GR5 a)'s test — is argument <paramref name="a"/>, bound to <paramref name="place"/>, "an
    /// identifier that is permitted as a receiving operand, other than an object property or object data item"?
    /// Each clause is asked where it is answered: the argument must BE an identifier (a sole data reference that
    /// resolves to a data item — a function-identifier, keyword-omitted or not, is "not specified as a receiving
    /// operand" by §8.4.3.2.3 SR1); an object property and an object data item are named exceptions; and the
    /// receiving-operand prohibitions are the ONE table the receiving chokepoint reports
    /// (<see cref="ExpressionBinder.PermitsReceiving"/>).</summary>
    private bool IsReferenceIdentifier(Core.FunctionArgumentContext a, Place place) =>
        CobolNet.Frontend.Expressions.SoleOperand.DataRef(a.arithmeticExpression()) is { } dref
        && ctx.Refs.Probe(dref) is not null
        && !ctx.Refs.IsObjectPropertyReference(dref)
        && !ctx.Data.OoIsObjectData(place.Item)
        && host.Expr.PermitsReceiving(dref, place);

    /// <summary>⛔ THE ONE DRAIN OF THE PENDING PRE-OP SUFFIX, and the only place the list is mutated on the way
    /// out. Takes everything registered past <paramref name="mark"/> and REMOVES it, so exactly one carrier ends
    /// up owning each activation — the statement hoist, the per-evaluation condition wrapper, or the
    /// per-evaluation operand wrapper. Null when nothing was registered, which is what lets every caller return
    /// its subject UNCHANGED and keeps the generated source byte-identical on the function-free path.
    /// <para>It is one method because the three callers differ ONLY in the node they wrap the drained suffix in;
    /// three copies of the take-and-remove pair is the shape where one of them eventually forgets the remove and
    /// double-activates.</para>
    /// <para>⛔ IT IS ALSO WHERE AN OPERAND-EVALUATION STEP IS STAMPED (kb/Work PB892, PB1432). Everything it drains
    /// was WRITTEN INSIDE the statement now binding, so every step is marked
    /// <see cref="BoundStatement.OperandEvaluation"/> — an activation (§14.9.33.4 GR2 a) 2.: the applicable statement
    /// is "the statement in which the inline invocation or function invocation was specified") and a §15.4
    /// subscript temporary store alike (GR2 a) 1.: "the one in which the exception condition was raised"; before
    /// PB1432 an unmarked store's size-error landing wrote a <c>goto</c> inside a short-circuited operand's lambda,
    /// CS0159, and fell back INTO the statement when hoisted). An <see cref="IActivatingStatement"/> is also given THAT statement's
    /// §7.3.25 checking profile (§14.9.18.4 GR1 b) — "if checking for that exception condition is enabled in the
    /// activating runtime element"). The profile cannot wait for <c>EcBinder.EcWrap</c>'s stamp: a per-evaluation
    /// window's activations live inside a CONDITION or an OPERAND, which that statement-shaped walk never enters,
    /// so before PB892 they carried an empty profile and every condition they propagated was discarded.</para></summary>
    private List<BoundStatement>? DrainPending(int mark, List<BoundStatement>? leading = null)
    {
        var calls = Pending;
        var taken = leading ?? [];
        if (calls.Count > mark)
        {
            taken.AddRange(calls.GetRange(mark, calls.Count - mark));
            calls.RemoveRange(mark, calls.Count - mark);
        }
        if (taken.Count == 0) return null;
        EcCheckingProfile? profile = null;
        for (int i = 0; i < taken.Count; i++)
        {
            var step = taken[i] with { OperandEvaluation = true };
            if (step is IActivatingStatement a)
            {
                profile ??= ctx.EcState.Turn.ProfileAt(host.StatementLine);
                step = a.WithActivatorChecking(profile);
            }
            taken[i] = step;
            ctx.Data.OperandEvaluations++;
        }
        return taken;
    }

    /// <summary>Drain one per-evaluation window's suffix of BOTH lists (<see cref="Mark"/>): the object-property
    /// GETs first — the statement-level order, where the property wrap is the OUTER sequence, so a property
    /// argument's GET precedes the activation that consumes its temp — then the pre-ops, in registration order.
    /// Every property reference a window drains is a SENDING operand (a condition or an arithmetic expression
    /// has no receiving operand), so each one is a §8.4.3.9.4 GR1 GET (<c>OoBinder.OoDrainPropertyGets</c>).</summary>
    private List<BoundStatement>? DrainPerEvaluation(PendingMark mark) =>
        DrainPending(mark.PreOps, host.Oo.OoDrainPropertyGets(mark.PropertyOps));

    /// <summary>Drain THIS statement's pending function activations (registered while the statement bound)
    /// into the hoisted <see cref="BoundSequence"/>: every activation is a PRE-op — a function-identifier
    /// is never a receiving operand (§8.4.3.2.3 SR1), so unlike property references there is no polarity
    /// classification and no post-ops. Runs INSIDE the property-op wrap at the BindStatement chokepoint, so
    /// a property-reference argument's GET (a pre-op of the OUTER wrap) still precedes the activation that
    /// consumes its temp. The hoist is EXACT here: every conditionally- or repeatedly-evaluated window
    /// already drained its own suffix into a per-evaluation <see cref="BoundUdfEvaluated"/> wrapper BEFORE the
    /// statement completed
    /// binding, so what remains pending is evaluated exactly once per statement execution (a plain operand,
    /// a sole IF condition, a TIMES count — §14.9.28 GR7, a first-level VARYING FROM — GR13a init, an
    /// EVALUATE subject occurrence).</summary>
    internal BoundStatement UdfWrapCalls(BoundStatement core, int mark) =>
        DrainPending(mark) is { } taken ? new BoundSequence([.. taken, core]) : core;

    /// <summary>Attach the function activations registered while <paramref name="cond"/> bound (those past
    /// <paramref name="mark"/>) to the condition itself as a per-evaluation <see cref="BoundUdfEvaluated"/>
    /// wrapper — the §8.4.3.2.4 GR1/GR6a "value is determined when the function is referenced at runtime" semantics for a
    /// window the statement evaluates conditionally or repeatedly (§8.8.4.13 r2). The drained suffix leaves
    /// the pending list, so the statement-level hoist never double-activates them. No pending growth returns
    /// the condition unchanged (zero cost for the UDF-free path).</summary>
    internal BoundCondition UdfAttachPerEvaluation(BoundCondition cond, PendingMark mark) =>
        DrainPerEvaluation(mark) is { } taken ? new BoundUdfEvaluated(taken, cond) : cond;

    /// <summary>The EXPRESSION twin of <see cref="UdfAttachPerEvaluation"/>: attach the activations registered
    /// while <paramref name="e"/> bound (those past <paramref name="mark"/>) to the OPERAND as a
    /// <see cref="BoundUdfEvaluatedExpr"/>, for a window the statement evaluates repeatedly — ISO §14.9.28.4
    /// GR12's "item identification … is done each time the content … is used in a setting or augmenting
    /// operation" (§8.4.3.2.4 GR1/GR6a). The drained suffix leaves the pending list, so the statement-level
    /// hoist never double-activates it; no pending growth returns the expression unchanged, so the UDF-free
    /// path costs nothing and the generated source is byte-identical.
    /// <para>⛔ IT REPLACED A REFUSAL, NOT A SILENCE (kb/Work PB437). These two operand positions used to call
    /// the (since deleted) narrowed residue stage and REJECT the program (COBOLNET1509) while the adjacent
    /// first-level FROM and the UNTIL condition accepted the same construct — a capability limit shipped as a
    /// diagnostic, pinned green by two tests. The carrier the condition already used is what the augment and
    /// re-initialization sites needed too.</para></summary>
    internal BoundExpr UdfAttachPerEvaluation(BoundExpr e, PendingMark mark) =>
        DrainPerEvaluation(mark) is { } taken ? new BoundUdfEvaluatedExpr(taken, e) : e;

    /// <summary>EXIT FUNCTION (pre-2023 editions — introduced 2002 with user functions, REMOVED by 2023,
    /// Annex E.2 :49036; the <c>exit-function-window</c> registry row flags 0900/0902 at the window edges
    /// via the version-conformance pass, mirroring EXIT METHOD): inside a function definition it is the
    /// function-return synonym — equivalent to GOBACK (the §14.9.18.4 GR5 semantics: the activation
    /// terminates and the RETURNING item's value becomes the function result); outside one it violates its
    /// placement rule (the 0827 EXIT-family placement band). The optional RAISING tail stages exactly like
    /// GOBACK RAISING (§14.9.18 GR — re-raised in the activator).</summary>
    internal BoundStatement UdfBindExitFunction(Core.ExitStatementContext e)
    {
        // The EXIT statement's placement rule for the pre-2023 FUNCTION format, asked of the ONE bind-position
        // probe every other EXIT format now asks (kb/Work PB403) — §14.2.2 SR10's source-element kind, not a
        // per-verb predicate. A function PROTOTYPE definition is a function procedure division too: EXIT FUNCTION
        // means "return from this function", and the prototype's procedure division is one.
        if (ctx.Enclosing.SourceElement is not (SourceElementKind.FunctionDefinition or SourceElementKind.FunctionPrototype))
        {
            return BoundRejected.Report(ctx.Edition, "COBOLNET0827",
                "EXIT FUNCTION may be specified only in a function definition (the pre-2023 §14.9.14 "
                + "function form of the EXIT statement; this is not a function procedure division)");
        }
        if (e.raisingPhrase() is { } raising)
            return host.Ec.EcBindRaising(raising, e.Start.Line, EcRaiseSite.Exit("EXIT FUNCTION")) is { } r
                ? new BoundGoback(null, r)
                : new BoundUnsupported("EXIT FUNCTION RAISING identifier (the exception-object form of the RAISING phrase — the OO wave)");
        return new BoundGoback(null);
    }

}
