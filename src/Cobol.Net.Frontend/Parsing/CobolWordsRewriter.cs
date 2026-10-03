// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using CobolNet.Editions;
using CobolNet.Frontend.Generated;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// The <c>&gt;&gt;COBOL-WORDS</c> token retype (ISO §7.3.10.4 GR2/GR3/GR4), applied by the lexer to each token as it
/// is emitted (<see cref="Plan"/>, kb/Work PB1372) per the <see cref="CobolWordsMap"/>, so the static ANTLR lexer
/// needs no per-group regeneration (the owner's recorded direction) and every lexer decision keyed on a keyword —
/// PIC's mode switch, the FUNCTION argument region, the SUBSCRIPT trigger — reads the word the DIRECTIVE made of it.
/// Two disjoint retypes: a synonym (EQUATE literal-2 / SUBSTITUTE literal-5) becomes its canonical keyword's token
/// type, and a de-reserved word (UNDEFINE literal-3 / SUBSTITUTE literal-4) becomes an <c>IDENTIFIER</c>, so it
/// drops out of its keyword syntax and is usable as a user word.
/// <para><b>This retype is HALF the mechanism, and it cannot be the other half</b> (kb/Work PB250). It can only
/// reach a word the lexer makes a keyword TOKEN; a word that lexes as a plain IDENTIFIER has no token type to
/// retype, so the retype is a silent no-op for it in BOTH directions. Measured against ISO §8.9 ∪ §8.10, 88 words
/// are in that class — ANYCASE and LOCALE among them, which is why <c>&gt;&gt;COBOL-WORDS EQUATE "LOCALE" …</c>
/// used to make TEST-NUMVAL-C reject legal source. Those words, and every intrinsic-function name, are reached at
/// the by-name classification points through <see cref="CobolWordsMap.Resolve"/> — the ONE rule both halves
/// share. <see cref="CobolWordsMap.Empty"/> ⇒ a no-op (byte-identical token stream). Design SSOT:
/// <c>docs/rearchitecture/DESIGN-cobol-words-directive.md</c>.</para>
/// </summary>
public static class CobolWordsRewriter
{
    /// <summary>The KEYWORD token types the directive DE-RESERVES (UNDEFINE literal-3 / SUBSTITUTE literal-4) — the
    /// types <see cref="Plan.Apply"/> retypes to <c>IDENTIFIER</c> when the token's text is the de-reserved word.
    /// Empty when no de-reserved word is a keyword lexer token.</summary>
    public static IReadOnlySet<int> DeReservedTokenTypes(CobolWordsMap map)
    {
        var types = new HashSet<int>();
        if (!map.IsEmpty)
            foreach (string word in map.DeReserved)
                if (CobolKeywordTokens.TryTokenType(word, out int kt))
                    types.Add(kt);
        return types;
    }

    /// <summary>
    /// The canonical COBOL word an ALREADY-LEXED token denotes under <paramref name="map"/> (UPPER-CASE), or null
    /// when the directive de-reserved it and it is a user-defined word now.
    /// <para>⛔ THE ONE PLACE THAT KNOWS WHICH WORDS THIS REWRITER ALREADY RESOLVED, and the reason a bare
    /// <see cref="CobolWordsMap.Resolve"/> on token text is WRONG. <see cref="Rewrite"/> applies the directive to
    /// every word it can reach (as the lexer emits it) and RE-SPELLS it canonically, so a token that is not an <c>IDENTIFIER</c> has been
    /// resolved already: resolving it a second time reads a SUBSTITUTE'd literal-4 — which is canonical AND
    /// de-reserved — as "not a keyword", and loses the synonym literal-5 the user legally wrote (measured:
    /// <c>SUBSTITUTE "LEADING" BY "LEFTMOST"</c> then <c>FUNCTION TRIM(X LEFTMOST)</c>). Only an IDENTIFIER can
    /// still carry an unresolved word — either a synonym for a keyword the lexer does not tokenize, or a
    /// de-reserved keyword this rewriter just turned into one. The directive is applied to a word EXACTLY ONCE
    /// (kb/Work PB250).</para>
    /// </summary>
    public static string? CanonicalWordOf(IToken? tok, CobolWordsMap map)
        => tok is null ? null
         : map.IsEmpty || tok.Type != CobolKeywordTokens.IdentifierType ? tok.Text.ToUpperInvariant()
         : map.Resolve(tok.Text.ToUpperInvariant());

    /// <summary>True when an already-lexed <paramref name="tok"/> denotes <paramref name="keyword"/> — the
    /// token-aware twin of <see cref="CobolWordsMap.Is"/>, carrying the same once-only guarantee as
    /// <see cref="CanonicalWordOf"/>. Allocation-free when the group has no directive.</summary>
    public static bool TokenIs(IToken? tok, string keyword, CobolWordsMap map)
    {
        if (tok is null) return false;
        if (map.IsEmpty || tok.Type != CobolKeywordTokens.IdentifierType)
            return string.Equals(tok.Text, keyword, StringComparison.OrdinalIgnoreCase);
        return map.Is(tok.Text, keyword);
    }

    /// <summary>
    /// ⛔ THE ONE RETYPE OF A GROUP'S WORDS, APPLIED BY THE LEXER AS IT EMITS EACH TOKEN (kb/Work PB1372, PB1915) —
    /// <see cref="CobolLexer"/> holds one (<c>SetCobolWords</c>, handed in by <see cref="TokenRetypes.PrimeLexer"/>)
    /// and calls <see cref="Apply"/> in <c>NextToken</c>, BEFORE it records the token as the "previous token" its
    /// context actions read and BEFORE it acts on the token's own mode. A retype that ran after lexing (the shape this
    /// replaced) could reach nothing the lexer had already acted on: a synonym of PIC/PICTURE never entered PICMODE, a
    /// synonym of FUNCTION never opened an argument region, a de-reserved PICTURE still swallowed the next word as a
    /// picture string, and every later reader of the filled stream (<c>DebuggingLineRewriter</c>'s unit boundaries)
    /// saw the words the DIRECTIVE had changed. One retype, applied where the decisions are made, leaves no later
    /// stage to forget it. Two disjoint retypes, both ISO §7.3.10.4:
    /// <list type="bullet">
    /// <item>SYNONYM (GR2 EQUATE literal-2 / GR4 SUBSTITUTE literal-5): an <c>IDENTIFIER</c> whose text is a synonym
    /// becomes the canonical reserved/context word's token type, spelled canonically.</item>
    /// <item>DE-RESERVED (GR3 UNDEFINE literal-3 / GR4 SUBSTITUTE literal-4): a token of the de-reserved word's keyword
    /// type whose TEXT is that word becomes an <c>IDENTIFIER</c> keeping its source spelling. The text test is
    /// load-bearing (kb/Work PB250): one token type can carry several COBOL words (<c>PIC : 'PICTURE' | 'PIC'</c>),
    /// while GR3/GR4 de-reserve exactly the one word the literal names.</item>
    /// </list>
    /// <see cref="Empty"/> is the no-directive plan: <see cref="Apply"/> is a no-op.</summary>
    public sealed class Plan
    {
        /// <summary>The no-directive plan — every token passes through unchanged.</summary>
        public static readonly Plan Empty = new(new Dictionary<string, (int, string)>(StringComparer.OrdinalIgnoreCase),
            new HashSet<int>(), CobolWordsMap.Empty);

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CobolWordsMap, Plan> Cache = new();

        private readonly Dictionary<string, (int Type, string Canonical)> _synonymToType;
        private readonly HashSet<int> _deReservedTypes;
        private readonly CobolWordsMap _map;

        private Plan(Dictionary<string, (int Type, string Canonical)> synonymToType, HashSet<int> deReservedTypes,
            CobolWordsMap map)
        {
            _synonymToType = synonymToType;
            _deReservedTypes = deReservedTypes;
            _map = map;
        }

        /// <summary>The plan for <paramref name="map"/> (memoized per map; <see cref="Empty"/> when the map reaches no
        /// word the lexer makes a keyword token — the intrinsic-name and phrase-word half is the by-name resolution of
        /// <see cref="CobolWordsMap.Resolve"/>).</summary>
        public static Plan For(CobolWordsMap map)
        {
            if (map.IsEmpty) return Empty;
            return Cache.GetValue(map, static m =>
            {
                // SYNONYM: the synonym text → the canonical word's token type (only when the canonical is a
                // reserved/context keyword; a canonical intrinsic-function name has no token type and is handled by
                // the binder's by-name resolution).
                var synonymToType = new Dictionary<string, (int Type, string Canonical)>(StringComparer.OrdinalIgnoreCase);
                foreach (var (synonym, canonical) in m.Synonyms)
                    if (CobolKeywordTokens.TryTokenType(canonical, out int kt))
                        synonymToType[synonym] = (kt, canonical);
                var deReservedTypes = new HashSet<int>(DeReservedTokenTypes(m));
                return synonymToType.Count == 0 && deReservedTypes.Count == 0
                    ? Empty
                    : new Plan(synonymToType, deReservedTypes, m);
            });
        }

        /// <summary>True when the plan retypes nothing.</summary>
        public bool IsEmpty => _synonymToType.Count == 0 && _deReservedTypes.Count == 0;

        /// <summary>Retype <paramref name="tok"/> in place when the directive changes it. Only a token on the default
        /// channel is touched (hidden markers and whitespace never are).</summary>
        public void Apply(CommonToken tok)
        {
            if (tok.Channel != Lexer.DefaultTokenChannel) return;
            if (tok.Type == CobolKeywordTokens.IdentifierType)
            {
                // A synonym written as a user word takes over its canonical keyword — spelled canonically so any
                // downstream GetText() sees the real keyword (the parser matches by type; text is for fidelity).
                if (_synonymToType.Count != 0 && _synonymToType.TryGetValue(tok.Text, out var target))
                {
                    tok.Type = target.Type;
                    tok.Text = target.Canonical;
                }
            }
            else if (_deReservedTypes.Contains(tok.Type) && _map.DeReserved.Contains(tok.Text))
            {
                // A de-reserved keyword becomes a user word — keep the source spelling (it is the data-name now).
                tok.Type = CobolKeywordTokens.IdentifierType;
            }
        }
    }
}
