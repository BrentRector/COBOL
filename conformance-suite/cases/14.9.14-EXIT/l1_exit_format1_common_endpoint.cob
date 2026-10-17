      *>--- conformance-case
      *> id: l1_exit_format1_common_endpoint
      *> rules: [GR-14.9.14.4-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.14.4 GR1 — "An EXIT statement serves only to enable
      *> the user to assign a procedure-name to a given point in a
      *> procedure division. Such an EXIT statement has no other effect
      *> on the compilation or execution."
      *> P-EXIT holds a bare EXIT alone in its paragraph (§14.9.14.3
      *> SR1) and is the common end point of PERFORM P1 THRU P-EXIT.
      *> It is reached two ways — by fall-through from P2 and by GO TO
      *> from P1 — and in NEITHER case may it contribute a line of its
      *> own or alter the flow. The only effect of the statement is
      *> that the paragraph-name P-EXIT can be written as a target.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1EXT01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-S PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 0 TO W-S.
           PERFORM P1 THRU P-EXIT.
           DISPLAY "AFTER-1".
           MOVE 1 TO W-S.
           PERFORM P1 THRU P-EXIT.
           DISPLAY "AFTER-2".
           STOP RUN.
       P1.
           DISPLAY "P1".
           IF W-S = 1
               GO TO P-EXIT
           END-IF.
       P2.
           DISPLAY "P2".
       P-EXIT.
           EXIT.
