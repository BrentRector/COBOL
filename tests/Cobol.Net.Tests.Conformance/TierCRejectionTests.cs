// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The Tier-C mixed-usage-group image boundary (rearchitecture PHASE-11 Step C) — the loud-failure lock. A GROUP
/// with a non-image-capable leaf has no whole-group image, so the verbs that need one stage LOUD by name
/// (COBOLNET_DESIGN §1.4, §4.2). Step C routed the ~12 scattered emit guards through the ONE
/// <c>TierCIsland.Reason</c> source, and these facts are "a lock to flip against" — kb/Work PB164 wave 1
/// FLIPPED the COMP-5/BINARY-CHAR..DOUBLE arms, wave 2 the floats, and the R40 owner decision the INDEX leaf
/// (the 8-byte occurrence-number image; <c>DisplayIndexGroup_RendersVerbatimBytes</c> pins THAT working
/// behavior below), so the island's remaining boundaries are the VARIABLE-LENGTH group (the primary lock
/// fixture) and a POINTER/OBJECT-CLASS leaf (the R40 fleet's correction — every NUMERIC kind is in, the
/// pointer/object categories had no image until kb/Work PB244 gave them the ONE-WAY transfer image —
/// <c>DisplayPointerGroup_RendersPlaceholderPositions</c>/<c>MovePointerGroup_SendsPlaceholderPositions</c> pin it WORKING). DISPLAY of a COMPOSABLE variable-length group is NOT here: it renders the documented A.1 item-57
/// format (<c>2023/pb164_vlg_display</c>); the DISPLAY loud lives on the UNCOMPOSABLE shape
/// (<c>DisplayOdoTableOfDynamicElements_FailsLoudNotCs1061</c>). ACCEPT/STRING receivers and INSPECT's identifier-1
/// are BIND-screened by their own syntax rules (§14.9.1.3 SR6 / §14.9.43.3 SR11 / §14.9.22.3 SR1) and pinned as such.
/// </summary>
public sealed class TierCRejectionTests
{
    private static string Program(string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. TIERCREJ.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-G.
           05 WS-G-A PIC X(3).
           05 WS-G-D PIC X DYNAMIC LENGTH.
        01 WS-SRC  PIC X(7) VALUE "HELLOXX".
        01 WS-DEST PIC X(7).
        01 WS-CNT  PIC 9(2).
        PROCEDURE DIVISION.
        MAIN.
        {proc}
            STOP RUN.
        """;

    /// <summary>An operand its own SYNTAX RULE bars (§14.9.1.3 SR6 ACCEPT / §14.9.43.3 SR11 STRING /
    /// §14.9.25.3 SR9 MOVE) fails at BIND with the rule's diagnostic — earlier and more precise than the
    /// runtime island.</summary>
    private static void AssertBindRejected(string proc)
    {
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun(Program(proc));
        Assert.False(ok, "a variable-length-group operand its syntax rule bars shall fail at bind");
        Assert.Contains("variable-length", detail);
    }

    /// <summary>⛔ BOTH MOVE LEGS ARE BIND REJECTIONS, NOT RUNTIME LOUDS, AND THE STANDARD IS WHY (kb/Work
    /// PB393). They were <c>AssertLoudTierC</c> until MOVE's §8.5.1.12 screen existed, and that was the
    /// wrong posture rather than a stricter one: §14.9.25.3 SR9 is a SYNTAX RULE ("If identifier-1 or
    /// identifier-2 references a variable-length group then these groups shall be compatible groups as
    /// specified in 8.5.1.12"), and §8.5.1.12.1 states it over the OTHER operand — such a group "may not
    /// undergo ... a move operation, in either direction ... unless the other operand is a compatible group".
    /// <c>WS-SRC</c> and <c>WS-DEST</c> are ELEMENTARY, so neither leg can ever be that other operand, and
    /// §4.2.2 ¶2 requires a compile-time mechanism for a violated syntax rule. A COMPATIBLE pair now moves
    /// rather than aborting (<c>conformance:2014/pb393_move_varlen_group</c>), so the Tier-C island no longer
    /// owns MOVE at all — its remaining MOVE-adjacent lock is the pointer/object-class arm below.
    /// ⛔ Do NOT "restore" these to a runtime loud; the loud was the defect.</summary>
    [Fact] public void MoveIntoGroup_BindRejected() => AssertBindRejected("    MOVE WS-SRC TO WS-G.");

    /// <inheritdoc cref="MoveIntoGroup_BindRejected"/>
    [Fact] public void MoveGroupToElementary_BindRejected() => AssertBindRejected("    MOVE WS-G TO WS-DEST.");

    /// <summary>INSPECT joins the MOVE legs for the same reason (kb/Work PB856): ISO §14.9.22.3 SR1 admits as
    /// identifier-1 only "an alphanumeric or national group item or an elementary item described implicitly or
    /// explicitly as usage display or national", and a variable-length group is neither of the two group kinds
    /// it names (§3.11 excludes it from "alphanumeric group item" by name). A SYNTAX rule, so a bind rejection
    /// (COBOLNET1626) — the runtime Tier-C loud this used to assert was the binder admitting ANY group.</summary>
    [Fact] public void InspectGroup_BindRejected() => AssertBindRejected("    INSPECT WS-G REPLACING ALL \"A\" BY \"B\".");
    [Fact] public void StringIntoGroup_BindRejected() => AssertBindRejected("    STRING WS-SRC DELIMITED BY SIZE INTO WS-G.");
    [Fact] public void AcceptIntoGroup_BindRejected() => AssertBindRejected("    ACCEPT WS-G.");

    /// <summary>The POINTER/OBJECT-CLASS leg, pinned WORKING (kb/Work PB244). A strongly-typed group with a class
    /// pointer leaf is a legal DISPLAY identifier-1 (ISO §14.9.11.3 SR1 bars only an item OF class message-tag,
    /// object or pointer; a strongly-typed group's class is its type-name, §8.5.2.1) and a legal MOVE sender
    /// (§14.9.25.3 SR2 constrains only a strongly-typed RECEIVER). Both used to compile and abort at run time with
    /// the Tier-C loud — a green test pinned that refusal. They now transfer the group's ONE-WAY storage image:
    /// the pointer leaf contributes its 8 reserved placeholder positions (spaces), exactly what the same group
    /// shows from a BASED storage cell (D-SLOT; CONFORMANCE.md A.1 item 56).
    /// <para>⛔ THE GROUP IS A STRONG TYPEDEF, AND THAT IS FORCED BY THE STANDARD: §13.18.60.3 SR14 admits a
    /// POINTER usage only at level 1 or subordinate to a type declaration that includes the STRONG phrase
    /// (COBOLNET1724 rejects the ordinary-group spelling). Do NOT relax that screen to simplify a fixture.</para>
    /// <para>What stays refused is NOT this: comparison and every read-back ask the two-way capability, because
    /// the placeholder image is neither injective nor invertible (<c>DataItem.TransferImageCapable</c>).</para></summary>
    private static string PointerGroupRun(string proc)
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun($$"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TIERCRE5.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 GPT IS TYPEDEF STRONG.
               05 WS-GP-A PIC X(3).
               05 WS-GP-P USAGE POINTER.
            01 WS-GP TYPE GPT.
            01 WS-DST PIC X(13).
            PROCEDURE DIVISION.
            MAIN.
                MOVE "abc" TO WS-GP-A OF WS-GP.
            {{proc}}
                STOP RUN.
            """);
        Assert.True(ok, $"a pointer-leafed strong group is a legal transfer operand (kb/Work PB244): {detail}");
        return stdout.TrimEnd('\r', '\n');
    }

    [Fact] public void DisplayPointerGroup_RendersPlaceholderPositions() =>
        Assert.Equal("[abc        ]", PointerGroupRun("    DISPLAY \"[\" WS-GP \"]\"."));

    [Fact] public void MovePointerGroup_SendsPlaceholderPositions() =>
        Assert.Equal("[abc          ]", PointerGroupRun("    MOVE WS-GP TO WS-DST. DISPLAY \"[\" WS-DST \"]\"."));

    /// <summary>The R40 leg, pinned WORKING: an INDEX-leaf group displays its verbatim content — the leaf's
    /// occurrence number as 8 big-endian two's-complement bytes (the R40 pin; A.1 items 56 + 211). SET (one
    /// of the references §13.18.60.3 SR10 permits) seeds the value.</summary>
    [Fact]
    public void DisplayIndexGroup_RendersVerbatimBytes()
    {
        var (ok, stdout, _) = new CobolNetCompiler(2023).CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TIERCRE4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-T.
               05 WS-E PIC X OCCURS 5 INDEXED BY IX.
            01 WS-GI.
               05 WS-GI-A PIC X(3) VALUE "ABC".
               05 WS-GI-N USAGE INDEX.
            PROCEDURE DIVISION.
            MAIN.
                SET IX TO 3.
                SET WS-GI-N TO IX.
                DISPLAY WS-GI.
                STOP RUN.
            """);
        Assert.True(ok, "an INDEX-leaf group DISPLAYs its verbatim content (the R40 pin)");
        Assert.StartsWith("ABC", stdout);
        for (int i = 3; i < 10; i++) Assert.Equal('\0', stdout[i]);   // occurrence number 3, 8 bytes big-endian
        Assert.Equal((char)3, stdout[10]);
    }

    /// <summary>PB164's DISPLAY leg for a COMP-5 leaf CLOSED with the image widening: the group has a whole
    /// image now, and DISPLAY transfers a group's character content VERBATIM (the A.1 item-56 determination —
    /// a group is class alphanumeric), so the COMP-5 leaf's two's-complement bytes appear raw in the output,
    /// exactly as GnuCOBOL renders such a group (the split-latitude tiebreaker). GR-14.9.11.4-4's COMP-5
    /// residue is discharged; the floats closed with wave 2 and the INDEX leg with R40 — every leaf-kind
    /// display leg is now a WORKING pin.</summary>
    [Fact]
    public void DisplayComp5Group_RendersVerbatimBytes()
    {
        var (ok, stdout, _) = new CobolNetCompiler(2023).CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TIERCRE2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-G5.
               05 WS-G5-A PIC X(3) VALUE "ABC".
               05 WS-G5-N USAGE COMP-5 PIC 9(4) VALUE 7.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY WS-G5.
                STOP RUN.
            """);
        Assert.True(ok, "an image-capable group DISPLAYs its verbatim content (kb/Work PB164 wave 1)");
        Assert.StartsWith("ABC", stdout);
        Assert.Equal('\0', stdout[3]);   // 0x0007 big-endian, verbatim
        Assert.Equal('\a', stdout[4]);
    }

    /// <summary>kb/Work PB176 — a group whose OCCURS DEPENDING table holds a dynamic-length ELEMENT member must
    /// COMPILE and stage the runtime Tier-C loud. Before the <c>PlaceRenderer.GroupImage</c> capability guard (the
    /// SEVENTH two-arm-dispatch instance — the write twin <c>WriteGroupImage</c> was guarded, the read side
    /// was not), the ODO sender path emitted <c>.AsImage()</c> on a struct that never receives one, and this
    /// legal source failed BACKEND compilation with CS1061 — the loud-failure rule violated in the worst
    /// direction. The lock pins the restored posture: compiles, throws Tier-C, names the dynamic mechanism.
    /// ⛔ The fixture is the ONE shape <c>DataItem.CurrentExtentImageCapable</c> still excludes (kb/Work PB244): an
    /// ODO table whose element carries a dynamic-length item has a RUN-TIME multiplicity of components. A group
    /// that merely holds an ODO table BESIDE a dynamic member composes — <c>DisplayOdoGroupWithDynamicMember_RendersCurrentExtent</c>.</summary>
    [Fact]
    public void DisplayOdoTableOfDynamicElements_FailsLoudNotCs1061()
    {
        var (ok, _, detail) = new CobolNetCompiler(2023).CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TIERCRE3.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-GO.
               05 WS-GO-N PIC 9(1) VALUE 2.
               05 WS-GO-T OCCURS 1 TO 5 DEPENDING ON WS-GO-N.
                  10 WS-GO-D PIC X DYNAMIC LENGTH.
                  10 WS-GO-F PIC X(3).
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY WS-GO.
                STOP RUN.
            """);
        Assert.False(ok, "an ODO table of dynamic elements has no whole-group image — loud, never CS1061 (kb/Work PB176)");
        Assert.Contains("Tier-C", detail);
        Assert.Contains("dynamic", detail);
    }

    /// <summary>kb/Work PB244 shape (b), ISO §14.9.11.4 GR7 (A.1 item 57): a group with BOTH an OCCURS DEPENDING
    /// table and a dynamic member composes — the dynamic item at its current content, the table at its CURRENT
    /// count (§13.18.38.4 GR8, here 2 of 5 occurrences of three characters). The pre-PB244 posture was the
    /// Tier-C loud this fact replaces.</summary>
    [Fact]
    public void DisplayOdoGroupWithDynamicMember_RendersCurrentExtent()
    {
        var (ok, stdout, detail) = new CobolNetCompiler(2023).CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TIERCRE4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-GO.
               05 WS-GO-N PIC 9(1) VALUE 2.
               05 WS-GO-D PIC X DYNAMIC LENGTH.
               05 WS-GO-T PIC X(3) OCCURS 1 TO 5 DEPENDING ON WS-GO-N.
            PROCEDURE DIVISION.
            MAIN.
                MOVE "ab" TO WS-GO-D.
                MOVE "xyz" TO WS-GO-T(1).
                MOVE "uvw" TO WS-GO-T(2).
                DISPLAY WS-GO.
                STOP RUN.
            """);
        Assert.True(ok, detail);
        Assert.Equal("2abxyzuvw", stdout.TrimEnd('\r', '\n'));
    }
}
