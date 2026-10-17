      *>--- conformance-case
      *> id: l1c33_terminate_resume_next_implicit
      *> rules: [GR-14.9.46.4-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.46.4 GR4 — RESUME NEXT: next implicit TERMINATE
      *> GR4 sentence 2: "If an implicit TERMINATE statement results in
      *> the execution of a declarative procedure that executes a RESUME
      *> statement with the NEXT STATEMENT phrase, processing resumes at
      *> the next implicit TERMINATE statement, if any."
      *>   cite.py: OK  §14.9.46.4 4)  (General rules)
      *> Supporting: §14.9.46.4 GR1 "If the report is not in the active
      *> state, the EC-REPORT-INACTIVE exception condition is set to
      *> exist" (cite.py: OK  §14.9.46.4 1)); §14.9.33.4 GR2a - without
      *> GR4 the implicit CONTINUE "immediately follows the end of the
      *> statement that was executing when control was transferred to
      *> the exception processing procedure unless general rules
      *> associated with the applicable statement specify otherwise"
      *> (cite.py: OK  §14.9.33.4 2) a)).
      *> §14.9.46.4 GR4 is that "otherwise".
      *> §14.9.49.4 GR8 (cite.py: OK  §14.9.49.4 8)) makes each report
      *> footing's production visible through USE BEFORE REPORTING.
      *> DERIVATION. R-A and R-C are active and have had a GENERATE;
      *> R-B was never initiated. TERMINATE R-C R-B R-A is three
      *> implicit TERMINATEs in written order (GR4 sentence 1):
      *>   R-C: its RF is produced              -> "RF-C"
      *>   R-B: inactive, EC-REPORT-INACTIVE raised (checking is ON);
      *>        the declarative runs            -> "INACTIVE EC-REPORT-
      *>        INACTIVE" and RESUMEs AT NEXT STATEMENT, which per GR4
      *>        is the next implicit TERMINATE, R-A -> "RF-A"
      *>   then the statement ends              -> "AFTER"
      *> An implementation that resumed after the whole TERMINATE
      *> statement would omit "RF-A". The fatal EC is handled by the
      *> declarative, so the run unit continues.
      *> EDITION: >>TURN, USE AFTER EXCEPTION CONDITION, RESUME and
      *> FUNCTION EXCEPTION-STATUS are COBOL-2002.
       >>TURN EC-REPORT-INACTIVE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C33B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTA ASSIGN TO "L1C33BA.RPT".
           SELECT RPTB ASSIGN TO "L1C33BB.RPT".
           SELECT RPTC ASSIGN TO "L1C33BC.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD RPTA REPORT IS R-A.
       FD RPTB REPORT IS R-B.
       FD RPTC REPORT IS R-C.
       REPORT SECTION.
       RD R-A.
       01 DA TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "DA".
       01 FA TYPE RF LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "RFA".
       RD R-B.
       01 DB TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "DB".
       01 FB TYPE RF LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "RFB".
       RD R-C.
       01 DC TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "DC".
       01 FC TYPE RF LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "RFC".
       PROCEDURE DIVISION.
       DECLARATIVES.
       UI SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-INACTIVE.
       UI-P.
           DISPLAY "INACTIVE " FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       UA SECTION.
           USE BEFORE REPORTING FA.
       UA-P.
           DISPLAY "RF-A".
       UB SECTION.
           USE BEFORE REPORTING FB.
       UB-P.
           DISPLAY "RF-B".
       UC SECTION.
           USE BEFORE REPORTING FC.
       UC-P.
           DISPLAY "RF-C".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT RPTA RPTB RPTC.
           INITIATE R-A R-C.
           GENERATE DA.
           GENERATE DC.
           TERMINATE R-C R-B R-A.
           DISPLAY "AFTER".
           CLOSE RPTA RPTB RPTC.
           STOP RUN.
