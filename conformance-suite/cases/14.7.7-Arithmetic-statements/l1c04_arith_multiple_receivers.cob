      *>--- conformance-case
      *> id: l1c04_arith_multiple_receivers
      *> rules: [GR-14.7.7-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.7.7 rule 4 — multiple resultant identifiers: one
      *>   intermediate, stored left to right, receivers identified as
      *>   accessed; size error per receiver; NOTE 3 overlap.
      *> Rule 4 fragments, each --check'ed (cite.py labels every
      *> paragraph of rule 4, b) and NOTE 3 included, "4) a)"):
      *> cite.py --check 14.7.7 "The initial evaluation of the
      *>   statement is done and the result of this operation is
      *>   placed in an intermediate data item" -> OK §14.7.7 4) a)
      *> cite.py --check 14.7.7 "All item identification for the data
      *>   items involved in the initial evaluation is done at the
      *>   start of the execution of the statement" -> OK §14.7.7 4) a)
      *> cite.py --check 14.7.7 "If the size error condition is raised
      *>   during the initial evaluation, none of the resultant data
      *>   items are changed" -> OK §14.7.7 4) a)
      *> cite.py --check 14.7.7 "in the left-to-right order in which
      *>   the receiving data items are specified in the statement"
      *>   -> OK §14.7.7 4) a)   [text is 4) b)]
      *> cite.py --check 14.7.7 "Item identification for the receiving
      *>   data items is done as each data item is accessed unless it
      *>   was already done in step a" -> OK §14.7.7 4) a) [is 4) b)]
      *> cite.py --check 14.7.7 "If the size error condition is raised
      *>   when attempting to store in a resulting data item, only
      *>   that data item remains unchanged" -> OK §14.7.7 4) a)
      *>   [text is 4) b)]
      *> cite.py --check 14.7.7 "Those results in the receiving
      *>   operands are the same as if no receiving operand shared any
      *>   part of its storage area with any sending operand"
      *>   -> OK §14.7.7 4) a)   [text is NOTE 3]
      *> cite.py --check 14.7.5 "if the divisor in a divide operation
      *>   or in a DIVIDE statement is zero" -> OK §14.7.5 2)
      *> cite.py --check 14.7.5 "control is transferred to the
      *>   imperative-statement specified in the SIZE ERROR phrase"
      *>   -> OK §14.7.5 3)
      *> Derivation (a wrong order/timing changes the line shown in []):
      *> 1 ADD X TO X Y, X=5 Y=1: intermediate 5 fixed first; X=5+5,
      *>   Y=1+5.                     "1 X=10 Y=06"  [Y=11 if re-read]
      *> 2 COMPUTE X Y = X + 1, X=5: intermediate 6 for both.
      *>                              "2 X=06 Y=06"  [Y=07]
      *> 3 MULTIPLY X BY X Y, X=5 Y=2: multiplier 5 fixed; X=25 Y=10.
      *>                              "3 X=25 Y=10"  [Y=50]
      *> 4 ADD 1 TO I T (I), I=1: I stored first (2), then T (I) is
      *>   identified as accessed -> T(2).    "4 I=2 T=010"  [T=100]
      *> 5 COMPUTE I T (I) = 2, I=1: I=2, then T(2)=2.
      *>                              "5 I=2 T=020"
      *> 6 ADD T (I) TO I T (I), I=1, T=2,0,0: the sender T (I) is
      *>   identified at the start (T(1)=2); I=1+2=3; the receiver
      *>   T (I) is identified when accessed -> T(3)=0+2.
      *>                              "6 I=3 T=202"
      *> 7 ADD 5 TO S1 S2 ON SIZE ERROR, S1 PIC 9 = 7, S2 PIC 99 = 7:
      *>   12 does not fit S1 (size error, S1 unchanged), S2 = 12;
      *>   the imperative runs after the stores.
      *>                              "7 SE" then "7 S1=7 S2=12"
      *> 8 COMPUTE V W = 1 / Z0 ON SIZE ERROR, Z0 = 0: size error in
      *>   the initial evaluation - neither receiver changes.
      *>                              "8 SE" then "8 V=3 W=4"
      *> 9 NOTE 3: GR REDEFINES GA (A1=34 A2=12, so GR=3412).
      *>   COMPUTE A1 A2 = GR - 3400: intermediate 12 for both, as if
      *>   no storage were shared.      "9 A1=12 A2=12" [A2=88 if GR
      *>   were re-read after A1's store: 1212 - 3400]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C04O.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X  PIC 99.
       01 Y  PIC 99.
       01 I  PIC 9.
       01 TG.
          05 T PIC 9 OCCURS 3.
       01 S1 PIC 9.
       01 S2 PIC 99.
       01 V  PIC 9.
       01 W  PIC 9.
       01 Z0 PIC 9 VALUE 0.
       01 GA.
          05 A1 PIC 99.
          05 A2 PIC 99.
       01 GR REDEFINES GA PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 5 TO X. MOVE 1 TO Y.
           ADD X TO X Y.
           DISPLAY "1 X=" X " Y=" Y.
           MOVE 5 TO X. MOVE 0 TO Y.
           COMPUTE X Y = X + 1.
           DISPLAY "2 X=" X " Y=" Y.
           MOVE 5 TO X. MOVE 2 TO Y.
           MULTIPLY X BY X Y.
           DISPLAY "3 X=" X " Y=" Y.
           MOVE 1 TO I. MOVE ZERO TO TG.
           ADD 1 TO I T (I).
           DISPLAY "4 I=" I " T=" TG.
           MOVE 1 TO I. MOVE ZERO TO TG.
           COMPUTE I T (I) = 2.
           DISPLAY "5 I=" I " T=" TG.
           MOVE 1 TO I. MOVE "200" TO TG.
           ADD T (I) TO I T (I).
           DISPLAY "6 I=" I " T=" TG.
           MOVE 7 TO S1 S2.
           ADD 5 TO S1 S2
               ON SIZE ERROR DISPLAY "7 SE"
           END-ADD.
           DISPLAY "7 S1=" S1 " S2=" S2.
           MOVE 3 TO V. MOVE 4 TO W.
           COMPUTE V W = 1 / Z0
               ON SIZE ERROR DISPLAY "8 SE"
           END-COMPUTE.
           DISPLAY "8 V=" V " W=" W.
           MOVE 34 TO A1. MOVE 12 TO A2.
           COMPUTE A1 A2 = GR - 3400.
           DISPLAY "9 A1=" A1 " A2=" A2.
           STOP RUN.
