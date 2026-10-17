*>--- conformance-case
*> id: pb344_use_f3_perform_preempt
*> rules: [GR-9.1.12-1, GR-9.1.12-2, GR-14.9.49.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB344 — the ISO edition the compilation targets reaches the file connectors and the
      *> generated USE-declarative selector.  Until PB344 no edition reached either, so the 2023 rules
      *> were served to --std 85, 2002 and 2014 as well.
      *>
      *> THE RULES, and every expected value below derived from them (no observation):
      *>  14.9.49.4 GR6's leading clause — "If the input-output statement that raised the exception was
      *>    specified in imperative-statement-1 of an exception-checking PERFORM statement and the exception
      *>    condition did not match the criteria in any WHEN phrase" — the USE declarative is selected only on
      *>    the NO-MATCH side.  Q1 is the match side (the WHEN runs, the declarative does not); Q2 is the
      *>    no-match side (the declarative runs).
      *>  14.9.30.4 GR24 a) — the at end condition sets '10' and raises EC-I-O-AT-END (9.1.13.1).
      *>  14.9.33.4 GR2 — RESUME NEXT STATEMENT continues with the statement following the raising one, which
      *>    is why Q1-AFTER follows Q1-WHEN.
      *>
      *> EDITIONS: 2023 only — the exception-checking PERFORM is a 2023 facility (Annex E.2 item 19 b) calls
      *> the READ half of this change "part of the inline-exception-checking PERFORM enhancement"), so this
      *> program has no earlier-edition copy.  kb/Work PB344 closes GR-14.9.49.4-6's untested half with it.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB344F23.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb344f23.dat"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS F-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-IN SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       D-IN-P.
           DISPLAY "DECL-IN".
       END DECLARATIVES.
       MAIN SECTION.
       M-SEED.
           OPEN OUTPUT F
           WRITE F-REC FROM "AAAA"
           CLOSE F.
       M-Q1.
      *> Q1 — a WHEN phrase matches the raised EC-I-O-AT-END: it preempts and IGNORES the declarative.
           OPEN INPUT F
           PERFORM
               READ F
               READ F
               DISPLAY "Q1-AFTER"
           WHEN EC-I-O-AT-END
               DISPLAY "Q1-WHEN"
               RESUME NEXT STATEMENT
           END-PERFORM
           CLOSE F.
       M-Q2.
      *> Q2 — no WHEN phrase matches: the USE declarative tiers are selected instead.
           OPEN INPUT F
           PERFORM
               READ F
               READ F
               DISPLAY "Q2-AFTER"
           WHEN EC-SIZE-OVERFLOW
               DISPLAY "Q2-WHEN"
               RESUME NEXT STATEMENT
           END-PERFORM
           CLOSE F
           STOP RUN.
