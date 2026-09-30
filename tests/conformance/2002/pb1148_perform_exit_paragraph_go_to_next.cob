      *> PB1148 - ISO 14.6.3 rule 1 / 14.9.28.4 GR5: the PERFORM's return
      *>   mechanism sits AFTER the last statement of the exit paragraph, so
      *>   an explicit GO TO in that paragraph - even to the paragraph that
      *>   physically follows it - leaves the range and control stays at the
      *>   target; only control that completes the exit paragraph returns.
      *> cite.py --check 14.6.3 "an implied transfer of control occurs from
      *>   the last statement in the procedure to the control mechanism of
      *>   the last executed controlling statement" -> OK  14.6.3 1)
      *> Derivation, in order:
      *>   A  PERFORM P1: P1 falls off its end -> returns (A-BACK).
      *>   B  PERFORM P2 THRU P3: P2's GO TO P3 stays in the range, P3
      *>      falls off its end -> returns (B-BACK).
      *>   C  PERFORM P4: EXIT PARAGRAPH completes the paragraph -> returns
      *>      (C-BACK); P4-SKIPPED is never displayed.
      *>   D  PERFORM P5: P5's GO TO P6 (the paragraph physically next) is
      *>      an explicit transfer - the PERFORM never returns, so D-BACK
      *>      is not displayed; P6 runs and goes on to FIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1148.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM P1
           DISPLAY "A-BACK"
           PERFORM P2 THRU P3
           DISPLAY "B-BACK"
           PERFORM P4
           DISPLAY "C-BACK"
           PERFORM P5
           DISPLAY "D-BACK"
           STOP RUN.
       P1.
           DISPLAY "P1".
       P2.
           DISPLAY "P2"
           GO TO P3.
       P3.
           DISPLAY "P3".
       P4.
           DISPLAY "P4"
           EXIT PARAGRAPH
           DISPLAY "P4-SKIPPED".
       P5.
           DISPLAY "P5"
           GO TO P6.
       P6.
           DISPLAY "P6"
           GO TO FIN.
       FIN.
           DISPLAY "END"
           STOP RUN.
