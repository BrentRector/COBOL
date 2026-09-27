      *> reject-at: 2002 2014 2023
      *> kb/Work PB1627 - ISO 13.10.3 SR2 lets a constant-name stand for a
      *> literal "of the class and category of constant-name-1", and
      *> 8.3.3.6.3 SR2 requires the literal-1 of ALL literal-1 to be "an
      *> alphanumeric, boolean, or national literal". K9 stands for the
      *> numeric literal 7, so ALL K9 is refused by SR2: COBOLNET2491.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG1627NUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K9  CONSTANT AS 7.
       01 W-X PIC X(6).
       PROCEDURE DIVISION.
           MOVE ALL K9 TO W-X.
           STOP RUN.
