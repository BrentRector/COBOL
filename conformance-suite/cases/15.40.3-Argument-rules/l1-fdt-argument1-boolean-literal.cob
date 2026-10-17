*>--- conformance-case
*> id: l1-fdt-argument1-boolean-literal
*> rules: [AR-15.40.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.40.3 r1 - "Argument-1 shall be a NATIONAL or ALPHANUMERIC
      *> literal." A boolean literal is a literal, and §8.3.3.4.1 puts it
      *> outside both listed categories - "Boolean literals are of the class
      *> and category boolean". §15.40.1's type
      *> table has exactly two rows, Alphanumeric and National, and gives the
      *> function no type at all for any other argument-1 category - which
      *> is why this is a rejection rather than a defaulted result.
      *> The accept side, both listed categories, is the positive golden
      *> 2023/l1_fdt_argument1_literal_kinds.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1FDTNEG5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC X(40).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION FORMATTED-DATETIME(B"0101" 143951 45296) TO R
           STOP RUN.
