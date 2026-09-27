// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// Spec-pinned sequential file-I/O goldens (CONFORMANCE-FIX-QUEUE CA15/CA16). Each stages a filesystem precondition
/// (an over-length input line; an absent OPTIONAL file) in a temp run directory, compiles + runs the program there at
/// --std 2023, and asserts the spec-derived stdout — behaviours the self-contained write-then-read differential
/// goldens cannot express. Expected values are DERIVED from the spec, not the legacy oracle.
/// </summary>
public sealed class SequentialFileIoSpecTests
{
    private static string Run(string source, (string Name, string Content)? stage = null)
        => RunAt(source, 2023, permissive: false, stage).Stdout;

    /// <summary>The same compile-and-run in a staged temp directory, at a NAMED edition and on either severity
    /// axis, returning the warning channel too — the <c>--permissive</c> legs need both (a tolerated construct
    /// must WARN and then MEAN something, and a test that only checked stdout could not tell the warning apart
    /// from silence).</summary>
    private static (string Stdout, IReadOnlyList<string> Warnings) RunAt(
        string source, int edition, bool permissive, (string Name, string Content)? stage = null)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Seqio_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            if (stage is { } s) File.WriteAllText(Path.Combine(dir, s.Name), s.Content);
            string src = Path.Combine(dir, "prog.cob");
            src = CompiledProgramCache.StageSource(src, source);
            string dll = Path.Combine(dir, "prog.dll");
            var r = CompiledProgramCache.Compile(new CobolNet.CompilerDriver.Options(
                src, dll, DialectLevel: edition, Permissive: permissive));
            Assert.True(r.Success, $"must compile at --std {edition}"
                + (permissive ? " --permissive: " : " strict: ") + string.Join("\n", r.Errors));
            var (ran, stdout, detail) = CutRunner.Run(dll, dir);
            Assert.True(ran, "must run: " + detail);
            return (CutRunner.Normalize(stdout), r.Warnings);
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }

    /// <summary>CA15 — §14.9.30.4 GR15 + NOTE 3 / §9.1.13.2 item 5: an over-length LINE-SEQUENTIAL record is truncated
    /// on the right to the record width, the READ is SUCCESSFUL with I-O status '06', the file position indicator
    /// references the next unread character in the record, and the following READ continues the remainder ('00'). The
    /// buggy path reported '00' on the truncated read and silently discarded the tail (the next READ skipped to the
    /// following physical line).</summary>
    [Fact]
    public void LineSequential_OverLengthRecord_TruncatesWith06_AndContinuesRemainder()
    {
        const string prog = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LSLONG.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN "lslong.txt" ORGANIZATION LINE SEQUENTIAL FILE STATUS FS.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 REC PIC X(5).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            PROCEDURE DIVISION.
            MAIN.
                OPEN INPUT F.
                READ F.
                DISPLAY "1:" REC " FS=" FS.
                READ F.
                DISPLAY "2:" REC " FS=" FS.
                READ F AT END DISPLAY "E FS=" FS END-READ.
                CLOSE F.
                STOP RUN.
            """;
        // Input: one physical line 'ABCDEFGH' (8 chars) + newline. READ1 truncates 8->5 = 'ABCDE' with '06'; READ2
        // continues the remainder 'FGH' (3<=5, space-padded in the X(5) record) with '00'; READ3 hits EOF -> '10'.
        Assert.Equal("1:ABCDE FS=06\n2:FGH   FS=00\nE FS=10", Run(prog, ("lslong.txt", "ABCDEFGH\n")));
    }

    /// <summary>CA16 — §14.9.27 GR17 + §14.9.30.4 GR21 rule e / GR24: an ABSENT OPTIONAL file opened I-O is CREATED
    /// (as if OPEN OUTPUT + CLOSE; OPEN returns '05'), the FPI is 1 (GR14), and the first READ on the now-empty file
    /// finds no record → AtEnd '10'. The buggy path created only a writer, so the first READ returned '47'
    /// (not-open-for-input) — which flips the AT END control flow (status[0] != '1'), running NOT AT END instead.</summary>
    [Fact]
    public void OptionalSequential_OpenIoAbsent_Creates05_ThenFirstReadIsAtEnd10()
    {
        const string prog = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. OPTIO.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT OPTIONAL F ASSIGN "optabsent.dat" ORGANIZATION SEQUENTIAL FILE STATUS FS.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 REC PIC X(10).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            PROCEDURE DIVISION.
            MAIN.
                OPEN I-O F.
                DISPLAY "OPEN FS=" FS.
                READ F AT END DISPLAY "ATEND FS=" FS
                    NOT AT END DISPLAY "GOTREC FS=" FS END-READ.
                CLOSE F.
                STOP RUN.
            """;
        Assert.Equal("OPEN FS=05\nATEND FS=10", Run(prog));
    }

    /// <summary>CA18 — §14.9.35.4 GR17 (line-sequential REWRITE in place): (equal) a record of as many bytes as the
    /// line replaces it, '00'; (b) a record of MORE bytes than the line ⇒ '44', the line unchanged. The byte count
    /// compared is record-name-1's — five bytes for every REWRITE REC here, whatever its trailing spaces (kb/Work
    /// PB1168: GR17 has no trailing-space rule — the WRITE statement's is §14.9.51.4 GR21). The space-fill of
    /// §14.9.35.4 GR17 c), for a record of FEWER bytes, needs a variable-length record and is the golden
    /// 2023/pb1168_ls_rewrite_gr17.
    /// The buggy path returned '30' for every line-sequential REWRITE (the seekable in-place branch was guarded
    /// by !_lineSequential).</summary>
    [Fact]
    public void LineSequential_Rewrite_Gr17_Equal00_Longer44()
    {
        const string prog = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LSREW.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN "lsrew.txt" ORGANIZATION LINE SEQUENTIAL FILE STATUS FS.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 REC PIC X(5).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            PROCEDURE DIVISION.
            MAIN.
                OPEN I-O F.
                READ F. MOVE "WORLD" TO REC. REWRITE REC. DISPLAY "R1=" FS.
                READ F. MOVE "XYZ" TO REC. REWRITE REC. DISPLAY "R2=" FS.
                READ F. MOVE "HI" TO REC. REWRITE REC. DISPLAY "R3=" FS.
                CLOSE F.
                OPEN INPUT F.
                READ F. DISPLAY "L1=[" REC "]".
                READ F. DISPLAY "L2=[" REC "]".
                READ F. DISPLAY "L3=[" REC "]".
                CLOSE F.
                STOP RUN.
            """;
        // Input lines HELLO(5), AB(2), CDEFG(5); REC is 5 bytes. 5 == HELLO(5) ⇒ '00', line becomes WORLD;
        // 5 > AB(2) ⇒ '44' GR17 b), AB unchanged; 5 == CDEFG(5) ⇒ '00', line becomes "HI   ".
        Assert.Equal("R1=00\nR2=44\nR3=00\nL1=[WORLD]\nL2=[AB   ]\nL3=[HI   ]",
            Run(prog, ("lsrew.txt", "HELLO\nAB\nCDEFG\n")));
    }

    /// <summary>CA18 — §14.9.35.4 GR17a / §9.1.13.7 item 4d: a REWRITE of a record whose preceding READ transferred
    /// only PART of it (an over-length line-sequential read that returned '06') is '44'.</summary>
    [Fact]
    public void LineSequential_RewriteAfterOverLengthRead_IsPartial44()
    {
        const string prog = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. LSPART.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN "lspart.txt" ORGANIZATION LINE SEQUENTIAL FILE STATUS FS.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 REC PIC X(5).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            PROCEDURE DIVISION.
            MAIN.
                OPEN I-O F.
                READ F. DISPLAY "RD=" FS.
                MOVE "WORLD" TO REC. REWRITE REC. DISPLAY "RW=" FS.
                CLOSE F.
                STOP RUN.
            """;
        // 'ABCDEFGH' (8) into an X(5) record ⇒ READ truncates to 'ABCDE' with '06' (a PARTIAL transfer); a REWRITE
        // of that partially-read record is GR17a ⇒ '44'.
        Assert.Equal("RD=06\nRW=44", Run(prog, ("lspart.txt", "ABCDEFGH\n")));
    }

    // ── The INVALID KEY phrase on a SEQUENTIAL organization (kb/Work PB691) ──────────────────────────────────
    //
    // §14.9.51.3 SR2 — "If the organization of the write file is sequential, format 1 shall be specified" — and
    // Format 1 of §14.9.51.2 carries no INVALID KEY bracket, so the phrase is illegal on a sequential WRITE;
    // §14.9.35.3 SR2's first arm says the same for REWRITE. The strict compiler must therefore REJECT it, which
    // the two negative fixtures pin at all four editions. These tests pin the OTHER half, the half a rejection
    // test can never reach: under --permissive the compiler accepts the program, and having accepted it, it owes
    // the phrase the meaning §9.1.14 gives it. Both statements dropped the phrase entirely before — the WRITE
    // arm without even a diagnostic (PB691), the REWRITE arm with one but still no branch (PB144's residue).

    private const string SeqWriteInvalidKeyProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB691WIK.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN "pb691wik.dat" ORGANIZATION SEQUENTIAL FILE STATUS FS.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        01 REC PIC X(4).
        WORKING-STORAGE SECTION.
        01 FS PIC XX.
        PROCEDURE DIVISION.
        MAIN.
            OPEN OUTPUT F.
            MOVE "AAAA" TO REC.
            WRITE REC
                INVALID KEY DISPLAY "W-IK"
                NOT INVALID KEY DISPLAY "W-NIK"
            END-WRITE.
            DISPLAY "W=" FS.
            CLOSE F.
            STOP RUN.
        """;

    /// <summary>§9.1.14 final rule over a sequential WRITE that <c>--permissive</c> tolerated. The WRITE of a
    /// 4-character record to a just-opened OUTPUT file succeeds, so the I-O status is '00' (§9.1.13.2 item 1 —
    /// "the input-output statement is successfully executed and no further information is available"). §9.1.14's
    /// final rule then decides both branches: its opening sentence — "If the invalid key condition does not
    /// exist … the INVALID KEY phrase is ignored, if specified" — kills the INVALID arm (it can never fire on
    /// this organization at all: §9.1.13.5's four invalid-key statuses '21'–'24' each name a relative or indexed
    /// file), and item 2 — "If the I-O status indicates a successful completion, control is transferred to the
    /// end of the input-output statement or to the imperative-statement specified in the NOT INVALID KEY phrase
    /// if it is specified" — runs the NOT arm. Expected stdout is therefore exactly "W-NIK" then "W=00": the
    /// pre-fix compiler printed only "W=00", having dropped both imperatives on the floor.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Permissive_SequentialWriteInvalidKey_RunsTheNotArmOnSuccess_AndWarns(int edition)
    {
        var (stdout, warnings) = RunAt(SeqWriteInvalidKeyProgram, edition, permissive: true);
        Assert.Equal("W-NIK\nW=00", stdout);
        Assert.Contains(warnings, x => x.Contains("COBOLNET1720") && x.Contains("INVALID KEY")
            && x.Contains("WRITE") && x.Contains("14.9.51.3 SR2"));
    }

    /// <summary>The REWRITE twin, one method away in the same binder (§14.9.35.3 SR2 first arm). PB144 landed
    /// the COBOLNET1720 screen here but still dropped the phrase, reasoning that a sequential REWRITE has no
    /// '2x' condition to carry — true of the INVALID arm, false of the NOT INVALID arm, which §9.1.14's final
    /// rule item 2 runs on the SUCCESSFUL completion this REWRITE has ('00', §9.1.13.2 item 1). Expected stdout
    /// is "R-NIK" then "R=00"; the pre-fix compiler warned and then printed only "R=00".</summary>
    [Fact]
    public void Permissive_SequentialRewriteInvalidKey_RunsTheNotArmOnSuccess_AndWarns()
    {
        const string prog = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB691RWIK.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN "pb691rwik.dat" ORGANIZATION SEQUENTIAL FILE STATUS FS.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 REC PIC X(4).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            PROCEDURE DIVISION.
            MAIN.
                OPEN I-O F.
                READ F.
                MOVE "BBBB" TO REC.
                REWRITE REC
                    INVALID KEY DISPLAY "R-IK"
                    NOT INVALID KEY DISPLAY "R-NIK"
                END-REWRITE.
                DISPLAY "R=" FS.
                CLOSE F.
                STOP RUN.
            """;
        var (stdout, warnings) = RunAt(prog, 2023, permissive: true, ("pb691rwik.dat", "AAAA\n"));
        Assert.Equal("R-NIK\nR=00", stdout);
        Assert.Contains(warnings, x => x.Contains("COBOLNET1720") && x.Contains("REWRITE")
            && x.Contains("14.9.35.3 SR2"));
    }

    /// <summary>The NOT-alone spelling reaches the SAME screen and the SAME §9.1.14 item 2 transfer. The
    /// grammar's <c>writeInvalidKey</c> rule has two alternatives (INVALID first, or NOT INVALID first) and only
    /// the second travels through <c>PhraseBlocks.StartsWithNot</c>, so the alternative a real --permissive
    /// program is most likely to write is exactly the one an INVALID-first fixture leaves unmeasured.</summary>
    [Fact]
    public void Permissive_SequentialWriteNotInvalidKeyAlone_RunsOnSuccess_AndNamesThePhrase()
    {
        const string prog = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB691WNIK.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN "pb691wnik.dat" ORGANIZATION SEQUENTIAL FILE STATUS FS.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 REC PIC X(4).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            PROCEDURE DIVISION.
            MAIN.
                OPEN OUTPUT F.
                MOVE "AAAA" TO REC.
                WRITE REC
                    NOT INVALID KEY DISPLAY "W-NIK"
                END-WRITE.
                DISPLAY "W=" FS.
                CLOSE F.
                STOP RUN.
            """;
        var (stdout, warnings) = RunAt(prog, 2023, permissive: true);
        Assert.Equal("W-NIK\nW=00", stdout);
        Assert.Contains(warnings, x => x.Contains("COBOLNET1720") && x.Contains("NOT INVALID KEY"));
    }

    /// <summary>The same transfer with NO FILE STATUS clause on the file — the axis every other test here holds
    /// fixed. §9.1.14 speaks of "the I-O status of the file connector associated with the statement", not of the
    /// program's FILE STATUS item, and the two are different code paths: <c>EmitStoreFileStatus</c> returns
    /// immediately when there is no FILE STATUS clause, so a NOT-arm branch that read the user's status item
    /// would go dead here and nowhere else. The branch reads the CONNECTOR, so it still runs.</summary>
    [Fact]
    public void Permissive_SequentialWriteInvalidKey_NotArmRunsWithoutAFileStatusClause()
    {
        const string prog = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB691NOSTAT.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN "pb691ns.dat" ORGANIZATION SEQUENTIAL.
            DATA DIVISION.
            FILE SECTION.
            FD F.
            01 REC PIC X(4).
            PROCEDURE DIVISION.
            MAIN.
                OPEN OUTPUT F.
                MOVE "AAAA" TO REC.
                WRITE REC
                    INVALID KEY DISPLAY "W-IK"
                    NOT INVALID KEY DISPLAY "W-NIK"
                END-WRITE.
                DISPLAY "DONE".
                CLOSE F.
                STOP RUN.
            """;
        Assert.Equal("W-NIK\nDONE", RunAt(prog, 2023, permissive: true).Stdout);
    }

    /// <summary>The screen is a REJECTION at every edition on the strict axis — the fixtures
    /// <c>write-invalid-key-sequential-org</c> / <c>write-not-invalid-key-sequential-org</c> assert the code, and
    /// this asserts the edition SWEEP (a gate landed on one edition breaks or misses the other three —
    /// feedback_edition_gate_sweep). §14.9.51.3 SR2 is an all-editions rule: COBOL-85's WRITE likewise admitted
    /// the INVALID KEY phrase only in its random format, so no edition may accept this program strict.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void Strict_SequentialWriteInvalidKey_IsRejectedAtEveryEdition(int edition)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(SeqWriteInvalidKeyProgram, edition);
        Assert.False(ok, $"--std {edition} strict must REJECT the phrase (ISO §14.9.51.3 SR2)");
        Assert.Contains(errors, e => e.Contains("COBOLNET1720"));
    }

    /// <summary>kb/Work PB737 — §13.18.43.4 GR16's CLOSING SENTENCE, the third obligation of the rule whose two
    /// lettered arms `pb339_into_current_record` already pins: "If the number of bytes determined as above is
    /// zero, the record is a zero-length item." §13.18.43.3 SR7 makes integer-2 zero legal ("Integer-2 shall be
    /// greater than or equal to zero"), so a zero-length current record is a REACHABLE shape, not a corner.
    /// <para>Derivation, stated before the program was run. WRITE side: GR13 a) — "If data-name-1 is specified,
    /// by the content of the data item referenced by data-name-1" — writes 0 bytes then 4. READ side: GR15 puts
    /// the just-read length back in data-name-1 (0, then 4) and GR16 a) makes exactly that many bytes the sending
    /// operand. Record 1's sender is therefore ZERO bytes, and §14.6.8.5's alignment space-fills the whole X(5)
    /// receiver: the dots pre-loaded into it must be gone, and none of the record AREA's leftover "ZZZZZ" may
    /// appear. Record 2's four bytes into an X(5) JUSTIFIED receiver are §13.18.32.4 GR2's right alignment with
    /// one leading space fill, which also proves the zero-length read left the file position indicator on the
    /// next record rather than consuming or skipping one.</para>
    /// <para>Edition-invariant, and asserted so rather than assumed: no `docs/VERSION_CHANGE_REFERENCE.md` row
    /// touches §13.18.43, so the same bytes are required at all four editions.</para></summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void VaryingRecord_ZeroLengthCurrentRecord_SendsNoBytesAndSpaceFills(int edition)
    {
        const string prog = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB737Z.
            ENVIRONMENT DIVISION.
            INPUT-OUTPUT SECTION.
            FILE-CONTROL.
                SELECT F ASSIGN "pb737z.dat" ORGANIZATION SEQUENTIAL FILE STATUS FS.
            DATA DIVISION.
            FILE SECTION.
            FD F RECORD IS VARYING IN SIZE FROM 0 TO 20 DEPENDING ON WS-LEN.
            01 F-REC PIC X(20).
            WORKING-STORAGE SECTION.
            01 FS PIC XX.
            01 WS-LEN PIC 9(4) VALUE 0.
            01 WS-P5 PIC X(5).
            01 WS-J5 PIC X(5) JUSTIFIED RIGHT.
            PROCEDURE DIVISION.
            MAIN.
                OPEN OUTPUT F.
                MOVE 0 TO WS-LEN. MOVE "ZZZZZ" TO F-REC. WRITE F-REC.
                MOVE 4 TO WS-LEN. MOVE "WXYZ"  TO F-REC. WRITE F-REC.
                CLOSE F.
                MOVE 99 TO WS-LEN.
                MOVE ALL "." TO WS-P5. MOVE ALL "." TO WS-J5.
                OPEN INPUT F.
                READ F INTO WS-P5 AT END DISPLAY "ATEND1" END-READ.
                DISPLAY "1:" FS " LEN=" WS-LEN " [" WS-P5 "]".
                READ F INTO WS-J5 AT END DISPLAY "ATEND2" END-READ.
                DISPLAY "2:" FS " LEN=" WS-LEN " [" WS-J5 "]".
                CLOSE F.
                STOP RUN.
            """;
        Assert.Equal("1:00 LEN=0000 [     ]\n2:00 LEN=0004 [ WXYZ]",
            RunAt(prog, edition, permissive: false).Stdout);
    }
}
