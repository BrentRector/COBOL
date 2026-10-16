// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.Text.RegularExpressions;
using CobolNet.Binding.Procedure;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY IDENTIFIER FORM THAT IS NEVER A RECEIVING OPERAND IS PARSED IN EVERY RECEIVING POSITION AND REFUSED BY ITS OWN
/// RULE FROM ONE TABLE (kb/Work PB1551, PB1782 step 1). ISO §8.4.3 writes the exclusion into each identifier format —
/// §8.4.3.5.3 SR2 (object-view), §8.4.3.6.3 SR1 (EXCEPTION-OBJECT), §8.4.3.7.3 SR1 (NULL), §8.4.3.8.3 SR2 (SELF and
/// SUPER), §8.4.3.11.3 SR5 / §8.4.3.12.3 SR3 / §8.4.3.13.3 SR4 (the address-identifiers) — so a receiving slot that took
/// only a data reference refused them as a parse error re-coded to §8.9 (COBOLNET0901 "is a reserved word … cannot be
/// used as a user-defined word"): a rule the program had not broken, which would become SILENT ACCEPTANCE the moment the
/// identifier tier is widened.
/// <para>The shape these facts hold: <see cref="NonReceivableForm"/> has one row per member in
/// <see cref="ReceivingOperandForms.Rules"/> (a form cannot be added without deciding its rule); every alternative the
/// grammar's <c>nonReceivableIdentifier</c> spells is one <see cref="ReceivingOperandForms.FormOf"/> reads; and no
/// grammar rule spells a receiving phrase over a bare <c>dataReference</c> any more, other than the slots named in
/// <see cref="NotReceivingIdentifiers"/> with the reason each is not one.</para>
/// </summary>
public sealed class ReceivingOperandFormDriftTests
{
    private static readonly string[] GrammarFiles =
    [
        Path.Combine("src", "Cobol.Net.Frontend", "Grammar", "CobolParserCore.g4"),
        Path.Combine("src", "Cobol.Net.Frontend", "Grammar", "Core", "CobolControlFlow.g4"),
        Path.Combine("src", "Cobol.Net.Frontend", "Grammar", "Core", "CobolData.g4"),
        Path.Combine("src", "Cobol.Net.Frontend", "Grammar", "Core", "CobolIO.g4"),
        Path.Combine("src", "Cobol.Net.Frontend", "Grammar", "Core", "CobolOO.g4"),
    ];

    /// <summary>rule-name → its body text, comments stripped.</summary>
    private static Dictionary<string, string> LoadRules()
    {
        var rules = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string rel in GrammarFiles)
        {
            string path = Path.Combine(TestRepo.Root, rel);
            Assert.True(File.Exists(path), $"grammar file '{rel}' not found — if it moved, move this guard");
            string g4 = Regex.Replace(File.ReadAllText(path), @"//[^\n]*", " ");
            foreach (Match m in Regex.Matches(g4, @"^(?<name>[a-z][A-Za-z0-9_]*)\s*\r?\n?\s*:(?<body>.*?);",
                         RegexOptions.Multiline | RegexOptions.Singleline))
                rules.TryAdd(m.Groups["name"].Value, m.Groups["body"].Value);
        }
        return rules;
    }

    [Fact]
    public void EveryNonReceivableForm_HasItsRule_NamingItsClause()
    {
        foreach (NonReceivableForm form in Enum.GetValues<NonReceivableForm>())
        {
            Assert.True(ReceivingOperandForms.Rules.TryGetValue(form, out var rule),
                $"NonReceivableForm.{form} has no row in ReceivingOperandForms.Rules — decide the rule that forbids it as a "
                + "receiving operand before adding the form");
            string message = rule.Message("X");
            Assert.Matches(@"ISO §8\.4\.3\.\d+\.3 SR\d", message);
            Assert.StartsWith("ISO §8.4.3.", rule.Descriptor.IsoSection, StringComparison.Ordinal);
        }
    }

    /// <summary>The grammar's superset rule spells exactly the alternatives <see cref="ReceivingOperandForms.FormOf"/>
    /// reads — an alternative added to the rule without a form would reach FormOf's last arm and be refused under the
    /// wrong rule.</summary>
    [Fact]
    public void EveryNonReceivableAlternative_IsReadByFormOf()
    {
        var rules = LoadRules();
        Assert.True(rules.TryGetValue("nonReceivableIdentifier", out string? body), "the nonReceivableIdentifier rule is gone");
        var alternatives = body!.Split('|').Select(a => a.Trim()).Where(a => a.Length > 0).ToList();
        string[] read = ["dataReference objectViewPhrase+", "selfAndSuper objectViewPhrase*", "predefinedNull objectViewPhrase*",
            "functionAddressIdentifier", "programAddressIdentifier"];
        Assert.Equal(read, alternatives.Select(a => Regex.Replace(a, @"\s+", " ")).ToArray());
        foreach (string rule in new[] { "receivingIdentifier", "setReceivingIdentifier" })
        {
            Assert.True(rules.TryGetValue(rule, out string? r), $"the {rule} rule is gone");
            Assert.Matches(@"\bnonReceivableIdentifier\b", r!);
        }
    }

    /// <summary>The grammar slots that keep a bare <c>dataReference</c> after a receiving-phrase keyword, each with why it
    /// is no receiving identifier of the PROCEDURE DIVISION.</summary>
    private static readonly Dictionary<string, string> NotReceivingIdentifiers = new()
    {
        ["returningClause"] = "the PROCEDURE DIVISION header's RETURNING item — a definition, not a statement operand",
        ["mcsReceiveStatement"] = "the declined '85 communication RECEIVE (Annex A.4) — refused whole by name",
        ["mcsSendStatement"] = "the declined '85 communication SEND (Annex A.4) — refused whole by name",
        ["sortGivingPhrase"] = "SORT … GIVING names FILES (file-name-2), not identifiers",
        ["mergeGivingPhrase"] = "MERGE … GIVING names FILES (file-name-2), not identifiers",
        ["setBooleanStatement"] = "SET Format 4's receiver is condition-name-1 (§14.9.39.2), a user-defined word, not an identifier",
    };

    /// <summary>The words that introduce a receiving identifier in a statement's general format: TO (MOVE, ADD, SET),
    /// INTO, GIVING, RETURNING, REMAINDER, POINTER, VARYING, AFTER, REFERENCE (CALL / INVOKE BY REFERENCE, §14.9.4.3 SR5,
    /// §14.9.23.3 SR20), ACCEPT, INITIALIZE, SET's own receiving list and its CONTENT OF / SIZE OF, and UNSTRING's
    /// TALLYING IN / DELIMITER IN / COUNT IN. Each may be followed directly by the operand rule or by a parenthesized
    /// alternation one of whose alternatives is it.</summary>
    private static readonly Regex ReceivingSlot = new(
        @"\b(?:SET|TO|INTO|GIVING|RETURNING|REMAINDER|POINTER|VARYING|AFTER|REFERENCE|ACCEPT|INITIALIZE"
        + @"|TALLYING\s+IN\?|DELIMITER\s+IN\?|COUNT\s+IN\?|(?:CONTENT|SIZE)\s+OF\?)"
        + @"\s+(?:\((?:[^()|;]*\|)*\s*)?(?<op>[a-z][A-Za-z0-9_]*)\b");

    /// <summary>The receiving shapes no keyword introduces: an arithmetic resultant (<c>… ROUNDED</c>) and INSPECT
    /// TALLYING's counter (<c>identifier-2 FOR …</c>).</summary>
    private static readonly Regex TrailedReceiver = new(
        @"\b(?:dataReference|dataReferenceList)\s+(?:roundedPhrase|ROUNDED|FOR\s+inspectCountPhrase)\b");

    private static readonly Regex LeadsWithBareDataReference = new(@"^\s*(?:dataReference|dataReferenceList)\b");

    /// <summary>The rules that spell a receiving slot over a bare <c>dataReference</c>: directly after a receiving keyword
    /// (or as one alternative of the group it heads), through ONE named rule that begins with a bare data reference (a
    /// receiving list such as INITIALIZE's), or in a keyword-less receiving shape.</summary>
    internal static List<string> BareReceivingSlots(IReadOnlyDictionary<string, string> rules) =>
        rules.Where(kv => TrailedReceiver.IsMatch(kv.Value) || ReceivingSlot.Matches(kv.Value).Any(m =>
                m.Groups["op"].Value is "dataReference" or "dataReferenceList"
                || (m.Groups["op"].Value is not ("receivingIdentifier" or "setReceivingIdentifier")
                    && rules.TryGetValue(m.Groups["op"].Value, out string? body) && LeadsWithBareDataReference.IsMatch(body))))
            .Select(kv => kv.Key).Order(StringComparer.Ordinal).ToList();

    /// <summary>⛔ NO RECEIVING PHRASE OVER A BARE DATA REFERENCE. A receiving slot written `dataReference` re-opens the
    /// parse-error-as-§8.9 refusal for SELF / SUPER / NULL / a view / an address-identifier written there.</summary>
    [Fact]
    public void NoReceivingPhrase_TakesABareDataReference()
    {
        var rules = LoadRules();
        var offenders = BareReceivingSlots(rules).Where(k => !NotReceivingIdentifiers.ContainsKey(k)).ToList();
        Assert.True(offenders.Count == 0, "these grammar rules spell a receiving phrase over a bare dataReference, so a "
            + "non-receivable identifier format written there is a parse error instead of its own rule (kb/Work PB1551): "
            + string.Join(", ", offenders) + ". Use receivingIdentifier (setReceivingIdentifier in a SET statement).");
        var stale = NotReceivingIdentifiers.Keys.Where(k => !rules.ContainsKey(k)).ToList();
        Assert.True(stale.Count == 0, "an exemption names no grammar rule any more — remove it: " + string.Join(", ", stale));
        int served = rules.Count(kv => Regex.IsMatch(kv.Value, @"\b(set)?[rR]eceivingIdentifier\b"));
        Assert.True(served >= 20, $"only {served} rules take a receiving identifier — the scan lost them, so the zero above "
            + "proves nothing (feedback_verdict_evidence_invariant)");
    }

    /// <summary>The selector's own witness (feedback_measure_the_selectors_complement): every receiving shape the grammar
    /// spelled over a bare data reference before kb/Work PB1551 — the review found the first cut of the scan blind to TO,
    /// ACCEPT, SET CONTENT/SIZE OF, INITIALIZE's list, the arithmetic resultant, INSPECT's counter and BY REFERENCE — is
    /// flagged, and the converted shape is not.</summary>
    [Theory]
    [InlineData("moveReceivingPhrase", "TO dataReferenceList")]
    [InlineData("moveStatement", "MOVE (CORRESPONDING | CORR) dataReference TO dataReference")]
    [InlineData("acceptStatement", "ACCEPT dataReference (FROM acceptSource)?")]
    [InlineData("setContentStatement", "SET CONTENT OF? dataReference+ TO arithmeticExpression")]
    [InlineData("setSizeStatement", "SET SIZE OF? dataReference+ TO arithmeticExpression")]
    [InlineData("computeStore", "dataReference roundedPhrase?")]
    [InlineData("inspectTallyingItem", "dataReference FOR inspectCountPhrase+")]
    [InlineData("callByReference", "BY? REFERENCE (addressIdentifier | predefinedNull | dataReference | OMITTED)")]
    [InlineData("unstringIntoTarget", "dataReference (DELIMITER IN? dataReference)? (COUNT IN? dataReference)?")]
    [InlineData("initializeStatement", "INITIALIZE initializeOperandList")]
    public void BareReceivingSlots_FlagsEveryReceivingShape(string rule, string body)
    {
        var rules = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [rule] = body,
            ["initializeOperandList"] = "dataReference (COMMA? dataReference)*",
            ["converted"] = "TO receivingIdentifier (COMMA? receivingIdentifier)*",
        };
        var flagged = BareReceivingSlots(rules);
        Assert.Contains(rule, flagged);
        Assert.DoesNotContain("converted", flagged);
    }
}
