// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Lays a program out in FIXED-form reference format, for a test whose subject is a literal (or any token) too long
/// for one line. ISO §6.1 3) a) bounds a FREE-form line at 255 character positions ("ranging from a minimum of 0 to a
/// maximum of 255"; kb/Work PB1496, COBOLNET2653), so an 8,192-position literal written on one free-form line is not
/// a program the standard allows — the tests that build one programmatically must continue it, as fixed form does
/// (§6.3.5 / §6.5 4) and 8)): the continued line fills to margin R and ends in the floating literal continuation
/// indicator, and the continuation line resumes the literal at a quotation symbol. §6.1 2) b): after logical conversion
/// the line may be as long as it
/// likes, which is exactly what the literal-length rule (§8.3.3.2.3 SR1 and its siblings) is asked about.
/// </summary>
internal static class FixedFormLayout
{
    private const int TextStart = 7;    // 0-based index of position 8, the first program-text position
    private const int MarginR = 72;     // 0-based length of a line filled to position 72

    /// <summary>The program <paramref name="logicalProgram"/> (one logical line per <c>\n</c>, indentation kept) as a
    /// fixed-form source that starts with its own <c>&gt;&gt;SOURCE FORMAT FIXED</c> directive, so no reference-format
    /// detection is involved.</summary>
    public static string Source(string logicalProgram)
    {
        var sb = new StringBuilder(">>SOURCE FORMAT FIXED\n");
        foreach (string logical in logicalProgram.Split('\n'))
            foreach (string physical in LayLine(logical.TrimEnd()))
                sb.Append(physical).Append('\n');
        return sb.ToString();
    }

    private static IEnumerable<string> LayLine(string text)
    {
        int indent = text.Length - text.TrimStart().Length;
        var lines = new List<string>();
        string cur = new(' ', TextStart + indent);
        bool empty = true;
        foreach (string token in Tokenize(text.TrimStart()))
        {
            if (cur.Length + (empty ? 0 : 1) + token.Length <= MarginR)
            {
                cur += (empty ? "" : " ") + token;
                empty = false;
                continue;
            }
            if (!empty)
            {
                lines.Add(cur);
                cur = new string(' ', TextStart + 4);
                empty = true;
            }
            if (cur.Length + token.Length <= MarginR)
            {
                cur += token;
                empty = false;
                continue;
            }

            // A token longer than a line is a literal: fill the line to margin R and continue it with the FLOATING
            // literal continuation indicator — the opening quotation symbol and a hyphen end the line (§6.2.3.1, §6.5
            // 4)), and the next line begins with the quotation symbol (§6.2.3.2 SR6, §6.5 8)). It is the one form that
            // continues EVERY literal kind: a national literal may be continued only so (§6.3.5 2)), and the fixed
            // indicator is obsolete at 2023 (Annex F.2 item 4). A continued line never ends in content that is a
            // quotation symbol, which would read as the first half of a doubled one.
            char quote = token[token.IndexOfAny(['"', '\''])];
            string rest = token;
            while (cur.Length + rest.Length > MarginR)
            {
                int take = MarginR - cur.Length - 2;   // two positions for the indicator
                if (rest[take - 1] == quote) take--;
                lines.Add(cur + rest[..take] + quote + "-");
                rest = rest[take..];
                cur = new string(' ', TextStart + 4) + quote;
            }
            cur += rest;
            empty = false;
        }
        lines.Add(cur);
        return lines;
    }

    /// <summary>The space-separated tokens of a line, a quoted literal being one token whatever it holds.</summary>
    private static IEnumerable<string> Tokenize(string s)
    {
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == ' ') { i++; continue; }
            int j = i;
            char quote = '\0';
            while (j < s.Length && (s[j] != ' ' || quote != '\0'))
            {
                if (quote == '\0' && s[j] is '"' or '\'') quote = s[j];
                else if (quote != '\0' && s[j] == quote) quote = '\0';
                j++;
            }
            yield return s[i..j];
            i = j;
        }
    }
}
