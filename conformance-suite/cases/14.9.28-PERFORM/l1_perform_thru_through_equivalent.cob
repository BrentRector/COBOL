      *>--- conformance-case
      *> id: l1_perform_thru_through_equivalent
      *> rules: [SR-14.9.28.3-10]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.28.3 SR10 — "The words THROUGH and THRU are
      *> equivalent." (FORMAT 1.)
      *>
      *> A rule that states an EQUIVALENCE is directly testable: write
      *> the construct both ways over the same paragraphs and assert
      *> the two results are identical. That is a stronger assertion
      *> than pinning one answer, because it fails if EITHER spelling
      *> drifts. ⛔ Each line also PRINTS the shared value, so the
      *> assertion cannot pass VACUOUSLY — two ranges that both ran
      *> nothing, or both ran the whole source unit, would still print
      *> "OK", and the printed value is what makes that visible.
      *>
      *> The paragraphs add distinct decimal weights (100 / 10 / 1), so
      *> the printed total NAMES the paragraphs that ran:
      *>   FULL  P1 THRU P3 -> 0111 (all three; §14.9.28.4 GR5 b)
      *>         returns "after the last statement of procedure-name-2")
      *>   PART  P1 THRU P2 -> 0110 (P3 must NOT run — a range that
      *>         ran to the end of the source unit would print 0111)
      *>   LOOP  Q1 THRU Q2 UNTIL K > 2 — the range combined with the
      *>         Format-1 until-phrase, so the equivalence is asserted
      *>         where the range is RE-ENTERED. Q2 bumps K, so
      *>         §14.9.28.4 GR10 gives three passes before K > 2 holds:
      *>         3 x 110 = 0330.
      *>
      *> The rule is worded identically in COBOL-85/2002/2014/2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PFTH10.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TGT  PIC 9(4) VALUE 0.
       01 R-FT PIC 9(4) VALUE 0.
       01 R-FH PIC 9(4) VALUE 0.
       01 R-PT PIC 9(4) VALUE 0.
       01 R-PH PIC 9(4) VALUE 0.
       01 R-LT PIC 9(4) VALUE 0.
       01 R-LH PIC 9(4) VALUE 0.
       01 K    PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
      *> The FULL range, both spellings.
           MOVE 0 TO TGT.
           PERFORM P1 THRU P3.
           MOVE TGT TO R-FT.
           MOVE 0 TO TGT.
           PERFORM P1 THROUGH P3.
           MOVE TGT TO R-FH.
           IF R-FT = R-FH
               DISPLAY "SR10-FULL=OK " R-FT
           ELSE
               DISPLAY "SR10-FULL=BAD " R-FT " " R-FH
           END-IF.
      *> A range that STOPS SHORT, both spellings.
           MOVE 0 TO TGT.
           PERFORM P1 THRU P2.
           MOVE TGT TO R-PT.
           MOVE 0 TO TGT.
           PERFORM P1 THROUGH P2.
           MOVE TGT TO R-PH.
           IF R-PT = R-PH
               DISPLAY "SR10-PART=OK " R-PT
           ELSE
               DISPLAY "SR10-PART=BAD " R-PT " " R-PH
           END-IF.
      *> The range under an until-phrase, both spellings.
           MOVE 0 TO TGT.
           MOVE 0 TO K.
           PERFORM Q1 THRU Q2 UNTIL K > 2.
           MOVE TGT TO R-LT.
           MOVE 0 TO TGT.
           MOVE 0 TO K.
           PERFORM Q1 THROUGH Q2 UNTIL K > 2.
           MOVE TGT TO R-LH.
           IF R-LT = R-LH
               DISPLAY "SR10-LOOP=OK " R-LT
           ELSE
               DISPLAY "SR10-LOOP=BAD " R-LT " " R-LH
           END-IF.
           STOP RUN.

       P1.
           ADD 100 TO TGT.
       P2.
           ADD 10 TO TGT.
       P3.
           ADD 1 TO TGT.
       Q1.
           ADD 100 TO TGT.
       Q2.
           ADD 10 TO TGT.
           ADD 1 TO K.
