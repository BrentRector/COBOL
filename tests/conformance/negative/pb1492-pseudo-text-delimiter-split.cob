      *> reject-at: 85 2002 2014 2023
      *> 6.3.5 2): 'All characters composing any multiple-character
      *> separator or multiple-character indicator shall be specified
      *> on the same line.' The opening pseudo-text delimiter == below
      *> is split across a continuation (kb/Work PB1492).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1492SPL.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01 Q PIC X VALUE "Q".
000600 REPLACE =
000700-    =ABC== BY ==XYZ==.
000800 PROCEDURE DIVISION.
000900     DISPLAY Q.
001000     STOP RUN.
