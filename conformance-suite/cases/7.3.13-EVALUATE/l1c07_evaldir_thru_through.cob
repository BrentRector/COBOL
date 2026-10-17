      *>--- conformance-case
      *> id: l1c07_evaldir_thru_through
      *> rules: [SR-7.3.13.3-13]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.13.3 SR13 — EVALUATE directive: THRU and THROUGH are
      *> equivalent spellings of the range phrase.
      *> RULE 7.3.13.3 SR13: "The words THROUGH and THRU are
      *>   equivalent."
      *> RULE 7.3.13.4 GR4 b): "a TRUE result is returned if the
      *>   selection subject lies in the inclusive range ..."
      *> cite.py:
      *>   OK  §7.3.13.3 13)  (Syntax rules)
      *>   OK  §7.3.13.4 4) b)  (General rules)
      *> EXPECTED OUTPUT, DERIVED (each block has the same ranges; only
      *> the spelling differs, so SR13 requires the same branch):
      *>   A2-THRU             5 not in 1 THRU 4; 5 in 5 THRU 9.
      *>   B2-THROUGH          5 not in 1 THROUGH 4; 5 in 5 THROUGH 9.
      *>   C2-THRU-LOW-END     9 not in 1 THRU 8; 9 in 9 THRU 12 (the
      *>                       range is inclusive at its low end).
      *>   D1-THROUGH-HIGH-END 4 in 1 THROUGH 4 (inclusive high end).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C07B.
       PROCEDURE DIVISION.
       MAIN-PARA.
       >>EVALUATE 5
       >>WHEN 1 THRU 4
           DISPLAY "A1".
       >>WHEN 5 THRU 9
           DISPLAY "A2-THRU".
       >>WHEN OTHER
           DISPLAY "A3".
       >>END-EVALUATE
       >>EVALUATE 5
       >>WHEN 1 THROUGH 4
           DISPLAY "B1".
       >>WHEN 5 THROUGH 9
           DISPLAY "B2-THROUGH".
       >>WHEN OTHER
           DISPLAY "B3".
       >>END-EVALUATE
       >>EVALUATE 9
       >>WHEN 1 THRU 8
           DISPLAY "C1".
       >>WHEN 9 THRU 12
           DISPLAY "C2-THRU-LOW-END".
       >>WHEN OTHER
           DISPLAY "C3".
       >>END-EVALUATE
       >>EVALUATE 4
       >>WHEN 1 THROUGH 4
           DISPLAY "D1-THROUGH-HIGH-END".
       >>WHEN OTHER
           DISPLAY "D2".
       >>END-EVALUATE
           STOP RUN.
