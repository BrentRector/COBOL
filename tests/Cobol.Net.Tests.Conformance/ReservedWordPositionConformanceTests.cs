// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Per-edition enforcement of the screen/report allowlist-band reserved words through the POSITION-AWARE §8.9
/// funnel (P2.8 W2 — the RW104A adversarial review): a band context-keyword token (COLUMN, COL, SCREEN, BIT,
/// DEFAULT, LENGTH, NATIONAL are the table-backed ones) rejects with COBOLNET0901 (ISO §8.3.2.1 rule 1 /
/// §8.3.2.1 r1: "Reserved words shall not be used as user-defined words or system-names") ONLY when it occupies a provable
/// user-word position — the data entry-name slot (§13.16), a paragraph/section definition (§14.4.2/§14.4.3),
/// the SELECT file-name (§12.4.5.1), a program-name site (§11.10.2). KEYWORD occurrences that the permissive
/// grammar binds into optional entry-name slots — the report-group COLUMN clause (§13.18.14), the RW104A
/// false-reject the former blanket token-type exclusion parked — stay unflagged at every edition. Severity
/// routes through the ONE <c>EditionSeverityPolicy</c>: a word an edition ADDED to §8.9 is <c>Removed</c> (error
/// strict, warning permissive — the 0901 band contract), while a word reserved at this edition AND at every older
/// one is <c>NotYetIntroduced</c>, an error on BOTH axes, and the parser's reservation gate therefore holds under
/// <c>--permissive</c> too (kb/Work PB792).
/// </summary>
public sealed class ReservedWordPositionConformanceTests
{
    // ── The RW104A shape: a report-group COLUMN clause is a KEYWORD use, never a user word ─────────────────

    /// <summary>A minimal RW104A-style report program: the report-group <c>COLUMN 27</c> clause keyword
    /// (§13.18.14) parses into the optional report-group entry-name slot under the permissive grammar.</summary>
    private const string ReportColumnProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWPCOL.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT RPT ASSIGN TO "RWPCOLF".
        DATA DIVISION.
        FILE SECTION.
        FD RPT
            REPORT IS R-1.
        REPORT SECTION.
        RD R-1 PAGE LIMIT 60.
        01 DET TYPE DETAIL.
            03 LINE 1.
                05 COLUMN 27 PIC X(4) VALUE "MARK".
                05 COLUMN NUMBER 70 PIC X(5) VALUE "PAGE ".
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN OUTPUT RPT.
            INITIATE R-1.
            GENERATE DET.
            TERMINATE R-1.
            CLOSE RPT.
            STOP RUN.
        """;

    /// <summary>COLUMN is §8.9-reserved at EVERY edition, yet its report-group CLAUSE use is a keyword use —
    /// the position-aware funnel must not flag it anywhere (the RW104A/CCVS-85 no-false-reject guarantee),
    /// on either severity axis.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ReportGroupColumnClause_KeywordUse_NoReservedWordDiagnostic(int edition)
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(ReportColumnProgram, edition);
        Assert.True(ok, $"--std {edition}: {string.Join("; ", errors)}");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0901");
    }

    // ── The data entry-name slot (§13.16) ────────────────────────────────────────────────────────────────────

    /// <summary>A data item NAMED with the band word SCREEN (reserved 2002+ per reserved-words.json).</summary>
    private const string ScreenDataItemProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWPSCR.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 SCREEN PIC X(3) VALUE "ABC".
        PROCEDURE DIVISION.
        MAIN.
            DISPLAY SCREEN.
            STOP RUN.
        """;

    /// <summary>SCREEN was NOT reserved in X3.23-1985 — a data item so named is a conforming 85 program
    /// (the continuity leg of the interval).</summary>
    [Fact]
    public void DataItemNamedScreen_At85_Compiles()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(ScreenDataItemProgram, 85);
        Assert.True(ok, $"--std 85: {string.Join("; ", errors)}");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0901");
    }

    /// <summary>2002+ reserves SCREEN (§8.9 via reserved-words.json row SCREEN): the entry-name use is a
    /// provable user-word position, so strict rejects with 0901 naming the word.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void DataItemNamedScreen_2002Plus_Strict_Rejected0901(int edition)
    {
        var (ok, diags) = EditionHarness.Compile(ScreenDataItemProgram, edition);
        Assert.False(ok, $"--std {edition} strict must reject a data item named with a reserved word");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0901");
        EditionHarness.AssertHasDiagnostic(diags, "'SCREEN'");
    }

    /// <summary>The same compile PERMISSIVE keeps the pre-reservation semantics and downgrades 0901 to a
    /// warning (the §10 #1 migration posture — <c>EditionContext.Removed</c> severity contract).</summary>
    [Fact]
    public void DataItemNamedScreen_2002Permissive_WarnsAndCompiles()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(ScreenDataItemProgram, 2002, permissive: true);
        Assert.True(ok, $"--std 2002 --permissive: {string.Join("; ", errors)}");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET0901");
    }

    /// <summary>COLUMN has been reserved CONTINUOUSLY since 1985 (reserved-words.json row COLUMN) — naming a
    /// data item COLUMN violates §8.3.2.1 rule 1 even at --std 85, though the same word as a report CLAUSE
    /// keyword is fine (the position distinction in one word).</summary>
    [Fact]
    public void DataItemNamedColumn_Rejected0901_EvenAt85()
    {
        var (ok, diags) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWPCDN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 COLUMN PIC 9 VALUE 5.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY COLUMN.
                STOP RUN.
            """, 85);
        Assert.False(ok, "--std 85 strict must reject a data item named COLUMN (reserved since 1985)");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0901");
        EditionHarness.AssertHasDiagnostic(diags, "'COLUMN'");
    }

    // ── Paragraph definitions (§14.4.3) and the SELECT file-name (§12.4.5.1) ────────────────────────────────

    /// <summary>A paragraph NAMED with the band word COL (reserved 2002+).</summary>
    private const string ColParagraphProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWPPAR.
        PROCEDURE DIVISION.
        MAIN.
            PERFORM COL.
            STOP RUN.
        COL.
            DISPLAY "C".
        """;

    /// <summary>COL was not reserved in 1985 — the paragraph name is conforming at 85.</summary>
    [Fact]
    public void ParagraphNamedCol_At85_Compiles()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(ColParagraphProgram, 85);
        Assert.True(ok, $"--std 85: {string.Join("; ", errors)}");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0901");
    }

    /// <summary>2002 reserved COL: the paragraph DEFINITION is a provable user-word position — strict rejects,
    /// permissive warns and runs with the pre-reservation semantics.</summary>
    [Fact]
    public void ParagraphNamedCol_2002_StrictRejects_PermissiveWarns()
    {
        var (ok, diags) = EditionHarness.Compile(ColParagraphProgram, 2002);
        Assert.False(ok, "--std 2002 strict must reject a paragraph named COL");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0901");
        EditionHarness.AssertHasDiagnostic(diags, "'COL'");

        var (okP, errorsP, warningsP) = EditionHarness.CompileFull(ColParagraphProgram, 2002, permissive: true);
        Assert.True(okP, $"--std 2002 --permissive: {string.Join("; ", errorsP)}");
        EditionHarness.AssertHasDiagnostic(warningsP, "COBOLNET0901");
    }

    /// <summary>A file NAMED with the band word DEFAULT (reserved 2002+) in its SELECT clause.</summary>
    private const string DefaultFileProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWPSEL.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT DEFAULT ASSIGN TO "RWPSELF".
        DATA DIVISION.
        FILE SECTION.
        FD DEFAULT.
        01 REC PIC X(5).
        PROCEDURE DIVISION.
        MAIN.
            OPEN OUTPUT DEFAULT.
            MOVE "HELLO" TO REC.
            WRITE REC.
            CLOSE DEFAULT.
            STOP RUN.
        """;

    /// <summary>DEFAULT was not reserved in 1985 — the file-name is conforming at 85.</summary>
    [Fact]
    public void SelectFileNamedDefault_At85_Compiles()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(DefaultFileProgram, 85);
        Assert.True(ok, $"--std 85: {string.Join("; ", errors)}");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0901");
    }

    /// <summary>2002 reserved DEFAULT (the OPTIONS-paragraph family): the SELECT file-name is a provable
    /// user-word position — strict rejects with 0901.</summary>
    [Fact]
    public void SelectFileNamedDefault_2002_Strict_Rejected0901()
    {
        var (ok, diags) = EditionHarness.Compile(DefaultFileProgram, 2002);
        Assert.False(ok, "--std 2002 strict must reject a file named DEFAULT");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0901");
        EditionHarness.AssertHasDiagnostic(diags, "'DEFAULT'");
    }

    /// <summary>The full NIST RW104A program (the review's namesake) still compiles at --std 85 with NO
    /// reserved-word diagnostic — the report COLUMN clause keyword stays a keyword.</summary>
    [Fact]
    public void Rw104a_At85_NoReservedWordDiagnostic()
    {
        var (ok, diags) = EditionHarness.CompileNist("RW104A", 85);
        Assert.True(ok, $"RW104A --std 85: {string.Join("; ", diags)}");
    }

    // ── The W2 adversarial-review coverage additions (DEVLOG 595): the programName and section-name slots
    // end-to-end with a BAND token (the unit facts used IDENTIFIER names, which the funnel checks
    // position-blind anyway). (The non-ISO linkageProcedureParameter arm is deleted, kb/Work PB1252.)

    /// <summary>A program NAMED with a band word: user-definable at 85, 0901 at 2002+ strict (§8.3.2.1 r1;
    /// SCREEN reserved 2002 — the A.4.2 module word).</summary>
    [Fact]
    public void ProgramNamedScreen_At85Compiles_2002Strict0901()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SCREEN.
            PROCEDURE DIVISION.
            MAIN.
                STOP RUN.
            """;
        var (ok85, errors85, warn85) = EditionHarness.CompileFull(source, 85);
        Assert.True(ok85, string.Join("; ", errors85));
        EditionHarness.AssertNoDiagnostic(warn85, "COBOLNET0901");
        var (ok02, diags02) = EditionHarness.Compile(source, 2002);
        Assert.False(ok02, "--std 2002 strict must reject a program named SCREEN");
        EditionHarness.AssertHasDiagnostic(diags02, "COBOLNET0901");
    }

    /// <summary>A SECTION defined with a band word (BIT reserved 2002): 85 clean, 2002 strict 0901.</summary>
    [Fact]
    public void SectionNamedBit_At85Compiles_2002Strict0901()
    {
        const string source = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWPSEC1.
            PROCEDURE DIVISION.
            BIT SECTION.
            MAIN.
                STOP RUN.
            """;
        var (ok85, errors85, warn85) = EditionHarness.CompileFull(source, 85);
        Assert.True(ok85, string.Join("; ", errors85));
        EditionHarness.AssertNoDiagnostic(warn85, "COBOLNET0901");
        var (ok02, diags02) = EditionHarness.Compile(source, 2002);
        Assert.False(ok02, "--std 2002 strict must reject a section named BIT");
        EditionHarness.AssertHasDiagnostic(diags02, "COBOLNET0901");
    }

    // ── The KEYWORD slot that BORROWS cobolWord (kb/Work PB693) ─────────────────────────

    /// <summary>The VALIDATE-STATUS clause's ON phrase (ISO §13.18.62.2). FORMAT, CONTENT and RELATION are
    /// UNDERLINED in the printed general format — KEYWORDS (§5.2.2), not user-defined words — inside a CHOICE
    /// INDICATOR group (§5.2.6.4: one or more, each at most once, any order), so all three may be written.</summary>
    private const string ValidateStatusOnPhraseProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RWPVSON.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-REC.
           05 WS-A PIC X(4).
        01 WS-MSG PIC X(30) VALIDATE-STATUS IS "ERR" WHEN NO ERROR
           ON FORMAT CONTENT RELATION FOR WS-A.
        PROCEDURE DIVISION.
            DISPLAY "UNREACHABLE".
            STOP RUN.
        """;

    /// <summary>⛔ THE POSITION DISTINCTION THE §8.9 RESERVATION GATE MUST RESPECT. FORMAT is §8.9-reserved
    /// from 2002 AND has a lexer token, so the gate removes its <c>cobolWord</c> alternative there; the ON-phrase
    /// slot borrowed <c>cobolWord</c> to match it, which turned this legal clause into a parse error carrying a
    /// FALSE "'FORMAT' … cannot be used as a user-defined word". The word is a KEYWORD here, so the answer is
    /// the declined-facility COBOLNET1708 (Annex A.4.14 — the VALIDATE facility's support is not claimed) and
    /// NO §8.9 diagnostic at all. The <c>declined-validate-status</c> negative case is the corpus twin.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ValidateStatusOnPhrase_FormatIsAKeywordUse_DeclinedNever0901(int edition)
    {
        var (ok, diags) = EditionHarness.Compile(ValidateStatusOnPhraseProgram, edition);
        Assert.False(ok, $"--std {edition} must decline the VALIDATE-STATUS clause");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET1708");
        EditionHarness.AssertNoDiagnostic(diags, "COBOLNET0901");
    }

    /// <summary>The same clause under <c>--permissive</c>: FORMAT is a word COBOL-2002 ADDED to §8.9 (r85 false),
    /// so its verdict is <c>Removed</c>, the migration mode HAS a pre-reservation reading to restore and the gate
    /// stands down — the decline stays a warning and the program compiles. (The gate is not blanket-off under
    /// <c>--permissive</c>: a word reserved at this edition and at every older one keeps it — kb/Work PB792.)
    /// </summary>
    [Fact]
    public void ValidateStatusOnPhrase_2002Permissive_WarnsAndCompiles()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(ValidateStatusOnPhraseProgram, 2002, permissive: true);
        Assert.True(ok, $"--std 2002 --permissive: {string.Join("; ", errors)}");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1708");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0901");
    }

    /// <summary>ONE occurrence of the word, ONE report (kb/Work PB693). A REFERENCE to a reservation-gated
    /// word cannot parse, so the §8.9 answer comes from <c>CobolErrorListener</c>'s re-code rather than the
    /// bound-tree funnel — and ANTLR raises TWO syntax errors on the one offending token (the prediction
    /// failure and <c>CobolErrorStrategy</c>'s recovery message). Re-coding both printed the identical
    /// sentence twice; §8.3.2.1 rule 1 is violated once by one occurrence, and it is reported once.</summary>
    [Theory]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void ReservedWordReference_IsNamedExactlyOnce(int edition)
    {
        var (ok, diags) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RWPONCE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 FORMAT PIC X(4) VALUE "ABCD".
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY FORMAT.
                STOP RUN.
            """, edition);
        Assert.False(ok, $"--std {edition} strict must reject a reference to the reserved word FORMAT");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0901");
        Assert.Equal(1, diags.Count(d => d.Contains("COBOLNET0901", StringComparison.Ordinal)
                                         && d.Contains("'FORMAT'", StringComparison.Ordinal)));
    }
}
