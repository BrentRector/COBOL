*>--- conformance-case
*> id: pb59-national-of-hex-apostrophe-zero
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> PB59 / AR-15.66.3-3 - the APOSTROPHE spelling of the zero-length
      *> hexadecimal literal: X'' must draw 15.66.3 r3 exactly as X"" does
      *> (the lexer's apostrophe arm used to hard-code one-or-more digits, so
      *> X'' split into IDENTIFIER + a zero-length literal and the diagnostic
      *> fired on the WRONG argument).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59NEGHX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NR    PIC N(2).
       PROCEDURE DIVISION.
           MOVE FUNCTION NATIONAL-OF(X'') TO NR
           STOP RUN.
