*>--- conformance-case
*> id: pb1508-options-clause-repeated
*> rules: [FMT-11.9.2]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 11.9.2 encloses the arithmetic-clause in its own bracket with no ellipsis, so it is written at
      *> most once (5.2.6.2, 5.2.7). kb/Work PB1508: the repeat compiled clean and the LAST clause won.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1508NB.
       OPTIONS.
           ARITHMETIC IS NATIVE
           ARITHMETIC IS NATIVE.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
