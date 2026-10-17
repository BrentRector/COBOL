*>--- conformance-case
*> id: pb68-boolean-function-arithmetic
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.1.1: an arithmetic expression's operand is "an identifier referencing a numeric data item", a
      *> numeric literal, or the figurative constant ZERO. A function-identifier references a temporary data
      *> item (8.4.3.2.4 GR1) whose class is the function's type (15.2) - BOOLEAN-OF-INTEGER's is boolean
      *> (15.13.1) - so it is not a numeric operand: COBOLNET0844 at bind (the DA6 rule that already governs a
      *> non-numeric DATA ITEM here, same dialect gate: --permissive decodes the digit characters). Before PB68
      *> this compiled CLEAN and died at run time with an unhandled NotImplementedCobolFeatureException.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB68ARITH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE N = FUNCTION BOOLEAN-OF-INTEGER(5, 8) + 1
           STOP RUN.
