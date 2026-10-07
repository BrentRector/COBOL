      *> kb/Work PB2076 - 9.3.6 match rules 6 and 7 for the RETURNING
      *> item of an INVOKE through a UNIVERSAL object reference. Rule 7
      *> (cite.py --check 9.3.6 -> OK 9.3.6 7)): a returning item not of
      *> usage OBJECT REFERENCE, POINTER or INDEX needs "a corresponding
      *> specification in the invoked method that may be a sending item in
      *> a MOVE statement with the returning item as the receiving item" -
      *> the MOVE's VALIDITY by 14.9.25.3 (SR2 OK 14.9.25.3 2), SR8 OK
      *> 14.9.25.3 8), SR10 + Table 16 OK 14.9.25.3 10)). No match: the
      *> search goes up the INHERITS chain and "6) otherwise, the
      *> EC-OO-METHOD exception condition is set to exist" (OK 9.3.6 6)).
      *> A MOVE-valid pair MATCHES, binds the method, and 14.8.3.3's
      *> "same ... PICTURE ... USAGE clauses" is then GR7 c)'s
      *> conformance check: EC-OO-UNIVERSAL (OK 14.9.23.4 7)).
      *> DERIVATION:
      *>   RN (9V99)     RETURNING RN 9V99  same description   rn=125
      *>   RN (9V99)     RETURNING RX X(4)  Table 16 Numeric
      *>                 Noninteger -> Alphanumeric "No"     EC-OO-METHOD
      *>   RN (9V99)     RETURNING RB 1(4)  Numeric -> Boolean
      *>                 "No"                                EC-OO-METHOD
      *>   RL (BINARY-LONG) RETURNING RX X(4)  SR8: a numeric or
      *>                 numeric-edited receiver only        EC-OO-METHOD
      *>   RL (BINARY-LONG) RETURNING BL BINARY-LONG  same   bl=42
      *>   RL (BINARY-LONG) RETURNING N8 9(8)  SR8 satisfied, Table 16
      *>                 Numeric -> Numeric "Yes": MATCH; 14.8.3.3
      *>                 USAGE/PICTURE differ                EC-OO-UNIVERSAL
      *>   RX (X(4))     RETURNING RN 9V99  Alphanumeric -> Numeric "Yes":
      *>                 MATCH; PICTURE differs              EC-OO-UNIVERSAL
      *>   RX (X(4))     RETURNING SG (TYPE STRONG T2076)  SR2: a strongly
      *>                 typed receiver takes only a group of the same
      *>                 type                                EC-OO-METHOD
      *>   RX (X(4))     RETURNING RX X(4)  same             rx=WXYZ
      *> Rule 6 (OK 9.3.6 6)): a POINTER or INDEX returning item needs one
      *> that "may be a sending item in a SET statement" - 14.9.39.3:
      *>   RP (POINTER)  RETURNING DPU POINTER  SR17          dpu=ok
      *>   RP (POINTER)  RETURNING DPR (TYPE TPR, POINTER TO T2076)
      *>                 SR19: a restricted receiver takes only a
      *>                 data-pointer restricted to the same type
      *>                 (OK 14.9.39.3 19))                  EC-OO-METHOD
      *>   RI (INDEX)    RETURNING IX INDEX  SR2 class index  ix=ok (no EC)
      *>   RI (INDEX)    RETURNING N8 9(8)  rule 7, and 14.9.25.3 SR1:
      *>                 no MOVE operand of class index      EC-OO-METHOD
      *> A matched-and-bound method runs; one that fails 14.8.3.3 is
      *> stopped by EC-OO-UNIVERSAL BEFORE it runs, so the receiving item
      *> keeps its value: rn=125 after RX RETURNING RN.
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2076M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS C2076M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T2076 TYPEDEF STRONG.
          05 T2076-F PIC X(4).
       01 U  USAGE OBJECT REFERENCE.
       01 RX PIC X(4).
       01 RN PIC 9V99.
       01 RB PIC 1(4).
       01 BL USAGE BINARY-LONG.
       01 N8 PIC 9(8).
       01 SG TYPE T2076.
       01 TPR TYPEDEF USAGE POINTER TO T2076.
       01 DPR TYPE TPR.
       01 DPU USAGE POINTER.
       01 IX USAGE INDEX.
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
           INVOKE C2076M "NEW" RETURNING U
           INVOKE U "RN" RETURNING RN
           DISPLAY "rn=" RN
           INVOKE U "RN" RETURNING RX
           INVOKE U "RN" RETURNING RB
           INVOKE U "RL" RETURNING RX
           INVOKE U "RL" RETURNING BL
           IF BL = 42 DISPLAY "bl=42" ELSE DISPLAY "bl=WRONG" END-IF
           INVOKE U "RL" RETURNING N8
           INVOKE U "RX" RETURNING RN
           DISPLAY "rn=" RN
           INVOKE U "RX" RETURNING SG
           INVOKE U "RX" RETURNING RX
           DISPLAY "rx=" RX
           INVOKE U "RP" RETURNING DPU
           IF DPU = NULL DISPLAY "dpu=ok"
           ELSE DISPLAY "dpu=WRONG" END-IF
           INVOKE U "RP" RETURNING DPR
           INVOKE U "RI" RETURNING IX
           DISPLAY "ix=ok"
           INVOKE U "RI" RETURNING N8
           DISPLAY "END".
           STOP RUN.
       END PROGRAM PB2076M.

       IDENTIFICATION DIVISION.
       CLASS-ID. C2076M INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. RN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC 9V99.
       PROCEDURE DIVISION RETURNING LN.
           MOVE 1.25 TO LN.
       END METHOD RN.
       METHOD-ID. RL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LL USAGE BINARY-LONG.
       PROCEDURE DIVISION RETURNING LL.
           MOVE 42 TO LL.
       END METHOD RL.
       METHOD-ID. RX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX PIC X(4).
       PROCEDURE DIVISION RETURNING LX.
           MOVE "WXYZ" TO LX.
       END METHOD RX.
       METHOD-ID. RP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP USAGE POINTER.
       PROCEDURE DIVISION RETURNING LP.
           SET LP TO NULL.
       END METHOD RP.
       METHOD-ID. RI.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LI USAGE INDEX.
       PROCEDURE DIVISION RETURNING LI.
           CONTINUE.
       END METHOD RI.
       END OBJECT.
       END CLASS C2076M.
