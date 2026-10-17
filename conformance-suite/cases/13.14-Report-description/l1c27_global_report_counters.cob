      *>--- conformance-case
      *> id: l1c27_global_report_counters
      *> rules: [GR-13.14.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.14.4 1) — a GLOBAL report's groups and counters are
      *> global
      *> "If GLOBAL is specified, report-name-1 and all its constituent
      *> report groups, its PAGE-COUNTER and LINE-COUNTER, and any sum
      *> counters defined in report-name-1 are global."
      *> cite.py --check 13.14.4 "If GLOBAL is specified, report-name-1
      *>   and all its constituent report groups, its PAGE-COUNTER and
      *>   LINE-COUNTER, and any sum counters defined in report-name-1
      *>   are global" -> OK §13.14.4 1)
      *> Supporting rules (each --check OK):
      *>   §8.4.6.2.1 "A global name may be referenced in the source
      *>     element in which it is declared or in any source elements
      *>     that are directly or indirectly contained within that
      *>     source element"
      *>   §8.4.3.15.3 1) "In the procedure division, PAGE-COUNTER and
      *>     LINE-COUNTER may be referenced in any context where an
      *>     integer data item may appear"
      *>   §13.18.35.4 5) b) 3. "if that report group is the first body
      *>     group to be printed on the current page, the line number is
      *>     given by the FIRST DETAIL integer; otherwise ... adding
      *>     integer-2 to the current value of the report's
      *>     LINE-COUNTER"
      *>   §13.18.35.4 6) "the report's LINE-COUNTER is set equal to
      *>     that line number"
      *>   §13.18.54.4 2) "The sum counter is set to zero ... when the
      *>     INITIATE statement"; 5) "the data-name is the name of the
      *>     sum counter"; 7) c) 1. "if no UPON phrase is specified,
      *>     whenever any GENERATE statement is executed for the current
      *>     report or any detail defined for the current report"; 12)
      *>     procedure division statements may alter the content of sum
      *>     counters.
      *>   §14.9.16.3 3) "the file description entry associated with
      *>     that report description entry shall contain a GLOBAL
      *>     clause" (why the FD is GLOBAL too: L1C27C GENERATEs a
      *>     containing program's detail)
      *>
      *> DERIVATION (FIRST DETAIL 3; DET-1 is LINE PLUS 1; TOT-N sums
      *> WS-N at every GENERATE):
      *>   A: INITIATE -> PC=1, LC=0, TOT-N=0. WS-N=5, GENERATE DET-1:
      *>      first body group on the page -> line 3, LC=3; TOT-N=5
      *>                                     -> "A1 PC=01 LC=03 SUM=005"
      *>   C (contained; the names reach it only through GLOBAL):
      *>      WS-N=7, GENERATE DET-1 -> LC=4, TOT-N=12;
      *>      GENERATE DET-1 OF R-1 -> LC=5, TOT-N=19; PAGE-COUNTER is 1
      *>                                     -> "C  PC=01 LC=05 SUM=019"
      *>      MOVE 100 TO TOT-N (the containing program's sum counter).
      *>   A: LINE-COUNTER and TOT-N are the SAME items C used
      *>                                     -> "A2 PC=01 LC=05 SUM=100"
      *>      GENERATE DET-1 (WS-N still 7) -> LC=6, TOT-N=107
      *>                                     -> "A3 PC=01 LC=06 SUM=107"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C27B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R-FL ASSIGN TO "L1C27B.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD R-FL GLOBAL REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 WS-N   PIC 9   VALUE 0 GLOBAL.
       01 WS-PC  PIC 99  VALUE 0 GLOBAL.
       01 WS-LC  PIC 99  VALUE 0 GLOBAL.
       01 WS-T   PIC 999 VALUE 0 GLOBAL.
       REPORT SECTION.
       RD R-1 IS GLOBAL CONTROL IS FINAL
           PAGE LIMIT IS 30 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 25.
       01 DET-1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE IS WS-N.
       01 TOT TYPE CF FINAL LINE PLUS 1.
          02 TOT-N COLUMN 1 PIC 999 SUM WS-N.
       PROCEDURE DIVISION.
       A-1.
           OPEN OUTPUT R-FL.
           INITIATE R-1.
           MOVE 5 TO WS-N.
           GENERATE DET-1.
           MOVE PAGE-COUNTER TO WS-PC.
           MOVE LINE-COUNTER TO WS-LC.
           MOVE TOT-N TO WS-T.
           DISPLAY "A1 PC=" WS-PC " LC=" WS-LC " SUM=" WS-T.
           CALL "L1C27C".
           MOVE PAGE-COUNTER TO WS-PC.
           MOVE LINE-COUNTER TO WS-LC.
           MOVE TOT-N TO WS-T.
           DISPLAY "A2 PC=" WS-PC " LC=" WS-LC " SUM=" WS-T.
           GENERATE DET-1.
           MOVE PAGE-COUNTER TO WS-PC.
           MOVE LINE-COUNTER TO WS-LC.
           MOVE TOT-N TO WS-T.
           DISPLAY "A3 PC=" WS-PC " LC=" WS-LC " SUM=" WS-T.
           TERMINATE R-1.
           CLOSE R-FL.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C27C.
       PROCEDURE DIVISION.
       C-1.
           MOVE 7 TO WS-N.
           GENERATE DET-1.
           GENERATE DET-1 OF R-1.
           MOVE PAGE-COUNTER TO WS-PC.
           MOVE LINE-COUNTER OF R-1 TO WS-LC.
           MOVE TOT-N TO WS-T.
           DISPLAY "C  PC=" WS-PC " LC=" WS-LC " SUM=" WS-T.
           MOVE 100 TO TOT-N.
           EXIT PROGRAM.
       END PROGRAM L1C27C.
       END PROGRAM L1C27B.
