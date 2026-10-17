// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The <c>--permissive</c> lane for a FLOATING-POINT operand at an ISO §15.3 type-6 (integer) argument
/// position — the coercion extension the strict screen names, and the home of everything kb/Work PB2 and PB21
/// proved once their programs stopped being conforming under strict.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ WHY THESE PROGRAMS MOVED (kb/Work PB248). <c>tests/conformance/2023/pb21_float_arg_integer_family</c> was
/// a STRICT positive golden whose header derived its legality as "a COMP-2 item is category numeric hence class
/// numeric, and §15.3's INTEGER type resolves through CLASS numeric — the integer-ness is a VALUE property, not
/// a class the argument screen can reject on". That premise contradicts the landed PB40 reading of the SAME
/// clause, which rejects a <c>PIC 9V9</c> operand at an integer position for exactly the reason it would admit a
/// COMP-2 one; the two could not both be right. Re-derived, §15.3 type 6 admits "an arithmetic expression that
/// will always result in an integer value <b>or</b> an integer data item" (--check verified) and a floating-point
/// item is neither — §14.6.8.3 sets its content to "the algebraic value of the sending operand", so its DECLARED
/// value set contains non-integers whatever a given reference holds. Strict now rejects (COBOLNET1627), and the
/// four <c>negative/pb248-*</c> fixtures pin that.
/// </para>
/// <para>
/// ⚠ WHAT THEY REALLY PROVED IS PRESERVED HERE, WHICH IS THE POINT OF MOVING THEM RATHER THAN DELETING THEM.
/// PB21's subject was a CRASH — a float operand at an integer position emitted a call to a runtime member that
/// DOES NOT EXIST and failed Roslyn with a raw CS0117 — and PB2's was the sibling CS1503 from handing a
/// <c>double</c> to an <c>Int128</c> parameter. Both programs still REACH the renderer under <c>--permissive</c>,
/// so both still need a guard; only the strict verdict changed. Since kb/Work PB651 the renderer takes a type-6
/// operand's INTEGER before it chooses a lane (<c>IntrinsicRenderer.RawArg</c>), so a float there never enters the
/// binary64 lane at all. The invariant each asserted — a function's value must not depend on how its argument
/// happened to be stored — is asserted here as the float operand agreeing with the fixed-point one value for value,
/// and over every verified type-6 position by <c>IntrinsicIntegerPositionPermissiveDriftTests</c>.
/// </para>
/// </remarks>
public sealed class FloatIntegerArgumentPermissiveTests
{
    /// <summary>kb/Work PB21's program, verbatim in its permissive home: six §15.3 type-6 functions over COMP-2
    /// operands. The expected values are the fixed-point ones — that IS the invariant.</summary>
    private const string Pb21Program = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB21FLOATPERMISSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D-DAY  COMP-2 VALUE 1995046.
       01 D-DATE COMP-2 VALUE 19950215.
       01 D-INT  COMP-2 VALUE 143951.
       01 D-YY   COMP-2 VALUE 95.
       01 D-FAC  COMP-2 VALUE 5.
       01 R      PIC 9(9).
       01 F      PIC 9(12).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION INTEGER-OF-DAY(D-DAY)
           DISPLAY "IODAY=" R
           COMPUTE R = FUNCTION INTEGER-OF-DATE(D-DATE)
           DISPLAY "IODATE=" R
           COMPUTE R = FUNCTION DAY-OF-INTEGER(D-INT)
           DISPLAY "DAYOFINT=" R
           COMPUTE R = FUNCTION DATE-OF-INTEGER(D-INT)
           DISPLAY "DATEOFINT=" R
           COMPUTE R = FUNCTION YEAR-TO-YYYY(D-YY, 50, 2000)
           DISPLAY "Y2YYYY=" R
           COMPUTE F = FUNCTION FACTORIAL(D-FAC)
           DISPLAY "FACT=" F
           COMPUTE R = FUNCTION INTEGER-OF-DAY(1995046)
           DISPLAY "IODAY-FIXED=" R
           STOP RUN.
       """;

    /// <summary>The MOD pair kb/Work PB2 lost to the strict screen — §15.64.3 r1 is "Argument-1 and argument-2
    /// shall be integers", so the FLOAT operands live here while the corpus golden keeps the integer ones.
    /// §15.64.4's equivalent arithmetic expression makes MOD FLOORED, so the result takes the sign of
    /// argument-2: MOD(−7, 3) = 2. The float lane must produce the same 2.</summary>
    private const string Pb2ModProgram = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2MODFLOATPERMISSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    USAGE COMP-2 VALUE -7.0.
       01 B    USAGE COMP-2 VALUE 3.0.
       01 R    USAGE COMP-2.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION MOD(A B)
           DISPLAY R
           STOP RUN.
       """;

    /// <summary>Strict REJECTS both, with the §15.3 type-6 diagnostic — the half the negative corpus pins,
    /// asserted here too so the pair of verdicts is readable in one place.</summary>
    [Theory]
    [InlineData(nameof(Pb21Program))]
    [InlineData(nameof(Pb2ModProgram))]
    public void FloatAtAnIntegerPosition_IsRejectedUnderStrict(string which)
    {
        string src = which == nameof(Pb21Program) ? Pb21Program : Pb2ModProgram;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2023);
        Assert.False(ok, "a floating-point operand at an ISO §15.3 type-6 position is not conforming");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1627");
        EditionHarness.AssertHasDiagnostic(errors, "floating-point numeric data item");
    }

    /// <summary>
    /// <c>--permissive</c> WARNS and computes, and the computed values are the FIXED-POINT ones. This is PB21's
    /// original assertion, unchanged: every value below is what the same call with an integer operand yields, so
    /// a coercion that drifted — or a missing runtime member, which is the CS0117 PB21 was opened for — fails
    /// here. <c>IODAY-FIXED</c> is the in-program control: the same function over a literal integer operand.
    /// </summary>
    [Fact]
    public void Pb21IntegerFamily_UnderPermissive_WarnsAndAgreesWithTheFixedPointPath()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Pb21Program, 2023, permissive: true);
        Assert.True(ok, $"--permissive must accept the coercion: {string.Join("\n", errors)}");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1627");

        var (ranOk, stdout, detail) = EditionHarness.CompileAndRun(Pb21Program, 2023, permissive: true);
        Assert.True(ranOk, detail);
        Assert.Equal(
            new[]
            {
                "IODAY=000143951",       // §15.45.4 — the integer date of ordinal date 1995046
                "IODATE=000143951",      // §15.46.4 — the same day reached through the calendar date 19950215
                "DAYOFINT=001995046",    // §15.24.4 — the inverse of INTEGER-OF-DAY
                "DATEOFINT=019950215",   // §15.22.4 — the inverse of INTEGER-OF-DATE
                "Y2YYYY=000001995",      // §15.100.4 — 95 windowed on base 2000 − 50 = 1950 → 1995
                "FACT=000000000120",     // §15.36.4 — 5! = 120
                "IODAY-FIXED=000143951", // the FIXED-POINT control: the float lane must equal it
            },
            stdout.Replace("\r\n", "\n").TrimEnd('\n').Split('\n'));
    }

    /// <summary>MOD's float pair under permissive: §15.64.4's equivalent arithmetic expression is floored, so
    /// MOD(−7, 3) = 2 — the same value the corpus golden now computes from integer operands.</summary>
    [Fact]
    public void Pb2Mod_UnderPermissive_IsFlooredJustAsTheFixedPointPathIs()
    {
        var (ok, _, warnings) = EditionHarness.CompileFull(Pb2ModProgram, 2023, permissive: true);
        Assert.True(ok, "--permissive must accept the coercion");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1627");

        var (ranOk, stdout, detail) = EditionHarness.CompileAndRun(Pb2ModProgram, 2023, permissive: true);
        Assert.True(ranOk, detail);
        Assert.Equal("2", stdout.Replace("\r\n", "\n").TrimEnd('\n'));
    }

    /// <summary>kb/Work PB254’s float lane, which is the WRONG-ANSWER half of that defect and, after
    /// PB248, is reachable only here. §15.90.3 r1 / §15.91.3 r1 constrain nothing but integer-ness and
    /// §15.90.4 r1a / §15.91.4 r1a are CATCH-ALLS, so 1.0E19 — eleven orders of magnitude above the
    /// windows — owes the verdict <b>1</b> ("the date is not valid"). The shipped body answered <b>0</b>,
    /// which is §15.90.4 r1d / §15.91.4 r1c "The date is valid", because <c>TryIntegerArg(…) ? body : 0</c>
    /// used a literal 0 as its domain-guard tail — and 0 is a VERDICT here, not a sentinel.</summary>
    private const string Pb254TotalDateProgram = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB254FLOATPERMISSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FBIG  USAGE COMP-2.
       01 FOK   USAGE COMP-2 VALUE 19950215.
       01 R2    PIC 9(2).
       PROCEDURE DIVISION.
           COMPUTE FBIG = 1.0E19
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(FBIG)
           DISPLAY "F1=" R2
           COMPUTE R2 = FUNCTION TEST-DAY-YYYYDDD(FBIG)
           DISPLAY "F2=" R2
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(FOK)
           DISPLAY "F3=" R2
           STOP RUN.
       """;

    /// <summary>
    /// ⛔ 0 IS A VERDICT, NOT A SENTINEL, AND THAT IS WHY THIS FACT EXISTS. Under <c>--permissive</c> the
    /// float twins run, and the two out-of-window references must answer <c>01</c>; <c>F3</c> is the
    /// in-program control — a date INSIDE the window on the same carrier, which owes <c>00</c>, so a body
    /// that simply returned 1 for everything would fail here too.
    /// </summary>
    [Fact]
    public void Pb254TotalDateValidators_UnderPermissive_AnswerR1aAndNotTheValidVerdict()
    {
        var (ok, _, warnings) = EditionHarness.CompileFull(Pb254TotalDateProgram, 2023, permissive: true);
        Assert.True(ok, "--permissive must accept the coercion");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1627");

        var (ranOk, stdout, detail) = EditionHarness.CompileAndRun(Pb254TotalDateProgram, 2023, permissive: true);
        Assert.True(ranOk, detail);
        Assert.Equal(
            new[]
            {
                "F1=01", // §15.90.4 r1a — 1.0E19 is greater than 99 999 999
                "F2=01", // §15.91.4 r1a — and greater than 9 999 999
                "F3=00", // §15.90.4 r1d — 19950215 IS a valid date: the control
            },
            stdout.Replace("\r\n", "\n").TrimEnd('\n').Split('\n'));
    }

    /// <summary>Strict refuses this program too, on the same §15.3 type-6 screen — the verdict pair is
    /// readable in one place, as it is for PB21 and PB2 above.</summary>
    [Fact]
    public void Pb254TotalDateValidators_AreRejectedUnderStrict()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Pb254TotalDateProgram, 2023);
        Assert.False(ok, "a floating-point operand at an ISO §15.3 type-6 position is not conforming");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1627");
    }

    /// <summary>
    /// kb/Work PB635 — the MIXED-CLASS member of this lane, and the one whose failure was a CRASH rather than a
    /// value. FIND-STRING is the only catalogued function that puts a §15.3 type-6 INTEGER position beside two
    /// STRING ones (§15.37.3 r1/r2 "class alphabetic, alphanumeric, or national"; r3 "argument-3 shall be an
    /// integer data item or integer literal"), so a float argument-3 asked the emitter's float dispatch a
    /// question it could not answer: it read "is ANY argument floating", moved the WHOLE call to the binary64
    /// lane, converted <c>H</c> and <c>ND</c> to <c>double</c>, dropped LAST and ANYCASE, and named a
    /// <c>FindStringReal</c> body that does not exist — <c>error CS0117: 'CobolIntrinsics' does not contain a
    /// definition for 'FindStringReal'</c>, Roslyn refusing the generated C# on source the front end accepted.
    /// <para>⛔ THE PHRASES ARE THE ASSERTION, not merely the compile. §15.4.1 grants the binary64 lane its
    /// licence for "an equivalent arithmetic expression", and §15.37.4 specifies none — the returned value is a
    /// character POSITION — so there is no binary64 anything in this call and every phrase has to survive.
    /// F1..F4 are the four (LAST × ANYCASE) combinations over the SAME float argument-3, and they answer four
    /// values that a body ignoring either phrase could not produce; F5 is the fixed-point control, the same
    /// reference with an integer literal argument-3, which owes F4's value exactly.</para>
    /// </summary>
    private const string Pb635FindStringProgram = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB635FINDSTRINGPERMISSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 H     PIC X(12) VALUE "abcABCabcABC".
       01 ND    PIC X(3)  VALUE "ABC".
       01 FSK   USAGE COMP-2.
       01 P     PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE FSK = 1.0E0
           MOVE FUNCTION FIND-STRING(H ND START AFTER FSK) TO P
           DISPLAY "F1=" P
           MOVE FUNCTION FIND-STRING(H ND START AFTER FSK ANYCASE) TO P
           DISPLAY "F2=" P
           MOVE FUNCTION FIND-STRING(H ND LAST START AFTER FSK) TO P
           DISPLAY "F3=" P
           MOVE FUNCTION FIND-STRING(H ND LAST START AFTER FSK ANYCASE) TO P
           DISPLAY "F4=" P
           MOVE FUNCTION FIND-STRING(H ND LAST START AFTER 1 ANYCASE) TO P
           DISPLAY "F5=" P
           STOP RUN.
       """;

    /// <summary>
    /// H = "abcABCabcABC". Case-sensitively, ND = "ABC" occurs at {4, 10}; under ANYCASE (§15.37.4 r4 — "as if
    /// all uppercase letters in both argument-1 and argument-2 were replaced by their corresponding lowercase
    /// letters") at {1, 4, 7, 10}. §15.37.4 r2: "argument-3 represents the number of matches to ignore before
    /// determining the character position that shall be returned", so argument-3 = 1 drops one occurrence from
    /// the head, or from the tail under LAST (§15.37.4 r1). Hence 10, 4, 4, 7 — and 7 again from the integer
    /// literal, because the value of a function may not depend on how its argument was stored.
    /// </summary>
    [Fact]
    public void Pb635FindString_UnderPermissive_KeepsItsOwnLaneAndItsPhrases()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Pb635FindStringProgram, 2023, permissive: true);
        Assert.True(ok, $"--permissive must accept the coercion: {string.Join("\n", errors)}");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1627");

        var (ranOk, stdout, detail) = EditionHarness.CompileAndRun(Pb635FindStringProgram, 2023, permissive: true);
        Assert.True(ranOk, detail);
        Assert.Equal(
            new[]
            {
                "F1=0010", // {4,10}, ignore 1 from the head
                "F2=0004", // ANYCASE {1,4,7,10}, ignore 1 from the head
                "F3=0004", // LAST over {4,10}, ignore 1 from the tail
                "F4=0007", // LAST + ANYCASE over {1,4,7,10}, ignore 1 from the tail
                "F5=0007", // the FIXED-POINT control: an integer literal argument-3, same answer
            },
            stdout.Replace("\r\n", "\n").TrimEnd('\n').Split('\n'));
    }

    /// <summary>
    /// kb/Work PB651 — a FRACTIONAL float at a §15.3 type-6 position, on each of the three lanes a call with one used to
    /// take: MOD's exact arm (which aligned the fraction instead of dropping it), COMBINED-DATETIME (which the float moved
    /// to the binary64 lane), and ANNUITY (a §15.4.1 float-family function, whose body took the double as it stood).
    /// docs/CONFORMANCE.md DOC-A.1-124: with EC-ARGUMENT-FUNCTION checking off the truncated integer is the result, so
    /// each answers exactly what its integer answers.
    /// </summary>
    private const string Pb651FractionProgram = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB651FRACTIONPERMISSIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F75   USAGE FLOAT-LONG VALUE 7.5.
       01 FDT   USAGE FLOAT-LONG VALUE 143951.5.
       01 F35   USAGE FLOAT-LONG VALUE 3.5.
       01 RM    PIC 9.
       01 RC    PIC 9(6)V9(5).
       01 RA    PIC 9V9(9).
       PROCEDURE DIVISION.
           COMPUTE RM = FUNCTION MOD(F75 3)
           DISPLAY "MOD=" RM
           COMPUTE RC = FUNCTION COMBINED-DATETIME(FDT 3600)
           DISPLAY "CDT=" RC
           COMPUTE RA = FUNCTION ANNUITY(0.05 F35)
           DISPLAY "ANN=" RA
           STOP RUN.
       """;

    /// <summary>The three values from the equivalent arithmetic expressions over the TRUNCATED integers: §15.64.4
    /// 7 − 3 × INTEGER(7 / 3) = 1; §15.17.4 143951 + 3600 / 100000 = 143951.03600; §15.9.4 1) b)
    /// 0.05 / (1 − 1.05 ** −3) = 0.367208564…, truncated to the receiver's nine fraction digits.</summary>
    [Fact]
    public void Pb651FractionalFloat_AtATypeSixPosition_UnderPermissive_IsTruncatedOnEveryLane()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Pb651FractionProgram, 2023, permissive: true);
        Assert.True(ok, $"--permissive must accept the coercion: {string.Join("\n", errors)}");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1627");

        var (ranOk, stdout, detail) = EditionHarness.CompileAndRun(Pb651FractionProgram, 2023, permissive: true);
        Assert.True(ranOk, detail);
        Assert.Equal(new[] { "MOD=1", "CDT=14395103600", "ANN=0367208564" },
            stdout.Replace("\r\n", "\n").TrimEnd('\n').Split('\n'));
    }

    /// <summary>
    /// kb/Work PB651 review R1-2 — a float whose integer lies PAST the <c>Int128</c> carrier (±1.7e38) at a §15.3 type-6
    /// position. The position intake used to CLAMP it to the carrier's extreme, which answers for a different integer:
    /// <c>MOD(1.0E39, 7)</c> answered 1, the remainder of 2¹²⁷ − 1, with no exception condition. Now a function whose
    /// value depends on the integer keeps it or says it cannot — EC-ARGUMENT-FUNCTION, here handled by a declarative
    /// that resumes, so the receiver keeps its 9999 — while the two TOTAL functions whose answer is the same for every
    /// integer past a bound (TEST-DATE-YYYYMMDD / TEST-DAY-YYYYDDD argument-1, FIND-STRING argument-3) still answer,
    /// with checking ON, and raise nothing (kb/Work PB254). <c>M30</c> is the inside-the-carrier control: the
    /// binary64 nearest 1.0E30 is the integer 1000000000000000019884624838656, whose remainder by 7 is 5.
    /// </summary>
    private const string Pb651PastTheCarrierProgram = """
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB651PASTCARRIER.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F39   USAGE FLOAT-LONG VALUE 1.0E39.
       01 FN39  USAGE FLOAT-LONG VALUE -1.0E39.
       01 F30   USAGE FLOAT-LONG VALUE 1.0E30.
       01 RM    PIC 9(4) VALUE 9999.
       01 RN    PIC 9(4) VALUE 9999.
       01 RV    PIC 9(2).
       PROCEDURE DIVISION.
       DECLARATIVES.
       EC-H SECTION. USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       EC-H-P.
           DISPLAY "EC=" FUNCTION EXCEPTION-STATUS
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           COMPUTE RM = FUNCTION MOD(F30 7)
           DISPLAY "M30=" RM
           COMPUTE RN = FUNCTION MOD(F39 7)
           DISPLAY "M39=" RN
           COMPUTE RV = FUNCTION TEST-DATE-YYYYMMDD(F39)
           DISPLAY "TD=" RV
           COMPUTE RV = FUNCTION TEST-DAY-YYYYDDD(FN39)
           DISPLAY "TY=" RV
           COMPUTE RM = FUNCTION FIND-STRING("abcabc" "c" START AFTER F39)
           DISPLAY "FS=" RM
           STOP RUN.
       """;

    /// <summary>§15.64.4: MOD(1000000000000000019884624838656, 7) = 5. §15.3: a value the implementation cannot represent
    /// at an integer argument of a function that depends on it is EC-ARGUMENT-FUNCTION (docs/CONFORMANCE.md
    /// DOC-A.1-124). §15.90.4 r1a / §15.91.4 r1a: 01 for every integer outside the window. §15.37.4 r2/r3: 0 once every
    /// match is ignored.</summary>
    [Fact]
    public void Pb651FloatPastTheCarrier_KeepsItsValueOrRaises_AndOnlyAConstantAnswerClamps()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Pb651PastTheCarrierProgram, 2023, permissive: true);
        Assert.True(ok, $"--permissive must accept the coercion: {string.Join("\n", errors)}");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1627");

        var (ranOk, stdout, detail) = EditionHarness.CompileAndRun(Pb651PastTheCarrierProgram, 2023, permissive: true);
        Assert.True(ranOk, detail);
        Assert.Equal(new[] { "M30=0005", "EC=EC-ARGUMENT-FUNCTION", "M39=9999", "TD=01", "TY=01", "FS=0000" },
            stdout.Replace("\r\n", "\n").TrimEnd('\n').Split('\n'));
    }

    /// <summary>Strict refuses it on the §15.3 type-6 screen, as it does every other program in this file —
    /// and the negative corpus pins the same verdict per function at
    /// <c>conformance-suite/cases/_unassigned/pb635-find-string-float-skip</c>.</summary>
    [Fact]
    public void Pb635FindString_IsRejectedUnderStrict()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Pb635FindStringProgram, 2023);
        Assert.False(ok, "a floating-point operand at an ISO §15.3 type-6 position is not conforming");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET1627");
        EditionHarness.AssertHasDiagnostic(errors, "FUNCTION FIND-STRING argument-3");
    }
}
