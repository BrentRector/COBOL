      *>--- conformance-case
      *> id: pb3147_area_free_placement
      *> rules: []
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3147 - ISO 6.3.1: the program-text area "begins in
      *> character position 8 and terminates with the character position
      *> immediately to the left of margin R" - it has no area A and no
      *> area B, which ANSI X3.23-1985 had and ISO 2002 removed;
      *> 6.3.5 continues an entry "in the program-text area of a
      *> subsequent line". So every item below stands where COBOL-85
      *> forbade it, and the program is conforming from 2002 on: the
      *> division, section and paragraph headers, the level indicator,
      *> level-numbers 01 and 77, DECLARATIVES, END DECLARATIVES and the
      *> end marker begin in area B (position 12) or follow other text
      *> of their line; two statements, a continued clause and a period
      *> begin in area A (position 8). At --std 85 each is COBOLNET3734
      *> or COBOLNET3735 (the two pb3147 negatives).
      *> Expected (14.4.3 order): MAIN-PARA shows A and W-N (7),
      *> control falls through to P2, which performs P3 (W-X, M) and
      *> shows B before STOP RUN - A / 7 / M / B. A header read as
      *> statement text, or a lost clause, fails the compile or the
      *> output.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB3147FP.
000300     ENVIRONMENT DIVISION.
000400     INPUT-OUTPUT SECTION.
000500     FILE-CONTROL.
000600     SELECT PB3147-F ASSIGN TO "PB3147F.DAT".
000700     DATA DIVISION.
000800     FILE SECTION.
000900     FD  PB3147-F.
001000     01  PB3147-R PIC X.
001100     WORKING-STORAGE SECTION.
001200     77  W-N PIC 9 VALUE 7.
001300     01  W-G.
001400         05  W-X
001500 PIC X VALUE "M".
001600     PROCEDURE DIVISION.
001700     DECLARATIVES.
001800     D1 SECTION.
001900         USE AFTER STANDARD ERROR PROCEDURE ON PB3147-F.
002000     END DECLARATIVES.
002100     M SECTION.
002200     MAIN-PARA.
002300 DISPLAY "A".
002400 DISPLAY W-N
002500 .
002600     P2. PERFORM P3. DISPLAY "B". STOP RUN. P3.
002700         DISPLAY W-X.
002800     END PROGRAM PB3147FP.
