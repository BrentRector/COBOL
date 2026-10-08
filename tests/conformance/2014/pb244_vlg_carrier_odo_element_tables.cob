      *> kb/Work PB244 - the COMPONENT-CARRIER statements over a VARIABLE-LENGTH GROUP that
      *> holds an OCCURS DEPENDING table whose ELEMENTS are variable-length groups: the
      *> group MOVE, the relation condition, a Format-2 CALL boundary and INITIALIZE, for a
      *> declared group and for a cell-backed (EXTERNAL) one.
      *>
      *> 8.5.1.12.1: a group with "at least one dynamic-length elementary item ... as a
      *> subordinate item" is a variable-length group, and a table element above the item
      *> does not exempt it - every occurrence is a variable-length group of its own.
      *> 14.9.25.4 GR9: a MOVE of such a group moves the common part and, where the
      *> sending group is the shorter, "each location that occupies the excess part is
      *> space filled": a dynamic-length item gets length zero (GR9b 1) and every other
      *> character position a space (GR9b 3). 8.8.4.2.17: the relation proceeds from left
      *> to right, a table pair as 14.6.9.3 (the shorter table's missing elements are
      *> compared with spaces). 14.8.2.2: an OCCURS DEPENDING group crosses by reference
      *> at its MAXIMUM length, and 14.2.3 GR8 makes the formal occupy the argument's
      *> storage. 14.9.20.4: INITIALIZE gives every alphanumeric elementary item the
      *> figurative constant SPACES and every numeric one ZERO (its Table of defaults), a
      *> dynamic-length item ending at length zero (kb/Work PB393's determination).
      *>
      *> EXPECTED VALUES, DERIVED (K = 2, K2 = 3 unless a MOVE says otherwise; an element is its
      *> dynamic-length item, FX and NN, so the third occurrence's excess part is "" + " " + "  "):
      *>   A1 G1 = h + abc + pp + (x,1,11) (yy,2,22)                = habcppx111yy222
      *>   A2 MOVE G1 TO G2, whose third occurrence held (QQ,9,99): the two sent
      *>      occurrences land, the third is the excess part, space filled  = habcppx111yy222 + 3 blanks
      *>   A3 K = 3: the third occurrence (zzz,3,33) is sent too            = habcppx111yy222zzz333
      *>   B1 G1 = G2 (same content)                                       = EQ
      *>   B2 after DD2(2) = "w": the first difference is "yy" v "w"        = NE, and GT ("y" follows "w")
      *>   C1 MOVE G1 TO G3 (a fixed table of 3, K = 2)                     = habcppx111yy222 + 3 blanks
      *>   C2 G1 = G3 at K = 2: G1's missing third element is compared with
      *>      spaces, and G3's third element is empty and blank             = EQ
      *>   S  the callee sees G1 at the count K names                       = habcppx111yy222
      *>   D1 CALL by reference: the callee stores D "QQ", FX(2) "9", raises
      *>      its count (the DEPENDING item crosses too) to 3 and stores
      *>      (rrr,8) - 14.2.3 GR8, the formal occupies the argument           = hQQppx111yy922rrr833 3
      *>   D2 BY CONTENT (count 2): the callee sees the result, its stores
      *>      reach a copy, the caller still shows                          = hQQppx111yy922 2
      *>   A4 GR8 a): DEPENDING item K2 is OUTSIDE G2, so a MOVE into G2 uses only
      *>      "that part of the table area that is specified by the value of the data
      *>      item ... at the start of the operation" - with K2 = 1 the first occurrence
      *>      only; occurrences 2 and 3 (marked uu/u/77 and vv/v/88) keep their content
      *>                                                                 = habcppx111uuu77vvv88
      *>   F1 the same MOVE into the cell-backed GX, KX = 1: the group shows
      *>      the one occurrence KX names                                  = hQQppx111
      *>   F2 KX = 3: occurrence 1 is the sent one, 2 and 3 kept their markers
      *>                                                                 = hQQppx111uuu77vvv88
      *>   F3 MOVE G1 TO GX with KX = 3: the two sent occurrences and the excess
      *>      third (G1 is at K = 2)                                         = hQQppx111yy922 + 3 blanks
      *>   F4 G1 = GX                                                      = EQ
      *>   F5 DX = "m", MOVE GX TO G1 (K = 2 uses two occurrences)           = hmppx111yy922
      *>   E1 INITIALIZE G1 (K = 2): H and P blank, D length zero, each used
      *>      occurrence (length zero, FX blank, NN 00)                     = 4 blanks + 00 + blank + 00
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244CAR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 VALUE 2.
       01 K2 PIC 9 VALUE 3.
       01 KX PIC 9 EXTERNAL.
       01 G1.
          05 H PIC X VALUE "h".
          05 D PIC X DYNAMIC LENGTH LIMIT 5.
          05 SUB.
             10 P PIC XX VALUE "pp".
             10 TE OCCURS 1 TO 3 DEPENDING ON K.
                15 DD PIC X DYNAMIC LENGTH LIMIT 5.
                15 FX PIC X.
                15 NN PIC 99.
       01 G2.
          05 H2 PIC X VALUE "g".
          05 D2 PIC X DYNAMIC LENGTH LIMIT 5.
          05 SUB2.
             10 P2 PIC XX VALUE "qq".
             10 TE2 OCCURS 1 TO 3 DEPENDING ON K2.
                15 DD2 PIC X DYNAMIC LENGTH LIMIT 5.
                15 FX2 PIC X.
                15 NN2 PIC 99.
       01 G3.
          05 H3 PIC X VALUE "g".
          05 D3 PIC X DYNAMIC LENGTH LIMIT 5.
          05 SUB3.
             10 P3 PIC XX VALUE "qq".
             10 TE3 OCCURS 3.
                15 DD3 PIC X DYNAMIC LENGTH LIMIT 5.
                15 FX3 PIC X.
                15 NN3 PIC 99.
       01 GX EXTERNAL.
          05 HX PIC X.
          05 DX PIC X DYNAMIC LENGTH LIMIT 5.
          05 SUBX.
             10 PX PIC XX.
             10 TEX OCCURS 1 TO 3 DEPENDING ON KX.
                15 DDX PIC X DYNAMIC LENGTH LIMIT 5.
                15 FXX PIC X.
                15 NNX PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "abc" TO D
           MOVE "x" TO DD(1)
           MOVE "yy" TO DD(2)
           MOVE "zzz" TO DD(3)
           MOVE "1" TO FX(1)
           MOVE "2" TO FX(2)
           MOVE "3" TO FX(3)
           MOVE 11 TO NN(1)
           MOVE 22 TO NN(2)
           MOVE 33 TO NN(3)
           DISPLAY "A1=[" G1 "]"
           MOVE "QQ" TO DD2(3)
           MOVE "9" TO FX2(3)
           MOVE 99 TO NN2(3)
           MOVE G1 TO G2
           DISPLAY "A2=[" G2 "]"
           MOVE 3 TO K
           MOVE G1 TO G2
           DISPLAY "A3=[" G2 "]"
           IF G1 = G2 DISPLAY "B1 EQ" ELSE DISPLAY "B1 NE" END-IF
           MOVE "w" TO DD2(2)
           IF G1 = G2 DISPLAY "B2 EQ" ELSE DISPLAY "B2 NE" END-IF
           IF G1 > G2 DISPLAY "B2 GT" ELSE DISPLAY "B2 NOT GT" END-IF
           MOVE "uu" TO DD2(2)
           MOVE "u" TO FX2(2)
           MOVE 77 TO NN2(2)
           MOVE "vv" TO DD2(3)
           MOVE "v" TO FX2(3)
           MOVE 88 TO NN2(3)
           MOVE 1 TO K2
           MOVE G1 TO G2
           MOVE 3 TO K2
           DISPLAY "A4=[" G2 "]"
           MOVE 2 TO K
           MOVE G1 TO G3
           DISPLAY "C1=[" G3 "]"
           IF G1 = G3 DISPLAY "C2 EQ" ELSE DISPLAY "C2 NE" END-IF
           CALL "PB244CAS" AS NESTED USING G1 K
           MOVE 3 TO K
           DISPLAY "D1=[" G1 "] " K
           MOVE 2 TO K
           CALL "PB244CAS" AS NESTED USING BY CONTENT G1 BY CONTENT K
           DISPLAY "D2=[" G1 "] " K
           MOVE 3 TO KX
           MOVE "uu" TO DDX(2)
           MOVE "u" TO FXX(2)
           MOVE 77 TO NNX(2)
           MOVE "vv" TO DDX(3)
           MOVE "v" TO FXX(3)
           MOVE 88 TO NNX(3)
           MOVE 1 TO KX
           MOVE G1 TO GX
           DISPLAY "F1=[" GX "] " KX
           MOVE 3 TO KX
           DISPLAY "F2=[" GX "]"
           MOVE G1 TO GX
           DISPLAY "F3=[" GX "]"
           IF G1 = GX DISPLAY "F4 EQ" ELSE DISPLAY "F4 NE" END-IF
           MOVE "m" TO DX
           MOVE GX TO G1
           DISPLAY "F5=[" G1 "]"
           INITIALIZE G1
           DISPLAY "E1=[" G1 "]"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244CAS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK PIC 9.
       01 LG.
          05 LH PIC X.
          05 LD PIC X DYNAMIC LENGTH LIMIT 5.
          05 LSUB.
             10 LP PIC XX.
             10 LTE OCCURS 1 TO 3 DEPENDING ON LK.
                15 LDD PIC X DYNAMIC LENGTH LIMIT 5.
                15 LFX PIC X.
                15 LNN PIC 99.
       PROCEDURE DIVISION USING LG LK.
       SA.
           DISPLAY "S=[" LG "]"
           MOVE "QQ" TO LD
           MOVE "9" TO LFX(2)
           MOVE 3 TO LK
           MOVE "rrr" TO LDD(3)
           MOVE "8" TO LFX(3)
           GOBACK.
       END PROGRAM PB244CAS.
       END PROGRAM PB244CAR.
