      *> options: source-format=fixed
      * ISO/IEC 1989:2023 6.1 1) c) "The implementor shall specify the meaning of
      * lines and character positions." -> docs/CONFORMANCE.md DOC-A.1-157: a TAB
      * (U+0009) advances to the next tab stop - positions 1, 9, 17, 25, ... - and
      * occupies the one to eight positions that fill the gap (GnuCOBOL's tab-width
      * of 8, rule 1 precedence). kb/Work PB1586: a TAB was ONE position, so a line
      * written with two leading TABs put 'L' of DISPLAY in the indicator area.
      * T1: two leading TABs - DISPLAY starts at position 17 (program-text area),
      *     the indicator area (position 7) is one of the spaces the first TAB fills.
      * T2: a TAB after the six-digit sequence number fills positions 7-8, so the
      *     indicator area is a space; the second TAB fills 9-16, DISPLAY is at 17.
      * T3: a TAB at expanded position 64 advances to 72, so the text after it
      *     starts at position 73 - outside the program-text area (margin R, item
      *     158) - and is ignored. A TAB counted as one position put it INSIDE the
      *     area. The line also runs far past 255 positions: fixed form is not
      *     subject to the 255-position limit (6.1 3) a) is free form only).
      * The Area A headers (position 9) are a TAB in the sequence area.
	IDENTIFICATION DIVISION.
	PROGRAM-ID. PB1586FX.
	PROCEDURE DIVISION.
		DISPLAY "T1".
000100		DISPLAY "T2".
		DISPLAY "T3".                                   	DISPLAY "NO".ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ
		STOP RUN.
