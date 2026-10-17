*>--- conformance-case
*> id: pb1507-program-id-attribute-repeated
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1507 - 5.2.6.4: "any single alternative may be specified only once" - the PROGRAM-ID attribute
      *>   group of 11.10.2 Format 1 is a choice-indicator group, so `IS COMMON COMMON PROGRAM` repeats COMMON.
      *>   COBOLNET2104 (choice-alternative-repeated).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DUP1507.
       PROCEDURE DIVISION.
           CALL "DUP1507IN"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DUP1507IN IS COMMON COMMON PROGRAM.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           GOBACK.
       END PROGRAM DUP1507IN.
       END PROGRAM DUP1507.
