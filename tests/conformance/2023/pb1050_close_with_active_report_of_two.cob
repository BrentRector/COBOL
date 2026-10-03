       >>TURN EC-REPORT-NOT-TERMINATED CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1050CL.
      *> kb/Work PB1050 - several reports on ONE file (ISO 13.18.46), the CLOSE half.
      *> 13.18.46.4 GR2: "After execution of an INITIATE statement and before the execution of a TERMINATE
      *> statement for the same report, no OPEN or CLOSE statements shall be executed that reference the report
      *> file." 14.9.6.4 GR5: "No report associated with a report file that is referenced in the CLOSE statement
      *> shall be in the active state. If any report is in the active state, the CLOSE statement for that file is
      *> completed and the EC-REPORT-NOT-TERMINATED exception condition is set to exist." (both cite.py OK)
      *> DERIVATION. R-A and R-B share file RPT. Both are INITIATEd; only R-A is TERMINATEd. R-B is still in the
      *> active state when CLOSE RPT runs, so the CLOSE completes (status 00) and EC-REPORT-NOT-TERMINATED is set
      *> although R-A, the first report the FD names, is inactive: "any report" associated with the file.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb1050cl.rpt"
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORTS ARE R-A R-B.
       WORKING-STORAGE SECTION.
       01 WS-ST PIC XX.
       01 WS-SRC PIC 99 VALUE 7.
       REPORT SECTION.
       RD R-A CODE IS "A".
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       RD R-B CODE IS "B".
       01 DET-B TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 99 SOURCE IS WS-SRC.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-A
           INITIATE R-B
           GENERATE DET-A
           TERMINATE R-A
           CLOSE RPT
           DISPLAY "CLOSE=" WS-ST " ES=" FUNCTION EXCEPTION-STATUS
           STOP RUN.
