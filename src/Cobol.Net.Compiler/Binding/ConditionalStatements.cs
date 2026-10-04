// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Frozen;
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// ISO §14.5.1's classification of a written statement as CONDITIONAL or IMPERATIVE (kb/Work PB351): "An imperative
/// statement specifies an unconditional action to be taken by the runtime element or is a conditional statement that
/// is delimited by its explicit scope terminator, as specified in Table 12, Procedural statements" and "Any statement
/// with a conditional phrase that is not terminated by its explicit scope terminator is a conditional statement."
///
/// <para>⛔ BOTH HALVES ARE READ OFF THE PARSE, NEVER OFF A VERB LIST. A conditional phrase of Table 12 (AT END, INVALID
/// KEY, ON SIZE ERROR, ON OVERFLOW, ON EXCEPTION, AT END-OF-PAGE, THEN / ELSE, WHEN) always carries an
/// imperative-statement (or IF's statement-n) operand, which the grammar spells <c>statementBlock</c> — so "a conditional
/// phrase is written" is "the statement's own syntax holds a <c>statementBlock</c>" (nested statements are inside the
/// block and are not this statement's syntax). The explicit scope terminator is Table 12's third column, which for
/// every row that has one is <c>END-</c> followed by the statement name; <see cref="Table12StatementNames"/> already
/// resolves the name from the parse rule, so the terminator token is <c>END_</c> + that name. A new statement rule is
/// classified with no edit here. <c>Table12StatementNameDriftTests</c> re-derives the terminator column from the
/// scraped spec table, and holds the one statement whose operand is not a conditional phrase (PERFORM, whose END-PERFORM
/// the inline formats require) to its required terminator.</para>
/// </summary>
internal static class ConditionalStatements
{
    /// <summary>Table 12 statement name → its explicit scope terminator's token type, for every name the lexer
    /// vocabulary carries an <c>END_</c> token for.</summary>
    private static readonly FrozenDictionary<string, int> Terminators = BuildTerminators();

    private static FrozenDictionary<string, int> BuildTerminators()
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        var vocabulary = Core.DefaultVocabulary;
        for (int t = 1; t <= Core._ATN.maxTokenType; t++)
            if (vocabulary.GetSymbolicName(t) is { } name && name.StartsWith("END_", StringComparison.Ordinal))
                map[name[4..].Replace('_', ' ')] = t;
        return map.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>The explicit scope terminator token type Table 12 gives the statement named <paramref name="table12Name"/>,
    /// or null when the vocabulary has none (internal so the drift test asks the SAME lookup the pass uses).</summary>
    internal static int? TerminatorFor(string table12Name) =>
        Terminators.TryGetValue(table12Name, out int t) ? t : null;

    /// <summary>§14.5.1: whether <paramref name="s"/> is a CONDITIONAL statement — a conditional phrase is written and
    /// the statement's explicit scope terminator is not.</summary>
    public static bool IsConditional(Core.StatementContext s)
    {
        // NameOf asserts the one-rule-per-alternative shape LOUDLY, so it is asked first: a restructured `statement`
        // rule fails there instead of classifying every statement as imperative in silence.
        int? terminator = TerminatorFor(Table12StatementNames.NameOf(s));
        bool phrase = false, terminated = false;
        Scan(s.GetChild(0), terminator, ref phrase, ref terminated);
        return phrase && !terminated;
    }

    /// <summary>Walk the statement's OWN syntax: a <c>statementBlock</c> is a conditional phrase's operand and is not
    /// descended into (the statements inside it are other statements, whose terminators are theirs — §14.5.3.2 1)).</summary>
    private static void Scan(IParseTree node, int? terminator, ref bool phrase, ref bool terminated)
    {
        for (int i = 0; i < node.ChildCount; i++)
        {
            switch (node.GetChild(i))
            {
                case Core.StatementBlockContext:
                    phrase = true;
                    break;
                case ITerminalNode t:
                    if (terminator is int tt && t.Symbol.Type == tt) terminated = true;
                    break;
                case var child:
                    Scan(child, terminator, ref phrase, ref terminated);
                    break;
            }
        }
    }
}
