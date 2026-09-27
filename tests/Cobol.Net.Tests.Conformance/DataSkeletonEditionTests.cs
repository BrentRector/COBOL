// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The W2 data-skeleton × edition matrix (roadmap Phase 2; VERSION_TEST_MATRIX introduction invariants). A
/// 2002-introduced data construct that WiseOwl COBOL recognizes must NEVER compile silently: below 2002 the
/// ConstructRegistry introduction gate rejects (COBOLNET0900 naming COBOL-2002); at 2002/2014/2023 it either
/// WORKS or says so loudly. Before this sweep each of these silently misbound to USAGE DISPLAY / "pure numeric,
/// zero digits".
/// <para>
/// ⛔ THE STAGED SET IS NOW EMPTY, and it is asserted empty rather than described that way —
/// <see cref="NoDataSkeleton_StagesAt0899"/> compiles every shape that was ever in it and fails if any of them
/// returns to a COBOLNET0899 "not yet implemented" posture. The two theories that used to enumerate the staged
/// rows are gone with the last row: USAGE OBJECT REFERENCE left at the Phase-3 OO spine, USAGE POINTER at Phase
/// 4b, the BINARY-CHAR family at Phase 4 M2-DATA-1, NATIONAL (§8.5.2.10) and BOOLEAN (§8.5.2.5) data at Phase 4a
/// M2-DATA-3/4, the FLOAT-SHORT trio at Phase 6a, the PICTURE symbol E with kb/Work PB66, NATIONAL-EDITED with
/// PB492, and — last — the national-FORM numeric / numeric-edited / boolean shapes of ISO §13.18.60.3 SR12 with
/// kb/Work PB646. Each kept the introduction edge instead: it compiles at 2002+ and 0900s at 85, one positive
/// fact per construct below. A construct that is ever staged loud again re-adds its own row and its own theory.
/// </para>
/// </summary>
public sealed class DataSkeletonEditionTests
{
    /// <summary>A minimal program whose WORKING-STORAGE carries the construct under test.</summary>
    private static string Prog(string pid, string wsEntry) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {wsEntry}
        PROCEDURE DIVISION.
        MAIN-P.
            STOP RUN.
        """;

    /// <summary>⛔ THE SUCCESSOR TO THE STAGED-ROW THEORIES, and the reason deleting them is not a loss of
    /// coverage: EVERY shape that ever sat in the staged set compiles at 2023 with no COBOLNET0899 in its
    /// errors OR its warnings. A re-staged data construct fails here even if nobody remembers to re-add a row.
    /// <para>The last member to leave was the national FORM of ISO §13.18.60.3 SR12 — "An elementary data item
    /// with usage national shall be described with a picture character-string that describes a boolean,
    /// national, national-edited, numeric, or numeric-edited data item" — whose numeric, numeric-edited and
    /// boolean shapes were refused BY NAME as a "Phase 4a residue" (kb/Work PB646). A GREEN theory row asserted
    /// that refusal, which is how a staged loud reads as a decision
    /// (<c>feedback_green_test_can_hold_a_gap_open</c>); the shapes are asserted to WORK here and in
    /// <c>NationalBooleanDataTests.LiveNationalFormShape_CompilesAtNationalBearingEditions</c>.</para></summary>
    [Fact]
    public void NoDataSkeleton_StagesAt0899()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Prog("DSKLIVE", """
            01 WS-OREF USAGE OBJECT REFERENCE.
            01 WS-PTR  USAGE POINTER.
            01 WS-BC   USAGE BINARY-CHAR SIGNED.
            01 WS-FS   USAGE FLOAT-SHORT.
            01 WS-FL   USAGE FLOAT-LONG.
            01 WS-FX   USAGE FLOAT-EXTENDED.
            01 WS-EF   PIC +9.99E+99.
            01 WS-N    PIC N(4).
            01 WS-NE   PIC NNBNN.
            01 WS-B    PIC 1(8).
            01 WS-BIT  PIC 1(4) USAGE BIT.
            01 WS-NNUM PIC 9(4) USAGE NATIONAL.
            01 WS-NED  PIC ZZ9 USAGE NATIONAL.
            01 WS-NBOO PIC 1(4) USAGE NATIONAL.
            """), 2023);
        Assert.True(ok, "every data construct that ever sat in the W2 skeleton set is LIVE: "
            + string.Join("\n", errors));
        EditionHarness.AssertNoDiagnostic(errors, "COBOLNET0899");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0899");
    }

    /// <summary>The national FORM of §13.18.60.3 SR12 keeps the introduction edge the rest of the set keeps
    /// (kb/Work PB646, registry row national-data-2002): each shape compiles at every 2002+ edition and is
    /// rejected at 85 with COBOLNET0900 NAMING COBOL-2002 — never the historical silent DISPLAY misbind, and
    /// never the COBOLNET0899 staging this row used to assert.</summary>
    [Theory]
    [InlineData("01 WS-A PIC 9(4) USAGE NATIONAL.")]
    [InlineData("01 WS-A PIC S9(4) USAGE NATIONAL SIGN IS LEADING SEPARATE.")]
    [InlineData("01 WS-A PIC ZZ9 USAGE NATIONAL.")]
    [InlineData("01 WS-A PIC 1(4) USAGE NATIONAL.")]
    public void NationalFormPicture_CompilesAt2002Plus_RejectedAt85(string wsEntry)
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Prog("DSKNF" + edition, wsEntry), edition);
            Assert.True(ok, $"a national-form picture must compile at --std {edition}: {string.Join("\n", errors)}");
            EditionHarness.AssertNoDiagnostic(errors, "COBOLNET0899");
        }
        var (ok85, errors85, _) = EditionHarness.CompileFull(Prog("DSKNF85", wsEntry), 85);
        Assert.False(ok85, "national data is a 2002 introduction — rejected at --std 85");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOLNET0900");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOL-2002");
    }

    /// <summary>USAGE OBJECT REFERENCE went LIVE with the Phase-3 OO spine (ISO §13.18.60.4 / §8.5.2.14): a
    /// universal (class-less) reference item compiles at every 2002+ edition — a nullable <c>object?</c> field,
    /// COBOL initial state NULL — and stays introduction-gated at 85 (the <c>{is2002()}?</c> grammar hook +
    /// the W1.5 0900 edition-naming hint). The former recognized-but-unimplemented 0899 posture is retired.</summary>
    [Fact]
    public void UsageObjectReference_Universal_CompilesAt2002Plus_0900At85()
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(
                Prog("DSKOREF" + edition, "01 WS-O USAGE OBJECT REFERENCE."), edition);
            Assert.True(ok, $"a universal USAGE OBJECT REFERENCE item must compile at --std {edition}: "
                + string.Join("\n", errors));
        }
        var (ok85, errors85, _) = EditionHarness.CompileFull(Prog("DSKOREF85", "01 WS-O USAGE OBJECT REFERENCE."), 85);
        Assert.False(ok85, "USAGE OBJECT REFERENCE is a 2002 introduction — rejected at --std 85");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOLNET0900");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOL-2002");
    }

    /// <summary>PICTURE is prohibited with USAGE OBJECT REFERENCE (§13.18.60.4 — the item is picture-less);
    /// the conflict diagnoses COBOLNET0812, never an incoherent picture-with-reference classification.</summary>
    [Fact]
    public void UsageObjectReference_WithPicture_Rejects0812()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(
            Prog("DSKOREFP", "01 WS-O PIC X(4) USAGE OBJECT REFERENCE."), 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0812");
    }

    /// <summary>The floating-point numeric-edited PICTURE went LIVE with data-model design D21 (kb/Work PB66; ISO
    /// §13.18.40.4 GR13 b — a significand character-string and an exponent +9{1..4} joined by E; §14.6.8.4 the
    /// store): it compiles at every 2002+ edition and stays introduction-gated at 85 (COBOLNET0900 naming
    /// COBOL-2002 — registry row pic-external-float-2002). The former 0899 not-implemented posture is retired.</summary>
    [Theory]
    [InlineData("01 WS-EF PIC +9.99E+99.")]
    [InlineData("01 WS-EF PIC -9(3).9(2)E+999 VALUE 1.5E+3.")]
    [InlineData("01 WS-EF PIC 9(3)E+9.")]
    public void FloatEditedPicture_CompilesAt2002Plus_RejectedAt85(string wsEntry)
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Prog("DSKFE" + edition, wsEntry), edition);
            // (a VALUE numeric literal on a numeric-edited item is itself a 2023 capability — VCR row 86 — so the
            // VALUE-bearing shape is checked at 2023 only)
            if (wsEntry.Contains("VALUE") && edition < 2023) continue;
            Assert.True(ok, $"a floating-point numeric-edited picture must compile at --std {edition}: {string.Join("\n", errors)}");
        }
        var (ok85, errors85, _) = EditionHarness.CompileFull(Prog("DSKFE85", wsEntry), 85);
        Assert.False(ok85, "the floating-point numeric-edited PICTURE is a 2002 introduction — rejected at --std 85");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOLNET0900");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOL-2002");
    }

    /// <summary>NATIONAL-EDITED went LIVE with kb/Work PB492 (ISO §8.5.2.11 / §13.18.40.4 GR10 — "at least one
    /// symbol 'N', and at least one instance of character-1 or one of the symbols from the set 'B', '0', '/'";
    /// §13.18.40.5 Table 7 gives the category SIMPLE INSERTION): every one of those shapes compiles at every
    /// 2002+ edition and stays introduction-gated at 85 (COBOLNET0900 naming COBOL-2002 — registry row
    /// national-edited-2002). The former 0899 not-implemented posture is retired, and so is the COBOLNET0808 that
    /// refused the character-1 leg by quoting a Table-10 reading GR10 contradicts.</summary>
    [Theory]
    [InlineData("01 WS-NE PIC NNBNN.")]
    [InlineData("01 WS-NE PIC NN0NN.")]
    [InlineData("01 WS-NE PIC N/N.")]
    [InlineData("01 WS-NE PIC N(2)B0/N(2).")]
    [InlineData("01 WS-NE PIC NNTNN EDITING T IS N\":\".")]
    public void NationalEditedPicture_CompilesAt2002Plus_RejectedAt85(string wsEntry)
    {
        // The COBOL-2023 PICTURE EDITING phrase is itself a 2023 introduction (registry row picture-editing-2023),
        // so the character-1 shape is exercised at 2023 only — its 85 leg is the EDITING phrase's own 0900.
        bool editing = wsEntry.Contains("EDITING");
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            if (editing && edition < 2023) continue;
            var (ok, errors, _) = EditionHarness.CompileFull(Prog("DSKNE" + edition, wsEntry), edition);
            Assert.True(ok, $"a national-edited picture must compile at --std {edition}: {string.Join("\n", errors)}");
        }
        var (ok85, errors85, _) = EditionHarness.CompileFull(Prog("DSKNE85", wsEntry), 85);
        Assert.False(ok85, "national-edited data is a 2002 introduction — rejected at --std 85");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOLNET0900");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOL-2002");
    }

    /// <summary>The BINARY-CHAR family went LIVE with Phase 4 M2-DATA-1 (ISO §13.18.60.4 GR12): a PICTURE-less
    /// fixed-width binary item compiles at every 2002+ edition (a native 1/2/4/8-byte integer) and stays
    /// introduction-gated at 85 (COBOLNET0900 naming COBOL-2002). The former 0899 not-implemented posture is
    /// retired.</summary>
    [Theory]
    [InlineData("01 WS-C BINARY-CHAR SIGNED.")]
    [InlineData("01 WS-D USAGE IS BINARY-CHAR SIGNED.")]
    [InlineData("01 WS-E USAGE BINARY-SHORT.")]
    [InlineData("01 WS-F USAGE BINARY-LONG UNSIGNED.")]
    [InlineData("01 WS-G USAGE BINARY-DOUBLE.")]
    public void BinaryCharFamily_CompilesAt2002Plus_RejectedAt85(string wsEntry)
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Prog("DSKBIN" + edition, wsEntry), edition);
            Assert.True(ok, $"a fixed-width binary usage must compile at --std {edition}: {string.Join("\n", errors)}");
        }
        var (ok85, errors85, _) = EditionHarness.CompileFull(Prog("DSKBIN85", wsEntry), 85);
        Assert.False(ok85, "the BINARY-CHAR family is a 2002 introduction — rejected at --std 85");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOLNET0900");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOL-2002");
    }

    /// <summary>NATIONAL data went LIVE at Phase 4a M2-DATA-3 (ISO §8.5.2.10 / §13.18.40.4 GR9 /
    /// §13.18.60.4 SR13a): a PIC N item (usage implied NATIONAL) compiles at every 2002+ edition — one UTF-16
    /// char per national position (D-N1) — and stays introduction-gated at 85 (COBOLNET0900 naming COBOL-2002).
    /// The former 0899 not-implemented posture is retired.</summary>
    [Theory]
    [InlineData("01 WS-N PIC N(4).")]
    [InlineData("01 WS-N PIC N(4) USAGE NATIONAL.")]
    public void NationalData_CompilesAt2002Plus_RejectedAt85(string wsEntry)
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Prog("DSKNAT" + edition, wsEntry), edition);
            Assert.True(ok, $"a national item must compile at --std {edition}: {string.Join("\n", errors)}");
        }
        var (ok85, errors85, _) = EditionHarness.CompileFull(Prog("DSKNAT85", wsEntry), 85);
        Assert.False(ok85, "national data is a 2002 introduction — rejected at --std 85");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOLNET0900");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOL-2002");
    }

    /// <summary>BOOLEAN data went LIVE at Phase 4a M2-DATA-4 (ISO §8.5.2.5 / §13.18.40.4 GR8/GR14 R14 /
    /// §13.18.60.4 SR5): a PIC 1 item (display- or bit-form — the same one-'0'/'1'-character-per-position
    /// representation, D-B1) compiles at every 2002+ edition and stays introduction-gated at 85. The former
    /// 0899 not-implemented posture is retired.</summary>
    [Theory]
    [InlineData("01 WS-B PIC 1(8).")]
    [InlineData("01 WS-B PIC 1(4) USAGE BIT.")]
    public void BooleanData_CompilesAt2002Plus_RejectedAt85(string wsEntry)
    {
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(Prog("DSKBOOL" + edition, wsEntry), edition);
            Assert.True(ok, $"a boolean item must compile at --std {edition}: {string.Join("\n", errors)}");
        }
        var (ok85, errors85, _) = EditionHarness.CompileFull(Prog("DSKBOOL85", wsEntry), 85);
        Assert.False(ok85, "boolean data is a 2002 introduction — rejected at --std 85");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOLNET0900");
        EditionHarness.AssertHasDiagnostic(errors85, "COBOL-2002");
    }

    /// <summary>The bare and full USAGE spellings bind IDENTICALLY (ISO §13.18.60 general format — the USAGE
    /// word is optional): bare <c>BINARY-CHAR SIGNED</c> historically string-glued to "BINARY-CHARSIGNED" and
    /// silently misbound to DISPLAY; now both PICTURE-less spellings COMPILE cleanly at 2002+ (Phase 4
    /// M2-DATA-1) — restoring the parity the string-glue bug broke.</summary>
    [Fact]
    public void BareAndFullBinaryChar_BindIdentically()
    {
        var (okBare, bareErrors, _) = EditionHarness.CompileFull(Prog("DSKBARE1", "01 WS-C BINARY-CHAR SIGNED."), 2023);
        var (okFull, fullErrors, _) = EditionHarness.CompileFull(Prog("DSKFULL1", "01 WS-C USAGE IS BINARY-CHAR SIGNED."), 2023);
        Assert.True(okBare, "bare BINARY-CHAR SIGNED must compile: " + string.Join("\n", bareErrors));
        Assert.True(okFull, "USAGE IS BINARY-CHAR SIGNED must compile: " + string.Join("\n", fullErrors));
    }

    /// <summary>PICTURE is prohibited with the BINARY-CHAR family (ISO §13.16.3 SR8 — the item is picture-less);
    /// the conflict diagnoses COBOLNET0870, never an incoherent picture-with-binary classification (mirrors the
    /// USAGE OBJECT REFERENCE 0812 rule).</summary>
    [Fact]
    public void BinaryChar_WithPicture_Rejects0870()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Prog("DSKBINP", "01 WS-C PIC S9(4) USAGE BINARY-CHAR."), 2002);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0870");
    }

    /// <summary>A symbol outside the §13.18.40.3 SR2 PICTURE whitelist is an invalid PICTURE (COBOLNET0808) at
    /// EVERY edition — previously it fell through the classifier to "pure numeric, zero digits".</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void InvalidPictureSymbol_0808AtEveryEdition(int edition)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(
            Prog("DSKQ" + edition, "01 WS-Q PIC 9Q9."), edition);
        Assert.False(ok, $"PIC 9Q9 must be rejected at --std {edition} (ISO §13.18.40.3 SR2)");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0808");
        EditionHarness.AssertHasDiagnostic(errors, "invalid PICTURE symbol");
    }

    /// <summary>A class definition went LIVE with the Phase-3 OO spine part 2 (ISO §11.2/§11.3 — the ClassUnit
    /// collection + pass-1 class symbol table): a class-only compilation unit COMPILES clean at every 2002+
    /// edition (the emitted module carries the class and an empty Main — a class translation unit with no
    /// program is legal, §10.6), and stays grammar-gated at 85 (the <c>{is2002()}?</c> hook + the W1.5 0900
    /// mapping). The former recognized-but-unimplemented 0899 posture (and before that the SILENT DROP the W2
    /// loud-guard sweep caught) is retired.</summary>
    [Fact]
    public void ClassDefinition_CompilesAt2002Plus_RejectedAt85()
    {
        const string cls = """
            IDENTIFICATION DIVISION.
            CLASS-ID. DSKCLS1.
            END CLASS DSKCLS1.
            """;
        foreach (int edition in new[] { 2002, 2014, 2023 })
        {
            var (ok, errors, _) = EditionHarness.CompileFull(cls, edition);
            Assert.True(ok, $"a class-only compilation unit must compile at --std {edition} (ISO §10.6): "
                + string.Join("\n", errors));
        }
        var (ok85, _, _) = EditionHarness.CompileFull(cls, 85);
        Assert.False(ok85, "a class definition must be rejected at --std 85 (grammar-gated OO/2002)");
    }

    /// <summary>The zero-regression leg: the COBOL-85 corpus symbol/usage repertoire still compiles clean at
    /// every edition — the whitelist and the explicit usage map must not reject anything the '85 classifier
    /// accepted (ISO §13.18.40.4 / §13.18.60).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void EightyFiveRepertoire_StillCompilesClean(int edition)
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Prog("DSKOK" + edition, """
            01 WS-X PIC X(10).
            01 WS-N9 PIC S9(4)V99 COMP-3.
            01 WS-ED PIC ZZ9.99.
            01 WS-CR PIC 9(5)CR.
            01 WS-ST PIC ****.
            01 WS-SL PIC 99/99/99.
            01 WS-BI PIC 9(8) BINARY.
            """), edition);
        Assert.True(ok, $"--std {edition}: {string.Join("; ", errors)}");
        EditionHarness.AssertNoDiagnostic(warnings, "COBOLNET0899");
    }
}
