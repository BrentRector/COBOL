      *> kb/Work PB1904 - a BIT group that redefines another record is written with its STORAGE image: its m bits
      *> PACKED into ceil(m/8) bytes, never the m boolean positions of its operand value.
      *>
      *> THE RULES.
      *> 13.18.60.4 GR5: "The USAGE BIT clause specifies that bits shall be used to represent a boolean data item."
      *>   cite.py: OK  13.18.60.4 5)  (General rules)
      *> 8.5.1.6.3: bit data items are aligned "at the next bit position in storage" after a bit item of the same
      *> level, and "a level 1 bit group are at the first bit of a byte" - so FB's 32 bits are exactly the 4 bytes
      *> of the record area FR also describes.
      *>   cite.py: OK  8.5.1.6.3  (Alignment of data items of usage bit)
      *> 13.18.44.4 GR1: "Storage association for the subject of the entry starts at the first bit of the data item
      *> referenced by data-name-2" - BG's bits ARE the bits of REC's four characters.
      *>   cite.py: OK  13.18.44.4 1)  (General rules)
      *> 13.18.29.4 GR1 b): a bit group's VALUE is that of a usage-bit item of PICTURE 1(m), "where m is the bit
      *> length of the group" - which is what MOVE BG TO FB transfers, position for position.
      *>   cite.py: OK  13.18.29.4 1) b)  (General rules)
      *>
      *> DERIVATION. REC holds "AB12", so BG (REDEFINES REC) holds the 32 bits 01000001 01000010 00110001
      *> 00110010. WRITE FB FROM BG moves them, as a 32-position boolean value, into FB - the second record of
      *> FD F, which shares FR's record area - and writes the record: the bytes those bits occupy, "AB12". Reading
      *> the file back through FR therefore shows [AB12]; FB1 and FB2 after the READ are the bits of "C" and "D"
      *> of the record "CD34" written first. Before the fix the WRITE of FB took the view's operand value - the
      *> 32 CHARACTERS "0100000101000010..." - and the record came back as [0100].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1904BV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1904bv.dat" ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  FR.
           05 FX PIC X(2).
           05 FN PIC 9(2).
       01  FB GROUP-USAGE BIT.
           05 FB1 PIC 1(8) USAGE BIT.
           05 FB2 PIC 1(8) USAGE BIT.
           05 FB3 PIC 1(16) USAGE BIT.
       WORKING-STORAGE SECTION.
       01  REC.
           05 RX PIC X(2) VALUE "AB".
           05 RN PIC 9(2) VALUE 12.
       01  BG REDEFINES REC GROUP-USAGE BIT.
           05 B1 PIC 1(8) USAGE BIT.
           05 B2 PIC 1(8) USAGE BIT.
           05 B3 PIC 1(16) USAGE BIT.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "CD34" TO FR.
           WRITE FR.
           CLOSE F.
           OPEN INPUT F.
           READ F.
           DISPLAY "FB1=[" FB1 "] FB2=[" FB2 "]".
           CLOSE F.
           OPEN OUTPUT F.
           WRITE FB FROM BG.
           CLOSE F.
           OPEN INPUT F.
           READ F.
           DISPLAY "FR=[" FR "]".
           CLOSE F.
           STOP RUN.
