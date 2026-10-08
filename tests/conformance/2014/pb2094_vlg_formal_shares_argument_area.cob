      *> kb/Work PB2094 - a VARIABLE-LENGTH group formal (a dynamic-length
      *> item or a dynamic-capacity table subordinate) occupies its
      *> argument's storage area like any other group formal.
      *> cite.py --check 14.2.3 "operates as if the formal parameter
      *>   occupies the same storage area as the argument" -> OK 14.2.3 8)
      *> cite.py --check 14.8.2.2 "the formal parameter and the argument
      *>   shall be compatible, as described in 8.5.1.12" -> OK 14.8.2.2 2)
      *> cite.py --check 8.5.1.9.3 "the capacity of the table is
      *>   increased to the value given by the subscript" -> OK 8.5.1.9.3
      *> cite.py --check 14.2.3 "allocated by the activating runtime
      *>   element" -> OK 14.2.3 9)
      *> The same argument passed twice BY REFERENCE is ONE storage area,
      *> so a store through the first formal is seen through the second
      *> while the program is active, and nothing is copied back over it
      *> at return. Before the fix each formal held a copy: the second
      *> formal showed the old value and its copy-back undid the store.
      *> DERIVATION:
      *>   A  V = H + "dd" + table a,b (capacity 2), passed twice. A
      *>      stores Q, "new" and Z into element 2 through L1 and shows
      *>      L2: A:Q/new/aZ; V is then Q/new/aZ.
      *>   B  RG(2) of R, after RX and RG(1) (whose components come first
      *>      in R's storage), passed twice: "q" and "deep" through LB1
      *>      show through LB2 - B:q/deep - and R keeps RX and RG(1):
      *>      R=r0/1/g1/q/deep.
      *>   C  W = ww + dyn + TAIL against LC = XX + dyn + XX (a shorter
      *>      tail, compatible under 8.5.1.12.2): LC lies over W's first
      *>      positions, so "dyn" and "zz" through LC1 show through LC2 -
      *>      C:ww/dyn/zz - and W=ww/dyn/zzIL.
      *>   D  the INVOKE lane: M stores m, "meth" and Y into element 3 of
      *>      the first formal (8.5.1.9.3: capacity becomes 3) and shows
      *>      the second: D:m/meth/aZY; V=m/meth/aZY.
      *>   E  BY CONTENT V twice: each formal is its own record (GR9), so
      *>      L2 shows V as it was - A:m/meth/aZ - and V is unchanged.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2094M14.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C2094V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U USAGE OBJECT REFERENCE C2094V.
       01 V.
          05 VH PIC X VALUE "H".
          05 VD PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 VT PIC X OCCURS DYNAMIC CAPACITY IN VC FROM 1 TO 5.
       01 R.
          05 RX PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 RG OCCURS 2.
             10 GH PIC X.
             10 GD PIC X DYNAMIC LENGTH LIMIT IS 10.
       01 W.
          05 WH PIC X(2) VALUE "ww".
          05 WD PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 WT PIC X(4) VALUE "TAIL".
       PROCEDURE DIVISION.
           MOVE "dd" TO VD
           SET VC TO 2
           MOVE "a" TO VT(1)
           MOVE "b" TO VT(2)
           CALL "P2094A" AS NESTED USING V V
           DISPLAY "V=" VH "/" VD "/" VT(1) VT(2)
           MOVE "r0" TO RX
           MOVE "1" TO GH(1)
           MOVE "g1" TO GD(1)
           MOVE "2" TO GH(2)
           MOVE "g2" TO GD(2)
           CALL "P2094B" AS NESTED USING RG(2) RG(2)
           DISPLAY "R=" RX "/" GH(1) "/" GD(1) "/" GH(2) "/" GD(2)
           CALL "P2094C" AS NESTED USING W W
           DISPLAY "W=" WH "/" WD "/" WT
           INVOKE C2094V "NEW" RETURNING U
           INVOKE U "M" USING V V
           DISPLAY "V=" VH "/" VD "/" VT(1) VT(2) VT(3)
           CALL "P2094A" AS NESTED USING BY CONTENT V BY CONTENT V
           DISPLAY "V=" VH "/" VD "/" VT(1) VT(2) VT(3)
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2094A.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1.
          05 H1 PIC X.
          05 D1 PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 T1 PIC X OCCURS DYNAMIC CAPACITY IN C1 FROM 1 TO 5.
       01 L2.
          05 H2 PIC X.
          05 D2 PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 T2 PIC X OCCURS DYNAMIC CAPACITY IN C2 FROM 1 TO 5.
       PROCEDURE DIVISION USING L1 L2.
           MOVE "Q" TO H1
           MOVE "new" TO D1
           MOVE "Z" TO T1(2)
           DISPLAY "A:" H2 "/" D2 "/" T2(1) T2(2).
       END PROGRAM P2094A.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2094B.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB1.
          05 BH1 PIC X.
          05 BD1 PIC X DYNAMIC LENGTH LIMIT IS 10.
       01 LB2.
          05 BH2 PIC X.
          05 BD2 PIC X DYNAMIC LENGTH LIMIT IS 10.
       PROCEDURE DIVISION USING LB1 LB2.
           MOVE "q" TO BH1
           MOVE "deep" TO BD1
           DISPLAY "B:" BH2 "/" BD2.
       END PROGRAM P2094B.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2094C.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LC1.
          05 CH1 PIC X(2).
          05 CD1 PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 CX1 PIC X(2).
       01 LC2.
          05 CH2 PIC X(2).
          05 CD2 PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 CX2 PIC X(2).
       PROCEDURE DIVISION USING LC1 LC2.
           MOVE "dyn" TO CD1
           MOVE "zz" TO CX1
           DISPLAY "C:" CH2 "/" CD2 "/" CX2.
       END PROGRAM P2094C.
       END PROGRAM P2094M14.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2094V INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01 M1.
          05 MH1 PIC X.
          05 MD1 PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 MT1 PIC X OCCURS DYNAMIC CAPACITY IN MC1 FROM 1 TO 5.
       01 M2.
          05 MH2 PIC X.
          05 MD2 PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 MT2 PIC X OCCURS DYNAMIC CAPACITY IN MC2 FROM 1 TO 5.
       PROCEDURE DIVISION USING M1 M2.
           MOVE "m" TO MH1
           MOVE "meth" TO MD1
           MOVE "Y" TO MT1(3)
           DISPLAY "D:" MH2 "/" MD2 "/" MT2(1) MT2(2) MT2(3).
       END METHOD M.
       END OBJECT.
       END CLASS C2094V.
