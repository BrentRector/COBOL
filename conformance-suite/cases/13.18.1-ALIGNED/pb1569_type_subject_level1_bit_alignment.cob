*>--- conformance-case
*> id: pb1569_type_subject_level1_bit_alignment
*> rules: [GR-13.18.1.4-1, GR-13.18.57.4-2]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1569 — a group TYPE subject is aligned as a LEVEL 1 item, so a bit group typed after a
      *> same-level bit item starts on a fresh byte instead of sharing its predecessor's.
      *> ISO §13.18.57.4 GR2 d): "the subject of the entry is aligned as though it were a level 1 item"
      *>   (cite.py --check 13.18.57.4 -> OK §13.18.57.4 2) d)).
      *> ISO §8.5.1.6.3: "a bit group item immediately following a bit group item or elementary bit data
      *>   item of the same level" goes at the next bit position, but "Alignment of elementary bit data
      *>   items of level 1 or level 77 and a level 1 bit group are at the first bit of a byte"
      *>   (cite.py --check 8.5.1.6.3 -> OK, both).
      *> ISO §13.18.49.4 GR1: SAME AS acts "as though the data description identified by data-name-1 had
      *>   been coded in place of the SAME AS clause" (cite.py --check 13.18.49.4 -> OK §13.18.49.4 1)) —
      *>   so a SAME AS of a TYPE subject re-states its TYPE clause and inherits GR2 d), while a SAME AS of
      *>   a plain level-1 bit group does NOT: its level-number is excluded, and §13.18.49.4 has no d).
      *> DERIVATION (bits high-order first; every record is cleared to all-zero bits first):
      *>  R  = F(1) | filler(7) | S{B1 B2}(2) T(1) | filler(5)  -> 16 bits, BYTE-LENGTH 02
      *>       T is an elementary bit item after the same-level bit group S: next bit (rule 1)
      *>       F=1 B1=1 B2=1 T=1  -> RV=1000000011100000
      *>  E  = EF(1) ES(1) | filler(6): ES's type BE is ELEMENTARY — no GR2, so it
      *>       shares EF's byte (rule 1)        -> BYTE-LENGTH 01, EV=11000000
      *>  K  = KA(8)      | KB(2) | filler(6): byte-aligned control   -> BYTE-LENGTH 02
      *>  R2 = F2(1) | filler(7) | S2{B1 B2}(2) | filler(6): S2 SAME AS the TYPE subject A
      *>                                          -> BYTE-LENGTH 02, R2V=1000000011000000
      *>  R3 = F3(1) S3{P1 P2}(2) | filler(5): S3 SAME AS the plain level-1 bit group PB
      *>       continues F3's byte (rule 2)     -> BYTE-LENGTH 01, S3=11
      *>  Z  = TYPE TT, whose member H is itself a TYPE BT subject after the same-level bit G:
      *>       the reproduced member keeps d) (§13.18.58.4 GR3 — "subordinate data descriptions are
      *>       assumed by data defined using the type-name", cite.py --check -> OK) -> BYTE-LENGTH 02
      *>  AL = AF(1) | filler(7) | AG(1) | filler(7): the SIBLING ARM — an ALIGNED member (§13.18.1.4
      *>       GR1 "aligned on the first bit of the first available byte boundary", cite.py --check
      *>       13.18.1.4 -> OK §13.18.1.4 1)) likewise starts a fresh byte, and a group MOVE composes and
      *>       distributes the record image by that same placement in both directions:
      *>       AL -> AX puts AF at bit 1 and AG at bit 9        -> AX19=11
      *>       AX (bit 9 set only) -> AL sets AG, clears AF    -> AFG=01
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1569TA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BT TYPEDEF GROUP-USAGE BIT.
           05  B1          PIC 1 USAGE BIT.
           05  B2          PIC 1 USAGE BIT.
       01  BE TYPEDEF      PIC 1 USAGE BIT.
       01  R.
           05  F           PIC 1 USAGE BIT.
           05  S TYPE BT.
           05  T           PIC 1 USAGE BIT.
       01  RV REDEFINES R  PIC 1(16) USAGE BIT.
       01  E.
           05  EF          PIC 1 USAGE BIT.
           05  ES TYPE BE.
       01  EV REDEFINES E  PIC 1(8) USAGE BIT.
       01  K.
           05  KA          PIC X.
           05  KB TYPE BT.
       01  A TYPE BT.
       01  PB GROUP-USAGE BIT.
           05  P1          PIC 1 USAGE BIT.
           05  P2          PIC 1 USAGE BIT.
       01  R2.
           05  F2          PIC 1 USAGE BIT.
           05  S2 SAME AS A.
       01  R2V REDEFINES R2 PIC 1(16) USAGE BIT.
       01  R3.
           05  F3          PIC 1 USAGE BIT.
           05  S3 SAME AS PB.
       01  TT TYPEDEF.
           05  G           PIC 1 USAGE BIT.
           05  H TYPE BT.
       01  Z TYPE TT.
       01  AL.
           05  AF          PIC 1 USAGE BIT.
           05  AG          PIC 1 USAGE BIT ALIGNED.
       01  AX.
           05  AXV         PIC 1(16) USAGE BIT.
       01  N               PIC 99.
       PROCEDURE DIVISION.
           MOVE B"0000000000000000" TO RV.
           MOVE B"1" TO F.
           MOVE B"1" TO B1 OF S.
           MOVE B"1" TO B2 OF S.
           MOVE B"1" TO T.
           MOVE FUNCTION BYTE-LENGTH (R) TO N.
           DISPLAY "BLR=" N.
           DISPLAY "RV=" RV.
           MOVE B"00000000" TO EV.
           MOVE B"1" TO EF.
           MOVE B"1" TO ES.
           MOVE FUNCTION BYTE-LENGTH (E) TO N.
           DISPLAY "BLE=" N.
           DISPLAY "EV=" EV.
           MOVE FUNCTION BYTE-LENGTH (K) TO N.
           DISPLAY "BLK=" N.
           MOVE B"0000000000000000" TO R2V.
           MOVE B"1" TO F2.
           MOVE B"1" TO B1 OF S2.
           MOVE B"1" TO B2 OF S2.
           MOVE FUNCTION BYTE-LENGTH (R2) TO N.
           DISPLAY "BLR2=" N.
           DISPLAY "R2V=" R2V.
           MOVE B"1" TO F3.
           MOVE B"1" TO P1 OF S3.
           MOVE B"1" TO P2 OF S3.
           MOVE FUNCTION BYTE-LENGTH (R3) TO N.
           DISPLAY "BLR3=" N.
           DISPLAY "S3=" S3.
           MOVE FUNCTION BYTE-LENGTH (Z) TO N.
           DISPLAY "BLZ=" N.
           MOVE B"1" TO AF.
           MOVE B"1" TO AG.
           MOVE AL TO AX.
           DISPLAY "AX19=" AXV (1:1) AXV (9:1).
           MOVE B"0000000010000000" TO AXV.
           MOVE AX TO AL.
           DISPLAY "AFG=" AF AG.
           STOP RUN.
