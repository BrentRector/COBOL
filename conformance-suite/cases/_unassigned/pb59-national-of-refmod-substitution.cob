*>--- conformance-case
*> id: pb59-national-of-refmod-substitution
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> PB59 / AR-15.66.3-2 - a two-position REF-MOD view as argument-2 must draw
      *> the 15.66.3 r2 LENGTH half (the refmod arm returned null and the is-guard
      *> skipped the screen).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59NEGNR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N4    PIC N(4) VALUE N"WXYZ".
       01 NR    PIC N(2).
       PROCEDURE DIVISION.
           MOVE FUNCTION NATIONAL-OF("AB", N4(1:2)) TO NR
           STOP RUN.
