*>--- conformance-case
*> id: pb205-control-operand-report-section
*> rules: [SR-13.18.16.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.16.3 SR2: "Data-name-1 shall not be defined in the report
      *> section."
      *> MEASURED BEFORE kb/Work PB205: rejected, but under the WRONG RULE - a
      *> report-section name is not in the storage forest, so the operand failed
      *> ordinary name resolution and drew the 8.4.2.1 unresolved-operand
      *> diagnostic. Right verdict, wrong rule, and it would have changed meaning
      *> the day report-section names became resolvable.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB205N3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb205n3.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-SRC PIC 99 VALUE 7.
       REPORT SECTION.
       RD R-1 CONTROL IS RS-ITEM.
       01 DET-A TYPE DE LINE PLUS 1.
          02 RS-ITEM COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
