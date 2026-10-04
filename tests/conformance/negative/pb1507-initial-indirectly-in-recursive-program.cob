      *> reject-at: 2002 2014 2023
      *> kb/Work PB1507 - 11.10.3 SR5 says "directly or indirectly contains": S5C1507 is INITIAL and is contained in
      *>   the plain S5B1507, which is contained in the RECURSIVE S5A1507 - and S5B1507 IS a recursive program
      *>   (11.10.4 GR4: the RECURSIVE clause makes the programs contained within the program recursive). COBOLNET2755.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S5A1507 IS RECURSIVE.
       PROCEDURE DIVISION.
           CALL "S5B1507"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S5B1507.
       PROCEDURE DIVISION.
           CALL "S5C1507"
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S5C1507 IS INITIAL.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           GOBACK.
       END PROGRAM S5C1507.
       END PROGRAM S5B1507.
       END PROGRAM S5A1507.
