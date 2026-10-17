      *>--- conformance-case
      *> id: l1c08-entry-name-subscripted
      *> rules: [SR-13.18.20.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.20.3 SR1 — the entry-name data-name-1 is subscripted
      *> SR1: "Data-name-1 shall not be qualified or subscripted."
      *> cite.py:
      *>   OK  §13.18.20.3 1)  (Syntax rules)
      *> `05 A(1)` names the item being described with a subscripted
      *> data-name; the rest of the program is valid (the same entry
      *> spelled `05 A` compiles), so SR1 is the only reason to reject.
      *> Pure syntax rule: the grammar's declaration slot takes a single
      *> word, so the rejection is the generic parse error.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C08R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A(1) PIC X(3).
       PROCEDURE DIVISION.
       R-MAIN.
           DISPLAY "NOT-REACHED".
           STOP RUN.
