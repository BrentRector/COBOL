// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Generated;
using CobolNet.Runtime;

namespace CobolNet.Frontend.Parsing;

/// <summary>
/// The reverse map <c>keyword word → ANTLR lexer token type</c> — the question "what token does the lexer make
/// of this word?", answered for the <c>&gt;&gt;COBOL-WORDS</c> lexer retype (retype IDENTIFIER→keyword for
/// EQUATE/SUBSTITUTE, keyword→IDENTIFIER for UNDEFINE/SUBSTITUTE) and for the compiler's SR3/SR4 category
/// validation. Intrinsic function names are NOT here — they are IDENTIFIERs (see <c>IntrinsicCatalog</c>), a
/// separate category.
/// <para><b>⛔ THE INVARIANT THIS CLASS USED TO ASSERT IS FALSE, AND THE FALSE ONE COST A SILENT BUG.</b> It
/// read "Every reserved word and context-sensitive word is a literal lexer token", and the whole map was built
/// from <see cref="Antlr4.Runtime.IVocabulary.GetLiteralName"/> alone. Measured against ISO §8.9 ∪ §8.10 (552
/// words), that walk reached 447: <b>17 words are lexed as a keyword token yet have no literal NAME</b>, because
/// ANTLR publishes a literal name only for a token defined by exactly ONE literal — a multi-spelling rule
/// (<c>PIC : 'PICTURE' | 'PIC'</c>; until kb/Work PB510 also <c>ZERO : 'ZERO' | 'ZEROS' | 'ZEROES'</c> and the
/// figurative plurals, now one token per word) or a word several lexer MODES spell (<c>IS</c>/<c>IN</c>/<c>OF</c>/
/// <c>ALL</c>) publishes none, so <c>&gt;&gt;COBOL-WORDS UNDEFINE "ZERO"</c> was silently inert. A further <b>88 words are no lexer token at all</b> (ANYCASE, LOCALE, HEX, NAT, ANUM, BYTE,
/// CURRENT, ACTIVATING, NESTED, STACK, TOP-LEVEL, the LC_ categories, UCS-4/UTF-8/UTF-16, …): those are
/// unreachable BY CONSTRUCTION here — no token type exists to retype — and are reached instead at the by-name
/// classification points through <see cref="CobolNet.Editions.CobolWordsMap.Resolve"/>. The two mechanisms
/// partition the population; neither alone covers it (kb/Work PB250).</para>
/// <para><b>The derivation is mechanical, so the next multi-spelling token is automatic</b> (no hand-maintained
/// list): the vocabulary walk is the fast path, and a miss falls back to <see cref="LexProbe"/> — running the
/// real lexer over the word itself and reading the type it produces. That is the same question asked of the same
/// authority, and it is cold: it runs only for a word a <c>&gt;&gt;COBOL-WORDS</c> directive actually names.
/// <c>CobolWordsReachDriftTests</c> pins both halves — every literal alternative in <c>CobolLexer.g4</c> resolves
/// here, and the probe agrees with the vocabulary wherever both answer.</para>
/// </summary>
public static class CobolKeywordTokens
{
    private static readonly Dictionary<string, int> ByWord = Build();

    /// <summary>Memoized <see cref="LexProbe"/> answers for words the vocabulary walk did not carry (0 = "the
    /// lexer does not make this word a keyword token"). Written under its own lock; the population is bounded by
    /// the words the compilation group's directives name.</summary>
    private static readonly Dictionary<string, int> Probed = new(CobolNames.Comparer);

    private static Dictionary<string, int> Build()
    {
        var vocab = CobolLexer.DefaultVocabulary;
        var map = new Dictionary<string, int>(CobolNames.Comparer);
        // GetLiteralName returns null for out-of-range indices (and for rule-defined tokens), so an upper bound
        // generously above the lexer's token count is safe — the COBOL lexer has a few hundred tokens.
        for (int t = 1; t < 8192; t++)
        {
            string? lit = vocab.GetLiteralName(t);   // e.g. "'MOVE'"; null for rule-defined tokens (IDENTIFIER, …)
            if (lit is not ['\'', .., '\''] || lit.Length < 3) continue;
            string word = lit[1..^1];
            // A COBOL word starts with a letter and contains only letters/digits/hyphens; this excludes the
            // operator/punctuation literals ('(', '*>', …) and figurative/special-character tokens.
            if (word.Length == 0 || !char.IsLetter(word[0])
                || !word.All(c => char.IsLetterOrDigit(c) || c == '-')) continue;
            map[word] = t;   // keyword literals are unique, so no meaningful collision
        }
        return map;
    }

    /// <summary>The lexer token type for a reserved/context-sensitive <paramref name="word"/> (case-insensitive),
    /// or false when the lexer does not make the word a keyword token (it lexes as an IDENTIFIER — the
    /// <see cref="CobolNet.Editions.CobolWordsMap.Resolve"/> population).</summary>
    public static bool TryTokenType(string word, out int type)
    {
        if (ByWord.TryGetValue(word, out type)) return true;
        type = ProbedType(word);
        return type != 0;
    }

    private static int ProbedType(string word)
    {
        lock (Probed)
        {
            if (Probed.TryGetValue(word, out int cached)) return cached;
            int t = LexProbe(word);
            Probed[word] = t;
            return t;
        }
    }

    /// <summary>
    /// Ask the LEXER what it makes of <paramref name="word"/> standing alone: the type of the single token it
    /// produces, or 0 when the word is not a keyword token. This is what reaches a keyword whose lexer rule
    /// carries several spellings (<c>PIC : 'PICTURE' | 'PIC'</c>) and therefore publishes no literal
    /// name for the vocabulary walk to find — the alternative was a hand-maintained list of such rules, which is
    /// exactly the shape that rots (kb/Work PB250).
    /// <para>Conditions for an answer to count, all checked: the lexer consumed the WHOLE word as ONE
    /// default-channel token, reported no error, and produced something other than <c>IDENTIFIER</c>. Anything
    /// else (a word that lexes as two tokens, a literal, an error) answers 0 — the conservative direction, since
    /// 0 routes the word to the name-level mechanism rather than retyping the wrong tokens. The probe runs the
    /// same <see cref="CobolLexer"/> the pipeline runs, over free-form text, exactly as
    /// <c>Frontend.LexAndParse</c> constructs it.</para>
    /// </summary>
    private static int LexProbe(string word)
    {
        if (word.Length == 0 || !char.IsLetter(word[0])) return 0;
        foreach (char c in word)
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '_') return 0;
        try
        {
            var lexer = new CobolLexer(new Antlr4.Runtime.AntlrInputStream(word));
            lexer.RemoveErrorListeners();
            var sink = new CountingErrorListener();
            lexer.AddErrorListener(sink);
            var tokens = new Antlr4.Runtime.CommonTokenStream(lexer);
            tokens.Fill();
            var onChannel = tokens.GetTokens()
                .Where(t => t.Channel == Antlr4.Runtime.Lexer.DefaultTokenChannel
                            && t.Type != Antlr4.Runtime.TokenConstants.EOF)
                .ToList();
            if (sink.Count > 0 || onChannel.Count != 1) return 0;
            var tok = onChannel[0];
            if (tok.Type == CobolLexer.IDENTIFIER) return 0;
            if (tok.StartIndex != 0 || tok.StopIndex != word.Length - 1) return 0;
            return tok.Type;
        }
        catch (Exception)
        {
            return 0;   // a probe never breaks a compile: an unlexable word is simply not a keyword token
        }
    }

    private sealed class CountingErrorListener : Antlr4.Runtime.IAntlrErrorListener<int>
    {
        public int Count { get; private set; }
        public void SyntaxError(TextWriter output, Antlr4.Runtime.IRecognizer recognizer, int offendingSymbol,
            int line, int charPositionInLine, string msg, Antlr4.Runtime.RecognitionException e) => Count++;
    }

    /// <summary>The IDENTIFIER token type — the user-word type the rewriter retypes UNDEFINE/SUBSTITUTE keywords
    /// to, and the source type it retypes EQUATE/SUBSTITUTE synonyms from.</summary>
    public static int IdentifierType => CobolLexer.IDENTIFIER;
}
