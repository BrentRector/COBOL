*>--- conformance-case
*> id: pb3220_editing_for_fixed_occurrences_size_2023
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3220 - ISO 1989:2023 13.18.40.4 GR14, the 'es' entry: "If character-1 is a simple insertion
      *> symbol or a fixed insertion symbol, the size of literal-1 is counted in the size of the item. ... For
      *> extended editing sign control symbols with fixed insertion, each occurrence of the character(s) specified
      *> in the associated literal are counted in the size of the item. For floating inserting, one occurrence of
      *> literal-2 or literal-3 is counted in the size of the item plus one character for each repetition of
      *> character-1." GR4 makes that count the item's SIZE, so FUNCTION LENGTH, the group size and the image a
      *> group MOVE copies must all agree with the image the editing rules render.
      *>
      *> The FOR form's character-1 is FIXED insertion (rule 5) unless two or more identical symbols are adjacent
      *> (rule 6, floating), so the size count has to ask the SAME fixed-or-floating question the renderer asks.
      *> It read every FOR rule as floating, so two FIXED occurrences of a multi-character literal were counted
      *> once: LENGTH 7 for an item that renders 9 characters, shifting every later member of the group.
      *>
      *> S01 L999L, NEGATIVE "DB " - two FIXED occurrences of one character-1, 3 + 3 + 3 = 9; the group adds the
      *>     one-character trailing X, 10. The group MOVE to R holds the rendered image of -123 and the filler.
      *> S02 LLLL9.99, NEGATIVE "DEBIT " - ONE FLOATING string of four L: Annex D.24's own arithmetic, "6 for the
      *>     first 'L', 3 for the next three, and 4 for the numbers" = 13.
      *> S03 99T99T9, IS ":;" - the IS form is simple insertion at EVERY occurrence, 2 + 2 + 2 + 2 + 1 = 9.
      *> S04 L$999L, NEGATIVE "(("  - two fixed occurrences around a currency symbol, 2 + 1 + 3 + 2 = 8.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3220FIX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 S01 PIC L999L EDITING L FOR NEGATIVE IS "DB ".
          05 T PIC X VALUE "|".
       01 R PIC X(12).
       01 S02 PIC LLLL9.99 EDITING L FOR NEGATIVE IS "DEBIT ".
       01 S03 PIC 99T99T9 EDITING T IS ":;".
       01 S04 PIC L$999L EDITING L FOR NEGATIVE IS "((".
       PROCEDURE DIVISION.
           MOVE -123 TO S01
           DISPLAY "S01=[" S01 "] LEN=" FUNCTION LENGTH(S01)
                   " GROUP LEN=" FUNCTION LENGTH(G)
           MOVE G TO R
           DISPLAY "R=[" R "]"
           MOVE 123 TO S01
           DISPLAY "S01 POSITIVE=[" S01 "]"
           MOVE -12.5 TO S02
           DISPLAY "S02=[" S02 "] LEN=" FUNCTION LENGTH(S02)
           MOVE 12345 TO S03
           DISPLAY "S03=[" S03 "] LEN=" FUNCTION LENGTH(S03)
           MOVE -12 TO S04
           DISPLAY "S04=[" S04 "] LEN=" FUNCTION LENGTH(S04)
           STOP RUN.
