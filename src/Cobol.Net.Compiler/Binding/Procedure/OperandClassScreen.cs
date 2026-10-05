// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding.Procedure;

/// <summary>The operand CLASSES a statement's identifier-position syntax rule can enumerate. Each member is one
/// phrase the standard writes, read through the ONE §8.5.2.1 Table-2 classifier
/// (<see cref="IntrinsicArgumentRules.ClassOfItem"/>) and the ONE §5.5 integer primitive
/// (<see cref="PicInfo.IsIntegerDescription"/>) — never a private category switch at the call site.</summary>
[Flags]
internal enum OperandClasses
{
    None = 0,

    /// <summary>"an integer data item" · "a data item that is an integer" · "a numeric elementary data item that
    /// is an integer": a fixed-point numeric ELEMENTARY item of scale zero (§5.5 2)b)2 through
    /// <see cref="PicInfo.IsIntegerDescription"/>; a floating-point item and an index data item are excluded
    /// there, a group has no PICTURE and is excluded by its class).</summary>
    IntegerItem = 1 << 0,

    /// <summary>"a data item whose usage is index" · "a data item of class index" — §8.5.2.1 Table 2's class
    /// INDEX, which is the index DATA item (never an index-name: that is not a data item at all, §13.18.38.3
    /// SR7).</summary>
    IndexDataItem = 1 << 1,

    /// <summary>"a numeric elementary item" — class NUMERIC (fixed- or floating-point), elementary.</summary>
    NumericElementaryItem = 1 << 2,
}

/// <summary>ONE statement operand position whose syntax rule closes the operand to a set of classes: the
/// statement and operand as the general format names them, the rule that closes it, the rule's own words for
/// what it admits, the admitted set, and the diagnostic the position reports under.</summary>
/// <remarks>⚠ <see cref="Statement"/> is spelled WITHOUT an ellipsis: the diagnostic renderer transliterates U+2026
/// to ASCII, and a message opening "SET … TO" printed a statement nobody wrote (kb/Work PB388,
/// <c>SetDiagnosticNamesReceiversDriftTests</c>); the operand the program wrote is named in the message itself.</remarks>
internal sealed record OperandPosition(
    string Statement, string Operand, string Rule, string Requirement, OperandClasses Admits,
    DiagnosticDescriptor Diagnostic);

/// <summary>⛔ THE REGISTER OF CLASS-CLOSED IDENTIFIER POSITIONS (kb/Work PB210 · PB211 · PB212). Every statement
/// operand whose syntax rule reads "identifier-n shall reference a data item of class …" is a row HERE and is
/// screened by <see cref="OperandClassScreen"/>, never by a hand-written test at its binder.
/// <para>Why a register and not three <c>if</c>s: the three defects this closed were each the SAME absence — a
/// binder that bound the position with a bare <c>FieldOperand</c> / <c>Refs.Resolve</c> and asked nothing — found
/// three times by three sweeps. A position that is a row has its rule written once, its diagnostic chosen once and
/// its drift test (<c>OperandClassScreenDriftTests</c>) run against every class shape automatically; a new
/// class-closed position is one row and one call.</para>
/// <para>What is NOT a row: a position whose rule is not a class closure over an IDENTIFIER — the
/// arithmetic-expression positions (§8.8.1.1, <c>OperandContextRules</c>), the arithmetic resultants
/// (<c>ExpressionBinder.ScreenResultant</c>, whose numeric-edited axis §8.8.1.1 does not have), the PERFORM … TIMES
/// count and the STOP/GOBACK status operand (each admits a LITERAL or a function as well, so it is screened on
/// the bound shape by its own binder). DESIGN-binder-bound-tree.md §3.6 lists every position of both kinds.</para></summary>
internal static class OperandPositions
{
    /// <summary>ISO §14.9.17.3 SR1 — GO TO … DEPENDING ON identifier-1 (kb/Work PB210).</summary>
    public static readonly OperandPosition GoToDependingSelector = new(
        "GO TO DEPENDING ON", "identifier-1", "§14.9.17.3 SR1",
        "a numeric elementary data item that is an integer",
        OperandClasses.IntegerItem, DiagnosticCatalog.GoToDependingSelectorClass);

    /// <summary>ISO §14.9.37.3 SR5 first sentence — SEARCH … VARYING identifier-2 (kb/Work PB211). The rule's second
    /// sentence is a subscript prohibition, not a class, and is asked by <c>SearchBinder</c> under the same
    /// diagnostic.</summary>
    public static readonly OperandPosition SearchVaryingIdentifier = new(
        "SEARCH VARYING", "identifier-2", "§14.9.37.3 SR5",
        "a data item whose usage is index or a data item that is an integer",
        OperandClasses.IntegerItem | OperandClasses.IndexDataItem, DiagnosticCatalog.SearchVaryingOperand);

    /// <summary>ISO §14.9.39.3 SR1 — the SET Format-1 receiving identifier-1 (kb/Work PB212). An index-name-1
    /// receiver is the brace's OTHER alternative and is not screened here.</summary>
    public static readonly OperandPosition SetIndexAssignmentReceiver = new(
        "SET TO (Format 1, index-assignment)", "identifier-1", "§14.9.39.3 SR1",
        "a data item of class index or an integer data item",
        OperandClasses.IntegerItem | OperandClasses.IndexDataItem, DiagnosticCatalog.SetIndexAssignmentOperand);

    /// <summary>ISO §14.9.28.3 SR2 first sentence over the varied identifier of the VARYING / AFTER phrase — "Each
    /// identifier shall reference a numeric elementary item described in the data division" (found by the
    /// PB210–PB212 sibling sweep: a <c>PIC X</c> induction variable compiled and threw at run time).</summary>
    public static readonly OperandPosition PerformVaryingIdentifier = new(
        "PERFORM VARYING / AFTER", "the varied identifier", "§14.9.28.3 SR2",
        "a numeric elementary item described in the data division",
        OperandClasses.NumericElementaryItem, DiagnosticCatalog.PerformVaryingOperandRule);

    /// <summary>ISO §14.9.28.3 SR5 a) — the varied identifier when an index-name is in the FROM phrase.</summary>
    public static readonly OperandPosition PerformVaryingIdentifierFromIndex = new(
        "PERFORM VARYING / AFTER with an index-name FROM", "the varied identifier", "§14.9.28.3 SR5 a)",
        "an integer data item",
        OperandClasses.IntegerItem, DiagnosticCatalog.PerformVaryingOperandRule);

    /// <summary>ISO §14.9.51.3 SR14 — WRITE … ADVANCING identifier-2 (kb/Work PB1023). The brace's OTHER
    /// alternative, integer-1 (SR15 — "positive or zero"), is a literal and is screened beside this row by
    /// <c>SequentialIoBinder.BindAdvancing</c> under the same diagnostic.</summary>
    public static readonly OperandPosition WriteAdvancingIdentifier = new(
        "WRITE ADVANCING", "identifier-2", "§14.9.51.3 SR14",
        "an integer data item",
        OperandClasses.IntegerItem, DiagnosticCatalog.WriteAdvancingOperand);

    /// <summary>Every row (the drift test holds this against the declared fields, so a row cannot be declared
    /// and forgotten here).</summary>
    public static IReadOnlyList<OperandPosition> All { get; } =
    [
        GoToDependingSelector, SearchVaryingIdentifier, SetIndexAssignmentReceiver,
        PerformVaryingIdentifier, PerformVaryingIdentifierFromIndex, WriteAdvancingIdentifier,
    ];
}

/// <summary>⛔ THE ONE OPERAND-CLASS SCREEN for the positions <see cref="OperandPositions"/> registers.
/// <see cref="ClassesOf"/> answers "which of the enumerable classes is this operand in" from the §8.5.2.1 Table-2
/// classifier; <see cref="Screen(EditionContext,OperandPosition,BoundOperand,string)"/> asks it for a position and
/// reports the position's own rule, in both dialect lanes — an identifier slot has no coercion to offer, and every
/// shape it refuses either threw at run time or stored a value the standard defines for no such operand.</summary>
internal static class OperandClassScreen
{
    /// <summary>The enumerable classes <paramref name="p"/> belongs to. <see cref="OperandClasses"/> with every flag
    /// set when the class is not statically decidable (a recovery profile for an entry whose PICTURE was already
    /// refused) — the screen FAILS OPEN there, as every screen over the Table-2 classifier does (kb/Work PB960),
    /// rather than re-diagnosing an item the compile has already reported.</summary>
    public static OperandClasses ClassesOf(Place p)
    {
        // §8.4.3.3.4 GR6 c): reference modification makes a numeric item "class and category national if the usage
        // is national; otherwise … class and category alphanumeric" — never integer, numeric or index. A
        // reference-modified place is the one kind that denotes no declared item (Place.DenotedItem).
        if (p.DenotedItem is null) return OperandClasses.None;
        return IntrinsicArgumentRules.ClassOfItem(p.Item) switch
        {
            null => OperandClasses.IntegerItem | OperandClasses.IndexDataItem | OperandClasses.NumericElementaryItem,
            CobolClass.Index => OperandClasses.IndexDataItem,
            CobolClass.Numeric => OperandClasses.NumericElementaryItem
                | (p.Pic is { IsIntegerDescription: true } ? OperandClasses.IntegerItem : OperandClasses.None),
            _ => OperandClasses.None,
        };
    }

    /// <summary>True when <paramref name="p"/> is in a class <paramref name="pos"/> admits.</summary>
    public static bool Admits(OperandPosition pos, Place p) => (ClassesOf(p) & pos.Admits) != 0;

    /// <summary>Screen a data-item operand at <paramref name="pos"/>; false (after reporting) when refused.</summary>
    public static bool Screen(EditionContext edition, OperandPosition pos, Place p, string text)
    {
        if (Admits(pos, p)) return true;
        Report(edition, pos, text, Describe(p));
        return false;
    }

    /// <summary>Screen a BOUND operand at <paramref name="pos"/>. Every row is an IDENTIFIER position that closes
    /// the operand to data items, so a literal a constant-name substitutes (§13.10.4 GR1) or a figurative constant
    /// is refused by the same rule; a <see cref="BoundOperandError"/> was reported where it was made and is not
    /// reported again.
    /// <para>A FUNCTION-IDENTIFIER is an identifier (§8.4.3.1.2 Format 1), and §8.4.3.2.4 GR1 says it "references a
    /// temporary data item" — an elementary item of the function's category — so it is classed by its RESULT
    /// (<see cref="FunctionResultClasses"/>), never refused as "not a data item" (kb/Work PB1421: an integer
    /// function was refused at GO TO … DEPENDING). A user-defined function's or an inline method invocation's
    /// temporary arrives as a <see cref="BoundFieldOperand"/> over its returning item and takes the data-item
    /// arm. A RECEIVING row never sees one: §8.4.3.2.3 SR1 keeps a function-identifier out of a receiving
    /// operand, and those slots do not parse one.</para></summary>
    public static bool Screen(EditionContext edition, OperandPosition pos, BoundOperand op, string text)
    {
        switch (op)
        {
            case BoundOperandError: return false;
            case BoundFieldOperand { Place: var p }: return Screen(edition, pos, p, text);
            case BoundComputedOperand or BoundNumericLiteral { FunctionValue: true }:
                if ((FunctionResultClasses(op) & pos.Admits) != 0) return true;
                Report(edition, pos, text, DescribeFunctionResult(op));
                return false;
            default:
                Report(edition, pos, text, "not a data item");
                return false;
        }
    }

    /// <summary>The enumerable classes of the temporary item an intrinsic function-identifier (or a counter
    /// register, "a temporary unsigned integer data item", §8.4.3.15.4 GR1) references: the ONE integer classifier
    /// (§15.2 type 5, <see cref="IntrinsicResultType.IsIntegerOperand(BoundOperand)"/>) and the ONE operand
    /// category reader. A temporary item is never of class index.</summary>
    private static OperandClasses FunctionResultClasses(BoundOperand op) =>
        IntrinsicResultType.IsIntegerOperand(op) ? OperandClasses.IntegerItem | OperandClasses.NumericElementaryItem
        : IntrinsicResultType.OperandCategory(op) is PicCategory.Numeric ? OperandClasses.NumericElementaryItem
        : OperandClasses.None;

    private static string DescribeFunctionResult(BoundOperand op) =>
        IntrinsicResultType.OperandCategory(op) is PicCategory.Numeric
            ? "a numeric function-identifier, and \"A numeric function shall not be specified where an integer "
              + "operand is required\" (ISO §8.4.3.2.3 SR11)"
            : $"a function-identifier of category {IntrinsicResultType.OperandCategory(op)?.ToString().ToLowerInvariant() ?? "unknown"}";

    private static void Report(EditionContext edition, OperandPosition pos, string text, string what) =>
        edition.Error(pos.Diagnostic,
            $"{pos.Statement} {pos.Operand} '{text}' shall reference {pos.Requirement} (ISO {pos.Rule}); "
            + $"'{text}' is {what}");

    private static string Describe(Place p)
    {
        if (p.DenotedItem is null) return "reference-modified, which makes it alphanumeric or national (ISO §8.4.3.3.4 GR6)";
        if (p.Item.IsGroup) return "a group item";
        return IntrinsicArgumentRules.ClassOfItem(p.Item) switch
        {
            CobolClass.Index => "an index data item",
            CobolClass.Numeric when p.Pic is { IsFloat: true } => "a floating-point numeric item",
            CobolClass.Numeric when p.Pic is { IsIntegerDescription: false } => "a numeric item that is not an integer",
            CobolClass.Numeric => "an integer data item",
            { } c => $"of class {IntrinsicArgumentRules.Name(c)}",   // the ONE Table-2 class spelling
            null => "of an undecidable class",
        };
    }
}
