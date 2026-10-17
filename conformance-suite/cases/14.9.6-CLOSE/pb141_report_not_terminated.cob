*>--- conformance-case
*> id: pb141_report_not_terminated
*> rules: [GR-14.9.6.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       >>TURN EC-REPORT-NOT-TERMINATED CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB141RT.
      *> kb/Work PB141 - ISO 14.9.6.4 GR5: closing a report file while an
      *> associated report is INITIATEd and not TERMINATEd completes the
      *> CLOSE and sets EC-REPORT-NOT-TERMINATED to exist (nonfatal). The
      *> EC was catalogued and never raised anywhere.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb141rt.rpt"
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       01 WS-SRC PIC 99 VALUE 7.
       REPORT SECTION.
       RD R-1
           PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 2.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           CLOSE RPT
           DISPLAY "CLOSE=" WS-ST " ES=" FUNCTION EXCEPTION-STATUS
           STOP RUN.
