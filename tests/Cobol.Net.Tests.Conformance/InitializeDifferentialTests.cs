// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// INITIALIZE (ISO §14.9.20): the bind-time expansion to implicit elementary MOVEs (GR4) — the GR6c category
/// defaults (numeric/numeric-edited → ZEROES through MOVE editing, the others → SPACES), REPLACING category
/// selection with full MOVE semantics (GR5c2/GR6b), the FILLER and REDEFINES exclusions (GR5a2/GR5a3), OCCURS
/// expansion of every occurrence (GR5b2), and multi-identifier order (GR3). The COBOL-85 facts are differential
/// against the committed golden (NIST NC223A/NC201A-proven for this verb); the 2002+ surface (WITH FILLER) is
/// spec-pinned at --std 2023 and edition-REJECTED at 85 (the VERSION TEST MATRIX invariant — a construct is
/// rejected below its introducing edition).
/// </summary>
public sealed class InitializeDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string working, string procedure) => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. INITDF.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {{working}}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {{procedure}}
            STOP RUN.
        """;

    // §14.9.20 GR5c4 + GR6c: the bare-85 form makes every non-excluded elementary a receiver; defaults are
    // ZEROES for numeric AND numeric-edited (the EDITED zero — '$$$9.99' shows '  $0.00', never spaces), and
    // alphanumeric SPACES for alphabetic / alphanumeric / alphanumeric-edited (plain spaces, NO editing).
    [Fact]
    public void Initialize_BareGroup_CategoryDefaults()
        => AssertMatchesGolden(Program("""
            01 G1.
               05 N1 PIC 9(4).
               05 E1 PIC $(3)9.99.
               05 X1 PIC X(4).
               05 A1 PIC A(4).
               05 AE1 PIC XXBXX.
            """, """
                MOVE 1234 TO N1.
                MOVE 567.89 TO E1.
                MOVE "QQQQ" TO X1.
                MOVE "BBBB" TO A1.
                MOVE "WWWW" TO AE1.
                INITIALIZE G1.
                DISPLAY N1.
                DISPLAY E1.
                IF X1 = SPACES DISPLAY "X1-SP" ELSE DISPLAY "X1-NO".
                IF A1 = SPACES DISPLAY "A1-SP" ELSE DISPLAY "A1-NO".
                IF AE1 = SPACES DISPLAY "AE1-SP" ELSE DISPLAY "AE1-NO".
            """));

    // §14.9.20 GR5c2: with REPLACING (and no DEFAULT), ONLY items of a listed category are receivers — every
    // other item is left UNCHANGED (the GR5c filter; '85 and 2023 agree).
    [Fact]
    public void Initialize_Replacing_LeavesOtherCategoriesUntouched()
        => AssertMatchesGolden(Program("""
            01 G1.
               05 N1 PIC 9(4) VALUE 1234.
               05 X1 PIC X(4) VALUE "QQQQ".
            """, """
                INITIALIZE G1 REPLACING NUMERIC DATA BY 7.
                DISPLAY N1.
                DISPLAY X1.
            """));

    // §14.9.20 GR4 + GR6b (NC223A INI-TEST-GF-7 shape): each REPLACING sender stores under FULL MOVE semantics —
    // a numeric identifier-2 into a numeric-edited receiver EDITS (1234 → '$234.00': alignment + high-order
    // truncation at the mask's digit positions, §14.9.25.4 GR5); an alphanumeric literal into an
    // alphanumeric-edited receiver places characters through the B-insertion mask (§13.18.40).
    [Fact]
    public void Initialize_Replacing_AllFiveCategories_FullMoveSemantics()
        => AssertMatchesGolden(Program("""
            01 G2.
               05 R-NUM PIC 9(4).
               05 R-NED PIC $(3)9.99.
               05 R-ALN PIC X(4).
               05 R-AED PIC XXBXX.
               05 R-ALP PIC A(4).
            01 NUM-SRC PIC 9(4) VALUE 1234.
            """, """
                INITIALIZE G2
                    REPLACING ALPHABETIC DATA     BY "AAAAAA"
                              ALPHANUMERIC        BY "****"
                              ALPHANUMERIC-EDITED BY "DDDD"
                              NUMERIC DATA        BY 1234
                              NUMERIC-EDITED      BY NUM-SRC.
                DISPLAY R-NUM.
                DISPLAY R-NED.
                DISPLAY R-ALN.
                DISPLAY R-AED.
                DISPLAY R-ALP.
            """));

    // §14.9.20 GR5a2: an (explicit or implicit) FILLER elementary item is EXCLUDED from the bare form — the
    // whole-group display shows the FILLER bytes intact between the cleared neighbors.
    [Fact]
    public void Initialize_FillerExcluded()
        => AssertMatchesGolden(Program("""
            01 G0.
               05 P1 PIC X(2) VALUE "AA".
               05 FILLER PIC XX VALUE "FF".
               05 P2 PIC 99 VALUE 77.
            """, """
                INITIALIZE G0.
                DISPLAY G0.
            """));

    // §14.9.20 GR5a3: a subordinate whose entry has REDEFINES — and its subtree — is excluded; the REDEFINED
    // (canonical) item still initializes, and items after the class initialize normally. (Exercises the Tier-B
    // string-canonical window store: X(4) redefined by 9(4) shares ONE backing.)
    [Fact]
    public void Initialize_RedefinesSubtreeExcluded()
        => AssertMatchesGolden(Program("""
            01 G3.
               05 RA PIC X(4) VALUE "QQQQ".
               05 RB REDEFINES RA PIC 9(4).
               05 RC PIC 99 VALUE 55.
            """, """
                INITIALIZE G3.
                DISPLAY RC.
                IF RA = SPACES DISPLAY "RA-SPACES" ELSE DISPLAY "RA-KEPT".
            """));

    // §14.9.20 GR5b2 + GR8 (NC201A PFM-F4 shape — a named group over a COMP OCCURS table): a table element makes
    // EVERY occurrence a receiving operand, initialized per element (a 5 × S9(3) COMP table is five scaled
    // integers, not one byte run).
    [Fact]
    public void Initialize_Occurs_EveryOccurrence()
        => AssertMatchesGolden(Program("""
            01 GT.
               03 TN PIC S9(3) COMP OCCURS 5.
            01 SHOW-N PIC 9(3).
            """, """
                MOVE 111 TO TN(1).
                MOVE 222 TO TN(3).
                MOVE 333 TO TN(5).
                INITIALIZE GT.
                MOVE TN(1) TO SHOW-N.
                DISPLAY SHOW-N.
                MOVE TN(3) TO SHOW-N.
                DISPLAY SHOW-N.
                MOVE TN(5) TO SHOW-N.
                DISPLAY SHOW-N.
            """));

    // §14.9.20 GR3 + GR5b1 (NC223A INI-TEST-GF-8 shape): multiple identifier-1 behave as separate INITIALIZE
    // statements in source order, and an ELEMENTARY identifier-1 is itself the (single) receiver — the
    // numeric-edited elementary gets the edited zero, the alphanumeric elementary spaces.
    [Fact]
    public void Initialize_MultipleIdentifiers_AndElementaryTargets()
        => AssertMatchesGolden(Program("""
            01 D1 PIC $(3)9.99.
            01 G4.
               05 GN PIC 99.
               05 GX PIC XX.
            01 D2 PIC X(4).
            """, """
                MOVE 999.99 TO D1.
                MOVE 77 TO GN.
                MOVE "QQ" TO GX.
                MOVE "ZZZZ" TO D2.
                INITIALIZE D1 G4 D2.
                DISPLAY D1.
                DISPLAY GN.
                IF GX = SPACES DISPLAY "GX-SP" ELSE DISPLAY "GX-NO".
                IF D2 = SPACES DISPLAY "D2-SP" ELSE DISPLAY "D2-NO".
            """));

    // §14.9.20 GR5a2 (the 2002+ FILLER phrase, Annex E): WITH FILLER re-includes FILLER elementary items.
    // Spec-pinned at --std 2023 (the former legacy engine has no 2002 surface): the bare form leaves the FILLER bytes
    // ('  FF00' — P1 spaces, FILLER kept, P2 zeros), WITH FILLER clears them too ('    00').
    [Fact]
    public void Initialize_WithFiller_2023_IncludesFillerItems()
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun(Program("""
            01 G5.
               05 P1 PIC X(2).
               05 FILLER PIC XX.
               05 P2 PIC 99.
            """, """
                MOVE "AAFF77" TO G5.
                INITIALIZE G5.
                DISPLAY G5.
                MOVE "AAFF77" TO G5.
                INITIALIZE G5 WITH FILLER.
                DISPLAY G5.
            """));
        Assert.True(ok, detail);
        Assert.Equal("  FF00\n    00", stdout);
    }

    // VERSION TEST MATRIX invariant: WITH FILLER was introduced by ISO/IEC 1989:2002 (§14.9.20 / Annex E) — at
    // --std 85 the compile is REJECTED with the INITIALIZE edition-gate diagnostic, not silently accepted.
    [Fact]
    public void Initialize_WithFiller_RejectedAt85()
    {
        var (ok, diags) = EditionHarness.Compile(Program("""
            01 G5.
               05 P1 PIC X(2).
               05 FILLER PIC XX.
               05 P2 PIC 99.
            """, """
                INITIALIZE G5 WITH FILLER.
                DISPLAY G5.
            """), 85);
        Assert.False(ok, "INITIALIZE … WITH FILLER must be rejected at --std 85 (introduced by ISO/IEC 1989:2002)");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET0830");
    }
}
