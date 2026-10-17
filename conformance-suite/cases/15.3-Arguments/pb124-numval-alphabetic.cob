*>--- conformance-case
*> id: pb124-numval-alphabetic
*> rules: [AR-15.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 15.67.3 r1 admits "an alphanumeric or national literal or data item" - Table 2's closing
      *> sentence ("refers to the CATEGORY unless class is specifically indicated") settles that against a
      *> PIC A item, whose category is ALPHABETIC (8.5.2.2) and whose class is Table 2's own distinct first
      *> row. The old classifier folded PIC A into alphanumeric and admitted it at every category-worded
      *> position (kb/Work PB124 wave 5, AR-15.3-1's measured over-admission).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AL PIC A(5) VALUE "abc".
       01 R PIC 9(4).
       01 RS PIC X(10).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION NUMVAL(AL)
           STOP RUN.
