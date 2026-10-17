      *>--- conformance-case
      *> id: l1c15_inspect_converting_duplicate_first
      *> rules: [GR-14.9.22.4-23]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.22.4 GR23 — CONVERTING: a character repeated in
      *> the FROM operand is replaced per its FIRST occurrence.
      *> Rule: "If the same character appears more than once in the
      *> data item referenced by identifier-6 or in literal-4, the
      *> first occurrence of the character is used for replacement."
      *> cite.py: OK  §14.9.22.4 23)  (General rules)
      *> DERIVATION (both FROM arms, literal-4 and identifier-6):
      *>  L1: V1 = "abcabc" CONVERTING "aab" TO "XYZ".  'a' occurs at
      *>   FROM positions 1 and 2; the first (1) pairs with 'X'.  'b'
      *>   pairs with 'Z'; 'c' is not in FROM and is unchanged.
      *>   => "XZcXZc".  (A last-occurrence reading gives "YZcYZc".)
      *>  L2: V2 = "abcabc" CONVERTING FROMS ("bab") TO TOS ("123").
      *>   'b' first at FROM position 1 -> '1'; 'a' at position 2 ->
      *>   '2'; 'c' unchanged => "21c21c".  (Last occurrence of 'b' is
      *>   position 3 -> '3', which would give "23c23c".)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C15A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V1    PIC X(6) VALUE "abcabc".
       01 V2    PIC X(6) VALUE "abcabc".
       01 FROMS PIC X(3) VALUE "bab".
       01 TOS   PIC X(3) VALUE "123".
       PROCEDURE DIVISION.
       MAIN-P.
           INSPECT V1 CONVERTING "aab" TO "XYZ".
           DISPLAY "L1=" V1.
           INSPECT V2 CONVERTING FROMS TO TOS.
           DISPLAY "L2=" V2.
           STOP RUN.
