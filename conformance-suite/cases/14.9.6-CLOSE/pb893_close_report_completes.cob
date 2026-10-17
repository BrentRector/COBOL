      *>--- conformance-case
      *> id: pb893_close_report_completes
      *> rules: [GR-14.9.6.4-5, GR-14.9.6.4-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
       >>TURN EC-REPORT-NOT-TERMINATED CHECKING ON
      *> kb/Work PB893 - ISO 14.9.6.4 GR5: "If any report is in the
      *> active state, the CLOSE statement for that file is completed
      *> and the EC-REPORT-NOT-TERMINATED exception condition is set to
      *> exist." GR4 updates the I-O status; GR10 makes CLOSE RPT OTH
      *> two implicit CLOSE statements, and a RESUME NEXT STATEMENT
      *> "resumes at the next implicit CLOSE statement, if any".
      *> 1st CLOSE: RPT is closed (status 00) BEFORE the declarative
      *>   runs; RESUME AT P2 (14.9.33.4 GR3, as if GO TO P2) skips the
      *>   implicit CLOSE OTH, so OTH stays open - but RPT is closed,
      *>   and re-opening it succeeds (00). Before the fix the raise
      *>   preceded the close, the transfer abandoned it, and the
      *>   re-OPEN answered 41 (already open).
      *> 2nd CLOSE: the declarative sees RPT's updated status 00 and
      *>   RESUME AT NEXT STATEMENT runs the next implicit CLOSE (OTH).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB893CR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb893cr.rpt"
               FILE STATUS IS WS-ST.
           SELECT OTH ASSIGN TO "pb893cr.dat"
               FILE STATUS IS WS-OS.
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       FD OTH.
       01 OTH-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       01 WS-OS PIC XX.
       01 WS-SRC PIC 99 VALUE 7.
       01 N PIC 9 VALUE 0.
       REPORT SECTION.
       RD R-1
           PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 2.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D SECTION.
           USE AFTER EXCEPTION CONDITION EC-REPORT-NOT-TERMINATED.
       D-P.
           ADD 1 TO N
           DISPLAY "DECL " N " ST=" WS-ST " OS=" WS-OS
           IF N = 1
              RESUME AT P2
           END-IF
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           OPEN OUTPUT RPT OTH
           INITIATE R-1
           CLOSE RPT OTH
           DISPLAY "NOT REACHED".
       P2.
           DISPLAY "P2 ST=" WS-ST " OS=" WS-OS
           OPEN OUTPUT RPT
           DISPLAY "REOPEN RPT=" WS-ST
           CLOSE RPT OTH
           DISPLAY "END ST=" WS-ST " OS=" WS-OS
           STOP RUN.
