// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using CobolNet.Tests.Shared;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A TABLE'S INITIAL STATE IS EMITTED FROM ITS OCCURRENCE RUNS, NEVER ELEMENT BY ELEMENT (kb/Work PB1722).
/// <para>`03 G OCCURS 10000. 05 T PIC X OCCURS 10000 VALUE "A" FROM (1 1).` is legal source naming one hundred
/// million table elements. The plan materialized a subscript-tuple dictionary entry per element and every lane
/// read it element by element, and the record-struct lane wrote an array literal with one initializer per
/// occurrence whether or not a VALUE was written: a 1000 x 1000 table emitted 20 MB of C# and took 35 to 43 s and
/// 1.9 to 2.5 GB to compile, linearly, so the filed table was an out-of-memory crash. The plan is now its PHRASES
/// (<see cref="TableValuePlan"/>), the lanes compose one element per run period (<see cref="TableValueRuns"/>,
/// <c>OccurrenceRunEmit</c>), and INITIALIZE … TO VALUE tests rank ranges. These tests keep it that way
/// structurally — by the SIZE of what is emitted and by what the plan can hold, never by a clock.</para>
/// </summary>
public sealed class TableInitialStateRunsDriftTests
{
    /// <summary>Every shape of the filed table — a Format 2 VALUE whole and cycling with an overriding phrase, a
    /// Format 1 VALUE on the OCCURS entry, no VALUE, a group VALUE fill, a REDEFINES-aliased (image) record and an
    /// INITIALIZE … TO VALUE over it — emits C# whose length does not depend on its element count: the 10000 x
    /// 10000 program and its 10 x 10 twin differ only by the digits of the numbers they write.</summary>
    [Fact]
    public void EveryTableLane_EmitsTextIndependentOfTheElementCount()
    {
        string big = Emit(Program(10_000)), small = Emit(Program(10));
        Assert.True(big.Length < 20_000, $"the 10000 x 10000 program emitted {big.Length} characters of C#");
        Assert.InRange(big.Length - small.Length, -1000, 1000);   // only the numbers in it are longer
    }

    /// <summary>The plan can hold nothing per element: no member of <see cref="TableValuePlan"/> is a collection
    /// keyed by or enumerating <see cref="Subscripts"/>.</summary>
    [Fact]
    public void TheTableValuePlan_HoldsNoPerElementCollection()
    {
        foreach (var p in typeof(TableValuePlan).GetProperties())
            Assert.False(typeof(IEnumerable).IsAssignableFrom(p.PropertyType)
                    && p.PropertyType.GetGenericArguments().Any(a => a == typeof(Subscripts) || a.IsGenericType
                        && a.GetGenericArguments().Contains(typeof(Subscripts))),
                $"TableValuePlan.{p.Name} ({p.PropertyType.Name}) is a per-element collection. Answer from the "
                + "phrases instead (LiteralAt, TableValueRuns.Of, RankForm) — kb/Work PB1722.");
    }

    /// <summary>No data-division emitter writes a C# array literal over a table's occurrences: a fixed table's
    /// initial elements come from <c>OccurrenceRunEmit.Array</c> (<c>CobolTable.Fill</c>) and nowhere else.</summary>
    [Fact]
    public void NoDataDivisionEmitter_WritesAnArrayLiteralPerOccurrence()
    {
        var offenders = new List<string>();
        foreach (string file in Directory.GetFiles(TestRepo.Src("Cobol.Net.Compiler", "CodeGen", "DataDivision"), "*.cs"))
        {
            string code = Regex.Replace(Regex.Replace(File.ReadAllText(file), @"/\*.*?\*/", "", RegexOptions.Singleline),
                @"//[^\r\n]*", "");
            if (Regex.IsMatch(code, @"ElementType\}\[\]\s*\{\{|Enumerable\.Range\(1,\s*\w+\)\s*\.Select"))
                offenders.Add(Path.GetFileName(file));
        }
        Assert.True(offenders.Count == 0, "per-occurrence table initializer emitted in: " + string.Join(", ", offenders)
            + " — compose one element per run (TableValueRuns.Of + OccurrenceRunEmit), kb/Work PB1722.");
    }

    private static string Program(int n) => $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. TISR{n}.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 R1.
                 03 G1 OCCURS {n}.
                   05 T1 PIC X OCCURS {n}
                      VALUE "A" "B" "C" FROM (1 2)
                            "Z" FROM (5 1) TO (5 {n}).
               01 R2.
                 03 G2 OCCURS {n}.
                   05 T2 PIC 9 OCCURS {n} VALUE 7.
               01 R3.
                 03 G3 OCCURS {n}.
                   05 T3 PIC X OCCURS {n}.
               01 R4 VALUE ALL "AB".
                 03 G4 OCCURS {n}.
                   05 T4 PIC X OCCURS {n}.
               01 R5.
                 03 G5 OCCURS {n}.
                   05 T5 PIC X OCCURS {n} VALUE "Q" FROM (2 1).
               01 R5V REDEFINES R5 PIC X(3).
               PROCEDURE DIVISION.
                   INITIALIZE R1 ALL TO VALUE.
                   DISPLAY T1 (1 1) T2 (1 1) T3 (1 1) T4 (1 1) R5V.
                   STOP RUN.
        """;

    private static string Emit(string src)
    {
        string path = Path.Combine(Path.GetTempPath(), $"tisr_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var emitter = new CSharpEmitter();
            var bound = emitter.Bind(tree!, new EditionContext(2023));
            return emitter.EmitBound(bound, "DRIFTPROBE");
        }
        finally { try { File.Delete(path); } catch (IOException) { /* best-effort */ } }
    }
}
