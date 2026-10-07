      *> ISO 8.4.3.2.4 GR1/GR2/GR6a - a function-identifier references a temporary whose value is
      *> determined WHEN THE REFERENCE IS EVALUATED: two references in one statement are two
      *> activations (GR2, left to right); a reference in a PERFORM UNTIL condition activates per
      *> iteration (14.9.28 GR6); a non-first AND/OR operand activates only when short-circuit
      *> evaluation reaches it (8.8.4.13 r1/r2 - "if and when the conditions containing them are
      *> evaluated"); an EVALUATE selection object activates only when its WHEN is considered
      *> (14.9.13.4 GR4); a SEARCH WHEN condition activates per scan pass (14.9.37.4 GR1/GR4). The
      *> activation counter is EXTERNAL data - last-used per run unit (14.6.2.3.3) - because a
      *> function's internal data is per-activation (functions are always recursive, 8.6.6).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. SEQ-P10UV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CTR-P10UV PIC 9(4) EXTERNAL.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           ADD 1 TO CTR-P10UV.
           COMPUTE L-R = CTR-P10UV + L-X.
           GOBACK.
       END FUNCTION SEQ-P10UV.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UPEREVAL-P10UV.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION SEQ-P10UV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CTR-P10UV PIC 9(4) EXTERNAL.
       01 WS-A PIC 9(4).
       01 WS-R PIC 9(8).
       01 TAB-P10UV VALUE "010203".
          05 ROWX PIC 9(2) OCCURS 3 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN.
      *>   GR2: two references in ONE statement = two activations, left to right (1 + 2)
           COMPUTE WS-R = FUNCTION SEQ-P10UV(0) + FUNCTION SEQ-P10UV(0).
           DISPLAY "TWO=" WS-R.
           DISPLAY "C1=" CTR-P10UV.
      *>   per-iteration activation in the UNTIL condition: activations 3,4 run the body; 5 exits
           MOVE 0 TO WS-A.
           PERFORM UNTIL FUNCTION SEQ-P10UV(0) >= 5
               ADD 1 TO WS-A
           END-PERFORM.
           DISPLAY "LOOPS=" WS-A.
           DISPLAY "C2=" CTR-P10UV.
      *>   8.8.4.13 r1: the AND's right operand is skipped when the left is false
           MOVE 0 TO WS-A.
           IF WS-A = 1 AND FUNCTION SEQ-P10UV(0) = 999
               DISPLAY "NEVER-AND"
           END-IF.
           DISPLAY "C3=" CTR-P10UV.
      *>   the OR's right operand is skipped when the left is true
           IF WS-A = 0 OR FUNCTION SEQ-P10UV(0) = 999
               DISPLAY "OR-TAKEN"
           END-IF.
           DISPLAY "C4=" CTR-P10UV.
      *>   14.9.13.4 GR4: an object is evaluated only when its WHEN phrase is considered
           MOVE 77 TO WS-A.
           EVALUATE WS-A
               WHEN 1 DISPLAY "NEVER-W1"
               WHEN FUNCTION SEQ-P10UV(0) DISPLAY "NEVER-W2"
               WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE.
           DISPLAY "C5=" CTR-P10UV.
      *>   14.9.37.4 GR1/GR4: the WHEN re-evaluates each pass; never true -> AT END after 3 passes
           SET IX TO 1.
           SEARCH ROWX
               AT END DISPLAY "AT-END"
               WHEN ROWX(IX) = FUNCTION SEQ-P10UV(0) DISPLAY "FOUND"
           END-SEARCH.
           DISPLAY "C6=" CTR-P10UV.
           STOP RUN.
       END PROGRAM UPEREVAL-P10UV.

