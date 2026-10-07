// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// REDEFINES Tier B (ISO/IEC 1989:2023 §13.18.44; COBOLNET_DESIGN §4.2): a DISPLAY-homogeneous class (alphanumeric /
/// DISPLAY-numeric / numeric-edited views over one storage area). The canonical is ONE <see cref="string"/> backing of
/// class-max width; each view is a typed <c>(offset,width)</c> window over it — a numeric view decodes/encodes via
/// <c>CobolNum.ParseDisplay</c>/<c>FormatDisplay</c>. A write through any view is visible through every other (one
/// backing, NO byte[]). Pinned to the legacy oracle (spec-pinned where the legacy DISPLAY trailing-trim quirk shows).
/// </summary>
public sealed class RedefinesTierBDifferentialTests
{
    private static void AssertMatchesGolden(string source) => DifferentialGolden.Assert(source);

    private static string Program(string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. REDEFB.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN-PARA.
        {proc}
            STOP RUN.
        """;

    [Fact]
    public void NumericViewWrite_VisibleThroughAlphanumericOriginal()
        => AssertMatchesGolden(Program(
            "01 WS-A PIC X(6).\n01 WS-B REDEFINES WS-A PIC 9(6).",
            "    MOVE 42 TO WS-B.\n    DISPLAY WS-A."));   // 000042 — numeric view formats into the shared backing

    [Fact]
    public void NumericViewRead_DecodesTheBacking()
        => AssertMatchesGolden(Program(
            "01 WS-A PIC X(4) VALUE \"0025\".\n01 WS-B REDEFINES WS-A PIC 9(4).\n01 WS-C PIC 9(4).",
            "    MOVE WS-B TO WS-C.\n    ADD 5 TO WS-C.\n    DISPLAY WS-C."));   // 0030 — view read via ParseDisplay, then +5

    [Fact]
    public void AlphanumericView_OverNumericCanonical()
        => AssertMatchesGolden(Program(
            "01 WS-A PIC 9(6) VALUE 123456.\n01 WS-B REDEFINES WS-A PIC X(6).",
            "    DISPLAY WS-B."));   // 123456 — the numeric canonical's image read as characters

    [Fact]
    public void WriteAlphanumericView_ReadNumericView_Coherent()
        => AssertMatchesGolden(Program(
            "01 WS-A PIC X(6).\n01 WS-B REDEFINES WS-A PIC 9(6).",
            "    MOVE \"012345\" TO WS-A.\n    DISPLAY WS-B."));   // 012345 — one backing, both views agree

    [Fact]
    public void GroupView_PartialFields_AlphaAndNumeric()
        => AssertMatchesGolden(Program("""
            01 WS-A PIC X(6) VALUE "AB1234".
            01 WS-B REDEFINES WS-A.
               05 WS-B1 PIC X(2).
               05 WS-B2 PIC 9(4).
            """, "    DISPLAY WS-B1 \"|\" WS-B2."));   // AB|1234 — windows at offsets 0 and 2

    [Fact]
    public void LargerRedefiner_SR8_ClassMaxWidth()
        // SR8: a level-01 non-EXTERNAL item may be redefined larger; the backing is sized to the class max (8).
        => AssertMatchesGolden(Program(
            "01 WS-A PIC X(4) VALUE \"ABCD\".\n01 WS-B REDEFINES WS-A PIC X(8).",
            "    MOVE \"ABCDEFGH\" TO WS-B.\n    DISPLAY WS-B \"|\" WS-A."));   // ABCDEFGH|ABCD

    [Fact]
    public void InGroup_TierBBacking_NestedImage()
        // The CCVS COMPUTED-X shape: a REDEFINES class nested inside a group — the backing is a struct member, and the
        // outer group's AsImage counts it once (not the views).
        => AssertMatchesGolden(Program("""
            01 WS-REC.
               05 WS-HEAD PIC X(2) VALUE "HD".
               05 WS-X.
                  10 WS-A PIC X(4) VALUE "0042".
                  10 WS-B REDEFINES WS-A PIC 9(4).
               05 WS-TAIL PIC X(2) VALUE "TL".
            """, "    DISPLAY WS-REC."));   // HD0042TL

    [Fact]
    public void OriginalValueSeedsTheBacking()
        => AssertMatchesGolden(Program(
            "01 WS-A PIC X(6) VALUE \"SEEDED\".\n01 WS-B REDEFINES WS-A PIC X(3).",
            "    DISPLAY WS-A \"|\" WS-B."));   // SEEDED|SEE — only the original's VALUE inits (SR9)

    [Fact]
    public void FixedOccursValue_SeedsEveryOccurrence()
        // §13.18.63 GR9: every occurrence of a fixed-OCCURS entry takes the VALUE — the Tier-B backing seeds the
        // WHOLE canonical image ("ABABAB"), not one occurrence padded ("AB    "). Regression for ImageInitOf.
        => AssertMatchesGolden(Program(
            "01 WS-G.\n   05 WS-E PIC XX OCCURS 3 VALUE \"AB\".\n01 WS-V REDEFINES WS-G PIC X(6).",
            "    DISPLAY WS-V."));   // ABABAB

    [Fact]
    public void InGroup_ReadNestedCanonicalDirectly()
        // The CCVS BAIL-OUT shape: a nested Tier-B canonical referenced DIRECTLY from the PROCEDURE DIVISION (not via
        // the outer group's AsImage). The backing is a struct member, so its access must be qualified
        // (OUTER.GROUP._redef_X), not the bare field name — the bug NC101A's COMPUTED-A/CORRECT-A exposed.
        => AssertMatchesGolden(Program("""
            01 WS-REC.
               05 WS-X.
                  10 WS-A PIC X(4) VALUE "0042".
                  10 WS-B REDEFINES WS-A PIC 9(4).
            """, "    DISPLAY WS-A \"|\" WS-B."));   // 0042|0042 — both nested views read the shared backing

    [Fact]
    public void InGroup_WriteNestedNumericView_ReadNestedAlphaCanonical()
        // Write through a nested numeric view, read through the nested alphanumeric canonical — both directly from the
        // PROCEDURE DIVISION (the CCVS COMPUTED-N ← value, then COMPUTED-A read pattern).
        => AssertMatchesGolden(Program("""
            01 WS-REC.
               05 WS-X.
                  10 WS-A PIC X(6).
                  10 WS-B REDEFINES WS-A PIC 9(6).
            """, "    MOVE 1234 TO WS-B.\n    DISPLAY WS-A."));   // 001234 — view write visible through the canonical

    [Fact]
    public void InGroup_CompareNestedViewToSpace()
        // The CCVS BAIL-OUT comparison: IF nested-canonical NOT EQUAL TO SPACE — a direct relational over a nested
        // Tier-B view, the exact form that drove NC101A's 304 unqualified-backing errors.
        => AssertMatchesGolden(Program("""
            01 WS-REC.
               05 WS-X.
                  10 WS-A PIC X(4) VALUE SPACE.
                  10 WS-B REDEFINES WS-A PIC 9(4).
            """, "    IF WS-A EQUAL TO SPACE\n        DISPLAY \"BLANK\"\n    ELSE\n        DISPLAY \"FILLED\"."));   // BLANK
}
