      *> kb/Work PB244 shape (b) - DISPLAY of a VARIABLE-LENGTH GROUP that
      *> combines two variable-length mechanisms, and of one whose dynamic-length
      *> item sits INSIDE a fixed OCCURS table.
      *>
      *> 14.9.11.4 GR7: "If identifier-1 references a variable-length group,
      *> the format in which its contents are displayed is defined by the
      *> implementor" - docs/CONFORMANCE.md DOC-A.1-57. OURS: the members'
      *> images in declaration order, each dynamic-length item at its CURRENT
      *> content and each OCCURS DEPENDING table at its CURRENT count (13.18.38.4
      *> GR8 - a group holding such a table is an "occurs-depending group item"
      *> and only the part the DEPENDING item names is used). 13.18.38.3 SR22
      *> makes the table the trailing storage of its record, so the current image
      *> is the prefix of the maximum one. 8.5.1.12.1: a variable-length group is
      *> one with "at least one dynamic-length elementary item ... as a subordinate
      *> item" - a table element above the item does not exempt it, so every
      *> occurrence of TE below is its own dynamic-length member.
      *>
      *> EXPECTED VALUES, DERIVED (15.50.4 r7: LENGTH of a variable-length group
      *> is the sum of the fixed parts, each dynamic-length item's current length
      *> and the occurs-depending table's current extent, so it EQUALS the
      *> displayed width):
      *>   A1 K=2: H "H" + D "abc" + T(1) "x" + T(2) "y"        = Habcxy     6
      *>   A2 K=3 after T(3)="z": the third occurrence joins     = Habcxyz    7
      *>   A3 K=1: only the first occurrence is used              = Habcx      5
      *>   B1 H "h" + DD(1) "ab" + FX(1) "1" + DD(2) "c" + FX(2) "2" = hab1c2 6
      *>   B2 DD(1) becomes "zzz"                                  = hzzz1c2    7
      *>   B3 MOVE to the same shape: a compatible variable-length pair moves
      *>      component by component (14.9.25.4 GR9, 8.5.1.12.1) and compares EQ;
      *>      changing one dynamic item makes it NE.
      *>   C1 nested group SUB holds the table: D3 "q" + P3 "PP" + T3(1) "m" = qPPm  4
      *>   C2 K=3 with T3(2)="n" T3(3)="o"                        = qPPmno     6
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244VLB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 VALUE 2.
       01 G1.
          05 H PIC X VALUE "H".
          05 D PIC X DYNAMIC LENGTH LIMIT 5.
          05 T PIC X OCCURS 1 TO 3 DEPENDING ON K.
       01 G2.
          05 H PIC X VALUE "h".
          05 TE OCCURS 2.
             10 DD PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX PIC X.
       01 G2B.
          05 H PIC X VALUE "-".
          05 TE OCCURS 2.
             10 DD PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX PIC X.
       01 G3.
          05 D3 PIC X DYNAMIC LENGTH LIMIT 5.
          05 SUB.
             10 P3 PIC X(2).
             10 T3 PIC X OCCURS 1 TO 3 DEPENDING ON K.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "abc" TO D OF G1
           MOVE "x" TO T OF G1(1)
           MOVE "y" TO T OF G1(2)
           DISPLAY "A1=[" G1 "] " FUNCTION LENGTH(G1)
           MOVE 3 TO K
           MOVE "z" TO T OF G1(3)
           DISPLAY "A2=[" G1 "] " FUNCTION LENGTH(G1)
           MOVE 1 TO K
           DISPLAY "A3=[" G1 "] " FUNCTION LENGTH(G1)
           MOVE "ab" TO DD OF G2(1)
           MOVE "c" TO DD OF G2(2)
           MOVE "1" TO FX OF G2(1)
           MOVE "2" TO FX OF G2(2)
           DISPLAY "B1=[" G2 "] " FUNCTION LENGTH(G2)
           MOVE "zzz" TO DD OF G2(1)
           DISPLAY "B2=[" G2 "] " FUNCTION LENGTH(G2)
           MOVE G2 TO G2B
           DISPLAY "B3=[" G2B "] " FUNCTION LENGTH(G2B)
           IF G2 = G2B DISPLAY "B3 EQ" ELSE DISPLAY "B3 NE" END-IF
           MOVE "Y" TO DD OF G2B(2)
           IF G2 = G2B DISPLAY "B4 EQ" ELSE DISPLAY "B4 NE" END-IF
           MOVE "q" TO D3
           MOVE "PP" TO P3
           MOVE "m" TO T3(1)
           DISPLAY "C1=[" G3 "] " FUNCTION LENGTH(G3)
           MOVE 3 TO K
           MOVE "n" TO T3(2)
           MOVE "o" TO T3(3)
           DISPLAY "C2=[" G3 "] " FUNCTION LENGTH(G3)
           STOP RUN.
