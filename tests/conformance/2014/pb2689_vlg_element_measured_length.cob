      *> kb/Work PB2689 - TWO CORRESPONDING TABLES WHOSE ELEMENTS HOLD A
      *> DYNAMIC-CAPACITY TABLE OPPOSITE A FIXED ONE: the element lengths are
      *> the ones the PAIR gives them, not their one-occurrence images.
      *>
      *> 8.5.1.12.3: "Two corresponding tables match when the byte length of
      *> their elements is equal and their elements are compatible." An E1
      *> element holds IA OCCURS DYNAMIC (one element, 1 byte, by itself) and
      *> an E2 element holds IB OCCURS 2 (2 bytes); but "If one of the
      *> corresponding tables is not a dynamic-capacity table ... For purposes
      *> of determining compatibility, the dynamic-capacity table is
      *> considered to be the same length as the corresponding table", so IA
      *> counts 2 bytes, KA and KB then share relative byte position 2, and
      *> both elements are 3 bytes: E1 and E2 match, G1 and G2 are
      *> compatible (8.5.1.12.1) and the MOVE and the comparison are legal.
      *>
      *> Expected values (14.9.25.4 GR9 a; 14.6.9.2; 14.6.9.3):
      *> - MOVE G1 TO G2 recreates E2 with E1's capacity 2; correspondingly
      *>   numbered elements move as groups, so IA (p q) fills IB (p q), and
      *>   IA (r), which has a lower capacity than IB, fills IB (r) and the
      *>   remaining IB element is space filled: G2 = "pqk" "r m" "z1".
      *> - G1 = G2: IA (r) against IB (r space) - the larger table's remaining
      *>   element compares with spaces, so EQ; after IB (2, 2) = "s" it is NE.
      *> - MOVE G2 TO G1 recreates each IA with IB's 2 elements: G1 =
      *>   "pqk" "rsm" "z1" (Z2 took "z1"), and the groups compare EQ again.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2689EML.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 E1 OCCURS DYNAMIC CAPACITY IN C1 FROM 1.
             10 IA PIC X OCCURS DYNAMIC CAPACITY IN CA FROM 1.
             10 KA PIC X.
          05 Z1 PIC X(2) VALUE "z1".
       01 G2.
          05 E2 OCCURS DYNAMIC CAPACITY IN C2 FROM 1.
             10 IB PIC X OCCURS 2.
             10 KB PIC X.
          05 Z2 PIC X(2) VALUE "z2".
       PROCEDURE DIVISION.
       MAIN.
           MOVE "p" TO IA (1, 1)
           MOVE "q" TO IA (1, 2)
           MOVE "k" TO KA (1)
           MOVE "r" TO IA (2, 1)
           MOVE "m" TO KA (2)
           DISPLAY "G1=[" G1 "]"
           MOVE G1 TO G2
           DISPLAY "G2=[" G2 "]"
           IF G1 = G2 DISPLAY "EQ" ELSE DISPLAY "NE" END-IF
           MOVE "s" TO IB (2, 2)
           IF G1 = G2 DISPLAY "EQ" ELSE DISPLAY "NE" END-IF
           MOVE G2 TO G1
           DISPLAY "G1=[" G1 "]"
           IF G1 = G2 DISPLAY "EQ" ELSE DISPLAY "NE" END-IF
           STOP RUN.
