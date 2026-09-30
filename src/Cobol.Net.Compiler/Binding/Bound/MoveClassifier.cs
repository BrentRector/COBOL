// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Model;

namespace CobolNet.Binding.Bound;

/// <summary>The per-TARGET dispatch class of one MOVE store (P7 Step 7; ISO §14.9.25.4 GR4 decides
/// elementary-vs-group FIRST, sender side included). Computed ONCE by <see cref="MoveClassifier"/> when the
/// <see cref="BoundMove"/> is constructed — the emitter renders by kind and re-derives NOTHING. Deviation from
/// the phase doc's sketch (recorded in the ledger, audit-mandated): the kind is PER TARGET, not scalar (one MOVE
/// can have a ref-mod slice, a group, and an elementary receiver simultaneously), and the STORAGE FORM does not
/// travel on the node — <c>DataItem.Storage</c> is null while the binder runs (StorageFormPass is a group-tail
/// pass, and the MOVE's own bind records the facts that decide it); the emitter reads the by-then-settled
/// <c>item.Storage</c> projection instead.</summary>
public enum MoveKind
{
    /// <summary>A reference-modified receiver: the slice takes the source characters (or a figurative fills
    /// every slice position, §8.3.3.6.4 GR2 / §8.4.3.3 GR5-6).</summary>
    RefModSlice,
    /// <summary>A GROUP receiver — the §14.9.25.4 GR4 group move (no conversion; filled without consideration
    /// for subordinate items).</summary>
    Group,
    /// <summary>A GROUP SENDER into an elementary receiver — still a group move by GR4 (the sender side of the
    /// elementary-vs-group test; never receiver-category conversion/editing).</summary>
    GroupToElementary,
    /// <summary>An alphanumeric figurative (S/Q/H/L) or non-digit ALL "literal" into an ELEMENTARY NUMERIC
    /// receiver — the pre-2023 removed construct's fill semantics (§14.9.25.3 SR5; Annex E.2 item 1). Whether
    /// the fill can be stored (image-backed) or is the narrow loud residue is an EMIT-time storage question.</summary>
    FigurativeToNumericImage,
    /// <summary>The ordinary elementary move — rendered by the receiver-category conversion/editing table
    /// (§14.9.25.3 Table 16 / GR5).</summary>
    Convert,
}

/// <summary>ONE receiving operand's bound store: the SENDING operand as ISO §14.9.25.4 GR2/GR3 leave it for
/// THIS receiver (<see cref="MoveClassifier.Sender"/> — the zero-length-literal substitution is per-receiver,
/// because its own exclusion is), paired with the dispatch <see cref="MoveKind"/> classified over that
/// substituted sender. The pair travels together on <see cref="BoundMove.Stores"/> so a per-(sender, receiver)
/// rule is written ONCE, at construction, and every consumer — the emitter's renderer, the binder's storage
/// marking — reads the same answer instead of re-deriving it (kb/Work PB425; feedback_one_rule_one_place).</summary>
public readonly record struct MoveStore(BoundOperand Sender, MoveKind Kind, MoveSenderOrigin Origin);

/// <summary>Where one store's SENDING operand came from: the operand the programmer WROTE, or the figurative
/// constant an ISO §14.9.25.4 substitution rule put in its place. A BIND-time fact, carried rather than
/// re-derived, for two reasons. It is not recoverable from the sender alone — a written <c>MOVE SPACE</c> and a
/// substituted zero-length literal both arrive as <c>BoundFigurative 'S'</c> — and the only way to recover it at
/// emit time is to read <c>BoundMove.Source</c>, which is precisely the scalar read the per-target store exists
/// to eliminate. It decides which RULE a diagnostic about the stored value names: the pre-2023 residue
/// §14.9.25.3 SR5 removed, or the substitution the standard still requires on legal source
/// (kb/Work PB425).</summary>
public enum MoveSenderOrigin
{
    /// <summary>The sending operand as written — identifier-1, literal-1, or a figurative constant spelled in
    /// the source.</summary>
    Written,
    /// <summary>§14.9.25.4 GR2/GR3 — the figurative constant substituted for a zero-length LITERAL-1.</summary>
    ZeroLengthLiteral,
    /// <summary>§14.9.25.4 GR1 — the figurative constant substituted, at run time, when identifier-1 turns out
    /// to be a zero-length ITEM (§8.5.4); it reaches GR2/GR3 through GR1's "as if literal-1 were specified as
    /// a zero-length literal".</summary>
    ZeroLengthItem,
}

/// <summary>THE MOVE-dispatch classifier (P7 Step 7): one pure function over bind-time facts, the single
/// authority for <see cref="MoveKind"/> AND for the §14.9.25.4 GR2/GR3 sending-operand substitution — called by
/// <c>BindMove</c> AND by the emitter's synthetic implicit-MOVE constructions (READ INTO / WRITE FROM /
/// RETURN INTO / INITIALIZE / CORRESPONDING / function RETURNING — the same classification, never a second
/// table; feedback_one_mechanism_per_job).</summary>
public static class MoveClassifier
{
    /// <summary>
    /// ⛔ <b>THE ZERO-LENGTH-LITERAL SUBSTITUTION — ISO §14.9.25.4 GR2 and GR3, written down ONCE</b>
    /// (kb/Work PB425). GR2: "If literal-1 is an alphanumeric or national zero-length literal and the receiving
    /// operand is other than a dynamic-length elementary item, literal-1 is treated as if it were the figurative
    /// constant SPACE." GR3: the same sentence for "a boolean zero-length literal" and the figurative constant
    /// ZERO. Only the EXCLUSION half used to exist (the emitter's dynamic-length store); the substitution itself
    /// was implemented nowhere, and it COINCIDES with a plain empty sender for most receivers — which is why it
    /// survived a large corpus and diverged exactly where the substituted figurative's own value is not a space:
    /// <c>MOVE "" TO PIC 9(3)</c> stored 000 where <c>MOVE SPACE</c> stores the space fill, and
    /// <c>MOVE B"" TO PIC X(3)</c> stored spaces where figurative ZERO stores "000" (§8.3.3.6.4 GR4 —
    /// "one or more of the character '0' in the computer's runtime coded character set").
    ///
    /// <para><b>It is per RECEIVER</b>, because GR2/GR3's own exclusion is: <c>MOVE "" TO D-DYN, N-NUM</c>
    /// leaves the dynamic-length item at length zero (§8.5.1.10.4) AND space-fills the numeric one, from one
    /// statement. A REFERENCE-MODIFIED receiver is never "a dynamic-length elementary item": the receiving
    /// operand is then the unique elementary alphanumeric data item of §8.4.3.3.4 GR6, so the substitution
    /// applies to it whatever the underlying item is.</para>
    ///
    /// <para>⛔ <b>It is applied AFTER the syntax screens, never to them.</b> §14.9.25.3 SR5 (the figurative →
    /// numeric prohibition ISO 2023 introduced), SR6 and SR7 are SYNTAX rules over a CLOSED LIST of figurative
    /// constants WRITTEN IN THE SOURCE — "SPACE, QUOTE, HIGH-VALUE, LOW-VALUE, ALL "literal", or ALL
    /// symbolic-character" — and a zero-length literal is not one of them. GR2/GR3 are GENERAL rules about the
    /// value moved. Feeding the rewrite to those gates would turn <c>MOVE "" TO PIC 9(3)</c> — which the
    /// standard permits at every edition — into a COBOLNET0902 rejection, i.e. a wrong answer traded for a false
    /// rejection. The screens run on <c>BoundMove.Source</c> (the written operand) and this runs on
    /// <c>BoundMove.Stores</c>; <c>VersionConformancePass.GateMove</c> likewise re-derives from
    /// <c>m.Source</c>.</para>
    /// </summary>
    public static BoundOperand Sender(BoundOperand source, Place target) =>
        source is BoundStringLiteral { Value.Length: 0 } zl && SubstitutesForZeroLength(target)
            // GR3 for the boolean literal, GR2 for the alphanumeric and national ones — BoundStringLiteral is the
            // ONE node for all three categories, so the two rules are one two-armed answer rather than two screens.
            ? new BoundFigurative(zl.Category is PicCategory.Boolean ? 'Z' : 'S')
            : source;

    /// <summary>
    /// ⛔ <b>GR1's ZERO-LENGTH-ITEM clause — the RUNTIME half of the same substitution</b> (kb/Work PB425).
    /// ISO §14.9.25.4 GR1: "If identifier-1 is a zero-length item, it is as if literal-1 were specified as a
    /// zero-length literal" — which lands in GR2/GR3 above. A zero-length item (§8.5.4) is a RUNTIME state,
    /// not a description, so this cannot be decided at bind time; it returns the sending PLACE whose current
    /// length the emitted store must test, or <see langword="null"/> when no test is needed.
    ///
    /// <para>⛔ <b>THE RECEIVER FILTER IS THE RULE'S OWN SET, NOT A LIST OF CATEGORIES</b> (kb/Work PB943). GR2/GR3
    /// substitute "if … the receiving operand is other than a dynamic-length elementary item" — a SET the code can
    /// ask directly (<see cref="SubstitutesForZeroLength"/>, the same predicate <see cref="Sender"/> asks for a
    /// written literal). The filter used to be NUMERIC and NUMERIC-EDITED receivers only, excused by a doc
    /// comment asserting that for every other category "the two readings are the same store". That argument
    /// held only for an ELEMENTARY ALPHANUMERIC or NATIONAL sender into an unedited receiver, and three measured
    /// counter-examples refute it: a zero-length GROUP sender is a GR4 group move (no editing, alphanumeric
    /// fill), so <c>MOVE ZG TO PIC XX/XX</c> stored five spaces where GR1's SPACE edits to <c>"  /  "</c>; a
    /// zero-length BIT GROUP sender substitutes GR3's ZERO, so <c>MOVE BG TO PIC X(3)</c> stored three spaces
    /// where ZERO stores <c>"000"</c>; and into a boolean receiver a group move deposits alphanumeric spaces
    /// where SPACE is D-B2's boolean zero. Asking the rule makes the next receiver kind automatic: the zero arm is
    /// the statement's own dispatch over the substituted figurative (<c>MoveEmitter.EmitStore</c>), so whatever
    /// SPACE / ZERO means for that receiver is what it stores.</para>
    /// <para>⚠ <b>DETERMINATION — a GR9 move keeps GR9.</b> When both operands are group items and one is a
    /// VARIABLE-LENGTH group, §14.9.25.3 SR9 admits the statement only because both are compatible GROUPS, and
    /// §14.9.25.4 GR9 then defines the move component by component — including a sender whose components are
    /// all empty (its step 1 sets a receiving dynamic-length item's length to zero). Rewriting that sender into
    /// a figurative would produce a statement SR9 forbids (a figurative is not a compatible group), so the
    /// specific rule governs and no test is emitted. Rejected reading: GR1 over GR9, which fills a receiving
    /// dynamic-length member with a space GR9 says it does not hold.</para>
    /// <para><b>The sender filter is total</b>, because everything else is normalized into it: the two
    /// RUNTIME-LENGTH elementary shapes are a DYNAMIC LENGTH item (§8.5.4 item 4) and an ANY LENGTH item
    /// (item 3), and <c>MoveBinder</c> materializes a reference-modified (item 9) or function-identifier (item 6)
    /// sender into <c>SendingValueTemp</c>'s dynamic-length carrier BEFORE constructing the move — and only when
    /// §8.5.4 lets that shape actually BE zero-length (<see cref="NeedsLengthFreeze"/>) — which GR1's own
    /// "evaluated only once" sentence requires anyway, and which is what makes reading the length here a
    /// second time safe.</para>
    ///
    /// <para>⛔ <b>AND THE GROUP-SHAPED ITEMS TOO, BECAUSE GR1 CHANGES THE MOVE'S KIND</b> (kb/Work PB896).
    /// §8.5.4's items 1, 2, 5 and 7 make a GROUP a zero-length item, and this arm used to exclude them on the
    /// grounds that they "send through §14.9.25.4 GR4's group move" — but that is the defect, not the reason:
    /// GR1's substitution replaces identifier-1 with a zero-length LITERAL, and GR4's first sentence — "Any move
    /// in which the sending operand is either a literal or an elementary item and the receiving item is an
    /// elementary item is an elementary move" — then makes the statement an ELEMENTARY move. The KIND changes,
    /// so the group path cannot carry the rule. Measured: <c>MOVE ZG TO R-NUM</c> with a zero-occurrence
    /// occurs-depending group stored <c>000</c> where the identical <c>MOVE "" TO R-NUM</c> stores spaces, and
    /// GR1 makes those two statements the same statement. The predicate is <see cref="DataItem.MinimumLengthIsZero"/>
    /// — §8.5.4's stem read as one structure — never a copy of its four group bullets.</para>
    /// <para>⚠ Which of the four is REACHABLE here is §14.9.25.3 SR9's business, not this rule's: a
    /// VARIABLE-LENGTH group (items 5 and 7, and any group holding a dynamic-length member) may move only to or
    /// from a compatible GROUP, so it never reaches an elementary receiver and COBOLNET1931 refuses it first.
    /// Item 1 — the occurs-depending group with integer-1 zero — is the shape that gets here, and the predicate
    /// covers the others without a second site if SR9 ever admits one.</para>
    /// </summary>
    public static ZeroLengthSender? ZeroLengthItemRoute(BoundOperand source, Place target) =>
        source switch
        {
            BoundFieldOperand { Place.DenotedItem: not null } f
                when SubstitutesForZeroLength(target)
                && !IsVariableLengthGroupMove(f.Place, target)
                && (f.Place.Item is { IsGroup: false } si ? si.IsDynamicLength || si.IsAnyLength
                                                          : f.Place.Item.MinimumLengthIsZero)
                => new ZeroLengthSender(f.Place, null),
            // ⛔ §8.5.4 item 5 — "A logical record that has been specified using the variable-length or the
            // fixed-or-variable-length format of the RECORD clause in which the number of characters positions is
            // zero" — IS a zero-length item, and the sender of READ / RETURN INTO over such a file is the CURRENT
            // RECORD (a BoundCurrentRecord, PB339), not a field: the route used to match BoundFieldOperand alone, so
            // a zero-length record was stored as an empty alphanumeric slice with no SPACE substitution and no
            // editing (kb/Work PB1409: `READ F INTO PIC XX/XX` left five raw spaces where GR2's SPACE edits to
            // "  /  "). "Whose minimum length is zero" (§8.5.4's stem) is the file's minimum record size (VaryMin).
            BoundCurrentRecord { Area.DenotedItem: not null } cr
                when SubstitutesForZeroLength(target)
                && cr.File.VaryMin == 0
                && !cr.AlphanumericGroupMove   // Format 2 (RECORD VARYING): §14.9.30.4 4) b) designates the implied move an alphanumeric GROUP move — no substitution
                && !IsVariableLengthGroupMove(cr.Area, target)
                => new ZeroLengthSender(cr.Area, cr),
            _ => null,
        };

    /// <summary>The sending operand <see cref="ZeroLengthItemRoute"/> found and the length the emitted test reads:
    /// the sending <see cref="Place"/> (its item's category picks GR2's SPACE or GR3's ZERO, and a field's own
    /// current length is read off it), or — for a READ / RETURN INTO current record (§8.5.4 item 5) — the
    /// <see cref="CurrentRecord"/> operand whose length is GR16's byte count, not any field's.</summary>
    public readonly record struct ZeroLengthSender(Place Place, BoundCurrentRecord? CurrentRecord);

    /// <summary>§14.9.25.4 GR9's antecedent — "If both the sending operand and the receiving data item are group
    /// items and one or both is a variable-length group" — asked of a sending PLACE, for
    /// <see cref="ZeroLengthItemRoute"/>'s determination. A level-66 THROUGH alias is a group item
    /// (§13.18.45.4 GR2), so the CATEGORY question is asked, never the structural one — the same predicate
    /// <c>MoveEmitter.VariableLengthGroupMove</c> and <c>StatementValidation.CheckVariableLengthMove</c> ask.</summary>
    private static bool IsVariableLengthGroupMove(Place sender, Place target) =>
        target.DenotedItem is not null
        && ItemCategory.IsGroupItem(sender.Item) && ItemCategory.IsGroupItem(target.Item)
        && (VariableLengthCompatibility.IsVariableLength(sender.Item)
            || VariableLengthCompatibility.IsVariableLength(target.Item));

    /// <summary>The figurative constant GR1's route substitutes for a zero-length SENDING ITEM — GR2's SPACE for
    /// an alphanumeric or national item, GR3's ZERO for a boolean one, read off the sending item's own category
    /// exactly as <see cref="Sender"/> reads it off the literal's (one rule, one two-armed answer).</summary>
    public static BoundFigurative ZeroLengthItemFigurative(ZeroLengthSender sender) =>
        new(sender.Place.Item.OperandPic?.Category is PicCategory.Boolean ? 'Z' : 'S');

    /// <summary>True when <paramref name="source"/> is a sending operand GR1's zero-length-item clause can
    /// reach but whose LENGTH is not a stable field read — a reference-modified or function-identifier operand
    /// (§8.5.4 items 9 and 6). <c>MoveBinder</c> materializes those through <c>SendingValueTemp</c> so the
    /// reference modifier / function is "evaluated only once" (GR1) and the runtime length test below reads an
    /// intermediate result item rather than re-running the expression.
    ///
    /// <para>⛔ <b>The shape is not the whole antecedent — the operand must be able to BE a zero-length item</b>
    /// (<see cref="CanBeZeroLengthItem"/>). GR1's own sentence is conditional — "If identifier-1 <i>is</i> a
    /// zero-length item" — and §8.5.4's enumeration makes both of these shapes conditional in turn. Freezing on
    /// the shape ALONE was a wrong answer, not a wasted temp: a numeric function-identifier sender was hoisted
    /// into the §15.4 numeric temporary, whose implementor description (<c>SendingValueTemp.FunctionValuePic</c>,
    /// 21 integer + 9 fraction digits) is narrower than a STANDARD-DECIMAL or float result, so
    /// <c>MOVE FUNCTION E TO a 31-digit item</c> stored 2718281828000000000000000000000 for
    /// 2718281828459045235360287471352 — the freeze re-rounded a value that can never be zero-length.</para></summary>
    public static bool NeedsLengthFreeze(BoundOperand source, IReadOnlyList<Place> targets)
    {
        // The receiver half is the ROUTE's own predicate, never a second copy of it: a freeze exists only so the
        // route has a field to read, and a receiver the route would test is a receiver that needs one.
        if (!CanBeZeroLengthItem(source)) return false;
        foreach (var t in targets)
            if (SubstitutesForZeroLength(t)) return true;
        return false;
    }

    /// <summary>ISO §8.5.4's enumeration, asked of the two sending shapes whose length is NOT a stable field
    /// read — the only ones the freeze above exists for. The other §8.5.4 items need no intermediate: a DYNAMIC
    /// LENGTH or ANY LENGTH item (items 4 and 3) carries its own length field, which
    /// <see cref="ZeroLengthItemRoute"/> reads directly, and the group-shaped ones (items 1, 2, 5 and 7) are a
    /// field read too — their CURRENT EXTENT, which the same route tests in place — so none of them needs an
    /// intermediate either (kb/Work PB896).</summary>
    private static bool CanBeZeroLengthItem(BoundOperand source) => source switch
    {
        // §8.5.4 item 9: "A reference-modified data item that has resolved to a length of zero, WHEN THAT HAS
        // BEEN PERMITTED BY USE OF THE COMPILER DIRECTIVE REF-MOD-ZERO-LENGTH." The trailing qualifier is the
        // rule, not an aside: outside such a region §7.3.23.3 GR1 — "when this directive is omitted or is
        // specified as off, then when reference-modification results in a zero-length data item, the exception
        // condition EC-BOUND-REF-MOD is raised" — produces an exception where item 9 would otherwise produce a
        // zero-length item, so GR1's antecedent can never be satisfied and there is nothing to freeze.
        BoundFieldOperand { Place: RefModPlace rm } => rm.AllowZeroLength,
        // §8.5.4 item 6: "An intrinsic function that returns a zero-length value." A NUMERIC returned value
        // never is one — §15.4 puts it in "a temporary elementary data item" of the implementor's numeric
        // description (§15.4.1), which has at least one digit position — so only a function of a character
        // category, whose returned length follows its arguments, qualifies (FUNCTION TRIM of an all-space
        // argument, §15.96.4 r4: "the returned value is of length zero"). The call's RESOLVED category is read,
        // never the catalog column, because twenty rows are argument-typed (BoundIntrinsicCall.ResultCategory —
        // the same one fact SendingValueTemp.OfComputed reads to pick the carrier).
        BoundComputedOperand { Expr: BoundIntrinsicCall ic } => ic.ResultCategory is not PicCategory.Numeric,
        // Every other operand form is either a stable field read or cannot be a zero-length item at all. A
        // non-intrinsic BoundComputedOperand is an arithmetic expression, numeric by §8.8.1.
        _ => false,
    };

    /// <summary>GR2/GR3's exclusion: "the receiving operand is other than a dynamic-length elementary item"
    /// (ISO §8.5.1.10 / §13.18.19 — the item whose current length the store REPLACES rather than pads, so a
    /// zero-length sender legitimately leaves it empty). A ref-mod receiver is the elementary ALPHANUMERIC
    /// unique item of §8.4.3.3.4 GR6, never the dynamic-length item underneath it.
    /// <para>⛔ ASKED BY ALL THREE ARMS OF THE SUBSTITUTION — the written literal (<see cref="Sender"/>), the
    /// zero-length item (<see cref="ZeroLengthItemRoute"/>) and the freeze that serves it
    /// (<see cref="NeedsLengthFreeze"/>) — because GR1 routes the item INTO GR2/GR3 and the set is theirs. The
    /// item arms used to carry their own numeric-only copy (kb/Work PB943).</para></summary>
    private static bool SubstitutesForZeroLength(Place target) =>
        !(target.DenotedItem is not null && target.Item.IsDynamicLength);

    /// <summary>The dispatch kind of storing <paramref name="source"/> into <paramref name="target"/> —
    /// EXACTLY the pre-P7.7 <c>EmitMove</c> dispatch order: ref-mod receiver → group receiver → group sender →
    /// figurative-into-numeric → elementary conversion. <paramref name="source"/> is the SUBSTITUTED sender
    /// (<see cref="Sender"/>) wherever the caller goes through <see cref="Classify"/>, so a zero-length literal
    /// into a numeric receiver classifies as the figurative fill §14.9.25.4 GR2 makes it.</summary>
    public static MoveKind Kind(BoundOperand source, Place target)
    {
        if (target is RefModPlace) return MoveKind.RefModSlice;
        // ⛔ THE RECEIVING HALF OF GR4'S ONE TEST, through THE ONE PREDICATE (kb/Work PB430). A bit / national
        // group RECEIVER is an elementary boolean / national receiver (§13.18.29.4 GR1b/GR2b — D20/PB79): the
        // Convert path over its as-if picture, stored through PlaceRenderer.Write's as-if arm. A level-66
        // THROUGH alias RECEIVER is a group item (§13.18.45.4 GR2) exactly as it is a group SENDER — this line
        // used to re-spell the structural test and therefore answered the two sides differently.
        if (IsGroupPlace(target)) return MoveKind.Group;
        if (IsGroupSender(source)) return MoveKind.GroupToElementary;
        if (target.Item.Pic is { Category: PicCategory.Numeric }
            && source is BoundFigurative { Kind: 'S' or 'Q' or 'H' or 'L' } or BoundAllLiteral { IsDigitOnly: false })
            return MoveKind.FigurativeToNumericImage;
        return MoveKind.Convert;
    }

    /// <summary>Classify every target of one MOVE (the <see cref="BoundMove.Stores"/> parallel list): each
    /// receiver's §14.9.25.4 GR2/GR3 sender AND the kind that sender dispatches to, computed together so the
    /// two can never disagree about which operand is being stored.</summary>
    public static IReadOnlyList<MoveStore> Classify(BoundOperand source, IReadOnlyList<Place> targets)
    {
        var stores = new MoveStore[targets.Count];
        for (int i = 0; i < targets.Count; i++)
        {
            var sender = Sender(source, targets[i]);
            stores[i] = new MoveStore(sender, Kind(sender, targets[i]),
                ReferenceEquals(sender, source) ? MoveSenderOrigin.Written : MoveSenderOrigin.ZeroLengthLiteral);
        }
        return stores;
    }

    /// <summary>True when a MOVE source operand is a GROUP data item (ISO §14.9.25.4 GR4 sender-side test). A
    /// reference-modified sender is excluded — its unique result is an elementary alphanumeric item whatever the
    /// underlying item (§8.4.3.3.4 GR6) — while a level-66 RENAMES … THROUGH alias IS one: §13.18.45.4 GR2,
    /// "data-name-1 defines an alphanumeric group item". A Tier-B REDEFINES group VIEW counts: GR4 classifies by the data item,
    /// not its storage shape. ⛔ The test is NOT purely structural: §14.9.30.4 GR4 b) / §14.9.34.4 GR5 b)
    /// DESIGNATE the READ/RETURN INTO implicit move an alphanumeric group move whenever the file description
    /// entry carries a RECORD IS VARYING clause, so <see cref="BoundCurrentRecord"/> can answer true over an
    /// ELEMENTARY record area (kb/Work PB339).</summary>
    public static bool IsGroupSender(BoundOperand source) => source switch
    {
        // THE CURRENT RECORD of a RECORD IS VARYING file is a group sender BY DESIGNATION, not by structure:
        // §14.9.30.4 GR4 b) / §14.9.34.4 GR5 b) — "If the file description entry contains a RECORD IS VARYING
        // clause, the implied move is an alphanumeric group move" — so `READ F INTO an-edited-item` deposits
        // characters and does NOT edit, even when the FD's 01 is an elementary PIC X(n) (kb/Work PB339). Without
        // that designation (a FORMAT 3 `RECORD CONTAINS m TO n` file) the ordinary structural test decides, so
        // the arm falls through to it rather than answering false.
        BoundCurrentRecord cr => cr.AlphanumericGroupMove || IsGroupPlace(cr.Area),
        BoundFieldOperand f => IsGroupPlace(f.Place),
        _ => false,
    };

    /// <summary>⛔ <b>IS THIS PLACE A GROUP ITEM for §14.9.25.4 GR4's elementary-vs-group test?</b> Written once
    /// and asked of BOTH operands — the sending one through <see cref="IsGroupSender"/> and the receiving one
    /// through <see cref="Kind"/> — because GR4 states ONE rule over both: "Any move in which the sending operand
    /// is either a literal or an elementary item AND the receiving item is an elementary item is an elementary
    /// move." Asking it twice, in two spellings, is how the RENAMES answer below came to be right on one side and
    /// wrong on the other (kb/Work PB430; feedback_two_arm_dispatch).
    ///
    /// <para><b>The test is not purely structural</b>, and two rules say so in opposite directions.
    /// §13.18.45.4 GR2 — "When the THROUGH phrase is specified, data-name-1 defines an alphanumeric group item
    /// that includes all elementary items starting with data-name-2 …" — makes a level-66 THROUGH alias a GROUP
    /// item although this compiler models it as ONE composed elementary alphanumeric view
    /// (<see cref="RenamesPlace"/>); a <see cref="RenamesPlace"/> exists ONLY for the THROUGH form, because
    /// §13.18.45.4 GR1 ("When the THROUGH phrase is not specified, all of the data attributes of data-name-2
    /// become the data attributes of data-name-1") makes the no-THROUGH alias forward to the renamed item's own
    /// place in <c>ReferenceResolver</c> — so the designation needs no further qualification here.
    /// §8.4.3.3.4 GR6 goes the other way for a reference-modified operand, whose unique result is an elementary
    /// alphanumeric item whatever the underlying item is.</para></summary>
    private static bool IsGroupPlace(Place p) => p switch
    {
        RefModPlace => false,                            // §8.4.3.3.4 GR6 — an elementary alphanumeric result
        RenamesPlace => true,                            // §13.18.45.4 GR2 — an alphanumeric GROUP item
        _ => p.Item.IsGroup && !p.Item.IsAsIfElementary, // a bit / national group acts as elementary (D20/PB79)
    };
}
