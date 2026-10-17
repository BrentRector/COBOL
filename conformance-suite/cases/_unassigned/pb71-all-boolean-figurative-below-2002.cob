*>--- conformance-case
*> id: pb71-all-boolean-figurative-below-2002
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> The figurative ALL B"…" carries a boolean literal-1 (ISO §8.3.3.6.3 SR2), a COBOL-2002 introduction
      *> (§8.3.3.4) - below 2002 it is the introduction diagnostic. kb/Work PB71: it compiled clean at --std 85
      *> (the tokens under figurativeConstant were not visited by the version pass) and died at run time.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB71NBOOL85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AR PIC X(3).
       PROCEDURE DIVISION.
           MOVE ALL B"1" TO AR.
           STOP RUN.
