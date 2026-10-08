// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using CobolNet.Frontend.Generated;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The PHASE-04 Group-A drift check (parallel to <see cref="ReservedWordsDriftTests"/>): the context-sensitive
/// word set is single-sourced from <c>tests/version-matrix/cobol-words.json</c> and GENERATED into two grammar
/// artifacts by <c>scripts/gen-cobol-words.ps1</c> —
/// <list type="bullet">
///   <item>the parser <c>cobolWord</c> rule (<c>Grammar/Core/CobolWords.g4</c>, the <c>nameSlot=true</c> rows), and</item>
///   <item>the lexer <c>_dataNameTokens</c> subscript-trigger set (<c>Parsing/CobolLexerWordSet.g.cs</c>, the
///   <c>subscriptTrigger=true</c> rows).</item>
/// </list>
/// These tests prove the two generated artifacts cannot silently desync from the JSON (a hand edit to either, or
/// a regen that touched only one, fails here), and cross-check the JSON against <c>reserved-words.json</c>. All
/// checks are set-based (order-independent). Content-filter rule: never print a full word list — mismatches
/// report counts and at most a few offending tokens.
/// </summary>
public sealed class CobolWordsDriftTests
{
    private sealed record WordRow(string Token, bool NameSlot, bool SubscriptTrigger);

    // token -> COBOL word spelling: ANTLR '_' becomes '-'; a trailing '_' is a generator-clash guard (FULL_ = "FULL").
    internal static string ToWord(string token) => token.Replace('_', '-').TrimEnd('-');

    private static List<WordRow> LoadJsonWords()
    {
        string path = TestRepo.VersionMatrix("cobol-words.json");
        Assert.True(File.Exists(path), $"canonical json missing: {path} — run scripts/gen-cobol-words.ps1");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("words").EnumerateArray()
            .Select(e => new WordRow(
                e.GetProperty("token").GetString()!,
                e.GetProperty("nameSlot").GetBoolean(),
                e.GetProperty("subscriptTrigger").GetBoolean()))
            .ToList();
    }

    /// <summary>⛔ THE DERIVED RESERVATION GATE (kb/Work PB693). The set of <c>cobolWord</c> alternatives that
    /// MUST carry <c>{userWordHere("W")}?</c>, computed here the way <c>gen-cobol-words.ps1</c> step 4b computes
    /// it: a name-slot word that ISO §8.9 reserves at ANY edition. §8.3.2.1 rule 1 — "Reserved words shall not be
    /// used as user-defined words or system-names" — and <c>cobolWord</c> IS the user-defined-word slot, so an
    /// ungated admission lets an operand list absorb the word at the editions that reserve it.
    /// <para>Recomputed from <c>reserved-words.json</c> rather than read off a flag ON PURPOSE: the gate used to
    /// be a hand-set <c>reservationGated</c> row property and fifty-one §8.9-straddling words never got one, so
    /// this test would have been asserting the mistake against itself.</para>
    /// <para>⛔ WITH NO EXCLUSION (kb/Work PB845). LENGTH, NATIONAL and BIT were exempt as "function names" and so
    /// were names in every slot at every edition; ISO §8.3.2.1 5) names LENGTH among the intrinsic function names that
    /// may not be user-defined words, and NATIONAL and BIT are no §8.11 names. The keyword-omitted <c>LENGTH (A)</c>
    /// parses through <c>reservedIntrinsicArgFn</c>, never through <c>cobolWord</c>.</para></summary>
    internal static HashSet<string> DerivedGateSet()
    {
        var reserved = LoadReservedIntervals();
        return LoadJsonWords()
            .Where(w => w.NameSlot && w.Token != "IDENTIFIER")
            .Where(w => reserved.TryGetValue(ToWord(w.Token), out var f) && f.Contains(true))
            .Select(w => w.Token)
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>The JSON <c>nameSlot=true</c> tokens are exactly the generated <c>cobolWord</c> alternatives plus the
    /// <c>reservedGatedWord</c> ones, and no token is in both (kb/Work PB655: a gated word's name reading is the
    /// token-level retype, never a cobolWord alternative).</summary>
    [Fact]
    public void CobolWordsG4_CobolWord_Matches_Json_NameSlot()
    {
        var json = LoadJsonWords().Where(w => w.NameSlot).Select(w => w.Token).ToHashSet(StringComparer.Ordinal);
        var cobolWord = ParseCobolWordAlternatives();
        var gated = ParseRuleAlternatives("reservedGatedWord").Keys.ToHashSet(StringComparer.Ordinal);
        var g4 = cobolWord.Union(gated).ToHashSet(StringComparer.Ordinal);

        var onlyJson = json.Where(w => !g4.Contains(w)).Take(5).ToList();
        var onlyG4 = g4.Where(w => !json.Contains(w)).Take(5).ToList();
        var both = cobolWord.Intersect(gated).Take(5).ToList();
        Assert.True(onlyJson.Count == 0 && onlyG4.Count == 0 && both.Count == 0,
            $"cobolWord drift: json={json.Count} g4={g4.Count} json-only=[{string.Join(",", onlyJson)}] "
            + $"g4-only=[{string.Join(",", onlyG4)}] in-both-rules=[{string.Join(",", both)}]");
    }

    /// <summary>⛔ THE §8.3.2.1 INVARIANT (kb/Work PB693) — every name-slot word that ISO §8.9 reserves at ANY
    /// edition carries the reservation gate. "Reserved words shall not be used as user-defined words or
    /// system-names" (§8.3.2.1 rule 1) and <c>cobolWord</c> IS the user-defined-word slot, so an UNGATED
    /// admission of a reserved word lets an operand list absorb it: <c>MOVE "ZZ" TO FS</c> followed by a
    /// period-less <c>UNLOCK F1</c> parsed as a three-receiver MOVE and legal COBOL-2002 source was rejected.
    /// Fifty-one further words had the same §8.9 straddle and the same ungated admission.
    /// <para>The gate was a hand-set <c>reservationGated</c> JSON flag; it is now DERIVED by
    /// <c>gen-cobol-words.ps1</c> step 4b from <c>reserved-words.json</c> (CLAUDE.md rule 5 — never a
    /// hand-maintained list where a structure belongs). This test recomputes the derivation independently.</para>
    /// <para>⛔ AND THE GATE IS TOKEN-LEVEL, NEVER A PREDICATE (kb/Work PB655). A <c>{userWordHere("W")}?</c> on a
    /// <c>cobolWord</c> alternative sits past the left edge of every enclosing decision, where ANTLR does not
    /// evaluate it — <c>SET P TO NULL</c> at 2002 then chose the wrong format. So: the derived set is EXACTLY
    /// <c>reservedGatedWord</c>, with no predicate and with the <c>gatedDeclaration</c> action the token-level gate
    /// reads; NONE of it is a <c>cobolWord</c> alternative; and no reservation predicate survives anywhere in the
    /// generated grammar.</para></summary>
    [Fact]
    public void CobolWordsG4_ReservationGate_Is_Derived_From_Section89()
    {
        var expected = DerivedGateSet();
        Assert.NotEmpty(expected);   // dataName references reservedGatedWord; an empty rule is invalid ANTLR

        var declaration = ParseRuleAlternatives("reservedGatedWord");
        var missing = expected.Where(w => !declaration.ContainsKey(w)).OrderBy(w => w, StringComparer.Ordinal).ToList();
        var stray = declaration.Keys.Where(w => !expected.Contains(w)).OrderBy(w => w, StringComparer.Ordinal).ToList();
        Assert.True(missing.Count == 0 && stray.Count == 0,
            $"reservedGatedWord drift: §8.9 derives {expected.Count} gated words, the rule has {declaration.Count}. "
            + $"Missing ({missing.Count}): [{string.Join(",", missing.Take(8))}{(missing.Count > 8 ? ",…" : "")}]; "
            + $"never reserved ({stray.Count}): [{string.Join(",", stray.Take(8))}{(stray.Count > 8 ? ",…" : "")}]. "
            + "Re-run scripts/gen-cobol-words.ps1.");
        Assert.True(declaration.Values.All(p => p.Length == 0), "a reservedGatedWord alternative carries a predicate");

        var inCobolWord = ParseCobolWordAlternatives().Where(expected.Contains).Take(5).ToList();
        Assert.True(inCobolWord.Count == 0,
            $"gated word(s) are cobolWord alternatives again: [{string.Join(",", inCobolWord)}] — a gated name reading "
            + "is the token-level retype (ReservationGateRewriter), never a predicate prediction cannot see");

        string g4 = File.ReadAllText(GeneratedWordsG4());
        Assert.DoesNotContain("userWordHere", g4);
        Assert.Contains("{ gatedDeclaration(TokenStream.LT(-1)); }", g4);

        // Confidence alignment (the generator's RW-3 throw, asserted from the other side): the token-level gate
        // keys on userWordHere() = !IsReservedAt (outside the migration mode), which is confidence-blind, while the §8.9 funnel only REPORTS
        // high-confidence rows. A gated lower-confidence word would be rejected with a bare parse error and no
        // COBOLNET0901 to explain it.
        var confidence = LoadConfidence();
        var lowConfidence = expected.Where(t => !(confidence.TryGetValue(ToWord(t), out var c) && c == "high"))
                                    .OrderBy(w => w, StringComparer.Ordinal).Take(5).ToList();
        Assert.True(lowConfidence.Count == 0,
            $"gated word(s) not high-confidence: [{string.Join(",", lowConfidence)}] — the gate would reject the "
            + "declaration with no COBOLNET0901 (the funnel reports high-confidence rows only)");
    }

    /// <summary>The lexer's runtime <c>_dataNameTokens</c> set equals the JSON <c>subscriptTrigger=true</c> tokens.
    /// This reads the ONE real compiled set (the hand-written HashSet was deleted from CobolLexer.g4 @members).</summary>
    [Fact]
    public void LexerRuntimeSet_Matches_Json_SubscriptTrigger()
    {
        var json = LoadJsonWords().Where(w => w.SubscriptTrigger).Select(w => w.Token).ToHashSet(StringComparer.Ordinal);

        var field = typeof(CobolLexer).GetField("_dataNameTokens", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        var ints = (HashSet<int>)field!.GetValue(null)!;
        var runtime = ints.Select(t => CobolLexer.DefaultVocabulary.GetSymbolicName(t) ?? $"<{t}>")
                          .ToHashSet(StringComparer.Ordinal);

        var onlyJson = json.Where(w => !runtime.Contains(w)).Take(5).ToList();
        var onlyRuntime = runtime.Where(w => !json.Contains(w)).Take(5).ToList();
        Assert.True(onlyJson.Count == 0 && onlyRuntime.Count == 0,
            $"_dataNameTokens drift: json={json.Count} runtime={runtime.Count} json-only=[{string.Join(",", onlyJson)}] runtime-only=[{string.Join(",", onlyRuntime)}]");
    }

    /// <summary>Reconciliation + reserved-words cross-check. The two documented asymmetries are pinned, and the
    /// SOUND reserved-words linkage (RW-1) holds. See the DESIGN-DEVIATION note in <c>gen-cobol-words.ps1</c> for
    /// why the plan's naive "every trigger word is user-legal at >=1 edition" predicate was rejected as unsound
    /// (COLUMN/LENGTH are §8.9-reserved at all editions yet name-slot-admitted; the six functionName collisions
    /// are reserved keywords).</summary>
    [Fact]
    public void Reconciliation_And_ReservedWords_CrossCheck()
    {
        var words = LoadJsonWords();
        var nameSlot = words.Where(w => w.NameSlot).Select(w => w.Token).ToHashSet(StringComparer.Ordinal);
        var subTrig = words.Where(w => w.SubscriptTrigger).Select(w => w.Token).ToHashSet(StringComparer.Ordinal);

        // Both asymmetry sides are PINNED to their documented membership (FU-1) — symmetric, so a one-sided flip of a
        // currently-SHARED word is drift. Without the subscriptTrigger-only pin, flipping a shared+2023-reserved word
        // (COLUMN/LENGTH/SCREEN) to nameSlot=false would silently drop its cobolWord admission yet pass RW-1 (it stays
        // reserved) — the false-green gap the adversarial review flagged.
        // AS joined BIT at P10 Step 15 BY DESIGN: the §13.10 constant entry's `AS (arith-expr)` must lex its
        // parenthesized expression in NORMAL mode, so AS cannot be a subscript trigger (the FU-1 ledger).
        var nameSlotOnly = nameSlot.Where(w => !subTrig.Contains(w)).OrderBy(w => w, StringComparer.Ordinal).ToList();
        Assert.True(nameSlotOnly.SequenceEqual(new[] { "AS", "BIT" }),
            $"nameSlot-only expected [AS,BIT] but was [{string.Join(",", nameSlotOnly)}] — update the FU-1 ledger if intended");

        var subTrigOnly = subTrig.Where(w => !nameSlot.Contains(w)).OrderBy(w => w, StringComparer.Ordinal).ToList();
        Assert.True(subTrigOnly.SequenceEqual(new[] { "DISPLAY", "MERGE", "RANDOM", "SIGN", "SORT", "SUM" }),
            $"subscriptTrigger-only expected the six functionName collisions but was [{string.Join(",", subTrigOnly)}] — a shared word flipped to nameSlot=false lands here; update the FU-1 ledger if intended");

        // RW-1 (sound reserved-words linkage): each subscriptTrigger-only word is a genuine 2023-reserved keyword.
        var reserved2023 = LoadReserved2023();
        var rwViolations = subTrigOnly.Where(t => !(reserved2023.TryGetValue(ToWord(t), out var r) && r)).Take(5).ToList();
        Assert.True(rwViolations.Count == 0,
            $"RW-1: subscriptTrigger-only word(s) not 2023-reserved keywords: [{string.Join(",", rwViolations)}] — a stray user-word belongs in cobolWord too");
    }

    /// <summary>⛔ NO LEXER TOKEN MAY COST THE USER A WORD ISO LEAVES FREE (kb/Work PB655). A word the lexer
    /// tokenizes never reaches <c>IDENTIFIER</c>, so unless <c>cobolWord</c> admits its token it is refused as a
    /// user-defined word at EVERY edition — a raw COBOL0001, never a §8.9 diagnostic. §8.3.2.1 3) makes a §8.10
    /// context-sensitive word a legal user-defined word outside its context, and a word §8.9 does not reserve at
    /// edition E is one at E; 99 tokens once broke that (<c>01 GOBACK PIC X.</c> at COBOL-85, <c>01 PAGE PIC X.</c>
    /// at COBOL-2023, <c>01 SIGNED PIC X.</c> anywhere).
    /// <para>The join reads the COMPILED lexer vocabulary — every default-mode token whose rule is a single word
    /// literal — so the next token the grammar adds is checked with no edit here. It must be admitted by a
    /// <c>nameSlot</c> row (the derived gate of step 4b then keeps it out wherever §8.9 reserves it), be §8.9-reserved
    /// at all four editions (a pure keyword), or be one of the <c>extensionReserved</c> words ISO §4.2.10 lets an
    /// implementation reserve for a nonstandard extension — and that list must name only words ISO reserves
    /// nowhere, and must equal the §4.2.10 documentation in <c>docs/CONFORMANCE.md</c> D-RW1.</para></summary>
    [Fact]
    public void LexerWordTokens_Cost_The_User_No_Word_Iso_Leaves_Free()
    {
        var vocab = CobolLexer.DefaultVocabulary;
        var wordTokens = new Dictionary<string, string>(StringComparer.Ordinal);   // token -> word
        for (int t = 1; t <= ((Antlr4.Runtime.Vocabulary)vocab).getMaxTokenType(); t++)
        {
            string? sym = vocab.GetSymbolicName(t), lit = vocab.GetLiteralName(t);
            if (sym is null || lit is null || sym.StartsWith("PIC_", StringComparison.Ordinal)) continue;
            string word = lit.Trim('\'').ToUpperInvariant();
            if (Regex.IsMatch(word, "^[A-Z][A-Z0-9-]*$")) wordTokens[sym] = word;
        }
        Assert.True(wordTokens.Count > 400, $"the lexer vocabulary yielded {wordTokens.Count} word tokens — the read broke");

        var admitted = LoadJsonWords().Where(w => w.NameSlot).Select(w => w.Token).ToHashSet(StringComparer.Ordinal);
        var reserved = LoadReservedIntervals();
        var contextSensitive = LoadContextSensitive();
        var extension = LoadExtensionReserved();

        // No residual: the §8.9 gate is TOKEN-LEVEL (ReservationGateRewriter), so a word that is also an operand-
        // position keyword (NULL, SELF, ADDRESS, …) is admitted like every other — prediction never sees a gate.
        var leaks = wordTokens.Where(kv => !admitted.Contains(kv.Key))
            .Where(kv => !(reserved.TryGetValue(kv.Value, out var f) && f.All(x => x)))
            .Where(kv => !extension.Contains(kv.Value))
            .Select(kv => kv.Value).OrderBy(w => w, StringComparer.Ordinal).ToList();
        Assert.True(leaks.Count == 0,
            $"{leaks.Count} lexer token(s) refuse a word ISO leaves free at >=1 edition: [{string.Join(",", leaks.Take(8))}"
            + $"{(leaks.Count > 8 ? ",…" : "")}] — add a cobol-words.json nameSlot row (the §8.9 gate is derived) and "
            + "re-run scripts/gen-cobol-words.ps1, or, for a vendor-only word, an extensionReserved entry + CONFORMANCE.md D-RW1");

        // The exemption is only for words ISO reserves NOWHERE and that the lexer really tokenizes, unadmitted.
        var tokenWords = wordTokens.Values.ToHashSet(StringComparer.Ordinal);
        var badExt = extension.Where(w => reserved.ContainsKey(w) || contextSensitive.Contains(w) || !tokenWords.Contains(w)
                                          || wordTokens.Any(kv => kv.Value == w && admitted.Contains(kv.Key)))
                              .OrderBy(w => w, StringComparer.Ordinal).ToList();
        Assert.True(badExt.Count == 0,
            $"extensionReserved word(s) that are ISO words, not lexer tokens, or admitted anyway: [{string.Join(",", badExt)}]");

        // §4.2.10: "Documentation … shall specify any reserved words added for nonstandard extensions."
        string conformance = File.ReadAllText(TestRepo.Docs("CONFORMANCE.md"));
        var d = Regex.Match(conformance, @"\*\*D-RW1 .*?\r?\n\r?\n", RegexOptions.Singleline);   // CONFORMANCE.md is CRLF
        Assert.True(d.Success, "docs/CONFORMANCE.md lost its D-RW1 determination (§4.2.10 documentation)");
        // The bold list wraps across (CRLF) lines, so the separator is a comma plus ANY whitespace.
        var documented = Regex.Matches(d.Value, @"\*\*([A-Z][A-Z0-9-]*(?:,\s+[A-Z][A-Z0-9-]*)+)\*\*")
            .SelectMany(m => Regex.Split(m.Groups[1].Value, @",\s+")).ToHashSet(StringComparer.Ordinal);
        Assert.True(documented.SetEquals(extension),
            $"CONFORMANCE.md D-RW1 lists {documented.Count} word(s), cobol-words.json extensionReserved {extension.Count} — §4.2.10 requires the two to agree");
    }

    private static HashSet<string> LoadExtensionReserved()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestRepo.VersionMatrix("cobol-words.json")));
        return doc.RootElement.GetProperty("extensionReserved").EnumerateArray()
            .Select(e => e.GetProperty("word").GetString()!).ToHashSet(StringComparer.Ordinal);
    }

    private static HashSet<string> LoadContextSensitive()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(TestRepo.VersionMatrix("context-sensitive-words.json")));
        return doc.RootElement.GetProperty("words").EnumerateArray()
            .Select(e => e.GetProperty("word").GetString()!).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Structural invariants the generator guarantees (a regression here means the JSON or script broke).</summary>
    [Fact]
    public void Structural_Sanity()
    {
        var words = LoadJsonWords();
        Assert.Equal(words.Count, words.Select(w => w.Token).Distinct(StringComparer.Ordinal).Count());   // unique tokens
        Assert.All(words, w => Assert.Matches("^[A-Z][A-Z0-9_]*$", w.Token));
        Assert.All(words, w => Assert.True(w.NameSlot || w.SubscriptTrigger, $"{w.Token} in neither set"));
        var ident = Assert.Single(words, w => w.Token == "IDENTIFIER");
        Assert.True(ident.NameSlot && ident.SubscriptTrigger, "IDENTIFIER must be nameSlot=true AND subscriptTrigger=true");
    }

    private static string GeneratedWordsG4()
    {
        string path = TestRepo.Src("Cobol.Net.Frontend", "Grammar", "Core", "CobolWords.g4");
        Assert.True(File.Exists(path), $"generated grammar missing: {path} — run scripts/gen-cobol-words.ps1");
        return path;
    }

    /// <summary>The single-token alternatives of one generated rule. One alternative per line: <c>: X</c>,
    /// <c>| X</c> or (reservedGatedWord's grouped form) <c>: ( X</c>, optionally behind a predicate
    /// <c>{…}?</c>; the predicate texts are returned too so a test can assert what gates what.</summary>
    private static Dictionary<string, string> ParseRuleAlternatives(string ruleName)
    {
        var alts = new Dictionary<string, string>(StringComparer.Ordinal);   // token -> predicate text ("" = none)
        bool inRule = false;
        foreach (var raw in File.ReadAllLines(GeneratedWordsG4()))
        {
            string line = raw.Trim();
            if (line == ruleName) { inRule = true; continue; }
            if (!inRule) continue;
            if (line == ";") break;
            var m = Regex.Match(line, @"^(?::\s*\(|[:|])\s*(?:\{([^}]*)\}\?\s*)?([A-Z][A-Z0-9_]*)\s*$");
            if (m.Success) alts[m.Groups[2].Value] = m.Groups[1].Value;
        }
        Assert.True(inRule, $"rule '{ruleName}' not found — run scripts/gen-cobol-words.ps1");
        return alts;
    }

    private static HashSet<string> ParseCobolWordAlternatives() =>
        ParseRuleAlternatives("cobolWord").Keys.ToHashSet(StringComparer.Ordinal);

    /// <summary>word → its four §8.9 reservation flags, in edition order {85, 2002, 2014, 2023}.</summary>
    internal static Dictionary<string, bool[]> LoadReservedIntervals()
    {
        string path = TestRepo.VersionMatrix("reserved-words.json");
        Assert.True(File.Exists(path), $"reserved-words.json missing: {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("words").EnumerateArray()
            .ToDictionary(e => e.GetProperty("word").GetString()!,
                          e => new[] { e.GetProperty("r85").GetBoolean(), e.GetProperty("r2002").GetBoolean(),
                                       e.GetProperty("r2014").GetBoolean(), e.GetProperty("r2023").GetBoolean() },
                          StringComparer.Ordinal);
    }

    /// <summary>word → its reserved-words.json confidence band ("high" / "medium").</summary>
    internal static Dictionary<string, string> LoadConfidence()
    {
        string path = TestRepo.VersionMatrix("reserved-words.json");
        Assert.True(File.Exists(path), $"reserved-words.json missing: {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("words").EnumerateArray()
            .ToDictionary(e => e.GetProperty("word").GetString()!,
                          e => e.GetProperty("confidence").GetString()!, StringComparer.Ordinal);
    }

    private static Dictionary<string, bool> LoadReserved2023()
    {
        string path = TestRepo.VersionMatrix("reserved-words.json");
        Assert.True(File.Exists(path), $"reserved-words.json missing: {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("words").EnumerateArray()
            .ToDictionary(e => e.GetProperty("word").GetString()!, e => e.GetProperty("r2023").GetBoolean(),
                          StringComparer.Ordinal);
    }
}
