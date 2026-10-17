*>--- conformance-case
*> id: pb957-next-group-page-footing-relative
*> rules: [SR-13.18.37.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> A RELATIVE NEXT GROUP OF A PAGE FOOTING BEYOND THE PAGE LIMIT (kb/Work PB957).
      *> ISO/IEC 1989:2023 §13.18.37.3 SR7 b): "If the current report group is a page footing, the
      *> minimum last line number of the report group plus integer-2 shall not exceed the page limit."
      *> The footing's last line is 19, integer-2 is 2, the page limit is 20.
      *> Drawn on COBOLNET2284 (report-next-group-clause-rule).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB957NFR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb957nfr.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT 20 LINES FOOTING 16.
       01  DET TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "X".
       01  PFG TYPE PF NEXT GROUP PLUS 2.
           02  LINE 19.
               03  COLUMN 1 PIC X VALUE "F".
       PROCEDURE DIVISION.
           OPEN OUTPUT PRTF
           INITIATE RPT
           GENERATE DET
           TERMINATE RPT
           CLOSE PRTF
           STOP RUN.
