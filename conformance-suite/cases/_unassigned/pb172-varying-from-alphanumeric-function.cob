*>--- conformance-case
*> id: pb172-varying-from-alphanumeric-function
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
      *> 14.9.28.3 syntax rule 2: "Each identifier shall reference a numeric
      *> elementary item described in the data division." Measured on 9a89fbd1:
      *> compiled clean and the loop STARTED AT ZERO - the reversed string
      *> digit-decoded to 0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB172N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "ABCD".
       01 V PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM VARYING V FROM FUNCTION REVERSE(X) BY 1
               UNTIL V > 3
               CONTINUE
           END-PERFORM
           STOP RUN.
