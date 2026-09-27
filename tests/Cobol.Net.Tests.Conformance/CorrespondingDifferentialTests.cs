// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// MOVE/ADD/SUBTRACT CORRESPONDING (ISO/IEC 1989:2023 §14.7.6 + §14.9.25 F2 / §14.9.2 F3 / §14.9.44 F3): the six
/// pair-selection rules, D1-declaration-order expansion, the one statement rounded-phrase applied per pair, the
/// STATEMENT-level aggregated SIZE ERROR (one dispatch after all pairs, erring receiver unchanged, NOT suppressed),
/// and item identification — group subscripts included — at statement START. Differential facts pin the
/// NIST-85-proven shapes to the legacy oracle; the facts where the legacy deviates from the spec (the missing
/// rule-2 / rule-3 filters, per-pair group re-identification) are SPEC-PINNED with the governing § cited.
/// </summary>
public sealed class CorrespondingDifferentialTests
{
    private static readonly ICompilerUnderTest CobolNet = new CobolNetCompiler();

    private static void AssertOutput(string source, string expected)
    {
        var (ok, outp, detail) = CobolNet.CompileAndRun(source);
        Assert.True(ok, $"WiseOwl COBOL failed: {detail}");
        Assert.Equal(expected, outp);
    }

    private static void AssertSameAsLegacy(string source) => DifferentialGolden.Assert(source);

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CORRT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    // ── Pair selection over nested groups, rule 1 (name + relative qualification path), D1 order. ───────────────
    [Fact]
    public void MoveCorresponding_NestedPairs_PartialMatch()
        => AssertSameAsLegacy(Program("""
            01 SRC-G.
               02 HDR PIC XX VALUE "SH".
               02 SUB-G.
                  03 N1 PIC 99 VALUE 11.
                  03 ONLY-SRC PIC 9 VALUE 7.
               02 N2 PIC 999 VALUE 222.
            01 DST-G.
               02 HDR PIC XX VALUE "DH".
               02 SUB-G.
                  03 N1 PIC 99 VALUE 88.
                  03 ONLY-DST PIC 9 VALUE 5.
               02 N2 PIC 999 VALUE 999.
               02 EXTRA PIC X VALUE "E".
            """, """
                MOVE CORRESPONDING SRC-G TO DST-G.
                DISPLAY HDR OF DST-G.
                DISPLAY N1 OF SUB-G OF DST-G.
                DISPLAY ONLY-DST.
                DISPLAY N2 OF DST-G.
                DISPLAY EXTRA.
            """));

    // ── Rules 1/4: FILLER, an OCCURS child, and a REDEFINES child do not correspond (§14.7.6 r1/r4). ────────────
    [Fact]
    public void MoveCorresponding_ExcludesFillerOccursAndRedefinesChildren()
        => AssertSameAsLegacy(Program("""
            01 SRC-G.
               02 BASE PIC 99 VALUE 33.
               02 OVR REDEFINES BASE PIC 99.
               02 ITM PIC 99 VALUE 44.
            01 DST-G.
               02 BASE PIC 99 VALUE 11.
               02 FILLER PIC X VALUE "F".
               02 ITM PIC 99 OCCURS 2.
               02 OVR PIC 99 VALUE 77.
            """, """
                MOVE 10 TO ITM OF DST-G (1).
                MOVE 20 TO ITM OF DST-G (2).
                MOVE CORRESPONDING SRC-G TO DST-G.
                DISPLAY BASE OF DST-G.
                DISPLAY ITM OF DST-G (1).
                DISPLAY ITM OF DST-G (2).
                DISPLAY OVR OF DST-G.
            """));

    // ── Rule 2 (§14.7.6 r2 + Table 16 §14.9.25.3 SR10): a numeric NONINTEGER → alphanumeric move is invalid, so
    //    the namesake pair does NOT correspond — a silent skip, not an error. SPEC-PINNED: the legacy matcher has
    //    no rule-2 filter and would move FRAC. ─────────────────────────────────────────────────────────────────
    [Fact]
    public void MoveCorresponding_Rule2_NonintegerToAlphanumeric_IsNotAPair()
        => AssertOutput(Program("""
            01 SRC-G.
               02 FRAC PIC 9V9 VALUE 3.5.
               02 WNUM PIC 99 VALUE 42.
            01 DST-G.
               02 FRAC PIC XX VALUE "AB".
               02 WNUM PIC 99 VALUE 11.
            """, """
                MOVE CORRESPONDING SRC-G TO DST-G.
                DISPLAY FRAC OF DST-G.
                DISPLAY WNUM OF DST-G.
            """), "AB\n42");

    // ── Rule 3 (§14.7.6 r3): ADD/SUBTRACT pairs require BOTH items numeric — an alphanumeric namesake silently
    //    does not correspond. SPEC-PINNED: the legacy matcher has no rule-3 filter. ──────────────────────────────
    [Fact]
    public void AddCorresponding_Rule3_NonNumericNamesakeIsNotAPair()
        => AssertOutput(Program("""
            01 SRC-G.
               02 TAG PIC 9 VALUE 5.
               02 AMT PIC 99 VALUE 30.
            01 DST-G.
               02 TAG PIC X VALUE "A".
               02 AMT PIC 99 VALUE 12.
            """, """
                ADD CORRESPONDING SRC-G TO DST-G.
                DISPLAY TAG OF DST-G.
                DISPLAY AMT OF DST-G.
            """), "A\n42");

    // ── The ONE statement rounded-phrase applies to EVERY implied pair statement (§14.9.2.2 F3 / §14.7.4). ──────
    [Fact]
    public void AddCorresponding_RoundedAppliesToEachPair()
    {
        var src = Program("""
            01 SRC-G.
               02 AMT1 PIC 9V99 VALUE 1.25.
               02 AMT2 PIC 9V99 VALUE 2.34.
            01 DST-G.
               02 AMT1 PIC 9V9 VALUE 1.0.
               02 AMT2 PIC 9V9 VALUE 1.0.
            """, """
                ADD CORRESPONDING SRC-G TO DST-G ROUNDED.
                DISPLAY AMT1 OF DST-G.
                DISPLAY AMT2 OF DST-G.
            """);
        // 1.0+1.25 = 2.25 → 2.3 and 1.0+2.34 = 3.34 → 3.3, each rounded NEAREST-AWAY-FROM-ZERO (§14.7.4.3 r1).
        AssertOutput(src, "23\n33");
        AssertSameAsLegacy(src);
    }

    // ── SIZE ERROR is STATEMENT-level (§14.7.6, "after all of the implied statements are completed"): one flag
    //    across the pairs, the erring pair's receiver UNCHANGED (§14.7.5) while later pairs still store, ONE
    //    dispatch of the ON imperative, and the NOT phrase ignored. SPEC-PINNED to the governing §§. ─────────────
    [Fact]
    public void AddCorresponding_SizeError_AggregatedAcrossPairs()
        => AssertOutput(Program("""
            01 SRC-G.
               02 OVF PIC 9 VALUE 7.
               02 ACC PIC 99 VALUE 20.
            01 DST-G.
               02 OVF PIC 9 VALUE 5.
               02 ACC PIC 99 VALUE 10.
            """, """
                ADD CORRESPONDING SRC-G TO DST-G
                    ON SIZE ERROR DISPLAY "SE"
                    NOT ON SIZE ERROR DISPLAY "NSE".
                DISPLAY OVF OF DST-G.
                DISPLAY ACC OF DST-G.
            """), "SE\n5\n30");

    // ── SUBTRACT direction (§14.9.44.4 GR3/GR5: id-5 ← id-5 − id-4 — GR5's reduction to separate `SUBTRACT a
    //    FROM b` statements settles it) and the CORR abbreviation (SR5: CORR ≡ CORRESPONDING). SPEC-PINNED. ──────
    [Fact]
    public void SubtractCorr_TargetMinusSource_CorrToken()
        => AssertOutput(Program("""
            01 SRC-G.
               02 BAL PIC 99 VALUE 3.
            01 DST-G.
               02 BAL PIC 99 VALUE 10.
            """, """
                SUBTRACT CORR SRC-G FROM DST-G.
                DISPLAY BAL OF DST-G.
            """), "07");

    // ── Qualified source group + SUBSCRIPTED receiving group (the NC209A shapes — D1/D2 themselves may be
    //    qualified table elements; the exclusions apply only to items WITHIN them, §14.7.6). ─────────────────────
    [Fact]
    public void MoveCorresponding_QualifiedGroup_SubscriptedReceiver()
        => AssertSameAsLegacy(Program("""
            01 SRC-TOP.
               02 SUB-G.
                  03 NAM PIC XX VALUE "PP".
                  03 QTY PIC 99 VALUE 42.
            01 TBL-TOP.
               02 ROW-G OCCURS 3.
                  03 NAM PIC XX.
                  03 QTY PIC 99.
            """, """
                MOVE "AA" TO NAM OF ROW-G (1).
                MOVE 11 TO QTY OF ROW-G (1).
                MOVE "BB" TO NAM OF ROW-G (2).
                MOVE 22 TO QTY OF ROW-G (2).
                MOVE "CC" TO NAM OF ROW-G (3).
                MOVE 33 TO QTY OF ROW-G (3).
                MOVE CORRESPONDING SUB-G OF SRC-TOP TO ROW-G (2).
                DISPLAY NAM OF ROW-G (1).
                DISPLAY QTY OF ROW-G (1).
                DISPLAY NAM OF ROW-G (2).
                DISPLAY QTY OF ROW-G (2).
                DISPLAY NAM OF ROW-G (3).
                DISPLAY QTY OF ROW-G (3).
            """));

    // ── Item identification — the group operands' subscripts included — happens ONCE at statement START, never
    //    per implied statement (§14.7.6: "Any item identification … is done at the start of the execution of the
    //    statement"). Pair 1 overwrites the subscript item of the SOURCE group; pair 2 must still read the row
    //    identified before any pair ran. SPEC-PINNED: the legacy re-resolves the group per pair. ─────────────────
    [Fact]
    public void MoveCorresponding_GroupSubscriptIdentifiedOnce_AtStatementStart()
        => AssertOutput(Program("""
            01 DST-G.
               02 IDX PIC 9 VALUE 1.
               02 VAL PIC 9 VALUE 0.
            01 STAB.
               02 SROW OCCURS 2.
                  03 IDX PIC 9.
                  03 VAL PIC 9.
            """, """
                MOVE 2 TO IDX OF SROW (1).
                MOVE 5 TO VAL OF SROW (1).
                MOVE 9 TO IDX OF SROW (2).
                MOVE 8 TO VAL OF SROW (2).
                MOVE CORRESPONDING SROW (IDX OF DST-G) TO DST-G.
                DISPLAY IDX OF DST-G.
                DISPLAY VAL OF DST-G.
            """), "2\n5");

    // ── The NC202A table shape (NC202A :1112): an OCCURS namesake inside both groups is excluded (r4) while its
    //    siblings pair — RECORD1: 8+6=14, RECORD3: 9+7=16, RECORD2 occurrences untouched. ────────────────────────
    [Fact]
    public void AddCorresponding_TableShape_OccursChildExcluded()
        => AssertSameAsLegacy(Program("""
            01 TABLE1.
               02 RECORD1 PIC 99 VALUE 6.
               02 RECORD2 PIC 99 OCCURS 2.
               02 RECORD3 PIC 99 VALUE 7.
            01 TABLE2.
               02 RECORD1 PIC 99 VALUE 8.
               02 RECORD2 PIC 99 OCCURS 2.
               02 RECORD3 PIC 99 VALUE 9.
            """, """
                MOVE 01 TO RECORD2 OF TABLE1 (1).
                MOVE 02 TO RECORD2 OF TABLE1 (2).
                MOVE 03 TO RECORD2 OF TABLE2 (1).
                MOVE 04 TO RECORD2 OF TABLE2 (2).
                ADD CORRESPONDING TABLE1 TO TABLE2.
                DISPLAY RECORD1 OF TABLE2.
                DISPLAY RECORD2 OF TABLE2 (1).
                DISPLAY RECORD2 OF TABLE2 (2).
                DISPLAY RECORD3 OF TABLE2.
            """));
}
