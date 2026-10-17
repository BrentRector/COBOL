*>--- conformance-case
*> id: pb8-refmod-arg-permitting-function
*> rules: [SR-8.4.3.2.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.2.3 SR6: "If a function's definition permits arguments and a left parenthesis immediately
      *> follows ... the left parenthesis is ALWAYS treated as the left parenthesis of that function's
      *> arguments." RANDOM (15.75) permits an argument, so this '(' opened an ARGUMENT LIST and 1:4 is not a
      *> valid argument (SR8) - it is not a reference modification at all. The SR6 NOTE uses RANDOM for
      *> exactly this trap. Rejected as the argument-list error it is, not as a ref-mod class error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB8NEGSR6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION RANDOM (1:4) TO T
           STOP RUN.
