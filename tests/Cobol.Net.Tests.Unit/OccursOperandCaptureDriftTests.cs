// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A <i>data-name-n</i> CLAUSE OPERAND IS CAPTURED IN ONE PLACE, AND A CAPTURE THAT DROPS WHAT WAS WRITTEN IS
/// A NAMED EXCEPTION, NEVER A DEFAULT (kb/Work PB885). The grammar feeds every data-division and report-section
/// clause the shared <c>dataReference</c> nonterminal, which admits subscripts, reference-modifiers and IN/OF
/// qualifiers. Two captures lost what the programmer wrote:
/// <list type="bullet">
/// <item><c>KeyReference</c> keeps the name and qualifiers and DISCARDS the rest — so the report-writer
/// <c>OCCURS … DEPENDING ON WS-TE (2)</c> bound the unsubscripted name and printed the wrong number of
/// repetitions, in silence, while ISO §13.18.38.3 SR2 says "Data-name-1 and data-name-2 shall not be
/// subscripted";</item>
/// <item><c>dataReference()?.GetText()</c> GLUES the tokens — so the data-division <c>DEPENDING ON CNT OF G1</c>
/// became the undefined name <c>CNTOFG1</c> (legal source rejected) and <c>CAPACITY IN CAP3 (1)</c> silently
/// defined a register spelled <c>CAP3(1)</c>.</item>
/// </list>
/// <c>ClauseDataName</c> is the ONE capture: it screens the shapes §8.4.2.2.2 Format 1 does not admit and keeps
/// the qualifiers. These guards keep it the only one.
/// </summary>
public sealed class OccursOperandCaptureDriftTests
{
    private static string[] BinderFiles() => Directory.GetFiles(
        Path.Combine(TestRepo.Src("Cobol.Net.Compiler"), "Binding"), "DataBinder*.cs");

    /// <summary>No binder file captures a clause operand by gluing its tokens.</summary>
    [Fact]
    public void NoClauseOperand_IsCapturedByGluingItsTokens()
    {
        var files = BinderFiles();
        // Population: the data binder's partial files are where this scan thinks they are.
        Assert.Contains(files, f => f.EndsWith("DataBinder.Odo.cs", StringComparison.Ordinal));
        Assert.Contains(files, f => f.EndsWith("DataBinder.Reports.cs", StringComparison.Ordinal));
        var glued = files
            .SelectMany(f => File.ReadLines(f).Select((l, i) => (File: Path.GetFileName(f), Line: i + 1, Text: l)))
            .Where(x => Regex.IsMatch(x.Text, @"dataReference\(\)\??\.GetText\(\)"))
            .Select(x => $"{x.File}:{x.Line}: {x.Text.Trim()}")
            .ToList();
        Assert.True(glued.Count == 0, "a clause operand captured by GetText() glues `X OF Y` into `XOFY` and a "
            + "subscript into the name; capture it through ClauseDataName:\n" + string.Join("\n", glued));
    }

    /// <summary>The raw <c>KeyReference</c> capture — which DROPS a subscript or reference-modifier — is called
    /// only by <c>ClauseDataName</c> (after its screen) and by the four report-section operands whose own rules
    /// PERMIT, or screen for themselves, what it would drop. A new caller fails here until it is named, with its
    /// reason, in this list.</summary>
    [Fact]
    public void TheDroppingCapture_HasOnlyItsNamedCallers()
    {
        string[] allowed =
        [
            "ClauseDataName",          // DataBinder.cs — calls it only after ScreenClauseOperandShape passes
            "ResolveReportCode",       // §13.18.12.3 SR2 RD CODE: the screen is over the BASE item; the written reference rides on
            "SumAddendRef",            // §13.18.54 SUM identifier-1: a subscript is LEGAL and bound separately
            "UponDetailRef",           // §13.18.54.3 SR7 UPON: screens subscript/ref-mod itself (COBOLNET2046)
            "ControlOperandRef",       // §13.18.16.3 SR4 CONTROL: a ref-mod is LEGAL and kept separately
        ];
        var callers = BinderFiles()
            .SelectMany(f =>
            {
                string? method = null;
                var found = new System.Collections.Generic.List<string>();
                foreach (var line in File.ReadLines(f))
                {
                    var m = Regex.Match(line, @"^    (?:private|internal|public|protected)\b[^=;]*?\b(?!static\b|async\b|override\b|readonly\b|new\b)(\w+)\s*\(");
                    if (m.Success) method = m.Groups[1].Value;
                    if (line.Contains("KeyReference(dref)", StringComparison.Ordinal)
                        || Regex.IsMatch(line, @"\bKeyReference\((?!Core\.)"))
                        found.Add(method ?? "?");
                }
                return found;
            })
            .Where(m => m != "KeyReference")
            .Distinct()
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToList();
        // Population: the scan sees ClauseDataName's own call, so a rename cannot make this pass vacuously.
        Assert.Contains("ClauseDataName", callers);
        Assert.Equal(allowed.OrderBy(m => m, StringComparer.Ordinal), callers);
    }

    /// <summary>⛔ THE REPORT BINDER NEVER INVENTS AN OPERAND (kb/Work PB853). ISO §13.15.3 SR10 — "Every
    /// elementary entry with a COLUMN clause shall also contain either a SOURCE, VALUE or SUM clause" — had no
    /// site, and a <c>new FieldValueSource("SPACE")</c> stood in for the clause the programmer never wrote, which
    /// printed <c>000</c> under <c>PIC 999</c>. Every VALUE operand is the programmer's own literal; a
    /// compiler-spelled one is the fabrication coming back. And the clause-presence screen runs over the flat
    /// entry array BEFORE the walk, so a subtree replay cannot multiply it (kb/Work PB884's shape).</summary>
    [Fact]
    public void TheReportBinder_NeverFabricatesAnOperand()
    {
        string src = File.ReadAllText(
            Path.Combine(TestRepo.Src("Cobol.Net.Compiler"), "Binding", "DataBinder.Reports.cs"));
        // Population: the VALUE-operand construction this guard polices is still in this file.
        Assert.Contains("new FieldValueSource(r)", src, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"new\s+FieldValueSource\(\s*""", RegexOptions.None), src);
        int screen = src.IndexOf("ScreenReportEntryClausePresence(entries, model);", StringComparison.Ordinal);
        // The walk is one call per RD (BindReportSectionEntries binds the report group runs between the RD's constant
        // entries, §13.8.2): the screens above it run once, before the first run and every replay inside it.
        int walk = src.IndexOf("BindReportSectionEntries(rd, entries, model);", StringComparison.Ordinal);
        Assert.True(screen > 0 && walk > screen, "the §13.15.3 clause-presence screen must run once per RD, "
            + "before the entry walk and its replays");
    }
}
