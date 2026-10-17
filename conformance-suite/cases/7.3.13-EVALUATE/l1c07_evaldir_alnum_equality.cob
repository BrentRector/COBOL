      *>--- conformance-case
      *> id: l1c07_evaldir_alnum_equality
      *> rules: [GR-7.3.13.4-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §7.3.13.4 GR7 — alphanumeric / national literal subject of
      *> the EVALUATE directive: binary per-character equality, and
      *> literals of unequal length are unequal (no space padding).
      *> RULE 7.3.13.4 GR7: "If literal-1 is an alphanumeric or national
      *>   literal, a character by character comparison for equality
      *>   based on the binary value of each character's encoding is
      *>   used. If the literals are of unequal length they are not
      *>   equal."
      *> cite.py:
      *>   OK  §7.3.13.4 7)  (General rules)
      *> EXPECTED OUTPUT, DERIVED:
      *>  A-EXACT           "ABC" vs "ABC ": unequal length, not equal;
      *>                    vs "abc": encodings differ (no collating
      *>                    sequence, no case folding), not equal;
      *>                    vs "ABC": equal.
      *>  B-UNEQUAL-LENGTH  "AB " vs "AB": unequal length, not equal
      *>                    (a run-time comparison would pad with spaces
      *>                    and call them equal), so WHEN OTHER.
      *>  C-NATIONAL-EXACT  N"XY" vs N"XY ": unequal length, not equal;
      *>                    vs N"XY": equal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C07G.
       PROCEDURE DIVISION.
       MAIN-PARA.
       >>EVALUATE "ABC"
       >>WHEN "ABC "
           DISPLAY "A-WRONG-PADDED".
       >>WHEN "abc"
           DISPLAY "A-WRONG-CASE".
       >>WHEN "ABC"
           DISPLAY "A-EXACT".
       >>WHEN OTHER
           DISPLAY "A-WRONG-OTHER".
       >>END-EVALUATE
       >>EVALUATE "AB "
       >>WHEN "AB"
           DISPLAY "B-WRONG-SHORTER".
       >>WHEN OTHER
           DISPLAY "B-UNEQUAL-LENGTH".
       >>END-EVALUATE
       >>EVALUATE N"XY"
       >>WHEN N"XY "
           DISPLAY "C-WRONG-PADDED".
       >>WHEN N"XY"
           DISPLAY "C-NATIONAL-EXACT".
       >>WHEN OTHER
           DISPLAY "C-WRONG-OTHER".
       >>END-EVALUATE
           STOP RUN.
