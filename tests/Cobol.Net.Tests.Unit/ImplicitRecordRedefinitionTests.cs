// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN IMPLICIT REDEFINITION IS NOT A REDEFINES CLAUSE (kb/Work PB836).
///
/// <para><b>The rules.</b> ISO §13.18.33.4 GR3 — "Multiple level 1 entries subordinate to a FD or SD entry represent
/// implicit redefinitions of the same area" — and §12.4.6.4.4 GR2, a record-area SAME clause being "equivalent to an
/// implicit redefinition of the area with records aligned on the leftmost byte position". The REDEFINES-clause syntax
/// rules (§13.18.44.3 SR5/SR12/SR14/SR17) are rules about a written clause and its data-name-2 operand; §13.18.57.3
/// SR4 — "the subject of the entry shall not be implicitly or explicitly redefined in whole or in part" — is the one
/// that reaches the implicit case.</para>
///
/// <para><b>What went wrong.</b> The binder realized the shared area by writing <c>RedefinesTarget</c> on the later
/// records and nothing else, so every REDEFINES-clause screen fired on source containing no REDEFINES: a strong
/// record named "data-name-2" under SR14, a <c>USAGE POINTER</c> record refused under SR12 although no rule forbids
/// it. Each member now carries <see cref="DataItem.RedefinesKind"/>, written only with its target.</para>
/// </summary>
public sealed class ImplicitRecordRedefinitionTests
{
    private const string Strong = """
               01 T1 IS TYPEDEF STRONG.
                  05 A PIC X(4).
        """;

    /// <summary>The TWO ARMS of a symmetric sharing: a strong record is implicitly redefined whichever of the two
    /// records the binder anchors the area on, so §13.18.57.3 SR4 fires for both orders — and neither order may
    /// borrow a REDEFINES-clause rule (before the fix the first order drew SR14 + SR4, the second SR12 alone).</summary>
    [Theory]
    [InlineData("01 F-BIG TYPE T1.\n       01 F-OTHER PIC X(4).")]
    [InlineData("01 F-OTHER PIC X(4).\n       01 F-BIG TYPE T1.")]
    public void StrongRecord_InAMultiRecordFd_IsSr4_NeverAClauseRule(string records)
    {
        var (_, ed) = Compile(Fd(records, Strong));
        var sr4 = Assert.Single(ed.Diagnostics, x => x.Contains("COBOLNET1532", StringComparison.Ordinal));
        Assert.Contains("'F-BIG' is a strongly-typed item", sr4);
        Assert.Contains("§13.18.33.4 GR3", sr4);
        Assert.Contains("§13.18.57.3 SR4", sr4);
        Assert.DoesNotContain(ed.Diagnostics, x => x.Contains("§13.18.44", StringComparison.Ordinal));
    }

    /// <summary>§12.4.6.4.4 GR2's implicit redefinition reaches SR4 the same way, and the diagnostic names the
    /// SAME clause as the construct that shares the area.</summary>
    [Fact]
    public void StrongRecord_InASameRecordArea_IsSr4_CitingTheSameClause()
    {
        string src = $"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. IRRSRA.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT F1 ASSIGN TO "irr_f1.dat" ORGANIZATION IS SEQUENTIAL.
                       SELECT F2 ASSIGN TO "irr_f2.dat" ORGANIZATION IS SEQUENTIAL.
                   I-O-CONTROL.
                       SAME RECORD AREA FOR F1 F2.
                   DATA DIVISION.
                   FILE SECTION.
                   FD F1.
                   01 R1 PIC X(4).
                   FD F2.
                   01 R2 TYPE T1.
                   WORKING-STORAGE SECTION.
            {Strong}
                   PROCEDURE DIVISION.
                       STOP RUN.
            """;
        var (d, ed) = Compile(src);
        Assert.Equal(RedefinitionKind.SameRecordArea, d.ByName["R2"][0].RedefinesKind);
        var sr4 = Assert.Single(ed.Diagnostics, x => x.Contains("COBOLNET1532", StringComparison.Ordinal));
        Assert.Contains("§12.4.6.4.4 GR2", sr4);
        Assert.DoesNotContain(ed.Diagnostics, x => x.Contains("§13.18.44", StringComparison.Ordinal));
    }

    /// <summary>⛔ AN OUT-OF-LINE RECORD IS NEVER LINKED (kb/Work PB981 — determination D-FRA, docs/CONFORMANCE.md
    /// §3). A level-1 <c>USAGE POINTER</c> record is legal (§13.18.60.3 SR14 admits a pointer "only for an
    /// elementary data item at level 1"), and so are a dynamic-length record and a variable-length group; §13.18.33.4
    /// GR3 makes each an implicit redefinition of the FD's area. None of them has a window over the character area,
    /// so the binder does NOT link it into the area's class (which staged every one of them loud as COBOLNET0899,
    /// and before PB836 drew the REDEFINES-clause rules SR12/SR17). It shares the area at the transfer boundary
    /// instead: <see cref="FileModel.OutOfLineRecords"/> is what a READ reaches, and the character-window record is
    /// the <see cref="FileModel.AreaRecord"/>. One row per out-of-line shape, so a new shape added to
    /// <see cref="FileModel.IsOutOfLineRecord"/> without its binder half fails here. (A dynamic-capacity table is
    /// never one: it "may be defined in any place, other than the file section", §8.5.1.9.1 3) — COBOLNET1526.)</summary>
    [Theory]
    [InlineData("01 R3 USAGE POINTER.")]
    [InlineData("01 R3 USAGE PROGRAM-POINTER.")]
    [InlineData("01 R3 PIC X DYNAMIC LENGTH.")]
    [InlineData("01 R3.\n          05 A PIC X(2).\n          05 D PIC X DYNAMIC LENGTH LIMIT 9.")]
    public void OutOfLineRecord_InAMultiRecordFd_IsNotLinked_AndSharesTheAreaAtTheTransfer(string record)
    {
        var (d, ed) = Compile(Fd("01 R2 PIC X(10).\n       " + record, ""));
        Assert.Empty(ed.Diagnostics);
        var r3 = d.ByName["R3"][0];
        Assert.True(FileModel.IsOutOfLineRecord(r3));
        Assert.Equal(RedefinitionKind.None, r3.RedefinesKind);
        Assert.Null(r3.RedefinesTarget);
        var file = Assert.Single(d.Files, f => f.Records.Contains(r3));
        Assert.Same(d.ByName["R2"][0], file.AreaRecord);
        Assert.Same(r3, Assert.Single(file.OutOfLineRecords));
    }

    /// <summary>The record-area SAME clause twin (§12.4.6.4.4 GR2): an out-of-line record of one file is reached by
    /// a READ of the OTHER file — each sharing file learns its peers, and only the character-window records are
    /// linked into one class.</summary>
    [Fact]
    public void OutOfLineRecord_InASameRecordArea_IsReachedThroughThePeerFile()
    {
        string src = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. IRRSAME.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT F ASSIGN TO "irr_f.dat" ORGANIZATION IS SEQUENTIAL.
                   SELECT G ASSIGN TO "irr_g.dat" ORGANIZATION IS SEQUENTIAL.
               I-O-CONTROL.
                   SAME RECORD AREA FOR F G.
               DATA DIVISION.
               FILE SECTION.
               FD F.
               01 R1 PIC X(6).
               FD G.
               01 GD PIC X DYNAMIC LENGTH.
               01 G2 PIC X(3).
               PROCEDURE DIVISION.
                   STOP RUN.
        """;
        var (d, ed) = Compile(src);
        Assert.Empty(ed.Diagnostics);
        var f = Assert.Single(d.Files, x => x.Records.Contains(d.ByName["R1"][0]));
        Assert.Same(d.ByName["GD"][0], Assert.Single(f.OutOfLineRecords));
        Assert.Equal(RedefinitionKind.SameRecordArea, d.ByName["G2"][0].RedefinesKind);
        Assert.Same(d.ByName["R1"][0], d.ByName["G2"][0].RedefinesTarget);
        Assert.Equal(RedefinitionKind.None, d.ByName["GD"][0].RedefinesKind);
    }

    /// <summary>The record kinds, and the clause kind beside them: the one fact every clause screen now reads.</summary>
    [Fact]
    public void TheKind_NamesTheConstruct()
    {
        var (d, ed) = Compile(Fd("01 R1 PIC X(4).\n       01 R2 PIC 9(4).",
            "       01 W1 PIC X(4).\n       01 W2 REDEFINES W1 PIC 9(4)."));
        Assert.Empty(ed.Diagnostics);
        Assert.Equal(RedefinitionKind.None, d.ByName["R1"][0].RedefinesKind);
        Assert.Equal(RedefinitionKind.ImplicitFileRecord, d.ByName["R2"][0].RedefinesKind);
        Assert.Same(d.ByName["R1"][0], d.ByName["R2"][0].RedefinesTarget);
        Assert.Equal(RedefinitionKind.Clause, d.ByName["W2"][0].RedefinesKind);
        // One storage fact for both constructs: both pairs are ONE class.
        Assert.Same(d.ByName["R1"][0].Class, d.ByName["R2"][0].Class);
        Assert.Same(d.ByName["W1"][0].Class, d.ByName["W2"][0].Class);
    }

    private static string Fd(string records, string ws) => $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. IRRFD.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT F ASSIGN TO "irr_f.dat" ORGANIZATION IS SEQUENTIAL.
               DATA DIVISION.
               FILE SECTION.
               FD F.
               {records}
               WORKING-STORAGE SECTION.
        {ws}
               PROCEDURE DIVISION.
                   STOP RUN.
        """;

    /// <summary>Bind a whole program through <c>BinderDriver.Bind</c> (the group tail included) at COBOL-2023.</summary>
    private static (DataBinder Data, EditionContext Edition) Compile(string src)
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_irr_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            var ed = new EditionContext(2023);
            var bound = new BinderDriver().Bind(tree!, ed);
            return (bound.Units[0].Data, ed);
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }
}
