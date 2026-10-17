      *>--- conformance-case
      *> id: l1c22_pd_exec_order_nosections
      *> rules: [GR-14.2.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.2.3 GR1 — Format 2: execution begins with the sentence
      *> that precedes the first paragraph, then presentation order.
      *> RULE (14.2.3 GR1): "Execution begins with the first statement
      *> of the procedure division, excluding declaratives. Statements
      *> are then executed in the order in which they are presented for
      *> compilation, except where the rules indicate some other order."
      *> cite.py --check 14.2.3 "Execution begins with the first
      *>   statement of the procedure division, excluding declaratives"
      *>   -> OK  §14.2.3 1)  (General rules)
      *> cite.py --check 14.2.3 "Statements are then executed in the
      *>   order in which they are presented for compilation"
      *>   -> OK  §14.2.3 1)  (General rules)
      *> Format 2 (14.2.1): procedure-division-header [ sentence ] ...
      *> [ { paragraph-name-1. [ sentence ] ... } ... ].
      *> DERIVATION of every output line:
      *>  The first statement is DISPLAY "1 HEADER", in the unnamed
      *>  sentence before any paragraph: it runs first. The next
      *>  statement in presentation order is in the same sentence:
      *>  "2 HEADER". Then Q-PARA ("3 Q-PARA"), then C-PARA
      *>  ("4 C-PARA") and STOP RUN. An implementation that began at
      *>  the first paragraph would omit both HEADER lines; one that
      *>  ordered paragraphs by name would print C-PARA before Q-PARA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C22E.
       PROCEDURE DIVISION.
           DISPLAY "1 HEADER"
           DISPLAY "2 HEADER".
       Q-PARA.
           DISPLAY "3 Q-PARA".
       C-PARA.
           DISPLAY "4 C-PARA".
           STOP RUN.
