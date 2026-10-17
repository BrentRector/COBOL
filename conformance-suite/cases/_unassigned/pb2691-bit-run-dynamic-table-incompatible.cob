*>--- conformance-case
*> id: pb2691-bit-run-dynamic-table-incompatible
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2691, PB2818 - a bit run opposite a dynamic-capacity table.
      *> G2 packs A and the 3-bit fixed table T into ONE byte (8.5.1.6.3), so
      *> its D starts at relative byte 1. G1 puts A in byte 0 and its dynamic
      *> table T of bit items out of line at byte 1 (8.5.1.9.1 rule 3; PB2818),
      *> one element long for the comparison (8.5.1.12.3), so its D starts at
      *> byte 2. 8.5.1.12.2: dynamic-length items correspond only "if they
      *> start at the same relative byte positions within their groups", so
      *> G1 is compatible with no group of G2's shape and 14.9.25.3 SR9 refuses
      *> the MOVE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2691NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 A PIC 1 USAGE BIT VALUE B"1".
          05 T PIC 1 USAGE BIT OCCURS DYNAMIC CAPACITY IN C1.
          05 D PIC X DYNAMIC LENGTH LIMIT 5.
       01 G2.
          05 A PIC 1 USAGE BIT VALUE B"1".
          05 T PIC 1 USAGE BIT OCCURS 3.
          05 D PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION.
       MAIN.
           MOVE G1 TO G2
           STOP RUN.
