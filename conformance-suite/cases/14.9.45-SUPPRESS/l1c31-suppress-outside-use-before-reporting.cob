      *>--- conformance-case
      *> id: l1c31-suppress-outside-use-before-reporting
      *> rules: [SR-14.9.45.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.45.3 SR1 — SUPPRESS written in a nondeclarative
      *> procedure (not a USE BEFORE REPORTING procedure).
      *> Rule: "The SUPPRESS statement may appear only in a USE BEFORE
      *> REPORTING procedure."
      *> cite.py --check 14.9.45.3 "The SUPPRESS statement may appear
      *>   only in a USE BEFORE REPORTING procedure."
      *>   -> OK  §14.9.45.3 1)  (Syntax rule)
      *> The program is otherwise a valid Report Writer program (the
      *> same shape as conformance:2023/rw_suppress with the SUPPRESS
      *> moved out of its declarative into MAIN-PARA and no
      *> DECLARATIVES at all), so the only reason to reject it is SR1:
      *> expect COBOLNET1581 (report-suppress-context).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C31C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "L1C31C.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-SUP.
       WORKING-STORAGE SECTION.
       01 WS-AMT  PIC 99 VALUE 0.
       REPORT SECTION.
       RD R-SUP
           PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(4) VALUE "AMT=".
          02 COLUMN 5 PIC 99 SOURCE IS WS-AMT.
       PROCEDURE DIVISION.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R-SUP.
           MOVE 10 TO WS-AMT.
           SUPPRESS PRINTING.
           GENERATE DET-A.
           TERMINATE R-SUP.
           CLOSE RPT.
           STOP RUN.
