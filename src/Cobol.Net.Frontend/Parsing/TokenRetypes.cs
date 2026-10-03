// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// ⛔ THE TOKEN DECISIONS OF ONE COMPILATION GROUP — the ONE object every parse of that group's text applies: the
/// <c>&gt;&gt;COBOL-WORDS</c> retype (<see cref="CobolWordsRewriter.Plan"/>, ISO §7.3.10.4 GR2/GR3/GR4), which the LEXER
/// applies to each token as it emits it (<see cref="PrimeLexer"/> hands it over), and the §8.9 reservation gate
/// (<see cref="ReservationGateRewriter"/>, kb/Work PB655), which retypes the filled stream between lexing and parsing.
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

    /// <summary>Prime <paramref name="lexer"/> BEFORE any tokenization with this compile's decisions about words, which
    /// the lexer must take before any parser predicate runs:
    /// <list type="bullet">
    /// <item>the <c>&gt;&gt;COBOL-WORDS</c> retype (kb/Work PB1372): the lexer retypes each synonym to its keyword and
    /// each de-reserved keyword to <c>IDENTIFIER</c> AS IT EMITS THE TOKEN, so every decision it takes from a keyword
    /// (PIC's mode switch, the FUNCTION argument region, the SUBSCRIPT trigger) reads the word the directive made of
    /// it — a de-reserved keyword is a data-name, so its '(' opens a SUBSCRIPT;</item>
    /// <item>a word <paramref name="edition"/> RESERVES is a data name only where the PROGRAM DECLARES it as one (ISO
    /// §8.3.2.1 rule 1: reserved words shall not be user-defined words, and <c>--permissive</c> admits the word an
    /// edition added as the user-defined word an older program declared). Until a program declares it, a reserved
    /// word's '(' is no subscript — the '(' after a boolean operator at 2002+ groups a boolean sub-expression (§8.8.2
    /// Table 4; kb/Work PB1465, PB1669). The declaration is the parser's own finding
    /// (<see cref="FreedReservedWords"/>, the §8.9 gate), so the SAME decision serves the lexer, the parser's
    /// <c>userWordHere</c> gate and the funnel; <see cref="LexesDifferentlyFrom"/> tells the frontend when a parse
    /// found a declaration that changes which '(' open a SUBSCRIPT, and the text is lexed again.</item>
    /// </list></summary>
    public void PrimeLexer(CobolLexer lexer, EditionInfo edition)
    {
        lexer.SetCobolWords(CobolWordsRewriter.Plan.For(CobolWords));
        lexer.SetReservedNonDataNames(ReservedNonDataNames(edition, FreedReservedWords));
    }

    /// <summary>True when <paramref name="other"/>'s decisions prime the lexer differently from this one's at
    /// <paramref name="edition"/> — the declared words of a parse (<see cref="FreedReservedWords"/>) include one whose
    /// '(' the lexer refused as a subscript trigger. The frontend then lexes and parses the text again with them
    /// (kb/Work PB1669); a program that declares no reserved word — nearly every program — never does.</summary>
    public bool LexesDifferentlyFrom(TokenRetypes other, EditionInfo edition) =>
        !ReservedNonDataNames(edition, FreedReservedWords).SetEquals(ReservedNonDataNames(edition, other.FreedReservedWords));

    /// <summary>The SUBSCRIPT-trigger tokens whose word <paramref name="edition"/> RESERVES (§8.9 at the edition's year,
    /// <b>both axes off</b>: <c>--permissive</c> does not make an undeclared reserved word a name) and the program has
    /// not declared (<paramref name="declared"/>, upper-case). An intrinsic-function-name token is never in it: its '('
    /// opens a keyword-omitted function call's argument capture (§8.4.3.2.3 SR2) whatever the word's reservation, and
    /// the set of such tokens is read off the GENERATED parser's <c>functionName</c> rule, never listed here.</summary>
    private static IReadOnlySet<int> ReservedNonDataNames(EditionInfo edition, IReadOnlySet<string> declared)
    {
        var reserved = s_reservedNonDataNames.GetOrAdd(edition.Year, static year =>
        {
            var strict = EditionInfo.Of(year);
            var functionNames = FunctionNameTokens.Value;
            var set = new HashSet<int>();
            foreach (int t in CobolLexer.SubscriptTriggerTokens)
            {
                if (t == CobolLexer.IDENTIFIER || functionNames.Contains(t)) continue;
                if (WordOf(t) is { } word && !ReservedWordSet.Default.AdmitsAsUserWord(word, strict)) set.Add(t);
            }
            return set;
        });
        if (declared.Count == 0) return reserved;
        var remaining = new HashSet<int>(reserved);
        remaining.RemoveWhere(t => WordOf(t) is { } word && declared.Contains(word));
        return remaining;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, IReadOnlySet<int>>
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

    /// <summary>Apply the §8.9 reservation-gate retype to the filled token stream (the <c>&gt;&gt;COBOL-WORDS</c> retype
    /// is already done: the lexer applied it as it emitted each token). Byte-identical when <see cref="None"/>.</summary>
    public void Rewrite(CommonTokenStream tokens)
    {
        ReservationGateRewriter.Retype(tokens, FreedReservedWords);
    }
}
