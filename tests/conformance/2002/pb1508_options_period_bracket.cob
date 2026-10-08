      *> ISO 11.9.2 (kb/Work PB1508): the OPTIONS paragraph's general format prints `OPTIONS.`, seven
      *> separate clause brackets and then an INDEPENDENT `[.]` bracket. 5.2.6.2 lets a bracketed portion
      *> be written or omitted, and 11.9.3 SR1 ("If any of the clauses are specified, then there shall be a
      *> terminating separator period") requires the period only when a clause is written - so with no
      *> clause the period may still be written: `OPTIONS. .` (both on one line, PB1508P) and `OPTIONS.`
      *> with the period on a line of its own (PB1508Q) mean what `OPTIONS.` means. They used to be
      *> COBOL0001 "unexpected '.'". PB1508R writes a clause and its required terminating period.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1508P.
       OPTIONS. .
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "P: EMPTY OPTIONS WITH ITS PERIOD".
           CALL "PB1508Q".
           CALL "PB1508R".
           STOP RUN.
       END PROGRAM PB1508P.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1508Q.
       OPTIONS.
           .
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "Q: THE PERIOD ON ITS OWN LINE".
           GOBACK.
       END PROGRAM PB1508Q.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1508R.
       OPTIONS.
           ARITHMETIC IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9V9.
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE W = 1 / 4.
           DISPLAY "R: W=" W.
           GOBACK.
       END PROGRAM PB1508R.
