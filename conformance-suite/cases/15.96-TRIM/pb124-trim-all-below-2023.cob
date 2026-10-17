*>--- conformance-case
*> id: pb124-trim-all-below-2023
*> rules: [FMT-15.96.2]
*> source-format: free
*> portable: unverified
*>---
      *> A table(ALL) argument to TRIM enumerates a VARIABLE argument list - the 2023 argument-2 form
      *> whatever the element count (E.3.3 item 31); through 2014 TRIM takes exactly one argument. The old
      *> gate counted the enumeration as ONE operand and let TRIM(ALL T) through at 2014 (kb/Work PB124,
      *> FMT-15.96.2's edition-gate hole).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB124NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X OCCURS 3 TIMES.
       01 RS PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION TRIM(E(ALL)) TO RS
           STOP RUN.
