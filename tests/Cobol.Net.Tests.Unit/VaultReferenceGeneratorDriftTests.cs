// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// "Generator runs clean" check for <c>scripts/gen-vault-reference.ps1</c> — the Obsidian code-reference layer
/// (<c>kb/Reference/</c>), which mirrors every greenfield type's <c>///</c> summary into a per-type note.
/// Because that output is a GITIGNORED build artifact (not committed), this is NOT a committed-vs-regenerated
/// drift comparison like <c>DiagnosticRegistryDriftTests</c>; instead it asserts the generator still PARSES the
/// source trees and completes with exit 0, so a refactor that breaks the parse (or a syntactically-broken script)
/// fails CI rather than silently producing an empty/garbled reference. Requires <c>pwsh</c> on PATH (already a
/// build prerequisite for the ANTLR generation).
/// </summary>
[Trait("Sweep", "tree")]
public sealed class VaultReferenceGeneratorDriftTests
{
    [Fact]
    public void Generator_RunsClean()
    {
        string script = TestRepo.Scripts("gen-vault-reference.ps1");
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
        Assert.True(p.WaitForExit(120_000), "gen-vault-reference.ps1 -Check timed out (>120s)");

        Assert.True(p.ExitCode == 0,
            $"gen-vault-reference.ps1 -Check exited {p.ExitCode}\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        Assert.Contains("type notes", stdout); // sanity: it reported a generation summary
    }
}
