      *>--- conformance-case
      *> id: pb2983-supplementary-letters-parser-error-column
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2983 - ONE column unit for every diagnostic. ISO/IEC 1989:2023 6.1 1) a) describes
      *> reference format "in terms of character positions", and 6.3.1 numbers them: the indicator area is
      *> the 7th character position, the program-text area begins in the 8th. A supplementary-plane
      *> letter (an Annex B extended letter, 8.1.3.2 4)) is ONE character position though two UTF-16
      *> units, so ZZZ below stands in character position 24 whatever precedes it; the parser used to
      *> name column 26, counting each of the two letters as two columns.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB2983P.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  𐀍𐀎 PIC X(2) ZZZ.
000600 PROCEDURE DIVISION.
000700     STOP RUN.
