*>--- conformance-case
*> id: pb363-generate-in-before-reporting
*> rules: [SR-14.9.49.3-10]
*> source-format: free
*> portable: unverified
*>---
*> ISO §14.9.49.3 SR10: "The GENERATE, INITIATE, or TERMINATE statements shall not appear in a
*> paragraph within a USE BEFORE REPORTING procedure." (kb/Work PB363.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB363GBR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb363gbr.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-GRP PIC 9 VALUE 1.
       01 WS-SEQ PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-1 CONTROL IS WS-GRP
           PAGE LIMIT IS 20 LINES.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE IS WS-SEQ.
       PROCEDURE DIVISION.
       DECLARATIVES.
       BR-SEC SECTION.
           USE BEFORE REPORTING DET-A.
       BR-P.
           GENERATE DET-A.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
