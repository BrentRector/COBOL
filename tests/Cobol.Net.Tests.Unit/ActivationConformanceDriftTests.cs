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
/// <c>CallBinder</c> and <c>UdfBinder</c> call them, and no other verb binder except INVOKE's own lane
/// (<c>OoBinder</c>) reaches the §14.8.2 comparators in <c>OoConformance</c> directly.</para>
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

    [Fact]
    public void TheArgumentRules_AreDefinedInParameterConformanceAlone()
    {
        string[] definitions = ["void CheckArgument(", "ContentConformanceReason(", "void ScreenBitAlignment(",
            "ValueArgumentClass(BoundOperand"];
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

    [Fact]
    public void NoOtherVerbBinder_ReachesTheArgumentComparatorsDirectly()
    {
        // OoBinder is INVOKE's lane over the same comparators (§14.9.23.3); ParameterConformance is the shared one.
        // The ARGUMENT use of DescriptionMismatch is the one carrying §14.8.2.2 rule 1's byRefGroupPrefix; the
        // RETURNING use (§14.8.3.2 — "the same length", no prefix latitude) is a different rule and stays where
        // the CALL statement's RETURNING phrase binds.
        string[] allowed = ["ParameterConformance.cs", "OoBinder.cs"];
        var argumentComparator = new Regex(
            @"OoConformance\s*\.\s*(DescriptionMismatch\s*\([^;]*byRefGroupPrefix|Content\w*Mismatch\s*\()");
        var offenders = Directory.EnumerateFiles(VerbsDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !allowed.Contains(Path.GetFileName(f)))
            .Where(f => argumentComparator.IsMatch(CodeOf(f)))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(offenders.Count == 0,
            "A verb binder calls the §14.8.2 argument comparators directly instead of through "
            + "ParameterConformance.CheckArgument (kb/Work PB1418): " + string.Join(", ", offenders));
    }
}
