// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ABBREVIATED COMBINED RELATION CONDITIONS (ISO/IEC 1989:2023 §8.8.4.12). In a paren-free sequence of relations joined
/// by AND / OR / XOR, a succeeding relation may omit the subject (operator stated — <c>A &gt; B OR &lt; C</c>) or both
/// the subject and operator (a bare operand — <c>A = B AND C</c> ≡ <c>A = C</c>). §8.8.4.12.4 GR1: the last STATED
/// subject and the last STATED relational operator are inserted; a NOT may be part of a carried relational operator
/// (<c>A NOT &lt; C</c>); a parenthesized sub-condition is a complete simple condition that starts a fresh scope.
/// <para>
/// SPEC-ANCHORED: each case asserts the result DERIVED FROM §8.8.4.12.4 (the ISO spec is the authority for behavior),
/// then cross-checks that the legacy oracle agrees (a regression net only — a divergence would mean one is
/// non-conformant, which this test would surface). The expected value is computed by hand-expanding per GR1 with
/// A=5, B=3, C=7, D=5 (shown in each case).
/// </para>
/// </summary>
public sealed class AbbreviatedConditionDifferentialTests
{
    private static readonly ICompilerUnderTest Legacy = new LegacyCompiler();
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    // A=5, B=3, C=7, D=5.
    private static string Program(string proc) => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. ABBREVTEST.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 A PIC S9(3) VALUE 5.
        01 B PIC S9(3) VALUE 3.
        01 C PIC S9(3) VALUE 7.
        01 D PIC S9(3) VALUE 5.
        PROCEDURE DIVISION.
        MAIN-PARA.
            IF {{proc}} DISPLAY "Y" ELSE DISPLAY "N" END-IF.
            STOP RUN.
        """;

    /// <summary>Assert the program's result equals the SPEC-DERIVED <paramref name="expected"/> (ISO §8.8.4.12.4),
    /// then assert the legacy oracle agrees (cross-check; the spec value is the authority).</summary>
    private static void AssertSpec(string proc, string expected)
    {
        string source = Program(proc);
        var (cok, cout, cdetail) = CobolNet.CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(expected, cout);                 // primary: conformance to the ISO spec
        var (lok, lout, ldetail) = Legacy.CompileAndRun(source);
        Assert.True(lok, $"legacy oracle failed: {ldetail}");
        Assert.Equal(expected, lout);                 // cross-check: the oracle agrees with the spec value
    }

    [Theory]
    // Subject omitted (operator stated):
    [InlineData("A > B OR > C",   "Y")] // (5>3) OR (5>7) = T
    [InlineData("A > C OR > B",   "Y")] // (5>7) OR (5>3) = T
    [InlineData("A = D OR = B",   "Y")] // (5=5) OR (5=3) = T
    [InlineData("A > B AND > D",  "N")] // (5>3) AND (5>5) = F
    // Subject AND operator omitted (a bare operand carries both):
    [InlineData("A = B OR C",     "N")] // (5=3) OR (5=7) = F
    [InlineData("A = D OR B",     "Y")] // (5=5) OR (5=3) = T
    [InlineData("A < C AND D",    "N")] // (5<7) AND (5<5) = F
    // NOT as part of the carried relational operator:
    [InlineData("A NOT = B OR C", "Y")] // (5≠3) OR (5≠7) = T
    [InlineData("A NOT = D OR = B", "N")] // (5≠5) OR (5=3) = F
    [InlineData("A > B AND NOT < C", "N")] // (5>3) AND (5 NOT< 7 ≡ 5≥7) = F
    // Longer chains (operator carry updates as it goes):
    [InlineData("A < B OR < C OR < D", "Y")] // (5<3) OR (5<7) OR (5<5) = T
    [InlineData("A = D AND > B AND C", "N")] // (5=5) AND (5>3) AND (5>7): bare C carries the last stated op (>) = F
    // The §8.8.4.12.4 worked example: A > B AND NOT < C OR D ≡ ((A>B) AND (A NOT< C)) OR (A NOT< D); the carried
    // operator for the trailing bare D is the NOT-< from the abbreviated relation.
    [InlineData("A > B AND NOT < C OR D", "Y")] // ((5>3) AND (5≥7)) OR (5≥5) = (T AND F) OR T = T
    // A leading logical NOT on relation-condition-1 is NOT carried (the relation's own operator is): the §8.8.4.12.4
    // NOTE — NOT a = b OR c ≡ (NOT (a=b)) OR (a=c).
    [InlineData("NOT A = B OR C", "Y")] // (NOT(5=3)) OR (5=7) = T OR F = T
    // A parenthesized sub-condition is a complete simple condition that starts a FRESH abbreviation scope (GR1): inside,
    // B < C OR > A ≡ (B<C) OR (B>A) (subject B), independent of the outer A = D subject.
    [InlineData("A = D AND (B < C OR > A)", "Y")] // (5=5) AND ((3<7) OR (3>5)) = T AND (T OR F) = T
    public void Abbreviated(string proc, string expected) => AssertSpec(proc, expected);
}
