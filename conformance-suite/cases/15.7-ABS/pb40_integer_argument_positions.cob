*>--- conformance-case
*> id: pb40_integer_argument_positions
*> rules: [FMT-15.7.2, FMT-15.36.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.3 type 6 (Integer) — THE ACCEPT SIDE, which is the half that
      *> matters: a screen that rejects a NUMERIC function in an integer position
      *> must not also reject the four shapes the rule admits.
      *>
      *> "An arithmetic expression that will always result in an integer value or
      *> an integer data item shall be specified." An always-integral arithmetic
      *> EXPRESSION is admitted in as many words, so a screen written as "reject
      *> unless PROVABLY an integer" would refuse line 1 — the PB1 failure mode
      *> reached from the opposite direction. The screen therefore rejects only
      *> the two PROVABLE shapes (a numeric function, per 8.4.3.2.3 SR11; and a
      *> scaled numeric data item) and fails open on everything else.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB40INTARG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-I PIC 9(3) VALUE 65.
       01 W-R PIC X.
       01 W-N PIC S9(9)V99.
       PROCEDURE DIVISION.
       MAIN.
      *> 1 — an always-integral ARITHMETIC EXPRESSION. 15.15.4: CHAR returns the
      *> character in ordinal position n (1-based), so position 66 is 'A'.
           MOVE FUNCTION CHAR(W-I + 1) TO W-R.
           DISPLAY "1-ARITH=[" W-R "]".
      *> 2 — an integer LITERAL.
           MOVE FUNCTION CHAR(66) TO W-R.
           DISPLAY "2-LITERAL=[" W-R "]".
      *> 3 — an integer DATA ITEM (scale 0). Ordinal 65 is '@'.
           MOVE FUNCTION CHAR(W-I) TO W-R.
           DISPLAY "3-ITEM=[" W-R "]".
      *> 4 — an INTEGER function. SR11 bars a NUMERIC one; 15.7.1 makes ABS over
      *> an unscaled item an integer function, so this is admitted.
           MOVE FUNCTION CHAR(FUNCTION ABS(W-I)) TO W-R.
           DISPLAY "4-INTEGER-FN=[" W-R "]".
      *> 5 — the same rule at a different function. 15.36.3 r1 makes FACTORIAL's
      *> argument an integer; W-I - 62 is always integral. 15.36.4: 3! = 6.
           COMPUTE W-N = FUNCTION FACTORIAL(W-I - 62).
           DISPLAY "5-FACTORIAL=" W-N.
      *> 6 — 15.64.3 r1 makes BOTH of MOD's arguments integers. 15.64.4:
      *> MOD(13,7) = 13 - 7*INTEGER(13/7) = 6.
           COMPUTE W-N = FUNCTION MOD(W-I - 52, 7).
           DISPLAY "6-MOD=" W-N.
           STOP RUN.
