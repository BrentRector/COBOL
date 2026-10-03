*> reject-at: 2023
*> ISO 7.3.10.4 GR3 (kb/Work PB1372): when the UNDEFINE option is specified, "any syntax requiring the use of the
*> COBOL word that is the content of literal-3 shall not be available for use in this compilation group". PICTURE is
*> withdrawn here, so it is a user-defined word and `PICTURE X(3)` is no PICTURE clause: the lexer must not open
*> its picture-string mode on the word, and the entry is a syntax error.
       >>COBOL-WORDS UNDEFINE "PICTURE"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1372N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PICTURE X(3).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
