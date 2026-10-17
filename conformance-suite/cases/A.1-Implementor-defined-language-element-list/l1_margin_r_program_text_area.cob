      *>--- conformance-case
      *> id: l1_margin_r_program_text_area
      *> rules: [DOC-A.1-158]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §6.3.1 margin R — the rightmost character position of the
      *> program-text area (Annex A.1 item 158; docs/CONFORMANCE.md
      *> DOC-A.1-158). ⛔ THIS FILE IS FIXED-FORM ON PURPOSE: columns 1-6
      *> carry a sequence number and columns 73+ carry text that is NOT
      *> program text and shall be ignored.
      *
      *> THE RULE. §6.3.1: "Margin R is immediately to the right of
      *> the rightmost character position of the program-text area. The
      *> rightmost character position of the program-text area is a
      *> fixed position defined by the implementor." and "The
      *> program-text area begins in character position 8 and
      *> terminates with the character position immediately to the left
      *> of margin R." §6.3.4 lists what the program-text area may
      *> contain; nothing in §6.3 gives any meaning to characters
      *> outside it, and nothing makes them an error.
      *> COBOL.NET's determination: the rightmost character position is
      *> COLUMN 72, so the program-text area is columns 8-72 and columns
      *> 73 onward are outside the program text and are DISCARDED.
      *
      *> STILL-IN / no PAST-R line - the DISCRIMINATOR. Line 000900
      *>   carries a complete, output-producing DISPLAY statement that
      *>   starts at column 73. If margin R stood anywhere to the right
      *>   of column 72 that statement would be program text and the run
      *>   would print PAST-R; the expected output has no such line.
      *>   (A margin R to the LEFT of 72 is refuted by the next leg.)
      *> AT-COL-72 - the BOUNDARY. Line 001000's statement ENDS exactly
      *>   at column 72: its closing period is the 72nd character.
      *>   Column 72 is therefore INSIDE the program-text area; a margin
      *>   R at column 71 or below would cut the period and the program
      *>   would not compile. The two legs pin the rightmost position
      *>   at 72 from both sides.
      *> T - a literal that ENDS inside the area, with characters
      *>   immediately past column 72: they neither extend the literal
      *>   nor become tokens. W-T is PIC X(10) and receives exactly the
      *>   10 in-area characters.
000100 IDENTIFICATION DIVISION.                                         IGNORED1
000200 PROGRAM-ID. L1MRG01.                                             IGNORED2
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01 W-T PIC X(10) VALUE "unset".
000600 PROCEDURE DIVISION.
000700 MAIN.
000800     DISPLAY "IN-AREA".
000900     DISPLAY "STILL-IN".                                          DISPLAY "PAST-R".
001000                                              DISPLAY "AT-COL-72".+++ IGNORED PAST MARGIN R
001100     MOVE "abcdefghij" TO W-T.                                    ZZZZZZZZ
001200     DISPLAY "T=[" W-T "]".
001300     STOP RUN.
