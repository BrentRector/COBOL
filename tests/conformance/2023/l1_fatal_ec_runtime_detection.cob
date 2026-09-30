      *> ISO §14.6.13.1.3 8) / A.1 item 69 — fatal ECs detected at run time, code always produced
      *> RULE (§14.6.13.1.3 8), last paragraph; cite.py OK): "If checking
      *> is not enabled for a fatal exception condition and the exception
      *> condition is detected by the compiler, the implementor is not
      *> required to produce executable code. It is implementor-defined
      *> which fatal exception conditions, if any, are detected at compile
      *> time, and the circumstances under which they are detected."
      *> DETERMINATION PINNED (docs/CONFORMANCE.md DOC-A.1-69): the
      *> compile-time detections are the COBOLNET1662 warning for an
      *> unprovided ORDER TABLE and a LITERAL reference modification
      *> outside a fixed-size item (kb/Work PB1707: COBOLNET2670 error
      *> where EC-BOUND-REF-MOD checking is off, COBOLNET2671 warning
      *> where it is on). With checking ON the compiler still produces
      *> executable code: a constant subscript outside the OCCURS bounds,
      *> a constant reference modification outside the item (the warning
      *> above), a literal zero divisor and a constant intrinsic argument
      *> outside its domain compile and raise the fatal condition at RUN
      *> time. (DOC-A.1-69 also says "with checking off it continues";
      *> that is item 70's question, and DOC-A.1-70 answers it
      *> differently for a zero divisor and, since PB1707, for a
      *> reference modification. See the unchecked half below.)
      *> WHY EACH LEG CAN FAIL: a compile-time REJECTION of any of the
      *> four constant cases at checking ON fails the strict compile;
      *> omitting their code loses a CAUGHT line or leaves V=0050000; a
      *> run-time crash with checking OFF loses the last lines (the
      *> corpus also requires exit code 0).
      *> DERIVATION (checked half, >>TURN ... CHECKING ON):
      *>   E (6) on OCCURS 5: §8.4.2.3.4 2) "greater than the highest
      *>     permissible occurrence number, the EC-BOUND-SUBSCRIPT
      *>     exception condition is set to exist" (cite.py OK)
      *>   S (7:2) on PIC X(5): §8.4.3.3.4 5) c) EC-BOUND-REF-MOD
      *>     (cite.py OK)
      *>   N / 0: §14.7.5 2) "if the divisor in a divide operation ... is
      *>     zero, the EC-SIZE-ZERO-DIVIDE exception condition is set to
      *>     exist" (cite.py OK)
      *>   SQRT (-1): §15.84.3 2) "The value of argument-1 shall be zero
      *>     or positive"; §15.3 14) EC-ARGUMENT-FUNCTION (cite.py OK)
      *>   All four are Fatal (Table 13). §14.6.13.1.3 5): checking is
      *>   enabled and a USE names the condition, so the declarative runs;
      *>   it displays §15.33.3 1) EXCEPTION-STATUS (31 chars, trailing
      *>   spaces trimmed by the harness) and RESUME AT NEXT STATEMENT
      *>   (§14.9.33.4 2) a), cite.py OK) continues after the statement,
      *>   so each statement yields exactly one CAUGHT line, in order.
      *> DERIVATION (unchecked half, after >>TURN EC-ALL CHECKING OFF):
      *>   §14.6.13.1.1 "if checking for an exception condition is not
      *>   enabled, the exception condition will not be raised" -> no
      *>   CAUGHT line. Whether execution continues is A.1 item 70 (NOT
      *>   item 69), §14.6.13.1.3 8) "the implementor defines whether or
      *>   not execution will continue" (cite.py OK), determined by
      *>   docs/CONFORMANCE.md DOC-A.1-70.
      *>   NO UNCHECKED ZERO DIVISOR: DOC-A.1-70 determines that a zero
      *>   divisor (§14.7.5 case 2) has no result to store and, with
      *>   checking not enabled, the run unit terminates abnormally
      *>   (exit 1), which a golden cannot carry. It is left out here.
      *>   NO UNCHECKED REFERENCE MODIFICATION: a literal one outside the
      *>   item is refused at compile time while checking is off
      *>   (COBOLNET2670, negative:pb1707-refmod-literal-out-of-range),
      *>   and a computed one ends the run unit (owner decision R60),
      *>   which a golden cannot carry (tests in
      *>   RefModUncheckedTerminationTests). It is left out here.
      *>   The unchecked subscript leg has no observable receiving
      *>   value; it shows only that the code runs and execution
      *>   continues.
      *>   OBSERVABLE LEG: V is set to 5, then SQRT (-1) with checking
      *>   off. §15.3 14) "the implementor defines the result of the
      *>   function reference" (cite.py OK); DOC-A.1-90 determines the
      *>   numeric result is 0, so V=0000000 (PIC 9(3)V9(4), 7 digits, no
      *>   point). If the code for the statement were omitted, V would
      *>   still show 0050000, so this line shows code was produced.
      *>   Then UNCHECKED-DONE prints.
       >>TURN EC-BOUND-SUBSCRIPT EC-BOUND-REF-MOD CHECKING ON
       >>TURN EC-SIZE-ZERO-DIVIDE EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FECRT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X OCCURS 5 TIMES.
       01 S PIC X(5) VALUE "HELLO".
       01 R PIC X(2) VALUE "??".
       01 N PIC 9(3) VALUE 7.
       01 V PIC 9(3)V9(4) VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT
               EC-BOUND-REF-MOD EC-SIZE-ZERO-DIVIDE
               EC-ARGUMENT-FUNCTION.
       H-P.
           DISPLAY "CAUGHT=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       CHECKED-P.
           MOVE ALL "A" TO T.
           MOVE E (6) TO R.
           MOVE S (7:2) TO R.
           COMPUTE N = N / 0.
           COMPUTE V = FUNCTION SQRT (-1).
           DISPLAY "CHECKED-DONE".
       >>TURN EC-ALL CHECKING OFF
       UNCHECKED-P.
           MOVE E (6) TO R.
           MOVE 5 TO V.
           COMPUTE V = FUNCTION SQRT (-1).
           DISPLAY "V=" V.
           DISPLAY "UNCHECKED-DONE".
           STOP RUN.
