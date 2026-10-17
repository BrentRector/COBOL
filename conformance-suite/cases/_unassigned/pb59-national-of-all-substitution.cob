*>--- conformance-case
*> id: pb59-national-of-all-substitution
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> PB59 / AR-15.66.3-2 - ALL "QQ" as argument-2 must draw the 15.66.3 r2
      *> LENGTH half (KnownWidth had no BoundAllLiteral arm; 8.3.3.6.4 GR3c gives
      *> it the literal's length, 2). ALL over a NATIONAL literal is a separate
      *> parse gap, registered on kb/Work (found while writing this fixture).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB59NEGNA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NR    PIC N(2).
       PROCEDURE DIVISION.
           MOVE FUNCTION NATIONAL-OF("AB", ALL "QQ") TO NR
           STOP RUN.
