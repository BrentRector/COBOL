// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB433 — <b>the SECTION travels with a resolved procedure-name, and §14.9.28.3 SR11 is asked of it.</b>
/// "When procedure-name-1 and procedure-name-2 are both specified and either is the name of a procedure in the
/// declaratives portion of the procedure division, both shall be procedure-names in the same declarative
/// section" (ISO §14.9.28.3 SR11).
///
/// <para><b>What the missing structure cost.</b> The resolver returned a naked pc pair, so by the time the range
/// existed nothing could say which section either end came from and the rule was implemented NOWHERE. The loud
/// half — one end declarative, one not — compiled clean at every <c>--std</c> and then ran the positional range
/// forward out of the declarative section into whatever physically followed, including the PERFORM itself, until
/// the CLR killed the run unit with a stack overflow. The quiet half — both ends declarative, in DIFFERENT
/// declarative sections — ran and printed a wrong answer, spanning two USE procedures with different dispatch.</para>
///
/// <para>These pins hold BOTH halves against one predicate and hold the structure that makes the next rule about
/// a procedure's section (GO TO's, ALTER's, the SORT/MERGE procedure phrases, §14.9.49.3 SR3/SR4) a predicate
/// rather than a re-derivation: the ONE success constructor carries the section, and no site re-derives
/// "declarative" from pc arithmetic.</para>
/// </summary>
public sealed class ProcedureRangeDeclarativesDriftTests
{
    // ── The structure: the section travels, and nothing re-derives it ────────────────────────────────────────

    /// <summary>One success constructor for a resolution, and it takes the section. A second path that builds a
    /// <c>ResolvedProcedure</c> inline is how a resolution learns to forget the section again.</summary>
    [Fact]
    public void ResolvedProcedure_HasOneSuccessConstructor_AndItCarriesTheSection()
    {
        string builder = File.ReadAllText(
            TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "ProcedureTableBuilder.cs"));
        Assert.Single(Regex.Matches(builder, @"new ResolvedProcedure\("));
        Assert.Contains("private static ProcedureResolution Found(PcRange range, SectionInfo? section)", builder);

        string resolution = File.ReadAllText(
            TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "ProcedureResolution.cs"));
        Assert.Contains("internal readonly record struct ResolvedProcedure(PcRange Range, SectionInfo? Section)",
            resolution);
        Assert.Contains("public bool IsDeclarative => Section is { IsDeclarative: true };", resolution);
    }

    /// <summary>No rule asks "is this procedure declarative?" by comparing its pc against the entry pc. That
    /// arithmetic is a re-derivation of a fact the collection already recorded (ISO §14.3 makes the declaratives
    /// portion a set of SECTIONS), and it was the shape RESUME AT's §14.9.33.3 SR3 check carried before PB433.</summary>
    [Fact]
    public void NoBinderSite_ReDerivesDeclarativeFromPcArithmetic()
    {
        var offenders = new List<string>();
        string root = TestRepo.Src("Cobol.Net.Compiler", "Binding");
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            foreach (Match m in Regex.Matches(File.ReadAllText(file), @"[^\n]*\bEntryPc\b[^\n]*"))
                if (Regex.IsMatch(m.Value, @"(Start|End|pc|Pc)\s*[<>]=?\s*[A-Za-z_.]*EntryPc")
                    && !m.Value.TrimStart().StartsWith("//", StringComparison.Ordinal))
                    offenders.Add($"{Path.GetFileName(file)}: {m.Value.Trim()}");
        Assert.True(offenders.Count == 0,
            "a procedure's PORTION is a property of its SECTION, not of its pc (kb/Work PB433):\n"
            + string.Join("\n", offenders));
    }

    // ── The rule: both halves, every edition, plus the controls ──────────────────────────────────────────────

    /// <summary>HALF 1 — procedure-name-1 in the declaratives, procedure-name-2 outside. SR11 is COBOL-85's rule
    /// carried unchanged through 2002/2014/2023, so it is refused at every edition, not gated at one.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ARangeStraddlingTheDeclarativesBoundary_IsRefused(int level)
    {
        var diagnostics = BindDiagnostics(Straddle, level);
        Assert.Contains(diagnostics, d => d.Contains("COBOLNET2186") && d.Contains("§14.9.28.3 SR11"));
    }

    /// <summary>HALF 2 — the quiet one. Both names ARE in the declaratives portion, so a screen written as "one
    /// end declarative, the other not" passes this program through; SR11 requires the SAME declarative section.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ARangeSpanningTwoDeclarativeSections_IsRefused(int level)
    {
        var diagnostics = BindDiagnostics(TwoDeclarativeSections, level);
        Assert.Contains(diagnostics, d => d.Contains("COBOLNET2186") && d.Contains("§14.9.28.3 SR11"));
    }

    /// <summary>The control, and it is the load-bearing half: a range whose two ends are procedures of the SAME
    /// declarative section conforms, a range wholly outside the declaratives conforms, and a ONE-name PERFORM of
    /// a declarative section is not the rule's subject at all ("procedure-name-1 and procedure-name-2 are both
    /// specified"). Without this, refusing every PERFORM that touches the declaratives would pass the two tests
    /// above.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void LegalRanges_AreAccepted(int level)
    {
        var diagnostics = BindDiagnostics(Legal, level);
        Assert.DoesNotContain(diagnostics, d => d.Contains("COBOLNET2186"));
        Assert.True(diagnostics.Count == 0, string.Join("\n", diagnostics));
    }

    private const string Straddle = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB433DRIFTSTRADDLE.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT F1 ASSIGN TO "pb433-drift-1.dat"
                       ORGANIZATION IS SEQUENTIAL.
               DATA DIVISION.
               FILE SECTION.
               FD F1.
               01 F1-REC PIC X(10).
               WORKING-STORAGE SECTION.
               01 N PIC 9(4) VALUE 0.
               PROCEDURE DIVISION.
               DECLARATIVES.
               D-SEC SECTION.
                   USE AFTER STANDARD ERROR PROCEDURE ON F1.
               D-P1.
                   ADD 1 TO N.
               END DECLARATIVES.
               MAIN-SEC SECTION.
               MAIN-P.
                   PERFORM D-P1 THRU MAIN-P2
                   STOP RUN.
               MAIN-P2.
                   ADD 10 TO N.
        """;

    private const string TwoDeclarativeSections = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB433DRIFTTWOSECS.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT F1 ASSIGN TO "pb433-drift-2.dat"
                       ORGANIZATION IS SEQUENTIAL.
                   SELECT F2 ASSIGN TO "pb433-drift-3.dat"
                       ORGANIZATION IS SEQUENTIAL.
               DATA DIVISION.
               FILE SECTION.
               FD F1.
               01 F1-REC PIC X(10).
               FD F2.
               01 F2-REC PIC X(10).
               WORKING-STORAGE SECTION.
               01 N PIC 9(4) VALUE 0.
               PROCEDURE DIVISION.
               DECLARATIVES.
               D-SEC1 SECTION.
                   USE AFTER STANDARD ERROR PROCEDURE ON F1.
               D-P1.
                   ADD 1 TO N.
               D-SEC2 SECTION.
                   USE AFTER STANDARD ERROR PROCEDURE ON F2.
               D-P2.
                   ADD 10 TO N.
               END DECLARATIVES.
               MAIN-SEC SECTION.
               MAIN-P.
                   PERFORM D-P1 THRU D-P2
                   STOP RUN.
        """;

    private const string Legal = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB433DRIFTLEGAL.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT F1 ASSIGN TO "pb433-drift-4.dat"
                       ORGANIZATION IS SEQUENTIAL.
               DATA DIVISION.
               FILE SECTION.
               FD F1.
               01 F1-REC PIC X(10).
               WORKING-STORAGE SECTION.
               01 N PIC 9(4) VALUE 0.
               PROCEDURE DIVISION.
               DECLARATIVES.
               D-SEC SECTION.
                   USE AFTER STANDARD ERROR PROCEDURE ON F1.
               D-P1.
                   ADD 1 TO N.
               D-P2.
                   ADD 10 TO N.
               END DECLARATIVES.
               MAIN-SEC SECTION.
               MAIN-P.
                   PERFORM D-P1 THRU D-P2
                   PERFORM D-SEC
                   PERFORM MAIN-A THRU MAIN-B
                   STOP RUN.
               MAIN-A.
                   ADD 5 TO N.
               MAIN-B.
                   ADD 50 TO N.
        """;

    /// <summary>Bind one fixture at <paramref name="level"/> and return the BIND diagnostics — SR11 is a
    /// syntax rule decided in the binder, so the parse must succeed for the test to mean anything.</summary>
    private static IReadOnlyList<string> BindDiagnostics(string source, int level)
    {
        string path = Path.Combine(Path.GetTempPath(), $"pb433drift_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = level }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var edition = new EditionContext(level);
            new CSharpEmitter().Bind(tree!, edition);
            return edition.Diagnostics;
        }
        finally { try { File.Delete(path); } catch (IOException) { /* best-effort */ } }
    }
}
