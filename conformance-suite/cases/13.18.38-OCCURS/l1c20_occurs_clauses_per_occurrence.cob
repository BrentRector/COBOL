      *>--- conformance-case
      *> id: l1c20_occurs_clauses_per_occurrence
      *> rules: [GR-13.18.38.4-1, GR-13.18.38.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.38.4 GR1, GR4 — every data description clause of an
      *> OCCURS entry applies to each occurrence (formats 1 and 2); a
      *> format 1 table has exactly integer-2 occurrences.
      *>
      *> THE RULES.
      *> §13.18.38.4 GR1: "Except for the OCCURS clause itself, all
      *>   data description clauses associated with an item whose
      *>   description includes an OCCURS clause apply to each
      *>   occurrence of the item described."
      *>   OK  §13.18.38.4 1)  (General rules)
      *> §13.18.38.4 GR4: "The value of integer-2 represents the fixed
      *>   number of occurrences of the subject of the entry."
      *>   OK  §13.18.38.4 4)  (General rules)
      *> §13.18.38.4 GR7: "The value of the data item referenced by
      *>   data-name-1 represents the current number of occurrences
      *>   of the subject of the entry."
      *>   OK  §13.18.38.4 7)  (General rules)
      *> The per-clause results the derivation applies to EACH
      *> occurrence:
      *> §13.18.40.5 5) "Fixed insertion editing results in the
      *>   insertion character(s) occupying the same character
      *>   position(s) in the edited item as the associated symbol
      *>   occupies in character-string-1." Table 8: '-' gives space
      *>   for a positive or zero value, '-' for a negative value.
      *>   OK  §13.18.40.5 5)  (Editing rules)
      *> §13.18.40.5 7) "If the symbol 'Z' is used, the replacement
      *>   character is the character space"
      *>   OK  §13.18.40.5 7)  (Editing rules)
      *> §13.18.32.4 GR2: "the data is aligned at the rightmost
      *>   character position ... with ... space fill for the
      *>   leftmost character positions."
      *>   OK  §13.18.32.4 2)  (General rules)
      *> §13.18.8.4 GR1: "the content of the data item is set to all
      *>   spaces when the item is a receiving operand and the value
      *>   being stored is zero."
      *>   OK  §13.18.8.4 1)  (General rules)
      *> §13.18.52.4 GR6 b) SIGN ... SEPARATE: "The operational signs
      *>   for positive and negative are the basic special
      *>   characters '+' and '-', respectively." (6 a): the sign is
      *>   the leading character position.)
      *>   OK  §13.18.52.4 6) b)  (General rules)
      *> §14.6.8.5: alphanumeric receiving data is "aligned at the
      *>   leftmost character position in the data item with space
      *>   fill or truncation to the right".
      *>   OK  §14.6.8.5   (Receiving data items of categories ...)
      *>
      *> DERIVATION. A wrong implementation that applied a clause only
      *> to the first occurrence (or to none) changes the bracketed
      *> image of the whole table.
      *> E=[-005 007-005]  PIC -9(3): -5, 7, -5 each edited 4 chars.
      *> J=[   A  BC DEF]  JUSTIFIED RIGHT X(4): "A", "BC", "DEF" each
      *>                   right-aligned, space filled on the left.
      *> B=[   012   ]     BLANK WHEN ZERO 9(3): 0, 12, 0 -> spaces,
      *>                   012, spaces.
      *> S=[+000-004+025]  SIGN LEADING SEPARATE S9(3): 0, -4, 25.
      *> V=[ 510 0]        format 2, N = 3 occurrences of Z9: 5, 10, 0
      *>                   -> " 5", "10", " 0" (GR7: 3 occurrences).
      *> G6=[ABCDEFG]      GR4: F OCCURS 3 of X(2) is 6 characters, so
      *>                   G6 (F + F-T) is 7; "ABCDEFGH" is truncated
      *>                   on the right to 7.
      *> F3=[EF] FT=[G]    occurrence 3 is characters 5-6; F-T is 7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C20G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 E  PIC -9(3) OCCURS 3 TIMES.
       01 G2.
          05 J  PIC X(4) JUSTIFIED RIGHT OCCURS 3 TIMES.
       01 G3.
          05 B  PIC 9(3) BLANK WHEN ZERO OCCURS 3 TIMES.
       01 G4.
          05 S  PIC S9(3) SIGN LEADING SEPARATE OCCURS 3 TIMES.
       01 N     PIC 9 VALUE 3.
       01 G5.
          05 V  PIC Z9 OCCURS 1 TO 4 TIMES DEPENDING ON N.
       01 G6.
          05 F  PIC X(2) OCCURS 3 TIMES.
          05 F-T PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE -5 TO E (1).
           MOVE 7 TO E (2).
           MOVE -5 TO E (3).
           DISPLAY "E=[" G1 "]".
           MOVE "A" TO J (1).
           MOVE "BC" TO J (2).
           MOVE "DEF" TO J (3).
           DISPLAY "J=[" G2 "]".
           MOVE 0 TO B (1).
           MOVE 12 TO B (2).
           MOVE 0 TO B (3).
           DISPLAY "B=[" G3 "]".
           MOVE 0 TO S (1).
           MOVE -4 TO S (2).
           MOVE 25 TO S (3).
           DISPLAY "S=[" G4 "]".
           MOVE 5 TO V (1).
           MOVE 10 TO V (2).
           MOVE 0 TO V (3).
           DISPLAY "V=[" G5 "]".
           MOVE "ABCDEFGH" TO G6.
           DISPLAY "G6=[" G6 "]".
           DISPLAY "F3=[" F (3) "] FT=[" F-T "]".
           STOP RUN.
