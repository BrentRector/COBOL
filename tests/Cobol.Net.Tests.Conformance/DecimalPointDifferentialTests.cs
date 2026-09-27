// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// SPECIAL-NAMES DECIMAL-POINT IS COMMA + CURRENCY SIGN (ISO/IEC 1989:2023 §12.3.7 GR13/GR14, §13.18.40.2 SR13,
/// §13.18.8 BLANK WHEN ZERO, §13.18.63 VALUE): spec-derived facts at COBOL-85, differential against the legacy
/// oracle (NIST NC107A/NC108M-green) except where the legacy deviates from the spec — zero suppression across
/// comma-mode grouping periods (the legacy's edit-pattern pass ignores its comma flag; §13.18.40.5 + the SR13
/// role exchange win) and the GR6 numeric-VALUE-on-edited conversion — those are SPEC-PINNED.
/// </summary>
public sealed class DecimalPointDifferentialTests
{
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    private static void AssertSameAsLegacy(string source) => DifferentialGolden.Assert(source);

    /// <summary>Spec-pinned (no oracle): asserted against the ISO-derived expected output directly. The optional
    /// <paramref name="dialect"/> targets a specific edition (a numeric literal VALUE on a numeric-edited item is a
    /// COBOL-2023 feature, ISO §13.18.63 SR6 / Annex E.3.3 item 43 — such tests pin at 2023).</summary>
    private static void AssertSpecPinned(string source, string expected, int dialect = 85)
    {
        var (cok, cout, cdetail) = new CobolNetCompiler(dialect).CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(expected, cout);
    }

    /// <summary>§12.3.7 GR14a: under the clause the comma is the literal decimal separator — a comma VALUE
    /// initializes the scaled item; §13.18.40.2 SR13 + GR14b: the edited mask's comma is the decimal point and
    /// its periods are grouping separators, INSERTED as written (NC107A's DATA-K → DATA-L shape).</summary>
    [Fact]
    public void CommaDecimal_ValueInit_And_GroupedEditedMask()
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DPC1.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                DECIMAL-POINT IS COMMA.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            77 DATA-K PICTURE IS 9999999V99 VALUE IS 1234567,89.
            77 DATA-L PICTURE IS 9.999.999,99.
            PROCEDURE DIVISION.
            MAIN-P.
                MOVE DATA-K TO DATA-L.
                DISPLAY "L=" DATA-L.
                STOP RUN.
            """);

    /// <summary>§12.3.7 GR14a: comma literals in MOVE and in a relation condition (NC107A's 9116,44 shape).</summary>
    [Fact]
    public void CommaDecimal_MoveAndCompareLiterals()
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DPC2.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                DECIMAL-POINT IS COMMA.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            77 W PICTURE 9999V99.
            PROCEDURE DIVISION.
            MAIN-P.
                MOVE 9116,44 TO W.
                IF W EQUAL TO 9116,44 DISPLAY "EQ" ELSE DISPLAY "NE".
                ADD 0,06 TO W.
                IF W EQUAL TO 9116,50 DISPLAY "SUM-OK" ELSE DISPLAY "SUM-BAD".
                STOP RUN.
            """);

    /// <summary>§12.3.7 GR13 + SR22: a bare CURRENCY SIGN literal is both currency string and PICTURE symbol —
    /// 'W' masks classify and edit exactly like '$' masks (NC107A's DATA-J/DATA-M shape).</summary>
    [Fact]
    public void CurrencySign_W_FloatingAndFixedMasks()
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DPC3.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CURRENCY SIGN IS "W".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            77 SRC PICTURE 9999 VALUE 123.
            77 FLO PICTURE WWWW9.
            77 FIX PICTURE W9999 BLANK WHEN ZERO.
            PROCEDURE DIVISION.
            MAIN-P.
                MOVE SRC TO FLO.
                MOVE SRC TO FIX.
                DISPLAY "FLO=" FLO.
                DISPLAY "FIX=" FIX.
                MOVE 0 TO FIX.
                DISPLAY "FIXZ=" FIX "=".
                STOP RUN.
            """);

    /// <summary>NC108M's shape: CURRENCY "&lt;" with DEFAULT separators — a floating &lt; string with ','
    /// grouping and '.' decimal inserts per §13.18.40.4.</summary>
    [Fact]
    public void CurrencySign_AngleBracket_DefaultSeparators()
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DPC4.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CURRENCY "<".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            77 SRC PICTURE 9999V99 VALUE 1111.11.
            77 FL PICTURE <(3),<<<.99.
            PROCEDURE DIVISION.
            MAIN-P.
                MOVE SRC TO FL.
                DISPLAY "FL=" FL "=".
                STOP RUN.
            """);

    /// <summary>§13.18.8 GR1/GR2: BLANK WHEN ZERO on a category-numeric picture defines the item numeric-edited;
    /// a zero store sets ALL SPACES, and the item compares as alphanumeric (NC108M's FMT-TEST-GF-3 /
    /// ABR-TEST-GF-4 shapes).</summary>
    [Fact]
    public void BlankWhenZero_OnPlainNumeric_StoresSpaces()
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DPC5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SEND-BLANK PICTURE 9(5) VALUE ZERO.
            01 RECEIVE-BLANK PICTURE 9(9) BLANK ZERO.
            PROCEDURE DIVISION.
            MAIN-P.
                MOVE SEND-BLANK TO RECEIVE-BLANK.
                IF RECEIVE-BLANK EQUAL TO "         " DISPLAY "BLANKED" ELSE DISPLAY "NOT-BLANKED".
                MOVE 42 TO RECEIVE-BLANK.
                DISPLAY "R=" RECEIVE-BLANK "=".
                STOP RUN.
            """);

    /// <summary>SPEC-PINNED — §13.18.40.5 zero suppression + the §13.18.40.2 SR13 role exchange: a suppressed
    /// zone ABSORBS the comma-mode grouping PERIODS and suppression stops at the COMMA decimal position. The
    /// legacy's edit-pattern pass reads its comma flag and never uses it (hard-coded period-as-decimal), so the
    /// expectation derives from the spec: 12,34 in Z.ZZZ.ZZZ,99 → "       12,34" (7 suppressed positions —
    /// Z.ZZZ.ZZZ — then the comma decimal and the fraction).</summary>
    [Fact]
    public void CommaDecimal_ZeroSuppressionAcrossGroupingPeriods_SpecPinned()
        => AssertSpecPinned("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DPC6.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                DECIMAL-POINT IS COMMA.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            77 W PICTURE 9999V99 VALUE 12,34.
            77 Z PICTURE Z.ZZZ.ZZZ,99.
            PROCEDURE DIVISION.
            MAIN-P.
                MOVE W TO Z.
                DISPLAY "Z=" Z "=".
                STOP RUN.
            """, "Z=       12,34=");

    /// <summary>SPEC-PINNED — §13.18.63 GR6: a NUMERIC literal VALUE on a numeric-edited item is converted to
    /// its edited form per the MOVE rules ("01.50", not the raw text).</summary>
    [Fact]
    public void NumericValue_OnEditedItem_ConvertsPerMoveRules_SpecPinned()
        => AssertSpecPinned("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DPC7.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            77 E PICTURE 99.99 VALUE 1.5.
            PROCEDURE DIVISION.
            MAIN-P.
                DISPLAY "E=" E "=".
                STOP RUN.
            """, "E=01.50=", dialect: 2023);   // numeric literal VALUE on a numeric-edited item is 2023 (Annex E.3.3 item 43)

    /// <summary>§12.3.7 GR14a (negative): a tight-comma decimal literal WITHOUT the clause is rejected with the
    /// specific COBOLNET0895 diagnostic — §8.3.3.3.2 admits only the decimal point. (The legacy accepts it
    /// unconditionally — a version-invariant non-conformance, not ported.)</summary>
    [Fact]
    public void CommaLiteral_WithoutClause_Rejected()
    {
        var (cok, _, cdetail) = CobolNet.CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DPC8.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            77 W PICTURE 99V9.
            PROCEDURE DIVISION.
            MAIN-P.
                MOVE 1,5 TO W.
                STOP RUN.
            """);
        Assert.False(cok, "a tight-comma literal without DECIMAL-POINT IS COMMA must be rejected");
        Assert.Contains("COBOLNET0895", cdetail);
    }

    /// <summary>§13.18.60 GR1: a group-level USAGE applies to each subordinate elementary item, and the
    /// typed-native whole-group semantics hold for the all-binary aligned-layout shapes (NC107A's
    /// USAGE-TEST-4/6: MOVE U5 TO U9 copies positionally; IF U22 &gt; U12 compares the representation).</summary>
    [Fact]
    public void GroupUsageComp_WholeGroupMoveAndCompare()
        => AssertSameAsLegacy("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DPC9.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 U1.
                02 U5 USAGE IS COMPUTATIONAL.
                   03 U6 PICTURE 9 USAGE COMPUTATIONAL VALUE 5.
                   03 U7 PICTURE 9 VALUE 6.
            01 U9 USAGE COMPUTATIONAL.
                02 U10 PICTURE 9.
                02 U11 PICTURE 9 COMPUTATIONAL.
            01 U12.
                02 U13 PICTURE 9 USAGE IS BINARY VALUE 3.
                02 U14 PICTURE 9 USAGE IS BINARY VALUE 3.
            01 U22.
                02 U23 PICTURE 9 USAGE IS BINARY VALUE 4.
                02 U24 PICTURE 9 USAGE IS BINARY VALUE 4.
            PROCEDURE DIVISION.
            MAIN-P.
                MOVE U5 TO U9.
                IF U6 EQUAL TO U10 DISPLAY "M1-OK" ELSE DISPLAY "M1-BAD".
                IF U7 EQUAL TO U11 DISPLAY "M2-OK" ELSE DISPLAY "M2-BAD".
                IF U22 GREATER THAN U12 DISPLAY "C1-OK" ELSE DISPLAY "C1-BAD".
                STOP RUN.
            """);
}
