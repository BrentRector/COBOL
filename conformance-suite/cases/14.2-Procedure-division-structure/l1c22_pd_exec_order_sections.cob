      *>--- conformance-case
      *> id: l1c22_pd_exec_order_sections
      *> rules: [GR-14.2.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §14.2.3 GR1 — Format 1: execution skips the declaratives,
      *> starts at the first statement, then follows presentation order.
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
      *> Format 1 (14.2.1) lets a section hold zero sentences and lets
      *> sentences precede the section's first paragraph.
      *> DERIVATION of every output line:
      *>  The textually first statement (DISPLAY "DECL") is in the
      *>  declaratives, so it is excluded; no I-O error ever occurs, so
      *>  the USE procedure never runs and DECL never prints.
      *>  E-SEC is empty, so the first statement of the procedure
      *>  division is the unnamed sentence opening Z-SEC: "1 Z-SEC".
      *>  Presentation order then runs Y-PARA ("2 Y-PARA"), falls into
      *>  the next section A-SEC and its paragraphs X-PARA ("3 X-PARA")
      *>  and B-PARA ("4 B-PARA"), which ends the run. Names are chosen
      *>  in reverse alphabetic order so that no order but presentation
      *>  order yields 1, 2, 3, 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C22D.
       DATA DIVISION.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       D-PARA.
           DISPLAY "DECL".
       END DECLARATIVES.
       E-SEC SECTION.
       Z-SEC SECTION.
           DISPLAY "1 Z-SEC".
       Y-PARA.
           DISPLAY "2 Y-PARA".
       A-SEC SECTION.
       X-PARA.
           DISPLAY "3 X-PARA".
       B-PARA.
           DISPLAY "4 B-PARA".
           STOP RUN.
