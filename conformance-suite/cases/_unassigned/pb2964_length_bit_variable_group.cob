*>--- conformance-case
*> id: pb2964_length_bit_variable_group
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2964 + PB2498 - FUNCTION LENGTH and BYTE-LENGTH of a
      *> VARIABLE-LENGTH GROUP THAT ALSO HOLDS USAGE BIT ITEMS.
      *>
      *> 15.50.4 rule 7 (LENGTH) / 15.14.4 rule 6 (BYTE-LENGTH): the value of
      *> a variable-length group is the sum of (a) the lengths of all
      *> subordinate non-variable-length data items, (b) the current lengths
      *> of all subordinate dynamic-length elementary items and (c) the
      *> lengths of all subordinate dynamic-capacity tables based on their
      *> current capacity. A group's bit items are laid out by 8.5.1.6.3 (bit
      *> items of the same level share a byte; any other item starts at the
      *> first bit of the first available byte), and 15.14.4 rule 4 /
      *> 15.50.4 rule 9 round a partly used byte up, so the fixed part of a
      *> bit-bearing group is the laid-out extent, never a member-by-member
      *> sum.
      *>
      *> Expected values, derived by hand:
      *>   G1  A1, B1 share ONE byte (two same-level bit items); D1 "ab" is 2
      *>       bytes (rule 7b): 1 + 2 = 3.
      *>   G2  X1 is one byte; the table DT holds 4 one-byte occurrences once
      *>       DT (4) is referenced (8.5.1.9.3 grows the capacity to 4), rule
      *>       7c: 4; X2 starts a byte of its own after the out-of-line table
      *>       (8.5.1.9.1 rule 3; docs/CONFORMANCE.md DOC-A.1-61): 1 + 4 + 1 = 6.
      *>   G3  a dynamic-capacity table of USAGE BIT elements: each occurrence
      *>       takes the ceil(1 / 8) = 1 byte BYTE-LENGTH reports for a bit
      *>       item (15.14.4 rule 4; 8.5.1.12.3 "the byte length of their
      *>       elements"), so capacity 3 is 3 bytes.
      *>   G4  a dynamic-capacity table whose element holds a bit item and a
      *>       dynamic-length item: element 1 is 1 + 3, element 2 is 1 + 1,
      *>       so 1 + 3 + 1 + 1 = 6.
      *>   G5  D5 "abc" 3, X5 one byte, DT5 with capacity 3 is 3 bytes, and H5
      *>       (X6 plus a 5-occurrence OCCURS DEPENDING table of bit items:
      *>       6 bits, rule 4 rounds to one byte): 3 + 1 + 3 + 1 = 8. H5
      *>       alone: 1.
      *> Each prints LENGTH then BYTE-LENGTH; a variable-length group of
      *> alphanumeric usage measures its character positions in bytes
      *> (15.50.4 rule 3), so the two agree.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2964LEN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 A1 PIC 1 USAGE BIT.
          05 B1 PIC 1 USAGE BIT.
          05 D1 PIC X DYNAMIC LENGTH.
       01 G2.
          05 X1 PIC 1 USAGE BIT.
          05 DT PIC X OCCURS DYNAMIC CAPACITY IN C2.
          05 X2 PIC 1 USAGE BIT.
       01 G3.
          05 T3 PIC 1 USAGE BIT OCCURS DYNAMIC CAPACITY IN C3.
       01 G4.
          05 TE4 OCCURS DYNAMIC CAPACITY IN C4.
             10 B4 PIC 1 USAGE BIT.
             10 D4 PIC X DYNAMIC LENGTH.
       01 N5 PIC 9.
       01 G5.
          05 D5 PIC X DYNAMIC LENGTH.
          05 X5 PIC 1 USAGE BIT.
          05 DT5 PIC X OCCURS DYNAMIC CAPACITY IN C5.
          05 H5.
             10 X6 PIC 1 USAGE BIT.
             10 TB5 PIC 1 USAGE BIT OCCURS 1 TO 8 DEPENDING ON N5.
       01 R PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ab" TO D1
           MOVE FUNCTION LENGTH(G1) TO R
           DISPLAY "G1 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G1) TO R
           DISPLAY " " R
           MOVE "A" TO DT (4)
           MOVE FUNCTION LENGTH(G2) TO R
           DISPLAY "G2 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G2) TO R
           DISPLAY " " R
           MOVE B"1" TO T3 (3)
           MOVE FUNCTION LENGTH(G3) TO R
           DISPLAY "G3 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G3) TO R
           DISPLAY " " R
           SET C4 TO 2
           MOVE "xyz" TO D4 (1)
           MOVE "q" TO D4 (2)
           MOVE FUNCTION LENGTH(G4) TO R
           DISPLAY "G4 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G4) TO R
           DISPLAY " " R
           MOVE "abc" TO D5
           MOVE "A" TO DT5 (3)
           MOVE 5 TO N5
           MOVE FUNCTION LENGTH(G5) TO R
           DISPLAY "G5 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(G5) TO R
           DISPLAY " " R
           MOVE FUNCTION LENGTH(H5) TO R
           DISPLAY "H5 " R WITH NO ADVANCING
           MOVE FUNCTION BYTE-LENGTH(H5) TO R
           DISPLAY " " R
           STOP RUN.
