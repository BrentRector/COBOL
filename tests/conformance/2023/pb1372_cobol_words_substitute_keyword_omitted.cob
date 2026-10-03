      *> PB1372 - ISO 1989:2023 7.3.10.4 GR4 with 8.4.3.2.3 SR2: a SUBSTITUTE literal-5 for an intrinsic function whose name
      *> is a reserved word names that function in the REPOSITORY paragraph and, with FUNCTION omitted, in a reference.
       >>COBOL-WORDS SUBSTITUTE "SUM" BY "TOTAL"
       >>COBOL-WORDS SUBSTITUTE "LENGTH" BY "LEN"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1372C.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION TOTAL INTRINSIC
           FUNCTION LEN INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC 99 VALUE 0.
       01 N PIC 99 VALUE 0.
       01 W PIC X(7) VALUE "abcdefg".
       PROCEDURE DIVISION.
       MAIN.
           MOVE TOTAL(1 2 3) TO T.
           DISPLAY T.
           MOVE LEN(W) TO N.
           DISPLAY N.
           STOP RUN.
