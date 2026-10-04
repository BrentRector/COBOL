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
/// ⛔ EVERY <c>ADDRESS OF identifier-1</c> OPERAND PASSES THROUGH THE ONE §8.4.3.11.3 SCREEN (kb/Work PB1407, PB1062).
/// The operand rules of a data-address-identifier (an object reference, a leaf of a strongly-typed group, a CONSTANT
/// RECORD item, a bit item's alignment, a dynamic-capacity shape, object data) are asked by
/// <c>AddressOfOperandScreen.Admit</c>, which <c>PtrBinder.PtrBindAddressOf</c> calls for EVERY surface that takes the
/// identifier — the SET Format-7 sender, the CALL / INVOKE argument and the relation operand all reach it through
/// <c>PtrBinder.BindDataAddress</c>. A surface that built its own <c>BoundAddressOf</c>, or resolved the operand with
/// <c>ReferenceResolver.ResolveForAddressOf</c> itself, would hand back a live pointer the standard forbids — the
/// state of the tree before the screen existed, when only the cell-backing was checked.
/// <para><b>What this can and cannot see.</b> It counts constructions and calls across the compiler's
/// comment-stripped sources. It cannot see an arm that calls the screen and ignores its answer; the negative corpus
/// (<c>pb1407-address-of-*</c>, <c>pb1062-address-of-object-data</c>) covers the behaviour.</para>
/// </summary>
public sealed class AddressOfOperandScreenDriftTests
{
    private const string Owner = "PtrBinder.cs";

    private static IEnumerable<(string File, string Src)> CompilerSources() =>
        Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(f => (Path.GetFileName(f), StripComments(File.ReadAllText(f))));

    [Fact]
    public void TheBoundAddressOperand_IsConstructedInExactlyOnePlace()
    {
        var sites = CompilerSources()
            .SelectMany(s => Enumerable.Repeat(s.File, Regex.Matches(s.Src, @"\bnew\s+BoundAddressOf\s*\(").Count))
            .ToList();
        Assert.True(sites.Count == 1 && sites[0] == Owner,
            $"'new BoundAddressOf' belongs at exactly one site — {Owner}#PtrBindAddressOf, after the §8.4.3.11.3 screen. "
            + $"Found {sites.Count}: {string.Join(", ", sites.Distinct().OrderBy(s => s, StringComparer.Ordinal))}");
    }

    [Fact]
    public void TheOperandIsResolvedOnlyByTheBinderThatScreensIt()
    {
        var callers = CompilerSources()
            .Where(s => Regex.IsMatch(s.Src, @"\.ResolveForAddressOf\s*\("))
            .Select(s => s.File).ToList();
        Assert.True(callers.Count == 1 && callers[0] == Owner,
            $"ReferenceResolver.ResolveForAddressOf is read by {Owner} alone (it screens the result). Found: "
            + string.Join(", ", callers));
    }

    [Fact]
    public void PtrBindAddressOf_AsksTheScreen()
    {
        string src = StripComments(File.ReadAllText(
            TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", Owner)));
        Assert.Matches(@"_addressScreen\s*\.\s*Admit\s*\(", src);
    }

    /// <summary>Drop line and block comments so a rule NAMED in prose is never mistaken for a rule APPLIED in code.</summary>
    private static string StripComments(string s) =>
        Regex.Replace(Regex.Replace(s, @"/\*.*?\*/", " ", RegexOptions.Singleline), @"//[^\n]*", " ");
}
