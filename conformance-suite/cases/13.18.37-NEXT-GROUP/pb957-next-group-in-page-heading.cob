*>--- conformance-case
*> id: pb957-next-group-in-page-heading
*> rules: [SR-13.18.37.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> A NEXT GROUP CLAUSE IN A PAGE HEADING (kb/Work PB957).
      *> ISO/IEC 1989:2023 §13.18.37.3 SR4: "The NEXT GROUP clause shall not be specified in a page
      *> heading or report footing."
      *> Drawn on COBOLNET2284 (report-next-group-clause-rule).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB957NPH.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb957nph.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT 20 LINES.
       01  PHG TYPE PH NEXT GROUP PLUS 1.
           02  LINE 1.
               03  COLUMN 1 PIC X VALUE "H".
       01  DET TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "X".
       PROCEDURE DIVISION.
           OPEN OUTPUT PRTF
           INITIATE RPT
           GENERATE DET
           TERMINATE RPT
           CLOSE PRTF
           STOP RUN.
