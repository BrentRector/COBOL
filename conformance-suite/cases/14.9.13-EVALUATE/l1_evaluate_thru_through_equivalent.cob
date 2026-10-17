      *>--- conformance-case
      *> id: l1_evaluate_thru_through_equivalent
      *> rules: [SR-14.9.13.3-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.13.3 SR1 — "The words THROUGH and THRU are
      *> equivalent."  Every EVALUATE below is written TWICE, differing
      *> only in that one spelling of the range-expression word, so any
      *> difference between the two columns of a line is a violation.
      *> Range determination is NOT §14.9.13.4's own: GR2 routes it to
      *> §14.7.8, THROUGH phrase.  §14.7.8 1) fixes the NUMERIC range —
      *> both endpoints and all algebraic values between — so 2 THRU 4
      *> hits N=2,3,4 only, and THAT is a spec obligation.  §14.7.8 2)
      *> does not fix the alphanumeric one: with no IN alphabet-name
      *> phrase "the collating sequence is defined by the implementor".
      *> So "B" THRU "D" with W-A="C" is IMPLEMENTOR-DEFINED; this
      *> implementation collates the native UTF-16 repertoire, in which
      *> B < C < D, so it answers HIT — this implementation's
      *> documented latitude, not a spec obligation.  What SR1 mandates
      *> about that line is only that the THRU and THROUGH columns
      *> AGREE.
      *> The NOT line rests on §14.9.13.4 GR4 a) 5. instead, whose NOT
      *> form is the explicit conditional  subject < left-part OR
      *> subject > right-part  (and its operands are numeric anyway).
      *> Selection / WHEN OTHER per §14.9.13.4 GR4 b)-d), GR5 a)-b).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1EVT01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9 VALUE 0.
       01 W-T PIC XXX VALUE SPACES.
       01 W-G PIC XXX VALUE SPACES.
       01 W-A PIC X VALUE SPACE.
       PROCEDURE DIVISION.
       MAIN-P.
      *> 2 THRU 4 against 2 THROUGH 4, over every value 1..6.
           PERFORM VARYING W-N FROM 1 BY 1 UNTIL W-N > 6
               EVALUATE W-N
                   WHEN 2 THRU 4
                       MOVE "HIT" TO W-T
                   WHEN OTHER
                       MOVE "MIS" TO W-T
               END-EVALUATE
               EVALUATE W-N
                   WHEN 2 THROUGH 4
                       MOVE "HIT" TO W-G
                   WHEN OTHER
                       MOVE "MIS" TO W-G
               END-EVALUATE
               DISPLAY "N=" W-N " THRU=" W-T " THROUGH=" W-G
           END-PERFORM.
      *> The NOT form of the same range, subject INSIDE the range.
           MOVE 3 TO W-N.
           EVALUATE W-N
               WHEN NOT 2 THRU 4
                   MOVE "HIT" TO W-T
               WHEN OTHER
                   MOVE "MIS" TO W-T
           END-EVALUATE.
           EVALUATE W-N
               WHEN NOT 2 THROUGH 4
                   MOVE "HIT" TO W-G
               WHEN OTHER
                   MOVE "MIS" TO W-G
           END-EVALUATE.
           DISPLAY "NOT3 THRU=" W-T " THROUGH=" W-G.
      *> An alphanumeric range: the equivalence is not numeric-only.
           MOVE "C" TO W-A.
           EVALUATE W-A
               WHEN "B" THRU "D"
                   MOVE "HIT" TO W-T
               WHEN OTHER
                   MOVE "MIS" TO W-T
           END-EVALUATE.
           EVALUATE W-A
               WHEN "B" THROUGH "D"
                   MOVE "HIT" TO W-G
               WHEN OTHER
                   MOVE "MIS" TO W-G
           END-EVALUATE.
           DISPLAY "ALPHA-C THRU=" W-T " THROUGH=" W-G.
           STOP RUN.
