// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Procedure;

/// <summary>
/// ⛔ <b>THE ONE MATERIALIZATION OF A SENDING VALUE</b> — kb/Work PB394.
///
/// <para><b>The rule, in two verbs.</b> ISO §14.9.25.4 GR1 (MOVE): "If identifier-1 is reference-modified,
/// subscripted, or is a function-identifier, the reference modifier, subscript, or function-identifier is
/// evaluated only once, immediately before data is moved to the first of the receiving operands", and the rule
/// then writes the required shape out as an EQUIVALENCE — <c>MOVE a (b) TO b, c (b)</c> is "equivalent to:
/// MOVE a (b) TO temp / MOVE temp TO b / MOVE temp to c (b) … where 'temp' is an intermediate result item
/// provided by the implementor". ISO §14.9.13.4 GR3 (EVALUATE): "At the beginning of the execution of the
/// EVALUATE statement, each selection subject is evaluated and assigned a value, a range of values, or a truth
/// value". Both sentences say ONE thing: <b>the sending value is materialized, once, before it is used.</b></para>
///
/// <para><b>Why it is a mechanism and not two edits (CLAUDE.md rule 5).</b> Before this, neither verb had
/// anywhere to PUT a value, so each re-ran the source EXPRESSION at every place it needed one — MOVE once per
/// receiver (<c>MoveEmitter.Emit</c>'s per-target loop re-rendered <c>BoundMove.Source</c> in all four
/// <c>MoveKind</c> arms), EVALUATE once per selection pair and TWICE per THRU arm. The two defects had one
/// cause, and the repair is the missing noun: an intermediate result item. Every future verb with one sender
/// and many uses (<c>SET … TO</c>, <c>ADD … TO</c>, <c>STRING … INTO</c>) asks this class for one rather than
/// growing its own hoist.</para>
///
/// <para><b>The item's description is the OPERAND's own</b>, so the store into it is an identity move and the
/// equivalence is exact rather than approximate:</para>
/// <list type="bullet">
///   <item><b>A data item</b> (<see cref="BoundFieldOperand"/> over an undecorated place,
///     <see cref="BoundCurrentRecord"/>) — the item's description, cloned. <c>DataBinder.CreateCompilerTemp</c>
///     copies the PICTURE and the description clauses at the root and deep-clones a group's subtree, and it does
///     NOT copy the root's OCCURS, which is exactly right: the sender <c>a (b)</c> is ONE OCCURRENCE, and the
///     temp holds one occurrence's value.</item>
///   <item><b>A reference-modified data item</b> — §8.4.3.3.4 GR6's "unique data item", which "has the same
///     class, category, and usage as that defined for identifier-1" with the three lettered exceptions
///     (<see cref="RefModPlace.Category"/> is the ONE reader of that rule). Its LENGTH is a run-time value, so
///     the temp is a RUN-TIME-LENGTH item — see below.</item>
///   <item><b>A function-identifier</b> — §15.4: "The evaluation of a function produces a returned value in a
///     temporary elementary data item", and §15.4.1: "When native arithmetic is in effect, the characteristics
///     and representation of the returned value are defined by the implementor". A NUMERIC result lands in an
///     item DESCRIBED FOR ITS VALUE AND ITS RECEIVERS (<see cref="IntermediatePic"/>, kb/Work PB3268) — a binary64
///     for the float family, the widest receiver's fixed-point description otherwise — and never in the fixed
///     S9(21)V9(9) of <see cref="FunctionValuePic"/>, which describes the returned-value item <c>FUNCTION
///     LENGTH</c> measures: that cut a receiver's fraction digits past 9 off a value `MOVE FUNCTION PI TO Q1`
///     keeps. An alphanumeric / national / boolean result has a
///     run-time length and lands in a run-time-length item.</item>
/// </list>
///
/// <para><b>The run-time-length carrier</b> is a dynamic-length elementary item (§8.5.1.10), because
/// §8.5.1.10.4 states precisely the property the intermediate needs: "A dynamic-length elementary item that is
/// used as a sending operand or is reference-modified is treated as a fixed-length data item whose length is
/// the dynamic-length elementary item's current length." ⚠ §8.5.1.10.1's "may be category alphanumeric or
/// national" constrains what a programmer may DECLARE with the §13.18.19 DYNAMIC LENGTH clause; this item is
/// not declared by anyone — it is §14.9.25.4 GR1's "intermediate result item provided by the implementor" and
/// §15.4.1's implementor-defined representation — so a BOOLEAN slice's carrier keeps category boolean and the
/// emitter's category-aware store (<c>RuntimeApi.StrStoreBoolean</c>) still sees the right class.</para>
///
/// <para><b>What is NOT materialized, and why each is a decision rather than an omission:</b></para>
/// <list type="bullet">
///   <item>A <b>literal</b> or <b>figurative constant</b> — GR1's antecedent names only a reference-modified,
///     subscripted or function-identifier operand, a literal's value cannot change, and a figurative /
///     <c>ALL "literal"</c> operand has NO description of its own to clone: §8.3.3.6.4 GR2 sizes it from THE
///     RECEIVER, so materializing it at one receiver's width would corrupt the others.</item>
///   <item>A <b>variable-length group</b> (§8.5.1.12 — one with a dynamic-length elementary item or a
///     dynamic-capacity table beneath it) — the clone carries neither the member's current length nor the
///     table's capacity, so its extent would be a DIFFERENT number from the sender's rather than a frozen copy
///     of it. That is the one shape of GR1's "The length of the data item referenced by identifier-1 is
///     evaluated only once" this mechanism does not discharge, and it is recorded as the residual on row
///     GR-14.9.25.4-1 rather than silently claimed. ⭐ An <c>OCCURS … DEPENDING ON</c> group IS discharged:
///     its control value is an ordinary data item, so <see cref="FreezeOdoExtent"/> mints a second intermediate
///     for it and points the clone's OCCURS at that (kb/Work PB394).</item>
///   <item>A <b>boolean expression</b> or an <b>error operand</b> — neither is a §14.9.25 sending operand this
///     path can reach with more than one use (a boolean-expression sender is COMPUTE Format 2's channel), and
///     an error operand was already diagnosed.</item>
/// </list>
/// </summary>
internal sealed class SendingValueTemp(BinderContext ctx)
{
    /// <summary>The §15.4 temporary's description for a NUMERIC function-identifier value (a function-bearing
    /// subscript's temporary is a different item — <c>StatementBinder.SegmentTempPic</c>, the saturated integer
    /// POSITION, kb/Work PB1890). §15.4.1: "When native arithmetic is in effect, the
    /// characteristics and representation of the returned value are defined by the implementor."
    /// <para>21 integer digits × 9 fraction digits — 30 total, so the item takes the <c>Int128</c> wide tier
    /// (<c>PicInfo.IsWide</c>, &gt;18 digits) and no function value a program can consume overflows it; the
    /// fraction is carried because a scale-0 temp would TRUNCATE, turning a legal non-integer result into a
    /// different value. 9 is also the working scale an intrinsic renders at when no receiver widens it
    /// (<c>ReceiverContext</c>).</para>
    /// <para>⛔ IT IS THE RETURNED-VALUE ITEM'S DESCRIPTION (DOC-A.1-92: what <c>FUNCTION LENGTH</c> measures, what
    /// IN-ARITHMETIC-RANGE tests) AND NOT THE INTERMEDIATE THAT
    /// CARRIES A VALUE TO ITS RECEIVERS: that item is described for the value and its receivers
    /// (<see cref="IntermediatePic"/>, kb/Work PB3268), because a receiver of more than 9 fraction digits is owed
    /// the digits a single-receiver MOVE stores.</para></summary>
    internal static readonly PicInfo FunctionValuePic =
        new(PicCategory.Numeric, Usage.Display, Length: ReturnedIntegerDigits + ReturnedFractionDigits,
            Digits: ReturnedIntegerDigits + ReturnedFractionDigits, Scale: ReturnedFractionDigits, Signed: true);

    /// <summary>The integer digits of <see cref="FunctionValuePic"/>.</summary>
    private const int ReturnedIntegerDigits = 21;

    /// <summary>The fraction digits of <see cref="FunctionValuePic"/>.</summary>
    private const int ReturnedFractionDigits = 9;

    /// <summary>The §15.4 temporary's description for an INTEGER function's returned value (§15.2 item 5 — "no
    /// digits to the right of the decimal point"): the same 30-digit wide tier as <see cref="FunctionValuePic"/>,
    /// at scale 0, so the temporary's literal-form text image (DOC-A.1-92) is the integer the function renders
    /// directly, with no fraction digits (kb/Work PB1007).</summary>
    internal static readonly PicInfo IntegerFunctionValuePic =
        new(PicCategory.Numeric, Usage.Display, Length: 30, Digits: 30, Scale: 0, Signed: true);

    /// <summary>⛔ THE ONE DESCRIPTION OF A NUMERIC OR INTEGER FUNCTION-IDENTIFIER'S RETURNED-VALUE ITEM (§15.4 — "a
    /// temporary elementary data item"; §8.4.3.2.1 — the function-identifier "references the unique data item that
    /// results from the evaluation of a function"): <see cref="IntegerFunctionValuePic"/> for an INTEGER function
    /// (§15.2 item 5, resolved per call by the ONE <see cref="IntrinsicResultType"/> reader), otherwise
    /// <see cref="FunctionValuePic"/>. The LENGTH / BYTE-LENGTH folds that measure the item
    /// (<c>IntrinsicBinder.FunctionResultLength</c>, kb/Work PB1398) and the IN-ARITHMETIC-RANGE extremes
    /// (<c>AlgebraicRange.OfFunctionReturnedValue</c>) read it here, so the item the value is ASKED ABOUT cannot be
    /// two different items. The intermediate a statement STORES the value in to hand it to several uses is
    /// <see cref="IntermediatePic"/>'s.</summary>
    internal static PicInfo NumericReturnedValuePic(BoundOperand c) =>
        IntrinsicResultType.IsIntegerOperand(c) ? IntegerFunctionValuePic : FunctionValuePic;

    /// <summary>How a materialized value is going to be read — the one fact, besides the function itself, that
    /// decides the description of a NUMERIC function's intermediate (<see cref="IntermediatePic"/>). The default is a
    /// VALUE operand (an EVALUATE subject, a frozen function argument).</summary>
    /// <param name="Receivers">The items the value is about to be moved to (the MOVE's receiving operands), or null.</param>
    /// <param name="AsText">True when the value will be read as TEXT in a character formal (an INVOKE argument), the one
    /// consumer whose characters come from the intermediate's own scale (DOC-A.1-92).</param>
    internal readonly record struct ValueUse(IReadOnlyList<Place>? Receivers = null, bool AsText = false)
    {
        /// <summary>The value is moved to <paramref name="receivers"/> (§14.9.25.4 GR1).</summary>
        public static ValueUse MovedTo(IReadOnlyList<Place> receivers) => new(receivers);

        /// <summary>The value is passed to a character formal and read as its literal-form text.</summary>
        public static readonly ValueUse Text = new(null, AsText: true);
    }

    /// <summary>The intermediate result item of a NUMERIC function whose value is a binary64: a COMP-2 item, the SAME
    /// carrier the float family's returned value has wherever no fixed-point scale governs the render
    /// (<c>ReceiverContext.FloatLanding</c>), so a store into it and a store from it lose nothing the function's own
    /// value did not. USAGE FLOAT-LONG is "held in a floating-point format suitable for the machine" (§13.18.60.4 GR13);
    /// this compiler's format is the IEEE binary64 that GR15's FLOAT-BINARY-64 names.</summary>
    internal static readonly PicInfo Binary64ReturnedValuePic = PicInfo.FloatItem(Usage.FloatLong);

    /// <summary>⛔ THE ONE DESCRIPTION OF THE INTERMEDIATE RESULT ITEM THAT HOLDS A NUMERIC FUNCTION'S VALUE FOR ITS
    /// USES (kb/Work PB3268). It is a different item from the §15.4 returned-value item whose SIZE
    /// <c>FUNCTION LENGTH</c> asks (<see cref="NumericReturnedValuePic"/>, DOC-A.1-92's S9(21)V9(9)): §14.9.25.4 GR1's
    /// equivalence — "MOVE a TO temp / MOVE temp TO b / MOVE temp TO c" — makes the intermediate an item "provided by
    /// the implementor" whose job is to hand EVERY receiver the value `MOVE a TO that receiver` would, and §15.4.1's
    /// "the returned value is the same for all instances of a given function" forbids a function's value depending
    /// on how many receivers its MOVE names. The scale-9 description did exactly that: `MOVE FUNCTION PI TO Q1 Q2`
    /// (PIC 9V9(15)) stored 3.141592653000000 where `MOVE FUNCTION PI TO Q3` stores 3.141592653589793.
    /// <para>The carrier follows the value, in the order the renderer chooses it:</para>
    /// <list type="number">
    ///   <item>an INTEGER function (§15.2 item 5) — <see cref="IntegerFunctionValuePic"/>, unchanged;</item>
    ///   <item>a function the renderer returns as a BINARY64 (<see cref="FunctionValueLane.ReturnsBinary64"/>, asked of
    ///     the call and its ARGUMENTS' carriers — a floating-point literal argument takes a function with an SDIDI body
    ///     off the float lane, a binary64 argument puts any numeric-argument function onto it), or any function whose
    ///     every receiver is a floating-point item (a binary64 receiver takes the value as a binary64) —
    ///     <see cref="Binary64ReturnedValuePic"/>, so the store into the intermediate quantizes NOTHING and each
    ///     receiver's own transfer (the ONE float landing) quantizes once, at ITS scale;</item>
    ///   <item>otherwise a fixed-point item described by the receivers (<see cref="ShapedPic"/>), or, when no
    ///     receiver is known (an EVALUATE subject, an argument frozen for §8.4.3.2.4 GR2's order),
    ///     <see cref="ValueOperandPic"/>.</item>
    /// </list></summary>
    private PicInfo IntermediatePic(BoundOperand c, ValueUse use)
    {
        if (IntrinsicResultType.IsIntegerOperand(c)) return IntegerFunctionValuePic;
        var receivers = use.Receivers;
        if (c is BoundComputedOperand { Expr: BoundIntrinsicCall call } && FunctionValueLane.ReturnsBinary64(call, ctx.Data.Options.Arithmetic)
            || receivers is { Count: > 0 } && receivers.All(r => r.DenotedItem is not null && r.Item.OperandPic is { IsFloat: true }))
            return Binary64ReturnedValuePic;
        return receivers is { Count: > 0 } ? ShapedPic(receivers) : use.AsText ? FunctionValuePic : ValueOperandPic;
    }

    /// <summary>The fixed-point intermediate of a NUMERIC, non-binary64 function value that is read as a VALUE
    /// operand — an EVALUATE subject compared with its WHEN objects, an argument frozen for §8.4.3.2.4 GR2's order —
    /// where no receiver bounds the digits a comparison or the consuming function will ask of it: the widest scaled
    /// item the carrier holds with <see cref="FunctionValuePic"/>'s 21 integer digits, so `EVALUATE FUNCTION
    /// NUMVAL("1.23456789012") WHEN 1 WHEN 1.23456789012` compares the exact value the un-hoisted
    /// `IF FUNCTION NUMVAL("1.23456789012") = 1.23456789012` does.</summary>
    private static readonly PicInfo ValueOperandPic =
        new(PicCategory.Numeric, Usage.Display, Length: PicInfo.ScaledCarrierDigits, Digits: PicInfo.ScaledCarrierDigits,
            Scale: PicInfo.ScaledCarrierDigits - ReturnedIntegerDigits, Signed: true);

    /// <summary>The integer digits a floating-point receiver asks of a fixed-point intermediate (an implementor choice
    /// under §15.4.1; <see cref="ShapedPic"/>): 12, leaving 26 fraction digits.</summary>
    private const int FloatWindowIntegerDigits = 12;

    /// <summary>The fixed-point intermediate for a value that lands in <paramref name="receivers"/>: the WIDEST
    /// receiver's description (§14.9.25.4 GR1's `MOVE a TO temp` with temp described as the largest receiver, so
    /// every other receiver's store is a MOVE of a value the widest one already truncated the same way). A
    /// numeric or numeric-edited receiver contributes its fraction scale (<c>PicInfo.ReceiverScale</c>, the ONE
    /// rule) and integer digit positions; a receiver with no numeric scale (a character or boolean one, reachable only
    /// under --permissive for a NUMERIC function, Table 16) contributes the <see cref="FunctionValuePic"/> description
    /// its text image has always been written from. The fraction is capped so the item stays on the <c>Int128</c>
    /// carrier (<see cref="PicInfo.ScaledCarrierDigits"/>); integer digits keep priority, because an under-counted
    /// integer part is a wrong value and an under-counted fraction a truncated one (the order
    /// <c>ReceiverContext.WorkingScale</c> keeps).
    /// <para>⚠ A FLOATING-POINT RECEIVER BESIDE A FIXED-POINT ONE IS THE CARRIER'S LIMIT (kb/Work PB3268, review R1-2). A
    /// binary64 receiver holds ~17 significant digits at ANY magnitude (1E-20 and 1E+30 alike), which no fixed-point
    /// split of 38 digits holds. It is reached here only for a value that is NOT a binary64 (a binary64 value takes
    /// <see cref="Binary64ReturnedValuePic"/>, and every receiver lands from it at its own scale), i.e. an exact or SDIDI
    /// value; the float receiver takes what the fixed-point receivers leave of the 38 digits, with
    /// <see cref="FloatWindowIntegerDigits"/> integer digits at least (so a value from 1E-9 to 1E+12 keeps the 17
    /// significant digits a binary64 holds), and a fixed-point receiver never loses a fraction digit to a float one. An
    /// exact value outside that window reaches the float receiver cut. An SDIDI-carried intermediate item is the
    /// redesign that removes the limit (the same one the values past the <c>Int128</c> carrier need).</para></summary>
    private static PicInfo ShapedPic(IReadOnlyList<Place> receivers)
    {
        int scale = 0, integer = 0;
        bool sized = false, anyFloat = false;
        foreach (var receiver in receivers)
        {
            // A reference-modified receiver is the elementary ALPHANUMERIC unique data item of §8.4.3.3.4 GR6, never
            // the numeric item under it; an index or object receiver has no description to size by.
            bool slice = receiver.DenotedItem is null;
            var pic = slice ? null : receiver.Item.OperandPic;
            if (!slice && pic is null) continue;
            sized = true;
            if (pic is null) { scale = Math.Max(scale, ReturnedFractionDigits); integer = Math.Max(integer, ReturnedIntegerDigits); }
            else if (pic.IsFloat) anyFloat = true;
            else if (pic.Category is PicCategory.Numeric or PicCategory.NumericEdited)
            {
                int s = pic.ReceiverScale();
                scale = Math.Max(scale, s);
                integer = Math.Max(integer, Math.Max(0, pic.DigitPositions - s));
            }
            else { scale = Math.Max(scale, ReturnedFractionDigits); integer = Math.Max(integer, ReturnedIntegerDigits); }
        }
        // No receiver had a description to size by (an index or object receiver is not a numeric MOVE receiver, so this
        // is unreachable from a legal MOVE): the value operand's item, never a one-digit guess.
        if (!sized) return ValueOperandPic;
        integer = Math.Clamp(integer, 1, PicInfo.ScaledCarrierDigits);
        scale = Math.Min(scale, PicInfo.ScaledCarrierDigits - integer);
        if (anyFloat)
        {
            integer = Math.Max(integer, Math.Min(FloatWindowIntegerDigits, PicInfo.ScaledCarrierDigits - scale));
            scale = Math.Max(scale, PicInfo.ScaledCarrierDigits - integer);
        }
        int digits = integer + scale;
        return new PicInfo(PicCategory.Numeric, Usage.Display, Length: digits, Digits: digits, Scale: scale, Signed: true);
    }

    /// <summary>Materialize <paramref name="op"/>'s value into the implementor's intermediate result item and
    /// return the <see cref="Place"/> holding it, or <see langword="null"/> when the operand needs no
    /// materialization (a literal / figurative constant) or carries a description this clone cannot freeze (a
    /// variable-length group). The store registers as a statement-scoped PRE-op on the shared
    /// <c>DataBinder.PendingPreOps</c>, which the <c>BindStatement</c> chokepoint drains into a
    /// <c>BoundSequence</c> ahead of the carrying statement — "immediately before data is moved to the first of
    /// the receiving operands" (§14.9.25.4 GR1) and "at the beginning of the execution of the EVALUATE
    /// statement" (§14.9.13.4 GR3) are the same position in the generated code.</summary>
    /// <param name="op">The sending operand / selection subject to freeze.</param>
    /// <param name="tag">A short source tag for the temp's synthesized names (diagnostics and readability only).</param>
    /// <param name="use">How the value will be read (<see cref="ValueUse"/>); a NUMERIC function's intermediate is
    /// described FOR it (<see cref="IntermediatePic"/>). Defaults to a VALUE operand.</param>
    internal Place? Materialize(BoundOperand op, string tag, ValueUse use = default)
    {
        if (Model(op, use) is not { } model) return null;
        var temp = ctx.Data.CreateCompilerTemp(model.Item, "__SENDVAL-", "__sendval", tag);
        // ⛔ GR1's OTHER sentence, for a group whose extent is decided at run time: "The length of the data item
        // referenced by identifier-1 is evaluated only once, immediately before the data is moved to the first of
        // the receiving operands", and the paragraph after it names the mechanism — "The evaluation of the length
        // of identifier-1 or identifier-2 may be affected by the DEPENDING ON phrase of the OCCURS clause". The
        // clone carries the DESCRIPTION, so it has to carry the CONTROL VALUE too (kb/Work PB394).
        if (!FreezeOdoExtent(temp, model.Item, tag)) return null;
        // The run-time-length attributes are not part of the cloned DESCRIPTION (CreateCompilerTemp copies the
        // PICTURE and the description clauses); §8.5.1.10 is a storage property, set here for the carrier shapes.
        // DynLimit 0 means "this clone is FIXED-length", not "a maximum size of zero" — the two are different
        // facts and DynMaxSize carries only the second (§8.5.1.10.1), so a fixed-length clone keeps the real
        // default rather than being stamped with a bound no rule gives it (kb/Work PB463).
        temp.IsDynamicLength = model.DynLimit > 0;
        if (model.FunctionValue) temp.FunctionValueSeparator = ctx.Data.DecimalSeparator;   // PB2507: the unit's spelling of the literal form
        if (temp.IsDynamicLength) temp.DynMaxSize = model.DynLimit;
        if (ctx.Refs.ResolveItem(temp) is not { } place) return null;
        // The store is a plain BoundMove, NOT a re-entry into MoveBinder.BindMoveOf: the statement's own syntax
        // rules (the SR5 edition gates, the Table-16 legality, SR2's strong-typing check, SR9's §8.5.1.12
        // compatibility screen) are about the operands the PROGRAMMER wrote and have already been applied to
        // them; re-running them against the implementor's intermediate would report the same source twice and —
        // for a strongly-typed sender, whose clone is a DIFFERENT strongly-typed item (it carries the type-name,
        // kb/Work PB888, but is not the sender's declaration) — report a violation that no rule
        // states. The move itself is an identity copy by construction (the temp's description IS the sender's).
        // ⛔ AN INDEX DATA ITEM IS STORED BY SET, NEVER BY MOVE (kb/Work PB1661). A USAGE INDEX item is class index, not
        // category numeric (§13.16.3 SR10): §14.9.39 GR2 b) is what stores a value into one, and a MOVE into it is not that
        // store — `EVALUATE IDA WHEN I2 WHEN I1` materialized its subject into an index clone through a MOVE and
        // then compared a value that never arrived, so it took WHEN OTHER. The identity copy of an index data item into
        // its own class-index clone is the SET it names.
        ctx.Data.PendingPreOps.Add(IdentityStore(op, model.Item, place));
        return place;
    }

    /// <summary>The identity copy of <paramref name="op"/> into its own clone <paramref name="place"/>: a MOVE for
    /// every item MOVE can store, and the SET that names the copy for the classes MOVE does not store.
    /// <para>⛔ A CLASS-POINTER OR OBJECT-REFERENCE ITEM IS STORED BY SET, NEVER BY MOVE — the PB1661 index rule's
    /// siblings (kb/Work PB1060's sweep). A pointer, program-pointer, function-pointer or object reference is not a
    /// MOVE operand (§14.9.25.3 SR1: "The class of identifier-1 or identifier-2 shall not be index, message-tag,
    /// object, or pointer"); §14.9.39 Format 7 (data-pointer), Format 8
    /// (function-pointer), Format 9 (program-pointer) and Format 5 (object-reference) are what store a value into
    /// one. A MOVE into the clone stored NOTHING, so
    /// `SET P TO ADDRESS OF B` / `EVALUATE P WHEN Q … WHEN P …` — a subject read by two arms, hence materialized
    /// (§14.9.13.4 GR3) — compared a null clone and took WHEN OTHER: a wrong answer on conforming source.</para></summary>
    private static BoundStatement IdentityStore(BoundOperand op, DataItem model, Place place) =>
        (op, model.Pic?.Usage) switch
        {
            (BoundFieldOperand { Place: var src }, Usage.Index) =>
                new BoundSetTo([new SetPlaceTarget(place)], new BoundNumRef(src)),
            (BoundFieldOperand { Place: var src }, Usage.Pointer) =>
                new BoundSetPointer([new BoundPointerReceiver(place, null)], src, ToNull: false),
            (BoundFieldOperand { Place: var src }, Usage.ProgramPointer) =>
                new BoundSetProgramPointer([place], src, ToNull: false),
            (BoundFieldOperand { Place: var src }, Usage.FunctionPointer) =>
                new BoundSetFunctionPointer([place], src, ToNull: false),
            (BoundFieldOperand { Place: var src }, Usage.ObjectReference) =>
                new BoundSetObjectRef([place], src, SourceIsNull: false, SourceIsSelf: false),
            _ => new BoundMove(op, [place]),
        };

    /// <summary>
    /// ⛔ <b>THE TRUTH-VALUE ARM — the condition→boolean bridge</b> (kb/Work PB842 / PB912). ISO §14.9.13.4 GR3's
    /// stem assigns EVERY selection subject its value "at the beginning of the execution of the EVALUATE
    /// statement", and GR3 e) says what a condition subject is assigned: "Any selection subject specified by
    /// condition-1 is assigned a truth value according to the rules for evaluating conditional expressions".
    /// <see cref="Materialize"/> could hold a VALUE and nothing could hold a TRUTH value — <see cref="BoundCondition"/>
    /// and <see cref="BoundBoolExpr"/> are separate hierarchies — so a condition subject was re-bound and
    /// re-evaluated once per WHEN, and a user-defined function inside one was refused (COBOLNET1509) rather than
    /// over-activated.
    /// <para>The intermediate result item for a truth value is a ONE-POSITION BOOLEAN item: §8.8.4.3.4 GR1 —
    /// "Boolean-expression-1 evaluates true if the result of the expression is 1" — reads exactly such an item, so
    /// the store (<c>IF condition-1 → B"1" ELSE B"0"</c>, a statement-scoped PRE-op on the same
    /// <c>DataBinder.PendingPreOps</c> list, so it lands at the same "beginning of the execution" position as a
    /// value subject's store) and the read-back (<see cref="BoundBooleanCondition"/> over
    /// <see cref="BoundBoolRef"/>) are both ordinary nodes the emitter already renders. No new bound leaf.</para>
    /// <para>User-defined-function activations inside the condition stay on the pending list AHEAD of this
    /// store (they registered while the condition bound), so the statement hoist runs each of them once, before
    /// the truth value is computed from their temps — the exact §8.4.3.2.4 GR1 cardinality for a window evaluated
    /// once per statement. A non-first AND/OR operand's activations were already attached per evaluation by
    /// <c>ConditionBinder.BindFlatSequence</c> and travel INSIDE the stored condition (§8.8.4.13 r2).</para>
    /// <para>An already-diagnosed <see cref="BoundConditionError"/> is returned as it stands.</para>
    /// </summary>
    internal BoundCondition MaterializeTruth(BoundCondition truth, string tag)
    {
        if (truth is BoundConditionError) return truth;
        var model = new DataItem
        {
            Level = 1, CobolName = "__SENDTRUTH", CsName = "__sendtruth",
            Pic = new PicInfo(PicCategory.Boolean, Usage.Display, Length: 1, Digits: 0, Scale: 0, Signed: false),
        };
        var temp = ctx.Data.CreateCompilerTemp(model, "__SENDTRUTH-", "__sendtruth", tag);
        if (ctx.Refs.ResolveItem(temp) is not { } place) return truth;
        ctx.Data.PendingPreOps.Add(new BoundIf(truth, [StoreBit(place, "1")], [StoreBit(place, "0")]));
        return new BoundBooleanCondition(new BoundBoolRef(place));

        static BoundStatement StoreBit(Place p, string bit) => new BoundComputeBoolean(new BoundBoolLiteral(bit), [p]);
    }

    /// <summary>The BOOLEAN-EXPRESSION arm (kb/Work PB1423): store a boolean expression's value, of
    /// <paramref name="positions"/> boolean positions (§8.8.2 rule 10 — "the number of boolean positions of the
    /// larger item referenced"), into an intermediate boolean item, as a PRE-op on the same
    /// <c>DataBinder.PendingPreOps</c> — so a function argument that IS a boolean expression is evaluated at its place
    /// in the argument list (§8.4.3.2.4 GR2) and not after a later argument's activation changed an item it reads.
    /// The store is COMPUTE Format 2's own (<see cref="BoundComputeBoolean"/>), the channel a boolean expression has
    /// and <see cref="Materialize"/> deliberately does not reach. Null when the item cannot be resolved.</summary>
    internal Place? MaterializeBoolean(BoundBoolExpr expr, int positions, string tag)
    {
        var model = new DataItem
        {
            Level = 1, CobolName = "__SENDBOOL", CsName = "__sendbool",
            Pic = new PicInfo(PicCategory.Boolean, Usage.Display, Length: positions, Digits: 0, Scale: 0, Signed: false),
        };
        var temp = ctx.Data.CreateCompilerTemp(model, "__SENDBOOL-", "__sendbool", tag);
        if (ctx.Refs.ResolveItem(temp) is not { } place) return null;
        ctx.Data.PendingPreOps.Add(new BoundComputeBoolean(expr, [place]));
        return place;
    }

    /// <summary>
    /// ⛔ <b>THE EXTENT FREEZE</b> — §14.9.25.4 GR1's "The length of the data item referenced by identifier-1 is
    /// evaluated only once, immediately before the data is moved to the first of the receiving operands", for the
    /// one shape whose length is a run-time value the clone can hold: an <c>OCCURS … DEPENDING ON</c> table
    /// (§13.18.38 Format 2) beneath the sending group. §13.18.38.4 GR8 a) makes the group's extent "only that part
    /// of the table area that is specified by the value of the data item referenced by data-name-1", so the
    /// intermediate's own extent has to be pinned to data-name-1's value AT THE HOIST, not to whatever the value
    /// has become after the first receiver was stored.
    ///
    /// <para><b>What it does.</b> For every cloned node whose model carries a resolved DEPENDING item, a SECOND
    /// compiler temp is minted from data-name-1's own description, a store of data-name-1 into it is registered as
    /// a PRE-op <i>ahead of</i> the group store (<see cref="Materialize"/> adds the group store after this call
    /// returns, so the order is the rule's own: freeze the length, then move the data), and the clone's
    /// <see cref="OccursSpec.Depending"/> is repointed at it — which is what <c>ReferenceResolver.WrapIfOdoGroup</c>
    /// reads to build the GR8 extent slice, so the temp's every use inherits the frozen length with no second
    /// mechanism. The clone's <see cref="OccursSpec.DependingName"/> deliberately stays data-name-1 AS WRITTEN: it
    /// is a description-clause fact (the temp's description IS identifier-1's, §14.9.25.4 GR1), nothing re-resolves
    /// a name after <c>DataBinder.OdoResolve</c>, and the RESOLVED item is the only thing the extent is read from.</para>
    ///
    /// <para><b>Measured</b> (kb/Work PB394's own repro): a 3-of-5 <c>OCCURS 1 TO 5 DEPENDING ON N</c> group into
    /// <c>N, Z</c> where <c>N</c> is the first receiver gave <c>Z=[1    ]</c> — the second receiver saw a
    /// ONE-occurrence group — and gives <c>Z=[125  ]</c> once the extent is frozen.</para>
    ///
    /// <para>Returns <see langword="false"/> when data-name-1 cannot be resolved to a place of its own (it is
    /// within a table, so a reference to it needs subscripts this hoist has none of) — the caller then leaves the
    /// operand un-materialized, i.e. exactly the behaviour that stood before, rather than freezing it at a length
    /// no rule gives.</para>
    /// </summary>
    private bool FreezeOdoExtent(DataItem temp, DataItem model, string tag)
    {
        if (model.OccursSpec is { Depending: { } dep } && temp.OccursSpec is { } clonedSpec)
        {
            if (ctx.Refs.ResolveItem(dep) is not { } depPlace) return false;
            var frozen = ctx.Data.CreateCompilerTemp(dep, "__SENDODO-", "__sendodo", tag);
            if (ctx.Refs.ResolveItem(frozen) is not { } frozenPlace) return false;
            ctx.Data.PendingPreOps.Add(new BoundMove(new BoundFieldOperand(depPlace), [frozenPlace]));
            clonedSpec.Depending = frozen;
        }
        // The clone is node-for-node the model's (DataBinder.CloneTempNode walks Children in order), so the
        // parallel walk is exact; the bound is defensive only.
        for (int i = 0; i < model.Children.Count && i < temp.Children.Count; i++)
            if (!FreezeOdoExtent(temp.Children[i], model.Children[i], tag)) return false;
        return true;
    }

    /// <summary>The intermediate result item's description: the cloned model plus, for a run-time-length
    /// carrier, its §8.5.1.10 limit (0 = a fixed-length clone). Null when the operand is not materialized.
    /// <paramref name="FunctionValue"/> marks the §15.4 temporary of a NUMERIC function's returned value, whose
    /// character image is DOC-A.1-92's literal form (<see cref="DataItem.FunctionValueSeparator"/>).</summary>
    private readonly record struct TempModel(DataItem Item, int DynLimit, bool FunctionValue = false);

    /// <summary>⛔ THE ONE description derivation — every <see cref="BoundOperand"/> leaf is named, so a leaf
    /// added tomorrow is a COMPILE error here rather than a silent fall-through to "no hoist" (the
    /// feedback_a_dead_lookup_is_also_unverified shape: a default arm that quietly means "correct as it is").</summary>
    private TempModel? Model(BoundOperand op, ValueUse use) => op switch
    {
        // §8.3.3.6.4 GR2 sizes a figurative / ALL "literal" from the RECEIVER, and §14.9.25.4 GR1's antecedent
        // names neither; a literal's value cannot change. No description of their own ⇒ no intermediate.
        BoundNumericLiteral or BoundStringLiteral or BoundFigurative or BoundAllLiteral => null,
        BoundOperandError => null,                 // already diagnosed
        BoundPredefinedNull => null,               // §8.4.3.10.1 — a predefined address, no storage to change
        BoundBoolOperand => null,                  // the §8.8.2 boolean-expression channel (COMPUTE Format 2)
        // An address-identifier (kb/Work PB1021) is "a unique data item of class pointer" CREATED at the reference
        // (§8.4.3.11.4 GR1 / §8.4.3.13.4 GR1) — it has no storage an earlier store could change.
        BoundAddressOperand => null,
        BoundFieldOperand f => OfPlace(f.Place),
        BoundCurrentRecord cr => OfPlace(cr.Area),
        BoundComputedOperand c => OfComputed(c, use),
        // Every leaf of the hierarchy is named above; the arm exists because C# cannot prove a public abstract
        // record's leaf set is closed. It is LOUD rather than a silent "no hoist", which would let a new operand
        // form inherit the very defect this class removes (feedback_a_dead_lookup_is_also_unverified).
        _ => throw new InvalidOperationException(
            $"SendingValueTemp: the BoundOperand leaf '{op.GetType().Name}' has no §14.9.25.4 GR1 / §14.9.13.4 "
            + "GR3 intermediate-result description — name it in SendingValueTemp.Model"),
    };

    /// <summary>A data item's intermediate: the item's own description, or §8.4.3.3.4 GR6's "unique data item"
    /// for a reference-modified one.</summary>
    private TempModel? OfPlace(Place place)
    {
        // A reference modifier's LENGTH is an arithmetic expression (§8.4.3.3.3 SR4) — a run-time value — so the
        // unique data item GR6 describes is carried by a run-time-length item, whose current length IS the slice
        // length (§8.5.1.10.4). Its class/category/usage are GR6's, read through the ONE reader.
        if (place is RefModPlace rm)
            return CharacterCarrier("__SENDVAL-REFMOD", "__sendvalRefMod", rm.Category, UsageOf(rm.Inner.Item),
                // ⛔ THE CAPACITY IS COUNTED IN THE POSITIONS REFERENCE MODIFICATION INDEXES (§8.4.3.3.4 GR5 a),
                // through the ONE reader — never in the item's character OCCUPANCY (kb/Work PB886). A slice of a
                // USAGE BIT item may be up to its BOOLEAN-position count long and a slice of a DYNAMIC LENGTH item
                // up to its §13.18.19.4 GR2 LIMIT, both of which ImageWidth answers with a smaller number (1 and 0),
                // so the intermediate silently truncated the value it exists to preserve. An ANY LENGTH item's
                // description fixes no bound at all (§13.18.2.4 GR1 b)): its slice's capacity is the carrier's
                // ceiling, never the one position its PICTURE spells (kb/Work PB3128).
                limit: Math.Max(1, rm.MaximumInnerPositions));
        var item = place.Item;
        // A DYNAMIC-LENGTH elementary sender IS frozen exactly, by the carrier the standard defines for it:
        // §8.5.1.10.4 — "A dynamic-length elementary item that is used as a sending operand … is treated as a
        // fixed-length data item whose length is the dynamic-length elementary item's current length" — so the
        // intermediate is a dynamic-length item of the same limit, and GR1's "The length of the data item
        // referenced by identifier-1 is evaluated only once" holds for it.
        if (item is { IsDynamicLength: true, IsGroup: false })
            return new TempModel(item, DynLimit: Math.Max(1, item.DynMaxSize));
        // ⛔ A §8.5.1.12 VARIABLE-LENGTH GROUP IS STILL NOT FROZEN BY A CLONE, AND THAT IS MEASURED, NOT ASSUMED.
        // The intermediate result item is a CLONED DESCRIPTION, and a clone of a dynamic-length member carries
        // neither its limit nor its current length, while a dynamic-capacity table's clone starts at capacity zero
        // (§8.5.1.9) and is not image-capable at all — so the clone's extent is a DIFFERENT number from the
        // sender's, and §14.9.25.4 GR1 requires the sender's. Measured on this tree with a 3-of-5 occurs-depending
        // group into a JUSTIFIED PIC X(5) receiver: the current-extent answer is "  125" (§13.18.38.4 GR8 a)'s
        // extent, right-aligned by §13.18.34) and a maximum-extent intermediate gives "125  ".
        // <para>So THIS shape is left UN-MATERIALIZED — its sending operand is still re-read per receiver, the
        // pre-existing state — and is recorded as the residual on row GR-14.9.25.4-1 rather than papered over.
        // The OCCURS DEPENDING shape, which used to share this arm, no longer does: its control value IS
        // representable, and <see cref="FreezeOdoExtent"/> freezes it (kb/Work PB394).</para>
        if (item.IsGroup && VariableLengthCompatibility.IsVariableLength(item))
            return null;
        return new TempModel(item, DynLimit: 0);
    }

    /// <summary>The intermediate result item of a function-identifier's returned value. A NUMERIC result takes the
    /// description <see cref="IntermediatePic"/> derives (§15.4.1 leaves it to the implementor); an alphanumeric / national /
    /// boolean result has a run-time length (FUNCTION TRIM's is its argument's content, CONCAT's the sum of its
    /// arguments') and takes the §8.5.1.10.4 carrier.</summary>
    private TempModel? OfComputed(BoundComputedOperand c, ValueUse use)
    {
        // §15.2 classifies every function; the bound node carries the call's RESOLVED category
        // (BoundIntrinsicCall.ResultCategory — never the declared column, for the twenty argument-typed rows).
        // A folded arithmetic sum (FUNCTION LENGTH of a variable-length group, §15.50.4 r7) and every other
        // arithmetic-expression operand are numeric by §8.8.1.
        PicCategory cat = c.Expr is BoundIntrinsicCall ic ? ic.ResultCategory : PicCategory.Numeric;
        // ⛔ THE TEMPORARY IS THE RETURNED VALUE, SO IT MUST MOVE AS THE FUNCTION MOVES (kb/Work PB1007). §14.9.25.4
        // GR1's equivalence — MOVE a TO temp / MOVE temp TO b / MOVE temp TO c — is exact only if `MOVE temp TO b`
        // stores what `MOVE a TO b` stores, and for a character receiver that is DOC-A.1-92's literal form of the
        // value: the temporary is FLAGGED (FunctionValueSeparator) so its text image is that form, and an INTEGER
        // function (§15.2 item 5, "no digits to the right of the decimal point", resolved per call by the ONE
        // IntrinsicResultType reader) is described at scale 0, so the literal form carries no fraction digits — the
        // form the function itself renders. It used to be the 30-digit image of FunctionValuePic, left-justified:
        // `MOVE FUNCTION INTEGER(N) TO A B` stored 0000 in both where `MOVE FUNCTION INTEGER(N) TO A` stores 3.
        if (cat is PicCategory.Numeric)
            return new TempModel(
                new DataItem { Level = 1, CobolName = "__SENDVAL-FN", CsName = "__sendvalFn", Pic = IntermediatePic(c, use) },
                DynLimit: 0, FunctionValue: true);
        return CharacterCarrier("__SENDVAL-FNS", "__sendvalFns", cat, CharacterUsage(cat), FunctionTextLimit);
    }

    /// <summary>THE ONE description of a RUN-TIME-LENGTH character carrier (§8.5.1.10.4 — a dynamic-length
    /// elementary item "used as a sending operand … is treated as a fixed-length data item whose length is the
    /// dynamic-length elementary item's current length"): a PICTURE of the category and usage with no static
    /// length, plus the §8.5.1.10 limit. The reference-modified sender, the character function result and the
    /// conceptual item of <see cref="ConceptualCharacterItem"/> are all this one shape.</summary>
    private static TempModel CharacterCarrier(string cobolName, string csName, PicCategory category, Usage usage, int limit) =>
        new(new DataItem
            {
                Level = 1, CobolName = cobolName, CsName = csName,
                Pic = new PicInfo(category, usage, Length: 0, Digits: 0, Scale: 0, Signed: false),
            },
            DynLimit: limit);

    /// <summary>The usage of a character category's carrier: NATIONAL for category national, DISPLAY otherwise.</summary>
    private static Usage CharacterUsage(PicCategory category) =>
        category is PicCategory.National ? Usage.National : Usage.Display;

    /// <summary>
    /// ⛔ <b>THE CONCEPTUAL SENDING ITEM</b> of a statement whose own general rule DEFINES an elementary sender and
    /// then stores it "according to the rules for the MOVE statement" (kb/Work PB979). ISO §14.9.48.4 GR11 c):
    /// "The characters examined, excluding any delimiting characters, shall be treated as an elementary national
    /// data item if identifier-1 is of category national, and otherwise as an elementary alphanumeric data item,
    /// and shall be moved into the current receiving area according to the rules for the MOVE statement" — and
    /// GR11 d) says the same of the delimiting characters.
    /// <para>The item has a RUN-TIME length (the number of characters examined), so it is the
    /// <see cref="CharacterCarrier"/> shape, limited only by the largest character value the runtime carries. The
    /// statement's emitter writes the characters into it and the receiver's store is an ordinary
    /// <see cref="BoundMove"/> from it, BOUND through <c>MoveBinder.BindMoveOf</c> — so every receiver shape the
    /// MOVE statement knows (ANY LENGTH, reference-modified, dynamic-length, national, group, JUSTIFIED, numeric)
    /// is stored by the one MOVE mechanism rather than by a verb-private copy of it.</para>
    /// <para>Unlike <see cref="Materialize"/> it registers NO pre-op: the item's value is produced INSIDE the
    /// statement, once per receiving area, by the carrying statement itself.</para></summary>
    /// <param name="category">Category alphanumeric or national — GR11 c)'s two cases.</param>
    /// <param name="tag">A short source tag for the temp's synthesized names.</param>
    internal Place? ConceptualCharacterItem(PicCategory category, string tag)
    {
        var model = CharacterCarrier("__CONCEPT", "__concept", category, CharacterUsage(category),
                                     CobolNet.Runtime.CobolDynString.MaxLength);
        var temp = ctx.Data.CreateCompilerTemp(model.Item, "__CONCEPT-", "__concept", tag);
        temp.IsDynamicLength = true;
        temp.DynMaxSize = model.DynLimit;
        return ctx.Refs.ResolveItem(temp);
    }

    /// <summary>The implementor's maximum length for a §15.4 returned value of a character category — the bound
    /// §15.4 names ("If the length of the returned value exceeds the maximum length specified by the implementor
    /// for a returned value, an EC-ARGUMENT-FUNCTION exception condition is set to exist"). 32 767 character
    /// positions, the largest fixed-length alphanumeric item this compiler admits.</summary>
    private const int FunctionTextLimit = 32767;

    /// <summary>The usage the §8.4.3.3.4 GR6 unique data item inherits ("the same class, category, and usage as
    /// that defined for identifier-1"), through THE ONE §8.5.2.1 usage reader.
    /// <para>⛔ NOT <c>inner.Pic?.Usage ?? Usage.Display</c>, which is what this was (kb/Work PB411's sibling
    /// sweep). §8.4.3.3.3 SR5 permits reference modification of "a data item of class alphanumeric, boolean, or
    /// national", so identifier-1 may be a GROUP-USAGE NATIONAL group — whose <c>Pic</c> is null and whose usage
    /// is NATIONAL (§13.18.29.4 GR2 b) — and the raw <c>Pic</c> read gave its intermediate usage DISPLAY beside
    /// the category NATIONAL <see cref="RefModPlace.Category"/> already answers: one item, two usages. The
    /// reader settles it once. DISPLAY remains the fallback for the shapes §8.5.2.1 states no usage for (a
    /// strongly-typed or variable-length group), where GR6 still owes the intermediate one.</para></summary>
    private static Usage UsageOf(DataItem inner) => ItemCategory.UsageOf(inner) ?? Usage.Display;

}
