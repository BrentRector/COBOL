      *> reject-at: 2002 2014 2023
      *> 6.2.3.2 SR4: 'A given literal shall not be continued with more
      *> than one form of continuation.' The first line continues the
      *> literal with the floating indicator, the next with the
      *> column-7 hyphen (kb/Work PB1359).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1359SR4.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01 X PIC X(12) VALUE "AB"-
000600-    "CD".
000700 PROCEDURE DIVISION.
000800 DISPLAY X.
000900     STOP RUN.
