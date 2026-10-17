*>--- conformance-case
*> id: pb714_merge_using_sharing_read_only
*> rules: [GR-14.9.24.4-7, GR-14.9.24.4-12]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB714MRO.
      *> kb/Work PB714 — MERGE's two implicit OPENs carry the SHARING
      *> phrases their general rules name, exactly as SORT's do.  The
      *> emitter serves both verbs from one pair of methods, so this golden
      *> is what keeps the MERGE half from being an unexercised claim.
      *>
      *> ISO 14.9.24.4 GR7 a) (USING): "The processing of the file is
      *> initiated.  If the file-control entry for the file has a SHARING
      *> clause with the ALL phrase, the initiation is performed as if an
      *> OPEN statement with the INPUT phrase and the SHARING WITH READ ONLY
      *> phrase had been executed; otherwise, the initiation is performed as
      *> if an OPEN statement with the INPUT phrase and without a SHARING
      *> phrase is executed."
      *> ISO 14.9.24.4 GR12 a) (GIVING): "The processing of the file is
      *> initiated.  The initiation is performed as if an OPEN statement
      *> with the OUTPUT and SHARING WITH NO OTHER phrases had been
      *> executed."
      *>
      *> 9.1.13.1 and 12.4.5.8.4 GR1 are what make either refusal readable:
      *> the I-O status of the as-if OPEN is stored into the FILE STATUS
      *> item before the USE procedure GR7 a) / GR12 a) invoke runs.  Every
      *> status item is seeded "ZZ" so a status that was never stored would
      *> show as such.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
      *> LEG 1 — the USING arm.  FA declares the ALL phrase, so its implicit
      *> OPEN carries SHARING WITH READ ONLY; FA-OTH holds the same physical
      *> file open EXTEND, and Table 19 row "SHARING WITH READ ONLY / INPUT"
      *> x column "sharing with all other / extend I-O output" is
      *> Unsuccessful open => 9.1.13.9 item 1 => '61'.  '61' is nonfatal and
      *> the USE procedure completes normally, so the MERGE "continues
      *> processing as if the exception condition did not exist" (14.9.24.4
      *> GR7 a).  The as-if READ of the connector that did not open is then
      *> unsuccessful '47' (14.9.30.4 GR2) - FATAL, and with no MERGE rule of
      *> its own for a failed retrieval 9.1.13.1 transfers control "to the
      *> end of the statement that produced the fatal exception condition"
      *> (kb/Work PB993): FB is never read and FM-OUT is never created - its
      *> OPEN INPUT below answers '35', and the read-back loop, which stops
      *> on any unsuccessful status, counts 0.
           SELECT FA ASSIGN TO "pb714mroa.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               FILE STATUS IS FA-ST.
           SELECT FA-OTH ASSIGN TO "pb714mroa.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS FAO-ST.
           SELECT FB ASSIGN TO "pb714mrob.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FB-ST.
           SELECT FM-OUT ASSIGN TO "pb714mrom.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FM-ST.
      *> LEG 2 — the GIVING arm.  FG-OUT's physical file is held open INPUT
      *> by FG-OTH across the MERGE.  Every "Open request" row of Table 19
      *> whose open mode is OUTPUT is Unsuccessful open in all five columns
      *> — 9.1.13.9 1) e), "An attempt is made to open a physical file in
      *> the output mode and the physical file is currently open by another
      *> file connector" — so the implicit OPEN OUTPUT is refused '61'
      *> whatever sharing mode it carries.  What this leg pins is that the
      *> refusal is STORED: the USE procedure GR12 a) invokes sees '61' and
      *> not the seeded "ZZ".  '61' is nonfatal and the USE procedure
      *> completes normally, so the file "is processed as if the exception
      *> did not exist" (14.9.24.4 GR12 a); each as-if WRITE then fails '48'
      *> and, its USE procedure completing normally, "the MERGE continues
      *> execution" (GR12 b) - both records; the as-if CLOSE answers '42'.
           SELECT FC ASSIGN TO "pb714mroc.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FC-ST.
           SELECT FD-FILE ASSIGN TO "pb714mrod.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FD-ST.
           SELECT FG-OUT ASSIGN TO "pb714mrog.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FG-ST.
           SELECT FG-OTH ASSIGN TO "pb714mrog.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FGO-ST.
           SELECT MRG-FILE ASSIGN TO "pb714mros.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD FA.
       01 FA-REC   PIC X(5).
       FD FA-OTH.
       01 FAO-REC  PIC X(5).
       FD FB.
       01 FB-REC   PIC X(5).
       FD FM-OUT.
       01 FM-REC   PIC X(5).
       FD FC.
       01 FC-REC   PIC X(5).
       FD FD-FILE.
       01 FD-REC   PIC X(5).
       FD FG-OUT.
       01 FG-REC   PIC X(5).
       FD FG-OTH.
       01 FGO-REC  PIC X(5).
       SD MRG-FILE.
       01 MRG-REC  PIC X(5).
       WORKING-STORAGE SECTION.
       01 FA-ST    PIC XX.
       01 FAO-ST   PIC XX.
       01 FB-ST    PIC XX.
       01 FM-ST    PIC XX.
       01 FC-ST    PIC XX.
       01 FD-ST    PIC XX.
       01 FG-ST    PIC XX.
       01 FGO-ST   PIC XX.
       01 N        PIC 9 VALUE 0.
       01 EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DA-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FA.
       DA-PARA.
           DISPLAY "M1-USE-FA=" FA-ST.
       DG-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FG-OUT.
       DG-PARA.
           DISPLAY "M2-USE-FG=" FG-ST.
       END DECLARATIVES.
       MAIN SECTION.
       SEED-LEG-1.
           MOVE "ZZ" TO FA-ST.
           OPEN OUTPUT FA-OTH.
           MOVE "AAAAA" TO FAO-REC.
           WRITE FAO-REC.
           MOVE "CCCCC" TO FAO-REC.
           WRITE FAO-REC.
           CLOSE FA-OTH.
           OPEN OUTPUT FB.
           MOVE "BBBBB" TO FB-REC.
           WRITE FB-REC.
           MOVE "DDDDD" TO FB-REC.
           WRITE FB-REC.
           CLOSE FB.
       MERGE-LEG-1.
           OPEN EXTEND FA-OTH.
           MERGE MRG-FILE ON ASCENDING KEY MRG-REC
               USING FA FB GIVING FM-OUT.
           CLOSE FA-OTH.
           MOVE 0 TO N.
           MOVE "N" TO EOF-FLAG.
           OPEN INPUT FM-OUT.
           DISPLAY "M1-OUT-OPEN=" FM-ST.
           PERFORM UNTIL EOF-FLAG = "Y" OR FM-ST NOT = "00"
               READ FM-OUT
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END ADD 1 TO N
                       DISPLAY "M1-REC=" FM-REC
               END-READ
           END-PERFORM.
           CLOSE FM-OUT.
           DISPLAY "M1-COUNT=" N " FA=" FA-ST.
       SEED-LEG-2.
           MOVE "ZZ" TO FG-ST.
           OPEN OUTPUT FC.
           MOVE "EEEEE" TO FC-REC.
           WRITE FC-REC.
           CLOSE FC.
           OPEN OUTPUT FD-FILE.
           MOVE "FFFFF" TO FD-REC.
           WRITE FD-REC.
           CLOSE FD-FILE.
           OPEN OUTPUT FG-OTH.
           MOVE "OLDIE" TO FGO-REC.
           WRITE FGO-REC.
           CLOSE FG-OTH.
       MERGE-LEG-2.
           OPEN INPUT FG-OTH.
           MERGE MRG-FILE ON ASCENDING KEY MRG-REC
               USING FC FD-FILE GIVING FG-OUT.
           CLOSE FG-OTH.
           DISPLAY "M2-FG=" FG-ST.
      *> 14.9.27.4 GR25 — an unsuccessful OPEN leaves the file "not
      *> affected", so the refused implicit OPEN OUTPUT truncated nothing
      *> and the pre-existing record is still there.
           MOVE 0 TO N.
           MOVE "N" TO EOF-FLAG.
           OPEN INPUT FG-OUT.
           PERFORM UNTIL EOF-FLAG = "Y"
               READ FG-OUT
                   AT END MOVE "Y" TO EOF-FLAG
                   NOT AT END ADD 1 TO N
                       DISPLAY "M2-REC=" FG-REC
               END-READ
           END-PERFORM.
           CLOSE FG-OUT.
           DISPLAY "M2-COUNT=" N.
           STOP RUN.
