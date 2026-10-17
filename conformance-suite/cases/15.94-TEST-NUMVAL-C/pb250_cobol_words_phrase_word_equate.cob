*>--- conformance-case
*> id: pb250_cobol_words_phrase_word_equate
*> rules: [FMT-15.94.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB250 - >>COBOL-WORDS reaches a keyword the lexer does NOT tokenize.
      *> ANYCASE is an ISO 8.9 RESERVED word, yet CobolExpressions.g4 leaves it a plain IDENTIFIER so the
      *> 15.68.2 / 15.94.2 phrase parses as ordinary arguments - so the post-lex token retype cannot reach
      *> it and only the by-name resolution can. 7.3.10.4 GR2: the EQUATEd literal-2 "may be used in any
      *> syntax requiring the use of the reserved word ... that is the content of literal-1".
      *> Before the fix EITHER-CASE bound as a THIRD operand and the arity check rejected this program.
       >>COBOL-WORDS EQUATE "ANYCASE" WITH "EITHER-CASE"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB250CWA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(9) VALUE "usd123.45".
       01 T PIC 9.
       01 V PIC S9(5)V99.
       PROCEDURE DIVISION.
       MAIN.
      *> 15.68.3 4)f) - ANYCASE makes the currency-string match case-insensitive, so the upper-case
      *> argument-2 "USD" matches the lower-case "usd" written in S. 15.94.4 1)a) - argument-1 then
      *> conforms, so TEST-NUMVAL-C returns 0; NUMVAL-C returns the numeric value 123.45.
           MOVE FUNCTION TEST-NUMVAL-C(S "USD" EITHER-CASE) TO T
           DISPLAY "TEST=" T
           COMPUTE V = FUNCTION NUMVAL-C(S "USD" EITHER-CASE)
           IF V = 123.45
               DISPLAY "VALUE OK"
           ELSE
               DISPLAY "VALUE BAD"
           END-IF
      *> The canonical spelling is GONE for this compilation group only in the SUBSTITUTE case; EQUATE
      *> leaves literal-1 reserved (GR2 says nothing about removing it), so ANYCASE still works here.
           MOVE FUNCTION TEST-NUMVAL-C(S "USD" ANYCASE) TO T
           DISPLAY "CANON=" T
           STOP RUN.
