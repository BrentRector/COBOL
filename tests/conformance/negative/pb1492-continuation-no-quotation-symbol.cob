      *> reject-at: 85 2002 2014 2023
      *> 6.2.3.2 SR6: 'the first nonblank character of each
      *> continuation line shall be the quotation symbol used in the
      *> opening delimiter of the literal.' The continuation line below
      *> starts with FGH (kb/Work PB1492).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1492NQ.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01 X PIC X(80) VALUE "ABCDE                                      
000600-    FGH".
000700 PROCEDURE DIVISION.
000800 DISPLAY X.
000900     STOP RUN.
