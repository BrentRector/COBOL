// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using Xunit;
using Xunit.Abstractions;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AFTER WARM-UP THE LEXER PERFORMS NO ATN SIMULATION (kb/Work PB1715; DESIGN-test-build-ci.md §3.14.5 M6):
/// once the suite's sources have been lexed, lexing them again takes a cached DFA edge for every ASCII character
/// of every token, and every mode of CobolLexer.g4 has a cached start state. A semantic predicate anywhere on a
/// path the DFA must cache breaks it, because ANTLR 4.13.1 never caches a start state whose closure passed through a
/// predicate (<c>LexerATNSimulator.MatchATN</c>) nor a DFA edge whose target did (<c>AddDFAEdge</c>) — so the tokens that
/// reach the predicate re-run ATN simulation, and re-add their state under <c>lock (dfa.states)</c>, on every
/// occurrence. Six left-edge predicates did exactly that to every token of every compile: the <c>cobol</c> CLI
/// paid the simulation serially and every parallel compile serialized on the one lock. Context the lexer needs
/// goes in an ACTION (actions do not suppress DFA edges), never in a predicate.
/// <para>Not asserted, REPORTED: a character above 127 (<c>LexerATNSimulator.MAX_DFA_EDGE</c>) has no DFA edge,
/// so a non-ASCII source is simulated at each such character; and ANTLR never caches the EOF transition, so
/// the token that reaches the end of its input is simulated once per input.</para>
/// </summary>
public sealed class LexerDfaCacheDriftTests(ITestOutputHelper output)
{
    /// <summary>Why a <see cref="LexerATNSimulator.Match"/> call would leave the cached DFA.</summary>
    private enum Miss { None, StartState, AsciiEdge, NonAscii, Eof }

    [Fact]
    public void ReLexingTheSuiteSources_TakesOnlyCachedDfaEdges()
    {
        var sources = SuiteSources();
        Assert.True(sources.Count > 1000, $"only {sources.Count} suite sources found — the probe is blind");

        // A PRIVATE DFA, so the measurement does not depend on what other tests of this process lexed first, and
        // the warm-up is provably the only thing that fills it.
        var dfa = new DFA[CobolLexer._ATN.NumberOfDecisions];
        for (int i = 0; i < dfa.Length; i++) dfa[i] = new DFA(CobolLexer._ATN.GetDecisionState(i), i);

        var warm = new Tally();
        foreach (var (_, text) in sources) Lex(text, dfa, warm);
        // The probe must be able to see a miss, or a zero below proves nothing: a cold DFA misses.
        Assert.True(warm.StartState > 0 && warm.AsciiEdge > 0,
            $"the warm-up pass saw no DFA miss (start {warm.StartState}, edge {warm.AsciiEdge}) — the probe is blind");

        var ascii = new Tally();
        var nonAscii = new Tally();
        var offenders = new List<string>();
        foreach (var (path, text) in sources)
        {
            bool isAscii = text.All(c => c <= LexerATNSimulator.MAX_DFA_EDGE);
            var tally = isAscii ? ascii : nonAscii;
            int before = tally.StartState + tally.AsciiEdge;
            Lex(text, dfa, tally);
            if (isAscii && tally.StartState + tally.AsciiEdge > before && offenders.Count < 10) offenders.Add(path);
        }

        output.WriteLine($"{sources.Count} sources; warm-up: {warm}");
        output.WriteLine($"re-lex, ASCII sources: {ascii}");
        output.WriteLine($"re-lex, non-ASCII sources (reported, never asserted — no DFA edge above 127): {nonAscii}");
        for (int mode = 0; mode < CobolLexer.modeNames.Length; mode++)
            Assert.True(dfa[mode].s0 is not null,
                $"lexer mode {CobolLexer.modeNames[mode]} has no cached start state after the warm-up — a semantic "
                + "predicate on the left edge of one of its rules (ANTLR caches a start state only when no predicate "
                + "is in its closure), or the suite never enters the mode");
        Assert.True(ascii.StartState == 0 && ascii.AsciiEdge == 0,
            $"re-lexing the ASCII suite sources left the cached DFA {ascii.StartState} time(s) at a token start and "
            + $"{ascii.AsciiEdge} time(s) mid-token — a semantic predicate is on a path the DFA must cache, so every "
            + "token through it re-runs ATN simulation under ANTLR's lock (kb/Work PB1715). Move the context test into "
            + "an ACTION. First sources: " + string.Join(", ", offenders));
    }

    /// <summary>Every NIST program and copybook and every conformance golden and negative, read as text.</summary>
    private static List<(string Path, string Text)> SuiteSources() =>
        [.. new[] { TestRepo.Nist(), TestRepo.Tests("conformance") }
            .SelectMany(root => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".cob" or ".cbl" or ".cpy")
            .Order(StringComparer.Ordinal)
            .Select(f => (Path.GetRelativePath(TestRepo.Root, f), File.ReadAllText(f)))];

    private static void Lex(string text, DFA[] dfa, Tally tally)
    {
        var input = new ProbedCharStream(text, dfa, tally);
        var lexer = new IsolatedLexer(input, dfa);
        lexer.RemoveErrorListeners();
        input.Lexer = lexer;
        while (lexer.NextToken().Type != TokenConstants.EOF) { }
    }

    /// <summary>The generated lexer over a caller-supplied DFA (the generated one shares a static DFA).</summary>
    private sealed class IsolatedLexer : CobolLexer
    {
        public IsolatedLexer(ICharStream input, DFA[] dfa) : base(input) =>
            Interpreter = new LexerATNSimulator(this, _ATN, dfa, new PredictionContextCache());
    }

    private sealed class Tally
    {
        public int StartState, AsciiEdge, NonAscii, Eof;
        public void Add(Miss m)
        {
            switch (m)
            {
                case Miss.StartState: StartState++; break;
                case Miss.AsciiEdge: AsciiEdge++; break;
                case Miss.NonAscii: NonAscii++; break;
                case Miss.Eof: Eof++; break;
            }
        }
        public override string ToString() =>
            $"start-state misses {StartState}, ASCII edge misses {AsciiEdge}, non-ASCII character misses {NonAscii}, "
            + $"EOF transitions {Eof}";
    }

    /// <summary>
    /// The measuring instrument. ANTLR's <c>LexerATNSimulator</c> exposes no virtual seam around its simulation
    /// (<c>MatchATN</c> and <c>ComputeTargetState</c> are not virtual), so the probe asks the question from the
    /// input side: <c>Lexer.NextToken</c> and <c>LexerATNSimulator.Match</c> each <see cref="Mark"/> the stream as
    /// a match begins, and at that moment the probe walks the cached DFA of the lexer's current mode over the
    /// coming characters exactly as <c>ExecATN</c> will — start state, then one edge per character until the
    /// cached ERROR edge ends the token — and records the first place the walk would leave the cache.
    /// </summary>
    private sealed class ProbedCharStream(string text, DFA[] dfa, Tally tally) : ICharStream
    {
        private readonly AntlrInputStream _inner = new(text);
        private int _lastIndex = -1, _lastMode = -1;
        public Lexer? Lexer { get; set; }

        public int Mark()
        {
            int mode = Lexer!.CurrentMode;
            // NextToken marks, then Match marks at the same place: one match, probed once.
            if (_inner.Index != _lastIndex || mode != _lastMode)
            {
                _lastIndex = _inner.Index;
                _lastMode = mode;
                tally.Add(Walk(dfa[mode], _inner));
            }
            return _inner.Mark();
        }

        private static Miss Walk(DFA modeDfa, ICharStream input)
        {
            DFAState? s = modeDfa.s0;
            if (s is null) return Miss.StartState;
            for (int k = 1; ; k++)
            {
                int t = input.LA(k);
                if (t == IntStreamConstants.EOF) return Miss.Eof;
                if (t > LexerATNSimulator.MAX_DFA_EDGE) return Miss.NonAscii;
                DFAState? next = s.edges?[t - LexerATNSimulator.MIN_DFA_EDGE];
                if (next is null) return Miss.AsciiEdge;
                if (next == ATNSimulator.ERROR) return Miss.None;
                s = next;
            }
        }

        public void Consume() => _inner.Consume();
        public int LA(int i) => _inner.LA(i);
        public void Release(int marker) => _inner.Release(marker);
        public int Index => _inner.Index;
        public void Seek(int index) => _inner.Seek(index);
        public int Size => _inner.Size;
        public string SourceName => _inner.SourceName;
        public string GetText(Interval interval) => _inner.GetText(interval);
    }
}
