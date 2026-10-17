*>--- conformance-case
*> id: pb3220_editing_for_mixed_floating_fixed_2023
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3220 (review round 1, finding R1-1) - ISO 1989:2023 13.18.40.4 GR14, the 'es' entry: "For extended
      *> editing sign control symbols with fixed insertion, each occurrence of the character(s) specified in the
      *> associated literal are counted in the size of the item. For floating inserting, one occurrence of literal-2
      *> or literal-3 is counted in the size of the item plus one character for each repetition of character-1."
      *>
      *> One character-1 can hold BOTH kinds: PIC LL9L is a floating string of two L (13.18.40.5 rule 6, a single
      *> extended editing sign control symbol under 13.18.40.3 SR24) followed by a FIXED L (rule 5), the second symbol
      *> SR24 admits. Each occurrence is counted and rendered by its own kind: the floating string is one literal
      *> plus one character, the fixed occurrence the literal's width, and only the floating L after the first is a
      *> digit position. Before the fix the whole rule was read as floating: the trailing L was a third digit
      *> position, never rendered its negative literal, and the item was 6 characters instead of 8.
      *>
      *> S01 LL9L, NEGATIVE "DB " - floating LL = 3 + 1, the 9 = 1, the fixed L = 3: size 8; digit positions 2.
      *>     -125 keeps the low-order two digits (14.6.8.2): "DB " + "25" + "DB ".
      *>     -5 puts the literal right-aligned in the floating string's four columns, immediately before the 5.
      *>     5 is positive: the defaulted POSITIVE literal is three spaces at each place.
      *> S02 LLL9L, NEGATIVE "((" - floating LLL = 2 + 2, the 9 = 1, the fixed L = 2: size 7; digit positions 3.
      *> The group G adds the one-character X to S01: 9.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3220MIX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 S01 PIC LL9L EDITING L FOR NEGATIVE IS "DB ".
          05 T PIC X VALUE "|".
       01 R PIC X(12).
       01 S02 PIC LLL9L EDITING L FOR NEGATIVE IS "((".
       PROCEDURE DIVISION.
           MOVE -125 TO S01
           DISPLAY "S01=[" S01 "] LEN=" FUNCTION LENGTH(S01)
                   " GROUP LEN=" FUNCTION LENGTH(G)
           MOVE G TO R
           DISPLAY "R=[" R "]"
           MOVE -5 TO S01
           DISPLAY "S01 -5=[" S01 "]"
           MOVE 5 TO S01
           DISPLAY "S01 +5=[" S01 "]"
           MOVE -12 TO S02
           DISPLAY "S02=[" S02 "] LEN=" FUNCTION LENGTH(S02)
           MOVE 7 TO S02
           DISPLAY "S02 +7=[" S02 "]"
           STOP RUN.
