// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The TYPEDEF residue rules (Phase 6, data-model D17, increment 4; P10 Step 16 reworked the EXTERNAL leg):
/// COBOLNET1558 — the EXTERNAL type-declaration conformance rules (§13.18.22 GR2 level-1 reference; SR5
/// strong-external pairing — the former COBOLNET1534 stage is LIFTED, the accepted form is the
/// <c>typedef_external</c> golden); COBOLNET1535 — a strong group with a boolean/object/pointer element compared
/// with an ordering operator (a RENAMES inside a TYPEDEF is cloned into each reference, §13.18.58.4 GR1, kb/Work PB1304 —
/// its staged refusal was this code's other meaning) (§8.8.4.2.3 SR4 — the complete spec rule: equality/inequality only — asked of the CLASS, so a
/// program-pointer leaf is refused as a data-pointer one is). An ordering over same-type strong groups with a SIGNED
/// numeric leaf is LEGAL and compiles (§8.8.4.2.12 element by element — kb/Work PB1469 retired the COBOLNET0899
/// stage; the values are run-verified by the <c>strong_group_element_order</c> golden). A type whose OCCURS has an INDEXED BY
/// phrase referenced ≥2× is LEGAL and works — each clone declares its own index cells (kb/Work PB919; the former
/// COBOLNET1531 stage is retired). The positive companions (INDEXED-type references — also the
/// <c>typedef_indexed</c> golden — and a strong boolean-group EQUALITY compare) must NOT trip a guard.
/// </summary>
public sealed class TypedefResidueTests
{
    /// <summary>§13.18.22 GR2 — a data description containing an EXTERNAL type declaration shall be at
    /// level-number 1; a NESTED (level > 1) TYPE reference to an external type is COBOLNET1558. (The level-1
    /// form is ACCEPTED and run-verified by the <c>typedef_external</c> golden — the 1534 stage is lifted.)</summary>
    [Fact]
    public void ExternalTypeReferenceBelowLevel1_Rejected1558()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR58A.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 REC-T TYPEDEF EXTERNAL.
               05 F PIC X.
            01 G.
               05 R TYPE REC-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a below-level-1 reference to an EXTERNAL type must be rejected (ISO §13.18.22 GR2)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1558");
    }

    /// <summary>§13.18.22 SR5 — an EXTERNAL record described with a STRONG type requires that type declaration
    /// to be external too; a plain STRONG typedef on an EXTERNAL record is COBOLNET1558.</summary>
    [Fact]
    public void ExternalRecordWithNonExternalStrongType_Rejected1558()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR58B.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 REC-T TYPEDEF STRONG.
               05 F PIC X.
            01 R TYPE REC-T EXTERNAL.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2002);
        Assert.False(ok, "an EXTERNAL record of a non-external STRONG type must be rejected (ISO §13.18.22 SR5)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1558");
    }

    /// <summary>§13.18.58.4 GR1 — a level-66 RENAMES inside a TYPEDEF is "part of the type declaration of that type", so a
    /// TYPE reference compiles and owns its own alias (the staged COBOLNET1535 refusal is gone, kb/Work PB1304; the
    /// values are run-verified by the <c>pb1304_typedef_renames</c> golden).</summary>
    [Fact]
    public void RenamesInsideTypedef_IsPartOfTheType_AndCompiles()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR35R.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 REC-T TYPEDEF.
               05 A PIC X.
               05 B PIC X.
            66 RN RENAMES A THRU B.
            01 R TYPE REC-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2002);
        Assert.True(ok, "a RENAMES inside a TYPEDEF is part of the type and compiles (ISO §13.18.58.4 GR1): " + string.Join("; ", diag));
        Assert.DoesNotContain(diag, d => d.Contains("COBOLNET1535", StringComparison.Ordinal));
    }

    /// <summary>§8.8.4.2.3 SR4 — a strongly-typed group whose elements include class boolean / object / pointer may be
    /// compared only for equality; an ordering relation on such a group is staged loud (COBOLNET1535).</summary>
    [Fact]
    public void StrongBooleanGroupOrderingCompare_Rejected1535()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR35B.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BREC-T TYPEDEF STRONG.
               05 FLAG PIC 1.
            01 R1 TYPE BREC-T.
            01 R2 TYPE BREC-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF R1 < R2
                    DISPLAY "X"
                END-IF.
                STOP RUN.
            """, 2002);
        Assert.False(ok, "an ordering compare of a boolean-bearing strong group must be rejected (ISO §8.8.4.2.3 SR4)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1535");
    }

    /// <summary>§8.8.4.2.12 — an ORDERING relation between same-type strong groups with a SIGNED numeric leaf is
    /// legal (§8.8.4.2.3 SR4 restricts only boolean / message-tag / object / pointer contents) and compares element
    /// by element, the signed pair algebraically (§8.8.4.2.4). It was staged COBOLNET0899 while the relation was a
    /// whole-group image (kb/Work PB1469); the run-time VALUES are the <c>strong_group_element_order</c> golden's.</summary>
    [Fact]
    public void StrongSignedGroupOrderingCompare_CompilesClean()
    {
        const string decl = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {0}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 SREC-T TYPEDEF STRONG.
               05 AMT PIC S9(3).
            01 R1 TYPE SREC-T.
            01 R2 TYPE SREC-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF R1 {1} R2
                    DISPLAY "X"
                END-IF.
                STOP RUN.
            """;
        foreach (var (id, op) in new[] { ("TR12A", "<"), ("TR12B", "="), ("TR12C", ">=") })
        {
            var (ok, diag) = EditionHarness.Compile(decl.Replace("{0}", id).Replace("{1}", op), 2002);
            Assert.True(ok, $"'R1 {op} R2' over signed-leaf strong groups must compile clean (ISO §8.8.4.2.12): "
                + string.Join("; ", diag));
        }
    }

    /// <summary>§8.8.4.2.3 SR4 names the CLASS pointer, which §8.5.2.1 Table 2 makes of THREE categories —
    /// data-pointer, program-pointer and function-pointer. A PROGRAM-POINTER leaf makes a strong group
    /// equality-only exactly as a data-pointer leaf does (kb/Work PB1469: the screen listed categories and let this
    /// ordering compile, and the run unit aborted on it); the EQUALITY compare of the same groups stays legal.</summary>
    [Fact]
    public void StrongProgramPointerGroupOrderingCompare_Rejected1535()
    {
        const string decl = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. {0}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 PREC-T TYPEDEF STRONG.
               05 PP USAGE PROGRAM-POINTER.
            01 R1 TYPE PREC-T.
            01 R2 TYPE PREC-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF R1 {1} R2
                    DISPLAY "X"
                END-IF.
                STOP RUN.
            """;
        var (okGt, diagGt) = EditionHarness.Compile(decl.Replace("{0}", "TR35P").Replace("{1}", ">"), 2002);
        Assert.False(okGt, "an ordering compare of a program-pointer strong group must be rejected (ISO §8.8.4.2.3 SR4)");
        EditionHarness.AssertHasDiagnostic(diagGt, "COBOLNET1535");

        var (okEq, diagEq) = EditionHarness.Compile(decl.Replace("{0}", "TR35Q").Replace("{1}", "="), 2002);
        Assert.True(okEq, "an EQUALITY compare of program-pointer strong groups is legal: " + string.Join("; ", diagEq));
    }

    /// <summary>kb/Work PB919 — a type whose OCCURS carries an INDEXED BY phrase, referenced twice, yields two tables
    /// whose indexes are both named IX (§13.18.58.4 GR1 — the subordinate entries are part of the type); each owns
    /// its cell and is referenced through its table (§8.4.2.2.3 SR6), while a bare IX is §8.4.2.2.3 SR1's
    /// ambiguity. This was the COBOLNET1531 stage while the two shared one spelling-keyed cell.</summary>
    [Fact]
    public void IndexedTypeReferencedTwice_EachCloneOwnsItsIndex()
    {
        var (okRun, stdout, detail) = EditionHarness.CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR31R.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL-T TYPEDEF.
               05 ROW OCCURS 3 INDEXED BY IX PIC X.
            01 A TYPE TBL-T.
            01 B TYPE TBL-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE ALL "." TO A B.
                SET IX OF A TO 1.
                SET IX OF B TO 3.
                MOVE "P" TO ROW OF A (IX OF A).
                MOVE "Q" TO ROW OF B (IX OF B).
                DISPLAY A "|" B.
                STOP RUN.
            """, 2002);
        Assert.True(okRun, detail);
        Assert.Equal("P..|..Q", stdout.Trim());

        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR31.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL-T TYPEDEF.
               05 ROW OCCURS 3 INDEXED BY IX PIC X.
            01 A TYPE TBL-T.
            01 B TYPE TBL-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET IX TO 1.
                STOP RUN.
            """, 2002);
        Assert.False(ok, "a bare IX over two TYPE clones' indexes is §8.4.2.2.3 SR1's ambiguity");
        EditionHarness.AssertHasDiagnostic(diag, "does not uniquely identify an index-name");
        EditionHarness.AssertNoDiagnostic(diag, "COBOLNET1531");
    }

    /// <summary>The positive companions must NOT trip a guard: a SINGLE INDEXED-type reference (also the
    /// <c>typedef_indexed</c> golden) and a strong boolean-group EQUALITY compare (SR4 bans only the
    /// ordering relation).</summary>
    [Fact]
    public void SingleIndexedTypeAndBooleanEquality_CompileClean()
    {
        var (ok1, diag1) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR1IX.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TBL-T TYPEDEF.
               05 ROW OCCURS 3 INDEXED BY IX PIC X.
            01 A TYPE TBL-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET IX TO 1.
                MOVE "Z" TO ROW OF A (IX).
                STOP RUN.
            """, 2002);
        Assert.True(ok1, $"a single INDEXED-type reference must compile clean: {string.Join("; ", diag1)}");

        var (ok2, diag2) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TR1EQ.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 BREC-T TYPEDEF STRONG.
               05 FLAG PIC 1.
            01 R1 TYPE BREC-T.
            01 R2 TYPE BREC-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF R1 = R2
                    DISPLAY "EQ"
                END-IF.
                STOP RUN.
            """, 2002);
        Assert.True(ok2, $"an EQUALITY compare of a boolean strong group must compile clean: {string.Join("; ", diag2)}");
    }
}
