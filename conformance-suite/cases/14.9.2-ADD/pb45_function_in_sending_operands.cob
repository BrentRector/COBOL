*>--- conformance-case
*> id: pb45_function_in_sending_operands
*> rules: [GR-14.9.2.4-1, GR-14.9.2.4-2, GR-14.9.12.4-5, FMT-14.9.2.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.1.2 Format 1 - A FUNCTION-IDENTIFIER IS AN IDENTIFIER, so it is
      *> admissible wherever a general format writes `identifier-n | literal-n` in a
      *> SENDING role (fix-queue PB45). 8.4.3.2.3 SR1 bars it only as a RECEIVING
      *> operand, which is why every receiving position below is a plain data item.
      *>
      *> IT WAS A PARSE ERROR ACROSS THE WHOLE ARITHMETIC FAMILY. The grammar wrote
      *> the sending operand FOUR TIMES - addOperand / subtractOperand /
      *> multiplyOperand / divideOperand, each `dataReference | literal` - and all
      *> four omitted functionCall, while COMPUTE accepted it because its RHS is an
      *> arithmeticExpression. One rule written down four times, wrong in all four.
      *> INSPECT's inspectChar was a fifth copy with the same omission.
      *>
      *> ⚠ EVERY LINE BELOW CHECKS THE VALUE, NOT MERELY THAT IT COMPILED. Making
      *> the grammar accept the operand was HALF the fix: the binder's operand-
      *> wrapper walk had no functionCall arm, so it descended INTO the call and
      *> bound the ARGUMENT - `ADD FUNCTION SQRT(W-Z) TO W-R` with W-Z = 4 added
      *> FOUR instead of TWO. It compiled and printed a plausible number. Only
      *> comparing against the spec-derived answer caught it.
      *>
      *> FUNCTION SQRT(4) = 2 throughout.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB45SENDOPS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-Z  PIC 9(4) VALUE 4.
       01 W-Q  PIC 9(4) VALUE 9.
       01 W-R  PIC 9(4) VALUE 0.
       01 W-U  PIC X(9) VALUE "ABABAB   ".
       01 W-V  PIC X(9) VALUE "ABABAB   ".
       01 W-P  PIC X(2) VALUE "ab".
       01 W-N  PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
      *> ADD - both formats (14.9.2.2 Format 1 and 2).
           MOVE 1 TO W-R.
           ADD FUNCTION SQRT(W-Z) TO W-R.
           DISPLAY "ADD-TO=" W-R.
           ADD FUNCTION SQRT(W-Z) TO W-Q GIVING W-R.
           DISPLAY "ADD-GIVING=" W-R.
      *> SUBTRACT (14.9.44.2): 9 - 2 = 7.
           SUBTRACT FUNCTION SQRT(W-Z) FROM W-Q GIVING W-R.
           DISPLAY "SUB-GIVING=" W-R.
      *> MULTIPLY (14.9.26.2): 2 * 9 = 18.
           MULTIPLY FUNCTION SQRT(W-Z) BY W-Q GIVING W-R.
           DISPLAY "MUL-GIVING=" W-R.
      *> DIVIDE, both operand positions (14.9.10.2): 9 / 2 = 4 (truncated).
           DIVIDE FUNCTION SQRT(W-Z) INTO W-Q GIVING W-R.
           DISPLAY "DIV-INTO=" W-R.
           DIVIDE W-Q BY FUNCTION SQRT(W-Z) GIVING W-R.
           DISPLAY "DIV-BY=" W-R.
      *> COMPUTE - the one that ALWAYS worked, kept as the control that proves the
      *> change did not disturb the arithmeticExpression path.
           COMPUTE W-R = FUNCTION SQRT(W-Z).
           DISPLAY "COMPUTE=" W-R.
      *> INSPECT TALLYING (14.9.22.2): UPPER-CASE("ab") = "AB", 3 occurrences.
           INSPECT W-U TALLYING W-N FOR ALL FUNCTION UPPER-CASE(W-P).
           DISPLAY "INSPECT-TALLY=" W-N.
      *> INSPECT REPLACING - the same operand rule on both sides of BY.
           INSPECT W-V REPLACING ALL FUNCTION UPPER-CASE(W-P) BY "xy".
           DISPLAY "INSPECT-REPL=[" W-V "]".
           STOP RUN.
