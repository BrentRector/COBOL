      *>--- conformance-case
      *> id: pb1690-directive-after-program-text-fixed
      *> rules: [SR-7.3.3-2, SR-7.3.12.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> 7.3.3 SR2: 'A compiler directive shall be preceded only by
      *> zero, one, or more space characters.' The directive below
      *> follows a DISPLAY statement on its line; it is refused by
      *> name, not as a stray '>' (kb/Work PB1690).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1690FX.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01 N PIC 9 VALUE 1.
000600 PROCEDURE DIVISION.
000700 DISPLAY N. >>DEFINE X AS 1
000800     STOP RUN.
