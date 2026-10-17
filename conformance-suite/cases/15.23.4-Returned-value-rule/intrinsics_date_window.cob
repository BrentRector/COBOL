*>--- conformance-case
*> id: intrinsics_date_window
*> rules: [RV-15.23.4-1, RV-15.25.4-1, AR-15.100.3-1, RV-15.100.4-1,
*>    FMT-15.23.2, FMT-15.25.2, FMT-15.100.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.100 YEAR-TO-YYYY / §15.23 DATE-TO-YYYYMMDD / §15.25 DAY-TO-YYYYDDD — the Y2K windowing
      *> trio (P11 Step 5). (The SECONDS-PAST-MIDNIGHT clock leg moved to 2014/seconds_past_midnight:
      *> the function is a 2014 introduction per the WG4 CD's D.2 new-function list — kb/Work R28.)
      *> Values hand-derived in
      *> docs/rearchitecture/PHASE-11-scout-notes.md (spec:date-window): the window is ALWAYS 100 years
      *> ending at maximum-year = argument-2 + argument-3 (§15.100.4 r1 — argument-2 is a SIGNED offset,
      *> NOT a window size); r2a when MOD(maximum-year,100) >= argument-1, else r2b. Every probe PINS
      *> argument-3 (a defaults-only golden would flip every calendar year — the scout's boundary note).
      *> T1/T2 are the spec's own §15.100.4 NOTE-1 examples; T7/T8 the §15.23.4 NOTE-1 pair; T10/T11 the
      *> §15.25.4 NOTE-1 pair.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11DATEWIN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y4  PIC 9(4).
       01 D8  PIC 9(8).
       01 J7  PIC 9(7).
       PROCEDURE DIVISION.
       MAIN.
      *> §15.100.4 NOTE 1: in 1995, (4, 23) -> max 2018, MOD 18 >= 4 -> r2a -> 2004.
           COMPUTE Y4 = FUNCTION YEAR-TO-YYYY(4, 23, 1995)
           DISPLAY "T1=" Y4
      *> §15.100.4 NOTE 1: (98, -15, 2008) -> max 1993, MOD 93 < 98 -> r2b -> 1898.
           COMPUTE Y4 = FUNCTION YEAR-TO-YYYY(98, -15, 2008)
           DISPLAY "T2=" Y4
      *> The window boundary at max 2049: MOD 49 >= 49 -> 2049 (the window's ENDING year)...
           COMPUTE Y4 = FUNCTION YEAR-TO-YYYY(49, 50, 1999)
           DISPLAY "T3=" Y4
      *> ...but 49 < 50 -> r2b wraps to the window START: 1950.
           COMPUTE Y4 = FUNCTION YEAR-TO-YYYY(50, 50, 1999)
           DISPLAY "T4=" Y4
      *> §15.100.3 r1: argument-1 = 0 is LEGAL (nonnegative) -> max 2076, MOD 76 >= 0 -> 2000.
           COMPUTE Y4 = FUNCTION YEAR-TO-YYYY(0, 50, 2026)
           DISPLAY "T5=" Y4
      *> §15.23.4 NOTE 1: (851003, 10, 2002) -> YY 85, max 2012, MOD 12 < 85 -> 1985 -> 19851003.
           COMPUTE D8 = FUNCTION DATE-TO-YYYYMMDD(851003, 10, 2002)
           DISPLAY "T6=" D8
      *> §15.23.4 NOTE 1: (981002, -10, 1994) -> YY 98, max 1984, MOD 84 < 98 -> 1898 -> 18981002.
           COMPUTE D8 = FUNCTION DATE-TO-YYYYMMDD(981002, -10, 1994)
           DISPLAY "T7=" D8
      *> The boundary pair through the composite (§15.23.4 r1 delegation): 2049 vs 1950.
           COMPUTE D8 = FUNCTION DATE-TO-YYYYMMDD(490101, 50, 1999)
           DISPLAY "T8=" D8
           COMPUTE D8 = FUNCTION DATE-TO-YYYYMMDD(500101, 50, 1999)
           DISPLAY "T9=" D8
      *> §15.25.4 NOTE 1: (10004, 20, 2002) -> YY 10, max 2022, MOD 22 >= 10 -> 2010 -> 2010004.
           COMPUTE J7 = FUNCTION DAY-TO-YYYYDDD(10004, 20, 2002)
           DISPLAY "T10=" J7
      *> §15.25.4 NOTE 1: (95005, -10, 2013) -> YY 95, max 2003, MOD 3 < 95 -> 1995 -> 1995005.
           COMPUTE J7 = FUNCTION DAY-TO-YYYYDDD(95005, -10, 2013)
           DISPLAY "T11=" J7
           STOP RUN.
