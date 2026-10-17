*>--- conformance-case
*> id: pb66-value-form-float-edited
*> rules: [SR-13.18.63.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 13.18.63.3 SR6: "literals for floating-point formats shall be specified as floating-point,
      *> though the figurative constant ZERO or ZEROES and the integer and decimal forms of the literal zero may
      *> also be specified" - a NONZERO fixed-point literal on a floating-point numeric-edited item is
      *> COBOLNET1659 (kb/Work PB66); the level-88 (format 2) form of the VALUE clause is under the same rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB66NVF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC -9.9(5)E+99 VALUE 12.5.
       01 E2 PIC +9.99E+99.
          88 E2-BIG VALUE 100.
       PROCEDURE DIVISION.
           STOP RUN.
