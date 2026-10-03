      *> reject-at: 2023
      *> 7.3.12.2 + 5.2.6.4: the braces of the UPON group carry choice
      *> indicators - each alternative at most once - so LISTING cannot
      *> be written twice (kb/Work PB807).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB807L.
000300 PROCEDURE DIVISION.
000400 >>DISPLAY "X" UPON LISTING LISTING
000500     STOP RUN.
