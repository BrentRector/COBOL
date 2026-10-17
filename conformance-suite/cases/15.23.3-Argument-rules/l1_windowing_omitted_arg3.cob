*>--- conformance-case
*> id: l1_windowing_omitted_arg3
*> rules: [AR-15.23.3-5, AR-15.25.3-5]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.23.3 r5 / §15.25.3 r5 — an OMITTED argument-3 evaluates as though
      *> "(FUNCTION NUMVAL (FUNCTION CURRENT-DATE (1:4)))" had been written there.
      *>
      *> BOTH RULES ARE THE SAME SENTENCE, verbatim, one for DATE-TO-YYYYMMDD and
      *> one for DAY-TO-YYYYDDD: "If argument-3 is omitted, the function shall be
      *> evaluated as though the following were specified for argument-3:
      *> (FUNCTION NUMVAL (FUNCTION CURRENT-DATE (1:4)))". A rule that states an
      *> EQUIVALENCE is tested by writing the construct BOTH ways and asserting the
      *> results are identical — clock-INDEPENDENT, because both spellings read the
      *> same clock in the same run, where any literal expectation for the omitted
      *> form would drift every January 1.
      *>
      *> ⛔ r5's EXPRESSION CANNOT BE WRITTEN IN THE ARGUMENT-3 SLOT, and an earlier
      *> draft of this file wrote it there. §8.4.3.2.3 SR11: "A numeric function
      *> shall not be specified where an integer operand is required, even though a
      *> particular reference of the numeric function might yield an integer value."
      *> §15.67.1 makes NUMVAL a NUMERIC function ("The type of this function is
      *> numeric"); §15.23.3 r4 and §15.25.3 r4 make argument-3 "an integer greater
      *> than 1600 and less than 10000", which §15.3 type 6 admits only as "an
      *> arithmetic expression that will always result in an integer value or an
      *> integer data item". r5 says the omitted form is EVALUATED AS THOUGH that
      *> expression were specified — an evaluation equivalence, not a licence to
      *> write it. (The rejection is already pinned, for the general shape, by
      *> negative/pb40-numeric-function-in-integer-position.) So r5's value is
      *> STAGED through an integer item by a COMPUTE, whose receiver is not an
      *> integer operand position — the only conforming spelling of the substituted
      *> value, and therefore the only one a POSITIVE golden may carry.
      *>
      *> ⛔ AND AN EQUALITY MUST BE ABLE TO FAIL. YEAR-TO-YYYY's answer is a step
      *> function of the execution year with 100-YEAR PLATEAUS, so a fixed
      *> argument-1 of 851003 with argument-2 = 10 returns 19851003 in EVERY year
      *> from about 1990 to about 2085: an implementation that hard-coded a year, or
      *> read a second clock skewed by less than that, would pass an identity built
      *> on it. That is not hypothetical here — this row was once OVERTURNED because
      *> YearToYyyy defaulted through the run unit's clock while CurrentDate read a
      *> different one (kb/Work R21, "one run unit, one clock").
      *> The probes therefore put the WINDOW BOUNDARY ON THE EXECUTION YEAR itself.
      *> With YY = MOD(CY,100) and argument-1 = YY*10000 + 1003, §15.100.4 gives:
      *>   argument-2 = 0  -> maximum-year = CY, MOD(CY,100) = YY, and YY >= YY is
      *>     TRUE, so r2a returns YY + 100*INTEGER(CY/100) = CY. A year one LOW
      *>     makes MOD = YY-1 < YY, takes r2b, and drops a century.
      *>   argument-2 = -1 -> maximum-year = CY-1, which takes r2b. A year one HIGH
      *>     makes maximum-year = CY, takes r2a, and gains a century.
      *> The two together break on a ONE-YEAR skew in either direction. Both stay
      *> legal: §15.23.3 r6 / §15.25.3 r6 need CY and CY-1 inside 1699..10000, and
      *> §15.23.3 r1 / §15.25.3 r1 cap argument-1 at 991003 < 1000000 and
      *> 99365 < 100000 (both positive, since 1003 and 365 survive YY = 0).
      *>
      *> TWO CLOCK-FREE WITNESSES stand beside the equality so it cannot pass on
      *> pairs of equal zeros:
      *>  · THE VALUE IS REAL. §15.23.4 r1 makes the result
      *>    "(FUNCTION YEAR-TO-YYYY (YY, argument-2, argument-3) * 10000 + mmdd)"
      *>    with "mmdd = FUNCTION MOD (argument-1, 10000)", so the low four digits
      *>    are 1003 whatever the year is; §15.25.4 r1's "nnn = FUNCTION MOD
      *>    (argument-1, 1000)" gives 365 the same way.
      *>  · ARGUMENT-3 IS LOAD-BEARING. Two EXPLICIT argument-3 values must give
      *>    DIFFERENT answers, or the equality would also hold for a compiler that
      *>    ignored argument-3 entirely. Hand-derived from §15.100.4 (r1
      *>    maximum-year = argument-2 + argument-3):
      *>      (851003, 10, 1900): YY = 85, max = 1910, MOD(1910,100) = 10, and
      *>      10 >= 85 is FALSE, so r2b -> 85 + 100 * (INTEGER(1910/100) - 1)
      *>      = 85 + 1800 = 1885, giving 18851003.
      *>      (851003, 10, 2000): max = 2010, MOD = 10 < 85, r2b -> 85 + 1900
      *>      = 1985, giving 19851003.
      *>      (85365, 10, 1900) -> 1885 * 1000 + 365 = 1885365; (…, 2000) -> 1985365.
      *>
      *> THE COMMA SEPARATORS ON THE PROBE LINES ARE LOAD-BEARING TOO. §8.3.5 r2
      *> makes them legal anywhere a space separator is ("the COBOL characters comma
      *> and semicolon, immediately followed by a space, are separators that may be
      *> used anywhere the separator space is used") — and here they are REQUIRED:
      *> written space-separated, "A1 -1" reads as the arithmetic expression A1 - 1,
      *> one argument, not two.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1WIN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CY      PIC 9(4).
       01 YY      PIC 9(2).
       01 A1      PIC 9(6).
       01 B1      PIC 9(5).
       01 A-OM-0  PIC 9(8).
       01 A-EX-0  PIC 9(8).
       01 A-OM-M1 PIC 9(8).
       01 A-EX-M1 PIC 9(8).
       01 J-OM-0  PIC 9(7).
       01 J-EX-0  PIC 9(7).
       01 J-OM-M1 PIC 9(7).
       01 J-EX-M1 PIC 9(7).
       01 MMDD    PIC 9(4).
       01 NNN     PIC 9(3).
       01 CTL8    PIC 9(8).
       01 CTL7    PIC 9(7).
       PROCEDURE DIVISION.
       MAIN.
      *> r5's own expression, staged once into an integer item. A COMPUTE receiver
      *> is not an integer operand position, so §8.4.3.2.3 SR11 does not reach it.
           COMPUTE CY = FUNCTION NUMVAL (FUNCTION CURRENT-DATE (1:4))
           COMPUTE YY = FUNCTION MOD(CY, 100)
           COMPUTE A1 = YY * 10000 + 1003
           COMPUTE B1 = YY * 1000 + 365
      *> ── §15.23.3 r5, DATE-TO-YYYYMMDD ──────────────────────────────────────
      *> argument-2 = 0 puts the window's last year AT the execution year, so an
      *> argument-3 one year LOW changes the answer by a century.
           COMPUTE A-OM-0 = FUNCTION DATE-TO-YYYYMMDD(A1, 0)
           COMPUTE A-EX-0 = FUNCTION DATE-TO-YYYYMMDD(A1, 0, CY)
      *> argument-2 = -1 puts it one year BELOW, so an argument-3 one year HIGH
      *> changes the answer by a century the other way.
           COMPUTE A-OM-M1 = FUNCTION DATE-TO-YYYYMMDD(A1, -1)
           COMPUTE A-EX-M1 = FUNCTION DATE-TO-YYYYMMDD(A1, -1, CY)
           IF A-OM-0 = A-EX-0 AND A-OM-M1 = A-EX-M1
               DISPLAY "DATE-R5=OK"
           ELSE
               DISPLAY "DATE-R5=BAD " A-OM-0 " " A-EX-0 " "
                   A-OM-M1 " " A-EX-M1
           END-IF
           COMPUTE MMDD = FUNCTION MOD(A-OM-0, 10000)
           DISPLAY "DATE-MMDD=" MMDD
           IF A-OM-0 > 16000000
               DISPLAY "DATE-YEAR-IN-WINDOW=OK"
           ELSE
               DISPLAY "DATE-YEAR-IN-WINDOW=BAD " A-OM-0
           END-IF
           COMPUTE CTL8 = FUNCTION DATE-TO-YYYYMMDD(851003 10 1900)
           DISPLAY "DATE-A3-1900=" CTL8
           COMPUTE CTL8 = FUNCTION DATE-TO-YYYYMMDD(851003 10 2000)
           DISPLAY "DATE-A3-2000=" CTL8
      *> ── §15.25.3 r5, DAY-TO-YYYYDDD ────────────────────────────────────────
           COMPUTE J-OM-0 = FUNCTION DAY-TO-YYYYDDD(B1, 0)
           COMPUTE J-EX-0 = FUNCTION DAY-TO-YYYYDDD(B1, 0, CY)
           COMPUTE J-OM-M1 = FUNCTION DAY-TO-YYYYDDD(B1, -1)
           COMPUTE J-EX-M1 = FUNCTION DAY-TO-YYYYDDD(B1, -1, CY)
           IF J-OM-0 = J-EX-0 AND J-OM-M1 = J-EX-M1
               DISPLAY "DAY-R5=OK"
           ELSE
               DISPLAY "DAY-R5=BAD " J-OM-0 " " J-EX-0 " "
                   J-OM-M1 " " J-EX-M1
           END-IF
           COMPUTE NNN = FUNCTION MOD(J-OM-0, 1000)
           DISPLAY "DAY-NNN=" NNN
           IF J-OM-0 > 1600000
               DISPLAY "DAY-YEAR-IN-WINDOW=OK"
           ELSE
               DISPLAY "DAY-YEAR-IN-WINDOW=BAD " J-OM-0
           END-IF
           COMPUTE CTL7 = FUNCTION DAY-TO-YYYYDDD(85365 10 1900)
           DISPLAY "DAY-A3-1900=" CTL7
           COMPUTE CTL7 = FUNCTION DAY-TO-YYYYDDD(85365 10 2000)
           DISPLAY "DAY-A3-2000=" CTL7
           STOP RUN.
       END PROGRAM L1WIN01.
