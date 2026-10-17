*>--- conformance-case
*> id: pb140_read_gate
*> rules: [GR-9.1.13.7-3, GR-14.9.10.4-2, GR-14.9.10.4-10,
*>    SR-14.9.30.3-8, GR-14.9.47.4-3, FMT-14.9.47.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB140RG.
      *> kb/Work PB140 - ISO 9.1.13.7 3): the last I-O statement before a
      *> sequential-access DELETE RECORD must be a successful READ, and
      *> 9.1.13.1's statement set includes UNLOCK and OPEN - so READ/UNLOCK/
      *> DELETE and READ/failed-OPEN('41')/DELETE are both '43' with the
      *> record SURVIVING. Expected values derived from the rules, per leg.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb140rg.dat"
               ORGANIZATION RELATIVE ACCESS SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           MOVE "AAAAAAAA" TO F-REC
           WRITE F-REC
           MOVE "BBBBBBBB" TO F-REC
           WRITE F-REC
           MOVE "CCCCCCCC" TO F-REC
           WRITE F-REC
           CLOSE F
           OPEN I-O F
           READ F AT END CONTINUE END-READ
           DISPLAY "READ1=" WS-ST
           UNLOCK F RECORD
           DISPLAY "UNLK=" WS-ST
           DELETE F RECORD
           DISPLAY "DEL1=" WS-ST
           READ F AT END CONTINUE END-READ
           DISPLAY "READ2=" WS-ST
           DELETE F RECORD
           DISPLAY "DEL2=" WS-ST
           CLOSE F
           OPEN I-O F
           READ F AT END CONTINUE END-READ
           DISPLAY "READ3=" WS-ST
           OPEN I-O F
           DISPLAY "OPEN41=" WS-ST
           DELETE F RECORD
           DISPLAY "DEL3=" WS-ST
           READ F AT END CONTINUE END-READ
           DISPLAY "READ4=" WS-ST
           DELETE F RECORD
           DISPLAY "DEL4=" WS-ST
           CLOSE F
           OPEN INPUT F
           READ F AT END CONTINUE END-READ
           DISPLAY "FINAL=" WS-ST " REC=" F-REC
           READ F AT END CONTINUE END-READ
           DISPLAY "EOF=" WS-ST
           CLOSE F
           STOP RUN.
