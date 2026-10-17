      *>--- conformance-case
      *> id: l1_if_else_next_sentence_omissible
      *> rules: [SR-14.9.19.3-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.19.3 SR3 (FORMAT 2) — "The ELSE NEXT SENTENCE phrase
      *> may be omitted if it immediately precedes the terminal
      *> separator period of the sentence."
      *> SR3 is a PERMISSION, not a constraint: it forbids nothing, it
      *> obliges the implementation to accept BOTH spellings in that one
      *> position and to give them the SAME meaning. Its testable
      *> content is therefore an EQUIVALENCE, and each W-/O- pair of
      *> lines below is one half of it.
      *> The two meanings are computed independently from the general
      *> rules of §14.9.19.4:
      *>   written form, condition FALSE — GR6: "the THEN phrase is
      *>     ignored and control is transferred to an implicit CONTINUE
      *>     statement immediately preceding the next separator period";
      *>   omitted form, condition FALSE — GR7: "the THEN phrase is
      *>     ignored";
      *>   either form, condition TRUE — GR3: statement-1 runs and "The
      *>     ELSE phrase, if specified, is ignored".
      *> In the position SR3 names, the phrase sits on the terminal
      *> period of its own sentence, so both forms continue with the
      *> next sentence and the W- and O- lines must pair up exactly.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1IFN02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
      *> FALSE — the phrase WRITTEN, immediately before the period.
           MOVE 0 TO W-X.
           IF W-X = 1
               DISPLAY "W-FALSE-THEN"
           ELSE
               NEXT SENTENCE.
           DISPLAY "W-FALSE-NEXT".
      *> FALSE — the same sentence with the phrase OMITTED.
           MOVE 0 TO W-X.
           IF W-X = 1
               DISPLAY "O-FALSE-THEN".
           DISPLAY "O-FALSE-NEXT".
      *> TRUE — the phrase WRITTEN; the ELSE phrase is ignored.
           MOVE 1 TO W-X.
           IF W-X = 1
               DISPLAY "W-TRUE-THEN"
           ELSE
               NEXT SENTENCE.
           DISPLAY "W-TRUE-NEXT".
      *> TRUE — the same sentence with the phrase OMITTED.
           MOVE 1 TO W-X.
           IF W-X = 1
               DISPLAY "O-TRUE-THEN".
           DISPLAY "O-TRUE-NEXT".
           STOP RUN.
