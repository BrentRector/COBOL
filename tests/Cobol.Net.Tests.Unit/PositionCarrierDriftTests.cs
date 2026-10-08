// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Reflection;
using System.Text.RegularExpressions;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ NO POSITION TRAVELS BETWEEN THE BINDER AND THE CODE GENERATOR AS C# TEXT (kb/Work PB2151, D10's second half;
/// docs/rearchitecture/DESIGN-binder-bound-tree.md §3.9). A subscript (ISO §8.4.2.3.2 <c>arithmetic-expression-1</c>),
/// a reference-modifier position (§8.4.3.3.3 SR4) and every offset or ordinal the place model computes from them is
/// a typed <see cref="Position"/>, built by the binder and rendered by <c>CodeGen.PositionRenderer</c> alone. Before
/// PB2151 the binder rendered each position to a string and spliced it into seven string carriers that codegen
/// re-read as text — byte offsets computed as string arithmetic, constancy decided by a character scan, and a
/// position parsed twice through a second grammar entry. These facts keep it from coming back:
/// <list type="bullet">
/// <item>every <c>string</c> property of a place-model record (<see cref="Place"/>, <see cref="AccessSegment"/>,
/// <see cref="WindowCoding"/>) and of a <see cref="Position"/> node is ADJUDICATED below as a C# member name, never an
/// expression — a new string property is a failure here until someone states why it is not a position;</item>
/// <item>no binder source references <c>PositionRenderer</c> — the binder builds structure, it never renders it;</item>
/// <item>the deleted text machinery (<c>RenderSegment</c>, the fragment re-parse, the string hoist and the text
/// evaluator of the bit-alignment proof) is named nowhere under <c>src/</c>.</item>
/// </list>
/// </summary>
public sealed class PositionCarrierDriftTests
{
    /// <summary>The string properties of the place model and the position nodes, each with why it is a NAME — of a C#
    /// member, local or field the code generator emits as is — and not a position expression.</summary>
    private static readonly Dictionary<string, string> AdjudicatedNames = new(StringComparer.Ordinal)
    {
        ["RootFieldSegment.CsField"] = "the root field's C# member name",
        ["MemberSegment.CsMember"] = "a nested record struct member's C# name",
        ["PositionIndexCell.Cell"] = "an index-name's occurrence cell, a C# member name (IndexDeclaration.Cell)",
        ["PositionLocal.CsName"] = "a statement local's C# name",
        ["PositionPointerOffset.PointerField"] = "a BASED item's data-address pointer field, a C# member name",
        ["PositionPointerDynBase.PointerField"] = "a BASED or area-formal class's data-address pointer field, a C# member name",
    };

    private static readonly Type[] CarrierRoots = [typeof(Place), typeof(AccessSegment), typeof(WindowCoding), typeof(Position)];

    [Fact]
    public void EveryStringPropertyOfThePlaceModel_IsAnAdjudicatedName()
    {
        var found = typeof(Place).Assembly.GetTypes()
            .Where(t => CarrierRoots.Any(r => r.IsAssignableFrom(t)))
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.PropertyType == typeof(string) && p.Name != "EqualityContract")
                .Select(p => $"{t.Name}.{p.Name}"))
            .ToHashSet(StringComparer.Ordinal);
        var unadjudicated = found.Where(n => !AdjudicatedNames.ContainsKey(n)).OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.True(unadjudicated.Count == 0,
            "A place-model carrier gained a string property — a position belongs in a typed Position (kb/Work PB2151, "
            + "DESIGN-binder-bound-tree.md §3.9); if it is a C# NAME, adjudicate it here with the reason. Unadjudicated: "
            + string.Join(", ", unadjudicated));
        var stale = AdjudicatedNames.Keys.Where(n => !found.Contains(n)).OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.True(stale.Count == 0, "Adjudicated names that no longer exist (drop them): " + string.Join(", ", stale));
    }

    [Fact]
    public void NoBinderSource_RendersAPosition()
    {
        var offenders = Sources(TestRepo.Src("Cobol.Net.Compiler", "Binding"))
            .Where(s => Regex.IsMatch(s.Src, @"\bPositionRenderer\b"))
            .Select(s => s.File)
            .ToList();
        Assert.True(offenders.Count == 0,
            "The binder builds a typed Position and the code generator renders it; a binder call to PositionRenderer "
            + "is a string carrier coming back (kb/Work PB2151). Offenders: " + string.Join(", ", offenders));
    }

    [Theory]
    [InlineData("RenderSegment")]
    [InlineData("SubscriptExpressionFragment")]
    [InlineData("subscriptExpressionFragment")]
    [InlineData("MaterializeSubscriptSegment")]
    [InlineData("HoistFragment")]
    [InlineData("ConstIndex")]
    public void TheDeletedTextMachinery_IsNamedNowhereUnderSrc(string name)
    {
        var offenders = Directory.EnumerateFiles(TestRepo.Src(), "*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs", StringComparison.Ordinal) || f.EndsWith(".g4", StringComparison.Ordinal))
            .Where(f => !IsBuildOutput(f))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), $@"\b{name}\b"))
            .Select(Path.GetFileName)
            .ToList();
        Assert.True(offenders.Count == 0,
            $"'{name}' was deleted with the string position carrier (kb/Work PB2151) and must not return, not even as a "
            + "name in a comment that invites it back. Found in: " + string.Join(", ", offenders));
    }

    private static IEnumerable<(string File, string Src)> Sources(string dir) =>
        Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsBuildOutput(f))
            .Select(f => (Path.GetFileName(f), StripComments(File.ReadAllText(f))));

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}Generated{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    /// <summary>Drop line and block comments so a name in prose is never mistaken for a call in code.</summary>
    private static string StripComments(string s) =>
        Regex.Replace(Regex.Replace(s, @"/\*.*?\*/", " ", RegexOptions.Singleline), @"//[^\n]*", " ");
}
