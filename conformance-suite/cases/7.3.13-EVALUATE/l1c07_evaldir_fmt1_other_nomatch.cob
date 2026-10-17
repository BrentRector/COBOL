      *>--- conformance-case
      *> id: l1c07_evaldir_fmt1_other_nomatch
      *> rules: [GR-7.3.13.4-5, GR-7.3.13.4-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.13.4 GR5 GR6 — format 1 EVALUATE directive with no
      *> TRUE WHEN: WHEN OTHER text-2 included; with no WHEN OTHER,
      *> every WHEN text-1 omitted.
      *> RULE 7.3.13.4 GR5: "If no WHEN phrase evaluates to TRUE, all
      *>   lines of text-2 associated with the WHEN OTHER phrase, if
      *>   specified, are included in the resultant text. All lines of
      *>   text-1 associated with other WHEN phrases are omitted from
      *>   the resultant text."
      *> RULE 7.3.13.4 GR6: "If the END-EVALUATE phrase is reached
      *>   without any WHEN phrase evaluating to TRUE, and without
      *>   encountering a WHEN OTHER phrase, all lines of text-1
      *>   associated with all WHEN phrases are omitted from the
      *>   resultant text."
      *> cite.py:
      *>   OK  §7.3.13.4 5)  (General rules)
      *>   OK  §7.3.13.4 6)  (General rules)
      *> EXPECTED OUTPUT, DERIVED:
      *>   A-OTHER-LINE-1   subject 5: not = 1, not in 2 THRU 4, so
      *>   A-OTHER-LINE-2   every line of WHEN OTHER text-2 is included
      *>                    and both WHEN text-1s omitted (GR5).
      *>  A-AFTER          text after END-EVALUATE is ordinary text.
      *>  B-AFTER-NO-OTHER subject 5 matches neither WHEN 1 nor WHEN 2
      *>                   and there is no WHEN OTHER: both text-1s are
      *>                   omitted (GR6); only the following line shows.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C07F.
       PROCEDURE DIVISION.
       MAIN-PARA.
       >>EVALUATE 5
       >>WHEN 1
           DISPLAY "A-WRONG-1".
       >>WHEN 2 THRU 4
           DISPLAY "A-WRONG-2-4".
       >>WHEN OTHER
           DISPLAY "A-OTHER-LINE-1".
           DISPLAY "A-OTHER-LINE-2".
       >>END-EVALUATE
           DISPLAY "A-AFTER".
       >>EVALUATE 5
       >>WHEN 1
           DISPLAY "B-WRONG-1".
       >>WHEN 2
           DISPLAY "B-WRONG-2".
       >>END-EVALUATE
           DISPLAY "B-AFTER-NO-OTHER".
           STOP RUN.
