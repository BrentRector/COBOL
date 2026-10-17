*>--- conformance-case
*> id: pb250_cobol_words_phrase_word_undefine
*> rules: [FMT-15.94.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB250 - the other direction. 7.3.10.4 GR3: an UNDEFINE'd word "shall no longer be
      *> reserved or restricted in any way, and may be used as a user-defined intrinsic name, data-name or
      *> any other user-defined word, and any syntax requiring the use of the COBOL word that is the
      *> content of literal-3 shall not be available for use in this compilation group".
      *> Before the fix ANYCASE was still eaten as the keyword by its raw text: the compiler turned on
      *> case-insensitive matching and injected the compilation-unit currency instead of using the named
      *> item as argument-2 - a WRONG ANSWER with no diagnostic.
       >>COBOL-WORDS UNDEFINE "ANYCASE"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB250CWU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S       PIC X(9) VALUE "usd123.45".
       01 ANYCASE PIC X(3) VALUE "usd".
       01 T PIC 9.
       01 V PIC S9(5)V99.
       PROCEDURE DIVISION.
       MAIN.
      *> ANYCASE is the user's data item now, so it is ARGUMENT-2 - the currency string "usd". No ANYCASE
      *> keyword is written, so 15.68.3 4)f) does not apply and the match is case-SENSITIVE; "usd" matches
      *> the "usd" in S exactly, argument-1 conforms, and 15.94.4 1)a) returns 0 with the value 123.45.
           MOVE FUNCTION TEST-NUMVAL-C(S ANYCASE) TO T
           DISPLAY "TEST=" T
           COMPUTE V = FUNCTION NUMVAL-C(S ANYCASE)
           IF V = 123.45
               DISPLAY "VALUE OK"
           ELSE
               DISPLAY "VALUE BAD"
           END-IF
           DISPLAY "ITEM=" ANYCASE
           STOP RUN.
