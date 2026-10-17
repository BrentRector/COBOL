      *>--- conformance-case
      *> id: l1_log_domain_ec
      *> rules: [AR-15.55.3-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §15.55.3 r2 — LOG's argument-VALUE rule, both branches
      *> "The value of argument-1 shall be greater than zero."
      *>
      *> DERIVATION. ISO 15.3: "If the evaluation of an argument
      *> results in an incorrect value for that argument or for the
      *> returned value according to the rules specified in the
      *> function definition and no exception condition was raised
      *> during item identification or expression evaluation, the
      *> EC-ARGUMENT-FUNCTION exception condition is set to exist."
      *> With checking ON the condition is RAISED, so the USE AFTER
      *> EXCEPTION CONDITION declarative runs and prints CAUGHT. With
      *> checking off 15.3's next sentence hands the result to the
      *> implementor, so the raise is the only spec-fixed observable
      *> and >>TURN is what makes r2 testable at all.
      *>
      *> 1-ZERO is r2's OWN BOUNDARY (zero is not greater than zero);
      *> 2-NEGATIVE is the other violating side. Both owe CAUGHT.
      *> 3-LEGAL-ONE and 4-LEGAL-SMALL satisfy r2, so NO declarative
      *> may fire for them - that contrast is what stops a guard that
      *> raises unconditionally from passing this golden.
      *> Values from ISO 15.55.4 r1, "the approximation of the
      *> logarithm to the base e of argument-1": log-e 1 = 0 exactly,
      *> and log-e of a value in the open interval (0,1) is negative,
      *> so LOG(0.0001) < 0.
      *>
      *> The LOG10 twin of this rule (15.56.3 r2) is already pinned by
      *> pb26_ec_argument_function_every_statement. LOG had no test
      *> for r2: LOG appears in pb19 (LOG(1), the legal side of the
      *> CLASS rule), in pb56 (LOG(1) under standard-decimal) and in
      *> the negative pb62-all-subscript-log (LOG(E(ALL)), which pins
      *> 15.3's ALL-subscript admissibility, not 15.55.3 r2).
       >>TURN EC-ARGUMENT-FUNCTION CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1LOGDOM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-R PIC S9(4)V9(4) VALUE 0.
       01 W-Z PIC S9(4)V9(4) VALUE 0.
       01 W-N PIC S9(4)V9(4) VALUE -2.5.
       01 W-P PIC S9(4)V9(4) VALUE 1.
       01 W-S PIC S9(4)V9(4) VALUE 0.0001.
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
           DISPLAY "1-ZERO".
           COMPUTE W-R = FUNCTION LOG(W-Z).
           DISPLAY "2-NEGATIVE".
           COMPUTE W-R = FUNCTION LOG(W-N).
           DISPLAY "3-LEGAL-ONE".
           COMPUTE W-R = FUNCTION LOG(W-P).
           IF W-R = 0
               DISPLAY "  LOG1=ZERO"
           ELSE
               DISPLAY "  LOG1=NONZERO"
           END-IF.
           DISPLAY "4-LEGAL-SMALL".
           COMPUTE W-R = FUNCTION LOG(W-S).
           IF W-R < 0
               DISPLAY "  LOGSMALL=NEGATIVE"
           ELSE
               DISPLAY "  LOGSMALL=NONNEGATIVE"
           END-IF.
           DISPLAY "DONE".
           STOP RUN.
