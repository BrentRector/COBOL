      *>--- conformance-case
      *> id: l1c34_report_without_detail
      *> rules: [SR-13.18.57.3-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.57.3 SR16 — no GENERATE data-name, so no DETAIL is
      *>   needed
      *> "If no GENERATE data-name statements are specified in the
      *>   procedure
      *> division, the report description need not contain a DETAIL."
      *> cite.py --check 13.18.57.3 "If no GENERATE data-name
      *>   statements are
      *>   specified in the procedure division, the report description
      *>     need not
      *>   contain a DETAIL" -> OK §13.18.57.3 16) (Syntax rules)
      *> R-ND has one body group (§13.18.57.3 SR15), a CONTROL
      *>   FOOTING, and no
      *> DETAIL; the procedure division has only GENERATE report-name
      *>   (summary
      *> reporting, §14.9.16.4 GR2). The program is therefore LEGAL
      *>   and must
      *> compile and run; an implementation demanding a DETAIL rejects
      *>   it.
      *> cite.py --check 13.18.57.3 "Each report description shall
      *>   include at
      *>   least one body group" -> OK §13.18.57.3 15) (Syntax rules)
      *> cite.py --check 13.18.57.4 "when the TERMINATE statement is
      *>   executed
      *>   for the report, provided that at least one GENERATE
      *>     statement has
      *>   been executed" -> OK §13.18.57.4 6) e) 2.
      *> DERIVATION: a USE BEFORE REPORTING procedure DISPLAYs "CF" at
      *>   each
      *> presentation of the footing.
      *>  GENERATE #1 (K=1): first GENERATE, no break, no footing   ->
      *>    (none)
      *>  GENERATE #2 (K=2): control break, CF printed (GR6 e) 1.)  ->
      *>    CF
      *>  TERMINATE: a GENERATE was executed, CF printed (GR6 e) 2.)
      *>    -> CF
      *>  then DISPLAY "END"
      *>    -> END
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C34D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "L1C34D.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R-ND.
       WORKING-STORAGE SECTION.
       01  WS-K    PIC 9 VALUE 1.
       REPORT SECTION.
       RD  R-ND CONTROL IS WS-K.
       01  G-CF TYPE CONTROL FOOTING WS-K LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "FFF".
       PROCEDURE DIVISION.
       DECLARATIVES.
       S-CF SECTION.
           USE BEFORE REPORTING G-CF.
       P-CF.
           DISPLAY "CF".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R-ND.
           GENERATE R-ND.
           MOVE 2 TO WS-K.
           GENERATE R-ND.
           TERMINATE R-ND.
           CLOSE RPT.
           DISPLAY "END".
           STOP RUN.
