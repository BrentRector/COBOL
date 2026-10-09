// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ EVERY NAMESPACE-QUALIFIED NAME THE CODE GENERATOR WRITES IS global::-ROOTED (docs/rearchitecture/
/// DESIGN-external-repository.md §4.5; kb/Work PB2097). Every emitted type lives in the module's namespace
/// <c>Cobol.&lt;S&gt;</c>, <c>&lt;S&gt;</c> the assembly simple name, so the namespace <c>Cobol.System</c> of an assembly
/// named <c>System</c> (or <c>Cobol.CobolNet</c>, <c>Cobol.Microsoft</c>) is a member of the enclosing <c>Cobol</c>
/// namespace, and C# resolves a written <c>System.Math.Max</c> to <c>Cobol.System.Math</c> before it reaches the
/// global namespace: CS0234 on legal source. The generated file's using directives sit inside the module's namespace
/// and name their namespaces from <c>global::</c>, which roots every SIMPLE name; this test roots every QUALIFIED one.
/// It reads every string the compiler's code generator and binder hold (literal and interpolated text, through the
/// Roslyn syntax tree, so the code between an interpolation's braces is not read as text) and is red when one spells
/// <c>System.</c>, <c>CobolNet.</c> or <c>Microsoft.</c> without <c>global::</c> in front.
/// </summary>
public sealed class EmittedGlobalNameDriftTests
{
    /// <summary>A root namespace at the start of a qualified name: not preceded by a name character, a member dot, the
    /// <c>::</c> of <c>global::</c>, or a verbatim <c>@</c>.</summary>
    private static readonly Regex UnrootedQualifiedName = new(@"(?<![\w.:@])(System|CobolNet|Microsoft)\.[A-Za-z_]",
        RegexOptions.Compiled);

    /// <summary>Strings the compiler holds that are NOT C# it emits, with the reason: each is matched by file and by a
    /// fragment of the string, so a new emitted string in the same file is still read.</summary>
    private static readonly (string File, string Fragment, string Why)[] NotEmittedCSharp =
    [
        ("CodeGen/AssemblyPackager.cs", "Microsoft.NETCore.App", "the runtimeconfig JSON names the shared framework"),
        ("Binding/PictureAnalyzer.cs", "System.Decimal is a different", "a diagnostic message a user reads"),
    ];

    private static IEnumerable<(string Rel, string Text)> CompilerStrings()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string rel = Path.GetRelativePath(root, f).Replace('\\', '/');
            if (rel.StartsWith("Generated/", StringComparison.Ordinal) || rel.Contains("/obj/") || rel.StartsWith("obj/", StringComparison.Ordinal)
                || rel.StartsWith("bin/", StringComparison.Ordinal))
                continue;
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(f));
            foreach (var token in tree.GetRoot().DescendantTokens())
                if (token.Kind() is SyntaxKind.StringLiteralToken or SyntaxKind.InterpolatedStringTextToken
                    or SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.MultiLineRawStringLiteralToken
                    or SyntaxKind.Utf8StringLiteralToken)
                    yield return (rel, token.ValueText);
        }
    }

    [Fact]
    public void EveryQualifiedNameTheCompilerWrites_IsGlobalRooted()
    {
        var strings = CompilerStrings().ToList();
        // The scan reads the strings it guards: the using directives the module header writes.
        Assert.Contains(strings, s => s.Text.Contains("using global::CobolNet.Runtime;", StringComparison.Ordinal));
        var unrooted = strings
            .Where(s => UnrootedQualifiedName.IsMatch(s.Text))
            .Where(s => !NotEmittedCSharp.Any(n => n.File == s.Rel && s.Text.Contains(n.Fragment, StringComparison.Ordinal)))
            .Select(s => $"{s.Rel}: \"{s.Text.Trim()}\"")
            .Distinct()
            .ToList();
        Assert.True(unrooted.Count == 0,
            "the compiler writes these qualified names without global::, so an assembly named System, CobolNet or Microsoft "
            + "captures them inside its namespace Cobol.<S> (CS0234 on legal source; DESIGN-external-repository §4.5) — write "
            + "each as global::System.… (or, when the string is not emitted C#, list it in NotEmittedCSharp with why):\n  "
            + string.Join("\n  ", unrooted));
    }

    [Fact]
    public void EveryNotEmittedEntry_StillNamesAString()
    {
        var strings = CompilerStrings().ToList();
        var stale = NotEmittedCSharp
            .Where(n => !strings.Any(s => s.Rel == n.File && s.Text.Contains(n.Fragment, StringComparison.Ordinal)))
            .Select(n => $"{n.File}: {n.Fragment}")
            .ToList();
        Assert.True(stale.Count == 0, "these NotEmittedCSharp entries match no string any more; delete them: " + string.Join(", ", stale));
    }

    [Theory]
    [InlineData("PAY", "Cobol.PAY", "Cobol.PAY")]
    [InlineData("PAY.V2", "Cobol.PAY_u002E_V2", "Cobol.PAY_u002E_V2")]   // never Cobol.PAY's type V2 (design §4.5)
    [InlineData("char-call", "Cobol.char_call", "Cobol.char_call")]
    [InlineData("1PAY", "Cobol._1PAY", "Cobol._1PAY")]                   // an identifier cannot start with a digit
    [InlineData("class", "Cobol.@class", "Cobol.class")]                 // a keyword is escaped in C#, not in metadata
    public void ModuleNamespace_IsOneIdentifierUnderCobol(string assembly, string cs, string clr)
    {
        var ns = CsNames.ModuleNamespaceOf(assembly);
        Assert.Equal(cs, ns.CsName);
        Assert.Equal(clr, ns.ClrName);
        Assert.Equal($"{clr}.__CobolModule", ns.RegistrarClrName);
    }
}
