// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// "Generator runs clean" check for <c>scripts/gen-grammar-diagrams.ps1</c> — the Obsidian grammar railroad-diagram
/// layer (<c>kb/Grammar/</c>), which renders one syntax diagram per rule for each ANTLR parser fragment via the
/// vendored bottlecaps <c>tools/rr/rr.war</c>. Like <see cref="VaultReferenceGeneratorDriftTests"/>, the output is a
/// GITIGNORED build artifact, so this is NOT a committed-vs-regenerated comparison; it asserts the generator still
/// cleans every <c>.g4</c> fragment to valid EBNF and rr renders it with exit 0 — so a grammar edit that breaks the
/// cleaning pass (or a broken script/missing tool) fails CI rather than silently producing empty/garbled diagrams.
/// Requires <c>pwsh</c> and <c>java</c> on PATH (both already build prerequisites for the ANTLR generation).
/// </summary>
[Trait("Sweep", "tree")]
public sealed class GrammarDiagramGeneratorDriftTests
{
    [Fact]
    public void Generator_RunsClean()
    {
        string script = TestRepo.Scripts("gen-grammar-diagrams.ps1");
        Assert.True(File.Exists(script), $"missing generator: {script}");

        var psi = new ProcessStartInfo("pwsh", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -Check")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = TestRepo.Root,
        };

        using var p = Process.Start(psi)!;
        string stdout = p.StandardOutput.ReadToEnd();
        string stderr = p.StandardError.ReadToEnd();
        Assert.True(p.WaitForExit(180_000), "gen-grammar-diagrams.ps1 -Check timed out (>180s)");

        Assert.True(p.ExitCode == 0,
            $"gen-grammar-diagrams.ps1 -Check exited {p.ExitCode}\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        Assert.Contains("grammar-diagram note", stdout); // sanity: it reported a generation summary
    }
}
