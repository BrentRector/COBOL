*>--- conformance-case
*> id: pb326_ec_flow_report
*> rules: [GR-14.9.49.4-10]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.49.4 GR10: "If a GENERATE, INITIATE, or TERMINATE statement is executed within the range of
      *> a declarative procedure whose USE statement contains the BEFORE REPORTING phrase, the EC-FLOW-REPORT
      *> exception condition is set to exist, the result of the execution of the GENERATE, INITIATE, or
      *> TERMINATE statement is unsuccessful, and the state of the report is unchanged."
      *>
      *> The CHECKED half (its unconditional twin, at the edition with no >>TURN at all, is
      *> 85/pb326_flow_report_unconditional).  Table 13 makes EC-FLOW-REPORT Fatal, so under
      *> >>TURN … CHECKING ON the format-3 declarative runs and RESUME AT NEXT STATEMENT (§14.9.33) returns to
      *> the statement after the nested GENERATE — i.e. back into the BEFORE REPORTING procedure, which then
      *> completes normally and the outer presentation proceeds.
      *>
      *> The program is legal: §14.9.49.3 SR10 forbids GENERATE/INITIATE/TERMINATE only "in a paragraph within a
      *> USE BEFORE REPORTING procedure", and GEN-P is a paragraph of a DIFFERENT declarative section, which SR4
      *> expressly lets BR-P reach with a PERFORM.
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  The single GENERATE presents DET-1; §14.9.49.4 GR8/GR9d run BR-P
      *> just before the group's LINE clauses are processed.  BR-P sets WS-N to 1 and PERFORMs GEN-P, whose
      *> GENERATE is inside the range: EC-FLOW-REPORT is raised (the declarative prints HANDLED=), the GENERATE
      *> is unsuccessful and the report's state is unchanged.  The outer presentation then composes DET-1 with
      *> WS-N = 1.  Expected, in execution order: HANDLED=EC-FLOW-REPORT, N=1, and EXACTLY ONE detail line D=1.
      >>TURN EC-FLOW-REPORT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB326FLOW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb326-flow.rpt".
           SELECT RBACK ASSIGN TO "pb326-flow.rpt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-FLOW.
       FD RBACK.
       01 RB-REC PIC X(30).
       WORKING-STORAGE SECTION.
       01 WS-N   PIC 9 VALUE 0.
       01 WS-EOF PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-FLOW PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 DET-1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
          02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       BR-SEC SECTION.
           USE BEFORE REPORTING DET-1.
       BR-P.
           ADD 1 TO WS-N.
           IF WS-N < 3
               PERFORM GEN-P
           END-IF.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-REPORT.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       GEN-P.
           GENERATE DET-1.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R-FLOW.
           GENERATE DET-1.
           TERMINATE R-FLOW.
           CLOSE RPT.
           DISPLAY "N=" WS-N.
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
