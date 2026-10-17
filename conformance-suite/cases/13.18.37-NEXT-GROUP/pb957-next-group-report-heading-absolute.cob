*>--- conformance-case
*> id: pb957-next-group-report-heading-absolute
*> rules: [SR-13.18.37.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> AN ABSOLUTE NEXT GROUP OF A REPORT HEADING THAT REACHES THE FIRST DETAIL LINE (kb/Work PB957).
      *> ISO/IEC 1989:2023 §13.18.37.3 SR6 a): "If the current report group is a report heading,
      *> integer-1 shall be greater than the minimum last line number of the report group and less than
      *> the FIRST DETAIL integer." FIRST DETAIL 5, integer-1 5.
      *> Drawn on COBOLNET2284 (report-next-group-clause-rule).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB957NRA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb957nra.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT 20 LINES FIRST DETAIL 5.
       01  RHG TYPE RH NEXT GROUP 5.
           02  LINE 1.
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
