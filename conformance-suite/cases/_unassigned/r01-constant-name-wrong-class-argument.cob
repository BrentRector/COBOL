*>--- conformance-case
*> id: r01-constant-name-wrong-class-argument
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.10.3 SR2 admits a constant-name "anywhere that a format specifies
      *> a literal of THE CLASS AND CATEGORY of constant-name-1" - so making an
      *> alphanumeric constant usable in argument positions (fix-queue R01) must
      *> NOT make it usable in a NUMERIC one. 15.84.3 r1: "Argument-1 shall be of
      *> class numeric."
      *>
      *> This was rejected before R01 too, but for the wrong reason and with the
      *> wrong clause: the numeric-expression bind reported "constant-name
      *> 'K-TEXT' substitutes a literal of category Alphanumeric and is not a
      *> numeric operand (ISO 8.8.1.1)". It now reports the function's OWN
      *> argument rule, which is the rule the source actually violates.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R01NEGCLASS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K-TEXT CONSTANT AS "abcdef".
       01 L PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION SQRT(K-TEXT) TO L.
           DISPLAY "L=" L.
           STOP RUN.
