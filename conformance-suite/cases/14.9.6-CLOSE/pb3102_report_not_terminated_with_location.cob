*>--- conformance-case
*> id: pb3102_report_not_terminated_with_location
*> rules: [GR-14.9.6.4-5]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3102 - ISO 14.9.6.4 GR5: closing a report file while an associated report is INITIATEd and not
      *> TERMINATEd completes the CLOSE and sets EC-REPORT-NOT-TERMINATED to exist (nonfatal). With the LOCATION
      *> option on the TURN directive, 7.3.25.4 GR7 makes "all information necessary to identify a source statement"
      *> available, so 15.32.3 rule 2 returns "the name of the statement that caused the exception condition" (Table 12's
      *> 'Statement name' column - CLOSE), 63 characters left-justified and space-filled, and 15.30.3 rule 2 returns
      *> the location "element-name; paragraph-name; line-identifier".
      *>
      *> THE DEFECT: the CLOSE statement never named EC-REPORT-NOT-TERMINATED among the exception-names it can raise (the
      *> SequentialIoBinder decided the check on its own), so the statement carried no ambient (statement, location)
      *> context and both functions answered spaces. The condition itself WAS raised (the pb141 golden), which is why
      *> the omission was invisible without WITH LOCATION.
       >>TURN EC-REPORT-NOT-TERMINATED CHECKING ON WITH LOCATION
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3102RT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb3102rt.rpt"
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
           DISPLAY "CLOSE=" WS-ST
           DISPLAY "ES=[" FUNCTION EXCEPTION-STATUS "]"
           DISPLAY "STMT=[" FUNCTION EXCEPTION-STATEMENT "]"
           DISPLAY "LOC=[" FUNCTION EXCEPTION-LOCATION "]"
           STOP RUN.
