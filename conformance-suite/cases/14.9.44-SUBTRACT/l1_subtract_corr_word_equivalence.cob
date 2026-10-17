      *>--- conformance-case
      *> id: l1_subtract_corr_word_equivalence
      *> rules: [SR-14.9.44.3-5]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.9.44.3 SR5 (FORMAT 3) — "The words CORR and
      *> CORRESPONDING are equivalent."
      *> ⛔ THE RULE IS ABOUT THE TWO SPELLINGS, SO THE GOLDEN WRITES
      *> BOTH.  A fixture that spells only one of them cannot
      *> distinguish "CORR and CORRESPONDING are equivalent" from
      *> "only the word I happened to write is implemented", which is
      *> the entire content of SR5; the two statements below are
      *> IDENTICAL apart from the word, and run over two receiving
      *> groups seeded identically, so any difference in the printed
      *> lines is a difference the standard forbids.
      *>
      *> EXPECTED VALUES, DERIVED FROM THE RULE TEXT.  §14.7.6 pairs
      *> P with P and Q with Q — same data-name, same qualifiers up to
      *> but not including the two group operands, both members
      *> numeric (its rule 3), no OCCURS / REDEFINES / RENAMES.
      *> §14.9.44.4 GR3: "data items in identifier-4 are subtracted
      *> from and stored in corresponding items in identifier-5".
      *> §14.9.44.4 GR5: "The results are the same as if the user had
      *> referred to each pair of corresponding identifiers in
      *> separate SUBTRACT statements."  Hence P: 10 - 03 = 07 and
      *> Q: 20 - 07 = 13, into PIC 9(2) receivers (§14.6.8.2 rule 4,
      *> aligned with zero fill) -> 07 and 13 for EACH spelling.
      *> The values are chosen so the two members differ from one
      *> another, so a fixture that matched only the first pair, or
      *> that crossed the pairs, would not print 0713.
      *> The receivers are copied to unqualified items before DISPLAY
      *> only to keep the printed line free of qualification syntax;
      *> a PIC 9(2) to PIC 9(2) move is an identity transfer under
      *> §14.6.8.2 rule 4 and adds nothing to the derivation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1SBCORW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC.
          05 P PIC 9(2) VALUE 03.
          05 Q PIC 9(2) VALUE 07.
       01 D1.
          05 P PIC 9(2) VALUE 10.
          05 Q PIC 9(2) VALUE 20.
       01 D2.
          05 P PIC 9(2) VALUE 10.
          05 Q PIC 9(2) VALUE 20.
       01 A1 PIC 9(2).
       01 B1 PIC 9(2).
       01 A2 PIC 9(2).
       01 B2 PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           SUBTRACT CORRESPONDING SRC FROM D1.
           SUBTRACT CORR SRC FROM D2.
           MOVE P OF D1 TO A1.
           MOVE Q OF D1 TO B1.
           MOVE P OF D2 TO A2.
           MOVE Q OF D2 TO B2.
           DISPLAY "LONG=" A1 B1
           DISPLAY "SHORT=" A2 B2
           STOP RUN.
