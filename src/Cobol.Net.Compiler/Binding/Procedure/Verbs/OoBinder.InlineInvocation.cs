// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Compiler.Oo;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>
/// ⛔ <b>ONE INVOCATION ARGUMENT, DECOUPLED FROM THE SYNTAX THAT WROTE IT.</b> ISO §14.9.23.2's USING phrase
/// and §8.4.3.4.2's parenthesised list are two spellings of one operand: §8.4.3.4.4 GR1 makes the inline
/// form's arguments "the operands specified within parentheses" of the equivalent
/// <c>INVOKE … USING arguments</c>, so both must reach the same §14.8 conformance rule with the same
/// effective passing mode. This record is what lets <c>OoBindInvocationArg</c> be ONE body with two thin
/// front-ends instead of two copies of §14.8.2.3.2/§14.8.2.3.3 (feedback_one_rule_one_place).
/// </summary>
/// <param name="ByValueWritten">The BY VALUE phrase was written (INVOKE only — §14.9.23.2).</param>
/// <param name="ByReferenceWritten">The BY REFERENCE phrase was written (INVOKE only).</param>
/// <param name="ByContentWritten">The BY CONTENT phrase was written (INVOKE only).</param>
/// <param name="Omitted">The OMITTED operand (§8.4.3.4.2's brace; §14.9.23.2's BY REFERENCE branch).</param>
/// <param name="Expression">The operand is an EXPRESSION rather than an identifier or a literal — it has no
/// storage, so §14.9.23.3 SR9 cannot make it BY REFERENCE. True only for the inline form's bare arguments,
/// where no passing phrase exists to say so; an INVOKE spells the mode itself.</param>
/// <param name="Address">The operand is an ADDRESS-IDENTIFIER (§8.4.3.1.2 identifier Format 9) — §14.9.23.3 SR9
/// names it for identifier-3 and SR19 makes it a SENDING operand (kb/Work PB1021).</param>
/// <param name="Self">The operand is the predefined object reference SELF (§8.4.3.8; kb/Work PB1137) — identifier-5
/// of the INVOKE statement, identifier-2 of the inline form (kb/Work PB1425) — never a receiving operand.</param>
internal readonly record struct InvocationArg(
    bool ByValueWritten,
    bool ByReferenceWritten,
    bool ByContentWritten,
    bool Omitted,
    bool Expression,
    Core.BooleanExpressionContext? Bool,
    Core.ArithmeticExpressionContext? Arith,
    Core.LiteralContext? Literal,
    Core.DataReferenceContext? Ref,
    Core.AddressIdentifierContext? Address = null,
    bool Self = false)
{
    /// <summary>The INVOKE statement's <c>invokeArgument</c> reading (§14.9.23.2). A KEYWORD-LESS expression
    /// (kb/Work PB1135) has no BY phrase to say it is BY CONTENT, so — like the inline form's bare argument —
    /// it is marked <c>Expression</c>: §14.9.23.3 SR9 cannot make it BY REFERENCE and §14.9.23.4 GR6 a) 2 implies
    /// BY CONTENT. Written with a phrase, the phrase says so and <c>Expression</c> stays false.</summary>
    public static InvocationArg OfInvokeArgument(Core.InvokeArgumentContext a)
    {
        bool keywordLess = a.VALUE() is null && a.REFERENCE() is null && a.CONTENT() is null;
        // ⛔ A KEYWORD-LESS IDENTIFIER ARRIVES INSIDE AN EXPRESSION NODE (kb/Work PB1135, decision R59): the grammar has no
        // bare `dataReference` arm, so that `USING N + 1` is one expression — the sole-identifier reduction (ISO
        // §14.9.4.4 GR8's INVOKE twin) happens HERE, once, for every consumer of the argument (the typed path and the
        // universal path alike), and it is what `Ref` has always meant.
        var reference = a.dataReference() ?? (keywordLess ? ConditionBinder.SoleDataReference(a.arithmeticExpression()) : null);
        // SELF is identifier-5 under BY CONTENT, BY VALUE or no phrase (kb/Work PB1137), and it arrives as the sole
        // primary of the expression arm (the identifier tier, kb/Work PB1425) — recovered here, once, like `Ref`.
        bool self = IsSoleSelf(a.arithmeticExpression());
        return new(
            a.VALUE() is not null, a.REFERENCE() is not null, a.CONTENT() is not null, Omitted: a.OMITTED() is not null,
            Expression: keywordLess && reference is null && a.literal() is null && a.OMITTED() is null
                && a.addressIdentifier() is null && !self,
            a.booleanExpression(), a.arithmeticExpression(), a.literal(), reference,
            a.addressIdentifier(), self);
    }

    /// <summary>The argument is SELF and nothing else (§8.4.3.8). SUPER is not: §8.4.3.8.3 SR3 refuses it as an
    /// argument, and that refusal is the OO identifier door's (<c>OoBinder.OoBindOoIdentifier</c>).</summary>
    private static bool IsSoleSelf(Core.ArithmeticExpressionContext? arith) =>
        ConditionBinder.SolePredefinedObject(arith)?.SELF() is not null;

    /// <summary>The inline form's <c>argument</c> reading (§8.4.3.4.2). No passing phrase exists in that
    /// general format, so the mode is §14.9.23.4 GR6's default — exactly what a bare INVOKE argument takes.
    /// <c>Expression</c> is set for the arithmetic / boolean arms because the sole-reference and sole-literal
    /// reductions (<c>OoBindInvocationArg</c>) are what recover an identifier-2 or literal-2 that parsed into
    /// one; what survives as an expression genuinely has no storage.</summary>
    public static InvocationArg OfInlineArgument(Core.ArgumentContext a) => new(
        ByValueWritten: false, ByReferenceWritten: false, ByContentWritten: false,
        Omitted: a.OMITTED() is not null,
        Expression: a.literal() is null && a.OMITTED() is null && a.addressIdentifier() is null
            && !IsSoleSelf(a.arithmeticExpression()),
        a.booleanExpression(), a.arithmeticExpression(), a.literal(), Ref: null, a.addressIdentifier(),
        IsSoleSelf(a.arithmeticExpression()));
}

/// <summary>
/// ⛔ <b>AN INVOCATION SITE</b> — everything the §14.9.23 binder reads off a statement, abstracted so the
/// §8.4.3.4 INLINE form reaches the SAME receiver resolution, the SAME roster selection and the SAME §14.8
/// conformance. §8.4.3.4.4 GR1 does not merely resemble INVOKE, it DEFINES the inline form as one of four
/// INVOKE statements, and §8.4.3.4.3 SR3 requires that statement to be valid under §14.9.23's own syntax
/// rules — so a second activation path would be a second answer to one rule.
/// </summary>
internal sealed class InvocationSite
{
    /// <summary>The argument operands in written order.</summary>
    public required IReadOnlyList<InvocationArg> Args { get; init; }

    /// <summary>Whether an argument PHRASE was written at all (INVOKE's USING, or the inline form's
    /// parenthesised list). Distinct from an empty <see cref="Args"/>: the standard class BASE's methods (ISO §16.2
    /// — no USING phrase in either header) reject the phrase, not merely a non-empty one.</summary>
    public required bool ArgsWritten { get; init; }

    /// <summary>INVOKE's written RETURNING identifier; null for the inline form.</summary>
    public Core.DataReferenceContext? ReturningRef { get; init; }

    /// <summary>The inline form: "a returning item is implicitly specified in the activating element when a
    /// function or inline method invocation is referenced" (ISO §14.8.3.1), so the activation delivers into
    /// §8.4.3.4.4 GR1 c) temporary rather than into a written identifier.</summary>
    public bool ReturningImplicit { get; init; }

    /// <summary>How the site names itself in a diagnostic — <c>INVOKE</c> or
    /// <c>the inline method invocation of</c>.</summary>
    public required string Verb { get; init; }

    /// <summary>Set by the resolved-invocation tail when <see cref="ReturningImplicit"/> holds: the
    /// §8.4.3.4.4 GR1 b)/c) temporary the activation delivers into, which the inline expression then reads.
    /// </summary>
    public Place? ImplicitReturningPlace { get; set; }

    /// <summary>The INVOKE statement's own reading of the site (§14.9.23.2).</summary>
    public static InvocationSite OfInvokeStatement(Core.InvokeStatementContext inv)
    {
        var raw = inv.invokeUsing()?.invokeArgument() ?? [];
        var args = new InvocationArg[raw.Length];
        for (int i = 0; i < raw.Length; i++) args[i] = InvocationArg.OfInvokeArgument(raw[i]);
        return new InvocationSite
        {
            Args = args,
            ArgsWritten = inv.invokeUsing() is not null,
            ReturningRef = inv.invokeReturning()?.dataReference(),
            Verb = "INVOKE",
        };
    }

    /// <summary>One <c>:: literal-1 [ ( arguments ) ]</c> segment of an inline method invocation
    /// (§8.4.3.4.2).</summary>
    public static InvocationSite OfInlineSegment(Core.InlineInvocationSegmentContext seg)
    {
        var raw = seg.argumentList()?.argument() ?? [];
        var args = new InvocationArg[raw.Length];
        for (int i = 0; i < raw.Length; i++) args[i] = InvocationArg.OfInlineArgument(raw[i]);
        return new InvocationSite
        {
            Args = args,
            // GROUPING-PAREN-ONLY: this '(' can never be the FNARG_LPAREN twin, and that is a property of the
            // lexer rather than an assumption. `OnDefaultLParen` retypes a '(' to FNARG_LPAREN only when
            // `PreviousIsFunctionName()` holds — the token two back is `FUNCTION` (ISO §8.4.3.2.3 SR6). Here
            // the two tokens before the paren are always `COLONCOLON literal` (the §8.4.3.4.2 general format
            // admits nothing else between the operator and the argument list), so the predicate is
            // structurally false and only the plain token can appear. Handling FNARG_LPAREN as well would be a
            // second reading of SR6 at a site SR6 does not reach.
            ArgsWritten = seg.LPAREN() is not null,
            ReturningImplicit = true,
            Verb = "the inline method invocation of",
        };
    }
}

/// <summary>
/// ⛔ <b>THE §8.4.3.1.2 FORMAT 4 IDENTIFIER</b> (kb/Work PB428) — <c>{object-class-name-1 | identifier-1} ::
/// literal-1 [ ( arguments ) ]</c>, §8.4.3.4.
///
/// <para><b>What it binds to, and why nothing new was invented.</b> §8.4.3.4.4 GR1 writes the semantics out as
/// an INVOKE statement — "An inline method invocation references a temporary data item with the same class,
/// category, and content as the temp-identifier that would be returned from the execution of the applicable
/// form of INVOKE statement" — so this binder does exactly two things the INVOKE binder does not: it
/// SYNTHESIZES GR1 c)'s temporary from the method's own RETURNING item (GR1 b): "temp-identifier has the same
/// description, class, and category as the RETURNING parameter in the specification of the method identified
/// by literal-1"), and it registers the activation as a statement-scoped PRE-OP. Receiver resolution, roster
/// selection, arity, §14.8 argument conformance and the emitted <c>BoundInvoke</c> are the INVOKE binder's,
/// reached through <c>OoBindByReceiver</c> / <c>OoBindResolvedInvoke</c>.</para>
///
/// <para><b>The hoist is the EXISTING one</b> — <c>DataBinder.PendingPreOps</c>, drained by
/// <c>UdfBinder.UdfWrapCalls</c> at the <c>StatementBinder.BindStatement</c> chokepoint and attached
/// per-evaluation by <c>UdfAttachPerEvaluation</c> inside a conditionally-evaluated condition window. That
/// list is documented as the ONE statement-scoped pre-op list precisely so a third client registers rather
/// than grows a fourth ordering rule, and its two existing clients (a user-function activation, a
/// function-bearing subscript's §15.4 temp store) share this one's hoist rules exactly: §8.4.3.4.3 SR1
/// ("Inline method invocation shall not be specified as a receiving operand") is the same non-receiving
/// property §8.4.3.2.3 SR1 gives a function-identifier, so there is no store polarity to classify and there
/// are no post-ops. Registration order gives §8.4.3.1.4 GR1's left-to-right component order for free: a
/// nested invocation registers while its consumer's arguments bind.</para>
///
/// <para><b>Where SR1 is enforced.</b> Format 4 was added to exactly the operand rules that already admit
/// <c>functionCall</c> (§8.4.3.1.2 Format 1), whose exclusion §8.4.3.2.3 SR1 words identically, and no purely
/// RECEIVING rule admits either — <c>InlineMethodInvocationOperandDriftTests</c> keeps that true of the .g4. But
/// four of those rules are MIXED-ROLE: the arithmetic TO / FROM / BY / INTO operand is a receiver in Format 1 and
/// a sender in Format 2 (kb/Work PB134), so there the rule is a BIND-time check — the ONE
/// <c>ArithmeticOperandRole</c> classification every arithmetic verb's Format-1 screen asks (kb/Work PB1142;
/// pinned by <c>ArithmeticOperandRoleDriftTests</c>). This binder still never has to re-derive it.</para>
/// </summary>
internal sealed partial class OoBinder
{
    /// <summary>Bind one inline method invocation and return the expression that READS its result — a
    /// <see cref="BoundNumRef"/> over the §8.4.3.4.4 GR1 c) temporary, the same carrier a user-defined
    /// function's result takes, so every general-operand chokepoint
    /// (<c>IntrinsicBinder.OperandOf</c> → <see cref="BoundFieldOperand"/>) already serves it.</summary>
    public BoundExpr OoBindInlineInvocation(Core.InlineMethodInvocationContext imi)
    {
        // The INTRODUCTION gate fires on RECOGNITION in the VersionConformancePass parse arm
        // (VisitInlineMethodInvocation → Check(InlineMethodInvocation2002)), never here: a below-2002 inline
        // invocation is an edition violation independent of whether its method resolves.
        var target = imi.objectReferenceAtom();
        // The plain term; null for an object-view receiver (§8.4.3.1.4 GR1 c) applies the view first), which is bound
        // below as a computed receiver — its SR2 screens are the view's own (§8.4.3.5.3), asked of its description.
        var term = target.objectReferenceTerm();

        // §8.4.3.4.3 SR2, screened BEFORE resolution because both rejected shapes are RECEIVER shapes and the
        // general format admits them syntactically (the P3 superset parse: `objectReferenceAtom` is INVOKE's own
        // receiver rule — see CobolOO.g4).
        if (term?.predefinedNull() is not null)
        {
            ctx.Edition.Error(DiagnosticCatalog.InlineInvocationReceiver,
                "an inline method invocation's identifier-1 shall be of class object; the predefined object "
                + "reference NULL shall not be specified (ISO §8.4.3.4.3 SR2)");
            return BoundExprError.Refused(ctx.Edition, "inline method invocation through NULL");
        }
        if (term?.dataReference() is { } dr0 && ctx.Refs.Probe(dr0) is not null
            && host.Expr.ResolveSending(dr0).Place is { } r0   // a non-place answer is bound (and answered) below
            && RefuseUniversalInlineReceiver(r0, dr0.GetText()) is { } refusedFirst)
            return refusedFirst;

        // §8.4.3.1.3 SR1 recursion: each segment invokes on the temporary the previous one produced. A
        // function-identifier identifier-1 (`FUNCTION FOBJ (1) :: "M"`, kb/Work PB1425) is bound once, here, and its
        // temporary is the first segment's receiver exactly as a chained segment's is — so SR2's universal half below
        // asks it too, and it never reaches the receiver dispatch a second time.
        Place? chained = null;
        if (OoBindComputedAtom(target) is { } computed
            && (chained = OoObjectReferenceTemporary(computed, target, DiagnosticCatalog.InlineInvocationReceiver.Code,
                "an inline method invocation's identifier-1 shall be of class object (ISO §8.4.3.4.3 SR2)")) is null)
            return BoundExprError.Refused(ctx.Edition, "inline method invocation through a non-object identifier-1");
        // The receiver AS WRITTEN (spaces kept), which every diagnostic about a chained receiver names — never the
        // compiler's temporary.
        string written = DataBinder.WrittenText(target);
        foreach (var seg in imi.inlineInvocationSegment())
        {
            // §8.4.3.4.3 SR2 binds EVERY segment's identifier-1, not only the written first one: identifier is
            // defined recursively (§8.4.3.1.3 SR1), so `A1 :: "UNIV"` IS identifier-1 of `… :: "GETNAME"`, and a
            // universal temp there is the same excluded receiver (kb/Work PB1429 — it fell into the universal
            // dynamic path, which never sets an implicit returning place, and ended in COBOLNET2362).
            if (chained is { } recv && RefuseUniversalInlineReceiver(recv, written) is { } refusedChained)
                return refusedChained;
            string receiverText = written;
            written += " " + DataBinder.WrittenText(seg);
            if (OoMethodNameOf(seg.literal()) is not { Length: > 0 } methodName)
            {
                ctx.Edition.Error(DiagnosticCatalog.InlineInvocationReceiver,
                    $"the inline method invocation '{seg.GetText()}': literal-1 (the method name) shall be a "
                    + "non-zero-length literal of class alphanumeric or national (ISO §8.4.3.4.2; "
                    + "§8.4.3.4.3 SR3 → §14.9.23.3 SR2)");
                return BoundExprError.Refused(ctx.Edition, "inline method invocation method name");
            }
            var site = InvocationSite.OfInlineSegment(seg);
            var activation = chained is { } prev
                ? OoBindInstanceInvoke(site, prev, methodName, receiverText)
                : OoBindByReceiver(site, term!, methodName);
            if (site.ImplicitReturningPlace is not { } result)
                // Every failure path already reported (the receiver, roster, arity and conformance
                // diagnostics are the INVOKE binder's own, re-worded by InvocationSite.Verb).
                return BoundExprError.Refused(ctx.Edition, $"inline method invocation of \"{methodName}\"");
            ctx.Data.PendingPreOps.Add(activation);
            chained = result;
        }
        if (chained is not { } place) return BoundExprError.Refused(ctx.Edition, "inline method invocation");

        // §8.4.3.1.4 GR1 g): a reference modifier applies to the identifier on the left — and the identifier
        // on the left of the tail IS the invocation, whose value is the temporary. The ONE ref-mod reading
        // (ReferenceResolver) is asked for it, so §8.4.3.3.3's rules screen the temp exactly as they screen
        // any other data item.
        foreach (var rm in imi.refModPart())
        {
            if (ctx.Refs.RefModOf(place, rm, imi.GetText()) is not { } modified)
                return BoundExprError.Refused(ctx.Edition, "reference-modified inline method invocation");
            place = modified;
        }
        return new BoundNumRef(place);
    }

    /// <summary>The universal half of §8.4.3.4.3 SR2 — "Identifier-1 shall be of class object; neither the predefined
    /// object reference NULL nor a universal object reference shall be specified" — asked of ONE receiver, the
    /// written first one or a chained segment's temporary alike (kb/Work PB1429), so the two can never disagree.
    /// Null when the receiver is not a universal object reference; otherwise the reported refusal.</summary>
    private BoundExpr? RefuseUniversalInlineReceiver(Place receiver, string written)
    {
        if (receiver.Item.Pic is not { Category: PicCategory.ObjectReference } pic
            || !(pic.ObjectRef ?? ObjectRefDescriptor.Universal).IsUniversal)
            return null;
        ctx.Edition.Error(DiagnosticCatalog.InlineInvocationReceiver,
            $"the inline method invocation through '{written}': a UNIVERSAL object reference shall "
            + "not be specified as identifier-1 (ISO §8.4.3.4.3 SR2) — use an INVOKE statement, whose "
            + "§14.9.23.4 GR7c dynamic path carries the runtime conformance check");
        return BoundExprError.Refused(ctx.Edition, "inline method invocation through a universal object reference");
    }

    /// <summary>The operand form of <see cref="OoBindInlineInvocation"/> — the MOVE/DISPLAY/INSPECT/FROM
    /// chokepoints take a <see cref="BoundOperand"/>, and <c>IntrinsicBinder.OperandOf</c> is the ONE
    /// expression→operand mapping (a <see cref="BoundNumRef"/> over the temp becomes a
    /// <see cref="BoundFieldOperand"/>, carrying the cloned category into Table-16 legality, the relation
    /// class dispatch and the LENGTH fold).</summary>
    public BoundOperand OoInlineInvocationOperand(Core.InlineMethodInvocationContext imi) =>
        IntrinsicBinder.OperandOf(OoBindInlineInvocation(imi));
}
