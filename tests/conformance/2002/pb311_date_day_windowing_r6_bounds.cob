      *> ISO §15.23.3 r6 / §15.25.3 r6 — the SUM WINDOW of the two composite windowing functions, with an
      *> EXPLICIT argument-3 chosen FAR FROM THE EXECUTION YEAR on both bounds (kb/Work PB311).
      *>   §15.23.3 r6  "The sum of the year at the time of execution and the value of argument-2 shall be
      *>                 less than 10000 and greater than 1699."
      *>   §15.25.3 r6  "The sum of the values of argument-2 and argument-3 shall be less than 10000 and
      *>                 greater than 1699."
      *>   §15.23.1     "Argument-3 specifies the year at the time of execution."
      *> (cite.py --check on each -> OK; the DATE sentence at rule 6 of 15.23.3, the DAY one at rule 6 of
      *> 15.25.3, the General sentence at 15.23.1.)
      *>
      *> THE TWO WORDINGS ARE ONE RULE. §15.23.3 r6 is worded on "the year at the time of execution", but the
      *> same function's §15.23.1 DEFINES that year as argument-3 (when argument-3 is omitted r5 supplies the
      *> real execution year, so the two coincide). So the DATE sum is argument-2 + argument-3, exactly
      *> §15.25.3 r6's, and §15.23.4 r1 delegates the arithmetic to FUNCTION YEAR-TO-YYYY (YY, argument-2,
      *> argument-3), whose own §15.100.3 r6 is the same sum. A reading that keyed the DATE window on the
      *> machine clock instead would make an explicit argument-3 irrelevant to r6 while §15.23.4 r1 still
      *> hands it to the core; the probes below separate the two readings by writing argument-3 where the
      *> clock year cannot reach: the legal highs use argument-3 = 1700 with argument-2 near 8300 (clock
      *> year + argument-2 would be > 10000), the legal lows use argument-3 = 2100 with argument-2 = -400
      *> (clock year - 400 would be < 1700 until the year 2100), and one violation has a clock-legal
      *> argument-2 against an out-of-window argument-3 sum.
      *> Before this fixture the only r6-shaped line in the corpus was pb65_date_windowing's
      *> DAY-TO-YYYYDDD(85365, 9000, 1995), whose sum of 10995 is far outside the window; it cannot tell a
      *> bound at 10000 from one at 20000, and DATE-TO-YYYYMMDD had no r6 witness at all.
      *>
      *> Derivations (§15.100.4: maximum-year = argument-2 + argument-3; r2a when MOD(max, 100) >= YY,
      *> YY + 100 * INTEGER(max / 100); r2b otherwise, YY + 100 * (INTEGER(max / 100) - 1)); YY = 85, so
      *> mmdd = 1003 and nnn = 365 (§15.23.4 r1 / §15.25.4 r1):
      *>   LOW1700   (-400, 2100) max 1700 > 1699 LEGAL; MOD 0 < 85 -> r2b -> 85 + 100*16 = 1685
      *>             -> 1685*10000 + 1003 = 16851003; DAY 1685*1000 + 365 = 1685365.
      *>   LOW1699   (-401, 2100) max 1699 is NOT greater than 1699 -> EC-ARGUMENT-FUNCTION.
      *>   HIGH9999  (8299, 1700) max 9999 < 10000 LEGAL; MOD 99 >= 85 -> r2a -> 85 + 100*99 = 9985
      *>             -> 99851003; DAY 9985365.
      *>   HIGH10000 (8300, 1700) max 10000 is NOT less than 10000 -> EC-ARGUMENT-FUNCTION.
      *>   A3SUM     (7000, 3000) max 10000 -> EC-ARGUMENT-FUNCTION even though a clock-year-keyed sum
      *>             (about 2026 + 7000 = 9026) would be legal.
      *> Argument-3 is 1700, 2100 or 3000 in every probe, inside r4's "greater than 1600 and less than 10000",
      *> so a raised exception is attributable to r6 alone. Checking is ON, so §15.3 item 14's exception is the
      *> observable rather than its implementor-defined result; RESUME AT NEXT STATEMENT abandons the failed
      *> COMPUTE and the receiver still holding the previous probe's value shows nothing was stored.
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB311R6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R    PIC S9(9).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "  CAUGHT".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           COMPUTE R = FUNCTION DATE-TO-YYYYMMDD(851003 -400 2100)
           IF R = 16851003
               DISPLAY "DATE-LOW1700 OK"
           ELSE
               DISPLAY "DATE-LOW1700 BAD " R
           END-IF
           DISPLAY "DATE-LOW1699"
           COMPUTE R = FUNCTION DATE-TO-YYYYMMDD(851003 -401 2100)
           IF R = 16851003
               DISPLAY "  UNTOUCHED" ELSE DISPLAY "  STORED " R END-IF
           COMPUTE R = FUNCTION DATE-TO-YYYYMMDD(851003 8299 1700)
           IF R = 99851003
               DISPLAY "DATE-HIGH9999 OK"
           ELSE
               DISPLAY "DATE-HIGH9999 BAD " R
           END-IF
           DISPLAY "DATE-HIGH10000"
           COMPUTE R = FUNCTION DATE-TO-YYYYMMDD(851003 8300 1700)
           IF R = 99851003
               DISPLAY "  UNTOUCHED" ELSE DISPLAY "  STORED " R END-IF
           DISPLAY "DATE-A3SUM"
           COMPUTE R = FUNCTION DATE-TO-YYYYMMDD(851003 7000 3000)
           IF R = 99851003
               DISPLAY "  UNTOUCHED" ELSE DISPLAY "  STORED " R END-IF
           COMPUTE R = FUNCTION DAY-TO-YYYYDDD(85365 -400 2100)
           IF R = 1685365
               DISPLAY "DAY-LOW1700 OK"
           ELSE
               DISPLAY "DAY-LOW1700 BAD " R
           END-IF
           DISPLAY "DAY-LOW1699"
           COMPUTE R = FUNCTION DAY-TO-YYYYDDD(85365 -401 2100)
           IF R = 1685365
               DISPLAY "  UNTOUCHED" ELSE DISPLAY "  STORED " R END-IF
           COMPUTE R = FUNCTION DAY-TO-YYYYDDD(85365 8299 1700)
           IF R = 9985365
               DISPLAY "DAY-HIGH9999 OK"
           ELSE
               DISPLAY "DAY-HIGH9999 BAD " R
           END-IF
           DISPLAY "DAY-HIGH10000"
           COMPUTE R = FUNCTION DAY-TO-YYYYDDD(85365 8300 1700)
           IF R = 9985365
               DISPLAY "  UNTOUCHED" ELSE DISPLAY "  STORED " R END-IF
           STOP RUN.
       END PROGRAM PB311R6.
