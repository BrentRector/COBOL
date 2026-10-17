*>--- conformance-case
*> id: pb957-next-group-page-footing-absolute
*> rules: [SR-13.18.37.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> AN ABSOLUTE NEXT GROUP OF A PAGE FOOTING AT ITS OWN LAST LINE (kb/Work PB957).
      *> ISO/IEC 1989:2023 §13.18.37.3 SR6 c): "If the current report group is a page footing, integer-1
      *> shall be greater than the minimum last line number of the report group." The footing's last line
      *> is 17 and integer-1 is 17.
      *> Drawn on COBOLNET2284 (report-next-group-clause-rule).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB957NFA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb957nfa.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT 20 LINES FOOTING 16.
       01  DET TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "X".
       01  PFG TYPE PF NEXT GROUP 17.
           02  LINE 17.
               03  COLUMN 1 PIC X VALUE "F".
       PROCEDURE DIVISION.
           OPEN OUTPUT PRTF
           INITIATE RPT
           GENERATE DET
           TERMINATE RPT
           CLOSE PRTF
           STOP RUN.
