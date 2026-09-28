// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// ⛔ THE POST-LEX TOKEN DECISIONS OF ONE COMPILATION GROUP — the ONE object every parse of that group's text applies
/// between lexing and parsing: the <c>&gt;&gt;COBOL-WORDS</c> retype (<see cref="CobolWordsRewriter"/>, ISO §7.3.10.4
/// GR2/GR3/GR4) and the §8.9 reservation gate (<see cref="ReservationGateRewriter"/>, kb/Work PB655).
/// <para>It exists because the decisions have more than one reader. The main parse applies them in
/// <c>Frontend.LexAndParse</c>, and the binder RE-LEXES source text for the D18 subscript / reference-modifier
/// segment and the D2 keyword-omitted argument list (<see cref="FragmentParse"/>). A fragment that lexed the text
/// afresh without them would read <c>T(GOBACK)</c> — GOBACK a declared data item at COBOL-85 — as the GOBACK
/// keyword, and a <c>&gt;&gt;COBOL-WORDS UNDEFINE</c>d word as its keyword again: the same text, two parses, two
/// answers. Carrying one value makes the fragment's token stream the main parse's by construction.</para>
/// </summary>
/// <param name="CobolWords">The group's <c>&gt;&gt;COBOL-WORDS</c> override (empty when none).</param>
/// <param name="FreedReservedWords">The reservation-gated words §8.9 leaves free at the compile edition that the
/// program declares — retyped to <c>IDENTIFIER</c> (upper-case spellings; empty when none).</param>
public sealed record TokenRetypes(CobolWordsMap CobolWords, IReadOnlySet<string> FreedReservedWords)
{
    /// <summary>No directive and no freed word — every parse's token stream is the lexer's, unchanged.</summary>
    public static readonly TokenRetypes None = new(CobolWordsMap.Empty, new HashSet<string>(StringComparer.Ordinal));

    /// <summary>Prime <paramref name="lexer"/> BEFORE any tokenization with this compile's answer to "can a '(' after
    /// this word open a SUBSCRIPT", which the lexer must decide before any parser predicate runs:
    /// <list type="bullet">
    /// <item>a de-reserved keyword (<c>&gt;&gt;COBOL-WORDS</c> UNDEFINE / SUBSTITUTE) may be used as a SUBSCRIPTED data
    /// name, so it becomes a trigger although the retype runs only after lexing;</item>
    /// <item>a word <paramref name="edition"/> RESERVES is never a data name (ISO §8.3.2.1 rule 1), so it stops being
    /// one — the '(' after a boolean operator at 2002+ groups a boolean sub-expression (§8.8.2 Table 4; kb/Work
    /// PB1465). The decision is <see cref="ReservedWordSet.AdmitsAsUserWord"/>, the SAME one the parser's
    /// <c>userWordHere</c> gate and the §8.9 funnel make, so <c>--permissive</c> keeps every word it keeps as a
    /// name a trigger too. A freed reservation-gated word (retyped to IDENTIFIER after lexing) is admitted, so it
    /// keeps its trigger.</item>
    /// </list></summary>
    public void PrimeLexer(CobolLexer lexer, EditionInfo edition)
    {
        if (!CobolWords.IsEmpty)
            lexer.SetCobolWordsDataNames(CobolWordsRewriter.DeReservedTokenTypes(CobolWords));
        lexer.SetReservedNonDataNames(ReservedNonDataNames(edition));
    }

    /// <summary>The SUBSCRIPT-trigger tokens <paramref name="edition"/> does not admit as user-defined words —
    /// computed once per edition. An intrinsic-function-name token is never in it: its '(' opens a keyword-omitted
    /// function call's argument capture (§8.4.3.2.3 SR2) whatever the word's reservation, and the set of such
    /// tokens is read off the GENERATED parser's <c>functionName</c> rule, never listed here.</summary>
    private static IReadOnlySet<int> ReservedNonDataNames(EditionInfo edition) =>
        s_reservedNonDataNames.GetOrAdd(edition, static e =>
        {
            var functionNames = FunctionNameTokens.Value;
            var set = new HashSet<int>();
            foreach (int t in CobolLexer.SubscriptTriggerTokens)
            {
                if (t == CobolLexer.IDENTIFIER || functionNames.Contains(t)) continue;
                if (WordOf(t) is { } word && !ReservedWordSet.Default.AdmitsAsUserWord(word, e)) set.Add(t);
            }
            return set;
        });

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<EditionInfo, IReadOnlySet<int>>
        s_reservedNonDataNames = new();

    private static readonly Lazy<Antlr4.Runtime.Misc.IntervalSet> FunctionNameTokens = new(() =>
    {
        var atn = CobolParserCore._ATN;
        return atn.NextTokens(atn.ruleToStartState[CobolParserCore.RULE_functionName]);
    });

    /// <summary>The COBOL word a keyword token spells: its symbolic name with '_' read as '-' and the generator's
    /// clash-avoiding trailing '_' dropped (the <c>cobol-words.json</c> token convention, e.g. <c>B_OR</c> → B-OR,
    /// <c>FULL_</c> → FULL).</summary>
    private static string? WordOf(int tokenType) =>
        CobolLexer.DefaultVocabulary.GetSymbolicName(tokenType) is { } name
            ? name.TrimEnd('_').Replace('_', '-')
            : null;

    /// <summary>Apply both retypes to the filled token stream. Byte-identical when <see cref="None"/>.</summary>
    public void Rewrite(CommonTokenStream tokens)
    {
        CobolWordsRewriter.Rewrite(tokens, CobolWords);
        ReservationGateRewriter.Retype(tokens, FreedReservedWords);
    }
}
