// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// A <see cref="Editions.Diagnostics.DiagnosticDescriptor"/> carries TWO identifiers and they are not
/// interchangeable: <c>Code</c> is the emitted <c>COBOLNETnnnn</c> the user sees, is documented in
/// <c>docs/DIAGNOSTICS.md</c>, and is what <c>--suppress</c> falls back to; <c>Id</c> is the stable kebab-case
/// slug that survives renumbering. <c>EditionContext.Error(string, string)</c> takes the CODE, so passing
/// <c>descriptor.Id</c> to it compiles perfectly and silently emits the SLUG in the code position —
/// <c>error intrinsic-argument-class: …</c> where every doc, every golden and every user expects
/// <c>error COBOLNET1627: …</c>.
/// <para>⛔ THIS IS NOT HYPOTHETICAL. It was found by the PB8 sibling sweep (CLAUDE.md rule 4) and it had
/// already shipped twice on the day it was found: PB1's <c>COBOLNET1627</c> and PB6's <c>COBOLNET1628</c> both
/// emitted their slug, so the code their <c>DIAGNOSTICS.md</c> rows advertise was never printed. Both landed
/// under a green battery — nothing in it looked at the code position — which is exactly why the guard is a
/// SOURCE-FORM check and not a behavioural one: no runtime test can see a mistake that only a caller can make.</para>
/// <para>The descriptor overload <c>Error(DiagnosticDescriptor, string)</c> is the correct form and reads
/// better anyway. This test admits it and bare <c>"COBOLNETnnnn"</c> literals, and rejects only the
/// <c>.Id</c> spelling in a code position.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class DiagnosticEmitFormDriftTests
{
    // Error( / Warning( / Removed( whose FIRST argument ends in `.Id` — the slug-in-the-code-position mistake.
    // Removed is in the set because it is the THIRD emit form on EditionContext (the strict/permissive severity
    // seam) and takes a code in the same position: a guard that names only two of three emit forms teaches the
    // next caller that the third is exempt (feedback_two_arm_dispatch — ask which arm you fixed).
    private static readonly Regex SlugInCodePosition =
        new(@"\.(?:Error|Warning|Removed)\(\s*[A-Za-z0-9_.]*\.Id\s*,", RegexOptions.Compiled);

    [Fact]
    public void NoDiagnosticIsEmittedByItsSlug_InTheCodePosition()
    {
        var offenders = new List<string>();
        foreach (string file in Directory.EnumerateFiles(TestRepo.Src(), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}")) continue;
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (SlugInCodePosition.IsMatch(lines[i]))
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
        }

        Assert.True(offenders.Count == 0,
            "A diagnostic is emitted by its kebab-case Id instead of its COBOLNETnnnn Code, so the code the "
            + "user sees is the slug. Pass the descriptor itself — Error(DiagnosticCatalog.X, …) — which uses "
            + "descriptor.Code. Offending sites:\n  " + string.Join("\n  ", offenders));
    }
}
