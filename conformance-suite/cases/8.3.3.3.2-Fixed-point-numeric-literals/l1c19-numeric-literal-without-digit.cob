      *>--- conformance-case
      *> id: l1c19-numeric-literal-without-digit
      *> rules: [SR-8.3.3.3.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.3.3.2 1) — a numeric literal shall contain at least
      *>   one digit
      *> "A literal shall contain at least one digit."
      *> OK  §8.3.3.3.2 1)  (Fixed-point numeric literals)
      *> ADD +1 TO N is valid (sign, then one digit). ADD + TO N writes
      *>   a
      *> would-be signed literal with no digit, so the source shall be
      *>   rejected
      *> in every edition. This is a pure formation rule: a digitless
      *>   string is
      *> no literal at all, so the rejection is a syntax (parse)
      *>   diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C19K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC S9(4) VALUE 1.
       PROCEDURE DIVISION.
           ADD +1 TO N
           ADD + TO N
           STOP RUN.
