*>--- conformance-case
*> id: pb1372_cobol_words_lexer_keywords
*> rules: [GR-7.3.10.4-2, GR-7.3.10.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> PB1372 - ISO 1989:2023 7.3.10.4 GR2 and GR3: a synonym of PIC and a synonym of FUNCTION
      *> work wherever the keyword is required, and an UNDEFINEd PICTURE is a data-name.
       >>COBOL-WORDS EQUATE "PIC" WITH "PX"
       >>COBOL-WORDS EQUATE "FUNCTION" WITH "FN"
       >>COBOL-WORDS UNDEFINE "PICTURE"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1372A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PX 9(3) VALUE 7.
       01 PICTURE PIC X(3) VALUE "PPP".
       01 Y PIC X(3) VALUE "abc".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY X.
           DISPLAY PICTURE.
           DISPLAY FN UPPER-CASE(Y).
           STOP RUN.
