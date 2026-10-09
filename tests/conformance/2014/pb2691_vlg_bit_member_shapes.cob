      *> kb/Work PB2691 - VARIABLE-LENGTH GROUPS WITH USAGE BIT MEMBERS, as
      *> whole-group MOVE and comparison operands of DIFFERENT shapes.
      *>
      *> 8.5.1.6.3: "an elementary bit data item immediately following an
      *> elementary bit data item or bit group item of the same level" is at
      *> "the next bit position in storage"; "all other bit data items" are
      *> "at the first bit position of the first available byte". So a run of
      *> same-level bit items SHARES bytes, and whatever follows the run
      *> starts on a byte boundary: A and B of G3 below (1 + 1 bits) occupy
      *> ONE byte, and D of G3 starts at relative byte position 1, exactly
      *> where D of G4 (after its 2-bit A) starts.
      *>
      *> 8.5.1.12.2: "Two dynamic-length elementary items correspond if they
      *> start at the same relative byte positions within their groups", so
      *> G3 and G4 are compatible; a per-member byte count put D of G3 at
      *> byte 2 and refused the MOVE (COBOLNET1931 / COBOLNET2492). G5/G6:
      *> a 4-bit run then a fixed table opposite a 4-bit item then a
      *> dynamic-capacity table at the same relative byte position 1.
      *>
      *> 14.6.9.2 / 14.6.9.3 / 8.8.4.2.17: the pair moves and compares by its
      *> correspondence, never ordinally. G1/G2 differ only in the table kind
      *> after an 8-bit leader: MOVE G1 TO G2 gives the fixed table the one
      *> occurrence "a" and a space ("xa dd"), MOVE G2 TO G1 recreates the
      *> dynamic table with two occurrences (C1 = 2, "xpqzz") and the two
      *> then compare EQUAL. Handing the carrier over ordinally printed
      *> "x  a" and "xzz" and NE. B"01111000" is the character "x".
      *>
      *> The nested CALL passes G1 to a formal of G2's shape (14.8.2.2 makes
      *> the pair compatible as in 8.5.1.12): the formal sees "xa dd".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2691BIT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 X1 PIC 1(8) USAGE BIT VALUE B"01111000".
          05 T1 OCCURS DYNAMIC CAPACITY IN C1 FROM 1.
             10 E1 PIC X.
          05 D1 PIC X DYNAMIC LENGTH LIMIT 5.
       01 G2.
          05 X2 PIC 1(8) USAGE BIT VALUE B"01111001".
          05 T2 OCCURS 2.
             10 E2 PIC X.
          05 D2 PIC X DYNAMIC LENGTH LIMIT 5.
       01 G3.
          05 A3 PIC 1 USAGE BIT VALUE B"0".
          05 B3 PIC 1 USAGE BIT VALUE B"1".
          05 D3 PIC X DYNAMIC LENGTH LIMIT 5.
          05 Y3 PIC X VALUE "y".
       01 G4.
          05 A4 PIC 11 USAGE BIT VALUE B"10".
          05 D4 PIC X DYNAMIC LENGTH LIMIT 5.
          05 Y4 PIC XX VALUE "zz".
       01 G5.
          05 A5 PIC 1 USAGE BIT VALUE B"0".
          05 B5 PIC 1(3) USAGE BIT VALUE B"001".
          05 T5 OCCURS 2.
             10 E5 PIC X.
          05 Y5 PIC X VALUE "y".
       01 G6.
          05 A6 PIC 1(4) USAGE BIT VALUE B"0000".
          05 T6 OCCURS DYNAMIC CAPACITY IN C6 FROM 1.
             10 E6 PIC X.
          05 Y6 PIC X VALUE "z".
       PROCEDURE DIVISION.
       MAIN.
           MOVE "a" TO E1 (1)
           MOVE "dd" TO D1
           CALL "PB2691SB" AS NESTED USING G1
           MOVE G1 TO G2
           DISPLAY "G2=[" G2 "]"
           MOVE "p" TO E2 (1)
           MOVE "q" TO E2 (2)
           MOVE "zz" TO D2
           MOVE G2 TO G1
           DISPLAY "G1=[" G1 "] C1=" C1
           IF G1 = G2 DISPLAY "G1 EQ G2" ELSE DISPLAY "G1 NE G2" END-IF
           MOVE "abc" TO D3
           MOVE "ab" TO D4
           IF G3 < G4 DISPLAY "G3 LT G4" ELSE DISPLAY "G3 GE G4" END-IF
           MOVE G3 TO G4
           DISPLAY "A4=" A4 " D4=[" D4 "] Y4=[" Y4 "]"
           IF G3 = G4 DISPLAY "G3 EQ G4" ELSE DISPLAY "G3 NE G4" END-IF
           MOVE "p" TO E5 (1)
           MOVE "q" TO E5 (2)
           MOVE G5 TO G6
           DISPLAY "C6=" C6 " A6=" A6 " E6=" E6 (1) E6 (2) " Y6=" Y6
           IF G5 = G6 DISPLAY "G5 EQ G6" ELSE DISPLAY "G5 NE G6" END-IF
           MOVE "r" TO E6 (1)
           MOVE G6 TO G5
           DISPLAY "A5=" A5 " B5=" B5 " E5=" E5 (1) E5 (2) " Y5=" Y5
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2691SB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L.
          05 XL PIC 1(8) USAGE BIT.
          05 TL OCCURS 2.
             10 EL PIC X.
          05 DL PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION USING L.
           DISPLAY "SB=[" L "]"
           GOBACK.
       END PROGRAM PB2691SB.
       END PROGRAM PB2691BIT.
