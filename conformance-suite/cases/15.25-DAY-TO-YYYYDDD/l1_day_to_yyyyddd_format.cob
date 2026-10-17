*>--- conformance-case
*> id: l1_day_to_yyyyddd_format
*> rules: [FMT-15.25.2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.25.2 general format — FUNCTION DAY-TO-YYYYDDD ( argument-1
      *> [ argument-2 [ argument-3 ] ] ): the three argument counts it admits.
      *>
      *> THE FORMAT IS THE RULE, AND UNDERLINING IS WHAT IT SAYS. Both FUNCTION and
      *> DAY-TO-YYYYDDD are underlined, so both are required words; argument-1 is
      *> unbracketed, so it is required; argument-2 is bracketed and argument-3 is
      *> bracketed INSIDE argument-2's bracket, so argument-3 cannot be written
      *> without argument-2. The admitted argument counts are therefore EXACTLY
      *> one, two and three — every one of which is exercised below, in one program
      *> so the optionality STRUCTURE is pinned in a single place rather than
      *> inferred from three files that each happen to use a different count.
      *> The reject side is conformance-suite/cases/15.25-DAY-TO-YYYYDDD/l1-day-to-yyyyddd-arity4
      *> (a fourth argument) and l1-day-to-yyyyddd-no-function-word (the required
      *> word omitted without the §8.4.3.2.3 SR2 REPOSITORY permission).
      *>
      *> WHAT EACH COUNT MUST PRODUCE, derived from §15.25.4 r1 —
      *> "(FUNCTION YEAR-TO-YYYY (YY, argument-2, argument-3) * 1000 + nnn)" with
      *> "YY = FUNCTION INTEGER (argument-1/1000)" and "nnn = FUNCTION MOD
      *> (argument-1, 1000)" — over §15.100.4's window (r1 maximum-year =
      *> argument-2 + argument-3; r2a when MOD(maximum-year,100) >= argument-1,
      *> else r2b):
      *>   THREE ARGUMENTS (85365, 10, 1900): YY = 85, nnn = 365, max = 1910,
      *>     MOD(1910,100) = 10, and 10 >= 85 is FALSE, so r2b gives
      *>     85 + 100 * (INTEGER(1910/100) - 1) = 85 + 1800 = 1885, hence 1885365.
      *>     Fully clock-free — the only count whose whole answer can be pinned.
      *>   TWO ARGUMENTS (85365, 10): argument-3 defaults to the execution year
      *>     (§15.25.3 r5), so only the clock-free half is pinned: nnn is
      *>     MOD(85365, 1000) = 365 whatever the year, and the windowed year is
      *>     above 1600 because §15.25.3 r6 keeps maximum-year above 1699.
      *>   ONE ARGUMENT (85365): §15.25.3 r3 — "If argument-2 is omitted, the
      *>     function shall be evaluated as though 50 were specified for
      *>     argument-2" — so it must equal the two-argument form written with 50.
      *>     Both read the same clock in the same run, so the equality is
      *>     clock-free even though neither value is.
      *> §8.3.5 r2 makes "the COBOL characters comma and semicolon, immediately
      *> followed by a space … separators that may be used anywhere the separator
      *> space is used", so the format's spaces admit commas: the three-argument
      *> line is written both ways and must give the same answer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1DTY01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 J3   PIC 9(7).
       01 J2   PIC 9(7).
       01 J1   PIC 9(7).
       01 J50  PIC 9(7).
       01 NNN  PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
      *> THREE arguments — the whole answer is clock-free.
           COMPUTE J3 = FUNCTION DAY-TO-YYYYDDD(85365 10 1900)
           DISPLAY "A3=" J3
           COMPUTE J3 = FUNCTION DAY-TO-YYYYDDD(85365, 10, 1900)
           DISPLAY "A3-COMMA=" J3
      *> TWO arguments — argument-3 omitted.
           COMPUTE J2 = FUNCTION DAY-TO-YYYYDDD(85365 10)
           COMPUTE NNN = FUNCTION MOD(J2, 1000)
           DISPLAY "A2-NNN=" NNN
           IF J2 > 1600000
               DISPLAY "A2-IN-WINDOW=OK"
           ELSE
               DISPLAY "A2-IN-WINDOW=BAD " J2
           END-IF
      *> ONE argument — argument-2 and argument-3 both omitted.
           COMPUTE J1 = FUNCTION DAY-TO-YYYYDDD(85365)
           COMPUTE J50 = FUNCTION DAY-TO-YYYYDDD(85365 50)
           IF J1 = J50
               DISPLAY "A1-EQ-A2-50=OK"
           ELSE
               DISPLAY "A1-EQ-A2-50=BAD " J1 " " J50
           END-IF
           COMPUTE NNN = FUNCTION MOD(J1, 1000)
           DISPLAY "A1-NNN=" NNN
           STOP RUN.
       END PROGRAM L1DTY01.
