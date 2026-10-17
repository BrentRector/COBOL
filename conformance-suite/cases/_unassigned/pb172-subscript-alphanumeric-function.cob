*>--- conformance-case
*> id: pb172-subscript-alphanumeric-function
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.2: a function's type is the class and category of the temporary
      *> data item its result occupies, and "the function so described may be
      *> used anywhere a sending data item of that class and category may be
      *> specified"; item 1 makes LOWER-CASE/UPPER-CASE/REVERSE/TRIM alphanumeric
      *> functions. 8.8.1.1 admits only a NUMERIC identifier, a numeric literal
      *> or ZERO in an arithmetic expression.
      *> The 8.8.1.1 intrinsic screen keyed on OperandContext.Arithmetic ONLY,
      *> because widening it to the index-name window rejected legal SOLE-function
      *> relation comparands (six NIST IF programs). The eight genuinely-arithmetic
      *> window sites were therefore unscreened so that two comparison sites could
      *> stay correct - one enum member, two rule regimes.
      *> 
      *> The D18 route: a function-bearing segment routes to MaterializeSegment,
      *> which re-parses and binds it under the index-name window context.
      *> 8.4.2.3.2 admits arithmetic-expression-1, so 8.8.1.1 governs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB172N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "ABCD".
       01 R PIC X.
       01 T.
          05 E PIC X OCCURS 3 TIMES.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABC" TO T
           MOVE E(FUNCTION LOWER-CASE(X)) TO R
           STOP RUN.
