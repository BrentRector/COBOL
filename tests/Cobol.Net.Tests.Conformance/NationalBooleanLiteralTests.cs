// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// N"…" / B"…" literal BINDING at every operand funnel (ISO §8.3.3.5 national literals / §8.3.3.4 boolean
/// literals; Phase 4a M2-DATA-3/4). Before this increment every funnel silently misbound the token text
/// (the leading N/B and the quotes leaked into the value); now each funnel either DECODES the literal with its
/// category tag (string-legal contexts — DISPLAY, MOVE, EVALUATE selection, INSPECT args, CALL USING) or
/// rejects COBOLNET0844 (numeric contexts — §8.8.1: a boolean/national literal is not a numeric operand).
/// The literal guard rides COBOLNET0814: length &gt; 8,191 positions (§8.3.3.4.3 SR1 / §8.3.3.5.3 SR1; asked of every literal token by LiteralScreenPass — kb/Work PB1393). The
/// content repertoire is the FULL national set (one UTF-16 char per position, D-N1) — the former
/// Latin-1-only staged guard was LIFTED by the P10 national wave when the §8.1.2 alphanumeric↔national
/// correspondence landed (FUNCTION DISPLAY-OF / NATIONAL-OF, §15.26/§15.66).
/// </summary>
public sealed class NationalBooleanLiteralTests
{
    /// <summary>DISPLAY of a national literal (ISO §14.9.11 SR1 — a national literal is a legal DISPLAY
    /// operand; §8.3.3.5 GR1 — the value is the literal's characters, prefix and quotes stripped).</summary>
    [Fact]
    public void Display_NationalLiteral_DecodesPrefixAndQuotes()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBLIT01.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "N=" N"AB".
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("N=AB", stdout);
    }

    /// <summary>DISPLAY of a boolean literal (ISO §14.9.11 SR1; §8.3.3.4 GR1 — the value is the boolean
    /// characters themselves, prefix and quotes stripped).</summary>
    [Fact]
    public void Display_BooleanLiteral_DecodesPrefixAndQuotes()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBLIT02.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "B=" B"0101".
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("B=0101", stdout);
    }

    /// <summary>EVALUATE selection objects take the decode arm (the SoleNumLiteral funnel): a WHEN national
    /// literal compares under the national relation rules (ISO §14.9.13; §8.8.4.2.9 — equality under the
    /// national collating sequence).</summary>
    [Fact]
    public void Evaluate_NationalLiteralSelection_Matches()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBLIT03.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 NW PIC N(2) VALUE N"AB".
            PROCEDURE DIVISION.
            MAIN.
                EVALUATE NW
                    WHEN N"XY" DISPLAY "W=XY"
                    WHEN N"AB" DISPLAY "W=AB"
                    WHEN OTHER DISPLAY "W=OTHER"
                END-EVALUATE.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("W=AB", stdout);
    }

    /// <summary>EVALUATE with a boolean subject and boolean-literal WHEN objects (ISO §14.9.13; §8.8.4.2.8 —
    /// boolean equality by VALUE regardless of usage).</summary>
    [Fact]
    public void Evaluate_BooleanLiteralSelection_Matches()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBLIT04.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BW PIC 1(2) VALUE B"10".
            PROCEDURE DIVISION.
            MAIN.
                EVALUATE BW
                    WHEN B"01" DISPLAY "W=01"
                    WHEN B"10" DISPLAY "W=10"
                    WHEN OTHER DISPLAY "W=OTHER"
                END-EVALUATE.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("W=10", stdout);
    }

    /// <summary>INSPECT REPLACING with national-literal arguments over a national item (ISO §14.9.22 — the
    /// all-national operand rule; the Inspect funnel's decode arm).</summary>
    [Fact]
    public void Inspect_NationalLiteralArgs_Replace()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBLIT05.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 NW PIC N(5) VALUE N"AABBA".
            PROCEDURE DIVISION.
            MAIN.
                INSPECT NW REPLACING ALL N"A" BY N"Z".
                DISPLAY "R=" NW.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("R=ZZBBZ", stdout);
    }

    /// <summary>INSPECT REPLACING with boolean-literal arguments over a boolean item (ISO §14.9.22; under
    /// D-B1 a boolean position is a character position, so the character INSPECT machinery carries it).</summary>
    [Fact]
    public void Inspect_BooleanLiteralArgs_Replace()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBLIT06.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BW PIC 1(4) VALUE B"1010".
            PROCEDURE DIVISION.
            MAIN.
                INSPECT BW REPLACING ALL B"1" BY B"0".
                DISPLAY "R=" BW.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("R=0000", stdout);
    }

    /// <summary>CALL USING with national and boolean LITERAL arguments (ISO §14.9.4 — a literal argument
    /// passes BY CONTENT: spelled explicitly here, though Format 2's keyword-less literal binds the same
    /// mode per §14.9.4.4 GR9 a)2 since kb/Work PB130/PB131; the callee is CONTAINED because AS NESTED
    /// requires it, §14.9.4.3 SR15): the formals see the decoded values, never the prefixed token text.</summary>
    [Fact]
    public void Call_UsingNationalAndBooleanLiterals_PassesDecodedContent()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBLIT07.
            PROCEDURE DIVISION.
            MAIN.
                CALL "NBLIT07S" AS NESTED USING BY CONTENT N"AB" BY CONTENT B"01".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBLIT07S.
            DATA DIVISION.
            LINKAGE SECTION.
            01 LK-N PIC N(2).
            01 LK-B PIC 1(2).
            PROCEDURE DIVISION USING LK-N LK-B.
            SUB-P.
                DISPLAY "SUB=" LK-N "/" LK-B.
                EXIT PROGRAM.
            END PROGRAM NBLIT07S.
            END PROGRAM NBLIT07.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("SUB=AB/01", stdout);
    }

    /// <summary>MOVE of national/boolean literals to same-category receivers (ISO §14.9.25 Table 16;
    /// receivers fill right with national spaces §14.6.8.5 / boolean zeros §14.6.8.6).</summary>
    [Fact]
    public void Move_NationalAndBooleanLiterals_StoreWithCategoryFill()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBLIT08.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 NW PIC N(4).
            01 BW PIC 1(4).
            PROCEDURE DIVISION.
            MAIN.
                MOVE N"AB" TO NW.
                MOVE B"10" TO BW.
                DISPLAY "N=" NW "!".
                DISPLAY "B=" BW.
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("N=AB  !\nB=1000", stdout);
    }

    /// <summary>A national literal longer than 8,191 national positions is rejected COBOLNET0814
    /// (ISO §8.3.3.5.3 SR1 — "shall be less than or equal to 8,191 national character positions"). The source is built programmatically —
    /// an 8,192-character literal never belongs in a checked-in file.</summary>
    [Fact]
    public void NationalLiteral_Over8191Positions_Rejects0814()
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBLIT09.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY N"{new string('A', 8192)}".
                STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok, "an 8,192-position national literal must be rejected (ISO §8.3.3.5.3 SR1)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0814");
    }

    /// <summary>A boolean literal longer than 8,191 boolean positions is rejected COBOLNET0814
    /// (ISO §8.3.3.4.3 SR1 — "shall be less than or equal to 8,191 boolean character positions").</summary>
    [Fact]
    public void BooleanLiteral_Over8191Positions_Rejects0814()
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBLIT10.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY B"{new string('0', 8192)}".
                STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok, "an 8,192-position boolean literal must be rejected (ISO §8.3.3.4.3 SR1)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0814");
    }

    /// <summary>A boolean literal in a numeric context is COBOLNET0844 (ISO §8.8.1 — an arithmetic operand
    /// is a numeric item or numeric literal; class boolean is neither).</summary>
    [Fact]
    public void BooleanLiteral_InNumericContext_Rejects0844()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBLIT11.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X PIC 9(3) VALUE 1.
            PROCEDURE DIVISION.
            MAIN.
                ADD B"01" TO X.
                STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok, "ADD of a boolean literal must be rejected (ISO §8.8.1)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0844");
    }

    /// <summary>A national literal in a numeric-expression position is COBOLNET0844 (ISO §8.8.1).</summary>
    [Fact]
    public void NationalLiteral_InNumericContext_Rejects0844()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBLIT12.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 X PIC 9(3) VALUE 1.
            PROCEDURE DIVISION.
            MAIN.
                ADD N"12" TO X.
                STOP RUN.
            """;
        // (An INNER-expression position — COMPUTE X = N"12" + 1 — never reaches the binder: the
        // primaryExpression grammar admits numeric literals only, so it is a structural parse rejection.
        // The ADD-operand shape exercises the binder's §8.8.1.1 0844 arm, like the boolean twin.)
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2002);
        Assert.False(ok, "a national literal in an arithmetic context must be rejected (ISO §8.8.1)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0844");
    }

    /// <summary>A non-Latin-1 character in an N"…" literal is LEGAL (§8.3.3.5 — the content repertoire is the
    /// full national set, one UTF-16 char per position, D-N1; the P10 national wave lifted the staged
    /// Latin-1-only 0814 guard). FUNCTION DISPLAY-OF carries it UNCHANGED: the alphanumeric↔national
    /// correspondence is the Annex A.1 item-33 TOTAL UTF-16 identity (PB59), so §15.26.4 r2/r3's substitution
    /// machinery is vacuous — argument-2 is accepted and inert, and no EC-DATA-CONVERSION is set from any
    /// character pathway. The standard display device writes UTF-8 (CONFORMANCE.md item 59), which is what
    /// makes the wide character assertable in stdout at all.
    /// ⚠ ENCODING-SENSITIVE: the GREEK CAPITAL OMEGA (U+03A9) is written via a \u escape so this .cs file
    /// stays ASCII; the harness writes the .cob as UTF-8 — the compiler's source decoding must see ONE char
    /// &gt; U+00FF (a Latin-1 read would mis-decode it as two ≤U+00FF chars and the identity would carry
    /// two mangled chars below).</summary>
    [Fact]
    public void NationalLiteral_NonLatin1Char_Accepted_DisplayOfCarriesIt()
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NBLIT13P10N.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY FUNCTION DISPLAY-OF(N"A{'\u03A9'}B").
                DISPLAY FUNCTION DISPLAY-OF(N"A{'\u03A9'}B", "#").
                STOP RUN.
            """;
        var (ok, stdout, detail) = new CobolNetCompiler(2002).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal($"A{'\u03A9'}B\nA{'\u03A9'}B", stdout);
    }
}
