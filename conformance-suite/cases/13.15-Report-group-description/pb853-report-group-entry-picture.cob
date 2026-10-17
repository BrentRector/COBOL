*>--- conformance-case
*> id: pb853-report-group-entry-picture
*> rules: [SR-13.15.3-11]
*> source-format: free
*> portable: unverified
*>---
      *> A GROUP REPORT ENTRY MAY NOT WRITE A PICTURE OR VALUE CLAUSE (kb/Work PB853's §13.15.3 sweep).
      *> ISO/IEC 1989:2023 §13.15.3 SR11: "The PICTURE, COLUMN, SOURCE, VALUE, SUM, and GROUP INDICATE
      *> clauses may be written only in an elementary entry." The 02 below has a subordinate 03, so it is a
      *> group entry; its PICTURE and VALUE were discarded in silence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB853GEP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb853gep.txt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(3) VALUE "ABC".
       REPORT SECTION.
       RD RPT.
       01 DET TYPE DE.
          02 LINE 1 PIC X(5) VALUE "GROUP".
             03 COLUMN 1 PIC X(3) SOURCE WS-A.
       PROCEDURE DIVISION.
           OPEN OUTPUT PRTF
           INITIATE RPT
           GENERATE DET
           TERMINATE RPT
           CLOSE PRTF
           STOP RUN.
