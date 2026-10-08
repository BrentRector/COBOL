      *> kb/Work PB2496 - A DYNAMIC-CAPACITY TABLE WHOSE ELEMENTS ARE
      *> THEMSELVES VARIABLE-LENGTH GROUPS, as a whole-group MOVE and
      *> comparison operand (and two compatible groups of DIFFERENT shapes).
      *>
      *> 8.5.1.12.1: a group with "at least one dynamic-length elementary item
      *> or dynamic-capacity table as a subordinate item" is variable-length,
      *> and a table element above the item does not exempt it, so every
      *> occurrence of TD below is a variable-length group of its own. Such a
      *> group "may not undergo a comparison or a move operation ... unless the
      *> other operand is a compatible group"; G2 and G3 are (8.5.1.12.3:
      *> "Two corresponding tables match when the byte length of their elements
      *> is equal and their elements are compatible"). 14.6.9.2 then defines the
      *> move: the receiving table is recreated with a copy of the sending table
      *> and "Correspondingly numbered elements are moved according to the rules
      *> of the MOVE statement"; 14.6.9.3 compares the tables element by element
      *> "until either inequality is detected or the last element of the table
      *> with the smallest current capacity has been compared", then "each
      *> successive remaining element of the larger table with spaces".
      *> 8.8.4.2.17 continues "with the next data item in each of the compatible
      *> groups". DISPLAY is the A.1 item 57 format (14.9.11.4 GR7).
      *>
      *> EXPECTED VALUES, DERIVED:
      *>   A1 G2 = h + (ab,1) + (empty,space) + (cde,3) + zz; occurrence 2 is
      *>      created by the reference to 3 (8.5.1.9.3)  = hab1 cde3zz, cap 3
      *>   A2 MOVE G2 TO G3: every element moved, capacity 3   = same, cap 3
      *>   A3 G2 = G3                                          = EQ
      *>   A4 DD of G3 (1) := "ac" > "ab"                      = LT
      *>   A5 G3 (4) created, all spaces: compared with spaces = EQ
      *>   A6 FX of G3 (4) := "x" > space                      = LT
      *>   B1 A: (a1, IA p q) (a2, IA r) za                    = a1pqa2rza, cap 2
      *>   B2 MOVE A TO B: IB has ONE occurrence, so IA's superfluous "q" is not
      *>      moved (14.6.9.2 rule 1); ZB is fixed material   = a1pa2rza
      *>   B3 MOVE B TO A: IA recreated from IB's one occurrence = a1pa2rza
      *>   B4 A = B                                            = EQ
      *>   C1 MOVE F TO V: F's fixed table of fixed elements corresponds to TV
      *>      (8.5.1.12.3 sentence 3: treated as a dynamic-capacity table of
      *>      its fixed number of occurrences) = (11, a) (22, b) = 11a22b, cap 2
      *>   C2 IV (2, 1) := "x"; MOVE V TO F                    = 11a22x
      *>   C3 F = V                                            = EQ
      *>   D1 S1: a dynamic table (cap 1, "a") opposite a FIXED 2-occurrence
      *>      table in a group of another shape: the occurrence moves and the
      *>      remaining one is space filled (14.6.9.2 rule 2)  = xa dd
      *>   D2 back: the fixed table's two occurrences recreate the dynamic one
      *>                                                       = xpqzz, cap 2
      *>   D3 S1 = S2                                          = EQ
      *>   E1 the same moves over an EXTERNAL (cell-backed) group = wa12b3
      *>   E2 X1 = W1 = EQ; XI (2, 2) := "9" makes X1 greater  = GT
      *>   E3 MOVE X1 TO W1                                    = wa12b39
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2496DET.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-C PIC 9.
       01 G2.
          05 H PIC X VALUE "h".
          05 TD OCCURS DYNAMIC CAPACITY IN CAP2 FROM 1.
             10 DD PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX PIC X.
          05 Z PIC X DYNAMIC LENGTH LIMIT 4.
       01 G3.
          05 H PIC X VALUE "k".
          05 TD OCCURS DYNAMIC CAPACITY IN CAP3 FROM 1.
             10 DD PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX PIC X.
          05 Z PIC X DYNAMIC LENGTH LIMIT 4.
       01 A.
          05 TA OCCURS DYNAMIC CAPACITY IN CA FROM 1.
             10 DA PIC X DYNAMIC LENGTH LIMIT 5.
             10 IA PIC X OCCURS DYNAMIC CAPACITY IN CIA FROM 1.
          05 ZA PIC X(2) VALUE "za".
       01 B.
          05 TB OCCURS DYNAMIC CAPACITY IN CB FROM 1.
             10 DB PIC X DYNAMIC LENGTH LIMIT 5.
             10 IB PIC X OCCURS 1.
          05 ZB PIC X(2) VALUE "zb".
       01 F.
          05 TF OCCURS 2.
             10 XF PIC X(2).
             10 IF2 PIC X OCCURS 1.
       01 V.
          05 TV OCCURS DYNAMIC CAPACITY IN CV FROM 1.
             10 XV PIC X(2).
             10 IV PIC X OCCURS DYNAMIC CAPACITY IN CIV FROM 1.
       01 S1.
          05 X PIC X VALUE "x".
          05 T OCCURS DYNAMIC CAPACITY IN C1 FROM 1.
             10 E PIC X.
          05 D PIC X DYNAMIC LENGTH LIMIT 5.
       01 S2.
          05 X PIC X VALUE "y".
          05 T OCCURS 2.
             10 E PIC X.
          05 D PIC X DYNAMIC LENGTH LIMIT 5.
       01 X1 EXTERNAL.
          05 XH PIC X.
          05 XT OCCURS DYNAMIC CAPACITY IN XC FROM 1.
             10 XA PIC X.
             10 XI PIC X OCCURS DYNAMIC CAPACITY IN XIC FROM 1.
       01 W1.
          05 WH PIC X VALUE "w".
          05 WT OCCURS DYNAMIC CAPACITY IN WC FROM 1.
             10 WA PIC X.
             10 WI PIC X OCCURS DYNAMIC CAPACITY IN WIC FROM 1.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ab" TO DD OF G2 (1)
           MOVE "1" TO FX OF G2 (1)
           MOVE "cde" TO DD OF G2 (3)
           MOVE "3" TO FX OF G2 (3)
           MOVE "zz" TO Z OF G2
           MOVE CAP2 TO WS-C
           DISPLAY "A1=" WS-C " [" G2 "]"
           MOVE G2 TO G3
           MOVE CAP3 TO WS-C
           DISPLAY "A2=" WS-C " [" G3 "]"
           IF G2 = G3 DISPLAY "A3=EQ" ELSE DISPLAY "A3=NE" END-IF
           MOVE "ac" TO DD OF G3 (1)
           IF G2 < G3 DISPLAY "A4=LT" ELSE DISPLAY "A4=GE" END-IF
           MOVE G2 TO G3
           MOVE SPACE TO FX OF G3 (4)
           IF G2 = G3 DISPLAY "A5=EQ" ELSE DISPLAY "A5=NE" END-IF
           MOVE "x" TO FX OF G3 (4)
           IF G2 < G3 DISPLAY "A6=LT" ELSE DISPLAY "A6=GE" END-IF
           MOVE "a1" TO DA (1)
           MOVE "p" TO IA (1, 1)
           MOVE "q" TO IA (1, 2)
           MOVE "a2" TO DA (2)
           MOVE "r" TO IA (2, 1)
           MOVE CA TO WS-C
           DISPLAY "B1=" WS-C " [" A "]"
           MOVE A TO B
           DISPLAY "B2=[" B "]"
           MOVE B TO A
           DISPLAY "B3=[" A "]"
           IF A = B DISPLAY "B4=EQ" ELSE DISPLAY "B4=NE" END-IF
           MOVE "11a22b" TO F
           MOVE F TO V
           MOVE CV TO WS-C
           DISPLAY "C1=" WS-C " [" V "]"
           MOVE "x" TO IV (2, 1)
           MOVE V TO F
           DISPLAY "C2=[" F "]"
           IF F = V DISPLAY "C3=EQ" ELSE DISPLAY "C3=NE" END-IF
           MOVE "a" TO E OF S1 (1)
           MOVE "dd" TO D OF S1
           MOVE S1 TO S2
           DISPLAY "D1=[" S2 "]"
           MOVE "p" TO E OF S2 (1)
           MOVE "q" TO E OF S2 (2)
           MOVE "zz" TO D OF S2
           MOVE S2 TO S1
           MOVE C1 TO WS-C
           DISPLAY "D2=" WS-C " [" S1 "]"
           IF S1 = S2 DISPLAY "D3=EQ" ELSE DISPLAY "D3=NE" END-IF
           MOVE "a" TO WA (1)
           MOVE "1" TO WI (1, 1)
           MOVE "2" TO WI (1, 2)
           MOVE "b" TO WA (2)
           MOVE "3" TO WI (2, 1)
           MOVE W1 TO X1
           DISPLAY "E1=[" X1 "]"
           IF X1 = W1 DISPLAY "E2=EQ" ELSE DISPLAY "E2=NE" END-IF
           MOVE "9" TO XI (2, 2)
           IF X1 > W1 DISPLAY "E2=GT" ELSE DISPLAY "E2=LE" END-IF
           MOVE X1 TO W1
           DISPLAY "E3=[" W1 "]"
           STOP RUN.
