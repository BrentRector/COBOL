*>--- conformance-case
*> id: rw_suppress
*> rules: [SR-13.18.16.3-1, GR-13.18.16.4-3, GR-13.18.16.4-5, SR-13.18.57.3-11, FMT-14.9.45.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.45 SUPPRESS statement. A USE BEFORE REPORTING procedure
      *> (§14.9.49 Format 2) on the detail group executes SUPPRESS PRINTING
      *> for the rows flagged hidden (WS-HIDE = 1). Per §14.9.45.4 GR3 the
      *> statement inhibits ONLY: (a) printing the group's lines, (b) any
      *> page advance, (c) NEXT GROUP, (d) LINE-COUNTER changes. It does
      *> NOT inhibit sum-counter accumulation (§13.18.54.4 GR7) nor the
      *> end-of-group sum reset (GR2) — only PRESENT WHEN / OCCURS DEPENDING
      *> absence skips the reset (GR10), which this program does not use.
      *> Proof: the amount of the SUPPRESSED middle row (20) never prints as
      *> a detail line, yet it STILL rolls into the group-1 control total, so
      *> the control footing prints TOTAL=060 (= 10 + 20 + 30), not 040.
      *> GR2 (current instance only) is exercised too: SUPPRESS is executed
      *> conditionally per GENERATE, so the surrounding shown rows print.
      *> PLACEMENT (kb/Work PB484). The report's FIRST printed line is the first line of the file:
      *> a line number IS a page line number (ISO 13.18.35.4 GR6 - "the report's LINE-COUNTER is set
      *> equal to that line number and the line is now printed on the page at that vertical location"),
      *> and GR7's "Any unoccupied lines on the page result in a blank line" fixes the blanks above it.
      *> Here the first body group is a control heading, the chronologically first body group on the
      *> page, so GR5 b) 3 gives it the FIRST DETAIL integer, 3: lines 1 and 2 are unoccupied and blank,
      *> GROUP-1 prints on line 3, and every later relative line follows at LINE-COUNTER + 1 (GR7 b)).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWSUPPRESS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "rw-suppress.rpt".
           SELECT RBACK ASSIGN TO "rw-suppress.rpt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-SUP.
       FD RBACK.
       01 RB-REC PIC X(30).
       WORKING-STORAGE SECTION.
       01 WS-GRP  PIC 9  VALUE 0.
       01 WS-AMT  PIC 99 VALUE 0.
       01 WS-HIDE PIC 9  VALUE 0.
       01 WS-EOF  PIC 9  VALUE 0.
       REPORT SECTION.
       RD R-SUP CONTROL IS WS-GRP
           PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01 TYPE CH LINE PLUS 1.
          02 COLUMN 1 PIC X(6) VALUE "GROUP-".
          02 COLUMN 7 PIC 9 SOURCE IS WS-GRP.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(4) VALUE "AMT=".
          02 COLUMN 5 PIC 99 SOURCE IS WS-AMT.
       01 TYPE CF WS-GRP LINE PLUS 1.
          02 COLUMN 1 PIC X(6) VALUE "TOTAL=".
          02 COLUMN 7 PIC 999 SUM WS-AMT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SUP-SECTION SECTION.
           USE BEFORE REPORTING DET-A.
       SUP-PARA.
           IF WS-HIDE = 1
               SUPPRESS PRINTING
           END-IF.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R-SUP.
           MOVE 1 TO WS-GRP.
           MOVE 10 TO WS-AMT. MOVE 0 TO WS-HIDE. GENERATE DET-A.
           MOVE 20 TO WS-AMT. MOVE 1 TO WS-HIDE. GENERATE DET-A.
           MOVE 30 TO WS-AMT. MOVE 0 TO WS-HIDE. GENERATE DET-A.
           MOVE 2 TO WS-GRP.
           MOVE 5 TO WS-AMT.  MOVE 0 TO WS-HIDE. GENERATE DET-A.
           TERMINATE R-SUP.
           CLOSE RPT.
           OPEN INPUT RBACK.
           PERFORM UNTIL WS-EOF = 1
               READ RBACK
                   AT END MOVE 1 TO WS-EOF
                   NOT AT END DISPLAY "L=" RB-REC
               END-READ
           END-PERFORM.
           CLOSE RBACK.
           STOP RUN.
