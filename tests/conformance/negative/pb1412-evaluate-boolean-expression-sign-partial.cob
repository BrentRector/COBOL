      *> reject-at: 2002 2014 2023
      *> kb/Work PB1412. ISO 1989:2023 14.9.13.3 SR8 treats partial-expression-1 as the conditional expression that results
      *> from preceding it by the selection subject, and SR7 d) requires that expression to be well formed. `A B-AND C
      *> POSITIVE` is a sign condition whose operand is a BOOLEAN value, and 8.8.4.7.3 SR1 admits only "any single numeric
      *> data item ... or any form of arithmetic expression" there, so the boolean-expression-1 subject cannot take a
      *> sign partial-expression (COBOLNET2318, naming the rule).
      *>   cite.py --check 8.8.4.7.3 "Arithmetic-expression-1 shall be any single numeric data item described with a
      *>     usage other than a standard floating-point usage, or any form of arithmetic expression." -> OK 1)
      *>   cite.py --check 14.9.13.3 "If a selection object is specified by partial-expression-1, that selection object
      *>     is treated as though it were specified as condition-2" -> OK 8)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1412NEP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 1(4) VALUE B"1100".
       01 C PIC 1(4) VALUE B"1010".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE A B-AND C
              WHEN POSITIVE DISPLAY "P"
              WHEN OTHER DISPLAY "O"
           END-EVALUATE
           STOP RUN.
