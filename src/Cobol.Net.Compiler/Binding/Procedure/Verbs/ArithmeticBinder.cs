// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Editions.Diagnostics;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// The arithmetic-verb binder (P7 Step 10p): ADD (ISO §14.9.2) / SUBTRACT (§14.9.44) / MULTIPLY (§14.9.26) /
/// DIVIDE (§14.9.12, all five formats incl. REMAINDER's single GIVING receiver, §14.9.12.2 Formats 4–5) / COMPUTE (§14.9.8 Format 1 + the Format-2 boolean
/// compute with the F1→F2 sole-boolean re-route — the "ANTLR alternative-order reality" precedent). The CORR
/// formats (ADD/SUBTRACT Format 3) retarget <see cref="CorrespondingBinder"/> via the host accessor. The shared
/// receiving machinery — BindSizeError/BuildSizeError, Receivers, RoundingOf, ResolveReceiving — and the
/// expression spine (BindExpr) RIDE THE HOST until 10q (ExpressionBinder), when those edges flip; the remaining
/// host edges flip at 10t. Boolean-channel edges (BindBoolExpr / Gr3Width / SoleDataRef) go to
/// <see cref="ConditionBinder"/> (statics direct, the instance via the host forwarder — 10o deviation (a)).
/// The COBOLNET1511 Format-2 SR bodies stay in-binder pending the 10t pure-lift sweep (10o deviation (b)).
/// </summary>
internal sealed class ArithmeticBinder(BinderContext ctx, StatementBinder host)
{
    public BoundStatement BindAdd(Core.AddStatementContext add)
    {
        if (add.sendingOperand() is not { Length: > 0 } operands) return host.Corr.BindAddCorresponding(add);   // Format 3 (§14.9.2.2)
        var addends = operands.Select(host.Expr.BindExpr).ToList();
        var sizeErr = host.BindSizeError(add.arithmeticOnSizeError());
        if (add.arithmeticGivingPhrase() is { } giving)
        {
            // ADD a… [TO b] GIVING c…  →  c = (b +) Σa  (ISO §14.9.2.2 Format 2: the TO operand is an addend,
            // NOT a receiver — ONE `{identifier-2 | literal-2}` with no ROUNDED; kb/Work PB134 widened the
            // grammar to the figure and this narrows the union back per format).
            if (add.addToPhrase()?.receiversOrSendingOperand() is { } toAddend)
            {
                Format2SendingOperand("ADD", "§14.9.2.2", toAddend.receivingArithmeticOperand());
                if (toAddend.receivingArithmeticOperand() is { Length: > 0 } tors)
                    addends.AddRange(tors.Select(r => host.Expr.BindExpr(r.dataReference())));
                else
                    // The literal / functionCall arm binds through the WRAPPER so the operand walk's
                    // function-call arm fires — passing the raw FunctionCallContext re-springs PB45's
                    // documented trap (the walk descends into the ARGUMENT and drops the function; the
                    // first probe of this very fix computed 1 + 9 for `ADD 1 TO FUNCTION SQRT(9)`).
                    addends.Add(host.Expr.BindExpr(toAddend));
            }
            var givingRecv = host.Expr.Receivers(giving.receivingArithmeticOperand(), editedOk: true, "§14.9.2.3 SR4");
            // §14.9.2.3 SR1b: the ADD Format-2 composite is "all of the operands ... excluding the data items that
            // follow the word GIVING" — the resultant identifiers are NOT superimposed into the composite (§14.7.7
            // rule 2). Pass no receivers so a wide GIVING target does not spuriously push the composite past 31
            // (COBOLNET0805). MULTIPLY (§14.9.26.3 SR4 counts the GIVING receiver) is correctly unchanged.
            ctx.Validation.CheckComposite("ADD", addends, []);
            return new BoundAddGiving(addends, givingRecv, sizeErr);
        }
        if (add.addToPhrase()?.receiversOrSendingOperand() is { } to)
        {
            if (!Format1Receivers("ADD", "§14.9.2.2", [to]))
                return BoundRejected.Reported(ctx.Edition);
            var recv = host.Expr.Receivers(to.receivingArithmeticOperand(), editedOk: false, "§14.9.2.3 SR2");
            ctx.Validation.CheckComposite("ADD", addends, recv);
            return new BoundAddTo(addends, recv, sizeErr);
        }
        // §14.9.2.2 (kb/Work PB134): no ADD format prints operands with BOTH phrases absent — `ADD A B.` is
        // illegal source, and the old BoundUnsupported staged it to a RUNTIME loud.
        return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ArithmeticFormatOperand,
            "ADD without a TO or GIVING phrase: no ADD format prints operands alone (ISO §14.9.2.2)");
    }

    public BoundStatement BindSubtract(Core.SubtractStatementContext sub)
    {
        if (sub.sendingOperand() is not { Length: > 0 } operands) return host.Corr.BindSubtractCorresponding(sub);   // Format 3 (§14.9.44.2)
        var minuends = operands.Select(host.Expr.BindExpr).ToList();
        var sizeErr = host.BindSizeError(sub.arithmeticOnSizeError());
        if (sub.arithmeticGivingPhrase() is { } giving && sub.subtractFromPhrase()?.receiversOrSendingOperand() is { } from)
        {
            Format2SendingOperand("SUBTRACT", "§14.9.44.2", from.receivingArithmeticOperand());
            var fromX = host.Expr.BindExpr(from);
            var recv = host.Expr.Receivers(giving.receivingArithmeticOperand(), editedOk: true, "§14.9.44.3 SR4");
            // §14.9.44.3 SR1b: the SUBTRACT Format-2 composite excludes the data items following GIVING (§14.7.7
            // rule 2) — the resultants are not superimposed. Pass no receivers (see the ADD GIVING note above).
            ctx.Validation.CheckComposite("SUBTRACT", [.. minuends, fromX], []);
            return new BoundSubtractGiving(minuends, fromX, recv, sizeErr);
        }
        if (sub.subtractFromPhrase()?.receiversOrSendingOperand() is { } targets)
        {
            if (!Format1Receivers("SUBTRACT", "§14.9.44.2", [targets]))
                return BoundRejected.Reported(ctx.Edition);
            var recv = host.Expr.Receivers(targets.receivingArithmeticOperand(), editedOk: false, "§14.9.44.3 SR2");
            ctx.Validation.CheckComposite("SUBTRACT", minuends, recv);
            return new BoundSubtractFrom(minuends, recv, sizeErr);
        }
        // §14.9.44.2 (kb/Work PB134): the same no-phrase screen as ADD's.
        return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ArithmeticFormatOperand,
            "SUBTRACT without a FROM phrase: no SUBTRACT format prints operands alone (ISO §14.9.44.2)");
    }

    public BoundStatement BindMultiply(Core.MultiplyStatementContext mul)
    {
        if (mul.sendingOperand() is not { } aCtx)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "MULTIPLY with no multiplicand: every MULTIPLY format prints identifier-1 or "
                + "literal-1 after the verb (ISO §14.9.26.2)");
        var a = host.Expr.BindExpr(aCtx);
        var byOps = mul.multiplyByOperand();
        var sizeErr = host.BindSizeError(mul.arithmeticOnSizeError());
        if (mul.arithmeticGivingPhrase() is { } giving && byOps.Length > 0)
        {
            // §14.9.26.2 Format 2: ONE `BY {identifier-2 | literal-2}` sending operand, no ROUNDED.
            if (byOps.Length > 1 || byOps[0].roundedPhrase() is not null)
                ctx.Edition.Error(DiagnosticCatalog.ArithmeticFormatOperand,
                    "MULTIPLY … BY … GIVING: Format 2 prints ONE `BY {identifier-2 | literal-2}` operand "
                    + "with no ROUNDED (ISO §14.9.26.2; the extra operands the old binder silently dropped)");
            var b = host.Expr.BindExpr(byOps[0]);
            var recv = host.Expr.Receivers(giving.receivingArithmeticOperand(), editedOk: true, "§14.9.26.3 SR2");
            ctx.Validation.CheckComposite("MULTIPLY", [a, b], recv);
            return new BoundMultiplyGiving(a, b, recv, sizeErr);
        }
        // In-place: each BY operand is itself the receiver (target ← target × a).
        if (!Format1Receivers("MULTIPLY", "§14.9.26.2", byOps))
            return BoundRejected.Reported(ctx.Edition);
        var byRecv = host.Expr.Receivers(byOps);
        ctx.Validation.CheckComposite("MULTIPLY", [a], byRecv);
        return new BoundMultiplyBy(a, byRecv, sizeErr);
    }

    public BoundStatement BindDivide(Core.DivideStatementContext div)
    {
        if (div.sendingOperand() is not { } aCtx) return DivideShape();
        var a = host.Expr.BindExpr(aCtx);   // INTO: the divisor; BY: the dividend
        var sizeErr = host.BindSizeError(div.arithmeticOnSizeError());

        // DIVIDE … GIVING q REMAINDER r (ISO §14.9.12 Formats 4–5): exactly one GIVING receiver (§14.9.12.2 — identifier-3 carries no ellipsis in either format).
        if (div.divideRemainderPhrase() is { } rem)
        {
            if (div.arithmeticGivingPhrase() is not { } g)
            {
                // §14.9.12.2 Formats 4–5 print GIVING before REMAINDER — no format has REMAINDER without it.
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ArithmeticFormatOperand,
                    "DIVIDE … REMAINDER without GIVING: Formats 4–5 print GIVING identifier-3 before "
                    + "REMAINDER (ISO §14.9.12.2)");
            }
            var quotients = host.Expr.Receivers(g.receivingArithmeticOperand(), editedOk: true, "§14.9.12.3 SR2");
            if (quotients.Count != 1)
            {
                // §14.9.12.2 Formats 4–5 print exactly ONE identifier-3. It is a GENERAL FORMAT fact and
                // there is no syntax rule to pair with it — §14.9.12.3 has four (kb/Work PB388).
                return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ArithmeticFormatOperand,
                    "DIVIDE … GIVING … REMAINDER: Formats 4–5 print exactly one quotient receiver "
                    + "(ISO §14.9.12.2)");
            }
            // kb/Work PB128: identifier-4 rides the ONE receiving chokepoint like every other resultant —
            // the direct Refs.Resolve bypass skipped the CONSTANT RECORD / CAPACITY-register / constant-name
            // screens, and §14.9.12.3 SR2 fixes its category (numeric or numeric-edited).
            if (host.Expr.ResolveReceiving(rem.dataReference()) is not { } r0
                || host.Expr.ScreenResultant(r0, rem.dataReference().GetText(), editedOk: true, "§14.9.12.3 SR2") is not { } r)
                return new BoundUnsupported($"DIVIDE REMAINDER receiver '{rem.dataReference().GetText()}'");
            BoundExpr dividend = div.divideIntoPhrase() is { } i ? host.Expr.BindExpr(i.receiversOrSendingOperand())
                : div.divideByPhrase() is not null ? a
                : a;
            BoundExpr divisor = div.divideIntoPhrase() is not null ? a
                : div.divideByPhrase() is { } b ? host.Expr.BindExpr(b.sendingOperand())
                : a;
            ctx.Validation.CheckComposite("DIVIDE", [dividend, divisor], quotients);
            ctx.Data.OoClaimInterleavedReceiver(r);   // the quotient was claimed with the other Receivers (ExpressionBinder.ReceiverOf)
            return new BoundDivideRemainder(dividend, divisor, quotients[0], r, sizeErr);
        }

        if (div.divideIntoPhrase() is { } into)
        {
            if (div.arithmeticGivingPhrase() is { } giving)
            {
                Format2SendingOperand("DIVIDE", "§14.9.12.2", into.receiversOrSendingOperand().receivingArithmeticOperand());
                var dividendX = host.Expr.BindExpr(into.receiversOrSendingOperand());
                var recv = host.Expr.Receivers(giving.receivingArithmeticOperand(), editedOk: true, "§14.9.12.3 SR2");
                ctx.Validation.CheckComposite("DIVIDE", [dividendX, a], recv);
                return new BoundDivideGiving(dividendX, a, recv, sizeErr);
            }
            if (!Format1Receivers("DIVIDE", "§14.9.12.2", [into.receiversOrSendingOperand()]))
                return BoundRejected.Reported(ctx.Edition);   // a receiver-less BoundDivideInto crashed the emitter (targets.Max on empty)
            var intoRecv = host.Expr.Receivers(into.receiversOrSendingOperand().receivingArithmeticOperand(), editedOk: false, "§14.9.12.3 SR1");
            ctx.Validation.CheckComposite("DIVIDE", [a], intoRecv);
            return new BoundDivideInto(a, intoRecv, sizeErr);   // target ← target ÷ a
        }
        if (div.divideByPhrase() is { } byPhrase && div.arithmeticGivingPhrase() is { } gv)
        {
            var divisorX = host.Expr.BindExpr(byPhrase.sendingOperand());
            var recv = host.Expr.Receivers(gv.receivingArithmeticOperand(), editedOk: true, "§14.9.12.3 SR2");
            ctx.Validation.CheckComposite("DIVIDE", [a, divisorX], recv);
            return new BoundDivideGiving(a, divisorX, recv, sizeErr);
        }
        if (div.divideByPhrase() is not null)
        {
            // §14.9.12.2: every BY format (3–5) prints GIVING — `DIVIDE A BY B.` exists in no format.
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.ArithmeticFormatOperand,
                "DIVIDE … BY without GIVING: every BY format of DIVIDE prints the GIVING phrase "
                + "(ISO §14.9.12.2 Formats 3–5)");
        }
        return DivideShape();
    }

    /// <summary>The ONE refusal for a DIVIDE the grammar parsed but no DIVIDE format prints (kb/Work PB909) —
    /// both of the union parse's defensive arms, so they cannot drift apart.</summary>
    private BoundRejected DivideShape() => BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "DIVIDE: every DIVIDE format prints "
        + "identifier-1 or literal-1 followed by an INTO or a BY phrase (ISO §14.9.12.2)");

    /// <summary>§14.9.2.2 / §14.9.44.2 / §14.9.26.2 / §14.9.12.2's ONE Format-2 discipline (kb/Work PB134):
    /// the TO/FROM/BY/INTO operand of a GIVING form is ONE `{identifier | literal}` sending operand with no
    /// ROUNDED — the repetition and the rounding belong to Format 1's receiver role. The grammar parses the
    /// union; this narrows it back (the old binders silently dropped the extra operands and the ROUNDED).</summary>
    private void Format2SendingOperand(string verb, string cite, Core.ReceivingArithmeticOperandContext[] recvShaped)
    {
        if (recvShaped.Length > 1 || (recvShaped.Length == 1 && recvShaped[0].roundedPhrase() is not null))
            ctx.Edition.Error(DiagnosticCatalog.ArithmeticFormatOperand,
                $"{verb} … GIVING: Format 2 prints ONE sending operand with no ROUNDED (ISO {cite}); the "
                + "extra operands / the ROUNDED the old binder silently dropped are a format violation");
    }

    /// <summary>The Format-1 half of the same discipline: without GIVING, the TO/FROM/BY/INTO operands are
    /// RECEIVERS — Format 1 prints <c>{identifier-2 [rounded-phrase]}</c> only — so a literal, a
    /// function-identifier (§8.4.3.2.3 SR1) or an inline method invocation (§8.4.3.4.3 SR1) there is illegal.
    /// ALL FOUR VERBS ask the ONE <see cref="ArithmeticOperandRole"/> classification (kb/Work PB1142): the
    /// hand-enumerated per-verb lists this replaced missed the inline-invocation arm, which dropped the operand
    /// silently (ADD / SUBTRACT / MULTIPLY) or crashed the emitter on an empty receiver list (DIVIDE). Reports
    /// every offending operand, so a MULTIPLY with two bad BY operands names both. False = rejected.</summary>
    private bool Format1Receivers(string verb, string cite, IEnumerable<Antlr4.Runtime.ParserRuleContext> mixedOperands)
    {
        bool ok = true;
        foreach (var mixed in mixedOperands)
            if (ArithmeticOperandRole.FirstNonReceiver(mixed) is { } bad)
            {
                ctx.Edition.Error(DiagnosticCatalog.ArithmeticFormatOperand,
                    $"{verb} without GIVING: Format 1 prints receiving identifiers only (ISO {cite}), and "
                    + $"'{bad.GetText()}' is {ArithmeticOperandRole.Describe(bad)} — a sending operand belongs to "
                    + "the GIVING form");
                ok = false;
            }
        return ok;
    }

    public BoundStatement BindCompute(Core.ComputeStatementContext compute)
    {
        // COMPUTE Format 2 — boolean-compute (ISO §14.9.8). The .g4 alternative is predicate-free; the
        // 2002 introduction gate lives in VersionConformancePass.VisitComputeStatement (GateBooleanOperators
        // — kb/Work PB157 corrected the stale {is2002()}? claim here).
        if (compute.booleanExpression() is { } boolExpr) return BindComputeBoolean(compute, boolExpr);
        if (compute.arithmeticExpression() is not { } expr)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "COMPUTE with no expression after '=': both COMPUTE formats print an "
                + "arithmetic or a boolean expression (ISO §14.9.8.2)");
        // F1 → F2 re-route: `COMPUTE bool-item = bool-item` parses as Format 1 (a sole-identifier RHS predicts
        // the arithmetic alt), so a boolean receiver or a sole boolean-category RHS re-routes to the boolean
        // bind (the "ANTLR alternative-order reality" precedent). A boolean RHS/receiver never reaches the
        // numeric channel.
        // OperandPic, BOTH probes, over EVERY receiver (kb/Work PB157): a GROUP-USAGE BIT receiver has Pic
        // null but IS an elementary boolean for these rules (§13.18.29.4 GR1b — OperandPic carries the as-if
        // PICTURE 1(m)), and probing only computeStore(0) let `COMPUTE N B = 1` miss the boolean receiver
        // (the reroute then judged it a pure Format 1).
        bool receiverBoolean = compute.computeStore()
            .Any(s => ctx.Refs.Probe(s.dataReference()) is { Item.OperandPic.Category: PicCategory.Boolean });
        bool rhsBoolean = ConditionBinder.SoleDataRef(expr) is { } d && ctx.Refs.Probe(d) is { Item.OperandPic.Category: PicCategory.Boolean };
        if (receiverBoolean || rhsBoolean)
        {
            BoundBoolExpr rerouted;
            var sole = ConditionBinder.SoleDataRef(expr) is { } sd && ctx.Refs.Probe(sd) is not null
                ? host.Expr.ResolveSending(sd) : null;
            if (sole is { Place: null })
                rerouted = sole.BoolError(ctx.Edition);   // the resolver's own answer (kb/Work PB1030)
            else if (sole?.Place is { } sp
                && (sp is RefModPlace rm2 ? rm2.Category : sp.Item.OperandPic?.Category) is PicCategory.Boolean)
                rerouted = new BoundBoolRef(sp);
            else
            {
                // The PB68 sweep's sixth site (kb/Work PB157): this arm built the error NODE without the
                // DIAGNOSTIC, so `COMPUTE B = N + 1` compiled clean and threw at run time.
                ctx.Edition.Error("COBOLNET1511", $"a boolean COMPUTE receiver takes a boolean expression, "
                    + $"not '{expr.GetText()}' (ISO §14.9.8 Format 2 / §8.8.2)");
                rerouted = BoundBoolError.Refused(ctx.Edition, $"COMPUTE boolean receiver takes a boolean expression, not '{expr.GetText()}' "
                    + "(ISO §14.9.8 Format 2)");
            }
            return BuildComputeBoolean(compute, rerouted);
        }
        var rhs = host.Expr.BindExpr(expr);
        return new BoundCompute(rhs, host.Expr.Receivers(compute.computeStore()), host.BindSizeError(compute.computeOnSizeError()));
    }

    /// <summary>The <c>booleanExpression</c> parse tree reduced to a sole figurative ZERO (<c>[ALL]
    /// ZERO</c>/<c>ZEROS</c>/<c>ZEROES</c> — ALL is Format 1's OPTIONAL word, §8.3.3.6.2; kb/Work PB157
    /// widened this from the bare form), or null for anything else — including <c>ALL B"…"</c> (Format 6,
    /// the actual ALL literal). Used by the F2→F1 reroute; the SR3 screen now reads
    /// <c>BoundBoolAll.IsAllLiteral</c> instead (one flag, set at the one Format-6 construction site).</summary>
    private static Core.FigurativeConstantContext? SoleFigurativeZero(Core.BooleanExpressionContext? b)
    {
        if (b?.booleanXorTerm() is not [{ } xor]) return null;
        if (xor.booleanAndTerm() is not [{ } and]) return null;
        if (and.booleanShiftTerm() is not [{ } shift]) return null;
        if (shift.booleanShiftSuffix().Length != 0) return null;
        if (shift.booleanFactor() is not { } factor) return null;
        var fig = factor.valueOperand()?.nonNumericLiteral()?.figurativeConstant();
        return fig is not null && fig.zeroWord() is not null ? fig : null;   // [ALL] ZERO — ALL is optional (PB157)
    }

    private BoundStatement BindComputeBoolean(Core.ComputeStatementContext compute, Core.BooleanExpressionContext boolExpr)
    {
        // ⛔ F2 → F1 RE-ROUTE, THE MIRROR OF THE ONE ABOVE (fix-queue PB51). `COMPUTE <numeric> = ZERO` parsed
        // as Format 2 and was REJECTED as "a boolean COMPUTE expression shall not consist solely of an ALL
        // literal" — on a statement §8.8.1.1 makes legal arithmetic ("An arithmetic expression may be … the
        // figurative constant ZERO"), with a diagnostic naming a construct the source does not contain.
        // WHY IT LANDS HERE: a bare `ZERO` is not adjacent to an operator or paren, so ZeroTokenRewriter leaves
        // it figurative and F1's `arithmeticExpression` cannot match it — the parser takes F2, whose
        // `valueOperand` leaf admits the figurative. `BindBoolExpr` then normalises it to `BoundBoolAll("0")`
        // (§8.3.3.6.4 GR4's boolean reading), indistinguishable from `ALL B"0"` by the time SR3 runs.
        // ⚠ THE TEST IS ON THE PARSE TREE, NOT THE BOUND NODE, precisely because that normalisation is lossy:
        // `SoleFigurativeZero` asks whether the SOURCE wrote a bare ZERO/ZEROS/ZEROES, so `ALL B"0"` and
        // `ALL ZERO` keep their existing verdicts (§8.3.3.6.3 SR1a — "the only figurative constant permitted is
        // ZERO … WITHOUT the ALL phrase").
        // ⚠ AND IT IS GATED ON A NON-BOOLEAN RECEIVER: with a boolean receiver GR4's boolean reading is the right
        // one and Format 2 genuinely applies, so that path is untouched.
        // ⚙ MEASURED SCOPE: every OTHER arithmetic position already accepts a bare ZERO — ADD/SUBTRACT/MULTIPLY,
        // the GIVING forms, IF and MOVE all have their own operand rules that admit the figurative. COMPUTE is
        // the one verb whose RHS is a bare `arithmeticExpression`, which is why this is a targeted re-route and
        // not the grammar re-architecture the queue entry proposed.
        if (SoleFigurativeZero(boolExpr) is not null
            && compute.computeStore().Length > 0
            // OperandPic (kb/Work PB157): a GROUP-USAGE BIT receiver is boolean (§13.18.29.4 GR1b) — raw Pic
            // read it as non-boolean and misrouted `COMPUTE <bit-group> = ZERO` to the arithmetic channel.
            && ctx.Refs.Probe(compute.computeStore(0).dataReference()) is not { Item.OperandPic.Category: PicCategory.Boolean })
        {
            // §8.3.3.6.4 GR4 — "the numeric value '0' … depending on context"; a numeric receiver IS that context.
            return new BoundCompute(new BoundNumLiteral("0"), host.Expr.Receivers(compute.computeStore()),
                host.BindSizeError(compute.computeOnSizeError()));
        }

        // The COBOL-2002 boolean-operator introduction gate on COMPUTE Format 2 (BooleanOperators2002) fires on
        // RECOGNITION in the VersionConformancePass parse-arm (VisitComputeStatement, HasBoolOp on the F2
        // booleanExpression); Step 14h.4b.
        var rhs = host.Cond.BindBoolExpr(boolExpr);
        // SR3 (§14.9.8 :26575): the expression shall not consist solely of THE FIGURATIVE CONSTANT ALL
        // LITERAL — Format 6, the only construction that sets BoundBoolAll.IsAllLiteral. Figurative ZERO —
        // bare or `ALL ZERO`, whose ALL is Format 1's OPTIONAL word (§8.3.3.6.2) — is a DISJOINT §8.8.2
        // operand alternative and legal here, as is a B-NOT fold (the expression is then not "solely" the
        // literal). The old bound-node test conflated all three with the ALL literal and rejected them
        // (kb/Work PB157 — the PB51 reading, decided by the spec; §8.3.3.6.3 SR2 excludes figuratives from
        // Format 6's literal-1).
        if (rhs is BoundBoolAll { IsAllLiteral: true })
            ctx.Edition.Error("COBOLNET1511", "a boolean COMPUTE expression shall not consist solely of an ALL "
                + "literal (ISO §14.9.8.3 Format 2 SR3)");
        return BuildComputeBoolean(compute, rhs);
    }

    /// <summary>Shared tail for both the direct Format-2 bind and the F1→F2 re-route: receiver conformance
    /// (SR2 — elementary boolean), the ROUNDED / SIZE-ERROR prohibition (F2 has neither), the GR3 store width.</summary>
    private BoundStatement BuildComputeBoolean(Core.ComputeStatementContext compute, BoundBoolExpr rhs)
    {
        if (compute.computeOnSizeError() is not null)
            ctx.Edition.Error("COBOLNET1511", "ON SIZE ERROR may not be specified on a boolean COMPUTE "
                + "(ISO §14.9.8 Format 2 — no size-error phrase)");
        var targets = new List<Place>();
        foreach (var store in compute.computeStore())
        {
            if (store.roundedPhrase() is not null)
                ctx.Edition.Error("COBOLNET1511", "ROUNDED may not be specified on a boolean COMPUTE "
                    + "(ISO §14.9.8 Format 2)");
            // kb/Work PB128: the Format-2 boolean receivers ride the ONE receiving chokepoint (the direct
            // Refs.Resolve bypass skipped the CONSTANT RECORD / CAPACITY / constant-name screens); the
            // ARITHMETIC category screen deliberately does not apply — §14.9.8.3 SR2's boolean-receiver rule
            // is BuildComputeBoolean's own (kb/Work PB157).
            if (host.Expr.ResolveReceiving(store.dataReference()) is not { } p)
            {
                ctx.Edition.Error("COBOLNET1511", $"COMPUTE receiver '{store.dataReference().GetText()}' is unresolvable");
                continue;
            }
            // OperandPic — THE ONE category reader (D20/PB79; kb/Work PB157): a GROUP-USAGE BIT receiver IS
            // an elementary boolean for SR2 (§13.18.29.4 GR1b's as-if PICTURE 1(m)); raw Pic rejected it
            // while the SENDING side of the same statement accepted it.
            // (kb/Work PB173 LANDED: the PB157 containment that staged a reference-modified BIT-GROUP receiver
            // loud is DELETED, together with DiagnosticCatalog.RefModBitGroupSlice. Its premise — "the boolean
            // channel sizes in BOOLEAN positions while the group ref-mod substrate slices the PACKED BYTE
            // image" — was a true description of a SUBSTRATE defect, and the substrate is now a BitImagePlace
            // over the unpacked boolean string (§8.4.3.3.4 GR5a). The units agree, so the receiver is an
            // ordinary boolean one and needs no exclusion.)
            var cat = p is RefModPlace rm ? rm.Category : p.Item.OperandPic?.Category;
            if (cat is not PicCategory.Boolean)
                ctx.Edition.Error("COBOLNET1511", $"the receiver '{store.dataReference().GetText()}' of a boolean "
                    + "COMPUTE shall be an elementary boolean item (ISO §14.9.8.3 Format 2 SR2)");
            targets.Add(p);
        }
        return new BoundComputeBoolean(rhs, targets);
    }
}
