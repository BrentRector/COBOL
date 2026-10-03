// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// ISO §13.18.40.3 SR7 — "If the symbol ',' or the symbol '.' is the last symbol of character-string-1, the
/// PICTURE clause shall be the last clause of the data description entry and shall be followed immediately
/// (without an intervening separator space) by the separator period." (kb/Work PB569.)
/// <para><b>WHY A TOKEN-LEVEL RULE, AND WHY HERE.</b> The PICMODE lexer rule (<c>PIC_STRING</c>) is the one place
/// character-string-1 is delimited, and it already decides two §8.3.5 questions at its right edge: a trailing '.'
/// followed by a space is the separator period (rule 3) and backs up to become the entry's DOT, and a trailing
/// ',' or ';' followed by a space is a separator (rule 2) and backs up to be skipped. After those trims the
/// token's last symbol is ',' or '.' in exactly two situations — the separator period follows it (the legal SR7
/// shape, NIST NC125A's <c>PIC 9,9,…,9,.</c>) or a separator comma/semicolon does (<c>PIC 999,, USAGE DISPLAY.</c>,
/// <c>PIC 999., VALUE ZERO.</c>, <c>PIC 999,; VALUE ZERO.</c>) — and the second is the violation. It used to be
/// read as "structurally impossible" for the '.' arm; it is not, since a '.' followed by ',' is an embedded
/// symbol to the greedy match. So the rule is decided from the SOURCE CHARACTERS the lexer delimited, once per
/// PICTURE clause, for every parent that writes one (data, report group and screen description entries): the
/// character immediately after character-string-1 shall be the separator period. A separator period
/// necessarily ends the entry, so "shall be the last clause" follows from "followed immediately by the
/// separator period" and needs no second test.</para>
/// </summary>
public static class PictureSeparatorPeriodRule
{
    /// <summary>Every <c>PIC_STRING</c> token in <paramref name="tokens"/> that violates SR7, judged against
    /// <paramref name="source"/>, the exact character sequence the tokens were lexed from.</summary>
    public static IEnumerable<IToken> Violations(string source, IList<IToken> tokens)
    {
        foreach (var t in tokens)
        {
            if (t.Type != CobolLexer.PIC_STRING || t.Channel == CobolLexer.ABSENT_DEBUG_LINE
                || t.Text is not { Length: > 0 } text) continue;   // an absent debugging line is a comment (kb/Work PB1705)
            if (text[^1] is not (',' or '.')) continue;
            if (SeparatorRule.CharAt(source, t.StopIndex + 1) != '.') yield return t;
        }
    }

    /// <summary>The COBOLNET2419 message for one violating character-string.</summary>
    public static string Message(IToken t)
        => $"the PICTURE character-string {t.Text} ends in the symbol '{t.Text![^1]}', so the PICTURE clause shall "
           + "be the last clause of the entry and be followed immediately, with no intervening separator, by the "
           + "separator period (ISO §13.18.40.3 SR7); a comma or semicolon followed by a space is a separator "
           + "(ISO §8.3.5 rule 2), not the end of the entry. Write the clause last and end it with '.', or drop "
           + "the trailing symbol.";
}
