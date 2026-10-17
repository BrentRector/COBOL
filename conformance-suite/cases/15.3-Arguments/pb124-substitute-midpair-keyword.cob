*>--- conformance-case
*> id: pb124-substitute-midpair-keyword
*> rules: [AR-15.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.87.2: the phrase keywords precede the pair - a keyword BETWEEN argument-2 and argument-3
      *> is outside the format (kb/Work PB124; the old binder let it 'ride the pair').
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RS PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION SUBSTITUTE("aAa" "a" FIRST "z") TO RS
           STOP RUN.
