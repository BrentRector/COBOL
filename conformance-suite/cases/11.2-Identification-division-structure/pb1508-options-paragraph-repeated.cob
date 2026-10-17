*>--- conformance-case
*> id: pb1508-options-paragraph-repeated
*> rules: [FMT-11.2.1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 11.2.1 prints `[ options-paragraph ]` once in the identification division (5.2.6.2, 5.2.7).
      *> kb/Work PB1508's sibling arm: the second OPTIONS paragraph compiled clean and was never read.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1508ND.
       OPTIONS.
           ARITHMETIC IS NATIVE.
       OPTIONS.
           ARITHMETIC IS NATIVE.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
