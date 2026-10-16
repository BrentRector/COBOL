// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE GENERATED PARSER IS A FUNCTION OF ITS INPUTS, DECIDED IN ONE PLACE (kb/Work PB2719, PB2730). The csproj's
/// <c>EnsureGeneratedFiles</c> target is the ONE up-to-date decision for <c>Generated/</c>: its Inputs name every
/// generation input (every <c>Grammar/**/*.g4</c> through an item, the generator script, the vendored jar, and a stamp
/// carrying the <c>-package</c> namespace and the jar path), and it runs the ONE generator directly with every
/// single-sourced value as a parameter. Before this, a second staleness loop (<c>GenerateIfNewer.ps1</c>) compared only
/// <c>.g4</c> timestamps, the target's Inputs held a literal wildcard MSBuild never expands, and the generator's
/// "called directly" guard was true when it was dot-sourced, so every regeneration ran ANTLR twice, first with a
/// hard-coded namespace: a namespace, jar or tool-flag change left a warm tree on a stale parser that a fresh CI
/// checkout never has (the CI invariant). So: (1) the generator declares its namespace, jar and output directory as
/// mandatory parameters with no default and carries no self-invocation guard and no second copy of a csproj value;
/// (2) the target's Inputs carry no wildcard and name the grammar item, the generator, the jar and the stamp, the
/// stamp carries the namespace and the jar, and the Exec passes each value from the csproj; (3) the generator is the
/// only script in the project, so no second up-to-date decision can return.
/// </summary>
public sealed class AntlrGenerationDriftTests
{
    private static readonly string ProjectDir = TestRepo.Src("Cobol.Net.Frontend");

    private static XDocument Project() => XDocument.Load(Path.Combine(ProjectDir, "Cobol.Net.Frontend.csproj"));

    private static string Property(XDocument project, string name) =>
        project.Descendants(name).Select(e => e.Value.Trim()).SingleOrDefault()
        ?? throw new Xunit.Sdk.XunitException($"Cobol.Net.Frontend.csproj defines no <{name}> property");

    private static XElement Target(XDocument project, string name) =>
        project.Descendants("Target").SingleOrDefault(t => (string?)t.Attribute("Name") == name)
        ?? throw new Xunit.Sdk.XunitException($"Cobol.Net.Frontend.csproj has no {name} target");

    private static string GeneratorText(XDocument project) =>
        File.ReadAllText(Path.Combine(ProjectDir, Property(project, "AntlrGenerator")));

    [Fact]
    public void TheGenerator_TakesEverySingleSourcedValueAsAMandatoryParameter()
    {
        var project = Project();
        string script = GeneratorText(project);
        foreach (string parameter in new[] { "PackageName", "JarPath", "OutputDir" })
        {
            Assert.Matches(new Regex(@"\[Parameter\(Mandatory\)\]\s*\[string\]\$" + parameter + @"\s*[,)\r\n]"), script);
        }

        Assert.DoesNotContain(Property(project, "AntlrNamespace"), script, StringComparison.Ordinal);
        Assert.DoesNotContain(Path.GetFileName(Property(project, "AntlrJar")), script, StringComparison.Ordinal);
    }

    [Fact]
    public void TheGenerator_HasNoSelfInvocationGuard()
    {
        // A dot-sourced script sees $MyInvocation.MyCommand.Path equal to $PSCommandPath, so a "called directly" guard
        // is true for its caller too (kb/Work PB2730): the generator is a script, run once by the target, never a
        // function library with an entry point.
        string script = GeneratorText(Project());
        Assert.DoesNotContain("$MyInvocation", script, StringComparison.Ordinal);
        Assert.DoesNotContain("$PSCommandPath", script, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"^function\s+Invoke-Antlr4CSharp\b", RegexOptions.Multiline), script);
    }

    [Fact]
    public void TheTarget_IsTheOneUpToDateDecisionOverEveryGenerationInput()
    {
        var project = Project();
        var target = Target(project, "EnsureGeneratedFiles");
        string inputs = (string?)target.Attribute("Inputs") ?? "";

        // MSBuild expands a wildcard only in an item's Include; in a target's Inputs it names a file that never
        // exists, and the target runs on every build.
        Assert.DoesNotContain("*", inputs, StringComparison.Ordinal);
        Assert.DoesNotContain("?", inputs, StringComparison.Ordinal);
        string[] declared = inputs.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(new[] { "@(AntlrGrammar)", "$(AntlrGenerator)", "$(AntlrJar)", "$(AntlrGenerationStamp)" }, declared);

        string grammarInclude = project.Descendants("AntlrGrammar").Select(e => (string?)e.Attribute("Include")).Single() ?? "";
        Assert.Equal("Grammar/**/*.g4", grammarInclude.Replace('\\', '/'));

        Assert.Equal("WriteAntlrGenerationStamp", (string?)target.Attribute("DependsOnTargets"));
        var stamp = Target(project, "WriteAntlrGenerationStamp");
        string stampLines = string.Join(";", stamp.Descendants("_AntlrGenerationStampLine").Select(e => (string?)e.Attribute("Include")));
        Assert.Contains("$(AntlrNamespace)", stampLines, StringComparison.Ordinal);
        Assert.Contains("$(AntlrJar)", stampLines, StringComparison.Ordinal);
        var write = stamp.Descendants("WriteLinesToFile").Single();
        Assert.Equal("$(AntlrGenerationStamp)", (string?)write.Attribute("File"));
        Assert.Equal("true", (string?)write.Attribute("WriteOnlyWhenDifferent"));

        string command = (string?)target.Descendants("Exec").Single().Attribute("Command") ?? "";
        Assert.Contains("/$(AntlrGenerator)", command, StringComparison.Ordinal);
        Assert.Contains("-PackageName $(AntlrNamespace)", command, StringComparison.Ordinal);
        Assert.Contains("-JarPath \"$(MSBuildProjectDirectory)/$(AntlrJar)\"", command, StringComparison.Ordinal);
        Assert.Contains("-OutputDir ", command, StringComparison.Ordinal);
    }

    [Fact]
    public void TheGenerator_IsTheProjectsOnlyScript()
    {
        // A second script beside it is where a second staleness decision lived (GenerateIfNewer.ps1, kb/Work PB2719).
        string generator = Property(Project(), "AntlrGenerator");
        string[] scripts = Directory.EnumerateFiles(ProjectDir, "*.ps1", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(ProjectDir, f).Replace('\\', '/'))
            .Where(f => !f.StartsWith("bin/", StringComparison.Ordinal) && !f.StartsWith("obj/", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(new[] { generator }, scripts);
    }

    [Fact]
    public void TheGeneratedNamespace_IsTheSuperClassBasesNamespace()
    {
        // The grammar names its superClass CobolParserCoreBase without a namespace, so the hand-written base must live
        // in the package the generator is given; the csproj's <AntlrNamespace> is the one spelling of it.
        string ns = Property(Project(), "AntlrNamespace");
        string baseClass = File.ReadAllText(Path.Combine(ProjectDir, "Parsing", "CobolParserCoreBase.cs"));
        Assert.Matches(new Regex(@"^namespace\s+" + Regex.Escape(ns) + @"\s*;", RegexOptions.Multiline), baseClass);
    }
}
