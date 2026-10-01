      *> reject-at: 85 2002 2014 2023
      *> 6.2.3.2 SR6: the continuation line of a literal opened with a
      *> QUOTATION MARK begins with a quotation mark, not an apostrophe
      *> (kb/Work PB1492).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1492OQ2.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01 X PIC X(80) VALUE "ABCDE                                      
000600-    'FGH".
000700 PROCEDURE DIVISION.
000800 DISPLAY X.
000900     STOP RUN.
