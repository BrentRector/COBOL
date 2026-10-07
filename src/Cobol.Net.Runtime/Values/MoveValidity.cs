// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime;

/// <summary>An operand's heading in ISO §14.9.25.3 <b>Table 16 — Validity of types of MOVE statements</b>, as the table
/// prints its ROWS: the numeric row split into Integer and Noninteger, the edited forms separate from their plain
/// categories. The COLUMNS pair "Alphanumeric-edited, Alphanumeric", "National, National-edited" and "Numeric,
/// Numeric-edited", which is why a receiver is read through the same heading.
/// <para>The table's Type-name row and column have no member: a strongly-typed group is <see cref="None"/>, because
/// SR2 ("If identifier-2 references a strongly-typed group item, identifier-1 shall be specified and be described as
/// a group item of the same type") refuses every "No" of the Type-name column before SR10 reads the table, and the
/// Type-name row is all "Yes" — what §14.9.25.4 GR4's group exemption gives every group sender.</para></summary>
public enum Table16Category
{
    /// <summary>Not a heading of the table: a group (§14.9.25.4 GR4 — an alphanumeric group moves as a character copy
    /// without conversion; a strongly-typed group is SR2's), or an operand of a class SR1 governs (index, object,
    /// pointer). The table admits every pair with this side.</summary>
    None = 0,
    /// <summary>Alphabetic (PICTURE A).</summary>
    Alphabetic,
    /// <summary>Alphanumeric.</summary>
    Alphanumeric,
    /// <summary>Alphanumeric-edited.</summary>
    AlphanumericEdited,
    /// <summary>Boolean (a bit group's as-if PICTURE too, §13.18.29.4 GR1 b)).</summary>
    Boolean,
    /// <summary>National (a national group's as-if PICTURE too, §13.18.29.4 GR2 b)).</summary>
    National,
    /// <summary>National-edited.</summary>
    NationalEdited,
    /// <summary>Numeric, Integer.</summary>
    NumericInteger,
    /// <summary>Numeric, Noninteger — digits to the right of the decimal point, or a floating-point item.</summary>
    NumericNoninteger,
    /// <summary>Numeric-edited.</summary>
    NumericEdited,
}

/// <summary>
/// ⭐ THE RULES OF ISO §14.9.25.3 THAT A DATA DESCRIPTION ALONE DECIDES, in ONE place for both phases (kb/Work PB2076):
/// SR10's Table 16 (<see cref="Table16Refusal"/>) and SR8's fixed-width binary sender (<see cref="BinaryWidthRefusal"/>).
/// <para>The compiler asks them for every MOVE it binds (<c>MoveTable16</c>'s chain, through
/// <c>Table16Operand.Heading</c>), and the generated universal-dispatch switch asks them at RUN TIME, where §9.3.6
/// match rule 7 needs "may be a sending item in a MOVE statement" between a method's returning item and the
/// invocation's — two descriptions compiled independently, so the question cannot be answered at either compile
/// (<see cref="ActivationRelations.ReturningMatches"/>). Before, the run-time side asked only whether both items were
/// MOVE-class, and a method returning <c>PIC 9V99</c> matched an invocation returning <c>PIC X(4)</c>; a second copy
/// of the table in the runtime would have been the next drift, so the table moved here and the compiler reads it.</para>
/// <para>Every reason names its rule; the texts are the ones the compiler's MOVE diagnostics have always carried.</para>
/// </summary>
public static class MoveValidity
{
    /// <summary>ISO §14.9.25.3 SR10 — "For all other cases not described in Syntax rules 8 and 9, table 16, Validity of
    /// types of MOVE statements, specifies the validity of the move" — as the reason the table refuses the
    /// <paramref name="sender"/> → <paramref name="receiver"/> pair, or null when it admits it. A
    /// <see cref="Table16Category.None"/> side is admitted (a group, §14.9.25.4 GR4; a class SR1 governs).
    /// <para>The arms are the table read AS PRINTED (specs/ISO_COBOL.md, Table 16); with them every cell over the
    /// nine headings is decided here (<c>Table16PrintedTableDriftTests</c> compares all 81). The classic '85 rows carry
    /// the same "No" cells, so the table is version-invariant.</para></summary>
    public static string? Table16Refusal(Table16Category sender, Table16Category receiver)
    {
        if (sender is Table16Category.None || receiver is Table16Category.None) return null;

        // ── BOOLEAN column: alphabetic, alphanumeric-edited, national-edited, numeric and numeric-edited are "No" ──
        if (receiver is Table16Category.Boolean)
            return sender is Table16Category.Alphabetic or Table16Category.AlphanumericEdited
                   or Table16Category.NationalEdited or Table16Category.NumericInteger
                   or Table16Category.NumericNoninteger or Table16Category.NumericEdited
                ? "an alphabetic, alphanumeric-edited, numeric or numeric-edited sending operand does not move "
                  + "to a boolean receiver (ISO §14.9.25.3 SR10, Table 16)"
                : null;

        // ── NATIONAL column ("National, National-edited"): only a NONINTEGER numeric sender is "No" ──
        if (receiver is Table16Category.National or Table16Category.NationalEdited)
            return sender is Table16Category.NumericNoninteger
                ? "a noninteger numeric sending operand does not move to a national receiver "
                  + "(ISO §14.9.25.3 SR10, Table 16)"
                : null;

        // ── NATIONAL-EDITED row: the ONLY "Yes" is the "National, National-edited" column, decided above. It is a
        //    SEPARATE ROW from National (which is "Yes" into boolean and into the numeric column) for the reason the
        //    alphanumeric-edited row differs from alphanumeric: an edit mask has no de-editable value and no boolean
        //    characters (kb/Work PB492). ──
        if (sender is Table16Category.NationalEdited)
            return "a national-edited sending operand moves only to a national or national-edited receiver "
                 + "(ISO §14.9.25.3 SR10, Table 16)";

        // ── NATIONAL row: alphabetic / alphanumeric / alphanumeric-edited receivers are "No" ──
        if (sender is Table16Category.National)
            return receiver is Table16Category.Alphabetic or Table16Category.Alphanumeric
                       or Table16Category.AlphanumericEdited
                ? "a national sending operand does not move to an alphabetic, alphanumeric or "
                  + "alphanumeric-edited receiver (ISO §14.9.25.3 SR10, Table 16; FUNCTION DISPLAY-OF is the "
                  + "sanctioned conversion)"
                : null;

        // ── BOOLEAN row: alphabetic / numeric / numeric-edited receivers are "No" (plain alphanumeric is Yes) ──
        if (sender is Table16Category.Boolean)
            return receiver is Table16Category.Alphabetic || IsNumericColumn(receiver)
                ? "a boolean sending operand does not move to an alphabetic, numeric or numeric-edited receiver "
                  + "(ISO §14.9.25.3 SR10, Table 16)"
                : null;

        // ── ALPHABETIC column: a numeric or numeric-edited sender is "No" (`MOVE 5 TO a-pic-a` stored "5   "). ──
        if (receiver is Table16Category.Alphabetic && IsNumericColumn(sender))
            return "a numeric or numeric-edited sending operand does not move to an alphabetic receiver "
                 + "(ISO §14.9.25.3 SR10, Table 16)";

        // ── ALPHABETIC row: numeric and numeric-edited receivers are "No" (a PIC A sender into PIC 9 stored zeros). ──
        if (sender is Table16Category.Alphabetic && IsNumericColumn(receiver))
            return "an alphabetic sending operand does not move to a numeric or numeric-edited receiver "
                 + "(ISO §14.9.25.3 SR10, Table 16)";

        // ── ALPHANUMERIC-EDITED row: numeric and numeric-edited receivers are "No". The DE-EDITING move is the
        //    NUMERIC-edited row's (numeric-edited → numeric is Yes) — an ALPHANUMERIC edit mask has no de-editable
        //    value, which is exactly why the two rows differ. ──
        if (sender is Table16Category.AlphanumericEdited && IsNumericColumn(receiver))
            return "an alphanumeric-edited sending operand does not move to a numeric or numeric-edited "
                 + "receiver (ISO §14.9.25.3 SR10, Table 16)";

        // ── NUMERIC row, Noninteger: alphanumeric and alphanumeric-edited receivers are "No" (`MOVE 5.5 TO a-pic-x`
        //    printed "5.5"); the alphabetic receiver is refused by the column arm above, and the INTEGER row's Yes
        //    is the classic digit-image move. ──
        if (sender is Table16Category.NumericNoninteger
            && receiver is Table16Category.Alphanumeric or Table16Category.AlphanumericEdited)
            return "a noninteger numeric sending operand does not move to an alphabetic, alphanumeric or "
                 + "alphanumeric-edited receiver (ISO §14.9.25.3 SR10, Table 16)";

        return null;
    }

    /// <summary>ISO §14.9.25.3 SR8 — "If identifier-1 references a data item described with usage binary-char,
    /// binary-short, binary-long, or binary-double, identifier-2 shall reference a numeric or numeric-edited item" — as
    /// the reason, or null. SR10 defers to it ("for all other cases not described in Syntax rules 8 and 9"), so it is
    /// asked before <see cref="Table16Refusal"/>, never instead of it. A receiver that is no heading of the table (a
    /// group) is refused: a group item is neither numeric nor numeric-edited, and §14.9.25.4 GR4's exemption is the
    /// table's, which SR10 does not reach for a case SR8 describes.</summary>
    /// <param name="binaryWidthSender">The sending data item is described with one of the four usages.</param>
    /// <param name="receiver">The receiving operand's heading.</param>
    public static string? BinaryWidthRefusal(bool binaryWidthSender, Table16Category receiver) =>
        binaryWidthSender && !IsNumericColumn(receiver)
            ? "a BINARY-CHAR/-SHORT/-LONG/-DOUBLE sending operand shall reference only a numeric or "
              + "numeric-edited receiver (ISO §14.9.25.3 SR8)"
            : null;

    /// <summary>Table 16's "Numeric, Numeric-edited" column (and the three rows it pairs).</summary>
    private static bool IsNumericColumn(Table16Category c) =>
        c is Table16Category.NumericInteger or Table16Category.NumericNoninteger or Table16Category.NumericEdited;
}
