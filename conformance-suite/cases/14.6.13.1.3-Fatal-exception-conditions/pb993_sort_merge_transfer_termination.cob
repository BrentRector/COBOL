*>--- conformance-case
*> id: pb993_sort_merge_transfer_termination
*> rules: [GR-14.6.13.1.3-2, GR-14.9.24.4-1, GR-14.9.24.4-7, GR-14.9.24.4-12, GR-14.9.40.4-12, GR-14.9.40.4-15]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB993STT.
      *> kb/Work PB993 - the TERMINATION rules of the SORT/MERGE implicit
      *> transfers: what an unsuccessful as-if OPEN / READ / WRITE does to
      *> the statement once its USE procedure (if any) has run.
      *>
      *> THE DEFAULT (every cell no specific rule names): 9.1.13.1 - a
      *> status beginning 3, 4 or 7 (or 9) is FATAL, and "If the
      *> implementor chooses to continue execution of the run unit, control
      *> is transferred to the end of the statement that produced the fatal
      *> exception condition unless the rules for that statement define
      *> other behavior".  The statement is the SORT/MERGE - the as-if
      *> statement is not one of the program's.  14.6.13.1.3 2) puts the
      *> SORT/MERGE rules ahead of the run-unit termination of 5)/7), even
      *> with checking enabled (leg 9).  A nonfatal status continues.
      *> THE SPECIFIC RULES exercised here:
      *>   14.9.40.4 GR15 - "If a fatal exception condition exists for
      *>     file-name-3 as a result of the implicit OPEN during file
      *>     initiation, the SORT is terminated."             (legs 1, 2)
      *>   14.9.40.4 GR12 b) - "If a fatal exception condition exists for
      *>     file-name-1, the SORT is terminated" (the USING file).  (leg 3)
      *>   14.9.24.4 GR7 a) - "If a nonfatal exception condition exists as a
      *>     result of the execution of the implicit OPEN statement, the
      *>     MERGE statement is terminated unless there is an applicable USE
      *>     procedure that completes normally".              (legs 4, 5)
      *>   14.9.24.4 GR12 a) - "If a fatal exception condition exists as a
      *>     result of this implicit OPEN statement and there is an
      *>     applicable USE procedure that completes normally, processing
      *>     for the file connector that caused the exception condition is
      *>     bypassed" - the OTHER GIVING file is still written. (legs 6, 7)
      *>   14.9.24.4 GR12 b) - "If an exception condition exists as a result
      *>     of this implicit WRITE statement and there is an applicable USE
      *>     procedure that completes normally, the MERGE continues
      *>     execution, otherwise the MERGE statement is terminated". (leg 8)
      *> A terminated statement performs none of its remaining implicit
      *> functions (no as-if CLOSE), so the status each leg displays after
      *> the statement is the one that terminated it.  OUT-G is seeded "OLD"
      *> before every leg that must leave it untouched.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRC-A ASSIGN TO "pb993a.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS SA-ST.
           SELECT SRC-B ASSIGN TO "pb993b.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS SB-ST.
           SELECT MISS-IN ASSIGN TO "pb993-never-written.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS MI-ST.
           SELECT OUT-G ASSIGN TO "pb993g.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS OG-ST.
      *> A GIVING file whose OPEN OUTPUT cannot create the physical file
      *> (its directory does not exist): '30', a permanent error.  BAD-U
      *> has a USE procedure, BAD-N has none.
           SELECT BAD-U ASSIGN TO "pb993-no-such-dir/u.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS BU-ST.
           SELECT BAD-N ASSIGN TO "pb993-no-such-dir/n.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS BN-ST.
      *> A USING file held open EXTEND by another connector: its implicit
      *> OPEN carries SHARING WITH READ ONLY (GR7 a), and Table 19 refuses
      *> it '61' - nonfatal.  SH-N has no USE procedure, SH-U has one.
           SELECT SH-N ASSIGN TO "pb993s.dat"
               ORGANIZATION IS SEQUENTIAL SHARING WITH ALL OTHER
               FILE STATUS IS SN-ST.
           SELECT SH-U ASSIGN TO "pb993s.dat"
               ORGANIZATION IS SEQUENTIAL SHARING WITH ALL OTHER
               FILE STATUS IS SU-ST.
           SELECT SH-OTH ASSIGN TO "pb993s.dat"
               ORGANIZATION IS SEQUENTIAL SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL FILE STATUS IS SO-ST.
      *> A GIVING file whose physical file another connector holds open
      *> INPUT: the implicit OPEN OUTPUT is refused '61' (9.1.13.9 1) e)).
           SELECT GV-OUT ASSIGN TO "pb993v.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS GV-ST.
           SELECT GV-OTH ASSIGN TO "pb993v.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS GO-ST.
           SELECT SRT-FILE ASSIGN TO "pb993w.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD SRC-A.
       01 SA-REC   PIC X(3).
       FD SRC-B.
       01 SB-REC   PIC X(3).
       FD MISS-IN.
       01 MI-REC   PIC X(3).
       FD OUT-G.
       01 OG-REC   PIC X(3).
       FD BAD-U.
       01 BU-REC   PIC X(3).
       FD BAD-N.
       01 BN-REC   PIC X(3).
       FD SH-N.
       01 SN-REC   PIC X(3).
       FD SH-U.
       01 SU-REC   PIC X(3).
       FD SH-OTH.
       01 SO-REC   PIC X(3).
       FD GV-OUT.
       01 GV-REC   PIC X(3).
       FD GV-OTH.
       01 GO-REC   PIC X(3).
       SD SRT-FILE.
       01 SRT-REC  PIC X(3).
       WORKING-STORAGE SECTION.
       01 SA-ST    PIC XX.
       01 SB-ST    PIC XX.
       01 MI-ST    PIC XX.
       01 OG-ST    PIC XX.
       01 BU-ST    PIC XX.
       01 BN-ST    PIC XX.
       01 SN-ST    PIC XX.
       01 SU-ST    PIC XX.
       01 SO-ST    PIC XX.
       01 GV-ST    PIC XX.
       01 GO-ST    PIC XX.
       01 LEG      PIC XX.
       01 EOF-FLAG PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-BADU SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON BAD-U.
       D-BADU-P.
           DISPLAY LEG "-USE-BADU=" BU-ST.
       D-MISS SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON MISS-IN.
       D-MISS-P.
           DISPLAY LEG "-USE-MISS=" MI-ST.
       D-SHU SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON SH-U.
       D-SHU-P.
           DISPLAY LEG "-USE-SHU=" SU-ST.
       END DECLARATIVES.
       MAIN SECTION.
       SEED.
           OPEN OUTPUT SRC-A.
           MOVE "A" TO SA-REC WRITE SA-REC.
           MOVE "C" TO SA-REC WRITE SA-REC.
           CLOSE SRC-A.
           OPEN OUTPUT SRC-B.
           MOVE "B" TO SB-REC WRITE SB-REC.
           MOVE "D" TO SB-REC WRITE SB-REC.
           CLOSE SRC-B.
           OPEN OUTPUT SH-OTH.
           MOVE "S" TO SO-REC WRITE SO-REC.
           CLOSE SH-OTH.
      *> LEG 1 - SORT GIVING, fatal implicit OPEN ('30'), USE procedure:
      *> the USE runs once and the SORT is terminated (GR15) - no WRITE,
      *> no CLOSE, so no '48' and no '42'.
       LEG-1.
           MOVE "L1" TO LEG.
           SORT SRT-FILE ON ASCENDING KEY SRT-REC
               INPUT PROCEDURE IS FEED
               GIVING BAD-U.
           DISPLAY "L1-AFTER=" BU-ST.
      *> LEG 2 - the same with no USE procedure: still terminated, and the
      *> run unit continues (checking is off).
       LEG-2.
           MOVE "L2" TO LEG.
           SORT SRT-FILE ON ASCENDING KEY SRT-REC
               INPUT PROCEDURE IS FEED
               GIVING BAD-N.
           DISPLAY "L2-AFTER=" BN-ST.
      *> LEG 3 - SORT USING a file that does not exist: its implicit OPEN
      *> is '35', FATAL, so the SORT is terminated (GR12 b) before any READ
      *> and before the GIVING phase - OUT-G still holds "OLD".
       LEG-3.
           MOVE "L3" TO LEG.
           PERFORM SEED-OUT.
           SORT SRT-FILE ON ASCENDING KEY SRT-REC
               USING MISS-IN GIVING OUT-G.
           DISPLAY "L3-AFTER=" MI-ST.
           PERFORM SHOW-OUT.
      *> LEG 4 - MERGE USING, nonfatal implicit OPEN ('61'), NO USE
      *> procedure: terminated at once (GR7 a) - SRC-B is never read and
      *> the GIVING file is never opened.
       LEG-4.
           MOVE "L4" TO LEG.
           PERFORM SEED-OUT.
           OPEN EXTEND SH-OTH.
           MERGE SRT-FILE ON ASCENDING KEY SRT-REC
               USING SH-N SRC-B GIVING OUT-G.
           CLOSE SH-OTH.
           DISPLAY "L4-AFTER=" SN-ST.
           PERFORM SHOW-OUT.
      *> LEG 5 - the same with a USE procedure that completes normally:
      *> the MERGE "continues processing as if the exception condition did
      *> not exist" (GR7 a) - and its as-if READ of the connector that did
      *> not open is '47' (14.9.30.4 GR2), fatal: terminated by the
      *> default, after the second USE invocation.
       LEG-5.
           MOVE "L5" TO LEG.
           PERFORM SEED-OUT.
           OPEN EXTEND SH-OTH.
           MERGE SRT-FILE ON ASCENDING KEY SRT-REC
               USING SH-U SRC-B GIVING OUT-G.
           CLOSE SH-OTH.
           DISPLAY "L5-AFTER=" SU-ST.
           PERFORM SHOW-OUT.
      *> LEG 6 - MERGE GIVING two files; the first one's implicit OPEN is
      *> fatal ('30') and its USE procedure completes normally, so that
      *> file is BYPASSED (GR12 a) and the second receives the whole merge.
       LEG-6.
           MOVE "L6" TO LEG.
           PERFORM SEED-OUT.
           MERGE SRT-FILE ON ASCENDING KEY SRT-REC
               USING SRC-A SRC-B GIVING BAD-U OUT-G.
           DISPLAY "L6-AFTER=" BU-ST.
           PERFORM SHOW-OUT.
      *> LEG 7 - the same with no USE procedure for the failed file: GR12
      *> a) bypasses only after a USE that completes normally, so the
      *> default applies - terminated; the second file is never written.
       LEG-7.
           MOVE "L7" TO LEG.
           PERFORM SEED-OUT.
           MERGE SRT-FILE ON ASCENDING KEY SRT-REC
               USING SRC-A SRC-B GIVING BAD-N OUT-G.
           DISPLAY "L7-AFTER=" BN-ST.
           PERFORM SHOW-OUT.
      *> LEG 8 - MERGE GIVING a file whose implicit OPEN is refused '61'
      *> (nonfatal: the file is processed as if the exception did not
      *> exist), so the first as-if WRITE meets a connector that is not
      *> open, '48'.  No USE procedure: "otherwise the MERGE statement is
      *> terminated" (GR12 b) - after ONE write, and with no as-if CLOSE.
       LEG-8.
           MOVE "L8" TO LEG.
           OPEN OUTPUT GV-OTH.
           MOVE "OLD" TO GO-REC WRITE GO-REC.
           CLOSE GV-OTH.
           OPEN INPUT GV-OTH.
           MERGE SRT-FILE ON ASCENDING KEY SRT-REC
               USING SRC-A SRC-B GIVING GV-OUT.
           CLOSE GV-OTH.
           DISPLAY "L8-AFTER=" GV-ST.
      *> LEG 9 - leg 2 with EC-I-O checking ENABLED.  An explicit I-O
      *> statement's fatal status with no declarative would now terminate
      *> the run unit (14.6.13.1.3 7)), but 2) comes first: "If the
      *> executed statement is a MERGE or SORT statement, then the rules
      *> for those statements apply" - the SORT is terminated and the run
      *> unit continues.
       >>TURN EC-I-O CHECKING ON
       LEG-9.
           MOVE "L9" TO LEG.
           SORT SRT-FILE ON ASCENDING KEY SRT-REC
               INPUT PROCEDURE IS FEED
               GIVING BAD-N.
       >>TURN EC-I-O CHECKING OFF
           DISPLAY "L9-AFTER=" BN-ST.
           STOP RUN.
       FEED-SECT SECTION.
       FEED.
           MOVE "Z" TO SRT-REC RELEASE SRT-REC.
           MOVE "Y" TO SRT-REC RELEASE SRT-REC.
       UTIL-SECT SECTION.
       SEED-OUT.
           OPEN OUTPUT OUT-G.
           MOVE "OLD" TO OG-REC WRITE OG-REC.
           CLOSE OUT-G.
       SHOW-OUT.
           MOVE "N" TO EOF-FLAG.
           OPEN INPUT OUT-G.
           PERFORM UNTIL EOF-FLAG = "Y" OR OG-ST NOT = "00"
               READ OUT-G
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY LEG "-OUT=" OG-REC
               END-READ
           END-PERFORM.
           CLOSE OUT-G.
