*>--- conformance-case
*> id: pb65_selection_alignment
*> rules: [RV-15.59.4-1, RV-15.63.4-1, RV-15.63.4-2, RV-15.71.4-2,
*>    RV-15.72.4-2, RV-15.72.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §8.8.4.2.4 / §15.59 / §15.63 / §15.71 / §15.72 — comparison and selection at 39 ALIGNED
      *> digits (fix-queue PB65). BIGV (PIC 9(24)) and SMLV (PIC 9V9(15)) align to 24+15 = 39 digits,
      *> past the Int128 intermediate: the old common-scale alignment WRAPPED silently, so
      *> IF BIGV > SMLV answered FALSE and FUNCTION MIN returned a NEGATIVE value — the content of NO
      *> argument, against §15.63.4 r1's "the returned value is the content of the argument-1 having
      *> the least value". Now: comparisons ride the exact non-widening CobolNum.Compare (sign split +
      *> the overflow-means-greater magnitude trick), selection compares each argument AT ITS OWN
      *> SCALE, and only the selected value rescales — to the receiver's scale with store semantics.
      *> Derivations: BIGV > SMLV true · MIN = SMLV (2e23 vs 1e-15) · MAX into 9(24) = BIGV exactly ·
      *> MIN into 9V9(15) = SMLV's image · ORD-MIN = 2, ORD-MAX = 1 (§15.71.4/§15.72.4 r2 ordinals).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB65SEL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BIGV PIC 9(24)    VALUE 200000000000000000000000.
       01 SMLV PIC 9V9(15)  VALUE 0.000000000000001.
       01 RM   PIC 9V9(15).
       01 RB   PIC 9(24).
       01 O1   PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           IF BIGV > SMLV DISPLAY "GT=OK" ELSE DISPLAY "GT=BAD" END-IF
           IF SMLV < BIGV DISPLAY "LT=OK" ELSE DISPLAY "LT=BAD" END-IF
           IF FUNCTION MIN(BIGV SMLV) = SMLV
               DISPLAY "MN=OK" ELSE DISPLAY "MN=BAD" END-IF
           COMPUTE RB = FUNCTION MAX(BIGV SMLV)
           DISPLAY "CB=" RB
           COMPUTE RM = FUNCTION MIN(BIGV SMLV)
           DISPLAY "CM=" RM
           COMPUTE O1 = FUNCTION ORD-MIN(BIGV SMLV)
           DISPLAY "OM=" O1
           COMPUTE O1 = FUNCTION ORD-MAX(BIGV SMLV)
           DISPLAY "OX=" O1
           STOP RUN.
