// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A REPORT PRINTABLE ITEM'S VALUE/SOURCE OPERANDS ARE A LIST, CYCLED BY ONE READER, AND A VALUE IS NOT A
/// MOVE (kb/Work PB506) — and this keeps all three true.
///
/// <para>ISO writes the same two rules twice, once per clause: §13.18.63.3 SR35 / §13.18.53.3 SR6 (a
/// multi-operand clause requires a repeating entry and an operand count that matches its repetitions) and
/// §13.18.63.4 GR23 / §13.18.53.4 GR4 ("successive operands are assigned to successive repeating printable
/// items … If no further operands remain, assignment begins again from the first operand"). The compiler holds
/// ONE operand list (<see cref="ReportFieldModel.Sources"/>), ONE cycling reader
/// (<see cref="ReportFieldModel.SourceAt"/>) and ONE syntax screen
/// (<c>DataBinder.Reports.ScreenRepeatingOperandCount</c>), each fed by both clauses.</para>
///
/// <para>GR23's SECOND sentence — the wrap-around — has no reachable COBOL source today: SR35 admits a short
/// operand list only when a HIGHER-level repeating entry multiplies the count, and both higher-level vehicles
/// (report-group OCCURS, the multiple LINE clause) stage loud. A rule with no reachable population is a rule
/// nothing contradicts (feedback_a_dead_lookup_is_also_unverified), so the wrap is asserted DIRECTLY on the
/// model here, and it will still be right on the day the OCCURS vehicle lands.</para>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class ReportOperandListDriftTests
{
    private static ReportFieldModel Field(int columns, params ReportFieldSource[] sources) => new()
    {
        Columns = Enumerable.Range(1, columns).Select(c => new ReportColumnSpec(false, c * 5)).ToList(),
        PrintItem = new DataItem { Level = 3, CsName = "_t", CobolName = "T" },
        Sources = sources,
    };

    /// <summary>ISO §13.18.63.4 GR23 / §13.18.53.4 GR4, sentence 1: successive operands go to successive
    /// repetitions, in order.</summary>
    [Fact]
    public void SuccessiveOperands_GoToSuccessiveRepetitions()
    {
        var f = Field(3, new FieldValueSource("\"AAA\""), new FieldValueSource("\"BBB\""), new FieldValueSource("\"CCC\""));
        Assert.Equal(["\"AAA\"", "\"BBB\"", "\"CCC\""],
            Enumerable.Range(0, 3).Select(r => ((FieldValueSource)f.SourceAt(r)).Raw).ToArray());
    }

    /// <summary>ISO §13.18.63.4 GR23 / §13.18.53.4 GR4, sentence 2: "If no further operands remain, assignment
    /// begins again from the first operand." Unreachable from COBOL source while the higher-level repetition
    /// vehicles stage loud — asserted on the model so the day one lands, this is already right.</summary>
    [Fact]
    public void OperandsExhausted_AssignmentBeginsAgainFromTheFirst()
    {
        var f = Field(5, new FieldValueSource("\"A\""), new FieldValueSource("\"B\""));
        Assert.Equal(["\"A\"", "\"B\"", "\"A\"", "\"B\"", "\"A\""],
            Enumerable.Range(0, 5).Select(r => ((FieldValueSource)f.SourceAt(r)).Raw).ToArray());
    }

    /// <summary>A single-operand clause — the COBOL-85 shape and the overwhelming majority of real report
    /// entries — gives every repetition the same operand, which is GR23 applied to a one-element list rather
    /// than a separate rule.</summary>
    [Fact]
    public void OneOperand_FeedsEveryRepetition()
    {
        var f = Field(4, new FieldValueSource("\"X\""));
        Assert.All(Enumerable.Range(0, 4), r => Assert.Equal("\"X\"", ((FieldValueSource)f.SourceAt(r)).Raw));
    }

    private static IEnumerable<(string Rel, string Text)> CompilerSources()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler");
        foreach (string f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (f.Contains("Generated", StringComparison.Ordinal)) continue;
            yield return (Path.GetRelativePath(root, f), File.ReadAllText(f));
        }
    }

    /// <summary>⛔ THE REGRESSION THAT WAS THE BUG: a format-4 VALUE operand pushed through the SOURCE clause's
    /// implicit MOVE (§13.18.53.4 GR1). §13.18.63.4 GR21 imports GR7 ("initialization is not affected by a
    /// JUSTIFIED clause and no editing takes place") and GR8, and §13.18.63.3 SR34 imports SR11 — the MOVE
    /// applies exactly those three excluded transforms, and applying them printed `AB  C` for
    /// `PIC XXBXX VALUE "AB CD"`, `   AB` for a JUSTIFIED item and spaces for a BLANK-WHEN-ZERO one, while the
    /// identical WORKING-STORAGE entry was right. No compiler source may name a
    /// <c>FieldValueSource</c> in the same expression as a MOVE conversion again.</summary>
    [Fact]
    public void NoFieldValueSource_IsRoutedThroughTheMoveConversion()
    {
        var sources = CompilerSources().ToList();
        // Population guard (feedback_green_gates_arent_evidence): the type this scans for must still exist and
        // still be read by the emitter, or the regex below passes by looking at nothing.
        Assert.Contains(sources, s => s.Text.Contains("FieldValueSource", StringComparison.Ordinal));
        Assert.Contains(sources, s => s.Rel.EndsWith("ReportWriterEmitter.cs", StringComparison.Ordinal)
            && s.Text.Contains("ConvertSource", StringComparison.Ordinal));

        var routed = new Regex(@"FieldValueSource[^;{}]{0,200}?ConvertSource\(",
            RegexOptions.Singleline | RegexOptions.Compiled);
        var hits = sources.Where(s => routed.IsMatch(s.Text)).Select(s => s.Rel).ToList();
        Assert.True(hits.Count == 0,
            "A format-4 VALUE operand is being converted by the MOVE path again (ISO §13.18.63.4 GR21/GR7/GR8, "
            + "§13.18.63.3 SR34/SR11 exclude JUSTIFIED, editing and BLANK WHEN ZERO from initialization). "
            + "Route it through ValueInitializer.InitializerFrom, the ONE VALUE recipe: " + string.Join(", ", hits));
    }

    /// <summary>The operand LIST has exactly one indexer — <see cref="ReportFieldModel.SourceAt"/>. A second
    /// site indexing <c>Sources[…]</c> is a second copy of GR23's cycling rule, which is how the two-arm
    /// dispatch gets rebuilt (feedback_one_rule_one_place).</summary>
    [Fact]
    public void TheOperandList_HasExactlyOneIndexer()
    {
        var sources = CompilerSources().ToList();
        var indexer = new Regex(@"\bSources\s*\[", RegexOptions.Compiled);
        var hits = sources
            .SelectMany(s => indexer.Matches(s.Text).Select(_ => s.Rel))
            .Where(rel => !rel.EndsWith(Path.Combine("Binding", "DataBinder.Reports.cs"), StringComparison.Ordinal))
            .ToList();
        Assert.True(hits.Count == 0,
            "ReportFieldModel.Sources is indexed outside its own model file; ISO §13.18.63.4 GR23 / §13.18.53.4 "
            + "GR4's per-repetition assignment is ReportFieldModel.SourceAt and nothing else: "
            + string.Join(", ", hits));
    }
}
