// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A LEVEL-66 RENAMES ENTRY'S PLACE IS BUILT BY THE ONE ITEM→PLACE BUILDER, SO EVERY REFERENCE TO IT TAKES THE
/// SAME TAIL AS EVERY OTHER ITEM (kb/Work PB1380). <c>ReferenceResolver</c> used to compose the alias in an arm of
/// its own inside the syntactic resolver that RETURNED before the reference-modification tail, so <c>RN(2:3)</c>
/// bound the whole alias: ISO §8.4.3.3.4 GR5's "unique data item that is a subset of the data item referenced by
/// identifier-1" was never created, a zero-length slice never raised EC-BOUND-REF-MOD (§7.3.23.3 GR1), and the
/// §8.4.3.3.3 SR1 screen never ran. The same arm was also the only place a RENAMES place could be built, so the
/// by-item entries (<c>ResolveItem</c>, <c>ResolveItemRefMod</c>, <c>ResolveByName</c>) built none.
/// <para>The rule, stated as what these tests hold: inside <c>ReferenceResolver.cs</c> the RENAMES descriptor is
/// read at exactly ONE code line — <c>PlaceForItem</c>'s dispatch to <c>PlaceForRenames</c> — and a
/// <c>RenamesPlace</c> is constructed at exactly one site in <c>src/</c>. A second reader in the resolver is a
/// second arm that can return before the shared tail again.</para>
/// <para><b>Why a source scan.</b> A second arm looks exactly like the working compiler until a program writes the
/// one operand shape it forgets. The behavioural half lives in the conformance corpus:
/// <c>tests/conformance/85/pb1380_renames_ref_mod.cob</c>, <c>tests/conformance/2023/
/// pb1380_renames_ref_mod_zero_length.cob</c> and the negative <c>pb1380-renames-alias-binary-ref-mod</c>.</para>
/// </summary>
public sealed class RenamesPlaceBuilderDriftTests
{
    private static readonly string SrcRoot = Path.Combine(TestRepo.Root, "src");

    private static readonly string ReferenceResolverPath =
        Path.Combine(SrcRoot, "Cobol.Net.Compiler", "Binding", "ReferenceResolver.cs");

    /// <summary>A read of a data item's RENAMES descriptor (<c>x.Renames</c>, not the owner's <c>Renames66</c>).</summary>
    private static readonly Regex RenamesRead = new(@"\.Renames\b(?!66)", RegexOptions.CultureInvariant);

    private static IEnumerable<(string File, int Line, string Text)> CodeLines(string file)
    {
        string[] lines = File.ReadAllLines(file);
        for (int i = 0; i < lines.Length; i++)
        {
            string t = lines[i].Trim();
            if (t.StartsWith("//", StringComparison.Ordinal)) continue;   // prose about the rule is not a site
            yield return (file, i + 1, t);
        }
    }

    /// <summary>The resolver asks "is this a RENAMES entry?" once, in the one builder's dispatch.</summary>
    [Fact]
    public void TheResolver_ReadsTheRenamesDescriptor_OnlyInThePlaceBuilderDispatch()
    {
        var sites = CodeLines(ReferenceResolverPath).Where(l => RenamesRead.IsMatch(l.Text)).ToList();
        Assert.True(sites.Count == 1 && sites[0].Text.Contains("PlaceForRenames(", StringComparison.Ordinal),
            "ReferenceResolver.cs must read a DataItem's RENAMES descriptor at exactly one code line — PlaceForItem's "
            + "dispatch to PlaceForRenames — so a level-66 entry takes the same reference-modification tail as every "
            + "other item (kb/Work PB1380). Found:\n"
            + string.Join("\n", sites.Select(s => $"  line {s.Line}: {s.Text}")));
    }

    /// <summary>A <c>RenamesPlace</c> is composed at one site — <c>PlaceForRenames</c>.</summary>
    [Fact]
    public void ARenamesPlace_IsConstructedAtExactlyOneSite()
    {
        var sites = Directory.EnumerateFiles(SrcRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.Combine("obj", ""), StringComparison.Ordinal)
                        && !f.Contains(Path.Combine("bin", ""), StringComparison.Ordinal))
            .SelectMany(CodeLines)
            .Where(l => l.Text.Contains("new RenamesPlace(", StringComparison.Ordinal))
            .ToList();
        Assert.True(sites.Count == 1 && sites[0].File == ReferenceResolverPath,
            "a RenamesPlace must be built only by ReferenceResolver.PlaceForRenames (kb/Work PB1380). Found:\n"
            + string.Join("\n", sites.Select(s => $"  {Path.GetRelativePath(SrcRoot, s.File)}:{s.Line}: {s.Text}")));
    }
}
