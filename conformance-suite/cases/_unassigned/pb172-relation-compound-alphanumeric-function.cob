*>--- conformance-case
*> id: pb172-relation-compound-alphanumeric-function
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
      *> ⛔ THE FIXTURE THAT DISCRIMINATES BETWEEN THE TWO DESIGNS. The operand is
      *> COMPOUND, so it is an arithmetic-expression and 8.8.1.1 applies IN FULL -
      *> in the very statement where the SOLE form is legal. THE BOUNDARY IS
      *> SOLE-vs-COMPOUND, NOT STATEMENT-vs-STATEMENT, which is why splitting
      *> OperandContext into "comparison" and "arithmetic" members (kb/Work
      *> PB172's note's design) could never have expressed it: this case and
      *> pb172_relation_sole_alphanumeric_function differ inside ONE statement
      *> kind. A design that gets this fixture wrong is the wrong design.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB172N7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "ABCD".
       01 Y PIC 9(4) VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           IF FUNCTION LOWER-CASE(X) + 1 = Y
               DISPLAY "T"
           END-IF
           STOP RUN.
