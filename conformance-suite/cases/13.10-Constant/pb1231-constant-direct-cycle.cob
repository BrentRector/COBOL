*>--- conformance-case
*> id: pb1231-constant-direct-cycle
*> rules: [SR-13.10.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1231 - 13.10.3 SR5: "Neither the value of literal-1 nor the value of any of the literals in
      *> arithmetic-expression-1 shall be dependent, directly or indirectly, upon the value of constant-name-1".
      *> J's expression names J itself: the DIRECT dependence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1231DCY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 J CONSTANT AS J + 1.
       PROCEDURE DIVISION.
           DISPLAY J
           STOP RUN.
