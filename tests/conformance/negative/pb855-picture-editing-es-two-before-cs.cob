      *> reject-at: 2023
      *> kb/Work PB855 - ISO 1989:2023 13.18.40.3 SR24 and SR25 (second sentences) and SR26 (second sentence).
      *> SR26: "For extended editing sign control, the currency symbol when used shall be either the leftmost symbol
      *> in character-string-1, optionally preceded by character-1" - ONE character-1, not two. SR25: "When
      *> extended editing sign control symbols are used and two are specified, the first occurrence of the EDITING
      *> phrase shall be for the leftmost symbol in character-string-1 and the second occurrence shall be for the
      *> rightmost symbol in character-string-1." So two extended symbols are the string's two ENDS, and
      *> PE02 LF$999 - 'L' leftmost, 'F' second and the currency symbol third - puts the second symbol's character-1
      *>     nowhere near the rightmost symbol. It bound clean before kb/Work PB855 (the rule was read as constraining
      *>     only the order of the phrases) and rendered MOVE -12 as "()$012". COBOLNET1984, 2023 only.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB855TWO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PE02 PIC LF$999 EDITING L FOR NEGATIVE IS "("
                          EDITING F FOR NEGATIVE IS ")".
       PROCEDURE DIVISION.
           STOP RUN.
