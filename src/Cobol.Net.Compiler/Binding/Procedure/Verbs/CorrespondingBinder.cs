// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions.Diagnostics;
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Binding.Validation;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>The MOVE/ADD/SUBTRACT CORRESPONDING binder (P7 Step 10e — a real collaborator over
/// <see cref="BinderContext"/>): the ONE <c>_corrCounter</c> owner — hoist-local numbering spans all three
/// verbs' statements of the unit, so exactly ONE instance exists per unit (the byte-exact-snapshot hazard
/// the census flagged). The four bound types stayed in <c>Binding/Bound/BoundCorresponding.cs</c>.</summary>
internal sealed class CorrespondingBinder(BinderContext ctx, StatementBinder host)
{
    private int _corrCounter;   // unique hoist-local ids (__corrNs / __corrNt) across the program unit

    /// <summary>Bind <c>ADD {CORRESPONDING|CORR} id-4 TO id-5 [ROUNDED] [ON SIZE ERROR …] [END-ADD]</c>
    /// (ISO §14.9.2.2 Format 3; SR5 — CORR and CORRESPONDING are equivalent, BOTH tokens tested). The one
    /// rounded-phrase follows the receiving group and applies to every implied statement; <c>ROUNDED MODE IS</c>
    /// is edition-gated inside <c>RoundingOf</c> (2014+, §14.7.4). Reached from <c>StatementBinder.BindAdd</c> when
    /// the operand-list alternative is absent — i.e. exactly the Format-3 parse.</summary>
    public BoundStatement BindAddCorresponding(Core.AddStatementContext add) =>
        add.CORRESPONDING() is not null || add.CORR() is not null
            ? Bind(CorrVerb.Add, add.dataReference(), host.Expr.RoundingOf(add.roundedPhrase()),
                host.BindSizeError(add.arithmeticOnSizeError()))
            : BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "ADD: the statement matches no ADD format (ISO §14.9.2.2)");

    /// <summary>Bind <c>SUBTRACT {CORRESPONDING|CORR} id-4 FROM id-5 [ROUNDED] [ON SIZE ERROR …] [END-SUBTRACT]</c>
    /// (ISO §14.9.44.2 Format 3; SR5 — CORR ≡ CORRESPONDING).</summary>
    public BoundStatement BindSubtractCorresponding(Core.SubtractStatementContext sub) =>
        sub.CORRESPONDING() is not null || sub.CORR() is not null
            ? Bind(CorrVerb.Subtract, sub.dataReference(), host.Expr.RoundingOf(sub.roundedPhrase()),
                host.BindSizeError(sub.arithmeticOnSizeError()))
            : BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, "SUBTRACT: the statement matches no SUBTRACT format (ISO §14.9.44.2)");

    /// <summary>
    /// Bind a CORRESPONDING statement (ISO §14.7.6): resolve both group operands ONCE, hoist their anchors (item
    /// identification at statement start), compute the corresponding pairs at BIND time, and expand to a
    /// <see cref="BoundCorresponding"/> the backend renders as the per-pair implied statements (MOVE GR11 §14.9.25.4 /
    /// ADD GR5 §14.9.2.4 / SUBTRACT GR5 §14.9.44.4 — "the same as if the user had referred to each pair … in
    /// separate statements"). The entire Format-2/Format-3 surface is COBOL-85 (no edition gate on the forms
    /// themselves).
    /// <para>⛔ THE "NO REPRESENTATION IN THIS DATA MODEL YET" CLAIM THAT STOOD HERE WAS FALSE (kb/Work PB391).
    /// It covered the SR6/SR12 operand categories AND "the rule-4 object/pointer/message-tag exclusion classes",
    /// and the model carries all of them: <see cref="GroupUsage"/> for bit and national groups,
    /// <see cref="StrongTypeModel"/> and <see cref="VariableLengthCompatibility"/> for the other two kinds, and
    /// <see cref="PicCategory"/>'s Pointer / ProgramPointer / FunctionPointer / ObjectReference members plus
    /// <c>Usage.MessageTag</c> for rule 4's classes. Rule 4's class leg is now in <see cref="CorrEligible"/>; the
    /// SR6/SR12 operand-kind screen is <c>StatementValidation.CheckCorrespondingGroupOperand</c>.</para>
    /// </summary>
    public BoundStatement Bind(
        CorrVerb verb, Core.DataReferenceContext[] groups, CobolRounding rounding, SizeErrorPhrase? sizeErr)
    {
        // ⛔ EACH VERB ASKS ITS OWN RULE, NOT A SHARED APPROXIMATION (kb/Work PB392). What stood here was a
        // verb-NAME switch here and a CLAUSE-STRING switch below, so the three rules differed only in the
        // citation the message printed while the predicate — a boolean `IsGroup` — was the intersection of none
        // of them: it admitted a GROUP-USAGE BIT group to ADD and SUBTRACT, whose SR6 names four group kinds and
        // not that one. The row carries the verb's spelling AND the clause AND the admitted kind set AND whether
        // the rule's text names level-66, so the three cannot drift apart again.
        CorrespondingOperandRule rule = verb switch
        {
            CorrVerb.Move => CorrespondingOperandRule.Move,
            CorrVerb.Add => CorrespondingOperandRule.Add,
            _ => CorrespondingOperandRule.Subtract,
        };
        string verbName = rule.Verb;
        if (groups.Length < 2)
            return BoundRejected.Report(ctx.Edition, DiagnosticCatalog.StatementFormatShape, $"{verbName} CORRESPONDING: its general format prints exactly two group "
                + "operands, the sending group and the receiving group");
        if (host.Expr.ResolveSending(groups[0]) is var srcR && srcR.Place is not { } src)
            return srcR.Refusal(ctx.Edition);   // kb/Work PB1030
        if (host.Expr.ResolveReceiving(groups[1]) is not { } dst)
            return BoundRejected.Reported(ctx.Edition);   // the receiving chokepoint reported it — not a deferral (kb/Work PB236, PB881)
        // ⛔ BOTH OPERANDS SHALL BE GROUP ITEMS, AND THAT IS A SYNTAX RULE, SO IT IS DECIDED AT BIND TIME
        // (kb/Work PB236, row SR-14.9.2.3-6). MOVE §14.9.25.3 SR12 — "Identifier-3 and identifier-4 shall
        // specify group data items and shall not be reference-modified" — and ADD §14.9.2.3 SR6 / SUBTRACT
        // §14.9.44.3 SR6 — "Identifier-4 and identifier-5 shall be alphanumeric group items, national group
        // items, variable-length groups, or strongly-typed group items and shall not be described with
        // level-number 66". The predicate and the citation were already right here; the STAGE was not: the old
        // BoundUnsupported made `ADD CORR ELEM TO GRP` compile clean and throw NotImplementedCobolFeatureException
        // only if the statement was reached, where ISO §4.2.2 ¶2 requires a compile-time mechanism.
        // ⛔ AND THE LEVEL-66 CLAIM THAT USED TO STAND HERE WAS FALSE. This comment said a RENAMES entry
        // "already failed the resolves above"; it does not. DataBinder.BindRenames builds it with Pic null and
        // no Children into `_lastRoot.Renames66`, so DataItem.IsGroup is false for it and it landed in the
        // elementary-operand arm — rejected for a reason the rule does not give. SR6 excludes it BY NAME, and
        // StatementValidation now says so. MOVE's SR12 names no level number, and a THROUGH alias IS an
        // alphanumeric group item (§13.18.45.4 GR2), so it is admitted there (kb/Work PB966) and its members are
        // the elementary items GR2 says it includes (CorrMembers).
        // ⛔ AND SR12's SECOND HALF IS NOW CHECKED INSTEAD OF ASSUMED (kb/Work PB390). What stood here claimed
        // SR12's "not reference-modified" half "DOES hold structurally: reference modification resolves only over
        // elementary character items, never a group" — false. A group item IS reference-modifiable and has its own
        // golden (tests/conformance/2023/pb70_group_reference_modification.cob). Nothing checked the prohibition;
        // the shape was refused only by an accident of CorrAccess.Create's default arm, under a storage-shape
        // message naming neither the rule nor reference modification — and once that factory switched on
        // Place.Undecorated (kb/Work PB393) the accident stopped refusing it at all, so `MOVE CORRESPONDING
        // G1(1:3) TO G2` moved the WHOLE group with the modifier silently discarded. The screen below reads the
        // PLACE, decorations intact, which is the only level at which a reference modifier is visible.
        // Both operands are screened before the verdict — a statement with two bad operands reports two
        // diagnostics, not the first one only (a short-circuit here would hide the second).
        bool srcOk = ctx.Validation.CheckCorrespondingGroupOperand(src, groups[0].GetText(), rule);
        bool dstOk = ctx.Validation.CheckCorrespondingGroupOperand(dst, groups[1].GetText(), rule);
        if (!srcOk || !dstOk) return BoundRejected.Reported(ctx.Edition);

        int id = _corrCounter++;
        var hoists = new List<CorrespondingHoist>();
        if (CorrAccess.Create(src, $"__corr{id}s", hoists, ctx.Refs) is not { } srcAcc
            || CorrAccess.Create(dst, $"__corr{id}t", hoists, ctx.Refs) is not { } dstAcc)
            return new BoundUnsupported($"{verbName} CORRESPONDING group operand storage shape "
                + $"('{src.Item.CobolName}' / '{dst.Item.CobolName}' is neither a member path nor a REDEFINES view)");

        var pairs = new List<CorrespondingPair>();
        if (CorrMatch(verb, verbName, src.Item, dst.Item, srcAcc, dstAcc, [], [], pairs, rounding) is { } fail)
            return new BoundUnsupported(fail);
        // Zero pairs is a legal EMPTY implied set (§14.7.6 selects none silently): MOVE does nothing; an arithmetic
        // statement raises no size error, so a NOT ON SIZE ERROR phrase still runs (§14.7.5 rule 3).
        return new BoundCorresponding(verb, hoists, pairs, rounding, sizeErr);
    }

    /// <summary>
    /// The pair matcher (ISO §14.7.6, one source level per call): index D2's ELIGIBLE children by name, iterate
    /// D1's children in DECLARATION order (the implied-statement execution order), and for each name present and
    /// unique on BOTH sides descend matched group×group levels (rule 1 — identical relative qualification paths)
    /// or yield the pair, filtered per verb (rule 2 move-validity / rule 3 both-numeric). Rule 5 holds for free:
    /// the recursion never descends into an ineligible (OCCURS/REDEFINES) child. Returns a loud-failure reason for
    /// an unreachable storage shape, else <see langword="null"/>.
    /// </summary>
    private string? CorrMatch(CorrVerb verb, string verbName, DataItem src, DataItem dst,
        CorrAccess srcAcc, CorrAccess dstAcc, List<DataItem> sChain, List<DataItem> dChain,
        List<CorrespondingPair> pairs, CobolRounding rounding)
    {
        var dstByName = new Dictionary<string, List<DataItem>>(CobolNames.Comparer);
        foreach (var d in CorrMembers(dst))
        {
            if (!dstByName.TryGetValue(d.CobolName!, out var list)) dstByName[d.CobolName!] = list = [];
            list.Add(d);
        }
        // Rule 6 is SYMMETRIC ("the name … is unique after application of the implied qualifiers"): a duplicated
        // name on EITHER side makes the implied qualified reference ambiguous — excluded, not an error. The count is
        // over EVERY namesake the qualified reference could identify (kb/Work PB1111), not over the rule-1/4/5
        // eligible set: an OCCURS / REDEFINES / class-excluded twin still defeats uniqueness, because the rule's
        // subject is the eligible item and its predicate is the uniqueness of that item's NAME.
        List<DataItem> srcMembers = CorrMembers(src);
        var srcCount = NamesakeCounts(src);
        var dstCount = NamesakeCounts(dst);

        foreach (var s in srcMembers)
        {
            if (srcCount[s.CobolName!] > 1) continue;                          // rule 6, source side
            if (!dstByName.TryGetValue(s.CobolName!, out var cands)) continue; // rule 1: no same-named target
            if (dstCount[s.CobolName!] > 1) continue;                          // rule 6, target side
            DataItem d = cands[0];

            // A group × group namesake pair DESCENDS, and that is true of a bit group and a national group too:
            // NOTE 5 under §14.9.25.4 GR11 — "For purposes of MOVE CORRESPONDING, bit group items and national
            // group items are processed as group items, rather than as elementary items" — which is the one
            // place the MOVE statement does NOT treat them as the elementary items §14.9.25.4 GR4 makes them.
            if (s.IsGroup && d.IsGroup)
            {
                sChain.Add(s); dChain.Add(d);
                string? fail = CorrMatch(verb, verbName, s, d, srcAcc, dstAcc, sChain, dChain, pairs, rounding);
                sChain.RemoveAt(sChain.Count - 1); dChain.RemoveAt(dChain.Count - 1);
                if (fail is not null) return fail;
                continue;
            }
            if (verb is CorrVerb.Move)
            {
                // Rule 2: at least one elementary (guaranteed — not both groups) AND the move valid per the MOVE
                // rules; an invalid combination means the pair simply does NOT correspond (silent skip, never an
                // error). The question goes to the ONE Table 16 — see CorrRule2MoveValid, which is a delegation.
                if (!CorrRule2MoveValid(s, d)) continue;
            }
            // Rule 3: BOTH shall be numeric data items — a group is class alphanumeric and numeric-edited is
            // category numeric-edited (§8.4.2), so only elementary-numeric × elementary-numeric qualifies; every
            // other namesake silently does not correspond. (The legacy matcher lacked this filter; spec governs.)
            else if (s.Pic?.Category is not PicCategory.Numeric || d.Pic?.Category is not PicCategory.Numeric)
                continue;

            sChain.Add(s); dChain.Add(d);
            Place? sp = srcAcc.ChildPlace(sChain);
            Place? dp = dstAcc.ChildPlace(dChain);
            sChain.RemoveAt(sChain.Count - 1); dChain.RemoveAt(dChain.Count - 1);
            if (sp is null || dp is null)
                return $"{verbName} CORRESPONDING pair '{s.CobolName}' (subscripted group operand over an "
                    + "interior REDEFINES class — member storage unreachable in this slice)";
            // The per-edition composite of operands is per PAIR — §14.9.2.3 SR1c / §14.9.44.3 SR1c: "the two
            // corresponding operands for each separate pair".
            if (verb is not CorrVerb.Move)
                ctx.Validation.CheckComposite(verbName, [new BoundNumRef(sp)], [new Receiver(dp, rounding)]);
            // MOVE CORRESPONDING's implied MOVE is BOUND here, with the pair (kb/Work PB880) — §14.9.25.4 GR11
            // makes it "the same as if the user had referred to each pair … in separate MOVE statements".
            pairs.Add(new CorrespondingPair(sp, dp)
            {
                Move = verb is CorrVerb.Move
                    ? host.Move.BindMoveOf(new BoundFieldOperand(sp), [dp], ImplicitMovePhrase.MoveCorresponding)
                    : null,
            });
        }
        return null;
    }

    /// <summary>A child participates in correspondence matching unless excluded (ISO §14.7.6): rule 1 — FILLER
    /// (and a nameless group's whole subtree, which can never satisfy rule 1's name-qualified identity: a FILLER
    /// level contributes no qualifier — the legacy/NIST-proven reading); rule 4 — <i>"Neither data item contains
    /// an OCCURS, REDEFINES, or RENAMES clause or is of class index, message-tag, object, or pointer"</i>.
    /// Level-66 entries never join <see cref="DataItem.Children"/>, so rule 4's RENAMES leg is structural; the
    /// <see cref="DataItem.Renames"/> test is defensive.
    /// <para>⛔ RULE 4'S CLASS LEG IS THE WHOLE FOUR-CLASS SET, THROUGH THE ONE PREDICATE (kb/Work PB391). What
    /// stood here was <c>Pic?.Usage is not Usage.Index</c> under a comment claiming "the 2002+/2014 exclusion
    /// classes (object, pointer, message-tag) have no representation in this data model yet" — a premise
    /// <see cref="PicInfo"/> contradicts (it carries Pointer / ProgramPointer / FunctionPointer /
    /// ObjectReference categories and a MessageTag usage). A POINTER namesake pair was excluded only by the
    /// ACCIDENT of the private Table-16 copy's <c>_ =&gt; false</c> default, measured: with that copy deleted and
    /// this leg absent, <c>MOVE CORRESPONDING</c> over two <c>TYPE PT</c> groups would have silently copied a
    /// pointer. <see cref="ItemCategory.IsIndexMessageTagObjectOrPointer"/> is §13.16.3 SR24 e)'s and
    /// §13.18.60.3 SR11's population too — one class test, three rules.</para>
    /// <para>⛔ AND "IS IT A DATA ITEM AT ALL" IS SCREENED HERE, not inside the rule-2 filter where it used to
    /// sit. A PICTURE-less entry with no subordinates is neither elementary (§8.5.1.3.1 — "The most basic
    /// subdivisions of a record, that is, those not further subdivided, are called elementary items") nor a group item — an
    /// error-recovery artifact (a refused MESSAGE-TAG or staged FUNCTION-POINTER entry, a picture-less
    /// <c>USAGE NATIONAL</c> awaiting COBOLNET0881) — and §14.7.6 speaks only of "a data item in D1". Keeping it
    /// in a MOVE-validity filter made the exclusion invisible to ADD and SUBTRACT CORRESPONDING, which reached
    /// it only through rule 3's numeric test by luck of ordering.</para></summary>
    /// <summary>⛔ THE DATA ITEMS "IN" ONE LEVEL OF A CORRESPONDING OPERAND, rule 1/4/5-eligible, in declaration
    /// order — the ONE place the matcher asks, so both operands (D1 AND D2) answer the same way (kb/Work PB966).
    /// For an ordinary group they are its eligible subordinate entries. For a level-66 THROUGH alias —
    /// §13.18.45.4 GR2, "data-name-1 defines an alphanumeric group item that includes all elementary items
    /// starting with data-name-2 … and concluding with data-name-3" — they are those ELEMENTARY items
    /// (<see cref="RenamesInfo.IncludedElementaryItems"/>).
    /// <para>⚠ DETERMINATION (docs/CONFORMANCE.md §7, kb/Work PB966): §14.7.6 rule 1 compares "the same
    /// qualifiers, if any, up to, but not including, D1 and D2", and an alias is in no included item's
    /// qualifier chain — the items are subordinate to the renamed RECORD, not to the alias. Read with GR2's own
    /// wording, the alias includes elementary items and no groups, so each included item stands directly in D1
    /// with NO qualifier between it and D1: it corresponds by data-name alone with an item at the first level of
    /// the other operand, and a name included twice falls to rule 6. Rejected: reading the record's intermediate
    /// groups inside the window as qualifiers — GR2 does not include them in the alias. Rules 4 and 5 still apply,
    /// to the included item and to every group between it and the window's enclosing group (the lowest group
    /// containing both endpoints): an item under an OCCURS or a REDEFINES entry inside the window is excluded
    /// exactly as it would be under a group subordinate to D1.</para></summary>
    private static List<DataItem> CorrMembers(DataItem group)
    {
        if (group.Renames is not { IsAlias: false } ren)
            return group.Children.Where(CorrEligible).ToList();
        DataItem? enclosing = EnclosingGroup(ren);
        var members = new List<DataItem>();
        foreach (var e in ren.IncludedElementaryItems)
        {
            if (!CorrEligible(e)) continue;
            bool excluded = false;
            for (DataItem? a = e.Parent; a is not null && !ReferenceEquals(a, enclosing); a = a.Parent)
                if (a.IsTable || a.RedefinesTargetName is not null) { excluded = true; break; }   // rule 5
            if (!excluded) members.Add(e);
        }
        return members;
    }

    /// <summary>⛔ THE RULE-6 UNIVERSE (§14.7.6 rule 6, kb/Work PB1111): how many data items answer to each name
    /// AT THIS QUALIFICATION LEVEL of one CORRESPONDING operand — every subordinate item named so, ELIGIBLE OR NOT
    /// (an OCCURS or REDEFINES entry, a level-66 alias), because "DU OF G1" identifies each of them. A nameless
    /// group (FILLER) contributes no qualifier, so its subordinates stand at THIS level too and are counted; a
    /// named group is a qualifier of its own subordinates, which therefore stand one level down. A THROUGH alias
    /// counts its included elementary items, the same universe <see cref="CorrMembers"/> filters. ONE reader for both
    /// operands, so the two sides of the rule answer alike.</summary>
    private static Dictionary<string, int> NamesakeCounts(DataItem group)
    {
        var counts = new Dictionary<string, int>(CobolNames.Comparer);
        void Count(IEnumerable<DataItem> items)
        {
            foreach (var item in items)
            {
                if (!(item.IsGroup || item.IsElementary || item.Renames is not null)) continue;   // not a data item
                if (item.CobolName is { } name) counts[name] = counts.GetValueOrDefault(name) + 1;
                else Count(item.Children);   // FILLER: no qualifier — its subordinates stand at this level
            }
        }
        Count(group.Renames is { IsAlias: false } ren ? ren.IncludedElementaryItems : group.Children);
        return counts;
    }

    /// <summary>The lowest group containing both of a THROUGH alias's endpoints — the groups strictly below it
    /// are the ones the window spans, so they are the ones rule 5 is asked of (see <see cref="CorrMembers"/>).</summary>
    private static DataItem? EnclosingGroup(RenamesInfo ren)
    {
        if (ren.From is not { } from || ren.Thru is not { } thru) return null;
        var ancestors = new HashSet<DataItem>(ReferenceEqualityComparer.Instance);
        for (DataItem? a = from.Parent; a is not null; a = a.Parent) ancestors.Add(a);
        for (DataItem? a = thru.Parent; a is not null; a = a.Parent)
            if (ancestors.Contains(a)) return a;
        return null;
    }

    private static bool CorrEligible(DataItem item) =>
        item.CobolName is not null
        && (item.IsGroup || item.IsElementary)
        && !item.IsTable   // rule 4 — ANY OCCURS: fixed OR Format-4 DYNAMIC (Occurs is null for a dynamic table, D9)
        && item.RedefinesTargetName is null
        && item.Renames is null
        && !ItemCategory.IsIndexMessageTagObjectOrPointer(item);

    /// <summary>
    /// ⛔ ISO §14.7.6 rule 2's move-validity filter, WHICH IS NOT A RULE WRITTEN HERE — it is a DELEGATION, and
    /// must stay one. Rule 2 reads <i>"In a MOVE statement, at least one of the data items is an elementary data
    /// item and the resulting move is valid according to the rules for the MOVE statement"</i>, so the question
    /// belongs to <see cref="MoveTable16"/> — the ONE home of §14.9.25.3 SR10, Table 16 — and asking it a second
    /// time here is how the two answers drift. The "at least one elementary" half is structural: the caller
    /// descends every group × group namesake pair (NOTE 5 under §14.9.25.4 GR11) and only reaches this filter
    /// when one side is elementary.
    /// <para>⛔ THEY HAD DRIFTED, IN BOTH DIRECTIONS AT ONCE, WHICH IS WHY THIS IS ONE LINE (kb/Work PB391).
    /// The private copy that stood here had no alphabetic row or column, no national and no boolean row (its
    /// sender switch ended <c>_ =&gt; false</c>), and a receiver axis that was ONE boolean folding National and
    /// Boolean receivers into Table 16's "Numeric, Numeric-edited" column. Five cells were measured wrong
    /// against the SAME compiler's direct-MOVE answer: <c>9(5)</c>→<c>A(5)</c>, <c>A(5)</c>→<c>9(5)</c> and
    /// <c>9(4)</c>→<c>1(4) BIT</c> were paired where Table 16 says No, and <c>N(3)</c>→<c>N(3)</c> and
    /// <c>1(4) BIT</c>→<c>1(4) BIT</c> were silently dropped where it says Yes.</para>
    /// <para>⚠ A REFUSAL IS A SILENT NON-SELECTION, NEVER A DIAGNOSTIC. §14.7.6 defines which pairs CORRESPOND;
    /// a pair whose implied move would be invalid simply is not one, so the receiving item keeps its prior
    /// content and the direct-MOVE COBOLNET0819 must not fire from here. That is why this reads
    /// <see cref="MoveTable16.Refusal"/>'s null-ness and discards its message.</para>
    /// <para>⚠ THE ≥2002 DE-EDITING GATE THE PRIVATE COPY CARRIED IS GONE, DELIBERATELY. It admitted a
    /// numeric-edited sender into a numeric receiver only at <c>--std</c> 2002 and above, with no citation and
    /// no row in <c>docs/VERSION_CHANGE_REFERENCE.md</c>, while the direct MOVE through this same table admits
    /// it at every edition. Two answers to one question is the defect; if the edition axis is real it belongs in
    /// <see cref="MoveTable16"/>, where BOTH askers would get it, behind a sourced VCR row.</para>
    /// <para>⛔ AND IT ASKS THE WHOLE QUESTION, NOT ONLY TABLE 16 (kb/Work PB391, second half). Rule 2 says
    /// "the rules for the MOVE statement", and §14.9.25.3 SR10 — the rule that routes to Table 16 — governs only
    /// <i>"all other cases not described in Syntax rules 8 and 9"</i>. Asking <see cref="MoveTable16.Refusal"/>
    /// alone therefore skipped SR8 (a <c>BINARY-CHAR</c>/<c>-SHORT</c>/<c>-LONG</c>/<c>-DOUBLE</c> sender needs a
    /// numeric or numeric-edited receiver) and SR9 (a variable-length group operand needs a compatible group on
    /// the other side), both MEASURED wrong: <c>MOVE CORRESPONDING</c> over a <c>BINARY-LONG</c> K and a
    /// <c>PIC X(5)</c> K paired them and overwrote the receiver, while the written <c>MOVE K OF G1 TO K OF G2</c>
    /// was refused COBOLNET0819 by the same compiler; and a variable-length-group namesake paired with an
    /// elementary one reached the run time as a <c>NotImplementedCobolFeatureException</c>. Both are asked now
    /// through the ONE composite entry <see cref="MoveTable16.DataItemRefusal"/> — a single call, so the next
    /// MOVE syntax rule lands here without an edit.</para>
    /// </summary>
    private static bool CorrRule2MoveValid(DataItem src, DataItem dst) =>
        MoveTable16.DataItemRefusal(src, dst) is null;

    /// <summary>
    /// The per-statement child-place factory over ONE resolved group operand: §14.7.6 requires all item
    /// identification (including the group's subscripts) at statement START, so the group is anchored exactly once
    /// — a member-path group by a <c>ref var</c> local (added lazily on first use, so no unused local is emitted),
    /// a Tier-B REDEFINES view group by a <c>long</c> local pinning its window offset. The relative chain from the
    /// group to a pair item never crosses an OCCURS level (rules 4/5 exclude them), so a member child is a plain
    /// dotted path off the anchor; a view child is a sibling window at the class-offset delta (every descendant of
    /// a view shares the class and carries its own <see cref="DataItem.ClassOffset"/>).
    /// </summary>
    private sealed class CorrAccess
    {
        private readonly List<CorrespondingHoist> _hoists;
        private readonly string _local;
        private readonly Place? _group;        // the member group's anchor Place (the `ref var` hoist target)
        private readonly string _offsetInit;   // the view group's window-offset expression (the `long` hoist init)
        private readonly bool _isMember;       // MemberPlace group vs Tier-B RedefViewPlace group
        private readonly bool _subscripted;    // the member path evaluates a subscript (a table access)
        private readonly AccessPath? _backing; // the view group's backing path
        private readonly DataItem _groupItem;
        private readonly ReferenceResolver _refs;
        private bool _hoisted;
        private bool _isAlias;                 // a level-66 THROUGH alias: each member resolves on its own

        private CorrAccess(List<CorrespondingHoist> hoists, string local, Place? group, string offsetInit,
            bool isMember, bool subscripted, AccessPath? backing, DataItem groupItem, ReferenceResolver refs)
        {
            _hoists = hoists; _local = local; _group = group; _offsetInit = offsetInit; _isMember = isMember;
            _subscripted = subscripted; _backing = backing; _groupItem = groupItem; _refs = refs;
        }

        /// <summary>Create the access over a resolved group place, or <see langword="null"/> for a storage shape
        /// no CORRESPONDING child can be built from (the caller fails loud — a genuine DEFERRAL: this factory is
        /// about storage forms a child access can be built over). A <see cref="RefModPlace"/> operand never
        /// arrives, because §14.9.25.3 SR12's second half REFUSES it at bind in the syntax-rule catalog (kb/Work
        /// PB390) — not, as the claim here used to read, because a group "cannot" be reference-modified. It can,
        /// and this switch's <see cref="Place.Undecorated"/> would have let it straight through.
        /// <para>⛔ THE SWITCH IS OVER THE STORAGE FORM, SO IT ASKS <see cref="Place.Undecorated"/> (kb/Work
        /// PB393). An occurs-depending group operand resolves to an <c>OdoGroupPlace</c> WRAPPING the member
        /// place, and a decoration answers a question about the group's whole-group IMAGE EXTENT — a question
        /// CORRESPONDING never asks, because §14.7.6 rule 4 excludes every OCCURS item from correspondence, so
        /// the pairs are exactly the fixed members at their fixed offsets. Switching on the decorated place sent
        /// the operand to the default arm, and `SUBTRACT CORRESPONDING` over two occurs-depending groups —
        /// §14.9.44.3 SR6 admits the kind by name — aborted the run unit.</para></summary>
        public static CorrAccess? Create(Place group, string local, List<CorrespondingHoist> hoists, ReferenceResolver refs)
            => group.Undecorated switch
            {
                MemberPlace m => new CorrAccess(hoists, local, group: m, offsetInit: "", isMember: true,
                    subscripted: m.Path.HasIndex, backing: null, m.Item, refs),
                RedefViewPlace v => new CorrAccess(hoists, local, group: null, offsetInit: v.OffsetExpr, isMember: false,
                    subscripted: false, v.Backing, v.Item, refs),
                // A level-66 THROUGH alias (kb/Work PB966): its members are the elementary items §13.18.45.4 GR2
                // says it includes, none subject to an OCCURS clause (§13.18.45.3 SR3 keeps both endpoints out of
                // one, and CorrMembers excludes an item under an OCCURS inside the window), so each is identified
                // by its own unsubscripted reference — there is nothing to anchor at statement start.
                RenamesPlace n => new CorrAccess(hoists, local, group: null, offsetInit: "", isMember: false,
                    subscripted: false, backing: null, n.Item, refs) { _isAlias = true },
                _ => null,
            };

        /// <summary>The pair item's <see cref="Place"/> for the relative <paramref name="chain"/> (group-exclusive,
        /// pair item last), or <see langword="null"/> for an unreachable shape (the caller fails loud).</summary>
        public Place? ChildPlace(IReadOnlyList<DataItem> chain)
        {
            DataItem leaf = chain[^1];
            // An alias member is ELEMENTARY (GR2 includes no groups), so its chain is the member alone.
            if (_isAlias) return chain.Count == 1 ? _refs.ResolveItem(leaf) : null;
            if (_isMember)
            {
                if (chain.All(CorrPlainMember))
                {
                    // The child is a dotted path off the anchor local: RootFieldSegment(anchor) + one MemberSegment
                    // per chain step (byte-identical to the former "{local}.{c1}.{c2}" string).
                    var segs = new List<AccessSegment> { new RootFieldSegment(Hoist(isRef: true)) };
                    segs.AddRange(chain.Select(c => new MemberSegment(c.CsName)));
                    return new MemberPlace(new AccessPath(segs), leaf);
                }
                // A chain member inside a non-alias REDEFINES class stores in the class BACKING, not as a struct
                // member — reachable absolutely (from the root) only when the group reference has no subscript.
                return _subscripted ? null : _refs.ResolveItem(leaf);
            }
            // A Tier-B view group: the child window sits at the class-offset delta from the group's hoisted
            // offset; the group's subscript displacement (already inside the hoisted value) applies to both
            // identically (ISO §13.18.44 — a redefined table lays its occurrences end-to-end in the one backing).
            if (!ReferenceEquals(leaf.Class, _groupItem.Class)) return null;
            // ⛔ THROUGH THE ONE WINDOW BUILDER (kb/Work PB203): `hoist - groupItem.ClassOffset` is precisely the
            // group's RUNTIME displacement (its subscript/BASED part, with the static in-class offset removed),
            // which is what a BIT member's window needs — its own position is carried in BITS by ClassBitOffset.
            string displacement = $"{Hoist(isRef: false)} - {_groupItem.ClassOffset}";
            return RedefViewPlace.For(_backing!, leaf,
                $"{displacement} + {leaf.ClassOffset}", displacement,
                cell: ReferenceResolver.BuildCellPath(_groupItem.Class));
        }

        /// <summary>Record the anchor hoist on first use and return its local name.</summary>
        private string Hoist(bool isRef)
        {
            if (!_hoisted)
            {
                _hoists.Add(isRef
                    ? new CorrespondingHoist(_local, RefGroup: _group, LongInit: null)
                    : new CorrespondingHoist(_local, RefGroup: null, LongInit: $"(long)({_offsetInit})"));
                _hoisted = true;
            }
            return _local;
        }

        /// <summary>True when a chain member is an ordinary struct member: no REDEFINES class, or the stored
        /// canonical of a Tier-A alias class (the alias tier keeps the canonical's typed field).</summary>
        private static bool CorrPlainMember(DataItem c) =>
            c.Class is null || (c.Class.Tier is RedefinesTier.Alias && c.IsCanonical);
    }
}
