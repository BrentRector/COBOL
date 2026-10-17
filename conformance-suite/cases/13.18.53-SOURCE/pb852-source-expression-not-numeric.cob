*>--- conformance-case
*> id: pb852-source-expression-not-numeric
*> rules: [SR-13.18.53.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.53.3 SR3 - "If arithmetic-expression-1 or the ROUNDED phrase
      *> is specified, the entry shall define either a numeric data item or a
      *> numeric-edited data item."  The entry below defines PIC X(5) -
      *> alphanumeric - and writes an arithmetic-expression operand.  The rule
      *> exists because 13.18.53.4 GR2 makes that operand the sending side of an
      *> implicit COMPUTE whose receiving operand IS this item, and a COMPUTE has
      *> no receiving category outside numeric and numeric-edited (14.9.8.3 SR1).
      *> Before kb/Work PB852 the expression operand had no grammar surface at
      *> all, so SR3 had no reachable population to screen.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB852N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb852n2.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 999 VALUE 100.
       01 WS-B PIC 999 VALUE 23.
       REPORT SECTION.
       RD R-1 PAGE LIMIT IS 20 LINES.
       01 DET-A TYPE DE LINE PLUS 1.
          03 COLUMN 1 PIC X(5) SOURCE IS WS-A + WS-B.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
