*>--- conformance-case
*> id: pb483-report-format-element-repeated
*> rules: [SR-13.14.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.14.2 prints each clause of the report description entry in its own bracket, and 13.18.39.2
      *> each phrase of the PAGE clause, with no ellipsis: 5.2.6.2 - a bracketed element "may be explicitly
      *> specified or that portion of the general format may be omitted"; 5.2.7 - repetition exists only where
      *> an ellipsis stands.  13.14.3 SR2 ("The clauses that follow report-name-1 may appear in any order")
      *> and 13.18.39.3 SR4 (the phrases "may be written in any order") license ORDER, never a repeat.  The
      *> HEADING phrase below is written twice (the second value used to win silently) - COBOLNET2423
      *> (kb/Work PB483's sibling sweep).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB483NRP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb483nrp.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-K PIC 9 VALUE 1.
       REPORT SECTION.
       RD R-1 PAGE LIMIT IS 30 LINES HEADING 1 HEADING 2.
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
