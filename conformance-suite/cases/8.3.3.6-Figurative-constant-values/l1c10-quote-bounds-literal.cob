      *>--- conformance-case
      *> id: l1c10-quote-bounds-literal
      *> rules: [GR-8.3.3.6.4-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.3.3.6.4 GR8 — QUOTE shall not bound a literal
      *> "The word QUOTE or QUOTES shall not be used in place of a
      *> quotation symbol to bound a literal."
      *> cite.py --check 8.3.3.6.4 "The word QUOTE or QUOTES shall
      *>   not be used in place of a quotation symbol to bound a
      *>   literal" -> OK  §8.3.3.6.4 8)  (General rules)
      *> VALUE QUOTE ABC QUOTE would be the literal "ABC" only if
      *> QUOTE could bound a literal. It cannot, so ABC is a bare
      *> word in the literal-1 position of the VALUE clause
      *> (§13.18.63.2), which names no constant: the source is
      *> rejected. The same item with VALUE "ABC" compiles and
      *> DISPLAYs ABC, so this rule is the only reason to reject.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C10N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(5) VALUE QUOTE ABC QUOTE.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY W.
           STOP RUN.
