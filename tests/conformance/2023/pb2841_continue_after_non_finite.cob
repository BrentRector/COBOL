      *> kb/Work PB2841 - CONTINUE AFTER with an interval that is not a finite number.
      *> ISO 14.9.9.4 GR1: "If arithmetic-expression-1 evaluates to a value that is less than zero, ...
      *> b) If checking for FC-CONTINUE-LESS-THAN-ZERO is enabled, the EC-CONTINUE-LESS-THAN-ZERO
      *> exception condition is set to exist and processing continues as specified in 14.6.13.1.4,
      *> Nonfatal exception conditions." (the standard spells the checking FC-, a typo for EC-).
      *> ISO 14.6.13.2 item 3 limits EC-DATA-NOT-FINITE to a sending operand "described with a
      *> standard floating-point usage"; F below is FLOAT-LONG, which is not one, so a NaN or an
      *> infinity in it is NOT incompatible data even with EC-DATA-NOT-FINITE checking ON. It used to
      *> be raised (fatal) by CobolTiming.ContinueAfter for every non-finite interval.
      *> DETERMINATION (PB2647's, ISO/IEC 60559): an infinity is a NUMBER and a NaN is not.
      *> -infinity is a value less than zero: GR1 a-c, so the declarative for
      *> EC-CONTINUE-LESS-THAN-ZERO runs, then execution continues.
      *> A NaN is no number and has no seconds: the interval is zero (the disposition a NaN landing in a
      *> fixed-point item has, CobolFloat.ToScaled), so execution neither suspends nor raises anything.
      *> +infinity is a number greater than the maximum meaningful value, so GR1 suspends for the
      *> maximum (86,400 s, docs/CONFORMANCE.md DOC-A.1-39); a stdout golden cannot wait a day, so
      *> CobolTimingTests.NonFiniteInterval_TakesTheRuleOfItsKind pins it through the suspension observer.
      *> G is a STANDARD floating-point item, FLOAT-BINARY-64, read with its EC-DATA-NOT-FINITE checking
      *> turned OFF: no 14.6.13.2 item 3 raise, so -infinity takes the same GR1 less-than-zero leg.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES AND NOT FROM A RUN:
      *> HANDLED=[EC-CONTINUE-LESS-THAN-ZERO ...]  the declarative, once per -infinity statement.
      *> NEGINF-LONG DONE / NAN-LONG DONE / NEGINF-STD DONE   execution continued after each statement.
       >>TURN EC-CONTINUE-LESS-THAN-ZERO CHECKING ON
       >>TURN EC-DATA-NOT-FINITE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2841CA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X.
          05 F USAGE FLOAT-LONG.
       01 X2 REDEFINES X PIC X(8).
       01 Y.
          05 G USAGE FLOAT-BINARY-64.
       01 Y2 REDEFINES Y PIC X(8).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-CONTINUE-LESS-THAN-ZERO.
       H-P.
           DISPLAY "HANDLED=[" FUNCTION EXCEPTION-STATUS "]".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE X"FFF0000000000000" TO X2.
           CONTINUE AFTER F SECONDS.
           DISPLAY "NEGINF-LONG DONE".
           MOVE X"7FF8000000000000" TO X2.
           CONTINUE AFTER F SECONDS.
           DISPLAY "NAN-LONG DONE".
           MOVE X"FFF0000000000000" TO Y2.
           >>TURN EC-DATA-NOT-FINITE CHECKING OFF
           CONTINUE AFTER G SECONDS.
           DISPLAY "NEGINF-STD DONE".
           STOP RUN.
