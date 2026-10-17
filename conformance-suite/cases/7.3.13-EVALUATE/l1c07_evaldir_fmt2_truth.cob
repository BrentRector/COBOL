      *>--- conformance-case
      *> id: l1c07_evaldir_fmt2_truth
      *> rules: [GR-7.3.13.4-8, GR-7.3.13.4-9, GR-7.3.13.4-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.13.4 GR8 GR9 GR10 — format 2 (EVALUATE TRUE)
      *> directive: first TRUE WHEN only; WHEN OTHER when none; nothing
      *> when none and no WHEN OTHER.
      *> RULE 7.3.13.4 GR8: "For each WHEN phrase in turn, the
      *>   constant-conditional-expression is evaluated in accordance
      *>   with 7.3.8 ... If a WHEN phrase evaluates to TRUE, all lines
      *>   of text-1 associated with that WHEN phrase are included ...
      *>   All lines of text-1 associated with other WHEN phrases of
      *>   that EVALUATE directive and all lines of text-2 associated
      *>   with a WHEN OTHER phrase are omitted ..."
      *> RULE 7.3.13.4 GR9: "If no WHEN phrase evaluates to TRUE, all
      *>   lines of text-2 associated with the WHEN OTHER phrase, if
      *>   specified, are included in the resultant text. ..."
      *> RULE 7.3.13.4 GR10: "If the END-EVALUATE phrase is reached
      *>   without any WHEN phrase evaluating to TRUE, and without
      *>   encountering a WHEN OTHER phrase, all lines of text-1
      *>   associated with all WHEN phrases are omitted ..."
      *> RULE 7.3.8.4.4 GR1: "A defined condition using the IS DEFINED
      *>   syntax evaluates TRUE if compilation-variable-name-1 is
      *>   currently defined."
      *> cite.py:
      *>   OK  §7.3.13.4 8)  (General rules)
      *>  OK  §7.3.13.4 5)  (General rules)  [GR9 = GR5 word for word;
      *>                                     cite.py resolves to 5)]
      *>   OK  §7.3.13.4 6)  (General rules)  [GR10 likewise = GR6]
      *>   OK  §7.3.8.4.4 1)  (General rule)
      *> EXPECTED OUTPUT, DERIVED (L1C07VV defined as 3):
      *>   A-FIRST-TRUE-1  WHEN L1C07VV > 5 is FALSE; the next WHEN
      *>   A-FIRST-TRUE-2  (L1C07VV IS DEFINED AND L1C07VV = 3) is TRUE:
      *>                   all its text-1 lines included; the later WHEN
      *>                   1 = 1, although TRUE, and WHEN OTHER omitted.
      *>   B-OTHER         L1C07NOPE IS DEFINED FALSE (never defined),
      *>                   "X" = "Y" FALSE -> WHEN OTHER text-2 (GR9).
      *>   C-AFTER-NO-OTHER 1 = 2 FALSE, L1C07VV NOT = 3 FALSE, no WHEN
      *>                   OTHER -> all text-1 omitted (GR10); only the
      *>                   line after END-EVALUATE shows.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C07H.
       PROCEDURE DIVISION.
       MAIN-PARA.
       >>DEFINE L1C07VV AS 3
       >>EVALUATE TRUE
       >>WHEN L1C07VV > 5
           DISPLAY "A-WRONG-GT5".
       >>WHEN L1C07VV IS DEFINED AND L1C07VV = 3
           DISPLAY "A-FIRST-TRUE-1".
           DISPLAY "A-FIRST-TRUE-2".
       >>WHEN 1 = 1
           DISPLAY "A-WRONG-LATER-TRUE".
       >>WHEN OTHER
           DISPLAY "A-WRONG-OTHER".
       >>END-EVALUATE
       >>EVALUATE TRUE
       >>WHEN L1C07NOPE IS DEFINED
           DISPLAY "B-WRONG-DEFINED".
       >>WHEN "X" = "Y"
           DISPLAY "B-WRONG-XY".
       >>WHEN OTHER
           DISPLAY "B-OTHER".
       >>END-EVALUATE
       >>EVALUATE TRUE
       >>WHEN 1 = 2
           DISPLAY "C-WRONG-1-2".
       >>WHEN L1C07VV NOT = 3
           DISPLAY "C-WRONG-NOT-3".
       >>END-EVALUATE
           DISPLAY "C-AFTER-NO-OTHER".
           STOP RUN.
