*>--- conformance-case
*> id: pb653-nested-float-rounded-mode-below-2014
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB653 / PB654 -- THE EDITION EDGE of the two 2014 positives. The ROUNDED MODE IS phrase is a
      *> COBOL-2014 introduction: ISO 14.7.4's general format acquired `ROUNDED [ MODE IS rounding-mode ]` with
      *> the eight rounding modes 14.7.4.3 rules 3-10 define, and nothing else in this program asks for
      *> anything past COBOL-85 -- FUNCTION SQRT, COMPUTE and a PIC 9V9(9) receiver are all 85 constructs, and
      *> the whole substance of PB653 is stated at that edition in
      *> tests/conformance/85/pb653_nested_float_operand. At --std 2002 the phrase alone is refused,
      *> COBOLNET0803.
      *> The positives are tests/conformance/2014/pb653_nested_float_rounded_mode (the eight modes over an
      *> expression with a NESTED float operand, applied ONCE at the resultant identifier) and
      *> tests/conformance/2014/pb654_prohibited_gate_one_evaluation (14.7.7 rule 4's single initial
      *> evaluation, and 14.7.4.3 rule 7's gate over every fixed-scale receiver category).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB653RMNEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R9  PIC 9V9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R9 ROUNDED MODE IS NEAREST-EVEN = FUNCTION SQRT(3) * 1.
           STOP RUN.
