      *>--- conformance-case
      *> id: pb363_before_reporting_legal_store
      *> rules: [SR-14.9.49.3-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.49.3 SR10/SR11 (kb/Work PB363) restrict a USE BEFORE
      *> REPORTING procedure: no GENERATE, INITIATE or TERMINATE in its
      *> paragraphs, and no alteration of a control data item. This is the
      *> LEGAL shape: the procedure stores into WS-SEQ, which is NOT a
      *> control data item (the CONTROL clause names WS-GRP).
      *> §14.9.49.4 GR9 performs the procedure "on each occasion that the
      *> named report group is processed", d) "Before the processing of
      *> any LINE clauses defined for the report group and any printable
      *> items described in entries subordinate to them" - once per
      *> GENERATE DET-A, so the procedure's own DISPLAY shows 1, 2, 3 and
      *> WS-SEQ ends at 3. WS-GRP never changes, so no control break.
      *> (The report file is not read back: the LINE SEQUENTIAL
      *> organization that would read it is a COBOL-2023 introduction.)
      *> The refused shapes are conformance:negative/pb363-*.
      *> EDITION: Report Writer and USE BEFORE REPORTING are COBOL-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB363BRS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb363brs.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-GRP PIC 9 VALUE 1.
       01 WS-SEQ PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-1 CONTROL IS WS-GRP
           PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(4) VALUE "SEQ=".
          02 COLUMN 5 PIC 9 SOURCE IS WS-SEQ.
       PROCEDURE DIVISION.
       DECLARATIVES.
       BR-SEC SECTION.
           USE BEFORE REPORTING DET-A.
       BR-P.
           ADD 1 TO WS-SEQ.
           DISPLAY "BEFORE-REPORTING SEQ=" WS-SEQ.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DET-A.
           GENERATE DET-A.
           GENERATE DET-A.
           TERMINATE R-1.
           CLOSE RPT.
           DISPLAY "WS-SEQ=" WS-SEQ.
           STOP RUN.
