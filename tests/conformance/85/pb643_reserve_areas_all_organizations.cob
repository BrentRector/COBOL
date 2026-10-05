      *> kb/Work PB643 - RESERVE integer-1 AREAS on every organization:
      *> the areas are allocated (sequential: the connector's reader and
      *> writer; relative and indexed: the store handle) and the records
      *> pass through them unchanged.
      *>
      *> "If the RESERVE clause is specified, the number of
      *> input-output areas allocated is equal to the value of
      *> integer-1."
      *>   cite.py: OK  §12.4.5.14.3 1)  (General rule)
      *> "The RESERVE clause allows the user to specify the number of
      *> input-output areas allocated."
      *>   cite.py: OK  §12.4.5.14.1  (General)
      *> The clause names a COUNT of areas and nothing else, so it
      *> changes no record a program writes or reads.
      *>
      *> DERIVATION. With an input-output area of 4,096 bytes
      *> (docs/CONFORMANCE.md DOC-A.1-164):
      *>   SEQ (RESERVE 3 AREAS = 12,288 bytes): 300 records of 50
      *>     bytes (15,000 bytes) overflow the areas once before the
      *>     CLOSE; read back -> 300 records, first 001, last 300.
      *>   REL (RESERVE 2 AREAS): relative records 1 through 9; a
      *>     random READ of key 7 -> 007.
      *>   IDX (RESERVE 1 AREA): keys K01 through K09; a random READ
      *>     of K04 -> 004.
      *>   RPT, a report file (RESERVE 2 AREAS): 300 GENERATEs of a
      *>     one-line detail "ABC"; read back one byte at a time by
      *>     a second SELECT (CHK-F) -> 300 "A" bytes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB643ORG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-F ASSIGN TO "pb643seq.dat"
               ORGANIZATION IS SEQUENTIAL
               RESERVE 3 AREAS
               FILE STATUS IS S-ST.
           SELECT REL-F ASSIGN TO "pb643rel.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS REL-KEY
               RESERVE 2 AREAS
               FILE STATUS IS R-ST.
           SELECT IDX-F ASSIGN TO "pb643idx.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IDX-KEY
               RESERVE 1 AREA
               FILE STATUS IS X-ST.
           SELECT RPT-F ASSIGN TO "pb643rpt.txt"
               RESERVE 2 AREAS.
           SELECT CHK-F ASSIGN TO "pb643rpt.txt"
               FILE STATUS IS C-ST.
       DATA DIVISION.
       FILE SECTION.
       FD SEQ-F.
       01 SEQ-REC.
          05 SEQ-NO   PIC 9(3).
          05 SEQ-FILL PIC X(47).
       FD REL-F.
       01 REL-REC.
          05 REL-NO   PIC 9(3).
          05 REL-FILL PIC X(17).
       FD IDX-F.
       01 IDX-REC.
          05 IDX-KEY  PIC X(3).
          05 IDX-NO   PIC 9(3).
       FD RPT-F REPORT IS R-P.
       FD CHK-F.
       01 CHK-REC  PIC X.
       WORKING-STORAGE SECTION.
       01 C-ST     PIC XX.
       01 S-ST     PIC XX.
       01 R-ST     PIC XX.
       01 X-ST     PIC XX.
       01 REL-KEY  PIC 9(4).
       01 I        PIC 9(3).
       01 N        PIC 9(3).
       01 FIRST-NO PIC 9(3).
       01 LAST-NO  PIC 9(3).
       REPORT SECTION.
       RD R-P.
       01 DET TYPE DE.
          02 LINE PLUS 1.
             03 COLUMN 1 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-F
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 300
               MOVE I TO SEQ-NO
               MOVE ALL "S" TO SEQ-FILL
               WRITE SEQ-REC
           END-PERFORM
           CLOSE SEQ-F
           MOVE 0 TO N
           OPEN INPUT SEQ-F
           PERFORM UNTIL S-ST NOT = "00"
               READ SEQ-F
               IF S-ST = "00"
                   ADD 1 TO N
                   IF N = 1
                       MOVE SEQ-NO TO FIRST-NO
                   END-IF
                   MOVE SEQ-NO TO LAST-NO
               END-IF
           END-PERFORM
           CLOSE SEQ-F
           DISPLAY "SEQ " N " FIRST " FIRST-NO " LAST " LAST-NO
           OPEN OUTPUT REL-F
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 9
               MOVE I TO REL-KEY
               MOVE I TO REL-NO
               MOVE ALL "R" TO REL-FILL
               WRITE REL-REC
           END-PERFORM
           CLOSE REL-F
           OPEN INPUT REL-F
           MOVE 7 TO REL-KEY
           READ REL-F
           DISPLAY "REL " R-ST " " REL-NO
           CLOSE REL-F
           OPEN OUTPUT IDX-F
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 9
               MOVE I TO IDX-NO
               MOVE "K0" TO IDX-KEY(1:2)
               MOVE IDX-NO(3:1) TO IDX-KEY(3:1)
               WRITE IDX-REC
           END-PERFORM
           CLOSE IDX-F
           OPEN INPUT IDX-F
           MOVE "K04" TO IDX-KEY
           READ IDX-F
           DISPLAY "IDX " X-ST " " IDX-NO
           CLOSE IDX-F
           OPEN OUTPUT RPT-F
           INITIATE R-P
           PERFORM 300 TIMES
               GENERATE DET
           END-PERFORM
           TERMINATE R-P
           CLOSE RPT-F
           MOVE 0 TO N
           OPEN INPUT CHK-F
           PERFORM UNTIL C-ST NOT = "00"
               READ CHK-F
               IF C-ST = "00" AND CHK-REC = "A"
                   ADD 1 TO N
               END-IF
           END-PERFORM
           CLOSE CHK-F
           DISPLAY "RPT " N
           STOP RUN.
