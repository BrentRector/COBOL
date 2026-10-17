// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding.Bound;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Binding.Procedure;

using Core = CobolParserCore;

/// <summary>Why a literal-1 operand cannot name a program, or <see cref="None"/> when it can.</summary>
internal enum ProgramNameFault
{
    /// <summary>The literal forms a program-name.</summary>
    None,

    /// <summary>The operand is not an alphanumeric or national literal: a numeric or boolean literal, a figurative
    /// constant (a symbolic-character included), or a constant-name that substitutes one of them.</summary>
    NotAlphanumericOrNational,

    /// <summary>The literal is zero-length.</summary>
    ZeroLength,
}

/// <summary>A literal-1 that names a program, as written: the operand it binds to and its spelling for a diagnostic.
/// <paramref name="Spelling"/> is the literal as the program wrote it, or the constant-name's word.</summary>
internal readonly record struct ProgramNameOperand(BoundOperand? Operand, string Spelling)
{
    /// <summary>The name <see cref="Operand"/> forms, in the edition <paramref name="editionYear"/>, or the reason it
    /// forms none.</summary>
    public (string? Name, ProgramNameFault Fault) Form(int editionYear) =>
        ProgramNameLiteral.Form(Operand, editionYear);
}

/// <summary>⛔ THE ONE READER OF A LITERAL THAT NAMES A PROGRAM (kb/Work PB130, PB780). Four positions write one:
/// CALL's literal-1 (§14.9.4.3 SR2), the CALL … AS NESTED literal-1 (SR15), CANCEL's literal-1 (§14.9.5.3 SR2) and
/// the program-address-identifier's literal-1 (§8.4.3.13.3 SR2), plus the vendor <c>SET … TO ENTRY</c> spelling of
/// the last. Each rule admits an alphanumeric or national literal whose length is not zero, and each names the
/// program "as described in 8.3.2.2", so the literal's content is FORMED by the ONE rule the run time applies
/// (<see cref="ExternalizedNames.FormInEdition"/>: spaces trimmed, kb/Work PB1539; the edition's spelling, PB2840).
/// <para>A constant-name stands in every one of them: "constant-name-1 may be used anywhere that a format specifies
/// a literal of the class and category of constant-name-1" (ISO §13.10.3 SR2), and its effect "is as if literal-1 …
/// were written where constant-name-1 is written" (§13.10.4 GR1). The grammar spells each position
/// <c>literal | dataReference</c> (or its nonNumericLiteral twin), so the word arrives as a data reference and is
/// recognized by the ONE literal-alias resolution, <see cref="ExpressionBinder.LiteralAliasOf"/>, before the data
/// reference is resolved as an identifier or read as a program-prototype-name. Before kb/Work PB780 each position
/// went straight to the identifier resolver and refused <c>CALL K</c> as "'K' is not defined".</para></summary>
internal static class ProgramNameLiteral
{
    /// <summary>The literal-1 a WRITTEN literal is.</summary>
    public static ProgramNameOperand Of(ExpressionBinder expr, Core.NonNumericLiteralContext? literal, string spelling) =>
        new(expr.NonNumericLiteralOperand(literal), spelling);

    /// <summary>The literal-1 a data reference SPELLS when the word is a constant-name or a symbolic-character
    /// (§13.10.3 SR2; §8.3.3.6.3 SR1), or <see langword="null"/> when it is neither and the reference is an
    /// identifier or a program-prototype-name.</summary>
    public static ProgramNameOperand? Of(ExpressionBinder expr, Core.DataReferenceContext reference) =>
        expr.LiteralAliasOf(reference) is { } alias ? new(expr.AliasOperand(alias), alias.Word) : null;

    /// <summary>The program-name <paramref name="operand"/> forms, or the fault that keeps it from naming one. A
    /// boolean, numeric or figurative operand is not "an alphanumeric or national literal"; a hexadecimal literal is
    /// §8.3.3.2 Format 2 of an alphanumeric one and a concatenation expression has already folded (§8.8.3.3 GR3).
    /// A literal of spaces only is NOT zero-length and stays legal: it forms the empty name, which the run-time
    /// locate reports as EC-PROGRAM-NOT-FOUND.</summary>
    public static (string? Name, ProgramNameFault Fault) Form(BoundOperand? operand, int editionYear) =>
        operand is not BoundStringLiteral { Category: PicCategory.Alphanumeric or PicCategory.National } literal
            ? (null, ProgramNameFault.NotAlphanumericOrNational)
        : literal.Value.Length == 0 ? (null, ProgramNameFault.ZeroLength)
        : (ExternalizedNames.FormInEdition(literal.Value, editionYear), ProgramNameFault.None);
}
