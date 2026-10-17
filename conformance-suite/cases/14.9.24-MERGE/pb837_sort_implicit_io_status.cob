*>--- conformance-case
*> id: pb837_sort_implicit_io_status
*> rules: [GR-14.9.24.4-7, GR-14.9.40.4-9, GR-14.9.40.4-12,
*>    GR-14.9.40.4-15]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB837SRTIO.
      *> kb/Work PB837 - EACH implicit input-output operation of a
      *> SORT/MERGE transfer offers ITS OWN I-O status to the USE procedure.
      *>
      *> ISO 14.9.40.4 GR12 (SORT USING): a) "as if an OPEN statement",
      *> b) "Each record is obtained as if a READ statement with the NEXT
      *> phrase, the IGNORING LOCK phrase, and the AT END phrase had been
      *> executed", c) "as if a CLOSE statement without optional phrases",
      *> and the closing paragraph: "These implicit functions are performed
      *> such that any applicable USE procedures are executed".  14.9.24.4
      *> GR7 is the same rule for MERGE; 14.9.40.4 GR15 (GIVING) performs
      *> each record "as if a WRITE statement without any optional phrases".
      *> 14.9.49.4 GR6 runs the USE procedure "upon the unsuccessful
      *> execution of an input-output operation unless an AT END or INVALID
      *> KEY phrase takes precedence" - so three as-if statements that fail
      *> are three declarative invocations, each seeing its own status
      *> (9.1.13.1: the status is set "prior to the execution of ... any
      *> applicable exception processing statements").
      *>
      *> LEGS 1 AND 2 (USING, SORT then MERGE).  The input connector's
      *> file-control entry has SHARING WITH ALL OTHER, so the implicit
      *> OPEN carries SHARING WITH READ ONLY (GR12 a / GR7 a), and another
      *> connector holds the same physical file open EXTEND: Table 19 row
      *> "SHARING WITH READ ONLY / INPUT" x "sharing with all other /
      *> extend" is an unsuccessful open, 9.1.13.9 item 1 => '61'.  '61' is
      *> NONFATAL, so "the SORT statement continues as if the exception
      *> condition did not exist" (GR12 a; MERGE GR7 a continues after a
      *> USE procedure that completes normally).  The as-if READ then meets
      *> a connector that is not open: 14.9.30.4 GR2 - "the execution of
      *> the READ statement is unsuccessful and the I-O status value for
      *> file-name-1 is set to '47'" - which is not the at end condition,
      *> so the AT END phrase does not take precedence and the USE
      *> procedure runs.  '47' is FATAL (9.1.13.1), so the statement is
      *> terminated after that invocation (kb/Work PB993): SORT by GR12 b)
      *> "If a fatal exception condition exists ... the SORT is
      *> terminated", MERGE by 9.1.13.1's transfer "to the end of the
      *> statement that produced the fatal exception condition".  No as-if
      *> CLOSE, no OUTPUT PROCEDURE, and the MERGE never reads M-OK.
      *> EXPECTED per leg: 61, 47, then the status after is '47'.
      *>
      *> LEG 3 (GIVING).  The GIVING file is LINE SEQUENTIAL and the middle
      *> sorted record holds X"01", outside the line sequential character
      *> set (Annex A.1 item 115, docs/CONFORMANCE.md DOC-A.1-115: U+0020
      *> and above).  14.9.51.4 GR23: "the execution of the WRITE statement
      *> is unsuccessful and the I-O status ... is set to '71'" - one USE
      *> invocation with '71'.  '71' is not an attempt to write outside the
      *> file's boundaries (GR15's boundary paragraph closes the file), but
      *> it is FATAL (9.1.13.1: "any that begin with the digit 3, 4, or 7"),
      *> and SORT - unlike MERGE GR12 b) - has no rule that continues past
      *> a failed WRITE, so 9.1.13.1 transfers control to the end of the
      *> SORT (kb/Work PB993).  The as-if CLOSE is never executed, so the
      *> GIVING file is still open when the SORT ends (docs/CONFORMANCE.md,
      *> the PB993 determination): the program's own CLOSE succeeds.
      *> EXPECTED: one 71, the CLOSE '00', then the file reads back A only.
      *>
      *> Every invocation displays a running sequence number, so the ORDER
      *> is part of the expected output.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S-IN ASSIGN TO "pb837s1.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               FILE STATUS IS S-ST.
           SELECT S-OTH ASSIGN TO "pb837s1.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS SO-ST.
           SELECT M-IN ASSIGN TO "pb837m1.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               FILE STATUS IS M-ST.
           SELECT M-OTH ASSIGN TO "pb837m1.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS MO-ST.
           SELECT M-OK ASSIGN TO "pb837m2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS MK-ST.
           SELECT G-OUT ASSIGN TO "pb837g.dat"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS G-ST.
           SELECT SRT-FILE ASSIGN TO "pb837s.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD S-IN.
       01 S-REC    PIC X(3).
       FD S-OTH.
       01 SO-REC   PIC X(3).
       FD M-IN.
       01 M-REC    PIC X(3).
       FD M-OTH.
       01 MO-REC   PIC X(3).
       FD M-OK.
       01 MK-REC   PIC X(3).
       FD G-OUT.
       01 G-REC    PIC X(3).
       SD SRT-FILE.
       01 SRT-REC.
          05 SRT-KEY  PIC X.
          05 SRT-REST PIC X(2).
       WORKING-STORAGE SECTION.
       01 S-ST     PIC XX.
       01 SO-ST    PIC XX.
       01 M-ST     PIC XX.
       01 MO-ST    PIC XX.
       01 MK-ST    PIC XX.
       01 G-ST     PIC XX.
       01 SEQ      PIC 9 VALUE 0.
       01 EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DS-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON S-IN.
       DS-PARA.
           ADD 1 TO SEQ.
           DISPLAY "S-USE " SEQ "=" S-ST.
       DM-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON M-IN.
       DM-PARA.
           ADD 1 TO SEQ.
           DISPLAY "M-USE " SEQ "=" M-ST.
       DG-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON G-OUT.
       DG-PARA.
           ADD 1 TO SEQ.
           DISPLAY "G-USE " SEQ "=" G-ST.
       END DECLARATIVES.
       MAIN SECTION.
       SORT-LEG.
           OPEN OUTPUT S-OTH.
           MOVE "AAA" TO SO-REC.
           WRITE SO-REC.
           CLOSE S-OTH.
           OPEN EXTEND S-OTH.
           MOVE 0 TO SEQ.
           SORT SRT-FILE ON ASCENDING KEY SRT-KEY
               USING S-IN
               OUTPUT PROCEDURE IS DRAIN.
           CLOSE S-OTH.
           DISPLAY "S-AFTER=" S-ST.
       MERGE-LEG.
           OPEN OUTPUT M-OTH.
           MOVE "AAA" TO MO-REC.
           WRITE MO-REC.
           CLOSE M-OTH.
           OPEN OUTPUT M-OK.
           MOVE "KKK" TO MK-REC.
           WRITE MK-REC.
           CLOSE M-OK.
           OPEN EXTEND M-OTH.
           MOVE 0 TO SEQ.
           MERGE SRT-FILE ON ASCENDING KEY SRT-KEY
               USING M-IN M-OK
               OUTPUT PROCEDURE IS DRAIN.
           CLOSE M-OTH.
           DISPLAY "M-AFTER=" M-ST.
       GIVING-LEG.
           MOVE 0 TO SEQ.
           SORT SRT-FILE ON ASCENDING KEY SRT-KEY
               INPUT PROCEDURE IS FEED
               GIVING G-OUT.
           DISPLAY "G-AFTER=" G-ST.
           CLOSE G-OUT.
           DISPLAY "G-CLOSE=" G-ST.
           MOVE "N" TO EOF-FLAG.
           OPEN INPUT G-OUT.
           PERFORM UNTIL EOF-FLAG = "Y"
               READ G-OUT
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY "G-REC=" G-REC
               END-READ
           END-PERFORM.
           CLOSE G-OUT.
           STOP RUN.
       FEED-SECT SECTION.
       FEED.
           MOVE "CCC" TO SRT-REC.
           RELEASE SRT-REC.
           MOVE "B" TO SRT-KEY.
           MOVE X"0101" TO SRT-REST.
           RELEASE SRT-REC.
           MOVE "AAA" TO SRT-REC.
           RELEASE SRT-REC.
       DRAIN-SECT SECTION.
       DRAIN.
           MOVE "N" TO EOF-FLAG.
           PERFORM UNTIL EOF-FLAG = "Y"
               RETURN SRT-FILE
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END DISPLAY "RET=" SRT-REC
               END-RETURN
           END-PERFORM.
