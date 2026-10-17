*>--- conformance-case
*> id: pb853-report-value-without-column
*> rules: [SR-13.15.3-13]
*> source-format: free
*> portable: unverified
*>---
      *> AN ELEMENTARY REPORT ENTRY WITH A VALUE CLAUSE NEEDS A COLUMN CLAUSE (kb/Work PB853's sweep).
      *> ISO/IEC 1989:2023 §13.15.3 SR13: "A COLUMN clause shall be specified in each elementary entry that
      *> has a VALUE clause." The column-less VALUE entry below printed nothing, in silence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB853VWC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb853vwc.txt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF REPORT IS RPT.
       REPORT SECTION.
       RD RPT.
       01 DET TYPE DE.
          02 LINE 1.
             03 PIC X(3) VALUE "NOC".
             03 COLUMN 10 PIC X(3) VALUE "YES".
       PROCEDURE DIVISION.
           OPEN OUTPUT PRTF
           INITIATE RPT
           GENERATE DET
           TERMINATE RPT
           CLOSE PRTF
           STOP RUN.
