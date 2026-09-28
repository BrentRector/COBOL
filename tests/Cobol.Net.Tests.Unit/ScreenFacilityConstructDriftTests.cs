// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using CobolNet.Binding;
using CobolNet.Editions;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The Annex A.4.2 (screen handling) REFUSAL, held from the three directions a negative golden cannot reach
/// (kb/Work PB260).
///
/// <para>The negative corpus proves that each declined construct DRAWS its named diagnostic. It cannot prove
/// (a) that every clause the grammar admits HAS a name — a clause added to <c>CobolScreen.g4</c> with no
/// <see cref="ScreenFacility"/> row would fall silently through to the section's own diagnostic and every
/// witness would still be green; (b) that a diagnostic is ABSENT, which is what the kb/Work R32 no-cascade
/// property is; or (c) that the refusal has a COMPLEMENT — that an ordinary program still compiles and the
/// three screen context-sensitive words are still legal user-defined words. A gate that can only go green is
/// not evidence (feedback_green_gates_arent_evidence, feedback_measure_the_selectors_complement).</para>
/// </summary>
public sealed class ScreenFacilityConstructDriftTests
{
    private static (bool Ok, IReadOnlyList<string> Errors) Compile(string source, int edition)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Scr_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "prog.dll"), DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Auto));
            return (r.Success, r.Success ? [] : [.. r.Errors]);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    private static bool Has(IEnumerable<string> diags, string needle) =>
        diags.Any(d => d.Contains(needle, StringComparison.OrdinalIgnoreCase));

    /// <summary>⛔ THE DRIFT GATE. Every alternative of the grammar's <c>screenClause</c> rule has a
    /// <see cref="ScreenFacility"/> row, so a clause added to the SCREEN SECTION surface cannot ship
    /// un-named — it would otherwise be swallowed by the section-header diagnostic and no witness would
    /// notice. Read from the <c>.g4</c> text, which is the only place the alternative list exists.</summary>
    [Fact]
    public void EveryScreenClauseAlternative_HasAScreenFacilityRow()
    {
        var alts = ScreenClauseAlternatives();
        var covered = ScreenFacility.CoveredClauseRules.ToHashSet(StringComparer.Ordinal);
        var missing = alts.Where(a => !covered.Contains(a)).Order().ToList();
        Assert.True(missing.Count == 0,
            "screenClause alternative(s) with no ScreenFacility row — the construct would be refused only by "
            + "the section header, with no name of its own: " + string.Join(", ", missing));
        // The complement: a row for a rule the grammar no longer offers is a stale citation nobody can reach.
        var stale = covered.Where(c => !alts.Contains(c)).Order().ToList();
        Assert.True(stale.Count == 0,
            "ScreenFacility row(s) for rules that are not screenClause alternatives: " + string.Join(", ", stale));
    }

    private const string ScreenProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. SCRREF.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-X PIC X.
        SCREEN SECTION.
        01 SG.
           05 SI1 LINE 1 COL 1 PIC X FROM WS-X.
        PROCEDURE DIVISION.
        MAIN.
            DISPLAY SG END-DISPLAY.
            STOP RUN.
        """;

    /// <summary>kb/Work R32, kept as an ABSENCE assertion because a negative golden can only assert presence.
    /// A name DECLARED in a refused SCREEN SECTION is not UNDEFINED: the program is rejected for the facility
    /// (COBOLNET1560 for the section, COBOLNET1707 for the screen DISPLAY), never with the §8.4.2.1
    /// "is not defined" verdict (COBOLNET1639), which would send the user hunting a declaration that is right
    /// there. This is what <c>tests/conformance/2023/screen_section_reference.cob</c> used to hold before PB260
    /// refuted its premise (it asserted the program COMPILED).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ScreenNameReference_IsRefusedByFacility_NeverAsUndefined(int edition)
    {
        var (ok, errors) = Compile(ScreenProgram, edition);
        Assert.False(ok, "a SCREEN SECTION program must be REJECTED — Annex A.4.1 admits an optional element's "
            + "syntax only when support is claimed, and docs/CONFORMANCE.md §5 records A.4.2 as Not claimed");
        Assert.True(Has(errors, "COBOLNET1560"), "expected the named screen-facility refusal:\n"
            + string.Join("\n", errors));
        Assert.True(Has(errors, "COBOLNET1707"), "expected the screen DISPLAY (format 2) to be named too — a "
            + "bare `DISPLAY screen-name` is token-identical to the device format and used to PRINT the screen "
            + "record:\n" + string.Join("\n", errors));
        Assert.False(Has(errors, "COBOLNET1639"), "a name declared in the SCREEN SECTION must not also be "
            + "reported as undefined (kb/Work R32):\n" + string.Join("\n", errors));
    }

    /// <summary>⛔ THE FAILING BRANCH, exercised. A program with no screen construct compiles clean and draws
    /// NEITHER code — without this the whole suite could pass with a refusal that fired on every program.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void OrdinaryProgram_DrawsNoScreenRefusal(int edition)
    {
        var (ok, errors) = Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCRNEG.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X(3) VALUE "ABC".
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY WS-X.
                STOP RUN.
            """, edition);
        Assert.True(ok, string.Join("\n", errors));
        Assert.False(Has(errors, "COBOLNET1560") || Has(errors, "COBOLNET1707"));
    }

    /// <summary>⛔ THE REGRESSION THIS WAVE ALMOST SHIPPED. `ON EXCEPTION` / `NOT ON EXCEPTION` is spelled the
    /// same on a screen ACCEPT/DISPLAY as on the statements that already own one, and DISPLAY sits inside every
    /// one of their imperative-statement slots — so a free-standing optional `screenExceptionPhrases` on
    /// DISPLAY made the INNER display swallow the ENCLOSING statement's NOT-arm, and the enclosing statement
    /// lost it silently. Four corpus programs went red (delete_file_absent, ec_external_*). The grammar now
    /// binds the exception phrases to the AT/LINE/COLUMN positioning phrase (`screenTail`), which no other
    /// statement's exception arm can begin with; this pins that coupling so it cannot be "simplified" away.
    /// The program must compile AND its own DISPLAYs must draw no screen verdict.</summary>
    [Fact]
    public void NestedDisplayInAnExceptionArm_DoesNotStealTheEnclosingStatementsNotArm()
    {
        var (ok, errors) = Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCRNEST.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN "scrnest.dat" ORGANIZATION SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 F-REC PIC X(10).
            PROCEDURE DIVISION.
            MAIN.
                DELETE FILE F
                    ON EXCEPTION DISPLAY "EXC"
                    NOT ON EXCEPTION DISPLAY "NOEXC"
                END-DELETE.
                STOP RUN.
            """, 2023);
        Assert.True(ok, string.Join("\n", errors));
        Assert.False(Has(errors, "COBOLNET1707"));
    }

    /// <summary>⛔ THE COMPLEMENT OF THE OPTIONS INITIALIZE REFUSAL. §11.9.10.4 GR1 — "If ALL is specified,
    /// LOCAL-STORAGE, SCREEN, and WORKING-STORAGE apply" — names three targets of which TWO are supported, so
    /// <c>INITIALIZE ALL SECTION TO SPACES</c> is legal source and must stay legal; only the EXPLICIT SCREEN leg
    /// of GR3 is refused (<c>a42-options-initialize-screen</c>). The standard wrote the two as separate rules,
    /// and refusing ALL over a section the program does not even have would be exactly the over-rejection the
    /// A.4.2 selector's excluded-near-miss list called out.</summary>
    [Fact]
    public void OptionsInitializeAll_StaysLegal()
    {
        var (ok, errors) = Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCROIA.
            OPTIONS.
                INITIALIZE ALL SECTION TO SPACES.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X(3).
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            """, 2023);
        Assert.True(ok, string.Join("\n", errors));
        Assert.False(Has(errors, "COBOLNET1560"));
    }

    // ══ THE SCREEN SURFACE'S OWN WORDS (kb/Work PB301) ═══════════════════════════════════════════════════════
    //
    // ⛔ THE POPULATION IS DERIVED FROM THE GRAMMAR, NEVER LISTED. This used to be eight `[InlineData]` rows
    // naming five words at two editions — a selector that was evidence about the five it named and silent about
    // the seventeen it dropped (feedback_measure_the_selectors_complement). Those seventeen pass today only
    // because kb/Work PB693/PB300 replaced the hand reservation gate with one DERIVED from §8.9; nothing held
    // them, and the next token the screen grammar adds without a `cobol-words.json` nameSlot row would cost the
    // user that word again in silence — which is PB301's defect exactly (CLAUDE.md rule 5: never a
    // hand-maintained list where a structure belongs).

    /// <summary>Every single-word lexer token the SCREEN surface references, paired with each edition. The
    /// surface is <c>CobolScreen.g4</c> plus the two SPECIAL-NAMES screen clauses in
    /// <c>CobolSpecialNames.g4</c>; the population keeps only the words ISO §8.9 leaves FREE at at least one
    /// edition, because a word §8.9 reserves everywhere was never the user's to lose.</summary>
    public static IEnumerable<object[]> ScreenSurfaceWordCases() =>
        from word in ScreenSurfaceWords()
        from edition in new[] { 85, 2002, 2014, 2023 }
        select new object[] { word, edition };

    /// <summary>⛔ THE DRIFT GATE FOR THE WORDS, the twin of
    /// <see cref="EveryScreenClauseAlternative_HasAScreenFacilityRow"/> for the clauses.
    /// <para>ISO §8.3.2.1 rule 3 — "Context-sensitive words may be used as user-defined words and system-names in
    /// contexts other than the language construct in which they are defined" — so the fifteen §8.10 screen words
    /// (AUTO, BELL, BLINK, BACKGROUND-COLOR, EOL, EOS, ERASE, FOREGROUND-COLOR, FULL, HIGHLIGHT, LOWLIGHT,
    /// REQUIRED, REVERSE-VIDEO, SECURE, UNDERLINE) are legal user-defined words at EVERY edition, declining
    /// Annex A.4.2 notwithstanding. ISO §8.3.2.1 rule 1 — "Reserved words shall not be used as user-defined words
    /// or system-names" — so the seven §8.9 screen words (COL, COLS, COLUMN, COLUMNS, CRT, CURSOR, SCREEN) are
    /// barred exactly where §8.9 reserves them, and §8.9 adds all but COLUMN in 2002, which leaves them legal
    /// COBOL-85 user words.</para>
    /// <para>Both arms are asserted here, and the REJECTING arm asserts the DIAGNOSTIC, not merely the rejection:
    /// COBOLNET0901 names §8.9, where the raw COBOL0001 "no viable alternative" this class of bug produces names
    /// nothing. The corpus lanes are <c>85/pb301_screen_words_as_user_words</c>,
    /// <c>2023/pb301_screen_words_as_user_words</c>, <c>negative/pb301-screen-words-reserved-from-2002</c> and
    /// <c>negative/pb301-column-reserved-at-every-edition</c>; the SCREEN-position complement — the same words
    /// still recognized in their own clause and refused BY NAME — is the <c>a42-screen-*</c> negative set.</para>
    /// </summary>
    [Theory]
    [MemberData(nameof(ScreenSurfaceWordCases))]
    public void EveryScreenSurfaceWord_IsAUserWordExactlyWhereSection89LeavesItFree(string word, int edition)
    {
        bool reserved = ReservedWords.Find(word)?.IsReservedAt(edition) == true;
        var (ok, errors) = Compile($"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCRW{new string(word.Where(char.IsLetterOrDigit).ToArray())}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 {word} PIC X(3) VALUE "ABC".
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY {word}.
                STOP RUN.
            """, edition);

        if (!reserved)
        {
            Assert.True(ok, $"'{word}' is a lexer token the screen surface needs, and §8.9 does not reserve it at "
                + $"COBOL-{edition} — §8.3.2.1 rule 3 makes it a legal user-defined word there and declining "
                + $"Annex A.4.2 must not cost it:\n{string.Join("\n", errors)}");
            return;
        }

        Assert.False(ok, $"§8.9 reserves '{word}' at COBOL-{edition}, so §8.3.2.1 rule 1 bars it from a "
            + "user-defined-word slot");
        Assert.True(Has(errors, "COBOLNET0901"), $"'{word}' must be refused BY NAME at COBOL-{edition} (§8.9 via "
            + $"the reservation gate's `reservedGatedWord` twin), never as a raw parse error:\n"
            + string.Join("\n", errors));
    }

    /// <summary>⛔ THE DERIVATION'S OWN FAILING BRANCH. A scrape that returned nothing — a renamed rule, a
    /// changed token spelling, a moved file — would make the theory above vacuously green, and a nameSlot row
    /// missing for a NEW screen token is PB301's defect returning. Both are asserted here: the population's size
    /// and both of its classes, and that every member carries a <c>cobol-words.json</c> <c>nameSlot</c> row,
    /// which is the single fact the whole mechanism rests on (the reservation gate is then derived from §8.9 by
    /// <c>gen-cobol-words.ps1</c>, so no per-word edition flag can rot).</summary>
    [Fact]
    public void ScreenSurfaceWordPopulation_IsDerived_AndEveryMemberHasANameSlotRow()
    {
        var words = ScreenSurfaceWords();
        Assert.True(words.Count >= 20,
            $"only {words.Count} screen-surface words found — the .g4 scrape broke and the word theory is blind");

        // Both classes must be present, or one arm of the theory never runs.
        Assert.Contains(words, w => ReservedWords.Find(w) is null);                         // §8.10, free always
        Assert.Contains(words, w => ReservedWords.Find(w) is { } e                          // §8.9, free at 85 only
                                    && !e.IsReservedAt(85) && e.IsReservedAt(2002));

        using var doc = JsonDocument.Parse(File.ReadAllText(TestRepo.VersionMatrix("cobol-words.json")));
        var nameSlot = doc.RootElement.GetProperty("words").EnumerateArray()
            .Where(e => e.TryGetProperty("nameSlot", out var n) && n.GetBoolean())
            .Select(e => e.GetProperty("token").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        // The token name gen-cobol-words.ps1 keys on: the ANTLR spelling of the COBOL word, with `-` written `_`
        // and a trailing `_` where the plain name would clash (FULL_, UNDERLINE_). Read back from the lexer
        // rather than guessed, and inverted ONCE — the per-word lookup used to re-read and re-scan CobolLexer.g4
        // for every member of the population.
        var tokenNameOf = LexerWordTokens()
            .GroupBy(kv => kv.Value, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Key, StringComparer.Ordinal);

        var missing = words.Where(w => !nameSlot.Contains(tokenNameOf[w])).Order().ToList();
        Assert.True(missing.Count == 0,
            "screen-surface word(s) with no cobol-words.json nameSlot row — the lexer takes the word and no name "
            + "slot gives it back, which is a legal declaration turned into a parse error (kb/Work PB301): "
            + string.Join(", ", missing));
    }

    /// <summary>Every <c>NAME : 'WORD' ;</c> rule of <c>CobolLexer.g4</c> — the single-literal tokens, which are
    /// the ones a name slot can admit as a whole word. Comments stripped first.</summary>
    private static IReadOnlyDictionary<string, string> LexerWordTokens()
    {
        string g4 = StripComments(File.ReadAllText(
            TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolLexer.g4")));
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(g4, @"^\s*([A-Z][A-Z0-9_]*)\s*:\s*'([A-Za-z][A-Za-z0-9-]*)'\s*;",
                     RegexOptions.Multiline))
            map[m.Groups[1].Value] = m.Groups[2].Value.ToUpperInvariant();
        Assert.True(map.Count > 300, $"only {map.Count} single-literal lexer tokens — the CobolLexer.g4 scrape broke");
        return map;
    }

    /// <summary>The screen-only grammar rules that do NOT live in <c>CobolScreen.g4</c>: the SPECIAL-NAMES half
    /// of Annex A.4.2 item 25, whose words (CRT, CURSOR) are the reason this note had a reservation-gated arm at
    /// all. Each is scraped BY NAME and each MUST be found — see
    /// <see cref="EveryScreenRefusalSite_IsClassified"/> for the complement that stops this pair going stale.
    /// </summary>
    private static readonly string[] ExternalScreenSurfaceRules = ["crtStatusClause", "cursorClause"];

    /// <summary>⛔ THE COMPLEMENT THAT KEEPS <see cref="ExternalScreenSurfaceRules"/> FROM GOING STALE, AND THE
    /// ONE THAT MAKES THE NEXT SCREEN RULE AUTOMATIC. Every grammar rule <see cref="ScreenFacility"/> refuses —
    /// read off its <c>Report*</c> signatures' ANTLR context parameters plus its clause table, so a new refusal
    /// site cannot be added without landing in one of these classes — must be:
    /// <list type="number">
    /// <item>defined in <c>CobolScreen.g4</c> — screen-only surface, scraped whole, no listing needed;</item>
    /// <item>a member of <see cref="ExternalScreenSurfaceRules"/> — screen-only but written where its DIVISION
    /// puts it (the SPECIAL-NAMES clauses), scraped by name;</item>
    /// <item>a <c>screenClause</c> ALTERNATIVE defined in another grammar file — a clause the screen entry SHARES
    /// with the data description entry (PICTURE, VALUE, OCCURS, USAGE, SIGN, JUSTIFIED, BLANK WHEN ZERO, GLOBAL).
    /// Those are core language refused only because §13.17's containing entry is optional, and their words are
    /// their own module's, not this one's — <c>occursClause</c> alone would drag CAPACITY, STEP and INITIALIZED,
    /// all kb/Work PB655 class 4 — so they are deliberately outside this population;</item>
    /// <item>named in <see cref="RefusalSitesOutsideTheScreenSurface"/> with a reason.</item>
    /// </list>
    /// Anything else fails: a refusal site naming a rule NO grammar defines is a broken context-type convention
    /// or a deleted rule, and a screen-only rule put in a third file would otherwise carry its words outside
    /// every scrape — PB301's defect (a lexer token with no name slot) returning unseen.</summary>
    [Fact]
    public void EveryScreenRefusalSite_IsClassified()
    {
        var sites = RefusedGrammarRules();
        Assert.Contains("screenClause", sites);              // via the clause table
        Assert.Contains("cursorClause", sites);              // via a Report* context parameter
        Assert.Contains("optionsInitializeSection", sites);  // the excluded shape, so class 4 is never vacuous

        var definedIn = GrammarRuleDefinitions();
        var screenFile = Path.GetFileName(ScreenGrammarPath());
        var sharedAlternatives = ScreenClauseAlternatives().ToHashSet(StringComparer.Ordinal);

        var unclassified = sites.Where(r =>
                definedIn.GetValueOrDefault(r) != screenFile                               // 1
                && !ExternalScreenSurfaceRules.Contains(r, StringComparer.Ordinal)         // 2
                && !(definedIn.ContainsKey(r) && sharedAlternatives.Contains(r))           // 3
                && !RefusalSitesOutsideTheScreenSurface.Contains(r, StringComparer.Ordinal))
            .Order(StringComparer.Ordinal).ToList();
        Assert.True(unclassified.Count == 0,
            "ScreenFacility refuses grammar rule(s) that no screen-surface scrape reaches and that nothing "
            + "excludes — put each in CobolScreen.g4, add it to ExternalScreenSurfaceRules (its words then join "
            + "the population), or exclude it in RefusalSitesOutsideTheScreenSurface with a reason: "
            + string.Join(", ", unclassified) + ". A name defined in NO grammar file means the ANTLR "
            + "<rule>Context naming convention this derivation reads broke: "
            + string.Join(", ", unclassified.Where(r => !definedIn.ContainsKey(r))));

        var stale = ExternalScreenSurfaceRules.Concat(RefusalSitesOutsideTheScreenSurface)
            .Where(r => !sites.Contains(r)).Order(StringComparer.Ordinal).ToList();
        Assert.True(stale.Count == 0,
            "classified rule(s) ScreenFacility no longer refuses — a stale entry the population silently keeps "
            + "carrying: " + string.Join(", ", stale));
    }

    /// <summary>Rules the A.4.2 funnel refuses that are NOT screen surface, with the reason. Only ONE leg of
    /// <c>optionsInitializeSection</c> (§11.9.10 <c>INITIALIZE … SCREEN …</c>) is declined; the rule itself is
    /// core OPTIONS-paragraph language whose other words (LOCAL-STORAGE, WORKING-STORAGE, ALL) belong to no
    /// screen construct, so scraping its body would pull PB655's classes into this module's population. It is
    /// listed rather than caught by class 3 above because it is not a <c>screenClause</c> alternative.</summary>
    private static readonly string[] RefusalSitesOutsideTheScreenSurface = ["optionsInitializeSection"];

    /// <summary>Every grammar rule the A.4.2 funnel names: the ANTLR context type of each <c>Report*</c>
    /// parameter (<c>Core.CursorClauseContext</c> → <c>cursorClause</c>, ANTLR's own naming convention — a break
    /// in it fails the classification above, since the name would then match no rule in any <c>.g4</c>), plus the
    /// clause table's keys.</summary>
    private static IReadOnlySet<string> RefusedGrammarRules() =>
        typeof(ScreenFacility)
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name.StartsWith("Report", StringComparison.Ordinal))
            .SelectMany(m => m.GetParameters())
            .Select(p => p.ParameterType)
            .Where(t => typeof(ParserRuleContext).IsAssignableFrom(t)
                        && t.Name.EndsWith("Context", StringComparison.Ordinal))
            .Select(t => char.ToLowerInvariant(t.Name[0]) + t.Name[1..^"Context".Length])
            .Concat(ScreenFacility.CoveredClauseRules)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>Rule name → the <c>.g4</c> FILE NAME defining it, over every grammar in the frontend. A rule
    /// header is a lower-case identifier at the left margin, followed by its <c>:</c> on the same line or the
    /// next — both spellings are in use (<c>screenAutoClause : AUTO ;</c> and the block form).</summary>
    private static IReadOnlyDictionary<string, string> GrammarRuleDefinitions()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string path in Directory.EnumerateFiles(
                     TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "*.g4", SearchOption.AllDirectories))
            foreach (Match m in Regex.Matches(StripComments(File.ReadAllText(path)),
                         @"^([a-z][A-Za-z0-9_]*)\s*(?::|\r?\n\s*:)", RegexOptions.Multiline))
                map.TryAdd(m.Groups[1].Value, Path.GetFileName(path));
        Assert.True(map.Count > 200, $"only {map.Count} grammar rules found — the .g4 scrape broke");
        return map;
    }

    /// <summary>The <c>screenClause</c> rule's alternatives, read from the <c>.g4</c> text — the only place the
    /// list exists.</summary>
    private static List<string> ScreenClauseAlternatives()
    {
        var m = Regex.Match(File.ReadAllText(ScreenGrammarPath()),
            @"^screenClause\s*\r?\n(?<body>.*?)^\s*;\s*$", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(m.Success, "the screenClause rule was not found in CobolScreen.g4 — the drift gate is blind");
        var alts = Regex.Matches(m.Groups["body"].Value, @"^\s*[:|]\s*([A-Za-z_][A-Za-z0-9_]*)\s*$",
                RegexOptions.Multiline)
            .Select(x => x.Groups[1].Value).ToList();
        Assert.NotEmpty(alts);
        return alts;
    }

    private static string ScreenGrammarPath() =>
        TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolScreen.g4");

    private static string ScreenGrammar() => StripComments(File.ReadAllText(ScreenGrammarPath()));

    /// <summary>The population: every single-word lexer token the SCREEN surface references that ISO §8.9 leaves
    /// free at at least one edition. The surface is all of <c>CobolScreen.g4</c> plus each
    /// <see cref="ExternalScreenSurfaceRules"/> rule, scraped by name — and EACH must be found, because a scrape
    /// that silently returns one rule instead of two loses that rule's words from the population and every
    /// witness stays green (the whole-population floor below is a floor, not an exact count, so it cannot see a
    /// single rule go missing; measured 2026-09-06 by renaming <c>crtStatusClause</c>, which left this test
    /// PASSING before the per-rule assertion).</summary>
    private static IReadOnlyList<string> ScreenSurfaceWords()
    {
        string screen = ScreenGrammar();
        string specialNames = StripComments(File.ReadAllText(
            TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolSpecialNames.g4")));
        var surface = new StringBuilder(screen);
        foreach (string rule in ExternalScreenSurfaceRules)
        {
            var m = Regex.Match(specialNames, @"^" + rule + @"\s*\r?\n\s*:(?<body>.*?)^\s*;\s*$",
                RegexOptions.Multiline | RegexOptions.Singleline);
            Assert.True(m.Success, $"the screen-surface rule '{rule}' was not found in CobolSpecialNames.g4 — "
                + "the derivation is partial and every word that rule alone contributes is unguarded");
            surface.Append('\n').Append(m.Groups["body"].Value);
        }

        var tokens = LexerWordTokens();
        var referenced = Regex.Matches(surface.ToString(), @"\b([A-Z][A-Z0-9_]*)\b")
            .Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

        return [.. referenced.Where(tokens.ContainsKey).Select(t => tokens[t])
                             // Keep only what §8.9 leaves free somewhere: a word reserved at every edition
                             // (AT, IS, LINE, FILLER, SECTION …) was never a user word to lose, and belongs to
                             // no module — the whole-lexer join over those is kb/Work PB655.
                             .Where(w => ReservedWords.Find(w) is not { } e
                                         || !(e.IsReservedAt(85) && e.IsReservedAt(2002)
                                              && e.IsReservedAt(2014) && e.IsReservedAt(2023)))
                             .Order(StringComparer.Ordinal)];
    }

    private static string StripComments(string g4) =>
        Regex.Replace(Regex.Replace(g4, @"//[^\r\n]*", ""), @"/\*.*?\*/", "", RegexOptions.Singleline);

    /// <summary>ATTRIBUTE is §8.10 context-sensitive in the SET statement and is NOT a lexer token, so the
    /// SET format-6 arm is recognized by a left-edge text predicate (<c>setAttributeAhead</c>) — which means a
    /// data item actually NAMED <c>ATTRIBUTE</c> walks straight through that predicate. Both directions are
    /// pinned: <c>SET ATTRIBUTE TO 7</c> (the word in the RECEIVER slot, where the predicate must not fire) and
    /// <c>SET IDX TO ATTRIBUTE</c> (the word downstream, where the predicate DOES fire and the alternative must
    /// then fail to match and fall through to the ordinary SET forms). Values are asserted, not merely
    /// compilation — a predicate that stole the statement and bound a no-op would still "compile".</summary>
    [Fact]
    public void AttributeAsAUserWord_KeepsTheOrdinarySetForms()
    {
        var (ok, errors) = Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCRATTR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 ATTRIBUTE PIC 9(2) VALUE 3.
            01 N PIC 9(2) VALUE 0.
            01 T.
               05 TE PIC X OCCURS 5 TIMES INDEXED BY IDX.
            PROCEDURE DIVISION.
            MAIN.
                SET IDX TO 7.
                SET ATTRIBUTE TO IDX.
                SET IDX TO ATTRIBUTE.
                SET N TO IDX.
                DISPLAY ATTRIBUTE " " N.
                STOP RUN.
            """, 2023);
        Assert.True(ok, string.Join("\n", errors));
        Assert.False(Has(errors, "COBOLNET1707"));
    }

    /// <summary>The COBOL-85 complement of the CRT/CURSOR reservation gate: below 2002 those words are not
    /// reserved, so <c>SPECIAL-NAMES. CURSOR IS WS-C.</c> is an ordinary implementor-switch entry and must NOT
    /// be read as the screen module's CURSOR clause. At 2002+ the same text IS the clause and is refused.</summary>
    [Fact]
    public void SpecialNamesCursor_IsASwitchEntryAt85_AndTheScreenClauseAbove()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCRCUR.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CURSOR IS WS-CUR.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-CUR PIC 9(6).
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            """;
        var at85 = Compile(src, 85);
        Assert.False(Has(at85.Errors, "COBOLNET1560"),
            "at COBOL-85 CURSOR is not reserved (§8.9), so this is an implementor-switch entry and carries no "
            + "screen verdict:\n" + string.Join("\n", at85.Errors));
        var at2023 = Compile(src, 2023);
        Assert.True(Has(at2023.Errors, "COBOLNET1560: the SPECIAL-NAMES CURSOR clause"),
            "at 2002+ the same text IS the §12.3.7 CURSOR clause (Annex A.4.2 item 25):\n"
            + string.Join("\n", at2023.Errors));
    }
}
