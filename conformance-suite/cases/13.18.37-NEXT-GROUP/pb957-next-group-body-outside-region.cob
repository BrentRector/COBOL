*>--- conformance-case
*> id: pb957-next-group-body-outside-region
*> rules: [SR-13.18.37.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> AN ABSOLUTE NEXT GROUP OF A BODY GROUP ABOVE THE FIRST DETAIL LINE (kb/Work PB957).
      *> ISO/IEC 1989:2023 §13.18.37.3 SR6 b): "If the current report group is a body group, integer-1
      *> shall lie between the FIRST DETAIL integer and the FOOTING integer, inclusive." FIRST DETAIL 5,
      *> integer-1 3.
      *> Drawn on COBOLNET2284 (report-next-group-clause-rule).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB957NBR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb957nbr.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT 20 LINES FIRST DETAIL 5.
       01  DET TYPE DE NEXT GROUP 3.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "X".
       PROCEDURE DIVISION.
           OPEN OUTPUT PRTF
           INITIATE RPT
           GENERATE DET
           TERMINATE RPT
           CLOSE PRTF
           STOP RUN.
