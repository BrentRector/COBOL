      *> kb/Work PB2280 - ISO 1989:2023 14.8.2.2: "If either the formal
      *> parameter or the argument is a variable length group, the
      *> formal parameter and the argument shall be compatible, as
      *> described in 8.5.1.12" - and 8.5.1.12 constrains only where
      *> the variable-length items lie, so two compatible groups may
      *> differ in their fixed material, their tables and their tail.
      *> 14.2.3 GR8: BY REFERENCE "the activated runtime element
      *> operates as if the formal parameter occupies the same storage
      *> area as the argument" - a store through the formal reaches
      *> only the argument storage it overlays. 8.5.1.12.3: a fixed
      *> table opposite a dynamic-capacity table corresponds element by
      *> element. 14.8.3.2: the same compatibility for a RETURNING pair.
      *> cite.py --check 14.8.2.2 "the formal parameter and the argument
      *>   shall be compatible, as described in 8.5.1.12" -> OK 14.8.2.2
      *> cite.py --check 14.2.3 "operates as if the formal parameter
      *>   occupies the same storage area as the argument" -> OK
      *>   14.2.3 8)
      *> cite.py --check 14.8.3.2 "the sending operand and the receiving
      *>   operand shall be compatible" -> OK 14.8.3.2
      *> The INVOKE lanes convert such a pair (golden
      *> 2014/pb480_universal_variable_length_shapes); the CALL lane
      *> passed the argument's carrier through as it was, so G1 printed
      *> HH/Q/TT (the tail cut on write-back) and P31VB read its table
      *> from the wrong storage (TB:hh/      /AABBCC) and lost D2.
      *> DERIVATION: G1 = HH + "dyn" + TTTTT opposite LA = X(2) + dyn +
      *>   X(2): LA sees HH/dyn/TT; "Q" into AD -> G1 = HH/Q/TTTTT.
      *>   G2 = hh + T2 (capacity 3: AA BB CC) + "xyz" opposite LB =
      *>   X(2) + X(2) OCCURS 3 + dyn: LB sees hh/AABBCC/xyz; "ZZ" into
      *>   BT(2) is T2(2) -> G2 = hh/AAZZCC/xyz. BY CONTENT the same
      *>   view of G1, and the store into AD never reaches G1. RETURNING
      *>   RR = rr + "ret" + zz into G3 = X(2) + dyn + X(4): the
      *>   receiver's tail past the sender's is space filled -> rr/ret/
      *>   "zz  ".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2280M14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 H1 PIC X(2) VALUE "HH".
          05 D1 PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 T1 PIC X(5) VALUE "TTTTT".
       01 G2.
          05 H2 PIC X(2) VALUE "hh".
          05 T2 PIC X(2) OCCURS DYNAMIC CAPACITY IN C2 FROM 1 TO 5.
          05 D2 PIC X DYNAMIC LENGTH LIMIT IS 10.
       01 G3.
          05 H3 PIC X(2) VALUE "..".
          05 D3 PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 T3 PIC X(4) VALUE "WWWW".
       PROCEDURE DIVISION.
           MOVE "dyn" TO D1
           CALL "P2280A14" AS NESTED USING G1
           DISPLAY "G1=" H1 "/" D1 "/" T1
           SET C2 TO 3
           MOVE "AA" TO T2(1) MOVE "BB" TO T2(2) MOVE "CC" TO T2(3)
           MOVE "xyz" TO D2
           CALL "P2280B14" AS NESTED USING G2
           DISPLAY "G2=" H2 "/" T2(1) T2(2) T2(3) "/" D2
           CALL "P2280A14" AS NESTED USING BY CONTENT G1
           DISPLAY "G1=" H1 "/" D1 "/" T1
           CALL "P2280R14" AS NESTED RETURNING G3
           DISPLAY "G3=" H3 "/" D3 "/" T3 "."
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2280A14.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA.
          05 AH PIC X(2).
          05 AD PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 AX PIC X(2).
       PROCEDURE DIVISION USING LA.
           DISPLAY "TA:" AH "/" AD "/" AX
           MOVE "Q" TO AD.
       END PROGRAM P2280A14.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2280B14.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB.
          05 BH PIC X(2).
          05 BT PIC X(2) OCCURS 3.
          05 BD PIC X DYNAMIC LENGTH LIMIT IS 10.
       PROCEDURE DIVISION USING LB.
           DISPLAY "TB:" BH "/" BT(1) BT(2) BT(3) "/" BD
           MOVE "ZZ" TO BT(2).
       END PROGRAM P2280B14.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2280R14.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RR.
          05 RRH PIC X(2).
          05 RRD PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 RRT PIC X(2).
       PROCEDURE DIVISION RETURNING RR.
           MOVE "rr" TO RRH
           MOVE "ret" TO RRD
           MOVE "zz" TO RRT.
       END PROGRAM P2280R14.
       END PROGRAM P2280M14.
