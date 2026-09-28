// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Binding;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Runtime.IO;
using CobolNet.Tests.Shared;
using Xunit;
using CobolNet.Frontend.Preprocessor;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The GLOBAL clause on a report description entry (ISO §13.18.27.3 SR1 e), §13.18.27.4 GR1/GR2) and the Format-2
/// half of §14.9.49.4 GR4 — the USE BEFORE REPORTING declarative is selected per STATEMENT, from the source element
/// containing the GENERATE / TERMINATE and then from the GLOBAL declaratives of its containers (kb/Work PB369). The
/// run-time selection is witnessed by conformance:85/pb369_global_report_use_selection; this class pins the bind
/// rules and the shape the emitter gives the selection.
/// </summary>
public sealed class GlobalReportScopeTests
{
    /// <summary>A two-level compilation group: PB369UA declares R-1 (GLOBAL when <paramref name="rdGlobal"/>) on
    /// FD RPT (GLOBAL when <paramref name="fdGlobal"/>); PB369UB is contained in it and runs
    /// <paramref name="innerBody"/> after its own <paramref name="innerDecls"/>.</summary>
    private static string Group(bool rdGlobal, bool fdGlobal, string outerDecls, string innerDecls, string innerBody,
        string innerData = "") => $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB369UA.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT RPT ASSIGN TO "pb369ua.rpt".
               DATA DIVISION.
               FILE SECTION.
               FD RPT{(fdGlobal ? " GLOBAL" : "")} REPORT IS R-1.
               WORKING-STORAGE SECTION.
               01 WS-T PIC 99 VALUE 0 GLOBAL.
               REPORT SECTION.
               RD R-1{(rdGlobal ? " IS GLOBAL" : "")} CONTROL IS FINAL PAGE LIMIT IS 20 LINES.
               01 DET-1 TYPE DE LINE PLUS 1.
                  02 COLUMN 1 PIC XX VALUE "DE".
               01 TOT TYPE CF FINAL LINE PLUS 1.
                  02 TOT-N COLUMN 1 PIC 99 SUM WS-T.
               PROCEDURE DIVISION.
               {(outerDecls.Length == 0 ? "" : "DECLARATIVES.\n" + outerDecls + "\n       END DECLARATIVES.\n")}
               A-MAIN SECTION.
               A-1.
                   OPEN OUTPUT RPT.
                   INITIATE R-1.
                   CALL "PB369UB".
                   CLOSE RPT.
                   STOP RUN.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB369UB.
               {(innerData.Length == 0 ? "" : "DATA DIVISION.\n       WORKING-STORAGE SECTION.\n" + innerData)}
               PROCEDURE DIVISION.
               {(innerDecls.Length == 0 ? "" : "DECLARATIVES.\n" + innerDecls + "\n       END DECLARATIVES.\n")}
               B-MAIN SECTION.
               B-1.
               {innerBody}
                   EXIT PROGRAM.
               END PROGRAM PB369UB.
               END PROGRAM PB369UA.
        """;

    // ── §14.9.16.3 SR3/SR4 · §14.9.21.3 SR2 · §14.9.46.3 SR2 — the report's FD must be GLOBAL too ──────────────

    [Theory]
    [InlineData("GENERATE DET-1.", "§14.9.16.3 SR3")]
    [InlineData("GENERATE R-1.", "§14.9.16.3 SR4")]
    [InlineData("INITIATE R-1.", "§14.9.21.3 SR2")]
    [InlineData("TERMINATE R-1.", "§14.9.46.3 SR2")]
    public void AContainedReportVerbOnAGlobalReportWhoseFileIsNotGlobal_IsRefused(string stmt, string rule)
    {
        var d = BindDiagnostics(Group(rdGlobal: true, fdGlobal: false, "", "", "           " + stmt), 85);
        Assert.Single(d, x => x.Contains("COBOLNET2392") && x.Contains(rule));
    }

    [Theory]
    [InlineData("GENERATE DET-1.")]
    [InlineData("GENERATE R-1.")]
    [InlineData("INITIATE R-1.")]
    [InlineData("TERMINATE R-1.")]
    public void AContainedReportVerbOnAGlobalReportWithAGlobalFile_Binds(string stmt)
    {
        var d = BindDiagnostics(Group(rdGlobal: true, fdGlobal: true, "", "", "           " + stmt), 85);
        Assert.DoesNotContain(d, x => x.Contains("error"));
    }

    /// <summary>§13.18.27.4 GR2 — a report WITHOUT the GLOBAL clause is not a name the contained program can
    /// reference, whatever its file says.</summary>
    [Theory]
    [InlineData("GENERATE DET-1.")]
    [InlineData("INITIATE R-1.")]
    public void AContainedReportVerbOnANonGlobalReport_DoesNotResolve(string stmt)
    {
        var d = BindDiagnostics(Group(rdGlobal: false, fdGlobal: true, "", "", "           " + stmt), 85);
        Assert.Contains(d, x => x.Contains("COBOLNET1757"));
    }

    /// <summary>§13.18.27.4 GR1 — the sum counter TOT-N is a data-name subordinate to the global report, so a
    /// contained program reads it (through the declaring program's engine); a LOCAL declaration of the same name
    /// hides it (§8.4.6).</summary>
    [Fact]
    public void AGlobalReportsSumCounter_IsVisibleInAContainedProgram_UnlessHidden()
    {
        string visible = Emit(Group(true, true, "", "", "           MOVE TOT-N TO WS-T."));
        Assert.Contains("__outer.__RPT_", Inner(visible));
        Assert.Contains(".SumValue(", Inner(visible));

        string hidden = Emit(Group(true, true, "", "", "           MOVE TOT-N TO WS-T.", "       01 TOT-N PIC 99 VALUE 7.\n"));
        Assert.DoesNotContain(".SumValue(", Inner(hidden));
    }

    /// <summary>§8.4.6.2.1 rule 3 a) — "If the name is declared in source element B, the item in source element B
    /// is the referenced item": a contained program's OWN report group DET-1 and its own report's PAGE-COUNTER are
    /// referenced, not the same-named group and counter of the container's GLOBAL report — no ambiguity, and the
    /// statement drives the contained program's own engine.</summary>
    [Fact]
    public void ALocalReportHidesTheContainersGlobalReportOfTheSameGroupName()
    {
        string cs = Emit("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB369UA.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT RPT ASSIGN TO "pb369ua.rpt".
                   DATA DIVISION.
                   FILE SECTION.
                   FD RPT GLOBAL REPORT IS R-1.
                   WORKING-STORAGE SECTION.
                   01 WS-T PIC 99 VALUE 0 GLOBAL.
                   REPORT SECTION.
                   RD R-1 IS GLOBAL PAGE LIMIT IS 20 LINES.
                   01 DET-1 TYPE DE LINE PLUS 1.
                      02 COLUMN 1 PIC XX VALUE "DE".
                   PROCEDURE DIVISION.
                   A-1.
                       CALL "PB369UB".
                       STOP RUN.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB369UB.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT RPT2 ASSIGN TO "pb369ub.rpt".
                   DATA DIVISION.
                   FILE SECTION.
                   FD RPT2 REPORT IS R-2.
                   REPORT SECTION.
                   RD R-2 PAGE LIMIT IS 20 LINES.
                   01 DET-1 TYPE DE LINE PLUS 1.
                      02 COLUMN 1 PIC XX VALUE "LO".
                   PROCEDURE DIVISION.
                   B-1.
                       GENERATE DET-1.
                       MOVE PAGE-COUNTER TO WS-T.
                       EXIT PROGRAM.
                   END PROGRAM PB369UB.
                   END PROGRAM PB369UA.
            """);
        string inner = Inner(cs);
        Assert.Contains("__RPT_0.Generate(\"DET-1\");", inner);
        Assert.DoesNotContain("__outer.__RPT_", inner);
    }

    // ── §14.9.49.4 GR4, Format 2 — the selection travels with the statement ─────────────────────────────────

    /// <summary>GR4 b) admits only a GLOBAL declarative of a container, so a container's NON-global USE BEFORE
    /// REPORTING gives a contained GENERATE no selector at all; the container's own GENERATE keeps one (GR4 a)).</summary>
    [Fact]
    public void AContainersNonGlobalDeclarative_IsNotSelectedForAContainedGenerate()
    {
        string cs = Emit(Group(true, true, """
                   A-D1 SECTION.
                       USE BEFORE REPORTING DET-1.
                   A-D1-P.
                       DISPLAY "A".
            """, "", "           GENERATE DET-1."));
        Assert.Contains("__outer.__RPT_0.Generate(\"DET-1\");", Inner(cs));
        Assert.Matches(new Regex(@"case 0: if \(!__globalOnly\)"), Outer(cs));
    }

    /// <summary>GR4 b) — a GLOBAL declarative of the container qualifies: the contained program's selector has no
    /// case of its own and walks outward; the container's case runs whatever the caller's tier.</summary>
    [Fact]
    public void AContainersGlobalDeclarative_IsReachedByTheOutwardWalk()
    {
        string cs = Emit(Group(true, true, """
                   A-D1 SECTION.
                       USE GLOBAL BEFORE REPORTING DET-1.
                   A-D1-P.
                       DISPLAY "A".
            """, "", "           GENERATE DET-1."));
        string inner = Inner(cs);
        Assert.Contains("__BeforeReporting_", inner);
        Assert.Matches(new Regex(@"return __outer\.__BeforeReporting_\d+\(__gi, true\);"), inner);
        Assert.Matches(new Regex(@"__outer\.__RPT_0\.Generate\(""DET-1"", __brSel_\d+ \?\?= "), inner);
        Assert.Matches(new Regex(@"case 0: __RunUse\(0, "), Outer(cs));
    }

    /// <summary>A USE BEFORE REPORTING procedure of the contained program may name a group of the container's
    /// GLOBAL report (§14.9.49.3 SR9 — identifier-1 references a report group, and GR2 makes it referenceable), and
    /// its SUPPRESS statement drives the DECLARING program's engine (§14.9.45).</summary>
    [Fact]
    public void AContainedDeclarativeOnAGlobalGroup_SuppressesTheContainersEngine()
    {
        string cs = Emit(Group(true, true, "", """
                   B-D1 SECTION.
                       USE BEFORE REPORTING DET-1.
                   B-D1-P.
                       SUPPRESS PRINTING.
            """, "           GENERATE DET-1."));
        string inner = Inner(cs);
        // The engine is told WHICH group — DET-1, ordinal 0, the same key the selector's `case 0` switches on —
        // because §14.9.45.4 GR1 inhibits only the group the USE procedure names (kb/Work PB1186).
        Assert.Contains("__outer.__RPT_0.SuppressPrinting(0);", inner);
        Assert.Matches(new Regex(@"case 0: if \(!__globalOnly\) \{ __RunUse\(0, "), inner);
        Assert.Contains("return false;", inner);   // no GLOBAL declarative outward — the walk ends here
    }

    /// <summary>⛔ There is no per-group hook to install: the engine's only BEFORE REPORTING input is the selector a
    /// GENERATE / TERMINATE passes (the shape that makes GR4's per-statement selection expressible at all).</summary>
    [Fact]
    public void TheReportEngine_HasNoPerGroupHook()
    {
        Assert.Null(typeof(ReportGroup).GetProperty("BeforeReporting"));
        Assert.Contains(typeof(CobolReport).GetMethod(nameof(CobolReport.Generate))!.GetParameters(),
            p => p.ParameterType == typeof(Func<int, bool>));
        Assert.Contains(typeof(CobolReport).GetMethod(nameof(CobolReport.Terminate))!.GetParameters(),
            p => p.ParameterType == typeof(Func<int, bool>));
    }

    /// <summary>⛔ DRIFT: every procedure-division NAME resolution of a report reads <c>DataBinder.VisibleReports</c>
    /// (own + inherited GLOBAL). <c>DataBinder.Reports</c> — the reports a unit DECLARES — is read only where the
    /// unit acts as the report's OWNER: constructing, validating, or binding its description. A new reader of
    /// <c>Reports</c> in the procedure binder or the reference resolver would silently make a contained program
    /// blind to its container's GLOBAL reports again.</summary>
    [Fact]
    public void ProcedureBinding_ResolvesReportsOnlyThroughVisibleReports()
    {
        string[] roots = [TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure"),
                          TestRepo.Src("Cobol.Net.Compiler", "Binding", "Bound"),
                          TestRepo.Src("Cobol.Net.Compiler", "Binding", "ReferenceResolver.cs")];
        var owners = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            // BindReportGroupClauses binds the DECLARING unit's PRESENT WHEN / SOURCE / SUM expressions.
            ["ReportWriterBinder.cs"] = 1,
            // The guard that calls BindReportGroupClauses.
            ["StatementBinder.cs"] = 1,
        };
        var found = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (string root in roots)
            foreach (string file in File.Exists(root) ? [root] : Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                int n = File.ReadLines(file)
                    .Count(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)
                                && Regex.IsMatch(l, @"\bData\.Reports\b"));
                if (n > 0) found[Path.GetFileName(file)] = n;
            }
        Assert.Equal(owners.OrderBy(k => k.Key), found.OrderBy(k => k.Key));
    }

    // ── helpers ──────────────────────────────────────────────────────────────────────────────────────────────

    private static string Outer(string cs) => cs[..cs.IndexOf("class _PRG_PB369UB", StringComparison.Ordinal)];

    private static string Inner(string cs) => cs[cs.IndexOf("class _PRG_PB369UB", StringComparison.Ordinal)..];

    private static IReadOnlyList<string> BindDiagnostics(string source, int level)
    {
        string path = Path.Combine(Path.GetTempPath(), $"pb369_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = level }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var edition = new EditionContext(level);
            new CSharpEmitter().Bind(tree!, edition);
            return [.. edition.Diagnostics];
        }
        finally { try { File.Delete(path); } catch (IOException) { /* best-effort */ } }
    }

    private static string Emit(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), $"pb369_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 85 };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var emitter = new CSharpEmitter();
            var edition = new EditionContext(85);
            var bound = emitter.Bind(tree!, edition, frontend.Directives);
            Assert.False(edition.HasErrors, string.Join("\n", edition.Diagnostics));
            return emitter.EmitBound(bound);
        }
        finally { try { File.Delete(path); } catch (IOException) { /* best-effort */ } }
    }
}
