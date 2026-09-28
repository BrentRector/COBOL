// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using CobolNet.Frontend.Preprocessor;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// The P5 Step-8 keyed-offset BYTE-POSITION goldens. The step's mandated failing-first probe (a key after a
/// WIDER redefiner) turned out to be ILLEGAL COBOL — ISO §13.18.44.3 SR8: the redefining storage area shall not
/// be larger than the redefined unless the redefined item is level 1 (and SR3 bans level-1 REDEFINES in the FILE
/// SECTION entirely) — so on every LEGAL record the image and physical offset bases coincide and the
/// RecordLayout unification is a pure port. What the probe DID expose: the compiler silently ACCEPTED the
/// SR8-illegal shape and emitted an overlay with byte-position semantics no edition defines. These goldens pin
/// (a) the new SR8 rejection, (b) §12.4.5.12.4 GR4 — "the identical BYTE POSITIONS referenced ... in any one
/// record description entry are implicitly referenced as keys for all other record description entries" — via a
/// sibling-record START operand (§14.9.41.3 SR6b leftmost-position correspondence), and (c) key retrieval with a
/// LEGAL (narrower) redefinition ahead of the key.
/// </summary>
public sealed class KeyedOffsetSpecTests
{
    private static (bool Ok, string Stdout, string Detail) CompileAndRun(string source)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Koff_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            src = CompiledProgramCache.StageSource(src, source);
            string dll = Path.Combine(dir, "prog.dll");
            var r = CompiledProgramCache.Compile(new CobolNet.CompilerDriver.Options(src, dll, DialectLevel: 85, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, "must compile strict: " + string.Join("\n", r.Errors));
            return CutRunner.Run(dll, dir);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>§13.18.44.3 SR8: a non-level-1 redefinition whose subject is LARGER than the redefined item is
    /// rejected (COBOLNET1539) — previously a silent acceptance with undefined overlay semantics.</summary>
    [Fact]
    public void Redefines_WiderThanTarget_NonLevel01_Rejected()
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Koff_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "prog.cob");
            src = CompiledProgramCache.StageSource(src, """
                IDENTIFICATION DIVISION.
                PROGRAM-ID. KOFFSR8.
                DATA DIVISION.
                WORKING-STORAGE SECTION.
                01 WS-REC.
                   05 A-SMALL PIC X(3).
                   05 A-WIDE REDEFINES A-SMALL PIC X(5).
                PROCEDURE DIVISION.
                MAIN.
                    DISPLAY A-WIDE.
                    STOP RUN.
                """);
            var r = CompiledProgramCache.Compile(new CobolNet.CompilerDriver.Options(
                src, Path.Combine(dir, "prog.dll"), DialectLevel: 85, SourceFormat: InitialReferenceFormat.Auto));
            Assert.False(r.Success, "an SR8-illegal wider redefinition must be rejected");
            Assert.Contains(r.Errors, e => e.Contains("COBOLNET1539"));
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>A LEVEL-1 wider redefinition stays legal (the SR8 exception) — the sanity companion.</summary>
    [Fact]
    public void Redefines_WiderThanTarget_Level01_Accepted()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. KOFFL1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X(3) VALUE "ABC".
            01 WS-B REDEFINES WS-A PIC X(5).
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "B=[" WS-B "]".
                STOP RUN.
            """);
        Assert.True(ok, detail);
        Assert.Contains("B=[", stdout);
    }

    /// <summary>§12.4.5.12.4 GR4 + §14.9.41.3 SR6b: the START key of reference may be an item of a SIBLING record
    /// description occupying the same leftmost byte position as the declared alternate key — the retrieval must
    /// return the record whose alternate key matches.</summary>
    [Fact]
    public void Start_KeyOperandInSiblingRecord_MatchesByBytePosition()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. KOFF1.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT KFIL ASSIGN TO "KOFF1F"
                    ORGANIZATION IS INDEXED
                    ACCESS MODE IS DYNAMIC
                    RECORD KEY IS REC-ID
                    ALTERNATE RECORD KEY IS KEY-A
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD KFIL.
            01 REC-A.
               05 REC-ID PIC X(2).
               05 A-BODY PIC X(5).
               05 KEY-A PIC X(2).
            01 REC-B.
               05 B-PRE PIC X(7).
               05 KEY-B PIC X(2).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT KFIL.
                MOVE "01" TO REC-ID.
                MOVE "XXXXX" TO A-BODY.
                MOVE "BB" TO KEY-A.
                WRITE REC-A INVALID KEY DISPLAY "W1-BAD".
                MOVE "02" TO REC-ID.
                MOVE "AA" TO KEY-A.
                WRITE REC-A INVALID KEY DISPLAY "W2-BAD".
                MOVE "03" TO REC-ID.
                MOVE "CC" TO KEY-A.
                WRITE REC-A INVALID KEY DISPLAY "W3-BAD".
                CLOSE KFIL.
                OPEN INPUT KFIL.
                MOVE "BB" TO KEY-B.
                START KFIL KEY = KEY-B INVALID KEY DISPLAY "START-BAD FS=" WS-FS.
                READ KFIL NEXT AT END DISPLAY "EOF".
                DISPLAY "GOT=" REC-ID.
                CLOSE KFIL.
                STOP RUN.
            """);
        Assert.True(ok, detail);
        // The alternate-keyed retrieval by the SIBLING-record operand: the record whose KEY-A = "BB" is REC-ID 01.
        Assert.DoesNotContain("START-BAD", stdout);
        Assert.Contains("GOT=01", stdout);
    }

    /// <summary>Key retrieval with a LEGAL (narrower) redefinition ahead of the key: the key's byte position is
    /// unchanged by the overlay (§13.18.44 GR1 — the redefinition begins at the target's first position and, per
    /// SR8, cannot extend past it), and the declared alternate key retrieves correctly.</summary>
    [Fact]
    public void Start_DeclaredAlternateKey_AfterLegalRedefinition_Retrieves()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. KOFF2.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT KFIL ASSIGN TO "KOFF2F"
                    ORGANIZATION IS INDEXED
                    ACCESS MODE IS DYNAMIC
                    RECORD KEY IS REC-ID
                    ALTERNATE RECORD KEY IS KEY-A
                    FILE STATUS IS WS-FS.
            DATA DIVISION.
            FILE SECTION.
            FD KFIL.
            01 REC-A.
               05 REC-ID PIC X(2).
               05 A-BIG PIC X(5).
               05 A-PARTS REDEFINES A-BIG.
                  10 A-P1 PIC X(3).
                  10 A-P2 PIC X(2).
               05 KEY-A PIC X(2).
            WORKING-STORAGE SECTION.
            01 WS-FS PIC XX.
            PROCEDURE DIVISION.
            MAIN-PARA.
                OPEN OUTPUT KFIL.
                MOVE "01" TO REC-ID.
                MOVE "YYYYY" TO A-BIG.
                MOVE "QQ" TO KEY-A.
                WRITE REC-A INVALID KEY DISPLAY "W1-BAD".
                MOVE "02" TO REC-ID.
                MOVE "PP" TO KEY-A.
                WRITE REC-A INVALID KEY DISPLAY "W2-BAD".
                CLOSE KFIL.
                OPEN INPUT KFIL.
                MOVE "PP" TO KEY-A.
                START KFIL KEY = KEY-A INVALID KEY DISPLAY "START-BAD FS=" WS-FS.
                READ KFIL NEXT AT END DISPLAY "EOF".
                DISPLAY "GOT=" REC-ID.
                CLOSE KFIL.
                STOP RUN.
            """);
        Assert.True(ok, detail);
        Assert.DoesNotContain("START-BAD", stdout);
        Assert.Contains("GOT=02", stdout);
    }
}
