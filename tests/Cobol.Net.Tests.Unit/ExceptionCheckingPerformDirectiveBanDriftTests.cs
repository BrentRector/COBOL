// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Frontend.Preprocessor;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// kb/Work PB595 — <b>ONE lexical-containment predicate for the three directive bans</b>, and the warning owner
/// decision D20 (2026-07-19) specified:
/// <list type="bullet">
/// <item>ISO §7.3.25.3 SR5 — "A TURN directive shall not be specified within an exception processing PERFORM statement."</item>
/// <item>ISO §7.3.22.3 SR4 — "The PUSH directive shall not be specified within an exception checking PERFORM statement."</item>
/// <item>ISO §7.3.20.3 SR4 — "The POP directive shall not be specified within an exception checking PERFORM statement."</item>
/// </list>
///
/// <para><b>Why a warning and not a rejection.</b> D20: a FLAT ban (the whole statement, imperative-statement-1
/// included — Annex D.16.4 uses "exception-processing PERFORM statement" for the PUSH/POP ban that is stated
/// normatively as "exception checking", so the two phrasings are drafting synonyms), reported as a SUPPRESSIBLE
/// conformance warning; §4.2.2 requires for a syntax-rule violation only "a warning mechanism that optionally may
/// be invoked by the user at compile time", so the program still compiles and still runs.</para>
///
/// <para><b>What these pins hold.</b> A directive is not in the parse tree — it is removed by the preprocessor —
/// so containment is a question about POSITIONS, answered from the sites recorded on the FINAL text. The pins
/// below hold all THREE words against the ONE predicate, hold the controls that make the probe able to fail (the
/// same directives written before the PERFORM and after END-PERFORM), and hold the table/scanner agreement so a
/// fourth position-ruled directive is one row in two places and no new mechanism.</para>
/// </summary>
public sealed class ExceptionCheckingPerformDirectiveBanDriftTests
{
    /// <summary>The TURN ban — the row PB595 opened. The warning names §7.3.25.3 SR5 and quotes it, and the
    /// compile SUCCEEDS: a rejection here would be D20's explicitly rejected treatment.</summary>
    [Fact]
    public void ATurnInsideAnExceptionCheckingPerform_WarnsCitingSr5_AndStillCompiles()
    {
        var edition = Bind(Inside(">>TURN EC-OVERFLOW-STRING CHECKING ON", "PB595TURNIN"));
        Assert.Empty(edition.Diagnostics);
        Assert.Contains(edition.Warnings, w =>
            w.Contains("COBOLNET2187") && w.Contains("§7.3.25.3 SR5")
            && w.Contains("A TURN directive shall not be specified within an exception processing PERFORM statement"));
    }

    /// <summary>The PUSH and POP bans — the two D20 required the SAME predicate to decide. Each quotes its OWN
    /// clause and its own words: they are three syntax rules, not one rule with three spellings.</summary>
    [Theory]
    [InlineData(">>PUSH ALL", "§7.3.22.3 SR4", "The PUSH directive shall not be specified")]
    [InlineData(">>POP ALL", "§7.3.20.3 SR4", "The POP directive shall not be specified")]
    public void APushOrPopInsideAnExceptionCheckingPerform_WarnsCitingItsOwnRule(
        string directive, string clause, string quoted)
    {
        var edition = Bind(Inside(directive, "PB595PUSHPOP"));
        Assert.Empty(edition.Diagnostics);
        Assert.Contains(edition.Warnings,
            w => w.Contains("COBOLNET2187") && w.Contains(clause) && w.Contains(quoted));
    }

    /// <summary>THE CONTROL, and it is what makes the three above evidence: the SAME directives written OUTSIDE
    /// the statement — before the PERFORM and after END-PERFORM — are legal and draw nothing. Without it, a
    /// predicate that warned on every TURN in the program would pass every test above.</summary>
    [Fact]
    public void TheSameDirectivesOutsideTheStatement_DrawNothing()
    {
        var edition = Bind("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB595OUTSIDE.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 WS-D PIC X(3) VALUE SPACES.
                   PROCEDURE DIVISION.
                   MAIN-P.
                   >>TURN EC-OVERFLOW-STRING CHECKING ON
                   >>PUSH ALL
                       PERFORM
                           STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
                       WHEN EC-OVERFLOW-STRING
                           CONTINUE
                       END-PERFORM
                   >>POP ALL
                   >>TURN EC-OVERFLOW-STRING CHECKING OFF
                       STOP RUN.
            """);
        Assert.Empty(edition.Diagnostics);
        Assert.DoesNotContain(edition.Warnings, w => w.Contains("COBOLNET2187"));
    }

    /// <summary>NESTED Format-3 PERFORMs contain the same directive line, and each one's bind asks the same
    /// question of it. The rule is about the DIRECTIVE, so the answer is ONE warning — not one per enclosing
    /// statement.</summary>
    [Fact]
    public void ADirectiveInsideNestedExceptionCheckingPerforms_IsReportedOnce()
    {
        var edition = Bind("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB595NESTED.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 WS-D PIC X(3) VALUE SPACES.
                   PROCEDURE DIVISION.
                   MAIN-P.
                       PERFORM
                           PERFORM
                   >>TURN EC-OVERFLOW-STRING CHECKING ON
                               STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
                           WHEN EC-OVERFLOW-STRING
                               CONTINUE
                           END-PERFORM
                       WHEN EC-USER-DEMO
                           CONTINUE
                       END-PERFORM
                       STOP RUN.
            """);
        Assert.Empty(edition.Diagnostics);
        Assert.Single(edition.Warnings, w => w.Contains("COBOLNET2187"));
    }

    /// <summary>A banned directive inside an OMITTED conditional-compilation branch draws nothing: it is not
    /// compiled at all (ISO §7.3.11 — the omitted lines are removed from the compilation group), so there is no
    /// directive for §7.3.22.3 SR4 to be about. This is a property of WHERE the sites are recorded — after the
    /// conditional-compilation driver has already blanked the omitted lines — and it would be lost by a scanner
    /// that read the raw source instead.</summary>
    [Fact]
    public void ABannedDirectiveInAnOmittedBranch_DrawsNothing()
    {
        var edition = Bind("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB595OMITTED.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 WS-D PIC X(3) VALUE SPACES.
                   PROCEDURE DIVISION.
                   MAIN-P.
                       PERFORM
                   >>IF NEVER-DEFINED DEFINED
                   >>PUSH ALL
                   >>END-IF
                           STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
                       WHEN EC-OVERFLOW-STRING
                           CONTINUE
                       END-PERFORM
                       STOP RUN.
            """);
        Assert.Empty(edition.Diagnostics);
        Assert.DoesNotContain(edition.Warnings, w => w.Contains("COBOLNET2187"));
    }

    /// <summary>The scanner and the ban table name the SAME three words. A fourth position-ruled directive is one
    /// entry in <see cref="DirectiveSiteProcessor.PositionRuled"/> plus one row in the binder's table — and if
    /// only one of the two is written, this fails rather than silently skipping the new directive.</summary>
    [Fact]
    public void TheSiteScannerAndTheBanTable_NameTheSameDirectives()
    {
        string binder = File.ReadAllText(CobolNet.Tests.Shared.TestRepo.Src(
            "Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "EcBinder.ExceptionPerform.cs"));
        foreach (string word in DirectiveSiteProcessor.PositionRuled)
            Assert.Contains($"(\"{word}\", \"§7.3.", binder);
        Assert.Equal(3, DirectiveSiteProcessor.PositionRuled.Count);
    }

    /// <summary>A Format-3 PERFORM with <paramref name="directive"/> written inside imperative-statement-1.</summary>
    private static string Inside(string directive, string programId) => $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. {programId}.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 WS-D PIC X(3) VALUE SPACES.
               PROCEDURE DIVISION.
               MAIN-P.
                   PERFORM
        {directive}
                       STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-D
                   WHEN EC-OVERFLOW-STRING
                       CONTINUE
                   END-PERFORM
                   STOP RUN.
        """;

    /// <summary>Bind one fixture at COBOL-2023 (Format 3 is 2023-only, Annex E.3.3 item 36) and return the
    /// edition context, so BOTH channels — the failing diagnostics and the warnings — can be read.
    /// ⛔ The directive results MUST ride along: the ban is decided from the recorded directive SITES, and
    /// without them every assertion here would pass for the wrong reason.</summary>
    private static EditionContext Bind(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), $"pb595ban_{Guid.NewGuid():N}.cob");
        File.WriteAllText(path, source);
        try
        {
            var diags = new DiagnosticBag();
            var frontend = new CnFrontend { InitialFormat = InitialReferenceFormat.Auto, DialectLevel = 2023 };
            var tree = frontend.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            var edition = new EditionContext(2023);
            new CSharpEmitter().Bind(tree!, edition, frontend.Directives);
            return edition;
        }
        finally { try { File.Delete(path); } catch (IOException) { /* best-effort */ } }
    }
}
