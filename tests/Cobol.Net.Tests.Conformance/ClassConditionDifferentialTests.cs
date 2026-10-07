// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Class conditions (ISO §8.8.4.4): <c>IS [NOT] {NUMERIC | ALPHABETIC | ALPHABETIC-UPPER | ALPHABETIC-LOWER}</c>.
/// ALPHABETIC is the closed Latin set {A–Z, a–z, space} (not <c>char.IsLetter</c>); a typed-numeric field IS NUMERIC
/// folds to true. Pinned to the committed differential goldens.
/// </summary>
public sealed class ClassConditionDifferentialTests
{
    private static readonly CobolNetCompiler CobolNet = new();

    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CLSTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    [Theory]
    [InlineData("01 X PIC X(3) VALUE \"ABC\".", "    IF X IS ALPHABETIC DISPLAY \"AL\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"A1C\".", "    IF X IS ALPHABETIC DISPLAY \"AL\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"AB\".", "    IF X IS ALPHABETIC DISPLAY \"AL\" ELSE DISPLAY \"NO\" END-IF.")]   // "AB " — space is alphabetic
    [InlineData("01 X PIC X(3) VALUE \"123\".", "    IF X IS NUMERIC DISPLAY \"NU\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"12A\".", "    IF X IS NUMERIC DISPLAY \"NU\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"1 3\".", "    IF X IS NUMERIC DISPLAY \"NU\" ELSE DISPLAY \"NO\" END-IF.")]   // embedded space → not numeric
    [InlineData("01 N PIC 9(3) VALUE 42.", "    IF N IS NUMERIC DISPLAY \"NU\" ELSE DISPLAY \"NO\" END-IF.")]
    // ⛔ REMOVED — `IF N IS ALPHABETIC` over `01 N PIC 9(3)` is NOT CONFORMING SOURCE, and this row pinned the
    // compiler to the legacy oracle's acceptance of it (its comment read "digits not alphabetic", i.e. it was
    // asserting a RUN-TIME answer to a question §8.8.4.4.3 SR4 forbids asking). SR4: "ALPHABETIC,
    // ALPHABETIC-LOWER, ALPHABETIC-UPPER, or class-name-1 shall not be specified if the category of the data
    // item referenced by identifier-1 is boolean, numeric, or numeric-edited" — three categories, of which only
    // BOOLEAN was screened until kb/Work PB571. The legacy engine is a regression NET, never authority
    // (CLAUDE.md rule 1), and the case now lives where a rejection belongs:
    // tests/conformance/negative/pb571-class-alphabetic-on-numeric (COBOLNET0844, every edition).
    // The admission side — the same operand with an ALPHABET-NAME, which SR4 does NOT name — is
    // 85/pb571_class_condition_one_table's F line.
    [InlineData("01 X PIC X(3) VALUE \"ABC\".", "    IF X IS ALPHABETIC-UPPER DISPLAY \"UP\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"abc\".", "    IF X IS ALPHABETIC-UPPER DISPLAY \"UP\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"abc\".", "    IF X IS ALPHABETIC-LOWER DISPLAY \"LO\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"A1C\".", "    IF X IS NOT NUMERIC DISPLAY \"NN\" ELSE DISPLAY \"NO\" END-IF.")]
    [InlineData("01 X PIC X(3) VALUE \"ABC\".", "    IF X IS NOT ALPHABETIC DISPLAY \"NA\" ELSE DISPLAY \"AL\" END-IF.")]
    public void ClassConditions(string ws, string proc) => AssertMatchesGolden(Program(ws, proc));

    [Theory]
    // SPEC-ANCHORED — ISO §8.8.4.4.4 GR3 n)2: IS NUMERIC over an alphanumeric (non-numeric category) operand is true
    // ONLY if the content consists ENTIRELY of the digits 0–9; an operational sign is NOT a valid character. So
    // "+1234" / "12-" are NOT NUMERIC (the NC211A GF-48 CLASS-1 case). Result derived from the spec.
    [InlineData("01 X PIC X(5) VALUE \"+1234\".", "    IF X IS NUMERIC DISPLAY \"NU\" ELSE DISPLAY \"NO\" END-IF.", "NO")]
    [InlineData("01 X PIC X(5) VALUE \"+1234\".", "    IF X IS NOT NUMERIC DISPLAY \"NN\" ELSE DISPLAY \"NO\" END-IF.", "NN")]
    [InlineData("01 X PIC X(3) VALUE \"12-\".",   "    IF X IS NUMERIC DISPLAY \"NU\" ELSE DISPLAY \"NO\" END-IF.", "NO")]
    public void NumericClass_OverAlphanumeric_SignIsNotADigit(string ws, string proc, string expected)
    {
        string source = Program(ws, proc);
        string want = CutRunner.Normalize(expected);
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(want, cout);                       // ISO §8.8.4.4.4 GR3 n)2
    }
}
