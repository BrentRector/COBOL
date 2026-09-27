// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The relation's operand-PAIR classification (kb/Work PB1469 / PB1467): a same-type strongly-typed pair compares
/// element by element (ISO §8.8.4.2.12), a pair with a variable-length group is screened by §8.5.1.12.1 and compares
/// by §8.8.4.2.17, everything else compares as one item each. The run-time VALUES of both shapes are the goldens
/// <c>2002/strong_group_element_order</c> and <c>2014/variable_length_group_relation</c>; these are the screens and the
/// shapes the goldens do not reach.
/// </summary>
public sealed class RelationPairShapeTests
{
    private const string VariableLengthDecl = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {0}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 G1.
           05 G1A PIC X(2) VALUE "AB".
           05 G1D PIC X DYNAMIC LENGTH.
        01 G3.
           05 G3A PIC X(3) VALUE "ABC".
           05 G3D PIC X DYNAMIC LENGTH.
        01 G4.
           05 G4A PIC X(2) VALUE "AB".
           05 G4D PIC X DYNAMIC LENGTH.
        PROCEDURE DIVISION.
        MAIN-PARA.
            IF {1} DISPLAY "Y" END-IF
            STOP RUN.
        """;

    private static (bool Ok, IReadOnlyList<string> Diag) CompileVariable(string id, string relation) =>
        EditionHarness.Compile(VariableLengthDecl.Replace("{0}", id).Replace("{1}", relation), 2014);

    /// <summary>§8.5.1.12.1 — "a variable-length group … may not undergo a comparison … unless the other operand is a
    /// compatible group": a literal and a figurative constant are not groups, in either operand position.</summary>
    [Theory]
    [InlineData("RPS01", "G1 = \"AB\"")]
    [InlineData("RPS02", "SPACES = G1")]
    [InlineData("RPS03", "G1 > ZERO")]
    public void VariableLengthGroup_ComparedWithANonGroup_Rejected2492(string id, string relation)
    {
        var (ok, diag) = CompileVariable(id, relation);
        Assert.False(ok, $"'{relation}' compares a variable-length group with a non-group (ISO §8.5.1.12.1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET2492");
    }

    /// <summary>§8.5.1.12.1 rule 3 with §8.5.1.12.2 — the two dynamic-length items must START at the same relative
    /// position; G3's starts at 3, G1's at 2, so the groups are not compatible and may not be compared.</summary>
    [Fact]
    public void VariableLengthGroups_WhoseDynamicItemsDoNotCorrespond_Rejected2492()
    {
        var (ok, diag) = CompileVariable("RPS04", "G1 < G3");
        Assert.False(ok, "G1 and G3 are not compatible groups (ISO §8.5.1.12.2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET2492");
    }

    /// <summary>The compatible pair is legal (§8.8.4.2.17) and compiles clean.</summary>
    [Fact]
    public void CompatibleVariableLengthGroups_CompileClean()
    {
        var (ok, diag) = CompileVariable("RPS05", "G1 = G4");
        Assert.True(ok, "G1 and G4 are compatible (ISO §8.5.1.12.1): " + string.Join("; ", diag));
    }

    /// <summary>§8.8.4.2.12 over a strongly-typed group with a DYNAMIC LENGTH leaf — a strong group may itself be
    /// variable-length, and the element order carries it: the leaf compares at its current length (§8.5.1.10.4) as an
    /// alphanumeric operand, the shorter one extended with spaces (§8.8.4.2.7), so "AB" = "AB " and "AB" &lt; "ABC".</summary>
    [Fact]
    public void StrongGroupWithDynamicLengthLeaf_ComparesTheLeafAtItsCurrentLength()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RPS06.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 L-T TYPEDEF STRONG.
               05 LD PIC X DYNAMIC LENGTH.
               05 LZ PIC X.
            01 C TYPE L-T.
            01 E TYPE L-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE "AB" TO LD OF C
                MOVE "AB " TO LD OF E
                IF C = E DISPLAY "L1 EQ" ELSE DISPLAY "L1 NE" END-IF
                MOVE "ABC" TO LD OF E
                IF C < E DISPLAY "L2 LT" ELSE DISPLAY "L2 GE" END-IF
                STOP RUN.
            """, 2014);
        Assert.True(ok, detail);
        Assert.Equal("L1 EQ\nL2 LT", stdout.Replace("\r\n", "\n").Trim());
    }

    /// <summary>A strong group's OCCURS table makes the number of elementary items arbitrary. The lowering nests
    /// logarithmically (halves, not a pair-by-pair chain): the chain form overflowed the compiler's stack at 1000
    /// leaves. The last of 1000 algebraically-compared S9(3) pairs decides, and an early pair decides first.</summary>
    [Fact]
    public void StrongGroupWithAThousandLeaves_CompilesAndComparesInOrder()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RPS08.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 B-T TYPEDEF STRONG.
               05 BX PIC S9(3) OCCURS 1000.
            01 A TYPE B-T.
            01 B TYPE B-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A = B DISPLAY "B1 EQ" ELSE DISPLAY "B1 NE" END-IF
                MOVE 1 TO BX OF B (1000)
                IF A < B DISPLAY "B2 LT" ELSE DISPLAY "B2 GE" END-IF
                MOVE -1 TO BX OF B (2)
                IF A > B DISPLAY "B3 GT" ELSE DISPLAY "B3 LE" END-IF
                IF A >= B DISPLAY "B4 GE" ELSE DISPLAY "B4 LT" END-IF
                STOP RUN.
            """, 2002);
        Assert.True(ok, detail);
        Assert.Equal("B1 EQ\nB2 LT\nB3 GT\nB4 GE", stdout.Replace("\r\n", "\n").Trim());
    }

    /// <summary>The OTHER arm of the same dispatch: an EVALUATE THRU range under EC-RANGE-INVALID checking used to take
    /// the character-image range node for every pair, so a strongly-typed range over a float leaf compared images.
    /// §14.9.13.4 GR4 a) 5. makes the range "selection-subject &gt;= left-part AND selection-subject &lt;= right-part",
    /// and each half is a §8.8.4.2.12 element comparison: -10 &lt;= -5 &lt;= 3 is IN, with checking on or off.</summary>
    [Theory]
    [InlineData("RPS09", ">>TURN EC-RANGE-INVALID CHECKING ON")]
    [InlineData("RPS10", "")]
    public void StrongGroupThroughRange_ComparesElementByElement(string id, string directive)
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun($$"""
            {{directive}}
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {{id}}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 F-T TYPEDEF STRONG.
               05 FX USAGE FLOAT-LONG.
            01 A TYPE F-T.
            01 LO TYPE F-T.
            01 HI TYPE F-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE -5 TO FX OF A
                MOVE -10 TO FX OF LO
                MOVE 3 TO FX OF HI
                EVALUATE A
                    WHEN LO THRU HI DISPLAY "IN"
                    WHEN OTHER DISPLAY "OUT"
                END-EVALUATE
                STOP RUN.
            """, 2023);
        Assert.True(ok, detail);
        Assert.Equal("IN", stdout.Trim());
    }

    /// <summary>The staged residue of the element walk: a DYNAMIC-CAPACITY table member has a per-operand run-time
    /// capacity, so its occurrences cannot be enumerated at bind — refused as recognized-not-implemented (COBOLNET0899
    /// <c>strong-group-comparison-member</c>) rather than compared by an image the group does not have.</summary>
    [Fact]
    public void StrongGroupWithDynamicCapacityTable_IsTheStagedMemberShape()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. RPS07.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 D-T TYPEDEF STRONG.
               05 DA PIC X(2).
               05 DT PIC X OCCURS DYNAMIC FROM 0 TO 4.
            01 A TYPE D-T.
            01 B TYPE D-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF A = B DISPLAY "EQ" END-IF
                STOP RUN.
            """, 2014);
        Assert.False(ok, "a strong-group relation over a dynamic-capacity table member is staged");
        EditionHarness.AssertHasDiagnostic(diag, "cannot yet compare element by element");
    }
}
