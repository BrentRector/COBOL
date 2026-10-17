      *>--- conformance-case
      *> id: pb3147-area-a-item-in-area-b-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3147 - ANSI X3.23-1985 begins a paragraph header on its
      *> line in area A (character positions 8-11); ISO 2002 removed the
      *> areas (6.3.1). P2 below begins in column 12, area B, so at
      *> --std 85 it is COBOLNET3734 (the COBOL-2002 positive is
      *> 2002/pb3147_area_free_placement).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB3147NA.
000300 PROCEDURE DIVISION.
000400 P1.
000500     DISPLAY "A".
000600     P2.
000700     DISPLAY "B".
000800     STOP RUN.
