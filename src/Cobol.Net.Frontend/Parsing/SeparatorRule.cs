// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Common;
using CobolNet.Editions;
using CobolNet.Editions.Diagnostics;
using CobolNet.Frontend.Expressions;
using CobolNet.Frontend.Generated;

using Core = CobolNet.Frontend.Generated.CobolParserCore;

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
/// and <c>W"AB"</c> beside a data item W (<c>COBOLNET2633</c>);</item>
/// <item>the three operator clauses that state their OWN separator context — §8.7.1 "[the arithmetic operators are
/// COBOL characters] that shall be preceded by a space and followed by a space except that no space is required
/// between a left parenthesis and a unary operator or between a unary operator and a left parenthesis", §8.7.3 "The
/// concatenation operator is the COBOL character '&amp;', which shall be immediately preceded and followed by a
/// separator space" and §8.7.4 "The invocation operator is the two contiguous COBOL characters '::', which shall be
/// immediately preceded and followed by a separator space": <c>SPACE&amp;SPACE</c>, <c>O:: "M"</c>, <c>O ::"M"</c>,
/// <c>1*2</c>, <c>(A)+(B)</c> and <c>-Y</c> (<c>COBOLNET2993</c>; kb/Work PB1430, PB2017). §8.7.3 and §8.7.4 are asked
/// by <see cref="Violations"/> over the token stream; §8.7.1 by <see cref="ArithmeticOperatorViolations"/> over the
/// parse tree, because only the tree says whether a <c>+</c> or <c>-</c> is a binary operator, a unary operator or
/// no operator at all (a numeric literal's own sign, a <c>LINE PLUS</c> screen clause).</item>
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
/// INTEGERLIT for the parser to assemble (a list region's signed <c>-1,5</c> is one SIGNED_DECIMALLIT token, kb/Work
/// PB2506, so no COMMA token reaches this rule for it); whether that literal is legal is the numeric-literal rules' question (the
/// binder's one normalizer reports a comma decimal point without the clause). So a comma touching a following digit,
/// with a digit or a separator before it, is left alone here. A semicolon is never a decimal point.</para>
/// <para>⚠ A PERIOD, COMMA OR SEMICOLON INSIDE A LITERAL OR A PICTURE CHARACTER-STRING IS PART OF THAT TOKEN, never a
/// DOT / COMMA / SEMICOLON token, so rules 2 and 3 cannot misfire on one ("except when appearing in a literal", and
/// the PICMODE lexer owns the picture's own separators). A <c>;</c> with no space after it is a HIDDEN-channel token
/// (<c>SEMICOLON</c>) precisely so this rule can see it; the parser never does.</para>
/// <para>⚠ ONE REPORT PER BOUNDARY. An operator touching a literal breaks two rules at one boundary — §8.3.5 5) for the
/// literal's delimiter and §8.7.3 / §8.7.4 for the operator. The operator's clause is the specific one (it names the
/// very character that touches), so the operator reports the boundary and the literal's delimiter check stands down
/// for it: <c>O ::"M"</c> is one COBOLNET2993, never a COBOLNET2633 beside it.</para>
/// <para>Edition-invariant: §8.3.5's text, and COBOL-85's, carry no edition qualifier; §8.7.3 and §8.7.4 qualify no
/// edition either (the operators themselves are 2002 introductions, which <c>VersionConformancePass</c> gates on
/// recognition).</para>
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
        for (int i = 0; i < tokens.Count; i++)
        {
            var t = tokens[i];
            if (t.Channel == CobolLexer.ABSENT_DEBUG_LINE) continue;   // a debugging line that is a comment (kb/Work PB1705)
            switch (t.Type)
            {
                case CobolLexer.AMPERSAND or CobolLexer.COLONCOLON:
                    if (OperatorSpacingMessage(text, t) is { } message)
                        yield return new(t, DiagnosticCatalog.SeparatorOperatorSpacing, message);
                    break;
                case CobolLexer.COMMA or CobolLexer.SEMICOLON:
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
                    if (!OpeningDelimiterSeparated(text, t.StartIndex) && t.StartIndex - 1 != reportedCloseStop
                        && !IsOperatorTouching(tokens, i - 1, t.StartIndex - 1))
                        yield return new(t, DiagnosticCatalog.SeparatorLiteralDelimiter,
                            $"the literal {Abbreviated(t.Text)} begins immediately after '{(char)CharAt(text, t.StartIndex - 1)}' — "
                            + "ISO §8.3.5 rule 5 requires an opening delimiter to be immediately preceded by a space, a "
                            + "left parenthesis or an opening pseudo-text delimiter");
                    if (!ClosingDelimiterSeparated(text, t.StopIndex) && !IsOperatorTouching(tokens, i + 1, t.StopIndex + 1))
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

    /// <summary>The §8.7.3 / §8.7.4 verdict on a concatenation or invocation operator: null when a separator space
    /// immediately precedes AND follows it, otherwise the message naming the side (or sides) that touch.</summary>
    private static string? OperatorSpacingMessage(string text, IToken op)
    {
        var (name, clause) = op.Type == CobolLexer.COLONCOLON
            ? ("invocation operator", "§8.7.4")
            : ("concatenation operator", "§8.7.3");
        return OperatorSpacingMessage(text, op, name, clause, parenthesisAdmitted: false);
    }

    /// <summary>The ONE spacing verdict of the operator clauses (§8.7.1, §8.7.3, §8.7.4): null when a separator space
    /// immediately precedes AND follows <paramref name="op"/>, otherwise the message naming the side (or sides) that
    /// touch. <paramref name="parenthesisAdmitted"/> is §8.7.1's one exception — a UNARY arithmetic operator needs no
    /// space against a left parenthesis, on either side — so a '(' stands in for the space there. A comma or semicolon
    /// immediately followed by a space is "a separator that may be used anywhere the separator space is used"
    /// (§8.3.5 2)), so <c>1 *, 1</c> follows its operator with one — while <c>1 *,1</c> does not: that comma is the
    /// literal's decimal point (<see cref="IsDecimalComma"/>), no separator.</summary>
    private static string? OperatorSpacingMessage(string text, IToken op, string name, string clause,
        bool parenthesisAdmitted)
    {
        bool before = IsSeparatorSpace(CharAt(text, op.StartIndex - 1))
            || (parenthesisAdmitted && CharAt(text, op.StartIndex - 1) == '(');
        int next = CharAt(text, op.StopIndex + 1);
        bool after = IsSeparatorSpace(next) || (parenthesisAdmitted && next == '(')
            || (next is ',' or ';' && IsSeparatorSpace(CharAt(text, op.StopIndex + 2)));   // §8.3.5 2): usable anywhere the space is
        if (before && after) return null;
        var (missing, fix) = (before, after) switch
        {
            (false, false) => ("preceded or followed", "on both sides of it"),
            (false, true) => ("preceded", "before it"),
            _ => ("followed", "after it"),
        };
        string what = parenthesisAdmitted ? "a separator space or a left parenthesis" : "a separator space";
        return $"the {name} '{op.Text}' is not immediately {missing} by {what} — ISO {clause} requires it to be "
            + $"immediately preceded and followed by {what}; write a space {fix}";
    }

    /// <summary>⭐ §8.7.1 — "[the arithmetic operators are COBOL characters] that shall be preceded by a space and followed
    /// by a space except that no space is required between a left parenthesis and a unary operator or between a unary
    /// operator and a left parenthesis" — decided over the PARSE TREE of <paramref name="tree"/>, lexed from
    /// <paramref name="text"/>, in source order (kb/Work PB2017).
    /// <para>⛔ WHY THE TREE, WHEN THE REST OF THIS CLASS READS TOKENS. Whether a <c>+</c> or <c>-</c> is a BINARY
    /// operator (spaced on both sides), a UNARY operator (a left parenthesis may stand where the space does) or no
    /// operator at all is a question of grammar, not of the character beside it: <c>N -1</c> is a subtraction written
    /// without its second space, <c>-1</c> alone is a numeric literal whose sign is its leftmost character
    /// (§8.3.3.3.2 2) — the ONE contiguity test, <see cref="ArithmeticFormationRules.SignIsPartOfLiteral"/>), and the
    /// same <c>+</c> in <c>LINE + 1</c> is a screen clause's relative-line symbol. Only the expression tiers know, so the
    /// verdict is read off <c>addOp</c> / <c>mulOp</c> / <c>POWER</c> under <c>additiveExpression</c>,
    /// <c>multiplicativeExpression</c>, <c>powerExpression</c> (binary) and <c>unaryExpression</c> (unary) — every
    /// arithmetic expression of every statement, subscript, reference modifier and function argument at once, with no
    /// consumer to forget.</para>
    /// <para>Tokens are judged by their own source indices, so the tree may come from the unit's text or from a
    /// directive operand lexed apart (kb/Work PB1616) — each caller passes the text its tree was lexed from.</para></summary>
    public static IReadOnlyList<Violation> ArithmeticOperatorViolations(string text, IParseTree tree)
    {
        List<Violation>? found = null;
        void Judge(IToken? op, string name, bool unary)
        {
            if (op is null || op.StartIndex < 0) return;   // a missing token of an error-recovered parse has no characters
            if (OperatorSpacingMessage(text, op, name, "§8.7.1", parenthesisAdmitted: unary) is { } message)
                (found ??= []).Add(new(op, DiagnosticCatalog.SeparatorOperatorSpacing, message));
        }

        var pending = new Stack<IParseTree>();
        pending.Push(tree);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (node is ITerminalNode) continue;
            switch (node)
            {
                case Core.AdditiveExpressionContext add:
                    foreach (var op in add.addOp()) Judge(op.Start, "binary arithmetic operator", unary: false);
                    break;
                case Core.MultiplicativeExpressionContext mul:
                    foreach (var op in mul.mulOp()) Judge(op.Start, "binary arithmetic operator", unary: false);
                    break;
                case Core.PowerExpressionContext pow:
                    foreach (var op in pow.POWER()) Judge(op.Symbol, "binary arithmetic operator", unary: false);
                    break;
                case Core.UnaryExpressionContext u when u.addOp() is { } sign
                        && !ArithmeticFormationRules.SignIsPartOfLiteral(sign.Start, u.unaryExpression()):
                    Judge(sign.Start, "unary arithmetic operator", unary: true);
                    break;
            }
            for (int i = node.ChildCount - 1; i >= 0; i--) pending.Push(node.GetChild(i));
        }
        if (found is null) return [];
        found.Sort(static (a, b) => a.Token.StartIndex.CompareTo(b.Token.StartIndex));
        return found;
    }

    /// <summary>Whether <paramref name="tokens"/>[<paramref name="at"/>] is a §8.7.3 / §8.7.4 operator whose own
    /// characters include <paramref name="boundary"/> — a boundary that operator's report already covers.</summary>
    private static bool IsOperatorTouching(IList<IToken> tokens, int at, int boundary) =>
        (uint)at < (uint)tokens.Count
        && tokens[at] is { Type: CobolLexer.AMPERSAND or CobolLexer.COLONCOLON } op
        && boundary >= op.StartIndex && boundary <= op.StopIndex;

    /// <summary>The source character at <paramref name="index"/>, or -1 before the first or after the last.</summary>
    public static int CharAt(string text, int index) => (uint)index < (uint)text.Length ? text[index] : -1;

    /// <summary>The separator space as the lexer reads it — the characters of its skipped <c>WS</c> rule (the space and
    /// the line end: the line-entry stage leaves no tab or CR LF, kb/Work PB1800) — or the start or end of the text,
    /// which no character-string can extend past.</summary>
    private static bool IsSeparatorSpace(int c) => c == -1 || CobolSpace.IsSeparator((char)c);

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
    /// digit, an operator character, a left parenthesis or a separator space precedes it (<c>1,5</c>, <c>,5</c>,
    /// <c>-,5</c>, <c>*,5</c>, <c>=,5</c>). §8.3.3.3.2 3) lets the decimal point be the leftmost character of a literal,
    /// so a literal that begins with it can follow anything that ends a character-string; an operator touching it is
    /// the OPERATOR's violation (§8.7.1, <see cref="ArithmeticOperatorViolations"/>), and calling the comma a missing
    /// separator advises a space that changes the program's meaning: <c>1 *,1</c> is 1 times 0,1, and <c>1 *, 1</c> —
    /// conforming, clean — is 1 times 1 (kb/Work PB2017).</summary>
    private static bool IsDecimalComma(string text, IToken t)
    {
        if (t.Type != CobolLexer.COMMA) return false;
        if (!char.IsAsciiDigit((char)Math.Max(0, CharAt(text, t.StopIndex + 1)))) return false;
        int before = CharAt(text, t.StartIndex - 1);
        return IsSeparatorSpace(before) || before is '+' or '-' or '*' or '/' or '=' or '<' or '>' or '('
            || char.IsAsciiDigit((char)Math.Max(0, before));
    }

    private static string Name(IToken t) => t.Type == CobolLexer.COMMA ? "comma" : "semicolon";

    private static string Abbreviated(string raw) => CobolLiteral.Abbreviated(raw);
}
