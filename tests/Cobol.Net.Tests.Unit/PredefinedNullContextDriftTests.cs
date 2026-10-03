// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE PREDEFINED NULL HAS ONE OPERAND MODEL, AND ONLY A §8.4.3.10.3 SR1 CONTEXT MAY PRODUCE IT (kb/Work PB1427).
/// NULL is an IDENTIFIER — §8.4.3.1.2 Format 8 (predefined-address) and the NULL arm of Format 6 (predefined-object)
/// — and §8.4.3.10.3 SR1 enumerates every context it may be written in: "as a sending operand in an INITIALIZE or a
/// SET statement; as an argument in a program-prototype format CALL statement, a function-prototype format function
/// activation, or a method invocation; or in a pointer-or-object-reference relation condition". While the grammar
/// carried it as a <c>figurativeConstant</c> arm it bound to <c>BoundFigurative('N')</c>, which every generic
/// figurative consumer read as LOW-VALUE, so STRING, INSPECT, UNSTRING and FUNCTION LENGTH ran it, and the refusal
/// was re-stated per verb (MOVE, DISPLAY, the termination status) wherever someone had thought to write one.
/// <para>The shape these tests pin: the token is spelled by ONE grammar rule (<c>predefinedNull</c>; the only other
/// mention is the reserved-word gate that lets COBOL-85 use NULL as a user word); the bound node has ONE producer
/// (<c>ExpressionBinder.NullAdmittingOperand</c>); and every caller of that producer is an SR1 context, named here
/// with the context it implements. A new caller fails the test until it states which SR1 context it is — and an
/// allowance that no longer has a caller fails it too, so the list cannot rot into a superset.</para>
/// </summary>
public sealed class PredefinedNullContextDriftTests
{
    /// <summary>Each file that calls <c>NullAdmittingOperand</c>, with the §8.4.3.10.3 SR1 a) context it binds.</summary>
    private static readonly Dictionary<string, string> Sr1Callers = new()
    {
        ["CallBinder.cs"] = "an argument in a program-prototype format CALL statement",
        ["OoBinder.cs"] = "an argument in a method invocation",
        ["ConditionBinder.cs"] = "a pointer-or-object-reference relation condition",
        ["EvaluateBinder.cs"] = "a relation condition (a selection subject/object pair, §14.9.13.3 SR7 a)",
        ["InitializeBinder.cs"] = "a sending operand in an INITIALIZE statement",
        ["IntrinsicBinder.cs"] = "an argument in a function-prototype format function activation (BindArgOperand's "
            + "nullAdmitting leg, which only UdfBinder sets)",
    };

    private static string StripLineComments(string text, string marker) =>
        string.Join('\n', text.Split('\n').Select(l => l.IndexOf(marker, StringComparison.Ordinal) is var i and >= 0
            ? l[..i] : l));

    private static IEnumerable<string> CompilerSources() =>
        Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));

    [Fact]
    public void NullToken_IsSpelledOnlyByThePredefinedNullRule()
    {
        var grammars = Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Frontend", "Grammar"), "*.g4",
                SearchOption.AllDirectories)
            .Where(p => !Path.GetFileName(p).Contains("Lexer", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(grammars);
        var rule = new Regex(@"^(?<name>[a-z]\w*)\s*$|^(?<name>[a-z]\w*)\s*:", RegexOptions.Multiline);
        var offenders = new List<string>();
        int inPredefinedNull = 0;
        foreach (var g in grammars)
        {
            string text = StripLineComments(File.ReadAllText(g).Replace("\r\n", "\n"), "//");
            foreach (Match m in Regex.Matches(text, @"\bNULL_\b"))
            {
                // The rule a token belongs to is the last rule header written before it.
                var owner = rule.Matches(text[..m.Index]).LastOrDefault()?.Groups["name"].Value ?? "(none)";
                if (owner == "predefinedNull") inPredefinedNull++;
                else if (owner != "reservedGatedWord")   // the COBOL-85 user-word gate, not an operand
                    offenders.Add($"{Path.GetFileName(g)}: NULL_ in rule '{owner}'");
            }
        }
        Assert.True(inPredefinedNull == 1, $"expected the ONE `predefinedNull : NULL_ ;` rule, found {inPredefinedNull} "
            + "NULL_ references in it — the scan no longer sees the rule; fix the scan, not the assertion");
        Assert.True(offenders.Count == 0, "NULL_ is spelled outside `predefinedNull` (§8.4.3.1.2 makes NULL an "
            + "identifier; a second spelling — a figurativeConstant arm above all — re-opens kb/Work PB1427): "
            + string.Join("; ", offenders));
    }

    [Fact]
    public void BoundPredefinedNull_HasOneProducer_AndEveryCallerIsAnSr1Context()
    {
        var producers = new List<string>();
        var callers = new HashSet<string>();
        var nullAdmittingSetters = new List<string>();
        foreach (var file in CompilerSources())
        {
            string name = Path.GetFileName(file);
            string code = StripLineComments(File.ReadAllText(file), "//");
            if (code.Contains("BoundPredefinedNull.Instance", StringComparison.Ordinal)) producers.Add(name);
            if (Regex.IsMatch(code, @"\bNullAdmittingOperand\(") && name != "ExpressionBinder.cs") callers.Add(name);
            if (code.Contains("nullAdmitting: true", StringComparison.Ordinal)) nullAdmittingSetters.Add(name);
        }
        Assert.Equal(["ExpressionBinder.cs"], producers);
        var unlisted = callers.Except(Sr1Callers.Keys).Order().ToList();
        var stale = Sr1Callers.Keys.Except(callers).Order().ToList();
        Assert.True(unlisted.Count == 0, "NullAdmittingOperand is called from a file that names no §8.4.3.10.3 SR1 "
            + "context: " + string.Join(", ", unlisted) + ". NULL may be written only in the contexts SR1 a)/b) "
            + "enumerate; bind every other slot through NonNumericLiteralOperand, which refuses it (COBOLNET2576).");
        Assert.True(stale.Count == 0, "an SR1 allowance has no caller any more — remove it: " + string.Join(", ", stale));
        Assert.Equal(["UdfBinder.cs"], nullAdmittingSetters);
    }

    /// <summary>The §8.4.3.10.3 SR1 refusal has ONE text: every COBOLNET2576 report goes through
    /// <c>PredefinedNullRule.Report</c> (kb/Work PB1427's wave-69 finisher — the operand binder and the concatenation
    /// folder each carried their own wording, and the literal decoders outside the operand model carried none).</summary>
    [Fact]
    public void TheRefusal_IsReportedOnlyThroughPredefinedNullRule()
    {
        var reporters = CompilerSources()
            .Where(f => StripLineComments(File.ReadAllText(f), "//")
                .Contains("DiagnosticCatalog.PredefinedNullContext", StringComparison.Ordinal))
            .Select(Path.GetFileName).Order().ToList();
        Assert.Equal(["PredefinedNullRule.cs"], reporters);
    }

    /// <summary>Files outside the PROCEDURE DIVISION operand model that decode a <c>nonNumericLiteral</c> by its
    /// arms, and why each is exempt from deciding the <c>predefinedNull</c> arm itself. Every other such file must
    /// name it, because NULL used to BE one of the figurative arms these decoders read: when it became its own arm,
    /// the constant entry's decoder took it for the boolean arm and threw, and the VALUE screen counted it a literal
    /// and stored LOW-VALUE (kb/Work PB1427).</summary>
    private static readonly Dictionary<string, string> LiteralDecodersExempt = new()
    {
        ["OptionsBinder.cs"] = "the INITIALIZE fill literal must be a one-byte X\"nn\" (§11.9.10.3 SR1); NULL is not one",
        ["SearchAllFormat2Rules.cs"] = "a PROCEDURE DIVISION operand: the WHEN relation binds NULL through the relation "
            + "checkpoint (COBOLNET0869), and this file only asks whether a literal is zero-length",
    };

    /// <summary>⛔ A literal decoder that reads the <c>figurativeConstant</c> arm of a <c>nonNumericLiteral</c> also
    /// decides the <c>predefinedNull</c> arm (or is listed in <see cref="LiteralDecodersExempt"/> with its reason). The
    /// PROCEDURE DIVISION verb binders are out of scope: their literals bind through
    /// <c>ExpressionBinder.NonNumericLiteralOperand</c>, which refuses NULL, and the test above pins that.</summary>
    [Fact]
    public void EveryLiteralDecoder_OutsideTheOperandModel_DecidesTheNullArm()
    {
        string procedureDir = $"{Path.DirectorySeparatorChar}Procedure{Path.DirectorySeparatorChar}";
        var decoders = CompilerSources()
            .Where(f => !f.Contains(procedureDir, StringComparison.Ordinal))
            .Select(f => (Name: Path.GetFileName(f), Code: StripLineComments(File.ReadAllText(f), "//")))
            .Where(s => s.Code.Contains("figurativeConstant()", StringComparison.Ordinal)
                        && Regex.IsMatch(s.Code, @"nonNumericLiteral\(\)|NonNumericLiteralContext"))
            .ToList();
        Assert.True(decoders.Count >= 5, $"the scan found only {decoders.Count} literal decoders — it no longer sees "
            + "them; fix the scan, not the assertion");
        var silent = decoders.Where(s => !s.Code.Contains("predefinedNull()", StringComparison.Ordinal)
                                         && !LiteralDecodersExempt.ContainsKey(s.Name))
            .Select(s => s.Name).Order().ToList();
        Assert.True(silent.Count == 0, "these files decode a nonNumericLiteral's figurative arm but never decide its "
            + "predefinedNull arm, so NULL falls through to whatever arm they read last: " + string.Join(", ", silent)
            + ". Refuse it through PredefinedNullRule.Report (COBOLNET2576), or list the file with the rule that "
            + "already refuses it.");
        var staleExempt = LiteralDecodersExempt.Keys.Except(decoders.Select(s => s.Name)).Order().ToList();
        Assert.True(staleExempt.Count == 0, "an exemption names a file that no longer decodes a nonNumericLiteral "
            + "— remove it: " + string.Join(", ", staleExempt));
    }
}
