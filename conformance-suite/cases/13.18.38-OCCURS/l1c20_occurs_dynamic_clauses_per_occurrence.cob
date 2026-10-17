      *>--- conformance-case
      *> id: l1c20_occurs_dynamic_clauses_per_occurrence
      *> rules: [GR-13.18.38.4-1]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.38.4 GR1 — every data description clause of an
      *> OCCURS DYNAMIC (format 4) entry applies to each occurrence.
      *>
      *> THE RULES.
      *> §13.18.38.4 GR1 (FORMATS 1, 2 AND 4): "Except for the OCCURS
      *>   clause itself, all data description clauses associated
      *>   with an item whose description includes an OCCURS clause
      *>   apply to each occurrence of the item described."
      *>   OK  §13.18.38.4 1)  (General rules)
      *> §8.5.1.9.2: a receiving reference within the current capacity
      *>   behaves "the same as for a fixed-capacity table whose
      *>   number of occurrences is the current capacity".
      *>   OK  §8.5.1.9.2   (Operations on a single element)
      *> The per-clause results applied to EACH occurrence:
      *> §13.18.40.5 5) + Table 8: '-' gives space for a positive or
      *>   zero value, '-' for a negative value.
      *>   OK  §13.18.40.5 5)  (Editing rules)
      *> §13.18.32.4 GR2: JUSTIFIED data "is aligned at the rightmost
      *>   character position ... with ... space fill for the
      *>   leftmost character positions."
      *>   OK  §13.18.32.4 2)  (General rules)
      *> §13.18.8.4 GR1: BLANK WHEN ZERO "the content of the data item
      *>   is set to all spaces when the item is a receiving operand
      *>   and the value being stored is zero."
      *>   OK  §13.18.8.4 1)  (General rules)
      *> §13.18.52.4 GR6 b) SIGN ... SEPARATE: "The operational signs
      *>   for positive and negative are the basic special
      *>   characters '+' and '-', respectively."
      *>   OK  §13.18.52.4 6) b)  (General rules)
      *> The companion 85 golden l1c20_occurs_clauses_per_occurrence
      *> pins formats 1 and 2.
      *>
      *> DERIVATION (FROM 3: current capacity 3, every occurrence set
      *> before it is displayed).
      *> E=[-005][ 007][-005]  PIC -9(3): -5, 7, -5.
      *> J=[   A][  BC][ DEF]  JUSTIFIED RIGHT X(4): "A", "BC", "DEF".
      *> B=[   ][012][   ]     BLANK WHEN ZERO 9(3): 0, 12, 0.
      *> S=[+000][-004][+025]  SIGN LEADING SEPARATE S9(3): 0, -4, 25.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C20H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 E  PIC -9(3) OCCURS DYNAMIC FROM 3 TO 9.
       01 G2.
          05 J  PIC X(4) JUSTIFIED RIGHT OCCURS DYNAMIC FROM 3 TO 9.
       01 G3.
          05 B  PIC 9(3) BLANK WHEN ZERO OCCURS DYNAMIC FROM 3 TO 9.
       01 G4.
          05 S  PIC S9(3) SIGN LEADING SEPARATE
                OCCURS DYNAMIC FROM 3 TO 9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE -5 TO E (1).
           MOVE 7 TO E (2).
           MOVE -5 TO E (3).
           DISPLAY "E=[" E (1) "][" E (2) "][" E (3) "]".
           MOVE "A" TO J (1).
           MOVE "BC" TO J (2).
           MOVE "DEF" TO J (3).
           DISPLAY "J=[" J (1) "][" J (2) "][" J (3) "]".
           MOVE 0 TO B (1).
           MOVE 12 TO B (2).
           MOVE 0 TO B (3).
           DISPLAY "B=[" B (1) "][" B (2) "][" B (3) "]".
           MOVE 0 TO S (1).
           MOVE -4 TO S (2).
           MOVE 25 TO S (3).
           DISPLAY "S=[" S (1) "][" S (2) "][" S (3) "]".
           STOP RUN.
