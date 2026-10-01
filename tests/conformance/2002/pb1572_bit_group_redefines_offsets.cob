      *> kb/Work PB1572 - a bit GROUP that continues a bit run is placed at its predecessor's next bit, and
      *> the first member INSIDE it starts at the group's OWN bit, in a record that is viewed through a
      *> REDEFINES (the Tier-B one-backing storage class) exactly as in a record that is not.
      *> ISO 8.5.1.6.3: "a bit group item immediately following a bit group item or elementary bit data item
      *>   of the same level" is at the next bit position, and "The alignment of the start of a group item and
      *>   the alignment of the first item within that group, when the first item is a bit data item, are at
      *>   the same bit position in storage" (cite.py --check 8.5.1.6.3 -> OK, both).
      *> ISO 13.18.44.4 GR1: "Storage association for the subject of the entry starts at the first bit of the
      *>   data item referenced by data-name-2" (cite.py --check 13.18.44.4 -> OK 13.18.44.4 1)), so a
      *>   PIC 1(8) view sees bit k of the record at its bit k.
      *> DERIVATION (bits high-order first; the record is 7 bits plus 1 filler bit, 8 bits in all):
      *>   R3: F3(1)=bit0 | S3{P1 P2}=bits1-2 | F4(1)=bit3 | S5{Q1(2)=bits4-5, S6{Q2(1)}=bit6}
      *>   every bit set        -> R3V=11111110, S3=11, S5=111, S6=1
      *>   R3V=0, F3 0, P2 1, Q2 1 -> bit2 and bit6    -> R3V=00100010, S3=01, S5=001
      *>   R3V=10110100            -> F3=1 S3=01 P1=0 P2=1 F4=1 S5=010 Q1=01 S6=0 Q2=0
      *>   R4 is the SAME layout with NO REDEFINES (the control arm, the record-struct lane): the members and
      *>   the groups they form agree as the layout says -> U1 1, U2 0, V1 01, V2 1: T3=10, T5=011, T6=1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1572A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R3.
          05 F3 PIC 1 USAGE BIT.
          05 S3 GROUP-USAGE BIT.
             10 P1 PIC 1 USAGE BIT.
             10 P2 PIC 1 USAGE BIT.
          05 F4 PIC 1 USAGE BIT.
          05 S5 GROUP-USAGE BIT.
             10 Q1 PIC 1(2) USAGE BIT.
             10 S6 GROUP-USAGE BIT.
                15 Q2 PIC 1 USAGE BIT.
       01 R3V REDEFINES R3 PIC 1(8) USAGE BIT.
       01 R4.
          05 G3 PIC 1 USAGE BIT.
          05 T3 GROUP-USAGE BIT.
             10 U1 PIC 1 USAGE BIT.
             10 U2 PIC 1 USAGE BIT.
          05 G4 PIC 1 USAGE BIT.
          05 T5 GROUP-USAGE BIT.
             10 V1 PIC 1(2) USAGE BIT.
             10 T6 GROUP-USAGE BIT.
                15 V2 PIC 1 USAGE BIT.
       PROCEDURE DIVISION.
           MOVE B"1" TO F3
           MOVE B"1" TO P1
           MOVE B"1" TO P2
           MOVE B"1" TO F4
           MOVE B"11" TO Q1
           MOVE B"1" TO Q2
           DISPLAY "A=" R3V " " S3 " " S5 " " S6
           MOVE B"00000000" TO R3V
           MOVE B"0" TO F3
           MOVE B"1" TO P2
           MOVE B"1" TO Q2
           DISPLAY "B=" R3V " " S3 " " S5
           MOVE B"10110100" TO R3V
           DISPLAY "C=" F3 " " S3 " " P1 " " P2 " " F4 " " S5 " " Q1
               " " S6 " " Q2
           MOVE B"1" TO U1
           MOVE B"01" TO V1
           MOVE B"1" TO V2
           MOVE B"1" TO G3
           DISPLAY "D=" T3 " " T5 " " T6 " " U1 " " U2 " " V1 " " V2
           STOP RUN.
