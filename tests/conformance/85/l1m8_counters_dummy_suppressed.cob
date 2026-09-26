      *> ISO §8.4.3.15.4 5) — PAGE-COUNTER/LINE-COUNTER unaffected by a
      *> dummy report group and by a SUPPRESSed report group
      *> GR5: "The values of PAGE-COUNTER and LINE-COUNTER are not
      *>   affected by the processing of a dummy report group, nor by the
      *>   processing of a report group whose printing is suppressed by
      *>   means of the SUPPRESS statement."
      *>   cite.py: OK  §8.4.3.15.4 5)  (General rules)
      *> DUMMY. The standard uses the term (8.4.3.15.4 5); 13.18.37.4
      *>   4) a) "just before any other non-dummy body group is printed"
      *>   -> OK §13.18.37.4 4) a)) without a glossary entry. A body group
      *>   with no LINE clause is one: "Each report group is divided
      *>   vertically into zero, one, or more lines" (OK §13.8.6.2.2),
      *>   "the set of report lines, if any" (OK §13.18.35.3 6)), and
      *>   "Each LINE clause in a given report group description defines
      *>   a report line" (OK §13.18.35.4 1)). DUM below has no LINE
      *>   clause and one unprintable SOURCE item, so it has zero lines.
      *> SUPPRESS. The declarative runs "Before any page fit processing"
      *>   (OK §14.9.49.4 9) c)); SUPPRESS then inhibits "Any page
      *>   advance associated with the report group" (OK §14.9.45.4 3)
      *>   b)), "The processing of any NEXT GROUP clause" (OK 3) c)) and
      *>   "Any changes to LINE-COUNTER" (OK 3) d)).
      *> Layout rules (all OK under cite.py --check):
      *>   INITIATE: PC 1 (OK §8.4.3.15.4 2)), LC 0 (OK §8.4.3.15.4 3)).
      *>   DE lower limit = LAST DETAIL 7 (OK §13.18.57.4 8) e)).
      *>   Relative group fit: trial = LC + 1, success iff <= 7
      *>     (OK §13.18.35.4 4)); line = FIRST DETAIL 2 if first body
      *>     group on the page (OK §13.18.35.4 5) b) 3.), else LC + 1.
      *>   Page advance: PC + 1 (OK §14.9.16.4 6) d)), LC 0 (6) e)).
      *>   NEXT GROUP PLUS 9: LC + 9 if < FOOTING 9, else LC := 9
      *>     (§13.18.37.4 4) b); cite.py labels it "4) a) 3." - the
      *>     PB1554 labelling slip).
      *> DERIVATION (C = what an unsuppressed / printed group would do):
      *>   S0  after INITIATE                      LC=00 PC=01
      *>   S1  DET-A, first body group -> line 2   LC=02 PC=01
      *>   S2  DUM                                 LC=02 PC=01
      *>   S3  DET-S suppressed (C: line 3, NG+1 -> LC 4)
      *>                                           LC=02 PC=01
      *>   S4  DET-A x5 -> lines 3,4,5,6,7         LC=07 PC=01
      *>   S5  DUM (C: a printed line would need 8 > 7, advance)
      *>                                           LC=07 PC=01
      *>   S6  DET-S suppressed (C: fit 8 > 7 -> page advance)
      *>                                           LC=07 PC=01
      *>   S7  DET-A: fit 8 > 7 -> advance, line 2 LC=02 PC=02
      *>   S8  DET-N: fit 3 <= 7, line 3; NG 3+9=12 not < 9 -> LC 9
      *>                                           LC=09 PC=02
      *>   S9  DUM - the discriminating leg: any page fit run for the
      *>       dummy (trial >= 9 > 7) would advance to PC 3
      *>                                           LC=09 PC=02
      *>   S10 DET-S suppressed (C: fit 10 > 7 -> advance)
      *>                                           LC=09 PC=02
      *>   S11 DET-A: fit 10 > 7 -> advance, line 2 LC=02 PC=03
      *> S2/S5/S9 pin the dummy half; S3/S6/S10 the SUPPRESS half; S7,
      *> S8 and S11 are the printed controls that do move the counters.
      *> Edition: every construct here (TYPE DE, LINE PLUS, NEXT GROUP
      *>   PLUS, the PAGE LIMIT phrases, USE BEFORE REPORTING, SUPPRESS
      *>   PRINTING, LINE-COUNTER/PAGE-COUNTER as senders) is COBOL-85
      *>   Report Writer, so the golden lives in the 85 directory and
      *>   the introducing edition is the one the corpus runner compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1M8RC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "l1m8rc.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-CNT.
       WORKING-STORAGE SECTION.
       01 WS-AMT  PIC 99 VALUE 0.
       01 W-LC    PIC 99.
       01 W-PC    PIC 99.
       01 W-TAG   PIC X(3).
       01 I       PIC 9.
       REPORT SECTION.
       RD R-CNT
           PAGE LIMIT IS 12 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 7 FOOTING 9.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(4) VALUE "AMT=".
          02 COLUMN 5 PIC 99 SOURCE IS WS-AMT.
       01 DET-S TYPE DE LINE PLUS 1 NEXT GROUP PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "SUP".
       01 DET-N TYPE DE LINE PLUS 1 NEXT GROUP PLUS 9.
          02 COLUMN 1 PIC X(3) VALUE "NGR".
       01 DUM TYPE DE.
          02 PIC 99 SOURCE IS WS-AMT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SUP-SECTION SECTION.
           USE BEFORE REPORTING DET-S.
       SUP-PARA.
           SUPPRESS PRINTING.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-PARA.
           OPEN OUTPUT RPT.
           INITIATE R-CNT.
           MOVE "S0" TO W-TAG. PERFORM SHOW.
           MOVE 1 TO WS-AMT. GENERATE DET-A.
           MOVE "S1" TO W-TAG. PERFORM SHOW.
           MOVE 2 TO WS-AMT. GENERATE DUM.
           MOVE "S2" TO W-TAG. PERFORM SHOW.
           GENERATE DET-S.
           MOVE "S3" TO W-TAG. PERFORM SHOW.
           PERFORM VARYING I FROM 3 BY 1 UNTIL I > 7
               MOVE I TO WS-AMT
               GENERATE DET-A
           END-PERFORM.
           MOVE "S4" TO W-TAG. PERFORM SHOW.
           MOVE 8 TO WS-AMT. GENERATE DUM.
           MOVE "S5" TO W-TAG. PERFORM SHOW.
           GENERATE DET-S.
           MOVE "S6" TO W-TAG. PERFORM SHOW.
           MOVE 9 TO WS-AMT. GENERATE DET-A.
           MOVE "S7" TO W-TAG. PERFORM SHOW.
           GENERATE DET-N.
           MOVE "S8" TO W-TAG. PERFORM SHOW.
           MOVE 10 TO WS-AMT. GENERATE DUM.
           MOVE "S9" TO W-TAG. PERFORM SHOW.
           GENERATE DET-S.
           MOVE "S10" TO W-TAG. PERFORM SHOW.
           MOVE 11 TO WS-AMT. GENERATE DET-A.
           MOVE "S11" TO W-TAG. PERFORM SHOW.
           TERMINATE R-CNT.
           CLOSE RPT.
           DISPLAY "END".
           STOP RUN.
       SHOW.
           MOVE LINE-COUNTER TO W-LC.
           MOVE PAGE-COUNTER TO W-PC.
           DISPLAY W-TAG " LC=" W-LC " PC=" W-PC.
