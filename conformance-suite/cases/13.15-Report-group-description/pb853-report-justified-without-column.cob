*>--- conformance-case
*> id: pb853-report-justified-without-column
*> rules: [SR-13.15.3-15]
*> source-format: free
*> portable: unverified
*>---
      *> JUSTIFIED OR BLANK WHEN ZERO IN A REPORT ENTRY NEEDS A COLUMN CLAUSE (kb/Work PB853's sweep).
      *> ISO/IEC 1989:2023 §13.15.3 SR15: "If BLANK WHEN ZERO or JUSTIFIED is specified, a COLUMN clause
      *> shall also be specified." The 03 below is a legal non-printable SOURCE entry except for JUSTIFIED.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB853JWC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb853jwc.txt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(3) VALUE "ABC".
       REPORT SECTION.
       RD RPT.
       01 DET TYPE DE.
          02 LINE 1.
             03 PIC X(3) JUSTIFIED SOURCE WS-A.
             03 COLUMN 10 PIC X(3) VALUE "END".
       PROCEDURE DIVISION.
           OPEN OUTPUT PRTF
           INITIATE RPT
           GENERATE DET
           TERMINATE RPT
           CLOSE PRTF
           STOP RUN.
