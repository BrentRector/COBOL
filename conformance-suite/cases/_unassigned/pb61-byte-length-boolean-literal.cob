*>--- conformance-case
*> id: pb61-byte-length-boolean-literal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.14.3 r1: "Argument-1 shall be an alphanumeric or national literal, a based entry, a type-name, or
      *> a data item of any class or category." A BOOLEAN literal is not in the list - unlike 15.50.3 r1, which
      *> admits "an alphanumeric, national, or boolean literal" to FUNCTION LENGTH. The two folds are siblings
      *> with DIFFERENT literal rules and must not be unified (AR-15.14.3-1's negative half).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB61BLBOOL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L PIC 9(3).
       PROCEDURE DIVISION.
           COMPUTE L = FUNCTION BYTE-LENGTH(B"101")
           STOP RUN.
