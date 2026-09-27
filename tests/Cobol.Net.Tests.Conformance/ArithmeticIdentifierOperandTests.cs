// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB1142 — the arithmetic verbs' operand screens over EVERY identifier format the grammar writes in an
/// operand position: a data reference, a function-identifier (ISO §8.4.3.1.2 Format 1) and an inline method
/// invocation (Format 4).
/// <para>
/// <b>Receivers.</b> Format 1 of ADD / SUBTRACT / MULTIPLY / DIVIDE prints <c>{identifier-2 [rounded-phrase]}</c>
/// (§14.9.2.2 / §14.9.44.2 / §14.9.26.2 / §14.9.12.2), a RECEIVER; §8.4.3.2.3 SR1 ("A function-identifier shall
/// not be specified as a receiving operand") and §8.4.3.4.3 SR1 ("Inline method invocation shall not be specified
/// as a receiving operand") bar the two non-data-name formats. Before the fix an inline invocation written there
/// compiled clean with the receiver silently DROPPED (ADD / SUBTRACT / MULTIPLY) and crashed the compiler (DIVIDE:
/// <c>InvalidOperationException: Sequence contains no elements</c> in the emitter).
/// </para>
/// <para>
/// <b>Senders.</b> §14.9.2.3 SR2 / §14.9.44.3 SR2 ("shall reference numeric data items"), §14.9.26.3 SR1 and
/// §14.9.12.3 SR1 ("of category numeric") and §8.8.1.1 ("An arithmetic expression may be an identifier referencing
/// a numeric data item") bar an alphanumeric function or invocation result as an arithmetic operand. COMPUTE
/// rejected the function form; every other verb's written operand bound it unscreened and digit-decoded it, and
/// NO position screened the invocation form.
/// </para>
/// <para>The corpus pins the positive shapes (<c>2002/pb1142_arithmetic_identifier_operands</c>) and one
/// negative per mechanism; THIS class owns the full verb × arm matrix, which one-diagnostic corpus fixtures cannot
/// express.</para>
/// </summary>
public sealed class ArithmeticIdentifierOperandTests
{
    /// <summary>A program plus a class whose GETX returns a NUMERIC item and GETNAME an ALPHANUMERIC one.</summary>
    private static string Program(string id, string statement) => $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. {id}.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS {id}C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(3)V9 VALUE 2.5.
       01 B PIC 9(3)V9 VALUE 3.3.
       01 C PIC 9(3)V9 VALUE 0.
       01 OB USAGE OBJECT REFERENCE {id}C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE {id}C "NEW" RETURNING OB.
           {statement}
           DISPLAY "B=" B " C=" C.
           STOP RUN.
       END PROGRAM {id}.

       IDENTIFICATION DIVISION.
       CLASS-ID. {id}C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-X PIC 9(3)V9.
       PROCEDURE DIVISION RETURNING LK-X.
       MAIN.
           MOVE 4 TO LK-X.
       END METHOD GETX.
       METHOD-ID. GETNAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-N PIC X(2).
       PROCEDURE DIVISION RETURNING LK-N.
       MAIN.
           MOVE "12" TO LK-N.
       END METHOD GETNAME.
       END OBJECT.
       END CLASS {id}C.
""";

    private static int Count(IEnumerable<string> diagnostics, string code) =>
        diagnostics.Count(d => d.Contains(code, StringComparison.Ordinal));

    /// <summary>Every Format-1 verb × every non-receiver arm the mixed operand rules write: ONE COBOLNET1689 per
    /// bad operand, no crash — at 2002 (Format 4's introduction) and at the default 2023. The MULTIPLY row with a
    /// valid receiver BEFORE the invocation is the silent-drop shape the note measured.</summary>
    [Theory]
    [InlineData("ADD A TO OB :: \"GETX\".")]
    [InlineData("ADD A TO FUNCTION SQRT(4).")]
    [InlineData("ADD A TO 3.")]
    [InlineData("SUBTRACT A FROM OB :: \"GETX\".")]
    [InlineData("SUBTRACT A FROM FUNCTION SQRT(4).")]
    [InlineData("SUBTRACT A FROM 3.")]
    [InlineData("MULTIPLY A BY OB :: \"GETX\".")]
    [InlineData("MULTIPLY A BY B OB :: \"GETX\".")]
    [InlineData("MULTIPLY A BY FUNCTION SQRT(4).")]
    [InlineData("MULTIPLY A BY 3.")]
    [InlineData("DIVIDE A INTO OB :: \"GETX\".")]
    [InlineData("DIVIDE A INTO FUNCTION SQRT(4).")]
    [InlineData("DIVIDE A INTO 3.")]
    public void Format1_NonReceiverOperand_IsRejected_NotDroppedOrCrashed(string statement)
    {
        foreach (int edition in new[] { 2002, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Program("PB1142R", statement), edition);
            Assert.False(ok, $"[std {edition}] '{statement}' must be rejected — Format 1 prints receivers only");
            Assert.True(Count(errors, "COBOLNET1689") == 1,
                $"[std {edition}] '{statement}': expected exactly one COBOLNET1689, got:\n{string.Join("\n", errors)}");
        }
    }

    /// <summary>Two bad BY operands draw two diagnostics — each operand is screened, not only the first.</summary>
    [Fact]
    public void Format1_EveryBadMultiplyOperand_IsNamed()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(
            Program("PB1142R2", "MULTIPLY A BY OB :: \"GETX\" FUNCTION SQRT(4)."), 2023);
        Assert.False(ok);
        Assert.True(Count(errors, "COBOLNET1689") == 2, string.Join("\n", errors));
    }

    /// <summary>Every sending identifier position of every arithmetic verb, with an ALPHANUMERIC function and an
    /// ALPHANUMERIC inline invocation: rejected with COBOLNET0844 under STRICT, exactly as COMPUTE already rejected
    /// the function form.</summary>
    [Theory]
    [InlineData("ADD {0} TO B.")]
    [InlineData("ADD A TO {0} GIVING C.")]
    [InlineData("SUBTRACT {0} FROM B.")]
    [InlineData("SUBTRACT A FROM {0} GIVING C.")]
    [InlineData("MULTIPLY {0} BY B.")]
    [InlineData("MULTIPLY A BY {0} GIVING C.")]
    [InlineData("DIVIDE {0} INTO B.")]
    [InlineData("DIVIDE A INTO {0} GIVING C.")]
    [InlineData("DIVIDE {0} BY A GIVING C.")]
    [InlineData("DIVIDE A BY {0} GIVING C.")]
    [InlineData("COMPUTE C = {0}.")]
    [InlineData("COMPUTE C = {0} + 1.")]
    public void AlphanumericIdentifier_AsArithmeticOperand_IsRejected(string shape)
    {
        foreach (string id in new[] { "FUNCTION UPPER-CASE(\"12\")", "OB :: \"GETNAME\"" })
        {
            string statement = string.Format(shape, id);
            var (ok, errors, _) = EditionHarness.CompileFull(Program("PB1142S", statement), 2023);
            Assert.False(ok, $"'{statement}' must be rejected — ISO §8.8.1.1 admits only a NUMERIC identifier");
            EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0844");
        }
    }

    /// <summary>The THIRD spelling of a function-identifier — the word FUNCTION omitted under a REPOSITORY
    /// entry (§8.4.3.2.3 SR2), which the grammar parses as a data reference — takes the same screen in every
    /// arithmetic position (it bypassed it everywhere, COMPUTE included: <c>COMPUTE C = UPPER-CASE("12") + 1</c>
    /// gave 13 under STRICT); a SOLE one stays a legal relation operand.</summary>
    [Theory]
    [InlineData("COMPUTE C = UPPER-CASE(\"12\") + 1.", false)]
    [InlineData("MULTIPLY UPPER-CASE(\"12\") BY B.", false)]
    [InlineData("ADD A TO UPPER-CASE(\"12\") GIVING C.", false)]
    [InlineData("IF UPPER-CASE(\"ab\") = \"AB\" DISPLAY \"EQ\" END-IF.", true)]
    public void KeywordOmittedFunction_TakesTheSameScreen(string statement, bool legal)
    {
        string src = $"""
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1142K.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION UPPER-CASE INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9(3)V9 VALUE 2.5.
       01 B PIC 9(3)V9 VALUE 3.3.
       01 C PIC 9(3)V9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           {statement}
           STOP RUN.
""";
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2023);
        if (legal)
            Assert.True(ok, string.Join("\n", errors));
        else
        {
            Assert.False(ok, $"'{statement}' must be rejected — ISO §8.8.1.1");
            EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0844");
        }
    }

    /// <summary>The dialect gate is the data item's (owner decision 2026-07-29): under --permissive both
    /// identifier formats are ACCEPTED with the 0844 warning and digit-decode alike — 3.3 × 12 = 39.6.</summary>
    [Theory]
    [InlineData("FUNCTION UPPER-CASE(\"12\")", "PB1142PF")]
    [InlineData("OB :: \"GETNAME\"", "PB1142PI")]
    public void Permissive_AlphanumericIdentifier_DigitDecodes_WithTheWarning(string id, string programId)
    {
        var (okc, _, warnings) = EditionHarness.CompileFull(Program(programId, $"MULTIPLY {id} BY B."), 2023,
            permissive: true);
        Assert.True(okc);
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET0844");
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(Program(programId, $"MULTIPLY {id} BY B."), 2023,
            permissive: true);
        Assert.True(ok, detail);
        Assert.Equal("B=0396 C=0000", stdout.Trim());
    }
}
