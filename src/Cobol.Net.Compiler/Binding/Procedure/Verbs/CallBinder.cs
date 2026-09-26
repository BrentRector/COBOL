// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The CALL / inter-program slice of the binder (ISO §14.9.4 / §14.9.5 / §14.9.18; deep-dive design
/// COBOLNET_INTERPROGRAM_DESIGN). All semantic resolution happens here ONCE — target form, the GR5 transitive
/// pass-mode threading, the receiving-operand checks, and the per-edition gates (the G1 four-compilers rule:
/// the complete behavior where the edition HAS the construct, a targeted COBOLNET-08xx diagnostic where it
/// lacks it — see the deep-dive "Edition gating" section). P7 Step 10j: a real collaborator over
/// <see cref="BinderContext"/>; the two hooks that outlive the batch stay HOST edges — <c>InMethod</c>/
/// <c>OoBindMethodGoback</c> (the OO half converts LAST, 10s) and <c>EcBindRaising</c> (EcBinder lands 10r).
/// The 0884/0880 gates moved VERBATIM incl. the 0880 bare-GOBACK null-condition that encodes the
/// 0900-subsumption contract with the VersionConformancePass. The five bound types stayed in
/// <c>Binding/Bound/BoundCall.cs</c>.</summary>
internal sealed class CallBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind <c>CALL</c> (ISO §14.9.4 Format 1; design D2 — the uniform opaque ABI call).</summary>
    public BoundStatement BindCall(Core.CallStatementContext call)
    {
        // ── §14.9.4.2 FORMAT 2 (fix-queue PB46 · kb/Work PB131 · PB237) ───────────────────────────────────────
        // The RENDERED general format (PDF page 619) is
        //     CALL ⎡{ identifier-1 | literal-1 } AS⎤ { NESTED | program-prototype-name-1 }
        // — the optional bracket encloses the target brace AND the word AS, and the outer brace is NOT optional.
        // So Format 2 has TWO spellings, and the compiler must recognize both:
        //   (a) `… AS NESTED` / `… AS program-prototype-name-1` — a SYNTACTIC discriminator (Format 1 has no AS
        //       phrase at all), which is what refuted PB46's "the formats are indistinguishable at parse time";
        //   (b) the bracket omitted whole: `CALL program-prototype-name-1`, spelled EXACTLY like Format 1's
        //       `CALL identifier-1`. That one IS semantic, and §14.9.4.4 GR3 b) is written for it ("If neither
        //       identifier-1 nor literal-1 is specified, program-prototype-name-1 determines the externalized
        //       program-name of the program being called").
        // Spelling (b) is resolved AFTER the data-reference attempt, below, so no program that binds today
        // changes meaning: a word that names a data item stays identifier-1.
        bool asNested = false;
        ProgramPrototype? prototype = null;
        if (call.callAsPhrase() is { } asPhrase)
        {
            // The grammar decided the brace (kb/Work PB764): the NESTED keyword arm is a formatWord, recognized
            // through the >>COBOL-WORDS map (ISO §7.3.10.4; kb/Work PB250) — an EQUATEd synonym takes it and an
            // UNDEFINE'd NESTED falls to the program-prototype-name arm, a cobolWord.
            asNested = asPhrase.formatWord() is not null;
            if (!asNested)
            {
                // §14.9.4.3 SR16: "Program-prototype-name-1 shall be specified in a program-specifier in the
                // REPOSITORY paragraph." ONE lookup answers it, because ProgramPrototypesOf also registers
                // §8.4.6.8's no-specifier spelling (a containing program definition's program-name).
                if (ResolvePrototype(asPhrase.cobolWord().GetText(), "CALL … AS") is not { } proto) return BoundRejected.Reported(ctx.Edition);
                prototype = proto;
            }
            else
            {
                // §14.9.4.3 SR13: "The NESTED phrase may be specified only in a program definition" — ONE
                // question of the §14.2.2 SR10 enumeration (kb/Work PB894). It used to be the two-bit predicate
                // `InMethod || UdfSelfName is not null`, which excluded a method and a function but ADMITTED a
                // program PROTOTYPE definition — the fifth element, which the rule's "program definition" does
                // not name (unobservable until §11.10.2 Format 2 parsed).
                if (ctx.Enclosing.SourceElement is var element and not SourceElementKind.Program)
                {
                    string noun = element switch
                    {
                        SourceElementKind.MethodDefinition => "method definition",
                        SourceElementKind.ProgramPrototype => "program prototype definition",
                        SourceElementKind.FunctionPrototype => "function prototype definition",
                        _ => "function definition",
                    };
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.CallAsNestedContext,
                        $"CALL … AS NESTED inside a {noun}: the NESTED phrase may be specified only in a program "
                        + "definition (ISO §14.9.4.3 SR13)");
                }
                // §14.9.4.3 SR15: literal-1 shall be specified, and shall name a COMMON program or a program
                // directly contained in the calling program.
                if (call.callTarget()?.literal() is null)
                {
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.CallAsNestedScope,
                        "CALL … AS NESTED: literal-1 shall be specified — the NESTED phrase names a contained or "
                        + "COMMON program by its PROGRAM-ID literal, not through an identifier (ISO §14.9.4.3 SR15)");
                }
            }
        }
        // ⛔ ONE Format-2 callee signature, TWO producers (kb/Work PB237). Everything downstream — GR9's
        // formal-decides mode derivation, SR19/SR21's explicit-phrase agreement, SR24's OPTIONAL correspondence,
        // §14.8.2's argument conformance and §14.9.4.3 SR25→§14.8.3's returning conformance — reads `calleeFormals` /
        // `callee`, never "was this AS NESTED". Wiring the prototype producer therefore delivered the whole
        // §14.8.2/§14.8.3 regime to prototype calls in one assignment rather than by copying seven checks.
        CalleeSignature? callee = null;
        // kb/Work PB131 — the AS NESTED callee's bound formals (§14.9.4.3 SR15 sentence 2 enforced here:
        // the name shall be a directly-contained or visible-COMMON program; the old binder bound the flag,
        // discarded it, and let ProgramTable resolve ANY outermost program at run time).
        if (asNested && call.callTarget()?.literal() is { } asLit)
        {
            string nestedName = CobolLiteral.Decode(asLit.GetText());
            if (host.NestedCallables is { } nc && nc.TryGetValue(nestedName, out var sig))
                callee = sig;
            else
            {
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.CallAsNestedScope,
                    $"CALL … AS NESTED \"{nestedName}\": the name shall be a program contained directly within "
                    + "the calling program, or a visible common program (ISO §14.9.4.3 SR15)");
            }
        }

        // ── Target: literal (static) or identifier (dynamic, resolved at run time — GR3b) ──
        string? literalName = null;
        BoundOperand? dynamicName = null;
        bool isPointerTarget = false;   // identifier-1 is a PROGRAM-POINTER item (§14.9.4.3 SR1; P10 Step 7)
        var target = call.callTarget();
        if (target?.literal() is { } lit)
        {
            // kb/Work PB130: through the ONE program-name-literal reader — SR2 admits alphanumeric AND
            // national literals (a hexadecimal literal IS §8.3.3.2 Format 2 of an alphanumeric one), and the
            // old STRINGLIT-only read sent N"P" / X".." to a run-time loud on legal source.
            if (ProgramNameLiteral(lit, "CALL", "§14.9.4.3 SR2") is not { } pn) return BoundRejected.Reported(ctx.Edition);
            literalName = pn;
        }
        else if (target?.dataReference() is { } dref)
        {
            // §14.9.4.2 Format 2 spelling (b), kb/Work PB237: with the whole `⎡{identifier-1|literal-1} AS⎤`
            // bracket omitted, the operand IS program-prototype-name-1 and is spelled exactly like identifier-1.
            // The DATA REFERENCE is tried first, and through Probe rather than Resolve because an unresolved
            // Probe reports nothing — so a word that names a data item stays identifier-1 and no program that
            // binds today changes meaning. When the AS phrase already named a prototype, identifier-1 IS
            // specified and GR3 b)'s FIRST bullet governs: the item's content is the program-name and the
            // prototype only supplies the characteristics (GR7) — hence the `prototype is null` guard.
            if (prototype is null && ctx.Refs.Probe(dref) is null && BarePrototypeWord(dref) is { } bareWord
                && host.ProgramPrototypes?.GetValueOrDefault(bareWord) is { } bareProto)
                // GR3 b) third bullet: "If neither identifier-1 nor literal-1 is specified,
                // program-prototype-name-1 determines the externalized program-name of the program being
                // called, according to the rules specified in 12.3.8, REPOSITORY paragraph."
                prototype = bareProto;
            else if (host.Expr.ResolveSending(dref) is var calleeAnswer && calleeAnswer.Place is not { } place)
                return calleeAnswer.Refusal(ctx.Edition);   // the resolver's answer picks the node (kb/Work PB1030)
            else
            {
                dynamicName = new BoundFieldOperand(place);
                // §14.9.4.3 SR1: "Identifier-1 shall be defined as an alphanumeric, national, or program-pointer
                // data item" — a program-pointer target activates the HELD program (ProgramRegistry.CallPointer,
                // P10 Step 7); any OTHER class is rejected here (kb/Work PB132 — the old arm read the category
                // only to set the pointer flag, and a numeric or boolean target fell through to a garbage
                // name-string read at run time).
                if (place.Item.Pic?.Category is PicCategory.ProgramPointer) isPointerTarget = true;
                else if (IntrinsicArgumentRules.ClassOf(new BoundFieldOperand(place))
                         is { } tCls and not (CobolClass.Alphanumeric or CobolClass.National))
                {
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.CallTargetCategory,
                        $"CALL target '{DataBinder.WrittenText(dref)}' is of class {tCls.ToString().ToLowerInvariant()}; ISO "
                        + "§14.9.4.3 SR1 admits an alphanumeric, national, or program-pointer data item");
                }
            }
        }
        else if (prototype is null)
            // The bracket may be omitted only in Format 2, where the outer brace then supplies the operand. With
            // neither a target nor a prototype there is nothing to activate.
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "CALL with neither a program identifier or literal nor a "
                + "program-prototype-name: every CALL format names the program to activate (ISO §14.9.4.2)");

        // §14.9.4.4 GR7: "If the NESTED phrase is not specified, program-prototype-name-1 is used to determine
        // the characteristics of the called program." Its externalized name is the program-name whenever
        // identifier-1/literal-1 was not written (GR3 b)), and its signature — when §12.3.8.4 GR10 a) found the
        // definition in this compilation group — is the ONE callee signature every §14.8.2/§14.8.3 check below
        // reads. A GR10 c) prototype (a separately-compiled program) legally carries none; the locate and the
        // EC-PROGRAM-ARG-MISMATCH conformance check then happen at activation, per GR3 b) and GR3 d).
        if (prototype is { } proto2)
        {
            if (literalName is null && dynamicName is null) literalName = proto2.ExternalizedName;
            callee ??= proto2.Signature;
        }
        // Format 2 is selected by the AS phrase OR by a bare program-prototype-name operand (the two spellings of
        // §14.9.4.2 Format 2). Every narrowing below that reads `formatTwo` — Format 1's ban on BY VALUE, on a
        // BY CONTENT literal and on bare non-identifier arguments — therefore relaxes for both.
        bool formatTwo = call.callAsPhrase() is not null || prototype is not null;
        IReadOnlyList<LinkageFormal>? calleeFormals = callee?.Formals;
        // How the shared §14.8.2/§14.8.3 messages below name the callee they resolved. One string, because the
        // CHECKS are one set: only the provenance of the signature differs.
        string calleeWhere = asNested
            ? "CALL … AS NESTED"
            : $"CALL … program-prototype '{prototype?.Name}'";
        // §14.9.4.3 SR25 is the rule that imports §14.8.2 into a Format-2 CALL (the function-identifier's twin is
        // §8.4.3.2.3 SR13 — UdfBinder).
        var conformanceSite = new ActivationSite(calleeWhere, "§14.9.4.3 SR25", DiagnosticCatalog.CallArgumentConformance);

        // ── USING arguments — the §14.9.4.4 GR5 TRANSITIVE pass mode: BY REFERENCE is assumed before the
        //    first phrase; each explicit BY REFERENCE / BY CONTENT / BY VALUE phrase applies to every following
        //    bare argument until the next phrase (the legacy CallBinder mode-threading, re-derived from GR5). ──
        var args = new List<BoundCallArg>();
        var mode = CobolPassMode.Reference;
        foreach (var a in call.callUsingPhrase()?.callArgument() ?? [])
        {
            if (a.callByReference() is { } byRef)
            {
                mode = CobolPassMode.Reference;
                // §14.9.4.2 Format 2: `[BY REFERENCE] {identifier-2 | OMITTED}` (kb/Work PB130). The omitted-
                // argument carrier joins the OPTIONAL-formal landing (PB133); until then the recognized form
                // draws the same staged diagnostic family instead of a parse error on legal source.
                if (byRef.OMITTED() is not null)
                {
                    // §14.9.4.4 GR11 (kb/Work PB133 wave C): the omitted argument occupies its position with
                    // the NULL carrier — the callee's Present test, the §8.8.4.8 condition, and §14.9.4.4 GR12's checked
                    // raise all read that one fact. SR24's OPTIONAL correspondence is checked after the loop.
                    args.Add(new BoundCallArg(CobolPassMode.Reference, null, null, Omitted: true));
                    continue;
                }
                if (byRef.addressIdentifier() is { } refAddr)
                {
                    // §14.9.4.3 SR3/SR4 — an address-identifier BY REFERENCE is still a SENDING operand, so it
                    // never meets the receiving chokepoint below (kb/Work PB239).
                    if (AddressArg(refAddr, CobolPassMode.Reference) is not { } ra) return BoundRejected.Reported(ctx.Edition);
                    args.Add(ra);
                    continue;
                }
                if (byRef.dataReference() is not { } byRefDref)
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "CALL … USING BY REFERENCE with neither an identifier nor OMITTED "
                        + "(ISO §14.9.4.2)");
                // kb/Work PB128: a BY REFERENCE argument is a RECEIVING operand and rides the ONE receiving
                // chokepoint — the direct Refs.Resolve bypass skipped the CONSTANT RECORD (§13.18.15.3 SR2),
                // CAPACITY-register (§13.18.38 SR30–32), constant-name and LINE-COUNTER screens, letting a
                // structured constant be silently overwritten by the callee (and a CAPACITY register reach
                // PlaceRenderer.Write's internal throw — an unhandled compiler exception).
                if (host.Expr.ResolveReceiving(byRefDref) is not { } p)
                    return OperandUnresolved(byRefDref, "USING argument");
                ScreenCallOperand(p, CobolPassMode.Reference, formatTwo, isReturning: false);
                args.Add(new BoundCallArg(CobolPassMode.Reference, p, null));
            }
            else if (a.callByContent() is { } byContent)
            {
                mode = CobolPassMode.Content;
                // Format 1's `{ identifier-2 } …` includes the address-identifier SR3 names (kb/Work PB239).
                if (byContent.addressIdentifier() is { } contentAddr)
                {
                    if (AddressArg(contentAddr, CobolPassMode.Content) is not { } ca) return BoundRejected.Reported(ctx.Edition);
                    args.Add(ca);
                    continue;
                }
                // ── ONE OPERAND, THREE CHANNELS — normalized once, exactly as OoBindInvokeArg does ──
                // The grammar parses BY CONTENT wide (both formats share this rule). The reduction is
                // §14.9.4.4 GR8's and lives in ONE place (kb/Work PB238 — the BY VALUE arm below and both
                // keyword-less arms read the SAME helper; the reduction used to be written out here only, so
                // GR8 held for one spelling of an argument and not for the others).
                var (cLit, cBareLit, cDref, cBool, cArith) = Gr8Classify(
                    byContent.literal(), byContent.dataReference(),
                    byContent.booleanExpression(), byContent.arithmeticExpression());
                // §14.9.4.2 FORMAT 1's BY CONTENT IS `{ identifier-2 } …` AND NOTHING ELSE. An expression operand
                // is legal only under Format 2, which the AS phrase selects — so accepting one here without that
                // phrase would admit illegal source, the exact trade this item refused to make in the grammar.
                if (!formatTwo && (cBool is not null || cLit is not null || (cArith is not null && cDref is null))
                    && !Format1VendorSpelling(
                        $"CALL … USING BY CONTENT {byContent.GetText()}: an expression operand belongs to the "
                        + "program-prototype CALL (ISO §14.9.4.2 Format 2), which the AS phrase selects. "
                        + "Format 1's BY CONTENT admits `{ identifier-2 } …` only"))
                    return BoundRejected.Reported(ctx.Edition);
                // Probe to DISCRIMINATE (the cArith arm below is the legal alternative and its bind demands —
                // R30), then RESOLVE to commit: a probe is unscreened, so its Place must never enter the bound
                // tree (kb/Work PB221 — this arm used to commit the probe's Place, so `BY CONTENT E(XE)` with
                // `XE PIC X(4)` compiled clean while the BY REFERENCE operand of the same statement drew
                // COBOLNET0844, and a function-bearing subscript bound occurrence 1).
                if (cDref is not null && ctx.Refs.Probe(cDref) is not null)
                {
                    // Discriminated as identifier-2: the committed answer decides, never a fall-through to the
                    // expression arms (kb/Work PB1030).
                    if (host.Expr.ResolveSending(cDref) is var cr && cr.Place is not { } cp) return cr.Refusal(ctx.Edition);
                    ScreenCallOperand(cp, CobolPassMode.Content, formatTwo, isReturning: false);
                    args.Add(new BoundCallArg(CobolPassMode.Content, cp, null));
                }
                else if (cLit is { } clit)
                    args.Add(new BoundCallArg(CobolPassMode.Content, null, host.Expr.LiteralOperand(clit)));
                else if (cBareLit is { } cbl && host.Expr.NonNumericLiteralOperand(cbl) is { } cblOp)
                    args.Add(new BoundCallArg(CobolPassMode.Content, null, cblOp));
                else if (cArith is { } cax)
                    // Format-2 arithmetic-expression-1: bind through the ONE expression path and pass its value.
                    args.Add(new BoundCallArg(CobolPassMode.Content, null,
                        IntrinsicBinder.OperandOf(host.Expr.BindExpr(cax))));
                else if (cBool is { } cbx)
                    args.Add(BooleanContentArg(cbx));
                else
                    return new BoundUnsupported($"CALL USING BY CONTENT argument '{byContent.GetText()}'");
            }
            else if (a.callByValue() is { } byValue)
            {
                // §14.9.4.2 FORMAT 1 HAS NO BY VALUE ARM (kb/Work PB130): its USING brace prints BY REFERENCE
                // and BY CONTENT only (the repaired figure notes’ required-word list has no VALUE), and
                // SR21–SR23 sit under Format 2. Accepting it here passed a GR5-impossible mode.
                if (!formatTwo && !Format1VendorSpelling(
                        "CALL … USING BY VALUE belongs to the program-prototype CALL (ISO §14.9.4.2 Format 2), "
                        + "which the AS phrase selects — Format 1’s USING admits BY REFERENCE and BY CONTENT only"))
                    return BoundRejected.Reported(ctx.Edition);
                // BY VALUE (§14.9.4) is a COBOL-2002 introduction; the edition gate moved to the post-bind
                // VersionConformancePass (Step 14c), firing on a BoundCallProgram whose args use value passing.
                mode = CobolPassMode.Value;
                // identifier-4 as an address-identifier: §14.9.4.3 SR22 admits class pointer BY VALUE, and
                // §8.4.3.11.4 GR1 / §8.4.3.13.4 GR1 make each arm "a unique data item of class pointer"
                // (kb/Work PB239).
                if (byValue.addressIdentifier() is { } valueAddr)
                {
                    if (AddressArg(valueAddr, CobolPassMode.Value) is not { } va) return BoundRejected.Reported(ctx.Edition);
                    args.Add(va);
                    continue;
                }
                // ── §14.9.4.4 GR8 APPLIES HERE TOO, AND THAT IS THE WHOLE OF kb/Work PB238 ────────────────
                // "An argument that consists merely of a single identifier or literal is regarded as an
                // identifier or literal rather than an arithmetic or boolean expression." The printed
                // §14.9.4.2 Format-2 BY VALUE brace is `arithmetic-expression-1 | identifier-4 | literal-2`,
                // and this arm used to bind ALL THREE as arithmetic-expression-1 — so identifier-4 lost its
                // own carrier and its own PICTURE meta on the way to CallEmitter.ArgText, which then spelled
                // the crossing `ManagedPointer<Int128>.Cell((Int128)(expr)), 38, scale`: the item's digits
                // replaced by a constant, and a FLOAT item's fractional part TRUNCATED AWAY (measured:
                // `01 F FLOAT-LONG VALUE 1.5` reached a `PIC S9(3)V99` BY VALUE formal as 001.00, where
                // §14.2.3 GR10 requires "a COMPUTE statement without the ROUNDED phrase" ⇒ 001.50). The
                // reduction is the SAME one the BY CONTENT arm runs, from the SAME helper.
                var (vLit, vBareLit, vDref, _, vArith) = Gr8Classify(
                    byValue.literal(), null, null, byValue.arithmeticExpression());
                if (vLit is { } vlit)
                {
                    // §14.9.4.3 SR23 — reported BY NAME. The `literal` alternative exists in `callByValue`
                    // solely so this verdict is the compiler's, with its citation, instead of a raw ANTLR
                    // "no viable alternative" (the numeric spelling never reaches here: `arithmeticExpression`
                    // is the earlier alternative and subsumes every numeric literal and the figurative ZERO).
                    Sr23LiteralIsNumeric(vlit, "BY VALUE");
                    args.Add(new BoundCallArg(CobolPassMode.Value, null, host.Expr.LiteralOperand(vlit)));
                }
                else if (vDref is { } vdref && ctx.Refs.Probe(vdref) is not null)
                {
                    if (host.Expr.ResolveSending(vdref) is var vr && vr.Place is not { } vp) return vr.Refusal(ctx.Edition);   // kb/Work PB1030
                    // identifier-4 — a SENDING operand (SR17), crossing on ITS OWN carrier and its own
                    // Digits/Scale through the Place arm of CallEmitter.ArgText, exactly as the BY CONTENT
                    // identifier does. Probe-to-discriminate then Resolve-to-commit, the PB221 discipline.
                    // Resolve, never ResolveReceiving: SR17 makes it sending, so the receiving screens do
                    // not apply and their diagnostics would be false rejections.
                    ScreenCallOperand(vp, CobolPassMode.Value, formatTwo, isReturning: false);
                    // §14.9.4.3 SR22 over the RESOLVED item — the same screen the keyword-less BY VALUE
                    // argument runs. It no longer has to unwrap a BoundComputedOperand to recover the
                    // identifier-ness GR8 says was never lost.
                    ValueClassScreen(IntrinsicArgumentRules.ClassOf(new BoundFieldOperand(vp)), vdref.GetText());
                    args.Add(new BoundCallArg(CobolPassMode.Value, vp, null));
                }
                else if (vBareLit is not null)
                {
                    // A non-numeric literal-2 that reached the boolean node on a LATER argument's B-operator
                    // is still literal-2, and §14.9.4.3 SR23 still governs it. (`callByValue` has no boolean
                    // alternative, so this arm is reachable only through a shared reduction — which is the
                    // argument for having one.)
                    return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.CallByValueLiteralKind,
                        $"CALL … USING {vBareLit.GetText()} with BY VALUE: literal-2 shall be a NUMERIC "
                        + "literal (ISO §14.9.4.3 SR23)");
                }
                else if (vArith is { } vax)
                {
                    // arithmetic-expression-1 — the genuine residue.
                    // ⛔ BindByValueExpr, NOT BindExpr — §14.9.4.3 SR22 governs a BY VALUE operand, not
                    // §8.8.1.1. The production is named arithmeticExpression and binding it as arithmetic put
                    // DA6's §8.8.1.1 screen on it, so an alphanumeric operand was refused with a message about
                    // arithmetic expressions and a "digit-decoding extension" — the right verdict quoting the
                    // wrong rule.
                    var byValueOperand = new BoundComputedOperand(host.Expr.BindByValueExpr(vax));
                    CheckByValueClass(vax.GetText(), byValueOperand);
                    args.Add(new BoundCallArg(CobolPassMode.Value, null, byValueOperand));
                }
                else
                    return new BoundUnsupported($"CALL USING BY VALUE argument '{byValue.GetText()}'");
            }
            else if (a.addressIdentifier() is { } bareAddr)
            {
                // A keyword-less address-identifier (kb/Work PB239). Format 1: the prevailing GR5 mode.
                // Format 2: GR9 a)1 assumes BY REFERENCE for an argument that meets SR3 — which names the
                // address-identifier explicitly — and GR9 b) BY VALUE when the corresponding formal is BY VALUE.
                CobolPassMode addrMode = mode;
                if (formatTwo && calleeFormals is not null)
                    addrMode = args.Count < calleeFormals.Count && calleeFormals[args.Count].ByValue
                        ? CobolPassMode.Value : CobolPassMode.Reference;
                if (AddressArg(bareAddr, addrMode) is not { } ba) return BoundRejected.Reported(ctx.Edition);
                args.Add(ba);
            }
            else if (a.dataReference() is { } bare)
            {
                // Format 1: a bare argument takes the prevailing transitive mode — §14.9.4.4 GR5 names
                // BY REFERENCE and BY CONTENT only. Format 2 (kb/Work PB131): GR5 is a FORMAT 1 rule; GR9
                // takes the keyword-less identifier's mode from the CORRESPONDING FORMAL, resolved at bind
                // through the AS NESTED callee table (the old single transitive `mode` silently passed
                // `USING BY VALUE A B`'s B detached, losing the callee's writeback).
                // ⛔ §14.9.4.4 GR8 FIRST: a constant-name substitutes a LITERAL — §13.10.4 GR1 ("the effect of
                // specifying constant-name-1 in other than this entry is as if literal-1 … were written where
                // constant-name-1 is written") with §13.10.3 SR2 admitting it "anywhere that a format
                // specifies a literal", which the Format-2 figure's literal-2 slot does —
                // so `CALL … AS NESTED USING K` is a bare LITERAL-2 argument, never identifier-2 — and
                // literal-2 never meets Syntax rule 3, so GR9 a)2 assumes BY CONTENT (or GR9 b) BY VALUE).
                // The old arm resolve-RECEIVED it and drew COBOLNET1548 "constant-name shall not be specified
                // as a receiving operand", rejecting conforming Format-2 source (kb/Work PB238).
                if (formatTwo && host.Expr.ConstantOperand(bare) is { } bareConst)
                {
                    var kMode = Gr9BareLiteralMode(calleeFormals, args.Count);
                    if (kMode is CobolPassMode.Value) Sr23ConstantIsNumeric(bare, kMode);
                    args.Add(new BoundCallArg(kMode, null, bareConst));
                    continue;
                }
                CobolPassMode bareMode = mode;
                bool byContentAssumed = false;
                if (formatTwo && calleeFormals is not null)
                {
                    int pos = args.Count;
                    bareMode = pos < calleeFormals.Count && calleeFormals[pos].ByValue
                        ? CobolPassMode.Value : CobolPassMode.Reference;
                }
                // ── §14.9.4.4 GR9 a)2 + §14.9.4.3 SR20 — the leg with no arm at all (kb/Work PB238) ────────
                // GR9 a)'s test is SYNTAX RULE 3 ("an address-identifier or a data item defined in the file,
                // working-storage, local-storage, or linkage section"), NOT "the receiving chokepoint accepted
                // it": a)1 assumes BY REFERENCE only when the argument MEETS SR3, and a)2 assumes BY CONTENT
                // when it does not. An OBJECT PROPERTY is the standard's own worked example of the second leg —
                // it is not a data item of any of those four sections, and SR20 names it explicitly as the ONE
                // identifier permitted as a receiving operand for which "BY CONTENT may be omitted". Binding it
                // BY REFERENCE made §8.4.3.9.4's desugar classify the occurrence ReadWrite (BoundStores) and
                // invoke the SET accessor on an operand SR17 calls SENDING.
                if (formatTwo && bareMode is CobolPassMode.Reference && ctx.Refs.IsObjectPropertyReference(bare))
                {
                    bareMode = CobolPassMode.Content;
                    byContentAssumed = true;
                }
                // A sending operand resolves through the SENDING resolver: the receiving chokepoint's screens
                // (§13.18.15.3 SR2, the CAPACITY register, LINE-COUNTER) are receiving-operand rules and would
                // be false rejections here.
                Place? bp;
                if (byContentAssumed || bareMode is CobolPassMode.Value)
                {
                    if (host.Expr.ResolveSending(bare) is var br && (bp = br.Place) is null)
                        return OperandUnresolved(bare, "USING argument", br);
                }
                else if ((bp = host.Expr.ResolveReceiving(bare)) is null)
                    return OperandUnresolved(bare, "USING argument");
                ScreenCallOperand(bp, bareMode, formatTwo, isReturning: false);
                // §14.9.4.3 SR22's OTHER arm (kb/Work PB132): "identifier-4 OR ITS CORRESPONDING FORMAL
                // PARAMETER is specified with a BY VALUE phrase" — the formal-derived Value mode (GR9 b))
                // must meet the same class screen the explicit BY VALUE arm runs.
                if (bareMode is CobolPassMode.Value)
                    ValueClassScreen(IntrinsicArgumentRules.ClassOf(new BoundFieldOperand(bp)), bare.GetText());
                args.Add(new BoundCallArg(bareMode, bp, null));
            }
            // §14.9.4.2 Format 2's keyword-less non-identifier arguments (kb/Work PB130): literal-2,
            // arithmetic-expression-1, boolean-expression-1 and OMITTED all print bare in Format 2 (its BY
            // phrases are plain brackets — GR9 a)2 exists precisely for the non-identifier bare argument);
            // Format 1's bare argument is identifier-2 only, so each arm narrows on formatTwo. A bare
            // non-identifier crosses BY CONTENT semantics (a value, never a writeback carrier).
            else if (a.OMITTED() is not null)
                // §14.9.4.4 GR11 — same carrier as the BY REFERENCE spelling (kb/Work PB133 wave C).
                args.Add(new BoundCallArg(CobolPassMode.Reference, null, null, Omitted: true));
            else if (a.literal() is { } bLit)
            {
                if (!formatTwo && !BareNeedsFormat2(bLit.GetText())) return BoundRejected.Reported(ctx.Edition);
                // §14.9.4.4 GR9 b) — BY VALUE is assumed when the corresponding formal is BY VALUE, and the
                // MODE is the mechanism, not a side effect of the copy-out (kb/Work PB238: this arm hard-coded
                // Content and the VALUE outcome happened only because a Content literal has no storage to copy
                // back to — right by accident, and it would not have survived a change to the copy-out).
                var bLitMode = Gr9BareLiteralMode(calleeFormals, args.Count);
                // §14.9.4.3 SR23's SECOND subject — "or its corresponding formal parameter is specified with
                // the BY VALUE phrase" — which had no reachable arm at all before the mode was derived here.
                if (bLitMode is CobolPassMode.Value) Sr23LiteralIsNumeric(bLit, "a BY VALUE formal parameter");
                args.Add(new BoundCallArg(bLitMode, null, host.Expr.LiteralOperand(bLit)));
            }
            else if (a.booleanExpression() is { } bBool)
            {
                if (!formatTwo && !BareNeedsFormat2(bBool.GetText())) return BoundRejected.Reported(ctx.Edition);
                // §14.9.4.4 GR8's reduction applies to the bare spelling too: an operator-free
                // booleanExpression is its bare valueOperand, and the identifier/literal it holds is
                // identifier-4 / literal-2 — the BY CONTENT arm's own discipline, from the same helper.
                var (nLit, nBareLit, nDref, nBool, nArith) = Gr8Classify(null, null, bBool, null);
                if (nBool is { } nbx)
                    // boolean-expression-1 prints ONLY in the BY CONTENT brace of §14.9.4.2 Format 2, so the
                    // omitted phrase can only be BY CONTENT; §14.9.4.3 SR19 is then checked against the
                    // corresponding formal by the loop below (a BY VALUE formal draws its own diagnostic).
                    args.Add(BooleanContentArg(nbx));
                else if (nLit is { } nlit)
                    args.Add(new BoundCallArg(CobolPassMode.Content, null, host.Expr.LiteralOperand(nlit)));
                else if (nBareLit is { } nbl && host.Expr.NonNumericLiteralOperand(nbl) is { } nblOp)
                    args.Add(new BoundCallArg(Gr9BareLiteralMode(calleeFormals, args.Count), null, nblOp));
                else if (nDref is { } ndref)
                {
                    if (host.Expr.ResolveSending(ndref) is var nr && nr.Place is not { } np) return nr.Refusal(ctx.Edition);   // kb/Work PB1030
                    ScreenCallOperand(np, CobolPassMode.Content, formatTwo, isReturning: false);
                    args.Add(new BoundCallArg(CobolPassMode.Content, np, null));
                }
                else if (nArith is { } nax)
                    args.Add(new BoundCallArg(CobolPassMode.Content, null,
                        IntrinsicBinder.OperandOf(host.Expr.BindExpr(nax))));
                else
                    return new BoundUnsupported($"CALL USING argument '{bBool.GetText()}'");
            }
            else if (a.arithmeticExpression() is { } bArith)
            {
                // A parenthesized sole reference reduces to its identifier (the callByContent discipline).
                if (ConditionBinder.SoleDataReference(bArith) is { } sd)
                {
                    if (host.Expr.ResolveReceiving(sd) is not { } sp)
                        return OperandUnresolved(sd, "USING argument");
                    ScreenCallOperand(sp, mode, formatTwo, isReturning: false);
                    args.Add(new BoundCallArg(mode, sp, null));
                }
                else if (!formatTwo && !BareNeedsFormat2(bArith.GetText())) return BoundRejected.Reported(ctx.Edition);
                else
                    args.Add(new BoundCallArg(CobolPassMode.Content, null,
                        IntrinsicBinder.OperandOf(host.Expr.BindExpr(bArith))));
            }
        }

        // ⛔ ONE BOOLEAN VALUE CHANNEL for the CALL side, mirroring BoundInvokeArg.ContentBool exactly
        // (kb/Work PB46 landed the INVOKE half and left this one; PB238 closes it). §14.9.4.3 SR17 makes every
        // identifier inside boolean-expression-1 a SENDING operand, and a boolean expression evaluates to a
        // '0'/'1' bit-string VALUE (§8.8.2) whose length is fixed by §8.8.2 rule 10 — it is neither an
        // algebraic value nor a storage reference, so it is its own channel rather than a spelling of the
        // arithmetic one. BY CONTENT by construction: an expression has no storage to write back to.
        BoundCallArg BooleanContentArg(Core.BooleanExpressionContext bx)
        {
            var bound = host.Cond.BindBoolExpr(bx);
            return new BoundCallArg(CobolPassMode.Content, null, null) { ContentBool = bound };
        }

        // §14.9.4.4 GR9 for a keyword-less LITERAL-2 (or a constant-name, which substitutes one): a literal is
        // never "a data item defined in the file, working-storage, local-storage, or linkage section", so it
        // never meets Syntax rule 3 — GR9 a)2 gives BY CONTENT, and GR9 b) gives BY VALUE whenever the
        // CORRESPONDING FORMAL is BY VALUE. Without a known formal (no AS NESTED — the program-prototype
        // registry is P13) GR9 has nothing to read, and BY CONTENT is the format's own omitted-phrase default.
        static CobolPassMode Gr9BareLiteralMode(IReadOnlyList<LinkageFormal>? formals, int pos) =>
            formals is not null && pos < formals.Count && formals[pos].ByValue
                ? CobolPassMode.Value : CobolPassMode.Content;

        bool BareNeedsFormat2(string text) =>
            Format1VendorSpelling(
                $"CALL … USING {text}: a keyword-less literal or expression argument belongs to the "
                + "program-prototype CALL (ISO §14.9.4.2 Format 2), which the AS phrase selects — Format 1's "
                + "bare argument is identifier-2 only");

        // ⛔ THE ONE FRAMING OF §14.9.4.2 FORMAT 1'S NARROWING (kb/Work PB162), for all five spellings it refuses —
        // BY VALUE, a BY CONTENT literal / expression, and a bare literal / boolean / arithmetic expression. Strict:
        // the error (PB130's ISO-correct narrowing, unchanged). --permissive (the dialect axis every leniency rides,
        // project_dialect_two_axes): a WARNING under the same code, and the argument binds with the Format-2
        // semantics the spelling plainly intends — a literal or expression is a detached BY CONTENT value, BY VALUE
        // is the Value mode. GnuCOBOL accepts all five spellings, and IBM Enterprise COBOL and Micro Focus accept
        // BY VALUE and a BY CONTENT literal, on the CALL that names no prototype (CLAUDE.md rule 1's precedence
        // for implementor latitude; the strict verdict is the standard's, not latitude). True = bind on.
        bool Format1VendorSpelling(string refusal)
        {
            if (ctx.Edition.Permissive)
            {
                ctx.Edition.Warning(DiagnosticCatalog.CallContentOperandFormat, refusal
                    + "; accepted under --permissive as the vendor extension (GnuCOBOL, IBM, Micro Focus) and bound "
                    + "with the Format-2 semantics it spells");
                return true;
            }
            ctx.Edition.Error(DiagnosticCatalog.CallContentOperandFormat, refusal);
            return false;
        }

        // §14.8.2.1 + §14.9.4.3 SR24 (kb/Work PB133 wave C, generalized by PB237): with the Format-2 callee's
        // formals known at BIND time — from the AS NESTED containment table or from the program prototype's
        // §12.3.8.4 GR10 a) definition — the argument COUNT (equality, except trailing OPTIONAL formals omitted)
        // and each written OMITTED's OPTIONAL correspondence are compile-time checks — the design doc's "or
        // diagnostic" lane; the dynamic Format-1 count check (EC-PROGRAM-ARG-MISMATCH at activation) rides wave C2.
        if (calleeFormals is not null)
        {
            bool countBad = args.Count > calleeFormals.Count;
            for (int i = args.Count; !countBad && i < calleeFormals.Count; i++)
                if (!calleeFormals[i].Optional) countBad = true;
            if (countBad)
                ctx.Edition.Error(DiagnosticCatalog.CallArgumentCount,
                    $"{calleeWhere} supplies {args.Count} argument(s) where the program declares "
                    + $"{calleeFormals.Count} formal parameter(s); ISO §14.8.2.1 requires the counts to be "
                    + "equal, except for trailing formal parameters declared OPTIONAL and omitted");
            for (int i = 0; i < args.Count && i < calleeFormals.Count; i++)
            {
                var f = calleeFormals[i];
                // ISO §14.2.3 GR2's positional correspondence, RECORDED (kb/Work PB640). This loop is where the
                // activating element learns the formal's description, and GR9's second branch / GR10 make the
                // ACTIVATING element perform the argument crossing against it — "a COMPUTE statement without the
                // ROUNDED phrase" for a numeric formal — so the emitter needs the same fact this conformance
                // screen reads. Recorded for EVERY argument, not only the ones a check below rejects.
                var arg = args[i] with { Formal = f.Item };
                args[i] = arg;
                if (arg.Omitted)
                {
                    if (!f.Optional)
                        ctx.Edition.Error(DiagnosticCatalog.CallOmittedNeedsOptional,
                            $"CALL … USING OMITTED at argument {i + 1}: the corresponding formal parameter "
                            + $"'{f.Item.CobolName}' is not declared OPTIONAL (ISO §14.9.4.3 SR24)");
                    continue;
                }
                // §14.9.4.3 SR19/SR21 (kb/Work PB133 wave C2): the EXPLICIT phrase's mode must match the
                // corresponding formal's (a keyword-less argument already DERIVED its mode from that formal
                // — GR9, PB131 — so only a written phrase can disagree).
                if (arg.Mode is CobolPassMode.Reference or CobolPassMode.Content && f.ByValue)
                    ctx.Edition.Error(DiagnosticCatalog.CallArgumentMode,
                        $"CALL … argument {i + 1} passes {(arg.Mode == CobolPassMode.Content ? "BY CONTENT" : "BY REFERENCE")} "
                        + $"where the corresponding formal parameter '{f.Item.CobolName}' is BY VALUE "
                        + "(ISO §14.9.4.3 SR19)");
                else if (arg.Mode is CobolPassMode.Value && !f.ByValue)
                    ctx.Edition.Error(DiagnosticCatalog.CallArgumentMode,
                        $"CALL … argument {i + 1} passes BY VALUE where the corresponding formal parameter "
                        + $"'{f.Item.CobolName}' is not BY VALUE (ISO §14.9.4.3 SR21)");
                // §14.8.2.3.2 / §14.8.2.2 (BY REFERENCE) and §14.8.2.3.3 (BY CONTENT / BY VALUE), the address-
                // identifier's class-pointer paragraph and the restricted-pointer sentence — THE ONE ARGUMENT HALF
                // OF §14.8.2 (kb/Work PB1418), shared with the function-identifier's §8.4.3.2.3 SR13 lane so the two
                // activations cannot drift. Run-time it would be EC-PROGRAM-ARG-MISMATCH; at bind it is the design's
                // diagnostic lane.
                else
                    host.Params.CheckArgument(f.Item, arg, i + 1, conformanceSite);
            }
        }

        // ── RETURNING (CALL side) — COBOL-2002+ (deep-dive "Edition gating"); maps to the activation result
        //    delivered through the opaque ABI's returning carrier (§14.2.3 GR7). ──
        Place? returning = null;
        if (call.callReturningPhrase() is { } rp)
        {
            // call-returning-2002: the VersionConformancePass owns the edition gate (Exec Step E).
            // kb/Work PB128: identifier-3 is a pure receiver — the chokepoint's screens apply.
            if (host.Expr.ResolveReceiving(rp.dataReference()) is not { } rpl)
                return OperandUnresolved(rp.dataReference(), "RETURNING item");
            ScreenCallOperand(rpl, CobolPassMode.Reference, formatTwo, isReturning: true);
            returning = rpl;
        }
        // §14.9.4.3 SR25 → §14.8.3, RETURNING ITEMS — the half that had NO home at all (kb/Work PB204). SR25
        // makes §14.8.3's rules apply to a Format-2 CALL, and wherever the callee's PD header is bound — the
        // AS NESTED containment table, or a program prototype's §12.3.8.4 GR10 a) definition (PB237) —
        // §14.8.3.1's "if and only if" and §14.8.3.2/§14.8.3.3's description rules are compile-time facts
        // rather than the run-time EC-PROGRAM-ARG-MISMATCH a dynamically-resolved callee would need. THE SAME
        // comparator the USING loop and INVOKE use, so the three can never drift.
        // §14.8.3.1: "The returning item in the activated element is the sending operand, the corresponding
        // returning item in the activating element is the receiving operand" — hence the callee's item is
        // parameter 1, which is also what the §14.8.3.3 rule-4/5 ANY LENGTH relaxation keys on. No prefix
        // latitude: §14.8.3.2 asks for "the same length", not §14.8.2.2 rule 1's "same or smaller".
        if (callee is { } calleeSig)
        {
            if ((returning is null) != (calleeSig.Returning is null))
                ctx.Edition.Error(DiagnosticCatalog.CallReturningConformance,
                    returning is null
                        ? $"{calleeWhere}: the activated program's procedure division header declares "
                          + $"RETURNING '{calleeSig.Returning!.CobolName}' but the CALL statement specifies no "
                          + "RETURNING item (ISO §14.8.3.1 — one shall be specified if and only if the "
                          + "activated element specifies one)"
                        : $"{calleeWhere} specifies a RETURNING item but the activated program's procedure "
                          + "division header declares none (ISO §14.8.3.1)");
            else if (returning is { } rr && calleeSig.Returning is { } cr
                     && CobolNet.Compiler.Oo.OoConformance.DescriptionMismatch(cr, rr.Item,
                            anyLengthActivationRelax: true) is { } rwhy)
                ctx.Edition.Error(DiagnosticCatalog.CallReturningConformance,
                    $"{calleeWhere} RETURNING '{rr.Item.CobolName}' does not conform to the activated "
                    + $"program's returning item '{cr.CobolName}': {rwhy} (ISO §14.8.3 via §14.9.4.3 SR25)");
        }

        // ── Exception phrases — edition-gated spellings (deep-dive "Edition gating"; VERSION_CHANGE_REFERENCE
        //    row 3): [NOT] ON EXCEPTION is ANSI X3.23-1985 surface (CALL Format 2; CCVS-85 IC222A tests both
        //    phrases — "'ON OVERFLOW' CAN BE USED IN PLACE OF 'ON EXCEPTION'"); ON OVERFLOW is the 74-carried
        //    synonym, accepted 85–2014 and REMOVED at 2023 (Annex E.2 item 1c). ──
        List<BoundStatement>? onExc = null, notOnExc = null;
        bool usedOverflow = false;
        // The two phrases live under ONE container (callExceptionPhrases) so either order parses — ISO 5.2.6.4
        // choice indicators. Order of WRITING does not change binding: each phrase keeps its own role.
        var excPhrases = call.callExceptionPhrases();
        if (excPhrases?.callOnExceptionPhrase() is { } onp)
        {
            usedOverflow |= onp.OVERFLOW() is not null;
            onExc = host.BindBlocks([onp.statementBlock()]);
        }
        if (excPhrases?.callNotOnExceptionPhrase() is { } notp)
        {
            usedOverflow |= notp.OVERFLOW() is not null;
            notOnExc = host.BindBlocks([notp.statementBlock()]);
        }

        // ON OVERFLOW is the COBOL-74-carried synonym for ON EXCEPTION, REMOVED at ISO 2023; the edition gate
        // (CallOnOverflowRemoved2023) moved to the post-bind VersionConformancePass (Step 14d), reading the flag.
        return new BoundCallProgram(literalName, dynamicName, args, returning, onExc, notOnExc)
        {
            UsedOverflowSpelling = usedOverflow,
            IsPointerTarget = isPointerTarget,
        };
    }

    /// <summary>Bind ONE address-identifier argument (ISO §8.4.3.1.2 identifier Format 9; §14.9.4.3 SR3/SR4 —
    /// kb/Work PB239) through the ONE operand binder of each arm — <c>PtrBinder.BindDataAddress</c> for
    /// §8.4.3.11, <c>SetBinder.BindProgramAddressOperand</c> for §8.4.3.13 — the same binders the SET senders use,
    /// so the operand rules (the cell-backing check, §8.4.3.13.3 SR1–SR3) are stated once. Null having reported.
    /// <para>⚠ DETERMINATION — §14.9.4.3 SR10 does not reach it. SR10 bars a Format-1 BY REFERENCE identifier-2
    /// "of class object or pointer", and §8.4.3.11.4 GR1 makes an address-identifier "a unique data item of class
    /// pointer". Read literally the two would forbid `CALL "S" USING ADDRESS OF R` (BY REFERENCE implied); the
    /// reading chosen is the one SR3–SR5 and Annex D.6.5.6.4 state — "address-identifiers may be passed in the
    /// non-prototype formats of the CALL statements, and with all three passing mechanisms … it will never be
    /// updated even when passed by reference. It behaves like being passed by content." SR10 protects program
    /// storage from being aliased by a callee that sees it untyped; an address-identifier crosses as a detached
    /// value (SR4, SR5), so there is no storage for that rule to protect. GnuCOBOL, IBM and Micro Focus all accept
    /// the spelling (the CLAUDE.md rule-1 precedence, were the text read as latitude).</para></summary>
    private BoundCallArg? AddressArg(Core.AddressIdentifierContext ai, CobolPassMode mode) =>
        host.Ptr.BindAddressIdentifier(ai, "CALL … USING") is { } ao
            ? new BoundCallArg(mode, null, null) { DataAddress = ao.Data, ProgramAddress = ao.Program }
            : null;

    /// <summary>Bind <c>CANCEL {literal|identifier}…</c> (ISO §14.9.5 — targets resolved like CALL's, §8.4.6.3).</summary>
    /// <summary>The ONE program-name-literal reader (kb/Work PB130) — §14.9.4.3 SR2 / §14.9.5.3 SR2 admit
    /// an alphanumeric OR national literal (a hexadecimal literal is §8.3.3.2 Format 2 of the alphanumeric
    /// kind; a concatenation folds per §8.8.3.3 GR3; the D-N1 identity repertoire makes the national name
    /// the same string). Reports the SR violation itself and returns null — a boolean literal, a numeric
    /// literal, and a zero-length name each draw the cited diagnostic instead of a run-time loud.</summary>
    private string? ProgramNameLiteral(Core.LiteralContext lit, string verb, string clause)
    {
        if (host.Expr.NonNumericLiteralOperand(lit.nonNumericLiteral()) is BoundStringLiteral
            { Category: PicCategory.Alphanumeric or PicCategory.National } sl)
        {
            if (sl.Value.Length != 0) return sl.Value;
            ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentClass,
                $"{verb} with a zero-length literal program name (ISO {clause})");
            return null;
        }
        ctx.Edition.Error(DiagnosticCatalog.IntrinsicArgumentClass,
            $"{verb} '{lit.GetText()}': the literal program name shall be an alphanumeric or national literal "
            + $"(ISO {clause})");
        return null;
    }

    /// <summary>THE program-prototype-name lookup (kb/Work PB237) — ONE place, because §14.9.4.3 syntax rule 16
    /// ("Program-prototype-name-1 shall be specified in a program-specifier in the REPOSITORY paragraph") and
    /// §14.9.5.3 syntax rule 3 ("Program-prototype-name-1 shall be a program prototype specified in the REPOSITORY
    /// paragraph") state the SAME obligation about the same subject, and §8.4.6.8 adds the one further spelling
    /// they both inherit — "the program-name of a containing program definition". The table
    /// (<c>BinderDriver.ProgramPrototypesOf</c>) carries all of it, so both verbs ask this one question. Reports
    /// COBOLNET1760 and returns null when the name is not a program prototype here.</summary>
    private ProgramPrototype? ResolvePrototype(string name, string verb, string alsoNot = "")
    {
        if (host.ProgramPrototypes?.GetValueOrDefault(name) is { } proto) return proto;
        ctx.Edition.Error(DiagnosticCatalog.ProgramPrototypeUndeclared,
            $"{verb} {name}: '{name}' is not a program prototype — it is neither declared by a PROGRAM specifier "
            + "in the REPOSITORY paragraph (ISO §12.3.8.2) nor the program-name of a containing program "
            + $"definition (§8.4.6.8){alsoNot}");
        return null;
    }

    /// <summary>The BARE user-defined word a data reference spells, or null when it is qualified, subscripted,
    /// reference-modified, or one of the special registers the rule lists first. A program-prototype-name is a
    /// plain word (§8.4.6.8 — it names a source unit, so it takes no qualification and no subscript), so this is
    /// the SHAPE test that decides whether an operand written like identifier-1 can be read as
    /// program-prototype-name-1 at all (kb/Work PB237). Used by both reference sites, so CALL and CANCEL cannot
    /// disagree about what a prototype operand looks like.</summary>
    private static string? BarePrototypeWord(Core.DataReferenceContext dref) =>
        dref.cobolWord() is { } w && dref.dataReferenceSuffix().Length == 0 ? w.GetText() : null;

    public BoundStatement BindCancel(Core.CancelStatementContext cancel)
    {
        var targets = new List<(string?, BoundOperand?)>();
        foreach (var t in cancel.cancelTarget())
        {
            if (t.literal() is { } lit)
            {
                // kb/Work PB130: the ONE reader (national + hex admitted per §14.9.5.3 SR2 — the old
                // rejection also miscited §14.9.5.2 SR1); a bad target reports and the LOOP CONTINUES, so a
                // statement with one illegal target no longer discards its legal ones.
                if (ProgramNameLiteral(lit, "CANCEL", "§14.9.5.3 SR2") is { } pn)
                    targets.Add((pn, null));
            }
            else if (t.dataReference() is { } dref)
            {
                // §14.9.5.2's THIRD brace alternative — program-prototype-name-1 (kb/Work PB237). It has no
                // production of its own because it is spelled exactly like identifier-1, so the discrimination is
                // semantic and takes the SAME order the CALL twin takes: Probe the data reference first (Probe,
                // not Resolve — an unresolved Probe reports nothing), and only a word that is NOT a data item is
                // read as a prototype. §14.9.5.4 GR1 c) then makes the prototype the identification of the program
                // to be canceled, and §12.3.8.4 GR10 NOTE 1 says which name that is — literal-3 if the specifier
                // wrote AS, otherwise the prototype name. A prototype cancel is therefore a STATIC-name cancel,
                // the same bound shape literal-1 produces (GR1 b)); the run-time locate is GR2's.
                if (ctx.Refs.Probe(dref) is null && BarePrototypeWord(dref) is { } cWord)
                {
                    if (host.ProgramPrototypes?.GetValueOrDefault(cWord) is { } cProto)
                    {
                        targets.Add((cProto.ExternalizedName, null));
                        continue;
                    }
                    // §14.9.5.3 SR1 and SR3 together: the word is neither an alphanumeric/national data item nor a
                    // program prototype, so NEITHER identifier-shaped alternative of §14.9.5.2's operand brace is
                    // available. The resolver's §8.4.2.1 "not defined" report names only the data reading, which
                    // would leave SR3's obligation invisible on the one operand that can violate it.
                    ResolvePrototype(cWord, "CANCEL",
                        ", and no data item of that name is defined either — §14.9.5.2's operand brace is "
                        + "identifier-1, literal-1 or program-prototype-name-1");
                    continue;
                }
                // ⛔ A TARGET THAT DID NOT RESOLVE IS NEVER DROPPED SILENTLY (kb/Work PB1030): a reported one keeps
                // binding the rest for their diagnostics (the compile fails anyway); a DEFERRED one refuses the
                // statement, which would otherwise run and skip that target.
                if (host.Expr.ResolveSending(dref) is var cr && cr.Outcome == RefOutcome.Deferred) return cr.Refusal(ctx.Edition);
                if (cr.Place is { } p)
                {
                    // §14.9.5.3 SR1 (kb/Work PB154): "Identifier-1 shall be defined as an alphanumeric or
                    // national data item" — the CALL twin's class screen (PB132) that this arm never got:
                    // CANCEL WS-NUM compiled clean and the digit image resolved to no program, a silent
                    // no-op at both stages. Same rule family, but NOT the same rule — §14.9.4.3 SR1 also
                    // admits program-pointer, so CANCEL carries its own descriptor.
                    if (IntrinsicArgumentRules.ClassOf(new BoundFieldOperand(p))
                        is { } cCls and not (CobolClass.Alphanumeric or CobolClass.National))
                    {
                        ctx.Edition.Error(DiagnosticCatalog.CancelTargetCategory,
                            $"CANCEL target '{DataBinder.WrittenText(dref)}' is of class {cCls.ToString().ToLowerInvariant()}; "
                            + "ISO §14.9.5.3 SR1 admits an alphanumeric or national data item");
                        continue;
                    }
                    targets.Add((null, new BoundFieldOperand(p)));
                }
                // an unresolved name was already diagnosed by the resolver; keep binding the rest
            }
        }
        return new BoundCancel(targets);
    }

    /// <summary>Bind <c>GOBACK [RETURNING x]</c> (ISO §14.9.18). GOBACK itself was introduced by ISO/IEC
    /// 1989:2002 — at <c>--std 85</c> it is rejected with a targeted diagnostic (the G1 lacks-it obligation;
    /// COBOL-85 programs use STOP RUN / EXIT PROGRAM). The 2023 WITH ERROR/NORMAL STATUS phrase (§14.9.18.2,
    /// VERSION_CHANGE_REFERENCE row 75) parses through the shared <c>statusPhrase</c> rule and is introduction-
    /// gated by the VersionConformancePass (GobackStatus2023); it is decoded and screened by the shared
    /// STOP+GOBACK termination-status slice (<see cref="DecodeGobackPhrases"/>), before the program/method fork.</summary>
    public BoundStatement BindGoback(Core.GobackStatementContext g)
    {
        // §14.9.18.3 SR1, asked BEFORE the program/method fork because the rule has no such qualifier — through
        // the ONE asker that also serves EXIT's §14.9.14.3 SR2 (kb/Work PB409). The run-time twin, §14.9.18.4
        // GR6's "executed within the RANGE of" a global declarative, is LEGAL source and is raised by
        // CallEmitter.EmitGoback as EC-FLOW-GLOBAL-GOBACK; this screen must not try to approximate it.
        if (PlacementRules.RefusedInGlobalDeclarative(ctx, EcRaiseSite.Goback)) return BoundRejected.Reported(ctx.Edition);
        var p = DecodeGobackPhrases(g);
        if (host.InMethod) return host.Oo.OoBindMethodGoback(p);   // §14.9.18.4 GR4 — a METHOD return, never an activation return (D8)
        // goback-bare-2002 / goback-returning-2002: the VersionConformancePass owns both edition gates
        // (Exec Step E folded the bare-GOBACK 0880; the RETURNING 0900 subsumption lives in its parse arm).
        Place? source = null;
        if (p.Returning is { } dref)
        {
            // GOBACK RETURNING x ≡ move x into the procedure-division RETURNING item, then return (§14.9.18 GR2
            // — the activation result; the grammar already 2002-gates the phrase).
            if (host.Expr.ResolveSending(dref) is var gr && gr.Place is not { } rp)
                return gr.Refusal(ctx.Edition);   // kb/Work PB1030
            source = rp;
        }
        // ⛔ The RETURNING move is BOUND here (kb/Work PB880) — into the program's procedure-division RETURNING
        // item, the same item ProgramEmitter hands the activation result from. It used to be built by
        // CallEmitter.EmitGoback, after every bind-time MOVE screen and storage fact had already run.
        BoundMove? returningMove = source is not null && ctx.Data.LinkageReturning is { } retItem
                                   && ctx.Refs.ResolveItem(retItem) is { } retPlace
            ? host.Move.BindMoveOf(new BoundFieldOperand(source), [retPlace], ImplicitMovePhrase.GobackReturning)
            : null;
        if (p.Raising is { } raising)
            return host.Ec.EcBindRaising(raising, g.Start.Line, EcRaiseSite.Goback) is { } r
                ? new BoundGoback(source, r) { ReturningMove = returningMove }
                : new BoundUnsupported("GOBACK RAISING identifier (the exception-object form of the RAISING phrase — the OO wave)");
        // GOBACK … WITH {NORMAL|ERROR} STATUS [value] (§14.9.18.2, COBOL-2023, 2023-gated in the pass; mutually
        // exclusive with RAISING by the grammar). The emit passes the decoded status to the OS only in a MAIN
        // program (§14.9.18.4 GR3/GR10 — a called-program status is inert).
        return new BoundGoback(source, null, p.Status) { ReturningMove = returningMove };
    }

    /// <summary>⛔ <b>THE ONE DECODE of every phrase §14.9.18.2's general format gives GOBACK</b>, run BEFORE the
    /// program/method fork so no arm can be reached without it (kb/Work PB411).
    /// <para>WHY IT IS A DECODE AND NOT TWO BINDERS. The fork used to hand the whole
    /// <c>GobackStatementContext</c> to <c>OoBinder.OoBindMethodGoback</c>, which RE-DECIDED which phrases
    /// exist — and read only <c>dataReference()</c> and <c>raisingPhrase()</c>. The 2023 status phrase was
    /// therefore discarded in silence on the method arm: §14.9.18.3 SR6/SR7/SR8 never ran there, and neither did
    /// the COBOL-2023 introduction gate (measured: <c>GOBACK WITH ERROR STATUS ""</c> inside a method compiled
    /// and ran clean at 2023, and <c>… STATUS 1.5</c> at 2014). That is the two-arm dispatch shape: a phrase is
    /// added to the ONE grammar rule and only one consumer learns of it. With the decode here and the method arm
    /// taking <see cref="GobackPhrases"/> instead of the parse node, an arm CANNOT read past a phrase it does
    /// not know about — <c>GobackPhraseDecodeDriftTests</c> holds both halves of that true.</para>
    /// <para>The STATUS phrase is decoded to its bound form because its screen is CONTEXT-FREE: §14.9.18.3's
    /// syntax rules carry no "in a main program" qualifier (only General rules 7–10 do), so SR6/SR7/SR8 are owed
    /// by a method's GOBACK exactly as by a program's. RETURNING and RAISING stay parse contexts: what the two
    /// arms DO with them genuinely differs (§14.9.18.4 GR2's activation result vs GR4's method return).</para></summary>
    /// <remarks>⛔ THE TAIL IS A LIST because §14.9.18.2's bracket carries CHOICE INDICATORS (kb/Work PB407):
    /// both phrases may be written, in either order, each once. The grammar says zero-or-more-any-order with a
    /// <c>*</c>; §5.2.6.4's "only once" half is read HERE, through the shared
    /// <see cref="ChoiceIndicators.AtMostOnce"/> — not as a hand-written "if both, error", so the next figure
    /// with choice indicators inherits it.</remarks>
    private GobackPhrases DecodeGobackPhrases(Core.GobackStatementContext g) =>
        new(g.dataReference(),
            ChoiceIndicators.AtMostOnce(ctx.Edition, g.raisingPhrase(), "GOBACK", "the RAISING phrase", "14.9.18.2"),
            host.ControlFlow.BindTerminationStatus(
                ChoiceIndicators.AtMostOnce(ctx.Edition, g.statusPhrase(), "GOBACK", "the status phrase", "14.9.18.2")));

    /// <summary>The phrases of ONE <c>gobackStatement</c>, decoded once by <see cref="DecodeGobackPhrases"/> and
    /// consumed by both arms of the §14.9.18.4 GR2/GR4 fork — the activation return (<see cref="BindGoback"/>)
    /// and the method return (<c>OoBinder.OoBindMethodGoback</c>).</summary>
    /// <param name="Returning">RETURNING/GIVING identifier-1 (§14.9.18.4 GR2's activation result) — the only
    /// direct <c>dataReference</c> child of the rule; the RAISING tail nests its own.</param>
    /// <param name="Raising">The RAISING phrase (§14.9.18.3 SR2–SR5), unbound: the program arm binds it through
    /// <c>EcBinder.EcBindRaising</c> and the method arm through <c>OoBinder.OoBindMethodRaising</c>.</param>
    /// <param name="Status">The 2023 status phrase, ALREADY SCREENED by §14.9.18.3 SR6/SR7/SR8 (the shared
    /// <c>ControlFlowBinder.BindTerminationStatus</c>), or null when the phrase is absent.</param>
    internal readonly record struct GobackPhrases(
        Core.DataReferenceContext? Returning,
        Core.RaisingPhraseContext? Raising,
        TerminationStatus? Status);

    /// <summary>
    /// ISO §14.9.4.3 SR22 — a BY VALUE operand "shall be of class numeric, object, or pointer".
    /// </summary>
    /// <remarks>
    /// <para>
    /// Strict-reject with the leniency dialect-gated, the disposition DA6 established for the sibling §8.8.1.1
    /// question. GnuCOBOL accepts an alphanumeric operand as an extension and silently assumes BY CONTENT — a
    /// DIFFERENT passing mode from the one written, which is why accepting it quietly is the wrong kindness: the
    /// callee would receive an address where the source said value.
    /// </para>
    /// <para>
    /// ⚠ Fail-open on an undecidable class, exactly as the intrinsic screen does: a false reject turns legal
    /// COBOL away, a missed one leaves a rule unenforced, and only the first is forbidden outright.
    /// </para>
    /// </remarks>
    private void CheckByValueClass(string what, BoundComputedOperand operand)
    {
        // ⛔ CLASSIFY THE UNDERLYING REFERENCE, NOT THE WRAPPER — ParameterConformance.ValueArgumentClass unwraps a
        // bare identifier (BoundNumRef) and an index-name (BoundIndexRef, class index — kb/Work PB132). It is the
        // SAME class answer the function-identifier's §8.4.3.2.3 SR10 screen asks (kb/Work PB1418).
        ValueClassScreen(ParameterConformance.ValueArgumentClass(operand), what);
    }

    /// <summary>ISO §14.9.4.4 GR8 — <b>the ONE reduction</b> that turns a Format-2 argument's PARSE NODE into
    /// what the standard says the argument IS: "An argument that consists merely of a single identifier or
    /// literal is regarded as an identifier or literal rather than an arithmetic or boolean expression."
    /// <para>Two facts about the grammar make it necessary, and they are the same two on both the CALL and
    /// INVOKE sides: <c>booleanExpression</c>'s leaf is <c>valueOperand</c>, so a B-operator-FREE boolean node
    /// holds an ordinary arithmetic operand; and <c>arithmeticExpression</c> SUBSUMES <c>dataReference</c> and
    /// every numeric literal, so a lone identifier reaches the binder inside an expression node. The predicate
    /// therefore decides only WHICH NODE an operand lands in, never what it means.</para>
    /// <para>⛔ IT IS SHARED BY EVERY SPELLING (kb/Work PB238). The reduction used to be written out inside the
    /// BY CONTENT arm alone, so GR8 held for <c>BY CONTENT N</c> and not for <c>BY VALUE N</c> or for the
    /// keyword-less <c>N</c> — one rule, four arms, one of them fixed. Pure: it reads parse contexts, resolves
    /// nothing and reports nothing.</para></summary>
    private static (Core.LiteralContext? Literal, Core.NonNumericLiteralContext? BareLiteral,
                    Core.DataReferenceContext? Reference, Core.BooleanExpressionContext? Bool,
                    Core.ArithmeticExpressionContext? Arith)
        Gr8Classify(Core.LiteralContext? lit, Core.DataReferenceContext? dref,
                    Core.BooleanExpressionContext? boolExpr, Core.ArithmeticExpressionContext? arith)
    {
        Core.NonNumericLiteralContext? bareLit = null;
        if (boolExpr is not null && ConditionBinder.UnwrapBareBool(boolExpr) is { } bare)
        {
            boolExpr = null;
            arith = bare.arithmeticExpression();
            // ⛔ AND ITS LITERAL LEG (kb/Work PB238). `valueOperand` is `arithmeticExpression |
            // nonNumericLiteral`, so an operand that reached the boolean node ON ANOTHER ARGUMENT'S
            // B-OPERATOR (the predicate's scan runs to the statement's period) can be a plain literal:
            // `CALL "S" AS NESTED USING "AB" B1 B-AND B2` put "AB" in a booleanExpression node. Recovering
            // only the arithmetic leg left it classified as nothing at all, and the argument staged a RUN-TIME
            // NotImplemented on conforming source. OoBindInvokeArg's reduction has always recovered both legs;
            // this one recovered one, which is the same rule written twice and only one of them complete.
            bareLit = bare.nonNumericLiteral();
        }
        // The grammar keeps a `dataReference` arm on callByContent (the legacy binder shares this rule), so a
        // bare identifier can land there directly; the sole-reference reduction covers the one that arrived
        // inside an expression node. The two paths must agree, which is why both are consulted.
        return (lit, bareLit, dref ?? ConditionBinder.SoleDataReference(arith), boolExpr, arith);
    }

    /// <summary>ISO §14.9.4.3 SR23 — "If literal-2 or its corresponding formal parameter is specified with the
    /// BY VALUE phrase, literal-2 shall be a numeric literal." Reported BY NAME (kb/Work PB238): the rule was
    /// previously enforced only as a side effect of the grammar's expression spine, which bottoms out at
    /// <c>numericLiteral | ZERO_ARITH | functionCall | dataReference | ( … )</c> — the right verdict, delivered
    /// as a raw ANTLR "no viable alternative" that names no rule, and unreachable altogether for the rule's
    /// second subject (a keyword-less literal-2 whose FORMAL carries the phrase).</summary>
    private void Sr23LiteralIsNumeric(Core.LiteralContext lit, string subject)
    {
        // The figurative ZERO is a numeric literal wherever a literal is restricted to numeric (§8.3.3.6.3
        // SR1a) — and it never reaches here under an explicit BY VALUE, where ZERO_ARITH takes the earlier
        // `arithmeticExpression` alternative; the keyword-less arm can still spell it, so admit it by rule.
        if (lit.numericLiteral() is not null
            || lit.nonNumericLiteral()?.figurativeConstant()?.zeroWord() is not null)
            return;
        ctx.Edition.Error(DiagnosticCatalog.CallByValueLiteralKind,
            $"CALL … USING {lit.GetText()} with {subject}: literal-2 shall be a NUMERIC literal "
            + "(ISO §14.9.4.3 SR23)");
    }

    /// <summary>The <see cref="Sr23LiteralIsNumeric"/> twin for a constant-name argument. §13.10.4 GR1 makes
    /// it literal-2 by substitution and §13.10.4 GR2 gives it the SUBSTITUTED literal's class and category,
    /// so SR23 is a question about that literal and never about the name (§13.10.3 SR2 is the admission —
    /// a constant-name "may be used anywhere that a format specifies a literal of [its] class and
    /// category" — which is why the Format-2 literal-2 slot accepts one at all).</summary>
    private void Sr23ConstantIsNumeric(Core.DataReferenceContext dref, CobolPassMode mode)
    {
        if (ctx.Data.ConstantOf(dref) is not { } k || k.Category is PicCategory.Numeric) return;
        ctx.Edition.Error(DiagnosticCatalog.CallByValueLiteralKind,
            $"CALL … USING {DataBinder.WrittenText(dref)} with a {(mode is CobolPassMode.Value ? "BY VALUE" : "BY CONTENT")} "
            + $"formal parameter: the constant-name substitutes the {k.Category.ToString().ToLowerInvariant()} "
            + $"literal {k.RawText} (ISO §13.10.4 GR1/GR2), and §14.9.4.3 SR23 requires literal-2 to be a numeric "
            + "literal when the corresponding formal parameter is specified with the BY VALUE phrase");
    }

    /// <summary>The §14.9.4.3 SR22 class screen, shared by the EXPLICIT BY VALUE arm and the Format-2
    /// bare argument whose FORMAL is BY VALUE (kb/Work PB132 — the two arms each ran half the rule).</summary>
    private void ValueClassScreen(CobolClass? actual, string what)
    {
        if (actual is null or CobolClass.Numeric or CobolClass.Object or CobolClass.Pointer) return;
        string rule = $"CALL … USING BY VALUE operand '{what}' is of class {actual.Value.ToString().ToLowerInvariant()}; "
            + "ISO §14.9.4.3 SR22 admits only class numeric, object or pointer by value";
        if (ctx.Edition.Permissive)
            ctx.Edition.Warning(DiagnosticCatalog.CallByValueOperandClass, $"{rule}; accepted under --permissive");
        else
            ctx.Edition.Error(DiagnosticCatalog.CallByValueOperandClass, $"{rule}. --permissive accepts it as an extension");
    }

    /// <summary>An unresolved CALL operand (kb/Work PB132): a SCREEN SECTION name draws the CITED §14.9.4.3
    /// SR3/SR7 rejection at bind time — the R32 posture distinguishes "declared in an unsupported section"
    /// from "not defined", and the old BoundUnsupported staged the answer to run time (the PB88 wrong-stage
    /// shape). Everything else keeps the staged-loud posture.</summary>
    /// <param name="answer">The SENDING resolver's closed answer (kb/Work PB1030); absent for a receiving operand,
    /// whose chokepoint (<see cref="ExpressionBinder.ResolveReceiving"/>) reports every null it returns.</param>
    private BoundStatement OperandUnresolved(Core.DataReferenceContext dref, string role, RefResolution? answer = null)
    {
        string text = dref.GetText();
        if (ctx.Data.ScreenNames.Contains(text))
        {
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.CallOperandSection,
                $"CALL {role} '{text}' names a SCREEN SECTION entry; ISO §14.9.4.3 "
                + (role.StartsWith("RETURNING") ? "SR7" : "SR3")
                + " requires a data item defined in the file, working-storage, local-storage, or linkage section");
        }
        return answer?.Refusal(ctx.Edition) ?? BoundRejected.Reported(ctx.Edition);
    }

    /// <summary>The CALL operand chokepoint (kb/Work PB132) — ISO §14.9.4.3 SR3/SR6/SR8/SR10/SR11/SR12/SR18
    /// over the RESOLVED mode (after GR5's transitivity or GR9's formal derivation), for every Place-carrying
    /// USING argument and the RETURNING item, so both arms of every dispatch meet the same law.</summary>
    private void ScreenCallOperand(Place p, CobolPassMode mode, bool formatTwo, bool isReturning)
    {
        var item = p.Item;
        string? name = item.CobolName;
        string role = isReturning ? "RETURNING item" : "USING argument";

        // ⛔ THE ANY LENGTH SCREEN IS EXACTLY AS WIDE AS THE TWO RULES THAT STATE IT (kb/Work PB240):
        //   SR11 (FORMAT 1) — "Identifier-2 and identifier-3 shall not be described with the ANY LENGTH clause":
        //        every USING operand in every mode, and the RETURNING item.
        //   SR18 (FORMAT 2) — "Identifier-4 shall not be described with the ANY LENGTH clause": identifier-4 is
        //        the BY CONTENT / BY VALUE operand of the Format-2 figure, so only those modes.
        // A FORMAT-2 BY REFERENCE identifier-2 and a Format-2 identifier-3 are NOT screened here, because no
        // syntax rule forbids them: SR25 hands both to §14.8's conformance rules, which ADMIT an ANY LENGTH
        // operand against an ANY LENGTH formal/returning item — §14.8.2.3.2 rule e) ("If the argument is
        // described with the ANY LENGTH clause, the corresponding formal parameter shall be described with the
        // ANY LENGTH clause") and §14.8.3.3 rule 4 (the receiving returning item likewise) — and the
        // activation-mode DescriptionMismatch in the conformance loop below states exactly those. The screen
        // used to fire for EVERY Format-2 operand, refusing a contained program that forwards its own ANY
        // LENGTH formal `CALL "C" AS NESTED USING BY REFERENCE L` to another ANY LENGTH formal — legal source
        // (§13.18.2.3 SR2/SR3 make such a formal exist only in a contained program, function or method).
        bool anyLengthBanned = !formatTwo || (!isReturning && mode is CobolPassMode.Content or CobolPassMode.Value);
        if (item.IsAnyLength && anyLengthBanned)
            ctx.Edition.Error("COBOLNET1542", $"CALL {role} '{name}' is described with the ANY LENGTH clause "
                + (formatTwo ? "(ISO §14.9.4.3 SR18)" : "(ISO §14.9.4.3 SR11)"));

        // SR3 sentence 2: BY REFERENCE (specified or implied) shall not carry factory/instance object data.
        if (!isReturning && mode is CobolPassMode.Reference && ctx.Data.OoIsObjectData(item))
            ctx.Edition.Error(DiagnosticCatalog.CallByReferenceObjectData,
                $"CALL … USING BY REFERENCE '{name}': identifier-2 shall not be defined in the working-storage "
                + "or file section of a factory or an instance object (ISO §14.9.4.3 SR3)");

        // SR10 (Format 1, BY REFERENCE specified or implied).
        if (!isReturning && !formatTwo && mode is CobolPassMode.Reference)
        {
            string? kind =
                StrongTypeModel.IsStrongGroup(item) ? "a strongly-typed group item"
                : p.Pic?.Category is PicCategory.ObjectReference ? "a data item of class object"
                : p.Pic?.Category is PicCategory.Pointer or PicCategory.ProgramPointer
                                     or PicCategory.FunctionPointer ? "a data item of class pointer"
                : null;
            if (kind is not null)
                ctx.Edition.Error(DiagnosticCatalog.CallByReferenceOperandKind,
                    $"CALL … USING BY REFERENCE '{name}' is {kind} (ISO §14.9.4.3 SR10 — Format 1 shall pass "
                    + "neither by reference; the program-prototype CALL admits them under §14.8.2)");
        }

        // SR12 (Format 1, any mode): no variable-length group (§8.5.1.12.1) — the ONE predicate.
        // ⛔ THE `!isReturning && !formatTwo` GATE IS SPEC-CORRECT ON BOTH CONJUNCTS. DO NOT "FIX" IT, and do
        // not clone this screen at INVOKE (kb/Work PB177 arm C — the derivation reversed the finding that
        // proposed exactly that). In the PRINTED standard SR12 sits under the **FORMAT 1** heading, which opens
        // at SR10 and closes at SR13's `FORMAT 2`, and its sentence names **identifier-2** only — never
        // identifier-3, the RETURNING item of SR7/SR9. Format 2's own law is SR25: "The rules for conformance
        // specified in 14.8.2, Parameters and 14.8.3, Returning items apply" — and §14.8.2.2 / §14.8.3.2 ADMIT
        // a variable-length group subject to the §8.5.1.12 COMPATIBILITY relation rather than forbidding it,
        // with §14.9.4.4 GR3d making the verdict a RUNTIME EC-PROGRAM-ARG-MISMATCH where the callee's
        // description is not statically visible. §14.9.23.3 SR1–SR17 (INVOKE) contains no such prohibition at
        // all; an SR12 clone there would REJECT LEGAL SOURCE. The compatibility relation itself now EXISTS —
        // VariableLengthCompatibility, applied by OoConformance.DescriptionMismatch at all three Format-2 /
        // INVOKE boundaries, with the crossing carried by CobolVarGroup (kb/Work PB204) — so this gate stays
        // exactly as narrow as SR12's own sentence.
        if (!isReturning && !formatTwo && item.IsGroup && ReferenceResolver.HasVariableLengthSubordinate(item))
            ctx.Edition.Error(DiagnosticCatalog.CallVariableLengthGroup,
                $"CALL … USING argument '{name}' references a variable-length group (a DYNAMIC LENGTH item or "
                + "dynamic-capacity table is subordinate to it) — ISO §14.9.4.3 SR12");

        // SR6 (BY REFERENCE argument) / SR8 (RETURNING): a bit item must sit statically on a byte boundary — the
        // proof the function-identifier's §8.4.3.2.3 SR14 asks too, so it lives in ParameterConformance.
        if ((isReturning || mode is CobolPassMode.Reference) && BitLayout.IsBitItem(item))
            host.Params.ScreenBitAlignment(p, DiagnosticCatalog.CallBitAlignment, $"CALL {role}",
                isReturning ? "§14.9.4.3 SR8" : "§14.9.4.3 SR6");
    }
}
