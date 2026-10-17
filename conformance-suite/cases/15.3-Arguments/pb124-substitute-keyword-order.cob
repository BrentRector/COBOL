*>--- conformance-case
*> id: pb124-substitute-keyword-order
*> rules: [AR-15.3-7]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.87.2 prints [ANYCASE] [FIRST|LAST] before each pair IN THAT ORDER; FIRST ANYCASE is the
      *> reverse (kb/Work PB124, AR-15.3-7 - the old pending-flag accumulation accepted either order).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RS PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION SUBSTITUTE("aAa" FIRST ANYCASE "a" "z") TO RS
           STOP RUN.
