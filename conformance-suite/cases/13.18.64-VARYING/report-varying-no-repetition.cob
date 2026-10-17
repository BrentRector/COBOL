*>--- conformance-case
*> id: report-varying-no-repetition
*> rules: [SR-13.18.64.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.64.3 SR1 — an entry containing a VARYING clause shall also
      *> contain an OCCURS clause or, in a report group description entry, a
      *> multiple LINE or multiple COLUMN clause. A single-operand COLUMN entry
      *> gives the counter nothing to repeat over (COBOLNET1559).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWVYSR1P10RP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "rwvysr1-p10rp.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       REPORT SECTION.
       RD R-1.
       01 D-1 TYPE DE.
          03 LINE PLUS 1.
             05 COLUMN 1 PIC 9 SOURCE IS RV-A
                VARYING RV-A FROM 1.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE D-1.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
