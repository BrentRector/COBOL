*>--- conformance-case
*> id: pb61-length-numeric-literal
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.50.3 r1: "Argument-1 shall be an alphanumeric, national, or boolean literal; a data item of any
      *> class or category; a based entry; or a type-name." A NUMERIC literal is in none of those - a numeric
      *> DATA ITEM is ("a data item of any class or category"), and so is a figurative (8.3.3.6.3 SR1, PB25) - so
      *> FUNCTION LENGTH(123) is rejected at bind (AR-15.50.3-1's negative half). LENGTH is a COBOL-85 intrinsic
      *> (the 1989 Intrinsic Function Module), so every edition rejects it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB61LENNUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L PIC 9(3).
       PROCEDURE DIVISION.
           COMPUTE L = FUNCTION LENGTH(123)
           STOP RUN.
