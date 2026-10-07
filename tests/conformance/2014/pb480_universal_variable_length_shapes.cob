      *> kb/Work PB480 - two VARIABLE-LENGTH groups of DIFFERENT shapes crossing an INVOKE BY REFERENCE, through a
      *> UNIVERSAL object reference (U) and through a typed one (T). 14.8.2.2: "If either the formal parameter or the
      *> argument is a variable length group, the formal parameter and the argument shall be compatible, as described
      *> in 8.5.1.12, Variable-length groups" (cite.py --check 14.8.2.2 -> OK 14.8.2.2 2)); 14.8.3.2 the same for a
      *> returning pair (OK 14.8.3.2); 14.9.23.4 GR7 c) applies both to a universal invocation (OK 14.9.23.4 7)).
      *> 8.5.1.12 constrains only WHERE the variable-length items lie: "Two dynamic-length elementary items correspond
      *> if they start at the same relative byte positions within their groups" (OK 8.5.1.12.2); "Two tables
      *> correspond if at least one of them is a dynamic-capacity table and they occupy the same relative byte
      *> positions within their groups" (OK 8.5.1.12.2); "If one of the corresponding tables is not a dynamic-capacity
      *> table, that table is treated as though it were a dynamic-capacity table" (OK 8.5.1.12.3); a dynamic-capacity
      *> table beyond the shorter group's last character corresponds to "a space-filled fixed-length table" (OK
      *> 8.5.1.12.2). 14.2.3 GR8: the formal "occupies the same storage area as the argument" (OK 14.2.3 8)), so a
      *> store through the formal reaches only the argument storage it overlays. Before the fix the universal lane
      *> compared the two groups' signatures for equality (EC-OO-UNIVERSAL for every pair below), and the typed lane
      *> passed the argument's carrier through unconverted (TB read its fixed table out of the wrong place and the
      *> write-back of TA cut the argument's trailing material to the formal's length).
      *> DERIVATION (U, then T, for each):
      *>   TA  G1 = HH + D1 "dyn" + T1 "TTTTT" into LA = AH X(2) + AD dynamic + AX X(2): both dynamic items start at
      *>       byte 2, so compatible; the formal sees AX = the first 2 of T1. It stores Q into AD and "ax" into AX:
      *>       D1 becomes Q and T1's first 2 characters become ax, the other 3 survive.
      *>         TA:HH/dyn/TT  G1=HH/Q/axTTT
      *>   TB  G2 = hh + T2 dynamic (capacity 3: AA BB CC) + D2 "xyz" into LB = BH X(2) + BT X(2) OCCURS 3 + BD
      *>       dynamic: BT corresponds to T2 (byte 2), its 3 occurrences make T2 6 bytes long (8.5.1.12.3), so BD and D2
      *>       both start at byte 8. It stores ZZ into BT(3).
      *>         TB:hh/AABBCC/xyz  G2=hh/AABBZZ/xyz
      *>       with capacity 2 (AA BB): the formal's third occurrence has no argument occurrence under it, so it reads
      *>       spaces and its store is not reflected (a fixed-occurrence formal cannot re-size the argument's table).
      *>         TB:hh/AABB  /xyz  G2=hh/AABB/xyz C2=2
      *>   TA  G5 = 55 + D5 "d5" + X5 "xx" + T5 dynamic (capacity 2: p q): T5 starts at byte 4, beyond LA's last
      *>       character, so it corresponds to nothing and survives the store.
      *>         TA:55/d5/xx  G5=55/Q/ax/pq
      *>   TA  U only: G3 = HHH + D3 dynamic: D3 starts at byte 3 and LA's AD at byte 2, so the two do not correspond -
      *>       the groups MATCH (9.3.6 3 e): neither carries a clause) and do not CONFORM -> HANDLED=EC-OO-UNIVERSAL
      *>   TR  RETURNING LR = RR + LD "ret" + LZ "Z" into G4 = 2 + dynamic + X(4) "wxyz": the result is moved as
      *>       14.9.25.4 GR9 moves variable-length groups - the receiving group's excess part is space filled (OK
      *>       14.9.25.4 9)).
      *>         G4=RR/ret/Z   |
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB480S.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C480S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U  USAGE OBJECT REFERENCE.
       01 T  USAGE OBJECT REFERENCE C480S.
       01 G1.
          05 H1 PIC X(2) VALUE "HH".
          05 D1 PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 T1 PIC X(5) VALUE "TTTTT".
       01 G2.
          05 H2 PIC X(2) VALUE "hh".
          05 T2 PIC X(2) OCCURS DYNAMIC CAPACITY IN C2 FROM 1 TO 5.
          05 D2 PIC X DYNAMIC LENGTH LIMIT IS 10.
       01 G3.
          05 H3 PIC X(3) VALUE "HHH".
          05 D3 PIC X DYNAMIC LENGTH LIMIT IS 10.
       01 G4.
          05 H4 PIC X(2) VALUE "hh".
          05 D4 PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 T4 PIC X(4) VALUE "wxyz".
       01 G5.
          05 H5 PIC X(2) VALUE "55".
          05 D5 PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 X5 PIC X(2) VALUE "xx".
          05 T5 PIC X OCCURS DYNAMIC CAPACITY IN C5 FROM 1 TO 5.
       01 C2D PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE C480S "NEW" RETURNING T
           SET U TO T
           MOVE "dyn" TO D1
           INVOKE U "TA" USING G1
           DISPLAY "G1=" H1 "/" D1 "/" T1
           MOVE "dyn" TO D1 MOVE "TTTTT" TO T1
           INVOKE T "TA" USING G1
           DISPLAY "G1=" H1 "/" D1 "/" T1
           SET C2 TO 3
           MOVE "AA" TO T2(1) MOVE "BB" TO T2(2) MOVE "CC" TO T2(3)
           MOVE "xyz" TO D2
           INVOKE U "TB" USING G2
           DISPLAY "G2=" H2 "/" T2(1) T2(2) T2(3) "/" D2
           MOVE "CC" TO T2(3)
           INVOKE T "TB" USING G2
           DISPLAY "G2=" H2 "/" T2(1) T2(2) T2(3) "/" D2
           SET C2 TO 2
           INVOKE U "TB" USING G2
           MOVE C2 TO C2D
           DISPLAY "G2=" H2 "/" T2(1) T2(2) "/" D2 " C2=" C2D
           INVOKE T "TB" USING G2
           MOVE C2 TO C2D
           DISPLAY "G2=" H2 "/" T2(1) T2(2) "/" D2 " C2=" C2D
           SET C5 TO 2
           MOVE "p" TO T5(1) MOVE "q" TO T5(2)
           MOVE "d5" TO D5
           INVOKE U "TA" USING G5
           DISPLAY "G5=" H5 "/" D5 "/" X5 "/" T5(1) T5(2)
           MOVE "d5" TO D5 MOVE "xx" TO X5
           INVOKE T "TA" USING G5
           DISPLAY "G5=" H5 "/" D5 "/" X5 "/" T5(1) T5(2)
           MOVE "d3" TO D3
           INVOKE U "TA" USING G3
           MOVE "old" TO D4
           INVOKE U "TR" RETURNING G4
           DISPLAY "G4=" H4 "/" D4 "/" T4 "|"
           MOVE "hh" TO H4 MOVE "old" TO D4 MOVE "wxyz" TO T4
           INVOKE T "TR" RETURNING G4
           DISPLAY "G4=" H4 "/" D4 "/" T4 "|"
           DISPLAY "END".
           STOP RUN.
       END PROGRAM PB480S.

       IDENTIFICATION DIVISION.
       CLASS-ID. C480S INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA.
          05 AH PIC X(2).
          05 AD PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 AX PIC X(2).
       PROCEDURE DIVISION USING LA.
           DISPLAY "TA:" AH "/" AD "/" AX
           MOVE "Q" TO AD
           MOVE "ax" TO AX.
       END METHOD TA.
       METHOD-ID. TB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB.
          05 BH PIC X(2).
          05 BT PIC X(2) OCCURS 3.
          05 BD PIC X DYNAMIC LENGTH LIMIT IS 10.
       PROCEDURE DIVISION USING LB.
           DISPLAY "TB:" BH "/" BT(1) BT(2) BT(3) "/" BD
           MOVE "ZZ" TO BT(3).
       END METHOD TB.
       METHOD-ID. TR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR.
          05 RTH PIC X(2).
          05 RTD PIC X DYNAMIC LENGTH LIMIT IS 10.
          05 RTZ PIC X.
       PROCEDURE DIVISION RETURNING LR.
           MOVE "RR" TO RTH
           MOVE "ret" TO RTD
           MOVE "Z" TO RTZ.
       END METHOD TR.
       END OBJECT.
       END CLASS C480S.
