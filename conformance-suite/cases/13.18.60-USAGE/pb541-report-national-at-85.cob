*>--- conformance-case
*> id: pb541-report-national-at-85
*> rules: [SR-13.18.60.3-7]
*> source-format: free
*> portable: unverified
*>---
*> ⛔ THE NEGATIVE BELOW THE INTRODUCING EDITION (kb/Work PB541). §13.18.60.3 SR7 admits a NATIONAL report
*> group item — conformance:2002/pb541_report_national_item is the positive — but national data itself is a
*> COBOL-2002 introduction (ISO §8.5.2, category national), so the same entry at --std 85 is not conforming
*> COBOL-85 and the edition gate refuses it (COBOLNET0900).
*>
*> This is the edition-gate sweep the new SR7 screen owes: widening a usage gate from DISPLAY to
*> DISPLAY-or-NATIONAL must not let a post-85 category through an '85 compilation, and the gate fires on the
*> REPORT ITEM in its own right ("RD 'R-1' printable item …"), not merely on the working-storage item the
*> SOURCE clause names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB541RN85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb541rn85.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01  WN PIC N(4) VALUE N"WXYZ".
       REPORT SECTION.
       RD  R-1 PAGE LIMIT IS 10 LINES.
       01  DET-1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC N(4) USAGE NATIONAL SOURCE WN.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-1.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
