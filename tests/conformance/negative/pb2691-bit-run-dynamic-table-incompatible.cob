      *> reject-at: 2014 2023
      *> kb/Work PB2691 - a dynamic-capacity table packed into a bit run.
      *> 8.5.1.6.3 puts T at "the next bit position in storage" after A (a
      *> same-level bit item), so T starts inside A's byte and occupies no
      *> relative byte positions of its own. 8.5.1.12.2 makes two tables
      *> correspond only when "they occupy the same relative byte positions
      *> within their groups", so 8.5.1.12.1 rule 1 finds no table
      *> corresponding to T: G1 is compatible with no group, and 14.9.25.3
      *> SR9 refuses the MOVE.
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
