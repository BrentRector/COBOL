*>--- conformance-case
*> id: pb883-sum-expression-report-section
*> rules: [SR-13.18.54.3-1, SR-13.18.54.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.54.3 SR6 - "If the addend is arithmetic-expression-1, any
      *> identifiers it contains may reference entries in any section of the data
      *> division other than the report section."  The expression addend below
      *> names CF-T, a sum counter of this very report (13.18.54.4 GR5 - "the
      *> data-name is the name of the sum counter"), which is a report section
      *> entry and therefore outside what SR6 admits.  Note how narrowly the
      *> standard draws this: the SOURCE clause's twin rule, 13.18.53.3 SR4, DOES
      *> admit "a report counter identifier or a sum counter defined in the
      *> current report", and tests/conformance/85/pb852_report_source_expression
      *> exercises that side.  Two clauses, two lines, and each is screened
      *> against its own rule (COBOLNET2144).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB883N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb883n2.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 99 VALUE 11.
       REPORT SECTION.
       RD R-1 CONTROL IS FINAL PAGE LIMIT IS 20 LINES.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
       01 CFT TYPE CF FINAL LINE PLUS 1.
          02 CF-T COLUMN 1 PIC 9999 SUM WS-A.
          02 CF-V COLUMN 7 PIC 9999 SUM CF-T + 1.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
