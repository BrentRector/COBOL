      *> reject-at: 2002 2014 2023
      *> kb/Work PB1507 - 10.6.1: the function-definition format prints `END FUNCTION user-function-name-1.`
      *>   UNBRACKETED (a program-definition's END PROGRAM is the bracketed one). FNE1507 has none.
      *>   COBOLNET2274.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. FNE1507.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R.
           MOVE 1 TO R
           GOBACK.
