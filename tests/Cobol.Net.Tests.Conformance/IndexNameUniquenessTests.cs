// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>kb/Work PB919 — ISO §8.4.2.2.3 SR1 ("For each non unique user-defined name that is explicitly referenced,
/// uniqueness shall be established through a sequence of qualifiers that precludes any ambiguity of reference") and
/// SR6 ("The qualification of an index-name may include the name of the table with which the index-name is
/// associated, as well as any name by which that table may be qualified") for INDEX-NAMES. Before: two tables'
/// <c>INDEXED BY IX</c> shared one cell, a bare <c>IX</c> compiled clean, and <c>IX OF EA</c> was "not defined".
/// The run-verified positive is the <c>pb919_index_name_qualified</c> golden; the ambiguous negative is
/// <c>negative/pb919-index-name-ambiguous</c>.</summary>
public sealed class IndexNameUniquenessTests
{
    private const string Head = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. W57IX{0}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 TA.
           05 EA PIC 9(2) OCCURS 3 INDEXED BY IX.
        01 TB.
           05 EB PIC 9(2) OCCURS 4 INDEXED BY IX.
        01 W PIC 9.
        PROCEDURE DIVISION.

        """;

    private static (bool Ok, IReadOnlyList<string> Diags) Compile(string id, string body, int edition = 85) =>
        EditionHarness.Compile(string.Format(Head, id) + body + "\n    STOP RUN.\n", edition);

    [Theory]
    [InlineData("A", "    SET IX TO 1.")]                           // SET receiving operand
    [InlineData("B", "    MOVE EA(IX) TO W.")]                      // a subscript
    [InlineData("C", "    IF IX > 1 DISPLAY \"X\" END-IF.")]         // a relation condition
    [InlineData("D", "    PERFORM VARYING IX FROM 1 BY 1 UNTIL IX > 3 CONTINUE END-PERFORM.")]
    public void DuplicatedIndexName_Unqualified_IsSR1(string id, string body)
    {
        var (ok, diags) = Compile(id, body);
        Assert.False(ok, "a bare IX declared by two tables must not compile (§8.4.2.2.3 SR1)");
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET1639");
        EditionHarness.AssertHasDiagnostic(diags, "does not uniquely identify an index-name");
    }

    [Theory]
    [InlineData("E", "    SET IX OF EA TO 2. SET IX IN TB TO 3. MOVE EA(IX OF EA) TO W. MOVE EB(IX OF EB) TO W.")]
    [InlineData("F", "    PERFORM VARYING IX OF TA FROM 1 BY 1 UNTIL IX OF TA > 3 MOVE 1 TO EA(IX OF TA) END-PERFORM.")]
    [InlineData("G", "    SET IX OF EA TO 1. SEARCH EA VARYING IX OF EA WHEN EA(IX OF EA) = 1 CONTINUE END-SEARCH.")]
    public void DuplicatedIndexName_QualifiedThroughItsTable_IsSR6(string id, string body)
    {
        var (ok, diags) = Compile(id, body);
        Assert.True(ok, "IX OF <table> / IX OF <record> is §8.4.2.2.3 SR6's form: " + string.Join("; ", diags));
    }

    [Fact]
    public void QualifierNamingNoDeclaringTable_IsNotDefined()
    {
        var (ok, diags) = Compile("H", "    SET IX OF W TO 1.");
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET1639");
        EditionHarness.AssertHasDiagnostic(diags, "SR6");
    }

    /// <summary>§8.4.2.3.3 SR4 is judged against the RESOLVED declaration, not the spelling: EB's hierarchy also
    /// declares an IX, but <c>IX OF EA</c> is the other table's.</summary>
    [Fact]
    public void OtherTablesQualifiedIndex_AsSubscript_IsSR4()
    {
        var (ok, diags) = Compile("I", "    MOVE EB(IX OF EA) TO W.");
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diags, "COBOLNET1961");
    }

    /// <summary>§14.9.37.3 SR8 ("shall be subscripted by the first index-name associated with identifier-1") is met
    /// by the QUALIFIED first index-name — which SR1 makes mandatory when another table shares the spelling — and
    /// judged by the declaration it resolves to: the OTHER table's IX is still refused.</summary>
    [Fact]
    public void SearchAll_KeySubscriptedByTheQualifiedFirstIndex_IsSR8()
    {
        const string src = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. W57IXS{0}.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TA.
               05 EA PIC 9(2) OCCURS 3 ASCENDING KEY EA INDEXED BY IX.
            01 TB.
               05 EB PIC 9(2) OCCURS 4 INDEXED BY IX.
            PROCEDURE DIVISION.
                SEARCH ALL EA AT END DISPLAY "NONE"
                    WHEN EA ({1}) = 2 DISPLAY "HIT"
                END-SEARCH
                STOP RUN.
            """;
        var (ok, diags) = EditionHarness.Compile(string.Format(src, "A", "IX OF EA"), 85);
        Assert.True(ok, "the qualified first index-name satisfies SR8: " + string.Join("; ", diags));
        var (ok2, diags2) = EditionHarness.Compile(string.Format(src, "B", "IX OF EB"), 85);
        Assert.False(ok2, "the other table's IX is not identifier-1's first index-name");
        EditionHarness.AssertHasDiagnostic(diags2, "first index-name");
    }

    /// <summary>The two cells are distinct storage: setting one index leaves the other at its initial 1.</summary>
    [Fact]
    public void TwoDeclarations_OwnTwoCells()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(string.Format(Head, "J") + """
                MOVE 11 TO EA(1) MOVE 12 TO EA(2) MOVE 21 TO EB(1) MOVE 22 TO EB(2)
                SET IX OF EA TO 2
                DISPLAY EA(IX OF EA) " " EB(IX OF EB).
                STOP RUN.
            """, 85);
        Assert.True(ok, detail);
        Assert.Equal("12 21", stdout.Trim());
    }

    /// <summary>§8.4.6.2.3 + §8.4.6.2.1 3) a): a contained program's own IX wins over its container's GLOBAL one,
    /// and the global one stays reachable qualified through its table.</summary>
    [Fact]
    public void ContainedProgram_LocalIndexWins_GlobalReachableQualified()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. W57IXK.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TG GLOBAL.
               05 EG PIC 9(2) OCCURS 3 INDEXED BY IX.
            PROCEDURE DIVISION.
                MOVE 11 TO EG(1) MOVE 22 TO EG(2) MOVE 33 TO EG(3)
                SET IX TO 3
                CALL "W57IXKIN"
                DISPLAY "OUTER=" EG(IX)
                STOP RUN.
            IDENTIFICATION DIVISION.
            PROGRAM-ID. W57IXKIN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 TL.
               05 EL PIC 9(2) OCCURS 3 INDEXED BY IX.
            PROCEDURE DIVISION.
                MOVE 44 TO EL(1)
                SET IX TO 1
                DISPLAY "LOCAL=" EL(IX) " GLOBAL=" EG(IX OF EG)
                SET IX OF TG TO 2
                EXIT PROGRAM.
            END PROGRAM W57IXKIN.
            END PROGRAM W57IXK.
            """, 85);
        Assert.True(ok, detail);
        Assert.Equal("LOCAL=44 GLOBAL=33\nOUTER=22", stdout.Replace("\r\n", "\n").Trim());
    }

    /// <summary>The <c>--permissive</c> disposition of an ambiguous index-name (§8.4.2.2.3 SR1, warned, resolved to
    /// the first declaration) still screens the table it resolves to: here that table lies in a LINKAGE record that
    /// is no operand of the USING phrase, so §13.7.3 SR4 e) ("It is a condition-name or index-name associated with a
    /// data item that satisfies one of the above conditions") refuses the reference, COBOLNET2746, exactly as it
    /// refuses the qualified <c>IX OF T1</c>.</summary>
    [Fact]
    public void Permissive_AmbiguousIndexName_ScreensTheLinkageRecordItResolvesTo()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. W57IXL.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L1.
               05 T1 PIC X OCCURS 3 INDEXED BY IX.
            01 L2.
               05 T2 PIC X OCCURS 3 INDEXED BY IX.
            PROCEDURE DIVISION USING L2.
                SET IX TO 2
                GOBACK.
            """, 2023, permissive: true);
        Assert.False(ok, "the first declaration's table is in an unreferenceable LINKAGE record (ISO §13.7.3 SR4)");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1639");
        EditionHarness.AssertHasDiagnostic(errors, "COBOLNET2746");
        EditionHarness.AssertNoDiagnostic(errors, "CS0131");
    }

    /// <summary>Every ask of one written ambiguous index-name reference gets the same answer under
    /// <c>--permissive</c>: the first declaration (§8.4.2.2.3 SR1's disposition). SET classifies its operand and then
    /// binds it, and the resolver reports a written reference once; before the wave 1074 fix the SECOND ask answered
    /// "failed", so the SET receiver became the inert occurrence number <c>1</c> and the generated C# failed Roslyn
    /// (CS0131, wave 1073 review R1-1). Here <c>IX</c> resolves to <c>L1</c>'s index: SET IX TO 2 and UP BY 1 make it
    /// 3, and <c>T1(IX OF T1)</c> reads <c>L1</c>'s third element.</summary>
    [Fact]
    public void Permissive_AmbiguousIndexName_SetResolvesToTheFirstDeclarationAtEveryAsk()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. W74IXS.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 L1.
               05 T1 PIC X OCCURS 3 INDEXED BY IX VALUE "A".
            01 L2.
               05 T2 PIC X OCCURS 3 INDEXED BY IX VALUE "B".
            PROCEDURE DIVISION.
                SET IX TO 2
                SET IX UP BY 1
                IF IX = 3 DISPLAY "THREE" END-IF
                DISPLAY T1(IX OF T1)
                STOP RUN.
            """, 2023, permissive: true);
        Assert.True(ok, detail);
        Assert.Equal(["THREE", "A"], stdout.ReplaceLineEndings().Trim().Split(Environment.NewLine));
    }

    /// <summary>The legal LINKAGE half of <see cref="Permissive_AmbiguousIndexName_ScreensTheLinkageRecordItResolvesTo"/>:
    /// the first declaration's record IS the USING operand (§13.7.3 SR4 a) and e)), so the warned SET compiles.</summary>
    [Fact]
    public void Permissive_AmbiguousIndexName_InAReferenceableLinkageRecord_Compiles()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. W74IXU.
            DATA DIVISION.
            LINKAGE SECTION.
            01 L1.
               05 T1 PIC X OCCURS 3 INDEXED BY IX.
            01 L2.
               05 T2 PIC X OCCURS 3 INDEXED BY IX.
            PROCEDURE DIVISION USING L1.
                SET IX TO 2
                GOBACK.
            """, 2023, permissive: true);
        Assert.True(ok, string.Join(Environment.NewLine, errors));
        EditionHarness.AssertHasDiagnostic(warnings, "COBOLNET1639");
    }
}
