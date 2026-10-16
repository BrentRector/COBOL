// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY PLACE THE FRONT END READS THE COMPILE EDITION IS ONE THE PB2291 VERDICT RECORDED (kb/Work PB2291;
/// docs/rearchitecture/DESIGN-architecture-review.md §8.4; COBOLNET_DESIGN.md §1.1, the D7 rule).
///
/// <para><b>The rule D7 states.</b> The grammar parses the SUPERSET of all editions and the ONE edition gate is the
/// post-bind <c>VersionConformancePass</c>. The front end nevertheless reads the edition in two ways that no post-bind
/// gate can replace — and, until PB2291, in one way it should not. A read either decides <b>word status</b> (ISO §8.9:
/// whether a word is RESERVED at this edition, so a legal earlier-edition program that uses LOCATION, RETRY or LOCALE as
/// a name keeps its reading; and the Annex C spelling a word has there) or it is a <b>construct gate</b> (a construct
/// whose words have no other reading below its introducing edition, refused at parse time instead of being named by
/// the conformance pass). Word-status reads are sanctioned; a construct gate is a D7 defect.</para>
///
/// <para><b>What this test computes, rather than lists</b> (§8.4's rule, applied):
/// <list type="number">
///   <item>EDITION STATE is the parser's <c>Edition</c> property, an <c>EditionInfo</c>, or a lexer field PRIMED from
///     one — the fields the retype stage's edition reads set through a lexer setter (computed from
///     <c>TokenRetypes</c>, never named here).</item>
///   <item>An EDITION READ is a method or property of the parser (<c>CobolParserCoreBase</c>, every committed
///     <c>CobolParserCore</c> partial and any parser grammar's <c>@members</c>), of the lexer (<c>CobolLexer.g4</c>'s
///     <c>@members</c> and its committed partials) or of the retype stage (<c>TokenRetypes</c>) whose syntax reaches
///     edition state, or that calls another read — to a fixpoint.</item>
///   <item>A SITE is a predicate or action body of any grammar that names a read OR edition state itself
///     (<c>{Edition.Has(2014)}?</c>, a primed lexer field); any C# member elsewhere in <c>src/</c> (the parser's
///     syntax-error listeners, the front end's driver) that calls or reads a read callable from outside its stage; a
///     front-end member that hands the edition to a parser (<c>Edition = …</c>); and any caller of such a member that
///     is not private (it passes the edition on).</item>
/// </list>
/// Outside the parser, edition state cannot be read at all: the parser's <c>Edition</c> getter is <c>protected</c>
/// (the compiler refuses <c>parser.Edition</c> in a listener or pass) and the lexer's primed fields are private, and
/// <see cref="TheParsersEdition_IsWriteOnlyOutsideIt"/> pins that, so the C# arm needs no type inference.
/// Both computed sets are compared, per file, with the inventory PB2291's verdict recorded below. A new read under
/// ANY name — a sixth <c>{isXXXX()}?</c> spelling, a new <c>reservedHere</c> site, a new member, a lexer field primed
/// from the edition, a new listener call — fails here until it is adjudicated and recorded, with its verdict.</para>
///
/// <para><b>Why a construct gate is still recorded.</b> PB2291 found three (<see cref="Verdict.ConstructGate"/>), all
/// on the declined Annex A.4 surface of <c>CobolDeclined.g4</c>: the §13.16.2 validation clauses, the VALUE clause's
/// §13.18.63 format-5 phrase and the §12.4.6.3 APPLY COMMIT clause. Below its introducing edition each is refused with a
/// bare COBOL0001 where D7 wants the conformance pass to name the edition (COBOLNET0900). Moving them needs a
/// declined-construct row form the version matrix does not have yet (a constructs.json row that is refused by name AT
/// and after its introduction), so they are pinned here as defects with their verdict rather than left unseen.
/// Removing one is a change to this table, which is the point: the table is the verdict, and the test keeps it equal
/// to the code.</para>
/// </summary>
public sealed class EditionReadDriftTests
{
    /// <summary>PB2291's two outcomes for a site.</summary>
    private enum Verdict
    {
        /// <summary>ISO §8.9 word status: whether a word is reserved at the edition (or, in the lexer, its Annex C
        /// spelling there). A superset parse cannot avoid it.</summary>
        WordStatus,

        /// <summary>A construct refused at parse time by edition: belongs in <c>VersionConformancePass</c>.</summary>
        ConstructGate,

        /// <summary>Decides nothing itself: hands the edition to a lexer or parser (or primes one), whose own reads are
        /// adjudicated at their own sites.</summary>
        Carrier,
    }

    // ── The verdict (kb/Work PB2291), recorded ────────────────────────────────────────────────────────────────────

    /// <summary>The edition-read MEMBERS each stage holds, as the rule computes them, keyed by the stage's principal
    /// source (the parser's key also covers its <c>CobolParserCore</c> partials, the lexer's its partials).</summary>
    private static readonly SortedDictionary<string, string[]> RecordedMembers = new(StringComparer.Ordinal)
    {
        ["src/Cobol.Net.Frontend/Parsing/CobolParserCoreBase.cs"] =
        [
            "IsKeywordReadingHere", "NoteGatedOffender", "ReservedGatedWord",
            "ReservedUserWordViolation", "entryNameHere", "facilityWord", "gatedDeclaration", "is2002", "is2014",
            "is2023", "is85", "keywordContinuesHere", "reservedHere", "screenPositionAhead", "setLocaleAhead",
            "userWordHere",
        ],
        ["src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4"] =
            ["ApplyEditionSpelling", "NextToken", "OnDefaultLParen", "PreviousTokenCouldBeDataName"],
        ["src/Cobol.Net.Frontend/Parsing/TokenRetypes.cs"] =
            ["LexesDifferentlyFrom", "PrimeLexer", "ReservedNonDataNames"],
    };

    /// <summary>Every SITE that calls a read: <c>file | rule or Type.Member | read</c> → its count and verdict.</summary>
    private static readonly SortedDictionary<string, (int Count, Verdict Verdict)> RecordedSites = new(StringComparer.Ordinal)
    {
        // PERFORM [WITH] LOCATION: bare LOCATION is the 2023 phrase only where §8.9 reserves the word; below 2023
        // `PERFORM LOCATION` is an out-of-line PERFORM of a paragraph named LOCATION.
        ["src/Cobol.Net.Frontend/Grammar/Core/CobolControlFlow.g4 | performLocationPhrase | is2023"] = (1, Verdict.WordStatus),
        // OPEN's RETRY phrase: RETRY is a §8.9 user word below 2002 and sits in the file-name list.
        ["src/Cobol.Net.Frontend/Grammar/Core/CobolIO.g4 | openClause | is2002"] = (1, Verdict.WordStatus),
        // APPLY COMMIT: APPLY is §8.10 context-sensitive at every edition and no edition before 2023 has an APPLY
        // clause, so no earlier program reads `APPLY COMMIT` another way: the gate refuses a construct (COBOL0001)
        // where the conformance pass should name its 2023 introduction.
        ["src/Cobol.Net.Frontend/Grammar/Core/CobolIO.g4 | ioControlClause | is2023"] = (1, Verdict.ConstructGate),
        // The §13.16.2 validation clauses: CLASS, DESTINATION, INVALID and VARYING are reserved at every edition and
        // have no data-description reading below 2002, so at --std 85 the gate turns a construct the conformance pass
        // should name into COBOL0001. (DEFAULT, PRESENT, VALIDATE-STATUS and VAL-STATUS are §8.9 user words there,
        // so the replacement keeps a word-status read on those four arms.)
        ["src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4 | dataDescriptionClause | is2002"] = (1, Verdict.ConstructGate),
        // The VALUE clause's format-5 tail `[IS|ARE] {INVALID|VALID}`: INVALID is reserved at every edition (a
        // construct gate below 2002); VALID is a §8.9 user word below 2002 and can be a VALUE operand there (a
        // symbolic-character, §8.3.3.6.3 SR1), which a word-status read must keep.
        ["src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4 | valueClause | is2002"] = (1, Verdict.ConstructGate),
        // SET LOCALE: setLocaleAhead's `|| Edition.Has(2002)` decides whether LOCALE and USER-DEFAULT are §8.9 user
        // words (two receivers of a COBOL-85 SET) or the Format-11 keywords.
        ["src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4 | setLocaleStatement | setLocaleAhead"] = (1, Verdict.WordStatus),
        // The declined facility statements VALIDATE, COMMIT and ROLLBACK: is the verb reserved here.
        ["src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4 | statement | facilityWord"] = (3, Verdict.WordStatus),
        // DISPLAY's operand loop stops at COL / COLS / COLUMNS only where §8.9 reserves them (kb/Work PB260).
        ["src/Cobol.Net.Frontend/Grammar/CobolParserCore.g4 | displayStatement | screenPositionAhead"] = (1, Verdict.WordStatus),
        // The SPECIAL-NAMES CRT STATUS and CURSOR clauses: CRT and CURSOR are reserved from 2002.
        ["src/Cobol.Net.Frontend/Grammar/Core/CobolSpecialNames.g4 | specialNameEntry | reservedHere"] = (2, Verdict.WordStatus),
        // The generated user-word slot (single-sourced from cobol-words.json): a keyword that continues the
        // enclosing format is not a user word here, and a declared reservation-gated word is recorded.
        ["src/Cobol.Net.Frontend/Grammar/Core/CobolWords.g4 | cobolWord | keywordContinuesHere"] = (67, Verdict.WordStatus),
        // The optional entry-name yields to the clause a word §8.9 reserves here begins (kb/Work PB845, PB2501).
        ["src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4 | dataDescriptionEntry | entryNameHere"] = (1, Verdict.WordStatus),
        ["src/Cobol.Net.Frontend/Grammar/Core/CobolWords.g4 | reservedGatedWord | gatedDeclaration"] = (1, Verdict.WordStatus),
        // The lexer: the Annex C spelling of a word at the edition, and whether a '(' after a word the edition
        // reserves can open a subscript (kb/Work PB1465).
        ["src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4 | LPAREN | OnDefaultLParen"] = (1, Verdict.WordStatus),
        // The listeners: the authoritative pass's syntax-error listener reports and retypes a reservation-gated word.
        ["src/Cobol.Net.Frontend/Parsing/CobolErrorListener.cs | CobolErrorListener.SyntaxError | ReservedUserWordViolation"] = (1, Verdict.WordStatus),
        ["src/Cobol.Net.Frontend/Parsing/ReservationGateRewriter.cs | GatedOffenderWitness.SyntaxError | NoteGatedOffender"] = (1, Verdict.WordStatus),
        // The front end's driver decides whether a re-parse lexes differently because a word the program declared is
        // reserved at the edition.
        ["src/Cobol.Net.Frontend/Pipeline/Frontend.cs | Frontend.LexAndParse | LexesDifferentlyFrom"] = (1, Verdict.WordStatus),
        // The carriers: the driver and the fragment parser prime each lexer and hand each parser the edition, and the
        // fragment parser's callers pass one on (a directive expression parses at the newest edition's word status).
        ["src/Cobol.Net.Frontend/Pipeline/Frontend.cs | Frontend.LexAndParseOnce | PrimeLexer"] = (1, Verdict.Carrier),
        ["src/Cobol.Net.Frontend/Pipeline/Frontend.cs | Frontend.LexAndParseOnce | Edition="] = (1, Verdict.Carrier),
        ["src/Cobol.Net.Frontend/Parsing/FragmentParse.cs | FragmentParse.Parse | PrimeLexer"] = (1, Verdict.Carrier),
        ["src/Cobol.Net.Frontend/Parsing/FragmentParse.cs | FragmentParse.Parse | Edition="] = (1, Verdict.Carrier),
        ["src/Cobol.Net.Frontend/Parsing/FragmentParse.cs | FragmentParse.ParseTokens | Edition="] = (1, Verdict.Carrier),
        ["src/Cobol.Net.Frontend/Parsing/DirectiveExpressionFragment.cs | DirectiveExpressionFragment.Parse | FragmentParse.Parse"] = (1, Verdict.Carrier),
        ["src/Cobol.Net.Compiler/Oo/OoExpansion.cs | OoExpansion.Expand | FragmentParse.ParseTokens"] = (2, Verdict.Carrier),
    };

    // ── The rule, applied ─────────────────────────────────────────────────────────────────────────────────────────

    private const string ParserBase = "src/Cobol.Net.Frontend/Parsing/CobolParserCoreBase.cs";
    private const string LexerGrammar = "src/Cobol.Net.Frontend/Grammar/Core/CobolLexer.g4";
    private const string RetypeStage = "src/Cobol.Net.Frontend/Parsing/TokenRetypes.cs";

    [Fact]
    public void EditionReads_AreExactlyTheAdjudicatedSet()
    {
        var model = EditionReadModel.Compute();

        var computedMembers = new SortedDictionary<string, string[]>(StringComparer.Ordinal)
        {
            [ParserBase] = [.. model.ParserReads.Keys.Order(StringComparer.Ordinal)],
            [LexerGrammar] = [.. model.LexerReads.Keys.Order(StringComparer.Ordinal)],
            [RetypeStage] = [.. model.RetypeReads.Keys.Order(StringComparer.Ordinal)],
        };
        var problems = new List<string>();
        foreach (var (file, members) in computedMembers)
        {
            var recorded = RecordedMembers.GetValueOrDefault(file) ?? [];
            foreach (var m in members.Except(recorded)) problems.Add($"NEW edition read {file}#{m}");
            foreach (var m in recorded.Except(members)) problems.Add($"STALE recorded read {file}#{m}");
        }

        foreach (var (site, count) in model.Sites)
        {
            if (!RecordedSites.TryGetValue(site, out var r)) problems.Add($"NEW site {site} ×{count}");
            else if (r.Count != count) problems.Add($"site {site}: recorded ×{r.Count}, computed ×{count}");
        }

        foreach (var site in RecordedSites.Keys.Where(s => !model.Sites.ContainsKey(s)))
            problems.Add($"STALE recorded site {site}");

        Assert.True(problems.Count == 0,
            "the front end's edition reads drifted from the PB2291 verdict (kb/Work PB2291; DESIGN-architecture-review "
            + "§8.4). Adjudicate each one — ISO §8.9 word status, or a construct gate that belongs in "
            + "VersionConformancePass — and record it:\n  " + string.Join("\n  ", problems)
            + "\n\nComputed inventory:\n" + model.Render());
    }

    /// <summary>The rule is not vacuous: each stage it reads contributes reads, each kind of site is found, and the
    /// lexer's primed fields are derived from the retype stage rather than assumed. A scanner that silently stopped
    /// finding bodies or members would empty the computed sets, and the comparison above would then report only
    /// STALE rows — this states the population directly.</summary>
    [Fact]
    public void TheRule_FindsReadsInEveryStage()
    {
        var model = EditionReadModel.Compute();
        Assert.Contains("is2002", model.ParserReads.Keys);
        Assert.Contains("ApplyEditionSpelling", model.LexerReads.Keys);
        Assert.Contains("PrimeLexer", model.RetypeReads.Keys);
        Assert.Equal(["_caseMappingEdition", "_reservedNonNames"], model.PrimedLexerFields.Order(StringComparer.Ordinal));
        Assert.Contains(model.Sites.Keys, s => s.EndsWith(".g4 | cobolWord | keywordContinuesHere", StringComparison.Ordinal));
        Assert.Contains(model.Sites.Keys, s => s.EndsWith(".cs | GatedOffenderWitness.SyntaxError | NoteGatedOffender", StringComparison.Ordinal));
        // The fixpoint is real: NextToken reaches the edition only through ApplyEditionSpelling.
        Assert.Contains("NextToken", model.LexerReads.Keys);
    }

    /// <summary>Every recorded site carries a verdict, and the construct gates are exactly the ones PB2291 found —
    /// a new one is a D7 defect to fix, never a row to add (CLAUDE.md rule 4).</summary>
    [Fact]
    public void ConstructGates_AreOnlyTheOnesPb2291Found()
    {
        string[] gates = [.. RecordedSites.Where(kv => kv.Value.Verdict == Verdict.ConstructGate).Select(kv => kv.Key)];
        Assert.Equal(
        [
            "src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4 | dataDescriptionClause | is2002",
            "src/Cobol.Net.Frontend/Grammar/Core/CobolData.g4 | valueClause | is2002",
            "src/Cobol.Net.Frontend/Grammar/Core/CobolIO.g4 | ioControlClause | is2023",
        ], gates);
    }

    /// <summary>Edition STATE named directly is a site too, not only a call of a read (review finding w1092e R1-1): a
    /// grammar predicate spelling <c>{Edition.Has(2014)}?</c>, a lexer action naming a primed field, and a parser
    /// partial or <c>@members</c> member that reads <c>Edition</c> are each found; and a C# use of an outside-callable
    /// read is found whether it is called or read as a property. Applied to synthetic text, so the rule's reach is
    /// stated without a construct gate in the tree.</summary>
    [Fact]
    public void TheRule_SeesEditionStateNamedDirectly()
    {
        var model = EditionReadModel.Compute();

        var parserReads = EditionReadModel.ParserReadsOf(
            EditionReadModel.ParserMembersOf("protected bool newGate() => Edition.Has(2014);"));
        Assert.Equal(["newGate"], parserReads.Keys);

        const string parserGrammar = "parser grammar T;\nr\n    : {Edition.Has(2014)}? a\n    | {newGate()}? b\n    ;\n";
        Assert.Equal([("r", "Edition"), ("r", "newGate")],
            EditionReadModel.GrammarSites(parserGrammar, parserReads, EditionReadModel.ParserState));

        string primedField = model.PrimedLexerFields.Order(StringComparer.Ordinal).First();
        string lexerGrammar = $"lexer grammar L;\nX : 'x' {{{primedField} >= 2002}}? ;\n";
        Assert.Equal([("X", primedField)],
            EditionReadModel.GrammarSites(lexerGrammar, new Dictionary<string, bool>(), model.PrimedLexerFields));

        var root = CSharpSyntaxTree.ParseText(
            "class L { void M(P p) { var y = p.Level; p.Level = 2; p?.Note(); p.Note(); } }").GetRoot();
        Assert.Equal(["Level", "Note", "Note"], EditionReadModel.OutsideUses(root, ["Level", "Note"]).Select(u => u.Name));
    }

    /// <summary>Outside the parser and lexer, edition state cannot be read at all, which is what lets the C# arm of
    /// the rule look only for reads: the parser's <c>Edition</c> getter is protected (a listener writing
    /// <c>parser.Edition.Has(2002)</c> does not compile) while the front end can still set it, and every primed lexer
    /// field is private.</summary>
    [Fact]
    public void TheParsersEdition_IsWriteOnlyOutsideIt()
    {
        var edition = typeof(CobolParserCoreBase).GetProperty(nameof(CobolParserCoreBase.Edition));
        Assert.NotNull(edition);
        Assert.True(edition.GetMethod is { IsFamily: true },
            "CobolParserCoreBase.Edition's getter must be protected: an outside read is an edition read no inventory sees");
        Assert.True(edition.SetMethod is { IsPublic: true }, "the front end hands the parser its edition");

        var model = EditionReadModel.Compute();
        foreach (string field in model.PrimedLexerFields)
            Assert.True(typeof(CobolLexer).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic) is { IsPrivate: true },
                $"CobolLexer.{field} is primed from the edition and must stay private");
    }

    /// <summary>The computed model: the reads of each stage, the primed lexer fields and the sites.</summary>
    private sealed class EditionReadModel
    {
        public required IReadOnlyDictionary<string, bool> ParserReads { get; init; }   // name → callable from outside
        public required IReadOnlyDictionary<string, bool> LexerReads { get; init; }
        public required IReadOnlyDictionary<string, bool> RetypeReads { get; init; }
        public required IReadOnlySet<string> PrimedLexerFields { get; init; }
        public required SortedDictionary<string, int> Sites { get; init; }

        public string Render()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"  {ParserBase}: {string.Join(", ", ParserReads.Keys.Order(StringComparer.Ordinal))}");
            sb.AppendLine($"  {LexerGrammar}: {string.Join(", ", LexerReads.Keys.Order(StringComparer.Ordinal))}");
            sb.AppendLine($"  {RetypeStage}: {string.Join(", ", RetypeReads.Keys.Order(StringComparer.Ordinal))}");
            foreach (var (site, n) in Sites) sb.AppendLine($"  {site} ×{n}");
            return sb.ToString();
        }

        public static EditionReadModel Compute()
        {
            // The parser: the superclass, every committed partial of the generated parser and any parser grammar's
            // @members — their members that reach the parser's Edition property.
            var parserBodies = new List<MemberDeclarationSyntax>(ClassBodies(File.ReadAllText(Repo(ParserBase)), "CobolParserCoreBase"));
            foreach (var path in SourceFiles().Where(p => IsPartialOf(File.ReadAllText(p), ParserType)))
                parserBodies.AddRange(ClassBodies(File.ReadAllText(path), ParserType));
            foreach (var path in Grammars().Where(p => Rel(p) != LexerGrammar))
                foreach (var (_, body) in NamedActions(File.ReadAllText(path), "members"))
                    parserBodies.AddRange(ParserMembersOf(body));
            var parserReads = ParserReadsOf(parserBodies);

            // The retype stage: its members whose syntax names an EditionInfo.
            var retypeMembers = Members(ClassBodies(File.ReadAllText(Repo(RetypeStage)), "TokenRetypes"));
            var retypeReads = Fixpoint(retypeMembers, ids => ids.Contains("EditionInfo"), exclude: []);

            // The lexer: @members plus every committed partial of CobolLexer.
            string lexerText = File.ReadAllText(Repo(LexerGrammar));
            var lexerBodies = new List<MemberDeclarationSyntax>();
            foreach (var (_, body) in NamedActions(lexerText, "members"))
                lexerBodies.AddRange(ClassBodies($"partial class CobolLexer {{ {body} }}", "CobolLexer"));
            foreach (var path in SourceFiles().Where(p => IsPartialOf(File.ReadAllText(p), LexerType)))
                lexerBodies.AddRange(ClassBodies(File.ReadAllText(path), LexerType));
            var lexerMembers = Members(lexerBodies);
            var lexerFields = lexerBodies.OfType<FieldDeclarationSyntax>()
                .SelectMany(f => f.Declaration.Variables.Select(v => v.Identifier.Text)).ToHashSet(StringComparer.Ordinal);

            // The primed fields: a retype read calls a lexer SETTER (an invocation on a CobolLexer parameter) with an
            // argument that reaches its EditionInfo parameter, and the lexer fields that setter's body names are edition
            // state from then on. (A setter fed only from other state — the >>COBOL-WORDS plan — primes nothing.)
            var setters = new HashSet<string>(StringComparer.Ordinal);
            foreach (var m in retypeMembers.Where(m => retypeReads.ContainsKey(m.Key)).SelectMany(m => m.Value.Nodes))
            {
                var parameters = m.DescendantNodes().OfType<ParameterSyntax>().ToList();
                var lexerParams = parameters.Where(p => p.Type?.ToString() == "CobolLexer")
                    .Select(p => p.Identifier.Text).ToHashSet(StringComparer.Ordinal);
                var editionParams = parameters.Where(p => p.Type?.ToString() == "EditionInfo")
                    .Select(p => p.Identifier.Text).ToHashSet(StringComparer.Ordinal);
                foreach (var inv in m.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    if (inv.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax recv } ma
                        && lexerParams.Contains(recv.Identifier.Text)
                        && inv.ArgumentList.DescendantNodes().OfType<IdentifierNameSyntax>()
                              .Any(id => editionParams.Contains(id.Identifier.Text)))
                        setters.Add(ma.Name.Identifier.Text);
            }

            var primed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var s in setters)
                if (lexerMembers.TryGetValue(s, out var info))
                    primed.UnionWith(info.Identifiers.Where(lexerFields.Contains));
            var lexerReads = Fixpoint(lexerMembers, ids => ids.Overlaps(primed), exclude: setters);

            // Sites, 1: the predicate and action bodies of every grammar — a read, or the stage's edition state named
            // directly (the parser's Edition, a primed lexer field).
            var sites = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var path in Grammars())
            {
                string rel = Rel(path);
                bool lexer = rel == LexerGrammar;
                foreach (var (rule, id) in GrammarSites(File.ReadAllText(path), lexer ? lexerReads : parserReads,
                             lexer ? primed : ParserState))
                    Bump(sites, $"{rel} | {rule} | {id}");
            }

            // Sites, 2: every other C# member in src/ that calls or reads a read callable from outside its stage, and
            // every front-end DRIVER member that hands edition state to a parser (`Edition = …`) — plus, one level up,
            // each call of a non-private driver member that does so, which passes edition state on (the fragment
            // parser's callers). Edition state itself is unreadable here: the parser's getter is protected and the
            // primed lexer fields are private (TheParsersEdition_IsWriteOnlyOutsideIt).
            string[] outside = [.. parserReads.Concat(lexerReads).Concat(retypeReads).Where(kv => kv.Value).Select(kv => kv.Key).Distinct()];
            var memberSources = new HashSet<string>([ParserBase, RetypeStage], StringComparer.Ordinal);
            var trees = new List<(string Rel, SyntaxNode Root)>();
            foreach (var path in SourceFiles())
            {
                string rel = Rel(path);
                if (memberSources.Contains(rel)) continue;
                string text = File.ReadAllText(path);
                if (IsPartialOf(text, LexerType) || IsPartialOf(text, ParserType)) continue;
                bool frontEnd = rel.StartsWith("src/Cobol.Net.Frontend/", StringComparison.Ordinal);
                if (!outside.Any(n => text.Contains(n, StringComparison.Ordinal)) && !frontEnd) continue;
                trees.Add((rel, CSharpSyntaxTree.ParseText(text).GetRoot()));
            }

            var drivers = new HashSet<string>(StringComparer.Ordinal);   // "Type.Member" of a non-private hand-off
            foreach (var (rel, root) in trees)
            {
                foreach (var (node, name) in OutsideUses(root, outside))
                    Bump(sites, $"{rel} | {SiteOf(node)} | {name}");

                if (!rel.StartsWith("src/Cobol.Net.Frontend/", StringComparison.Ordinal)) continue;
                foreach (var a in root.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                {
                    string? target = a.Left switch
                    {
                        IdentifierNameSyntax id when a.Parent is InitializerExpressionSyntax => id.Identifier.Text,
                        MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text,
                        _ => null,
                    };
                    if (target is null || !ParserState.Contains(target)) continue;
                    Bump(sites, $"{rel} | {SiteOf(a)} | {target}=");
                    if (a.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault(m => m is not BaseTypeDeclarationSyntax) is { } md
                        && !md.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
                        drivers.Add(SiteOf(a));
                }
            }

            foreach (var path in SourceFiles())
            {
                string text = File.ReadAllText(path);
                if (!drivers.Any(d => text.Contains(d, StringComparison.Ordinal))) continue;
                string rel = Rel(path);
                foreach (var inv in CSharpSyntaxTree.ParseText(text).GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
                    if (inv.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax type } ma
                        && drivers.Contains($"{type.Identifier.Text}.{ma.Name.Identifier.Text}")
                        && SiteOf(inv) is var site && !drivers.Contains(site))
                        Bump(sites, $"{rel} | {site} | {type.Identifier.Text}.{ma.Name.Identifier.Text}");
            }

            return new EditionReadModel
            {
                ParserReads = parserReads, LexerReads = lexerReads, RetypeReads = retypeReads,
                PrimedLexerFields = primed, Sites = sites,
            };
        }

        private static void Bump(SortedDictionary<string, int> d, string key) => d[key] = d.GetValueOrDefault(key) + 1;

        /// <summary>The parser's edition state: the one property every parser read reaches, excluded from the reads
        /// themselves and named directly by a site.</summary>
        public static readonly IReadOnlySet<string> ParserState = new HashSet<string>(["Edition"], StringComparer.Ordinal);

        private const string ParserType = "CobolParserCore";
        private const string LexerType = "CobolLexer";

        /// <summary>The parser's edition reads among <paramref name="bodies"/>: the members whose syntax reaches its
        /// <see cref="ParserState"/>, to a fixpoint.</summary>
        public static Dictionary<string, bool> ParserReadsOf(IEnumerable<MemberDeclarationSyntax> bodies) =>
            Fixpoint(Members(bodies), ids => ids.Overlaps(ParserState), exclude: [.. ParserState]);

        /// <summary>The members a parser grammar's <c>@members</c> (or a committed partial's body) adds.</summary>
        public static IEnumerable<MemberDeclarationSyntax> ParserMembersOf(string body) =>
            ClassBodies($"partial class {ParserType} {{ {body} }}", ParserType);

        /// <summary>Every <c>(rule, name)</c> where a predicate or action body of <paramref name="g4"/> names a read
        /// or the edition state itself.</summary>
        public static IEnumerable<(string Rule, string Name)> GrammarSites(string g4, IReadOnlyDictionary<string, bool> reads,
            IReadOnlySet<string> state) =>
            RuleActions(g4).SelectMany(a => Identifiers(a.Body).Where(id => reads.ContainsKey(id) || state.Contains(id))
                .Select(id => (a.Rule, id)));

        /// <summary>Every use of an outside-callable read in <paramref name="root"/> — called (<c>x.M()</c>,
        /// <c>x?.M()</c>) or read as a property (<c>x.P</c>) — but not an assignment to it.</summary>
        public static IEnumerable<(SyntaxNode Node, string Name)> OutsideUses(SyntaxNode root, IReadOnlyCollection<string> outside)
        {
            foreach (var node in root.DescendantNodes())
            {
                string? name = node switch
                {
                    MemberAccessExpressionSyntax ma => ma.Name.Identifier.Text,
                    MemberBindingExpressionSyntax mb => mb.Name.Identifier.Text,
                    _ => null,
                };
                if (name is null || !outside.Contains(name)) continue;
                if (node.Parent is AssignmentExpressionSyntax a && a.Left == node) continue;
                yield return (node, name);
            }
        }

        private static bool IsPartialOf(string code, string type) =>
            Regex.IsMatch(code, $@"\bpartial\s+class\s+{type}\b");

        private static IEnumerable<string> Grammars() =>
            Directory.GetFiles(Repo("src/Cobol.Net.Frontend/Grammar"), "*.g4", SearchOption.AllDirectories)
                .Where(IsSourcePath).Order(StringComparer.Ordinal);

        /// <summary><c>Type.Member</c> enclosing <paramref name="node"/>.</summary>
        private static string SiteOf(SyntaxNode node)
        {
            string type = node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ?? "?";
            string member = node.Ancestors().OfType<MemberDeclarationSyntax>()
                .FirstOrDefault(a => a is not BaseTypeDeclarationSyntax) is { } md ? MemberName(md) ?? "?" : "?";
            return $"{type}.{member}";
        }

        private static string Repo(string rel) => Path.Combine([TestRepo.Root, .. rel.Split('/')]);

        private static string Rel(string path) => Path.GetRelativePath(TestRepo.Root, path).Replace('\\', '/');

        private static bool IsSourcePath(string p)
        {
            string r = Rel(p);
            return !r.Contains("/obj/", StringComparison.Ordinal) && !r.Contains("/bin/", StringComparison.Ordinal)
                && !r.Contains("/Generated/", StringComparison.Ordinal);
        }

        private static IEnumerable<string> SourceFiles() =>
            Directory.GetFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories).Where(IsSourcePath).Order(StringComparer.Ordinal);

        /// <summary>The direct members of every declaration of <paramref name="type"/> in <paramref name="code"/>.</summary>
        private static IEnumerable<MemberDeclarationSyntax> ClassBodies(string code, string type) =>
            CSharpSyntaxTree.ParseText(code).GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>()
                .Where(t => t.Identifier.Text == type).SelectMany(t => t.Members);

        private sealed record MemberInfo(List<MemberDeclarationSyntax> Nodes, HashSet<string> Identifiers, bool Outside);

        /// <summary>Methods and properties by name (overloads merged): the identifiers their syntax names, and whether
        /// anything outside the type can call them.</summary>
        private static Dictionary<string, MemberInfo> Members(IEnumerable<MemberDeclarationSyntax> nodes)
        {
            var result = new Dictionary<string, MemberInfo>(StringComparer.Ordinal);
            foreach (var n in nodes.Where(n => n is MethodDeclarationSyntax or PropertyDeclarationSyntax))
            {
                string name = MemberName(n)!;
                bool outside = !n.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))
                               && n.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.InternalKeyword));
                if (!result.TryGetValue(name, out var info))
                    result[name] = info = new MemberInfo([], new HashSet<string>(StringComparer.Ordinal), outside);
                info.Nodes.Add(n);
                info.Identifiers.UnionWith(n.DescendantNodes().OfType<IdentifierNameSyntax>().Select(i => i.Identifier.Text));
                if (outside && !info.Outside) result[name] = info with { Outside = true };
            }

            return result;
        }

        private static string? MemberName(MemberDeclarationSyntax n) => n switch
        {
            MethodDeclarationSyntax m => m.Identifier.Text,
            PropertyDeclarationSyntax p => p.Identifier.Text,
            ConstructorDeclarationSyntax c => c.Identifier.Text,
            FieldDeclarationSyntax f => f.Declaration.Variables.FirstOrDefault()?.Identifier.Text,
            _ => null,
        };

        /// <summary>The members whose syntax reaches edition state (<paramref name="seed"/>) or names another such
        /// member, to a fixpoint: name → callable from outside the stage.</summary>
        private static Dictionary<string, bool> Fixpoint(Dictionary<string, MemberInfo> members,
            Func<HashSet<string>, bool> seed, IReadOnlyCollection<string> exclude)
        {
            var reads = new Dictionary<string, bool>(StringComparer.Ordinal);
            bool grew = true;
            while (grew)
            {
                grew = false;
                foreach (var (name, info) in members)
                {
                    if (reads.ContainsKey(name) || exclude.Contains(name)) continue;
                    if (seed(info.Identifiers) || info.Identifiers.Any(i => i != name && reads.ContainsKey(i)))
                    {
                        reads[name] = info.Outside;
                        grew = true;
                    }
                }
            }

            return reads;
        }

        /// <summary>The identifiers a predicate or action body names outside its string and character literals.</summary>
        private static IEnumerable<string> Identifiers(string body)
        {
            var root = CSharpSyntaxTree.ParseText($"class __B {{ object __m() {{ return {body.Trim().TrimEnd(';')}; }} }}").GetRoot();
            // An action is a statement list rather than an expression; parse it as a block when the expression form fails.
            if (root.ContainsDiagnostics)
                root = CSharpSyntaxTree.ParseText($"class __B {{ void __m() {{ {body} }} }}").GetRoot();
            return root.DescendantNodes().OfType<IdentifierNameSyntax>().Select(i => i.Identifier.Text)
                .Where(i => i is not ("__B" or "__m"));
        }

        /// <summary>Every named action <c>@<paramref name="name"/> { … }</c> of a grammar.</summary>
        private static IEnumerable<(string Rule, string Body)> NamedActions(string g4, string name) =>
            Blocks(g4).Where(b => b.Kind == BlockKind.Named && b.Label == name).Select(b => (b.Label, b.Body));

        /// <summary>Every predicate <c>{…}?</c> and action <c>{…}</c> inside a rule, with the rule's name.</summary>
        private static IEnumerable<(string Rule, string Body)> RuleActions(string g4) =>
            Blocks(g4).Where(b => b.Kind == BlockKind.Rule).Select(b => (b.Label, b.Body));

        private enum BlockKind { Rule, Named, Other }

        /// <summary>A small scanner over ANTLR grammar text: skips comments, quoted literals and lexer character sets,
        /// tracks the rule a column-0 identifier opens, and yields every brace block — a named action (<c>@members</c>),
        /// an <c>options</c>/<c>tokens</c>/<c>channels</c> block, or a predicate or action inside a rule.</summary>
        private static IEnumerable<(BlockKind Kind, string Label, string Body)> Blocks(string g4)
        {
            string rule = "";
            string lastWord = "";
            bool fragmentPending = false;
            int i = 0;
            while (i < g4.Length)
            {
                char c = g4[i];
                if (c == '/' && i + 1 < g4.Length && g4[i + 1] == '/') { while (i < g4.Length && g4[i] != '\n') i++; continue; }
                if (c == '/' && i + 1 < g4.Length && g4[i + 1] == '*') { int e = g4.IndexOf("*/", i + 2, StringComparison.Ordinal); i = e < 0 ? g4.Length : e + 2; continue; }
                if (c is '\'' or '[') { i = SkipQuoted(g4, i, c == '[' ? ']' : '\''); continue; }
                if (char.IsLetter(c) || c == '_' || c == '@')
                {
                    int s = i;
                    // A named action's label may carry a scope (`@lexer::members`); no other word holds a colon.
                    while (i < g4.Length && (char.IsLetterOrDigit(g4[i]) || g4[i] is '_' or '@' || (g4[s] == '@' && g4[i] == ':'))) i++;
                    string word = g4[s..i];
                    bool col0 = s == 0 || g4[s - 1] == '\n';
                    if (fragmentPending) { rule = word; fragmentPending = false; }
                    else if (col0 && word == "fragment") fragmentPending = true;
                    else if (col0 && word is not ("parser" or "lexer" or "grammar" or "options" or "tokens" or "channels"
                                 or "import" or "mode") && !word.StartsWith('@'))
                        rule = word;
                    lastWord = word;
                    continue;
                }

                if (c == '{')
                {
                    int end = MatchBrace(g4, i);
                    string body = g4[(i + 1)..end];
                    var kind = lastWord.StartsWith('@') ? BlockKind.Named
                        : lastWord is "options" or "tokens" or "channels" ? BlockKind.Other : BlockKind.Rule;
                    string label = kind == BlockKind.Named ? lastWord[(lastWord.LastIndexOf(':') + 1)..].TrimStart('@') : rule;
                    yield return (kind, label, body);
                    i = end + 1;
                    lastWord = "";
                    continue;
                }

                if (!char.IsWhiteSpace(c)) lastWord = c.ToString();
                i++;
            }
        }

        private static int SkipQuoted(string s, int i, char close)
        {
            for (int j = i + 1; j < s.Length; j++)
            {
                if (s[j] == '\\') { j++; continue; }
                if (s[j] == close) return j + 1;
            }

            return s.Length;
        }

        /// <summary>The index of the brace closing the block opened at <paramref name="open"/>, skipping C# string,
        /// character and comment syntax inside it.</summary>
        private static int MatchBrace(string s, int open)
        {
            int depth = 0;
            for (int j = open; j < s.Length; j++)
            {
                char c = s[j];
                if (c == '/' && j + 1 < s.Length && s[j + 1] == '/') { while (j < s.Length && s[j] != '\n') j++; continue; }
                if (c == '/' && j + 1 < s.Length && s[j + 1] == '*') { int e = s.IndexOf("*/", j + 2, StringComparison.Ordinal); j = e < 0 ? s.Length : e + 1; continue; }
                if (c == '"') { j = SkipQuoted(s, j, '"') - 1; continue; }
                if (c == '\'') { j = SkipQuoted(s, j, '\'') - 1; continue; }
                if (c == '{') depth++;
                else if (c == '}' && --depth == 0) return j;
            }

            throw new InvalidOperationException($"unbalanced brace block at offset {open}");
        }
    }
}
