*>--- conformance-case
*> id: pb250-cobol-words-undefined-keyword-syntax-withdrawn
*> rules: [FMT-15.94.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 7.3.10.4 GR3 (kb/Work PB250): when the UNDEFINE option is specified, the COBOL word that is the
*> content of literal-3 "shall no longer be reserved or restricted in any way ... and any syntax requiring
*> the use of the COBOL word that is the content of literal-3 shall not be available for use in this
*> compilation group". ANYCASE is withdrawn here and NOTHING is declared under that name, so the word in
*> the argument list is an undefined user-defined word and the reference identifies no resource
*> (ISO 8.4.2.1). Before the fix the compiler still read it as the 15.94.2 keyword and compiled clean,
*> silently turning on case-insensitive currency matching the program had withdrawn.
       >>COBOL-WORDS UNDEFINE "ANYCASE"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB250CWN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(9) VALUE "usd123.45".
       01 T PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION TEST-NUMVAL-C(S "USD" ANYCASE) TO T
           DISPLAY "TEST=" T
           STOP RUN.
