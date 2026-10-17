*>--- conformance-case
*> id: pb957-next-group-not-level-1
*> rules: [SR-13.15.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> A NEXT GROUP CLAUSE ON A LEVEL 2 ENTRY (kb/Work PB957).
      *> ISO/IEC 1989:2023 §13.15.3 SR6: "The NEXT GROUP clause may be specified only in a level 1 entry."
      *> Drawn on COBOLNET2284 (report-next-group-clause-rule).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB957NL1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb957nl1.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT 20 LINES.
       01  DET TYPE DE.
           02  LINE PLUS 1 NEXT GROUP PLUS 1.
               03  COLUMN 1 PIC X VALUE "X".
       PROCEDURE DIVISION.
           OPEN OUTPUT PRTF
           INITIATE RPT
           GENERATE DET
           TERMINATE RPT
           CLOSE PRTF
           STOP RUN.
