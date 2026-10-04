// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The OCCURS DYNAMIC staged-loud guards (Phase 6, data-model D9, increment 5): a dynamic-capacity table has
/// construction/placement rules whose violation must be a LOUD bind-time rejection, never a silent mis-compile
/// (COBOLNET_DESIGN §1.4). Each guard cites its ISO §13.18.38 / §13.18.44 / §13.18.63 rule. The positive facts
/// (a well-formed FROM/TO, and a VALUE on a GROUP dynamic table's subordinate = the element seed) must NOT trip a
/// guard — the run-success corpus (<c>dyn_*</c>) covers behavior; these assert the negative gating.
/// </summary>
public sealed class OccursDynamicGuardTests
{
    private static string Prog(string tableEntry) => """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DYNG.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        """ + "\n" + tableEntry + "\n" + """
        PROCEDURE DIVISION.
        MAIN-PARA.
            DISPLAY "X".
            STOP RUN.
        """;

    /// <summary>§13.18.38 SR28 — integer-5 (TO) shall be greater than integer-4 (FROM); <c>FROM 8 TO 3</c> is a
    /// declaration error, COBOLNET1522, not a table that opens above its own expected capacity.</summary>
    [Fact]
    public void FromGreaterThanTo_Rejected1522()
    {
        var (ok, diag) = EditionHarness.Compile(Prog(
            "01 WS-TABLE.\n   05 WS-E PIC 9(3) OCCURS DYNAMIC FROM 8 TO 3."), 2014);
        Assert.False(ok, "FROM 8 TO 3 must be rejected (ISO §13.18.38 SR28)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1522");
    }

    /// <summary>⛔ THE TWO SIDES OF A REDEFINES OVER/BY A DYNAMIC-CAPACITY TABLE ARE DIFFERENT RULES, and this
    /// test used to conflate them under one citation and one code (kb/Work PB177 arm C follow-up).
    /// <para>The OBJECT side is named by a SYNTAX RULE: §13.18.44.3 SR5 SENTENCE 1, "The data description entry
    /// for data-name-2 shall not contain an OCCURS clause" — and OCCURS DYNAMIC is Format 4 OF the OCCURS clause
    /// (§13.18.38), so the dynamic-capacity object is as squarely named as a fixed one. COBOLNET1701. (This
    /// method's old doc-comment cited "§13.18.44 SR5 — a dynamic-capacity table is out-of-line storage that can
    /// neither be the subject nor the object of a REDEFINES", which is the right clause NUMBER attached to a
    /// storage-model sentence the rule does not contain.)</para></summary>
    [Fact]
    public void RedefinesOverDynamicTable_ObjectSide_Rejected1701()
    {
        var (ok, diag) = EditionHarness.Compile(Prog(
            "01 WS-TABLE.\n   05 WS-E PIC 9(3) OCCURS DYNAMIC FROM 3.\n   05 WS-R REDEFINES WS-E PIC X(9)."), 2014);
        Assert.False(ok, "a REDEFINES whose data-name-2 carries an OCCURS clause must be rejected (ISO §13.18.44.3 SR5)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1701");
    }

    /// <summary>The SUBJECT side, and the one for which "no syntax rule literally names it" is TRUE: §13.18.38.3
    /// carries no REDEFINES restriction and §13.18.44.3 SR17 does not reach an ELEMENTARY dynamic table
    /// (§8.5.1.12.1 defines "variable-length group" over items SUBORDINATE to the group). What decides it is the
    /// storage model — §13.18.44.4 GR1's fixed association area against §8.5.1.9.1's capacities that "may vary
    /// during execution". COBOLNET1525, narrowed to exactly this side.</summary>
    [Fact]
    public void RedefinesByDynamicTable_SubjectSide_Rejected1525()
    {
        var (ok, diag) = EditionHarness.Compile(Prog(
            "01 WS-TABLE.\n   05 WS-E PIC X(9).\n   05 WS-R REDEFINES WS-E PIC 9(3) OCCURS DYNAMIC FROM 3."), 2014);
        Assert.False(ok, "a REDEFINES entry that IS a dynamic-capacity table must be rejected (§13.18.44.4 GR1 / §8.5.1.9.1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1525");
    }

    /// <summary>⛔ A **FORMAT 1** VALUE ON AN ELEMENTARY DYNAMIC-CAPACITY ENTRY IS CONFORMING SOURCE, and the
    /// §13.18.63.4 GR16 capacity derivation does not reach it (kb/Work PB500). GR16 is a FORMAT 2 general rule
    /// (band GR11–GR16) and §13.18.63.3 SR22 — the rule that keeps GR16b from having no operand — is a FORMAT 2
    /// syntax rule (band SR16–SR23); §13.18.63 imports rules across bands ONLY into the later band (GR11, GR17,
    /// GR21, GR24), never back into FORMAT 1. So the capacity is the ordinary §14.6.2.3.2 item-6 / §13.18.38.4
    /// GR16 minimum — here FROM 3 — and §13.18.63.4 GR9 gives every one of those occurrences the value.
    /// <para>This test replaced a GREEN assertion that the construct was REJECTED (COBOLNET1528, now retired): a
    /// passing test pinning a loud stage reads as a decision, and this one was pinning a refusal of legal COBOL
    /// derived from a rule in the wrong band.</para></summary>
    [Fact]
    public void ValueOnElementaryDynamicEntry_CapacityIsMinimum_EveryOccurrenceTakesTheValue()
    {
        var (ok, diag) = EditionHarness.Compile(Prog(
            "01 WS-TABLE.\n   05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 3 VALUE 7."), 2014);
        Assert.True(ok, $"a Format 1 VALUE on an elementary dynamic entry is conforming source "
            + $"(§13.18.38.3 forbids it nowhere; §13.18.63.4 GR16 is FORMAT 2): {string.Join("; ", diag)}");
    }

    /// <summary>The same rule one level down: a **FORMAT 1** VALUE SUBORDINATE to a GROUP dynamic-capacity table,
    /// with the OCCURS carrying a TO (expected) capacity. §13.18.63.4 GR9 covers "an entry that is subordinate to
    /// an OCCURS clause" explicitly, and §14.6.2.3.2 item 6 still opens the table at its MINIMUM capacity — the
    /// expected capacity is a ceiling for EC-BOUND-OVERFLOW (§8.5.1.9.6), never an initial size.
    /// <para>⛔ THE TWO-ARM WITNESS. The retired COBOLNET1528 refusal fired on this arm only when the OCCURS
    /// carried a TO, so `OCCURS DYNAMIC FROM 2.` compiled and seeded correctly while `OCCURS DYNAMIC FROM 2 TO 10.`
    /// — the same construct, one optional phrase apart — was refused. This test and
    /// <see cref="WellFormedAndGroupSubordinateValue_CompileClean"/> are the pair that keeps the two arms
    /// answering alike.</para></summary>
    [Fact]
    public void GroupSubordinateValueWithTo_CompilesAndOpensAtMinimumCapacity()
    {
        var (ok, diag) = EditionHarness.Compile(Prog("""
            01 WS-TABLE.
               05 ROW OCCURS DYNAMIC CAPACITY IN WS-CAP FROM 2 TO 10.
                  10 ELEM PIC 99 VALUE 07.
            """), 2014);
        Assert.True(ok, $"a Format 1 VALUE subordinate to a dynamic table is conforming source, with or without "
            + $"an OCCURS TO (§13.18.63.4 GR9; GR16 is FORMAT 2): {string.Join("; ", diag)}");
    }

    /// <summary>§8.5.1.9.1 item 3 (review #6) — a dynamic-capacity table "may be defined in any place, OTHER THAN the
    /// file section" (its out-of-line storage has no place in a record image). COBOLNET1526.</summary>
    [Fact]
    public void FileSectionDynamicTable_Rejected1526()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FDYN.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "fdyn.dat" ORGANIZATION IS SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD  F.
            01  REC.
                05  DT OCCURS DYNAMIC CAPACITY IN CP FROM 2 PIC 9(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2014);
        Assert.False(ok, "OCCURS DYNAMIC in the FILE SECTION must be rejected (ISO §8.5.1.9.1)");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1526");
    }

    /// <summary>§8.5.1.9.1 3) holds on EVERY route a description reaches a file record by, not only the one
    /// written in place (kb/Work PB1604): a dynamic-capacity table arriving through a TYPE clause (whole record or
    /// a subordinate group), through SAME AS, or written in a SORT-MERGE file description is refused with
    /// COBOLNET1526. This is what makes the record-sizing walks (<c>FileModel.MaxDynamicExtent</c> /
    /// <c>MinRecordSize</c>, <c>RecordLayout.KeyWindowOf</c>) correct in having no dynamic-capacity-table arm:
    /// a route this test does not refuse would let a record be sized without its table.</summary>
    [Theory]
    [InlineData("FD F.\n01 R TYPE TT.\nSD S.\n01 SR PIC X.")]
    [InlineData("FD F.\n01 R.\n   05 A PIC X(2).\n   05 G TYPE TT.\nSD S.\n01 SR PIC X.")]
    [InlineData("FD F.\n01 R SAME AS TT-W.\nSD S.\n01 SR PIC X.")]
    [InlineData("FD F.\n01 R PIC X.\nSD S.\n01 SR.\n   05 A PIC X(2).\n   05 T OCCURS DYNAMIC FROM 2 TO 5.\n      10 E PIC X(3).")]
    public void FileSectionDynamicTable_EveryRoute_Rejected1526(string fileSection)
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. FDYNR.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN TO "fdynr.dat" ORGANIZATION IS SEQUENTIAL.
                SELECT S ASSIGN TO "fdynr.srt".
            DATA DIVISION.
            FILE SECTION.
            """ + "\n" + fileSection + "\n" + """
            WORKING-STORAGE SECTION.
            01 TT TYPEDEF.
               05 T OCCURS DYNAMIC FROM 2 TO 5.
                  10 E PIC X(3).
            01 TT-W.
               05 T OCCURS DYNAMIC FROM 2 TO 5.
                  10 E PIC X(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY "X".
                STOP RUN.
            """, 2023);
        Assert.False(ok, "a dynamic-capacity table under a FILE SECTION record must be rejected (ISO §8.5.1.9.1 3))");
        EditionHarness.AssertHasDiagnostic(diag, "COBOLNET1526");
    }

    /// <summary>The positive companions: a well-formed FROM/TO, and a Format 1 VALUE on the SUBORDINATE of a GROUP
    /// dynamic table with NO OCCURS TO (the element's per-occurrence seed at capacity = FROM, §13.18.63.4 GR9 +
    /// §14.6.2.3.2 item 6 — never a VALUE-derived capacity, which is the FORMAT 2 GR16's job alone), both compile
    /// cleanly. Guards must not over-restrict the supported surface.
    /// <para>The no-TO half of the pair whose TO half is
    /// <see cref="GroupSubordinateValueWithTo_CompilesAndOpensAtMinimumCapacity"/>: for two years exactly one of
    /// these two compiled, which is how the retired COBOLNET1528 arm split a single construct in half.</para></summary>
    [Fact]
    public void WellFormedAndGroupSubordinateValue_CompileClean()
    {
        var (ok1, diag1) = EditionHarness.Compile(Prog(
            "01 WS-TABLE.\n   05 WS-E PIC 9(3) OCCURS DYNAMIC FROM 2 TO 5."), 2014);
        Assert.True(ok1, $"a well-formed FROM 2 TO 5 must compile: {string.Join("; ", diag1)}");

        var (ok2, diag2) = EditionHarness.Compile(Prog("""
            01 WS-TABLE.
               05 WS-ROW OCCURS DYNAMIC FROM 3 INITIALIZED.
                  10 WS-NAME PIC X(4) VALUE "----".
                  10 WS-QTY  PIC 9(2) VALUE 7.
            """), 2014);
        Assert.True(ok2, $"a VALUE on a GROUP dynamic table's subordinate is the element seed: {string.Join("; ", diag2)}");
    }
}
