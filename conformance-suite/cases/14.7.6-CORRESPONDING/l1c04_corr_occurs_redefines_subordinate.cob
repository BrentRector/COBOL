      *>--- conformance-case
      *> id: l1c04_corr_occurs_redefines_subordinate
      *> rules: [GR-14.7.6-5, GR-14.9.2.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.7.6 rule 5 — no pair under an OCCURS or REDEFINES
      *>   group that is subordinate to D1 or D2 (either side).
      *> cite.py --check 14.7.6 "Neither data item is subordinate to a
      *>   group item that is subordinate to D1 or D2 when the group
      *>   item contains an OCCURS or REDEFINES clause"
      *>   -> OK §14.7.6 5)
      *> Every namesake below is numeric (rule 3 holds) and has the
      *> same qualifiers up to D1/D2 (rule 1 holds), so only rule 5
      *> decides:
      *>   K        directly in both            -> pair (control)
      *>   OG.O1    G1's OG has OCCURS 2        -> excluded
      *>   RR.R2    G1's RR has REDEFINES       -> excluded
      *>   PG.P1    plain group on both sides   -> pair (control)
      *>   SG.S1    G2's SG has REDEFINES (the D2 side)  -> excluded
      *> Rule 5 names groups SUBORDINATE to D1/D2, so D1 itself having
      *> REDEFINES (G3R REDEFINES G3) does not exclude its K.
      *> Start: every G1 item 1 (R1/R2 overlay: both 1); every G2
      *> item 10; K OF G3R = 1.
      *>   "K=011 P1=011"   ADD CORRESPONDING G1 TO G2 pairs K and P1.
      *>   "O1=010 R2=010 S1=010"  the three rule-5 exclusions
      *>                           stay 10.
      *>   "K=012"          ADD CORRESPONDING G3R TO G2: K pairs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C04M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 K PIC 9(3).
          05 OG OCCURS 2.
             10 O1 PIC 9(3).
          05 RG.
             10 R1 PIC 9(3).
          05 RR REDEFINES RG.
             10 R2 PIC 9(3).
          05 PG.
             10 P1 PIC 9(3).
          05 SG.
             10 S1 PIC 9(3).
       01 G2.
          05 K PIC 9(3).
          05 OG.
             10 O1 PIC 9(3).
          05 RR.
             10 R2 PIC 9(3).
          05 PG.
             10 P1 PIC 9(3).
          05 SX PIC X(3).
          05 SG REDEFINES SX.
             10 S1 PIC 9(3).
       01 G3 PIC X(3).
       01 G3R REDEFINES G3.
          05 K PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO K OF G1 O1 OF G1 (1) O1 OF G1 (2) R1 OF G1
                     P1 OF G1 S1 OF G1.
           MOVE 10 TO K OF G2 O1 OF G2 R2 OF G2 P1 OF G2 S1 OF G2.
           MOVE 1 TO K OF G3R.
           ADD CORRESPONDING G1 TO G2.
           DISPLAY "K=" K OF G2 " P1=" P1 OF G2.
           DISPLAY "O1=" O1 OF G2 " R2=" R2 OF G2 " S1=" S1 OF G2.
           ADD CORRESPONDING G3R TO G2.
           DISPLAY "K=" K OF G2.
           STOP RUN.
