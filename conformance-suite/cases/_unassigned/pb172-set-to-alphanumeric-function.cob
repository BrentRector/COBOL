*>--- conformance-case
*> id: pb172-set-to-alphanumeric-function
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
      *> 14.9.39.3 SR1/SR2 leave arithmetic-expression-1 as the only lane an
      *> alphanumeric function could take, and 8.8.1.1 closes it. Measured on
      *> 9a89fbd1: compiled clean AND RAN - a silent wrong answer, not the crash
      *> kb/Work PB172's note reports.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB172N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "ABCD".
       01 T.
          05 E PIC X OCCURS 3 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN.
           SET IX TO FUNCTION LOWER-CASE(X)
           STOP RUN.
