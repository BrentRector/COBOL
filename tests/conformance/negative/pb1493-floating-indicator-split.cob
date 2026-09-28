      *> reject-at: 85 2002 2014 2023
      *> ISO §6.2.3.2 SR3 "All the characters forming a multiple-
      *>   character floating indicator shall be specified on the same
      *>   line" (cite.py --check: OK §6.2.3.2 3)). The continued line
      *>   ends with * and its continuation line begins with >, so the
      *>   join spells *> that no line holds: COBOLNET2496 (kb/Work
      *>   PB1493). Before, the text after it vanished as a comment.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1493SP.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500  01 X PIC X(12).
000600 PROCEDURE DIVISION.
000700     MOVE "ABC" TO X *
000800-    > comment here
000900     .
001000     STOP RUN.
