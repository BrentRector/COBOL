// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Generated;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A CONSTANT-NAME STANDS WHEREVER A FORMAT SPECIFIES A LITERAL, AND THE GRAMMAR PROVES IT POSITION BY POSITION
/// (kb/Work PB780). ISO §13.10.3 SR2: "Except in a compiler directive, constant-name-1 may be used anywhere that a
/// format specifies a literal of the class and category of constant-name-1." Its subject is every literal position of
/// every general format, so no sample of programs can show it holds; fourteen positions refused a constant-name while
/// the literal control passed (the w1033 adjudication), in two kinds: a grammar slot written as the bare literal rule
/// (<c>COLONCOLON literal</c>, the VALUE table subscripts, OPTIONS INITIALIZE … TO literal-1) and a slot written
/// <c>literal | dataReference</c> whose binder arm resolved the word as an identifier and never asked the constant
/// table (CALL, CANCEL, ADDRESS OF PROGRAM, CODE, INVOKE). So both halves are DERIVED here from the grammar text:
/// <list type="bullet">
/// <item>every literal reference that sits in a block offering no word alternative is a position a constant-name cannot
/// be written in, and must be argued in <see cref="NotAConstantPosition"/> as a position no constant-name can stand in;</item>
/// <item>every literal reference that sits beside a word alternative is a SLOT, and an enabled positive golden of the
/// conformance corpus must write a declared constant-name in that slot's word arm (it compiles strict and matches its
/// output, so the binder arm admitted the word as the literal), or the slot is argued in <see cref="Unwitnessed"/>.</item>
/// </list>
/// The integer positions are held by the same rule elsewhere (<see cref="IntegerOperandSlotDriftTests"/>: an
/// <c>integer-n</c> writes <c>integerOperand</c>); this class owns the alphanumeric, national, boolean and numeric
/// literal positions. ⚠ A witness is per grammar RULE: a rule that carries two literal slots (DISPLAY's operands and
/// its UPON) is witnessed by a constant-name in either word arm.
/// </summary>
public sealed class ConstantNameLiteralPositionDriftTests
{
    /// <summary>The rules that name a literal position: a reference to one of them in a format's rule is a place the
    /// format specifies a literal. <c>integerLiteral</c> is the <c>integer-n</c> spelling, owned by
    /// <see cref="IntegerOperandSlotDriftTests"/>.</summary>
    private static readonly HashSet<string> LiteralRules =
        ["literal", "nonNumericLiteral", "numericLiteral", "signedNumericLiteral", "signedIntegerLiteral"];

    /// <summary>The literal tokens, which a rule may also reference directly.</summary>
    private static readonly HashSet<string> LiteralTokens =
    [
        "STRINGLIT", "NATLIT", "HEXLIT", "BOOLLIT", "INTEGERLIT", "DECIMALLIT", "FLOATLIT", "COMMA_FLOATLIT",
        "SIGNED_DECIMALLIT", "SIGNED_INTEGERLIT",
    ];

    /// <summary>The rules that BUILD a literal (or an integer-n position) rather than place one in a format: their own
    /// bodies are literal tokens by definition.</summary>
    private static readonly HashSet<string> Carriers =
    [
        .. LiteralRules, "integerLiteral", "integerOperand", "numericLiteralCore", "concatenationExpression",
        "concatOperand", "allLiteral", "figurativeConstant",
    ];

    private const string Removed2002 =
        "an X3.23-1985 element ISO 2002 deleted; constant entries first exist at 2002 (constant-entry-2002), so no edition "
        + "holds both the format and a constant-name to stand in it";

    /// <summary>The literal references with no word alternative beside them, each with the reason a constant-name is
    /// not written there.</summary>
    internal static readonly IReadOnlyDictionary<string, string> NotAConstantPosition = new Dictionary<string, string>
    {
        ["levelNumber"] = "a level-number is a user-defined word written as digits (ISO §8.3.2.2.13), never a literal",
        ["stopStatement"] = "STOP literal is the X3.23-1985 Format 2 (stop-literal-removed-2002): " + Removed2002,
        ["entryStatement"] = "ISO/IEC 1989 defines no ENTRY statement; StatementBinder refuses it whole, so no operand binds",
    };

    /// <summary>The word-admitting literal slots no enabled golden can witness, each with its reason.</summary>
    internal static readonly IReadOnlyDictionary<string, string> Unwitnessed = new Dictionary<string, string>
    {
        ["compileTimeOperand"] = "a compiler directive's operand (>>DEFINE, >>IF), which ISO §13.10.3 SR2 excepts: \"Except in "
            + "a compiler directive\"",
        ["genericClause"] = "the error production of a closed clause list (CobolExpressions.g4#unrecognizedClause), refused by "
            + "name (COBOLNET1970); it is no format, so it specifies no literal",
        ["procedureName"] = "a paragraph-name or section-name written in digits, a user-defined word (ISO §8.3.2.2), never a "
            + "literal",
        ["assignTarget"] = PB2500,
        ["localeClause"] = PB2500,
        ["valueOfClause"] = "the VALUE OF clause of the file description entry, " + Removed2002,
        ["paddingCharacterClause"] = "the SELECT PADDING CHARACTER clause, obsolete at 2002 and absent from ISO 2014 and 2023 "
            + "(COBOLNET0903 / COBOLNET0902); its operand is never bound, so a golden that writes a constant-name there would "
            + "witness the parse alone",
        ["mcsSendStatement"] = "the message server's SEND statement, a processor-dependent facility this implementation does not "
            + "provide: accepted inert with COBOLNET1578 (docs/CONFORMANCE.md §4 item 1), so its operand never binds",
        ["screenFromClause"] = Screen,
        ["validateDefaultClause"] = Validate,
        ["validateStatusClause"] = Validate,
    };

    private const string PB2500 =
        "a word here is either a constant-name or an implementor system-name (device-name-1, external-locale-name-1), and "
        + "which reading wins is the open latitude kb/Work PB2500 holds for an owner decision";

    private const string Screen =
        "the SCREEN SECTION is a declined optional module (Annex A.4.2), refused whole by COBOLNET1560, so its clauses never bind";

    private const string Validate =
        "the VALIDATE facility is a declined optional element (Annex A.4.14), its data-division clauses refused by name "
        + "(COBOLNET1708)";

    private static string GrammarDir() => TestRepo.Src("Cobol.Net.Frontend", "Grammar");

    [Fact]
    public void EveryLiteralWithNoWordBesideIt_IsArgued()
    {
        var slots = Grammar.LiteralPositions();
        Assert.True(slots.Count(p => p.WordBeside) >= 30, "fewer than 30 word-admitting literal slots — the scrape is blind");
        var bare = slots.Where(p => !p.WordBeside).Select(p => p.Rule).ToHashSet(StringComparer.Ordinal);
        var unargued = bare.Where(r => !NotAConstantPosition.ContainsKey(r)).Order(StringComparer.Ordinal).ToArray();
        Assert.True(unargued.Length == 0,
            "grammar rule(s) write a literal position with no word alternative, so a constant-name cannot be written there "
            + "(ISO §13.10.3 SR2): spell the slot `literal | cobolWord` (or its dataReference twin) and bind the word through "
            + "ExpressionBinder.LiteralAliasOf, or argue it in NotAConstantPosition: " + string.Join(", ", unargued));
        var stale = NotAConstantPosition.Keys.Where(r => !bare.Contains(r)).Order(StringComparer.Ordinal).ToArray();
        Assert.True(stale.Length == 0, "NotAConstantPosition names rule(s) with no bare literal position: " + string.Join(", ", stale));
    }

    [Fact]
    public void EveryWordAdmittingLiteralSlot_IsWitnessedByAGolden_OrArgued()
    {
        var slotRules = Grammar.LiteralPositions().Where(p => p.WordBeside).Select(p => p.Rule)
            .ToHashSet(StringComparer.Ordinal);
        var witnesses = CorpusWitnesses(slotRules, out int parsed);
        Assert.True(parsed >= 100, $"only {parsed} constant-bearing goldens parsed — the corpus scan is blind");

        var missing = slotRules.Where(r => !witnesses.ContainsKey(r) && !Unwitnessed.ContainsKey(r))
            .Order(StringComparer.Ordinal).ToArray();
        Assert.True(missing.Length == 0,
            "literal slot(s) that admit a word but no enabled golden writes a constant-name there (ISO §13.10.3 SR2): add a "
            + "golden that does, or argue the slot in Unwitnessed: " + string.Join(", ", missing));

        var nowWitnessed = Unwitnessed.Keys.Where(witnesses.ContainsKey)
            .Select(r => $"{r} ({witnesses[r]})").Order(StringComparer.Ordinal).ToArray();
        Assert.True(nowWitnessed.Length == 0, "Unwitnessed argues slot(s) a golden now witnesses: " + string.Join(", ", nowWitnessed));
        var stale = Unwitnessed.Keys.Where(r => !slotRules.Contains(r)).Order(StringComparer.Ordinal).ToArray();
        Assert.True(stale.Length == 0, "Unwitnessed names rule(s) that are no longer word-admitting literal slots: " + string.Join(", ", stale));
    }

    [Fact]
    public void EveryArgument_CarriesItsReason()
    {
        Assert.All(NotAConstantPosition.Concat(Unwitnessed), kv => Assert.False(string.IsNullOrWhiteSpace(kv.Value), kv.Key));
        // Every argued rule names a rule the generated parser has.
        var contexts = typeof(CobolParserCore).GetNestedTypes().Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        Assert.All(NotAConstantPosition.Keys.Concat(Unwitnessed.Keys), r => Assert.Contains(Grammar.ContextName(r), contexts));
    }

    /// <summary>For each slot rule, the first enabled 2002+ positive golden whose parse writes a declared constant-name in
    /// one of that rule's word arms.</summary>
    private static ConcurrentDictionary<string, string> CorpusWitnesses(HashSet<string> slotRules, out int parsed)
    {
        var slotTypes = typeof(CobolParserCore).GetNestedTypes()
            .Where(t => slotRules.Contains(Grammar.RuleName(t.Name)))
            .ToDictionary(t => t, t => Grammar.RuleName(t.Name));
        var literalTypes = typeof(CobolParserCore).GetNestedTypes()
            .Where(t => Carriers.Contains(Grammar.RuleName(t.Name))).ToHashSet();
        var witnesses = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        var programs = new List<(string Path, int Year)>();
        foreach (int year in new[] { 2002, 2014, 2023 })
        {
            string dir = TestRepo.Tests("conformance", year.ToString(System.Globalization.CultureInfo.InvariantCulture));
            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "manifest.json")));
            foreach (var name in manifest.RootElement.GetProperty("enabled").EnumerateArray())
            {
                string path = Path.Combine(dir, name.GetString() + ".cob");
                if (File.Exists(path) && File.ReadAllText(path).Contains("CONSTANT", StringComparison.OrdinalIgnoreCase))
                    programs.Add((path, year));
            }
        }
        int count = 0;
        Parallel.ForEach(programs, program =>
        {
            var frontend = new CobolNet.Frontend.Frontend { DialectLevel = program.Year, InitialFormat = InitialReferenceFormat.Auto };
            frontend.AddCopySearchPath(Path.GetDirectoryName(program.Path)!);
            if (frontend.Parse(program.Path, new DiagnosticBag()) is not { } tree) return;
            Interlocked.Increment(ref count);
            var constants = Descendants(tree).OfType<CobolParserCore.ConstantEntryBodyContext>()
                .Select(EntryName).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (constants.Count == 0) return;
            string label = $"{program.Year}/{Path.GetFileNameWithoutExtension(program.Path)}";
            foreach (var node in Descendants(tree))
            {
                if (!slotTypes.TryGetValue(node.GetType(), out string? rule) || witnesses.ContainsKey(rule)) continue;
                bool wordIsConstant = node.children?.OfType<ParserRuleContext>()
                    .Any(c => !literalTypes.Contains(c.GetType()) && constants.Contains(c.GetText())) == true;
                if (wordIsConstant) witnesses.TryAdd(rule, label);
            }
        });
        parsed = count;
        return witnesses;
    }

    /// <summary>The constant-name of a constant entry: the data-name of the nearest entry above its body (a data
    /// description entry's body sits one rule deeper than a report section constant entry's).</summary>
    private static string? EntryName(CobolParserCore.ConstantEntryBodyContext body)
    {
        for (var node = body.Parent as ParserRuleContext; node is not null; node = node.Parent as ParserRuleContext)
            if (node.GetRuleContext<CobolParserCore.DataNameContext>(0) is { } name) return name.GetText();
        return null;
    }

    private static IEnumerable<ParserRuleContext> Descendants(ParserRuleContext root)
    {
        var stack = new Stack<ParserRuleContext>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            if (node.children is null) continue;
            foreach (IParseTree child in node.children)
                if (child is ParserRuleContext c) stack.Push(c);
        }
    }

    /// <summary>A literal reference in a parser rule: the rule that writes it, and whether the block it is an
    /// alternative of also offers an alternative that is one lone user-defined word.</summary>
    internal readonly record struct LiteralPosition(string Rule, string Reference, bool WordBeside);

    /// <summary>The parser grammar's rules as nested alternative blocks, read from the <c>.g4</c> text with its comments
    /// removed. An element is a rule or token reference, or a parenthesized block; a <c>?</c> or <c>*</c> suffix makes it
    /// optional, and actions, predicates, labels and alternative labels carry no element.</summary>
    internal static class Grammar
    {
        private sealed class Element(string? reference, List<List<Element>>? block)
        {
            public string? Reference { get; } = reference;
            public List<List<Element>>? Block { get; } = block;
            public bool Optional { get; set; }
        }

        private static readonly Regex Token = new(
            @"'(?:[^'\\]|\\.)*'|\{[^{}]*\}\??|[A-Za-z_]\w*\s*\+?=|[A-Za-z_]\w*|[()|?*+~]|#\s*\w+|->[^;|)]*|\S", RegexOptions.Compiled);

        public static string ContextName(string rule) => char.ToUpperInvariant(rule[0]) + rule[1..] + "Context";

        public static string RuleName(string contextName) =>
            contextName.EndsWith("Context", StringComparison.Ordinal) && contextName.Length > 7
                ? char.ToLowerInvariant(contextName[0]) + contextName[1..^7] : contextName;

        public static List<LiteralPosition> LiteralPositions()
        {
            var rules = Rules();
            var words = new HashSet<string>(["cobolWord"], StringComparer.Ordinal);
            for (bool grew = true; grew;)
            {
                grew = false;
                foreach (var (name, block) in rules)
                    if (!words.Contains(name) && OffersAWord(block, words)) grew = words.Add(name);
            }
            var positions = new List<LiteralPosition>();
            foreach (var (name, block) in rules.OrderBy(r => r.Key, StringComparer.Ordinal))
                if (!Carriers.Contains(name)) Walk(name, block, words, positions);
            return positions;
        }

        private static void Walk(string rule, List<List<Element>> block, HashSet<string> words, List<LiteralPosition> positions)
        {
            foreach (var alt in block)
                foreach (var e in alt)
                {
                    if (e.Block is { } inner) { Walk(rule, inner, words, positions); continue; }
                    if (!LiteralRules.Contains(e.Reference!) && !LiteralTokens.Contains(e.Reference!)) continue;
                    // The literal is an alternative of its own (alone, or with optional elements only) when it is the
                    // alternative's one required element, or its only element.
                    bool lone = ReferenceEquals(Lone(alt), e) || alt.Count == 1;
                    positions.Add(new LiteralPosition(rule, e.Reference!, lone && OffersAWord(block, words)));
                }
        }

        /// <summary>The one required element of an alternative, or null when it has none or several.</summary>
        private static Element? Lone(List<Element> alt)
        {
            var required = alt.Where(e => !e.Optional).ToList();
            return required.Count == 1 ? required[0] : null;
        }

        private static bool OffersAWord(List<List<Element>> block, HashSet<string> words) =>
            block.Any(alt => Lone(alt) is { } e
                && (e.Reference is { } r ? words.Contains(r) || r == "IDENTIFIER" : OffersAWord(e.Block!, words)));

        private static Dictionary<string, List<List<Element>>> Rules()
        {
            var rules = new Dictionary<string, List<List<Element>>>(StringComparer.Ordinal);
            foreach (string file in Directory.GetFiles(GrammarDir(), "*.g4", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file) == "CobolLexer.g4") continue;
                string src = Regex.Replace(File.ReadAllText(file), @"//[^\r\n]*", string.Empty);
                src = Regex.Replace(src, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
                // A parser rule is `name : body ;`, on one line or many; a quoted literal or an action is one token, so
                // the `;` that ends the rule is the first one at token level.
                var tokens = Token.Matches(src).Select(t => t.Value).ToList();
                for (int at = 0; at + 1 < tokens.Count; at++)
                {
                    if (tokens[at + 1] != ":" || !Regex.IsMatch(tokens[at], @"^[a-z]\w*$")) continue;
                    int end = tokens.IndexOf(";", at + 2);
                    Assert.True(end > at, $"{Path.GetFileName(file)}: rule '{tokens[at]}' has no closing ';'");
                    int i = 0;
                    rules[tokens[at]] = Block(tokens.GetRange(at + 2, end - at - 2), ref i);
                    at = end;
                }
            }
            Assert.True(rules.Count >= 500, $"only {rules.Count} parser rules read — the grammar scrape is blind");
            return rules;
        }

        private static List<List<Element>> Block(List<string> tokens, ref int i)
        {
            var alts = new List<List<Element>> { new() };
            while (i < tokens.Count)
            {
                string t = tokens[i++];
                if (t == "(") { alts[^1].Add(new Element(null, Block(tokens, ref i))); continue; }
                if (t == ")") return alts;
                if (t == "|") { alts.Add([]); continue; }
                if (t is "?" or "*") { if (alts[^1].Count > 0) alts[^1][^1].Optional = true; continue; }
                if (t == "+" || t[0] is '{' or '#' || t.StartsWith("->", StringComparison.Ordinal)
                    || Regex.IsMatch(t, @"^\w+\s*\+?=$")) continue;
                if (char.IsLetter(t[0]) || t[0] is '_' or '\'') alts[^1].Add(new Element(t, null));
            }
            return alts;
        }
    }
}
