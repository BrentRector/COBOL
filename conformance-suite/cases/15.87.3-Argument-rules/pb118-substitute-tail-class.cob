*>--- conformance-case
*> id: pb118-substitute-tail-class
*> rules: [AR-15.87.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.87.3 r2 - "When argument-1 is an identifier that is class alphanumeric ... argument-2 and
      *> argument-3 shall be identifiers or literals that are class alphanumeric" (the national twin likewise),
      *> and 15.87.2 repeats the { [ANYCASE][FIRST|LAST] argument-2 argument-3 } pair - so r2 governs EVERY
      *> pair. The SECOND pair here is national against an alphanumeric argument-1: COBOLNET1627 (kb/Work
      *> PB118 - the cross-argument screen stopped at the three DECLARED schema positions and the variadic tail
      *> bound clean).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB118SB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X3 PIC X(3) VALUE "ABC".
       01 R PIC X(10).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION SUBSTITUTE(X3 "A" "Z" N"B" N"Y") TO R
           STOP RUN.
