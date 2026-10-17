      *>--- conformance-case
      *> id: pb3168_continuation_area_a
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3168 - ISO 6.3.1: the program-text area "begins in
      *> character position 8 and terminates with the character position
      *> immediately to the left of margin R" - it has no area A, which
      *> ANSI X3.23-1985 had ("Area A of a continuation line must be
      *> blank") and ISO 2002 removed. Both continuation lines below
      *> begin their text in area A (positions 8 and 10), so the program
      *> is conforming from 2002 on and, at --std 85, COBOLNET3735 (the
      *> pb3168 negative). 6.5 6) a): a continued literal appends the
      *> program text "beginning with the first character after the
      *> initial quotation symbol"; 6.5 6) b): any other continuation
      *> line appends "beginning with the first non-space character",
      *> here the separator period.
      *> Expected: the 52 characters of the continued line (positions
      *> 21-72, the alphabet twice) followed by JK, then W-N = 5.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB3168CA.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 77  W-N PIC 9 VALUE 0.
000600 PROCEDURE DIVISION.
000700 MAIN-PARA.
000800     DISPLAY "ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ
000900-"JK".
001000     MOVE 5 TO W-N
001100-  .
001200     DISPLAY W-N
001300     STOP RUN.
