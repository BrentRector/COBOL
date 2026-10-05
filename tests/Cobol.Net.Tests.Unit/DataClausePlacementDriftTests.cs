// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CobolNet.Binding;
using CobolNet.Frontend.Cst;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DATA-DESCRIPTION CLAUSE-PLACEMENT TABLE IS TOTAL, AND EACH ROW BITES (kb/Work PB507 / PB512 / PB518 /
/// PB519; <c>DataBinder.ClausePlacement.cs</c>).
///
/// <para>The defects this table closed were all one shape: a sentence of §13.16.3 that names SEVERAL clauses,
/// enforced for one of them. SR11 names PICTURE, JUSTIFIED and BLANK WHEN ZERO and only PICTURE had a screen; SR5
/// names REDEFINES and BASED beside EXTERNAL and only BASED had one; SR6/SR7 were <c>continue</c> filters in a
/// registration scan. So the drift tests pin the two properties that keep the NEXT clause from repeating it:
/// every <see cref="DataClauseKind"/> has its placement ANSWERED (a row, or a named site in
/// <see cref="ClausePlacementRules.PlacementAnsweredElsewhere"/>), and every row's subject is actually refused
/// by the binder — a row the screen cannot see would be a list, not a rule.</para>
/// </summary>
public sealed class DataClausePlacementDriftTests
{
    // ── the drift half ─────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EveryClauseKind_HasItsPlacementAnswered_ExactlyOnce()
    {
        var inRows = ClausePlacementRules.Rules.Aggregate(DataClauseKind.None, (acc, r) => acc | r.Clauses);
        foreach (var kind in Enum.GetValues<DataClauseKind>().Where(k => k != DataClauseKind.None))
        {
            bool row = (inRows & kind) != 0;
            bool elsewhere = ClausePlacementRules.PlacementAnsweredElsewhere.ContainsKey(kind);
            Assert.True(row ^ elsewhere,
                $"DataClauseKind.{kind}: its placement must be answered EXACTLY once — a row in "
                + "ClausePlacementRules.Rules, or an entry in PlacementAnsweredElsewhere naming the site that "
                + $"enforces it (row: {row}, elsewhere: {elsewhere}). A new clause forces the question \"where may "
                + "it be written?\"; it must not inherit silence (kb/Work PB512).");
        }
    }

    [Fact]
    public void EveryRule_CarriesItsSentence_AndTheShapeItsKindReads()
    {
        foreach (var rule in ClausePlacementRules.Rules)
        {
            Assert.False(string.IsNullOrWhiteSpace(rule.Sentence), $"{rule.Rule}: no sentence");
            Assert.StartsWith("§", rule.Rule, StringComparison.Ordinal);
            Assert.NotEqual(DataClauseKind.None, rule.Clauses);
            if (rule.Kind is ClausePlacementKind.NotWith)
                Assert.NotEqual(DataClauseKind.None, rule.Excluded);
            else
                Assert.Equal(DataClauseKind.None, rule.Excluded);
            Assert.Equal(rule.Kind is ClausePlacementKind.NotAtLevel, rule.Levels is { Count: > 0 });
            if (rule.Kind is not ClausePlacementKind.Residence)
                Assert.Equal(EntrySections.All, rule.Sections);
        }
    }

    /// <summary>A written sample of each clause that can be the subject of an <see cref="ClausePlacementKind.ElementaryOnly"/>
    /// row. ⛔ A clause joining such a row without a sample here fails <see cref="EveryElementaryOnlySubject_IsRefusedOnAGroup"/>
    /// — add the sample, and the arm in <c>DataBinder.ElementaryOnlyClausesOn</c> that makes the screen see it.</summary>
    private static readonly Dictionary<DataClauseKind, string> ElementaryOnlySample = new()
    {
        [DataClauseKind.Picture] = "PIC X(3)",
        [DataClauseKind.Justified] = "JUSTIFIED RIGHT",
        [DataClauseKind.BlankWhenZero] = "BLANK WHEN ZERO",
        [DataClauseKind.Property] = "PROPERTY",
    };

    [Fact]
    public void EveryElementaryOnlySubject_IsRefusedOnAGroup()
    {
        foreach (var rule in ClausePlacementRules.Rules.Where(r => r.Kind is ClausePlacementKind.ElementaryOnly))
            foreach (var kind in Enum.GetValues<DataClauseKind>().Where(k => k != DataClauseKind.None && (rule.Clauses & k) == k))
            {
                Assert.True(ElementaryOnlySample.TryGetValue(kind, out var clause),
                    $"DataClauseKind.{kind} is the subject of the ElementaryOnly row {rule.Rule} but has no sample "
                    + "in ElementaryOnlySample — the row is unverified.");
                var diags = BindDiags($"       01  G {clause}.\r\n           05  A  PIC 9(3).\r\n");
                Assert.Contains(diags, d => d.Contains(rule.Code.Code, StringComparison.Ordinal)
                                            && d.Contains(DataClauseKinds.Name(kind), StringComparison.Ordinal));
            }
    }

    // ── the behaviour half: each entry-local row refuses, its legal twin binds clean ────────────────────────

    [Theory]
    [InlineData("SR6-LEVEL", "       01  G.\r\n           05  H IS GLOBAL PIC X(4).\r\n")]
    [InlineData("SR6-77", "       77  H IS GLOBAL PIC X(4).\r\n")]
    [InlineData("SR7-GLOBAL", "       01  FILLER IS GLOBAL PIC X(4).\r\n")]
    [InlineData("SR7-EXTERNAL", "       01  IS EXTERNAL PIC X(4).\r\n")]
    [InlineData("EXT-LEVEL", "       01  G.\r\n           05  H IS EXTERNAL PIC X(4).\r\n")]
    [InlineData("EXT-77", "       77  H IS EXTERNAL PIC X(4).\r\n")]
    [InlineData("SR5-REDEFINES", "       01  A PIC X(4).\r\n       01  B REDEFINES A IS EXTERNAL PIC X(4).\r\n")]
    [InlineData("SR5-BASED", "       01  B IS EXTERNAL BASED PIC X(4).\r\n")]
    public void MisplacedExternalOrGlobal_IsRefused_AndNeverRegistered(string label, string ws)
    {
        var binder = Bind(label, "WORKING-STORAGE", ws, out var diags);
        Assert.Contains(diags, d => d.Contains("COBOLNET2404", StringComparison.Ordinal));
        Assert.Empty(binder.CallGlobalRoots);
        Assert.Empty(binder.CallExternalBackings);
    }

    [Theory]
    [InlineData("LOCAL-STORAGE")]
    [InlineData("LINKAGE")]
    public void External_OutsideWorkingStorage_IsRefused(string section)
    {
        Bind("EXT" + section, section, "       01  H IS EXTERNAL PIC X(4).\r\n", out var diags);
        Assert.Contains(diags, d => d.Contains("COBOLNET2404", StringComparison.Ordinal)
                                    && d.Contains("§13.18.22.3 SR1", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("WORKING-STORAGE")]
    [InlineData("LOCAL-STORAGE")]
    [InlineData("LINKAGE")]
    public void Global_AtLevelOne_InEveryAdmittedSection_IsRegistered(string section)
    {
        var binder = Bind("GLB" + section, section, "       01  H IS GLOBAL PIC X(4).\r\n", out var diags);
        Assert.DoesNotContain(diags, d => d.Contains("COBOLNET2404", StringComparison.Ordinal));
        Assert.Contains(binder.CallGlobalRoots, g => g.CobolName == "H");
    }

    /// <summary>kb/Work PB519's RUNTIME arm: an EXTERNAL clause on the redefinER used to re-base the redefines
    /// ANCHOR onto the run-unit cell. The legal spelling — EXTERNAL on the anchor — still re-bases the class.</summary>
    [Fact]
    public void ExternalOnTheAnchor_StillRebasesTheWholeClass()
    {
        var binder = Bind("ANCHOR", "WORKING-STORAGE",
            "       01  A IS EXTERNAL PIC X(4).\r\n       01  B REDEFINES A PIC X(4).\r\n", out var diags);
        Assert.DoesNotContain(diags, d => d.Contains("COBOLNET2404", StringComparison.Ordinal));
        Assert.Single(binder.CallExternalBackings);
        Assert.Equal("A", binder.CallExternalBackings[0].Record.CobolName);
    }

    /// <summary>§13.18.38.3 SR1 a) (kb/Work PB1260) — "The OCCURS clause shall not be specified in a data description
    /// entry that: a) Has a level-number of 01, 66, 77, or 88". The row refuses a level-01 or level-77 table in every
    /// section, and its legal twin (the table one level below a group) binds clean.</summary>
    [Theory]
    [InlineData("WORKING-STORAGE", "       01  A PIC X OCCURS 3.")]
    [InlineData("WORKING-STORAGE", "       77  A PIC X OCCURS 3.")]
    [InlineData("WORKING-STORAGE", "       01  A PIC X OCCURS 1 TO 3 DEPENDING ON N.")]
    [InlineData("WORKING-STORAGE", "       01  A PIC X OCCURS DYNAMIC.")]
    [InlineData("LOCAL-STORAGE", "       01  A PIC X OCCURS 3.")]
    [InlineData("LINKAGE", "       01  A PIC X OCCURS 3.")]
    public void OccursAtLevel01Or77_IsRefused(string section, string entry)
    {
        Bind("OCCLVL", section, "       77  N PIC 9.\r\n" + entry + "\r\n", out var diags);
        Assert.Contains(diags, d => d.Contains("COBOLNET2404", StringComparison.Ordinal)
                                    && d.Contains("§13.18.38.3 SR1 a)", StringComparison.Ordinal));
    }

    [Fact]
    public void OccursBelowLevelOne_BindsClean()
    {
        Bind("OCCOK", "WORKING-STORAGE", "       01  G.\r\n           05  A PIC X OCCURS 3.\r\n", out var diags);
        Assert.DoesNotContain(diags, d => d.Contains("COBOLNET2404", StringComparison.Ordinal));
    }

    // ── the subject rules (§13.18.8.3 SR1/SR2, §13.18.32.3 SR3/SR4) ────────────────────────────────────────

    [Theory]
    [InlineData("       01  X PIC X(5) BLANK WHEN ZERO.\r\n", "§13.18.8.3 SR1")]
    [InlineData("       01  X PIC 9(5) USAGE COMP BLANK WHEN ZERO.\r\n", "§13.18.8.3 SR2")]
    [InlineData("       01  X PIC 9(5) USAGE PACKED-DECIMAL BLANK WHEN ZERO.\r\n", "§13.18.8.3 SR2")]
    [InlineData("       01  X USAGE POINTER BLANK WHEN ZERO.\r\n", "§13.18.8.3 SR1")]
    [InlineData("       01  X PIC 9(5) JUSTIFIED RIGHT.\r\n", "§13.18.32.3 SR3")]
    [InlineData("       01  X PIC ZZ9 JUSTIFIED.\r\n", "§13.18.32.3 SR3")]
    [InlineData("       01  X PIC XXBXX JUSTIFIED.\r\n", "§13.18.32.3 SR3")]
    [InlineData("       01  X USAGE INDEX JUSTIFIED.\r\n", "§13.18.32.3 SR3")]
    public void ClauseOnAnInadmissibleSubject_IsRefused(string ws, string rule)
    {
        Bind("SUBJ", "WORKING-STORAGE", ws, out var diags);
        Assert.Contains(diags, d => d.Contains("COBOLNET2405", StringComparison.Ordinal)
                                    && d.Contains(rule, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("       01  X PIC 9(5) BLANK WHEN ZERO.\r\n")]
    [InlineData("       01  X PIC ZZ9.99 BLANK WHEN ZERO.\r\n")]
    [InlineData("       01  X PIC 9(3) USAGE NATIONAL BLANK WHEN ZERO.\r\n")]
    [InlineData("       01  X PIC X(5) JUSTIFIED RIGHT.\r\n")]
    [InlineData("       01  X PIC A(5) JUST.\r\n")]
    [InlineData("       01  X PIC N(5) JUSTIFIED.\r\n")]
    [InlineData("       01  X PIC 1(5) JUSTIFIED.\r\n")]
    public void ClauseOnAnAdmissibleSubject_BindsClean(string ws)
    {
        Bind("LEGAL", "WORKING-STORAGE", ws, out var diags);
        Assert.DoesNotContain(diags, d => d.Contains("COBOLNET2405", StringComparison.Ordinal)
                                          || d.Contains("COBOLNET2403", StringComparison.Ordinal));
    }

    /// <summary>§13.18.32.3 SR4 — "The JUSTIFIED clause shall not be specified for a dynamic-length elementary
    /// item." Enforced by §13.16.3 SR18's permitted set (COBOLNET1563), which is the same fact, so
    /// <c>CheckClauseSubjects</c> carries no second arm; this pins that the fact IS reported.</summary>
    [Fact]
    public void JustifiedOnADynamicLengthItem_IsRefused()
    {
        Bind("JDL", "WORKING-STORAGE", "       01  D PIC X DYNAMIC LENGTH JUSTIFIED.\r\n", out var diags);
        Assert.Contains(diags, d => d.Contains("COBOLNET1563", StringComparison.Ordinal)
                                    && d.Contains("JUSTIFIED", StringComparison.Ordinal));
    }

    // ── harness ─────────────────────────────────────────────────────────────────────────────────────────────

    private static List<string> BindDiags(string ws)
    {
        Bind("ELEM", "WORKING-STORAGE", ws, out var diags);
        return diags;
    }

    private static DataBinder Bind(string label, string section, string entries, out List<string> binderDiagnostics)
    {
        string id = "CP" + new string(label.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        string src = "       IDENTIFICATION DIVISION.\r\n"
            + $"       PROGRAM-ID. {id[..Math.Min(id.Length, 30)]}.\r\n"
            + "       DATA DIVISION.\r\n"
            + $"       {section} SECTION.\r\n"
            + entries
            + "       PROCEDURE DIVISION.\r\n"
            + "       MAIN-PARA.\r\n"
            + "           STOP RUN.\r\n";
        string path = Path.Combine(Path.GetTempPath(), "cn_clplace_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend() { InitialFormat = InitialReferenceFormat.Auto }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            var program = tree!.compilationGroup().SelectMany(g => g.programUnit()).First();
            var binder = new DataBinder();
            binder.Bind(program);
            binderDiagnostics = binder.Edition.Diagnostics.ToList();
            return binder;
        }
        finally
        {
            try { File.Delete(path); } catch (IOException) { }
        }
    }
}
