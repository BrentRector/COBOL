      *> kb/Work PB480 - an INVOKE through a UNIVERSAL object reference asks the standard's TWO relations of the
      *> argument and the formal: 9.3.6's MATCH (resolution - a method that does not match is not bound, and when no
      *> class has one, "6) otherwise, the EC-OO-METHOD exception condition is set to exist", cite.py --check 9.3.6
      *> -> OK 9.3.6 6)) and, of the bound method, 14.8.2 / 14.8.3's CONFORMANCE (14.9.23.4 GR7 c): "the rules for
      *> conformance specified in 14.8.2, Parameters and 14.8.3, Returning items apply", cite.py --check 14.9.23.4
      *> -> OK 14.9.23.4 7)), a violation of which is EC-OO-UNIVERSAL. Before the fix one descriptor STRING compared
      *> for equality stood for both: national / boolean / numeric-edited / LOCALE arguments were refused at compile
      *> time (COBOLNET0866), a group matched a PIC X of its width, a smaller formal group did not match, and a
      *> class-typed RETURNING item into a universal receiver raised EC-OO-UNIVERSAL.
      *> Match rule 3 c) "is the same class and category" (OK 9.3.6 3) c)) and 3 e) "has the same ALIGNED, ANY
      *> LENGTH, BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses" (OK 9.3.6 3)): a group
      *> carries none of them, so two groups match whatever their sizes and a group never matches a PIC X(n).
      *> 14.8.2.2 rule 1: "the formal parameter shall be described with the same number or a smaller number of bytes
      *> as the corresponding argument" (OK 14.8.2.2 1)); 14.2.3 GR8 "as if the formal parameter occupies the same
      *> storage area as the argument" (OK 14.2.3 8)), so a smaller formal's store reaches the leading positions.
      *> 8.5.2.1: "Both the class and the category of a strongly-typed group item are the type-name specified in the
      *> TYPE clause" (OK 8.5.2.1) - a plain group does not match a strong formal; two equivalent declarations of
      *> one type-name are the same type (8.5.3.1).
      *> 14.8.2.3.2 "Additionally" c): "A national group item matches an elementary data item of usage national
      *> described with the same number of national character positions" (OK 14.8.2.3.2 2)).
      *> 14.8.3.3 rule 1: an object-reference returning item conforms "as if a SET statement were performed" (OK
      *> 14.8.3.3 1)) - SET universal TO a class-typed reference is legal.
      *> DERIVATION:
      *>   TN  N(3) into N(3): match, conforms                         -> TN:ABC, a=XYZ
      *>   TB  1(4) into 1(4)                                          -> TB:1010, b=0101
      *>   TE  ZZ9.99 holding 12.5 into ZZ9.99                         -> TE: 12.50, e=  3.25
      *>   TL  LOCALE IS US SIZE 8 into LOCALE IS US2 SIZE 8, both "en-US" (rule 3 e) 3.) -> TL:  $10.00
      *>   TP  8-character group into a 4-character group: match; rule 1 -> TP:ABCD, p=WXYZEFGH
      *>   TW  4-character group into an 8-character group: match; rule 1 violated -> EC-OO-UNIVERSAL
      *>   TQ  3-position national group into a 4-position one: match; "Additionally" c) violated -> EC-OO-UNIVERSAL
      *>   TG  PIC X(4) into a group formal: no match (PICTURE on one side) -> EC-OO-METHOD
      *>   TX  group into a PIC X(4) formal: no match -> EC-OO-METHOD
      *>   TS  strong group of an equivalent type -> TS:WXYZ; a plain group -> EC-OO-METHOD
      *>   TR  a 4-character group returned into PIC X(4) (14.8.3.2: same length) -> r=RRRR
      *>   TC  an object reference described C480A returned into a universal one -> SAME
      *>   New invoked with an argument: no match (rule 1) -> EC-OO-METHOD
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB480A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       REPOSITORY.
           CLASS C480A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ST-T TYPEDEF STRONG.
          05 S1 PIC X(4).
       01 U  USAGE OBJECT REFERENCE.
       01 R  USAGE OBJECT REFERENCE.
       01 A  PIC N(3) USAGE NATIONAL VALUE N"ABC".
       01 B  PIC 1(4) VALUE B"1010".
       01 E  PIC ZZ9.99.
       01 LV PIC $Z9.99 LOCALE IS US SIZE IS 8.
       01 P.
          05 P1 PIC X(8) VALUE "ABCDEFGH".
       01 W.
          05 W1 PIC X(4) VALUE "ABCD".
       01 Q GROUP-USAGE NATIONAL.
          05 Q1 PIC N(3) VALUE N"ABC".
       01 X  PIC X(4) VALUE "ABCD".
       01 SV TYPE ST-T.
       01 RX PIC X(4).
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
           MOVE 12.5 TO E
           MOVE 10 TO LV
           INVOKE C480A "NEW" RETURNING U
           INVOKE U "TN" USING A
           DISPLAY "a=" A
           INVOKE U "TB" USING B
           DISPLAY "b=" B
           INVOKE U "TE" USING E
           DISPLAY "e=" E
           INVOKE U "TL" USING LV
           INVOKE U "TP" USING P
           DISPLAY "p=" P
           INVOKE U "TW" USING W
           INVOKE U "TQ" USING Q
           INVOKE U "TG" USING X
           INVOKE U "TX" USING W
           MOVE "WXYZ" TO S1 OF SV
           INVOKE U "TS" USING SV
           INVOKE U "TS" USING W
           INVOKE U "TR" RETURNING RX
           DISPLAY "r=" RX
           INVOKE U "TC" RETURNING R
           IF R = U DISPLAY "SAME" ELSE DISPLAY "OTHER" END-IF
           INVOKE U "NEW" USING X RETURNING R
           DISPLAY "END".
           STOP RUN.
       END PROGRAM PB480A.

       IDENTIFICATION DIVISION.
       CLASS-ID. C480A INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US2 IS "en-US".
       REPOSITORY.
           CLASS BASE
           CLASS C480A.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ST-T TYPEDEF STRONG.
          05 S1 PIC X(4).
       PROCEDURE DIVISION.
       METHOD-ID. TN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN PIC N(3) USAGE NATIONAL.
       PROCEDURE DIVISION USING LN.
           DISPLAY "TN:" LN
           MOVE N"XYZ" TO LN.
       END METHOD TN.
       METHOD-ID. TB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LB PIC 1(4).
       PROCEDURE DIVISION USING LB.
           DISPLAY "TB:" LB
           MOVE B"0101" TO LB.
       END METHOD TB.
       METHOD-ID. TE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LE PIC ZZ9.99.
       PROCEDURE DIVISION USING LE.
           DISPLAY "TE:" LE
           MOVE 3.25 TO LE.
       END METHOD TE.
       METHOD-ID. TL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LL PIC $Z9.99 LOCALE IS US2 SIZE IS 8.
       PROCEDURE DIVISION USING LL.
           DISPLAY "TL:" LL.
       END METHOD TL.
       METHOD-ID. TP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP.
          05 LP1 PIC X(4).
       PROCEDURE DIVISION USING LP.
           DISPLAY "TP:" LP
           MOVE "WXYZ" TO LP.
       END METHOD TP.
       METHOD-ID. TW.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LW.
          05 LW1 PIC X(8).
       PROCEDURE DIVISION USING LW.
           DISPLAY "TW:" LW.
       END METHOD TW.
       METHOD-ID. TQ.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LQ GROUP-USAGE NATIONAL.
          05 LQ1 PIC N(4).
       PROCEDURE DIVISION USING LQ.
           DISPLAY "TQ:" LQ.
       END METHOD TQ.
       METHOD-ID. TG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LG.
          05 LG1 PIC X(4).
       PROCEDURE DIVISION USING LG.
           DISPLAY "TG:" LG.
       END METHOD TG.
       METHOD-ID. TX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX PIC X(4).
       PROCEDURE DIVISION USING LX.
           DISPLAY "TX:" LX.
       END METHOD TX.
       METHOD-ID. TS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LS TYPE ST-T.
       PROCEDURE DIVISION USING LS.
           DISPLAY "TS:" S1 OF LS.
       END METHOD TS.
       METHOD-ID. TR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR.
          05 LR1 PIC X(4).
       PROCEDURE DIVISION RETURNING LR.
           MOVE "RRRR" TO LR1.
       END METHOD TR.
       METHOD-ID. TC.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LC USAGE OBJECT REFERENCE C480A.
       PROCEDURE DIVISION RETURNING LC.
           SET LC TO SELF.
       END METHOD TC.
       END OBJECT.
       END CLASS C480A.
