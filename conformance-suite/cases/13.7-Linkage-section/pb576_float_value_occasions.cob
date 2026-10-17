*>--- conformance-case
*> id: pb576_float_value_occasions
*> rules: [GR-13.7.4-5, GR-13.18.63.4-1]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB576 — ISO 13.18.63.4 GR1, "If the usage of the subject of the entry is float-short, float-long
      *> or float-extended, the actual value given to the item is an approximation of the arithmetic value of the
      *> literal", ON EVERY OCCASION A VALUE CLAUSE TAKES EFFECT. Until kb/Work PB420 (commit c2f0c4c50) the
      *> INITIALIZE emitter staged every non-ZERO sender into a floating-point receiver LOUD, so GR1's value could
      *> be given on GR4c's FIRST bullet and on no other path: GR3 (linkage), GR4a (external), GR4b (based, under
      *> ALLOCATE) and GR4c's second bullet (any INITIALIZE) all aborted at run time. No spec-derived test targeted
      *> GR1 at all. This golden asserts through COMPARISON rather than through a rendering, so nothing here
      *> depends on how a float is DISPLAYed:
      *>   L1 the initial-state occasion (GR4c first bullet). WA is compared against WS-C, which took the SAME
      *>      literal through a MOVE - 14.9.25 gives a MOVE of literal-1 into a float-long receiver the same
      *>      approximation GR1 gives the VALUE clause, so SAME is the rule's own assertion. The second line is
      *>      the other half of "approximation": if the item held the EXACT arithmetic value of 0.1 then 0.1*3
      *>      would be exactly 0.3; it holds binary64's nearest neighbour, so the product is not, and APPROX is
      *>      what GR1 licenses. Together the two lines pin the value from both sides.
      *>   L2 GR4c's SECOND bullet, "during the execution of an INITIALIZE statement".
      *>   L3 GR4b, "for based items and their subordinate items, during the execution of an ALLOCATE or an
      *>      explicit or implicit INITIALIZE statement" - here the ALLOCATE ... INITIALIZED form.
      *>   L5 GR4a, "for external items, during the execution of an INITIALIZE statement".
      *>   L4 GR3, "In the linkage section, VALUE clauses take effect ONLY during the execution of an explicit or
      *>      implicit INITIALIZE statement." PRE-NE is the "only" half: on entry LA holds the 9.0 the caller left
      *>      in the argument, NOT the VALUE clause's 0.1. SAME is the operative half, after the INITIALIZE.
      *> FLOAT-LONG is COBOL-2002 (13.18.60.2); the edition floor for the TO VALUE vehicle is already witnessed by
      *> conformance:negative/pb420-initialize-float-to-value-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB576FLV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-G.
          05 WA FLOAT-LONG VALUE 0.1.
       01 WS-C FLOAT-LONG.
       01 WS-3 FLOAT-LONG.
       01 EX-G EXTERNAL.
          05 EA FLOAT-LONG VALUE 0.1.
       01 BS-G BASED.
          05 BA FLOAT-LONG VALUE 0.1.
       PROCEDURE DIVISION.
           MOVE 0.1 TO WS-C
           IF WA = WS-C DISPLAY "L1 INIT  SAME" ELSE DISPLAY "L1 INIT  NE" END-IF
           COMPUTE WS-3 = WA * 3
           IF WS-3 = 0.3 DISPLAY "L1 INIT  EXACT" ELSE DISPLAY "L1 INIT  APPROX" END-IF
           MOVE 9.0 TO WA
           INITIALIZE WS-G ALL TO VALUE
           IF WA = WS-C DISPLAY "L2 TOVAL SAME" ELSE DISPLAY "L2 TOVAL NE" END-IF
           ALLOCATE BS-G INITIALIZED
           IF BA = WS-C DISPLAY "L3 BASED SAME" ELSE DISPLAY "L3 BASED NE" END-IF
           MOVE 9.0 TO EA
           INITIALIZE EX-G ALL TO VALUE
           IF EA = WS-C DISPLAY "L5 EXTRN SAME" ELSE DISPLAY "L5 EXTRN NE" END-IF
           MOVE 9.0 TO WA
           CALL "PB576FLS" USING WS-G
           STOP RUN.
       END PROGRAM PB576FLV.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB576FLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SC FLOAT-LONG.
       LINKAGE SECTION.
       01 LK-G.
          05 LA FLOAT-LONG VALUE 0.1.
       PROCEDURE DIVISION USING LK-G.
           MOVE 0.1 TO SC
           IF LA = SC DISPLAY "L4 LINK  PRE-SAME" ELSE DISPLAY "L4 LINK  PRE-NE" END-IF
           INITIALIZE LK-G ALL TO VALUE
           IF LA = SC DISPLAY "L4 LINK  SAME" ELSE DISPLAY "L4 LINK  NE" END-IF
           GOBACK.
       END PROGRAM PB576FLS.
