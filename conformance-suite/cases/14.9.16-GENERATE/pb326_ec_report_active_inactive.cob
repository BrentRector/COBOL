*>--- conformance-case
*> id: pb326_ec_report_active_inactive
*> rules: [GR-14.9.16.4-7, GR-14.9.21.4-2, GR-14.9.21.4-4, GR-14.9.46.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> The two report-state preconditions of the RWCS verbs, both catalogued names with no raise site before
      *> kb/Work PB326 (the same shape as EC-REPORT-FILE-MODE — one funnel now serves all four).
      *>
      *> §14.9.21.4 GR2: "An INITIATE statement shall not be executed if report-name-1 is in the active state.
      *> If it is in the active state, the EC-REPORT-ACTIVE exception condition is set to exist and the
      *> execution of the INITIATE statement has no other effect."
      *> §14.9.16.4 GR7: "The report associated with data-name-1 or report-name-1 shall be in the active state.
      *> If it is not, the EC-REPORT-INACTIVE exception condition is set to exist, if it is enabled."
      *> §14.9.46.4 GR1: "The TERMINATE statement may be executed only for a report that is in the active state.
      *> If the report is not in the active state, the EC-REPORT-INACTIVE exception condition is set to exist
      *> and the execution of the statement is unsuccessful."
      *>
      *> Both are Fatal in Table 13, so each declarative runs and RESUME AT NEXT STATEMENT (§14.9.33) continues.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT, statement by statement.  OPEN OUTPUT then INITIATE succeeds
      *> (§14.9.21.4 GR4 — the report becomes active).  The SECOND INITIATE hits GR2: EC-REPORT-ACTIVE, and "no
      *> other effect" — in particular GR1a-c do NOT re-run, so the sum counter keeps the 4 it accumulated and
      *> the second GENERATE's control footing prints S=008, not S=004.  GENERATE then TERMINATE run normally.
      *> The SECOND TERMINATE hits §14.9.46.4 GR1 (EC-REPORT-INACTIVE, unsuccessful) and the GENERATE after it
      *> hits §14.9.16.4 GR7 (EC-REPORT-INACTIVE) — neither adds a line.  Expected, in execution order:
      *> HANDLED-A, HANDLED-I, HANDLED-I, DONE, then the two detail lines and the one control footing.
      >>TURN EC-REPORT-ACTIVE EC-REPORT-INACTIVE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB326ACTINACT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb326-act.rpt".
           SELECT RBACK ASSIGN TO "pb326-act.rpt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-ACT.
       FD RBACK.
       01 RB-REC PIC X(30).
       WORKING-STORAGE SECTION.
       01 WS-G   PIC 9 VALUE 1.
       01 WS-N   PIC 9 VALUE 4.
       01 WS-EOF PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-ACT CONTROL IS WS-G
           PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
          02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       01 TYPE CF WS-G LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "S=".
          02 COLUMN 3 PIC 999 SUM WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HA SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-ACTIVE.
       HA-P.
           DISPLAY "HANDLED-A=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       HI SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-INACTIVE.
       HI-P.
           DISPLAY "HANDLED-I=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R-ACT.
           GENERATE DET-A.
           INITIATE R-ACT.
           GENERATE DET-A.
           TERMINATE R-ACT.
           TERMINATE R-ACT.
           GENERATE DET-A.
           CLOSE RPT.
           DISPLAY "DONE".
      *> The report content, blank lines skipped — the vertical placement of a report group is a separate
      *> question (kb/Work PB484) and this program deliberately does not depend on it.
       READ-BACK.
           OPEN INPUT RBACK.
           PERFORM UNTIL WS-EOF = 1
               READ RBACK
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END
                       IF RB-REC NOT = SPACES
                           DISPLAY RB-REC
                       END-IF
               END-READ
           END-PERFORM.
           CLOSE RBACK.
           STOP RUN.
