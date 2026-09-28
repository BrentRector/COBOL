// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE DATA MODEL'S CLASSIFICATION IS TOTAL: every bound <see cref="DataItem"/> is a group or an elementary
/// item, and never both and never neither.
///
/// <para><see cref="DataItem.IsElementary"/> is DEFINED as <c>Pic is not null</c> and
/// <see cref="DataItem.IsGroup"/> as <c>Pic is null &amp;&amp; Children.Count > 0</c>. Those two definitions are
/// total only if the binder guarantees two things about the forest it hands on, and the guarantees are the two
/// halves of the picture-PLACEMENT rule:</para>
/// <list type="bullet">
///   <item>NEITHER — an entry with no PICTURE and no subordinates. ISO §13.16.3 SR8's last sentence forbids it;
///   <c>DataBinder.CheckPictureRequired</c> reports it and gives the entry a recovery profile (kb/Work PB487).</item>
///   <item>BOTH — an entry with a PICTURE clause AND subordinates. ISO §13.18.40.3 SR1 forbids it ("The PICTURE
///   clause may be specified only at the elementary level", read with §8.5.1.3.1's "those not further
///   subdivided, are called elementary items"); <c>DataBinder.CheckElementaryOnlyClauses</c> (the table's PICTURE row) reports it and
///   clears the PICTURE so the entry becomes the group its hierarchy declares (kb/Work PB527).</item>
/// </list>
///
/// <para><b>Why the invariant is asserted here and not only through a golden.</b> The BOTH shape had no
/// enforcement site at all, and a model that cannot represent a shape does not fail loudly when it meets one:
/// every consumer that branches on the pair silently picked the elementary arm, so <c>01 G PIC X(3). 05 A PIC
/// X.</c> compiled clean, dropped A from the emitted record struct, and turned the first reference to A into a
/// raw Roslyn <c>CS1061</c> against a generated file. A corpus fixture pins THAT program; this pins the
/// PROPERTY, over the shapes a data division can be built from, so the next clause that can produce an
/// unclassifiable entry is caught by the model rather than by the backend.</para>
/// </summary>
public sealed class PicturePlacementInvariantDriftTests
{
    /// <summary>The data-division shapes an entry's classification has to survive: plain groups, nesting,
    /// tables, REDEFINES, RENAMES, condition-names, FILLER, picture-less usages, a USAGE-bearing group, and the
    /// TYPE / SAME AS copies (whose clones are built by a different path than the written entries).</summary>
    public static TheoryData<string, string> LegalDataDivisions() => new()
    {
        { "PLAIN", "       01  G.\r\n           05  A  PIC X(3).\r\n           05  B  PIC 9(2).\r\n" },
        { "NESTED", "       01  G.\r\n           05  H.\r\n               10  A  PIC X.\r\n           05  B  PIC 9.\r\n" },
        { "TABLE", "       01  G.\r\n           05  T  OCCURS 3.\r\n               10  A  PIC X(2).\r\n" },
        { "ELEMENTARY", "       01  E  PIC X(5).\r\n       77  F  PIC 9(3).\r\n" },
        { "FILLER", "       01  G.\r\n           05  FILLER  PIC X(2).\r\n           05  A  PIC X.\r\n" },
        { "REDEFINES", "       01  G.\r\n           05  A  PIC X(4).\r\n           05  R  REDEFINES A  PIC 9(4).\r\n" },
        { "RENAMES", "       01  G.\r\n           05  A  PIC X(2).\r\n           05  B  PIC X(2).\r\n       66  R  RENAMES A THRU B.\r\n" },
        { "CONDITION", "       01  N  PIC 9(2) VALUE 07.\r\n           88  N-SEVEN  VALUE 07.\r\n" },
        { "PICTURELESS", "       01  G  USAGE INDEX.\r\n           05  A.\r\n           05  B.\r\n" },
        { "GROUPUSAGE", "       01  G  USAGE PACKED-DECIMAL.\r\n           05  A  PIC 9(4).\r\n" },
        { "TYPEDEF", "       01  T  TYPEDEF.\r\n           05  A  PIC X(3).\r\n       01  R  TYPE T.\r\n" },
        { "SAMEAS", "       01  G.\r\n           05  A  PIC X(3).\r\n       01  S  SAME AS G.\r\n" },
        { "VALUEIMPLIED", "       01  W  VALUE \"AB\".\r\n" },   // §13.16.3 SR9's implied PICTURE (kb/Work PB504)
    };

    [Theory]
    [MemberData(nameof(LegalDataDivisions))]
    public void EveryBoundItem_IsAGroupOrElementary_NeverBothAndNeverNeither(string label, string ws)
    {
        var binder = Bind(label, ws);
        foreach (var item in Walk(binder.Roots))
        {
            // Levels 66 and 88 are not Format-1 data description entries (§13.16.2 formats 2 and 3): a
            // condition-name has no storage of its own (§13.16.4 GR3) and a RENAMES entry is a re-description.
            if (item.Level is 66 or 88) continue;
            string where = $"{label}: '{item.CobolName ?? "FILLER"}' (level {item.Level})";
            Assert.True(item.IsGroup ^ item.IsElementary,
                $"{where} is {(item.IsGroup ? "BOTH a group and elementary" : "NEITHER a group nor elementary")} — "
                + $"Pic {(item.Pic is null ? "null" : "set")}, {item.Children.Count} subordinate entries. The data "
                + "model's classification must be total (ISO §13.16.3 SR8 and §13.18.40.3 SR1 together).");
        }
    }

    /// <summary>The ILLEGAL shape §13.18.40.3 SR1 forbids: reported, AND repaired — the recovery is what keeps
    /// the invariant above true for a program that violates the rule, so nothing downstream of the binder can
    /// meet an unclassifiable entry even under an already-failed compile.
    /// <para>⛔ Both halves are asserted. A guard that only REPORTS would leave the model holding the shape it
    /// cannot represent, which is exactly how this defect reached Roslyn instead of the user (kb/Work PB527).</para></summary>
    [Theory]
    [InlineData("       01  G  PIC X(3).\r\n           05  A  PIC X.\r\n")]
    [InlineData("       01  G  PIC 9(3).\r\n           05  A  PIC 9.\r\n")]
    [InlineData("       01  G.\r\n           05  H  PIC X(3).\r\n               10  A  PIC X.\r\n")]
    public void PictureOnAnEntryWithSubordinates_IsReportedAndLeavesTheModelClassifiable(string ws)
    {
        var binder = Bind("SR1", ws, out var diags);
        Assert.Contains(diags, d => d.Contains("COBOLNET2191", StringComparison.Ordinal));
        foreach (var item in Walk(binder.Roots))
            Assert.False(item.Pic is not null && item.Children.Count > 0,
                $"'{item.CobolName ?? "FILLER"}' still carries BOTH a PICTURE and {item.Children.Count} "
                + "subordinate entries after the §13.18.40.3 SR1 screen ran — the recovery did not repair the model.");
    }

    // ── harness ─────────────────────────────────────────────────────────────────────────────────────────────

    private static IEnumerable<DataItem> Walk(IEnumerable<DataItem> items)
    {
        foreach (var item in items)
        {
            yield return item;
            foreach (var c in Walk(item.Children)) yield return c;
        }
    }

    private static DataBinder Bind(string label, string ws) => Bind(label, ws, out _);

    private static DataBinder Bind(string label, string ws, out List<string> binderDiagnostics)
    {
        string id = "PP" + new string(label.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        string src = "       IDENTIFICATION DIVISION.\r\n"
            + $"       PROGRAM-ID. {id}.\r\n"
            + "       DATA DIVISION.\r\n"
            + "       WORKING-STORAGE SECTION.\r\n"
            + ws
            + "       PROCEDURE DIVISION.\r\n"
            + "       MAIN-PARA.\r\n"
            + "           STOP RUN.\r\n";
        string path = Path.Combine(Path.GetTempPath(), "cn_picplace_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
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
