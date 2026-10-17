*>--- conformance-case
*> id: pb780_constant_names_slot_witnesses
*> rules: [SR-13.10.3-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB780 - the slot witnesses ConstantNameLiteralPositionDriftTests reads: each statement writes a
      *> constant-name in a literal slot of the grammar that no other golden witnessed. The constant entry's own
      *> literal-1 was REFUSED (COBOLNET1547 "all operands of the compile-time arithmetic expression shall be ...
      *> numeric") when the constant-name named an alphanumeric or national constant.
      *>
      *> RULES (cite.py --check, all OK):
      *> 13.10.3 SR2: "Except in a compiler directive, constant-name-1 may be used anywhere that a format specifies a
      *>   literal of the class and category of constant-name-1." 13.10.2 specifies literal-1 as the AS operand.
      *> 13.10.4 GR1: the effect is "as if literal-1 ... were written where constant-name-1 is written"; GR2: the
      *>   class and category of constant-name-1 is the same as that of literal-1.
      *> 13.10.3 SR1: an operand that consists of a single numeric literal "is treated as a literal, not as an
      *>   arithmetic-expression", so KG, written AS KF, is the literal 0.25, not the integer GR4 makes of an
      *>   expression.
      *>
      *> DERIVATION. K2 is AS K1, the alphanumeric literal "AAA": "K2=AAA". KX is AS KH, a national constant written
      *> after it: NX = "NAT". KG is 0.25, so KG * 4 = 1: "Q=1". MULTIPLY's literal-2 BY operand (14.9.26.2
      *> Format 2) is KN, 7: 2 * 7 = 14, "M=14". The factory method CAT joins its two arguments,
      *> K1 and K2: "R=AAAAAA". CALL ... AS NESTED passes KN, 7, BY VALUE to PB780WV, whose LV PIC 9(4) shows it:
      *> "LV=0007". SET PP TO ENTRY KE addresses the outermost program PB780WE, so CALL PP shows "IN ENTRY". The
      *> run ends with STOP RUN WITH NORMAL STATUS KZ, KZ being 0. The program's own AS phrase names KP2, which
      *> is AS KP1, the alphanumeric "PB780WS" (11.10.2 literal-1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780WS AS KP2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB780WC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  KP2 CONSTANT AS KP1.
       01  KP1 CONSTANT AS "PB780WS".
       01  K1  CONSTANT AS "AAA".
       01  K2  CONSTANT AS K1.
       01  KX  CONSTANT AS KH.
       01  KH  CONSTANT AS N"NAT".
       01  KF  CONSTANT AS 0.25.
       01  KG  CONSTANT AS KF.
       01  KN  CONSTANT AS 7.
       01  KE  CONSTANT AS "PB780WE".
       01  KZ  CONSTANT AS 0.
       01  NX  PIC N(3).
       01  Q   PIC 9.
       01  M   PIC 99.
       01  R   PIC X(6) VALUE SPACES.
       01  PP  USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "K2=" K2.
           MOVE KX TO NX.
           DISPLAY "NX=" NX.
           COMPUTE Q = KG * 4.
           DISPLAY "Q=" Q.
           MULTIPLY 2 BY KN GIVING M.
           DISPLAY "M=" M.
           MOVE PB780WC :: "CAT" (K1 K2) TO R.
           DISPLAY "R=" R.
           CALL "PB780WV" AS NESTED USING BY VALUE KN.
           SET PP TO ENTRY KE.
           CALL PP.
           STOP RUN WITH NORMAL STATUS KZ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780WV.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LV PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE LV.
           DISPLAY "LV=" LV.
           GOBACK.
       END PROGRAM PB780WV.
       END PROGRAM PB780WS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780WE.
       PROCEDURE DIVISION.
           DISPLAY "IN ENTRY".
           GOBACK.
       END PROGRAM PB780WE.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB780WC.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. CAT.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L1 PIC X(3).
       01  L2 PIC X(3).
       01  LR PIC X(6).
       PROCEDURE DIVISION USING L1 L2 RETURNING LR.
           STRING L1 L2 DELIMITED BY SIZE INTO LR.
       END METHOD CAT.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       END OBJECT.
       END CLASS PB780WC.
