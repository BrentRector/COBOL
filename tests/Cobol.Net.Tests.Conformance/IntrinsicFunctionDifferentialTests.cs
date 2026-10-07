// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The intrinsic-function catalog (ISO §15; COBOLNET_INTRINSICS_DESIGN spine 1; the Phase-1B brief). Two nets:
/// <list type="bullet">
///   <item><b>Differential</b> — pinned to the legacy oracle where it is sound (float math on values away from
///         quantization boundaries, string functions, statistics): the 42-program NIST IF suite is the bulk net;
///         these cover the channel matrix (COMPUTE / IF / EVALUATE / MOVE) compactly.</item>
///   <item><b>Spec-pinned</b> — where the legacy diverges from the standard or the value IS the spec: the §15.64.4
///         MOD sign table, §15.61.4 MEDIAN, the §15.5.2 integer-date epoch, NUMVAL/NUMVAL-C formats (§15.67/68),
///         CHAR/ORD ordinals (§15.15/§15.70), the FromDouble ROUNDING choice (hazard H2 — the legacy truncates the
///         LOG10(1000) double artifact to 2.999999; rounding is the better §15.4.1 approximation), D8 edition
///         gating, and the loud-failure doctrine for the named uncovered channels (hazard H3).</item>
/// </list>
/// </summary>
public sealed class IntrinsicFunctionDifferentialTests
{
    private static readonly CobolNetCompiler CobolNet = new();

    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static void AssertSpec(string source, string expected, int dialect = 85)
    {
        var (cok, cout, cdetail) = new CobolNetCompiler(dialect).CompileAndRun(source);
        Assert.True(cok, $"WiseOwl COBOL failed: {cdetail}");
        Assert.Equal(CutRunner.Normalize(expected), cout);
    }

    /// <summary>The counterpart of <see cref="AssertSpec"/> for a program the spec REQUIRES be rejected: the
    /// compile must fail, and it must fail with the NAMED diagnostic rather than incidentally. Asserting only
    /// "did not compile" would pass on a typo in the fixture, which is the failure mode this shape exists to
    /// avoid — the same reason the negative corpus pins an expected diagnostic substring per case.</summary>
    private static void AssertRejected(string source, string expectedCode, int dialect = 85)
    {
        var (cok, cout, cdetail) = new CobolNetCompiler(dialect).CompileAndRun(source);
        Assert.False(cok, $"expected rejection ({expectedCode}); the program compiled and produced: {cout}");
        Assert.Contains(expectedCode, cdetail + cout, StringComparison.Ordinal);
    }

    private static string Program(string ws, string proc, string id = "IFTEST") => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {id}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    // ── Differential: float math away from quantization boundaries (§15.4.1 approximation license) ──────────

    [Theory]
    [InlineData("COMPUTE R = FUNCTION SQRT(625).")]                     // 25 exact
    [InlineData("COMPUTE R = FUNCTION SQRT(2.25).")]                    // 1.5 exact
    [InlineData("COMPUTE R = FUNCTION SIN(0).")]                        // 0
    [InlineData("COMPUTE R = FUNCTION COS(0).")]                        // 1
    [InlineData("COMPUTE R = FUNCTION ATAN(0).")]                       // 0
    [InlineData("COMPUTE R = FUNCTION ACOS(1).")]                       // 0
    [InlineData("COMPUTE R = FUNCTION LOG10(100).")]                    // 2 (double-exact)
    [InlineData("COMPUTE R = FUNCTION ANNUITY(0, 4).")]                 // §15.9.4 rate 0 ⇒ 1/n = 0.25 exact
    [InlineData("COMPUTE R = FUNCTION SQRT(2) * 0.")]                   // intrinsic inside a larger expression
    public void FloatFamily_MatchesLegacy(string stmt) =>
        AssertMatchesGolden(Program("01 R PIC S9(5)V9(4).", $"    {stmt}\n    DISPLAY R."));

    // ── Differential: exact statistics + the channel matrix ─────────────────────────────────────────────────

    [Theory]
    [InlineData("01 R PIC S9(5)V9(4).", "COMPUTE R = FUNCTION SUM(1.5, 2.25, 3).")]         // §15.88 — scale-aligned Σ
    [InlineData("01 R PIC S9(5)V9(4).", "COMPUTE R = FUNCTION MEAN(1, 2, 4).")]             // §15.60 — Σ/n
    [InlineData("01 R PIC S9(5)V9(4).", "COMPUTE R = FUNCTION MAX(-4.3, 10.2, -0.7, 3.9).")]
    [InlineData("01 R PIC S9(5)V9(4).", "COMPUTE R = FUNCTION MIN(7, -2, 19).")]
    [InlineData("01 R PIC S9(5)V9(4).", "COMPUTE R = FUNCTION RANGE(3, 11, 7).")]           // §15.76 max − min
    [InlineData("01 R PIC S9(5)V9(4).", "COMPUTE R = FUNCTION MIDRANGE(2, 10).")]           // §15.62 (min+max)/2
    [InlineData("01 R PIC S9(9).", "COMPUTE R = FUNCTION FACTORIAL(9).")]                   // §15.36 — 362880
    [InlineData("01 R PIC S9(9).", "COMPUTE R = FUNCTION INTEGER(-1.5).")]                  // §15.44 floor → −2
    [InlineData("01 R PIC S9(9).", "COMPUTE R = FUNCTION INTEGER-PART(-1.5).")]             // §15.49 truncate → −1
    public void ExactFamily_MatchesLegacy(string ws, string stmt) =>
        AssertMatchesGolden(Program(ws, $"    {stmt}\n    DISPLAY R."));

    [Fact]
    public void ChannelMatrix_IfEvaluateMove_MatchesLegacy() =>
        // The three non-COMPUTE channels the NIST suite exercises: a FUNCTION in an IF relation, as an EVALUATE
        // subject, and as a MOVE source (§15.2 — usable wherever a sending item of its category is).
        AssertMatchesGolden(Program("01 R PIC S9(5)V9(4).\n01 T PIC X(5).", """
                IF FUNCTION SQRT(625) = 25 DISPLAY "REL-OK" END-IF.
                EVALUATE FUNCTION MAX(3, 9, 4)
                    WHEN 9 DISPLAY "EVAL-OK"
                    WHEN OTHER DISPLAY "EVAL-BAD"
                END-EVALUATE.
                MOVE FUNCTION REVERSE("ABCDE") TO T.
                DISPLAY T.
            """));

    [Fact]
    public void TableAllExpansion_MatchesLegacy() =>
        // table(ALL): each occurrence becomes a separate argument, left to right (§15.3).
        AssertMatchesGolden(Program("""
            01 P PIC S9(4) VALUE 2.
            01 ARR VALUE "40537".
                02 IND OCCURS 5 TIMES PIC 9.
            01 R PIC S9(5)V9(4).
            """, """
                COMPUTE R = FUNCTION SUM(IND(ALL)).
                DISPLAY R.
                COMPUTE R = FUNCTION MAX(IND(ALL)).
                DISPLAY R.
                COMPUTE R = FUNCTION ORD-MAX(IND(ALL)).
                DISPLAY R.
                COMPUTE R = FUNCTION MEDIAN(IND(ALL)).
                DISPLAY R.
                COMPUTE R = FUNCTION SUM(IND(ALL)) + IND(P).
                DISPLAY R.
            """));

    [Theory]
    // SPEC-PINNED, not differential: the bracketed DISPLAY exposes the legacy's known non-conformance (it trims
    // an alphanumeric operand's trailing spaces, contra ISO §14.9.11.4 GR6 — "the size … is the sum of the sizes
    // of the operands"); the X(8) receiver's full padded image is the correct output.
    [InlineData("MOVE FUNCTION UPPER-CASE(\"hello9z\") TO T.", "[HELLO9Z ]")]   // §15.97
    [InlineData("MOVE FUNCTION LOWER-CASE(\"HELLO9Z\") TO T.", "[hello9z ]")]   // §15.57
    [InlineData("MOVE FUNCTION REVERSE(\"abc 12\") TO T.", "[21 cba  ]")]       // §15.78
    public void StringFamily_PinnedToSpec(string stmt, string expected) =>
        AssertSpec(Program("01 T PIC X(8).", $"    {stmt}\n    DISPLAY \"[\" T \"]\"."), expected);

    // ── Spec-pinned: the §15.64.4 MOD / §15.77.4 REM sign tables ─────────────────────────────────────────────

    [Theory]
    [InlineData("11, 5", "+0001")]     // the §15.64.4 NOTE table, row by row
    [InlineData("-11, 5", "+0004")]
    [InlineData("11, -5", "-0004")]
    [InlineData("-11, -5", "-0001")]
    public void Mod_SignTable_PinnedToSpec(string args, string expected) =>
        AssertSpec(Program("01 R PIC S9(4) SIGN LEADING SEPARATE.",
            $"    COMPUTE R = FUNCTION MOD({args}).\n    DISPLAY R."), expected);

    [Fact]
    public void Rem_TruncatedRemainder_PinnedToSpec() =>
        // §15.77.4: a − b × INTEGER-PART(a/b) — sign follows the dividend; fractional operands are class numeric.
        AssertSpec(Program("01 R PIC S9(4)V9 SIGN LEADING SEPARATE.", """
                COMPUTE R = FUNCTION REM(-11, 5).
                DISPLAY R.
                COMPUTE R = FUNCTION REM(7.5, 2).
                DISPLAY R.
            """), "-00010\n+00015");

    [Fact]
    public void Median_EvenCount_MeanOfMiddles_PinnedToSpec() =>
        // §15.61.4 rule 2: an even argument count returns (b + c) / 2 of the two middle values — exact at one
        // extra fraction digit (the renderer's ×10/2 discipline).
        AssertSpec(Program("01 R PIC 9(3)V9(2).",
            "    COMPUTE R = FUNCTION MEDIAN(1, 2, 3, 4).\n    DISPLAY R."), "00250");

    [Fact]
    public void OrdMax_TieTakesFirst_PinnedToSpec() =>
        // §15.71.4: the ordinal of the GREATEST argument; the strictly-greater scan keeps the FIRST of equals.
        AssertSpec(Program("01 R PIC 9(3).",
            "    COMPUTE R = FUNCTION ORD-MAX(3, 7, 7, 2).\n    DISPLAY R."), "002");

    // ── Spec-pinned: hazard H2 — FromDouble ROUNDS at the quantization point ────────────────────────────────

    [Fact]
    public void Log10_1000_RoundsTheDoubleArtifact_PinnedToSpec() =>
        // Math.Log10(1000) = 2.9999999999999996 in IEEE double. §15.4.1 licenses an implementor-defined
        // APPROXIMATION; rounding inside the ONE FromDouble yields the true value 3 (the legacy's truncation
        // yields 2.999999 — conforming but strictly worse; hazard H2). The greenfield value is pinned.
        AssertSpec(Program("01 R PIC 9V9(6).",
            "    COMPUTE R = FUNCTION LOG10(1000).\n    DISPLAY R."), "3000000");

    [Fact]
    public void OutOfDomain_EcDefaultZero_PinnedToSpec() =>
        // §15.3: with EC-ARGUMENT-FUNCTION checking disabled "the implementor defines the result" — this
        // implementation returns 0 (NaN → 0 in FromDouble; the legacy-compatible default the goldens encode).
        AssertSpec(Program("01 R PIC S9(4)V9(2) SIGN LEADING SEPARATE.", """
                COMPUTE R = FUNCTION SQRT(0 - 4).
                DISPLAY R.
                COMPUTE R = FUNCTION ACOS(2).
                DISPLAY R.
                COMPUTE R = FUNCTION LOG(0).
                DISPLAY R.
                COMPUTE R = FUNCTION FACTORIAL(0 - 1).
                DISPLAY R.
            """), "+000000\n+000000\n+000000\n+000000");

    // ── Spec-pinned: NUMVAL / NUMVAL-C formats (§15.67.3 / §15.68.3) ─────────────────────────────────────────

    [Theory]
    [InlineData("\"  -123.45 \"", "-0012345")]      // leading sign, surrounding spaces (format 1)
    [InlineData("\"   -  929.03\"", "-0092903")]    // spaces between sign and digits — ignored before the first digit (r2)
    [InlineData("\"82.93+\"", "+0008293")]          // trailing sign (format 2)
    [InlineData("\"12cr\"", "-0001200")]            // CR suffix, case-insensitive (§15.67.3 r1) ⇒ negative (§15.67.4 r2)
    [InlineData("\".5\"", "+0000050")]              // the ". digit" alternative
    [InlineData("\"1O2\"", "+0000000")]             // malformed (letter O) → EC-ARGUMENT default 0 (§15.3)
    public void Numval_Formats_PinnedToSpec(string arg, string expected) =>
        AssertSpec(Program("01 R PIC S9(5)V9(2) SIGN LEADING SEPARATE.",
            $"    COMPUTE R = FUNCTION NUMVAL({arg}).\n    DISPLAY R."), expected);

    [Theory]
    [InlineData("\"$1,234.56\"", "+0123456")]       // default currency + grouping separators ignored (§15.68.4 r2)
    [InlineData("\"- $ 890.05\"", "-0089005")]      // sign before currency with space-strings (§15.68.3 r4a)
    [InlineData("\"Z93,021\", \"Z\"", "+9302100")]  // argument-2 names the currency string (§15.68.3 r2)
    public void NumvalC_Formats_PinnedToSpec(string args, string expected) =>
        AssertSpec(Program("01 R PIC S9(5)V9(2) SIGN LEADING SEPARATE.",
            $"    COMPUTE R = FUNCTION NUMVAL-C({args}).\n    DISPLAY R."), expected);

    [Fact]
    public void NumvalC_DefaultCurrencyFromSpecialNames_PinnedToSpec() =>
        // §15.68.3 r3: with argument-2 omitted, the compilation unit's ONE currency string applies — the
        // SPECIAL-NAMES CURRENCY SIGN literal, injected by the binder at bind time.
        AssertSpec("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NVCCUR.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            SPECIAL-NAMES.
                CURRENCY SIGN IS "F".
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 R PIC S9(5)V9(2) SIGN LEADING SEPARATE.
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE R = FUNCTION NUMVAL-C("F12.50").
                DISPLAY R.
                STOP RUN.
            """, "+0001250");

    // ── Spec-pinned: CHAR / ORD ordinals + LENGTH fold + nesting ─────────────────────────────────────────────

    [Fact]
    public void CharOrd_NativeOrdinals_PinnedToSpec() =>
        // §15.15.4: CHAR(n) is the character in ordinal position n of the collating sequence (native: code n−1,
        // so CHAR(66) = 'A'); §15.70.4: ORD is its inverse; nesting must round-trip (the §15.3 nested-FUNCTION
        // argument shape).
        AssertSpec(Program("01 T PIC X(1).\n01 R PIC 9(3).", """
                MOVE FUNCTION CHAR(66) TO T.
                DISPLAY T.
                COMPUTE R = FUNCTION ORD("A").
                DISPLAY R.
                COMPUTE R = FUNCTION ORD(FUNCTION CHAR(42)).
                DISPLAY R.
            """), "A\n066\n042");

    [Fact]
    public void LengthFold_CharacterPositions_PinnedToSpec() =>
        // §15.50.4: length in character positions — a literal's size; a numeric item's digit positions (an
        // over-punched sign occupies none, a SEPARATE sign one, §13.18.52); a group's leaf sum; and
        // LENGTH(REVERSE(x)) = LENGTH(x) via the fixed-width image (D7).
        AssertSpec(Program("""
            01 N9 PIC S9(5)V9(3).
            01 NSEP PIC S9(3) SIGN TRAILING SEPARATE.
            01 G.
                02 A1 PIC X(7).
                02 A2 PIC 9(4) OCCURS 3 TIMES.
            01 R PIC 9(3).
            """, """
                COMPUTE R = FUNCTION LENGTH("ABCD").
                DISPLAY R.
                COMPUTE R = FUNCTION LENGTH(N9).
                DISPLAY R.
                COMPUTE R = FUNCTION LENGTH(NSEP).
                DISPLAY R.
                COMPUTE R = FUNCTION LENGTH(G).
                DISPLAY R.
                COMPUTE R = FUNCTION LENGTH(FUNCTION REVERSE("ABCDEFG")).
                DISPLAY R.
            """), "004\n008\n004\n019\n007");

    // ── Spec-pinned: the date/time family ────────────────────────────────────────────────────────────────────

    [Fact]
    public void IntegerDateForm_Epoch_PinnedToSpec() =>
        // §15.5.2: integer date 1 = Monday 1601-01-01; INTEGER-OF-DATE(99991231) = 3,067,671. Invalid calendar
        // dates → the EC-ARGUMENT default 0 (§15.3).
        AssertSpec(Program("01 R PIC 9(8).", """
                COMPUTE R = FUNCTION INTEGER-OF-DATE(16010101).
                DISPLAY R.
                COMPUTE R = FUNCTION DATE-OF-INTEGER(1).
                DISPLAY R.
                COMPUTE R = FUNCTION DAY-OF-INTEGER(1).
                DISPLAY R.
                COMPUTE R = FUNCTION INTEGER-OF-DATE(99991231).
                DISPLAY R.
                COMPUTE R = FUNCTION INTEGER-OF-DAY(FUNCTION DAY-OF-INTEGER(150000)).
                DISPLAY R.
                COMPUTE R = FUNCTION INTEGER-OF-DATE(20230230).
                DISPLAY R.
            """), "00000001\n16010101\n01601001\n03067671\n00150000\n00000000");

    [Fact]
    public void WhenCompiled_CompileTimeConstant_PinnedToSpec() =>
        // §15.99.3 r2: WHEN-COMPILED is the COMPILATION timestamp (baked constant) — never after CURRENT-DATE
        // at run time, and in the §15.21.3 21-character layout (4-digit year sanity-checked via ref-mod).
        AssertSpec(Program("01 T PIC X(21).", """
                MOVE FUNCTION WHEN-COMPILED TO T.
                IF T(1:2) = "20" DISPLAY "CENTURY-OK" ELSE DISPLAY "CENTURY-BAD " T END-IF.
                IF FUNCTION CURRENT-DATE >= FUNCTION WHEN-COMPILED
                    DISPLAY "ORDER-OK"
                ELSE DISPLAY "ORDER-BAD" END-IF.
            """), "CENTURY-OK\nORDER-OK");

    [Fact]
    public void Random_SeededSequence_PinnedToSpec()
    {
        // §15.75.3 r3/r5 + §15.75.4 r1/r2: a seeded reference restarts the sequence (same seed ⇒ same first
        // value), argument-less references continue it, and every value is in [0, 1).
        var (ok, stdout, detail) = CobolNet.CompileAndRun(Program(
            "01 R1 PIC V9(9).\n01 R2 PIC V9(9).\n01 R3 PIC V9(9).", """
                COMPUTE R1 = FUNCTION RANDOM(7).
                COMPUTE R2 = FUNCTION RANDOM.
                COMPUTE R3 = FUNCTION RANDOM(7).
                IF R1 = R3 DISPLAY "SEED-REPEAT-OK" ELSE DISPLAY "SEED-REPEAT-BAD" END-IF.
                IF R2 NOT = R1 DISPLAY "NEXT-DIFFERS-OK" ELSE DISPLAY "NEXT-DIFFERS-BAD" END-IF.
            """));
        Assert.True(ok, detail);
        Assert.Equal("SEED-REPEAT-OK\nNEXT-DIFFERS-OK", stdout);
    }

    // ── D8 edition gating + the loud-failure doctrine (hazards H3/H6) ────────────────────────────────────────

    [Fact]
    public void EditionGate_2014FunctionAt85_RejectedByName()
    {
        // D8: outside its window a function is rejected with a diagnostic naming the function and editions
        // (TRIM was introduced by ISO/IEC 1989:2014 — catalog row; the 1989-module rows bind at 85).
        var (ok, _, detail) = new CobolNetCompiler(85).CompileAndRun(
            Program("01 T PIC X(5).", "    MOVE FUNCTION TRIM(\"  X  \") TO T.\n    DISPLAY T."));
        Assert.False(ok);
        Assert.Contains("TRIM", detail);
        Assert.Contains("COBOLNET1502", detail);
    }

    /// <summary>DISPLAY-OF/NATIONAL-OF (§15.26/§15.66) are 2002 introductions — the D8 per-name catalog window
    /// (the §1.1 ledger's catalog-driven mechanism, no constructs.json row) rejects them by name at --std 85.</summary>
    [Fact]
    public void EditionGate_RepertoireFunctionsAt85_RejectedByName()
    {
        var (ok, _, detail) = new CobolNetCompiler(85).CompileAndRun(
            Program("01 T PIC X(5).", "    MOVE FUNCTION DISPLAY-OF(T) TO T.\n    DISPLAY T."));
        Assert.False(ok);
        Assert.Contains("DISPLAY-OF", detail);
        Assert.Contains("COBOLNET1502", detail);
        (ok, _, detail) = new CobolNetCompiler(85).CompileAndRun(
            Program("01 T PIC X(5).", "    MOVE FUNCTION NATIONAL-OF(\"AB\") TO T.\n    DISPLAY T."));
        Assert.False(ok);
        Assert.Contains("NATIONAL-OF", detail);
        Assert.Contains("COBOLNET1502", detail);
    }

    /// <summary>The NATIONAL-OF result is CATEGORY NATIONAL (§15.66.1 "the type of the function is national" →
    /// §15.2 type 4), so §14.9.25.3 Table 16 bars moving it to an alphanumeric receiver (National row, AN
    /// column = No) — the former alphanumeric result-category fold would have let this compile.</summary>
    [Fact]
    public void NationalOf_ResultIsCategoryNational_MoveToAlphanumericRejected()
    {
        var (ok, _, detail) = new CobolNetCompiler(2002).CompileAndRun(
            Program("01 T PIC X(5).", "    MOVE FUNCTION NATIONAL-OF(\"AB\") TO T.\n    DISPLAY T."));
        Assert.False(ok);
        Assert.Contains("COBOLNET0819", detail);
    }

    [Fact]
    public void StagedLoudResidue_FailsLoud_NeverWrong()
    {
        // The §1.4 loud-failure doctrine — an unimplemented RESIDUE of a catalogued-and-otherwise-implemented
        // function compiles and fails LOUD at run time naming the feature, never a silent wrong value. (P11
        // drove the whole IntrinsicBind.Deferred backlog to zero, so there is no longer a fully-deferred
        // function; the doctrine is proven on a staged residue.)
        // ⛔ THE RESIDUE THIS TEST PINNED USED TO BE `BYTE-LENGTH(WS-X(1:2))` — a REFERENCE-MODIFIED argument —
        // and pinning it GREEN held a gap open (feedback_green_test_can_hold_a_gap_open): §15.14.3 r1 admits
        // "a data item of any class or category" and §8.4.3.3.4 GR6 makes a ref-mod a data item, so that shape
        // is a conforming reference with the answer 2 (kb/Work PB61, AR-15.14.3-1). It is now the POSITIVE
        // twin below. The one NAMED residue left in the LENGTH family is a runtime-length item INSIDE an OCCURS
        // DEPENDING table's element (a run-time multiplicity of components — VariableLengthGroupSum's documented
        // loud stage): that is the shape that proves the doctrine now, and it stays loud with the shape in its
        // message. (kb/Work PB244: the FIXED-OCCURS table of such elements is summed per occurrence — the
        // positive twin of this fact, below.)
        var (ok, output, detail) = new CobolNetCompiler(2023).CompileAndRun(
            Program("01 WS-X PIC X(5) VALUE \"ABCDE\".\n01 T PIC 9(4).",
                "    MOVE FUNCTION BYTE-LENGTH(WS-X(1:2)) TO T.\n    DISPLAY T."));
        Assert.True(ok, detail);
        Assert.Contains("0002", output);
        (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(
            Program("01 N PIC 9 VALUE 2.\n01 G.\n   05 TB OCCURS 1 TO 2 DEPENDING ON N.\n      10 D PIC X DYNAMIC LENGTH.\n01 T PIC 9(4).",
                "    MOVE FUNCTION LENGTH(G) TO T.\n    DISPLAY T."));
        Assert.False(ok);
        Assert.Contains("LENGTH", detail);
        Assert.Contains("runtime-length element", detail);
    }

    /// <summary>kb/Work PB244 — the positive twin of <see cref="StagedLoudResidue_FailsLoud_NeverWrong"/>: a
    /// dynamic-length item inside a FIXED-OCCURS table element is its own dynamic-length member per occurrence
    /// (ISO §8.5.1.12.1), so §15.50.4 r7 sums each occurrence's current length: here 2 + 1.</summary>
    [Fact]
    public void LengthOfAFixedTableOfVariableLengthElements_SumsEveryOccurrence()
    {
        var (ok, output, detail) = new CobolNetCompiler(2023).CompileAndRun(
            Program("01 G.\n   05 TB OCCURS 2.\n      10 D PIC X DYNAMIC LENGTH.\n01 T PIC 9(4).",
                "    MOVE \"ab\" TO D(1).\n    MOVE \"c\" TO D(2).\n    MOVE FUNCTION LENGTH(G) TO T.\n    DISPLAY T."));
        Assert.True(ok, detail);
        Assert.Contains("0003", output);
    }

    [Fact]
    public void NumericIntrinsicInStringContext_IsTable16sNonintegerSender()
    {
        // ⛔ THIS FACT HAS BEEN INVERTED TWICE. DA2 (DEVLOG 1104) turned "a NUMERIC-result intrinsic moved to an
        // alphanumeric receiver FAILS LOUD" (hazard H3, a staged-out channel) into "renders its value", arguing that
        // a function-identifier is an identifier and §14.9.25.4 GR6 defines the numeric→alphanumeric move. It did
        // not weigh §14.9.25.3 SR10 / Table 16, whose numeric ROW splits on Integer vs Noninteger, nor §15.2 items 4
        // and 5 (an INTEGER function has "no digits to the right of the decimal point"; a NUMERIC function is not so
        // described) nor §8.4.3.2.3 SR11 ("a numeric function shall not be specified where an integer operand is
        // required, even though a particular reference … might yield an integer value"). kb/Work PB73 (adjudicated
        // 2026-08-18): a NUMERIC function is the Noninteger sender — refused into an alphanumeric receiver on the
        // strict axis (COBOLNET0819) — and the DA2 admission (the CONFORMANCE.md item-92 text form) is exactly what
        // --permissive keeps, with a warning. An INTEGER function (LENGTH, ORD, …) was and is legal there.
        var (ok, _, detail) = CobolNet.CompileAndRun(
            Program("01 T PIC X(8).", "    MOVE FUNCTION NUMVAL(\"1\") TO T.\n    DISPLAY T."));
        Assert.False(ok, "a NUMERIC-typed function is Table 16's Noninteger sender (ISO §14.9.25.3 SR10, §15.2 item 4)");
        Assert.Contains("COBOLNET0819", detail);
        var (okP, output, detailP) = EditionHarness.CompileAndRun(
            Program("01 T PIC X(8).", "    MOVE FUNCTION NUMVAL(\"1\") TO T.\n    DISPLAY T."), 2023, permissive: true);
        Assert.True(okP, detailP);
        // ⛔ "1", NOT "1.000000" (kb/Work PB251). The permissive text is the item-92 LITERAL form of the value,
        // and NUMVAL's value is now the parsed value at its OWN scale in every arithmetic mode — §15.67.4 r1
        // fixes it ("The returned value is the numeric value represented by argument-1") with no mode
        // qualification, and §15.4.1's implementor latitude reaches only a function whose definition does NOT
        // otherwise specify the value. "1" has no fraction digits, so the value has scale 0 and the literal form
        // carries none. The old six were the native working scale's floor showing through the text — the same
        // floor that printed 0.123456 for FUNCTION NUMVAL("0.1234567").
        Assert.Equal("1", output.Trim());
    }

    // ── 2023 string intrinsics (Phase 5, DEVLOG 628): CONCAT §15.18, BASECONVERT §15.12 ─────────────────────

    [Fact]
    public void Concat_ConcatenatesArgumentImages_2023()
    {
        // §15.18.4 rule 1 — all characters of each argument (a fixed-width image includes its trailing padding);
        // rule 4 — variadic, left to right. A = PIC X(3) VALUE "AB" ⇒ image "AB " ⇒ CONCAT(A, B) = "AB CD".
        AssertSpec(Program(
            "01 A PIC X(3) VALUE \"AB\".\n           01 B PIC X(2) VALUE \"CD\".\n           01 R PIC X(8).",
            "    MOVE FUNCTION CONCAT(A, B) TO R.\n    DISPLAY R.\n"
            + "    MOVE FUNCTION CONCAT(\"X\", \"Y\", \"Z\") TO R.\n    DISPLAY R.", "IFCAT"),
            "AB CD\nXYZ", 2023);
    }

    [Fact]
    public void Concat_AdmitsNumericLiterals_AndFoldedNumericFunctions_2023()
    {
        // PB59 / RV-15.18.4-1 — §15.18.3 r1 lists class NUMERIC: a numeric literal concatenates as its own
        // characters (§15.18.4 r1), and a FOLDED nested numeric function (FUNCTION LENGTH of a fixed item
        // becomes a BoundNumericLiteral through IntrinsicBinder.OperandOf) reaches the same admitting arm —
        // both shapes used to compile clean and abort at run time.
        AssertSpec(Program(
            "01 A PIC X(3) VALUE \"AB\".\n           01 B PIC X(2) VALUE \"CD\".\n           01 R PIC X(8).",
            "    MOVE FUNCTION CONCAT(A, 12) TO R.\n    DISPLAY R.\n"
            + "    MOVE FUNCTION CONCAT(A, FUNCTION LENGTH(B)) TO R.\n    DISPLAY R.", "IFCATN"),
            "AB 12\nAB 2", 2023);
    }

    [Theory]
    [InlineData("\"FF\", 16, 10", "255")]     // §15.12.4 — base 16 → base 10
    [InlineData("\"255\", 10, 16", "FF")]     //           base 10 → base 16
    [InlineData("\"1010\", 2, 16", "A")]      //           base 2  → base 16 (10 = A)
    [InlineData("\"0\", 10, 2", "0")]         //           zero — one digit CONSUMED (the digitless guard keys on the count, never the accumulator)
    [InlineData("\"FF  \", 16, 10", "255")]   // PB59 — the TRAILING fixed-width image pad is not content (TrimEnd(' '))
    [InlineData("255, 10, 16", "FF")]         // PB59 / RV-15.12.4-1 — an UNQUOTED numeric literal argument-1 (§15.12.3 r1
                                              //   admits "an unsigned integer … literal" below base 11; used to abort at run time)
    public void BaseConvert_ReExpressesInTargetBase_2023(string args, string expected)
        => AssertSpec(Program("01 R PIC X(10).",
            $"    MOVE FUNCTION BASECONVERT({args}) TO R.\n    DISPLAY R.", "IFBASE"), expected, 2023);

    [Theory]
    [InlineData("\"     \", 16, 10")]         // PB59 / AR-15.12.3-2 — DIGITLESS: the old value-keyed guard fabricated "0"
    [InlineData("\"  FF\", 16, 10")]          // PB59 / §15.12.3 r2 — a LEADING space is content, not padding
    [InlineData("\"ff\", 16, 10")]            // PB59 / §15.12.3 r2 + §8.1.3.1 Table 1 — lowercase is not a basic letter
    public void BaseConvert_InvalidDigits_EcArgumentDefault_2023(string args)
        // Checking OFF: EC-ARGUMENT-FUNCTION is set and the §15.3 default (zero-length) stands — the receiver
        // keeps only its space fill, so the display line is empty after Normalize's trailing trim.
        => AssertSpec(Program("01 R PIC X(10).",
            $"    MOVE FUNCTION BASECONVERT({args}) TO R.\n    DISPLAY \"R=\" R.", "IFBASEE"), "R=", 2023);

    [Fact]
    public void BaseConvert_InvalidDigits_FatalWhenChecked_2023()
    {
        // §15.3 + §14.6.13.1.6 — EC-ARGUMENT-FUNCTION is FATAL under enabled checking: the run terminates.
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(Program("01 R PIC X(10).",
            ">>TURN EC-ARGUMENT-FUNCTION CHECKING ON\n    MOVE FUNCTION BASECONVERT(\"  FF\", 16, 10) TO R.\n    DISPLAY R."));
        Assert.False(ok, "a leading space in argument-1 violates §15.12.3 r2 and must be fatal under checking");
        Assert.Contains("EC-ARGUMENT-FUNCTION", detail);
    }

    [Fact]
    public void BaseConvert_EqualDataItemBases_EcArgumentDefault_2023()
        // §15.12.3 r1 — the bases shall have "unequal values": DATA-ITEM bases are the runtime twin of the
        // compile-time equal-literals screen (PB59 / AR-15.12.3-1 leg b — two equal LITERALS are COBOLNET1642
        // at bind, negative pb59-baseconvert-equal-bases). Checking off: EC + the §15.3 zero-length default.
        => AssertSpec(Program("01 B2 PIC 99 VALUE 16.\n           01 B3 PIC 99 VALUE 16.\n           01 R PIC X(6).",
            "    MOVE FUNCTION BASECONVERT(\"FF\", B2, B3) TO R.\n    DISPLAY \"R=\" R.", "IFBASEQ"), "R=", 2023);

    [Fact]
    public void Convert_HexSource_TrailingPadTrimmed_2023()
        // PB59 — the trailing fixed-width image pad is not content; the digits convert (the previously
        // unpinned padded-but-valid case: every prior golden's HEX item was exactly filled).
        => AssertSpec(Program("01 H PIC X(4) VALUE \"41  \".\n           01 R PIC X(8).",
            "    MOVE FUNCTION CONVERT(H HEX ANUM) TO R.\n    DISPLAY \"R=\" R.", "IFCONVP"), "R=A", 2023);

    [Fact]
    public void Convert_HexSource_LeadingSpaceIsContent_FatalWhenChecked_2023()
    {
        // PB59 / AR-15.19.3-4 — a leading space is CONTENT (§15.19.3 r4: not a hexadecimal digit), fatal
        // EC-ARGUMENT-FUNCTION under enabled checking. (Checking off keeps the documented zero-fill result.)
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(Program(
            "01 H PIC X(4) VALUE \"  41\".\n           01 R PIC X(8).",
            ">>TURN EC-ARGUMENT-FUNCTION CHECKING ON\n    MOVE FUNCTION CONVERT(H HEX ANUM) TO R.\n    DISPLAY R."));
        Assert.False(ok, "a leading space in a HEX source violates §15.19.3 r4 and must be fatal under checking");
        Assert.Contains("EC-ARGUMENT-FUNCTION", detail);
    }

    [Fact]
    public void Convert_AllSpaceHexSource_FatalWhenChecked_2023()
    {
        // PB59 — an all-space HEX source has NO digits (§15.19.3 r4; NOT r1 — the ITEM is not zero length).
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(Program(
            "01 H PIC X(4) VALUE SPACES.\n           01 R PIC X(8).",
            ">>TURN EC-ARGUMENT-FUNCTION CHECKING ON\n    MOVE FUNCTION CONVERT(H HEX ANUM) TO R.\n    DISPLAY R."));
        Assert.False(ok, "a digitless HEX source violates §15.19.3 r4 and must be fatal under checking");
        Assert.Contains("EC-ARGUMENT-FUNCTION", detail);
    }

    [Fact]
    public void Concat_And_BaseConvert_GatedBelow2023_1502()
    {
        // Both are 2023 introductions (D8 edition window) — the binder names the edition (COBOLNET1502) at 2014.
        foreach (string call in new[] { "FUNCTION CONCAT(\"A\", \"B\")", "FUNCTION BASECONVERT(\"F\", 16, 10)" })
        {
            var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
                Program("01 R PIC X(8).", $"    MOVE {call} TO R.\n    DISPLAY R."));
            Assert.False(ok, $"{call} is 2023+; 2014 must reject");
            Assert.Contains("COBOLNET1502", detail);
        }
    }

    // ── TRIM (§15.96, Phase 5, DEVLOG 629): LEADING/TRAILING/both + the 2023 argument-2 char set ─────────────

    [Theory]
    [InlineData("FUNCTION TRIM(S)", "HELLO")]            // §15.96.4 r3 — both, default space (the 2014 form)
    [InlineData("FUNCTION TRIM(S LEADING)", "HELLO")]    // r1 — leading (the trailing padding is Normalize-trimmed)
    [InlineData("FUNCTION TRIM(S TRAILING)", "  HELLO")] // r2 — trailing (leading spaces are preserved)
    public void Trim_SpaceForm_2014(string call, string expected)
        => AssertSpec(Program("01 S PIC X(10) VALUE \"  HELLO   \".\n           01 R PIC X(12).",
            $"    MOVE {call} TO R.\n    DISPLAY R.", "IFTRIM"), expected, 2014);

    [Fact]
    public void Trim_ArgumentTwoCharSet_2023()
        // §15.96 argument-2 (delete a specified character) — the 2023 enhancement. Z = "0042" ⇒ leading "0" ⇒ "42".
        => AssertSpec(Program("01 Z PIC X(8) VALUE \"0042\".\n           01 R PIC X(12).",
            "    MOVE FUNCTION TRIM(Z LEADING \"0\") TO R.\n    DISPLAY R.", "IFTRIM2"), "42", 2023);

    [Fact]
    public void Trim_ArgumentTwo_GatedBelow2023_1502_ButSpaceFormBinds()
    {
        // The argument-2 form is a 2023 enhancement (Annex E.3.3 item 31) — rejected at 2014 by name+edition…
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 Z PIC X(8) VALUE \"0042\".\n           01 R PIC X(8).",
                "    MOVE FUNCTION TRIM(Z LEADING \"0\") TO R.\n    DISPLAY R."));
        Assert.False(ok, "TRIM argument-2 is 2023+; 2014 must reject");
        Assert.Contains("COBOLNET1502", detail);
        // …but TRIM itself (the space-trimming form) is 2014, so it binds+runs there.
        var (ok2, out2, d2) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 S PIC X(8) VALUE \"  HI  \".\n           01 R PIC X(8).",
                "    MOVE FUNCTION TRIM(S TRAILING) TO R.\n    DISPLAY R."));
        Assert.True(ok2, d2);
        Assert.Equal("  HI", out2);
    }

    // ── FIND-STRING (§15.37, Phase 5, DEVLOG 630): substring position; LAST / START AFTER argument-3 / ANYCASE ─

    [Theory]
    [InlineData("FUNCTION FIND-STRING(H N)", "1")]                     // §15.37.4 r1 — first occurrence
    [InlineData("FUNCTION FIND-STRING(H N LAST)", "7")]                // r1 — last occurrence (positions 1,4,7)
    [InlineData("FUNCTION FIND-STRING(H N START AFTER 1)", "4")]       // r2 — ignore 1 match from the first
    [InlineData("FUNCTION FIND-STRING(H N LAST START AFTER 1)", "4")]  // r1+r2 — 1 before the last
    [InlineData("FUNCTION FIND-STRING(H \"ZZ\")", "0")]                // r3 — no match
    // kb/Work R08 (Phase-B F25): argument-3 stays in the LONG domain. An unchecked (int) narrowing let any
    // skip whose LOW 32 BITS fell in 0..Count-1 return a match — 2^32 printed 1 (and 7 for LAST) where r2
    // exhausts the matches and r3 requires 0; 2^31 and 2^32-1 missed the wrap by luck, which is why sampling
    // never caught it.
    [InlineData("FUNCTION FIND-STRING(H N START AFTER 4294967296)", "0")]        // r2+r3 — 2^32 wrapped to 0
    [InlineData("FUNCTION FIND-STRING(H N LAST START AFTER 4294967296)", "0")]   // the LAST arm printed 7
    [InlineData("FUNCTION FIND-STRING(H N START AFTER 4294967299)", "0")]        // 2^32+3: low bits 3 = Count
    public void FindString_Positions_2023(string call, string expected)
        => AssertSpec(Program("01 H PIC X(9) VALUE \"ABCABCABC\".\n           01 N PIC X(3) VALUE \"ABC\".\n           01 P PIC 9.",
            $"    MOVE {call} TO P.\n    DISPLAY P.", "IFFIND"), expected, 2023);

    [Fact]
    public void FindString_Anycase_FoldsCase_2023()
        // §15.37.4 r4 — ANYCASE folds case per LOWER-CASE; "WORLD" matches "World" at position 7.
        => AssertSpec(Program("01 T PIC X(11) VALUE \"Hello World\".\n           01 P PIC 9.",
            "    MOVE FUNCTION FIND-STRING(T \"WORLD\" ANYCASE) TO P.\n    DISPLAY P.", "IFFINDA"), "7", 2023);

    [Fact]
    public void FindString_CaseSensitiveByDefault_2023()
        // Without ANYCASE the match is ordinal — "WORLD" (upper) does not occur in "Hello World" ⇒ 0 (r3).
        => AssertSpec(Program("01 T PIC X(11) VALUE \"Hello World\".\n           01 P PIC 9.",
            "    MOVE FUNCTION FIND-STRING(T \"WORLD\") TO P.\n    DISPLAY P.", "IFFINDC"), "0", 2023);

    [Fact]
    public void FindString_GatedBelow2023_1502()
    {
        // FIND-STRING is a 2023 addition (§15.37) — rejected by name+edition below 2023.
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 H PIC X(3) VALUE \"ABC\".\n           01 P PIC 9.",
                "    MOVE FUNCTION FIND-STRING(H \"B\") TO P.\n    DISPLAY P."));
        Assert.False(ok, "FIND-STRING is 2023+; 2014 must reject");
        Assert.Contains("COBOLNET1502", detail);
    }

    [Theory]
    [InlineData("FUNCTION FIND-STRING(H2 N2)", "1")]              // first of the OVERLAPPING matches
    [InlineData("FUNCTION FIND-STRING(H2 N2 LAST)", "3")]         // §15.37.4 r1 — "aa" in "aaaa" matches at 1,2,3; LAST ⇒ 3
    [InlineData("FUNCTION FIND-STRING(H2 N2 START AFTER 1)", "2")]// skip 1 overlapping match ⇒ 2
    public void FindString_OverlappingOccurrences_2023(string call, string expected)
        => AssertSpec(Program("01 H2 PIC X(4) VALUE \"aaaa\".\n           01 N2 PIC X(2) VALUE \"aa\".\n           01 P PIC 9.",
            $"    MOVE {call} TO P.\n    DISPLAY P.", "IFFOV"), expected, 2023);

    // ── SUBSTITUTE (§15.87, Phase 5, DEVLOG 631): per-pair replacement; ANYCASE / FIRST / LAST; multi-pair ────

    [Theory]
    [InlineData("FUNCTION SUBSTITUTE(S \"A\" \"X\")", "XBXBXB")]           // §15.87.4 r3 — all occurrences
    [InlineData("FUNCTION SUBSTITUTE(S FIRST \"A\" \"X\")", "XBABAB")]     // r3.a — first only
    [InlineData("FUNCTION SUBSTITUTE(S LAST \"A\" \"X\")", "ABABXB")]      // r3.b — last only
    [InlineData("FUNCTION SUBSTITUTE(S \"AB\" \"WXYZ\")", "WXYZWXYZWXYZ")] // growing replacement, all
    public void Substitute_SinglePair_2023(string call, string expected)
        => AssertSpec(Program("01 S PIC X(6) VALUE \"ABABAB\".\n           01 R PIC X(12).",
            $"    MOVE {call} TO R.\n    DISPLAY R.", "IFSUBS"), expected, 2023);

    [Fact]
    public void Substitute_Anycase_FoldsCase_2023()
        // §15.87.4 r5 — ANYCASE folds case; every a/A in "aAaA" becomes "-".
        => AssertSpec(Program("01 S PIC X(4) VALUE \"aAaA\".\n           01 R PIC X(8).",
            "    MOVE FUNCTION SUBSTITUTE(S ANYCASE \"a\" \"-\") TO R.\n    DISPLAY R.", "IFSUBA"), "----", 2023);

    [Fact]
    public void Substitute_MultiPair_OnePass_2023()
        // §15.87.4 r3/r4 — two pairs applied in one left-to-right pass: "CAT DOG" ⇒ "FISH BIRD".
        => AssertSpec(Program("01 S PIC X(7) VALUE \"CAT DOG\".\n           01 R PIC X(12).",
            "    MOVE FUNCTION SUBSTITUTE(S \"CAT\" \"FISH\" \"DOG\" \"BIRD\") TO R.\n    DISPLAY R.", "IFSUBM"),
            "FISH BIRD", 2023);

    [Fact]
    public void Substitute_GatedBelow2023_1502()
    {
        // SUBSTITUTE is a 2023 addition (§15.87) — rejected by name+edition below 2023.
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 S PIC X(3) VALUE \"ABC\".\n           01 R PIC X(3).",
                "    MOVE FUNCTION SUBSTITUTE(S \"B\" \"X\") TO R.\n    DISPLAY R."));
        Assert.False(ok, "SUBSTITUTE is 2023+; 2014 must reject");
        Assert.Contains("COBOLNET1502", detail);
    }

    // ── CONVERT (§15.19, Phase 5, DEVLOG 632): data-representation conversion; source/destination formats ─────

    [Theory]
    [InlineData("01 A PIC X VALUE \"A\".\n           01 R PIC X(4).", "CONVERT(A ANUM ANUM HEX)", "41")]   // NOTE 3a
    [InlineData("01 H PIC XX VALUE \"41\".\n           01 R PIC X(4).", "CONVERT(H HEX ANUM)", "A")]         // NOTE 3b
    [InlineData("01 H PIC XX VALUE \"41\".\n           01 R PIC X(4).", "CONVERT(H HEX BYTE)", "A")]         // r5
    [InlineData("01 A PIC XXX VALUE \"AB\".\n           01 R PIC X(8).", "CONVERT(A ANUM ANUM HEX)", "414220")] // r2, image space 0x20
    [InlineData("01 N PIC N VALUE N\"A\".\n           01 R PIC X(8).", "CONVERT(N NAT ANUM HEX)", "0041")]   // r2 over UTF-16BE
    // FMT-15.19.2 — NAT is a §8.10 CONTEXT-SENSITIVE word, legal as argument-1's own data-name: the positional
    // walk binds slot 0 as the OPERAND unconditionally (the old order-free harvest read it as a keyword and
    // rejected the call with "0 operand + 3 format keyword(s) given").
    [InlineData("01 NAT PIC XX VALUE \"41\".\n           01 R PIC X(4).", "CONVERT(NAT HEX ANUM)", "A")]
    // §15.19.3 r5 NOTE — the ANUM-source test is on the REPRESENTATION, not the class: a numeric DISPLAY item
    // is a valid string of characters from the alphanumeric coded character set ("005" → its hex).
    [InlineData("01 D PIC 9(3) VALUE 5.\n           01 R PIC X(8).", "CONVERT(D ANUM ANUM HEX)", "303035")]
    // §15.19.4 r4 — the HEX legs hex THE SOURCE'S OWN BITS, padded TRAILING with zero bits to a whole
    // destination character (16 for national, D-N1); the destination keyword picks only the DIGIT repertoire.
    // 8-bit "A" → 0x41 0x00 → national digits "4100" (RV-15.19.4-4 — NOT "0041", which would be a re-encoding
    // r4 never performs).
    [InlineData("01 A PIC X VALUE \"A\".\n           01 R PIC N(4).", "CONVERT(A ANUM NAT HEX)", "4100")]
    // §15.19.3 r7 + §15.19.4 r2 — an ANY source is the RAW STORAGE bits: a USAGE BIT item's three bits arrive
    // packed high-order-first (CobolBits.Pack), which IS r2's pad — B"101" → 1010 0000 → "A0". NOTE 3 c's "E0"
    // is not derivable from B"101" under either rule (the NOTE is defective; the rule's arithmetic decides).
    [InlineData("01 B PIC 111 USAGE BIT VALUE B\"101\".\n           01 R PIC X(4).", "CONVERT(B ANY ANUM HEX)", "A0")]
    // §15.19.4 r4 over the same sub-byte source — pad 3 bits to a 16-bit multiple: "A000" (RV-15.19.4-2 leg b).
    [InlineData("01 B PIC 111 USAGE BIT VALUE B\"101\".\n           01 R PIC N(4).", "CONVERT(B ANY NAT HEX)", "A000")]
    // §15.19.3 r7 — ANY over a COMP item hexes its radix-2 STORAGE bytes (V59/§14.4: 258 in a 2-byte binary
    // carrier = 0x0102), never its display digits (which would be "30323538").
    [InlineData("01 C PIC 9(4) COMP VALUE 258.\n           01 R PIC X(4).", "CONVERT(C ANY ANUM HEX)", "0102")]
    public void Convert_Formats_2023(string ws, string call, string expected)
        => AssertSpec(Program(ws, $"    MOVE FUNCTION {call} TO R.\n    DISPLAY R.", "IFCONV"), expected, 2023);

    [Fact]
    public void Convert_AnumNatAnum_RoundTrip_2023()
        // §15.19.4 r1/r3 — ANUM→NAT→ANUM round-trip returns the original character. The correspondence is the
        // Annex A.1 item-33 TOTAL UTF-16 identity (PB59), so the round trip holds for EVERY character — the
        // wide leg (Š, U+0160) is the one the old Latin-1 cut substituted to '?', which is the whole point.
        => AssertSpec(Program("01 A PIC XX VALUE \"ZŠ\".\n           01 NR PIC N(2).\n           01 R PIC X(4).",
            "    MOVE FUNCTION CONVERT(A ANUM NAT) TO NR.\n    MOVE FUNCTION CONVERT(NR NAT ANUM) TO R.\n"
            + "    IF R(1:2) = A DISPLAY \"RT=SAME\" ELSE DISPLAY \"RT=DIFFER\".",
            "IFCONVR"), "RT=SAME", 2023);

    [Theory]
    [InlineData("CONVERT(A ANUM ANUM)", "COBOLNET1514")]   // SR3 — source == destination
    [InlineData("CONVERT(A ANUM BYTE)", "COBOLNET1514")]   // SR9 — BYTE needs a HEX source
    [InlineData("CONVERT(A ANY ANUM)", "COBOLNET1514")]    // SR8 — ANY needs an ANUM HEX / NAT HEX destination
    public void Convert_SyntaxRuleViolations_1514(string call, string code)
    {
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(
            Program("01 A PIC X VALUE \"A\".\n           01 R PIC X(4).",
                $"    MOVE FUNCTION {call} TO R.\n    DISPLAY R."));
        Assert.False(ok, $"{call} violates a §15.19.3 syntax rule");
        Assert.Contains(code, detail);
    }

    // The §15.19.2 positional format + the §15.19.3 r4/r5/r6/r7 argument screens (PB59 family 5a). The format
    // is ( argument-1 source-format destination-format ) BY POSITION, so an operand after the format words is
    // malformed; the source-format keys what argument-1's STORAGE must hold, and the static half of each rule
    // is its REPRESENTATION (r5's NOTE: "distinct from simply requiring the string to be of class
    // alphanumeric" — see the admitted numeric-DISPLAY row in Convert_Formats_2023).
    [Theory]
    [InlineData("01 A PIC X VALUE \"A\".", "CONVERT(ANUM A ANUM HEX)")]     // operand mid-list (was accepted, printed 41)
    [InlineData("01 A PIC X VALUE \"A\".", "CONVERT(ANUM ANUM HEX A)")]     // operand after the formats (same defect)
    [InlineData("01 C PIC 9(4) COMP VALUE 5.", "CONVERT(C ANUM ANUM HEX)")] // r5 — binary storage is not characters
    [InlineData("01 N PIC N VALUE N\"A\".", "CONVERT(N ANUM ANUM HEX)")]    // r5 — national characters are not the alphanumeric set
    [InlineData("01 A PIC X VALUE \"A\".", "CONVERT(A NAT ANUM)")]          // r6 — display characters are not national
    [InlineData("01 C PIC 9(4) COMP VALUE 5.", "CONVERT(C HEX ANUM)")]      // r4 — HEX takes display or national usage
    [InlineData("01 P USAGE POINTER.", "CONVERT(P ANY ANUM HEX)")]          // r7 — pointer excluded under ANY
    [InlineData("01 IX USAGE INDEX.", "CONVERT(IX ANY ANUM HEX)")]          // r7 — index excluded under ANY
    public void Convert_ArgumentRuleViolations_1514(string ws, string call)
    {
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(
            Program(ws + "\n           01 R PIC X(8).",
                $"    MOVE FUNCTION {call} TO R.\n    DISPLAY R."));
        Assert.False(ok, $"{call} violates a §15.19.2 format rule or §15.19.3 argument rule");
        Assert.Contains("COBOLNET1514", detail);
    }

    [Fact]
    public void Convert_GatedBelow2023_1502()
    {
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 A PIC X VALUE \"A\".\n           01 R PIC X(4).",
                "    MOVE FUNCTION CONVERT(A ANUM ANUM HEX) TO R.\n    DISPLAY R."));
        Assert.False(ok, "CONVERT is 2023+; 2014 must reject");
        Assert.Contains("COBOLNET1502", detail);
    }

    [Fact]
    public void Convert_AnyOverNational_ResolvesToNatStorage_2023()
        // §15.19.3 r7 — ANY takes the operand's RAW storage bits. A national item's storage is UTF-16BE
        // (D-N1), delivered by the compiler's storage channel (OperandText.AsStorageImage → CobolBits.NatBytes
        // — PB59 family 5b; the former bind-time ANY→NAT remap that used to produce this same value is
        // DELETED, so this test now pins the real channel). N"A" = U+0041 ⇒ "0041" (not "41").
        => AssertSpec(Program("01 N PIC N VALUE N\"A\".\n           01 R PIC X(8).",
            "    MOVE FUNCTION CONVERT(N ANY ANUM HEX) TO R.\n    DISPLAY R.", "IFCONVANY"), "0041", 2023);

    [Fact]
    public void Convert_CharacterPathway_NeverSetsDataConversion_2023()
    {
        // The character↔character pathway is the Annex A.1 item-33 TOTAL identity (PB59): a wide national
        // character (Š, U+0160) IS translatable, so NO EC-DATA-CONVERSION is set even under enabled checking —
        // a POSITIVE assertion that the old '?'+EC arm is gone, not a blank-status accident (the value leg
        // proves the conversion really ran and carried the character).
        var src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFCONVEC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-N PIC N(2) VALUE N"AŠ".
            01 WS-R PIC X(4).
            01 WS-O PIC 9(6).
            PROCEDURE DIVISION.
            MAIN.
            >>TURN EC-DATA-CONVERSION CHECKING ON
                MOVE FUNCTION CONVERT(WS-N NAT ANUM) TO WS-R.
            >>TURN EC-DATA-CONVERSION CHECKING OFF
                COMPUTE WS-O = FUNCTION ORD(WS-R(2:1)).
                DISPLAY "O=" WS-O.
                DISPLAY "S=" FUNCTION EXCEPTION-STATUS.
                STOP RUN.
            """;
        var (ok, output, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        // The blank 31-space register trims to nothing under Normalize — the status must be EMPTY.
        Assert.Equal("O=000353\nS=", output);
    }

    [Fact]
    public void Convert_ByteSerializationSetsDataConversion_WhenChecked_2023()
    {
        // The 8-bit BYTE serialization (a SEPARATE §8.1.2 determination — see the CobolIntrinsics.Text char-set
        // model) substitutes '?' + EC-DATA-CONVERSION for a code unit above 0xFF. This is the DOCUMENTED
        // item-209 disposition (RV-15.19.4-2, decided 2026-08-09): §15.19.4 r2 has no substitution sentence
        // because the spec's model assumes a total serialization; where ours is partial, the r1/r3-analogous
        // substitution+EC is the smallest extension of the standard's own machinery — visible, never silent.
        var src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFCONVECB.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC XX VALUE "AŠ".
            01 WS-R PIC X(8).
            PROCEDURE DIVISION.
            MAIN.
            >>TURN EC-DATA-CONVERSION CHECKING ON
                MOVE FUNCTION CONVERT(WS-A ANUM ANUM HEX) TO WS-R.
            >>TURN EC-DATA-CONVERSION CHECKING OFF
                DISPLAY "R=" WS-R.
                DISPLAY "S=" FUNCTION EXCEPTION-STATUS.
                STOP RUN.
            """;
        var (ok, output, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        // 'A' → 41; Š (no one-byte image) → '?' = 3F. EXCEPTION-STATUS trailing spaces trimmed by Normalize.
        Assert.Equal("R=413F\nS=EC-DATA-CONVERSION", output);
    }

    // ── MODULE-NAME (§15.65, Phase 5, DEVLOG 633): the runtime COBOL hierarchy; CURRENT/ACTIVATING/… keyword ──

    [Theory]
    [InlineData("CURRENT", "[MODNM   ]")]        // §15.65.4 r7 — the running compilation unit's outermost program
    [InlineData("TOP-LEVEL", "[MODNM   ]")]      // r10 — the run-unit main
    [InlineData("ACTIVATING", "[        ]")]     // r5 — a main program's activator is a single space
    public void ModuleName_MainProgram_2023(string kw, string expected)
        => AssertSpec(Program("01 N PIC X(8).",
            $"    MOVE FUNCTION MODULE-NAME({kw}) TO N.\n    DISPLAY \"[\" N \"]\".", "MODNM"), expected, 2023);

    [Fact]
    public void ModuleName_ActivatingAcrossCall_2023()
    {
        // §15.65.4 r5 — in a CALLed program ACTIVATING is the caller's name; STACK (r9) is CURRENT;…;TOP-LEVEL;space.
        var src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. DRVMOD.
            PROCEDURE DIVISION.
            MAIN.
                CALL "HLPMOD".
                STOP RUN.
            END PROGRAM DRVMOD.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. HLPMOD.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 N PIC X(10).
            01 S PIC X(20).
            PROCEDURE DIVISION.
            HP.
                MOVE FUNCTION MODULE-NAME(ACTIVATING) TO N.
                DISPLAY "A=[" N "]".
                MOVE FUNCTION MODULE-NAME(STACK) TO S.
                DISPLAY "S=[" S "]".
                EXIT PROGRAM.
            END PROGRAM HLPMOD.
            """;
        var (ok, output, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("A=[DRVMOD    ]\nS=[HLPMOD;DRVMOD;      ]", output);
    }

    [Fact]
    public void ModuleName_GatedBelow2023_1502()
    {
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 N PIC X(4).", "    MOVE FUNCTION MODULE-NAME(CURRENT) TO N.\n    DISPLAY N.", "MODNG"));
        Assert.False(ok, "MODULE-NAME is 2023+; 2014 must reject");
        Assert.Contains("COBOLNET1502", detail);
    }

    [Fact]
    public void ModuleName_NestedOutsideNestedProgram_1515()
    {
        // §15.65.3 argument rule 1 — NESTED shall be specified only within a contained program.
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(
            Program("01 N PIC X(8).", "    MOVE FUNCTION MODULE-NAME(NESTED) TO N.\n    DISPLAY N.", "MODNN"));
        Assert.False(ok, "MODULE-NAME NESTED requires a contained program");
        Assert.Contains("COBOLNET1515", detail);
    }

    [Fact]
    public void ModuleName_StackFromNestedProgram_2023()
    {
        // ⛔ §15.65.4 r9 — STACK LISTS RUNTIME ELEMENTS, so a CONTAINED program yields its outermost name TWICE.
        // This test used to assert the opposite ("STACK collapses same-unit frames to ONE runtime element …, not
        // the OUTERM;OUTERM duplicate a per-CALL-frame walk gives") and that collapse was REMOVED by owner
        // decision 2026-08-05 (fix-queue PB36), because it could not represent RECURSION: three nested
        // activations of one RECURSIVE program reported a SINGLE entry, while ACTIVATING in the same frame
        // correctly named the program — STACK and ACTIVATING contradicting each other on plain COBOL.
        //
        // The duplicate is r9's own composition, not a defect: r9 builds the list from the other keywords, and
        // its first entry is CURRENT (r7 — OUTERMOST granularity) while every entry after it is the ACTIVATING
        // chain (r5 — ELEMENT granularity). For a contained program those two coincide, so OUTERM appears as both
        // the CURRENT entry and the first link of the chain. A visible repeat can be read; dropped frames cannot.
        // Recorded as an implementor determination in docs/CONFORMANCE.md §4.2.16 row 213.
        var src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OUTERM.
            PROCEDURE DIVISION.
            MP.
                CALL "INNERM".
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INNERM.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 S PIC X(20).
            PROCEDURE DIVISION.
            IP.
                MOVE FUNCTION MODULE-NAME(STACK) TO S.
                DISPLAY "K=" S.
                EXIT PROGRAM.
            END PROGRAM INNERM.
            END PROGRAM OUTERM.
            """;
        var (ok, output, detail) = new CobolNetCompiler(2023).CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("K=OUTERM;OUTERM;", output);   // the trailing operating-environment space is Normalize-trimmed
    }

    // ── SMALLEST/HIGHEST/LOWEST-ALGEBRAIC (§15.83/§15.43/§15.58, Phase 5, DEVLOG 634): PICTURE-metadata folds ──

    [Theory]
    [InlineData("HIGHEST-ALGEBRAIC(X)", "01 X PIC S99.\n           01 R PIC +999.", "+099", 2002)]
    [InlineData("LOWEST-ALGEBRAIC(X)",  "01 X PIC S99.\n           01 R PIC +999.", "-099", 2002)]
    [InlineData("HIGHEST-ALGEBRAIC(X)", "01 X PIC S9(4).\n           01 R PIC +99999.", "+09999", 2002)]
    [InlineData("LOWEST-ALGEBRAIC(X)",  "01 X PIC 99V9(3).\n           01 R PIC +99.999.", "+00.000", 2002)] // unsigned ⇒ 0
    [InlineData("SMALLEST-ALGEBRAIC(X)","01 X PIC S9PP.\n           01 R PIC +9999.", "+0100", 2023)]        // scale −2
    [InlineData("SMALLEST-ALGEBRAIC(X)","01 X PIC 99V9(3).\n           01 R PIC +9.999.", "+0.001", 2023)]
    public void Algebraic_Fold(string call, string ws, string expected, int dialect)
        => AssertSpec(Program(ws, $"    MOVE FUNCTION {call} TO R.\n    DISPLAY R.", "ALGB"), expected, dialect);

    // ── kb/Work R10 (Phase-B F72): floats are LEGAL HIGHEST/LOWEST arguments under native arithmetic — the
    //    implementor usage latitude is SMALLEST-ALGEBRAIC's alone (§15.83.3 r4 / Annex A.1 item 180); §15.43.3
    //    r2 / §15.58.3 r2-r3 bar only the STANDARD float usages under the OPPOSITE standard mode. The value is
    //    §15.43.4 r2's greatest finite magnitude of the carrier, negated for LOWEST (§15.58.4 r2). That value IS the
    //    carrier's, exactly, so a COMP-2 resultant holds it under the implied TRUNCATION (kb/Work PB1196): the binary32
    //    extreme widens exactly and prints as the binary64 it is, −3.4028234663852886E+38 (−(2 − 2^−23) × 2^127). ──

    [Fact]
    public void Algebraic_FloatArguments_FoldToCarrierExtremes_2023()
    {
        var (ok, output, detail) = new CobolNetCompiler(2023).CompileAndRun(Program(
            "01 D USAGE COMP-2.\n           01 S USAGE COMP-1.\n           01 R USAGE COMP-2.",
            "    COMPUTE R = FUNCTION HIGHEST-ALGEBRAIC(D).\n    DISPLAY R.\n"
            + "    COMPUTE R = FUNCTION LOWEST-ALGEBRAIC(S).\n    DISPLAY R.", "ALGF"));
        Assert.True(ok, detail);
        Assert.Equal("1.7976931348623157E+308\n-3.4028234663852886E+38", output);
    }

    [Fact]
    public void Algebraic_SmallestFloat_FoldsToCarrierSubnormal()
    {
        // §15.83.1/§15.83.4 r2 (kb/Work PB122): the smallest algebraic difference between two values the
        // item can represent = the carrier's smallest positive subnormal — a FORMAT property, foldable like
        // 10^(−scale). binary64 → 2^−1074, binary32 → 2^−149 (each printed shortest-round-trip by the
        // COMP-2 receiver's own display). The old guard refused every float on the r4 native latitude with
        // a false "exponent-dependent" rationale; under the STANDARD modes that refusal was over-rejection.
        var (ok, output, detail) = new CobolNetCompiler(2023).CompileAndRun(Program(
            "01 D USAGE COMP-2.\n           01 S USAGE COMP-1.\n           01 R USAGE COMP-2.",
            "    COMPUTE R = FUNCTION SMALLEST-ALGEBRAIC(D).\n    DISPLAY R.\n"
            + "    COMPUTE R = FUNCTION SMALLEST-ALGEBRAIC(S).\n    DISPLAY R.", "ALGS"));
        Assert.True(ok, detail);
        Assert.Equal("5E-324\n1.401298464324817E-45", output);
    }

    [Fact]
    public void Algebraic_SmallestStandardDecimalBarsStandardBinaryFloat_1516()
    {
        // §15.83.3 r2 now reaches SMALLEST through the shared mode-aware float path (kb/Work PB122).
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(
            "IDENTIFICATION DIVISION.\nPROGRAM-ID. ALGSB.\nOPTIONS.\n    ARITHMETIC IS STANDARD-DECIMAL.\n"
            + "DATA DIVISION.\nWORKING-STORAGE SECTION.\n"
            + "01 B USAGE FLOAT-BINARY-32.\n01 R USAGE FLOAT-BINARY-32.\nPROCEDURE DIVISION.\nM-P.\n"
            + "    COMPUTE R = FUNCTION SMALLEST-ALGEBRAIC(B).\n    DISPLAY R.\n    STOP RUN.");
        Assert.False(ok, "a standard binary float usage is barred under STANDARD-DECIMAL (§15.83.3 r2)");
        Assert.Contains("COBOLNET1516", detail);
        Assert.Contains("15.83.3", detail);
    }

    [Fact]
    public void Algebraic_StandardDecimalBarsStandardBinaryFloat_1516()
    {
        // §15.43.3 r2 — mode-aware, usage-precise: FLOAT-BINARY-64 (§3.166) barred under STANDARD-DECIMAL;
        // the native COMP-2 stays legal there (the old guard's message claimed otherwise).
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(
            "IDENTIFICATION DIVISION.\nPROGRAM-ID. ALGM.\nOPTIONS.\n    ARITHMETIC IS STANDARD-DECIMAL.\n"
            + "DATA DIVISION.\nWORKING-STORAGE SECTION.\n"
            + "01 B USAGE FLOAT-BINARY-64.\n01 R USAGE FLOAT-BINARY-64.\nPROCEDURE DIVISION.\nM-P.\n"
            + "    COMPUTE R = FUNCTION HIGHEST-ALGEBRAIC(B).\n    DISPLAY R.\n    STOP RUN.");
        Assert.False(ok, "a standard binary float usage is barred under STANDARD-DECIMAL (§15.43.3 r2)");
        Assert.Contains("COBOLNET1516", detail);
    }

    [Fact]
    public void Length_SuperIsRefusedByName()
    {
        // §15.3 type 11 — "An object reference shall be specified; the predefined object reference SUPER shall not be
        // specified". The refusal USED to be structural (SUPER was no identifier of the expression tier, so
        // LENGTH(SUPER) was a COBOL0001 parse error — kb/Work PB124 wave 5c, AR-15.3-11). Since kb/Work PB1425 SELF
        // and [object-class-name-1 OF] SUPER ARE identifiers there (§8.4.3.1.2 Format 6, §8.4.3.1.3 SR1), so
        // LENGTH(SELF) in a method is the legal type-11 argument it always was, and SUPER is refused BY THE RULE that
        // forbids it as any operand but an invocation's or a property's object — §8.4.3.8.3 SR3, COBOLNET2900, the
        // same verdict §15.3 type 11 asks for. Pinning the code, not a parse error, is what keeps the refusal a
        // rule's rather than ANTLR's recovery choice.
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(
            Program("01 R PIC 9(4).", "    COMPUTE R = FUNCTION LENGTH(SUPER).\n    DISPLAY R."));
        Assert.False(ok, "LENGTH(SUPER) must not compile (§15.3 type 11)");
        Assert.Contains("COBOLNET2900", detail);

        // The surface Format 4 newly makes REACHABLE, and the reason the bare form above is refused at all:
        // §8.4.3.8.3 SR3 — "SUPER may be specified only as the object in an object-property identifier or as
        // the object used to invoke a method with the INVOKE statement or an inline invocation of a method."
        // A COMPLETE `SUPER :: "M"` is that permitted use and is an identifier (§8.4.3.1.2 Format 4), so it is
        // no longer the parser's business to refuse it — and it must still be GOVERNED. Outside a method
        // definition there is no current object for SUPER to name, and the compiler says so by name. This is
        // the assertion the old test could not make, because the form did not parse at all; without it the
        // whole receiver surface PB428 opened would be pinned by nothing here.
        var (okInline, _, detailInline) = new CobolNetCompiler(2023).CompileAndRun(
            Program("01 R PIC 9(4).", "    COMPUTE R = FUNCTION LENGTH(SUPER :: \"M\").\n    DISPLAY R."));
        Assert.False(okInline, "SUPER outside a method definition must not compile (§8.4.3.8)");
        Assert.Contains("COBOLNET0827", detailInline);
    }

    [Fact]
    public void Concat_BooleanFunctionArgument_ImplicitUsageBit()
    {
        // §15.2 item 2: "Boolean functions … have an implicit usage bit." §15.18.3 r2 requires every CONCAT
        // argument to be usage display or every one usage national — usage bit satisfies neither family, and
        // §15.18.1's result table enumerates only display and national boolean rows. The old StaticUsageOf
        // reported a boolean FUNCTION as usage display, so this bound clean (kb/Work PB124 wave 5c,
        // GR-15.2-2). A PIC 1 display boolean ITEM stays a legal display argument.
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(
            Program("01 RS PIC X(10).",
                "    MOVE FUNCTION CONCAT(\"x\" FUNCTION BOOLEAN-OF-INTEGER(5 8)) TO RS.\n    DISPLAY RS."));
        Assert.False(ok, "a boolean function's implicit usage bit violates §15.18.3 r2");
        Assert.Contains("COBOLNET1627", detail);
        Assert.Contains("Bit", detail);
    }

    [Fact]
    public void Algebraic_LiteralArgument_1516()
    {
        // §15.83.3 r1 — argument-1 shall be a data item, not a literal.
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(
            Program("01 R PIC +999.", "    MOVE FUNCTION SMALLEST-ALGEBRAIC(5) TO R.\n    DISPLAY R."));
        Assert.False(ok, "a literal argument violates §15.83.3 r1");
        Assert.Contains("COBOLNET1516", detail);
    }

    [Fact]
    public void Algebraic_SmallestGatedBelow2023_1502()
    {
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 X PIC S999.\n           01 R PIC +999.",
                "    MOVE FUNCTION SMALLEST-ALGEBRAIC(X) TO R.\n    DISPLAY R."));
        Assert.False(ok, "SMALLEST-ALGEBRAIC is 2023+; 2014 must reject");
        Assert.Contains("COBOLNET1502", detail);
    }

    // ── The COBOL-2014 date/time + number family (§15.17/38-41/48/69/79/92/95, Phase 5, DEVLOG 635) ───────────

    [Fact]
    public void FormattedTime_UtcRollAcrossMidnight_2014()
        // §15.41 r2 — a UTC format shows local − offset; 01:00 local at +2h ⇒ 23:00:00Z (the previous day).
        => AssertSpec(Program("01 R PIC X(12).",
            "    MOVE FUNCTION FORMATTED-TIME(\"hh:mm:ssZ\", 3600, 120) TO R.\n    DISPLAY R.", "FT1"), "23:00:00Z", 2014);

    [Fact]
    public void IntegerOfFormattedDate_Ordinal_2014()
        // §15.48.3 r3 — argument-2 "shall be a DATA ITEM of the same type as argument-1"; a literal is not
        // admitted (kb/Work PB58), so the date travels through an item.
        => AssertSpec(Program("01 N PIC +9(9).\n01 D7 PIC X(7) VALUE \"2021167\".",
            "    MOVE FUNCTION INTEGER-OF-FORMATTED-DATE(\"YYYYDDD\", D7) TO N.\n    DISPLAY N.", "IOF"), "+000153569", 2014);

    [Fact]
    public void SecondsFromFormattedTime_Fractional_2014()
        // The result scale comes from the format's fractional-second count (2 here) — §15.79.4.
        => AssertSpec(Program("01 F PIC +9(5).99.",
            "    MOVE FUNCTION SECONDS-FROM-FORMATTED-TIME(\"hh:mm:ss.ss\", \"12:34:56.50\") TO F.\n    DISPLAY F.", "SFT"), "+45296.50", 2014);

    [Theory]
    [InlineData("YYYYMMDD", "20051314", "+000000006")]     // month 13 becomes provable at digit 6 (§15.92 NOTE)
    [InlineData("YYYYMMDD", "15990316", "+000000002")]     // year 15xx < 1601 provable at digit 2
    [InlineData("YYYY-MM-DD", "2021-06-16", "+000000000")] // valid ⇒ 0
    public void TestFormattedDatetime_ErrorPosition_2014(string fmt, string data, string expected)
        => AssertSpec(Program("01 N PIC +9(9).",
            $"    MOVE FUNCTION TEST-FORMATTED-DATETIME(\"{fmt}\", \"{data}\") TO N.\n    DISPLAY N.", "TFD"), expected, 2014);

    [Theory]
    [InlineData("1.5E+3", "+1500.0000")]
    [InlineData("-2.5E-2", "-0000.0250")]
    [InlineData("1E2", "+0000.0000")]   // §15.69.3 — a sign is REQUIRED after E; "1E2" is rejected ⇒ default 0
    public void NumvalF_Values_2014(string arg, string expected)
        => AssertSpec(Program("01 F PIC +9(4).9(4).",
            $"    COMPUTE F = FUNCTION NUMVAL-F(\"{arg}\").\n    DISPLAY F.", "NVF"), expected, 2014);

    [Theory]
    [InlineData("0 1E+2", "+000000003")]   // an embedded space ⇒ the first non-space after it (§15.95 r b.1)
    [InlineData(" +.", "+000000004")]       // no significand digit ⇒ LENGTH+1 (r c)
    [InlineData("1.5E+3", "+000000000")]    // valid ⇒ 0
    [InlineData("1E2", "+000000003")]       // a sign is required after E (§15.69.3) — error at the '2' (position 3)
    public void TestNumvalF_Positions_2014(string data, string expected)
        => AssertSpec(Program("01 N PIC +9(9).",
            $"    MOVE FUNCTION TEST-NUMVAL-F(\"{data}\") TO N.\n    DISPLAY N.", "TNF"), expected, 2014);

    [Fact]
    public void DateFamily_GatedBelow2014_1502()
    {
        var (ok, _, detail) = new CobolNetCompiler(2002).CompileAndRun(
            Program("01 R PIC X(10).", "    MOVE FUNCTION FORMATTED-DATE(\"YYYYMMDD\", 153569) TO R.\n    DISPLAY R."));
        Assert.False(ok, "FORMATTED-DATE is 2014+; 2002 must reject");
        Assert.Contains("COBOLNET1502", detail);
    }

    [Fact]
    public void FormattedDate_NonLiteralFormat_1517()
    {
        // §15.39.3 r1 — the format shall be a literal (analyzed at compile time).
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 FMT PIC X(8) VALUE \"YYYYMMDD\".\n           01 R PIC X(10).",
                "    MOVE FUNCTION FORMATTED-DATE(FMT, 153569) TO R.\n    DISPLAY R."));
        Assert.False(ok, "a non-literal format violates §15.39.3 r1");
        Assert.Contains("COBOLNET1517", detail);
    }

    // ── P7 Step 12: FUNCTION arguments as real arithmetic expressions (§8.4.3.2 SR8) ────────────────────────

    [Theory]
    [InlineData("COMPUTE R = FUNCTION MAX(A / B, C).")]                     // division in an argument — the Step-12 probe
    [InlineData("COMPUTE R = FUNCTION MAX(A * B, (C + 1) / 2, 3 + 4).")]    // the IF119A compound shape
    [InlineData("COMPUTE R = FUNCTION MIN(A * B, (3 + 1) / 2, 3 + 4).")]    // the IF123A paren-arg-after-separator shape
    [InlineData("COMPUTE R = FUNCTION SQRT(FUNCTION MAX(A / B, 4)).")]      // nested FUNCTION with a compound inner arg
    public void CompoundArgs_MatchLegacy(string stmt) =>
        AssertMatchesGolden(Program(
            "01 A PIC 9(3) VALUE 12.\n01 B PIC 9(3) VALUE 4.\n01 C PIC 9(3) VALUE 5.\n01 R PIC S9(5)V9(4).",
            $"    {stmt}\n    DISPLAY R."));

    // SPEC-PINNED: the space-vs-adjacent sign discrimination. §8.7.1 requires an arithmetic operator to be
    // preceded AND followed by a space; §8.3.3.3.2 r2 makes a literal's sign its leftmost CHARACTER — so
    // `MAX(A -4)` is TWO arguments (A and the literal −4) while `MAX(A - 4)` is ONE subtraction.
    [Theory]
    [InlineData("COMPUTE R = FUNCTION MAX(A -4).", "010")]      // two args: MAX(10, −4) = 10
    [InlineData("COMPUTE R = FUNCTION MAX(A - 4).", "006")]     // one arg: MAX(10 − 4) = 6
    [InlineData("COMPUTE R = FUNCTION MAX(-4 7).", "007")]      // space-separated signed literals: two args
    [InlineData("COMPUTE R = FUNCTION MAX(ZERO, 5).", "005")]   // figurative ZERO is a numeric operand (§8.8.1.1)
    public void SignAdjacency_SplitsArguments(string stmt, string expected) =>
        AssertSpec(Program("01 A PIC 9(3) VALUE 10.\n01 R PIC 9(3).", $"    {stmt}\n    DISPLAY R."),
            expected);

    [Fact]
    public void Exponentiation_InArgument_IsLeftAssociative() =>
        // §8.8.1.2 r3: consecutive operations of the SAME hierarchical level execute LEFT TO RIGHT — including
        // ** (the deleted hand parser folded ** right-associatively, a latent divergence; the ONE BindExpr
        // channel folds left): 2 ** 3 ** 2 = (2³)² = 64, not 2⁹ = 512.
        AssertSpec(Program("01 R PIC 9(3).", "    COMPUTE R = FUNCTION MAX(2 ** 3 ** 2, 1).\n    DISPLAY R."),
            "064");

    [Fact]
    public void StringChannel_CompoundNumericArg_Renders() =>
        // The former static string channel was division-incapable (hazard H3): CHAR's argument now renders
        // through the ONE expression renderer under the default receiver — ORD∘CHAR round-trips the ordinal
        // (§15.15.4/§15.70.4) without charset assumptions.
        AssertSpec(Program("01 R PIC 9(3).",
            "    COMPUTE R = FUNCTION ORD(FUNCTION CHAR(66 / 2 + 1)).\n    DISPLAY R."), "034");

    [Fact]
    public void IntegerArgument_NumericEdited_IsRejected() =>
        // ⛔ THIS TEST ASSERTED THE OPPOSITE, AND IT WAS THE OLD READING'S ONLY "spec-derived" WITNESS.
        // It read `AssertSpec(… FUNCTION ORD(FUNCTION CHAR(WS-ED)) …, "034")` over a PIC Z9 and was cited, with a
        // corpus golden, as evidence that a numeric-edited item de-edits into an INTEGER argument. Its own
        // comment cited §14.9.25.4 GR5 — a MOVE rule — for a §15.3 type-6 argument position, which is the
        // misapplication in one line. Owner decision 2026-08-02: exclude it. §8.8.1.1 admits "an identifier
        // referencing a NUMERIC data item"; §8.5.2.13 calls this a "numeric-edited data item" and §8.5.2.1
        // Table 2 puts that category in class ALPHANUMERIC/NATIONAL, never numeric; §15.3 type 6's only other
        // alternative is "an integer data item", which it also is not. ⚠ A golden and a test agreeing is not
        // independent evidence when both were written from the same premise.
        // The rejection fixture is `pb1-integer-arg-numeric-edited`; this pins the same verdict at the unit seam.
        AssertRejected(Program("01 WS-ED PIC Z9.\n01 R PIC 9(3).",
            "    MOVE 34 TO WS-ED.\n    COMPUTE R = FUNCTION ORD(FUNCTION CHAR(WS-ED)).\n    DISPLAY R."),
            "COBOLNET1627");

    [Fact]
    public void RefModArgument_Renders() =>
        // A reference-modified argument is an alphanumeric operand (§8.4.3.3.4) — live through the ONE
        // dataReference resolution (the hand parser rejected ref-mod arguments loud).
        AssertSpec(Program("01 WS-IN PIC X(8) VALUE \"abcdEFGH\".\n01 T PIC X(8).",
            "    MOVE FUNCTION UPPER-CASE(WS-IN(1:4)) TO T.\n    DISPLAY \"[\" T \"]\"."),
            "[ABCD    ]");

    [Fact]
    public void EmptyParens_NoArgFunction_2014() =>
        // FUNCTION PI () — the §8.4.3.2 SR6 NOTE's empty-argument-list shape.
        AssertSpec(Program("01 R PIC 9.9(5).", "    COMPUTE R = FUNCTION PI ().\n    DISPLAY R."),
            "3.14159", 2014);

    [Fact]
    public void KeywordOmitted_ExpressionArgs_2002() =>
        // The §8.4.3.2 SR2 keyword-omitted form with COMPOUND arguments: the captured argument text re-parses
        // through the ONE functionArgList grammar (the D2 fragment re-parse), so expressions bind identically
        // to the FUNCTION-keyword form.
        AssertSpec("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. IFKWOM2.
            ENVIRONMENT DIVISION.
            CONFIGURATION SECTION.
            REPOSITORY.
                FUNCTION ALL INTRINSIC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 A PIC 9(3) VALUE 10.
            01 R PIC 9(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                COMPUTE R = MAX(A + 1, A - 4, 7).
                DISPLAY R.
                STOP RUN.
            """, "011", 2002);

    [Fact]
    public void OmittedArgument_ToIntrinsic_1544()
    {
        // §8.4.3.2 SR7 — OMITTED shall not be specified for an intrinsic function.
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 R PIC 9(3).", "    COMPUTE R = FUNCTION MAX(OMITTED, 4).\n    DISPLAY R."));
        Assert.False(ok, "OMITTED violates §8.4.3.2 SR7 for an intrinsic");
        Assert.Contains("COBOLNET1544", detail);
    }

    // ── Spec-pinned: the boolean-conversion pair (§15.13 / §15.45 — P11 Step 2) ─────────────────────────────

    [Fact]
    public void BooleanOfInteger_MsbFirst_LeftTruncates_Spec() =>
        // §15.13.4 r1: rightmost boolean position = low-order digit; zero-filled or TRUNCATED ON THE LEFT to
        // argument-2 positions (544 = 0b1000100000 → the low 6 bits B"100000" — the Annex D.10 worked example);
        // the MOVE into PIC 1(8) then right-zero-fills (§14.6.8.6) → B"10000000". §15.45.4 r1: the unsigned
        // MSB-first value — 128, where an LSB-first defect would print 1.
        AssertSpec(Program("""
            01 B-8 PIC 1(8) USAGE BIT.
            01 R PIC 9(3).
            """, """
                MOVE FUNCTION BOOLEAN-OF-INTEGER(544, 6) TO B-8.
                DISPLAY B-8.
                COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN(B-8).
                DISPLAY R.
            """), "10000000\n128", 2002);

    [Fact]
    public void IntegerOfBoolean_RoundTrip_Spec() =>
        // §15.13.4 r1 ∘ §15.45.4 r1 — round-trip identity for 0 ≤ n < 2^k (both unsigned, MSB-first); the
        // boolean-literal argument leg rides §15.3 item 3.
        AssertSpec(Program("01 R PIC 9(5).", """
                COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN(
                    FUNCTION BOOLEAN-OF-INTEGER(200, 8)).
                DISPLAY R.
                COMPUTE R = FUNCTION INTEGER-OF-BOOLEAN(B"00000101").
                DISPLAY R.
            """), "00200\n00005", 2002);

    // ── Spec-pinned: BYTE-LENGTH vs LENGTH (§15.14/§15.50 — P11 Step 7) ─────────────────────────────────────

    [Fact]
    public void ByteLength_NationalDivergesFromLength_Spec() =>
        // §15.14.4 r1 counts BYTES (national = 2 bytes/position, D-N1), §15.50.4 r2 counts national POSITIONS —
        // the D7 distinction: BYTE-LENGTH(N(3)) = 6, LENGTH(N(3)) = 3.
        AssertSpec(Program("01 WS-N PIC N(3).", """
                DISPLAY FUNCTION BYTE-LENGTH(WS-N).
                DISPLAY FUNCTION LENGTH(WS-N).
            """), "6\n3", 2002);

    // ── Spec-pinned: SMALLEST-ALGEBRAIC (§15.83 — P11 Step 7) ──────────────────────────────────────────────

    [Fact]
    public void SmallestAlgebraic_PictureIncrement_Spec() =>
        // §15.83.4 RVR2 NOTE table: the smallest positive increment = 10^(−scale) from the PICTURE alone —
        // S999 → 1, 99V9(3) → 0.001, S9PP → 100 (P-scaling).
        AssertSpec(Program("""
            01 X-INT  PIC S999.
            01 X-FRAC PIC 99V9(3).
            01 X-PSCL PIC S9PP.
            """, """
                DISPLAY FUNCTION SMALLEST-ALGEBRAIC(X-INT).
                DISPLAY FUNCTION SMALLEST-ALGEBRAIC(X-FRAC).
                DISPLAY FUNCTION SMALLEST-ALGEBRAIC(X-PSCL).
            """), "1\n0.001\n100", 2023);

    [Fact]
    public void Concatenate_NotAnIsoFunction_1501()
    {
        // The P11 anchor re-scout established CONCATENATE is NOT an ISO/IEC 1989 function at any edition
        // (CONCAT §15.18 is new-in-2023; CONCATENATE has zero spec occurrences). A reference to it draws the
        // unknown-function diagnostic, not an edition-window one — even at 2014 where the phase doc's stale
        // premise would have accepted it.
        var (ok, _, detail) = new CobolNetCompiler(2014).CompileAndRun(
            Program("01 R PIC X(4).", "    MOVE FUNCTION CONCATENATE(\"A\", \"B\") TO R.\n    DISPLAY R."));
        Assert.False(ok, "CONCATENATE is not an ISO intrinsic function");
        Assert.Contains("COBOLNET1501", detail);
    }
}
