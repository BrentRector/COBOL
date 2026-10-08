// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

using CobolNet.Compiler.Oo;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;
using CobolNames = CobolNet.Runtime.CobolNames;   // the namespace holds a CobolClass too, which would hide the compiler's own CobolClass enum

/// <summary>
/// The OO binder (P7 Step 10s — the OO half converts LAST, behind the OO goldens + method-scope tests):
/// INVOKE §14.9.23 in every form (class/NEW · instance/interface · SELF/SUPER §8.4.3.8 · factory §11.4 ·
/// the D10 UNIVERSAL dynamic path with conformance descriptors) with the D6 USING/RETURNING marshaling
/// (§14.8.2/§14.8.3 conformance), SET Format 5 object-reference assignment (§14.9.39, D-U7), the
/// §8.4.3.9.4 GR1–GR3 object-property desugar (<see cref="OoWrapPropertyOps"/> — invoked between the UDF
/// wrap and the EC wrap at the host BindStatement exit), and the D8 method-context returns
/// (GOBACK/EXIT METHOD → <c>BoundMethodReturn</c>). The ride-along bound records moved to
/// <c>Binding/Bound/BoundOo.cs</c> (records-only, the established rule). The OO HOST state
/// (OoClasses/OoCurrentClass/OoInFactory — set by the emitter's OO bind half) stays on
/// <see cref="StatementBinder"/> (set by <c>Oo/OoDriver</c> since P9 Step 4), read here via host edges that flip
/// at 10t; BindMethodRoster (the class-roster entry-point twin of Bind()) stays on the host with the
/// procedure table until the 10t ProcedureTableBuilder hoist.
/// </summary>
internal sealed partial class OoBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Drain THIS statement's pending object-property ops (registered by the ReferenceResolver
    /// fallback while the statement bound) into the §8.4.3.9.4 GR1–GR3 desugar: classify each temp's store
    /// polarity over the BOUND statement (BoundStores — the emitter-verified taxonomy), then
    /// GR1 (pure sending) = prepend the get-invoke; GR2 (write-only receiving) = append the set-invoke, get
    /// NOT invoked; GR3 (read-modify-write) = both around ONE temp. SR3/SR4 (:7380/:7382 — the needed
    /// accessor must exist, on the instance or factory roster per the reference form) check HERE, against
    /// the classified need, both COBOLNET0843. The taxonomy is TOTAL — a non-nullable answer from an exhaustive
    /// visitor, so a new bound statement cannot compile without deciding each Place's polarity, and nothing here
    /// guesses whether a side-effecting accessor runs (kb/Work PB1275).
    /// <para>⛔ WHERE THE STEPS GO (kb/Work PB2078). A reference the statement's binder CLAIMED as one of its receivers
    /// (<see cref="DataBinder.OoClaimInterleavedReceiver"/>: the arithmetic statements, every MOVE built by
    /// <c>MoveBinder.BindMoveOf</c> -- the written MOVE and the implicit moves of READ / RETURN … INTO, ACCEPT, UNSTRING and
    /// INITIALIZE -- STRING INTO and INSPECT identifier-1, whose emitters store their receivers one at a time) gets a
    /// <see cref="ReceiverBracket"/> under a <see cref="BoundReceiverBrackets"/> —
    /// its Prelude and GET just before ITS access, its SET just after ITS store (§14.7.7 4) b), §14.9.25.4 GR1). Every
    /// other reference keeps the statement-level shape, GETs and Prelude before the statement and SETs after it.</para></summary>
    public BoundStatement OoWrapPropertyOps(BoundStatement core, int mark)
    {
        var ops = ctx.Data.OoPendingPropertyOps;
        if (ops.Count <= mark) return core;
        var taken = ops.GetRange(mark, ops.Count - mark);
        ops.RemoveRange(mark, ops.Count - mark);

        List<BoundStatement> pre = [], post = [];
        List<ReceiverBracket> brackets = [];
        foreach (var op in taken)
        {
            // TOTAL over every bound statement (kb/Work PB1275): §8.4.3.9.3 SR5/SR6 admit a property wherever a
            // data item of its description may send or receive, so every statement that can carry one classifies it.
            var kind = BoundStores.StoreKindOf(core, op.Temp);
            bool needGet = kind == StoreKind.None || kind == StoreKind.ReadWrite;
            bool needSet = kind == StoreKind.Write || kind == StoreKind.ReadWrite;
            string where = $"'{op.PropName}' OF '{op.ReceiverName}'";
            var tempPlace = ctx.Refs.ResolveItem(op.Temp)!;

            // ⛔ kb/Work PB2078: a statement-level GET runs before the statement and SET after it, each rendering
            // identifier-3 afresh, so an object selected through a run-time value is one object at the GET and another
            // at the SET (`ADD 1 TO I, BAL OF AR(I)` stored AR(1)'s BAL + 1 into AR(2)), and neither is the receiver
            // §14.7.7 4) b) and §14.9.25.4 GR1 identify "as each data item is accessed". A statement whose emitter
            // stores its receivers one at a time claims them (DataBinder.OoClaimInterleavedReceiver) and the accessors
            // go around THAT receiver's store below; every other statement refuses the shape, as it did before PB1425
            // admitted the subscript.
            if (needSet && op.SelectedByValue && !op.Interleaved)
            {
                ctx.Edition.Error(DiagnosticCatalog.ReceivingReferenceNotImplemented,
                    $"the object-property reference {where} is a RECEIVING operand of a statement that does not store its "
                    + "receivers one at a time, and its object is selected by a run-time value (a data-name subscript or a "
                    + "function-identifier): identifying that object when the statement reaches it (ISO §14.7.7 4) b), "
                    + "§14.9.25.4 GR1) is implemented for the arithmetic statements, MOVE and the implicit moves "
                    + "(READ / RETURN … INTO, ACCEPT, UNSTRING INTO, INITIALIZE), STRING INTO and INSPECT identifier-1 only");
                continue;
            }

            // identifier-3's own evaluation first (§8.4.3.1.4 GR1 a)–c) before d)), then the GET — once, whichever
            // accessors the polarity needs: the SET of a receiving property uses the same evaluated receiver.
            List<BoundStatement> open = [.. op.Prelude], close = [];
            if (needGet && PropertyGet(op) is { } get) open.Add(get);
            if (needSet)
            {
                if (op.Set is null)
                    ctx.Edition.Error("COBOLNET0843",
                        $"the object-property reference {where} is a RECEIVING operand but the class has no "
                        + "SET property method (ISO §8.4.3.9.3 SR4 — WITH NO SET, or no accessor defined)");
                else
                    close.Add(new BoundInvoke(op.Form, op.ClassCsName, op.Receiver, op.Set.CsName, null,
                        [new BoundInvokeArg(op.Set.Binding!.Formals[0].Item, tempPlace, null, null, WriteBack: false)],
                        null, op.OwnerCsNameOf(op.Set)));
            }
            if (op.Interleaved) brackets.Add(new ReceiverBracket(op.Temp, [.. open.Select(Operand)], [.. close.Select(Operand)]));
            else
            {
                pre.AddRange(open);
                post.AddRange(close);
            }
        }
        if (pre.Count + post.Count + brackets.Count == 0) return core;
        // §8.4.3.9.4 GR1/GR2: each accessor is invoked "as though" by an INVOKE, but it is written as an OPERAND of
        // the statement, so a condition it propagates resumes after THAT statement — the operand-activation mark
        // UdfBinder.DrainPending gives a function reference (kb/Work PB892; §14.9.33.4 GR2 a) 2.). Its checking
        // profile is EcBinder.EcWrap's sequence stamp, which reaches these steps (and the brackets' through
        // BoundReceiverBrackets).
        ctx.Data.OperandEvaluations += pre.Count + post.Count + brackets.Sum(b => b.Open.Count + b.Close.Count);
        BoundStatement inner = brackets.Count == 0 ? core : new BoundReceiverBrackets(core, brackets);
        return pre.Count + post.Count == 0
            ? inner
            : new BoundSequence([.. pre.Select(Operand), inner, .. post.Select(Operand)]);
        static BoundStatement Operand(BoundStatement s) => s with { OperandEvaluation = true };
    }

    /// <summary>The §8.4.3.9.4 GR1 GET of one SENDING property reference — "as though" an INVOKE of the GET
    /// property method RETURNING the reference's temp — or null after SR3's diagnostic when the class has no GET.
    /// ONE builder for both places a GET is placed: the statement-level wrap (<see cref="OoWrapPropertyOps"/>) and
    /// a per-evaluation window (<see cref="OoDrainPropertyGets"/>).</summary>
    private BoundInvoke? PropertyGet(DataBinder.OoPendingPropertyOp op)
    {
        if (op.Get is null)
        {
            ctx.Edition.Error("COBOLNET0843",
                $"the object-property reference '{op.PropName}' OF '{op.ReceiverName}' is a SENDING operand but the "
                + "class has no GET property method (ISO §8.4.3.9.3 SR3 — WITH NO GET, or no accessor defined)");
            return null;
        }
        return new BoundInvoke(op.Form, op.ClassCsName, op.Receiver, op.Get.CsName, ctx.Refs.ResolveItem(op.Temp)!, null, op.Get.Binding!.Returning, op.OwnerCsNameOf(op.Get));
    }

    /// <summary>Drain the property references registered since <paramref name="mark"/> for a PER-EVALUATION
    /// window (<c>UdfBinder.UdfAttachPerEvaluation</c> — a PERFORM UNTIL / VARYING condition, a SEARCH WHEN, an
    /// EVALUATE object, a non-first AND/OR operand, a VARYING BY / AFTER FROM operand) and return their GETs.
    /// ISO §8.8.4.13 2): "Values are established for arithmetic expressions and functions if and when the
    /// conditions containing them are evaluated" — an object-property reference is an inline activation of its
    /// GET method, so it is fetched at EACH evaluation, and not at all when a short-circuit (§8.8.4.13 1)) never
    /// reaches it (kb/Work PB987: it used to be hoisted once for the whole statement). Every reference a window
    /// drains is SENDING — neither a condition nor an arithmetic expression has a receiving operand — so no
    /// store-polarity classification is needed and GR2's SET never arises here.</summary>
    internal List<BoundStatement> OoDrainPropertyGets(int mark)
    {
        var ops = ctx.Data.OoPendingPropertyOps;
        if (ops.Count <= mark) return [];
        var taken = ops.GetRange(mark, ops.Count - mark);
        ops.RemoveRange(mark, ops.Count - mark);
        var gets = new List<BoundStatement>(taken.Count);
        foreach (var op in taken)
        {
            gets.AddRange(op.Prelude);   // identifier-3's evaluation precedes its accessor (see OoPendingPropertyOp)
            if (PropertyGet(op) is { } get) gets.Add(get);
        }
        return gets;
    }


    // ── INVOKE (ISO §14.9.23; deep-dive D5) ─────────────────────────────────────────────────────────────────

    /// <summary>Bind one INVOKE: resolve the receiver (identifier-1 first, class-name-1 second — a data-name
    /// shadows a class-name at reference resolution), the LITERAL method name, and the call form against the
    /// pass-1 symbol table. Part-2 spine scope: <c>Class "NEW" RETURNING obj</c> and the no-arg instance call
    /// are LIVE; SELF/SUPER (slice 3b), factory calls (§11.4 slice), USING/RETURNING marshaling (slice 2),
    /// universal/dynamic dispatch (D10 wave) stage loud.</summary>
    public BoundStatement OoBindInvoke(Core.InvokeStatementContext inv)
    {
        // The INVOKE statement's own reading of the ONE invocation site (see OoBinder.InlineInvocation.cs):
        // its USING arguments and its written RETURNING identifier. §8.4.3.4.4 GR1 defines the inline form
        // as the equivalent INVOKE, so both syntaxes reach this same resolution and the same §14.8 checks.
        var site = InvocationSite.OfInvokeStatement(inv);
        // §14.9.23.3 SR9 — "Identifier-3 shall be an address-identifier or shall reference a data item defined in the
        // file, working-storage, local-storage, or linkage section": BY REFERENCE NULL is neither, refused by THAT rule
        // (it drew the §8.9 reserved-word diagnostic through `dataReference`) — CallBinder's SR3 arm is the twin
        // (kb/Work PB1427). BY CONTENT / BY VALUE NULL are the §8.4.3.10.3 SR1 a) method-invocation argument.
        foreach (var a in inv.invokeUsing()?.invokeArgument() ?? [])
            if (a.predefinedNull() is not null)
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.InvokeOperandSection,
                    "INVOKE … USING BY REFERENCE NULL: ISO §14.9.23.3 SR9 — identifier-3 \"shall be an address-identifier "
                    + "or shall reference a data item defined in the file, working-storage, local-storage, or linkage "
                    + "section\", and the predefined NULL (§8.4.3.7 / §8.4.3.10) is neither; pass it BY CONTENT or BY VALUE");
        // identifier-4 receives the result, and §8.4.3.5.3 SR2 forbids an object-view there (kb/Work PB1425).
        if (inv.invokeReturning()?.objectViewReceiver() is { } returningView)
            return RefuseObjectViewReceiver(returningView, "the RETURNING identifier of an INVOKE statement");
        // INVOKE (§14.9.23, OO) is a COBOL-2002 introduction; the edition gate fires on RECOGNITION in the
        // VersionConformancePass parse arm (VisitInvokeStatement), never on the BoundInvoke node this method
        // builds. It keyed on the node until kb/Work PB353, which was wrong BOTH ways: an INVOKE whose target
        // resolves to neither a data item nor a class returns COBOLNET0823 before any node exists (so a
        // below-2002 INVOKE named no edition at all), and BoundInvoke is equally the bound form of a synthesized
        // property get/set and of NEW / SELF-NEW, none of which is "the INVOKE statement".
        var target = inv.invokeTarget().objectReference();
        // identifier-1 written as a function-identifier or an inline invocation (kb/Work PB1425): bound ONCE, here, to
        // the temporary item it references, which then IS the receiver — a typed one through the instance path, a
        // universal one through the dynamic path, exactly as a data-name receiver of the same description.
        Place? computedReceiver = null;
        if (OoBindComputedObjectReference(target) is { } computed
            && (computedReceiver = OoObjectReferenceTemporary(computed, target, "COBOLNET0824",
                "identifier-1 shall be an object reference (ISO §14.9.23.3 SR1)")) is null)
            return BoundRejected.Reported(ctx.Edition);
        // The plain term — null for an inline invocation or an object-view, both of which are computed above.
        var atom = target.objectReferenceAtom()?.objectReferenceTerm();

        // The method selector: an alphanumeric/national literal binds statically (§14.9.23.3 SR2);
        // identifier-2 (a method name held in a data item) is legal ONLY through a UNIVERSAL receiver
        // (§14.9.23.3 SR7) — the D10 dynamic path, live as of the universal wave.
        if (inv.invokeMethodName().dataReference() is { } mref)
        {
            // §14.9.23.3 SR3: "If object-class-name-1 is specified, literal-1 shall be specified" — asked BEFORE the
            // receiver resolves as identifier-1, because a class-name is not a data item and resolving it as one
            // drew the resolver's false "is not defined" (kb/Work PB1136). A data-name shadows a class-name, so the
            // class-name reading is taken only for a name that is not a data item (the OoBindByReceiver partition).
            if (computedReceiver is null && atom?.dataReference() is { } cref && ctx.Refs.Probe(cref) is null
                && Compiler.Oo.OoNameResolution.Lookup(host.OoClasses, cref, cref.GetText(),
                    Compiler.Oo.OoNameResolution.Want.Class).Class is { } namedClass)
                return BoundRejected.Report(ctx.Edition, "COBOLNET0866",
                    $"INVOKE {namedClass.Name} {mref.GetText()}: when object-class-name-1 is specified, literal-1 "
                    + "shall be specified (ISO §14.9.23.3 SR3) — identifier-2 may be specified only when identifier-1 "
                    + "is a universal object reference (SR7)");
            // kb/Work PB1030: a reference that did not resolve is the resolver's diagnostic, never SR7's.
            Place? urecv = computedReceiver;
            if (urecv is null && atom?.dataReference() is { } uref
                && (urecv = host.Expr.ResolveSending(uref).PlaceOrReported(ctx.Edition)) is null)
                return BoundRejected.Reported(ctx.Edition);
            if (urecv?.Item.Pic is not { Category: PicCategory.ObjectReference, ObjectRef.IsUniversal: true })
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0866",
                    "INVOKE: identifier-2 (a method name held in a data item) is permitted only when "
                    + "identifier-1 is a UNIVERSAL object reference (ISO §14.9.23.3 SR7)");
            }
            // The resolver reported (or, for a deferred shape, PlaceOrReported reported) why it did not resolve —
            // a second "not resolvable to storage" error on top named no rule (kb/Work PB1030).
            if (host.Expr.ResolveSending(mref).PlaceOrReported(ctx.Edition) is not { } msrc)
                return BoundRejected.Reported(ctx.Edition);
            // §14.9.23.3 SR8: "Identifier-2 shall reference an alphanumeric or national data item". The operand
            // category is the ONE reader's (DataItem.OperandPic): an alphanumeric group has none and is "class and
            // category alphanumeric" (§8.5.2.1); a national group is national and a bit group boolean (GR2b/GR1b of
            // §13.18.29.4). The run-time selector (CobolObject.NormalizeMethodName) takes the item's character
            // value whichever of the two classes it is (kb/Work PB1136).
            bool selectorAdmitted = msrc.Item.OperandPic is { } selectorPic
                ? selectorPic.Category is PicCategory.Alphanumeric or PicCategory.National
                : msrc.Item.IsGroup;
            if (!selectorAdmitted)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0866",
                    $"INVOKE: identifier-2 ('{mref.GetText()}') shall reference an alphanumeric or national data "
                    + "item (ISO §14.9.23.3 SR8)");
            }
            return OoBindUniversalInvoke(site, urecv!, methodLiteral: null, methodSource: msrc);
        }
        string? methodName = OoMethodNameOf(inv.invokeMethodName().literal());
        if (methodName is null)
        {
            return BoundRejected.Report(ctx.Edition, "COBOLNET0823",
                "INVOKE: literal-1 (the method name) shall be of class alphanumeric or national "
                + "(ISO §14.9.23.3 SR2)");
        }
        if (methodName.Length == 0)
        {
            return BoundRejected.Report(ctx.Edition, "COBOLNET0823",
                "INVOKE: literal-1 shall not be a zero-length literal (ISO §14.9.23.3 SR2)");
        }

        // ⛔ THE RECEIVER DISPATCH IS SHARED WITH THE INLINE FORM (kb/Work PB428): §8.4.3.4.4 GR1 says an
        // inline method invocation IS one of the four INVOKE statements it lists, so `O :: "M"` and
        // `INVOKE O "M"` shall not be able to resolve a receiver, a roster or a method differently.
        return computedReceiver is { } temp
            ? OoBindInstanceInvoke(site, temp, methodName, DataBinder.WrittenText(target))
            : OoBindByReceiver(site, atom!, methodName);
    }

    /// <summary>⛔ AN OBJECT-REFERENCE OPERAND WRITTEN AS A COMPUTED IDENTIFIER (kb/Work PB1425, PB1197) — an inline
    /// method invocation (§8.4.3.1.2 Format 4) or a function-identifier (Format 1, including the keyword-omitted
    /// spelling §8.4.3.2.3 SR2 parses as a data reference) — bound ONCE to the expression that reads the temporary item
    /// it references (§8.4.3.4.4 GR1; §8.4.3.2.1). Null when the operand is a data-name, a class-name or a predefined
    /// object reference, which the position's own arms take. Every object-reference position asks it: the INVOKE
    /// receiver, RAISE's identifier-1 and SET Format 5's sender, so no position can admit a format another
    /// refuses.</summary>
    internal BoundExpr? OoBindComputedObjectReference(Core.ObjectReferenceContext operand) =>
        operand.inlineMethodInvocation() is { } imi ? OoBindInlineInvocation(imi)
        : OoBindComputedAtom(operand.objectReferenceAtom());

    /// <summary>The object-view and function-identifier half of <see cref="OoBindComputedObjectReference"/>, for the
    /// receiver an inline invocation applies its first <c>::</c> segment to (§8.4.3.1.3 SR1 — identifier-1 is any
    /// identifier). An object-view (§8.4.3.1.2 Format 5) is an atom because §8.4.3.1.4 GR1 c) applies it before the
    /// invocation operator (e): it is computed like a function-identifier, to the temporary it references.</summary>
    private BoundExpr? OoBindComputedAtom(Core.ObjectReferenceAtomContext atom) =>
        atom.objectView() is { } ov ? OoBindObjectView(ov) : OoBindComputedTerm(atom.objectReferenceTerm());

    /// <summary>A function-identifier term (Format 1, including the keyword-omitted spelling §8.4.3.2.3 SR2 parses as a
    /// data reference), bound to its result; null for a data-name, a class-name or a predefined object reference.</summary>
    private BoundExpr? OoBindComputedTerm(Core.ObjectReferenceTermContext term) =>
        term.functionCall() is { } fc ? host.Intrinsic.BindIntrinsic(fc)
        : term.dataReference() is { } dref ? host.Intrinsic.KeywordOmittedFunction(dref)
        : null;

    /// <summary>The temporary item a computed object-reference operand references, when that item is an object
    /// reference; otherwise null, with the position's own rule reported against the identifier AS WRITTEN — never
    /// against the compiler's temporary, whose name the program never wrote. An intrinsic function has no item and
    /// never an object result (§15), so it is refused by the same rule. A bound error was already reported by the
    /// identifier's own binder.</summary>
    internal Place? OoObjectReferenceTemporary(BoundExpr bound, Antlr4.Runtime.ParserRuleContext written,
                                               string code, string rule)
    {
        if (bound is BoundExprError) return null;
        if (IntrinsicBinder.TemporaryItemOf(bound) is { Item.Pic.Category: PicCategory.ObjectReference } temp)
            return temp;
        ctx.Edition.Error(code, $"'{DataBinder.WrittenText(written)}': {rule}");
        return null;
    }

    /// <summary>Resolve an invocation's RECEIVER and dispatch to the roster it selects — the shared tail of
    /// the INVOKE statement (§14.9.23.2 <c>{identifier-1 | class-name-1}</c>) and of the §8.4.3.4.2 inline
    /// form's <c>{object-class-name-1 | identifier-1}</c>, which are the SAME operand: §8.4.3.4.4 GR1 defines
    /// the inline form as one of the INVOKE statements it writes out, and §8.4.3.4.3 SR3 requires that INVOKE
    /// to be valid by §14.9.23's own syntax rules. One activation mechanism, never a second.</summary>
    private BoundStatement OoBindByReceiver(InvocationSite site, Core.ObjectReferenceTermContext target,
                                            string methodName)
    {
        // The written method name is FORMED once, here, where both invocation spellings meet: leading and trailing
        // spaces are not part of an externalized name (DOC-A.1-68; the one rule, CobolNet.Runtime.ExternalizedNames),
        // so `INVOKE O " GET "` names the method `METHOD-ID. GET` names, by the typed and the universal path alike.
        methodName = CobolNet.Runtime.ExternalizedNames.Form(methodName);
        // `object-class-name-1 OF SUPER` is a word chain (`name OF SUPER`) that the resolver reads as the qualified SUPER
        // exactly when the name is a class-name (kb/Work PB1425 — otherwise it is the property of SUPER, an ordinary
        // receiver resolved below); a bare SELF or SUPER is the `selfAndSuper` term.
        Core.CobolWordContext? superQualifier = target.dataReference() is { } qref ? ctx.Refs.QualifiedSuperClass(qref) : null;
        if (target.selfAndSuper() is not null || superQualifier is not null)
        {
            // Slice 3b — §8.4.3.8: SELF/SUPER are the predefined object references of the CURRENT method's
            // object; legal only within a method body.
            bool isSuper = superQualifier is not null || target.selfAndSuper()!.SUPER() is not null;
            if (OoPredefinedSearchRoot(isSuper, superQualifier, "INVOKE") is not { } root)
                return BoundRejected.Reported(ctx.Edition);
            var (cur, searchRoot) = root;
            // Roster selection by CONTEXT (§14.9.23.3 SR4f/g/h/i): a factory method's SELF/SUPER resolve
            // over the FACTORY interface; an instance method's over the instance interface.
            var sm = host.OoInFactory ? searchRoot.FindFactoryMethod(methodName) : searchRoot.FindMethod(methodName);
            if (sm is null)
            {
                // §14.9.23.3 SR4 f)–i): SELF or SUPER × a factory or an instance method — the same two facts the
                // roster selection above used, so the printed rule is derived, never chosen per arm (kb/Work PB1136).
                string selfRule = (isSuper, host.OoInFactory) switch
                {
                    (false, true) => "§14.9.23.3 SR4 f) — literal-1 shall name a method contained in the factory "
                                     + "interface of the class containing the INVOKE statement",
                    (false, false) => "§14.9.23.3 SR4 g) — literal-1 shall name a method contained in the instance "
                                      + "interface of the class containing the INVOKE statement",
                    (true, true) => "§14.9.23.3 SR4 h) — literal-1 shall name a method contained in the factory "
                                    + "interface of a class inherited by the class containing the INVOKE statement",
                    (true, false) => "§14.9.23.3 SR4 i) — literal-1 shall name a method contained in the instance "
                                     + "interface of a class inherited by the class containing the INVOKE statement",
                };
                if (host.OoInFactory && IsStandardNew(methodName))
                    return NewWithoutBase($"INVOKE {(isSuper ? "SUPER" : "SELF")} \"{methodName}\"", searchRoot,
                        selfRule);
                return BoundRejected.Report(ctx.Edition, "COBOLNET0825",
                    $"INVOKE {(isSuper ? "SUPER" : "SELF")} \"{methodName}\": class '{searchRoot.Name}' (and "
                    + $"its inheritance chain) does not define a{(host.OoInFactory ? " factory" : "n instance")} "
                    + "method named '" + methodName + $"' (ISO {selfRule})");
            }
            var selfForm = isSuper ? InvokeForm.Super : InvokeForm.Self;
            // §14.8.3.3 rule 2 b) 2.: a method invoked with SELF or SUPER is invoked through ACTIVE-CLASS — the
            // description every invocation-dependent conformance rule reads (OoConformance.InvokedThroughActiveClass).
            var selfView = ObjectRefDescriptor.ActiveClass(cur.Name, factory: false);
            // A method of the standard class BASE through SELF/SUPER: the object it runs on is the current object,
            // whose class is the containing class or a subclass of it — exactly the §13.18.60.2 ACTIVE-CLASS
            // description, and exactly what §16.2's `active-class` returning items mean. So New in a factory method
            // creates the ACTIVE class (§16.2.1.2 GR1 through the polymorphic factory, SR4 f); SUPER restricts the
            // SEARCH (§8.4.3.8.4 GR3), and the method found is still BASE's, running on the same object.
            if (sm.Standard is not StandardMethod.None)
                return OoBindStandardInvoke(site, sm, selfForm, receiver: null, receiverClass: null, selfView);
            return OoBindResolvedInvoke(site, sm, selfForm, null, selfView);
        }
        if (target.dataReference() is not { } dref)
            // INVOKE NULL is LEGAL source: §14.9.23.3 SR1 asks only for an object reference, which NULL is
            // (§8.4.3.7.3 SR2 — class object, category object reference, not universal), and §14.9.23.4 GR5 gives
            // its meaning — EC-OO-NULL at run time. Until the receiver half of kb/Work PB1136 lands with the
            // object-reference identifier tier (kb/Work PB1782 step 2 / PB1425) this is a DEFERRAL, never a
            // refusal: it used to be refused under a §14.9.23.3 rule that does not exist.
            return new BoundUnsupported("INVOKE through the predefined object reference NULL (the run-time "
                + "EC-OO-NULL of ISO §14.9.23.4 GR5; kb/Work PB1136)");

        // identifier-1 vs class-name-1 (§14.9.23.2): resolve as a data item first (a data-name shadows);
        // an unresolved SIMPLE name is then a class-name candidate in the pass-1 table — a LEGAL alternative,
        // so this is a Probe; the else-tail below reports when NEITHER reading holds (R30).
        // ⛔ Probe to DISCRIMINATE, RESOLVE to commit (kb/Work PB221): a probe is unscreened, so its Place must
        // never enter the bound tree — the receiver's subscripts would bypass every position screen.
        if (ctx.Refs.Probe(dref) is not null)
            // Discriminated as identifier-1: the committed answer decides, never the class-name reading (kb/Work PB1030).
            return host.Expr.ResolveSending(dref) is var ra && ra.Place is { } receiver
                ? OoBindInstanceInvoke(site, receiver, methodName) : ra.Refusal(ctx.Edition);
        // The class-name-1 alternative is scoped by §8.4.6.4 to the names this SOURCE ELEMENT may reference,
        // so the partition asks the ONE funnel (kb/Work PB365 — `OoClasses.Find` asked the whole group).
        if (Compiler.Oo.OoNameResolution.Lookup(host.OoClasses, dref, dref.GetText(),
                Compiler.Oo.OoNameResolution.Want.Class).Class is { } cls)
            return OoBindClassInvoke(site, cls, methodName);
        return BoundRejected.Report(ctx.Edition, "COBOLNET0823",
            $"INVOKE: '{DataBinder.WrittenText(dref)}' is neither a resolvable data item nor a class this source element "
            + "may reference (ISO §14.9.23.2 — identifier-1 or class-name-1; §8.4.6.4)");
    }

    /// <summary>⛔ THE ONE §8.4.3.8 METHOD-SEARCH ROOT OF SELF / [object-class-name-1 OF] SUPER (kb/Work PB1425), asked by
    /// both places that select a method through them: the invocation receiver (<see cref="OoBindByReceiver"/>) and the
    /// object of an object property (<see cref="OoBindPropertyObject"/>, whose accessor is a method found the same way,
    /// §8.4.3.9.4 GR1 "as though the associated get property method were invoked"). SELF searches the containing class
    /// (GR2, dispatched on the run-time class); SUPER starts at the class the INHERITS clause names (GR3), and
    /// <c>object-class-name-1 OF SUPER</c> at object-class-name-1, which SR4 requires to be that class (GR4) — a class
    /// INHERITS one class here (a multiple-INHERITS class is declined, Annex A.4.10 item 1). Null after the refusal is
    /// reported: SR1 outside a method (COBOLNET0827), SR4 (COBOLNET2777), SUPER in a class that inherits nothing —
    /// reported unless <paramref name="report"/> is false (a resolver PROBE, which never diagnoses).
    /// <paramref name="position"/> names the construct for the message ("INVOKE", "the object-property reference …").</summary>
    internal (OoClassSymbol Current, OoClassSymbol SearchRoot)? OoPredefinedSearchRoot(bool isSuper,
        Core.CobolWordContext? qualifier, string position, bool report = true)
    {
        string written = qualifier is not null ? $"{qualifier.GetText()} OF SUPER" : isSuper ? "SUPER" : "SELF";
        if (!host.InMethod || host.OoCurrentClass is not { } cur)
        {
            if (report) _ = RefusePredefinedObjectOutsideMethod($"{position} {written}");
            return null;
        }
        if (!isSuper) return (cur, cur);
        if (qualifier is not null)
        {
            // §8.4.3.8.3 SR4: "Object-class-name-1 shall be the name of a class specified in the INHERITS clause of the
            // containing class definition"; GR4: "the search for the method shall include only those methods defined
            // for object-class-name-1".
            var named = Compiler.Oo.OoNameResolution.Lookup(host.OoClasses, qualifier, qualifier.GetText(),
                Compiler.Oo.OoNameResolution.Want.Class).Class;
            if (named is null || !ReferenceEquals(named, cur.Base))
            {
                if (report)
                    ctx.Edition.Error(DiagnosticCatalog.SuperQualifierNotInherited,
                    $"{position} {written}: '{qualifier.GetText()}' is "
                    + (named is null ? "not a class this source element may reference"
                        : cur.Base is null ? $"not inherited — class '{cur.Name}' has no INHERITS clause"
                        : $"not the class the INHERITS clause of '{cur.Name}' names ('{cur.Base.Name}')")
                    + " (ISO §8.4.3.8.3 SR4 — object-class-name-1 shall be the name of a class specified in the "
                    + "INHERITS clause of the containing class definition)");
                return null;
            }
            return (cur, named);
        }
        if (cur.Base is { } b) return (cur, b);   // GR3 — the restricted search STARTS at the base class
        // Trap #7 — SUPER in a root class is a clean compile diagnostic, never an internal error (applies identically
        // to the FACTORY flavor).
        if (report)
            ctx.Edition.Error("COBOLNET0827", $"{position} SUPER in class '{cur.Name}', which INHERITS from no class "
            + "(ISO §8.4.3.8 — SUPER references the inherited class's methods)");
        return null;
    }

    /// <summary>§8.4.3.8.3 SR1 — "This identifier format may be specified only in a method definition" — for SELF / SUPER
    /// written outside one, as an INVOKE receiver or an object-view's identifier-1 (kb/Work PB1425): the one spelling of
    /// the refusal those positions share. <paramref name="written"/> names the construct as the program wrote it.</summary>
    internal BoundRejected RefusePredefinedObjectOutsideMethod(string written) =>
        BoundRejected.Report(ctx.Edition, "COBOLNET0827", $"{written} may be specified only within a method definition "
            + "(ISO §8.4.3.8.3 SR1 — the predefined object references of the current object)");

    /// <summary><c>INVOKE class-name-1 "m" …</c>: §14.9.23.3 SR3 — "The value of literal-1 shall be the name of a
    /// method defined in the factory interface of object-class-name-1". Resolution walks the INHERITS chain over
    /// the factory rosters (§9.3.6), which is also how the standard class BASE's New is found: it is in the factory
    /// interface of exactly the classes that inherit BASE (§16.2; §9.3.9), and in no other (kb/Work PB1548).</summary>
    private BoundStatement OoBindClassInvoke(InvocationSite site, OoClassSymbol cls, string method)
    {
        if (cls.FindFactoryMethod(method) is { } fm)
        {
            // §14.8.3.3 rule 2 b) 1.: a method invoked with an object-class-name is invoked through "that same
            // object-class-name and an ONLY phrase" — and §16.2.1.2 GR1 agrees for New: invoked on the factory object
            // NAMED here it creates an instance object of EXACTLY cls, so the result is described ONLY.
            var classView = ObjectRefDescriptor.ObjectClass(cls.Name, factory: false, only: true);
            if (fm.Standard is not StandardMethod.None)
                return OoBindStandardInvoke(site, fm, InvokeForm.New, receiver: null, cls, classView);
            var bound = OoBindResolvedInvoke(site, fm, InvokeForm.Factory, null, classView);
            return bound is BoundInvoke bi ? bi with { ClassCsName = cls.CsName } : bound;
        }
        if (IsStandardNew(method))
            return NewWithoutBase($"INVOKE {cls.Name} \"{method}\"", cls,
                "§14.9.23.3 SR3 — literal-1 shall name a method defined in the factory interface of object-class-name-1");
        return BoundRejected.Report(ctx.Edition, "COBOLNET0825",
            $"INVOKE {cls.Name} \"{method}\": class '{cls.Name}' (and its inheritance chain) does not "
            + "define a FACTORY method named '" + method + "' (ISO §14.9.23.3 SR3 — literal-1 shall name "
            + "a method of the factory interface; the runtime analog is EC-OO-METHOD, §14.9.23.4 GR7b)");
    }

    /// <summary>True when <paramref name="method"/> names BaseFactoryInterface's New (§16.2; method-names compare
    /// case-insensitively, §8.3.2.2) — the one name whose absence from a factory interface has its own diagnostic,
    /// because until kb/Work PB1548 this compiler treated New as predefined for every class.</summary>
    private static bool IsStandardNew(string method) =>
        CobolNames.Same(method, OoStandardClasses.NewMethodName);

    /// <summary>COBOLNET2448: New named through a factory object whose class does not inherit the standard class
    /// BASE. <paramref name="rule"/> is the §14.9.23.3 rule the receiver form is governed by (SR3, SR4 a/c/f/h).</summary>
    private BoundStatement NewWithoutBase(string where, OoClassSymbol cls, string rule) =>
        BoundRejected.Report(ctx.Edition, DiagnosticCatalog.NewWithoutBase,
            $"{where}: class '{cls.Name}' does not inherit from the standard class BASE, so its factory interface "
            + "has no method New — New belongs to BASE's factory interface, BaseFactoryInterface (ISO §16.2), and a "
            + "class has it only through INHERITS (§9.3.9); write `INHERITS FROM BASE` in its CLASS-ID paragraph "
            + $"(or in a superclass's) with `CLASS BASE` in that class's REPOSITORY paragraph (ISO {rule})");

    /// <summary>Bind an invocation of a method of the standard class BASE (ISO §16.2) — New or FactoryObject. Their
    /// §16.2 signatures take no parameters and return an item described with ACTIVE-CLASS (New: <c>object reference
    /// active-class</c>; FactoryObject: <c>object reference factory of active-class</c>), whose class is the class of
    /// the object the method runs on. So the RESULT's description is the receiver's own class view with the
    /// FACTORY phrase the method implies: <paramref name="classView"/> is that view (the named class, exactly for a
    /// class-name; the declared class or ACTIVE-CLASS of a typed reference, or of SELF/SUPER), and New's result is its
    /// instance description, FactoryObject's its factory description. That description is then delivered by the
    /// §14.8.3.3 rule-1 SET conformance every other RETURNING takes.</summary>
    private BoundStatement OoBindStandardInvoke(InvocationSite site, OoMethodSymbol m, InvokeForm form,
        Place? receiver, OoClassSymbol? receiverClass, ObjectRefDescriptor classView)
    {
        string verb = $"{site.Verb} \"{m.Name}\"";
        if (site.ArgsWritten)
            return BoundRejected.Report(ctx.Edition, "COBOLNET0826",
                $"{verb}: the method {m.Name} of the standard class BASE takes no arguments (ISO §16.2 — its "
                + "procedure division header has no USING phrase; §14.8.2.1)");
        // §8.4.3.4.3 SR4: "The data item referenced in the RETURNING phrase of the invoked method's procedure division
        // header shall not be described with the ANY LENGTH clause or with the ACTIVE-CLASS phrase" — and §16.2
        // describes BOTH of BASE's returning items with ACTIVE-CLASS, so neither is inline-invocable.
        if (site.ReturningImplicit)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.InlineInvocationReturningShape,
                $"the inline method invocation of \"{m.Name}\": the returning item of the standard class BASE's "
                + $"{m.Name} ('{m.Binding!.Returning!.CobolName}') is described with the ACTIVE-CLASS phrase (ISO §16.2; "
                + "§8.4.3.4.3 SR4) — use the INVOKE statement with a RETURNING identifier");
        if (site.ReturningRef is not { } retRef)
            return BoundRejected.Report(ctx.Edition, "COBOLNET0826",
                $"{verb} without RETURNING — the method {m.Name} of the standard class BASE returns an object "
                + "reference, and the INVOKE shall specify RETURNING to receive it (ISO §16.2; §14.9.23.4 GR8)");
        if (host.Expr.ResolveReceiving(retRef) is not { } ret)
            return BoundRejected.Reported(ctx.Edition);   // the receiving chokepoint reported it — not a deferral (kb/Work PB236, PB881)
        if (ret.Item.Pic is not { Category: PicCategory.ObjectReference } retPic)
            return BoundRejected.Report(ctx.Edition, "COBOLNET0826",
                $"{verb} RETURNING '{retRef.GetText()}': the receiving item shall be a USAGE OBJECT REFERENCE data "
                + "item (ISO §14.9.23.4 GR8 / §14.8.3.3)");
        var result = classView with { Factory = m.Standard is StandardMethod.FactoryObject };
        if (OoConformance.ObjectRefAssignmentMismatch(host.OoClasses, PicInfo.ObjectReferenceItem(result), retPic)
            is { } err)
            return BoundRejected.Report(ctx.Edition, "COBOLNET0826",
                $"{verb} RETURNING '{retRef.GetText()}': {err} (ISO §14.8.3.3 rule 1)");
        return m.Standard switch
        {
            // Every New form renders through the ONE runtime body (OoEmitter.EmitNew): the class-name form names
            // the factory by class, a FACTORY OF reference is the receiver, SELF/SUPER is `this`.
            StandardMethod.New => new BoundInvoke(
                form switch
                {
                    InvokeForm.Self => InvokeForm.NewSelf,
                    InvokeForm.Super => InvokeForm.NewSuper,
                    _ => InvokeForm.New,
                },
                receiverClass?.CsName, receiver, null, ret),
            // FactoryObject is an ordinary instance call on the runtime BASE's virtual member (a COBOL override
            // adopts its CsName), so the Instance/Self/Super rendering and its GR5 null guard apply unchanged.
            _ => new BoundInvoke(form, null, receiver, m.CsName, ret, [], m.Binding!.Returning, m.Owner.CsName),
        };
    }
    /// <summary><c>INVOKE identifier-1 "method" …</c>: virtual dispatch through a TYPED object reference; the
    /// method resolves over the declared class's hierarchy at COMPILE time (§14.9.23.3 SR4 a)/b) — for the typed
    /// path a lookup failure is a compile-time diagnostic, the static analog of EC-OO-METHOD, GR7b).
    /// <paramref name="written"/> is the receiver as the program wrote it, for a receiver that is a TEMPORARY (a
    /// function-identifier's or an inline invocation's item, kb/Work PB1425): a diagnostic names what the program
    /// wrote, never the compiler's temporary (it used to say <c>INVOKE '__INV-TEMP-200003'</c>).</summary>
    private BoundStatement OoBindInstanceInvoke(InvocationSite site, Place receiver, string method,
                                                string? written = null)
    {
        string? recvName = written ?? receiver.Item.CobolName;
        // The written name FORMED (DOC-A.1-68, as OoBindByReceiver does): this path is also entered directly — by a
        // receiver that is a function-identifier or an inline invocation, and by each chained `::` segment, whose
        // receiver is the previous segment's temporary — so `A :: "ME" :: " GET "` names the method GET too.
        method = CobolNet.Runtime.ExternalizedNames.Form(method);
        if (receiver.Item.Pic is not { Category: PicCategory.ObjectReference } pic)
        {
            return BoundRejected.Report(ctx.Edition, "COBOLNET0824",
                $"INVOKE '{recvName}': identifier-1 shall be an object reference — a USAGE OBJECT "
                + "REFERENCE data item (ISO §14.9.23.3 SR1)");
        }
        // The receiver's §13.18.60.2 DESCRIPTION picks the roster (kb/Work PB389): universal → the dynamic
        // path; interface-name → the interface's prototype closure; object-class-name or ACTIVE-CLASS → the
        // named/containing class, and its FACTORY half when FACTORY OF was written.
        var rdesc = pic.ObjectRef ?? ObjectRefDescriptor.Universal;
        if (rdesc.IsUniversal)
            // A UNIVERSAL receiver with a literal selector (SR4 permits literal-1; it still cannot bind
            // statically — no roster exists at compile time): the D10 dynamic path.
            return OoBindUniversalInvoke(site, receiver, methodLiteral: method, methodSource: null);
        string className = rdesc.Name!;
        // An INTERFACE-typed receiver: resolution over the interface's prototype closure (§14.9.23.3 SR4e);
        // the emitted call is static C# interface dispatch behind the same GR5 null guard.
        if (rdesc.Kind is ObjectRefKind.Interface && host.OoClasses?.FindInterface(className) is { } recvIface)
        {
            var proto = recvIface.AllPrototypes()
                .FirstOrDefault(pm => CobolNames.Same(pm.Name, method));
            if (proto is null)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0825",
                    $"INVOKE '{recvName}' \"{method}\": interface '{recvIface.Name}' (and "
                    + "its INHERITS closure) does not declare a method named '" + method + "' "
                    + "(ISO §14.9.23.3 SR4 e))");
            }
            var ibound = OoBindResolvedInvoke(site, proto, InvokeForm.Instance, receiver, rdesc);
            return ibound is BoundInvoke ibi ? ibi with { OwnerCsName = recvIface.CsName } : ibound;
        }
        if (host.OoClasses?.Find(className) is not { } cls)
        {
            // Unreachable when DataBinder validated the declared class (COBOLNET0813) — defensive, loud.
            return BoundRejected.Report(ctx.Edition, "COBOLNET0813",
                $"INVOKE '{recvName}': its declared class '{className}' is not a class of the "
                + "compilation group (ISO §13.18.60.4)");
        }
        // §9.3.6: a class has TWO separate method interfaces, and which one a receiver selects is the FACTORY
        // axis of its own description — a FACTORY-OF reference holds the factory object (§13.18.60.4 GR22 d)1.a.)
        // and therefore resolves the FACTORY roster (§14.9.23.3 SR4b/SR4c). Both arms of ONE dispatch.
        var m = rdesc.Factory ? cls.FindFactoryMethod(method) : cls.FindMethod(method);
        // §14.9.23.3 SR4 a)–d): the rule a typed receiver's method name answers to is chosen by its description's
        // two axes — object-class-name or ACTIVE-CLASS, with or without FACTORY — so the citation is derived from
        // the same two facts the roster lookup above used, never written per arm (kb/Work PB1136).
        string typedRule = (rdesc.Kind is ObjectRefKind.ActiveClass, rdesc.Factory) switch
        {
            (false, true) => "§14.9.23.3 SR4 a) — literal-1 shall name a method contained in the factory interface of "
                             + "that object-class-name",
            (false, false) => "§14.9.23.3 SR4 b) — literal-1 shall name a method contained in the instance interface "
                              + "of that object-class-name",
            (true, true) => "§14.9.23.3 SR4 c) — literal-1 shall name a method contained in the factory interface of "
                            + "the class containing the INVOKE statement",
            (true, false) => "§14.9.23.3 SR4 d) — literal-1 shall name a method contained in the instance interface "
                             + "of the class containing the INVOKE statement",
        };
        if (m is null && rdesc.Factory && IsStandardNew(method))
            return NewWithoutBase($"INVOKE '{recvName}' \"{method}\"", cls, typedRule);
        if (m is null)
        {
            string other = rdesc.Factory ? "an INSTANCE" : "a FACTORY";
            string hint = (rdesc.Factory ? cls.FindMethod(method) : cls.FindFactoryMethod(method)) is not null
                ? $" ('{method}' IS {other} method of class '{cls.Name}' — the two interfaces are separate, "
                  + "§9.3.6, and this receiver's description selects the "
                  + (rdesc.Factory ? "factory" : "instance") + " one)"
                : "";
            return BoundRejected.Report(ctx.Edition, "COBOLNET0825",
                $"INVOKE '{recvName}' \"{method}\": class '{cls.Name}' (and its inheritance "
                + $"chain) does not define {(rdesc.Factory ? "a factory" : "an instance")} method named '"
                + method + $"' (ISO {typedRule}; compile-time for a typed receiver — the runtime analog is "
                + $"EC-OO-METHOD, §14.9.23.4 GR7b){hint}");
        }
        // A method of the standard class BASE through a typed reference: the object it runs on is described by the
        // receiver itself (its class, ONLY or not, or ACTIVE-CLASS), which is what §16.2's `active-class` means for
        // it — New through a FACTORY OF reference creates an object of the class that factory belongs to.
        if (m.Standard is not StandardMethod.None)
            return OoBindStandardInvoke(site, m, InvokeForm.Instance, receiver, cls, rdesc with { Factory = false });
        var bound = OoBindResolvedInvoke(site, m, InvokeForm.Instance, receiver, rdesc);
        // A factory-object receiver's argument PROFILES live in the FACTORY singleton type, not the instance
        // class — the same qualification InvokeForm.Factory gets by appending the suffix at emit time.
        return rdesc.Factory && bound is BoundInvoke fbi ? fbi with { OwnerCsName = cls.FactoryCsName } : bound;
    }

    /// <summary>The shared USING + RETURNING binding tail for a RESOLVED method — the Instance / SELF / SUPER
    /// forms differ only in receiver resolution and dispatch rendering (§8.4.3.8), never in marshaling.
    /// <paramref name="invokedWith"/> is how the object the method runs on is described (§14.8.3.3 rule 2 b)'s four
    /// cases), which an ACTIVE-CLASS formal or returning item conforms against (kb/Work PB1112).</summary>
    private BoundStatement OoBindResolvedInvoke(
        InvocationSite site, OoMethodSymbol m, InvokeForm form, Place? receiver, ObjectRefDescriptor invokedWith)
    {
        // ── USING marshaling (slice 2 — D6; §14.9.23.4 GR3: positional correspondence) ──
        var argCtxs = site.Args;
        var formals = m.Binding!.Formals;
        // §14.8.2.1: "The number of arguments in the activating element shall be equal to the number of formal
        // parameters in the activated element, with the exception of trailing formal parameters that are specified
        // with an OPTIONAL phrase in the procedure division header of the activated element and omitted from the
        // list of arguments" (§9.3.6 match rule 1 states the same equality for method resolution). The trap-#3
        // rule still holds for everything else: an arity mismatch is LOUD — a silently dropped/extra argument
        // would shift every following slot (the legacy DEVLOG-449 blocker: the first USING bound to RETURNING).
        if (argCtxs.Count > formals.Count
            || formals.Skip(argCtxs.Count).FirstOrDefault(f => !f.Optional) is { } required)
        {
            string why = argCtxs.Count > formals.Count ? ""
                : $" — only trailing OPTIONAL formal parameters may be omitted, and '{formals.Skip(argCtxs.Count).First(f => !f.Optional).Item.CobolName}' is not OPTIONAL";
            return BoundRejected.Report(ctx.Edition, "COBOLNET0828",
                $"{site.Verb} \"{m.Name}\": {argCtxs.Count} USING argument(s) for {formals.Count} formal "
                + $"parameter(s) of the method (ISO §14.8.2.1; §14.9.23.4 GR3 — correspondence is positional){why}");
        }
        var args = new List<BoundInvokeArg>(formals.Count);
        for (int i = 0; i < argCtxs.Count; i++)
        {
            if (OoBindInvocationArg(argCtxs[i], formals[i], m.Name, site.Verb, invokedWith) is not { } a)
                return BoundRejected.Reported(ctx.Edition);
            args.Add(a);
        }
        // The trailing omitted arguments (§14.9.23.4 GR9 — "or a trailing argument is omitted"): each takes its
        // positional slot explicitly, so the emitter renders one argument pair per formal and never infers arity.
        for (int i = argCtxs.Count; i < formals.Count; i++)
            args.Add(OmittedArg(formals[i].Item));

        // ── RETURNING pairing + conformance (GR8; §14.8.3; the deep-dive signature-check edge case:
        // BOTH mismatch directions are compile-time diagnostics) ──
        var retRef = site.ReturningRef;
        Place? retPlace = null;
        // ⛔ THE INLINE FORM'S RETURNING ITEM IS THE §8.4.3.4.4 GR1 b)/c) TEMPORARY, not a written
        // identifier: "temp-identifier has the same description, class, and category as the RETURNING
        // parameter in the specification of the method identified by literal-1", and it "is a temporary item
        // that exists for the purpose of effecting the inline invocation in this way and for no other
        // purpose". Cloning the method's own RETURNING item is what makes the delivery an IDENTITY crossing,
        // so §14.8.3.3's conformance check below has nothing to reject and is correctly skipped.
        if (site.ReturningImplicit)
        {
            if (m.Binding!.Returning is not { } retModel)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.InlineInvocationNoReturning,
                    $"the inline method invocation of \"{m.Name}\": the method's procedure division header "
                    + "declares no RETURNING item, so there is no temporary data item for the invocation to "
                    + "reference (ISO §8.4.3.4.1; §8.4.3.4.4 GR1 b))");
            }
            if (retModel.IsAnyLength)
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.InlineInvocationReturningShape,
                    $"the inline method invocation of \"{m.Name}\": the data item referenced in the "
                    + "RETURNING phrase of the invoked method's procedure division header shall not be "
                    + "described with the ANY LENGTH clause or with the ACTIVE-CLASS phrase "
                    + "(ISO §8.4.3.4.3 SR4)");
            }
            if (retModel.Pic is { Category: PicCategory.ObjectReference, ObjectRef: { Kind: ObjectRefKind.ActiveClass } })
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.InlineInvocationReturningShape,
                    $"the inline method invocation of \"{m.Name}\": the invoked method's RETURNING item is "
                    + "described with the ACTIVE-CLASS phrase (ISO §8.4.3.4.3 SR4)");
            }
            var temp = ctx.Data.OoCreateInvocationTemp(retModel, m.Name);
            if (ctx.Refs.ResolveItem(temp) is not { } tempPlace)
                return new BoundUnsupported($"the inline method invocation of \"{m.Name}\" (result temporary)");
            site.ImplicitReturningPlace = tempPlace;
            return new BoundInvoke(form, null, receiver, m.CsName, tempPlace, args, m.Binding!.Returning,
                m.Owner?.CsName);
        }
        if (retRef is not null && m.Binding!.Returning is null)
        {
            return BoundRejected.Report(ctx.Edition, "COBOLNET0828",
                $"{site.Verb} \"{m.Name}\" RETURNING: the method declares no RETURNING item (ISO §14.9.23.4 GR8 / "
                + "§14.8.3 — nothing to deliver)");
        }
        if (retRef is null && m.Binding!.Returning is not null)
        {
            return BoundRejected.Report(ctx.Edition, "COBOLNET0828",
                $"INVOKE \"{m.Name}\": the method declares a RETURNING item ('{m.Binding!.Returning.CobolName}') — "
                + "the INVOKE must specify RETURNING to receive it (the binder's signature check, deep-dive "
                + "D1; ISO §14.9.23.4 GR8)");
        }
        if (retRef is not null)
        {
            if (host.Expr.ResolveReceiving(retRef) is not { } rp)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0828",
                    $"INVOKE \"{m.Name}\" RETURNING '{retRef.GetText()}': the receiving identifier is not "
                    + "resolvable to storage");
            }
            if (!OoScreenReturning(rp, retRef, m.Name)) return BoundRejected.Reported(ctx.Edition);
            // §14.8.3.3 — the ONE returning-item half of §14.8.3, shared with the Format-2 CALL
            // (ParameterConformance.ReturningConformanceReason, kb/Work PB1164): an object reference conforms "as if
            // a SET statement were performed" (rule 1; rule 2's ACTIVE-CLASS sender is described by the INVOCATION),
            // everything else keeps the strict description check.
            string? rerr = host.Params.ReturningConformanceReason(m.Binding!.Returning!, rp.Item, invokedWith);
            if (rerr is not null)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0828",
                    $"INVOKE \"{m.Name}\" RETURNING '{retRef.GetText()}': {rerr} (ISO §14.8.3.3 "
                    + "returning-item conformance)");
            }
            retPlace = rp;
        }
        return new BoundInvoke(form, null, receiver, m.CsName, retPlace, args, m.Binding!.Returning, m.Owner?.CsName);
    }

    /// <summary>ISO §14.9.23.3 SR11 and SR12 over a RESOLVED RETURNING item (identifier-4) — through the typed and the
    /// universal receiver alike, which is why it is one helper (kb/Work PB1137): identifier-4 "shall reference a data
    /// item defined in the file, working-storage, local-storage, or linkage section" (a report's PAGE-COUNTER was
    /// accepted), and a bit data item shall be byte-aligned (a misaligned one crossed). Both screens are the ones
    /// CALL's SR7/SR8 ask, from <see cref="ParameterConformance"/>. False having reported.</summary>
    private bool OoScreenReturning(Place rp, Core.DataReferenceContext retRef, string? methodName)
    {
        string subject = methodName is null ? "INVOKE RETURNING item" : $"INVOKE \"{methodName}\" RETURNING item";
        if (!host.Params.ScreenSection(rp, retRef, DiagnosticCatalog.InvokeOperandSection, subject,
                "§14.9.23.3 SR11", addressAdmitted: false))
            return false;
        if (BitLayout.IsBitItem(rp.Item))
            host.Params.ScreenBitAlignment(rp, DiagnosticCatalog.InvokeBitAlignment, subject, "§14.9.23.3 SR12");
        return ActiveClassSubordinateAdmitted(rp, subject);
    }

    /// <summary>⛔ ISO §14.9.23.3 SR13, ITS OWN SCREEN (kb/Work PB1116): "If Identifier-3, identifier-4, or identifier-5
    /// references a group item, there shall not be an item subordinate to that group item that is an object reference
    /// described with the ACTIVE-CLASS phrase." Asked of every INVOKE operand the rule names — a BY REFERENCE
    /// (identifier-3) or BY CONTENT (identifier-5) argument, typed or universal, and the RETURNING item (identifier-4,
    /// through <see cref="OoScreenReturning"/>) — before any conformance question. Nothing asked it before: such a group
    /// was refused only because the crossing has no character image for a group with an object leaf (the Tier-C
    /// reason), which named the wrong rule and would have turned into a silent acceptance the moment the carrier learned
    /// to cross a strongly-typed group (§13.18.60.3 confines a subordinate object reference to one). False having
    /// reported.</summary>
    private bool ActiveClassSubordinateAdmitted(Place p, string subject)
    {
        if (p.DenotedItem is not { IsGroup: true } g) return true;
        if (DataItem.DescendantsOf(g).FirstOrDefault(d => d.Pic?.ObjectRef is { Kind: ObjectRefKind.ActiveClass })
            is not { } leaf)
            return true;
        ctx.Edition.Error(DiagnosticCatalog.InvokeActiveClassSubordinate,
            $"{subject} '{g.CobolName}' is a group item with the subordinate object reference '{leaf.CobolName}' "
            + "described with the ACTIVE-CLASS phrase (ISO §14.9.23.3 SR13: \"there shall not be an item subordinate "
            + "to that group item that is an object reference described with the ACTIVE-CLASS phrase\")");
        return false;
    }

    /// <summary>A literal-2 argument as the shared §14.8.2.3.3 verdict reads one — a BY CONTENT value with no
    /// storage (<see cref="ParameterConformance.ContentConformanceReason"/>; kb/Work PB1137).</summary>
    private static BoundCallArg LiteralArg(BoundOperand literal) => new(CobolNet.Runtime.CobolPassMode.Content, null, literal);

    /// <summary>An omitted argument's slot (kb/Work PB757) — spelled OMITTED or trailing-omitted; no source, no
    /// write-back (§14.9.23.4 GR9: the omitted-argument condition is true in the invoked method).</summary>
    private static BoundInvokeArg OmittedArg(DataItem formal) =>
        new(formal, null, null, null, WriteBack: false) { Omitted = true };

    /// <summary>Bind ONE INVOKE argument against its positional formal — the conformance RULE is selected
    /// by the EFFECTIVE passing mode (§14.9.23.4 GR6): BY REFERENCE takes §14.8.2.3.2 strict identity (with
    /// the §14.8.2.2 rule-1 group-prefix allowance); BY CONTENT — explicit, the §14.9.23.3 SR 10 object-data
    /// auto-CONTENT, and every literal — takes §14.8.2.3.3: COMPUTE rules for a numeric formal (any numeric
    /// argument), SET rules for an object-reference formal (widening), MOVE rules otherwise. A
    /// reference-modified argument conforms by its EFFECTIVE description (a unique elementary alphanumeric
    /// item of the window length, §8.4.3.3.4 GR6). Null on a diagnostic.</summary>
    private BoundInvokeArg? OoBindInvocationArg(InvocationArg arg, OoFormal oof, string methodName,
                                                string verb, ObjectRefDescriptor invokedWith)
    {
        var formal = oof.Item;
        void Err(string msg) => ctx.Edition.Error("COBOLNET0828", $"{verb} \"{methodName}\": {msg}");

        if (arg.Omitted)
        {
            // §14.9.23.2's `[BY REFERENCE] { identifier-3 | OMITTED }` and §8.4.3.4.2's argument brace (kb/Work
            // PB757). §14.9.23.3 SR18: "If an OMITTED phrase is specified, an OPTIONAL phrase shall be specified for
            // the corresponding formal parameter in the procedure division header." Past that there is nothing to
            // conform — §9.3.6 match rule 3 b): "No further checking is performed on this parameter".
            if (!oof.Optional)
            {
                ctx.Edition.Error(DiagnosticCatalog.InvokeOmittedNeedsOptional,
                    $"{verb} \"{methodName}\": the OMITTED argument corresponds to formal parameter "
                    + $"'{formal.CobolName}', which the method's procedure division header does not describe with "
                    + "the OPTIONAL phrase (ISO §14.9.23.3 SR18)");
                return null;
            }
            return OmittedArg(formal);
        }
        // ⛔ THE PASSING MODE OF THE ARGUMENT IS THE FORMAL'S WHEN THE FORMAL IS BY VALUE (kb/Work PB1051).
        // §14.9.23.3 SR5 a): "If a BY CONTENT or BY REFERENCE phrase is specified for an argument, a BY REFERENCE phrase
        // shall be specified or implied for the corresponding formal parameter"; b): "If a BY VALUE phrase is specified for
        // an argument, a BY VALUE phrase shall be specified or implied for the corresponding formal parameter"; and
        // §14.9.23.4 GR6 b): "When the BY VALUE phrase is specified or implied for the corresponding formal parameter, BY
        // VALUE is assumed" for a keyword-less argument. A BY VALUE argument is a detached sending value — §14.8.2.3.3's
        // regime ("passed by content or by value": COMPUTE / SET / MOVE rules) — so below it takes the BY CONTENT arms
        // with its own two screens: SR16 (a literal) and SR15 (an identifier's class).
        bool byValue = oof.ByValue;
        if (arg.ByValueWritten || byValue)
        {
            // SR16: "If literal-2 or its corresponding formal parameter is specified with the BY VALUE phrase,
            // literal-2 shall be a numeric literal" — CALL's SR23 word for word, so it is asked of THE ONE
            // predicate (kb/Work PB1631): a numeric literal, ZERO without ALL (§8.3.3.6.3 SR1 a)), or NULL,
            // which is identifier-5 rather than literal-2 (§8.4.3.1.3 SR7) and whose class §14.9.23.3 SR15 admits.
            if (arg.Literal is { } byValueLit && !CallBinder.ByValueLiteralAdmitted(byValueLit))
            {
                Err($"BY VALUE {ConcatFolder.Spelling(byValueLit)}: literal-2 shall be a numeric literal when the BY VALUE phrase is "
                    + "specified (ISO §14.9.23.3 SR16)");
                return null;
            }
            if (arg.ByValueWritten && !byValue)
            {
                Err($"BY VALUE argument for formal '{formal.CobolName}': the corresponding formal parameter is "
                    + "BY REFERENCE (ISO §14.9.23.3 SR5 b))");
                return null;
            }
            if (byValue && (arg.ByReferenceWritten || arg.ByContentWritten))
            {
                Err($"{(arg.ByReferenceWritten ? "BY REFERENCE" : "BY CONTENT")} argument for formal '{formal.CobolName}': "
                    + "the corresponding formal parameter is BY VALUE (ISO §14.9.23.3 SR5 a))");
                return null;
            }
            byValue = true;
        }

        // ⛔ AN ADDRESS-IDENTIFIER ARGUMENT (kb/Work PB1021 — the INVOKE twin of PB239's CALL arm). §14.9.23.3 SR9:
        // "Identifier-3 shall be an address-identifier or shall reference a data item defined in the file,
        // working-storage, local-storage, or linkage section"; SR19: "If identifier-3 references an
        // address-identifier, identifier-3 is a sending operand" — so whatever phrase was written it crosses as a
        // detached pointer VALUE and never writes back (§8.4.3.11.3 SR5 / §8.4.3.13.3 SR4 withhold the receiving
        // role). SR5 c) applies "14.8.2, Parameters", whose class-pointer law is the ONE verdict the CALL argument
        // reads (PtrBinder.AddressConformanceReason).
        if (arg.Address is { } addrCtx)
        {
            if (host.Ptr.BindAddressIdentifier(addrCtx, verb + " … USING") is not { } ao) return null;
            if (host.Ptr.AddressConformanceReason(formal, ao.Data, ao.Program) is { } awhy)
            {
                Err($"USING argument '{DataBinder.WrittenText(addrCtx)}' (an address-identifier) does not conform to "
                    + $"formal parameter '{formal.CobolName}': {awhy}");
                return null;
            }
            return new BoundInvokeArg(formal, null, null, null, WriteBack: false, ByContent: true) { Address = ao };
        }

        // ⛔ SELF IS AN IDENTIFIER-5 (kb/Work PB1137). §8.4.3.8 makes it an identifier format whose only role bar is
        // that it is not a receiving operand, so `USING [BY CONTENT] SELF` passes the containing method's object BY
        // CONTENT (it is in no DATA DIVISION section — GR6 a) 2.). An object-reference formal takes it "as if a SET
        // statement were performed" (§14.8.2.3.3), i.e. by the SAME SELF-sender rules SET Format 5 applies
        // (SelfSenderRefusals). It used to be a parse error.
        if (arg.Self)
        {
            if (host.OoCurrentClass is not { } selfClass)
            {
                Err("SELF is defined only within a method definition (ISO §8.4.3.8.3 SR1)");
                return null;
            }
            if (formal.Pic is not { Category: PicCategory.ObjectReference })
            {
                Err($"SELF is an object reference and formal '{formal.CobolName}' is not one — an object-reference "
                    + "argument conforms by the SET rules (ISO §14.8.2.3.3)");
                return null;
            }
            bool selfConforms = true;
            // An ACTIVE-CLASS formal takes SELF only by §14.8.2.3.3 alternative 1) — invoked through ACTIVE-CLASS
            // (SELF, SUPER or an ACTIVE-CLASS reference); alternative 2)'s receiver is described ONLY, and §14.9.39.3
            // SR12 c)1. admits no SELF sender into one (kb/Work PB1112).
            if (formal.Pic.ObjectRef is { Kind: ObjectRefKind.ActiveClass }
                && !OoConformance.InvokedThroughActiveClass(invokedWith))
            {
                Err($"SELF for ACTIVE-CLASS formal '{formal.CobolName}': the method is invoked with "
                    + $"{invokedWith.Spelled}, not with SELF, SUPER or an ACTIVE-CLASS reference, and the other "
                    + "alternative's receiver is described ONLY, which admits no SELF sender (ISO §14.8.2.3.3; "
                    + "§14.9.39.3 SR12 c)1.)");
                selfConforms = false;
            }
            foreach (string why in SelfSenderRefusals(formal.Pic.ObjectRef ?? ObjectRefDescriptor.Universal, selfClass))
            {
                Err($"SELF for formal '{formal.CobolName}': {why} (the SET rules, ISO §14.8.2.3.3)");
                selfConforms = false;
            }
            return selfConforms
                ? new BoundInvokeArg(formal, null, null, null, WriteBack: false, ByContent: true) { SelfObject = true }
                : null;
        }

        bool explicitReference = arg.ByReferenceWritten;
        // ⛔ A BARE ARGUMENT OF THE INLINE FORM IS BY CONTENT, AND THAT IS THE GENERAL FORMAT SPEAKING.
        // §8.4.3.4.2 prints NO passing phrase at all, and §8.4.3.4.4 GR1 makes the arguments those of
        // `INVOKE … USING arguments`, so §14.9.23.4 GR6 decides — the same default an INVOKE's bare
        // argument takes. `Expression` marks the shapes that have no storage to write back to
        // (§14.9.23.3 SR9 confines BY REFERENCE to an identifier).
        bool explicitContent = arg.ByContentWritten || byValue;   // a BY VALUE argument is a detached value (GR6 b))
        // An operand that survives the reductions below as an EXPRESSION has no storage, so §14.9.23.3 SR9
        // cannot be met and GR6 a)2 assumes BY CONTENT. Only the inline form can reach this: an INVOKE
        // spells its own phrase, so `arg.Expression` is false there and this path is byte-inert for it.
        bool impliedContent = arg.Expression && !arg.ByValueWritten;

        // ── ONE OPERAND, FOUR CHANNELS — resolved ONCE, here (ISO §14.9.23.2 BY CONTENT: `arithmetic-
        // expression-1 | boolean-expression-1 | identifier-5 | literal-2`) ──────────────────────────────────
        // ⛔ THE PARSE NODE AN OPERAND LANDS IN IS NOT ITS MEANING, and both of PB46's halves learned that the
        // hard way. `arithmeticExpression` SUBSUMES `dataReference` and every numeric literal, and the
        // `{boolExprAhead()}?`-gated `booleanExpression` alternative subsumes BOTH of those in turn — its leaf
        // is `valueOperand`, and the predicate's scan runs to the statement's period, so in
        // `USING BY CONTENT N + 1 BY CONTENT B1 B-AND B2` the FIRST argument reaches the boolean node on the
        // strength of the SECOND argument's B-AND. Normalizing here is what makes that harmless: a boolean node
        // carrying NO boolean operator reduces to its bare `valueOperand` (ConditionBinder.UnwrapBareBool — the
        // same reduction BindPrimaryBoolean uses) and rides exactly the arm it would have without the predicate.
        var boolCtx = arg.Bool;
        var arithCtx = arg.Arith;
        var nonNumCtx = arg.Literal?.nonNumericLiteral();
        BoundNumericLiteral? writtenLit = arg.Literal?.numericLiteral() is { } numericLit ? host.Expr.NumericLiteralOperand(numericLit.GetText()) : null;
        if (boolCtx is not null && ConditionBinder.UnwrapBareBool(boolCtx) is { } bare)
        {
            boolCtx = null;
            arithCtx = bare.arithmeticExpression();
            nonNumCtx = bare.nonNumericLiteral();
        }
        // A SOLE numeric literal is a literal wherever it parsed. The grammar's own `literal` alternative wins
        // it when the boolean/arithmetic arms are not taken, and the two paths must agree: the literal arm
        // admits an unsigned integer into an ALPHANUMERIC formal by the MOVE rules, which the expression arm
        // (§14.8.2.3.3 rule 2a, category-numeric formals only) correctly does not.
        if (writtenLit is null && arithCtx is not null && ConditionBinder.SoleNumLiteral(arithCtx) is { } soleNum)
        {
            writtenLit = host.Expr.NumericLiteralOperand(soleNum);
            arithCtx = null;
        }

        // ⛔ THE IDENTIFIER CASE IS RECOVERED HERE, NOT IN THE GRAMMAR (fix-queue PB46). BY CONTENT's operand
        // list admits an arithmetic expression, and `arithmeticExpression` SUBSUMES `dataReference` — so a bare
        // `BY CONTENT A` now arrives as an expression, and routing it to the expression arm would silently drop
        // the §14.9.23.3 SR9/SR10 object-data rules, the §14.8.2.3.2 conformance check and the ref-mod handling
        // that only the identifier arm performs. The grammar cannot express "a reference, unless it is part of
        // an expression"; the binder can, through the SAME sole-reference reduction ConditionBinder and
        // IntrinsicBinder already use (feedback_one_rule_one_place — that helper is now shared, not re-copied).
        var dref = arg.Ref ?? ConditionBinder.SoleDataReference(arithCtx);
        // ⛔ A CONSTANT-NAME IS LITERAL-2, NEVER AN IDENTIFIER (kb/Work PB1137). ISO §13.10.3 SR2: "constant-name-1
        // may be used anywhere that a format specifies a literal of the class and category of constant-name-1", and
        // §14.9.23.2's BY CONTENT branch specifies literal-2 — so a bare or BY CONTENT constant-name is literal-2,
        // passed BY CONTENT (GR6 a) 2.: a literal never meets SR9). It used to be read as an identifier: bare, the
        // receiving chokepoint refused it as a receiving operand; under BY CONTENT the sending resolver, which knows
        // no constant-names, answered "not defined". Only an EXPLICIT BY REFERENCE keeps it identifier-3 — the one
        // branch whose operand is identifier-3 or OMITTED — and the receiving chokepoint's §13.10.4 GR1 refusal is
        // then the right verdict.
        BoundOperand? literal2 = null;
        string? constantName = null;
        if (dref is not null && !explicitReference && host.Expr.ConstantOperand(dref) is { } constantLiteral)
        {
            literal2 = constantLiteral;
            constantName = dref.GetText();
            dref = null;
            arithCtx = null;
        }
        // ⛔ AN INLINE METHOD INVOCATION IS AN IDENTIFIER, NOT AN EXPRESSION — the SAME lesson as the
        // sole-dataReference recovery on the line above, one identifier format later (kb/Work PB428).
        // §8.4.3.4.1: "Inline method invocation references a temporary data item returned from invocation of
        // a method", and §8.4.3.1.2 Format 4 makes it an identifier-2 of §14.9.23.2's argument list, never
        // arithmetic-expression-1. Left to parse alone it reaches the expression arm below, whose rule is
        // §14.8.2.3.3 rule 2a ("the same as for a COMPUTE statement") — category-numeric formals only — so
        // `O :: "ECHO" (O :: "GETNAME")` into a PIC X formal was REFUSED as legal source. Recovered here it
        // takes rule 2d's MOVE lane like any other identifier.
        // ⚠ IT IS STILL BY CONTENT, and that is GR6 a)2 rather than a convenience: §8.4.3.4.4 GR1 c) makes
        // the referenced item "a temporary item that exists for the purpose of effecting the inline
        // invocation in this way and for no other purpose", which is NOT "a data item defined in the file,
        // working-storage, local-storage, or linkage section" — so §14.9.23.3 SR9 is not met and GR6 a)2
        // assumes BY CONTENT.
        // An object-view (Format 5) is the same shape — an identifier referencing a temporary item (§8.4.3.5.4 GR1) —
        // and crosses the same way; SUPER is refused there by §8.4.3.8.3 SR3 (SELF was taken above, kb/Work PB1425).
        Place? inlinePlace = null;
        if (dref is null && ConditionBinder.SoleOoIdentifier(arithCtx) is { } soleInline)
        {
            if (OoBindOoIdentifier(soleInline) is not BoundNumRef inlineRef) return null;   // reported there
            inlinePlace = inlineRef.Place;
        }
        // ⛔ A FUNCTION-IDENTIFIER IS AN IDENTIFIER TOO — the third shape of the same recovery (kb/Work PB923).
        // §8.4.3.1.2 Format 1 makes `FUNCTION f(…)` an identifier, and §15.2 lets a function "be used anywhere a
        // sending data item of that class and category may be specified", so a sole function-identifier argument
        // is §14.9.23.2's identifier-5, not arithmetic-expression-1. Left to parse alone it took the expression
        // arm below, whose rule is §14.8.2.3.3 rule 2 a) — so `BY CONTENT FUNCTION TRIM(X)` into a PIC X formal
        // was REFUSED (COBOLNET0828, "requires a category-numeric formal") as legal source.
        // The lane is chosen the way §14.8.2.3.3 rule 2 chooses it, by the FORMAL: a numeric formal is rule 2 a)'s
        // COMPUTE lane, which the expression arm already IS for a sole function (a COMPUTE with the function as
        // its only operand), so only a non-numeric formal is re-routed — to rule 2 d)'s MOVE lane, over the §15.4
        // "temporary elementary data item" the ONE sending-value materializer provides (SendingValueTemp, the same
        // intermediate MOVE and EVALUATE use), whose store runs as a statement pre-op ahead of the activation.
        // BY CONTENT, like the inline temporary above: the §15.4 item is not "a data item defined in the file,
        // working-storage, local-storage, or linkage section", so §14.9.23.3 SR9 is not met and GR6 a)2 assumes it.
        string? foldedAlnum = null;
        if (dref is null && inlinePlace is null && formal.Pic?.Category is not PicCategory.Numeric
            && arithCtx is not null && ConditionBinder.SoleFunctionCall(arithCtx) is { } soleFn)
        {
            var fnOperand = host.Intrinsic.IntrinsicOperand(soleFn);
            if (fnOperand is BoundOperandError) return null;                                        // reported there
            // A function the binder FOLDED to its value (FUNCTION LENGTH of a fixed-length item, …) has no
            // temporary to materialize — its value is a compile-time constant, so it crosses through the literal
            // arms below, whose conformance screen is rule 2 d)'s MOVE question, never the expression arm.
            if (fnOperand is BoundNumericLiteral foldedNum) { writtenLit = foldedNum; arithCtx = null; }
            else if (fnOperand is BoundStringLiteral { Category: PicCategory.Alphanumeric } foldedText)
            { foldedAlnum = foldedText.Value; arithCtx = null; }
            // ⛔ A NUMERIC-typed function takes the SAME lane (kb/Work PB1007). It used to stay on the expression arm,
            // which refused `BY CONTENT FUNCTION INTEGER(N)` into a PIC X formal as legal source, because the §15.4
            // temporary moved as its 30-digit implementor description rather than as the function. The temporary now
            // IS the function for every MOVE (SendingValueTemp.OfComputed: an INTEGER function at scale 0, the text
            // image DOC-A.1-92's literal form), so rule 2 d)'s MOVE question is asked of it by the ONE chain
            // (OoConformance.ContentMismatch → Table 16): an INTEGER function (§15.2 item 5) conforms to an
            // alphanumeric formal and a NUMERIC one (item 4, the Noninteger row) does not — the answer
            // `MOVE FUNCTION f TO x` gets for the same pair.
            // A USER function's result IS a temporary data item (§8.4.3.2.4 GR1) -- an object reference, a pointer, an
            // alphanumeric item -- so it crosses as that place, like the inline invocation above (kb/Work PB1930: it fell
            // to the expression arm and an object-reference formal drew COBOLNET0828).
            else if (fnOperand is BoundFieldOperand { Place: { } resultTemp }) inlinePlace = resultTemp;
            else if (fnOperand is not BoundComputedOperand { Expr: BoundIntrinsicCall }) { }
            else if (host.SendingValue.Materialize(fnOperand, "invokearg") is { } fnTemp) inlinePlace = fnTemp;
            else
            {
                Err($"BY CONTENT function-identifier argument '{soleFn.GetText()}' for formal "
                    + $"'{formal.CobolName}': its returned value has no intermediate item to cross by");
                return null;
            }
        }
        if (dref is not null || inlinePlace is not null)
        {
            string argText = dref?.GetText() ?? DataBinder.WrittenText(arithCtx!);   // as written — `FUNCTION NUMVAL("3.7")`, never run together
            // ⛔ THE MODE IS DECIDED BEFORE THE OPERAND IS RESOLVED, BECAUSE THE MODE DECIDES ITS ROLE (kb/Work PB881,
            // PB1137). An explicit BY REFERENCE argument is identifier-3, "a receiving operand" (§14.9.23.3 SR20); an
            // explicit BY CONTENT one is identifier-5, "a sending operand" (SR21). A KEYWORD-LESS identifier is either,
            // and §14.9.23.4 GR6 a) chooses: BY REFERENCE "if the argument meets the requirements of Syntax rules 9
            // and 10", BY CONTENT otherwise. That test is the ONE storage-section question CALL's GR9 a) asks too
            // (ParameterConformance.MeetsByReferenceRules): a data item of the file, working-storage, local-storage
            // or linkage section that is not factory/instance object data. It used to be answered AFTER a receiving
            // resolution and only for object data, so an OBJECT PROPERTY — §8.4.3.9.4's conceptual temporary, in no
            // section — crossed BY REFERENCE and the method's write reached the property through its SET accessor,
            // and a report's PAGE-COUNTER crossed BY REFERENCE too. An inline invocation's or a function's
            // temporary (above) fails SR9 the same way and is BY CONTENT already.
            bool bareMeetsSr9 = dref is not null && inlinePlace is null && !explicitReference && !explicitContent
                && host.Params.MeetsByReferenceRules(dref);
            bool byReference = explicitReference || bareMeetsSr9;
            if ((inlinePlace ?? (dref is null ? null
                    : byReference ? host.Expr.ResolveReceiving(dref)
                    : host.Expr.ResolveSending(dref).PlaceOrReported(ctx.Edition))) is not { } place)
            {
                // A data reference's null is already reported, by the resolver or the receiving chokepoint (kb/Work
                // PB1030); only an argument that is neither a reference nor an inline invocation reaches Err.
                if (dref is null)
                    Err($"USING argument '{argText}' is not resolvable to storage (or uses a reference "
                        + "form not yet carried across INVOKE)");
                return null;
            }
            // §14.9.23.3 SR 10: object data (factory/instance WS) cannot cross BY REFERENCE — explicit
            // BY REFERENCE violates the rule; a BARE object-data identifier is assumed BY CONTENT (GR6a2, above).
            if (explicitReference && ctx.Data.OoIsObjectData(place.Item))
            {
                Err($"BY REFERENCE argument '{argText}' references OBJECT data — factory/instance "
                    + "working-storage may not cross an INVOKE by reference (ISO §14.9.23.3 SR 10); pass it "
                    + "BY CONTENT");
                return null;
            }
            // SR9 over the resolved identifier-3 and SR12's byte-alignment proof — the screens CALL's SR3/SR6 run,
            // from the ONE ParameterConformance the CALL and function activations ask (kb/Work PB1137).
            if (byReference)
            {
                if (!host.Params.ScreenSection(place, dref!, DiagnosticCatalog.InvokeOperandSection,
                        $"{verb} \"{methodName}\" USING argument", "§14.9.23.3 SR9", addressAdmitted: true))
                    return null;
                if (BitLayout.IsBitItem(place.Item))
                    host.Params.ScreenBitAlignment(place, DiagnosticCatalog.InvokeBitAlignment,
                        $"{verb} \"{methodName}\" USING argument", "§14.9.23.3 SR12");
            }

            // §14.9.23.3 SR13 over identifier-3 / identifier-5, before any conformance question (kb/Work PB1116).
            if (!ActiveClassSubordinateAdmitted(place, $"{verb} \"{methodName}\" USING argument")) return null;
            // §14.9.23.3 SR15 — a BY VALUE identifier-5 is of class numeric, object or pointer (message-tag is the MCS
            // module, not modeled). Asked of the operand's CLASS, the answer CALL's SR22 screen reads
            // (IntrinsicArgumentRules.ClassOf), before any conformance question.
            if (byValue && IntrinsicArgumentRules.ClassOf(new BoundFieldOperand(place)) is { } valueClass
                && valueClass is not (CobolClass.Numeric or CobolClass.Object or CobolClass.Pointer))
            {
                ctx.Edition.Error(DiagnosticCatalog.InvokeByValueOperandClass,
                    $"{verb} \"{methodName}\": BY VALUE operand '{argText}' is of class {valueClass.ToString().ToLowerInvariant()}; "
                    + "ISO §14.9.23.3 SR15 admits only class message-tag, numeric, object or pointer by value");
                return null;
            }
            // A reference-modified operand is a unique ELEMENTARY ALPHANUMERIC item of the window length
            // (§8.4.3.3.4 GR6): conformance goes against that effective description, never the whole inner item.
            if (place is RefModPlace rmp)
            {
                if (formal.IsGroup || formal.Pic?.Category is not PicCategory.Alphanumeric)
                {
                    Err($"reference-modified argument '{argText}': the operand is elementary "
                        + $"alphanumeric (§8.4.3.3.4 GR6) and does not conform to formal '{formal.CobolName}'");
                    return null;
                }
                if (byReference)
                {
                    // Strict identity needs a PROVABLE window length equal to the formal's.
                    if (rmp.Start.Int32Literal is null || rmp.Length?.Int32Literal is not { } rlen)
                    {
                        Err($"BY REFERENCE reference-modified argument '{argText}' needs a "
                            + "compile-time (start:length) to prove §14.8.2.3.2 conformance — pass it "
                            + "BY CONTENT or use literal subscripts");
                        return null;
                    }
                    if (!formal.IsAnyLength && rlen != formal.Pic.Length)   // ANY LENGTH: any window length matches (§14.8.2.3.2 rule d)
                    {
                        Err($"reference-modified argument window ({rlen}) does not match formal "
                            + $"'{formal.CobolName}' X({formal.Pic.Length}) (ISO §14.8.2.3.2)");
                        return null;
                    }
                }
                return new BoundInvokeArg(formal, place, null, null,
                    WriteBack: byReference, ByContent: !byReference);
            }

            if (byReference)
            {
                if (OoConformance.DescriptionMismatch(formal, place.Item, byRefGroupPrefix: true,
                        anyLengthActivationRelax: true, invokedWith) is { } err1)   // §14.8.2.3.2 rules d/e (ANY LENGTH); rule 4
                {
                    Err($"USING argument '{argText}' does not conform to formal parameter "
                        + $"'{formal.CobolName}': {err1} (ISO §14.8.2.3.2 — BY REFERENCE requires the "
                        + "identical description)");
                    return null;
                }
                return new BoundInvokeArg(formal, place, null, null, WriteBack: true);
            }

            // Effective BY CONTENT (§14.8.2.3.3): rule-per-formal-category.
            if (OoConformance.ContentMismatch(host.OoClasses, host.Set.PointerAssignmentReason, formal, place, invokedWith) is { } cerr)
            {
                Err($"BY CONTENT argument '{argText}' does not conform to formal "
                    + $"'{formal.CobolName}': {cerr} (ISO §14.8.2.3.3)");
                return null;
            }
            // §14.8.2.3.3 rule 2a is "the same as for a COMPUTE statement", and a COMPUTE takes any numeric sender
            // in either direction: the INVOKE marshalling carries the fixed-point⇄float and float⇄float CONTENT
            // conversions (OoEmitter's float landing through FloatResultant — kb/Work PB1114, which retired the
            // marshalling residue that used to refuse them here).
            return new BoundInvokeArg(formal, place, null, null, WriteBack: false, ByContent: true);
        }

        // ── BY CONTENT arithmetic-expression-1 (ISO §14.9.23.2; fix-queue PB46) ─────────────────────────────
        // The general format's BY CONTENT branch admits an arithmetic expression, and this arm is what makes
        // that true end to end. It is BY CONTENT by construction: §14.9.23.3 SR9 confines BY REFERENCE to an
        // identifier, and an expression has no storage to write back to.
        // ── BY CONTENT boolean-expression-1 (ISO §14.9.23.2; fix-queue PB46) ────────────────────────────────
        // The third operand shape the BY CONTENT branch admits, and the ONE the BY VALUE branch does not — the
        // two phrases genuinely differ in the printed general format. It is its own VALUE channel (D-B1: a
        // '0'/'1' bit string, §8.8.2), never the numeric one, which is why it needs a slot of its own rather
        // than a second spelling of ContentExpr.
        // ⛔ BOTH ASK THE ONE VALUE VERDICT (kb/Work PB1113): each is described as the sending operand it is — a
        // numeric COMPUTE sender, a boolean value — and asked §14.8.2.3.3 rule 2's question in rule 2's order
        // (OoConformance.ContentValueMismatch, the verdict the CALL and function lanes ask through
        // ParameterConformance.ContentConformanceReason). The boolean arm used to refuse a NATIONAL formal "on purpose",
        // where Table 16's boolean row says Yes.
        if (boolCtx is { } bx && (explicitContent || impliedContent))
        {
            if (OoConformance.ContentValueMismatch(formal, ContentValue.Boolean) is { } bErr)
            {
                Err($"BY CONTENT boolean-expression argument '{bx.GetText()}' for formal "
                    + $"'{formal.CobolName}': {bErr}");
                return null;
            }
            var bound = host.Cond.BindBoolExpr(bx);
            return new BoundInvokeArg(formal, null, null, null, WriteBack: false, ByContent: true)
                { ContentBool = bound };
        }

        if (arithCtx is { } ax && (explicitContent || impliedContent))   // a SOLE reference / numeric literal / inline invocation was taken above
        {
            if (OoConformance.ContentValueMismatch(formal, ContentValue.Arithmetic) is { } aErr)
            {
                Err($"BY CONTENT arithmetic-expression argument '{ax.GetText()}' for formal "
                    + $"'{formal.CobolName}': {aErr}");
                return null;
            }
            return new BoundInvokeArg(formal, null, null, null, WriteBack: false, ByContent: true)
                { ContentExpr = host.Expr.BindExpr(ax) };
        }

        // ── literal-2 (ISO §14.9.23.2) — BY CONTENT, because a literal never meets SR9 (§14.9.23.4 GR6 a) 2.) ──
        // ⛔ THE ONE LITERAL MAPPING, NOT A PRIVATE DECODE (kb/Work PB1137). This arm used to decode STRINGLIT, an
        // alphanumeric concatenation, BOOLLIT and a numeric literal by hand — a fourth copy of the §8.3.3 mapping
        // ExpressionBinder.NonNumericLiteralOperand exists to be the only copy of (kb/Work DA3) — and the copy had
        // no hexadecimal arm, no national arm and no length screen: X"4142434445" and N"ABCDE" (legal literal-2)
        // fell to the trailing "not yet carried" refusal while "" (SR17) was ACCEPTED. The shared mapping hands back
        // the literal's VALUE and CATEGORY, so each lane below asks its conformance rule of the category the literal
        // actually has. Per §9.3.6 resolution rule 5 a literal that would TRUNCATE still conforms (the SET/MOVE
        // no-truncation requirements are ignored for literal arguments), so an overflow converts per the MOVE
        // rules rather than erroring. A constant-name arrived above already substituted (§13.10.4 GR1).
        literal2 ??= foldedAlnum is not null ? new BoundStringLiteral(foldedAlnum)
            : nonNumCtx is not null ? host.Expr.NullAdmittingOperand(nonNumCtx)   // §8.4.3.10.3 SR1 a): a method-invocation argument
            : writtenLit is not null ? writtenLit
            : null;
        // Into a numeric formal §14.8.2.3.3 rule 2a's COMPUTE reads the figurative constant ZERO as the numeric value
        // zero — §8.3.3.6.3 SR1 a) makes ZERO the one figurative a numeric literal's position admits, and §8.8.1.1 names
        // it among a COMPUTE's operands — so it takes the numeric-literal lane (its verdict and its carrier split), and
        // only a character-carried formal takes the fill below (kb/Work PB1617).
        literal2 = ParameterConformance.ArgumentForFormal(literal2, formal);
        string literalText = constantName ?? nonNumCtx?.GetText() ?? writtenLit?.Image ?? foldedAlnum ?? "";
        switch (literal2)
        {
            case BoundStringLiteral { Value.Length: 0 }:
                // §14.9.23.3 SR17 — and, through §8.4.3.4.3 SR3, the inline form's arguments too (this body is both).
                ctx.Edition.Error(DiagnosticCatalog.InvokeArgumentZeroLengthLiteral,
                    $"{verb} \"{methodName}\": the argument {literalText} for formal '{formal.CobolName}' is a "
                    + "zero-length literal; ISO §14.9.23.3 SR17: \"Literal-2 shall not be a zero-length literal\"");
                return null;
            case BoundStringLiteral { Category: PicCategory.Boolean } boolLit:
                // A BOOLEAN literal (or a boolean concatenation expression, §8.8.3.3 GR3) is a boolean VALUE with no
                // storage, so it rides the boolean channel — Table 16's BOOLEAN row (§14.8.2.3.3 rule 2d), the same
                // receivers the boolean-expression arm takes, from the SAME rule. A LITERAL contributes no item width
                // to §8.8.2 rule 10, so the value crosses at the formal's width (BooleanRenderer.RenderAtItemWidth).
                if (OoConformance.ContentValueMismatch(formal, ContentValue.Boolean) is { } blErr)
                {
                    Err($"boolean literal argument {literalText} for formal '{formal.CobolName}': {blErr}");
                    return null;
                }
                return new BoundInvokeArg(formal, null, null, null, WriteBack: false, ByContent: true)
                    { ContentBool = new BoundBoolLiteral(boolLit.Value) };
            case BoundStringLiteral textLit:
                // An ALPHANUMERIC literal — plain or hexadecimal (§8.3.3.2 makes X"…" a FORMAT of the alphanumeric
                // literal) — or a NATIONAL one (§8.3.3.5): the §14.8.2.3.3 verdict the CALL and function activations
                // get for the same literal (rule 2a's COMPUTE into a numeric formal, else rule 2d's MOVE asked of the
                // literal's own category — a national literal does not move to an alphanumeric formal, Table 16).
                if (host.Params.ContentConformanceReason(formal, LiteralArg(textLit)) is { } tErr)
                {
                    Err($"nonnumeric literal argument {literalText} for formal '{formal.CobolName}': {tErr}");
                    return null;
                }
                return new BoundInvokeArg(formal, null, null, textLit.Value, WriteBack: false, ByContent: true);
            case BoundNumericLiteral numLit:
                if (host.Params.ContentConformanceReason(formal, LiteralArg(numLit)) is { } nErr)
                {
                    Err($"numeric literal argument {numLit.Text} for formal '{formal.CobolName}' — {nErr}");
                    return null;
                }
                // The numeric literal crosses as ITSELF into an elementary formal: rule 2a's COMPUTE for a numeric
                // formal, rule 2d's MOVE for any other — the emitter stores it through the receiving category's ONE
                // MOVE store (kb/Work PB1113: it used to cross as its digit TEXT, which was right only for an
                // unsigned integer into PIC X — `-5` keeps no sign there, §14.9.25.4 GR6, and `12.5` edits into a
                // numeric-edited mask). A GROUP formal keeps the character copy §14.9.25.4 GR4 makes a group move.
                return formal.IsGroup
                    ? new BoundInvokeArg(formal, null, null, numLit.Text, WriteBack: false, ByContent: true)
                    : new BoundInvokeArg(formal, null, numLit.Text, null, WriteBack: false, ByContent: true);
            case BoundPredefinedNull:
                // ⛔ NULL IS AN IDENTIFIER, identifier-5 (kb/Work PB1137 + PB1630): §8.4.3.1.3 SR7 lists the
                // predefined-object references among the identifier formats, §8.4.3.7.3 SR2 describes the NULL object
                // reference as "class object and category object reference", and §8.4.3.10.3 SR1 a) admits the NULL
                // address of class pointer "as an argument in … a method invocation" — so `USING [BY CONTENT] NULL`
                // passes the formal's null BY CONTENT (it is in no DATA DIVISION section, so GR6 a) 2. assumes
                // CONTENT). Its conformance is the ONE §14.8.2.3.3 verdict the CALL and function lanes ask: a formal of
                // class pointer or object reference takes it by the SET rules, and every other formal refuses it.
                if (host.Params.ContentConformanceReason(formal, LiteralArg(literal2)) is { } nullErr)
                {
                    Err($"argument NULL for formal '{formal.CobolName}': {nullErr}");
                    return null;
                }
                // An ACTIVE-CLASS formal: a SET of NULL is valid into either alternative's receiver (§14.9.39.3 SR14
                // c) and SR12 d)), so what remains of §14.8.2.3.3 is the invocation condition (kb/Work PB1112).
                if (formal.Pic?.ObjectRef is { Kind: ObjectRefKind.ActiveClass }
                    && !OoConformance.InvokedThroughActiveClass(invokedWith)
                    && !OoConformance.InvokedThroughOnlyClass(invokedWith))
                {
                    Err($"argument NULL for ACTIVE-CLASS formal '{formal.CobolName}': the method is invoked with "
                        + $"{invokedWith.Spelled} — §14.8.2.3.3 requires it to be invoked with SELF, SUPER or an "
                        + "ACTIVE-CLASS reference, or with an object-class-name or a reference described with one and ONLY");
                    return null;
                }
                return new BoundInvokeArg(formal, null, null, null, WriteBack: false, ByContent: true)
                    { PredefinedNull = true };
            case BoundFigurative or BoundAllLiteral:
                // ⛔ A FIGURATIVE CONSTANT IS literal-2 TOO (kb/Work PB1617): §8.3.3.6.3 SR1 — "A figurative constant
                // may be used whenever 'literal' appears in a format" — and this switch refused every one but NULL as
                // "not yet carried". It takes the shared §14.8.2.3.3 verdict (rule 2a refuses any but ZERO into a
                // numeric formal — ZERO arrived above as the numeric literal 0; rule 2d / §14.8.2.2 rule 2 ask the
                // MOVE) and crosses on its own fill channel, which §14.2.3 GR9 sizes at the method's formal
                // (CallEmitter.FigurativeArgumentImage).
                if (host.Params.ContentConformanceReason(formal, LiteralArg(literal2)) is { } fErr)
                {
                    Err($"figurative-constant argument {literalText} for formal '{formal.CobolName}': {fErr}");
                    return null;
                }
                return new BoundInvokeArg(formal, null, null, null, WriteBack: false, ByContent: true)
                    { ContentFill = literal2 };
            case BoundOperandError { IsUnbuilt: false }:
                return null;   // refused, and the rule it breaks reported where the operand was bound
        }
        Err($"argument form for formal '{formal.CobolName}' is not yet carried across a method activation");
        return null;
    }

    /// <summary>Bind an INVOKE through a UNIVERSAL receiver (D10/D-U5; §14.9.23.4 GR7c): no compile-time
    /// conformance — each argument and the RETURNING item carry their CONFORMANCE DESCRIPTOR for the
    /// callee's runtime check (§9.3.8.2.1 NOTE). Argument rules, all COBOLNET0866 with citations: explicit
    /// BY CONTENT/BY VALUE are forbidden (SR6 — BY REFERENCE is assumed implicitly); a literal or
    /// arithmetic-expression argument cannot cross by reference (SR6 + GR6's non-universal-only scope);
    /// OBJECT data may not cross at all (SR10 bans by-reference and SR6 removes the typed path's GR6a2
    /// auto-CONTENT fallback); a Tier-C group (no character image) has no crossing form. An ADDRESS-IDENTIFIER
    /// is identifier-3 by SR9 and a SENDING operand by SR19, so it crosses as its pointer value with a class-pointer
    /// descriptor and is never copied out. Every identifier-3 and the RETURNING item take the storage-section
    /// (SR9/SR11) and bit-alignment (SR12) screens CALL's SR3/SR6/SR7/SR8 take, from
    /// <see cref="ParameterConformance"/> (kb/Work PB1137).</summary>
    private BoundStatement OoBindUniversalInvoke(
        InvocationSite site, Place receiver, string? methodLiteral, Place? methodSource)
    {
        // §8.4.3.4.3 SR2 bars a universal receiver from the INLINE form outright, and OoBindInlineInvocation
        // reports it there for EVERY segment's receiver (kb/Work PB1429) — so this path is reached only by the
        // INVOKE statement, and an inline site here would end in the internal-error refusal.
        System.Diagnostics.Debug.Assert(!site.ReturningImplicit,
            "a universal receiver reached the inline-invocation path past the §8.4.3.4.3 SR2 screen");
        var argCtxs = site.Args;
        var args = new List<BoundUniversalArg>(argCtxs.Count);
        foreach (var a in argCtxs)
        {
            // §14.9.23.2's OMITTED operand (kb/Work PB757) is the BY REFERENCE branch's, so SR6 admits it; with no
            // formal known until runtime, §14.9.23.3 SR18's OPTIONAL requirement is the callee switch's GR7c check.
            if (a.Omitted)
            {
                args.Add(new BoundUniversalArg(null, CobolNet.Runtime.ActivationDescription.Omitted));
                continue;
            }
            if (a.ByValueWritten || a.ByContentWritten)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0866",
                    "INVOKE through a universal object reference: neither BY CONTENT nor BY VALUE may be "
                    + "specified — BY REFERENCE is assumed implicitly (ISO §14.9.23.3 SR6)");
            }
            // ⛔ THE ADDRESS-IDENTIFIER ARM THE TYPED PATH GAINED IN kb/Work PB1021 AND THIS ONE NEVER DID (kb/Work
            // PB1137 — the two-arm dispatch). §14.9.23.3 SR9: "Identifier-3 shall be an address-identifier or …", and
            // SR6 makes every universal argument identifier-3 (BY REFERENCE "is assumed implicitly"), so
            // `INVOKE U "M" USING ADDRESS OF X` is legal; it fell to the literal arm below and was refused with a
            // message about a literal. SR19 makes it a SENDING operand: its pointer VALUE crosses in the box under
            // its class-pointer descriptor (the §14.9.23.4 GR7c check at the callee decides conformance) and the
            // box is never copied back.
            if (a.Address is { } addrCtx)
            {
                if (host.Ptr.BindAddressIdentifier(addrCtx, "INVOKE … USING") is not { } ao)
                    return BoundRejected.Reported(ctx.Edition);
                args.Add(new BoundUniversalArg(null,
                    ActivationDescriptions.OfAddress(ao, ao.Program?.Prototype is { } prototype ? host.ProgramRestrictionIdentityOf(prototype) : null))
                { Address = ao });
                continue;
            }
            if (a.Ref is not { } dref)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0866",
                    "INVOKE through a universal object reference: a literal or arithmetic-expression "
                    + "argument cannot cross BY REFERENCE (ISO §14.9.23.3 SR6 — every universal argument "
                    + "is implicitly BY REFERENCE)");
            }
            if (host.Expr.ResolveReceiving(dref) is not { } p)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0866",
                    $"INVOKE: the argument '{DataBinder.WrittenText(dref)}' is not resolvable to storage");
            }
            // SR9 and SR12 over identifier-3 — with no GR6 a) 2. fallback here (SR6), a special register or a
            // compiler temporary is refused rather than passed BY CONTENT.
            if (!host.Params.ScreenSection(p, dref, DiagnosticCatalog.InvokeOperandSection,
                    "INVOKE USING argument (through a universal object reference, BY REFERENCE by SR6)",
                    "§14.9.23.3 SR9", addressAdmitted: true))
                return BoundRejected.Reported(ctx.Edition);
            if (BitLayout.IsBitItem(p.Item))
                host.Params.ScreenBitAlignment(p, DiagnosticCatalog.InvokeBitAlignment, "INVOKE USING argument",
                    "§14.9.23.3 SR12");
            if (!ActiveClassSubordinateAdmitted(p, "INVOKE USING argument")) return BoundRejected.Reported(ctx.Edition);
            if (ctx.Data.OoIsObjectData(p.Item))
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0866",
                    $"INVOKE through a universal object reference: '{p.Item.CobolName}' is OBJECT "
                    + "(factory/instance) data — it may not cross BY REFERENCE (ISO §14.9.23.3 SR10), and "
                    + "the universal path has no BY CONTENT fallback (SR6)");
            }
            // A REFERENCE-MODIFIED argument is the unique data item §8.4.3.3.4 GR5/GR6 create, never the description of
            // the item it windows (it used to cross under identifier-1's whole description).
            if ((p is RefModPlace rm ? ActivationDescriptions.OfReferenceModification(rm) : ActivationDescriptions.Of(p.Item))
                is not { } d)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0866",
                    $"INVOKE: the argument '{p.Item.CobolName}' has no crossing form — "
                    + (p.Item.IsGroup ? TierCIsland.Reason(p.Item, "argument group") : "it has no PICTURE description"));
            }
            args.Add(new BoundUniversalArg(p, d));
        }

        Place? retPlace = null;
        CobolNet.Runtime.ActivationDescription? retDesc = null;
        if (site.ReturningRef is { } retRef)
        {
            if (host.Expr.ResolveReceiving(retRef) is not { } rp)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0866",
                    $"INVOKE RETURNING '{retRef.GetText()}': the receiving identifier is not resolvable "
                    + "to storage");
            }
            if (!OoScreenReturning(rp, retRef, methodName: null)) return BoundRejected.Reported(ctx.Edition);
            retDesc = ActivationDescriptions.Of(rp.Item);
            if (retDesc is null)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0866",
                    $"INVOKE RETURNING '{rp.Item.CobolName}': no crossing form — "
                    + (rp.Item.IsGroup ? TierCIsland.Reason(rp.Item, "returning group") : "it has no PICTURE description"));
            }
            retPlace = rp;
        }
        // GR2a/§8.3.2.2: the selector is a user-defined word — normalize the LITERAL at bind time through the SAME
        // mapping the identifier-2 value takes at run time and every case label is built from (OoMethodSymbol.DispatchKey).
        return new BoundInvokeUniversal(receiver, methodLiteral is null ? null : CobolNet.Runtime.CobolObject.NormalizeMethodName(methodLiteral), methodSource,
            args, retPlace, retDesc);
    }

    /// <summary>What refuses SELF as the SENDING operand of an object-reference assignment into a receiver of
    /// description <paramref name="rd"/> — ISO §14.9.39.3 SR8/SR10 d)/SR12 c)/SR14 b), read by the SET statement
    /// (Format 5) and by an INVOKE argument, whose BY CONTENT object-reference crossing conforms "as if a SET
    /// statement were performed" (§14.8.2.3.3; kb/Work PB1137). Empty when SELF conforms.</summary>
    private IEnumerable<string> SelfSenderRefusals(ObjectRefDescriptor rd, OoClassSymbol cur)
    {
        switch (rd.Kind)
        {
            case ObjectRefKind.Universal:
                yield break;   // SR8 — a universal receiver accepts any object

            case ObjectRefKind.Interface:
                // `Find` is class-only, so before the SR10d landing an interface-typed receiver fell
                // through unchecked and the emitter rendered a raw `(I)(this)` cast — a runtime
                // InvalidCastException, or a Roslyn CS error on generated user source for a sealed
                // class, which the G4 no-CS-on-user-source rule forbids.
                if (host.OoClasses?.FindInterface(rd.Name!) is { } tiface
                    && !host.OoClasses.ImplementsClosure(cur, host.OoInFactory).Contains(tiface))
                    yield return
                        $"the {(host.OoInFactory ? "factory" : "instance")} "
                        + $"definition of class '{cur.Name}' does not IMPLEMENT interface '{tiface.Name}' "
                        + $"(ISO §14.9.39.3 SR10d{(host.OoInFactory ? 1 : 2)})";
                yield break;

            case ObjectRefKind.ObjectClass:
                // c)1. — an ONLY receiver admits no SELF sender at all: SELF's run-time class is the
                // ACTIVE class, which may be a subclass, and ONLY forbids exactly that.
                if (rd.Only)
                    yield return
                        $"the receiving item is described with the ONLY phrase, so SELF is not "
                        + "a permitted sending operand (ISO §14.9.39.3 SR12c1)";
                // c)2. — the class containing the SET statement shall be the receiver's class or a
                // subclass of it.
                else if (host.OoClasses?.Find(rd.Name!) is { } tcls && !cur.ConformsTo(tcls))
                    yield return
                        $"class '{cur.Name}' is not '{tcls.Name}' or a "
                        + "subclass of it (ISO §14.9.39.3 SR12c2)";
                // c)3./c)4. — the FACTORY axis of the receiver picks WHICH definition the method shall
                // be defined in, and SELF is the object of that definition.
                if (rd.Factory != host.OoInFactory)
                    yield return
                        $"the receiving item is described {(rd.Factory ? "with" : "without")} "
                        + "the FACTORY phrase, so the method containing the SET statement shall be "
                        + $"defined in the {(rd.Factory ? "factory" : "instance")} definition of its "
                        + $"containing class (ISO §14.9.39.3 SR12c{(rd.Factory ? 4 : 3)})";
                yield break;

            default:   // ObjectRefKind.ActiveClass — SR14 b)
                if (rd.Factory != host.OoInFactory)
                    yield return
                        $"the receiving item is described ACTIVE-CLASS "
                        + $"{(rd.Factory ? "with" : "without")} the FACTORY phrase, so the method "
                        + $"containing the SET statement shall be defined in the "
                        + $"{(rd.Factory ? "factory" : "instance")} definition of its containing class "
                        + $"(ISO §14.9.39.3 SR14b{(rd.Factory ? 2 : 1)})";
                yield break;
        }
    }

    /// <summary>SET Format 5 core (§14.9.39; D-U7) — shared by the grammar's NULL/SELF/SUPER-sender rule
    /// and BindSetTo's SEMANTIC re-route (a dataReference sender parses as the Format-1 shape). Rules, all
    /// COBOLNET0867: every target an object-reference item (SR8 :31298); SUPER sender rejected (SR9
    /// :31300); SELF only inside a method, and a TYPED target requires the current class to conform
    /// (SR12c :31353); a dataReference sender must be an object-reference item, and a TYPED target
    /// requires a TYPED, conforming sender (SR12a2 :31341 — universal-into-typed is OUTSIDE SR12's closed
    /// list: the narrowing tool is an object view, the EC-OO wave); a UNIVERSAL target is unconstrained
    /// (SET universal TO typed is unconditionally legal). An unresolvable sender that names a CLASS of the
    /// group is the SR13 factory-object form — the factory singleton reference (D11 makes it directly
    /// emittable).</summary>
    public BoundStatement OoBindSetObjectRef(
        IReadOnlyList<Core.DataReferenceContext> targetRefs,
        SetSender? sender, bool senderNull, bool senderSelf, bool senderSuper,
        string? senderText = null)
    {
        // SET … TO object-reference (§14.9.39 Format 5) is a COBOL-2002 introduction; the edition gate moved to the
        // post-bind VersionConformancePass (PHASE-03 Step 14b) — it fires on the self-identifying BoundSetObjectRef
        // node this convergence point (NULL/SELF/SUPER route + the data-sender re-route) produces.
        if (senderSuper)
        {
            // ⛔ NAME THE RECEIVERS (kb/Work PB388's elision sweep): `targetRefs` is in hand, and the message
            // opened `SET … TO SUPER` — which the diagnostic renderer transliterates to `SET . TO SUPER`.
            return BoundRejected.Report(ctx.Edition, "COBOLNET0867",
                $"SET {SetFormatSelection.Written(targetRefs)} TO SUPER: SUPER shall not be the sending operand "
                + "of an object-reference SET (ISO §14.9.39.3 SR9)");
        }
        var targets = new List<Place>(targetRefs.Count);
        foreach (var t in targetRefs)
        {
            if (ctx.Refs.IsExceptionObjectRegister(t))
            {
                // ⛔ ONE RULE, ONE CODE (kb/Work PB922): the same §8.4.3.6.3 SR1 the general receiving chokepoint
                // now screens (ExpressionBinder.ResolveReceiving). The clause number was §8.4.3.6 here and that
                // is NOT where the rule is — `cite.py --check 8.4.3.6 "EXCEPTION-OBJECT shall not be specified as
                // a receiving operand"` FAILS and `--check 8.4.3.6.3` passes as SR1.
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ExceptionObjectReceiving,
                    "SET EXCEPTION-OBJECT: the predefined object reference shall not be a receiving "
                    + "operand (ISO §8.4.3.6.3 SR1)");
            }
            // ⛔ THREE ARMS, AND THE ORDER IS THE POINT — SR8 and "the name identifies nothing" are DIFFERENT
            // rules and each is reported once, by itself. The former single `Resolve(t) is not { } tp || …`
            // arm reported BOTH for one operand: Resolve names the unidentified reference (COBOLNET1639) and
            // SR8 was then stacked on top of it, so an undefined name drew a rule about a category nobody could
            // read. Worse for an INDEX-NAME: §13.18.38.3 SR7 lists "the SET statement" among the five contexts
            // where index-name-1 may be written, and an index-name is not a data reference, so the resolver
            // cannot resolve one — `SET IX TO U` produced a FALSE "'IX' is not defined" about a name the
            // program's INDEXED BY phrase declares, which is the class kb/Work PB457 ended.
            bool indexName = host.Expr.IndexFieldOf(t) is not null;
            var probe = indexName ? null : ctx.Refs.Probe(t);            // R30: the probe never diagnoses
            if (!indexName && probe is null)
            {
                host.Expr.ResolveReceiving(t);                                      // ISO §8.4.2.1 — the resolver's own rule
                return new BoundUnsupported("SET object-reference receiving operand");
            }
            if (indexName || probe!.Value.Item.Pic is not { Category: PicCategory.ObjectReference })
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0867",
                    $"SET '{t.GetText()}': the receiving operand of an object-reference SET shall be a "
                    + "USAGE OBJECT REFERENCE data item (ISO §14.9.39.3 SR8)");
            }
            if (host.Expr.ResolveReceiving(t) is not { } tp) return new BoundUnsupported("SET object-reference receiving operand");   // reported by the resolver
            targets.Add(tp);
        }

        Place? src = null;
        string? srcFactoryClassCs = null;
        if (senderSelf)
        {
            if (host.OoCurrentClass is not { } cur)
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0867",
                    // ⛔ INHERITED CITATION, RE-DERIVED (kb/Work PB388). This cited §14.9.39.3 SR12 c), which
                    // answers a DIFFERENT question: SR12 governs a receiver described with an OBJECT-CLASS-NAME,
                    // and its c)3./c)4. are about factory-vs-instance PLACEMENT of a method that exists. The
                    // rule that SELF needs a method at all is the identifier's own — §8.4.3.8.3 SR1, "This
                    // identifier format may be specified only in a method definition" — and it is the rule this
                    // arm enforces, for EVERY receiver description including the universal one SR12 never reaches.
                    $"SET {SetFormatSelection.Written(targetRefs)} TO SELF: SELF is defined only within a method "
                    + "definition (ISO §8.4.3.8.3 SR1)");
            }
            // ⛔ THE RECEIVER'S §13.18.60.2 DESCRIPTION DECIDES WHICH RULE GOVERNS A SELF SENDER — one arm per
            // general-format alternative, and all four are present (kb/Work PB389; before it the descriptor
            // could spell only two of them and the ONLY / FACTORY axes had nowhere to be read):
            //   universal      — SR8: unconstrained.
            //   interface-name — SR10 d)1./d)2.: the factory (in a factory method) or instance (in an instance
            //                    method) definition of the containing class shall IMPLEMENT int-1.
            //   object-class   — SR12 c)1.–c)4.
            //   ACTIVE-CLASS   — SR14 b)1./b)2.
            // SR12 c)3./c)4. and SR14 b)1./b)2. are the SAME sentence about the SAME axis: SELF is the factory
            // object inside a factory method and an instance object inside an instance one, so the receiver's
            // FACTORY presence shall equal host.OoInFactory.
            foreach (var tp in targets)
                foreach (string why in SelfSenderRefusals(tp.Item.Pic!.ObjectRef ?? ObjectRefDescriptor.Universal, cur))
                    ctx.Edition.Error("COBOLNET0867", $"SET '{tp.Item.CobolName}' TO SELF: {why}");
        }
        else if (!senderNull)
        {
            // ⛔ SR9 IS THE ANSWER FOR A SENDER THAT IS NOT A REFERENCE (kb/Work PB456). Format 5 is selected
            // from the RECEIVING list (§14.9.39.2; SetFormatSelection), so `SET U TO 5` and `SET U TO N + 1`
            // reach this bind with no sender identifier instead of silently declining a re-route and landing an
            // object reference in the Format-1 arithmetic store — which is what made both COMPILE CLEAN and
            // abort at run time. Identifier-4 "shall be an object reference"; a literal is not one.
            if (sender is not { IsIdentifierOperand: true })
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0867",
                    $"SET {SetFormatSelection.Written(targetRefs)} TO {senderText}: "
                    + "identifier-4 shall be an object reference — the sending operand of an object-reference "
                    + "SET is an object-reference data item, a function-identifier or inline method invocation of "
                    + "class object, object-class-name-1, NULL or SELF, never a literal "
                    + "or an arithmetic expression (ISO §14.9.39.2 Format 5, §14.9.39.3 SR9)");
            }
            // ⛔ A FUNCTION-IDENTIFIER OR INLINE INVOCATION IS identifier-4 TOO (kb/Work PB1929): §8.4.3.2.1 and
            // §8.4.3.4.4 GR1 make each a reference to a data item whose class and category are the result's, so a
            // user function RETURNING USAGE OBJECT REFERENCE sends exactly as an object-reference data item does —
            // through ITS returned temporary, bound once by the sender classifier. Only a Ref is subject to the
            // register / class-name readings below, which are properties of a WRITTEN name.
            var senderRef = sender.Ref;
            // ⛔ THE PREDEFINED REGISTER IS CLASSIFIED BEFORE THE GENERAL LOOKUP, and the ORDER is the rule
            // (kb/Work PB922; the same order SetFormatSelection.KindOf keeps on the receiving side). It used to
            // sit BELOW the resolved-sender arm and reach only because the resolver did not know the name — so
            // the moment the resolver learned it (§8.4.3.6.3 SR2), the general arm claimed EXCEPTION-OBJECT and
            // screened it against §14.9.39.3 SR12's closed list, refusing `SET <typed> TO EXCEPTION-OBJECT` at
            // COMPILE time. That is a rejection of legal source: the register is implicitly UNIVERSAL, and a
            // typed receiver is answered by the RUN-TIME narrow check below, not by SR12.
            if (senderRef is not null && ctx.Refs.IsExceptionObjectRegister(senderRef))
                // §8.4.3.6 — the predefined register (ONE per run unit, GR2; implicitly universal SR2):
                // a universal target copies the reference; a TYPED target gets the RUNTIME narrow check
                // in the emitter (§9.3.8.2 :12291 — EC-OO-UNIVERSAL on failure; the SR12 closed list is
                // satisfied through the object-view-equivalent runtime conformance this register carries).
                return new BoundSetObjectRef(targets, null, false, false) { FromExceptionObject = true };
            // Probe, then RESOLVE to commit, because a probe's Place is unscreened and must never enter the
            // bound tree (kb/Work PB221). A function-identifier / invocation sender has no probe to take: its
            // temporary was committed when the classifier bound it, and that Place is what enters the tree.
            Place? identifierPlace = senderRef is null ? sender.TemporaryItem : null;
            var sniff = senderRef is not null ? ctx.Refs.Probe(senderRef) : null;
            DataItem? senderItem = sniff?.Item ?? identifierPlace?.Item;
            if (senderItem is { Pic: { Category: PicCategory.ObjectReference } spic })
            {
                Place? sp;
                if (senderRef is null) sp = identifierPlace;
                else if (host.Expr.ResolveSending(senderRef).PlaceOrReported(ctx.Edition) is { } resolved) sp = resolved;
                else return BoundRejected.Reported(ctx.Edition);   // the committed answer (kb/Work PB1030)
                // The receiver's description selects SR10 / SR12 / SR14 and the sender's answers it — ONE
                // table, OoConformance.ObjectRefAssignmentMismatch. The former `ObjectClassName is not null`
                // pre-guard is gone: the table returns null for a universal receiver itself (SR8), so the
                // guard was a second, weaker copy of that rule.
                string senderName = senderRef is null ? sender.Text : senderItem.CobolName ?? sender.Text;
                foreach (var tp in targets)
                    if (OoConformance.ObjectRefAssignmentMismatch(host.OoClasses, spic, tp.Item.Pic!) is { } werr)
                        ctx.Edition.Error("COBOLNET0867",
                            $"SET '{tp.Item.CobolName}' TO '{senderName}': {werr}");
                src = sp;
            }
            // SR13's class-name-1 sender is a source reference and takes the §8.4.6.4 scope (PB365).
            else if (senderRef?.cobolWord()?.GetText() is { } sname
                     && Compiler.Oo.OoNameResolution.Lookup(host.OoClasses, senderRef, sname,
                            Compiler.Oo.OoNameResolution.Want.Class).Class is { } scls)
            {
                // SR11 + SR13: the sender names a CLASS → the FACTORY OBJECT of that class (D11's singleton
                // makes it a direct reference). ⛔ BOTH RULES FALL OUT OF THE ONE TABLE once the sender is
                // written as the description that factory object actually has — the factory object OF EXACTLY
                // object-class-name-1, i.e. FACTORY OF <sname> ONLY:
                //   • SR13's "the data item shall be described with the FACTORY phrase" is the table's SR12 a)3.
                //     FACTORY-presence equality against a sender whose Factory is true;
                //   • SR13 a) (ONLY receiver ⇒ the same object-class-name) is SR12 a)1., which the ONLY sender
                //     satisfies exactly when the names match;
                //   • SR13 b) (otherwise, the same class or a subclass) is SR12 a)2.;
                //   • SR11 (an interface-name receiver ⇒ the FACTORY object of object-class-name-1 IMPLEMENTS
                //     int-1) is the table's SR10 b)1., which asks the factory closure for exactly that.
                // Before kb/Work PB389 every typed receiver was refused here, because no FACTORY axis existed
                // to compare — the rejection WAS the rule's only enforcement.
                // ⛔ The SENDER's identity is §14.9.39.4 GR10 — "If object-class-name-1 is specified, a
                // reference to the factory object of the class identified by object-class-name-1 is placed
                // into each data item referenced by identifier-3 in the order specified" — NOT SR13, whose
                // own precondition ("the data item referenced by identifier-3 is described with an
                // object-class-name") is FALSE for an interface-name or ACTIVE-CLASS receiver.  Which syntax
                // rule governs is therefore READ OFF THE RECEIVER, never hard-coded (kb/Work PB451).
                var senderDesc = ObjectRefDescriptor.ObjectClass(scls.Name, factory: true, only: true);
                foreach (var tp in targets)
                {
                    var rdesc = tp.Item.Pic!.ObjectRef ?? ObjectRefDescriptor.Universal;
                    if (OoConformance.ObjectRefAssignmentMismatch(host.OoClasses!, senderDesc, rdesc) is { } ferr)
                    {
                        return BoundRejected.Report(ctx.Edition, "COBOLNET0867",
                            $"SET '{tp.Item.CobolName}' TO {sname}: object-class-name-1 sends the FACTORY "
                            + $"OBJECT of class '{scls.Name}' (ISO §14.9.39.4 GR10) and the receiver's "
                            + $"description puts the statement under {OoConformance.ClassNameSenderRule(rdesc.Kind)} "
                            + $"— {ferr}");
                    }
                }
                srcFactoryClassCs = scls.FactoryCsName;
            }
            else
            {
                return BoundRejected.Report(ctx.Edition, "COBOLNET0867",
                    // NAME THE RECEIVERS (kb/Work PB388): the renderer transliterates U+2026, so this read
                    // `SET . TO 'WX'` — a statement nobody wrote — and the receivers are in hand.
                    $"SET {SetFormatSelection.Written(targetRefs)} TO "
                    + $"'{sender.Text}': the sending operand shall be an object-reference "
                    + "data item, a function-identifier or inline method invocation of class object, NULL, SELF, "
                    + "or a class-name (ISO §14.9.39.3 SR9/SR12/SR13)");
            }
        }
        return new BoundSetObjectRef(targets, src, senderNull, senderSelf) { SourceFactoryCs = srcFactoryClassCs };
    }

    /// <summary>⛔ THE ONE SPELLING TEST for the predefined object reference <c>EXCEPTION-OBJECT</c>
    /// (ISO §8.4.3.6).
    /// <para>It is a WORD, not a token: the grammar reserves NULL, SELF and SUPER (<c>objectReference</c>) but
    /// spells EXCEPTION-OBJECT as an ordinary <c>cobolWord</c>, so every reader of a written reference has to
    /// ask this question of the TEXT. §8.4.3.6.3 SR2 gives the answer's content — "EXCEPTION-OBJECT is
    /// implicitly described as class object and category object reference, as an external data item, and as a
    /// universal object reference" — and because no data description entry declares it, a reader that does NOT
    /// ask gets "not defined" from the ordinary resolver, which is false about a name the standard declares.
    /// That was the shape of the false COBOLNET1639 on <c>SET EXCEPTION-OBJECT TO E</c>.</para>
    /// <para>⛔ A WRITTEN REFERENCE IS NOT ASKED HERE — it is asked of
    /// <c>ReferenceResolver.IsExceptionObjectRegister</c>, which adds the FORM and the EDITION to the spelling and
    /// is what every binder calls. This overload exists for the one reader that has neither a parse context nor a
    /// resolver: the §8.9 reserved-word funnel in <c>VersionConformancePass</c>, which sees an IDENTIFIER token.
    /// Comparing the spelling in one place is the point — a hand-written <c>== "EXCEPTION-OBJECT"</c> is exactly
    /// how the compiler came to hold three different opinions about what this name is (kb/Work PB922,
    /// feedback_one_rule_one_place).</para></summary>
    public static bool OoIsExceptionObject(string? word) =>
        CobolNames.Same(word, "EXCEPTION-OBJECT");

    /// <summary>True when an arithmetic expression is EXACTLY one bare data reference (the Format-5
    /// re-route's sender shape) — its single dataReference descendant spans the whole expression text.</summary>
    public static Core.DataReferenceContext? OoExtractBareReference(Core.ArithmeticExpressionContext e)
    {
        Core.DataReferenceContext? only = null;
        var stack = new Stack<Antlr4.Runtime.Tree.IParseTree>();
        stack.Push(e);
        while (stack.Count > 0)
        {
            var cur = stack.Pop();
            if (cur is Core.DataReferenceContext d)
            {
                if (only is not null) return null;
                only = d;
                continue;
            }
            for (int i = 0; i < cur.ChildCount; i++) stack.Push(cur.GetChild(i));
        }
        return only is not null && only.GetText() == e.GetText() ? only : null;
    }

    /// <summary>The VALUE of literal-1, the method name — for the INVOKE statement (§14.9.23.3 SR2: "Literal-1 shall
    /// be of class alphanumeric or national and shall not be a zero-length literal") and for every segment of the
    /// inline form, whose literal-1 §8.4.3.4.3 SR3 holds to the same rule. Null when the literal is of neither class
    /// (a boolean literal or concatenation, a figurative constant), so the caller reports it under its own code.
    /// <para>ONE decoder, the literal codec's (kb/Work PB1136): an alphanumeric, national, hexadecimal-alphanumeric or
    /// hexadecimal-national token decodes through <see cref="CobolLiteral.Decode"/>, so <c>NX"0047…"</c> has one
    /// national character per four-digit group (§8.3.3.5.4 GR4) exactly as it does in a MOVE or a DISPLAY — a private
    /// copy here stripped the N and ran the ALPHANUMERIC hex codec, making every digit pair a character. An
    /// alphanumeric or national concatenation expression stands wherever a literal of that class may (§8.8.3.3 GR3),
    /// in BOTH spellings — the inline form used to refuse one.</para></summary>
    private string? OoMethodNameOf(Core.LiteralContext? lit)
    {
        if (lit?.nonNumericLiteral() is not { } nn) return null;
        if (nn.concatenationExpression() is { } concat)
            return ConcatFolder.ClassOf(concat, ctx.Data.LiteralEnv) is not PicCategory.Boolean
                ? ConcatFolder.Fold(concat, ctx.Edition, ctx.Data.LiteralEnv).Value : null;
        return (nn.STRINGLIT() ?? nn.NATLIT() ?? nn.HEXLIT()) is { } token ? CobolLiteral.Decode(token.GetText()) : null;
    }

    /// <summary>The significant-digit count of a numeric literal rescaled to <paramref name="scale"/> (the
    /// same string math as the emitter's <c>EmitText.UnscaledAtScale</c>, counting only — the bind-time
    /// fits-the-formal check for literal arguments, §14.8.2).</summary>
    private static int OoUnscaledDigitCount(string raw, int scale)
    {
        string t = raw.Trim().TrimStart('+').TrimStart('-');
        int dot = t.IndexOf('.');
        string intPart = dot < 0 ? t : t[..dot];
        string fracPart = dot < 0 ? "" : t[(dot + 1)..];
        string digits = scale >= 0
            ? intPart + (fracPart.Length < scale ? fracPart.PadRight(scale, '0') : fracPart[..scale])
            : (intPart + fracPart) is var all && all.Length > -scale ? all[..^(-scale)] : "0";
        return digits.TrimStart('0').Length;
    }

    /// <summary>The §14.8.2/§14.8.3 STRICT conformance check between a formal/returning item and an
    /// argument/receiver item — delegates to the ONE shared description-equality rule
    /// (<see cref="OoConformance.DescriptionMismatch"/>, also the §9.3.8.2 override-signature check) that
    /// makes the emitted marshaling TYPE-PRESERVING. Null when conformant, else the mismatch.</summary>
    private static string? OoConformanceError(DataItem formal, DataItem arg)
        // Activation mode: §14.8.2.3.2 rules d/e for arguments; for the INVOKE RETURNING delivery pair the
        // sender (parameter 1 = the method's returning item) being ANY LENGTH matches any receiver length
        // (§14.8.3.3 rule 5) while an ANY LENGTH receiver demands an ANY LENGTH sender (rule 4).
        => OoConformance.DescriptionMismatch(formal, arg, anyLengthActivationRelax: true);

    // ── Method-context control flow (deep-dive D8) ──────────────────────────────────────────────────────────

    /// <summary>GOBACK inside a METHOD (§14.9.18.4 GR4): terminate the METHOD, control back to the INVOKE site.
    /// The RETURNING-item delivery is the method entry's job (slice 2 — no formals yet).
    /// <para>⛔ IT TAKES THE DECODED PHRASES, NEVER THE PARSE NODE (kb/Work PB411). While it took the
    /// <c>GobackStatementContext</c> it re-decided which phrases existed and read only two of the three, so the
    /// 2023 status phrase was dropped in silence here — §14.9.18.3 SR6/SR7/SR8 and the COBOL-2023 introduction
    /// gate never ran on a method's GOBACK. <c>CallBinder.DecodeGobackPhrases</c> now reads the rule ONCE, before
    /// the §14.9.18.4 GR2/GR4 fork, so this arm cannot be reached without every phrase having been decoded and
    /// screened.</para>
    /// <para>THE STATUS PHRASE IS SCREENED AND THEN INERT, and that is the standard's own division: §14.9.18.3's
    /// syntax rules carry no context qualifier, while EVERY general rule that gives the phrase an effect —
    /// GR7, GR8, GR9 and GR10 — opens "If the GOBACK … is executing in a main program". A method is never a main
    /// program, so there is no operating-system indication for this return to carry and nothing to put on
    /// <see cref="BoundMethodReturn"/>; the phrase's whole force in a method is its syntax rules, which
    /// <c>CallBinder.GobackPhrases.Status</c> has already applied.</para></summary>
    public BoundStatement OoBindMethodGoback(in CallBinder.GobackPhrases p)
    {
        if (p.Returning is not null)
            return new BoundUnsupported("GOBACK with a RETURNING/GIVING phrase inside a method "
                + "(ISO §14.9.18.4 GR4 returns the METHOD's RETURNING item — an activation-result form)");
        return new BoundMethodReturn(OoBindMethodRaising(p.Raising, EcRaiseSite.Goback));
    }

    /// <summary>Bind a method-context RAISING phrase (§14.9.18.4 GR1b — staged before the MethodReturn
    /// throw; the INVOKE site picks up). The <see cref="EcRaiseSite"/> carries which of the two statements this
    /// is (GOBACK §14.9.18.3 / EXIT METHOD §14.9.14.3).
    /// <para>⛔ THIS ARM NO LONGER DECIDES THE LAST PHRASE'S PLACEMENT (kb/Work PB410). It used to refuse
    /// <c>RAISING LAST</c> UNCONDITIONALLY inside a method, in a message that quoted §14.9.18.3 SR5's two
    /// admitted positions and then rejected source sitting in one of them — a WHEN phrase of an exception-
    /// checking PERFORM, which a method body may contain today. SR5 has no method qualifier, so the method arm
    /// asks the SAME <c>PlacementRules.RefusedRaisingLastHere</c> screen the program arm asks, inside the shared
    /// <c>EcBinder.EcBindRaising</c>; the declarative half of the position is simply never true in a method
    /// until method declaratives land, which the one predicate already says without a second rule.</para></summary>
    private BoundRaising? OoBindMethodRaising(Core.RaisingPhraseContext? raising, EcRaiseSite site) =>
        raising is null ? null : host.Ec.EcBindRaising(raising, raising.Start.Line, site);

    /// <summary>EXIT METHOD (pre-2023 editions — REMOVED by 2023, Annex E.2; the <c>exit-method-window</c>
    /// registry row already flags 0900/0902 at the window edges): inside a method it is the method-return
    /// synonym (≡ the §14.9.18.4 GR4 GOBACK); outside one it violates its placement rule.</summary>
    public BoundStatement OoBindExitMethod(Core.ExitStatementContext e)
    {
        // The pre-2023 METHOD format's placement rule, through the ONE bind-position probe the other EXIT
        // formats ask (kb/Work PB403) — §14.2.2 SR10's source-element kind.
        if (ctx.Enclosing.SourceElement is not SourceElementKind.MethodDefinition)
        {
            return BoundRejected.Report(ctx.Edition, "COBOLNET0827",
                "EXIT METHOD may be specified only in a method definition (ISO §14.9.14 — the method form "
                + "of the EXIT statement; this is not a method procedure division)");
        }
        return new BoundMethodReturn(OoBindMethodRaising(e.raisingPhrase(), EcRaiseSite.Exit("EXIT METHOD")));
    }
}
