// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Common;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The EVALUATE verb binder (P7 Step 10d — a real collaborator over <see cref="BinderContext"/>,
/// extracted from the <c>StatementBinder.Evaluate</c> partial; the condition/relation/expression spine is
/// reached through transitional host edges until batches 10o/10q). The <c>BoundEvaluate</c>/
/// <c>BoundEvaluateWhen</c> records stayed in <c>Binding/Bound/BoundEvaluate.cs</c> — the generated visitor
/// and <c>StatementChildren</c> key on them.</summary>
internal sealed class EvaluateBinder(BinderContext ctx, StatementBinder host)
{
    /// <summary>Bind EVALUATE (ISO §14.9.13). Subjects: TRUE/FALSE, an identifier/literal, an arithmetic
    /// expression, or a condition (condition-1); objects per WHEN: ANY, [NOT] operand [THRU operand], or a
    /// condition (against a TRUE/FALSE subject). The subject↔object pairing is positional across ALSO (SR
    /// — the object count must equal the subject count); each pair lowers to an equality / range / condition
    /// term and the WHEN's terms AND together.</summary>
    public BoundStatement Bind(Core.EvaluateStatementContext ev)
    {
        var subjects = ev.evaluateSubject();
        // ⛔ ONE SLOT PER SELECTION SUBJECT, FOR THE WHOLE STATEMENT (ISO §14.9.13.4 GR3, kb/Work PB394): "At the
        // beginning of the execution of the EVALUATE statement, each selection subject is evaluated and assigned
        // a value, a range of values, or a truth value". The subject used to be re-BOUND inside BindWhenItem —
        // per selection PAIR, and therefore per WHEN — so the subject EXPRESSION was emitted once per relational
        // use, twice for a THRU arm. A slot binds it once and every pair reads the same node.
        var slots = new SubjectSlot[subjects.Length];
        for (int i = 0; i < subjects.Length; i++) slots[i] = new SubjectSlot(this, subjects[i], SubjectUsageOf(ev, i));
        // ⛔ GR3 IS AN OBLIGATION OF THE STATEMENT, NOT OF WHICHEVER ARM HAPPENS TO READ THE SUBJECT (kb/Work
        // PB396). "At the beginning of the execution of the EVALUATE statement, each selection subject is
        // evaluated and assigned a value, a range of values, or a truth value" (ISO §14.9.13.4 GR3). The slots
        // were bound LAZILY, from inside the pair binder, so a statement no arm read the subject in never
        // evaluated it: `WHEN ANY` against every subject — and, before the §14.9.13.2 grammar repair, an
        // EVALUATE whose only clause was WHEN OTHER — compiled the subject away entirely. Measured: an
        // undefined selection subject lost its COBOLNET1639 (§8.4.2.1), `EVALUATE R (9)` over `OCCURS 3` lost
        // EC-BOUND-SUBSCRIPT, and `EVALUATE A / B` with B = 0 lost EC-SIZE-ZERO-DIVIDE — all three fatal on the
        // identical program with one dead WHEN arm added. Touching every slot HERE, before any arm binds, both
        // resolves the operand and — through SubjectUsage.NeedsIntermediate, which is true for every shape but
        // "one use, in the first arm" — registers the evaluation as the statement's PRE-op, which is the
        // generated-code position of "the beginning of the execution".
        // ⛔ AND THE SAME FOR A TRUTH VALUE (kb/Work PB842 / PB912): a subject that is condition-1 by its FORM —
        // a written condition, or a bare level-88 / switch-status condition-name — is assigned its truth value here,
        // once, before any arm binds (GR3 e). SR6 b)'s one-boolean-character boolean subject is condition-1 only
        // for a TRUE/FALSE pairing (a per-WHEN reclassification), so its truth value binds on first demand.
        foreach (var slot in slots) { _ = slot.Value; if (slot.IsCondition1ByForm) _ = slot.Truth; }
        var whens = new List<BoundEvaluateWhen>();

        foreach (var clause in ev.evaluateWhenClause())
        {
            var body = host.BindBlocks([clause.statementBlock()]);
            // Consecutive WHEN phrases share the body: OR their per-phrase matches (§14.9.13 — the 1985 form).
            var phraseMatches = new List<BoundCondition>();
            foreach (var phrase in clause.evaluateWhenPhrase())
            {
                var groups = phrase.evaluateWhenGroup();
                ScreenObjectCount(phrase, groups.Length, subjects.Length);
                var terms = new List<BoundCondition>();
                // ⛔ THE COUNT IS A SYNTAX RULE, SCREENED ABOVE — this clamp is pure RECOVERY (kb/Work PB399).
                // It used to be `if (i >= subjects.Length) return new BoundUnsupported(…)`: a defensive index
                // guard wearing a rule's clothes, which delivered §14.9.13.3 SR2 as a run-time abort in the
                // MORE-objects direction and not at all in the FEWER-objects direction, where the loop simply
                // ran out of written objects and the surplus SUBJECTS were never paired with anything — illegal
                // source silently branching on a strict subset of its own selection subjects. With the screen
                // above, an over- or under-long phrase has already failed the compile; binding the pairs the
                // programmer DID write keeps the rest of the statement's diagnostics flowing instead of
                // truncating the bind at the first bad phrase.
                int paired = System.Math.Min(groups.Length, subjects.Length);
                for (int i = 0; i < paired; i++)
                    terms.Add(BindWhenGroup(slots[i], groups[i]));
                phraseMatches.Add(terms.Count == 1 ? terms[0] : new BoundLogical("&&", terms));
            }
            BoundCondition match = phraseMatches.Count == 1 ? phraseMatches[0] : new BoundLogical("||", phraseMatches);
            whens.Add(new BoundEvaluateWhen(match, body));
        }
        // `[ WHEN OTHER imperative-statement-2 ]` — ONE optional phrase, AFTER the `{ … } …` repetition
        // (§14.9.13.2 + §5.2.7), so the grammar now admits it once and only last. The rule used to live in a
        // COMMENT on an `other = body` assignment inside this loop, which meant a second WHEN OTHER overwrote
        // the first and the first's imperative-statement-2 became dead source, never emitted and never
        // reported; §14.9.13.4 GR5 b) is written for exactly one such phrase and says nothing about two.
        var other = ev.evaluateWhenOther() is { } o ? host.BindBlocks([o.statementBlock()]) : null;
        return new BoundEvaluate(whens, other);
    }

    /// <summary>One subject↔object pair → a boolean term (§14.9.13 GR4–7):
    /// ANY → always true; a TRUE/FALSE subject pairs with a CONDITION object (TRUE → the condition, FALSE → its
    /// negation); a value subject pairs with an operand (equality), a THRU range (inclusive bounds), or — when
    /// the object IS a condition over the same item (the grammar's escape) — the condition itself. A leading NOT
    /// on the group negates the whole term.</summary>
    private BoundCondition BindWhenGroup(SubjectSlot subject, Core.EvaluateWhenGroupContext group)
    {
        // ONE selection-object per position (§14.9.13.2 general format — objects repeat only through ALSO, which is
        // this method's CALLER; §14.9.13.3 SR2 fixes the count against the subjects). The grammar enforces the arity,
        // so there is no list to fold here.
        // ⚠ This used to iterate an `evaluateWhenItem+` list and AND the terms together — a semantics with no clause
        // behind it, invented to give the unlicensed repetition a meaning. Legal source can never produce a second
        // item, so the only thing that rule ever bound was PB45's peeled misparse of
        // `WHEN FUNCTION SQRT(X) > 1` (item 1 = `FUNCTION SQRT`, item 2 = `(X) > 1`), which is exactly how a
        // function-identifier object reached the "value WHEN object" error below instead of binding as a condition.
        BoundCondition cond = BindWhenItem(subject, group.evaluateWhenItem());
        return group.NOT() is not null ? new BoundNot(cond) : cond;
    }

    /// <summary>The Table-15 coordinates of ONE selection pair, after §14.9.13.3 SR6's per-WHEN
    /// reclassification, together with each side's already-resolved bare operand — so the bind path below reads
    /// the SAME resolution the screen classified rather than resolving the symbol a second time.</summary>
    private readonly record struct EvaluatePairing(
        EvaluateSubjectOperand? Subject,
        EvaluateObjectOperand? Object,
        BareOperandAnalysis SubjectBare,
        BareOperandAnalysis ObjectBare);

    /// <summary>⛔ THE ONE CLASSIFIER for a selection pair (ISO §14.9.13.3 SR6 + SR10). Each side is named on
    /// Table 15's own axes through the SAME bare-operand classifier, and SR6 then decides a boolean operand's
    /// kind from the OTHER side of the pair, because that is what SR6 says: "The classification of some
    /// selection subjects or selection objects is changed for a particular WHEN phrase".
    /// <para>⛔ ONE FUNCTION, NOT TWO (kb/Work PB400). The subject and the object used to be classified by two
    /// independently written methods, and every difference between them was a Table-15 asymmetry: the object
    /// method recognised a switch-status condition-name and the subject method did not, so
    /// <c>EVALUATE W-ON WHEN TRUE</c> was refused as identifier × TRUE-or-FALSE while the level-88 spelling in
    /// the identical position compiled and ran. <see cref="BareOperandKind"/> is now the shared body and
    /// <c>EvaluateOperandCombinations.AsSubjectOperand</c> the row→column mapping, so the next condition form
    /// added is recognised on both sides by construction.</para>
    /// <para>GR3 a) — "If the selection subject is a numeric data item or a boolean data item whose length is
    /// one boolean position, the selection subject for this evaluation is treated as identifier-1 and not an
    /// arithmetic or boolean expression" — is a GENERAL rule scoped to that evaluation: it fixes how the
    /// subject's VALUE is obtained (the item's value, not the result of running an expression evaluator), which
    /// this compiler realizes by binding a bare boolean reference to a <c>BoundBoolRef</c> read. It does not
    /// move the operand out of Table 15's boolean-expression column, because §8.8.2 makes "an identifier
    /// referencing a boolean data item" a boolean expression and SR6 — a SYNTAX rule, explicitly "for a
    /// particular WHEN phrase" — is what classifies it. Reading it the other way would leave SR6 b) with no
    /// effect on the shape it plainly mirrors (SR6 a), which the object side has always honoured).</para>
    /// </summary>
    private EvaluatePairing ClassifyPair(SubjectSlot slot, Core.EvaluateWhenItemContext item)
    {
        // The subject's bare-operand analysis is the SLOT's — resolved once for the statement, not once per pair
        // (kb/Work PB400's "one resolution, consulted twice", now consulted N times from one resolution).
        var subject = slot.Node;
        var subjectBare = slot.Bare;
        var objectBare = item.valueOperand() is { } ovo ? host.Cond.AnalyzeBareOperand(ovo) : default;

        var s = SubjectKind(subject, subjectBare);
        var o = ObjectKind(item, objectBare);

        // §14.9.13.3 SR6 a) — a TRUE/FALSE subject makes a one-boolean-character boolean object condition-2.
        if (s is EvaluateSubjectOperand.TrueOrFalse && o is EvaluateObjectOperand.BooleanExpression
            && IsOneBooleanCharacter(objectBare.BooleanLength))
            o = EvaluateObjectOperand.Condition;
        // SR6 b) — its MIRROR, and the arm that did not exist: a TRUE/FALSE object makes a one-boolean-character
        // boolean SUBJECT condition-1. Without it `EVALUATE BW WHEN TRUE` bound TRUE as a standalone condition
        // and the emitted program died at run time on conforming source (kb/Work PB400).
        else if (o is EvaluateObjectOperand.TrueOrFalse && s is EvaluateSubjectOperand.BooleanExpression
            && IsOneBooleanCharacter(subjectBare.BooleanLength))
            s = EvaluateSubjectOperand.Condition;
        // SR6 c) and d) are the IDENTITY here: an operand facing a counterpart that is not TRUE or FALSE stays
        // boolean-expression-2 / boolean-expression-1, which is the kind BareOperandKind already gave it.
        // SR6 e) — "If the selection object is a partial expression and the selection subject is a data item of
        // the class boolean or numeric, the selection subject is treated as an identifier." It runs AFTER a)–d)
        // because it is the one arm keyed on the OBJECT being a partial expression, and it OVERRIDES d): a
        // one-boolean-character boolean data item facing a non-TRUE/FALSE object is boolean-expression-1 by d),
        // and e) names the narrower case (that object being a partial expression) and makes it identifier-1. The
        // rule is a no-op for a numeric data item, which BareOperandKind already calls an identifier — stated,
        // not assumed, because "the class boolean or numeric" is one sentence and implementing half of it would
        // be the two-arm shape again. A LITERAL or an arithmetic-expression subject is untouched: e) says "a
        // data item". (kb/Work PB398; unexercisable until this note landed a partial-expression object.)
        if (o is EvaluateObjectOperand.PartialExpression && IsDataItemOfClassBooleanOrNumeric(subject, subjectBare))
            s = EvaluateSubjectOperand.Identifier;
        return new EvaluatePairing(s, o, subjectBare, objectBare);
    }

    /// <summary>§14.9.13.3 SR6 e)'s antecedent — "the selection subject is a data item of the class boolean or
    /// numeric". A DATA ITEM, so a literal, an arithmetic expression, a function result and a condition-name are
    /// all excluded by construction (the reference must reduce to one data reference); the CLASS is read from the
    /// ONE category reader, <c>DataItem.OperandPic</c>, so a bit GROUP or a national group's §13.18.29.4 GR1b/GR2b
    /// as-if PICTURE answers exactly as its elementary twin does — reading raw <c>Pic</c> would return null for
    /// every group and silently drop them (the kb/Work PB728/PB741 shape).</summary>
    private bool IsDataItemOfClassBooleanOrNumeric(Core.EvaluateSubjectContext subject, in BareOperandAnalysis bare)
    {
        if (bare.IsConditionName) return false;   // a condition-name is not a data item in this position
        if (subject.condition() is not null || subject.booleanLiteral() is not null) return false;
        if (subject.valueOperand()?.arithmeticExpression() is not { } expr) return false;
        if (ConditionBinder.SoleDataRef(expr) is not { } dref) return false;
        // Probe — a routing predicate is diagnostic-free (R30); an unresolvable subject reports through the bind.
        return ctx.Refs.Probe(dref) is { Item.OperandPic.Category: PicCategory.Boolean or PicCategory.Numeric };
    }

    /// <summary>SR6's "results in one boolean character", over the §8.8.2 rules 9/10 result length. A
    /// POSITIONLESS operand (figurative ZERO / <c>ALL B"…"</c>, whose length is its sibling's — §8.3.3.6.4 GR4)
    /// and a boolean function whose length argument is a run-time value both report null, and both answer YES:
    /// against a TRUE-or-FALSE counterpart one boolean position is the only length that gives the pair a
    /// meaning, and the §8.8.4.3 SR1 screen on the resulting condition still refuses the rest. Failing OPEN is
    /// the direction that cannot reject legal source.</summary>
    private static bool IsOneBooleanCharacter(int? length) => length is null or 1;

    /// <summary>ISO §14.9.13.3 SR10 — screen the pairing against Table 15 BEFORE binding it, so an invalid
    /// combination is a compile-time diagnostic rather than the run-time fault it used to be (kb/Work PB47).
    /// <para>⚠ The classifier still abstains — a null on either side means NO diagnostic — but only where it
    /// genuinely cannot name the shape. That is now the PARTIAL-EXPRESSION row alone: the grammar stages the
    /// partial forms (see <c>comparisonExpression</c>'s DEVLOG-621 note), so no operand classifies into that row
    /// today and the table carries it for when one does. The BOOLEAN rows are no longer abstentions — SR6 names
    /// a boolean operand's kind from the other side of the pair, and <see cref="ClassifyPair"/> implements that
    /// rule instead of declining to answer it (kb/Work PB400).</para></summary>
    private void ScreenPairing(in EvaluatePairing pair, Core.EvaluateWhenItemContext item)
    {
        if (pair.Subject is not { } s || pair.Object is not { } o) return;
        if (EvaluateOperandCombinations.IsPermitted(s, o)) return;
        ctx.Edition.Error(DiagnosticCatalog.EvaluateOperandCombinationInvalid,
            $"the selection subject is {EvaluateOperandCombinations.Label(s)} and the selection object "
            + $"'{item.GetText()}' is {EvaluateOperandCombinations.Label(o)}; ISO §14.9.13.3 SR10 Table 15 marks "
            + "that combination invalid.");
    }

    /// <summary>ISO §14.9.13.3 SR2 — "The number of selection objects within each set of selection objects shall
    /// be equal to the number of selection subjects." A SYNTAX rule, so a compile-time diagnostic (§4.2.2), and
    /// an EQUALITY, so BOTH directions (kb/Work PB399).
    /// <para>⛔ IT LIVES BESIDE <see cref="ScreenPairing"/> AND NOT INSIDE THE PAIRING LOOP, because that is the
    /// difference between a rule and an index guard. The loop can only notice the direction in which it runs out
    /// of SUBJECTS; the other direction — fewer objects than subjects — is invisible to it by construction, and
    /// that was the half with no diagnostic at any stage. Counting the two sets against each other sees both.</para>
    /// <para>The count is per WHEN PHRASE, which is what "each set of selection objects" names: consecutive WHEN
    /// phrases sharing one imperative-statement are separate sets and are screened separately.</para></summary>
    private void ScreenObjectCount(Core.EvaluateWhenPhraseContext phrase, int objects, int subjects)
    {
        if (objects == subjects) return;
        ctx.Edition.Error(DiagnosticCatalog.EvaluateSelectionObjectCount,
            $"the WHEN phrase '{DataBinder.WrittenText(phrase)}' writes {objects} selection "
            + (objects == 1 ? "object" : "objects") + $" but the EVALUATE statement has {subjects} selection "
            + (subjects == 1 ? "subject" : "subjects")
            + "; ISO §14.9.13.3 SR2 requires the two counts to be equal (write ANY for a position the phrase "
            + "does not test — SR7 c)");
    }

    /// <summary>⛔ THE ONE ADMISSIBILITY SCREEN OVER A RANGE-EXPRESSION'S PAIR OF OPERANDS (kb/Work PB399) —
    /// ISO §14.9.13.3 SR4, "The two operands in a range-expression shall be of the same class and shall not be
    /// of class boolean, message-tag, object, or pointer", and SR9, "Neither identifier-3 nor identifier-4 shall
    /// reference a variable-length group". Both are SYNTAX rules, so both are compile-time (§4.2.2).
    /// <para>ONE screen and not three `if`s, because the two rules ask ONE question — "may these two operands be
    /// the ends of a range?" — of the SAME pair, at the SAME moment, and <see cref="ScreenPairing"/> structurally
    /// cannot ask it: <see cref="ObjectKind"/> names a <c>valueRange</c> as a single
    /// <see cref="EvaluateObjectOperand.RangeExpression"/> row and never looks inside it, which is correct,
    /// because Table 15 is about the subject↔object PAIRING and not about a range's internals.</para>
    /// <para>CLASS here is §8.5.2.1 Table 2's, read through the ONE Table-2 lattice
    /// (<see cref="IntrinsicArgumentRules.ClassOf"/>, projected onto the class column by
    /// <see cref="IntrinsicArgumentRules.TableTwoClass"/>) — never <see cref="CollatingSelection.ForComparison"/>,
    /// which answers the different question of which §8.8.4.2 comparison rule a RELATION selects and deliberately
    /// collapses alphabetic onto alphanumeric and numeric-against-alphanumeric onto alphanumeric. A null class is
    /// a figurative constant (§8.3.3.6.4 GR1 gives it its context's category, so the lattice answers a SET and
    /// not a class) or an already-reported error node, and abstains — the direction that cannot reject legal
    /// source. ⚠ Class MESSAGE-TAG, SR4's fourth exclusion, has no lattice member: USAGE MESSAGE-TAG is declined
    /// non-support (COBOLNET1943) and refused by name at every edition, so no such operand reaches here; the day
    /// it lands, the lattice gains the member and this screen sees it with no change.</para>
    /// <para>The pointer case is the sharpest, and it is why SR4 cannot be left to the relation checkpoint: a
    /// range lowers to `subject &gt;= low AND subject &lt;= high` (§14.9.13.4 GR4 a) 5.), and where that pair is
    /// NOT what gets emitted — an inverted or IN-alphabet range builds a <c>BoundRangeMembership</c> instead —
    /// the checkpoint is never reached at all. SR4 forbids the range outright, one phase earlier than any
    /// question about the comparison.</para></summary>
    private bool ScreenRangeOperands(Core.ValueRangeContext range, BoundOperand lo, BoundOperand hi)
    {
        bool ok = true;
        var loClass = TableTwoClassOf(lo);
        var hiClass = TableTwoClassOf(hi);
        // SR4, second half — the classes a range-expression may not be of, asked of EACH end so the diagnostic
        // can name the offender even when the other end abstains.
        bool excluded = false;
        for (int end = 0; end < 2; end++)
        {
            if ((end == 0 ? loClass : hiClass) is not { } c) continue;
            if (c is not (CobolClass.Boolean or CobolClass.Object or CobolClass.Pointer)) continue;
            excluded = true;
            ok = false;
            ctx.Edition.Error(DiagnosticCatalog.EvaluateRangeOperandInvalid,
                $"the {(end == 0 ? "first" : "second")} operand of the range-expression "
                + $"'{DataBinder.WrittenText(range)}' is of class {c.ToString().ToLowerInvariant()}; ISO §14.9.13.3 SR4 "
                + "excludes class boolean, message-tag, object and pointer from a range-expression");
        }
        // SR4, first half — the same class. Reported only when neither end is already excluded, so one written
        // range cannot draw two diagnostics about the same operand.
        if (!excluded && loClass is { } lc && hiClass is { } hc && lc != hc)
        {
            ok = false;
            ctx.Edition.Error(DiagnosticCatalog.EvaluateRangeOperandInvalid,
                $"the two operands of the range-expression '{DataBinder.WrittenText(range)}' are of class "
                + $"{lc.ToString().ToLowerInvariant()} and class {hc.ToString().ToLowerInvariant()}; ISO "
                + "§14.9.13.3 SR4 requires them to be of the same class (§8.5.2.1 Table 2 — CLASS, not "
                + "category, so alphabetic and alphanumeric are two classes)");
        }
        // SR9 — a variable-length group is §8.5.1.12.1's: "a group item whose data description has at least one
        // dynamic-length elementary item or dynamic-capacity table as a subordinate item". ⛔ NOT an
        // occurs-depending group, whose size is its maximum — through the ONE definition
        // (ReferenceResolver.HasVariableLengthSubordinate) so this screen cannot drift from the REDEFINES, VALUE
        // and report screens that ask the same question.
        for (int end = 0; end < 2; end++)
        {
            if ((end == 0 ? lo : hi) is not BoundFieldOperand { Place.Item: { IsGroup: true } item }) continue;
            if (!ReferenceResolver.HasVariableLengthSubordinate(item)) continue;
            ok = false;
            ctx.Edition.Error(DiagnosticCatalog.EvaluateRangeOperandInvalid,
                $"the {(end == 0 ? "first" : "second")} operand of the range-expression '{DataBinder.WrittenText(range)}' "
                + $"references '{item.CobolName ?? item.CsName}', a variable-length group (ISO §8.5.1.12.1 — a "
                + "dynamic-length elementary item or a dynamic-capacity table is subordinate to it); ISO "
                + "§14.9.13.3 SR9 forbids either end of a range-expression to reference one");
        }
        return ok;
    }

    /// <summary>The operand's ISO §8.5.2.1 Table 2 CLASS COLUMN — the lattice's answer projected through
    /// <see cref="IntrinsicArgumentRules.TableTwoClass"/>, so category numeric-edited answers with the class
    /// Table 2 gives it (alphanumeric) rather than with the refined member the lattice keeps for the
    /// category-worded rules. §14.9.13.3 SR4 says CLASS, and §8.5.2.1's closing sentence is why the projection
    /// is not optional: "Use of the name of a data class or data category in the rules of COBOL refers to the
    /// category unless class is specifically indicated."</summary>
    private static CobolClass? TableTwoClassOf(BoundOperand o) =>
        IntrinsicArgumentRules.ClassOf(o) is { } c ? IntrinsicArgumentRules.TableTwoClass(c) : null;

    /// <summary>The subject's Table-15 COLUMN before SR6, or null when the shape cannot be named with
    /// certainty. The two subject-only forms are grammatical (TRUE/FALSE, and a written condition — condition-1
    /// per §14.9.13.4 GR3 e)); everything else is the SHARED bare-operand classification.</summary>
    private static EvaluateSubjectOperand? SubjectKind(Core.EvaluateSubjectContext subject, in BareOperandAnalysis bare)
    {
        if (subject.booleanLiteral() is not null) return EvaluateSubjectOperand.TrueOrFalse;
        if (subject.condition() is not null) return EvaluateSubjectOperand.Condition;  // EVALUATE X > 1, NOT BW, X NUMERIC
        if (subject.valueOperand() is not { } vo) return null;
        return BareOperandKind(vo, bare) is { } row ? EvaluateOperandCombinations.AsSubjectOperand(row) : null;
    }

    /// <summary>The object's Table-15 ROW before SR6, or null when the shape cannot be named with certainty.
    /// The object-only forms are grammatical (ANY, a THRU range, an explicit condition — including the
    /// TRUE/FALSE spelling of one); everything else is the SHARED bare-operand classification.</summary>
    private EvaluateObjectOperand? ObjectKind(Core.EvaluateWhenItemContext item, in BareOperandAnalysis bare)
    {
        if (item.ANY() is not null) return EvaluateObjectOperand.Any;
        if (item.valueRange() is not null) return EvaluateObjectOperand.RangeExpression;
        // §14.9.13.3 SR5 — the object's leftmost portion is a relational operator, a class condition without the
        // identifier, or a sign condition without its operand. The GRAMMAR decides it (that is what SR5 is: a
        // statement about the written form's left edge), so this row needs no symbol resolution — and with it
        // Table 15's Partial-expression row stops being a lookup no operand can reach (kb/Work PB398).
        if (item.partialExpression() is not null) return EvaluateObjectOperand.PartialExpression;
        if (item.condition() is { } c)
            return SoleBooleanLiteral(c) is not null ? EvaluateObjectOperand.TrueOrFalse
                // SR5 again, where the grammar's `condition` claimed the object: its leftmost simple condition is a
                // BARE class-name / alphabet-name, i.e. "a class condition without the identifier" (kb/Work PB843).
                : host.Cond.LeadingBareClassWord(c) is not null ? EvaluateObjectOperand.PartialExpression
                : EvaluateObjectOperand.Condition;
        return item.valueOperand() is { } vo ? BareOperandKind(vo, bare) : null;
    }

    /// <summary>⛔ THE ONE bare-operand classification, named on Table 15's ROW axis and mapped to the COLUMN
    /// axis for a selection subject. A bare word that RESOLVES to a level-88 (§8.8.4.2.7 r2) or a switch-status
    /// (§8.8.4.6) condition-name IS a condition, not an identifier; a boolean operand (§8.8.2) is a boolean
    /// expression until SR6 says otherwise; everything else is <see cref="OperandKindOf"/>'s syntactic
    /// question.</summary>
    private static EvaluateObjectOperand? BareOperandKind(Core.ValueOperandContext vo, in BareOperandAnalysis bare) =>
        bare.Form switch
        {
            BareOperandForm.ConditionName or BareOperandForm.SwitchStatus => EvaluateObjectOperand.Condition,
            BareOperandForm.Boolean => EvaluateObjectOperand.BooleanExpression,
            // §14.9.13.3 SR5 — "a class condition without the identifier": a bare class-name / alphabet-name is a
            // partial-expression by its resolved SYMBOL, the spelling the grammar hands to valueOperand (PB843).
            BareOperandForm.ClassName => EvaluateObjectOperand.PartialExpression,
            _ => OperandKindOf(vo),
        };

    /// <summary>Classify a bare value operand as identifier / literal / arithmetic-expression, or null when the
    /// shape is not one of those with certainty — §14.9.13.4 GR1's question: "If an operand of the EVALUATE
    /// statement consists of a single literal, that operand is treated as a literal, not as an expression."
    /// <c>ConditionBinder.SoleNumLiteral</c> answers the "consists of a single literal" half, sign included
    /// (§8.3.3.3.2 rule 2), which is what makes <c>-5</c> a literal and the SEPARATED <c>- 5</c> an arithmetic
    /// expression.</summary>
    private static EvaluateObjectOperand? OperandKindOf(Core.ValueOperandContext vo)
    {
        if (vo.nonNumericLiteral() is not null) return EvaluateObjectOperand.Literal;     // GR1
        if (vo.arithmeticExpression() is not { } expr) return null;
        if (ConditionBinder.SoleDataRef(expr) is not null) return EvaluateObjectOperand.Identifier;
        if (ConditionBinder.SoleNumLiteral(expr) is not null) return EvaluateObjectOperand.Literal;   // GR1
        return EvaluateObjectOperand.ArithmeticExpression;
    }

    private BoundCondition BindWhenItem(SubjectSlot slot, Core.EvaluateWhenItemContext item)
    {
        var subject = slot.Node;
        // ⛔ CLASSIFY ONCE, THEN BIND FROM THAT CLASSIFICATION (kb/Work PB400). The screen used to run its own
        // resolution and the bind path a second, identical one — so a level-88 whose reference is ambiguous
        // reported §8.4.2.2 twice, and the two could disagree about what the operand IS with nothing to catch it.
        var pair = ClassifyPair(slot, item);
        ScreenPairing(pair, item);
        if (item.ANY() is not null) return Constant(true);   // GR4 a) 1. — the word ANY is true

        bool subjTrue = subject.booleanLiteral()?.TRUE_() is not null;
        bool subjFalse = subject.booleanLiteral()?.FALSE_() is not null;

        // User-function evaluation cardinality (§8.4.3.2.4 GR1/GR6a) over the EVALUATE windows:
        // — a VALUE SUBJECT is evaluated ONCE "at the beginning of the execution of the EVALUATE statement"
        //   (§14.9.13.4 GR3) and now IS: the slot binds it once and materializes its value into the
        //   implementor's intermediate result item, so a subject activation hoists to statement scope exactly
        //   as the rule describes (kb/Work PB394);
        // — a CONDITION SUBJECT (§14.9.13.4 GR3 e, "assigned a truth value") is the same: the slot binds the
        //   condition once and, where more than one arm reads it, materializes its TRUTH value into a
        //   one-position boolean intermediate (SendingValueTemp.MaterializeTruth — the condition→boolean bridge,
        //   kb/Work PB842 / PB912), so its activations too hoist to statement scope exactly once;
        // — an OBJECT is evaluated only when its WHEN phrase is considered, pairs left-to-right with a
        //   false pair stopping the phrase (GR4a–d) — its activations attach per-evaluation to the object
        //   term, and the composed &&/|| chain's C# short-circuit realizes GR4c exactly. A partial-expression
        //   object splices the subject's one VALUE in, so SR8's rewrite activates nothing of the subject's.
        // No subject window stages COBOLNET1509 any more: the narrowed residue stage had exactly these two
        // callers, and it is deleted with them.

        // A CONDITION subject (§14.9.13.4 GR3 e), Table 15's Condition column) pairs with TRUE/FALSE objects:
        // the term is the subject's ONE truth value (or its negation). Four spellings reach it — a written
        // condition (`EVALUATE X NUMERIC`, `EVALUATE WS-N > 1`, `EVALUATE NOT BW`, any combined condition), a
        // level-88, a switch-status condition-name, and SR6 b)'s one-boolean-character boolean subject — and the
        // CLASSIFIER decided which, so no shape can be recognised here and missed by the screen (or the reverse,
        // which is what refused `EVALUATE W-ON WHEN TRUE`).
        if (pair.Subject is EvaluateSubjectOperand.Condition && slot.Truth is { } subjCond)
        {
            // §14.9.13.4 GR4 a) 4. — a TRUE/FALSE object: "the selection subject is condition-1 … if the truth
            // value of the selection subject and selection object match, the result of the analysis is true",
            // which for a constant object IS the subject condition or its negation.
            if (item.condition() is { } c && SoleBooleanLiteral(c) is { } objBool)
                return objBool ? subjCond : new BoundNot(subjCond);
            // ⛔ TABLE 15 ALSO ADMITS Condition × Condition, AND IT IS THE SAME ANALYSIS (kb/Work PB400). The
            // Condition ROW is 'Y' under BOTH the Condition and the TRUE-or-FALSE column, and GR4 a) 3. and
            // a) 4. state the analysis in one shared sentence — "If the truth value of the selection subject
            // and selection object match, the result of the analysis is true. If they do not match, the result
            // is false" — which GR3 e) makes meaningful for a condition-1 subject too ("any selection subject
            // specified by condition-1 is assigned a truth value"). Each rule's FIRST sentence names the form
            // the other side takes in the case it was written for; neither retracts the table's cell. Without
            // this arm `EVALUATE W-A-ON WHEN W-B-ON` compiled clean and threw at RUN TIME — the PB45/PB47 shape
            // one cell over, and reachable in three more spellings now that a switch-status and a
            // one-boolean-character boolean subject classify as condition-1.
            // NOT XOR, so both truth values are evaluated exactly once: a pair is analysed as a whole (GR4 c)'s
            // short-circuit is between PAIRS, never inside one).
            if (pair.Object is EvaluateObjectOperand.Condition)
            {
                var objMark2 = host.Udf.Mark;
                if (ObjectAsCondition(item, pair) is { } objCond2)
                    return new BoundNot(new BoundLogical("^",
                        [subjCond, host.Udf.UdfAttachPerEvaluation(objCond2, objMark2)]));
            }
            return host.Cond.Refused($"EVALUATE condition-subject paired with non-boolean WHEN '{item.GetText()}'");
        }

        // ⛔ TRUE-or-FALSE × TRUE-or-FALSE (Table 15's two 'Y' cells for the words) IS A CONSTANT, AND NO CONDITION
        // IS BOUND FOR IT (kb/Work PB1668). §14.9.13.4 GR3 f) gives the subject word its truth value, GR4 a) 4. makes
        // the pair "true" exactly when the two truth values match — so `EVALUATE TRUE WHEN TRUE` is true and
        // `EVALUATE FALSE WHEN TRUE` is false. The object's word used to go through BindCondition, whose bare-word
        // arm is not a condition (§8.8.4.2.1) and refused conforming source through the COBOLNET2319 net.
        if (pair.Subject is EvaluateSubjectOperand.TrueOrFalse && item.condition() is { } wordObject
            && SoleBooleanLiteral(wordObject) is { } objectWordTrue)
            return Constant(subjTrue == objectWordTrue);

        // A `condition` the classifier re-read as SR5's partial-expression (a bare class-name leftmost — PB843) is
        // NOT condition-2: it falls through to the partial arm below, which splices the subject in.
        if (pair.Object is not EvaluateObjectOperand.PartialExpression && item.condition() is { } cond)
        {
            var objMark = host.Udf.Mark;
            var bound = host.Udf.UdfAttachPerEvaluation(host.Cond.BindCondition(cond), objMark);
            return subjFalse ? new BoundNot(bound) : bound;   // EVALUATE TRUE/FALSE WHEN <condition>
        }

        // §14.9.13.4 GR4a3 — "If the selection object is condition-2, the selection subject is either TRUE or
        // FALSE… If the truth value of the selection subject and selection object match, the result of the
        // analysis is true" — and §14.9.13.3 SR10 Table 15 admits ONLY a condition, TRUE/FALSE or ANY against a
        // TRUE/FALSE subject, never identifier-2 or a value.
        // ⛔ A BARE CONDITION-NAME IS condition-2 (§8.8.4.2.7 r2) BUT ARRIVES THROUGH THE valueOperand ARM, because a
        // bare word is equally an arithmeticExpression and the grammar cannot tell them apart — only the resolved
        // SYMBOL can. Without this arm the commonest EVALUATE idiom there is —
        //     EVALUATE TRUE  WHEN VALID-CODE …
        // — compiled clean and threw "value WHEN object" at RUN TIME (fix-queue PB45). Reordering
        // evaluateWhenItem's alternatives is NOT the fix: Table 15 makes the object's legality depend on the
        // SUBJECT, so putting `condition` first would retarget `EVALUATE X WHEN <88>` (a VALUE subject, where the
        // grammar reads the same name as an equality operand) in the opposite direction. The question is asked by
        // the CLASSIFIER above, once, and this arm reads its answer — SR6 a) is what routes a one-boolean-
        // character boolean object here as well.
        if (pair.Object is EvaluateObjectOperand.Condition && ObjectAsCondition(item, pair) is { } objCond)
        {
            var objMark = host.Udf.Mark;
            var bound = host.Udf.UdfAttachPerEvaluation(objCond, objMark);
            return subjFalse ? new BoundNot(bound) : bound;
        }

        // §14.9.13.3 SR8 + §14.9.13.4 GR4 a) 2. — partial-expression-1. The object is "treated as though it were
        // specified as condition-2, where condition-2 is the conditional expression that results from preceding
        // partial-expression-1 by the selection subject", and "the corresponding selection subject is treated as
        // though it were specified by the word TRUE" — so the pair's term IS that condition, with no comparison
        // against the subject wrapped around it.
        // ⛔ THE SUBJECT SPLICED IN IS THE SLOT'S ONE VALUE, NOT A RE-BIND OF ITS NODE (kb/Work PB912's sibling).
        // GR3 assigns the subject its value "at the beginning of the execution of the EVALUATE statement", and SR8
        // then forms a condition FROM that subject — nothing in SR8 re-evaluates it. This arm used to pass the
        // subject's PARSE NODE into the rewrite, which bound it again once per partial-expression object on top of
        // the statement's own evaluation: a user function in the subject was activated 1 + N times and the arms
        // compared N different values (measured: WHEN OTHER selected where WHEN = 1 was the answer), while the
        // residue stage meant to refuse the shape never fired. The class shape reads CONTENT (see
        // PartialSubjectOperand), so an in-place data item is handed over un-materialized for it.
        // ⛔ THREE PARSES, ONE ARM (kb/Work PB843): the grammar's partialExpression; a BARE class-name /
        // alphabet-name object — SR5's "class condition without the identifier" in the one spelling the grammar
        // cannot tell from identifier-2; and a `condition` whose LEFTMOST leaf is that bare word (`WHEN MY-CLASS
        // AND …`). The classifier named all three PartialExpression, the last two by their resolved symbol.
        if (pair.Object is EvaluateObjectOperand.PartialExpression)
        {
            if (slot.Node.valueOperand() is not { } subjOp || slot.Value is not { } subjValue)
                return host.Cond.Refused("EVALUATE TRUE/FALSE paired with a value WHEN object");
            var content = slot.InPlaceValue is BoundFieldOperand inPlace ? inPlace : subjValue;
            var spliced = new ConditionBinder.PartialSubjectOperand(subjOp, subjValue, content);
            var objMark = host.Udf.Mark;
            var partialCond = item.partialExpression() is { } partial
                ? host.Cond.BindPartialExpression(partial, spliced)
                : item.condition() is { } leadingClass
                    ? host.Cond.BindPartialExpression(leadingClass, spliced)
                : pair.ObjectBare.ClassWord is { } classWord
                    ? host.Cond.BindPartialClassName(classWord, spliced)
                    : host.Cond.Refused($"EVALUATE partial-expression '{item.GetText()}'");
            return host.Udf.UdfAttachPerEvaluation(partialCond, objMark);
        }

        // Value subject vs operand / range: equality or inclusive bounds (§14.9.13 GR5b/c).
        // ⛔ THE SLOT'S ONE VALUE (§14.9.13.4 GR3, kb/Work PB394) — NEVER a fresh bind of the subject's node. The
        // subject is "evaluated and assigned a value" at the beginning of the statement, and a RANGE object reads
        // that one value TWICE ("selection-subject >= left-part AND selection-subject <= right-part", GR4 a) 5.):
        // before this, the two halves of ONE range test saw two DIFFERENT values of a value-varying subject, an
        // answer no single subject value can produce. No residue stage here any more — a subject activation
        // hoists to statement scope with the value, which is what the rule asks for.
        if (slot.Value is not { } left)
            return host.Cond.Refused("EVALUATE TRUE/FALSE paired with a value WHEN object");

        if (item.valueRange() is { } range)
        {
            var objMark = host.Udf.Mark;
            var lo = BindValueOperand(range.valueOperand(0));
            var (hi, alphabetWord) = BindRangeHigh(range);
            // ⛔ SR4 + SR9 BEFORE THE CLASS IS ASKED (kb/Work PB399). CollatingSelection.ForComparison's own
            // remark says the message-tag / object / pointer categories "never reach here" — and they DID, by
            // this route, because nothing screened the range's operands: a pointer pair answered ALPHANUMERIC,
            // turned EC-RANGE-INVALID checking on, and had the emitter order raw addresses. The screen makes
            // that remark true again, one phase earlier.
            // ⛔ A REJECTED RANGE CONTRIBUTES AN ERROR NODE, NOT A COMPARISON. Both lowerings below — the
            // BoundRangeMembership one and the inclusive CheckedRelational pair — ask questions these operands
            // have already failed to be admissible for, and the relation checkpoint would draw the §8.8.4.2.2
            // Format 3 diagnostic TWICE more about the same two operands after SR4 has said the range may not
            // exist at all.
            if (!ScreenRangeOperands(range, lo, hi))
                return host.Cond.Refused(
                    $"EVALUATE range-expression '{DataBinder.WrittenText(range)}'");
            bool check = ctx.EcState.Turn.Enabled("EC-RANGE-INVALID", null, item.Start.Line);
            // ⛔ THE RANGE'S CLASS, ASKED ONCE, OF BOTH ENDS (§14.9.13.3 SR4 gives the pair one class to have) —
            // and it is what BOTH of §14.7.8's halves below are keyed on: rule 1 (numeric) is algebraic, names no
            // sequence and sets no exception; rule 2 (alphanumeric or national) has the sequence, the
            // IN alphabet-name-1 phrase AND the EC. Two separately-written tests of one antecedent is how the two
            // halves of rule 2 came to disagree about the same range (kb/Work PB401).
            var rangeClass = CollatingSelection.ThroughRangeClass(lo, hi);
            // §14.9.13.2's range-expression ends `[ IN alphabet-name-1 ]`, and §14.7.8 rule 2 makes that alphabet
            // THE collating sequence the range is evaluated in — overriding the no-phrase arm's "defined by the
            // implementor" default, which for this compiler is the PROGRAM COLLATING SEQUENCE. The SR3 screens and
            // the carrier registration are the ONE resolver §14.7.8's opening sentence asks for ("This specification
            // applies to THROUGH phrases specified in the VALUE clause and the EVALUATE statement"), so the VALUE
            // clause's identical phrase reaches the same code (kb/Work PB398).
            string? alphabet = RangeAlphabet(alphabetWord, rangeClass);
            // §14.7.8 rule 2: an inverted alphanumeric/national THRU range sets the nonfatal EC-RANGE-INVALID, and
            // then "execution proceeds as if the range of values were empty". The EC is a property of the RANGE's
            // CLASS, exactly as the collating sequence one sentence above it is — never of the written FORM of its
            // ends.
            // ⛔ THIS GATE USED TO READ `lo is BoundStringLiteral{…} && hi is BoundStringLiteral` (kb/Work PB401),
            // which raised the exception ONLY where a reader of the source can already see the inversion and NEVER
            // in the run-time case rule 2's own "in the collating sequence in effect at runtime" is written for:
            // `EVALUATE X WHEN WS-M THRU WS-A` set nothing while `EVALUATE X WHEN "M" THRU "A"` over the identical
            // values set EC-RANGE-INVALID, and the level-88 VALUE range — the OTHER clause §14.7.8's first sentence
            // governs — already asked the CLASS. One rule, one antecedent, both clauses.
            bool ecApplies = check && CollatingSelection.IsCollatedThroughRange(rangeClass);
            // ⛔ AN IN PHRASE ROUTES HERE WHETHER OR NOT CHECKING IS ON: a relation pair derives its sequence from
            // its operands' categories, which is the very rule the phrase overrides, so the named sequence has
            // nowhere to ride on that lowering. The unchecked render of this node is that same inclusive pair.
            // ⛔ …BUT ONLY FOR A PAIR THAT COMPARES AS ONE ITEM EACH (kb/Work PB1469 / PB1467 sibling arm). The node
            // compares character IMAGES, which is §8.8.4.2.7 for a Flat pair and wrong for the other two shapes: a
            // strongly-typed range orders element by element (§8.8.4.2.12 — `EVALUATE A WHEN LO THRU HI` over float
            // leaves answered OUT for -10 <= -5 <= 3 the moment EC-RANGE-INVALID checking routed it here), and a
            // variable-length group has no image at all (§8.8.4.2.17). Those take the inclusive relation pair, which
            // is §14.9.13.4 GR4 a) 5.'s own lowering and passes through the ONE relation checkpoint.
            bool flatRange = RelationPair.Classify(left, lo) is RelationPairShape.Flat
                && RelationPair.Classify(left, hi) is RelationPairShape.Flat;
            if (flatRange && (alphabet is not null || ecApplies))
                return host.Udf.UdfAttachPerEvaluation(
                    host.Cond.CheckedRangeMembership(left, lo, hi, checkInvalid: ecApplies, alphabet),
                    objMark);
            return host.Udf.UdfAttachPerEvaluation(new BoundLogical("&&",
                [host.Cond.CheckedRelational(left, ">=", lo), host.Cond.CheckedRelational(left, "<=", hi)]),
                objMark);
        }
        if (item.valueOperand() is { } v)
        {
            var objMark = host.Udf.Mark;
            return host.Udf.UdfAttachPerEvaluation(
                host.Cond.CheckedRelational(left, "==", BindValueOperand(v)), objMark);
        }
        return host.Cond.Refused($"EVALUATE WHEN object '{item.GetText()}'");
    }

    /// <summary>The range's <c>IN alphabet-name-1</c> phrase (ISO §14.9.13.2's range-expression), screened by
    /// §14.9.13.3 SR3 and registered as a runtime carrier — or null when the phrase is absent or was refused, in
    /// which case §14.7.8 rule 2's no-phrase arm applies and the sequence stays the implementor's.
    /// <para>The range's CLASS is asked of the ONE comparison-class rule over BOTH bounds, never of one of them:
    /// SR3's two sentences are a test on the pair ("the literals or identifiers specified in the THROUGH phrase",
    /// then "if literal-3 or identifier-3 is of class national"), and §14.9.13.3 SR4 already requires the two to be
    /// of the same class — so the pair has one class to have. It is COMPUTED BY THE CALLER and passed in, because
    /// the EC gate one line below keys on the SAME antecedent and the two must not be able to disagree
    /// (kb/Work PB401).</para></summary>
    private string? RangeAlphabet(string? name, CollatingClass rangeClass) =>
        name is not null && ctx.Data.TryResolveRangeAlphabet(name, rangeClass, "an EVALUATE WHEN THROUGH range")
            ? name : null;

    /// <summary>The range's right-hand operand and its <c>IN alphabet-name-1</c> word, with the one ambiguity the
    /// grammar cannot settle settled by SYMBOL (kb/Work PB843). §14.9.13.3 SR3 admits the phrase over identifiers
    /// ("the literals or identifiers specified in the THROUGH phrase"), but <c>IN</c> is also the qualification
    /// connective, so over an identifier-4 the parser's greedy <c>dataReferenceSuffix*</c> loop takes
    /// <c>WS-HI IN AL</c> as a qualified reference and the phrase is never seen.
    /// <para>The symbol decides it: §8.3.2.2 — "a given user-defined word may be used as only one type of
    /// user-defined word" — makes an alphabet-name a different type from every name a qualifier can be
    /// (§8.4.2.2.1: qualifiers are "superordinate names from the hierarchy to which a user-defined name belongs"),
    /// so a trailing <c>IN word</c> that names a declared alphabet IS the phrase, and the reference is bound without
    /// it. Only the LAST suffix, only the <c>IN</c> spelling (the phrase prints no <c>OF</c>) and only a bare word
    /// (the phrase takes no subscript) can be it; any other qualifier keeps its qualification reading.</para></summary>
    private (BoundOperand Hi, string? AlphabetWord) BindRangeHigh(Core.ValueRangeContext range)
    {
        var hiNode = range.valueOperand(1);
        if (range.cobolWord() is { } written) return (BindValueOperand(hiNode), written.GetText());
        if (hiNode.arithmeticExpression() is { } expr && ConditionBinder.SoleDataRef(expr) is { } dref
            && dref.dataReferenceSuffix() is { Length: > 0 } suffixes
            && suffixes[^1].qualification() is { } q && q.IN() is not null && q.ChildCount == 2
            && ctx.Data.IsAlphabetName(q.cobolWord().GetText()))
            return (host.Expr.FieldOperand(ReferenceResolver.WithoutTrailingSuffix(dref)), q.cobolWord().GetText());
        return (BindValueOperand(hiNode), null);
    }

    /// <summary>The selection OBJECT's condition when <see cref="ClassifyPair"/> put it in Table 15's Condition
    /// ROW: an explicit <c>condition</c> object, or the bare form (a level-88, a switch-status condition-name,
    /// or — through §14.9.13.3 SR6 a) — a one-boolean-character boolean object) that the SHARED analysis
    /// already resolved. One helper, so the two arms that need condition-2 cannot recognise different
    /// shapes.</summary>
    private BoundCondition? ObjectAsCondition(Core.EvaluateWhenItemContext item, in EvaluatePairing pair) =>
        item.condition() is { } cond ? host.Cond.BindCondition(cond) : host.Cond.AsCondition(pair.ObjectBare);

    /// <summary>Bind (and, when more than one pair reads it, MATERIALIZE) one selection subject's assigned
    /// TRUTH value — ISO §14.9.13.4 GR3 e), "Any selection subject specified by condition-1 is assigned a truth
    /// value according to the rules for evaluating conditional expressions". Null when the subject has no
    /// condition form (a value subject that is not SR6 b)'s one-boolean-character boolean).
    /// <para>The condition is a written <c>condition</c> — bound by THE ONE condition binder, so a class test, a
    /// relation, a sign test, <c>NOT</c> over a boolean item (§8.8.4.3.2) and every combined condition mean here
    /// exactly what they mean in an IF — or, through the SHARED bare-operand resolution the classifier already
    /// made, a level-88 condition-name (§8.8.4.2.7 r2), a switch-status condition-name (§8.8.4.6), or SR6 b)'s
    /// boolean subject as a §8.8.4.3 simple boolean condition (kb/Work PB400: one resolution, consulted twice).</para>
    /// <para>⛔ BOUND ONCE, NEVER PER WHEN (kb/Work PB842 / PB912). This used to run inside the pair binder, so the
    /// condition was re-bound — and re-emitted — once per WHEN arm, and a user-defined function inside it was
    /// refused (COBOLNET1509) because a per-WHEN re-bind would have over-activated it. With the truth value in an
    /// intermediate the re-bind and the refusal are both gone. The intermediate is taken on the SAME
    /// <see cref="SubjectUsage.NeedsIntermediate"/> test as a value subject's: a single read in the first arm is
    /// itself the one evaluation at "the beginning of the execution of the EVALUATE statement".</para>
    /// <para>A class test reads the ORIGINAL operand's character content here (the condition is bound over the
    /// written operand, never over a value intermediate), so <c>IS NUMERIC</c> still sees invalid content that a
    /// numeric intermediate would have normalized (§8.8.4.4) — only the resulting TRUTH value is stored.</para></summary>
    private BoundCondition? BindSubjectTruth(SubjectSlot slot)
    {
        var truth = slot.Node.condition() is { } cond ? host.Cond.BindCondition(cond)
            : host.Cond.AsCondition(slot.Bare);
        if (truth is null) return null;
        return slot.NeedsIntermediate ? host.SendingValue.MaterializeTruth(truth, "evaluate") : truth;
    }

    /// <summary>A pair whose truth value is known at bind time: the empty conjunction is true (it renders as the
    /// constant true), its negation false.</summary>
    private static BoundCondition Constant(bool value) =>
        value ? new BoundLogical("&&", []) : new BoundNot(new BoundLogical("&&", []));

    /// <summary>The boolean value of a condition that is a SOLE <c>TRUE</c>/<c>FALSE</c> literal, else null.</summary>
    private static bool? SoleBooleanLiteral(Core.ConditionContext cond)
    {
        Antlr4.Runtime.Tree.IParseTree n = cond;
        while (n is not Core.BooleanLiteralContext)
        {
            if (n.ChildCount != 1) return null;
            n = n.GetChild(0);
        }
        return ((Core.BooleanLiteralContext)n).TRUE_() is not null;
    }

    /// <summary>
    /// ⛔ ONE SELECTION SUBJECT, BOUND ONCE FOR THE WHOLE STATEMENT — ISO §14.9.13.4 GR3, "At the beginning of
    /// the execution of the EVALUATE statement, each selection subject is evaluated and assigned a value, a
    /// range of values, or a truth value" (kb/Work PB394).
    /// <para>The chained-selection lowering pairs the subject against every WHEN's object, and a RANGE object
    /// pairs against it TWICE (GR4 a) 5. — "selection-subject &gt;= left-part AND selection-subject &lt;=
    /// right-part"). Binding the subject inside that loop meant the subject EXPRESSION was emitted once per
    /// relational USE, so a value-varying subject gave each use a different value — measured: an EVALUATE with
    /// two THRU arms consumed FOUR values of <c>FUNCTION RANDOM</c> and printed a branch no single subject value
    /// can select. The slot binds it once and hands the same node to every pair.</para>
    /// <para><b>Both halves are lazy on purpose.</b> The bare-operand analysis is the resolution
    /// <c>ClassifyPair</c> used to redo per pair; the VALUE is bound only if some pair actually asks for one, so
    /// a condition-name / class-test / TRUE-FALSE subject never runs a value bind (which would be a symbol
    /// lookup no rule asks for, and a diagnostic no rule licenses).</para>
    /// </summary>
    private sealed class SubjectSlot(EvaluateBinder owner, Core.EvaluateSubjectContext node, SubjectUsage usage)
    {
        private BareOperandAnalysis _bare;
        private bool _bareBound;
        private BoundOperand? _value;
        private bool _valueBound;
        private BoundCondition? _truth;
        private bool _truthBound;
        private BoundOperand? _inPlace;

        /// <summary>The subject's parse node — the classifier and the condition path still read it.</summary>
        public Core.EvaluateSubjectContext Node => node;

        /// <summary>Whether GR3's evaluation needs the implementor's intermediate result item — the
        /// §14.9.25.4 GR1 argument applied to GR3: at ONE use IN THE FIRST ARM the single render already IS the
        /// one evaluation, so no intermediate is created. EVALUATE is a hot verb. Every other shape (no use at
        /// all, a use only in a later arm, more than one use) needs the store. See <see cref="SubjectUsage"/>.</summary>
        public bool NeedsIntermediate => usage.NeedsIntermediate;

        /// <summary>The subject's §14.9.13.3 SR6 bare-operand resolution, made once for the statement.</summary>
        public BareOperandAnalysis Bare
        {
            get
            {
                if (!_bareBound) { _bare = owner.AnalyzeSubjectBare(node); _bareBound = true; }
                return _bare;
            }
        }

        /// <summary>The subject's assigned VALUE (GR3 a/b/c/d) — materialized into the implementor's
        /// intermediate result item when more than one pair reads it. Null when the subject has no value form
        /// (TRUE/FALSE, or a condition-name this classifier already resolved as condition-1).</summary>
        public BoundOperand? Value
        {
            get
            {
                if (!_valueBound) { _value = owner.BindSubjectValue(this, out _inPlace); _valueBound = true; }
                return _value;
            }
        }

        /// <summary>The subject's operand as BOUND, before any materialization — the one bind of the written
        /// operand, read by a shape that must see the item's own content (a partial-expression class test).
        /// Null when the subject has no value form.</summary>
        public BoundOperand? InPlaceValue { get { _ = Value; return _inPlace; } }

        /// <summary>True when the subject is condition-1 by its FORM, whatever it is paired with: a written
        /// condition, or a bare level-88 / switch-status condition-name. (SR6 b)'s boolean subject is condition-1
        /// only for a TRUE/FALSE pairing, so it is not included.)</summary>
        public bool IsCondition1ByForm =>
            node.condition() is not null || Bare.Form is BareOperandForm.ConditionName or BareOperandForm.SwitchStatus;

        /// <summary>The subject's assigned TRUTH value (GR3 e) — bound once, materialized into a one-position
        /// boolean intermediate when more than one pair reads it. Null when the subject has no condition form.</summary>
        public BoundCondition? Truth
        {
            get
            {
                if (!_truthBound) { _truth = owner.BindSubjectTruth(this); _truthBound = true; }
                return _truth;
            }
        }
    }

    /// <summary>The subject's bare-operand analysis (§14.9.13.3 SR6) — analyzed only when the subject really IS
    /// a bare operand: under a written condition-1 the operands belong to the condition, and resolving one as a
    /// condition-name would be a symbol lookup no rule asks for (and a diagnostic no rule licenses).</summary>
    private BareOperandAnalysis AnalyzeSubjectBare(Core.EvaluateSubjectContext subject) =>
        subject.valueOperand() is { } svo ? host.Cond.AnalyzeBareOperand(svo) : default;

    /// <summary>Bind (and, when more than one pair reads it, MATERIALIZE) one selection subject's assigned value
    /// — ISO §14.9.13.4 GR3 a)–d). Null when the subject has no value form.
    /// <para>⛔ A written CONDITION-1 subject's operands are NOT materialized, and that is a rule, not an
    /// omission: GR3 e) assigns the subject <b>condition-1</b> a TRUTH value, not its operand a data value, and a
    /// class test reads the operand's CHARACTER CONTENT — copying a numeric-DISPLAY item through an intermediate
    /// would normalize content that <c>IS NUMERIC</c> exists to find invalid (§8.8.4.4). The TRUTH value is what
    /// is held once (<see cref="BindSubjectTruth"/>).</para>
    /// <para>⛔ A bare CONDITION-NAME / switch-status subject has no value form at all (§8.8.4.2.7 r2 /
    /// §8.8.4.6 make it condition-1), so it returns null rather than being bound as an operand.</para></summary>
    private BoundOperand? BindSubjectValue(SubjectSlot slot, out BoundOperand? inPlace)
    {
        inPlace = null;
        var subject = slot.Node;
        if (subject.valueOperand() is not { } vo) return null;
        if (slot.Bare.Form is BareOperandForm.ConditionName or BareOperandForm.SwitchStatus) return null;
        var value = inPlace = BindValueOperand(vo);
        // ⛔ NeedsIntermediate, NOT `Uses > 1` (kb/Work PB396). The §14.9.25.4 GR1 argument this slot already
        // carried — at ONE use the single render IS the one evaluation, so nothing is observable and no
        // intermediate is created — holds only when that use is the FIRST arm's. At ZERO uses (every object
        // paired with this subject is ANY) it has no premise at all: nothing renders the subject, so without
        // the intermediate GR3's evaluation never happens. And at one use in a LATER arm the render sits in an
        // `else if` an earlier arm can skip, which is not "the beginning of the execution of the EVALUATE
        // statement". Materializing gives it the one PRE-op store §14.9.13.4 GR3 requires, at the head.
        if (!slot.NeedsIntermediate || host.SendingValue.Materialize(value, "evaluate") is not { } frozen) return value;
        // The intermediate is the implementor's device; every §8.8.4.2 operand rule is about the operand WRITTEN,
        // so the relation checkpoint is told which one this stands for (kb/Work PB1468 — an index-name subject's
        // intermediate is a numeric item, and `EVALUATE I1 WHEN I2 WHEN IDA` was refused as numeric-vs-index).
        var intermediate = new BoundFieldOperand(frozen);
        host.Cond.RecordIntermediate(intermediate, value);
        return intermediate;
    }

    /// <summary>How the statement's WHEN phrases USE selection subject <c>index</c> — the two facts that
    /// together decide whether §14.9.13.4 GR3's evaluation needs the implementor's intermediate result item.
    /// <para><c>Uses</c> counts the RELATIONAL uses: GR4 a) 5.'s range form pairs the subject against BOTH
    /// bounds, so it counts twice, and <c>WHEN OTHER</c> pairs with nothing at all. ⛔ <c>ANY</c> counts ZERO
    /// (kb/Work PB396): GR4 a) 1. makes the result true WITHOUT consulting the subject, so an ANY object emits
    /// no render of it — the comment this replaced claimed counting ANY as a use "only over-materializes, never
    /// under-", and the opposite was true: an EVALUATE whose every object is ANY counted one use, skipped the
    /// intermediate, and then evaluated the subject NOWHERE.</para>
    /// <para><c>ReadByFirstPhrase</c> is the other half, and it is what makes a single use safe to leave
    /// un-hoisted: the one render then sits in the FIRST arm's condition, which the emitted if/else-if chain
    /// always executes, so it happens "at the beginning of the execution of the EVALUATE statement". A single
    /// use in a LATER arm does not — an earlier arm can select and the render is skipped — so that shape takes
    /// the intermediate too.</para></summary>
    private static SubjectUsage SubjectUsageOf(Core.EvaluateStatementContext ev, int index)
    {
        int uses = 0;
        bool readByFirst = false, seenFirst = false;
        foreach (var clause in ev.evaluateWhenClause())
            foreach (var phrase in clause.evaluateWhenPhrase())
            {
                var groups = phrase.evaluateWhenGroup();
                var item = index < groups.Length ? groups[index].evaluateWhenItem() : null;
                bool reads = item is not null && item.ANY() is null;
                if (!seenFirst) { readByFirst = reads; seenFirst = true; }
                if (reads) uses += item!.valueRange() is not null ? 2 : 1;
            }
        return new SubjectUsage(uses, readByFirst);
    }

    /// <summary>The §14.9.13.4 GR3 usage profile of one selection subject — see <see cref="SubjectUsageOf"/>.</summary>
    private readonly record struct SubjectUsage(int Uses, bool ReadByFirstPhrase)
    {
        /// <summary>True unless the subject's ONE relational use is the first arm's, which is the only shape in
        /// which the un-hoisted render is itself GR3's evaluation.</summary>
        public bool NeedsIntermediate => !(Uses == 1 && ReadByFirstPhrase);
    }

    /// <summary>Bind a <c>valueOperand</c> (an arithmetic expression or a non-numeric literal) as a comparison
    /// operand — the same shapes <see cref="ComparisonOperand"/> produces.</summary>
    private BoundOperand BindValueOperand(Core.ValueOperandContext vo)
    {
        // ⛔ THROUGH THE ONE nonNumericLiteral MAPPING (kb/Work PB172's sweep). This was a FOURTH hand-written
        // copy of that dispatch — concatenation, figurative, STRINGLIT, NATLIT, BOOLLIT — and it was **missing
        // the HEXLIT arm**, so `EVALUATE X WHEN X"6162"` fell past all five, found no arithmeticExpression, and
        // bound to `BoundOperandError("EVALUATE operand")` → an unhandled NotImplementedCobolFeatureException at
        // RUN TIME on conforming source (§8.3.3.2.1 makes both formats of the alphanumeric literal class and
        // category alphanumeric). That is DA3's defect exactly, in the one copy DA3's extraction missed:
        // ExpressionBinder.NonNumericLiteralOperand's own remark lists the three it collapsed. Measured on
        // 9a89fbd1 before the fix, not deduced.
        // NULL as a subject/object: §14.9.13.3 SR7 a) makes a selection object "valid operands for comparison … in
        // accordance with 8.8.4.2", i.e. the pair is a relation condition — a §8.4.3.10.3 SR1 context — and the
        // relation checkpoint decides the class pairing.
        if (host.Expr.NullAdmittingOperand(vo.nonNumericLiteral()) is { } litOp) return litOp;
        // A boolean operand enclosed in parentheses is that boolean operand (§8.8.2) — the relation side's reading
        // (ConditionBinder.EnclosedBooleanOperand, kb/Work PB1464).
        if (host.Cond.EnclosedBooleanOperand(vo) is { } enclosedBoolean) return enclosedBoolean;
        if (vo.arithmeticExpression() is { } expr)
            // ⛔ ARM FOR ARM IN THE SAME ORDER AS ConditionBinder.ComparisonOperandOf, DELIBERATELY (kb/Work
            // PB224). §14.9.13.4 GR2 makes an EVALUATE subject/object comparison "as if" the corresponding
            // relation condition were written, so the two operand binders answer ONE question and any divergence
            // between them is a latent Table-15-vs-§8.8.4.2 split. The order was data-ref → num-literal →
            // function-call here and data-ref → function-call → num-literal there; the alternatives are disjoint,
            // so the two agreed by luck, and "agrees by luck" is what this cluster keeps finding.
            return ConditionBinder.SoleDataRef(expr) is { } dref ? host.Expr.FieldOperand(dref)
                // A SOLE function-identifier is a §15.2 sending item of its own class, not an arithmetic term —
                // the same short-circuit ConditionBinder.ComparisonOperandOf makes, and for the same reason
                // (kb/Work PB172): `EVALUATE FUNCTION LOWER-CASE(X) WHEN "abc"` compares alphanumerically.
                : ConditionBinder.SoleFunctionCall(expr) is { } sfc
                    ? IntrinsicBinder.OperandOf(host.Intrinsic.BindIntrinsic(sfc))
                // …and its Format-4 twin, as on the relation side (kb/Work PB1142): a sole inline method
                // invocation is an identifier of its temporary's class (ISO §8.4.3.4.4 GR1).
                : ConditionBinder.SoleInlineInvocation(expr) is { } sii ? host.Oo.OoInlineInvocationOperand(sii)
                // A sole numeric LITERAL stays a literal operand — against an alphanumeric/group operand it
                // participates as its WRITTEN character form, leading zeros intact (ISO §8.8.4.2.1).
                : ConditionBinder.SoleNumLiteral(expr) is { } lit ? new BoundNumericLiteral(host.Expr.CheckLiteral(lit))
                // The ONE expression→operand mapping, as on the relation side: a user-function reference binds to
                // a BoundNumRef over its result temp and MUST surface as a FIELD operand so the temp's cloned
                // category (§8.4.3.2.4 GR1) drives the class dispatch; a raw BoundComputedOperand — which this
                // arm used to build — would compare an alphanumeric/national result NUMERICALLY. For every other
                // shape OperandOf returns the identical BoundComputedOperand, so the emit floor is unchanged.
                : IntrinsicBinder.OperandOf(host.Expr.BindIndexWindowExpr(expr));   // EVALUATE compares — a relation window (kb/Work R29)
        return BoundOperandError.Refused(ctx.Edition, "EVALUATE operand");
    }
}
