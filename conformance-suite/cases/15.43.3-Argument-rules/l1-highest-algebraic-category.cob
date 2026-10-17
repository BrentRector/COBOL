*>--- conformance-case
*> id: l1-highest-algebraic-category
*> rules: [AR-15.43.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.43.3 r1 — "Argument-1 shall be a data item of category numeric or numeric-edited and shall
      *> not be an integer function or numeric function."
      *>
      *> THE CATEGORY CONJUNCT, which no fixture in the corpus held. WS-X IS a data item, so it satisfies the
      *> first half; what it fails is the second. §8.5.2.1 Table 2 gives an elementary item described PIC X
      *> the category ALPHANUMERIC — neither of the two categories r1 admits. The distinction is not
      *> decorative: r1 admits numeric-EDITED precisely because §8.5.2.1 Table 2 also puts a display
      *> numeric-edited item in CLASS alphanumeric, so a screen written on CLASS instead of CATEGORY would
      *> have to accept this item to keep accepting `$**,**9.99` (which
      *> conformance:cases/15.43.4-Returned-value-rules/l1_highest_algebraic_note_table requires it to accept).
      *>
      *> The rule's other two conjuncts are pinned separately, since they fail for different reasons: a
      *> non-data-item argument by conformance:cases/15.43.3-Argument-rules/l1-highest-algebraic-literal, and the function
      *> exclusion by conformance:cases/15.43.3-Argument-rules/l1-highest-algebraic-intrinsic-arg (intrinsic) and
      *> conformance:cases/15.43.3-Argument-rules/algebraic-udf-argument (user-defined).
      *> Expected: COBOLNET1516, the algebraic-family argument diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1HACAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC X(4) VALUE "0999".
       01 R    PIC S9(9).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = FUNCTION HIGHEST-ALGEBRAIC(WS-X).
           DISPLAY R.
           STOP RUN.
