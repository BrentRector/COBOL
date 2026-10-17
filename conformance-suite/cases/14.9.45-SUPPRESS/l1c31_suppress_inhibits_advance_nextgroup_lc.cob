      *>--- conformance-case
      *> id: l1c31_suppress_inhibits_advance_nextgroup_lc
      *> rules: [GR-14.9.45.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.45.4 3) b)-e) — SUPPRESS inhibits page advance, NEXT
      *> GROUP and LINE-COUNTER changes, but not control-break
      *>    processing.
      *> Rule 3): "When the SUPPRESS statement is executed, the
      *>    following
      *> report group functions are inhibited: ... b) Any page advance
      *> associated with the report group. c) The processing of any NEXT
      *> GROUP clause in the report group. d) Any changes to
      *>    LINE-COUNTER
      *> associated with the report group. e) If the associated report
      *> group is a detail, the SUPPRESS statement does not affect the
      *> sensing for control breaks or the subsequent control break
      *> processing."  ((a), no printing, is
      *>    conformance:2023/rw_suppress)
      *> cite.py --check 14.9.45.4 "Any page advance associated with the
      *>   report group."           -> OK  §14.9.45.4 3) b)
      *> cite.py --check 14.9.45.4 "The processing of any NEXT GROUP
      *>   clause in the report group." -> OK  §14.9.45.4 3) c)
      *> cite.py --check 14.9.45.4 "Any changes to LINE-COUNTER
      *>   associated with the report group." -> OK  §14.9.45.4 3) d)
      *> cite.py --check 14.9.45.4 "the SUPPRESS statement does not
      *>   affect the sensing for control breaks" -> OK  §14.9.45.4 3)
      *>    e)
      *> Layout rules used (all §13.18 / §14.9.16, 2023 text):
      *>  PAGE LIMIT 12 HEADING 1 FIRST DETAIL 2 LAST DETAIL 7 FOOTING
      *>    9;
      *>  LAST CONTROL HEADING omitted = LAST DETAIL 7 (13.18.39.4
      *>    GR3c).
      *>  Lower limits: CH 7, DE 7, CF 9 (13.18.57 GR8 d/e/f).
      *>  Relative body group: fit test trial = LINE-COUNTER + 1,
      *>    success
      *>  iff <= lower limit (13.18.35.4 GR4c); line = FIRST DETAIL if
      *>    the
      *>  first body group on the page, else LINE-COUNTER + 1 (GR5b3).
      *>  DE NEXT GROUP PLUS 1: LC+1 if < FOOTING 9 (13.18.37.4 GR4b).
      *>  Page advance: PAGE-COUNTER + 1, LINE-COUNTER 0 (14.9.16.4
      *>    GR6).
      *> USE BEFORE REPORTING DET-A suppresses when WS-HIDE = 1; the CH
      *> and CF declaratives DISPLAY when those groups are produced.
      *> Main DISPLAYs LINE-COUNTER/PAGE-COUNTER after each GENERATE.
      *> Derivation (INITIATE: LC 0, PC 1):
      *>  G1 grp 1 shown: CH first body group -> line 2 ("CH"); DE fit
      *>     3<=7, line 3, NEXT GROUP -> LC 4.          G1 LC=04 PC=01
      *>  G2 SUPPRESSED: no LC change (d), no NEXT GROUP (c) -> LC 4
      *>     (printing it would give 5, +NEXT GROUP 6). G2 LC=04 PC=01
      *>  G3 shown: fit 5<=7, line 5, NG -> 6.          G3 LC=06 PC=01
      *>  G4 shown: fit 7<=7, line 7, NG -> 8.          G4 LC=08 PC=01
      *>  G5 SUPPRESSED: its fit test (9 > 7) would advance the page;
      *>     (b) inhibits it: PC stays 1, LC stays 8.   G5 LC=08 PC=01
      *>  G6 grp 2, SUPPRESSED detail: the break is still sensed and
      *>     processed (e): CF grp 1 declarative sees LC 8 ("CF LC=08"),
      *>     CF fit 9<=9 prints on line 9; CH grp 2 ("CH") fit 10 > 7 ->
      *>     page advance PC 2, LC 0, CH first body group -> line 2; the
      *>     suppressed DE leaves LC 2.                 G6 LC=02 PC=02
      *>  TERMINATE: CF grp 2 declarative sees LC 2 ("CF LC=02").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C31D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "L1C31D.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-SUP.
       WORKING-STORAGE SECTION.
       01 WS-GRP  PIC 9  VALUE 0.
       01 WS-AMT  PIC 99 VALUE 0.
       01 WS-HIDE PIC 9  VALUE 0.
       01 W-LC    PIC 99.
       01 W-PC    PIC 99.
       01 W-TAG   PIC XX.
       REPORT SECTION.
       RD R-SUP CONTROL IS WS-GRP
           PAGE LIMIT IS 12 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 7 FOOTING 9.
       01 CH-G TYPE CH WS-GRP LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "CH ".
          02 COLUMN 4 PIC 9 SOURCE IS WS-GRP.
       01 DET-A TYPE DE LINE PLUS 1 NEXT GROUP PLUS 1.
          02 COLUMN 1 PIC X(4) VALUE "AMT=".
          02 COLUMN 5 PIC 99 SOURCE IS WS-AMT.
       01 CF-G TYPE CF WS-GRP LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "CF ".
          02 COLUMN 4 PIC 9 SOURCE IS WS-GRP.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SUP-SECTION SECTION.
           USE BEFORE REPORTING DET-A.
       SUP-PARA.
           IF WS-HIDE = 1
               SUPPRESS PRINTING
           END-IF.
       CH-SECTION SECTION.
           USE BEFORE REPORTING CH-G.
       CH-PARA.
           DISPLAY "CH".
       CF-SECTION SECTION.
           USE BEFORE REPORTING CF-G.
       CF-PARA.
           MOVE LINE-COUNTER TO W-LC.
           DISPLAY "CF LC=" W-LC.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R-SUP.
           MOVE 1 TO WS-GRP.
           MOVE 1 TO WS-AMT. MOVE 0 TO WS-HIDE. MOVE "G1" TO W-TAG.
           PERFORM GEN-SHOW.
           MOVE 2 TO WS-AMT. MOVE 1 TO WS-HIDE. MOVE "G2" TO W-TAG.
           PERFORM GEN-SHOW.
           MOVE 3 TO WS-AMT. MOVE 0 TO WS-HIDE. MOVE "G3" TO W-TAG.
           PERFORM GEN-SHOW.
           MOVE 4 TO WS-AMT. MOVE 0 TO WS-HIDE. MOVE "G4" TO W-TAG.
           PERFORM GEN-SHOW.
           MOVE 5 TO WS-AMT. MOVE 1 TO WS-HIDE. MOVE "G5" TO W-TAG.
           PERFORM GEN-SHOW.
           MOVE 2 TO WS-GRP.
           MOVE 6 TO WS-AMT. MOVE 1 TO WS-HIDE. MOVE "G6" TO W-TAG.
           PERFORM GEN-SHOW.
           TERMINATE R-SUP.
           CLOSE RPT.
           DISPLAY "END".
           STOP RUN.
       GEN-SHOW.
           GENERATE DET-A.
           MOVE LINE-COUNTER TO W-LC.
           MOVE PAGE-COUNTER TO W-PC.
           DISPLAY W-TAG " LC=" W-LC " PC=" W-PC.
