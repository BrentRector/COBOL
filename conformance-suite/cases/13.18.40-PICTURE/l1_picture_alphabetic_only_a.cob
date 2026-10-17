      *>--- conformance-case
      *> id: l1_picture_alphabetic_only_a
      *> rules: [GR-13.18.40.4-5]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.40.4 GR5 PICTURE clause — "To define an item as
      *> alphabetic, character-string-1 shall contain only one or more
      *> occurrences of the symbol 'A'."
      *>   python scripts/spec/cite.py --check 13.18.40.4 "To define an
      *>   item as alphabetic, character-string-1 shall contain only
      *>   one or more occurrences of the symbol 'A'."
      *>   -> OK  §13.18.40.4 5)  (General rules)
      *>
      *> HOW A CATEGORY IS OBSERVED. GR5 assigns a CATEGORY, and the
      *> INITIALIZE statement selects its receiving operands BY
      *> category, so a category-named REPLACING phrase is a direct
      *> readout of the classification:
      *>   python scripts/spec/cite.py --check 14.9.20.4 "The REPLACING
      *>   phrase is specified and the category of the elementary data
      *>   item is one of the categories specified in the REPLACING
      *>   phrase"   -> OK  §14.9.20.4 5) c) 1.  [item 2.]
      *>   python scripts/spec/cite.py --check 14.9.20.4 "the
      *>   sending-operand is the literal-1 or identifier-2 associated
      *>   with the category specified in the REPLACING phrase."
      *>   -> OK  §14.9.20.4 6)  (General rules)  [b]
      *> An elementary item is a receiving operand of REPLACING
      *> ALPHABETIC exactly when its category is alphabetic, and then
      *> literal-1 is moved to it (§14.9.20.4 GR4's implicit MOVE).
      *>
      *> ⛔ GR5 IS AN "ONLY" TEST AND BOTH HALVES ARE PINNED HERE.
      *>   G-A  PIC AAA  — three occurrences of 'A' and nothing else,
      *>                   so alphabetic: the "one or more" half.
      *>   G-S  PIC A    — a single occurrence, still alphabetic: the
      *>                   "one" boundary.
      *>   G-M  PIC AA9  — contains 'A' but NOT ONLY 'A', so it is NOT
      *>                   alphabetic; §13.18.40.4 GR6 makes it
      *>                   alphanumeric ("a combination of symbols from
      *>                   the set 'A', 'X', and '9', that includes ...
      *>                   at least two different symbols from this
      *>                   set" — 'A' and '9').
      *>   G-X  PIC XXX  — alphanumeric by the same GR6 ("at least one
      *>                   symbol 'X'"), the control that carries no
      *>                   'A' at all.
      *> Line 1 (after REPLACING ALPHABETIC DATA BY "ZZZ") therefore
      *> reads A1=[ZZZ] S1=[Z] X1=[STU] M1=[VW7]: only the two
      *> all-'A' items are written, and G-S is one character so
      *> §14.6.8.5 truncates "ZZZ" on the right to Z.
      *> Line 2 (after REPLACING ALPHANUMERIC DATA BY "###") reads
      *> A2=[ZZZ] S2=[Z] X2=[###] M2=[###]: the complement is written
      *> and the alphabetic items are untouched.
      *> The two lines together are what makes the test an ONLY test —
      *> an implementation that classified PIC AA9 as alphabetic would
      *> put ### into M on line 1, and one that classified PIC AAA as
      *> alphanumeric would overwrite A on line 2.
      *>
      *> §14.9.20.3 SR4 is satisfied for both arms: Table 16 (§14.9.25)
      *> gives Yes for an alphanumeric sending operand into both an
      *> alphabetic and an alphanumeric receiving operand, so both
      *> literals are valid MOVE senders for their category.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1PALPH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 G-A PIC AAA.
          05 G-S PIC A.
          05 G-X PIC XXX.
          05 G-M PIC AA9.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "PQR" TO G-A.
           MOVE "K" TO G-S.
           MOVE "STU" TO G-X.
           MOVE "VW7" TO G-M.
           INITIALIZE G REPLACING ALPHABETIC DATA BY "ZZZ".
           DISPLAY "A1=[" G-A "] S1=[" G-S "] X1=[" G-X
               "] M1=[" G-M "]".
           INITIALIZE G REPLACING ALPHANUMERIC DATA BY "###".
           DISPLAY "A2=[" G-A "] S2=[" G-S "] X2=[" G-X
               "] M2=[" G-M "]".
           STOP RUN.
