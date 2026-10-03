// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;

namespace CobolNet.Binding;

/// <summary>Which rule of the §8.8.4.2 comparability table a general relation's operand pair breaks.</summary>
internal enum RelationComparabilityBreach
{
    /// <summary>ISO §8.8.4.2.1 item 6 with §8.8.4.2.5: a numeric operand compared with an operand of class
    /// alphanumeric or national is defined only when "The numeric integer operand shall be an integer literal or an
    /// integer numeric data item of usage display or national".</summary>
    NumericAgainstCharacter,

    /// <summary>ISO §8.8.4.2.13: "Relation tests may be made only between" two index-names, an index-name and a
    /// numeric data item or numeric literal, or an index data item and an index-name or another index data
    /// item.</summary>
    IndexPair,
}

/// <summary>⛔ THE ONE §8.8.4.2.1 COMPARABILITY TABLE for the GENERAL relation (kb/Work PB1468). ISO §8.8.4.2.1:
/// "Comparisons are defined for the following:" — thirteen items, a CLOSED list. A pair outside it has no
/// comparison rule at all, and the standard says so in two places a class screen can see: §8.8.4.2.5 restricts
/// item 6's numeric operand to "an integer literal or an integer numeric data item of usage display or national",
/// and §8.8.4.2.13 states item 8 ("Comparisons involving indexes or index data items") as "Relation tests may be
/// made only between" three pairs. Both are class/usage/form facts of the source, so §4.2.2's "This warning
/// mechanism shall indicate violations of such rules" reaches them.
/// <para>⛔ ONE TABLE, AND IT OWNS ONLY THE GENERAL-RELATION ROWS. The other items of the list already have their
/// own band at the same checkpoint (<c>StatementValidation.CheckRelationalOperands</c>), and this table answers
/// "not mine" for every pair one of them owns, so a written relation draws ONE diagnostic: item 4 (class boolean)
/// is COBOLNET0844's; items 9–11 (message-tag, object, pointer — and the predefined NULL, class pointer by
/// §8.4.3.10.1) are the §8.8.4.2.2 Format 3 screen's; item 12 (strongly-typed groups) is §8.8.4.2.3 SR1's; item 13
/// (variable-length groups) is §8.5.1.12.1's. What is left is items 1–3 and 5–8 over the classes numeric,
/// alphabetic, alphanumeric, national and index.</para>
/// <para>⛔ CLASS IS ASKED OF THE ONE §8.5.2.1 TABLE-2 LATTICE (<see cref="IntrinsicArgumentRules.CandidateClasses"/>),
/// never of a local category switch — the lattice already reports an index data item as class INDEX rather than
/// its storage category, an index-name as class INDEX, a numeric-edited item as class alphanumeric, and a
/// figurative constant as the SET of classes its context may choose (§8.3.3.6.4 GR1/GR4). An EMPTY set is "not
/// statically decidable" (an operand whose binding already failed — kb/Work PB960) and fails OPEN, as every screen
/// over the lattice does.</para></summary>
internal static class RelationComparability
{
    /// <summary>The breach of the general-relation comparability table the pair commits, or null when the pair is
    /// one §8.8.4.2.1 defines — or one another band of the checkpoint owns, or one whose class is not statically
    /// decidable. The rule is symmetric; both operand orders answer alike.</summary>
    public static RelationComparabilityBreach? Breach(BoundOperand left, BoundOperand right)
    {
        CobolClass[] l = IntrinsicArgumentRules.CandidateClasses(left), r = IntrinsicArgumentRules.CandidateClasses(right);
        if (l.Length == 0 || r.Length == 0 || OwnedByAnotherBand(l) || OwnedByAnotherBand(r)) return null;
        bool li = IsClass(l, CobolClass.Index), ri = IsClass(r, CobolClass.Index);
        if (li || ri)
            return li && ri || IndexNameAgainstNumeric(li ? left : right, li ? right : left)
                ? null : RelationComparabilityBreach.IndexPair;
        // §8.8.4.2.1 item 6 — the one mixed numeric/character pair the list defines. Every other numeric pair is
        // item 1 (two class numeric) and every other character pair items 2, 3, 5 and 7; a figurative constant
        // that can be numeric (ZERO, §8.3.3.6.4 GR4) takes the numeric reading opposite a numeric operand.
        // Only an operand FIXED in class numeric is item 6's numeric operand: ZERO opposite a character operand takes
        // its character reading (GR4), so it is item 3/5, never a breach of §8.8.4.2.5.
        if (l is [CobolClass.Numeric] && IsCharacter(r) && !IsNumericIntegerOperand(left))
            return RelationComparabilityBreach.NumericAgainstCharacter;
        if (r is [CobolClass.Numeric] && IsCharacter(l) && !IsNumericIntegerOperand(right))
            return RelationComparabilityBreach.NumericAgainstCharacter;
        return null;
    }

    /// <summary>⛔ ISO §8.8.4.2.5 — "The numeric integer operand shall be an integer literal or an integer numeric data
    /// item of usage display or national." The integer test is the ONE §5.5 integer classifier
    /// (<see cref="IntrinsicResultType.IsIntegerOperand(BoundOperand)"/> — "An integer literal is a fixed-point numeric
    /// literal that contains no decimal point", §8.3.3.3.2; <see cref="PicInfo.IsIntegerDescription"/> for an item);
    /// the USAGE conjunct is this rule's own. An arithmetic expression is none of the admitted forms.
    /// <para>An INTEGER intrinsic function (§15.2 item 5) is admitted as the integer numeric temporary §15.4 makes it,
    /// in its literal text form — the SAME determination by which it moves to an alphanumeric receiver as the Table-16
    /// Integer row (docs/CONFORMANCE.md §7, A.1 item 92; kb/Work PB73). §8.8.4.2.5 itself routes the comparison "according
    /// to the rules of the MOVE statement", so a relation and a MOVE of the same function agree; a NUMERIC function
    /// (§15.2 item 4) is the Table-16 noninteger row there and is refused here for the same reason.</para></summary>
    private static bool IsNumericIntegerOperand(BoundOperand o) => o switch
    {
        BoundNumericLiteral => IntrinsicResultType.IsIntegerOperand(o),
        BoundFieldOperand { Place.Item: { IsGroup: false, Pic: { Usage: Usage.Display or Usage.National } } } =>
            IntrinsicResultType.IsIntegerOperand(o),
        BoundComputedOperand { Expr: BoundIntrinsicCall } => IntrinsicResultType.IsIntegerOperand(o),
        // A COUNTER REGISTER is an unsigned integer data item whose implicit description is PIC 9(d) USAGE DISPLAY
        // (docs/CONFORMANCE.md, the counter registers' declared capacity): §8.4.3.15.3 SR1 admits PAGE-COUNTER and
        // LINE-COUNTER "in any context where an integer data item may appear", and this is one (kb/Work PB1153).
        BoundComputedOperand { Expr: var register } when AlgebraicRanges.IsCounterRegister(register) => true,
        _ => false,
    };

    /// <summary>§8.8.4.2.13 row 2 — "an index-name and a numeric data item or numeric literal". Rows 1 and 3 are
    /// every pair of two class-index operands and are answered by the caller. The figurative ZERO opposite an
    /// index-name is "the numeric value '0'" (§8.3.3.6.4 GR4 — a figurative constant stands where a literal may),
    /// so it is row 2's numeric literal; opposite an index DATA item it is not, because row 3 admits nothing
    /// but an index-name or another index data item there.
    /// <para>⛔ A NUMERIC OR INTEGER FUNCTION-IDENTIFIER IS ROW 2'S "NUMERIC DATA ITEM", FOLDED OR NOT (kb/Work PB1662).
    /// §8.4.3.2.1: "A function-identifier references the unique data item that results from the evaluation of a
    /// function", and §15.2 items 4 and 5 give that item class and category numeric — the item §8.5.2.12 items 6–7
    /// call category numeric. The screen used to admit the function the compiler happened to FOLD (a
    /// <c>FUNCTION LENGTH</c> of a fixed item reaches here as a literal-shaped operand) and refuse the one it did not
    /// (<c>FUNCTION ABS(3)</c>), so one rule drew two answers by what the optimizer could compute. The fold keeps its
    /// origin (<see cref="BoundNumericLiteral.FunctionValue"/>) and BOTH are the data item the function references.
    /// An ARITHMETIC EXPRESSION is none of row 2's forms (a data item or a literal), and stays refused. A COUNTER
    /// REGISTER references "a temporary unsigned integer data item of class and category numeric" (§8.4.3.14.4 GR1;
    /// §8.4.3.15.4 GR1 says the same of PAGE-COUNTER and LINE-COUNTER), a numeric data item likewise.</para></summary>
    private static bool IndexNameAgainstNumeric(BoundOperand index, BoundOperand other) =>
        index is BoundComputedOperand { Expr: BoundIndexRef }
        && other switch
        {
            BoundNumericLiteral or BoundFigurative { Kind: 'Z' } => true,
            BoundFieldOperand or BoundComputedOperand { Expr: BoundIntrinsicCall } =>
                IntrinsicArgumentRules.ClassOf(other) is CobolClass.Numeric,
            BoundComputedOperand { Expr: var register } => AlgebraicRanges.IsCounterRegister(register),
            _ => false,
        };

    /// <summary>True when the operand's class is fixed and is <paramref name="cls"/>, or — for a figurative constant
    /// — when <paramref name="cls"/> is among the classes its context may choose.</summary>
    private static bool IsClass(CobolClass[] set, CobolClass cls) => Array.IndexOf(set, cls) >= 0;

    /// <summary>Class alphanumeric, alphabetic or national in EVERY reading (§8.8.4.2.1 item 7's set; Table 2 puts
    /// numeric-edited in class alphanumeric) — so ZERO, which may also be numeric, is never "character" here.</summary>
    private static bool IsCharacter(CobolClass[] set) =>
        Array.TrueForAll(set, c => IntrinsicArgumentRules.TableTwoClass(c) is CobolClass.Alphanumeric
            or CobolClass.Alphabetic or CobolClass.National);

    /// <summary>A class another band of the relation checkpoint owns, in EVERY reading: boolean (§8.8.4.2.2 Format
    /// 2, COBOLNET0844), object and pointer (Format 3, COBOLNET0868/0869 — NULL's only class here).</summary>
    private static bool OwnedByAnotherBand(CobolClass[] set) =>
        Array.TrueForAll(set, c => c is CobolClass.Boolean or CobolClass.Object or CobolClass.Pointer);
}
