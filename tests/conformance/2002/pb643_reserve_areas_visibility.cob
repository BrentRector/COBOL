      *> kb/Work PB643 - the RESERVE clause allocates integer-1
      *> input-output areas, and how many there are is VISIBLE to a
      *> second file connector of the same physical file.
      *>
      *> "If the RESERVE clause is specified, the number of
      *> input-output areas allocated is equal to the value of
      *> integer-1. If the RESERVE clause is not specified, the number
      *> of input-output areas allocated is specified by the
      *> implementor."
      *>   cite.py: OK  §12.4.5.14.3 1)  (General rule)
      *> "The successful execution of a WRITE statement releases a
      *> logical record to the operating environment."
      *>   cite.py: OK  §14.9.51.4 12)  (General rules)
      *>
      *> WiseOwl COBOL's determination (docs/CONFORMANCE.md
      *> DOC-A.1-164): an input-output area is 4,096 bytes of buffer
      *> between the connector's record area and the medium; with no
      *> RESERVE clause a connector has ONE. A connector that holds the
      *> only writable handle releases its records to the medium as its
      *> areas fill (and at CLOSE); a connector whose sharing mode admits
      *> another writer releases each record during its own WRITE.
      *>
      *> DERIVATION. Each phase empties the file, opens a writer in the
      *> extend mode, WRITEs 100 records of 64 bytes (6,400 bytes) and
      *> then counts what a second connector (RDR) reads, before and
      *> after the writer's CLOSE.
      *>   no clause / RESERVE 1 AREA: one 4,096-byte area fills after
      *>     64 records and goes to the medium when the 65th arrives;
      *>     the other 36 (2,304 bytes) stay in the area -> 064, then
      *>     100 after CLOSE.
      *>   RESERVE 2 AREAS: 8,192 bytes of area hold all 6,400 bytes
      *>     -> 000, then 100 after CLOSE. Without the clause's effect
      *>     this phase would read 064, like the two above.
      *>   RESERVE 5 AREAS under SHARING WITH ALL OTHER: the sharing
      *>     mode admits another writer, so every WRITE releases its
      *>     record through the areas -> 100, then 100.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB643VIS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT W-DFLT ASSIGN TO "pb643vis.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH READ ONLY
               FILE STATUS IS W-ST.
           SELECT W-ONE ASSIGN TO "pb643vis.dat"
               ORGANIZATION IS SEQUENTIAL
               RESERVE 1 AREA
               SHARING WITH READ ONLY
               FILE STATUS IS W-ST.
           SELECT W-TWO ASSIGN TO "pb643vis.dat"
               ORGANIZATION IS SEQUENTIAL
               RESERVE 2 AREAS
               SHARING WITH READ ONLY
               FILE STATUS IS W-ST.
           SELECT W-ALL ASSIGN TO "pb643vis.dat"
               ORGANIZATION IS SEQUENTIAL
               RESERVE 5 AREAS
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS W-ST.
           SELECT RDR ASSIGN TO "pb643vis.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS R-ST.
       DATA DIVISION.
       FILE SECTION.
       FD W-DFLT.
       01 DFLT-REC  PIC X(64).
       FD W-ONE.
       01 ONE-REC   PIC X(64).
       FD W-TWO.
       01 TWO-REC   PIC X(64).
       FD W-ALL.
       01 ALL-REC   PIC X(64).
       FD RDR.
       01 RDR-REC   PIC X(64).
       WORKING-STORAGE SECTION.
       01 W-ST      PIC XX.
       01 R-ST      PIC XX.
       01 I         PIC 9(3).
       01 SEEN      PIC 9(3).
       01 SEEN-OPEN PIC 9(3).
       01 THE-REC.
          05 REC-NO PIC 9(3).
          05 FILLER PIC X(61) VALUE ALL "R".
       PROCEDURE DIVISION.
       MAIN-PARA.
           PERFORM EMPTY-FILE
           OPEN EXTEND W-DFLT
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 100
               MOVE I TO REC-NO
               WRITE DFLT-REC FROM THE-REC
           END-PERFORM
           PERFORM COUNT-VISIBLE
           MOVE SEEN TO SEEN-OPEN
           CLOSE W-DFLT
           PERFORM COUNT-VISIBLE
           DISPLAY "NO CLAUSE        OPEN " SEEN-OPEN " CLOSED " SEEN
           PERFORM EMPTY-FILE
           OPEN EXTEND W-ONE
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 100
               MOVE I TO REC-NO
               WRITE ONE-REC FROM THE-REC
           END-PERFORM
           PERFORM COUNT-VISIBLE
           MOVE SEEN TO SEEN-OPEN
           CLOSE W-ONE
           PERFORM COUNT-VISIBLE
           DISPLAY "RESERVE 1 AREA   OPEN " SEEN-OPEN " CLOSED " SEEN
           PERFORM EMPTY-FILE
           OPEN EXTEND W-TWO
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 100
               MOVE I TO REC-NO
               WRITE TWO-REC FROM THE-REC
           END-PERFORM
           PERFORM COUNT-VISIBLE
           MOVE SEEN TO SEEN-OPEN
           CLOSE W-TWO
           PERFORM COUNT-VISIBLE
           DISPLAY "RESERVE 2 AREAS  OPEN " SEEN-OPEN " CLOSED " SEEN
           PERFORM EMPTY-FILE
           OPEN EXTEND W-ALL
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 100
               MOVE I TO REC-NO
               WRITE ALL-REC FROM THE-REC
           END-PERFORM
           PERFORM COUNT-VISIBLE
           MOVE SEEN TO SEEN-OPEN
           CLOSE W-ALL
           PERFORM COUNT-VISIBLE
           DISPLAY "RESERVE 5 SHARED OPEN " SEEN-OPEN " CLOSED " SEEN
           STOP RUN.
       EMPTY-FILE.
           OPEN OUTPUT W-DFLT
           CLOSE W-DFLT.
       COUNT-VISIBLE.
           MOVE 0 TO SEEN
           OPEN INPUT RDR
           IF R-ST NOT = "00"
               DISPLAY "RDR OPEN STATUS " R-ST
           END-IF
           PERFORM UNTIL R-ST NOT = "00"
               READ RDR
               IF R-ST = "00"
                   ADD 1 TO SEEN
               END-IF
           END-PERFORM
           CLOSE RDR.
