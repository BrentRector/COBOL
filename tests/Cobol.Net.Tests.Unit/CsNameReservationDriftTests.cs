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
/// ⛔ EVERY MEMBER THE CODE GENERATOR WRITES INTO A RECORD STRUCT OR A PROGRAM CLASS UNDER A NAME A COBOL WORD CAN
/// SPELL IS RESERVED IN THAT SCOPE'S SEED (<see cref="CsNames"/>; kb/Work PB2093). A COBOL member is named by the ONE
/// allocator, <see cref="CsNames.Allocate"/>, over a scope seeded with the members the scope already carries; a
/// subordinate spelled <c>AsImage</c> or <c>Equals</c> drew CS0102 in the record struct, and a record spelled
/// <c>CloseFiles</c> in the program class, because the allocator knew only the COBOL siblings. So the seeds must be
/// TOTAL: this class reads every member declaration the generator emits under a name that begins with a letter (a
/// sanitized word begins with a letter, or with <c>_</c> and a digit — never <c>_</c> and a letter: ISO §8.3.2.1, "The
/// hyphen or underscore shall not appear as the first or last character in such words") and the members C#
/// synthesizes for a record struct and the
/// <c>ICobolProgram</c> surface a program class implements, and is red when one is in neither seed.
/// </summary>
public sealed class CsNameReservationDriftTests
{
    /// <summary>An emitted member declaration — a string literal that opens with an accessibility modifier:
    /// <c>w.Line($"public readonly string AsImage() …</c>, <c>w.Block("public void FromImage(…</c>, or a declaration
    /// handed to a helper as its signature (<c>"public void DescribeExternals()"</c>); the captured group is the
    /// member's name.</summary>
    private static readonly Regex EmittedMember = new(
        @"\$?@?""\s*(?:public|private|internal|protected)(?:\s+(?:static|readonly|override|sealed|new|const|ref))*\s+[^""(=;]*?\s(?<name>[A-Za-z][A-Za-z0-9_]*)\s*[(=;]",
        RegexOptions.Compiled);

    /// <summary>Members of the two generated classes that carry NO COBOL-named member, so nothing can collide with
    /// them: the <c>__CobolModule</c> registrar's <c>EnsureRegistered</c> and <c>Register</c>, and the entry class <c>Program</c>'s <c>Main</c>
    /// (<c>ProgramEmitter.EmitEntryWrapper</c>).</summary>
    private static readonly HashSet<string> MembersOfClassesWithoutDataMembers = new(StringComparer.Ordinal)
    {
        "Main", "Register", "EnsureRegistered",
    };

    private static IEnumerable<(string Rel, string Name)> EmittedMemberNames()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string f in Directory.EnumerateFiles(Path.Combine(root, "CodeGen"), "*.cs", SearchOption.AllDirectories))
            foreach (string line in File.ReadLines(f))
            {
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;
                foreach (Match m in EmittedMember.Matches(line))
                    yield return (Path.GetRelativePath(root, f), m.Groups["name"].Value);
            }
    }

    [Fact]
    public void EveryEmittedMemberName_IsReservedInItsScope()
    {
        var all = EmittedMemberNames().ToList();
        // The scan finds the members it guards — a regex that matched nothing would pass vacuously.
        Assert.Contains(all, e => e.Name == "AsImage");
        Assert.Contains(all, e => e.Name == "CloseFiles");
        var unreserved = all
            .Where(e => !CsNames.RecordStructMembers.Contains(e.Name) && !CsNames.ProgramClassMembers.Contains(e.Name)
                        && !MembersOfClassesWithoutDataMembers.Contains(e.Name))
            .Select(e => $"{e.Rel}: {e.Name}")
            .Distinct()
            .ToList();
        Assert.True(unreserved.Count == 0,
            "the code generator writes these members under a name a COBOL word can spell, but no CsNames seed reserves "
            + "it, so a data item spelled the same draws CS0102 (kb/Work PB2093) — add each to CsNames.RecordStructMembers "
            + "or CsNames.ProgramClassMembers:\n  " + string.Join("\n  ", unreserved));
    }

    [Fact]
    public void EveryMemberCSharpSynthesizesForARecordStruct_IsReserved()
    {
        var tree = CSharpSyntaxTree.ParseText("record struct S { public int X; }");
        var compilation = CSharpCompilation.Create("probe", [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var s = compilation.GetTypeByMetadataName("S")!;
        var synthesized = s.GetMembers().Where(m => m.CanBeReferencedByName || m.Kind == SymbolKind.Method)
            .Select(m => m.Name)
            .Where(n => n != "X" && !n.StartsWith('.') && !n.Contains('<'))
            .Distinct()
            .ToList();
        Assert.Contains("Equals", synthesized);   // the probe sees the synthesized members it guards
        var missing = synthesized.Where(n => !CsNames.RecordStructMembers.Contains(n)).ToList();
        Assert.True(missing.Count == 0,
            "C# synthesizes these members for every record struct, but CsNames.RecordStructMembers does not reserve "
            + "them: " + string.Join(", ", missing));
    }

    [Fact]
    public void EveryICobolProgramMember_IsReservedInTheProgramClass()
    {
        var missing = typeof(ICobolProgram).GetMethods().Select(m => m.Name)
            .Where(n => !CsNames.ProgramClassMembers.Contains(n)).Distinct().ToList();
        Assert.True(missing.Count == 0,
            "a program class implements ICobolProgram, but CsNames.ProgramClassMembers does not reserve: "
            + string.Join(", ", missing));
    }

    [Fact]
    public void Allocate_TakesTheSmallestFreeSuffix_InSourceOrder()
    {
        var scope = CsNameScope.RecordStruct();
        Assert.Equal("F", CsNames.Allocate("F", scope));
        Assert.Equal("F_2", CsNames.Allocate("F", scope));
        Assert.Equal("F_2_2", CsNames.Allocate("F_2", scope));   // a written F-2 after two F's
        Assert.Equal("AsImage_2", CsNames.Allocate("AsImage", scope));
        Assert.Equal("Equals_2", CsNames.Allocate("Equals", scope));
        var program = CsNameScope.ProgramClass();
        Assert.Equal("CloseFiles_2", CsNames.Allocate("CloseFiles", program));
        Assert.Equal("AsImage", CsNames.Allocate("AsImage", program));   // a record struct's member, not the class's
    }
}
