// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Binding.Model;
using CobolNet.Runtime;
using CobolNet.Tests.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A COBOL WORD THAT SPELLS A TYPE THE GENERATED CODE NAMES BY SIMPLE NAME IS NEVER GIVEN THAT SPELLING AS A C# NAME
/// (<see cref="CsNames.ImportedTypeNames"/>; kb/Work PB3270). The generated file imports <c>System</c> and the runtime's
/// namespaces and writes <c>CobolString.Store(…)</c>, <c>HostLanding.LandText(…)</c> and <c>Int128</c> unqualified, so a
/// field, member, parameter or host class spelled the same shadows the type and the module draws CS0236 / CS0120 /
/// CS1061 on legal COBOL. Every <see cref="CsNameScope"/> is therefore seeded with the imported type names, DERIVED
/// from <see cref="CsNames.ImportedNamespaces"/> — and this class keeps the derivation total: the list is the list of
/// using directives the emitter writes; the runtime's side of the seed is every top-level public type Roslyn sees in
/// those namespaces; and the <c>System</c> side, which reflection reads from the core library only, covers every type
/// of the framework's <c>System</c> namespace that any string the compiler holds names unqualified.
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ImportedTypeNameDriftTests
{
    private static readonly Regex UsingDirective = new(@"using global::(?<ns>[A-Za-z0-9_.]+);", RegexOptions.Compiled);

    /// <summary>A simple identifier: not preceded by a name character, a member dot, the <c>::</c> of <c>global::</c> or a
    /// verbatim <c>@</c>.</summary>
    private static readonly Regex SimpleName = new(@"(?<![\w.:@])(?<name>[A-Za-z_]\w*)", RegexOptions.Compiled);

    /// <summary>Strings the compiler holds that are NOT C# it emits, or are C# that names a System type by simple name on
    /// purpose without importing it (a message a user reads, a COBOL intrinsic-function name, a comment): each is matched
    /// by file and by a fragment of the string, so a new emitted string in the same file is still read.</summary>
    private static readonly (string File, string Fragment, string Why)[] NotEmittedCSharp = [];

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

    /// <summary>The compilation the generated code is compiled against, as far as its names go: the framework's reference
    /// set (the host's own, as <c>RoslynBackend</c> builds it) and the runtime assembly.</summary>
    private static CSharpCompilation FrameworkCompilation()
    {
        string frameworkDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var refs = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(Path.GetDirectoryName(p), frameworkDir, StringComparison.OrdinalIgnoreCase))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .Append(MetadataReference.CreateFromFile(typeof(ICobolProgram).Assembly.Location));
        return CSharpCompilation.Create("probe", [], refs, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static INamespaceSymbol NamespaceOf(CSharpCompilation compilation, string dotted) =>
        dotted.Split('.').Aggregate(compilation.GlobalNamespace, (ns, part) => ns.GetNamespaceMembers().Single(n => n.Name == part));

    private static IEnumerable<string> PublicTypeNames(CSharpCompilation compilation, string dotted) =>
        NamespaceOf(compilation, dotted).GetTypeMembers()
            .Where(t => t.DeclaredAccessibility == Accessibility.Public)
            .Select(t => t.Name)
            .Distinct();

    [Fact]
    public void EveryUsingDirectiveTheEmitterWrites_NamesAnImportedNamespace_AndEveryImportedNamespaceHasOne()
    {
        var strings = CompilerStrings().ToList();
        var written = strings.SelectMany(s => UsingDirective.Matches(s.Text).Select(m => m.Groups["ns"].Value)).ToHashSet();
        // The scan reads the directives it guards.
        Assert.Contains("CobolNet.Runtime", written);
        var unlisted = written.Where(ns => !CsNames.ImportedNamespaces.Contains(ns)).ToList();
        Assert.True(unlisted.Count == 0,
            "the emitter writes using directives for these namespaces, whose types it then names by simple name, but "
            + "CsNames.ImportedNamespaces does not list them, so a COBOL word spelled like one of their types shadows it "
            + "(kb/Work PB3270) — add each to CsNames.ImportedNamespaces: " + string.Join(", ", unlisted));
        var unwritten = CsNames.ImportedNamespaces.Where(ns => !written.Contains(ns)).ToList();
        Assert.True(unwritten.Count == 0,
            "CsNames.ImportedNamespaces lists namespaces the emitter writes no using directive for; delete them: "
            + string.Join(", ", unwritten));
    }

    [Fact]
    public void ImportedTypeNames_HoldEveryPublicTypeOfTheRuntimeNamespacesTheGeneratedFileImports()
    {
        var compilation = FrameworkCompilation();
        var missing = new List<string>();
        foreach (string ns in CsNames.ImportedNamespaces.Where(n => n != "System"))
            foreach (string name in PublicTypeNames(compilation, ns))
                if (!CsNames.ImportedTypeNames.Contains(name))
                    missing.Add($"{ns}.{name}");
        Assert.Contains("CobolString", CsNames.ImportedTypeNames.Items);   // the probe sees the names it guards
        Assert.Contains("HostLanding", CsNames.ImportedTypeNames.Items);
        Assert.Contains("Int128", CsNames.ImportedTypeNames.Items);
        Assert.True(missing.Count == 0,
            "these public runtime types are imported by the generated file but CsNames.ImportedTypeNames lacks them: "
            + string.Join(", ", missing));
    }

    [Fact]
    public void EverySystemTypeTheCompilerNamesBySimpleName_IsAnImportedTypeName()
    {
        var compilation = FrameworkCompilation();
        var systemTypes = PublicTypeNames(compilation, "System").ToHashSet(StringComparer.Ordinal);
        Assert.Contains("Console", systemTypes);   // a type outside the core library: the reflection side alone misses it
        var unreserved = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var (rel, text) in CompilerStrings())
        {
            if (NotEmittedCSharp.Any(n => n.File == rel && text.Contains(n.Fragment, StringComparison.Ordinal))) continue;
            foreach (Match m in SimpleName.Matches(text))
            {
                string name = m.Groups["name"].Value;
                if (systemTypes.Contains(name) && !CsNames.ImportedTypeNames.Contains(name))
                    unreserved.Add($"{rel}: {name}  in  \"{Trim(text)}\"");
            }
        }
        Assert.True(unreserved.Count == 0,
            "the compiler writes these System types by SIMPLE name, which the generated file's `using global::System;` "
            + "imports, but CsNames.ImportedTypeNames (the core library's System types) does not hold them, so a COBOL word "
            + "spelled the same shadows the type (kb/Work PB3270) — write each global::System.…, or, when the string is not "
            + "emitted C#, list it in NotEmittedCSharp with why:\n  " + string.Join("\n  ", unreserved));
    }

    private static string Trim(string text) => text.Length <= 80 ? text.Trim() : text.Trim()[..80] + "…";

    [Fact]
    public void ACobolWordSpelledLikeAnImportedType_IsGivenTheNextFreeNameInEveryScopeKind()
    {
        foreach (var scope in new[] { CsNameScope.RecordStruct(), CsNameScope.ProgramClass(), CsNameScope.HostEntry("Host"),
                                      CsNameScope.ModuleTypes(["Registrar"]) })
        {
            Assert.Equal("CobolString_2", CsNames.Allocate("CobolString", scope));
            Assert.Equal("CobolString_3", CsNames.Allocate("CobolString", scope));
            Assert.Equal("Int128_2", CsNames.Allocate("Int128", scope));
            Assert.Equal("HostLanding_2", CsNames.Allocate("HostLanding", scope));
            Assert.Equal("HostLanding_2_2", CsNames.Allocate("HostLanding_2", scope));   // a written HostLanding-2 after the rename
            Assert.Equal("HOSTLANDING", CsNames.Allocate("HOSTLANDING", scope));         // C# is case-sensitive: a different identifier
            Assert.Equal("CobolStringX", CsNames.Allocate("CobolStringX", scope));
        }
    }
}
