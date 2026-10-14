// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY ACTIVATION WHOSE FORMALS ARE KNOWN AT BIND ASKS THE ONE ARGUMENT HALF OF ISO §14.8.2 (kb/Work PB1418 /
/// PB1115, CLAUDE.md rule 5).
/// <para>§14.9.4.3 SR25 imports §14.8.2, Parameters into a Format-2 CALL and §8.4.3.2.3 SR13 imports the SAME rules
/// into a function-identifier; §14.8.2.3.2 rule 2 lists "a function" beside the NESTED and prototyped program. The
/// CALL binder carried the regime privately and the user-defined-function binder re-implemented CALL's argument
/// binding WITHOUT it, so a PIC X(4) identifier aliased a PIC 9(4) BY REFERENCE function formal, a misaligned bit
/// item crossed BY REFERENCE, and an OBJECT REFERENCE C1 ONLY argument reached an OBJECT REFERENCE C1 formal — each
/// a diagnostic on the CALL side and a silent run on the function side.</para>
/// <para>The shape that keeps the NEXT activation from repeating it: the per-argument conformance
/// (<c>ParameterConformance.CheckArgument</c>), the BY VALUE class answer (<c>ValueArgumentClass</c>) and the
/// bit-alignment proof (<c>ScreenBitAlignment</c>) are DEFINED in <c>ParameterConformance</c> alone, both
/// <c>CallBinder</c> and <c>UdfBinder</c> call them, and INVOKE (<c>OoBinder</c>) asks the same BY CONTENT entry; every
/// lane asks <c>ActivationRelations</c>, the one copy the run-time CALL lane asks too (kb/Work PB3101).</para>
/// </summary>
public sealed class ActivationConformanceDriftTests
{
    private static readonly string VerbsDir =
        TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs");

    /// <summary>The file's code with every comment removed — prose naming a method is not a call to it.</summary>
    private static string CodeOf(string path)
    {
        string text = File.ReadAllText(path);
        text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
        return Regex.Replace(text, @"//[^\r\n]*", "");
    }

    private static string Verb(string file) => CodeOf(Path.Combine(VerbsDir, file));

    [Theory]
    [InlineData("CallBinder.cs")]
    [InlineData("UdfBinder.cs")]
    public void BothActivations_AskTheSharedArgumentConformance(string file)
    {
        string code = Verb(file);
        Assert.True(code.Contains("Params.CheckArgument(", System.StringComparison.Ordinal),
            $"{file} no longer asks ParameterConformance.CheckArgument — its activation's arguments would skip the "
            + "§14.8.2 conformance rules the other activation enforces (kb/Work PB1418).");
        Assert.True(code.Contains("Params.ScreenBitAlignment(", System.StringComparison.Ordinal),
            $"{file} no longer asks ParameterConformance.ScreenBitAlignment — a bit data item passed BY REFERENCE "
            + "would cross unproven (§14.9.4.3 SR6 / §8.4.3.2.3 SR14).");
        Assert.True(code.Contains("ParameterConformance.ValueArgumentClass(", System.StringComparison.Ordinal),
            $"{file} no longer asks ParameterConformance.ValueArgumentClass for its BY VALUE class screen "
            + "(§14.9.4.3 SR22 / §8.4.3.2.3 SR10) — the two screens would classify arguments differently.");
    }

    /// <summary>⛔ INVOKE IS THE THIRD CALLER OF THE OPERAND SCREENS (kb/Work PB1137). §14.9.23.3 states CALL's storage
    /// section rule (SR9/SR11 for SR3/SR7) and bit-alignment rule (SR12 for SR6/SR8) in the same words, and §14.9.23.4
    /// GR6 a) builds the keyword-less mode on SR9 exactly as §14.9.4.4 GR9 a) builds it on SR3 — yet INVOKE's argument
    /// binder re-implemented a slice of CALL's screens and missed half of them: a report's PAGE-COUNTER crossed
    /// BY REFERENCE and was a RETURNING item, a misaligned bit item crossed, and a keyword-less object property
    /// crossed BY REFERENCE so the method's write reached it. INVOKE's arguments, its RETURNING item and its
    /// literal-2 verdict now ask <c>ParameterConformance</c>, and CALL asks the same section screen and mode test.</summary>
    [Theory]
    [InlineData("OoBinder.cs", new[] { "Params.ScreenSection(", "Params.ScreenBitAlignment(",
        "Params.MeetsByReferenceRules(", "Params.ContentConformanceReason(" })]
    [InlineData("CallBinder.cs", new[] { "Params.ScreenSection(", "Params.MeetsByReferenceRules(" })]
    public void TheActivationOperandScreens_AreAskedByCallAndInvoke(string file, string[] calls)
    {
        string code = Verb(file);
        var missing = calls.Where(c => !code.Contains(c, System.StringComparison.Ordinal)).ToList();
        Assert.True(missing.Count == 0,
            $"{file} no longer asks {string.Join(", ", missing)} — its activation's operands would skip a rule the other "
            + "activating statement enforces in the same words (ISO §14.9.4.3 SR3/SR6/SR7/SR8 = §14.9.23.3 "
            + "SR9/SR11/SR12; kb/Work PB1137).");
    }

    [Fact]
    public void TheArgumentRules_AreDefinedInParameterConformanceAlone()
    {
        string[] definitions = ["void CheckArgument(", "ContentConformanceReason(", "void ScreenBitAlignment(",
            "ValueArgumentClass(BoundOperand", "bool ScreenSection(", "bool MeetsByReferenceRules("];
        var offenders = Directory.EnumerateFiles(VerbsDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != "ParameterConformance.cs")
            .SelectMany(f => definitions
                .Where(d => Regex.IsMatch(CodeOf(f), @"(private|internal|public)[^\n;=]*\b" + Regex.Escape(d)))
                .Select(d => $"{Path.GetFileName(f)} defines {d.TrimEnd('(')}"))
            .ToList();
        Assert.True(offenders.Count == 0,
            "A verb binder defines its own copy of a §14.8.2 argument rule that ParameterConformance owns — the "
            + "CALL and function activations would drift apart again (kb/Work PB1418):\n" + string.Join("\n", offenders));
    }

    /// <summary>⛔ A BY CONTENT / BY VALUE ARGUMENT IS ASKED THE ONE RUN-TIME RELATION TOO (kb/Work PB3101). The BY CONTENT
    /// comparator family (<c>OoConformance.ContentMismatch</c>, <c>ContentValueMismatch</c> and the two literal arms)
    /// re-derived §14.8.2.3.3 from <c>DataItem</c> fields while the run-time CALL lane asked
    /// <c>ActivationRelations.CallArgumentViolation</c>, and the two disagreed: a group argument into a numeric formal was
    /// refused at bind (§14.8.2.2 rule 2's MOVE admits it), the run-time lane let a strongly-typed group argument reach a
    /// <c>PIC X</c> formal, and ZERO into a <c>PIC A</c> formal and SPACE into a <c>PIC 1</c> one ran on both (§14.9.25.3
    /// SR6 / SR7). The family is deleted: <c>OoConformance</c> answers a BY CONTENT argument only for the object-reference
    /// pair, which reads the class table as a pair, and every verb binder reaches the rule through
    /// <c>ParameterConformance.ContentConformanceReason</c>.</summary>
    [Fact]
    public void TheContentArgumentRule_IsTheRunTimeRelation_AskedThroughOneEntry()
    {
        var comparator = new Regex(@"^(Content\w*|MoveContent)Mismatch$");
        var copies = typeof(CobolNet.Compiler.Oo.OoConformance)
            .GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.NonPublic)
            .Select(m => m.Name)
            .Where(n => comparator.IsMatch(n))
            .Distinct()
            .ToList();
        Assert.True(copies.Count == 0,
            "OoConformance defines a BY CONTENT argument comparator again (" + string.Join(", ", copies) + ") — a second "
            + "copy of §14.8.2.3.3 beside ActivationRelations.CallArgumentViolation (kb/Work PB3101).");
        Assert.Contains("ActivationRelations.CallArgumentViolation(arg.Mode", Verb("ParameterConformance.cs"));
        var direct = new Regex(@"OoConformance\s*\.\s*\w*Content\w*Mismatch\s*\(");
        var offenders = Directory.EnumerateFiles(VerbsDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != "ParameterConformance.cs" && direct.IsMatch(CodeOf(f)))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(offenders.Count == 0,
            "A verb binder asks an OoConformance BY CONTENT comparator directly instead of "
            + "ParameterConformance.ContentConformanceReason (kb/Work PB1418, PB3101): " + string.Join(", ", offenders));
    }

    /// <summary>⛔ A BY REFERENCE ARGUMENT AND A RETURNING ITEM ARE ASKED THE ONE RUN-TIME RELATION AT BIND TIME (kb/Work
    /// PB3101, PB3127). The pair-mode comparator <c>OoConformance.DescriptionMismatch</c> re-derived §14.8.2.3.2 rule 2 and
    /// §14.8.3 from <c>DataItem</c> fields while the run-time CALL lane asked <c>ActivationRelations</c> over the
    /// <c>ActivationDescription</c>s, and the two disagreed where the standard does not: a reference-modified argument BY
    /// REFERENCE into a prototyped CALL's or a function's elementary formal ran (it has no PICTURE clause, docs/CONFORMANCE.md
    /// D-RM1), and the run-time lane's ANY LENGTH arm compared category and usage only. So the activation entry asks
    /// <c>CallArgumentViolation</c> / <c>CallReturningViolation</c>, and no verb binder asks the pair comparator for an
    /// activation — <c>MoveBinder</c>'s identity-conversion test is a pair question, not an activation.</summary>
    [Fact]
    public void ActivationConformance_AsksTheRunTimeRelations_NotThePairComparator()
    {
        string code = Verb("ParameterConformance.cs");
        foreach (string call in new[] { "ActivationRelations.CallArgumentViolation(", "ActivationRelations.CallReturningViolation(" })
            Assert.True(code.Contains(call, System.StringComparison.Ordinal),
                $"ParameterConformance no longer asks {call.TrimEnd('(')} — the bind-time activation lane would answer a "
                + "pair the run-time CALL lane answers differently (kb/Work PB3101).");
        string[] pairAskers = ["MoveBinder.cs"];
        var offenders = Directory.EnumerateFiles(VerbsDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !pairAskers.Contains(Path.GetFileName(f)))
            .Where(f => Regex.IsMatch(CodeOf(f), @"OoConformance\s*\.\s*DescriptionMismatch\s*\("))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(offenders.Count == 0,
            "A verb binder asks the pair-mode comparator OoConformance.DescriptionMismatch for an activation instead of "
            + "ActivationRelations (kb/Work PB3101): " + string.Join(", ", offenders));
    }

    /// <summary>⛔ THE RETURNING-ITEM HALF OF §14.8.3 IS ONE ENTRY TOO (kb/Work PB1164). §14.8.3.3 rule 1 makes two
    /// object references conform "as if a SET statement were performed" from the activated returning item into the
    /// activating one, while the Format-2 CALL asked §9.3.8.2.3's IDENTITY comparison and refused every legal
    /// widening (a subclass-typed sender into a class-typed or universal receiver) that INVOKE — which had been
    /// given the SET rule — accepted: the repo's most reproducible defect, a two-arm dispatch with one arm fixed.
    /// <c>ParameterConformance.ReturningConformanceReason</c> is the one entry; CALL and INVOKE both ask it, and
    /// neither may hold its own object-reference returning comparison.</summary>
    [Theory]
    [InlineData("CallBinder.cs")]
    [InlineData("OoBinder.cs")]
    public void CallAndInvoke_AskTheOneReturningConformance(string file)
    {
        string code = Verb(file);
        Assert.True(code.Contains("Params.ReturningConformanceReason(", System.StringComparison.Ordinal),
            $"{file} no longer asks ParameterConformance.ReturningConformanceReason — its RETURNING item would be "
            + "checked by a private copy of §14.8.3.3 that the other activating statement does not share.");
    }

    [Fact]
    public void NoOtherVerbBinder_ComparesAReturningItemItself()
    {
        var returningComparator = new Regex(
            @"OoConformance\s*\.\s*(ReturningSender|ObjectRefAssignmentMismatch\s*\([^;]*Returning)");
        var offenders = Directory.EnumerateFiles(VerbsDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) is not ("ParameterConformance.cs"))
            .Where(f => returningComparator.IsMatch(CodeOf(f)))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(offenders.Count == 0,
            "A verb binder applies §14.8.3.3's returning-item sender rule itself instead of asking "
            + "ParameterConformance.ReturningConformanceReason (kb/Work PB1164): " + string.Join(", ", offenders));
    }
}
