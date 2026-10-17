      *>--- conformance-case
      *> id: l1c16_justified_omitted_national_boolean
      *> rules: [GR-13.18.32.4-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.32.4 GR3 — JUSTIFIED omitted: standard alignment for
      *>   the
      *> national and boolean categories (the COBOL-2002 categories)
      *> GR3 "When the JUSTIFIED clause is omitted, the standard rules
      *>   for
      *>   aligning data within an elementary item apply (see 14.6.8,
      *>     ...)"
      *>   cite.py: OK  §13.18.32.4 3)  (General rules)
      *> §14.6.8.5 (national): "aligned at the leftmost character
      *>   position in
      *>   the data item with space fill or truncation to the right"
      *>   cite.py: OK  §14.6.8.5
      *> §14.6.8.6 (boolean): "into the corresponding boolean positions
      *>   of the
      *>   receiving data item, with zero fill or truncation to the
      *>     right"
      *>   cite.py: OK  §14.6.8.6
      *> Contrast (the clause present), GR2 "aligned at the rightmost
      *>   character
      *>   position or boolean position ... with zero fill for the
      *>     leftmost
      *>   boolean positions and space fill for the leftmost character
      *>     positions"
      *>   cite.py: OK  §13.18.32.4 2)  (General rules)
      *> and GR1 "the leftmost character positions or boolean positions
      *>   of the
      *>   sending operand shall be truncated"
      *>   cite.py: OK  §13.18.32.4 1)  (General rules)
      *> DERIVATION (receivers are 4 positions):
      *>   N"XY"     -> NL  (omitted) [XY  ]   NJ  (JUST) [  XY]
      *>   N"ABCDEF" -> NL  [ABCD]  (right truncated)   NJ [CDEF] (left)
      *>   B"11"     -> BO  (omitted) [1100]   BJ  (JUST) [0011]
      *>   B"101101" -> BO  [1011]  (right truncated)   BJ [1101] (left)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C16K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NL PIC N(4).
       01 NJ PIC N(4) JUST.
       01 BO PIC 1(4).
       01 BJ PIC 1(4) JUST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE N"XY" TO NL NJ.
           DISPLAY "SHORT NL [" NL "] NJ [" NJ "]".
           MOVE N"ABCDEF" TO NL NJ.
           DISPLAY "LONG  NL [" NL "] NJ [" NJ "]".
           MOVE B"11" TO BO BJ.
           DISPLAY "SHORT BO [" BO "] BJ [" BJ "]".
           MOVE B"101101" TO BO BJ.
           DISPLAY "LONG  BO [" BO "] BJ [" BJ "]".
           STOP RUN.
