      *> reject-at: 2023
      *> kb/Work PB529 - SR14's LOWER BOUND, reached through the one shape SR12 a's composition test lets past.
      *> ISO 1989:2023 13.18.40.3 SR14: "For data items of category numeric, and for fixed-point data items of
      *> category numeric-edited, the number of digit positions described by character-string-1 shall range from 1
      *> through 31." SR12 a is satisfied by "at least two occurrences of one of the symbols from the set
      *> character-1 ...", so `PIC LL EDITING L IS ":"` passes the composition validator - but 13.18.40.5 rule 3
      *> makes a character-1 given literal-1 a SIMPLE INSERTION symbol, and a simple insertion symbol is no digit
      *> position (13.18.40.4 GR14 counts only 9, Z, * and the floating symbols, and 'P'), so the picture
      *> describes ZERO digit positions and SR14 refuses it. The digit-capacity screen used to be skipped on
      *> exactly that count (its guard was DigitPositions > 0) and the item was accepted, a numeric-edited item
      *> that holds no digit; it now asks the lower bound itself, COBOLNET2882.
      *> Pinned at 2023: the EDITING phrase is a COBOL-2023 introduction.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB529NCE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NC1 PIC LL EDITING L IS ":".
       PROCEDURE DIVISION.
           STOP RUN.
