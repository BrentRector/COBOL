// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Expressions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// The ONE reader for a <c>signedIntegerLiteral</c> parse node (kb/Work PB553).
///
/// <para>The grammar rule is <c>(PLUS | MINUS)? INTEGERLIT</c> — the sign is a SEPARATE token, because a
/// DEFAULT-mode <c>(</c> after FROM/TO opens no signed-literal lexer region and the sign-adjacent
/// <c>SIGNED_INTEGERLIT</c> token therefore never reaches these slots. That is superset-parse: it also admits
/// <c>( + 1 )</c>, which the standard does not. ISO §8.3.3.3.2 2) is the rule — <i>"A literal shall not contain
/// more than one sign character. If a sign is used, it shall appear as the leftmost character of the
/// literal."</i> — and a literal is ONE character-string, so a space between the sign and its digits makes the
/// two things two things. The violation is REPORTED once, for this rule and <c>signedNumericLiteral</c> alike, by
/// <c>LiteralScreenPass</c> (COBOLNET2155; kb/Work PB1445); this reader only declines to read the separated sign
/// as part of the value.</para>
///
/// <para>⛔ THE VALUE IS NOT WHAT <c>GetText()</c> RETURNS, and the difference is not cosmetic: ANTLR's
/// <c>GetText()</c> concatenates the node's tokens with the whitespace stripped, so <c>+ 1</c> and <c>+1</c>
/// both read back as <c>"+1"</c> and the violation is invisible to any caller that goes straight to the text.
/// Every consumer of the rule comes through this method so that cannot happen once.</para>
/// </summary>
internal static class SignedIntegerLiteral
{
    /// <summary>The literal's text with its sign (e.g. <c>"+1"</c>, <c>"-3"</c>, <c>"12"</c>). When the sign does
    /// not abut the digits (<see cref="ArithmeticFormationRules.SignAbuts"/>) the source is already refused by
    /// <c>LiteralScreenPass</c>, and the UNSIGNED digits are returned so the caller's own range screens still run on
    /// a recovered value rather than cascading.</summary>
    /// <param name="ctx">The <c>signedIntegerLiteral</c> node.</param>
    public static string Read(Core.SignedIntegerLiteralContext ctx)
    {
        var digits = ctx.INTEGERLIT();
        var sign = (Antlr4.Runtime.Tree.ITerminalNode?)ctx.PLUS() ?? ctx.MINUS();
        return sign is not null && ArithmeticFormationRules.SignAbuts(sign.Symbol, digits.Symbol)
            ? sign.GetText() + digits.GetText()
            : digits.GetText();
    }
}
