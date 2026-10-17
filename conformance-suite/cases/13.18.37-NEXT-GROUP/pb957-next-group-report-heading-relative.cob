*>--- conformance-case
*> id: pb957-next-group-report-heading-relative
*> rules: [SR-13.18.37.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> A RELATIVE NEXT GROUP OF A REPORT HEADING THAT REACHES THE FIRST DETAIL LINE (kb/Work PB957).
      *> ISO/IEC 1989:2023 §13.18.37.3 SR7 a): "If the current report group is a report heading, the
      *> minimum last line number of the report group plus integer-2 shall be less than the FIRST DETAIL
      *> integer." The heading's last line is 3, integer-2 is 2, FIRST DETAIL is 5: 3 + 2 is not less.
      *> Drawn on COBOLNET2284 (report-next-group-clause-rule).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB957NRR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb957nrr.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT 20 LINES FIRST DETAIL 5.
       01  RHG TYPE RH NEXT GROUP + 2.
           02  LINE 3.
               03  COLUMN 1 PIC X VALUE "R".
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
