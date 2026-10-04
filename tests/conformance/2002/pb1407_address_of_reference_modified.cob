      *> kb/Work PB1407 -- ADDRESS OF a reference-modified identifier-1.
      *> ISO 8.4.3.11.3 SR4 a) itself speaks of "subscripting and
      *> reference modification in identifier-1", and identifier-1 is a
      *> general identifier (8.4.3.1.2), so a reference-modified operand
      *> is legal source.  8.4.3.11.4 GR1: the data-address-identifier
      *> "contains the address of identifier-1"; 8.4.3.3.4 GR5:
      *> reference modification "creates a unique data item that is a
      *> subset of the data item referenced by identifier-1", so that
      *> address is the one of its leftmost position.  Each address is
      *> re-based onto a BASED view (14.9.39 Format 7) and read back, so
      *> the DISPLAY proves where the pointer landed.  Legs: literal and
      *> data-name leftmost position, omitted length, a group, a
      *> qualified name, a NATIONAL item (positions are national
      *> characters, 13.18.60.4 GR8; two bytes each in this
      *> implementation, design D-N1), a subscripted element, a USAGE BIT
      *> item (positions are bit positions, 8.4.3.3.4 GR5 a)) and the
      *> pointer relation condition (8.8.4.2.16).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1407RM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(8) VALUE "ABCDEFGH".
       01 N PIC 9 VALUE 4.
       01 G.
          05 G1 PIC X(4) VALUE "WXYZ".
          05 G2 PIC X(4) VALUE "1234".
       01 NT PIC N(4) USAGE NATIONAL VALUE N"ABCD".
       01 TB.
          05 TE PIC X(3) OCCURS 3.
       01 BV PIC 1(16) USAGE BIT VALUE B"0100000101000010".
       01 P USAGE POINTER.
       01 Q USAGE POINTER.
       01 V3 PIC X(3) BASED.
       01 VN PIC N(2) USAGE NATIONAL BASED.
       01 V1 PIC X BASED.
       PROCEDURE DIVISION.
       MAIN.
           SET P TO ADDRESS OF W(3:2)
           SET ADDRESS OF V3 TO P
           DISPLAY "LIT=" V3
           SET ADDRESS OF V3 TO ADDRESS OF W(N:2)
           DISPLAY "VAR=" V3
           SET ADDRESS OF V3 TO ADDRESS OF W(5:)
           DISPLAY "OMIT=" V3
           SET ADDRESS OF V3 TO ADDRESS OF G(3:4)
           DISPLAY "GRP=" V3
           SET ADDRESS OF V3 TO ADDRESS OF G2 OF G(2:2)
           DISPLAY "QUAL=" V3
           SET ADDRESS OF VN TO ADDRESS OF NT(3:2)
           DISPLAY "NAT=" VN
           MOVE "AAA" TO TE(1)
           MOVE "BBB" TO TE(2)
           MOVE "CCC" TO TE(3)
           SET ADDRESS OF V3 TO ADDRESS OF TE(2)(2:2)
           DISPLAY "TAB=" V3
           SET ADDRESS OF V1 TO ADDRESS OF BV(9:8)
           DISPLAY "BIT=" V1
           SET Q TO ADDRESS OF W(3:1)
           IF P = Q
               DISPLAY "EQ"
           END-IF
           IF ADDRESS OF W(3:2) = ADDRESS OF W
               DISPLAY "SAME-AS-W"
           ELSE
               DISPLAY "DIFF-FROM-W"
           END-IF
           STOP RUN.
