// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Bound;
using CobolNet.Editions;

namespace CobolNet.CodeGen.Emit;

/// <summary>
/// Renders a bound condition to a side-effect-free C# boolean expression (COBOLNET_DESIGN §11): relational
/// comparisons (numeric scale-aligned, or alphanumeric via <c>CobolString.Compare</c>), logical AND/OR/XOR/NOT,
/// level-88 membership over the conditional variable, and sign conditions. An unbound condition fails loud (§1.4).
/// ONE deliberate exception to side-effect freedom: <see cref="BoundUdfEvaluated"/> — a per-evaluation
/// user-function window — renders as an immediately-invoked <c>Func&lt;bool&gt;</c> whose body runs the
/// activations then returns the inner predicate, so the activation executes exactly when (and only when) the
/// containing condition text evaluates (ISO §8.4.3.2.4 GR1/GR6a; §8.8.4.13 r2 — loop headers re-evaluate it per
/// iteration, a short-circuited <c>&amp;&amp;</c>/<c>||</c> operand skips it entirely).
/// </summary>
internal sealed class ConditionRenderer(NumericRenderer num, EmitContext ctx) : IBoundConditionVisitor<string>
{
    /// <summary>The CALL emitter — property-wired by <see cref="UnitEmitters"/> (the per-evaluation
    /// <see cref="BoundUdfEvaluated"/> window renders the SAME activation text the statement hoist emits;
    /// the ctor cannot take it — CallEmitter builds after this renderer).</summary>
    internal CallEmitter Calls { get; set; } = null!;

    /// <summary>The statement emitter — property-wired by <see cref="UnitEmitters"/> for the same reason
    /// <see cref="Calls"/> is. A per-evaluation window's pre-ops are not all CALLs: a D18 function-bearing
    /// subscript hoists a §15.4 temporary STORE (fix-queue PB17), which is rendered by capturing this emitter's
    /// output so the store gets the ONE arithmetic store path (scale alignment, truncation, the wide tier) rather
    /// than a second hand-written renderer.</summary>
    internal StatementEmitter Statements { get; set; } = null!;

    /// <summary>The pointer-value renderer an address-identifier relation operand reads through (kb/Work PB1021).</summary>
    internal PtrEmitter Ptr { get; set; } = null!;

    /// <summary>Render a bound condition as a C# boolean expression. Dispatch is the generated exhaustive
    /// <see cref="IBoundConditionVisitor{T}"/> (PHASE-07 Step 6e): every BoundCondition leaf has a Visit below, so a
    /// new leaf is a COMPILE error — the former loud <c>_ =></c> default is gone.</summary>
    public string Render(BoundCondition c)
    {
        // A condition is a receiver-less numeric context: clear the float-receiver flag so a stale one from a prior
        // arithmetic store cannot promote a fixed-operand comparison to IEEE double (the H1 staleness discipline, D16
        // review). Idempotent under the recursive Render calls below.
        return c.Accept(this);
    }

    public string Visit(BoundRelational n) => RenderRelational(n);
    // An EMPTY logical is the tautology (EVALUATE's ANY object composes as an AND over zero terms).
    public string Visit(BoundLogical n) => n.Operands.Count == 0
        ? "true"
        : "(" + string.Join($" {n.Op} ", n.Operands.Select(Render)) + ")";
    public string Visit(BoundNot n) => $"!({Render(n.Operand)})";
    public string Visit(BoundCondition88 n) => RenderCondition88(n);
    // A switch-status condition (ISO §8.8.4.6 GR1): true when the external switch is at the posited position.
    public string Visit(BoundSwitchCondition n) => n.TestsOn
        ? $"ExternalSwitches.Get({EmitText.CsLiteral(n.ImplementorName)})"
        : $"!ExternalSwitches.Get({EmitText.CsLiteral(n.ImplementorName)})";
    public string Visit(BoundSignCondition n) => RenderSign(n);

    /// <summary>§8.8.4.8 (kb/Work PB133 wave C) — the formal carrier's IsNull IS the omitted-argument
    /// condition; the CA10-checked GR12 raise lives in the carrier's accessors, never here (testing presence
    /// is one of the rule's two sanctioned reference forms).</summary>
    public string Visit(BoundOmittedCondition n) => n.Negated
        ? $"(!{CallEmitter.OmittedTest(n.Probe)})"
        : $"({CallEmitter.OmittedTest(n.Probe)})";
    // A simple boolean condition (ISO §8.8.4.3.4 GR1): true iff the boolean value is 1.
    public string Visit(BoundBooleanCondition n) => $"CobolBool.IsTrue({BooleanRenderer.Render(n.Expr, num)})";
    public string Visit(BoundClassCondition n) => RenderClass(n);
    // An EVALUATE WHEN alphanumeric/national THRU range (§14.7.8): ThruMember sets EC-RANGE-INVALID (nonfatal) for an
    // inverted range (Lo collating after Hi) and returns false (empty range), else the inclusive-bound membership.
    // Produced under EC-RANGE-INVALID checking OR when the range names its own sequence with an IN phrase — the
    // plain BoundLogical of two relations has no slot for a per-range collating sequence, and renders byte-identically
    // for every other range (kb/Work PB398).
    public string Visit(BoundRangeMembership n)
    {
        // The range test is a PAIR of relation conditions (§14.7.8 rule 2 over §8.8.4.2), so its collating
        // sequence comes from the ONE comparison-class rule over BOTH operands — not from a local "is either
        // side national" test, which read `Place.Item.Pic` and so missed a national GROUP (Pic null, the
        // §13.18.29.4 GR2b as-if PICTURE lives on OperandPic), a national ref-mod slice and a national function
        // result, handing all three the alphanumeric 256-entry weight table (kb/Work PB741 sweep).
        // §14.7.8 rule 2 — an IN alphabet-name-1 phrase names the sequence OUTRIGHT ("the collating sequence used
        // for range evaluation is the collating sequence defined by that alphabet"), so it displaces the
        // categories' own answer, PROGRAM COLLATING SEQUENCE included: a range written `IN STANDARD-1` collates
        // natively inside a program whose PCS reorders the alphabet. An identity alphabet registers no carrier and
        // renders as the native two-argument overload, which IS its sequence.
        PicCategory? subjectCat = StringCategoryOf(n.Left);
        string collate = RangeCollateArg(n.Alphabet, subjectCat, StringCategoryOf(n.Lo));
        // ⛔ A FIGURATIVE RANGE END IS A SEED, NOT A VALUE (ISO §8.3.3.6.4 GR2 — it is materialized to "the
        // associated data item"'s size, which for both ends of a range is the item being tested). It is rendered
        // through the SAME producer the figurative relation uses — <see cref="FigSeed"/>, so HIGH-/LOW-VALUE is
        // the tested category's own collating extreme — and SIZED by the runtime, which is the only place the
        // width is known when the tested operand is a ref-mod slice with computed bounds (kb/Work PB297's rule,
        // PB401's site). Before this the carrier compared a ONE-CHARACTER seed: `EVALUATE X WHEN LOW-VALUE THRU
        // "AA"` over PIC X(2) LOW-VALUES answered the OPPOSITE of the identical relation pair, the moment EC
        // checking or an IN phrase routed it here.
        static bool IsFigSeed(BoundOperand o) => o is BoundFigurative or BoundAllLiteral;
        bool loFig = IsFigSeed(n.Lo), hiFig = IsFigSeed(n.Hi);
        // ⛔ deSign, EXACTLY AS THE DIRECT STRING RELATION DOES IT (kb/Work PB401 sweep). §14.9.13.4 GR4 a) 5.
        // lowers this node to "selection-subject >= left-part AND selection-subject <= right-part", i.e. to the
        // very relation pair <see cref="RenderRelational"/> renders, and that pair drops a signed numeric
        // operand's operational sign when the comparison is alphanumeric (§8.8.4.2.5 → §14.9.25.4 GR6a). This arm
        // never did, so `EVALUATE S9-ITEM WHEN "A" THRU "Z"` compared an OVERPUNCHED image here and a plain one
        // one lowering over — the two-arm shape, and reachable the moment the EC gate stopped demanding a
        // literal pair. A no-op for every non-signed-numeric operand, which is why it is unconditional there too.
        string read = OperandText.AsString(n.Left, num, deSign: true);
        var lo = new MembershipOperand(loFig ? FigSeed(n.Lo, subjectCat) : OperandText.AsString(n.Lo, num, deSign: true), loFig);
        var hi = new MembershipOperand(hiFig ? FigSeed(n.Hi, subjectCat) : OperandText.AsString(n.Hi, num, deSign: true), hiFig);
        return RenderStringRange(read, lo, hi, n.CheckInvalid, collate);
    }

    /// <summary>One end of a character-comparison range, or a level-88 VALUE: the C# expression, and whether it
    /// is a figurative SEED (one fill character, or literal-1 of <c>ALL literal-1</c>) that the runtime sizes to
    /// the tested operand's own character-position count (ISO §8.3.3.6.4 GR2) rather than a value of its own.</summary>
    private readonly record struct MembershipOperand(string Text, bool IsFigSeed);

    /// <summary>⛔ THE ONE RENDERING OF AN ALPHANUMERIC / NATIONAL / BOOLEAN THROUGH-RANGE TEST — the EVALUATE
    /// WHEN range (<see cref="Visit(BoundRangeMembership)"/>) and the level-88 VALUE … THRU range
    /// (<see cref="RenderMembershipTest"/>) are the same §14.7.8 test, and §8.8.4.5.3 GR2 compares a
    /// condition-name value by the relation-condition rules, so the two may not size a figurative end, or raise
    /// rule 2's EC-RANGE-INVALID, by two different spellings (kb/Work PB1477: the 88 folded a figurative end to
    /// the variable's DECLARED width while the EVALUATE range sized it at run time).
    /// <para><paramref name="checkInvalid"/> selects <c>ThruMember</c> / <c>ThruMemberFig</c>, which add exactly
    /// the §14.7.8 rule 2 EC-set; unchecked, the test is the inclusive bound pair the relation lowering produces
    /// (§14.9.13.4 GR4 a) 5.) — emitting the carrier with checking off would set a nonfatal EC no >>TURN asked
    /// for. <paramref name="weights"/> is the trailing pad / collation fragment.</para></summary>
    private static string RenderStringRange(string read, MembershipOperand lo, MembershipOperand hi, bool checkInvalid,
        string weights)
    {
        if (checkInvalid)
            return lo.IsFigSeed || hi.IsFigSeed
                ? RuntimeApi.ThruMemberFig(read, lo.Text, hi.Text, lo.IsFigSeed, hi.IsFigSeed, weights)
                : RuntimeApi.ThruMember(read, lo.Text, hi.Text, weights);
        string loCmp = lo.IsFigSeed ? RuntimeApi.StrCompareFig(read, lo.Text, figIsLeft: false, weights)
                                    : RuntimeApi.StrCompare(read, lo.Text, weights),
               hiCmp = hi.IsFigSeed ? RuntimeApi.StrCompareFig(read, hi.Text, figIsLeft: false, weights)
                                    : RuntimeApi.StrCompare(read, hi.Text, weights);
        return $"({loCmp} >= 0 && {hiCmp} <= 0)";
    }

    // A user-defined class (§8.8.4.4 / §12.3.7): operand consists entirely of the class's member characters.
    public string Visit(BoundUserClassCondition n) => n.Negated
        ? $"!CobolClass.IsInClass({OperandText.AsString(n.Operand, num, sending: SendingRef.ClassCondition)}, {EmitText.CsLiteral(n.Members)})"
        : $"CobolClass.IsInClass({OperandText.AsString(n.Operand, num, sending: SendingRef.ClassCondition)}, {EmitText.CsLiteral(n.Members)})";
    // An alphabet-name class (§8.8.4.4.4 GR3 a; kb/Work PB109): membership of the alphabet's coded character set
    // (routed through the RuntimeApi façade — the P7 Step 4b ratchet forbids NEW bare runtime accesses here).
    public string Visit(BoundCodedSetClassCondition n) => n.Negated
        ? $"!{RuntimeApi.ClassInCodedSet(OperandText.AsString(n.Operand, num, sending: SendingRef.ClassCondition), n.Kind)}"
        : RuntimeApi.ClassInCodedSet(OperandText.AsString(n.Operand, num, sending: SendingRef.ClassCondition), n.Kind);
    public string Visit(BoundConditionError n) => EmitText.LoudValue("bool", n.Feature);
    // A per-evaluation user-function window (ISO §8.4.3.2.4 GR1/GR6a; §8.8.4.13 r2): the activations run each
    // time THIS condition text evaluates — an IIFE, so a while-header re-runs them per iteration and a
    // short-circuited &&/|| operand position skips them exactly when COBOL's rule 1 skips the operand.
    public string Visit(BoundUdfEvaluated n) =>
        $"new Func<bool>(() => {{\n{string.Concat(n.Activations.Select(PreOpText))}return {Render(n.Inner)}; }})()";

    /// <summary>One pending PRE-op rendered for EXPRESSION position (inside the IIFE above), through the ONE
    /// statement emitter, captured as text — a user-function activation exactly as a hoisted one is emitted, with
    /// its exception arms and its §14.9.18.4 GR1 b) propagation pickup.
    /// <para>⛔ THERE IS NO SECOND ACTIVATION TEXT ANY MORE (kb/Work PB892). A function activation used to render
    /// here through a dedicated single-statement text that emitted no propagation pickup, on the ground
    /// that a RESUME is a <c>__pc</c>-anchored statement surface a lambda cannot hold; so the registry's boundary
    /// default (since removed) DISCARDED every condition a function propagated from a PERFORM UNTIL, a SEARCH WHEN, an EVALUATE
    /// object or a short-circuited operand, although §14.9.18.4 GR1 b) raises it in the activating element. The
    /// activation is marked <c>OperandEvaluation</c> at bind, so its pickup leaves by throwing to the carrying
    /// statement's <c>BoundActivationSite</c> — never by a <c>goto</c> — and it can run inside the lambda.</para>
    /// <para>The captured text keeps its line breaks: a trailing <c>//</c> comment in it would otherwise swallow
    /// the rest of the lambda.</para></summary>
    private string PreOpText(BoundStatement s) => ctx.Writer.CaptureText(() => Statements.EmitStatement(s));

    private string RenderRelational(BoundRelational r)
    {
        // Object relations FIRST (D-U8; §8.8.4.2.15 :9769 — reference IDENTITY): the figurative branch
        // below would materialize NULL against a width — nonsense for references. The only legal operand
        // shapes reached here are an object-reference field and the NULL figurative (bind-checked, 0868);
        // C# implicit upcasts cover typed-vs-universal mixes (both are CobolObject-rooted).
        // ⛔ THE ONE CATEGORY READER (DataItem.OperandPic) here as everywhere in this file — never raw `Pic`,
        // which is NULL for every group and so silently answers "no category" for a bit / national GROUP
        // (kb/Work PB728). For the three reference categories the two readers agree BY CONSTRUCTION, and that is
        // stated rather than assumed: `OperandPic` is `Pic ?? AsIfPic`, and the only as-if pictures that exist
        // are §13.18.29.4 GR1b/GR2b's — a BIT group's (category boolean) and a NATIONAL group's (category
        // national) — so no group can carry ObjectReference, Pointer, ProgramPointer or FunctionPointer. Reading the ONE reader
        // here is therefore uniformity, not a behaviour change, and it is what lets the drift rule
        // (scripts/semgrep, cobolnet-condition-category-from-raw-picture) forbid a raw PICTURE read outright in
        // this file, with CollatingComparisonClassDriftTests pinning the comparison-class matrix beside it.
        static bool IsObj(BoundOperand o) =>
            o is BoundFieldOperand f && f.Place.Item.OperandPic?.Category == PicCategory.ObjectReference;
        if (IsObj(r.Left) || IsObj(r.Right))
        {
            static string ObjRead(BoundOperand o) => o is BoundFieldOperand f ? PlaceRenderer.Read(f.Place) : "null";
            string core = $"object.ReferenceEquals({ObjRead(r.Left)}, {ObjRead(r.Right)})";
            return r.Op == "==" ? core : $"!({core})";
        }
        // Data-pointer relations (Phase-4b; §8.8.4.2.16 — ManagedPointer.SameTarget: both-NULL / same-storage;
        // the NULL figurative renders as the null carrier). Before the figurative branch (NULL must not
        // width-materialize against a pointer).
        // ⛔ An ADDRESS-IDENTIFIER operand (kb/Work PB1021) is one of each pair's operand kinds: §8.4.3.11.4 GR1 /
        // §8.4.3.13.4 GR1 make it "a unique data item of class pointer" of the data- or program-pointer category,
        // so it rides the SAME category arm a pointer data item does, read through the ONE value renderer.
        static bool IsPtr(BoundOperand o) =>
            o is BoundFieldOperand f && f.Place.Item.OperandPic?.Category == PicCategory.Pointer
            || o is BoundAddressOperand { Data: not null };
        if (IsPtr(r.Left) || IsPtr(r.Right))
        {
            string PtrRead(BoundOperand o) => o switch
            {
                BoundFieldOperand f => PlaceRenderer.Read(f.Place),
                BoundAddressOperand ao => Ptr.AddressOperandText(ao),
                _ => "null",
            };
            string core = $"ManagedPointer.SameTarget({PtrRead(r.Left)}, {PtrRead(r.Right)})";
            return r.Op == "==" ? core : $"!({core})";
        }
        // Program-pointer relations (P10 Step 7; §8.8.4.2.16 — ProgramPointer.SameTarget: both-NULL / the same
        // program's identity; the NULL figurative renders as the Null carrier — a struct, never C# null).
        static bool IsPp(BoundOperand o) =>
            o is BoundFieldOperand f && f.Place.Item.OperandPic?.Category == PicCategory.ProgramPointer
            || o is BoundAddressOperand { Program: not null };
        if (IsPp(r.Left) || IsPp(r.Right))
        {
            string PpRead(BoundOperand o) => o switch
            {
                BoundFieldOperand f => PlaceRenderer.Read(f.Place),
                BoundAddressOperand ao => Ptr.AddressOperandText(ao),
                _ => "ProgramPointer.Null",
            };
            string core = $"ProgramPointer.SameTarget({PpRead(r.Left)}, {PpRead(r.Right)})";
            return r.Op == "==" ? core : $"!({core})";
        }
        // Function-pointer relations — the ProgramPointer arm's twin over the SEPARATE FunctionPointer carrier
        // (§8.8.4.2.16 compares pointers only within a category, and §8.4.3.10 GR2/GR3 keep the predefined NULL
        // function address distinct from the NULL program address). kb/Work PB452/PB817.
        static bool IsFp(BoundOperand o) =>
            o is BoundFieldOperand f && f.Place.Item.OperandPic?.Category == PicCategory.FunctionPointer;
        if (IsFp(r.Left) || IsFp(r.Right))
        {
            static string FpRead(BoundOperand o) =>
                o is BoundFieldOperand f ? PlaceRenderer.Read(f.Place) : "FunctionPointer.Null";
            string core = $"FunctionPointer.SameTarget({FpRead(r.Left)}, {FpRead(r.Right)})";
            return r.Op == "==" ? core : $"!({core})";
        }
        // Boolean-EXPRESSION relations (ISO §8.8.4.2.2 Format 2): when either side is a boolean expression
        // (a B-op tier, BoundBoolOperand), render BOTH sides as '0'/'1' strings and compare by VALUE with
        // right-zero-extension (§8.8.4.2.8) via CobolBool.Equal. A bare boolean item/literal mixed with an
        // expression reads the same '0'/'1' form. Equality-only + boolean purity are bind-enforced (1511).
        // (Bare item↔item boolean compares — no expression — ride the CobolString.Compare(pad:'0') branch
        // below, which is the identical zero-extension under D-B1.)
        if (r.Left is BoundBoolOperand || r.Right is BoundBoolOperand)
        {
            string core = $"CobolBool.Equal({BoolRead(r.Left)}, {BoolRead(r.Right)})";
            return r.Op == "==" ? core : $"!({core})";
        }
        // ⛔ A VARIABLE-LENGTH GROUP HAS NO IMAGE TO COMPARE (kb/Work PB1467). The pair's comparison is asked of the
        // ONE classifier the binder's screen asked (RelationPair — which also lowered every strongly-typed pair to
        // its elementary relations before this node was built), and §8.8.4.2.17's compatible pair compares
        // component by component. The screen refused every other operand opposite such a group (§8.5.1.12.1), so
        // both operands here are groups.
        if (RelationPair.Classify(r.Left, r.Right) is RelationPairShape.VariableLengthGroup)
            return RenderVariableLengthGroupRelational(r);
        // A figurative operand (a single-character constant OR an ALL "literal") is materialized against the OTHER
        // operand's width (ISO §8.3.3.6.4 GR2), so it routes through the width-aware figurative path.
        if (r.Left is BoundFigurative or BoundAllLiteral || r.Right is BoundFigurative or BoundAllLiteral)
            return RenderFigurativeRelational(r);
        // ⛔ WHICH §8.8.4.2 comparison rule this relation selects is asked ONCE, from BOTH operands' categories,
        // through the ONE comparison-class rule — the same call the figurative-relation, level-88-membership and
        // EVALUATE-THRU surfaces make. Reading the class off ONE operand is the kb/Work PB741 defect shape.
        var (leftCat, rightCat) = RelationCategories(r.Left, r.Right);
        CollatingClass cmp = CollatingSelection.ForComparison(leftCat, rightCat);
        // ⛔ BLANK WHEN ZERO IS A COMPARISON RULE TOO (ISO §13.18.8.4 GR3): "If the subject of the entry is a
        // sending data item, the object of an operation is a numeric or numeric-edited data item, and the content
        // of the sending data item is all spaces, the value of the sending data item is considered to be zero."
        // GR1, one rule earlier in the same subclause, says "receiving operand" where GR3 says "the object of an
        // OPERATION", and §8.8.4.2.1 calls the two operands of a relation its subject and its object — so the rule
        // reaches every operation in which the item is a SENDER, and a relation condition is the only one besides
        // the de-editing MOVE that may take a numeric-edited sender at all (§14.9.25.4 GR6 d) 1 legislates that
        // MOVE, and §8.8.1.1 admits only "an identifier referencing a numeric data item" into an arithmetic
        // expression, which an edited item is not). It has to name numeric-edited objects for a reason, and the
        // only reason available is that THE COMPARISON IS BY VALUE there: giving the blanked item "the value
        // zero" changes nothing in a character comparison.
        // The test is on the CONTENT, so it is a run-time one: GR3 speaks only of an all-spaces image, and a
        // BLANK WHEN ZERO item holding digits compares by the ordinary §8.8.4.2.5/.7 rules like any other
        // numeric-edited item. The false arm is therefore the WHOLE ordinary dispatch, not a copy of one of its
        // arms, and the true arm is the ordinary NUMERIC comparison — whose de-edit of an all-spaces image is
        // already GR3's zero, every digit position of it contributing zero (kb/Work PB509).
        if (BlankWhenZeroSenderTest(r, out string bwzBlanked))
            return $"({bwzBlanked} ? {RenderNumericRelational(r)} : {RenderRelationalCore(r, cmp, leftCat, rightCat)})";
        return RenderRelationalCore(r, cmp, leftCat, rightCat);
    }

    /// <summary>ISO §8.8.4.2.17 — "A comparison of two compatible groups, one or both of which is a variable-length
    /// group, proceeds from left to right as described under 8.8.4.2.7, Comparison of alphanumeric operands",
    /// with corresponding tables compared by §14.6.9.3 and dynamic-length items at their current length. Both
    /// operands decompose into the ONE §8.5.1.12 carrier (<see cref="PlaceRenderer.VarGroupCarrier"/> — the MOVE's
    /// GR9 reader), and <c>CobolVarGroup.Compare</c> walks them against the variable-length side's component
    /// offsets, under the alphanumeric program collating sequence §8.8.4.2.7 names.</summary>
    private string RenderVariableLengthGroupRelational(BoundRelational r)
    {
        if (r.Left is not BoundFieldOperand { Place: var lp } || r.Right is not BoundFieldOperand { Place: var rp })
            return EmitText.LoudValue("bool", "a variable-length group compared with an operand that is not a group");
        Place variable = VariableLengthCompatibility.IsVariableLength(lp.Item) ? lp : rp;
        string? left = PlaceRenderer.VarGroupCarrier(lp, rp.Item, "a compared variable-length group",
                          "a fixed-length group compared with a variable-length group"),
                right = PlaceRenderer.VarGroupCarrier(rp, lp.Item, "a compared variable-length group",
                          "a fixed-length group compared with a variable-length group");
        if (left is null || right is null)
            return EmitText.LoudValue("bool", TierCIsland.Reason((left is null ? lp : rp).Item,
                "the §8.8.4.2.17 comparison of"));
        // The sequence is the PAIR's, from the ONE pair reader (kb/Work PB649/PB741) — for two groups, the
        // alphanumeric program collating sequence §8.8.4.2.7 names.
        var (leftCat, rightCat) = RelationCategories(r.Left, r.Right);
        string collate = ctx.CollateArgFor(leftCat, rightCat);
        return $"{RuntimeApi.VarGroupCompare(left, right, PlaceRenderer.VarGroupComponentOffsets(variable), collate)} {r.Op} 0";
    }

    /// <summary>GR3's content test for whichever operand is the subject of a BLANK WHEN ZERO entry and has a
    /// numeric or numeric-edited DATA ITEM opposite it (a literal is neither — §13.18.8.4 GR3 says "data item",
    /// and §8.8.4.2.5 governs the literal case unchanged). With the clause on BOTH operands each is the other's
    /// object, so either being all spaces selects the value comparison.</summary>
    private bool BlankWhenZeroSenderTest(BoundRelational r, out string test)
    {
        // ⛔ NEITHER PREDICATE MAY READ THROUGH A REFERENCE MODIFICATION. §8.4.3.3.4 GR5 makes the slice "a
        // unique data item", and GR6 c) gives it a category of its own — "the categories numeric and
        // numeric-edited are considered class and category national if the usage is national; otherwise they are
        // considered class and category alphanumeric" — so `WS-BWZ(1:2)` is NOT the subject of the BLANK WHEN
        // ZERO entry and `WS-NUM(1:2)` is NOT the numeric data item GR3's object has to be. Reading the BASE
        // item's clause or category through the decorator is the kb/Work PB297 shape, one layer down.
        static BoundFieldOperand? Plain(BoundOperand o) =>
            o is BoundFieldOperand f && f.Place is not (RefModPlace or TableAllPlace) ? f : null;
        static bool Bwz(BoundOperand o) => Plain(o) is { } f && f.Place.Item.BlankWhenZero;
        // "a numeric or numeric-edited data item" — asked of the ONE category reader (DataItem.OperandPic), as
        // every category question in this file is (kb/Work PB728/PB741).
        static bool Object(BoundOperand o) =>
            Plain(o) is { } f && f.Place.Item.OperandPic?.Category is PicCategory.Numeric or PicCategory.NumericEdited;
        string? left = Bwz(r.Left) && Object(r.Right) ? RuntimeApi.EditIsBlanked(OperandText.AsString(r.Left, num)) : null;
        string? right = Bwz(r.Right) && Object(r.Left) ? RuntimeApi.EditIsBlanked(OperandText.AsString(r.Right, num)) : null;
        test = left is null ? right ?? "" : right is null ? left : $"{left} || {right}";
        return test.Length > 0;
    }

    /// <summary>The §8.8.4.2 comparison rules for a relation whose operands are neither references, boolean
    /// expressions nor figuratives — the collating-class dispatch and, by exhaustion, the numeric comparison.</summary>
    private string RenderRelationalCore(BoundRelational r, CollatingClass cmp, PicCategory? leftCat, PicCategory? rightCat)
    {
        // BOOLEAN relations (§8.8.4.2.2 Format 2 / §8.8.4.2.8): a VALUE comparison, usage-independent, the
        // shorter operand right-extended with boolean ZEROS — never the alphanumeric program collating
        // sequence (equality-only + class purity are bind-enforced, 0844).
        if (cmp is CollatingClass.Boolean)
            return $"CobolString.Compare({OperandText.AsString(r.Left, num)}, {OperandText.AsString(r.Right, num)}, pad: '0') {r.Op} 0";
        // NATIONAL relations (§8.8.4.2.9/.10): full ordering under the NATIONAL collating sequence — the
        // default is the UTF-16 code-unit ordinal (D-N3; NATIVE/UCS-4 are that same identity), and a
        // NON-native ALPHABET … FOR NATIONAL sequence rides ctx.NatCollateArg (§12.3.6 GR11). The
        // ALPHANUMERIC program collating sequence never applies (ctx.CollateArg — whose 256-entry weight
        // table would alias national chars through `& 0xFF` — is deliberately absent). A mixed alphanumeric
        // operand converts to national by the D-N4 Latin-1 identity (§8.8.4.2.6).
        if (cmp is CollatingClass.National)
            return $"CobolString.Compare({OperandText.AsString(r.Left, num, deSign: true)}, {OperandText.AsString(r.Right, num, deSign: true)}{ctx.NatCollateArg}) {r.Op} 0";
        if (OperandText.IsString(r.Left) || OperandText.IsString(r.Right))
            // An ALPHANUMERIC comparison (§8.8.4.2.7) under the alphanumeric program collating sequence. `cmp` is
            // necessarily Alphanumeric here — an IsString operand's category is never Numeric, so ForComparison's
            // both-numeric arm cannot be reached under this guard.
            // A signed numeric compared against an alphanumeric operand drops its sign (ISO §8.8.4.2.5 → §14.9.25.4 GR6a).
            return $"CobolString.Compare({OperandText.AsString(r.Left, num, deSign: true)}, {OperandText.AsString(r.Right, num, deSign: true)}{ctx.CollateArgFor(leftCat, rightCat)}) {r.Op} 0";
        return RenderNumericRelational(r);
    }

    /// <summary>The comparison of NUMERIC operands (ISO §8.8.4.2.4 — "a comparison is made with respect to the
    /// algebraic value of the operands regardless of the manner in which their usage is described"), reached by
    /// exhaustion from the collating dispatch and directly from §13.18.8.4 GR3's blanked-sender arm.</summary>
    private string RenderNumericRelational(BoundRelational r)
    {
        // Each side renders knowing the OTHER side's static scale (fix-queue PB60 / RV-15.68.4-1 half 2):
        // §8.8.4.2.4 compares ALGEBRAIC VALUES, so `IF FUNCTION NUMVAL-C(A) = 0.123456789` must see the
        // function's value at (at least) the literal's 9 fraction digits — the bare receiver-less context
        // floored the exact family at 6 and the relation agreed with a TRUNCATED value no channel should
        // hold. Receiverless STAYS TRUE (the float family keeps its deliberate binary64 compare above);
        // only the working-scale request carries the comparand.
        NumX l = num.AsNum(r.Left, ReceiverContext.None with { Scale = StaticScaleOf(r.Right) }),
             rr = num.AsNum(r.Right, ReceiverContext.None with { Scale = StaticScaleOf(r.Left) });
        // A float operand under NATIVE arithmetic (D16): compare the algebraic values natively in IEEE double
        // (§8.8.4.2.4 — "when native arithmetic is in effect, comparison proceeds by the rules of native
        // arithmetic"). IEEE NaN-unordered (every relation but != is false) and +0.0 == -0.0 fall out of C# —
        // spec-conformant, no epsilon. Under STANDARD-DECIMAL this branch is SKIPPED so the float lifts to SDIDI below.
        if ((l.Real || rr.Real) && !num.StandardDecimal)
            return $"{NumericRenderer.Real(l)} {r.Op} {NumericRenderer.Real(rr)}";
        // Under standard-decimal, §8.8.4.2.4 requires EACH operand converted to standard-decimal intermediate form
        // and compared decimally — a float lifts via the §8.8.1.5.1 float→SDIDI conversion (DecOperand →
        // CobolDec.FromDouble) and a fixed operand lifts EXACTLY (CobolDec.From), preserving decimal precision that a
        // native (double)-rounded compare would lose. A native STANDARD-DECIMAL intermediate (.Dec) also lands here.
        if (l.Dec || rr.Dec || l.Real || rr.Real)
            return $"CobolDec.Compare({num.DecOperand(l)}, {num.DecOperand(rr)}) {r.Op} 0";
        // An UNSIGNED-WIDE operand (a 16-byte unsigned COMP-5 read or the HIGHEST-ALGEBRAIC fold literal —
        // kb/Work R10) compares by algebraic VALUE over the full [0, 2^128) range: CobolNum.CompareU's overload
        // set covers U-vs-U and either mixed order, so the comparison never narrows through the Int128 funnel
        // (which would be loud for exactly the values this relation exists to test).
        if (l.U || rr.U)
            return $"{RuntimeApi.NumCompareU(l.Expr, $"{l.Scale}", rr.Expr, $"{rr.Scale}")} {r.Op} 0";
        int s = Math.Max(l.Scale, rr.Scale);
        // ⛔ DIFFERING SCALES COMPARE WITHOUT WIDENING (fix-queue PB65): aligning to the common scale first
        // wrapped silently past 38 aligned digits, and IF BIGV > SMLV over legal in-range items answered FALSE.
        // A comparison has a defined answer for every legal pair (§8.8.4.2.4), so it rides the exact
        // sign-split/magnitude compare — the same shape the unsigned lane above always used.
        if (l.Scale != rr.Scale)
            return $"{RuntimeApi.NumCompareScaled(NumericRenderer.Align(l, l.Scale), $"{l.Scale}", NumericRenderer.Align(rr, rr.Scale), $"{rr.Scale}")} {r.Op} 0";
        return $"{NumericRenderer.Align(l, s)} {r.Op} {NumericRenderer.Align(rr, s)}";
    }

    /// <summary>A relational comparison where one side is a figurative constant — it materializes to the other
    /// operand's category and width (ISO §8.3.3.6.4 r2 sizes it from the associated operand; §8.8.4.2.1
    /// treats a group anchor as an elementary alphanumeric item — kb/Work PB182 corrected the phantom
    /// §8.8.4.1.1 this used to cite): a numeric anchor → ZERO is 0; an alphanumeric/group anchor →
    /// the figurative is a string as long as the anchor's own value is.
    /// <para>⛔ THE WIDTH IS THE ANCHOR'S OWN RUNTIME LENGTH, NEVER A COMPILE-TIME TABLE (kb/Work PB297).
    /// This site used to carry an <c>AnchorWidth</c> switch over operand KINDS, and every arm that was not an
    /// unmodified elementary field was wrong: a <c>RefModPlace</c> field reported the BASE item's width where
    /// §8.4.3.3.4 GR5 makes the slice "a unique data item" of the ref-mod's own length (so <c>X(1:1) =
    /// LOW-VALUE</c> over <c>PIC X(2)</c> compared <c>"\0\0"</c> with <c>"\0"</c> → space-padded → FALSE), a
    /// computed operand fell through to the <c>_ =&gt; 1</c> default (so <c>FUNCTION UPPER-CASE(A) = ALL "AB"</c>
    /// compared one character), and with figuratives on BOTH sides each was sized to the OTHER's length instead
    /// of the §8.3.3.6.4 GR3 b/c length of its own. A ref-mod with computed bounds has no compile-time width at
    /// all, which is why no table could have been completed: the sizing belongs where the length is known, in
    /// <c>CobolString.CompareFig</c>.</para></summary>
    private string RenderFigurativeRelational(BoundRelational r)
    {
        static bool IsFig(BoundOperand o) => o is BoundFigurative or BoundAllLiteral;
        // A NON-NUMERIC figurative (SPACE/QUOTE/HIGH/LOW-VALUE — anything but ZERO) or an ALL "literal" makes the
        // comparison ALPHANUMERIC even against a numeric item (ISO §8.8.4.2.1 — the figurative is alphanumeric
        // class, so the numeric operand participates via its character image, at its own width).
        static bool NonNumericFig(BoundOperand o) => o is BoundFigurative { Kind: not 'Z' } or BoundAllLiteral;
        BoundOperand anchor = IsFig(r.Left) ? r.Right : r.Left;
        if (IsFig(anchor) || OperandText.IsString(anchor) || NonNumericFig(r.Left) || NonNumericFig(r.Right))
        {
            // ⛔ ONE COMPARISON-CLASS DECISION, MADE FROM BOTH OPERANDS, SHARED WITH THE NON-FIGURATIVE LEG
            // (see RenderRelationalCore's caller — the same call). This branch used to derive the pair's class
            // from the ANCHOR alone and then hand the same answer back to itself as the figurative's category,
            // so CollatingSelection.ForComparison was asked a two-operand question with one operand's answer
            // twice (kb/Work PB649). With an ALPHANUMERIC anchor opposite a NATIONAL figurative that chose the
            // ALPHANUMERIC program collating sequence: under `ALPHABET AL IS "ZYX…A"` as the PCS,
            // `IF ALL N"AB" < XA` answered 0 while `IF NB < XA` over the identical values answered 1 — the
            // same comparison, opposite answers, because §8.8.4.2.6 ("the alphanumeric operand is treated as
            // though it were converted and moved … to a temporary elementary data item of class national")
            // makes ONE national operand enough to make the whole comparison national.
            var (leftCat, rightCat) = RelationCategories(r.Left, r.Right);
            CollatingClass cmp = CollatingSelection.ForComparison(leftCat, rightCat);
            // A boolean/national comparison exempts the ALPHANUMERIC program collating sequence: boolean
            // comparisons are value comparisons (§8.8.4.2.8) and national comparisons order under the NATIONAL
            // sequence (§8.8.4.2.9 — the D-N3 ordinal identity, or __COLLATE_NAT under a non-native
            // ALPHABET … FOR NATIONAL; the alphanumeric 256-entry weight table would alias national chars
            // through `& 0xFF`). CollateArgFor asks ForComparison over the SAME pair, so the collation and the
            // class can no longer disagree.
            string collate = ctx.CollateArgFor(leftCat, rightCat);
            // A boolean comparison right-extends the shorter operand with boolean ZEROS (§8.8.4.2.8) — the same
            // pad the direct-relation and level-88 legs thread; pad and collate never coexist (the boolean arm
            // of CollateArgFor is empty). The figurative materializes category-aware (national/boolean
            // HIGH/LOW-VALUE = the category's own sequence — the explicit national PCS extremes when one is
            // declared, else the D-N3 pin — never the alphanumeric PCS extreme).
            string pad = cmp is CollatingClass.Boolean ? ", pad: '0'" : "";
            // BOTH sides figurative — there is no associated data item, so §8.3.3.6.4 GR2 does not apply and
            // GR3 gives each operand its OWN length: one character for a plain figurative word (GR3 b) and
            // literal-1's length for ALL literal-1 (GR3 c). That is exactly each side's SEED, unrepeated.
            // Each side is seeded in ITS OWN category: §8.3.3.6.3 SR2 gives ALL literal-1 its literal's class,
            // and a plain figurative word takes its context's, which RelationCategories resolved.
            if (IsFig(r.Left) && IsFig(r.Right))
                return $"{RuntimeApi.StrCompare(FigSeed(r.Left, leftCat), FigSeed(r.Right, rightCat), pad + collate)} {r.Op} 0";
            // Exactly one side figurative — GR2 repeats its seed to the ASSOCIATED operand's own character-position
            // count, which the runtime reads off that operand's rendered value (§8.4.3.3.4 GR5: a ref-modified
            // operand's positions are the slice's, and with computed bounds they exist only at runtime).
            bool figLeft = IsFig(r.Left);
            // ⛔ deSign, exactly as the direct string relation above does it — the SAME §8.8.4.2.5 comparison,
            // and this arm was the one that never got it (the two-arm dispatch shape; kb/Work PB741 sweep).
            // §8.8.4.2.5 moves the numeric integer operand to an item "of the same length in terms of character
            // positions as the NUMBER OF DIGITS in the integer", and §14.9.25.4 GR6a governs that move: "If the
            // sending operand is described as being signed numeric, the operational sign is not moved; if the
            // operational sign occupies a separate character position, that character is not moved and the size
            // of the sending operand is considered to be one less than its actual size." So `IF S9 < SPACE` over
            // PIC S9 compares "9", never an overpunched or sign-carrying image. A no-op for every non-signed-
            // numeric anchor (alphanumeric, numeric-edited — whose EDITED sign is part of its image and stays,
            // §8.8.4.2.1 NOTE — national, boolean), which is why it is unconditional here as it is above.
            string fig = FigSeed(figLeft ? r.Left : r.Right, figLeft ? leftCat : rightCat),
                   other = OperandText.AsString(anchor, num, deSign: true);
            return $"{RuntimeApi.StrCompareFig(figLeft ? fig : other, figLeft ? other : fig, figLeft, pad + collate)} {r.Op} 0";
        }
        NumX l = FigOrNum(r.Left), rr = FigOrNum(r.Right);
        if (l.Real || rr.Real)   // a float vs ZERO figurative — native IEEE compare (D16, §8.8.4.2.4)
            return $"{NumericRenderer.Real(l)} {r.Op} {NumericRenderer.Real(rr)}";
        int s = Math.Max(l.Scale, rr.Scale);
        return $"{NumericRenderer.Align(l, s)} {r.Op} {NumericRenderer.Align(rr, s)}";
    }

    /// <summary>An operand's STRING data category for the relation dispatch. ⛔ THE BODY MOVED to
    /// <see cref="CollatingSelection.OperandCategory"/> (kb/Work PB398): §14.9.13.3 SR3 asks the SAME question at
    /// BIND time — "the literals or identifiers specified in the THROUGH phrase are of class alphabetic,
    /// alphanumeric, or national" — and a second copy in the binder would be two readings of one category rule,
    /// the shape kb/Work PB728/PB741 already paid for once. This alias stays so the renderer's many call sites
    /// keep reading as the renderer's own question.</summary>
    private static PicCategory? StringCategoryOf(BoundOperand o) => CollatingSelection.OperandCategory(o);

    /// <summary>⛔ THE PAIR OF CATEGORIES A RELATION'S COMPARISON CLASS IS CHOSEN FROM — written down ONCE and
    /// consumed by BOTH relation legs (<see cref="RenderRelationalCore"/>'s caller and
    /// <see cref="RenderFigurativeRelational"/>), so neither can decide half of §8.8.4.2 from one operand.
    /// <para>The only operand shape with no category of its own is a plain figurative WORD, and ISO §8.3.3.6.4
    /// GR1 gives it exactly THREE readings, not its neighbour's category: "When a figurative constant is used
    /// in a context requiring national characters, the figurative constant represents a national character
    /// value … Otherwise, when a figurative constant represents a character value, the figurative constant
    /// represents an alphanumeric character value" — plus GR4's boolean reading of ZERO, the one figurative
    /// with a boolean form. So <see cref="FigurativeCategory"/> maps the context to National, Boolean or
    /// ALPHANUMERIC, and a NUMERIC neighbour yields alphanumeric, which is precisely §8.8.4.2.5's case: the
    /// numeric integer operand is moved to an item "of the same class and usage as the alphanumeric … operand"
    /// and §8.8.4.2.7 then collates the pair under the alphanumeric program collating sequence.
    /// ⛔ Inheriting the raw category instead would make <c>IF N9 &lt; SPACE</c> a NUMERIC comparison and drop
    /// the program collating sequence from it — kb/Work PB741's regression, NIST NC215A SEQ-TEST-GF-6/-7, which
    /// this file's own drift test caught within one gate of the attempt.</para>
    /// <para>⚠ <c>ALL literal-1</c> does NOT take its context: §8.3.3.6.3 SR2 — "Literal-1 shall be an
    /// alphanumeric, boolean, or national literal" — gives it its literal's own class, which
    /// <see cref="StringCategoryOf"/> already reports, and treating it as context-less is exactly how
    /// <c>ALL N"AB"</c> came to be compared under the ALPHANUMERIC program collating sequence (kb/Work
    /// PB649).</para>
    /// <para>⚠ A NON-figurative operand with no category — an error node, a picture-less leaf — is left null
    /// and NOT given its neighbour's: <c>CollatingSelection.ForComparison</c> reads null as the alphanumeric
    /// branch, which is the documented fail-open direction.</para></summary>
    private static (PicCategory? Left, PicCategory? Right) RelationCategories(BoundOperand left, BoundOperand right)
    {
        PicCategory? l = StringCategoryOf(left), r = StringCategoryOf(right);
        return (l ?? (left is BoundFigurative ? FigurativeCategory(r) : null),
                r ?? (right is BoundFigurative ? FigurativeCategory(l) : null));
    }

    /// <summary>ISO §8.8.4.2's category of a figurative constant WORD standing opposite an operand of
    /// <paramref name="context"/> — §8.3.3.6.4 GR1's national-or-alphanumeric split, with GR4's boolean reading
    /// of ZERO (the class mix is bind-rejected COBOLNET0844, so only ZERO reaches a boolean context).</summary>
    private static PicCategory FigurativeCategory(PicCategory? context) =>
        context is PicCategory.National or PicCategory.Boolean ? context.Value : PicCategory.Alphanumeric;

    /// <summary>Read a relation operand as a '0'/'1' boolean string (for a boolean-expression relation): a
    /// boolean expression via <see cref="BooleanRenderer"/>, a boolean field via its <c>Place.Read()</c>, a
    /// boolean literal via its value, and figurative ZERO as "0" (CobolBool.Equal zero-extends it to the other
    /// operand's width — §8.3.3.6.4 GR4 boolean zeros).</summary>
    private string BoolRead(BoundOperand o) => o switch
    {
        BoundBoolOperand b => BooleanRenderer.Render(b.Expr, num),
        // A bit GROUP's boolean value is its bit string (AsBits — OperandText's as-if arm), not the struct (D20/PB79).
        BoundFieldOperand f => f.Place.Item.IsAsIfElementary ? OperandText.FieldImage(f.Place) : PlaceRenderer.Read(f.Place),
        BoundStringLiteral { Category: PicCategory.Boolean } s => EmitText.CsLiteral(s.Value),
        BoundFigurative { Kind: 'Z' } => "\"0\"",
        // A boolean-result function reference — its '0'/'1' image through the ONE string channel (kb/Work PB68).
        BoundComputedOperand { Expr: BoundIntrinsicCall { ResultCategory: PicCategory.Boolean } } => OperandText.AsString(o, num),
        _ => EmitText.LoudValue("string", $"boolean relation operand '{o.GetType().Name}'"),
    };

    /// <summary>A figurative operand's SEED — the string §8.3.3.6.4 GR2 repeats to the associated operand's
    /// character-position count, and (unrepeated) the whole of its GR3 b/c value when nothing sizes it: ONE fill
    /// character for a figurative word, literal-1 for <c>ALL literal-1</c>. PCS-aware for alphanumeric anchors —
    /// HIGH-/LOW-VALUE are the program sequence's extreme characters (§8.3.3.6.4 GR6/GR7) — while a
    /// national/boolean anchor reads its OWN sequence (the D-N3 pin), through the ONE fill service.
    /// ⛔ No width here by construction: sizing is <c>CobolString.CompareFig</c>'s (kb/Work PB297).</summary>
    private string FigSeed(BoundOperand op, PicCategory? anchorCat) => op switch
    {
        BoundFigurative f => FigurativeConstants.FillText(f.Kind, ctx.Data.Collating, anchorCat, ctx.Data.NationalCollating),
        BoundAllLiteral a => EmitText.CsLiteral(a.Literal),
        // Unreachable by construction — both call sites select an operand that already satisfied IsFig. Loud
        // rather than a silent fall-through to the operand's own text, which would look like a working
        // comparison while sizing nothing (the failure shape this whole change exists to remove).
        _ => EmitText.LoudValue("string", $"figurative seed for '{op.GetType().Name}'"),
    };

    private NumX FigOrNum(BoundOperand op) => op switch
    {
        BoundFigurative { Kind: 'Z' } => EmitText.UnscaledLit("0"),
        BoundFigurative f => new NumX(EmitText.LoudValue("long", $"figurative '{f.Kind}' in a numeric comparison"), 0),
        _ => num.AsNum(op, ReceiverContext.None),
    };

    private string RenderSign(BoundSignCondition s)
    {
        // §14.6.13.2 rule 3 dash-2: a float sending item referenced in a SIGN condition is EXEMPT from
        // EC-DATA-NOT-FINITE — render the whole operand sub-tree with the finiteness wrap suppressed (a NaN/±Inf
        // sign test is well-defined: NaN is neither >0, <0, nor ==0, so a compound sibling like `AND Y > 0.0`
        // still raises on its own read).
        // ⛔ RULE 2 HAS NO SUCH DASH, and SendingRef is what lets the two lists differ (kb/Work PB230): a
        // FIXED-POINT sending item in a sign condition IS still checked for EC-DATA-INCOMPATIBLE. The asymmetry
        // is the standard's own — §8.8.4.7.4 GR2 gives a float sign test a defined answer for NaN by reading the
        // IEEE sign bit, and there is no corresponding rule making `IF N IS POSITIVE` meaningful over digits that
        // are not digits.
        NumX v = num.Render(s.Expr, ReceiverContext.None, SendingRef.SignCondition);
        // §8.8.4.7.4 GR2 (Format 2 — a bare standard-float name): POSITIVE/NEGATIVE test the IEEE sign BIT, not the
        // algebraic value, "regardless of whether the content would evaluate to true in a NUMERIC class test or a
        // ZERO sign test" — so +0.0 IS POSITIVE and −0.0 IS NEGATIVE. double.IsNegative reads the sign bit (true for
        // −0.0 and a negative-signed NaN; false for +0.0). ZERO (GR2c) is sign-agnostic. Format 1 keeps the algebraic
        // test. Widening FLOAT-SHORT→double preserves the sign of zero and NaN, so the single-precision case is covered.
        string test = s.Format2Float
            ? s.Kind switch { 'P' => $"!double.IsNegative({NumericRenderer.Real(v)})", 'N' => $"double.IsNegative({NumericRenderer.Real(v)})", _ => $"{NumericRenderer.Real(v)} == 0.0" }
            // An unsigned-wide operand (kb/Work R10) tests its sign by VALUE over the full range via CompareU —
            // C# defines no UInt128-vs-int operator, and the Widen funnel would be loud for exactly the large
            // values a sign test must accept. (NEGATIVE is structurally false for an unsigned item; the compare
            // form keeps the three kinds one mechanism.)
            // (The zero is cast — an int constant converts implicitly to BOTH Int128 and UInt128, and the
            // uncast form is a CS0121 ambiguity in the generated code.)
            : v.U
            ? s.Kind switch { 'P' => $"{RuntimeApi.NumCompareU(v.Expr, "0", "(Int128)0", "0")} > 0", 'N' => $"{RuntimeApi.NumCompareU(v.Expr, "0", "(Int128)0", "0")} < 0", _ => $"{RuntimeApi.NumCompareU(v.Expr, "0", "(Int128)0", "0")} == 0" }
            // An SDIDI intermediate (§8.8.1.5.2 — every STANDARD-DECIMAL arithmetic expression, and under native
            // arithmetic an integer power, kb/Work PB69) tests the sign of its significand: exact at every
            // exponent, and never a landing that could overflow (kb/Work PB84 — `IF 9 ** TWO + (180 - 90) IS
            // NOT POSITIVE`, NIST NC250A, was a Roslyn CS0019 on `CobolDec > 0`).
            : v.Dec
            ? s.Kind switch { 'P' => $"{RuntimeApi.DecSign(v.Expr)} > 0", 'N' => $"{RuntimeApi.DecSign(v.Expr)} < 0", _ => $"{RuntimeApi.DecSign(v.Expr)} == 0" }
            : s.Kind switch { 'P' => $"{v.Expr} > 0", 'N' => $"{v.Expr} < 0", _ => $"{v.Expr} == 0" };
        return s.Negated ? $"!({test})" : $"({test})";
    }

    /// <summary>A class condition (ISO §8.8.4.4). A typed-numeric operand IS NUMERIC folds to <c>true</c> ONLY
    /// when its storage is the native long/Int128 (it can only hold digits — COBOLNET_DESIGN §6.6); a numeric item
    /// whose storage is a CHARACTER window (a REDEFINES view, or a whole-group-aliased StoreAsImage leaf) can hold
    /// arbitrary characters and tests its image at run time — sign-aware for a signed zoned item (§8.8.4.4.4 GR3 n)1.a,
    /// NC174A CLASS-TEST-GF-8/10: S9(18) REDEFINES X(18) holding letters is NOT numeric).
    /// <para>⛔ A WINDOWED numeric leaf tests through <c>CobolNum.IsNumericImage</c> over its RAW WINDOW — the ONE
    /// §8.8.4.4.4 GR3 n)1 predicate, which §14.6.13.2 rule 2's checked sending read also calls, because the standard
    /// defines rule 2's test BY REFERENCE to this one ("would evaluate to false in a numeric class condition").
    /// Writing that rule twice is how the two answers drift, so it is written once (kb/Work PB230).</para>
    /// <para>THE RAW WINDOW IS THE POINT, and it is what the previous form could not reach: <c>arg</c> is
    /// <c>OperandText.AsString</c>, which for a NON-ZONED window DECODES the bytes and re-renders them as a
    /// DISPLAY image — so <c>IsNumeric(arg)</c> was asking whether a reformatted image is all digits, which it
    /// always is. A packed window with a non-decimal nibble, and a binary window whose value exceeds its
    /// PICTURE's range, were both reported NUMERIC; GR3 n)1.c asks instead for "a valid representation for the
    /// usage" and "the numeric value is within the range of values implied by the PICTURE clause", and only the
    /// undecoded bytes can answer either. The ZONED case is unchanged in behaviour: there the window IS its text,
    /// and the predicate delegates straight back to the same two <c>CobolClass</c> helpers this arm used to
    /// spell inline — so the sign-aware answer NC174A pins is the same code it always was.</para>
    /// <para>⛔ WHICH of GR3 n)'s two tests applies is decided by the OPERAND's category, read through THE ONE
    /// operand-category reader (<see cref="StringCategoryOf"/>), never by the ITEM's picture and never by a
    /// place-kind exclusion (kb/Work PB823). §8.4.3.3.4 GR6 c) makes a reference-modified numeric item "class and
    /// category alphanumeric" (national when its usage is national) and <c>RefModPlace.Category</c> is where that
    /// rewrite lives, so <c>S (1:4)</c> over <c>PIC S9(4)</c> reaches n) 2.'s all-digits test and the over-punched
    /// image "123M" is NOT numeric. The previous chain excluded the ref-mod shape from the WINDOWED arm only and
    /// then asked the ITEM's picture, so the slice fell into the typed-field fold (<c>true</c>) or, over a
    /// StoreAsImage base, a sign-admitting n) 1. test — both answers about the item, not the operand.</para></summary>
    private string RenderClass(BoundClassCondition c)
    {
        // §8.8.4.4.4 GR3 n) 1. versus n) 2. is a question about the OPERAND's category (kb/Work PB823): a
        // reference-modified slice answers alphanumeric / national here (§8.4.3.3.4 GR6 c), whatever its base
        // item is, and a bit / national group answers its as-if category (§13.18.29.4 GR1 b)/GR2 b), never numeric.
        // ⛔ ASKED OF EVERY OPERAND SHAPE, NOT ONLY A DATA-ITEM REFERENCE (kb/Work PB1401 / PB1466): §8.5.2.12 items
        // 3–7 make the three counter registers and every numeric / integer function category-numeric data items,
        // and they bind as COMPUTED operands. This renderer used to ask the category of a BoundFieldOperand only,
        // so a register was sent down the n) 2. string arm (a run-time NotImplemented) and FARTHEST-FROM-ZERO /
        // NEAREST-TO-ZERO / IN-ARITHMETIC-RANGE dereferenced a null field (a compiler crash), while
        // `FUNCTION NUMVAL("1.5") IS NUMERIC` applied n) 2.'s all-digits test to the text "1.5" and answered FALSE.
        bool numericCategory = StringCategoryOf(c.Operand) is PicCategory.Numeric;
        if (numericCategory && c.Operand is BoundComputedOperand computed)
            return RenderComputedNumericClass(computed, c.ClassKind, c.Negated);
        var fld = c.Operand as BoundFieldOperand;
        // Total over the operand kinds: a numeric-category operand is a data item or a computed one (above). No
        // other shape reaches identifier-1 with that category, and one that ever did is loud here, never a null
        // dereference in the arms below.
        if (numericCategory && fld is null)
            return EmitText.LoudValue("bool", $"numeric class condition over a '{c.Operand.GetType().Name}' operand");
        // A numeric OPERAND is a whole numeric data item. Its storage is either the native long/Int128, which can
        // only hold a valid value (the fold to true below), or a CHARACTER WINDOW (a REDEFINES view, a whole-
        // group-aliased StoreAsImage leaf), which is tested at run time by the ONE n) 1. predicate over the raw
        // window — its NumProfile carries the item's sign presentation and the compilation's --sign-encoding, so
        // no sign convention is re-spelled here.
        // ⛔ THE FOLD IS SOUND ONLY BECAUSE A NATIVE CARRIER IS NEVER WRITTEN BY A CHARACTER CHANNEL (kb/Work
        // PB1466): StorageFormPass.IsImagePromotable image-stores every DISPLAY, BINARY and PACKED leaf that a group
        // MOVE, a READ or a BY REFERENCE crossing reaches, so the only content a native leaf can hold is a value a
        // numeric store put there. It used to promote DISPLAY alone, and this fold answered TRUE over a packed field
        // READ with nibbles A–F. (A national-form numeric leaf is not yet promoted — the carriage question that
        // method records.)
        bool windowedNumeric = numericCategory && (fld!.Place is RedefViewPlace || fld.Place.Item.StoreAsImage);
        // §14.6.13.2 dash-1 of rules 1, 2 AND 3: a sending item referenced in a CLASS condition is EXEMPT from
        // every one of them — the class test inspects the content precisely in order to CATEGORIZE it, so raising
        // on the very content it was asked to report would leave it unable to answer.
        string arg = OperandText.AsString(c.Operand, num, sending: SendingRef.ClassCondition);
        string numericTest = numericCategory
            // §8.8.4.4.4 GR3 n) 1. b. — "If the usage … is any standard floating-point usage, the condition is true
            // only if the content … represents a finite numeric value" (kb/Work PB225): a FLOAT-BINARY-64 holding
            // an infinity or a NaN is NOT numeric, so the typed-field fold below would answer it wrongly.
            ? IsStandardFloat(fld!) ? FloatTest(fld!, CobolNet.Runtime.FloatClassTest.Finite)
            : windowedNumeric
                ? RuntimeApi.NumIsNumericImage(PlaceRenderer.Read(fld!.Place), fld.Place.Item.ProfileName)
                : "true"
            // §8.8.4.4.4 GR3 n) 2. — a NON-numeric-category operand (alphanumeric / edited / national, a ref-mod
            // slice included) is numeric iff its content "consists entirely of the characters 0, 1, 2, 3, …, 9",
            // with no operational sign admitted.
            : $"CobolClass.IsNumeric({arg})";
        // ALPHABETIC / -UPPER / -LOWER under a CHARACTER CLASSIFICATION (ISO §8.8.4.4.4 GR3 b1/c1/d1 — the classification
        // locale's LC_CTYPE, resolved at the module's activation into __CLASSIFY; kb/Work PB64 T5); without one the
        // coded-character-set rule (b2/c2/d2 — the closed Latin set) stands, exactly as before.
        // WHICH of the two classifications (§12.3.6.4 GR5 a)–e) alphanumeric, f)–j) national) is asked of the
        // ONE selector GR7 a)'s consumer, UPPER-CASE / LOWER-CASE, also calls (kb/Work PB760) — never a local test.
        string classify = ctx.Data.Classification is not null ? ObjectComputerEmit.ClassificationArg(c.Operand) : "";
        string test = c.ClassKind switch
        {
            'N' => numericTest,
            'A' => $"CobolClass.IsAlphabetic({arg}{classify})",
            'U' => $"CobolClass.IsAlphabeticUpper({arg}{classify})",
            'L' => $"CobolClass.IsAlphabeticLower({arg}{classify})",
            // §8.8.4.4.4 GR3 e) — "If BOOLEAN is specified, the condition is true if the content of the data
            // item referenced by identifier-1 consists entirely of the boolean values '0' and '1'." No
            // classification argument: LC_CTYPE governs the three ALPHABETIC forms (GR3 b1/c1/d1) and names no
            // boolean category, and the boolean values are the two characters of the D-B1 substrate, not a
            // locale's letters. CobolClass.IsBoolean is that scan, with §8.8.4.4.4 GR1's zero-length FALSE on
            // it — the same predicate §14.6.13.2 rule 1 reads through HasNonBooleanPosition, which is why the
            // two do not share an answer at zero length (kb/Work PB590).
            'B' => RuntimeApi.ClassIsBoolean(arg),
            // §8.8.4.4.4 GR3 h)–k) — the four IEEE special-value tests, over a standard floating-point operand
            // (SR7 has screened every other usage out at bind). Decided on the carrier's raw bits (CobolFloatClass).
            ClassConditionModel.FloatInfinity => FloatTest(fld!, CobolNet.Runtime.FloatClassTest.Infinity),
            ClassConditionModel.FloatNotANumber => FloatTest(fld!, CobolNet.Runtime.FloatClassTest.NotANumber),
            ClassConditionModel.FloatNotANumberQuiet => FloatTest(fld!, CobolNet.Runtime.FloatClassTest.QuietNaN),
            ClassConditionModel.FloatNotANumberSignaling =>
                FloatTest(fld!, CobolNet.Runtime.FloatClassTest.SignalingNaN),
            // §8.8.4.4.4 GR3 g) / m) / l) — the three numeric-content tests over a numeric-category operand (SR6).
            ClassConditionModel.FarthestFromZero or ClassConditionModel.NearestToZero =>
                RenderExtremeClass(fld!, c.ClassKind is ClassConditionModel.FarthestFromZero, numericTest),
            ClassConditionModel.InArithmeticRange => RenderInArithmeticRange(fld!, numericTest),
            _ => EmitText.LoudValue("bool", "class condition"),
        };
        return c.Negated ? $"!({test})" : $"({test})";
    }

    /// <summary>Is the operand described with a STANDARD floating-point usage (§3.166 / §3.167 — the
    /// FLOAT-BINARY and FLOAT-DECIMAL families, read from the ONE place they are written down).</summary>
    private static bool IsStandardFloat(BoundFieldOperand f) =>
        f.Place.Item.OperandPic is { } p && UsageFamilies.IsStandardFloat(p.Usage);

    /// <summary>One floating-point class question over the operand's OWN carrier: its typed <c>float</c>/<c>double</c>
    /// field, or — for a float stored as its IEEE window (a REDEFINES view, a whole-group-aliased leaf) — the
    /// window's raw bits. Never a decoded or widened value (see <c>CobolFloatClass</c>).</summary>
    private static string FloatTest(BoundFieldOperand f, CobolNet.Runtime.FloatClassTest test) =>
        f.Place is RedefViewPlace || f.Place.Item.StoreAsImage
            ? RuntimeApi.FloatClassImage(PlaceRenderer.Read(f.Place), f.Place.Item.ProfileName, test)
            : RuntimeApi.FloatClass(PlaceRenderer.Read(f.Place), test);

    /// <summary>ISO §8.8.4.4.4 GR3 g) FARTHEST-FROM-ZERO — "the numeric value farthest from zero that may be
    /// contained in that data item, whether that value is positive or negative" — and m) NEAREST-TO-ZERO, "the
    /// nonzero numeric value nearest to zero that may be contained in that data item, whether that value is positive
    /// or negative" (kb/Work PB225).
    /// <para>⛔ THE EXTREMES ARE <see cref="AlgebraicRanges"/>'s, NOT A THIRD COPY. The same quantity already has
    /// two surfaces — the §15.43/§15.83 HIGHEST-/SMALLEST-ALGEBRAIC intrinsics and SET Format 15's
    /// FARTHEST-FROM-ZERO / NEAREST-TO-ZERO (§14.9.39.4 GR32 a)/GR36 a), which Annex D.32 equates) — and the class
    /// condition is the third question about the one value, so a SET CONTENT OF X TO FARTHEST-FROM-ZERO followed by
    /// IF X IS FARTHEST-FROM-ZERO is TRUE by construction. A float carrier is asked on its own bits instead
    /// (<see cref="CobolNet.Runtime.CobolFloatClass"/>), which are the same extremes AlgebraicRanges states for it.</para>
    /// <para>⚠ DETERMINATION: "whether that value is positive or negative" is read as EITHER direction's extreme
    /// — the positive one and, when the item can hold a sign, the negative one — not only the single value of
    /// greatest magnitude. The readings differ only for a two's-complement container (§13.18.60.4 GR12: PIC S9(4)
    /// COMP-5 spans −32768..32767), and the either-direction reading is the one that keeps SET CONTENT … TO
    /// FARTHEST-FROM-ZERO SIGN POSITIVE (GR32 a)'s value "in the direction the SIGN phrase selects") answering TRUE
    /// here.</para>
    /// <para>Each extreme is compared through the ONE relation-condition renderer — an algebraic comparison
    /// (§8.8.4.2.4), so a scaled, P-scaled or binary-capacity item needs no arithmetic of its own here — and it
    /// is guarded by the operand's own NUMERIC test, because a windowed item holding non-numeric bytes has no numeric
    /// content to be an extreme.</para></summary>
    private string RenderExtremeClass(BoundFieldOperand f, bool farthest, string numericTest)
    {
        var pic = f.Place.Item.OperandPic!;
        if (pic.IsFloat)
            return FloatTest(f, farthest ? CobolNet.Runtime.FloatClassTest.FarthestFromZero
                                              : CobolNet.Runtime.FloatClassTest.NearestToZero);
        if (AlgebraicRanges.Of(pic) is not { } range)
            return EmitText.LoudValue("bool", $"{(farthest ? "FARTHEST-FROM-ZERO" : "NEAREST-TO-ZERO")} over a description with no numeric capacity");
        // The negative extreme exists only where the description can hold a sign (AlgebraicRange.FarthestNegative
        // null otherwise) — and it is NOT always the mirror of the positive one (a two's-complement container).
        string?[] values = farthest
            ? [range.Farthest, range.FarthestNegative]
            : [range.Nearest, range.FarthestNegative is null ? null : "-" + range.Nearest];
        string equal = string.Join(" || ", values.OfType<string>().Select(v =>
            RenderRelational(new BoundRelational(f, "==", new BoundNumericLiteral(v)))));
        return numericTest == "true" ? $"({equal})" : $"({numericTest} && ({equal}))";
    }

    /// <summary>ISO §8.8.4.4.4 GR3 l) IN-ARITHMETIC-RANGE — "the numeric content of the data item referenced by
    /// identifier-1 is neither farther from zero nor closer to zero than is permitted for the form of an intermediate
    /// data item appropriate to the mode of arithmetic in effect" (kb/Work PB225).
    /// <para>The mode's intermediate extremes are <see cref="ArithmeticModes.IntermediateExtremes"/> — the table
    /// SET Format 15's IN-ARITHMETIC-RANGE phrase (§14.9.39.4 GR32 b)/GR36 b)) already clamps against — and the
    /// item's own are <see cref="AlgebraicRanges"/>'s. Where the item's whole range lies inside the intermediate's
    /// (compared EXACTLY, as scaled decimal text), every numeric value the item can hold is in range, and the rule
    /// reduces to "the content IS a numeric value": finite for a float carrier (zero is permitted — the
    /// intermediate holds zero exactly, so "closer to zero than is permitted" cannot be zero itself), the operand's
    /// own NUMERIC test for a fixed-point one. That containment holds for every description this compiler can
    /// declare under every mode it accepts — binary64 is the widest carrier and its extremes ARE the native
    /// intermediate's — which is the same measurement SetBinder.ClampToArithmeticRange records. A description
    /// that ever escapes it is rendered LOUD here rather than guessed at, so the day a wider carrier lands this arm
    /// says so instead of answering.</para></summary>
    private string RenderInArithmeticRange(BoundFieldOperand f, string numericTest)
    {
        var pic = f.Place.Item.OperandPic!;
        if (AlgebraicRanges.Of(pic) is not { } range)
            return EmitText.LoudValue("bool", "IN-ARITHMETIC-RANGE over a description with no numeric capacity");
        var (modeFarthest, modeNearest) = ArithmeticModes.IntermediateExtremes(ctx.Data.Options.Arithmetic);
        bool contained = AlgebraicRanges.CompareMagnitude(range.Farthest, modeFarthest) <= 0
            && (range.FarthestNegative is null || AlgebraicRanges.CompareMagnitude(range.FarthestNegative, modeFarthest) <= 0)
            && (range.Nearest is null || AlgebraicRanges.CompareMagnitude(range.Nearest, modeNearest) >= 0);
        if (!contained)
            return EmitText.LoudValue("bool", "IN-ARITHMETIC-RANGE over a description wider than the intermediate data item");
        return pic.IsFloat ? FloatTest(f, CobolNet.Runtime.FloatClassTest.Finite) : numericTest;
    }

    /// <summary>The numeric class conditions over a COMPUTED category-numeric operand — a LINAGE-COUNTER,
    /// LINE-COUNTER or PAGE-COUNTER register, or a numeric / integer function-identifier (ISO §8.5.2.12 items 3–7;
    /// kb/Work PB1401, PB1466). Such an operand has a VALUE and no storage, so every question is asked of the value,
    /// and of that value ONCE — the runtime helpers take it as one argument, so a function with an effect (RANDOM's
    /// seed, an EC-ARGUMENT-FUNCTION raise) runs exactly as often as it is written, which a pair of relation
    /// conditions over the operand would not guarantee.
    /// <list type="bullet">
    /// <item>A binary64 value (the float function family, DOC-A.1-92) is asked on its own bits by
    /// <c>CobolFloatClass</c>, as a float data item is: NUMERIC is §8.8.4.4.4 GR3 n) 1.'s "valid representation" (a
    /// finite value), FARTHEST-FROM-ZERO / NEAREST-TO-ZERO are binary64's extremes, and IN-ARITHMETIC-RANGE is
    /// finiteness because binary64's range lies inside every mode's intermediate (the containment
    /// <see cref="RenderInArithmeticRange"/> states for a float item).</item>
    /// <item>Every other value is lifted EXACTLY into the decimal form (<c>NumericRenderer.DecOperand</c>) and asked
    /// there: NUMERIC is GR3 n) 1. c. — true, the content being a value its carrier holds; g) / m) compare it with
    /// the extremes of the data item the operand references (<see cref="AlgebraicRanges.OfCounterRegister"/> /
    /// <see cref="AlgebraicRanges.OfFunctionReturnedValue"/>); l) compares its magnitude with the mode's
    /// intermediate extremes (<see cref="ArithmeticModes.IntermediateExtremes"/>) — the rule's own words, "neither
    /// farther from zero nor closer to zero than is permitted for the form of an intermediate data item".</item>
    /// </list>
    /// The other alternatives cannot reach here: §8.8.4.4.3 SR3–SR5 and SR7 refuse a numeric operand for each of
    /// them at bind (<see cref="ClassConditionModel"/>), and an arm that is reached anyway is loud.</summary>
    private string RenderComputedNumericClass(BoundComputedOperand op, char kind, bool negated)
    {
        // §14.6.13.2 dash-1 of rules 1–3: a class condition's operand is exempt from the checked sending reads.
        NumX v = num.AsNum(op, ReceiverContext.None, SendingRef.ClassCondition);
        string test;
        if (v.Real)
        {
            string carrier = NumericRenderer.Real(v);
            test = kind switch
            {
                ClassConditionModel.Numeric or ClassConditionModel.InArithmeticRange =>
                    RuntimeApi.FloatClass(carrier, CobolNet.Runtime.FloatClassTest.Finite),
                ClassConditionModel.FarthestFromZero =>
                    RuntimeApi.FloatClass(carrier, CobolNet.Runtime.FloatClassTest.FarthestFromZero),
                ClassConditionModel.NearestToZero =>
                    RuntimeApi.FloatClass(carrier, CobolNet.Runtime.FloatClassTest.NearestToZero),
                _ => EmitText.LoudValue("bool", $"class condition '{kind}' over a computed numeric operand"),
            };
        }
        else
        {
            string dec = num.DecOperand(v);
            test = kind switch
            {
                ClassConditionModel.Numeric => RuntimeApi.ValueClassIsNumeric(dec),
                ClassConditionModel.FarthestFromZero or ClassConditionModel.NearestToZero =>
                    ComputedExtremeTest(op, v, dec, kind is ClassConditionModel.FarthestFromZero),
                ClassConditionModel.InArithmeticRange => ComputedInArithmeticRangeTest(dec),
                _ => EmitText.LoudValue("bool", $"class condition '{kind}' over a computed numeric operand"),
            };
        }
        return negated ? $"!({test})" : $"({test})";
    }

    /// <summary>§8.8.4.4.4 GR3 g) / m) over a computed operand's decimal value — its extremes read from the data item
    /// the operand references (the either-direction reading <see cref="RenderExtremeClass"/> documents).</summary>
    private string ComputedExtremeTest(BoundComputedOperand op, NumX v, string dec, bool farthest)
    {
        AlgebraicRange? described = AlgebraicRanges.OfCounterRegister(op.Expr)
            ?? (op.Expr is BoundIntrinsicCall
                ? AlgebraicRanges.OfFunctionReturnedValue(IntrinsicResultType.IsIntegerOperand(op), v.Dec)
                : null);
        if (described is not { } range || range.Nearest is null)
            return EmitText.LoudValue("bool", $"{(farthest ? "FARTHEST-FROM-ZERO" : "NEAREST-TO-ZERO")} over a computed operand with no described capacity");
        string positive = farthest ? range.Farthest : range.Nearest;
        string? negative = range.FarthestNegative is null ? null : farthest ? range.FarthestNegative : "-" + range.Nearest;
        return RuntimeApi.ValueClassIsExtreme(dec, DecLiteral(positive), negative is null ? null : DecLiteral(negative));
    }

    /// <summary>§8.8.4.4.4 GR3 l) over a computed operand's decimal value, against the mode's intermediate.</summary>
    private string ComputedInArithmeticRangeTest(string dec)
    {
        var (farthest, nearest) = ArithmeticModes.IntermediateExtremes(ctx.Data.Options.Arithmetic);
        return RuntimeApi.ValueClassIsInArithmeticRange(dec, DecLiteral(farthest), DecLiteral(nearest));
    }

    /// <summary>An exact decimal-literal TEXT (the forms <see cref="AlgebraicRanges"/> and
    /// <see cref="ArithmeticModes.IntermediateExtremes"/> produce) as a decimal-form constant — every one of them
    /// has at most 34 significant digits, so the lift is exact.</summary>
    private string DecLiteral(string text) =>
        NumericLiteral.TryParseExact(text, out var sig, out int exp10)
            ? RuntimeApi.DecFromParsedLiteral(sig, exp10, num.IntermediateMode)
            : EmitText.LoudValue("CobolDec", $"decimal constant '{text}'");

    private string RenderCondition88(BoundCondition88 c)
    {
        // ⛔ ONE CLASSIFICATION OF THE CONDITIONAL VARIABLE, computed HERE and shared by its IMAGE and by its
        // comparison rules (kb/Work PB728). ISO §8.8.4.5.3 GR2 — "The rules for comparing a conditional variable
        // with a condition-name value are the same as those specified for relation conditions" — so the variable
        // is classified by exactly the reader every relation surface uses, StringCategoryOf over the OPERAND
        // picture (§13.18.29.4 GR1b/GR2b). The two halves USED TO DISAGREE: the operand text came from this
        // reader (a national group rendered `.AsNat()`) while the collating sequence came from the parent's raw
        // PICTURE category, which is null for every group — so the national image was weighed on the ALPHANUMERIC
        // table. Deriving it once and PASSING it down is why a future arm cannot re-derive a second answer.
        var subject = new BoundFieldOperand(c.Parent);
        var cat = StringCategoryOf(subject);
        // ISO §8.8.4.2.1: an ALPHANUMERIC group item is compared as an elementary alphanumeric data item. It has
        // no PICTURE and no as-if PICTURE, so `cat` is null for it and CollatingSelection.ForComparison(null,
        // null) is the documented alphanumeric branch — the IsGroup disjunct here is only about taking the
        // character IMAGE rather than the record struct.
        bool isString = c.Parent.Item.IsGroup || cat is PicCategory.Alphanumeric
            or PicCategory.NumericEdited or PicCategory.National or PicCategory.Boolean;
        // ISO §8.8.4.5 GR2: a condition-name test compares the conditional variable by the RELATION-CONDITION rules, so
        // the variable is rendered as a comparison operand exactly as a relation condition renders it — an alphanumeric
        // GROUP is treated as an elementary alphanumeric data item (§8.8.4.1), i.e. its character IMAGE, not the raw
        // struct. (The numeric branch reads the scaled value directly; a numeric view stored as its image is a later
        // slice.)
        // A NUMERIC conditional variable goes through the ONE numeric read path (NumericRenderer.FieldNum) — a
        // whole-group-aliased / Tier-B-view leaf is string-STORED (StoreAsImage) and must decode via ParseDisplay,
        // never compare its raw image to an unscaled long (diagnosis B3).
        string read = isString ? OperandText.AsString(subject, num) : num.FieldNum(c.Parent).Expr;
        var tests = c.Condition.Values.Select(v => RenderMembershipTest(read, c.Parent.Item, cat, isString, v.Low, v.High,
            c.CheckRangeInvalid, c.Condition.Alphabet)).ToList();
        // ⛔ TOTAL OVER AN EMPTY VALUE SET (kb/Work PB501). §13.16.2 formats 3 and 4 both print the value-clause
        // UNBRACKETED, so an entry with no values is nonconforming source and `LevelNumberPass` refuses it by
        // name (COBOLNET1747) — this renderer is never reached for one in a compilation that gets as far as
        // codegen. But a bind-time recovery can still leave the list empty (an operand that was not a literal
        // position binds nothing, kb/Work PB732), and `string.Join` over an empty sequence rendered the
        // TWO-CHARACTER C# fragment `()`, so the generated source read `if (())` and the user was shown
        // `error CS1525: Invalid expression term ')'` against a .g.cs path instead of a COBOL diagnostic.
        // §13.16.4 GR3 makes a condition-name "the value, values, or range of values associated with the
        // condition-name", so a condition-name associated with NO value can only be false — that is the one
        // total answer, and it keeps an already-failed compile from failing a SECOND time in the backend.
        return tests.Count == 0 ? "false" : "(" + string.Join(" || ", tests) + ")";
    }

    /// <summary>The trailing collation argument for a THROUGH range, when its clause named one with
    /// <c>IN alphabet-name-1</c> (ISO §14.7.8 rule 2 — "the collating sequence used for range evaluation is the
    /// collating sequence defined by that alphabet"): the alphabet's carrier, or nothing when it is an identity
    /// sequence, whose ordering IS the native one. ⛔ It DISPLACES the categories' own answer, PROGRAM COLLATING
    /// SEQUENCE included — that default is precisely what rule 2's no-phrase arm gives and what the phrase is for.
    /// The ONE reader, shared by the EVALUATE range and the level-88 VALUE range, because §14.7.8's first sentence
    /// governs both clauses at once.</summary>
    private string RangeCollateArg(string? alphabet, PicCategory? left, PicCategory? right) => alphabet is { } a
        ? ctx.Data.RangeCollations.TryGetValue(a, out var c) && c.Field is { } f ? ", " + f : ""
        : ctx.CollateArgFor(left, right);

    /// <summary>One VALUE-set membership test: equality for a singleton, an inclusive bound test for a THRU range.
    /// When <paramref name="checkRangeInvalid"/> and the range is alphanumeric/national (§14.7.8 rule 2), the range
    /// test routes through <c>CobolString.ThruMember</c> which sets the nonfatal EC-RANGE-INVALID for an inverted
    /// range (lo collating after hi) and treats it as empty.</summary>
    private string RenderMembershipTest(string read, DataItem parent, PicCategory? cat, bool isString, string low,
        string? high, bool checkRangeInvalid = false, string? alphabet = null)
    {
        if (isString)
        {
            // A level-88 VALUE compares against the conditional variable, so a figurative ALL "literal" is repeated to
            // the variable's width (ISO §8.3.3.6.4 GR2); a plain literal is decoded as-is. A BOOLEAN conditional
            // variable compares by value with boolean-zero extension and never the alphanumeric PCS (§8.8.4.2.8);
            // a NATIONAL one orders under the NATIONAL sequence (§8.8.4.2.9 — the D-N3 ordinal identity, or
            // __COLLATE_NAT under a non-native ALPHABET … FOR NATIONAL; never the alphanumeric PCS weights).
            // §8.8.4.5.3 GR2 — "The rules for comparing a conditional variable with a condition-name value are
            // the same as those specified for relation conditions" — so the collating sequence comes from the ONE
            // comparison-class rule. A level-88 VALUE literal is always of the conditional variable's OWN
            // category (§13.18.63.3 SR2 numeric ⇒ numeric literals, SR4 alphabetic/alphanumeric/alphanumeric-
            // edited ⇒ alphanumeric, SR5 national/national-edited ⇒ national, SR10 boolean ⇒ boolean), so the
            // pair is the variable's category twice — this is a case where one category legitimately answers
            // for both, and it is stated rather than assumed.
            // ⛔ `cat` ARRIVES from RenderCondition88's ONE classification — it is NOT re-derived here, and in
            // particular never from the parent's raw PICTURE, which is null for every group: that read handed a
            // GROUP-USAGE NATIONAL 88 the ALPHANUMERIC weight table while its operand text was already
            // `.AsNat()`, and lost a bit group's boolean-zero pad (kb/Work PB728 / PB741).
            string collate = ctx.CollateArgFor(cat, cat);
            // ⛔ THE `IN alphabet-name-1` PHRASE GOVERNS THE RANGES ONLY, and the split is the clause boundary,
            // not a convenience: §14.7.8 is the THROUGH phrase's specification and its rule 2 names "the collating
            // sequence used for RANGE evaluation", while a SINGLETON value is compared by §8.8.4.5.3 GR2's plain
            // relation-condition rules, whose sequence is §8.8.4.2.7's PROGRAM COLLATING SEQUENCE. Handing the
            // alphabet to the equality tests as well would have made `88 X VALUE "A", "M" THRU "Z" IN AL` weigh
            // its singleton on a sequence no rule puts there.
            string rangeCollate = RangeCollateArg(alphabet, cat, cat);
            string pad = cat is PicCategory.Boolean ? ", pad: '0'" : "";
            // ⛔ A FIGURATIVE VALUE IS A SEED, SIZED BY THE RUNTIME AGAINST THE OPERAND AS READ — exactly as the
            // relation condition sizes it (kb/Work PB1477). §8.8.4.5.3 GR2 makes this comparison the relation
            // condition's, and §8.3.3.6.4 GR2 repeats a figurative to "the number of character positions in the
            // associated data item": the conditional variable AS IT IS COMPARED, i.e. at its CURRENT extent
            // (§13.18.38.4 GR8 a) — an occurs-depending group item referenced as an operand uses "only that part
            // of the table area that is specified by the value of the data item referenced by data-name-1"). A
            // compile-time fold to the variable's declared width sized `88 GV-ALLX VALUE ALL "X"` on a group
            // holding an OCCURS DEPENDING ON table to the MAXIMUM extent while `read` is the current one, so the
            // 88 answered FALSE where `IF GV = ALL "X"` answered TRUE. The seed and the sizing are now the ones
            // the relation (RenderFigurativeRelational) and the EVALUATE range (Visit(BoundRangeMembership)) use.
            var lo = StringMembershipOperand(low, parent, cat);
            if (high is null)
                return lo.IsFigSeed
                    ? $"{RuntimeApi.StrCompareFig(read, lo.Text, figIsLeft: false, pad + collate)} == 0"
                    : $"{RuntimeApi.StrCompare(read, lo.Text, pad + collate)} == 0";
            var hi = StringMembershipOperand(high, parent, cat);
            // §14.7.8 rule 2: an alphanumeric/national THRU range under checking routes through ThruMember (sets the
            // nonfatal EC-RANGE-INVALID for an inverted range, then treats it as empty — the empty behaviour is
            // otherwise emergent from the inclusive test). Boolean/other categories keep the inline form.
            // The class that governs the COMPARISON, not the raw category — the same pair rule the collate
            // argument above asks (§8.8.4.5.3 GR2; the pair is the variable's category twice, as stated there).
            // ForComparison(null, null) is the ALPHANUMERIC branch (an ordinary group item, ISO §8.8.4.2.1), so
            // an alphanumeric GROUP's 88 range now reaches §14.7.8 rule 2's exception exactly as its elementary
            // twin does. Boolean and numeric ranges keep the inline form (§14.7.8 rule 1 sets no
            // exception; a boolean subject may not carry THROUGH at all, §13.18.63.3 SR29).
            // ⛔ THE ONE §14.7.8 rule-1/rule-2 predicate, the same call the EVALUATE range's EC gate and
            // TryResolveRangeAlphabet's SR3 screen make (kb/Work PB401) — never a locally spelled-out class test.
            bool checkedRange = checkRangeInvalid
                && CollatingSelection.IsCollatedThroughRange(CollatingSelection.ForComparison(cat, cat));
            return RenderStringRange(read, lo, hi, checkedRange, pad + rangeCollate);
        }
        // A float (COMP-1/2/FLOAT-*) conditional variable: `read` is the native double `(double)(X)`, so the VALUE
        // literal must render as a native double too — NOT scaled-integer at the float item's Scale 0, which would
        // DROP the fraction (88 IS-HALF VALUE 0.5 became `== 0L`, the exact-inverse membership bug). (D16 review.)
        if (parent.OperandPic is { IsFloat: true })
        {
            string loF = FloatMembershipValue(low);
            if (high is null) return $"{read} == {loF}";
            return $"({read} >= {loF} && {read} <= {FloatMembershipValue(high)})";
        }
        int scale = parent.OperandPic?.Scale ?? 0;
        string loN = NumericMembershipValue(low, scale);
        if (high is null) return $"{read} == {loN}";
        return $"({read} >= {loN} && {read} <= {NumericMembershipValue(high, scale)})";
    }

    /// <summary>A numeric level-88 VALUE operand on a FLOAT conditional variable → a native C# <c>double</c> literal
    /// (D16). A figurative ZERO → <c>0.0</c>; a floating-point literal (1.5E3) is already valid C# double syntax;
    /// otherwise the fixed-point literal takes the <c>d</c> suffix. Keeps both sides of the membership test IEEE
    /// doubles (§8.8.4.2.4 algebraic compare), matching the direct relation-condition path.</summary>
    private static string FloatMembershipValue(string raw) =>
        FigurativeConstants.Classify(raw).Kind is 'Z' ? "0.0"
        : raw.IndexOf('E') >= 0 || raw.IndexOf('e') >= 0 ? raw.Trim().TrimStart('+')
        : $"{raw.Trim().TrimStart('+')}d";

    /// <summary>A string level-88 VALUE operand as a C# EXPRESSION, in the order the VALUE recipe reads it:
    /// <list type="bullet">
    /// <item>a format-2 (LOCALE) conditional variable's numeric VALUE composes its edited image AT RUNTIME under
    /// the locale then current (§13.18.40.5 r11 — no compile-time image exists; the ONE producer,
    /// <see cref="RuntimeApi.LocaleEditCompose"/>; comparing raw literal text is precisely the PB97 defect shape);</item>
    /// <item>a NUMERIC-EDITED conditional variable's numeric literal (or figurative ZERO at >= 2023) is its EDITED
    /// image — ISO §13.18.63.3 SR6 converts a numeric-edited item's numeric VALUE literals "according to the rules
    /// for the MOVE statement", and §8.8.4.5.3 GR2 then compares by the relation-condition rules (kb/Work PB97);</item>
    /// <item>a figurative <c>ALL "literal"</c> or a bare figurative WORD (QUOTE / SPACE / HIGH-VALUE / LOW-VALUE /
    /// ZERO) is a SEED (<see cref="MembershipOperand.IsFigSeed"/>) that the runtime sizes to the conditional variable
    /// AS READ (§8.3.3.6.4 GR2 — kb/Work PB1477; NC250A IF--TEST-26/27);</item>
    /// <item>else the decoded literal.</item>
    /// </list></summary>
    private MembershipOperand StringMembershipOperand(string raw, DataItem parent, PicCategory? cat)
    {
        if (parent.OperandPic is { LocaleEdit: not null } lpic
            && !raw.StartsWith('"') && !raw.StartsWith('\'')
            && ValueInitializer.TryParseNumeric(raw, out var uv, out int sc))
            return new(RuntimeApi.LocaleEditCompose(lpic, uv, sc, parent.BlankWhenZero), IsFigSeed: false);
        if (parent.OperandPic is { Category: PicCategory.NumericEdited } npic
            && ValueInitializer.EditedImageOfNumericValue(ctx.Data.Edition.DialectLevel,
                    parent, npic, raw) is { } edited)
            return new(EmitText.CsLiteral(edited), IsFigSeed: false);
        // ⛔ THE ONE §8.3.3.6.2 OPERAND CLASSIFIER (kb/Work PB461). §14.9.39.4 GR6 stores this same operand
        // "according to the rules for the VALUE clause" and §8.8.4.5.3 GR3 makes the test true exactly when the
        // stored value equals it, so the TEST is required to read the text the way the STORE and the VALUE
        // initializer do. This site's private strip fired only before a SPACE, and the parse tree glues the two
        // words — so `88 B-ALLSP VALUE ALL SPACES` arrived as "ALLSPACES", never stripped, and was compared as
        // the ten characters A-L-L-S-P-A-C-E-S against an item this compiler had correctly filled with spaces.
        // The fill is category-aware here as it is at the store (§8.3.3.6.4 GR6/GR7 — a national or boolean
        // anchor reads its OWN sequence, never the alphanumeric PCS); `cat` is RenderCondition88's ONE
        // classification of the variable, so a bit / national GROUP's as-if category (§13.18.29.4 GR1b/GR2b)
        // chooses the fill (kb/Work PB728). No width is computed here: the SEED is sized by the runtime against
        // the variable's own character-position count as read — boolean positions, national positions, or an
        // ODO group's CURRENT extent — which is the one quantity GR2 names (kb/Work PB1477).
        var op = FigurativeConstants.Classify(raw);
        return op.AllLiteral is { } lit ? new(EmitText.CsLiteral(CobolLiteral.Decode(lit)), IsFigSeed: true)
            : op.Kind is { } k ? new(FigurativeConstants.FillText(k, ctx.Data.Collating, cat, ctx.Data.NationalCollating), IsFigSeed: true)
            : new(EmitText.CsLiteral(CobolLiteral.Decode(raw)), IsFigSeed: false);
    }

    /// <summary>A numeric level-88 VALUE operand → its unscaled-<c>long</c> text. A figurative ZERO maps to <c>0</c>
    /// (ISO §8.3.3.6.4 GR4 — "the zero format represents the numeric value '0' … depending on context"); otherwise
    /// the literal is scaled. Without this a figurative VALUE word (e.g. <c>88 IS-ZERO VALUE ZERO</c>) would reach
    /// <c>UnscaledAtScale("ZERO", …)</c> and emit a bare identifier — which is exactly what the ALL-prefixed
    /// spelling did until this read the operand through THE one classifier (kb/Work PB461: the private word list
    /// here spelled out three of the forms §8.3.3.6.2 Format 1 admits, and `ALL ZEROS` — the same constant, ALL
    /// being an optional word there — emitted `ALLZEROSL`, a C# CS0103 on legal COBOL).</summary>
    private static string NumericMembershipValue(string raw, int scale) =>
        FigurativeConstants.Classify(raw).Kind is 'Z' ? "0L" : EmitText.UnscaledAtScale(raw, scale);

    /// <summary>The statically known fraction-digit count of a relation operand — a numeric literal's digits
    /// right of the decimal point, a field's declared scale (a numeric-edited comparand cannot appear in a
    /// NUMERIC relation, so the bare <c>PicInfo</c> scale suffices) — 0 when the shape carries no static scale,
    /// which is exactly the former blanket behavior. Feeds the OTHER side's working-scale request (fix-queue
    /// PB60 / RV-15.68.4-1): over-asking is safe (the Int128-headroom cap binds), under-asking was the
    /// measured relation-agrees-with-a-truncated-value defect.</summary>
    private static int StaticScaleOf(BoundOperand op) => op switch
    {
        BoundNumericLiteral nl => nl.Text.IndexOfAny(['.', ',']) is >= 0 and var dp ? nl.Text.Length - dp - 1 : 0,
        BoundFieldOperand f => f.Place.Item.OperandPic?.Scale ?? 0,
        _ => 0,
    };
}
