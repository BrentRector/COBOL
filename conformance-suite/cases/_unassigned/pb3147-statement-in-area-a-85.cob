      *>--- conformance-case
      *> id: pb3147-statement-in-area-a-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3147 - ANSI X3.23-1985 writes a sentence in area B
      *> (character position 12 on); ISO 2002 removed the areas (6.3.1,
      *> 6.3.5). The DISPLAY below begins in column 8, area A, so at
      *> --std 85 it is COBOLNET3735 (the COBOL-2002 positive is
      *> 2002/pb3147_area_free_placement).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB3147NB.
000300 PROCEDURE DIVISION.
000400 P1.
000500 DISPLAY "A".
000600     STOP RUN.
