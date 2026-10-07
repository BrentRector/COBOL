// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

using CobolNet.Binding.Model;
using CobolNet.Binding.Procedure;
using CobolNet.Common;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding.Validation;

using Core = CobolParserCore;
using CobolNet.Runtime;

/// <summary>
/// ⛔ THE FORMAT-2 <c>SEARCH ALL</c> OPERAND MODEL and the seven syntax rules written over it (ISO §14.9.37.3
/// SR7–SR13; kb/Work PB445). The rules are not seven independent screens: every one of them reads one of exactly
/// TWO things — the OCCURS KEY phrase of identifier-1 <b>in its own order</b>, and the WHEN phrase decomposed into
/// its Format-2 operands — and because neither existed as a model, all seven were unenforced together. The model
/// is built once here and each rule is a predicate over it, so the next Format-2 rule is a predicate rather than
/// another tree walk.
/// <list type="bullet">
///   <item>The KEY half is <see cref="OccursSpec.Keys"/> (phrase order — §13.18.38.4 GR3 makes that the order of
///   significance) resolved through <see cref="OdoModel.KeyItems"/>.</item>
///   <item>The WHEN half is the <c>condition</c> parse tree decomposed into conjuncts, each carrying a KEY side
///   and a SENDING side.</item>
/// </list>
/// <para>⛔ IT IS A COMPILE-TIME MODEL AND IT IS NOT PUT ON THE BOUND NODE. Nothing at run time would consume it:
/// §14.9.37.4 GR9 leaves the search TECHNIQUE to the implementor ("A non serial type of search operation may take
/// place"), and this implementation's scan is serial, so a field on <c>BoundSearch</c> would be exactly the
/// write-only lookup kb/Work PB445 is about (feedback_a_dead_lookup_is_also_unverified).</para>
/// <para>⛔ AND IT IS SCREENED AT BIND, NOT IN THE GRAMMAR. The <c>searchAllWhenClause</c> rule carries the
/// general <c>condition</c>, which is a SUPERSET of the closed operand shape Format 2 prints — the repo's
/// parse-wide / bind-narrow doctrine (stated in <c>CobolExpressions.g4</c> in those words). Screening here is what
/// lets the diagnostic quote the rule; an ANTLR rule tightened to the printed format could only report an
/// unexpected token. A WHEN whose shape is not Format 2 at all is reported through the EXISTING
/// <see cref="StatementValidation.RejectStatementOperand"/> (COBOLNET1757 — "an operand a statement's own syntax
/// rules or general format do not admit"), never a fourth code; a PARENTHESIZED condition is unwrapped silently,
/// so the SR screens still apply to what is inside it.</para>
/// <para>⛔ SR8 AND SR9 ARE RULES ABOUT THE SUBSCRIPT AS WRITTEN — "shall be subscripted by the first index-name
/// associated with identifier-1 … The index-name subscript shall not be followed by a '+' or a '–'" — which the
/// rendered C# index expression has already erased (an index-name and an integer item of the same value render
/// identically, and <c>IX + 1</c> folds into the arithmetic). They are therefore asked of the raw subscript token
/// segments, <see cref="ReferenceResolver.SubscriptSegments"/>.</para>
/// <para>No edition gate: Format 2 is a COBOL-85 construct and SR7–SR13 are unchanged text at 85, 2002, 2014 and
/// 2023, so all four editions get the same verdict (the PB445 negative goldens carry all four in their
/// <c>reject-at</c> headers).</para>
/// </summary>
internal readonly struct SearchAllFormat2Rules(DataBinder data, ReferenceResolver refs, StatementValidation validation,
                                              ConditionBinder conditions)
{
    /// <summary>Screen one <c>SEARCH ALL</c> against ISO §14.9.37.3 SR7–SR13. Pure in the check-catalog sense:
    /// every verdict is reported to the ONE sink and the caller's bind is unaffected (a Format-2 violation is the
    /// SOURCE's error and §4.2.2 ¶2 puts its indication at compile time — the statement still binds, so one
    /// compile reports every violation it can see rather than only the first).</summary>
    /// <param name="s">The statement.</param>
    /// <param name="table">identifier-1's resolved table item — the caller has already established that it is a
    /// table with an INDEXED phrase (§14.9.37.3 SR2), which is what makes <c>IndexNames[0]</c> answerable.</param>
    /// <returns>true when the statement satisfies every rule this screens.</returns>
    public bool Check(Core.SearchAllStatementContext s, DataItem table)
    {
        // SR7 — "The OCCURS clause associated with identifier-1 shall contain the KEY phrase." Reported ALONE:
        // SR8–SR12 are all written about that phrase, so continuing would report every WHEN operand as "not a
        // key" for a table that declares no keys at all.
        var keys = table.OccursSpec?.Keys;
        if (keys is null || keys.Count == 0)
        {
            data.Edition.Error(DiagnosticCatalog.SearchAllTableNoKeyPhrase,
                $"SEARCH ALL '{table.CobolName}': the OCCURS clause associated with identifier-1 shall contain "
                + "the KEY phrase (ISO §14.9.37.3 SR7). Declare ASCENDING/DESCENDING KEY on the OCCURS clause, or "
                + "use the serial SEARCH (Format 1), whose WHEN admits any conditional expression (SR6).");
            return false;
        }

        var keyItems = OdoModel.KeyItems(table);
        string firstIndex = table.IndexNames[0];
        // SR11's accumulator: which KEY PHRASE POSITIONS the WHEN referenced. Format 2's general format prints ONE
        // WHEN phrase (its ellipsis sits on the AND bracket, not on the WHEN brace as Format 1's does), and the
        // grammar spells exactly that (kb/Work PB446), so per-WHEN IS per-statement.
        // A WHEN with no condition context at all is an ERROR NODE: the parse has already failed and reported,
        // and every walk below would dereference it — `searchAllWhenClause : WHEN condition statementBlock` makes
        // the condition mandatory, so this is never a silent skip of a well-formed WHEN.
        if (s.searchAllWhenClause()?.condition() is not { } cond) return true;
        var referenced = new bool[keys.Count];
        var conjuncts = new List<Core.ComparisonExpressionContext>();
        if (!TryConjuncts(cond, conjuncts, out string? why))
            return validation.RejectStatementOperand(
                $"SEARCH ALL '{table.CobolName}' WHEN {cond.GetText()} — the Format-2 WHEN phrase "
                + "is `data-name-1 IS EQUAL TO {identifier-3 | literal-1 | arithmetic-expression-1}` or a "
                + $"bare condition-name, repeated with AND (ISO §14.9.37.2 Format 2); this WHEN specifies "
                + $"{why}. The serial SEARCH (Format 1) is the form whose WHEN takes any conditional "
                + "expression (§14.9.37.3 SR6).");
        bool ok = true;
        foreach (var ce in conjuncts)
            ok &= CheckConjunct(ce, table, keys, keyItems, firstIndex, referenced);
        ok &= CheckPrecedingKeys(table, keys, referenced);
        return ok;
    }

    // ── The WHEN phrase decomposed (the model's second half) ─────────────────────────────────────────────────

    /// <summary>Decompose a WHEN's <c>condition</c> into the Format-2 conjuncts — the <c>WHEN …</c> operand and
    /// every repetition of the <c>AND …</c> phrase. False, with <paramref name="why"/> naming the construct, for
    /// any shape the printed Format 2 does not admit: OR, XOR, NOT, an abbreviated combined relation (§8.8.4.12 —
    /// Format 2 writes data-name-2 out in full in every AND phrase), or a boolean condition. A PARENTHESIZED
    /// condition is unwrapped silently: whether Format 2 admits parentheses at all is a question about its general
    /// format, and unwrapping keeps the SR7–SR13 screens applying to what is inside.</summary>
    private static bool TryConjuncts(Core.ConditionContext cond, List<Core.ComparisonExpressionContext> into,
                                     out string? why)
    {
        why = null;
        // Each leading tier's first element is tested for null, so an ERROR NODE (a tier with no children at all,
        // which only a failed parse produces) leaves the walk rather than throwing out of the binder. Every
        // connective's operand is a SUCCEEDING tier (kb/Work PB1390), so its presence is what names the connective.
        if (cond.logicalOrExpression() is not { } or || or.logicalXorExpression() is not { } xor) return true;
        if (or.succeedingXorExpression().Length > 0) { why = "an OR"; return false; }
        if (xor.logicalAndExpression() is not { } and) return true;
        if (xor.succeedingAndExpression().Length > 0) { why = "an XOR"; return false; }
        if (and.succeedingCondition().Any(s => s.abbreviatedRelation() is not null))
        {
            why = "an abbreviated combined relation (Format 2's AND phrase writes data-name-2 out in full)";
            return false;
        }
        // The leading condition, then every AND operand — the succeeding operands are ordinary conditions here.
        var operands = new[] { and.unaryLogicalExpression() }
            .Concat(and.succeedingCondition().Select(s => s.unaryLogicalExpression()));
        foreach (var u in operands)
        {
            if (u is null) { why = "a condition Format 2 does not print"; return false; }
            if (u.NOT() is not null) { why = "a NOT"; return false; }
            if (u.primaryCondition() is not { } p) { why = "a condition Format 2 does not print"; return false; }
            if (!TryPrimary(p, into, out why)) return false;
        }
        return true;
    }

    private static bool TryPrimary(Core.PrimaryConditionContext p, List<Core.ComparisonExpressionContext> into,
                                   out string? why)
    {
        why = null;
        // GROUPING-PAREN-ONLY: this is the `LPAREN condition RPAREN` ALTERNATIVE of the primaryCondition parse
        // rule, tested through its own context accessor — not a token-type match over a stream. A function
        // argument list's '(' (FNARG_LPAREN, ISO §8.4.3.2.3 SR6) belongs to the separate functionCall rule and
        // can never be this node's paren, so there is no twin to consider here.
        if (p.LPAREN() is not null && p.condition() is { } inner) return TryConjuncts(inner, into, out why);
        if (p.booleanExpression().Length > 0 || p.booleanLiteral() is not null)
        { why = "a boolean condition"; return false; }
        if (p.comparisonExpression() is not { } ce) { why = "a condition Format 2 does not print"; return false; }
        into.Add(ce);
        return true;
    }

    // ── The seven rules, as predicates over the model ────────────────────────────────────────────────────────

    /// <summary>One conjunct — the <c>WHEN</c> operand or one <c>AND</c> repetition — against SR8/SR9 (the key
    /// side), SR10/SR13 (the sending side) and SR12 (both), marking SR11's accumulator on the way. The conjunct's
    /// SHAPE is screened first: Format 2 prints exactly two alternatives, a comparison for EQUALITY and a bare
    /// condition-name.</summary>
    private bool CheckConjunct(Core.ComparisonExpressionContext ce, DataItem table,
                               IReadOnlyList<OccursKey> keys, IReadOnlyList<DataItem?> keyItems,
                               string firstIndex, bool[] referenced)
    {
        if (ShapeViolation(ce) is { } shape)
            return validation.RejectStatementOperand(
                $"SEARCH ALL '{table.CobolName}' WHEN {ce.GetText()} — the Format-2 WHEN phrase is "
                + "`data-name-1 IS EQUAL TO {identifier-3 | literal-1 | arithmetic-expression-1}` or a bare "
                + $"condition-name (ISO §14.9.37.2 Format 2); this operand specifies {shape}. The serial SEARCH "
                + "(Format 1) is the form whose WHEN takes any conditional expression (§14.9.37.3 SR6).");

        var ops = ce.comparisonOperand();
        if (ops.Length == 0) return true;   // an error node — every comparisonExpression alternative has one
        if (ops.Length < 2) return CheckConditionNameSide(ops[0], table, keys, keyItems, firstIndex, referenced);
        bool ok = CheckKeySide(ops[0], table, keys, keyItems, firstIndex, referenced);
        return CheckSendingSide(ops[1], table, keyItems, firstIndex) && ok;
    }

    /// <summary>The construct this conjunct specifies that Format 2 does not print, or null when the shape is one
    /// of the two the format admits. The relational-operator test is EQUALITY only: the format writes
    /// <c>IS EQUAL TO</c> / <c>IS =</c> and nothing else, so an ordering operator is a GENERAL-FORMAT violation
    /// rather than a syntax-rule one.</summary>
    private static string? ShapeViolation(Core.ComparisonExpressionContext ce)
    {
        if (ce.OMITTED() is not null) return "an omitted-argument condition";
        if (ce.className() is not null) return "a class condition";
        if (ce.POSITIVE() is not null || ce.NEGATIVE() is not null || ce.ZERO() is not null)
            return "a sign condition";
        if (ce.comparisonOperand().Length < 2) return null;         // the bare condition-name arm
        var op = ce.comparisonOperator();
        return IsEqualTo(op)
            ? null
            : $"the relational operator '{op?.GetText() ?? "?"}' where Format 2 prints IS EQUAL TO";
    }

    /// <summary>The "equal to" relational operator of ISO §8.8.4.2.2, in any of its spellings — the ONE operator
    /// Format 2's general format prints (<c>IS EQUAL TO</c> and <c>IS =</c>). Every other cell of the operator
    /// table, and every negated form, is excluded BY NAME rather than by absence, so a new operator alternative in
    /// the grammar cannot silently become "equality" here.</summary>
    private static bool IsEqualTo(Core.ComparisonOperatorContext? op) =>
        op is not null && op.NOT() is null
        && op.GREATER() is null && op.LESS() is null && op.NOTEQUAL() is null
        && op.LT() is null && op.GT() is null && op.LTEQUAL() is null && op.GTEQUAL() is null
        && (op.EQUALS() is not null || op.EQUAL() is not null);

    /// <summary>SR8 (with SR12's data-name arm) — data-name-1 / data-name-2: referenced in the KEY phrase, and
    /// subscripted by the first index-name of identifier-1 with no '+' or '–' after it.</summary>
    private bool CheckKeySide(Core.ComparisonOperandContext operand, DataItem table,
                              IReadOnlyList<OccursKey> keys, IReadOnlyList<DataItem?> keyItems,
                              string firstIndex, bool[] referenced)
    {
        if (SoleDataReference(operand) is not { } dref)
            return Key(table, $"the WHEN phrase's receiving operand '{operand.GetText()}' is not a data-name; "
                + "data-name-1 and all repetitions of data-name-2 \"shall be … referenced in the KEY phrase in "
                + "the OCCURS clause associated with identifier-1\" (ISO §14.9.37.3 SR8)");

        var item = refs.Probe(dref)?.Item;
        int pos = KeyPositionOf(item, keyItems);
        // The next test asks EXISTENCE in the condition-name namespace — "is this spelling declared as a
        // condition-name at all", which chooses between two MESSAGES — and the base word is the right key for
        // that: a namespace is keyed by the name, and §8.4.2.2 qualification decides only WHICH declaration.
        // (The SELECTION, where the qualifier is load-bearing, is CheckConditionNameSide's, through the one
        // §8.4.2.2 Format-2 resolution — kb/Work PB443.)
        // A CONDITION-NAME written on the receiving side of a comparison is a FORMAT verdict, not SR8's: Format 2
        // prints condition-name-1 as an alternative to the whole comparison, so `WHEN cond-name (IX) = 05` is a
        // shape the format does not print. Without this arm it drew SR8's "not referenced in the KEY phrase",
        // which sends a reader looking for a KEY phrase edit that would not help.
        if (item is null
            && data.Symbols.TryResolveCondition(dref.cobolWord()?.GetText() ?? "", data.ActiveScope, out var cn)
            && cn.Count > 0)
            return validation.RejectStatementOperand(
                $"SEARCH ALL '{table.CobolName}' WHEN {DataBinder.WrittenText(dref)} … — '{dref.cobolWord()?.GetText()}' is a "
                + "condition-name, and Format 2 prints condition-name-1 as an ALTERNATIVE to the whole "
                + "`data-name-1 IS EQUAL TO …` comparison, never as its receiving operand (ISO §14.9.37.2 "
                + $"Format 2). Write `WHEN {DataBinder.WrittenText(dref)}` alone, or compare the key data-name itself.");
        if (pos < 0)
            return Key(table, $"'{DataBinder.WrittenText(dref)}' is not referenced in the KEY phrase of the OCCURS clause "
                + $"associated with '{table.CobolName}' — data-name-1 and all repetitions of data-name-2 \"shall "
                + $"be referenced in the KEY phrase\" (ISO §14.9.37.3 SR8). The KEY phrase declares {KeyList(keys)}.");
        referenced[pos] = true;

        // §14.9.37.2 Format 2 prints data-name-1 / data-name-2, and §8.4.3.3.3's NOTE says "where data-name-n is used
        // in a general format or syntax rule, then reference-modification is not permitted". The subscript is the
        // one suffix this operand MUST carry (SR8), so the shared data-name-n screen's reference-modifier arm is
        // asked on its own: `WHEN TK(IX)(1:2) = "AB"` otherwise resolved to the base item TK and ran a search on
        // the whole key (kb/Work PB1457). It does not disturb the SR11 accounting above (referenced[pos]).
        bool ok = DataBinder.ScreenReferenceModifier(dref, $"SEARCH ALL '{table.CobolName}' WHEN key operand",
                                                     data.Edition);
        ok &= CheckKeySubscript(dref, item!, table, firstIndex, "SR8");
        // SR12 — "Data-name-1, data-name-2, identifier-3, or identifier-4 shall not specify a variable-length
        // group." The predicate is the ONE VariableLengthCompatibility module (§8.5.1.12), never a second walk.
        if (VariableLengthCompatibility.IsVariableLength(item!))
            ok &= Key(table, $"'{DataBinder.WrittenText(dref)}' specifies a variable-length group; data-name-1 and data-name-2 "
                + "\"shall not specify a variable-length group\" (ISO §14.9.37.3 SR12)");
        return ok;
    }

    /// <summary>SR9 — the bare condition-name arm: single-valued, its associated data-name in the KEY phrase, and
    /// the same subscript requirement SR8 states for a data-name.</summary>
    private bool CheckConditionNameSide(Core.ComparisonOperandContext operand, DataItem table,
                                        IReadOnlyList<OccursKey> keys, IReadOnlyList<DataItem?> keyItems,
                                        string firstIndex, bool[] referenced)
    {
        if (SoleDataReference(operand) is not { } dref)
            return validation.RejectStatementOperand(
                $"SEARCH ALL '{table.CobolName}' WHEN {operand.GetText()} — a WHEN phrase written without a "
                + "relational operator is Format 2's condition-name-1 alternative, and this operand is not a "
                + "condition-name (ISO §14.9.37.2 Format 2)");

        string name = dref.cobolWord()?.GetText() ?? dref.GetText();
        if (!data.Symbols.TryResolveCondition(name, data.ActiveScope, out var conds) || conds.Count == 0)
            return validation.RejectStatementOperand(
                $"SEARCH ALL '{table.CobolName}' WHEN {operand.GetText()} — '{name}' is not a condition-name, and "
                + "a WHEN phrase written without a relational operator is Format 2's condition-name-1 alternative "
                + "(ISO §14.9.37.2 Format 2). Write `data-name-1 IS EQUAL TO …` to compare a key.");

        // ⛔ WHICH level-88 THIS REFERENCE NAMES IS §8.4.2.2 FORMAT 2's QUESTION, AND IT IS ANSWERED IN ONE PLACE
        // (kb/Work PB443). This used to pick `conds.FirstOrDefault(c => IsWithin(c.Parent, table)) ?? conds[0]` —
        // a base-word lookup with a tie broken by DECLARATION ORDER, the condition-name twin of the identifier-1
        // defect — so the qualifier was read by the condition BINDER and ignored by this SCREEN, and the two
        // disagreed about which key was referenced. Measured: with `ASCENDING KEY IS K2 K1` and a condition-name
        // spelled the same on each, the legal `WHEN CN OF K2 (IX)` was REJECTED under SR11 for "referencing K1
        // and not K2" (it referenced neither — it referenced K2), while with `ASCENDING KEY IS K1 K2` the SR11
        // violation in `WHEN CN OF K2 (IX)` went unreported. One reference, one resolution.
        if (conditions.ConditionOf(dref) is not { } cond)
            return Key(table, ReferenceResolver.MisqualifiedConditionText(DataBinder.WrittenText(dref), name,
                ConditionBinder.QualifiersOf(dref)));
        bool ok = true;

        // SR9 first clause — "All referenced condition-names shall be defined as having only a single value."
        // A THRU range is one Values entry carrying a High; several VALUE operands are several entries.
        if (cond.Values.Count != 1 || cond.Values[0].High is not null)
            ok &= Key(table, $"the condition-name '{name}' is defined with "
                + (cond.Values.Count != 1 ? $"{cond.Values.Count} values" : "a THRU range")
                + "; all referenced condition-names \"shall be defined as having only a single value\" "
                + "(ISO §14.9.37.3 SR9)");

        // SR9 third clause — "The data-name associated with each condition-name shall be specified in the KEY
        // phrase in the OCCURS clause associated with identifier-1."
        int pos = KeyPositionOf(cond.Parent, keyItems);
        if (pos < 0)
        {
            Key(table, $"the condition-name '{name}' is defined on '{cond.Parent.CobolName}', which is not "
                + $"specified in the KEY phrase of the OCCURS clause associated with '{table.CobolName}' — \"the "
                + "data-name associated with each condition-name shall be specified in the KEY phrase\" "
                + $"(ISO §14.9.37.3 SR9). The KEY phrase declares {KeyList(keys)}.");
            return false;
        }
        referenced[pos] = true;

        return CheckKeySubscript(dref, cond.Parent, table, firstIndex, "SR9") && ok;
    }

    /// <summary>SR10 (with SR12's identifier arm and SR13) — identifier-3 / identifier-4, the identifiers inside
    /// arithmetic-expression-1 / -2, and literal-1 / literal-2.</summary>
    private bool CheckSendingSide(Core.ComparisonOperandContext operand, DataItem table,
                                  IReadOnlyList<DataItem?> keyItems, string firstIndex)
    {
        // literal-1 / literal-2 — SR13 is the only rule about them, and only the non-numeric formats have a
        // zero-length spelling (ISO §3.178: "alphanumeric, boolean, or national literal that contains zero
        // characters"). A concatenation expression is zero-length exactly when its folded value is (§8.8.3.3 GR2),
        // asked of the diagnostic-free fold so ConcatFolder's own reporting does not run a second time here.
        if (operand.valueOperand()?.nonNumericLiteral() is { } nn)
            return !IsZeroLengthLiteral(nn) || Sending(table,
                $"the sending operand {nn.GetText()} is a zero-length literal; \"neither literal-1 nor literal-2 "
                + "shall be zero-length literals\" (ISO §14.9.37.3 SR13)");

        if (operand.valueOperand()?.arithmeticExpression() is not { } ae) return true;
        if (SoleDataReference(ae) is { } sole)
        {
            // ONE resolution for both rules on this operand — Probe is a full name resolution, not a field read.
            var item = refs.Probe(sole)?.Item;
            bool ok = CheckSendingIdentifier(sole, item, table, keyItems, firstIndex,
                                             "identifier-3 and identifier-4");
            // SR12's identifier arm — only a SOLE identifier is identifier-3/-4; the identifiers INSIDE an
            // arithmetic expression are named by SR10 alone and SR12 does not reach them.
            if (item is { } si && VariableLengthCompatibility.IsVariableLength(si))
                ok &= Sending(table, $"'{sole.GetText()}' specifies a variable-length group; identifier-3 and "
                    + "identifier-4 \"shall not specify a variable-length group\" (ISO §14.9.37.3 SR12)");
            return ok;
        }
        // arithmetic-expression-1 / -2: SR10 names "identifiers specified in" it, so EVERY data reference the
        // expression contains is screened, not only a top-level one.
        var found = new List<Core.DataReferenceContext>();
        CollectDataReferences(ae, found);
        bool all = true;
        foreach (var d in found)
            all &= CheckSendingIdentifier(d, refs.Probe(d)?.Item, table, keyItems, firstIndex,
                                          "identifiers specified in arithmetic-expression-1 and -2");
        return all;
    }

    /// <summary>SR10's two prohibitions on one sending identifier: neither referenced in the KEY phrase, nor
    /// subscripted by the first index-name associated with identifier-1. <paramref name="item"/> is the
    /// reference already resolved by the caller, so one operand costs one resolution.</summary>
    private bool CheckSendingIdentifier(Core.DataReferenceContext dref, DataItem? item, DataItem table,
                                        IReadOnlyList<DataItem?> keyItems, string firstIndex, string role)
    {
        bool ok = true;
        if (KeyPositionOf(item, keyItems) >= 0)
            ok &= Sending(table, $"the sending operand '{DataBinder.WrittenText(dref)}' is referenced in the KEY phrase of the "
                + $"OCCURS clause associated with '{table.CobolName}'; {role} \"shall be neither referenced in "
                + "the KEY phrase of the OCCURS clause associated with identifier-1 nor subscripted by the first "
                + "index-name associated with identifier-1\" (ISO §14.9.37.3 SR10)");
        if (table.Indexes.Count > 0 && refs.SubscriptNamesIndex(dref, table.Indexes[0]))
            ok &= Sending(table, $"the sending operand '{DataBinder.WrittenText(dref)}' is subscripted by '{firstIndex}', the "
                + $"first index-name associated with '{table.CobolName}'; {role} \"shall be neither referenced "
                + "in the KEY phrase of the OCCURS clause associated with identifier-1 nor subscripted by the "
                + "first index-name associated with identifier-1\" (ISO §14.9.37.3 SR10) — the search varies that "
                + "index, so such an operand moves with the probe instead of being compared against it");
        return ok;
    }

    /// <summary>SR11 — "When a data-name in the KEY phrase … is referenced …, all preceding data-names in that
    /// KEY phrase or their associated condition-names shall also be referenced." The KEY phrase's own order IS
    /// its order of significance (§13.18.38.4 GR3), which is why <see cref="OccursSpec.Keys"/> is ONE ordered
    /// list rather than a list per direction word.</summary>
    private bool CheckPrecedingKeys(DataItem table, IReadOnlyList<OccursKey> keys, bool[] referenced)
    {
        int last = -1;
        for (int i = keys.Count - 1; i >= 0; i--) if (referenced[i]) { last = i; break; }
        bool ok = true;
        for (int i = 0; i < last; i++)
            if (!referenced[i])
                ok &= Key(table, $"the WHEN phrase references the key '{keys[last].Written}' but not the more "
                    + $"significant key '{keys[i].Written}' that precedes it in the KEY phrase; when a key is "
                    + "referenced, \"all preceding data-names in that KEY phrase or their associated "
                    + "condition-names shall also be referenced\" (ISO §14.9.37.3 SR11). The KEY phrase declares "
                    + $"{KeyList(keys)}, in descending order of significance (§13.18.38.4 GR3).");
        return ok;
    }

    // ── Shared predicates over the model ─────────────────────────────────────────────────────────────────────

    /// <summary>The position of <paramref name="item"/> in the KEY phrase, or −1. IDENTITY against the resolved
    /// key items, never a name comparison: a key may be written qualified in the WHEN (<c>K OF E (IX)</c>) while
    /// the KEY phrase records only the base name (§13.18.38.3 SR3 confines data-name-2 to the table).</summary>
    private static int KeyPositionOf(DataItem? item, IReadOnlyList<DataItem?> keyItems)
    {
        if (item is null) return -1;
        for (int i = 0; i < keyItems.Count; i++) if (ReferenceEquals(keyItems[i], item)) return i;
        return -1;
    }

    /// <summary>SR8's and SR9's shared subscript requirement, asked of the subscript AS WRITTEN: the position that
    /// selects identifier-1's occurrence shall be the first index-name associated with identifier-1, and "the
    /// index-name subscript shall not be followed by a '+' or a '–'".</summary>
    private bool CheckKeySubscript(Core.DataReferenceContext dref, DataItem item, DataItem table,
                                   string firstIndex, string rule)
    {
        int pos = OdoModel.SubscriptPositionOf(item, table);
        var segs = refs.SubscriptSegments(dref);
        List<IToken> toks = pos >= 0 && segs is not null && pos < segs.Count ? segs[pos] : [];
        if (toks.Count == 0)
            return Key(table, $"'{DataBinder.WrittenText(dref)}' is not subscripted by '{firstIndex}': it \"shall be "
                + "subscripted by the first index-name associated with identifier-1 along with any subscripts "
                + $"required to uniquely identify the data item\" (ISO §14.9.37.3 {rule})");
        // The index-name may be written QUALIFIED (§8.4.2.2.2 Format 3; §8.4.2.2.3 SR6), and must be when another
        // table declares the same spelling (SR1) — so the qualifiers are consumed here and the reference is judged
        // by the DECLARATION it resolves to, never by its spelling alone (kb/Work PB919).
        int next = 1;
        List<string> quals = [];
        while (next + 1 < toks.Count && toks[next].Type is Core.OF or Core.IN
               && ReferenceResolver.IsNameToken(toks[next + 1]))
        {
            quals.Add(toks[next + 1].Text);
            next += 2;
        }
        if (!ReferenceResolver.IsNameToken(toks[0])
            || !CobolNames.Same(toks[0].Text, firstIndex)
            || (table.Indexes.Count > 0
                && refs.ResolveIndexName(toks[0].Text, quals, toks[0]) is { Outcome: ReferenceResolver.IndexRefOutcome.Resolved } ix
                && !ReferenceEquals(ix.Decl, table.Indexes[0])))
            return Key(table, $"'{DataBinder.WrittenText(dref)}' selects identifier-1's occurrence with '{Written(toks)}' where "
                + $"the first index-name associated with '{table.CobolName}' — '{firstIndex}' — is required "
                + $"(ISO §14.9.37.3 {rule})");
        if (toks.Count == next) return true;
        toks = [toks[0], .. toks.Skip(next)];
        return toks[1].Type is Core.PLUS or Core.MINUS
                             or Core.SIGNED_INTEGERLIT or Core.SIGNED_DECIMALLIT
            ? Key(table, $"'{DataBinder.WrittenText(dref)}' writes '{Written(toks)}': \"the index-name subscript shall not be "
                + $"followed by a '+' or a '–'\" (ISO §14.9.37.3 {rule})")
            : Key(table, $"'{DataBinder.WrittenText(dref)}' selects identifier-1's occurrence with '{Written(toks)}' where the "
                + $"first index-name '{firstIndex}' alone is required (ISO §14.9.37.3 {rule})");
    }

    /// <summary>ISO §3.178 asked of a <c>nonNumericLiteral</c>: a figurative constant is never zero-length
    /// (§8.3.3.6.4 GR3 b) — "when a figurative constant is other than ALL literal-1, the length of the string is
    /// one character" — and the ALL form's literal-1 cannot be zero-length either, §8.3.3.6.3 SR2), a
    /// concatenation expression is zero-length exactly when its value is (§8.8.3.3 GR2 — asked of the diagnostic-free
    /// <see cref="ConcatFolder.Peek"/>, so a constant-name operand counts by the literal it stands for, kb/Work
    /// PB1406), and each plain format answers <see cref="CobolLiteral.IsZeroLength"/>.</summary>
    private bool IsZeroLengthLiteral(Core.NonNumericLiteralContext nn)
    {
        if (nn.figurativeConstant() is not null) return false;
        if (nn.concatenationExpression() is { } cat) return ConcatFolder.Peek(cat, data.LiteralEnv).Value.Length == 0;
        return CobolLiteral.IsZeroLength(nn.GetText());
    }

    // ── Parse-tree readers ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>The reference a comparison operand IS, when the operand is exactly one data reference — Format 2's
    /// data-name-1 / data-name-2 / condition-name-1 / identifier-3 positions. Null for a literal, a function
    /// reference, a parenthesized or signed form, or any genuine arithmetic expression.</summary>
    private static Core.DataReferenceContext? SoleDataReference(Core.ComparisonOperandContext op) =>
        op.valueOperand()?.arithmeticExpression() is { } ae ? SoleDataReference(ae) : null;

    private static Core.DataReferenceContext? SoleDataReference(Core.ArithmeticExpressionContext ae)
    {
        if (ae.additiveExpression() is not { } add || add.multiplicativeExpression().Length != 1) return null;
        var mul = add.multiplicativeExpression()[0];
        if (mul.powerExpression().Length != 1) return null;
        var pow = mul.powerExpression()[0];
        if (pow.unaryExpression().Length != 1) return null;
        var un = pow.unaryExpression()[0];
        if (un.addOp() is not null || un.primaryExpression() is not { } prim) return null;
        // GROUPING-PAREN-ONLY: the `LPAREN arithmeticExpression RPAREN` ALTERNATIVE of primaryExpression, read
        // through its own context accessor. `FUNCTION name (…)` is the sibling functionCall alternative and
        // carries the FNARG twin; a function reference is not a sole data reference either way, so both
        // alternatives answer null here and there is no twin arm to miss.
        return prim.LPAREN() is not null ? null : prim.dataReference();
    }

    /// <summary>Every <c>dataReference</c> in the subtree — SR10's "identifiers specified in
    /// arithmetic-expression-1". A reference nested in a FUNCTION argument list is not reached: such a list is
    /// captured in SUBSCRIPT lexer mode as a flat token group, so it carries no <c>dataReference</c> node at all
    /// (the same boundary every other bind-time identifier walk has).</summary>
    private static void CollectDataReferences(IParseTree node, List<Core.DataReferenceContext> into)
    {
        if (node is Core.DataReferenceContext d) { into.Add(d); return; }
        for (int i = 0; i < node.ChildCount; i++) CollectDataReferences(node.GetChild(i), into);
    }

    private static string Written(List<IToken> toks) => string.Concat(toks.Select(t => t.Text));

    private static string KeyList(IReadOnlyList<OccursKey> keys) =>
        string.Join(", ", keys.Select(k => $"{(k.Descending ? "DESCENDING" : "ASCENDING")} {k.Written}"));

    // ── The two sinks (each returns false, so a call site reads as a verdict) ────────────────────────────────

    private bool Key(DataItem table, string message)
    {
        data.Edition.Error(DiagnosticCatalog.SearchAllWhenKeyOperand, $"SEARCH ALL '{table.CobolName}': {message}");
        return false;
    }

    private bool Sending(DataItem table, string message)
    {
        data.Edition.Error(DiagnosticCatalog.SearchAllWhenSendingOperand,
            $"SEARCH ALL '{table.CobolName}': {message}");
        return false;
    }
}
