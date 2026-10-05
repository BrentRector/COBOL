// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ <b>FORMAT 1 AND FORMAT 4 GO IN THE SAME POSITIONS, AND THAT IS A RULE OF THE STANDARD RATHER THAN A
/// CONVENTION OF THIS GRAMMAR</b> (kb/Work PB428).
///
/// <para>ISO §8.4.3.1.2 gives <i>identifier</i> eleven general formats. Two of them are not a data-name
/// reference and therefore need their own grammar alternative at every operand site: Format 1, the
/// function-identifier (<c>functionCall</c>), and Format 4, the inline method invocation
/// (<c>inlineMethodInvocation</c>). Their exclusions are word-for-word twins — §8.4.3.2.3 SR1 "A
/// function-identifier shall not be specified as a receiving operand" and §8.4.3.4.3 SR1 "Inline method
/// invocation shall not be specified as a receiving operand" — so the set of positions that admits one
/// admits the other, and the receiving rules admit neither.</para>
///
/// <para><b>Why a test and not a shared rule.</b> The obvious shape is ONE <c>sendingIdentifier</c> rule used
/// everywhere, and it is blocked for exactly the reason <c>ArithmeticSendingOperandDriftTests</c> records:
/// the FROZEN legacy compiler shares this grammar and reads <c>.dataReference()</c> / <c>.literal()</c> /
/// <c>.functionCall()</c> off these contexts BY NAME, so a collapse or an alias breaks its build until
/// PHASE 15 CUT 2 deletes it. The alternatives therefore stay per-site and this test is what makes the
/// pairing mechanical instead of a hand-maintained list (CLAUDE.md rule 5): the next rule to gain
/// <c>functionCall</c> fails here until it gains Format 4 too. Collapse both at CUT 2 and delete this with
/// <c>ArithmeticSendingOperandDriftTests</c>.</para>
///
/// <para>⚠ It reads the GRAMMAR SOURCE, not the generated parser: the property is about what the <c>.g4</c>
/// admits, and a generated-parser check would pass on a rule that merely happens not to be exercised.</para>
/// </summary>
public sealed class InlineMethodInvocationOperandDriftTests
{
    private static readonly string[] GrammarFiles =
    [
        Path.Combine("src", "Cobol.Net.Frontend", "Grammar", "CobolParserCore.g4"),
        Path.Combine("src", "Cobol.Net.Frontend", "Grammar", "Core", "CobolExpressions.g4"),
        Path.Combine("src", "Cobol.Net.Frontend", "Grammar", "Core", "CobolControlFlow.g4"),
        Path.Combine("src", "Cobol.Net.Frontend", "Grammar", "Core", "CobolData.g4"),
        Path.Combine("src", "Cobol.Net.Frontend", "Grammar", "Core", "CobolIO.g4"),
        Path.Combine("src", "Cobol.Net.Frontend", "Grammar", "Core", "CobolOO.g4"),
    ];

    /// <summary>The rule whose own DEFINITION is <c>functionCall</c>, the one that defines Format 4, and Format 4's
    /// own receiver's TERM <c>objectReferenceTerm</c> (kb/Work PB1425) — a rule cannot be asked to offer itself as an
    /// alternative, and the receiver cannot offer the inline form without indirect left recursion; the operand rule
    /// built on it (<c>objectReference</c>) offers both, and the pairing fact holds there.</summary>
    private static readonly string[] Definitions = ["functionCall", "inlineMethodInvocation", "objectReferenceTerm"];

    /// <summary>The rules whose slot ISO §8.4.3.1.4 GR1's ORDER makes Format 4 unspellable in, each with that reason —
    /// not an exclusion rule but a precedence: a Format-4 operand written there belongs to an enclosing identifier.
    /// <c>propertyObject</c> is Format 7's identifier-3 when a word cannot spell it (kb/Work PB1425): GR1 applies d) "OF
    /// for object properties" BEFORE e) "the inline method invocation operator", so <c>P OF X :: "M"</c> is the
    /// invocation of M on the property <c>P OF X</c>, never the property P of <c>X :: "M"</c>.</summary>
    private static readonly string[] Format4UnspellableByOrder = ["propertyObject"];

    /// <summary>rule-name → its body text, over every grammar file, comments stripped.</summary>
    private static Dictionary<string, string> LoadRules()
    {
        var rules = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string rel in GrammarFiles)
        {
            string path = Path.Combine(TestRepo.Root, rel);
            Assert.True(File.Exists(path), $"grammar file '{rel}' not found — if it moved, move this guard");
            string g4 = File.ReadAllText(path);
            g4 = Regex.Replace(g4, @"/\*.*?\*/", " ", RegexOptions.Singleline);
            g4 = Regex.Replace(g4, @"//[^\n]*", " ");
            foreach (Match m in Regex.Matches(g4,
                @"^(?<name>[a-z][A-Za-z0-9_]*)\s*\r?\n?\s*:(?<body>.*?);",
                RegexOptions.Multiline | RegexOptions.Singleline))
                rules.TryAdd(m.Groups["name"].Value, m.Groups["body"].Value);
        }
        return rules;
    }

    private static bool Mentions(string body, string rule) =>
        Regex.IsMatch(body, $@"\b{Regex.Escape(rule)}\b");

    /// <summary>⛔ THE FACT PB428 WAS HIDING BEHIND. Before the fix, TWENTY rules offered <c>functionCall</c>
    /// and NONE offered <c>inlineMethodInvocation</c> — Format 4 had no surface in any operand position, in
    /// any statement, at any edition. Proved to guard: deleting the alternative from any one of those rules
    /// fails this fact by name.</summary>
    [Fact]
    public void EveryRuleAdmittingAFunctionIdentifier_AlsoAdmitsAnInlineMethodInvocation()
    {
        var rules = LoadRules();
        // An exemption that no longer names a rule offering a function-identifier exempts nothing — and hides a rename.
        foreach (string exempt in Format4UnspellableByOrder)
            Assert.True(rules.TryGetValue(exempt, out string? body) && Mentions(body, "functionCall"),
                $"Format4UnspellableByOrder names '{exempt}', which is no grammar rule offering functionCall");
        var missing = rules
            .Where(kv => !Definitions.Contains(kv.Key) && !Format4UnspellableByOrder.Contains(kv.Key))
            .Where(kv => Mentions(kv.Value, "functionCall") && !Mentions(kv.Value, "inlineMethodInvocation"))
            .Select(kv => kv.Key)
            .ToList();
        Assert.True(missing.Count == 0,
            $"these grammar rules admit a function-identifier (ISO §8.4.3.1.2 Format 1) but not an inline "
            + $"method invocation (Format 4): {string.Join(", ", missing)}. Both are identifiers, and the two "
            + "exclusions (§8.4.3.2.3 SR1 / §8.4.3.4.3 SR1) are the same sentence, so a SENDING position "
            + "admits both or neither. Add `| inlineMethodInvocation` and the binder arm that reads it.");
        // The guard is worthless if the pairing is vacuous, so assert the population it measured.
        int paired = rules.Count(kv => !Definitions.Contains(kv.Key) && Mentions(kv.Value, "functionCall"));
        Assert.True(paired >= 18,
            $"only {paired} operand rules offer functionCall — the sweep lost sites, so the pairing above "
            + "proved nothing (feedback_verdict_evidence_invariant).");
    }

    /// <summary>⛔ EVERY OBJECT-REFERENCE POSITION TAKES THE ONE OBJECT-REFERENCE OPERAND RULE, AND THAT RULE TAKES BOTH
    /// COMPUTED IDENTIFIER FORMATS (kb/Work PB1425, PB1197). §14.9.23.3 SR1, §14.9.29.3 SR2 and §14.9.39.3 SR9 ask
    /// the identifier's class, so an inline invocation (Format 4) or a function-identifier (Format 1) whose item is an
    /// object reference is a legal INVOKE receiver, RAISE operand and SET Format 5 sender. They were parse errors
    /// because <c>objectReference</c> offered neither — a position that the functionCall pairing above cannot see,
    /// since the rule offered no functionCall either. Proved to guard: dropping either alternative, or giving one of
    /// the three positions a private operand rule, fails this fact by name.</summary>
    [Fact]
    public void ObjectReferencePositions_AdmitBothComputedIdentifierFormats()
    {
        var rules = LoadRules();
        Assert.True(rules.TryGetValue("objectReference", out string? operand), "the objectReference rule is gone");
        Assert.True(Mentions(operand, "inlineMethodInvocation"),
            "objectReference does not offer an inline method invocation (ISO §8.4.3.1.2 Format 4)");
        Assert.True(rules.TryGetValue("objectReferenceAtom", out string? atom), "the objectReferenceAtom rule is gone");
        Assert.True(rules.TryGetValue("objectReferenceTerm", out string? term), "the objectReferenceTerm rule is gone");
        Assert.True(Mentions(atom, "objectReferenceTerm") && Mentions(term, "functionCall"),
            "objectReferenceAtom does not offer a function-identifier (ISO §8.4.3.1.2 Format 1)");
        // Format 5, the object-view (§8.4.3.5), is an atom: §8.4.3.1.4 GR1 c) applies it before the inline
        // invocation operator (e), so it is what an inline invocation's segments apply to (kb/Work PB1425).
        Assert.True(Mentions(atom, "objectView"),
            "objectReferenceAtom does not offer an object-view (ISO §8.4.3.1.2 Format 5)");
        foreach (string position in new[] { "invokeTarget", "raiseStatement", "setObjectReferenceStatement" })
        {
            Assert.True(rules.TryGetValue(position, out string? body), $"the {position} rule is gone");
            Assert.True(Mentions(body, "objectReference"),
                $"'{position}' no longer takes the shared objectReference operand rule");
        }
    }

    /// <summary>The purely receiving rules must admit NEITHER — for them §8.4.3.4.3 SR1 holds STRUCTURALLY, by
    /// the construct's absence, exactly as §8.4.3.2.3 SR1 holds for a function-identifier. A guard that only
    /// checked the sending side would be satisfied by a careless edit that added it everywhere.
    /// <para>⚠ This does NOT cover the four MIXED-ROLE arithmetic rules (<c>addToPhrase</c>,
    /// <c>subtractFromOperand</c>, <c>multiplyByOperand</c>, <c>divideIntoOperand</c>), which admit both forms
    /// because Format 2 needs them as senders; there SR1 is the binder's Format-1 check, pinned by
    /// <c>ArithmeticOperandRoleDriftTests</c> (kb/Work PB1142 — crediting this fact with them is how an inline
    /// invocation receiver went silently dropped).</para></summary>
    [Fact]
    public void ReceivingOperandRules_DoNotAdmitAnInlineMethodInvocation()
    {
        var rules = LoadRules();
        foreach (string rule in new[]
                 {
                     "receivingArithmeticOperand", "receivingOperand", "moveReceivingPhrase",
                     "dataReferenceList", "invokeReturning", "returningClause",
                 })
        {
            if (!rules.TryGetValue(rule, out string? body)) continue;   // renamed — the sending guard still holds
            Assert.False(Mentions(body, "inlineMethodInvocation"),
                $"'{rule}' is a RECEIVING position and admits an inline method invocation — ISO §8.4.3.4.3 "
                + "SR1: \"Inline method invocation shall not be specified as a receiving operand.\"");
        }
    }

    /// <summary>The construct's own shape, pinned to the §8.4.3.4.2 general format rendered from the
    /// canonical PDF (page 163 / printed folio 133): the receiver is INVOKE's own <c>objectReference</c> (one
    /// activation mechanism — §8.4.3.4.4 GR1 defines the inline form AS that INVOKE), the operator is the
    /// §8.7.4 <c>::</c> token, literal-1 is required, and the parenthesised argument list is optional.</summary>
    [Fact]
    public void TheRuleMatchesThePrintedGeneralFormat()
    {
        var rules = LoadRules();
        Assert.True(rules.TryGetValue("inlineMethodInvocation", out string? body),
            "the inlineMethodInvocation rule is gone — §8.4.3.1.2 Format 4 has no surface");
        Assert.Contains("objectReferenceAtom", body);
        Assert.Contains("inlineInvocationSegment", body);
        Assert.Contains("refModPart", body);        // §8.4.3.1.4 GR1 g)
        Assert.True(rules.TryGetValue("inlineInvocationSegment", out string? seg));
        Assert.Contains("COLONCOLON", seg);
        Assert.Contains("literal", seg);
        Assert.Contains("argumentList", seg);

        // The §8.7.4 invocation operator is a LEXER token of its own — without it the construct is
        // punctuation, which is precisely the state PB428 measured.
        string lexer = File.ReadAllText(Path.Combine(TestRepo.Root,
            "src", "Cobol.Net.Frontend", "Grammar", "Core", "CobolLexer.g4"));
        Assert.Matches(@"COLONCOLON\s*:\s*'::'", lexer);

        // §8.4.3.4.2's argument brace, all five forms (OMITTED is the one underlined word).
        Assert.True(rules.TryGetValue("argument", out string? arg));
        foreach (string form in new[] { "OMITTED", "booleanExpression", "literal", "arithmeticExpression" })
            Assert.Contains(form, arg);
    }
}
