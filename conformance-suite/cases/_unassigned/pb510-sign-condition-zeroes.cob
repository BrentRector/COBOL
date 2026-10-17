*>--- conformance-case
*> id: pb510-sign-condition-zeroes
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.8.4.7.2 prints the sign condition as "IS [NOT] { POSITIVE | NEGATIVE | ZERO }" with ZERO
      *> underlined: a KEYWORD (5.2.2), and ZEROES is a different reserved word (8.9). A relation
      *> condition needs a relational operator, so "N1 IS NOT ZEROES" is no condition at all; the
      *> figurative comparison is spelled "N1 NOT = ZEROES". kb/Work PB510 (the lexer folded the three
      *> spellings into one token and this compiled as a sign condition). Refused COBOLNET2418.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB510N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC S9(3) VALUE 5.
       PROCEDURE DIVISION.
       MAIN.
           IF N1 IS NOT ZEROES
               DISPLAY "NONZERO"
           END-IF
           STOP RUN.
