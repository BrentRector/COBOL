// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY TYPE OF USER-DEFINED WORD IS DECLARED THROUGH THE ONE FUNNEL (kb/Work PB1083; ISO §8.3.2.2).
/// <para>§8.3.2.1 rule 5 and §12.3.8.3 SR12/SR13 forbid a user-defined word of ANY type from spelling an
/// intrinsic-function-name the REPOSITORY paragraph identifies. The screen was called from six hand-picked declaration
/// funnels, so an alphabet-name, a class-name, a symbolic-character, a program-name or a function-prototype-name
/// spelling one compiled clean. Every declaration now announces its word through <c>DataBinder.DeclareUserWord</c> with
/// its <see cref="UserWordKind"/>, and this test keeps that total: the enum carries exactly the types §8.3.2.2 lists
/// (parsed from <c>specs/ISO_COBOL.md</c>, never copied by hand), and every type is declared at some call site or
/// carried below as a type this compiler never declares in a bound source element, with the reason.</para>
/// </summary>
public sealed class UserWordDeclarationDriftTests
{
    /// <summary>The §8.3.2.2 types no binder declares, and why. A type here shall NOT also be declared (the list
    /// would then be stale), and each reason names the rule or the mechanism that makes the exemption true.</summary>
    private static readonly Dictionary<UserWordKind, string> NotDeclaredInASourceElement = new()
    {
        [UserWordKind.CompilationVariableName] = "§8.3.2.1: compilation-variable-names form intersecting sets with "
            + "intrinsic-function-names, and §8.3.2.2 exception 1 — they live in the text-manipulation stage",
        [UserWordKind.LevelNumber] = "a level-number is digits, never an intrinsic-function-name, and declares nothing",
        [UserWordKind.DirectiveName] = "the PUSH/POP directive-name names a compiler directive — a reference processed "
            + "in the text-manipulation stage, not a declaration",
        [UserWordKind.ParameterName] = "a parameterized class's parameter-names are substituted by OoExpansion before any "
            + "binder exists (lead filed with kb/Work PB1083)",
    };

    private static string[] SpecListedTypes()
    {
        string spec = File.ReadAllText(Path.Combine(TestRepo.Root, "specs", "ISO_COBOL.md"));
        int start = spec.IndexOf("The types of user-defined words are:", StringComparison.Ordinal);
        Assert.True(start >= 0, "§8.3.2.2's list header moved in specs/ISO_COBOL.md — re-derive this guard");
        int end = spec.IndexOf("Within a source element, a given user-defined word", start, StringComparison.Ordinal);
        Assert.True(end > start, "§8.3.2.2's list end moved in specs/ISO_COBOL.md — re-derive this guard");
        return [.. Regex.Matches(spec[start..end], @"^— ([a-z-]+)\s*$", RegexOptions.Multiline).Select(m => m.Groups[1].Value)];
    }

    /// <summary>The enum is §8.3.2.2's list: one member per listed type, no member the list does not name.</summary>
    [Fact]
    public void TheEnum_IsTheStandardsList()
    {
        var listed = SpecListedTypes();
        Assert.True(listed.Length >= 30, $"parsed only {listed.Length} types from §8.3.2.2 — re-derive this guard");
        Assert.Equal(listed.Order(StringComparer.Ordinal),
            Enum.GetValues<UserWordKind>().Select(k => k.Spelling()).Order(StringComparer.Ordinal));
    }

    /// <summary>Every type is declared somewhere (a <c>UserWordKind.X</c> argument in the compiler — the enum's only
    /// consumer is <c>DataBinder.DeclareUserWord</c>) or exempt with its reason, never both.</summary>
    [Fact]
    public void EveryType_IsDeclared_OrExemptWithItsReason()
    {
        string enumFile = Path.GetFullPath(TestRepo.Src("Cobol.Net.Compiler", "Binding", "UserWordKind.cs"));
        var used = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cs in Directory.EnumerateFiles(TestRepo.Src("Cobol.Net.Compiler"), "*.cs", SearchOption.AllDirectories))
        {
            if (Path.GetFullPath(cs) == enumFile || cs.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
            foreach (Match m in Regex.Matches(File.ReadAllText(cs), @"\bUserWordKind\.(\w+)"))
                used.Add(m.Groups[1].Value);
        }
        Assert.Contains(nameof(UserWordKind.DataName), used);   // the scan found the declarations at all
        foreach (var kind in Enum.GetValues<UserWordKind>())
        {
            bool declared = used.Contains(kind.ToString());
            bool exempt = NotDeclaredInASourceElement.ContainsKey(kind);
            Assert.True(declared ^ exempt, declared
                ? $"{kind.Spelling()} is declared through DataBinder.DeclareUserWord AND listed exempt — drop the exemption"
                : $"no declaration of a {kind.Spelling()} reaches DataBinder.DeclareUserWord — §8.3.2.1 rule 5 / §12.3.8.3 "
                  + "SR12-SR13 do not reach it; call the funnel where the word is declared, or exempt it with the rule "
                  + "that makes the exemption true");
        }
    }
}
