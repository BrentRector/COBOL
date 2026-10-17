*>--- conformance-case
*> id: intrinsic-phrase-word-argument
*> rules: [AR-15.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R19 (ledger F18) - a reserved phrase word in an argument slot of a function that takes
      *> no phrase compiled with zero diagnostics and threw at RUN time; 4.2.2 obliges the indication.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R19NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION EXP10(LEADING).
           DISPLAY R.
           STOP RUN.
