      *> reject-at: 2002 2014 2023
      *> kb/Work PB1507 - 11.10.3 SR6: "The RECURSIVE clause shall not be specified if any program that directly or
      *>   indirectly contains this program is an initial program." S6I1507 is RECURSIVE and is contained in the
      *>   INITIAL S6O1507. COBOLNET2755.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S6O1507 IS INITIAL.
       PROCEDURE DIVISION.
           DISPLAY "OUT"
           CALL "S6I1507"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S6I1507 IS RECURSIVE.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           GOBACK.
       END PROGRAM S6I1507.
       END PROGRAM S6O1507.
