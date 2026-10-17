*>--- conformance-case
*> id: pb326_ec_report_file_mode
*> rules: [GR-14.9.21.4-3, SR-14.9.27.3-1, GR-14.9.27.4-7, GR-14.9.46.4-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.21.4 GR3: "The INITIATE statement does not open any file connector with which report-name-1
      *> is associated. Therefore, the INITIATE statement may be executed only if the corresponding file
      *> connector is open in the extend mode or the output mode. If the file connector is not open in the
      *> output or extend mode, the EC-REPORT-FILE-MODE exception condition is set to exist and no action is
      *> taken on the report."  That is the DETECTION half of §14.9.27.4 GR7 ("The OPEN statement for a report
      *> file connector shall be executed before the execution of an INITIATE statement that references a
      *> report-name that is associated with file-name-1"); the other half — that nothing opens a report file
      *> connector implicitly — holds by construction.  Table 13 makes EC-REPORT-FILE-MODE Fatal, so under
      *> >>TURN … CHECKING ON the declarative runs and RESUME AT NEXT STATEMENT (§14.9.33) keeps the run alive.
      *>
      *> TWO ARMS, because the rule turns on the pair (is open, in which mode) and not on either alone:
      *>   1. NOT OPEN AT ALL          -> EC-REPORT-FILE-MODE.  "No action is taken on the report", so §14.9.21.4
      *>      GR4 never places it in the active state, and the TERMINATE that follows is therefore itself
      *>      unsuccessful with EC-REPORT-INACTIVE (§14.9.46.4 GR1) — the witness that GR3's "no action" held.
      *>   3. OPEN OUTPUT, then OPEN EXTEND -> both legal; GR3 names the two modes and this pins BOTH.
      *> (The former ARM 2 opened this report file INPUT, which §14.9.27.3 SR1 forbids — "The OPEN statement
      *> for a report file shall not contain the INPUT phrase or the I-O phrase" — and which now draws
      *> COBOLNET2371 at compile time (kb/Work PB318). It was never conforming source, so it is gone.)
      *> Before kb/Work PB326 the condition was a catalogue row with no raise site anywhere in src: every arm
      *> here INITIATEd silently and each GENERATE wrote into a connector that was not open.
      >>TURN EC-REPORT-FILE-MODE EC-REPORT-INACTIVE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB326FMODE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb326-file-mode.rpt".
           SELECT RBACK ASSIGN TO "pb326-file-mode.rpt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-FM.
       FD RBACK.
       01 RB-REC PIC X(30).
       WORKING-STORAGE SECTION.
       01 WS-N   PIC 9 VALUE 4.
       01 WS-EOF PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-FM PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 DET-FM TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) VALUE "D=".
          02 COLUMN 3 PIC 9 SOURCE IS WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       HF SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-FILE-MODE.
       HF-P.
           DISPLAY "HANDLED-F=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       HI SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-INACTIVE.
       HI-P.
           DISPLAY "HANDLED-I=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
      *> ARM 1 — the connector has never been opened.
       ARM-1.
           INITIATE R-FM.
           TERMINATE R-FM.
      *> ARM 3 — the two modes GR3 does permit.
       ARM-3.
           OPEN OUTPUT RPT.
           INITIATE R-FM.
           GENERATE DET-FM.
           TERMINATE R-FM.
           CLOSE RPT.
           OPEN EXTEND RPT.
           MOVE 5 TO WS-N.
           INITIATE R-FM.
           GENERATE DET-FM.
           TERMINATE R-FM.
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
