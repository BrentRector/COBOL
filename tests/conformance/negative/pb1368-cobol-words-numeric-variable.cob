      *> reject-at: 2023
      *> 7.3.11.4 GR1 lets a compilation-variable-name stand in a directive "where a literal of the category
      *> associated with the name is permitted"; every COBOL-WORDS literal is alphanumeric (7.3.10.3 SR2), so a
      *> NUMERIC variable there is refused (kb/Work PB1368).
       >>DEFINE NUM AS 3
       >>COBOL-WORDS UNDEFINE NUM
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1368CWNUM.
       PROCEDURE DIVISION.
           STOP RUN.
