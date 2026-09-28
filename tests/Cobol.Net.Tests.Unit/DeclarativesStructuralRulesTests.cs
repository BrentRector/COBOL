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
/// The declaratives' STRUCTURAL syntax rules — ISO §14.9.49.3 SR1 (placement of USE), SR3/SR4 (the reference
/// boundary between the declaratives portion and the rest), SR10/SR11 (the two restrictions on a USE BEFORE
/// REPORTING procedure). kb/Work PB361, PB362, PB363: each compiled clean at every <c>--std</c> before, the
/// misplaced USE then aborting the run unit as a "not implemented" feature.
///
/// <para>⛔ SR3 and SR4 are a TWO-ARM rule whose arms point in OPPOSITE directions (SR3 restricts references OUT of
/// a declarative, exempting RESUME; SR4 restricts references INTO a declarative section, exempting PERFORM), so
/// every arm is pinned from both sides, with the exempt verb as the control.</para>
/// </summary>
public sealed class DeclarativesStructuralRulesTests
{
    // ── SR1 — a USE statement anywhere but the first sentence of a declarative section (PB361) ────────────────

    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void AUseStatementInANondeclarativeParagraph_IsRefused(int level)
    {
        var d = BindDiagnostics(FileProgram("SR1A", "", """
                   M1.
                       STOP RUN.
                   M2.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
            """), level);
        Assert.Contains(d, x => x.Contains("COBOLNET2377") && x.Contains("nondeclarative portion"));
        Assert.DoesNotContain(d, x => x.Contains("COBOLNET1756"));   // never again "not implemented"
    }

    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void ASecondUseStatementInsideADeclarativeParagraph_IsRefused(int level)
    {
        var d = BindDiagnostics(FileProgram("SR1B", """
                   D1 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
                   DP1.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
            """, """
                   M1.
                       STOP RUN.
            """), level);
        Assert.Single(d, x => x.Contains("COBOLNET2377") && x.Contains("inside a declarative section"));
    }

    // ── SR4 — INTO a declarative section, PERFORM only (PB362) ──────────────────────────────────────────────

    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AGoToIntoADeclarativeFromTheNondeclarativePortion_IsRefused(int level)
    {
        var d = BindDiagnostics(FileProgram("SR4A", Decl1, """
                   M1.
                       GO TO DP1.
            """), level);
        Assert.Contains(d, x => x.Contains("COBOLNET2376") && x.Contains("nondeclarative portion"));
    }

    [Fact]
    public void AGoToIntoADifferentDeclarativeSection_IsRefused()
    {
        var d = BindDiagnostics(FileProgram("SR4B", Decl1 + Environment.NewLine + """
                   D2 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
                   DP2.
                       GO TO DP1.
            """, """
                   M1.
                       STOP RUN.
            """), 2023);
        Assert.Contains(d, x => x.Contains("COBOLNET2376") && x.Contains("different declarative section, 'D2'"));
    }

    [Fact]
    public void AnAlterOfAndToADeclarativeParagraph_IsRefused()
    {
        var d = BindDiagnostics(FileProgram("SR4C", """
                   D1 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
                   DP1.
                       GO TO DP2.
                   DP2.
                       EXIT.
            """, """
                   M1.
                       ALTER DP1 TO PROCEED TO DP2.
                       STOP RUN.
            """), 85);
        Assert.Contains(d, x => x.Contains("COBOLNET2376") && x.Contains("ALTER 'DP1'"));
    }

    [Fact]
    public void AnAlterToProceedToADeclarativeParagraph_IsRefused()
    {
        var d = BindDiagnostics(FileProgram("SR4E", Decl1, """
                   M1.
                       ALTER M2 TO PROCEED TO DP1.
                       STOP RUN.
                   M2.
                       GO TO M1.
            """), 85);
        Assert.Contains(d, x => x.Contains("COBOLNET2376") && x.Contains("ALTER TO PROCEED TO 'DP1'"));
    }

    [Fact]
    public void ASortInputProcedureNamingADeclarativeSection_IsRefused()
    {
        var d = BindDiagnostics($$"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. SR4D.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT F1 ASSIGN TO "sr4d-f1.dat".
                       SELECT SW ASSIGN TO "sr4d-sw.dat".
                   DATA DIVISION.
                   FILE SECTION.
                   FD  F1.
                   01  F1-REC PIC X(10).
                   SD  SW.
                   01  SW-REC PIC X(10).
                   PROCEDURE DIVISION.
                   DECLARATIVES.
            {{Decl1}}
                   END DECLARATIVES.
                   MAIN SECTION.
                   M1.
                       SORT SW ON ASCENDING KEY SW-REC
                           INPUT PROCEDURE IS D1
                           GIVING F1.
                       STOP RUN.
            """, 2023);
        Assert.Contains(d, x => x.Contains("COBOLNET2376") && x.Contains("SORT"));
    }

    /// <summary>The SR4 control: PERFORM is the exemption, from the nondeclarative portion AND from a different
    /// declarative section; and within its OWN section any statement may name a declarative paragraph.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void PerformIntoADeclarative_AndAnyReferenceWithinTheSameSection_AreAccepted(int level)
    {
        var d = BindDiagnostics(FileProgram("SR4OK", """
                   D1 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
                   DP1.
                       GO TO DP2.
                   DP2.
                       ADD 1 TO N.
                   D2 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
                   DP3.
                       PERFORM DP2.
            """, """
                   M1.
                       PERFORM DP1 THRU DP2.
                       PERFORM D1.
                       STOP RUN.
            """), level);
        Assert.True(d.Count == 0, string.Join("\n", d));
    }

    // ── SR3 — OUT of a declarative, RESUME only (PB362) — a WARNING by determination ─────────────────────────

    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void APerformOrGoToOutOfADeclarative_IsWarned_AndCompiles(int level)
    {
        var d = BindDiagnostics(FileProgram("SR3A", """
                   D1 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
                   DP1.
                       PERFORM NONDECL-P.
                       GO TO NONDECL-P.
            """, """
                   M1.
                       STOP RUN.
                   NONDECL-P.
                       ADD 1 TO N.
            """), level);
        Assert.Equal(2, d.Count(x => x.Contains("COBOLNET2375") && x.Contains("§14.9.49.3 SR3")));
        Assert.DoesNotContain(d, x => x.Contains("COBOLNET2376"));
    }

    /// <summary>The SR3 control: RESUME AT is the exemption (a COBOL-2002 statement).</summary>
    [Fact]
    public void AResumeAtANondeclarativeProcedure_IsNotWarned()
    {
        var d = BindDiagnostics(FileProgram("SR3OK", Decl1Resume, """
                   M1.
                       STOP RUN.
                   NONDECL-P.
                       ADD 1 TO N.
            """), 2002);
        Assert.DoesNotContain(d, x => x.Contains("COBOLNET2375") || x.Contains("COBOLNET2376"));
    }

    // ── SR10 / SR11 — USE BEFORE REPORTING (PB363) ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("GENERATE DET-A")]
    [InlineData("INITIATE R-1")]
    [InlineData("TERMINATE R-1")]
    public void AReportWriterVerbInABeforeReportingProcedure_IsRefused(string statement)
    {
        var d = BindDiagnostics(ReportProgram(statement), 2023);
        Assert.Single(d, x => x.Contains("COBOLNET2378") && x.Contains("§14.9.49.3 SR10"));
    }

    [Theory]
    [InlineData("MOVE 9 TO WS-GRP")]              // the control data item itself
    [InlineData("ADD 1 TO WS-GRP-LO")]            // read-modify-write of a subordinate item
    [InlineData("MOVE SPACES TO WS-CTL-REC")]     // a group containing it
    [InlineData("MOVE 1 TO WS-GRP-LO")]           // an item subordinate to it
    [InlineData("INITIALIZE WS-CTL-REC")]
    [InlineData("IF WS-N = 0 MOVE 9 TO WS-GRP END-IF")]   // nested: one diagnostic, not two
    public void AStoreIntoAControlDataItem_IsRefused(string statement)
    {
        var d = BindDiagnostics(ReportProgram(statement), 2023);
        Assert.Single(d, x => x.Contains("COBOLNET2378") && x.Contains("§14.9.49.3 SR11"));
    }

    /// <summary>The SR11 control: stores into other items conform, and a BY REFERENCE CALL argument is not an
    /// alteration the source can be convicted of (the callee may not store).</summary>
    [Theory]
    [InlineData("MOVE 7 TO WS-N")]
    [InlineData("ADD 1 TO WS-N")]
    [InlineData("CALL \"SOMEPROG\" USING BY REFERENCE WS-GRP")]
    [InlineData("DISPLAY WS-GRP")]
    public void OtherStatementsInABeforeReportingProcedure_AreAccepted(string statement)
    {
        var d = BindDiagnostics(ReportProgram(statement), 2023);
        Assert.DoesNotContain(d, x => x.Contains("COBOLNET2378"));
    }

    // ── The structure that keeps the next case automatic ──────────────────────────────────────────────────

    /// <summary>Only PERFORM and RESUME name an exemption; every other caller of the ONE procedure-name funnel
    /// takes the restricted default, so a new verb inherits SR3 and SR4 without being taught them.</summary>
    [Fact]
    public void OnlyPerformAndResume_ClaimAnExemptionFromTheDeclarativesBoundary()
    {
        string root = TestRepo.Src("Cobol.Net.Compiler", "Binding");
        var claims = new List<string>();
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            foreach (Match m in Regex.Matches(File.ReadAllText(file), @"(?:,|kind:)\s*ProcedureReferenceKind\.(Perform|Resume)\)"))
                claims.Add($"{Path.GetFileName(file)}:{m.Groups[1].Value}");
        Assert.Equal(new[] { "ControlFlowBinder.cs:Perform", "ControlFlowBinder.cs:Perform", "EcBinder.cs:Resume" },
            claims.Order().ToArray());
    }

    /// <summary>The three SR10 verbs read the ONE guard.</summary>
    [Fact]
    public void GenerateInitiateAndTerminate_ReadTheOneSr10Guard()
    {
        string rw = File.ReadAllText(
            TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "ReportWriterBinder.cs"));
        foreach (string verb in new[] { "INITIATE", "GENERATE", "TERMINATE" })
            Assert.Contains($"RejectInBeforeReporting(\"{verb}\")", rw);
    }

    // ── Fixtures ────────────────────────────────────────────────────────────────────────────────────────────

    private const string Decl1 = """
                   D1 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
                   DP1.
                       ADD 1 TO N.
            """;

    private const string Decl1Resume = """
                   D1 SECTION.
                       USE AFTER STANDARD ERROR PROCEDURE ON F1.
                   DP1.
                       RESUME AT NONDECL-P.
            """;

    /// <summary>A program with one sequential file F1 and an item N; <paramref name="declaratives"/> is empty for
    /// a program with no DECLARATIVES.</summary>
    private static string FileProgram(string id, string declaratives, string body) => $$"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. {{id}}.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT F1 ASSIGN TO "declstruct-f1.dat"
                           ORGANIZATION IS SEQUENTIAL.
                   DATA DIVISION.
                   FILE SECTION.
                   FD  F1.
                   01  F1-REC PIC X(10).
                   WORKING-STORAGE SECTION.
                   01  N PIC 9(4) VALUE 0.
                   PROCEDURE DIVISION.
            {{(declaratives.Length == 0 ? "" : "       DECLARATIVES.\n" + declaratives + "\n       END DECLARATIVES.\n       MAIN SECTION.")}}
            {{body}}
            """;

    /// <summary>A report whose CONTROL is WS-GRP (subordinate to WS-CTL-REC, with WS-GRP-LO subordinate to it) and
    /// a USE BEFORE REPORTING procedure whose one paragraph holds <paramref name="statement"/>.</summary>
    private static string ReportProgram(string statement) => $$"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. BRSTRUCT.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT RPT ASSIGN TO "brstruct.rpt".
                   DATA DIVISION.
                   FILE SECTION.
                   FD RPT REPORT IS R-1.
                   WORKING-STORAGE SECTION.
                   01 WS-CTL-REC.
                      02 WS-GRP.
                         03 WS-GRP-HI PIC 9.
                         03 WS-GRP-LO PIC 9.
                   01 WS-N PIC 9 VALUE 0.
                   REPORT SECTION.
                   RD R-1 CONTROL IS WS-GRP
                       PAGE LIMIT IS 20 LINES.
                   01 DET-A TYPE DE LINE PLUS 1.
                      02 COLUMN 1 PIC 9 SOURCE IS WS-N.
                   PROCEDURE DIVISION.
                   DECLARATIVES.
                   BR-SEC SECTION.
                       USE BEFORE REPORTING DET-A.
                   BR-P.
                       {{statement}}.
                   END DECLARATIVES.
                   MAIN SECTION.
                   M1.
                       OPEN OUTPUT RPT.
                       INITIATE R-1.
                       GENERATE DET-A.
                       TERMINATE R-1.
                       CLOSE RPT.
                       STOP RUN.
            """;

    /// <summary>Bind one fixture at <paramref name="level"/> and return the BIND errors AND warnings — every rule here is a
    /// syntax rule decided in the binder, so the parse must succeed for the test to mean anything.</summary>
    private static IReadOnlyList<string> BindDiagnostics(string source, int level)
    {
        string path = Path.Combine(Path.GetTempPath(), $"declstruct_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = level }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var edition = new EditionContext(level);
            new CSharpEmitter().Bind(tree!, edition);
            return [.. edition.Diagnostics, .. edition.Warnings];   // SR3 is a WARNING (D-DECLREF)
        }
        finally { try { File.Delete(path); } catch (IOException) { /* best-effort */ } }
    }
}
