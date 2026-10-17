*>--- conformance-case
*> id: pb363-control-item-altered
*> rules: [SR-14.9.49.3-11]
*> source-format: free
*> portable: unverified
*>---
*> ISO §14.9.49.3 SR11: "A USE BEFORE REPORTING procedure shall not alter the value of any control
*> data item." WS-GRP is the CONTROL clause operand (kb/Work PB363). The legal store into a
*> non-control item is conformance:cases/14.9.49-USE/pb363_before_reporting_legal_store.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB363CIA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb363cia.rpt".
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
           MOVE 2 TO WS-GRP.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
