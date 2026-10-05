*> reject-at: 85 2002 2014 2023
      *> kb/Work PB2018 - 13.18.38.3 SR7 admits an index-name "as an operand in a relation condition", and
      *> 8.8.4.2.13 says "Relation tests may be made only between" an index-name and a numeric data item or
      *> numeric literal. The relation's OPERAND is then the index-name. In N < (K + 2) the operand is an arithmetic
      *> expression and K is an operand of THAT expression, where 8.8.1.1 admits only numeric data items, numeric
      *> literals and ZERO. COMPUTE N = K + 2 is refused the same way (COBOLNET1637).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2018IXN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          02 E PIC X OCCURS 5 INDEXED BY K.
       01 N PIC 9(4) VALUE 3.
       PROCEDURE DIVISION.
           SET K TO 2
           IF N < (K + 2) DISPLAY "UNDER-REJECT" END-IF
           STOP RUN.
