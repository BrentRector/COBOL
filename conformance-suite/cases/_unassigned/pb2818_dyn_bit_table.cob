*>--- conformance-case
*> id: pb2818_dyn_bit_table
*> rules: []
*> accept: [2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2818 - A DYNAMIC-CAPACITY TABLE OF USAGE BIT ELEMENTS,
      *> in a group and as a whole-group MOVE / comparison operand.
      *>
      *> 8.5.1.9.1 rule 3: a dynamic-capacity table "may be defined in any
      *> place, other than the file section, in which a fixed-capacity
      *> table may be defined", and its "table data items are not required
      *> to be contiguous in physical memory. The size, location, and time
      *> of physical allocation ... are implementor-defined". So T1 below is
      *> out-of-line storage: it does not continue the bit position of A1
      *> (8.5.1.6.3: "all other bit data items" are "at the first bit
      *> position of the first available byte"), each occurrence takes the
      *> ceil(n / 8) bytes BYTE-LENGTH reports for a bit item (15.14.4 rule
      *> 4), and B1 after the table starts a byte of its own.
      *>
      *> 8.5.1.9.3: a receiving reference with a subscript past the current
      *> capacity grows the table to the subscript, so after MOVE B"1" TO
      *> T1 (1) the CAPACITY register C1 is 1.
      *>
      *> 8.5.1.12.2 / 8.5.1.12.3: two tables correspond at the same relative
      *> byte position and match when the byte length of their elements is
      *> equal, a dynamic-capacity table counting as ONE element. G1 and G2
      *> are the same shape, so MOVE G1 TO G2 recreates T2 from T1's three
      *> occurrences (14.6.9.2): C2 = 3 and T2 = 1, 0, 1. G3 / G4 differ in
      *> the width of the bit item before the table (1 bit, 7 bits) but
      *> both leave it at relative byte 1, so they correspond too. G5 / G6
      *> hold a 10-bit element (two bytes): MOVE G5 TO G6 carries both
      *> occurrences and the fixed material Y5 ("y") into Y6.
      *>
      *> 8.8.4.2.17: compatible groups compare item by item; G1 = G2 after
      *> the MOVE, and not once T2 (3) differs.
      *>
      *> G7 is the same shape held in an EXTERNAL area (8.6.7), whose table
      *> occurrences are cells of their own: T7 (1) and T7 (2) are read back
      *> as written, at the table's element and not at its byte in G7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2818BIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 A1 PIC 1 USAGE BIT VALUE B"1".
          05 T1 PIC 1 USAGE BIT OCCURS DYNAMIC CAPACITY IN C1.
          05 B1 PIC 1 USAGE BIT VALUE B"1".
          05 D1 PIC X DYNAMIC LENGTH LIMIT 5.
       01 G2.
          05 A2 PIC 1 USAGE BIT VALUE B"0".
          05 T2 PIC 1 USAGE BIT OCCURS DYNAMIC CAPACITY IN C2.
          05 B2 PIC 1 USAGE BIT VALUE B"0".
          05 D2 PIC X DYNAMIC LENGTH LIMIT 5.
       01 G3.
          05 A3 PIC 1 USAGE BIT VALUE B"1".
          05 T3 PIC 1 USAGE BIT OCCURS DYNAMIC CAPACITY IN C3.
          05 D3 PIC X DYNAMIC LENGTH LIMIT 5.
       01 G4.
          05 A4 PIC 1(7) USAGE BIT VALUE B"0000000".
          05 T4 PIC 1 USAGE BIT OCCURS DYNAMIC CAPACITY IN C4.
          05 D4 PIC X DYNAMIC LENGTH LIMIT 5.
       01 G5.
          05 W5 PIC 1(10) USAGE BIT OCCURS DYNAMIC CAPACITY IN C5.
          05 Y5 PIC X VALUE "y".
       01 G6.
          05 W6 PIC 1(10) USAGE BIT OCCURS DYNAMIC CAPACITY IN C6.
          05 Y6 PIC X VALUE "z".
       01 G7 EXTERNAL.
          05 A7 PIC 1 USAGE BIT.
          05 T7 PIC 1 USAGE BIT OCCURS DYNAMIC CAPACITY IN C7.
       PROCEDURE DIVISION.
       MAIN.
           MOVE B"1" TO T1 (1)
           DISPLAY "C1=" C1
           MOVE B"0" TO T1 (2)
           MOVE B"1" TO T1 (3)
           MOVE "hi" TO D1
           MOVE G1 TO G2
           DISPLAY "C2=" C2 " T2=" T2 (1) T2 (2) T2 (3)
              " A2=" A2 " B2=" B2 " D2=[" D2 "]"
           IF G1 = G2 DISPLAY "G1 EQ G2" ELSE DISPLAY "G1 NE G2" END-IF
           MOVE B"0" TO T2 (3)
           IF G1 = G2 DISPLAY "G1 EQ G2" ELSE DISPLAY "G1 NE G2" END-IF
           MOVE B"1" TO T3 (1)
           MOVE B"1" TO T3 (2)
           MOVE "ab" TO D3
           MOVE G3 TO G4
           DISPLAY "C4=" C4 " T4=" T4 (1) T4 (2) " D4=[" D4 "]"
           MOVE B"1010101010" TO W5 (1)
           MOVE B"0000011111" TO W5 (2)
           MOVE G5 TO G6
           DISPLAY "C6=" C6 " W6=" W6 (1) "," W6 (2) " Y6=" Y6
           MOVE B"0" TO T7 (1)
           MOVE B"1" TO T7 (2)
           DISPLAY "C7=" C7 " T7=" T7 (1) T7 (2)
           STOP RUN.
