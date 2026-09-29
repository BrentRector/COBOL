      *> kb/Work PB1460 - the positive half. ISO 8.4.6.3 2) (cite.py
      *> --check 8.4.6.3 -> OK 2)): the COMMON program and the programs
      *> contained within it "may reference the program-name only if the
      *> program possesses the recursive attribute". W73P1R is COMMON AND
      *> RECURSIVE, so its containee W73P1R1 may CALL it AS NESTED
      *> (14.9.4.3 SR15: "a common program as specified in 8.4.6.3"), and
      *> the run-time resolver finds the same program the binder admitted.
      *> Expected: the depth counter D walks 1 -> 2 through the nested
      *> call; both activations then display 2, since D is one GLOBAL item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73P1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC 9 VALUE 0 GLOBAL.
       PROCEDURE DIVISION.
           CALL "W73P1R" AS NESTED
           DISPLAY "P1 DONE"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73P1R IS COMMON RECURSIVE PROGRAM.
       PROCEDURE DIVISION.
           ADD 1 TO D
           IF D < 2
               CALL "W73P1R1" AS NESTED
           END-IF
           DISPLAY "R AT DEPTH " D
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73P1R1.
       PROCEDURE DIVISION.
           DISPLAY "R1 CALLS R"
           CALL "W73P1R" AS NESTED
           GOBACK.
       END PROGRAM W73P1R1.
       END PROGRAM W73P1R.
       END PROGRAM W73P1.
