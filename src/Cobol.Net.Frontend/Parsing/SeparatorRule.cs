// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Common;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// ⭐ THE §8.3.5 SEPARATOR-CONTEXT RULES, decided once over the whole token stream from the SOURCE CHARACTERS around
/// each token (kb/Work PB1394):
/// <list type="bullet">
/// <item>rule 2 — "The COBOL characters comma and semicolon, immediately followed by a space, are separators": a comma
/// or semicolon with no space after it is no separator, so <c>MOVE 1 TO N,M</c> names no second receiver
/// (<c>COBOLNET2631</c>);</item>
/// <item>rule 3 — "The COBOL character period, when followed by a space, is a separator": <c>DISPLAY N.DISPLAY M</c>
/// ends no sentence (<c>COBOLNET2632</c>);</item>
/// <item>rule 5 — "The opening delimiter shall be immediately preceded by a space, left parenthesis, or opening
/// pseudo-text delimiter. The closing delimiter shall be immediately followed by one of the separators space, comma,
/// semicolon, period, right parenthesis, or closing pseudo-text delimiter": <c>DISPLAY "AB"N</c>, <c>MOVE"AB" TO A</c>
/// and <c>W"AB"</c> beside a data item W (<c>COBOLNET2633</c>).</item>
/// </list>
/// </summary>
/// <remarks>
/// <para>⛔ WHY POST-LEX, FROM THE CHARACTERS, AND NOT IN THE GRAMMAR. The lexer skips the separator space, so the
/// parser sees the same token run whether or not a separator was written — <c>N,M</c> and <c>N, M</c>, <c>"AB"N</c>
/// and <c>"AB" N</c> are indistinguishable by the time any rule could ask. Every one of these rules is about the
/// CHARACTER next to a token, and the token's own source indices say exactly where that is, so the decision is made
/// here, once per token, for every statement and entry at once — the check that used to exist only as the parser's
/// recovery hint on the few statements whose parse the split happened to break ("Missing space before string
/// literal"). It is the sibling of <see cref="PictureSeparatorPeriodRule"/>, which decides the §13.18.40.3 SR7
/// question at a PICTURE string's right edge the same way, and it is reported through the same listener.</para>
/// <para>⚠ THE SPACE-LESS COMMA OF A NUMERIC LITERAL IS NOT THIS RULE'S. Under DECIMAL-POINT IS COMMA the comma is a
/// numeric literal's decimal point (§12.3.7.4 GR14 a)), and the lexer delivers <c>1,5</c> as INTEGERLIT COMMA
/// INTEGERLIT for the parser to assemble; whether that literal is legal is the numeric-literal rules' question (the
/// binder's one normalizer reports a comma decimal point without the clause). So a comma touching a following digit,
/// with a digit or a separator before it, is left alone here. A semicolon is never a decimal point.</para>
/// <para>⚠ A PERIOD, COMMA OR SEMICOLON INSIDE A LITERAL OR A PICTURE CHARACTER-STRING IS PART OF THAT TOKEN, never a
/// DOT / COMMA / SEMICOLON token, so rules 2 and 3 cannot misfire on one ("except when appearing in a literal", and
/// the PICMODE lexer owns the picture's own separators). A <c>;</c> with no space after it is a HIDDEN-channel token
/// (<c>SEMICOLON</c>) precisely so this rule can see it; the parser never does.</para>
/// <para>Edition-invariant: §8.3.5's text, and COBOL-85's, carry no edition qualifier.</para>
/// </remarks>
public static class SeparatorRule
{
    /// <summary>One violation: the token that carries it, its descriptor, and the message.</summary>
    public readonly record struct Violation(IToken Token, DiagnosticDescriptor Descriptor, string Message);

    /// <summary>Every §8.3.5 rule 2 / 3 / 5 violation in <paramref name="tokens"/> (all channels, in source order),
    /// judged against <paramref name="text"/>, the exact character sequence the tokens were lexed from.</summary>
    public static IEnumerable<Violation> Violations(string text, IList<IToken> tokens)
    {
        int reportedCloseStop = -2;   // a literal's closing-delimiter violation already covers the boundary after it
        foreach (var t in tokens)
        {
            switch (t.Type)
            {
                case CobolLexer.COMMA or CobolLexer.SUB_COMMA or CobolLexer.SEMICOLON or CobolLexer.SUB_SEMICOLON:
                    if (!IsSeparatorSpace(CharAt(text, t.StopIndex + 1)) && !IsDecimalComma(text, t))
                        yield return new(t, DiagnosticCatalog.SeparatorCommaWithoutSpace,
                            $"the {Name(t)} is not immediately followed by a space, so it is not a separator — ISO §8.3.5 "
                            + "rule 2 makes a comma or semicolon a separator only when a space follows it; write a space "
                            + "after it");
                    break;
                case CobolLexer.DOT:
                    if (!IsSeparatorSpace(CharAt(text, t.StopIndex + 1)))
                        yield return new(t, DiagnosticCatalog.SeparatorPeriodWithoutSpace,
                            "the period is not followed by a space, so it is not a separator period — ISO §8.3.5 rule 3 "
                            + "makes a period a separator only when a space follows it; write a space after it");
                    break;
                default:
                    if (!LiteralTokens.Types.Contains(t.Type)) break;
                    if (!OpeningDelimiterSeparated(text, t.StartIndex) && t.StartIndex - 1 != reportedCloseStop)
                        yield return new(t, DiagnosticCatalog.SeparatorLiteralDelimiter,
                            $"the literal {Abbreviated(t.Text)} begins immediately after '{(char)CharAt(text, t.StartIndex - 1)}' — "
                            + "ISO §8.3.5 rule 5 requires an opening delimiter to be immediately preceded by a space, a "
                            + "left parenthesis or an opening pseudo-text delimiter");
                    if (!ClosingDelimiterSeparated(text, t.StopIndex))
                    {
                        reportedCloseStop = t.StopIndex;
                        yield return new(t, DiagnosticCatalog.SeparatorLiteralDelimiter,
                            $"the literal {Abbreviated(t.Text)} is immediately followed by '{(char)CharAt(text, t.StopIndex + 1)}' — "
                            + "ISO §8.3.5 rule 5 requires a closing delimiter to be immediately followed by a space, "
                            + "comma, semicolon, period, right parenthesis or closing pseudo-text delimiter");
                    }
                    break;
            }
        }
    }

    /// <summary>The source character at <paramref name="index"/>, or -1 before the first or after the last.</summary>
    public static int CharAt(string text, int index) => (uint)index < (uint)text.Length ? text[index] : -1;

    /// <summary>The separator space as the lexer reads it — the characters of its skipped <c>WS</c> rule (the space and
    /// the line end: the line-entry stage leaves no tab or CR LF, kb/Work PB1800) — or the start or end of the text,
    /// which no character-string can extend past.</summary>
    private static bool IsSeparatorSpace(int c) => c is ' ' or '\n' or -1;

    /// <summary>§8.3.5 5): space, left parenthesis, or the opening pseudo-text delimiter <c>==</c>.</summary>
    private static bool OpeningDelimiterSeparated(string text, int start)
    {
        int before = CharAt(text, start - 1);
        return IsSeparatorSpace(before) || before == '(' || (before == '=' && CharAt(text, start - 2) == '=');
    }

    /// <summary>§8.3.5 5): space, comma, semicolon, period, right parenthesis, or the closing pseudo-text delimiter
    /// <c>==</c>. (A comma, semicolon or period with no space after it is rule 2's or 3's violation, reported on its
    /// own token.)</summary>
    private static bool ClosingDelimiterSeparated(string text, int stop)
    {
        int after = CharAt(text, stop + 1);
        return IsSeparatorSpace(after) || after is ',' or ';' or '.' or ')'
            || (after == '=' && CharAt(text, stop + 2) == '=');
    }

    /// <summary>A comma that is a numeric literal's decimal point under DECIMAL-POINT IS COMMA: a digit follows it, and a
    /// digit, a sign, a left parenthesis or a separator space precedes it (<c>1,5</c>, <c>,5</c>, <c>-,5</c>).</summary>
    private static bool IsDecimalComma(string text, IToken t)
    {
        if (t.Type is not (CobolLexer.COMMA or CobolLexer.SUB_COMMA)) return false;
        if (!char.IsAsciiDigit((char)Math.Max(0, CharAt(text, t.StopIndex + 1)))) return false;
        int before = CharAt(text, t.StartIndex - 1);
        return IsSeparatorSpace(before) || before is '+' or '-' or '(' || char.IsAsciiDigit((char)Math.Max(0, before));
    }

    private static string Name(IToken t) => t.Type is CobolLexer.COMMA or CobolLexer.SUB_COMMA ? "comma" : "semicolon";

    private static string Abbreviated(string raw) => CobolLiteral.Abbreviated(raw);
}
