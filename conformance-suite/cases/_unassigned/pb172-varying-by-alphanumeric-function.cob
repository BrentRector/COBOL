*>--- conformance-case
*> id: pb172-varying-by-alphanumeric-function
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
      *> THE BY ARM - the twin of FROM, pinned so the fix cannot land on FROM
      *> alone. Both bind through the same BindVaryingLevel, but "the same
      *> method" is exactly what was believed about the STOP/GOBACK pair too, and
      *> believing it is not measuring it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB172N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "ABCD".
       01 V PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM VARYING V FROM 1 BY FUNCTION REVERSE(X)
               UNTIL V > 3
               CONTINUE
           END-PERFORM
           STOP RUN.
