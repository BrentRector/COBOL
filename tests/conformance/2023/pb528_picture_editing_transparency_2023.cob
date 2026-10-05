      *> kb/Work PB528 / PB855 - the COBOL-2023 leg: an IS-form PICTURE EDITING character-1 is DELIBERATELY
      *> TRANSPARENT to the 13.18.40.6 Table 10 precedence walk, and this golden is what holds that determination
      *> in place. 13.18.40.6 gives a Table-10 precedence to 'es' ALONE - "If the EDITING phrase is specified,
      *> the precedence of 'es' as related to Table 10 ... has the same precedence as the 'cs' symbol in the
      *> column and row of non-floating insertion symbols" - and 13.18.40.3 SR12 makes 'es' the EXTENDED
      *> (FOR-phrase) symbol only: "If literal-1 is specified, character-1 is a fixed editing sign control
      *> symbol. If the FOR phrase is specified, character-1 is an extended editing sign control symbol."
      *> So the FOR form takes the 'cs' role (kb/Work PB855: negative/pb855-picture-editing-es-placement and
      *> 2023/pb855_picture_editing_es_placement), except against another currency-like symbol, where SR25
      *> ("the first occurrence of the EDITING phrase ... for the leftmost symbol in character-string-1 and the
      *> second occurrence ... for the rightmost symbol") and SR26 own the order; the IS form has no Table-10
      *> precedence at all (rule 3 makes it simple insertion), so it constrains no neighbour - N01 and N02.
      *>
      *> N01 9L9 EDITING L IS ":" with 12 - 13.18.40.5 rule 3 (simple insertion): character-1 inserts
      *>     literal-1 at each occurrence, sign-independent => "1:2".
      *> N02 9LL EDITING L IS ":" with 5 - two adjacent character-1 after the digit: SR12 a's second bullet
      *>     ("at least two occurrences of one of the symbols from the set character-1 ...") is met, and
      *>     what it proves is the ACCEPT the Table-10 'cs' mapping would have refused (row 'cs' trailing
      *>     has a BLANK 'cs' leading column). Rule 3 makes each character-1 a simple insertion symbol, so
      *>     the item is the digit then ":" at each occurrence => "5::". (kb/Work PB529: this item was `LL`
      *>     with no 9 until the SR14 lower bound became a screen of its own - two simple insertion symbols
      *>     describe ZERO digit positions, and 13.18.40.3 SR14 requires 1 through 31 for a numeric-edited
      *>     item; negative/pb529-picture-no-digit-position pins `PIC LL EDITING L IS ":"` as COBOLNET2882.)
      *> N03 L99F EDITING L FOR NEGATIVE IS "-" EDITING F FOR POSITIVE IS "+" with -12 - SR24's "either
      *>     one or two extended editing sign control symbols may be used" and SR25's leftmost/rightmost
      *>     placement. Table 9: character-1 with the NEGATIVE phrase renders literal-2 over a negative
      *>     value, and the POSITIVE phrase renders spaces over one => "-12 ".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB528EDT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N01 PIC 9L9 EDITING L IS ":".
       01 N02 PIC 9LL EDITING L IS ":".
       01 N03 PIC L99F EDITING L FOR NEGATIVE IS "-"
                       EDITING F FOR POSITIVE IS "+".
       PROCEDURE DIVISION.
           MOVE 12 TO N01
           MOVE 5 TO N02
           MOVE -12 TO N03
           DISPLAY "N01=[" N01 "]"
           DISPLAY "N02=[" N02 "]"
           DISPLAY "N03=[" N03 "]"
           STOP RUN.
