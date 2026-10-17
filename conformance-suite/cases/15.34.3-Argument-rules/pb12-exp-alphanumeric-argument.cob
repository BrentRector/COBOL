*>--- conformance-case
*> id: pb12-exp-alphanumeric-argument
*> rules: [AR-15.34.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> PB12 - the 15.3 argument-class screen now has a row for EXP. 15.34.3 rule 1: "Argument-1 shall be of
      *> class numeric." An alphanumeric item is not, so the screen rejects it with COBOLNET1627 where before
      *> the function had no row at all and the illegal argument was accepted and a value computed from it.
      *> EXP is 2002+ (15.34), so 85 is not in the reject set.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB12NEGEXP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(4) VALUE "ABCD".
       01 R PIC S9(9)V9(4).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION EXP(S)
           STOP RUN.
