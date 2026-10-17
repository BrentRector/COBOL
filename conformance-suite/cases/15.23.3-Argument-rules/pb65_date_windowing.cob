*>--- conformance-case
*> id: pb65_date_windowing
*> rules: [AR-15.23.3-4, AR-15.23.3-6, AR-15.25.3-4, AR-15.25.3-6, RV-15.25.4-1, AR-15.100.3-4, AR-15.100.3-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.100.3 / §15.23 / §15.25 — omitted-ness is the ARITY, never a value (fix-queue PB65).
      *> An EXPLICIT argument-3 of 0 violates r4 ("greater than 1600") and takes the EC-ARGUMENT-
      *> FUNCTION path with the documented default 0 — the old in-band sentinel silently windowed it
      *> against the execution year. And an errored delegation returns the default WHOLE: the
      *> DAY-TO-YYYYDDD wrapper no longer manufactures nnn on top of an errored YEAR-TO-YYYY (r6:
      *> 9000+1995 = 10995 is out of window). Derivations: DATE-TO-YYYYMMDD(851003, 10, 1900) —
      *> max = 1910, 10 < 85 ⇒ r2b: 85+100×(19−1) = 1885 ⇒ 18851003; YEAR-TO-YYYY(50, 10, 1900) —
      *> 10 < 50 ⇒ 1850; the two error shapes ⇒ 0, whole.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65DATE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D8 PIC 9(8).
       01 D7 PIC 9(7).
       01 Y4 PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE D8 = FUNCTION DATE-TO-YYYYMMDD(851003, 10, 1900)
           DISPLAY "OK-A3=" D8
           COMPUTE Y4 = FUNCTION YEAR-TO-YYYY(50, 10, 1900)
           DISPLAY "YY-A3=" Y4
           COMPUTE D8 = FUNCTION DATE-TO-YYYYMMDD(851003, 10, 0)
           DISPLAY "ZERO-A3=" D8
           COMPUTE D7 = FUNCTION DAY-TO-YYYYDDD(85365, 9000, 1995)
           DISPLAY "R6VIOL=" D7
           STOP RUN.
