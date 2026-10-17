      *>--- conformance-case
      *> id: pb414_next_sentence_last_sentence_inline
      *> rules: [GR-14.9.19.4-4, GR-14.9.19.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB414 - NEXT SENTENCE inside an inline PERFORM in the LAST sentence of a paragraph, both arms,
      *> at --std 85 (the earliest supported edition; NEXT SENTENCE is legal in all four and is flagged ARCHAIC
      *> with COBOLNET0903 at 2023 only, so there is no edition BELOW its introduction to reject it).
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE SPEC AND NOT FROM A RUN:
      *> 14.9.19.4 GR4 - "If condition-1 is true and NEXT SENTENCE is specified in the THEN phrase, the ELSE
      *>                  phrase, if specified, is ignored and control is transferred to an implicit CONTINUE
      *>                  statement immediately preceding the next separator period."
      *> 14.9.19.4 GR6 - "If condition-1 is false and NEXT SENTENCE is specified in the ELSE phrase, the THEN
      *>                  phrase is ignored and control is transferred to an implicit CONTINUE statement
      *>                  immediately preceding the next separator period."
      *> The two arms state ONE transfer, so both are measured: GR4-P writes it in the THEN phrase and GR6-P in
      *> the ELSE phrase. In each the ENTIRE paragraph is one sentence, so "the next separator period" is the
      *> period after the trailing DISPLAY: everything remaining in the paragraph - the rest of the inline
      *> PERFORM's body, the rest of its iterations, and the trailing DISPLAY - is skipped. An enclosing inline
      *> PERFORM is not a scope the rule knows about; it is part of the sentence being left.
      *> CTL-P is the CONTROL: the same statement in a paragraph with a SECOND sentence, where the transfer
      *> lands on that sentence boundary and CTL-SENT2 runs. It pins the arm that was already correct, so a
      *> regression on either side is visible here.
      *> K is 9(1) and the loop runs while K <= 3, so iteration 1 prints and iteration 2 transfers.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB414NEXTSENT85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       GR4-P.
           MOVE 0 TO K
           PERFORM UNTIL K > 3
               ADD 1 TO K
               IF K = 2 NEXT SENTENCE END-IF
               DISPLAY "GR4-IT " K
           END-PERFORM
           DISPLAY "GR4-TAIL".
       GR6-P.
           MOVE 0 TO K
           PERFORM UNTIL K > 3
               ADD 1 TO K
               IF K NOT = 2 CONTINUE ELSE NEXT SENTENCE END-IF
               DISPLAY "GR6-IT " K
           END-PERFORM
           DISPLAY "GR6-TAIL".
       CTL-P.
           MOVE 0 TO K
           PERFORM UNTIL K > 3
               ADD 1 TO K
               IF K = 2 NEXT SENTENCE END-IF
               DISPLAY "CTL-IT " K
           END-PERFORM
           DISPLAY "CTL-TAIL".
           DISPLAY "CTL-SENT2".
       END-P.
           DISPLAY "END".
           STOP RUN.
