// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

/// <summary>How one activation names itself in the argument-conformance diagnostics, and which descriptors and
/// importing rules it reports under. The RULES are shared (<see cref="ParameterConformance"/>); only the syntax
/// rule that imports them differs between the activating statements.</summary>
/// <param name="Callee">The activation as the message names it — <c>CALL … AS NESTED</c>,
/// <c>CALL … program-prototype 'P'</c>, <c>FUNCTION F</c>.</param>
/// <param name="ImportingRule">The syntax rule that makes §14.8.2 apply to this activation — §14.9.4.3 SR25 for a
/// Format-2 CALL, §8.4.3.2.3 SR13 for a function-identifier.</param>
/// <param name="Conformance">The descriptor a §14.8.2 non-conformance reports under.</param>
internal sealed record ActivationSite(string Callee, string ImportingRule, DiagnosticDescriptor Conformance);

/// <summary>
/// ⛔ THE ONE ARGUMENT HALF OF ISO §14.8.2, PARAMETERS — for every activation whose activated element's formal
/// parameters are known when the activating statement binds (kb/Work PB1418 / PB1115). Two syntax rules import the
/// same regime: §14.9.4.3 SR25 into a Format-2 CALL (the AS NESTED containment table or a program prototype's
/// §12.3.8.4 GR10 a) definition), and §8.4.3.2.3 SR13 into a function-identifier that names a function prototype
/// or a function pointer. §14.8.2.3.2 rule 2 then lists "a program for which there is a program-specifier …", "a
/// program and the NESTED phrase …", "a method" and "a function" side by side — ONE rule over four activated
/// elements.
/// <para>⛔ WHY IT IS A COLLABORATOR OF ITS OWN. The CALL binder carried this regime privately, and the
/// user-defined-function binder (<see cref="UdfBinder"/>) re-implemented CALL's argument binding without it: a
/// <c>PIC X(4)</c> identifier reached a <c>PIC 9(4)</c> BY REFERENCE function formal and aliased it, a
/// <c>PIC X(4)</c> argument reached a <c>PIC X(2)</c> formal, a misaligned bit item crossed BY REFERENCE, and an
/// object reference crossed into a formal of a different object-reference description — each one a diagnostic on
/// the CALL side and a silent run on the function side. The rule lives here so the two activations cannot drift
/// again; <c>ActivationConformanceDriftTests</c> pins that both lanes ask it. INVOKE's third lane is
/// <c>OoBinder</c>'s, over the SAME comparators in <see cref="CobolNet.Compiler.Oo.OoConformance"/>, and it asks this
/// type for everything its statement words like CALL's (kb/Work PB1137): the storage-section rule and the keyword-less
/// mode test built on it (<see cref="ScreenSection"/>, <see cref="MeetsByReferenceRules"/> — §14.9.23.3 SR9/SR11 and
/// §14.9.23.4 GR6 a), CALL's §14.9.4.3 SR3/SR7 and §14.9.4.4 GR9 a)), the bit-alignment proof (SR12, CALL's SR6/SR8)
/// and the literal-2 verdict (<see cref="ContentConformanceReason"/>).</para>
/// </summary>
internal sealed class ParameterConformance(BinderContext ctx, StatementBinder host)
{
    /// <summary>One bound argument against its corresponding formal parameter, reported under
    /// <paramref name="site"/>. <paramref name="position"/> is 1-based. An OMITTED argument is never asked:
    /// §14.8.2's rules are about a formal parameter's DESCRIPTION, which an omitted argument has none of.
    /// <list type="bullet">
    /// <item>An ADDRESS-IDENTIFIER (kb/Work PB239) is a pointer value with no storage behind it; its law is the
    /// class-pointer paragraph both passing regimes share.</item>
    /// <item>BY REFERENCE: §14.8.2.3.2 / §14.8.2.2 rule 1 — the same-description comparator, in ACTIVATION mode
    /// (rules d/e: an ANY LENGTH formal takes the argument's length), plus the class-pointer paragraph's last
    /// sentence: "If either is a restricted pointer, both shall be restricted and of the same type."</item>
    /// <item>BY CONTENT / BY VALUE: §14.8.2.3.3 / §14.8.2.2 rule 2 — the COMPUTE / SET / MOVE regime per formal
    /// category.</item>
    /// </list></summary>
    internal void CheckArgument(DataItem formal, BoundCallArg arg, int position, ActivationSite site)
    {
        if (arg.Omitted) return;
        if (arg.DataAddress is not null || arg.ProgramAddress is not null)
        {
            if (host.Ptr.AddressConformanceReason(formal, arg.DataAddress, arg.ProgramAddress) is { } awhy)
                ctx.Edition.Error(site.Conformance,
                    $"{site.Callee} argument {position} (an address-identifier) does not conform to formal "
                    + $"parameter '{formal.CobolName}': {awhy}");
            return;
        }
        if (arg.Mode is CobolPassMode.Reference && arg.Place is { } ap)
        {
            if (CobolNet.Compiler.Oo.OoConformance.DescriptionMismatch(formal, ap.Item,
                    byRefGroupPrefix: true,
                    // §14.8.2.3.2 rules d/e — ACTIVATION mode, the INVOKE twin's (OoBinder) setting. PAIR mode
                    // demanded the same ANY LENGTH clause on both sides, so a plain argument meeting an ANY LENGTH
                    // formal — the clause's whole purpose, rule d: "its length is considered to match the length
                    // of the corresponding argument" — was refused at every AS NESTED call (kb/Work PB240).
                    anyLengthActivationRelax: true) is { } why)
                ctx.Edition.Error(site.Conformance,
                    $"{site.Callee} argument {position} ('{ap.Item.CobolName}') does not conform to formal "
                    + $"parameter '{formal.CobolName}': {why} (ISO §14.8.2 via {site.ImportingRule})");
            // ⛔ THE `asNested` SCOPING IS GONE, AND ITS PREMISE WITH IT (kb/Work PB427). §8.5.3.1's equivalence
            // is decided from the DECLARATIONS themselves — StrongTypeModel.SameRestriction resolves each
            // restriction to its own source element's type declaration and compares them structurally — so a
            // separately-declared activated element is exactly the case the rule is FOR (kb/Work PB153).
            var argR = StrongTypeModel.PointerRestriction(ap.Item);
            var formalR = StrongTypeModel.PointerRestriction(formal);
            if ((argR.IsRestricted || formalR.IsRestricted) && !StrongTypeModel.SameRestriction(argR, formalR))
                ctx.Edition.Error(site.Conformance,
                    $"{site.Callee} argument {position} ('{ap.Item.CobolName}') and formal parameter "
                    + $"'{formal.CobolName}': one is a RESTRICTED data-pointer and the other is not restricted to "
                    + $"the same type (argument: {argR}; formal: {formalR}) — ISO §14.8.2.3.2 requires that if either "
                    + "is a restricted pointer, both shall be restricted and of the same type");
            return;
        }
        // §14.8.2.3.3 (BY CONTENT / BY VALUE) + §14.8.2.2 rule 2 — the OTHER ARM OF THE SAME DISPATCH (kb/Work
        // PB165). With no screen here, CobolArgAdapt's converting views silently ADAPTED a non-conforming pair:
        // `CALL "S" AS NESTED USING BY CONTENT A` with `A PIC X(4) VALUE "ABCD"` and a `PIC 9(4)` formal printed
        // `LA=0000` — a wrong answer, on source the standard says is in error, with no diagnostic.
        if (arg.Mode is CobolPassMode.Content or CobolPassMode.Value && ContentConformanceReason(formal, arg) is { } cwhy)
            ctx.Edition.Error(site.Conformance,
                $"{site.Callee} argument {position} "
                + $"({(arg.Place is { } cp ? $"'{cp.Item.CobolName}'" : "the value operand")}) does not conform to "
                + $"formal parameter '{formal.CobolName}' "
                + $"{(arg.Mode is CobolPassMode.Value ? "BY VALUE" : "BY CONTENT")}: {cwhy} "
                // §14.8.2 as a whole, as the BY REFERENCE arm says it: the reason names its own subclause —
                // §14.8.2.3.3 for an elementary formal, §14.8.2.2 rule 2 for a group one (kb/Work PB1617).
                + $"(ISO §14.8.2 via {site.ImportingRule})");
    }

    /// <summary>⛔ THE ONE reading of a figurative ZERO argument into an elementary NUMERIC formal (kb/Work PB1617
    /// for INVOKE, PB1634 for CALL). §14.8.2.3.3 2) a): "If the formal parameter is numeric, the conformance rules
    /// are the same as for a COMPUTE statement with the argument as the sending operand", and a COMPUTE reads the
    /// figurative ZERO as the numeric value zero (§8.8.1.1 names it among an arithmetic expression's operands;
    /// §8.3.3.6.3 SR1 a) makes it the one figurative a numeric literal's position admits). So the argument IS the
    /// numeric literal 0: it takes the numeric-literal verdict and the numeric carrier. The character fill is for
    /// a character-carried formal only. Before CALL asked this, a ZERO into a <c>PIC 9(4) BINARY</c> formal crossed
    /// as the one-character fill "0" and landed as its character code, 48, BY VALUE, BY CONTENT and keyword-less
    /// alike.</summary>
    internal static BoundOperand? ArgumentForFormal(BoundOperand? value, DataItem formal) =>
        value is BoundFigurative { Kind: 'Z' } && formal is { IsGroup: false, Pic.Category: PicCategory.Numeric }
            ? new BoundNumericLiteral("0")
            : value;

    /// <summary>ISO §14.8.2.3.3's conformance verdict for ONE bound BY CONTENT / BY VALUE argument, dispatched
    /// on the argument's SHAPE onto the rules that live with their BY REFERENCE sibling in
    /// <see cref="CobolNet.Compiler.Oo.OoConformance"/> (kb/Work PB165). Null when conformant.
    /// <para>Each shape takes the clause the standard names for it: an identifier the per-formal-category rule
    /// (2a COMPUTE / 2b SET / 2c ANY LENGTH / 2d MOVE, plus the class-pointer and object-reference SET paragraph);
    /// a boolean expression or boolean literal Table 16's BOOLEAN row (rule 2d); an arithmetic expression rule
    /// 2a's COMPUTE; an alphanumeric literal rule 2d's MOVE; a numeric literal whichever of 2a/2d its value can
    /// satisfy; an intrinsic function-identifier the rule for a data item of its RESULT category (§15.4 — "The
    /// evaluation of a function produces a returned value in a temporary elementary data item"). A CONSTANT-NAME
    /// argument arrives already substituted as its literal (§13.10.4 GR1), so it needs no arm of its own.</para>
    /// </summary>
    internal string? ContentConformanceReason(DataItem formal, BoundCallArg arg)
    {
        if (arg.ContentBool is not null)
            return CobolNet.Compiler.Oo.OoConformance.ContentBooleanMismatch(formal);
        if (arg.Place is { } p)
            return CobolNet.Compiler.Oo.OoConformance.ContentMismatch(host.OoClasses, formal, p);
        return arg.Value switch
        {
            // ⛔ NULL IS AN IDENTIFIER OF CLASS POINTER OR OBJECT, NOT A FIGURATIVE LITERAL (kb/Work PB1630). §8.4.3.1.2
            // lists it as Format 8 (predefined-address) and Format 6 (predefined-object); §8.4.3.10.1: "NULL is a
            // predefined address of class pointer", §8.4.3.7.3 SR2: "class object and category object reference". So it
            // is asked the question for an identifier of that class: §14.8.2.3.3's SET paragraph for a formal of class
            // pointer or object reference — and a SET of any such receiver TO NULL is valid (§14.9.39) —
            // and, for every other formal, the COMPUTE / SET-index / MOVE rule its class takes, none of which admits a
            // pointer or object operand (§14.9.25.3 SR1 for the MOVE, which §14.8.2.2 rule 2 also asks of a group
            // formal). The alphanumeric-figurative arm below used to answer it, so NULL crossed into a PIC X formal as
            // a one-character fill and into a pointer formal as a string the slot adapter refused at run time.
            BoundPredefinedNull => SlotWindow.CarriedBySlot(formal) ? null
                : "NULL is the predefined address of class pointer (ISO §8.4.3.10.1) or the predefined object reference "
                + "of class object (§8.4.3.7.3 SR2); §14.8.2.3.3 transfers it only into a formal parameter of class "
                + "pointer or object reference, by the SET rules, and a formal of any other class takes its argument by a "
                + "MOVE or COMPUTE, whose operands shall not be of class object or pointer (§14.9.25.3 SR1)",
            // ⛔ A GROUP FORMAL ASKS THE WHOLE MOVE QUESTION (kb/Work PB1617). §14.8.2.1 sends every pair that is not
            // elementary-to-elementary to §14.8.2.2, whose rule 2 makes a BY CONTENT argument's conformance "the same as
            // for a MOVE statement with the argument as the sending operand and the corresponding formal parameter as
            // the receiving operand" — and a MOVE into a group is more than Table 16: SR2 refuses anything but a
            // same-type group into a strongly-typed one, and SR9 / §8.5.1.12.1 refuse any operand but a compatible
            // group into a VARIABLE-LENGTH one ("may not undergo … a move operation … unless the other operand is a
            // compatible group"). The literal arms below answered every group formal with "conformant", so a
            // figurative constant reached a variable-length group formal and died at run time as
            // EC-PROGRAM-ARG-MISMATCH (or crossed silently), and an integer literal — which MOVEs to an
            // alphanumeric group — was refused. The literal is the sender exactly as the written MOVE binds it, so
            // MoveTable16.Validity (kb/Work PB878: "the WHOLE question, never Table 16 alone") answers it.
            // ⛔ A FORMAL OF CLASS POINTER OR OBJECT REFERENCE TAKES THE SET PARAGRAPH (kb/Work PB1617). §14.8.2.3.3: "If
            // the formal parameter is of class pointer or an object reference described without the ACTIVE-CLASS
            // phrase, the conformance rules shall be the same as if a SET statement were performed … with the argument
            // as the sending operand", and no SET format sends a nonnumeric, numeric or ALL literal, or a figurative
            // constant other than NULL, to such a receiver. The literal arms below asked Table 16 instead and admitted
            // SPACE, which then reached the callee's managed slot as a string (run-time EC-PROGRAM-ARG-MISMATCH on the
            // CALL lane, a backend CS1503 on the INVOKE lane once INVOKE carried figuratives). NULL is the one
            // figurative such a SET admits, and it is left to the lanes that carry it.
            BoundStringLiteral or BoundAllLiteral or BoundFigurative or BoundNumericLiteral
                when SlotWindow.CarriedBySlot(formal) =>
                "§14.8.2.3.3 transfers a value into a formal parameter of class pointer or object reference by the SET "
                + "rules, and a literal or a figurative constant other than NULL is not a sending operand of any SET format",
            BoundStringLiteral or BoundAllLiteral or BoundFigurative or BoundNumericLiteral when formal.IsGroup =>
                MoveTable16.Validity(arg.Value, Table16Operand.Of(formal), formal) is { } move
                    ? $"§14.8.2.2 rule 2 transfers the argument into the group formal parameter by the MOVE rules: {move.Reason}"
                    : null,
            // ⛔ RULE 2a FIRST: "If the formal parameter is numeric, the conformance rules are the same as for a
            // COMPUTE statement", and a COMPUTE's sending operands are numeric — §8.8.1.1 admits "a numeric literal,
            // the figurative constant ZERO" and no other literal. So a nonnumeric literal, an ALL literal or any
            // other figurative has no conforming crossing into a numeric formal, whatever Table 16 would say of a
            // MOVE of the same pair (kb/Work PB1418: `FUNCTION F("ABCD")` into a PIC 9(4) formal ran as 0000, and
            // the Format-2 CALL's BY CONTENT twin with it — rule 2d's MOVE question was being asked instead).
            BoundStringLiteral or BoundAllLiteral or BoundFigurative { Kind: not 'Z' }
                when formal is { IsGroup: false, Pic.Category: PicCategory.Numeric } =>
                "§14.8.2.3.3 rule 2a transfers a value into a numeric formal parameter by the COMPUTE rules, and a "
                + "nonnumeric literal, an ALL literal or a figurative constant other than ZERO is not a numeric "
                + "sending operand (ISO §8.8.1.1)",
            // A written nonnumeric literal carries its CATEGORY (alphanumeric — plain or hexadecimal —, national or
            // boolean), so rule 2d's MOVE question is asked of that sender exactly (kb/Work PB1137: a national literal
            // at an alphanumeric formal is Table 16's "No").
            BoundStringLiteral s =>
                CobolNet.Compiler.Oo.OoConformance.ContentNonNumericLiteralMismatch(formal, s.Category),
            // A figurative constant and an ALL literal are alphanumeric VALUES whose category the context chooses
            // (§8.3.2.1 / §8.3.3.6.4), so they take rule 2d's MOVE arm under any literal category.
            BoundAllLiteral or BoundFigurative =>
                CobolNet.Compiler.Oo.OoConformance.ContentAlphanumericLiteralMismatch(formal),
            BoundNumericLiteral n =>
                CobolNet.Compiler.Oo.OoConformance.ContentNumericLiteralMismatch(formal, n.Text),
            // A BY VALUE argument binds as a computed operand even when §14.9.4.4 GR8 says it is "merely a single
            // identifier or literal" — the literal case is recovered so the two spellings of one value get ONE
            // verdict (the CallEmitter.ArgText discipline, applied to conformance).
            BoundComputedOperand ce when Gr8ArgumentLiteral.NumericText(ce.Expr) is { } ct =>
                CobolNet.Compiler.Oo.OoConformance.ContentNumericLiteralMismatch(formal, ct),
            BoundComputedOperand { Expr: BoundIntrinsicCall ic } ce2 => IntrinsicResultMismatch(formal, ce2, ic),
            BoundComputedOperand => CobolNet.Compiler.Oo.OoConformance.ContentArithmeticMismatch(formal),
            // No bound value at all (a shape the arms above do not name) is not a conformance verdict to make:
            // the binder has already reported whatever refused to bind.
            _ => null,
        };
    }

    /// <summary>ISO §14.8.2.3.3 for an INTRINSIC function-identifier argument. §8.4.3.1.2 Format 1 makes a
    /// function-identifier an IDENTIFIER, and §15.4 produces its returned value "in a temporary elementary data item"
    /// of the function's category — so the crossing is the one a data item of that category takes. A NUMERIC
    /// result is a numeric value on the arithmetic channel, and takes the arithmetic-expression verdict unchanged.
    /// A character-valued result (alphanumeric, national or boolean — the string channel) takes rule 2a's
    /// COMPUTE into a numeric formal, which it cannot satisfy (a COMPUTE's sending operands are numeric,
    /// §8.8.1.1), and otherwise rule 2d's MOVE — the WHOLE §14.9.25.3 question (SR1's class screen, then
    /// <c>MoveTable16.Validity</c>'s SR2/SR8/SR9/Table-16 chain), asked with the function-identifier itself as the
    /// sender, whose Table-16 row is its §15.2 TYPE (kb/Work PB73).</summary>
    private static string? IntrinsicResultMismatch(DataItem formal, BoundComputedOperand sender, BoundIntrinsicCall ic)
    {
        if (ic.ResultCategory is PicCategory.Numeric)
            return CobolNet.Compiler.Oo.OoConformance.ContentArithmeticMismatch(formal);
        if (formal.Pic is { Category: PicCategory.Numeric })
            return "§14.8.2.3.3 rule 2a transfers a value into a numeric formal parameter by the COMPUTE rules, and "
                + $"the function's {ic.ResultCategory.ToString().ToLowerInvariant()} value is not a numeric sending "
                + "operand (ISO §8.8.1.1)";
        return (MoveTable16.SenderClassRefusal(sender)
                ?? MoveTable16.Validity(sender, Table16Operand.Of(formal), formal)?.Reason) is { } why
            ? $"§14.8.2.3.3 rule 2d transfers the function's value by the MOVE rules: {why}"
            : null;
    }

    /// <summary>The ISO §8.5.2.1 Table-2 class of an argument whose corresponding formal is BY VALUE — the question
    /// §14.9.4.3 SR22 (CALL) and §8.4.3.2.3 SR10 (function) both ask ("shall be of class numeric, object, or
    /// pointer"). Null when not statically decidable (the screens fail open).
    /// <para>⛔ CLASSIFY THE UNDERLYING REFERENCE, NOT THE WRAPPER. <see cref="IntrinsicArgumentRules.ClassOf"/>
    /// maps any <see cref="BoundComputedOperand"/> to NUMERIC — correct for a genuine arithmetic expression — so a
    /// bare identifier or index-name that reached the binder inside an expression node must be unwrapped first:
    /// a <see cref="BoundNumRef"/> is the identifier's own item, and a <see cref="BoundIndexRef"/> is class index
    /// (§8.5.2.1 Table 2's own row; kb/Work PB132).</para></summary>
    internal static CobolClass? ValueArgumentClass(BoundOperand op) => op switch
    {
        BoundComputedOperand { Expr: BoundNumRef { Place: { } place } } =>
            IntrinsicArgumentRules.ClassOf(new BoundFieldOperand(place)),
        BoundComputedOperand { Expr: BoundIndexRef } => CobolClass.Index,
        // A figurative constant's class is chosen by its context (§8.3.3.6.4 GR1/GR4), so the general classifier
        // answers "undecidable" for it. A BY VALUE position admits the one reading that can be numeric — ZERO —
        // and no figurative or ALL literal is ever of class object or pointer.
        BoundFigurative { Kind: 'Z' } => CobolClass.Numeric,
        BoundFigurative or BoundAllLiteral => CobolClass.Alphanumeric,
        _ => IntrinsicArgumentRules.ClassOf(op),
    };

    /// <summary>⛔ THE STORAGE-SECTION MODE TEST (kb/Work PB1137), asked of a KEYWORD-LESS identifier argument before it
    /// is resolved: does it meet "the requirements of" the rule both activating statements write in the same words
    /// — an address-identifier or "a data item defined in the file, working-storage, local-storage, or linkage
    /// section" that is not factory/instance object data (INVOKE §14.9.23.3 SR9 + SR10; CALL §14.9.4.3 SR3, whose
    /// second sentence is the object-data half)? §14.9.23.4 GR6 a) (INVOKE) and §14.9.4.4 GR9 a) (Format-2 CALL)
    /// assume BY REFERENCE — a receiving operand — when it does, and BY CONTENT — a sending one — when it does not,
    /// so the answer decides the operand's ROLE and must come first. Non-diagnosing: a reference that identifies
    /// nothing answers true, and the BY REFERENCE resolution that follows reports it through the resolver's own rule.
    /// <para>The report and linage counters and EXCEPTION-OBJECT are recognized by their REFERENCE ahead of the
    /// probe, because the ordinary resolver does not build their places; an object property by its reference,
    /// because its temporary exists only once a statement binds it (<see cref="SectionDataItem"/> answers the
    /// rest from the probed place).</para></summary>
    internal bool MeetsByReferenceRules(CobolNet.Frontend.Generated.CobolParserCore.DataReferenceContext dref)
    {
        if (dref.PAGE_COUNTER() is not null || dref.LINE_COUNTER() is not null || dref.LINAGE_COUNTER() is not null)
            return false;
        if (ctx.Refs.IsExceptionObjectRegister(dref) || ctx.Refs.IsObjectPropertyReference(dref)) return false;
        if (ctx.Refs.Probe(dref) is not { } probe) return true;
        return probe.NonSectionKind is null && !ctx.Data.OoIsObjectData(probe.Item);
    }

    /// <summary>The storage-section rule over a RESOLVED BY REFERENCE argument or RETURNING item — CALL §14.9.4.3
    /// SR3/SR7, INVOKE §14.9.23.3 SR9/SR11 (kb/Work PB1137): <see cref="SectionDataItem"/> says what the place is
    /// when it is not a data item of the four sections. An OBJECT PROPERTY is admitted whatever its temporary:
    /// §8.4.3.9.3 SR5/SR6 let it "be specified wherever a data item with that description would be valid" as a
    /// sending or receiving item. False having reported.</summary>
    /// <param name="p">The resolved operand.</param>
    /// <param name="written">The reference as written — the object-property test and the message text.</param>
    /// <param name="code">The descriptor the violation reports under.</param>
    /// <param name="subject">How the message names the operand — <c>CALL USING argument</c>,
    /// <c>INVOKE RETURNING item</c>.</param>
    /// <param name="clause">The rule the message cites.</param>
    /// <param name="addressAdmitted">The rule also admits an address-identifier (a USING argument; never the
    /// RETURNING item).</param>
    internal bool ScreenSection(Place p, CobolNet.Frontend.Generated.CobolParserCore.DataReferenceContext written,
        DiagnosticDescriptor code, string subject, string clause, bool addressAdmitted)
    {
        if (SectionDataItem.NonSectionKind(p) is not { } kind || ctx.Refs.IsObjectPropertyReference(written))
            return true;
        ctx.Edition.Error(code,
            $"{subject} '{DataBinder.WrittenText(written)}' references {kind}; ISO {clause} requires "
            + (addressAdmitted ? "an address-identifier or " : "")
            + "a data item defined in the file, working-storage, local-storage, or linkage section");
        return false;
    }

    /// <summary>The byte-boundary proof a bit data item passed BY REFERENCE owes — §14.9.4.3 SR6/SR8 (CALL),
    /// §14.9.23.3 SR12 (INVOKE — kb/Work PB1137) and
    /// §8.4.3.2.3 SR14 (function), which state the SAME two requirements: the item is "aligned on a byte
    /// boundary", and its subscripting and reference-modification leftmost position consist of only numeric
    /// literals or all-literal arithmetic expressions without exponentiation. The referenced occurrence's start
    /// bit is computed from the §8.5.1.6.3 cursor walk (<see cref="BitLayout.StartBitWithin"/>) plus each table
    /// subscript times its element stride, plus a ref-mod's leftmost boolean position; a non-literal subscript is
    /// itself the violation. An operand shape the walk cannot model (an unmodelled overlay, an exotic carrier) is
    /// ACCEPTED — the screen must never reject legal source it cannot prove misaligned.</summary>
    /// <param name="p">The argument (or CALL RETURNING item) as resolved.</param>
    /// <param name="code">The descriptor the violation reports under.</param>
    /// <param name="subject">How the message names the operand — <c>CALL USING argument</c>,
    /// <c>FUNCTION F argument 2</c>.</param>
    /// <param name="clause">The rule the message cites.</param>
    internal void ScreenBitAlignment(Place p, DiagnosticDescriptor code, string subject, string clause)
    {
        long extra = 0;
        Place core = p;
        while (core is PlaceDecorator dec)
        {
            if (core is RefModPlace rm)
            {
                if (ConstIndex(rm.Start) is not { } s0)
                {
                    ctx.Edition.Error(code,
                        $"{subject} '{p.Item.CobolName}': a bit item's reference-modification leftmost position "
                        + $"shall consist of only fixed-point numeric literals (ISO {clause})");
                    return;
                }
                extra += s0 - 1;
            }
            core = dec.Inner;
        }
        // ⛔ THE TIER-B REDEFINES VIEW (kb/Work PB240). A bit member of a REDEFINES class is a window over the
        // class's ONE backing, not a member path, so the walk below never saw it and the screen ACCEPTED the
        // operand unproven — a genuinely misaligned bit item crossed BY REFERENCE through any redefinition of
        // its record (measured: `05 R REDEFINES X. 10 RB1 PIC 1(3) BIT. 10 RB2 PIC 1(8) BIT.` passed RB2,
        // which starts at bit 3). The position is still statically known: §13.18.44.4 GR1 — "Storage association
        // for the subject of the entry starts at the first bit of the data item referenced by data-name-2" —
        // puts every redefinition at the redefined item's own first bit, so the class canonical's start within its
        // record (the SAME §8.5.1.6.3 walk, over its own parent chain) plus the member's class-relative bit
        // offset (BitWindow.ClassRelativeExpr — its in-class offset and each in-class subscript's stride) IS the
        // occurrence's start. A BASED class's runtime displacement is whole bytes and cannot move the answer.
        if (core is RedefViewPlace { Bit: { } bw, ViewItem.Class: { } viewCls })
        {
            if (ConstIndex(bw.ClassRelativeExpr) is not { } rel)
            {
                ReportNonLiteralSubscript();
                return;
            }
            if (RecordBitOffset(viewCls.Canonical) is not { } canon) return;   // an unmodelled chain — never reject
            ReportMisaligned(canon + rel + extra);
            return;
        }
        AccessPath? path = core switch { MemberPlace mp => mp.Path, DynTablePlace dp => dp.Path, _ => null };
        if (path is null) return;
        var chain = new List<DataItem>();
        for (var d = core.Item; d is not null; d = d.Parent) chain.Insert(0, d);
        var subs = new Queue<string>();
        foreach (var seg in path.Segments)
        {
            if (seg is FixedTableSegment ft) subs.Enqueue(ft.OneBasedIndex);
            else if (seg is DynTableSegment dt) subs.Enqueue(dt.OneBasedIndex);
        }
        long bit = 0;
        for (int i = 0; i < chain.Count; i++)
        {
            if (i > 0)
            {
                int within = BitLayout.StartBitWithin(chain[i - 1], chain[i]);
                if (within < 0) return;
                bit += within;
            }
            bool tabled = chain[i].Occurs is not null || chain[i].IsDynamicTable || chain[i].OccursSpec is not null;
            if (tabled && subs.Count > 0)
            {
                if (ConstIndex(subs.Dequeue()) is not { } k)
                {
                    ReportNonLiteralSubscript();
                    return;
                }
                bit += (k - 1) * (long)BitLayout.StrideBits(chain[i]);   // a SUBSCRIPT stride — ALIGNED strides whole bytes (§13.18.1.4 GR2)
            }
        }
        ReportMisaligned(bit + extra);

        void ReportNonLiteralSubscript() =>
            ctx.Edition.Error(code,
                $"{subject} '{p.Item.CobolName}': a bit item's subscripts shall consist of only fixed-point numeric "
                + $"literals or all-literal arithmetic expressions without exponentiation (ISO {clause})");

        void ReportMisaligned(long start)
        {
            if (start % BitLayout.BitsPerCharacter != 0)
                ctx.Edition.Error(code,
                    $"{subject} '{p.Item.CobolName}' starts at bit {start} of its record — a bit item passed by "
                    + $"reference shall be aligned on a byte boundary (ISO {clause} / §8.5.1.6.3)");
        }
    }

    /// <summary>The start bit of <paramref name="item"/> within its level-01 record — the §8.5.1.6.3 cursor walk
    /// (<see cref="BitLayout.StartBitWithin"/>) summed over its parent chain, with every OCCURS level at its
    /// FIRST occurrence; null when a link of the chain is unmodelled. Used for a REDEFINES class's canonical
    /// (kb/Work PB240), whose ancestors carry no OCCURS: a class whose backing sits inside a table has no place
    /// the resolver can build.</summary>
    private static long? RecordBitOffset(DataItem item)
    {
        long bit = 0;
        for (var d = item; d.Parent is { } parent; d = parent)
        {
            int within = BitLayout.StartBitWithin(parent, d);
            if (within < 0) return null;
            bit += within;
        }
        return bit;
    }

    /// <summary>Evaluate a rendered subscript/ref-mod index that the bit-alignment rules permit — an integer
    /// literal or an all-literal + - * / ( ) expression (no exponentiation; identifiers make it non-constant →
    /// null).</summary>
    private static long? ConstIndex(string rendered)
    {
        string s = rendered.Trim();
        if (long.TryParse(s, out long direct)) return direct;
        foreach (char ch in s)
            if (!(char.IsDigit(ch) || ch is '+' or '-' or '*' or '/' or '(' or ')' or ' ')) return null;
        int i = 0;
        long? r = AddSub(s, ref i);
        return r is not null && SkipWs(s, ref i) == s.Length ? r : null;

        static int SkipWs(string t, ref int j) { while (j < t.Length && t[j] == ' ') j++; return j; }
        static long? AddSub(string t, ref int j)
        {
            long? v = MulDiv(t, ref j);
            while (v is not null && SkipWs(t, ref j) < t.Length && t[j] is '+' or '-')
            {
                char op = t[j++];
                long? w = MulDiv(t, ref j);
                v = w is null ? null : op == '+' ? v + w : v - w;
            }
            return v;
        }
        static long? MulDiv(string t, ref int j)
        {
            long? v = Primary(t, ref j);
            while (v is not null && SkipWs(t, ref j) < t.Length && t[j] is '*' or '/')
            {
                char op = t[j++];
                long? w = Primary(t, ref j);
                v = w is null or 0 && op == '/' ? null : op == '*' ? v * w : v / w;
            }
            return v;
        }
        static long? Primary(string t, ref int j)
        {
            if (SkipWs(t, ref j) >= t.Length) return null;
            if (t[j] == '(')
            {
                j++;
                long? v = AddSub(t, ref j);
                if (SkipWs(t, ref j) >= t.Length || t[j] != ')') return null;
                j++;
                return v;
            }
            if (t[j] is '+' or '-')
            {
                char sign = t[j++];
                long? v = Primary(t, ref j);
                return sign == '-' ? -v : v;
            }
            int start = j;
            while (j < t.Length && char.IsDigit(t[j])) j++;
            return j > start && long.TryParse(t[start..j], out long n) ? n : null;
        }
    }
}
