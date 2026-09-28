// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Editions.Diagnostics;

namespace CobolNet.Binding;

/// <summary>
/// ⛔ THE ONE STATEMENT OF THE §8.4.3.10.3 SR1 REFUSAL (kb/Work PB1427) — COBOLNET2576, for the predefined NULL written
/// in a position the rule does not list. NULL is an IDENTIFIER (§8.4.3.1.2 Format 8, predefined-address; the NULL arm
/// of Format 6, predefined-object — §8.4.3.7.3 SR2), never a literal or a figurative constant, and SR1 enumerates the
/// only places it may be written: "it may be used only as a sending operand in an INITIALIZE or a SET statement; as an
/// argument in a program-prototype format CALL statement, a function-prototype format function activation, or a
/// method invocation; or in a pointer-or-object-reference relation condition".
/// <para>The grammar parses NULL wherever a non-numeric literal may stand (a superset parse: <c>nonNumericLiteral</c>
/// carries the <c>predefinedNull</c> arm), so every consumer of that rule meets it. The PROCEDURE DIVISION operands
/// meet it through <c>ExpressionBinder.NonNumericLiteralOperand</c>; the positions that read a literal directly — a
/// constant entry's AS operand, a VALUE clause operand, a concatenation operand, an externalized name — each ask
/// this, so the refusal names ONE rule with ONE text wherever the slot is (kb/Work PB1427's wave-69 finisher: the
/// constant entry's decoder assumed the last arm was a boolean literal and threw on NULL, and a VALUE clause
/// operand counted NULL as "a literal by shape" and stored LOW-VALUE). <c>PredefinedNullContextDriftTests</c> pins
/// that nothing else reports the code and that every literal decoder that reads the figurative arm also decides
/// this one.</para>
/// </summary>
internal static class PredefinedNullRule
{
    /// <summary>Report the §8.4.3.10.3 SR1 refusal for NULL written in <paramref name="position"/> — a phrase
    /// naming the slot and, where the slot's own format says what it admits, that format (e.g. "the AS operand of
    /// constant entry 'K' — literal-1 or an arithmetic expression of literals (ISO §13.10.2)").</summary>
    public static void Report(EditionContext edition, string position) =>
        edition.Error(DiagnosticCatalog.PredefinedNullContext,
            "NULL is the predefined address / object reference — an identifier (ISO §8.4.3.1.2 Format 8; §8.4.3.7), "
            + "not a literal or a figurative constant — and ISO §8.4.3.10.3 SR1 admits it only as a sending operand in "
            + "an INITIALIZE or a SET statement, as an argument in a program-prototype CALL, a function-prototype "
            + "activation or a method invocation, or in a pointer-or-object-reference relation condition; "
            + position + " is none of these");
}
