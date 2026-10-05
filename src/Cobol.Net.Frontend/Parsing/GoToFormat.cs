// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>Which general format of ISO §14.9.17.2 a parsed GO TO statement is — the ONE place that decision is
/// written down (kb/Work PB412).</summary>
public enum GoToFormat
{
    /// <summary>Format 1 (unconditional): <c>GO TO procedure-name-1</c> — exactly one procedure-name, no
    /// DEPENDING phrase.</summary>
    Unconditional,

    /// <summary>Format 2 (depending): <c>GO TO { procedure-name-1 } … DEPENDING ON identifier-1</c> — one or more
    /// procedure-names, DEPENDING required.</summary>
    Depending,

    /// <summary>The ANSI X3.23-1985 target-less <c>GO TO.</c> — no procedure-name at all. ISO/IEC 1989:2023
    /// §14.9.17.2 prints NO such format; it was deleted by ISO/IEC 1989:2002 and survives here only so the
    /// edition gate can name the edition (Constructs.BareGotoRemoved2002) instead of leaving a parse error.
    /// </summary>
    AnsiAlterable,
}

/// <summary>
/// ⛔ THE GO TO FORMAT DECISION, WRITTEN ONCE (kb/Work PB412).
///
/// <para>§14.9.17.2 prints exactly two general formats; the grammar rule <c>goToStatement</c> now carries one
/// alternative per printed format plus the edition-gated ANSI-85 arm, so the three cases below are EXHAUSTIVE
/// and DISJOINT by construction — the parser can no longer deliver a shape that is none of them. Before that
/// narrowing the rule was the union of all three with every element optional, the format was reconstructed from
/// optional children at four separate readers, and the complement (two procedure-names with no DEPENDING;
/// DEPENDING with no procedure-names) fell into whichever reader's `else` arm it reached.</para>
///
/// <para>Every reader of a GO TO's shape asks THIS classifier — the binder, the edition gate, and the ALTER
/// single-GO-TO-paragraph test — so a future format is added in one enum and every <c>switch</c> over it that
/// is exhaustive stops compiling until it is handled.</para>
/// </summary>
public static class GoToFormats
{
    /// <summary>The general format <paramref name="g"/> was written in. Total: the grammar admits no fourth
    /// shape.
    /// <para>Both probes are SINGLE-CHILD accessors — <c>DEPENDING()</c> and <c>procedureName(0)</c> scan
    /// the context's children and return the first match — never <c>procedureName()</c>, whose generated body
    /// allocates a fresh <c>List</c> on every call. This runs once per GO TO at bind, once more in the edition
    /// pass, and once per single-statement paragraph in the ALTER prepass. Format 2 is keyed on the DEPENDING
    /// keyword, not on its operand: identifier-1 has three alternatives (a data reference, a function-identifier,
    /// an inline method invocation — kb/Work PB1421), and the keyword is the one child every one of them
    /// shares.</para></summary>
    public static GoToFormat Of(CobolParserCore.GoToStatementContext g)
        => g.DEPENDING() is not null ? GoToFormat.Depending
         : g.procedureName(0) is not null ? GoToFormat.Unconditional
         : GoToFormat.AnsiAlterable;

    /// <summary>The message for a written GO TO shape that NEITHER general format admits, or null when the shape
    /// is one a format prints. The parse-layer error strategy asks this so the diagnostic names the formats and
    /// their cardinalities instead of reporting the token the parser happened to stop on.
    /// <para>The two complement shapes are exactly the two the old union rule accepted: a list of
    /// procedure-names with no DEPENDING phrase (only Format 2 prints the list, and its DEPENDING is required),
    /// and a DEPENDING phrase with no procedure-name (Format 2's <c>{ procedure-name-1 } …</c> is a brace group,
    /// so §5.2.6.3 requires one).</para></summary>
    public static string? DiagnoseWrittenShape(int procedureNames, bool hasDepending) => (procedureNames, hasDepending) switch
    {
        ( >= 2, false) => "A GO TO statement with more than one procedure-name is §14.9.17.2 Format 2, whose "
            + "DEPENDING phrase is required: Format 1 prints one unbracketed procedure-name and no DEPENDING "
            + "phrase (§5.2.6.2 — only a bracketed portion may be omitted), and Format 2 prints "
            + "'{ procedure-name-1 } … DEPENDING ON identifier-1' with DEPENDING underlined (§5.2.2).",
        (0, true) => "A GO TO statement with a DEPENDING phrase is §14.9.17.2 Format 2, which prints "
            + "'{ procedure-name-1 } …' ahead of it: one alternative of a brace group shall be explicitly "
            + "specified (§5.2.6.3), so at least one procedure-name is required.",
        _ => null,
    };
}
