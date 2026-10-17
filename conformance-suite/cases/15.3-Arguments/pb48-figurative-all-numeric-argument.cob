*>--- conformance-case
*> id: pb48-figurative-all-numeric-argument
*> rules: [AR-15.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.3.6.3 SR1a again, and this time it is the phrase "WITHOUT THE ALL
      *> PHRASE" that decides: where the literal is restricted to a numeric literal
      *> the only figurative constant permitted is ZERO without ALL, so even
      *> ALL "5" - whose characters are digits - is inadmissible. 15.7.3 r1 makes
      *> ABS's argument class numeric.
      *>
      *> The sibling of pb48-figurative-space-numeric-argument, and it failed the
      *> same way: clean compile, then "bound operand 'BoundAllLiteral'" at RUN
      *> TIME. ALL literal-1 is deliberately NOT class-neutral in the screen - it
      *> carries its literal's own class (8.3.3.6.4 GR9), which is what SR1a's
      *> exclusion of the ALL phrase amounts to.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB48NEGALL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC S9(4)V99.
       PROCEDURE DIVISION.
           COMPUTE N = FUNCTION ABS(ALL "5")
           STOP RUN.
