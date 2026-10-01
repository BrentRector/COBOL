      *> reject-at: 2002 2014 2023
      *> 6.3.5 2): 'National literals may be continued only with a
      *> floating literal continuation indicator.' This one is
      *> continued with the column-7 hyphen (kb/Work PB1492).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1492NAT.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01 X PIC N(80) VALUE N"ABCDE                                     
000600-    "FGH".
000700 PROCEDURE DIVISION.
000800 DISPLAY "X".
000900     STOP RUN.
