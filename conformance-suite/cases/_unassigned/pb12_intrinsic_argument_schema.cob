*>--- conformance-case
*> id: pb12_intrinsic_argument_schema
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> THE PER-POSITION ARGUMENT SCHEMA (fix-queue PB12/PB30/PB31).
      *>
      *> The ISO 15.3 class screen carried ONE kind per FUNCTION, so a function
      *> whose positions differ in class could not be screened at all without
      *> rejecting legal source. 15.37.3 is the clearest case: r1 makes
      *> argument-1 "a data item or literal of class alphabetic, alphanumeric,
      *> or national", r2 makes argument-2 the same family, and r3 makes
      *> argument-3 "an integer data item or integer literal". A single-kind row
      *> would have screened argument-3 as a string and REFUSED the legal call.
      *>
      *> THIS GOLDEN IS THE ACCEPT SIDE, and it is the half that matters most:
      *> the PB1 disaster was a screen that rejected 12 legal corpus programs,
      *> so every row here is legal COBOL that must keep compiling AND keep
      *> computing the right value. The reject side is pinned by the three
      *> pb12-* / pb31-* negative fixtures.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB12ARGSCHEMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A  PIC X(8) VALUE "ABCDABCD".
       01 WS-N  PIC S9(9)V99.
       01 WS-R  PIC X(10).
       01 WS-I  PIC S9(4) VALUE 2.
       PROCEDURE DIVISION.
       MAIN.
      *> ── 15.37.3 — the MIXED-CLASS shape. argument-1/2 are the string family,
      *> ── argument-3 is an integer, and all three positions are screened by
      *> ── their OWN rule. 15.37.4: the position of the 1st/(n+1)th match.
           COMPUTE WS-N = FUNCTION FIND-STRING(WS-A "BC").
           DISPLAY "01-FIND=" WS-N.
           COMPUTE WS-N = FUNCTION FIND-STRING(WS-A "BC" 1).
           DISPLAY "02-FIND-SKIP1=" WS-N.
      *> ── 15.68.3 — r1 category alphanumeric/national, r2 argument-2 of the
      *> ── SAME class. Both positions are the string family, so the schema is
      *> ── uniform in kind but the CROSS rule is what r2 adds.
           COMPUTE WS-N = FUNCTION NUMVAL-C("#1,234" "#").
           DISPLAY "03-NUMVALC=" WS-N.
      *> ── 15.39.3 / 15.41.3 — a format LITERAL plus numeric values. 15.39.4:
      *> ── integer date 1 is 1601-01-01, so day 2 is 1601-01-02.
           MOVE FUNCTION FORMATTED-DATE("YYYY-MM-DD" 2) TO WS-R.
           DISPLAY "04-FMTDATE=[" WS-R "]".
      *> ── 15.41.4 — standard numeric time form: 3600 seconds is 01:00:00.
           MOVE FUNCTION FORMATTED-TIME("hh:mm:ss" 3600) TO WS-R.
           DISPLAY "05-FMTTIME=[" WS-R "]".

      *> ── 15.59.3 r2 / 15.63.3 r2 — the CROSS-ARGUMENT rule (PB31). These are
      *> ── the LEGAL sides: one class throughout. A mixed list is the negative
      *> ── fixture pb31-max-mixed-argument-classes.
           COMPUTE WS-N = FUNCTION MAX(1 7 3).
           DISPLAY "06-MAX-NUM=" WS-N.
           MOVE FUNCTION MAX("A" "B") TO WS-R.
           DISPLAY "07-MAX-STR=[" WS-R "]".
      *> The figurative ZERO takes its class from the OTHER arguments
      *> (8.3.3.6.4 GR4), so it agrees with either family — PB48's set model is
      *> what makes r2 an intersection rather than a special case.
           COMPUTE WS-N = FUNCTION MAX(ZERO 5).
           DISPLAY "08-MAX-ZERO-NUM=" WS-N.
           MOVE FUNCTION MAX(ZERO "A") TO WS-R.
           DISPLAY "09-MAX-ZERO-STR=[" WS-R "]".

      *> ── 15.60.3 / 15.61.3 / 15.62.3 / 15.64.3 — four functions whose class
      *> ── rule no code consulted before PB30. MEAN/MEDIAN/MIDRANGE are
      *> ── VARIADIC and their rule names argument-1 only, which governs every
      *> ── occurrence of the repeated argument.
           COMPUTE WS-N = FUNCTION MEAN(1 2 3 4).
           DISPLAY "10-MEAN=" WS-N.
           COMPUTE WS-N = FUNCTION MEDIAN(1 2 3 4 10).
           DISPLAY "11-MEDIAN=" WS-N.
           COMPUTE WS-N = FUNCTION MIDRANGE(1 2 3 9).
           DISPLAY "12-MIDRANGE=" WS-N.
           COMPUTE WS-N = FUNCTION MOD(7 3).
           DISPLAY "13-MOD=" WS-N.
           COMPUTE WS-N = FUNCTION MOD(-7 3).
           DISPLAY "14-MOD-NEG=" WS-N.

      *> ── 15.43.3 / 15.58.3 — the ALGEBRAIC family is DELIBERATELY not in the
      *> ── class table: BindAlgebraicFold already enforces r1 in full, including
      *> ── the "shall be a DATA ITEM" half a class screen cannot express. Two
      *> ── mechanisms for one rule is the anti-pattern, so this row proves the
      *> ── surviving one still works rather than adding a second.
           COMPUTE WS-N = FUNCTION HIGHEST-ALGEBRAIC(WS-I).
           DISPLAY "15-HIGHEST=" WS-N.
           COMPUTE WS-N = FUNCTION LOWEST-ALGEBRAIC(WS-I).
           DISPLAY "16-LOWEST=" WS-N.

      *> ── The controls: functions screened BEFORE this change, whose verdict
      *> ── must be identical. The schema migration made every pre-existing row
      *> ── a uniform tail, which is position-for-position the old behaviour.
           COMPUTE WS-N = FUNCTION ABS(WS-I).
           DISPLAY "17-ABS=" WS-N.
           MOVE FUNCTION LOWER-CASE(WS-A) TO WS-R.
           DISPLAY "18-LOWER=[" WS-R "]".
           COMPUTE WS-N = FUNCTION LENGTH(WS-A).
           DISPLAY "19-LENGTH=" WS-N.
           STOP RUN.
