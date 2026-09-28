      *> reject-at: 85 2002 2014
      *> kb/Work PB1392 (fixed with PB1390) — the EXCLUSIVE-OR / XOR connective (ISO §8.8.4.9) is a COBOL-2023
      *> addition (Annex E.2 item 25), and an EVALUATE partial expression is "a sequence of COBOL words such that,
      *> were it preceded by the corresponding selection subject, a conditional expression would result"
      *> (§14.9.13.3 SR7 d)) — so an XOR inside one is the same 2023 connective. The introduction gate hung off the
      *> condition's own XOR tier only, so `WHEN > 5 XOR < 3` compiled clean below 2023; it now recognizes the ONE
      *> xorOperator rule every tier spells the connective through (COBOLNET0900).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W68NPX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE A
             WHEN > 5 XOR < 3 DISPLAY "W1"
             WHEN OTHER DISPLAY "W2"
           END-EVALUATE
           STOP RUN.
