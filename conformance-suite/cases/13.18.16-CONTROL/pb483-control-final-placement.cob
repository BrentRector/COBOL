*>--- conformance-case
*> id: pb483-control-final-placement
*> rules: [GR-13.18.16.4-2, FMT-13.18.16.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.16.2 (CONTROL clause general format, rendered from the PDF): the second operand form is
      *> FINAL [ data-name-1 ] ... - FINAL once, as the FIRST operand; the ellipsis applies only to the
      *> bracket enclosing data-name-1 (5.2.7).  13.18.16.4 GR2: "FINAL, if specified, is associated with the
      *> highest level in the hierarchy."  CONTROLS ARE FINAL FINAL WS-K writes FINAL twice - a second FINAL
      *> level that could never break - and is COBOLNET2421 (kb/Work PB483).  The positive twin is
      *> conformance/85/pb483_control_final_first.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB483NFF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb483nff.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-K PIC 9 VALUE 1.
       REPORT SECTION.
       RD R-1 CONTROLS ARE FINAL FINAL WS-K
           PAGE LIMIT IS 30 LINES.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE WS-K.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE RPT.
           STOP RUN.
