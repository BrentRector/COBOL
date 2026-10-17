*>--- conformance-case
*> id: pb172-refmod-alphanumeric-function
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
      *> 8.4.3.3.3 syntax rule 4 makes a leftmost-position an arithmetic
      *> expression. This fixture ALSO pins the position threading: the D18 hook
      *> serves BOTH positions, and a ref-mod bound now binds under Arithmetic
      *> rather than the index-name window (13.18.38.3 r7 does not list it).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB172N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "ABCD".
       01 W PIC X(5) VALUE "ABCDE".
       01 R PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE W(FUNCTION LOWER-CASE(X):2) TO R
           STOP RUN.
