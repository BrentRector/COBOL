*>--- conformance-case
*> id: pb326_flow_report_cross_report
*> rules: [GR-14.9.49.4-1, GR-14.9.49.4-10]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.49.4 GR10 — THE SCOPE OF "the range of a declarative procedure".  GR10 attaches no element
      *> qualifier to the range, where its nearest neighbour §14.9.18.4 GR6 attaches one explicitly for
      *> EC-FLOW-GLOBAL-GOBACK ("… and that USE statement is specified in the same program as the GOBACK
      *> statement").  The standard says "the same program" when it means it, and it does not say "the same
      *> report" anywhere.  So a GENERATE executed inside report R-A's USE BEFORE REPORTING procedure is inside
      *> the range EVEN WHEN IT NAMES A DIFFERENT REPORT — which a per-report flag would miss.  This program is
      *> the arm that a report-scoped guard passes and a correct one does not (kb/Work PB326).
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT.  Both reports are OPENed and INITIATEd.  GENERATE DET-A presents
      *> DET-A, running BR-P first (§14.9.49.4 GR8/GR9d); BR-P PERFORMs GEN-B, whose GENERATE DET-B is inside
      *> the range: EC-FLOW-REPORT is raised (HANDLED=), the GENERATE is unsuccessful, and R-B's state is
      *> unchanged — so R-B has still had NO successful GENERATE.  TERMINATE R-B therefore takes §14.9.46.4 GR2
      *> ("no report group is processed at all") and R-B's file is still empty at that point.  Back in MAIN,
      *> OUTSIDE every declarative, WS-N is set to 7 and R-B is re-INITIATEd and GENERATEd normally — the proof
      *> that the range was LEFT (the guard is a bracket, not a latch).  Expected: HANDLED=EC-FLOW-REPORT,
      *> N=1 (BR-P ran exactly once), DONE, and R-B's file holding EXACTLY ONE line, B=7.
      >>TURN EC-FLOW-REPORT CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB326XREP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RA ASSIGN TO "pb326-xrep-a.rpt".
           SELECT RB ASSIGN TO "pb326-xrep-b.rpt".
           SELECT RBACK ASSIGN TO "pb326-xrep-b.rpt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD RA REPORT IS R-A.
       FD RB REPORT IS R-B.
       FD RBACK.
       01 RB-REC PIC X(30).
       WORKING-STORAGE SECTION.
       01 WS-N   PIC 9 VALUE 0.
       01 WS-EOF PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-A PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "A=".
          02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       RD R-B PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 DET-B TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "B=".
          02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       BR-SEC SECTION.
           USE BEFORE REPORTING DET-A.
       BR-P.
           ADD 1 TO WS-N.
           PERFORM GEN-B.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-FLOW-REPORT.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       GEN-B.
           GENERATE DET-B.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RA.
           OPEN OUTPUT RB.
           INITIATE R-A.
           INITIATE R-B.
           GENERATE DET-A.
           TERMINATE R-A.
           TERMINATE R-B.
           DISPLAY "N=" WS-N.
           MOVE 7 TO WS-N.
           INITIATE R-B.
           GENERATE DET-B.
           TERMINATE R-B.
           CLOSE RA.
           CLOSE RB.
           DISPLAY "DONE".
      *> R-B's content, blank lines skipped — the vertical placement of a report group is a separate question
      *> (kb/Work PB484) and this program deliberately does not depend on it.
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
