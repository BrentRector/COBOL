      *>--- conformance-case
      *> id: l1c20-options-clause-no-period
      *> rules: [SR-11.9.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §11.9.3 SR1 — an OPTIONS paragraph clause without the
      *> terminating separator period.
      *> "1) If any of the clauses are specified, then there shall be
      *>   a terminating separator period."
      *>   OK  §11.9.3 1)  (Syntax rule)
      *> The ARITHMETIC IS NATIVE clause (a clause of every edition
      *> that has the OPTIONS paragraph, §11.9.5.1) is written and no
      *> separator period follows it before the DATA DIVISION header,
      *> so SR1 is violated. This is a pure general-format rule, so the
      *> expected rejection is the parser's missing-period diagnostic
      *> COBOL0307.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C20B.
       OPTIONS.
           ARITHMETIC IS NATIVE
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY W.
           STOP RUN.
