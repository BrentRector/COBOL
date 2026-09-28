// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;   // ParseCanceledException (thrown by BailErrorStrategy on the SLL pass)
using CobolNet.Editions;
using CobolNet.Frontend.Common;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Parsing;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Frontend;

/// <summary>
/// The WiseOwl COBOL front-end: source text → preprocessed free-form text → ANTLR parse tree.
/// </summary>
/// <remarks>
/// This is the WiseOwl COBOL front-end (assembly <c>Cobol.Net.Frontend</c>): the source preprocessor
/// (reference-format normalization, conditional compilation, COPY expansion, NIST placeholder substitution) and
/// the ANTLR lexer/parser. The parse tree it returns (<see cref="CobolParserCore.CompilationUnitContext"/>) is a
/// pure syntactic artifact — no semantic analysis, storage layout, or emission is involved. It is shared,
/// unchanged, by both the greenfield WiseOwl COBOL pipeline and (until the G8 cut-over) the legacy differential
/// oracle, which references this same assembly.
/// <para>
/// The pipeline mirrors the legacy <c>Compilation.Preprocess</c> + <c>Compilation.LexAndParse</c> exactly so
/// the proven preprocessing (incl. the SLL→LL two-stage parse and the <c>ZERO</c>→<c>ZERO_ARITH</c> rewrite)
/// is preserved bit-for-bit.
/// </para>
/// </remarks>
public sealed class Frontend
{
    /// <summary>Directories searched for COPY copybooks, in order.</summary>
    private readonly List<string> _copySearchPaths = [];

    /// <summary>
    /// When non-null, enables NIST CCVS preprocessing for the named test (e.g. <c>"NC101A"</c>): the
    /// <c>XXXXX###</c>/<c>XXXXP###</c>/<c>XXXXD###</c> placeholders are substituted with WiseOwl COBOL-appropriate
    /// values so the CCVS conformance programs compile.
    /// </summary>
    public string? NistTestName { get; init; }

    /// <summary>
    /// The dialect level (ISO year) the parser admits: <c>85</c>, <c>2002</c>, <c>2014</c>, or <c>2023</c>.
    /// Higher levels enable the post-85 grammar (OO, JSON/XML, generics, …). Defaults to COBOL-85, the level
    /// the NIST CCVS corpus targets.
    /// </summary>
    public int DialectLevel { get; init; } = 85;

    /// <summary>The strict/permissive severity axis (the P2.7 flip) as it applies to PREPROCESSOR-level
    /// removal gates (the W3 VCR 2/4/94 threading, DEVLOG 598): removed = error strict / warning permissive.
    /// Defaults strict, matching <c>EditionContext</c>.</summary>
    public bool Permissive { get; init; }

    /// <summary>The compilation's AMBIENT-INPUT record (kb/Work PB985): the source read, every copybook probe and
    /// read, every environment variable a directive consulted. A driver that must record reads it makes BEFORE the
    /// front end (the source-existence probe, the NIST copy library) passes its own instance here.</summary>
    public CompilationInputs Inputs { get; init; } = new();

    /// <summary>Add a directory to the COPY copybook search path.</summary>
    public void AddCopySearchPath(string path) => _copySearchPaths.Add(path);

    /// <summary>The RESULTANT-line → source-origin map of the last parsed source (kb/Work PB82): built by the
    /// preprocessing chain (fixed-form continuation joins, conditional compilation, COPY, REPLACE — the stages that
    /// change line counts), consulted by every user-facing position: the parser's diagnostic locations (via
    /// <see cref="CobolErrorListener"/>), the binder's diagnostic cursor and <c>FUNCTION EXCEPTION-LOCATION</c>'s
    /// line identifier. The directive stages after COPY are line-count preserving (asserted below), so their
    /// resultant lines — the &gt;&gt;TURN / &gt;&gt;FLAG anchors — stay directly comparable to token lines.</summary>
    public SourceLineMap LineMap { get; private set; } = SourceLineMap.Identity("<source>", 0);

    /// <summary>The <c>&gt;&gt;TURN</c> directive events of the LAST parsed source (ISO §7.3.25), anchored to
    /// 1-based lines of the final preprocessed text (so token <c>Start.Line</c> is directly comparable — the
    /// compile-time TurnState's basis, deep-dive D10). Empty when the source has no TURN directives.</summary>
    public DirectiveTimeline<TurnEvent> TurnEvents => Directives.TurnEvents;

    /// <summary>The frontend's <c>&gt;&gt;REF-MOD-ZERO-LENGTH</c> directive events (ISO §7.3.23) — they build the
    /// group's compile-time <see cref="Binding.RefModZeroLengthState"/> (the per-line zero-length allowance fold).</summary>
    public DirectiveTimeline<RefModZeroLengthEvent> RefModZeroLengthEvents => Directives.RefModZeroLengthEvents;

    /// <summary>The frontend's <c>&gt;&gt;FLAG-02</c> / <c>&gt;&gt;FLAG-14</c> directive events (ISO §7.3.14 /
    /// §7.3.15) — they build the group's compile-time <see cref="Binding.FlagState"/> (the per-line per-option
    /// migration-flag fold that <c>FlagConformancePass</c> queries). Empty when the source has no FLAG directives.</summary>
    public DirectiveTimeline<FlagEvent> FlagEvents => Directives.FlagEvents;

    /// <summary>The frontend's <c>&gt;&gt;COBOL-WORDS</c> override layer (ISO §7.3.10) — the per-group
    /// reserved/context-sensitive/intrinsic word-table modification the post-lex <c>CobolWordsRewriter</c>
    /// applies to the token stream and the compiler's composed <c>ReservedWordSet</c> / intrinsic resolution
    /// consult. <see cref="CobolWordsMap.Empty"/> when the source has no COBOL-WORDS directive.</summary>
    public CobolWordsMap CobolWordsMap => Directives.CobolWordsMap;

    /// <summary>The frontend's <c>&gt;&gt;LEAP-SECOND</c> state (ISO §7.3.17) — true when ON is in effect for the
    /// compilation group (kb/Work PB65): a formatted-time argument may carry a 60 in its seconds subfield
    /// (§15.3.3.3) and standard numeric time form is bounded at 86,401 (§7.3.17.4 GR4).</summary>
    public bool LeapSecondOn => Directives.LeapSecondOn;

    /// <summary>The POSITION-RULED directive sites of the LAST parsed source (ISO §7.3.20.3 SR4, §7.3.22.3 SR4,
    /// §7.3.25.3 SR5): WHERE each <c>&gt;&gt;TURN</c> / <c>&gt;&gt;PUSH</c> / <c>&gt;&gt;POP</c> was written, in
    /// the final line frame, for the ONE lexical-containment predicate that decides all three bans (owner
    /// decision D20; kb/Work PB595).</summary>
    public IReadOnlyList<DirectiveSite> DirectiveSites => Directives.DirectiveSites;

    /// <summary>EVERY directive-derived fact the binder consumes, as ONE record (kb/Work PB65 — the fifth
    /// positional parameter on <c>Bind</c> was the growing-list shape). Reflects the LAST parsed source, with
    /// §14.9.28.4 GR14's implicit PUSH ALL / POP ALL already replayed into every event timeline (kb/Work PB1004,
    /// PB1066 — the front end places them, because the conditional-compilation driver's state needs them too).</summary>
    public DirectiveResults Directives { get; private set; } = DirectiveResults.None;

    /// <summary>
    /// Preprocess and parse a COBOL source file. Returns the parse tree, or <see langword="null"/> if a fatal
    /// syntax error was reported (collected into <paramref name="diagnostics"/>).
    /// </summary>
    public CobolParserCore.CompilationUnitContext? Parse(string sourcePath, DiagnosticBag diagnostics)
    {
        var (normalized, referenceFormats) = Normalize(sourcePath, diagnostics);

        // ISO §14.9.28.4 GR14 (kb/Work PB1004, PB1066): "An implicit PUSH ALL … is assumed at the end of
        // imperative-statement-1. Immediately preceding the END PERFORM phrase, there is an implicit POP ALL". Only
        // the PARSE can place the two ops, yet the conditional-compilation driver — which runs before it — holds
        // directive state they restore (the compilation-variable table, the frontend-inline FLAG options), so a
        // >>DEFINE written in a WHEN phrase would outlive END-PERFORM. The text manipulation therefore re-runs with
        // the ops the previous parse placed, keyed to its directive encounters, until the parse places the same
        // program it was run with. KeyImplicitOps keeps only brackets that enclose a state change, so an ordinary
        // source converges on the FIRST pass. Convergence: the ops of a pass change only the text AFTER the first
        // directive whose state they change, so each pass settles at least one more directive encounter.
        IReadOnlyList<KeyedDirectiveOp> implicitOps = [];
        for (int pass = 1; ; pass++)
        {
            // Every stage from the driver on reports into THIS pass's bag; only the converged pass's diagnostics
            // are the compilation's (a DEFINE's SR2 redefinition or a FLAG warning depends on the state in effect).
            var passDiagnostics = new DiagnosticBag();
            var (processed, directives, encounters) = Preprocess(normalized, referenceFormats, sourcePath, passDiagnostics, implicitOps);
            LineMap = new SourceLineMap(processed.Lines);
            var tree = LexAndParse(processed.Text, sourcePath, directives.CobolWordsMap, passDiagnostics);
            bool mayMatter = directives.HasLineScopedEvents() || encounters.Directives.Any(e => e.ChangesState);
            var ops = tree is null || !mayMatter ? [] : ExceptionPerformDirectiveScope.ImplicitOps(tree);
            var keyed = encounters.KeyImplicitOps(ops);
            // A pass that did not parse is final: its syntax errors are the compilation's answer, and with no tree
            // there are no ops to converge on.
            if (tree is null || keyed.SequenceEqual(implicitOps))
            {
                foreach (var d in passDiagnostics.Diagnostics) diagnostics.Add(d);
                Directives = directives.WithStackOps(ops);
                return tree;
            }
            if (pass > encounters.Directives.Count + 1)
                throw new InvalidOperationException(
                    "the §14.9.28.4 GR14 implicit-op fixed point did not converge (kb/Work PB1066) — each pass must "
                    + "settle at least one more directive encounter");
            implicitOps = keyed;
        }
    }

    /// <summary>
    /// Phase 0 — source preprocessing: NIST archive-marker stripping → free-form normalization → conditional
    /// compilation (<c>&gt;&gt;DEFINE/IF/…</c>) → COPY expansion → NIST placeholder substitution. Each stage is a
    /// no-op on source that does not use it, so an ordinary program passes through essentially unchanged.
    /// </summary>
    /// <returns>The logical free-form text and the reference format each physical line was read in — which the COPY
    /// statements of the text need (§7.3.24.3 3), kb/Work PB1067).</returns>
    private (MappedText Text, ReferenceFormatMap Formats) Normalize(string sourcePath, DiagnosticBag diagnostics)
    {
        string raw = Inputs.ReadAllText(sourcePath);

        // The archive-marker strip is line-count preserving (markers become blank lines), so the origin map the
        // normalizer builds below still names the physical lines of the file on disk.
        raw = ReferenceFormatProcessor.StripNistArchiveMarkers(raw);
        // The edition-aware overload carries the fixed-form continuation gates (VCR rows 2/94, W3): only the
        // column-aware pass can see the col-7 indicator, so the per-edition obligations emit HERE. Mapped (kb/Work
        // PB82): a fixed-form continuation JOINS physical lines, and the map records which line each output came from.
        var text = ReferenceFormatProcessor.NormalizeToFreeFormMapped(raw, DialectLevel, Permissive, diagnostics, sourcePath,
            initialFixed: null, out var formats);
        return (text, formats);
    }

    /// <summary>The text manipulation and directive stages after normalization — repeatable, because the
    /// §14.9.28.4 GR14 implicit-op program (<paramref name="implicitOps"/>) comes from a parse of their own output
    /// (<see cref="Parse"/>). Returns the resultant text, the directive results (WITHOUT the implicit ops, which the
    /// caller replays once the program has converged), and the driver's directive encounters.</summary>
    private (MappedText Text, DirectiveResults Directives, ConditionalCompilationResult Encounters) Preprocess(
        MappedText normalized, ReferenceFormatMap referenceFormats, string sourcePath, DiagnosticBag diagnostics,
        IReadOnlyList<KeyedDirectiveOp> implicitOps)
    {
        // The MERGED text-manipulation driver (ISO §7.2.1) — conditional compilation INTERLEAVED with COPY, so a
        // >>DEFINE/>>IF/>>EVALUATE INSIDE a copybook is processed (the CC-before-COPY split could not see them), while
        // a main-source >>IF still gates a COPY (omitted-branch COPY is never expanded) and REPLACE (Step 3) runs over
        // the expanded group. leave* keep the post-85 directive families flowing to their dedicated stages below.
        // COPY runs BEFORE NIST substitution so placeholders inside copied library text are substituted.
        var copy = new CopyProcessor(_copySearchPaths, diagnostics, sourcePath,
            dialectLevel: DialectLevel, permissive: Permissive, inputs: Inputs);
        copy.RegisterReferenceFormat(sourcePath, referenceFormats);
        var manipulated = ConditionalCompilationProcessor.Manipulate(normalized, copy, LeftDirectives,
            diagnostics, sourcePath, DialectLevel, Permissive, Inputs, implicitOps);
        var mapped = manipulated.Text;

        // From here on the text is in its FINAL line frame: every stage below is line-count preserving (asserted),
        // so `mapped.Lines` stays the origin of each resultant line and the directive stages' event lines are the
        // parser's token lines. A stage that changed the count would break both at once — the one throw covers both.
        string text = mapped.Text;
        int linesBefore = CountLines(text);
        var lineMap = new SourceLineMap(mapped.Lines);   // the directive stages' diagnostics name SOURCE lines through it
        if (NistTestName is { } nist)
        {
            text = NistPreprocessor.Process(text, nist);
            if (CountLines(text) != linesBefore)
                throw new InvalidOperationException(
                    "NistPreprocessor changed the line count — the source-line map would misattribute every later line (kb/Work PB82)");
        }

        // The POSITION-RULED directive sites (ISO §7.3.20.3 SR4 / §7.3.22.3 SR4 / §7.3.25.3 SR5) — recorded
        // BEFORE the TURN stage blanks its lines, so all three words are still in the text and ONE scan answers
        // "where was a TURN / PUSH / POP written" for the binder's single lexical-containment predicate (owner
        // decision D20; kb/Work PB595). It also consumes the >>PUSH / >>POP lines, which the conditional-
        // compilation driver now leaves for it (LeftDirectives) because that driver runs before COPY has settled
        // the line frame and cannot name the resultant line a directive ended on — recording each as a
        // DirectiveStackOp every stage below replays over the state it holds, and warning (COBOLNET2297) for an
        // unsuccessful named POP (§7.3.20 / §7.3.22; kb/Work PB941). Line-count preserving.
        (text, var directiveSites, var stackOps) = DirectiveSiteProcessor.Process(text, diagnostics, sourcePath, lineMap);
        if (CountLines(text) != linesBefore)
            throw new InvalidOperationException(
                "DirectiveSiteProcessor changed the line count — every directive site would misanchor (hazard H3)");

        // >>TURN directive collection runs LAST — after COPY (so copybook TURNs are seen) and after the
        // line-count-neutral NIST substitution — on the FINAL text, so each TurnEvent.Line is directly
        // comparable to the parser tokens' Start.Line (the TurnState anchor, deep-dive D10 / hazard H3).
        (text, var turnEvents) = TurnDirectiveProcessor.Process(text, DialectLevel, diagnostics, sourcePath, lineMap, stackOps);
        if (CountLines(text) != linesBefore)
            throw new InvalidOperationException(
                "TurnDirectiveProcessor changed the line count — TURN scoping would silently misanchor (hazard H3)");

        // >>PROPAGATE (ISO §7.3.21): recognize + edition-gate (introduction gate; runtime semantics are PHASE-13).
        // Line-count preserving like the >>TURN stage.
        text = PropagateDirectiveProcessor.Process(text);
        if (CountLines(text) != linesBefore)
            throw new InvalidOperationException(
                "PropagateDirectiveProcessor changed the line count (hazard H3)");

        // >>REF-MOD-ZERO-LENGTH (ISO §7.3.23): recognize + edition-gate + collect the per-line zero-length toggle
        // events on the FINAL text (each event line is directly comparable to a ref-mod token's Start.Line — the
        // >>TURN anchoring discipline). Line-count preserving like the two stages above.
        (text, var refModZeroLengthEvents) = RefModZeroLengthDirectiveProcessor.Process(text, stackOps);
        if (CountLines(text) != linesBefore)
            throw new InvalidOperationException(
                "RefModZeroLengthDirectiveProcessor changed the line count (hazard H3)");

        // >>FLAG-02 / >>FLAG-14 (ISO §7.3.14 / §7.3.15): collect the per-option ON/OFF toggle events on the FINAL
        // text (each event line is directly comparable to a flagged construct's token Start.Line — the >>TURN
        // anchoring discipline). Line-count preserving like the stages above.
        (text, var flagEvents) = FlagDirectiveProcessor.Process(text, diagnostics, sourcePath, lineMap, stackOps);
        if (CountLines(text) != linesBefore)
            throw new InvalidOperationException(
                "FlagDirectiveProcessor changed the line count (hazard H3)");

        // >>COBOL-WORDS (ISO §7.3.10): parse the per-group reserved/context/intrinsic word-table modification into
        // the CobolWordsMap (the post-lex rewriter + composed ReservedWordSet consume it), edition-gate the
        // directive word, and enforce SR1/SR2/SR5. Line-count preserving like the stages above.
        (text, var cobolWordsMap) = CobolWordsDirectiveProcessor.Process(text, diagnostics, sourcePath, lineMap, stackOps);
        if (CountLines(text) != linesBefore)
            throw new InvalidOperationException(
                "CobolWordsDirectiveProcessor changed the line count (hazard H3)");

        // >>LEAP-SECOND (ISO §7.3.17): the ONE compilation-group ON/OFF fact the §15.3 date/time consumers read
        // (kb/Work PB65 — it used to be consumed and discarded). Line-count preserving like the stages above.
        (text, var leapSecondOn) = LeapSecondDirectiveProcessor.Process(text, diagnostics, sourcePath, lineMap, stackOps);
        if (CountLines(text) != linesBefore)
            throw new InvalidOperationException(
                "LeapSecondDirectiveProcessor changed the line count (hazard H3)");

        return (new MappedText(text, mapped.Lines),   // the constructor re-asserts the line-count invariant
            new DirectiveResults(turnEvents, refModZeroLengthEvents, flagEvents, cobolWordsMap, leapSecondOn, directiveSites),
            manipulated);
    }

    /// <summary>The ISO §7.3 directive keywords the merged text-manipulation driver LEAVES in the text for the
    /// dedicated stages above — ONE list, in the order those stages run (PUSH / POP / TURN for the site scan,
    /// then TURN, PROPAGATE, REF-MOD-ZERO-LENGTH, FLAG-02 / FLAG-14, COBOL-WORDS, LEAP-SECOND). A new directive
    /// with behavior is one entry here plus its stage.
    /// <para>PUSH and POP are here for their POSITION and their OPS: §7.3.22.3 SR4 and §7.3.20.3 SR4 are rules
    /// about WHERE they are written, and only the final line frame can say (kb/Work PB595), and
    /// <see cref="Preprocessor.DirectiveSiteProcessor"/> records each as a <see cref="DirectiveStackOp"/> that the
    /// later state-holding stages replay through the ONE <see cref="DirectiveStateStack"/> (§7.3.20 / §7.3.22,
    /// kb/Work PB941). The driver applies them too, in encounter order, to the state IT holds (DEFINE and the
    /// running FLAG options) before leaving the line.</para>
    /// This set answers WHICH STAGE OWNS THE LINE and nothing else: every word in it is also a
    /// <c>CompilerDirectiveCatalog</c> row, and the EDITION question was already answered by the driver before the
    /// line reached these stages (kb/Work PB725), which is why none of them takes a dialect any more.
    /// <c>CompilerDirectiveCatalogDriftTests</c> asserts the subset relation.</summary>
    public static readonly IReadOnlySet<string> LeftDirectives = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "TURN", "PUSH", "POP", "PROPAGATE", "REF-MOD-ZERO-LENGTH", "FLAG-02", "FLAG-14", "COBOL-WORDS", "LEAP-SECOND",
    };

    private static int CountLines(string s) => s.Count(c => c == '\n');

    /// <summary>
    /// Phase 1 — lex + parse. Uses the proven two-stage strategy: fast SLL prediction first (with a
    /// <see cref="BailErrorStrategy"/> so an ambiguity throws rather than hangs), falling back to full LL on
    /// failure. The <c>ZERO</c>→<c>ZERO_ARITH</c> token rewrite runs between lexing and parsing to avoid an
    /// exponential-prediction ambiguity. Returns <see langword="null"/> if any syntax error was reported.
    /// </summary>
    private CobolParserCore.CompilationUnitContext? LexAndParse(string text, string sourcePath, CobolWordsMap cobolWordsMap,
        DiagnosticBag diagnostics)
    {
        var lexer = new CobolLexer(new AntlrInputStream(text));
        // >>COBOL-WORDS (ISO §7.3.10.4 GR3/GR4): a de-reserved word (UNDEFINE/SUBSTITUTE) may be used as a
        // SUBSCRIPTED data name; the lexer must open SUBSCRIPT mode at its following '(' even though the word is
        // still lexed as its keyword token (the retype below runs post-lex, after the '(' decision is frozen).
        // Set BEFORE any tokenization (ZeroTokenRewriter.Fill). A no-op when no de-reserved word is a keyword token.
        var retypes = TokenRetypes.None with { CobolWords = cobolWordsMap };
        var edition = EditionInfo.Of(DialectLevel, Permissive);
        retypes.PrimeLexer(lexer, edition);
        var tokens = new CommonTokenStream(lexer);
        ZeroTokenRewriter.Rewrite(tokens);
        // >>COBOL-WORDS (ISO §7.3.10.4) — retype tokens per the per-group override: synonyms (EQUATE/SUBSTITUTE)
        // become their canonical keyword, de-reserved words (UNDEFINE/SUBSTITUTE) become IDENTIFIERs. A no-op when
        // the source has no directive (byte-identical).
        retypes.Rewrite(tokens);

        // ISO §13.18.40.3 SR7 — decided from the characters PICMODE delimited, once per PICTURE clause of every
        // entry kind (kb/Work PB569). Reported through the syntax-error listener so it is located exactly like a
        // parse error; the parse itself still runs, so every other diagnostic of the unit is reported too.
        var sr7 = new CobolErrorListener(diagnostics, sourcePath, LineMap);
        foreach (var pic in PictureSeparatorPeriodRule.Violations(tokens.GetTokens()))
            sr7.SyntaxError(TextWriter.Null, null!, pic, pic.Line, pic.Column,
                $"[{Diagnostics.DiagnosticDescriptors.COBOLNET2419.Code}] {PictureSeparatorPeriodRule.Message(pic)}", null!);

        // The parser needs the map as well as the lexer and the rewriter: its text predicates (LOCALE, ORDER,
        // CLASSIFICATION, ATTRIBUTE, the LC_ categories) recognize §8.9/§8.10 words the lexer deliberately does
        // not tokenize, so only CobolWordsMap.Resolve can reach them — kb/Work PB250.
        // ⛔ BOTH AXES, not just the year (kb/Work PB693). The parser's <c>Edition</c> is the single source its
        // predicates read, and the reservation gate (<c>userWordHere</c>) asks the PERMISSIVE axis: the migration
        // mode must keep a §8.9-reserved word usable as a user-defined word so the funnel can WARN instead of
        // the parse dying. Setting only DialectLevel left Permissive false for every real compile, so
        // `--permissive` could not save a program that names a reservation-gated word — the one thing the mode
        // exists for. Every other stage of this pipeline is already handed `Permissive`; the parser was the gap.
        var parser = new CobolParserCore(tokens)
        {
            Edition = edition,
            CobolWords = cobolWordsMap,
        };

        // ⛔ THE TOKEN-LEVEL §8.9 GATE (kb/Work PB655): the ONE loop both front ends share —
        // ReservationGateRewriter.ParseToFixpoint re-parses until no newly declared free gated word remains.
        var tree = ReservationGateRewriter.ParseToFixpoint(parser, tokens, retypes, diagnostics,
            (bag, witness) => ParsePass(parser, tokens, bag, witness, sourcePath));

        return parser.NumberOfSyntaxErrors > 0 ? null : tree;
    }

    /// <summary>One parse of the whole token stream from token 0: fast SLL prediction first (with a
    /// <see cref="BailErrorStrategy"/> so an ambiguity throws rather than hangs), falling back to full LL on
    /// failure.</summary>
    private CobolParserCore.CompilationUnitContext ParsePass(CobolParserCore parser, CommonTokenStream tokens,
        DiagnosticBag diagnostics, IAntlrErrorListener<IToken> gateWitness, string sourcePath)
    {
        tokens.Seek(0);
        parser.Reset();
        parser.RemoveErrorListeners();
        // ⛔ THE SLL PASS IS SPECULATIVE AND MUST NOT DIAGNOSE (kb/Work PB396). SLL is an APPROXIMATION of LL:
        // it can fail on input full LL prediction accepts, and when it fails the LL pass below re-derives every
        // diagnostic from token 0 — so a listener attached across both passes reported each real syntax error
        // TWICE (once with ANTLR's own wording from BailErrorStrategy's inherited ReportError, once with
        // CobolErrorStrategy's), and reported a FALSE error whenever SLL was merely the weaker predictor. The
        // listener is therefore attached to the AUTHORITATIVE pass only. Measured on `IF X = 1 END-IF`: two
        // errors at the same (8,12) before, one after.
        try
        {
            parser.Interpreter.PredictionMode = PredictionMode.SLL;
            parser.ErrorHandler = new BailErrorStrategy();
            return parser.compilationUnit();
        }
        catch (Exception e) when (e is ParseCanceledException or RecognitionException)
        {
            // SLL bailed on a genuine parse ambiguity/mismatch (BailErrorStrategy throws ParseCanceledException,
            // wrapping a RecognitionException) — retry with full LL prediction and the diagnostic-collecting error
            // strategy. A NON-parse exception (a predicate/lexer-action bug) now propagates instead of being
            // silently retried under LL, where it would surface as a misleading generic syntax error. (Rearch P1.)
            tokens.Seek(0);
            parser.Reset();
            parser.Interpreter.PredictionMode = PredictionMode.LL;
            parser.ErrorHandler = new CobolErrorStrategy();
            parser.AddErrorListener(new CobolErrorListener(diagnostics, sourcePath, LineMap));
            parser.AddErrorListener(gateWitness);
            return parser.compilationUnit();
        }
    }
}
