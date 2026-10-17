*>--- conformance-case
*> id: pb714_sort_using_sharing_read_only
*> rules: [GR-14.9.40.4-12]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB714SRO.
      *> kb/Work PB714 — the SHARING phrase of SORT's implicit USING OPEN.
      *>
      *> ISO 14.9.40.4 GR12 a): "The processing of the file is initiated.
      *> If the file-control entry for the file has a SHARING clause with
      *> the ALL phrase, the initiation is performed as if an OPEN statement
      *> with the INPUT phrase and the SHARING WITH READ ONLY phrase had
      *> been executed; otherwise, the initiation is performed as if an OPEN
      *> statement with the INPUT phrase and without a SHARING phrase is
      *> executed.  The absence of the SHARING phrase means that the sharing
      *> mode is completely determined by the SHARING clause, if any, in the
      *> file control entry for the file connector referenced by file-name-2."
      *> 14.9.24.4 GR7 a) is the same rule for MERGE — see
      *> pb714_merge_using_sharing_read_only.
      *>
      *> The override is only observable against a connector already
      *> associated with the same physical file, so each leg holds a second
      *> SELECT open across the SORT.  9.1.13.1 makes the refusal readable:
      *> "The value of the I-O status is set during the execution of a
      *> CLOSE, DELETE, OPEN, READ, REWRITE, START, UNLOCK or WRITE
      *> statement and prior to the execution of ... any applicable
      *> exception processing statements", and 12.4.5.8.4 GR1 updates the
      *> FILE STATUS item with it — so the USE procedure GR12 a) invokes
      *> ("If a nonfatal exception condition exists as a result of the
      *> execution of the implicit OPEN statement ... the SORT statement
      *> continues as if the exception condition did not exist") sees the
      *> status THIS open produced.  Each leg's I-ST is seeded "ZZ" first,
      *> so a status that was never stored would be visible as such.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
      *> LEG 1 — the entry HAS the ALL phrase, so the implicit OPEN carries
      *> SHARING WITH READ ONLY.  9.1.15 2): "The sharing with read only
      *> mode restricts concurrent access to a physical file through file
      *> connectors other than this one, to input mode."  The other
      *> connector is open EXTEND, so Table 19 row "SHARING WITH READ ONLY /
      *> INPUT" x column "sharing with all other / extend I-O output" is
      *> Unsuccessful open => 9.1.13.9 item 1 => '61'.  '61' is nonfatal, so
      *> the SORT continues (GR12 a) and the as-if READ of the connector that
      *> did not open is unsuccessful '47' (14.9.30.4 GR2) - FATAL, and GR12
      *> b)'s "If a fatal exception condition exists ... the SORT is
      *> terminated" ends the statement after that USE invocation (kb/Work
      *> PB993): no as-if CLOSE, and the GIVING phase never runs, so F1-OUT
      *> is never created - its OPEN INPUT below answers '35' and the
      *> read-back loop, which stops on any unsuccessful status, counts 0.
           SELECT F1-IN ASSIGN TO "pb714sro1.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               FILE STATUS IS I1-ST.
           SELECT F1-OTH ASSIGN TO "pb714sro1.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS O1-ST.
           SELECT F1-OUT ASSIGN TO "pb714sro1o.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS G1-ST.
      *> LEG 2 — the SAME ALL phrase, but the other connector is open INPUT.
      *> Table 19 row "SHARING WITH READ ONLY / INPUT" x column "sharing
      *> with all other / input" is Normal open, so the sort runs.  This is
      *> what makes the override READ ONLY and not NO OTHER: 9.1.15 2)
      *> refuses only a connector "whose open mode is other than input".
           SELECT F2-IN ASSIGN TO "pb714sro2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               FILE STATUS IS I2-ST.
           SELECT F2-OTH ASSIGN TO "pb714sro2.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS O2-ST.
           SELECT F2-OUT ASSIGN TO "pb714sro2o.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS G2-ST.
      *> LEG 3 — the "otherwise" arm, and the leg that pins the override as
      *> CONDITIONAL.  It differs from leg 2 in ONE character of source: the
      *> SHARING clause is NO OTHER, not the ALL phrase, so GR12 a)'s
      *> condition is false and the implicit OPEN carries no SHARING phrase
      *> — "the sharing mode is completely determined by the SHARING clause
      *> ... in the file control entry".  9.1.15 1): "The sharing with no
      *> other mode specifies exclusive access to a physical file."  Against
      *> the SAME open-INPUT connector leg 2 tolerates, Table 19 row
      *> "SHARING WITH NO OTHER / EXTEND I-O INPUT OUTPUT" is Unsuccessful
      *> open in every column => '61'.  An implementation that applied READ
      *> ONLY unconditionally would answer '00' here.  From the '61' on, the
      *> leg runs exactly as leg 1: '47', the SORT terminated, no F3-OUT.
           SELECT F3-IN ASSIGN TO "pb714sro3.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH NO OTHER
               FILE STATUS IS I3-ST.
           SELECT F3-OTH ASSIGN TO "pb714sro3.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS O3-ST.
           SELECT F3-OUT ASSIGN TO "pb714sro3o.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS G3-ST.
           SELECT SRT-FILE ASSIGN TO "pb714sros.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD F1-IN.
       01 I1-REC   PIC X(5).
       FD F1-OTH.
       01 O1-REC   PIC X(5).
       FD F1-OUT.
       01 G1-REC   PIC X(5).
       FD F2-IN.
       01 I2-REC   PIC X(5).
       FD F2-OTH.
       01 O2-REC   PIC X(5).
       FD F2-OUT.
       01 G2-REC   PIC X(5).
       FD F3-IN.
       01 I3-REC   PIC X(5).
       FD F3-OTH.
       01 O3-REC   PIC X(5).
       FD F3-OUT.
       01 G3-REC   PIC X(5).
       SD SRT-FILE.
       01 SRT-REC  PIC X(5).
       WORKING-STORAGE SECTION.
       01 I1-ST    PIC XX.
       01 O1-ST    PIC XX.
       01 G1-ST    PIC XX.
       01 I2-ST    PIC XX.
       01 O2-ST    PIC XX.
       01 G2-ST    PIC XX.
       01 I3-ST    PIC XX.
       01 O3-ST    PIC XX.
       01 G3-ST    PIC XX.
       01 N        PIC 9 VALUE 0.
       01 EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1-IN.
       D1-PARA.
           DISPLAY "L1-USE=" I1-ST.
       D2-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F2-IN.
       D2-PARA.
           DISPLAY "L2-USE=" I2-ST.
       D3-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F3-IN.
       D3-PARA.
           DISPLAY "L3-USE=" I3-ST.
       END DECLARATIVES.
       MAIN SECTION.
       LEG-1.
           MOVE "ZZ" TO I1-ST.
           OPEN OUTPUT F1-OTH.
           MOVE "CCCCC" TO O1-REC.
           WRITE O1-REC.
           MOVE "AAAAA" TO O1-REC.
           WRITE O1-REC.
           MOVE "BBBBB" TO O1-REC.
           WRITE O1-REC.
           CLOSE F1-OTH.
           OPEN EXTEND F1-OTH.
           SORT SRT-FILE ON ASCENDING KEY SRT-REC
               USING F1-IN GIVING F1-OUT.
           CLOSE F1-OTH.
           MOVE 0 TO N.
           MOVE "N" TO EOF-FLAG.
           OPEN INPUT F1-OUT.
           DISPLAY "L1-OUT-OPEN=" G1-ST.
           PERFORM UNTIL EOF-FLAG = "Y" OR G1-ST NOT = "00"
               READ F1-OUT
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END ADD 1 TO N
               END-READ
           END-PERFORM.
           CLOSE F1-OUT.
           DISPLAY "L1-COUNT=" N " I1=" I1-ST.
       LEG-2.
           MOVE "ZZ" TO I2-ST.
           OPEN OUTPUT F2-OTH.
           MOVE "CCCCC" TO O2-REC.
           WRITE O2-REC.
           MOVE "AAAAA" TO O2-REC.
           WRITE O2-REC.
           MOVE "BBBBB" TO O2-REC.
           WRITE O2-REC.
           CLOSE F2-OTH.
           OPEN INPUT F2-OTH.
           SORT SRT-FILE ON ASCENDING KEY SRT-REC
               USING F2-IN GIVING F2-OUT.
           CLOSE F2-OTH.
           MOVE 0 TO N.
           MOVE "N" TO EOF-FLAG.
           OPEN INPUT F2-OUT.
           PERFORM UNTIL EOF-FLAG = "Y" OR G2-ST NOT = "00"
               READ F2-OUT
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END ADD 1 TO N
                       DISPLAY "L2-REC=" G2-REC
               END-READ
           END-PERFORM.
           CLOSE F2-OUT.
           DISPLAY "L2-COUNT=" N " I2=" I2-ST.
       LEG-3.
           MOVE "ZZ" TO I3-ST.
           OPEN OUTPUT F3-OTH.
           MOVE "CCCCC" TO O3-REC.
           WRITE O3-REC.
           MOVE "AAAAA" TO O3-REC.
           WRITE O3-REC.
           MOVE "BBBBB" TO O3-REC.
           WRITE O3-REC.
           CLOSE F3-OTH.
           OPEN INPUT F3-OTH.
           SORT SRT-FILE ON ASCENDING KEY SRT-REC
               USING F3-IN GIVING F3-OUT.
           CLOSE F3-OTH.
           MOVE 0 TO N.
           MOVE "N" TO EOF-FLAG.
           OPEN INPUT F3-OUT.
           DISPLAY "L3-OUT-OPEN=" G3-ST.
           PERFORM UNTIL EOF-FLAG = "Y" OR G3-ST NOT = "00"
               READ F3-OUT
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END ADD 1 TO N
                       DISPLAY "L3-REC=" G3-REC
               END-READ
           END-PERFORM.
           CLOSE F3-OUT.
           DISPLAY "L3-COUNT=" N " I3=" I3-ST.
           STOP RUN.
