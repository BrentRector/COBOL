*>--- conformance-case
*> id: pb957-next-group-next-page-in-page-footing
*> rules: [SR-13.18.37.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> NEXT GROUP NEXT PAGE IN A PAGE FOOTING (kb/Work PB957).
      *> ISO/IEC 1989:2023 §13.18.37.3 SR5: "The NEXT PAGE phrase shall not be specified in a page
      *> footing."
      *> Drawn on COBOLNET2284 (report-next-group-clause-rule).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB957NPF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb957npf.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS RPT.
       REPORT SECTION.
       RD  RPT PAGE LIMIT 20 LINES FOOTING 18.
       01  DET TYPE DE.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X VALUE "X".
       01  PFG TYPE PF NEXT GROUP NEXT PAGE.
           02  LINE 19.
               03  COLUMN 1 PIC X VALUE "F".
       PROCEDURE DIVISION.
           OPEN OUTPUT PRTF
           INITIATE RPT
           GENERATE DET
           TERMINATE RPT
           CLOSE PRTF
           STOP RUN.
