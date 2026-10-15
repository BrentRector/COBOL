// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ ONE HOST SURFACE (DESIGN-external-repository §4.4, §17.3; owner decision 2026-10-05; kb/Work PB2099). A .NET host
/// activates a WiseOwl COBOL program through the program's generated host entry, <c>Cobol.&lt;assembly&gt;.&lt;NAME&gt;.Call</c>
/// and <c>Cancel</c>, and through nothing else: <c>ProgramRegistry</c> is the generated code's ABI, public only because
/// generated code in other assemblies calls it. This class is red when a test's code (its host sources included) or a
/// user-facing document names an untyped activation route — <c>ProgramRegistry.CallProgram</c>, <c>Cancel</c>,
/// <c>RunMain</c> or <c>RunModule</c>, the run unit's <c>Programs.CallProgram</c>/<c>Cancel</c>/<c>RunMain</c>/<c>RunModule</c>,
/// <c>ICobolProgram.Call</c> or <c>Activate</c>, or a module's <c>__CobolModule.Register</c> — so a host built from them
/// cannot grow back beside the entry.
/// <para><b>What it scans.</b> Every test <c>.cs</c> file's CODE, read as Roslyn tokens so comments are not scanned and
/// string literals (the C# hosts a test compiles) are; and the documents a user reads — <c>README.md</c>,
/// <c>docs/CONFORMANCE.md</c>, <c>docs/manual/</c> and <c>samples/</c> — whole. The generated code and the runtime are the
/// ABI's own home and are not scanned, and neither are the maintainer design documents, which describe that ABI, nor
/// the history (<c>DEVLOG/</c>, <c>kb/</c>, <c>adjudication/</c>). The ABI's own tests — the classes that assert what
/// generated code emits or that pin the boundary — are listed in <see cref="AbiTests"/>, each with its reason.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class OneHostSurfaceDriftTests
{
    /// <summary>The untyped activation routes. A member access on the facade, on the run unit's program table, on the
    /// program interface, or the module's registrar; <c>GetMethod("Register")</c> is the same registrar reached by
    /// reflection.</summary>
    private static readonly Regex Route = new(
        @"\bProgramRegistry\s*\.\s*(?:CallProgram|Cancel|RunMain|RunModule)\b"
        + @"|\.Programs\s*\.\s*(?:CallProgram|Cancel|RunMain|RunModule)\b"
        + @"|\bICobolProgram\s*\.\s*(?:Call|Activate)\b"
        + @"|\b__CobolModule\s*\.\s*Register\b"
        + @"|GetMethod\(\s*""Register""",
        RegexOptions.CultureInvariant);

    /// <summary>The ABI's own tests: each names a route because its subject is the generated code that calls it.</summary>
    private static readonly Dictionary<string, string> AbiTests = new(StringComparer.Ordinal)
    {
        ["HostEntryShapeDriftTests.cs"] = "asserts that a generated host entry's body makes exactly one boundary call and that its Cancel is one boundary cancel",
        ["RuntimeAbiPinDriftTests.cs"] = "pins the generated boundary's CallProgram and Register calls of the ABI fixture",
        ["OperandActivationDriftTests.cs"] = "asserts which generated statements activate a program through the boundary",
    };

    private static IEnumerable<string> TestSources() =>
        Directory.EnumerateFiles(TestRepo.Tests(), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    /// <summary>A C# file's code without its comments: the text of every token, string literals included.</summary>
    private static string Code(string file) =>
        string.Join(" ", CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot()
            .DescendantTokens().Select(t => t.Text));

    [Fact]
    public void NoTest_ActivatesAProgramThroughAnUntypedRoute()
    {
        var offenders = TestSources()
            .Where(f => !AbiTests.ContainsKey(Path.GetFileName(f)))
            .Select(f => (File: Path.GetRelativePath(TestRepo.Root, f), Hit: Route.Match(Code(f))))
            .Where(x => x.Hit.Success)
            .Select(x => $"{x.File}: {x.Hit.Value}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "a .NET host activates a program through its generated host entry, Cobol.<assembly>.<NAME>.Call/Cancel "
            + "(DESIGN-external-repository §4.4); these tests name an untyped route instead:\n" + string.Join("\n", offenders));
    }

    [Fact]
    public void EveryAbiTest_StillNamesTheAbi()
    {
        // A listed file that no longer names a route is a stale exemption, which would let a future host slip in.
        var stale = AbiTests.Keys
            .Where(name => TestSources().Where(f => Path.GetFileName(f) == name).All(f => !Route.IsMatch(Code(f))))
            .ToList();
        Assert.True(stale.Count == 0, "listed as ABI tests but naming no route (remove them): " + string.Join(", ", stale));
    }

    [Fact]
    public void NoUserDocument_NamesAnUntypedRoute()
    {
        var documents = new[] { TestRepo.At("README.md"), TestRepo.Docs("CONFORMANCE.md") }
            .Concat(new[] { TestRepo.Docs("manual"), TestRepo.At("samples") }
                .Where(Directory.Exists)
                .SelectMany(d => Directory.EnumerateFiles(d, "*", SearchOption.AllDirectories)))
            .Where(f => Path.GetExtension(f) is ".md" or ".cs" or ".cob" or ".cbl" or ".txt");
        var offenders = documents
            .SelectMany(f => File.ReadLines(f).Select((line, i) => (File: f, Line: i + 1, Hit: Route.Match(line))))
            .Where(x => x.Hit.Success)
            .Select(x => $"{Path.GetRelativePath(TestRepo.Root, x.File)}:{x.Line}: {x.Hit.Value}")
            .ToList();
        Assert.True(offenders.Count == 0,
            "the user documentation describes one host interface, the generated host entry (DESIGN-external-repository "
            + "§4.4, §9.4); these lines name an untyped route:\n" + string.Join("\n", offenders));
    }
}
