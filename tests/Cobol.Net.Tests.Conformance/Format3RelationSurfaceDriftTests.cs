// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ THE DRIFT TEST FOR "ONE RELATION CHECKPOINT" (kb/Work PB399).
/// <para>ISO §8.8.4.2's operand rules belong to the relation CONDITION, not to the statement that writes one.
/// For EVALUATE, §14.9.13.3 SR7 a) delegates to §8.8.4.2 by name (kb/Work PB1468 removed a "§14.9.13.4 GR2"
/// quotation here that is in no clause of the standard): the selection
/// objects "shall be valid operands for comparison to the corresponding operand in the set of selection
/// subjects in accordance with 8.8.4.2, Simple relation conditions."</para>
/// <para>The §8.8.4.2.2 Format 3 band (message-tag / object / pointer operands) used to be written inside
/// <c>ConditionBinder.BindComparison</c>'s relation arm — ONE caller of the
/// <c>StatementValidation.CheckRelationalOperands</c> checkpoint — so it screened the pair written as an IF and
/// said nothing about the identical pair written as an EVALUATE selection object, an EVALUATE range, a SEARCH
/// WHEN condition or a PERFORM UNTIL condition. Measured before the move: <c>EVALUATE WS-P WHEN WS-X</c>
/// compiled clean and failed in the BACKEND as a raw C# <c>CS1503</c>, and <c>EVALUATE WS-P WHEN WS-Q THRU
/// WS-R</c> emitted code that ordered raw addresses.</para>
/// <para>⛔ WHAT THIS TEST EXISTS TO CATCH is not the bug — the negative corpus holds one case per surface for
/// that — but the REFACTOR that quietly puts the rule back in one caller. Every surface here lowers to the same
/// <c>BoundRelational</c> construction, so they either all report or the checkpoint has moved. It flips the
/// axis the subject holds fixed: the OPERAND PAIR is identical in every leg and only the SURFACE varies.</para>
/// </summary>
public sealed class Format3RelationSurfaceDriftTests
{
    /// <summary>One program per relation surface, all over the SAME pointer-against-alphanumeric pair, which
    /// §8.8.4.2.3 SR5 refuses ("Identifier-3 and identifier-4 shall reference data items of class message-tag,
    /// object, or pointer, and shall be of the same category").</summary>
    private static string Surface(string pid, string body) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-P USAGE POINTER.
        01 WS-Q USAGE POINTER.
        01 WS-R USAGE POINTER.
        01 WS-X PIC X(3) VALUE "ABC".
        *> ⚠ THE TABLE ELEMENT IS NUMERIC, NOT A POINTER, ON PURPOSE. ISO §13.18.60.3 SR14 admits a USAGE
        *> POINTER item only "for an elementary data item at level 1 or an elementary data item subordinate to
        *> a type declaration that includes the STRONG phrase", so a table OF pointers is illegal source and a
        *> SEARCH leg written over one would reject for §13.18.60.3's reason and look like evidence for
        *> §8.8.4.2's (feedback_green_gates_arent_evidence). The SEARCH legs carry the pointer pair in the
        *> WHEN CONDITION instead, which is the surface under test.
        01 WS-T.
           05 WS-E OCCURS 3 TIMES INDEXED BY WS-I.
              10 WS-E-N PIC 9.
        PROCEDURE DIVISION.
        MAIN.
        {body}
            STOP RUN.
        """;

    [Theory]
    // The written relation condition — the ONE surface the band ever screened.
    [InlineData("PB399S1", "    IF WS-P = WS-X DISPLAY \"X\" END-IF.")]
    // An EVALUATE selection pair (§14.9.13.3 SR7 a) — "valid operands for comparison … in accordance with 8.8.4.2").
    [InlineData("PB399S2", "    EVALUATE WS-P WHEN WS-X DISPLAY \"X\" END-EVALUATE.")]
    // A PERFORM UNTIL condition.
    [InlineData("PB399S3", "    PERFORM UNTIL WS-P = WS-X CONTINUE END-PERFORM.")]
    // A SEARCH WHEN condition.
    [InlineData("PB399S4",
        "    SET WS-I TO 1.\n    SEARCH WS-E AT END CONTINUE WHEN WS-P = WS-X CONTINUE END-SEARCH.")]
    // Inside a combined condition — the boolean-alternative lowering, a third CheckedRelational caller.
    [InlineData("PB399S5", "    IF WS-P = WS-X AND WS-Q = WS-R DISPLAY \"X\" END-IF.")]
    public void EverySurface_ReportsTheFormat3OperandRule(string pid, string body)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Surface(pid, body), 2023);
        Assert.False(ok, $"{pid}: a pointer-against-alphanumeric relation is refused by ISO §8.8.4.2.3 SR5 at "
            + "EVERY surface that lowers to a relation — this one compiled clean, so the band has moved back "
            + "into one caller of the checkpoint");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0869");
    }

    /// <summary>The ORDERING half of Format 3, at the same surfaces. §8.8.4.2.2 Format 3 prints only
    /// <c>IS [NOT] EQUAL TO</c> / <c>=</c> / <c>&lt;&gt;</c>, so an ordering operator over a class-pointer
    /// operand is inadmissible wherever the relation is written.</summary>
    [Theory]
    [InlineData("PB399O1", "    IF WS-P >= WS-Q DISPLAY \"X\" END-IF.")]
    [InlineData("PB399O2", "    PERFORM UNTIL WS-P >= WS-Q CONTINUE END-PERFORM.")]
    [InlineData("PB399O3",
        "    SET WS-I TO 1.\n    SEARCH WS-E AT END CONTINUE WHEN WS-P >= WS-Q CONTINUE END-SEARCH.")]
    public void EverySurface_ReportsTheFormat3OrderingRule(string pid, string body)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Surface(pid, body), 2023);
        Assert.False(ok, $"{pid}: ISO §8.8.4.2.2 Format 3 prints no ordering operator for a pointer relation");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET0869");
    }

    /// <summary>⛔ THE COMPLEMENT LEG, so the theories above cannot pass by rejecting everything
    /// (feedback_measure_the_selectors_complement). The SAME surfaces over a LEGAL Format 3 pair — two data
    /// pointers, and a data pointer against the predefined NULL, which §8.4.3.10.3 SR1 a) admits by name "in a
    /// pointer-or-object-reference relation condition" — shall compile.</summary>
    [Theory]
    [InlineData("PB399L1", "    IF WS-P = WS-Q DISPLAY \"X\" END-IF.")]
    [InlineData("PB399L2", "    IF WS-P NOT = NULL DISPLAY \"X\" END-IF.")]
    [InlineData("PB399L3", "    EVALUATE WS-P WHEN WS-Q DISPLAY \"X\" END-EVALUATE.")]
    [InlineData("PB399L4", "    EVALUATE WS-P WHEN NULL DISPLAY \"X\" END-EVALUATE.")]
    [InlineData("PB399L5",
        "    SET WS-I TO 1.\n    SEARCH WS-E AT END CONTINUE WHEN WS-P = WS-Q CONTINUE END-SEARCH.")]
    public void EverySurface_AdmitsALegalFormat3Pair(string pid, string body)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Surface(pid, body), 2023);
        Assert.True(ok, $"{pid}: a data-pointer equality is the relation §8.8.4.2.16 defines — "
            + string.Join(" | ", errors));
    }

    /// <summary>ISO §14.9.13.3 SR2's two directions, and the shape that made only one of them reportable: the
    /// count is a property of the two SETS, not something a pairing loop notices when it runs out of subjects.
    /// The FEWER direction had no diagnostic at any stage before kb/Work PB399.</summary>
    [Theory]
    [InlineData("PB399C1", "    EVALUATE WS-N WHEN 1 ALSO 2 CONTINUE END-EVALUATE.")]
    [InlineData("PB399C2", "    EVALUATE WS-N ALSO WS-M WHEN 1 CONTINUE END-EVALUATE.")]
    [InlineData("PB399C3", "    EVALUATE WS-N ALSO WS-M WHEN 1 ALSO 2 WHEN 3 CONTINUE END-EVALUATE.")]
    public void SelectionObjectCount_BothDirections_Report2106(string pid, string body)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {pid}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-N PIC 9 VALUE 1.
            01 WS-M PIC 9 VALUE 2.
            PROCEDURE DIVISION.
            MAIN.
            {body}
                STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, 2023);
        Assert.False(ok, $"{pid}: ISO §14.9.13.3 SR2 is an EQUALITY — both directions are violations");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET2106");
    }

    /// <summary>The count rule is edition-INVARIANT: EVALUATE and its ALSO phrase are COBOL-85 constructs and
    /// no edition relaxes SR2, so the diagnostic is owed at every edition (kb/Work PB399 measured the defect at
    /// <c>--std 85</c> and at 2023 alike). A screen accidentally gated to one edition is the
    /// feedback_edition_gate_sweep shape.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void SelectionObjectCount_IsRaisedAtEveryEdition(int edition)
    {
        string src = $"""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB399E{edition}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-N PIC 9 VALUE 1.
            01 WS-M PIC 9 VALUE 2.
            PROCEDURE DIVISION.
            MAIN.
                EVALUATE WS-N ALSO WS-M
                    WHEN 1
                        CONTINUE
                END-EVALUATE.
                STOP RUN.
            """;
        var (ok, errors, _) = EditionHarness.CompileFull(src, edition);
        Assert.False(ok, $"ISO §14.9.13.3 SR2 is not edition-gated (--std {edition})");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET2106");
    }

    /// <summary>ISO §14.9.13.3 SR9 keys on §8.5.1.12.1's definition of a variable-length group — "a group item
    /// whose data description has at least one dynamic-length elementary item or dynamic-capacity table as a
    /// subordinate item" — and an OCCURS DEPENDING ON group is NOT one, its size being its maximum.
    /// <para>⛔ THIS IS THE PREMISE LEG. kb/Work PB399's own repro program used an ODO group and called it a
    /// variable-length group; a screen written from that repro would have rejected legal source and left the
    /// shape the rule actually names untouched. Both directions are asserted here so the screen cannot drift
    /// onto the wrong definition.</para></summary>
    [Fact]
    public void RangeOperand_OdoGroupIsLegal_DynamicLengthGroupIsNot()
    {
        const string odo = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB399VG1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-S PIC X(3) VALUE "BBB".
            01 WS-N PIC 9 VALUE 3.
            01 WS-G1.
               05 WS-G1-C PIC X OCCURS 1 TO 5 DEPENDING ON WS-N.
            01 WS-G2.
               05 WS-G2-C PIC X OCCURS 1 TO 5 DEPENDING ON WS-N.
            PROCEDURE DIVISION.
            MAIN.
                EVALUATE WS-S WHEN WS-G1 THRU WS-G2 CONTINUE END-EVALUATE.
                STOP RUN.
            """;
        const string dyn = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB399VG2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-S PIC X(3) VALUE "BBB".
            01 WS-G1.
               05 WS-G1-A PIC X(3).
               05 WS-G1-D PIC X DYNAMIC LENGTH.
            01 WS-G2.
               05 WS-G2-A PIC X(3).
               05 WS-G2-D PIC X DYNAMIC LENGTH.
            PROCEDURE DIVISION.
            MAIN.
                EVALUATE WS-S WHEN WS-G1 THRU WS-G2 CONTINUE END-EVALUATE.
                STOP RUN.
            """;
        var (okOdo, odoErrors, _) = EditionHarness.CompileFull(odo, 2023);
        Assert.True(okOdo, "an OCCURS DEPENDING ON group is a FIXED-length group (ISO §8.5.1.12.1), so ISO "
            + "§14.9.13.3 SR9 does not reach it — " + string.Join(" | ", odoErrors));
        var (okDyn, dynErrors, _) = EditionHarness.CompileFull(dyn, 2023);
        Assert.False(okDyn, "a group with a DYNAMIC LENGTH member IS a variable-length group (§8.5.1.12.1)");
        EditionHarness.AssertHasDiagnostic(dynErrors, "COBOLNET2107");
    }
}
