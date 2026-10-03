      *> PB1372 - ISO 1989:2023 7.3.10.4 GR4: SUBSTITUTE literal-4 BY literal-5 makes literal-5 usable
      *> wherever literal-4 is documented - PICTURE, the SIGN clause, and the intrinsic functions SUM, LENGTH,
      *> SIGN and RANDOM, whose names are also reserved words the lexer tokenizes.
       >>COBOL-WORDS SUBSTITUTE "PICTURE" BY "LOOKS"
       >>COBOL-WORDS SUBSTITUTE "SUM" BY "TOTAL"
       >>COBOL-WORDS SUBSTITUTE "LENGTH" BY "LEN"
       >>COBOL-WORDS SUBSTITUTE "SIGN" BY "SGN"
       >>COBOL-WORDS SUBSTITUTE "RANDOM" BY "RND"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1372B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X LOOKS 9(3) VALUE 7.
       01 T PIC 99 VALUE 0.
       01 N PIC 99 VALUE 0.
       01 S PIC S9 SGN IS LEADING SEPARATE VALUE 0.
       01 W PIC X(7) VALUE "abcdefg".
       01 R PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY X.
           MOVE FUNCTION TOTAL(1 2 3) TO T.
           DISPLAY T.
           MOVE FUNCTION LEN(W) TO N.
           DISPLAY N.
           MOVE FUNCTION SGN(-4) TO S.
           DISPLAY S.
           IF FUNCTION RND(5) < 1 MOVE "Y" TO R END-IF.
           DISPLAY R.
           STOP RUN.
