      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB2506. ISO 12.3.7.4 GR14 a) / 8.3.3.3.2: the comma is
      *> the decimal separator of a numeric literal ONLY under
      *> DECIMAL-POINT IS COMMA, and 8.3.5 2) makes a comma a separator
      *> only when "immediately followed by a space". So in a function
      *> argument list the signed -1,5 is ONE literal whatever the
      *> mode, and without the clause it is COBOLNET0895 - the same
      *> answer the unsigned 1,5 gets, never two arguments.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2506N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC S9V9.
       PROCEDURE DIVISION.
           MOVE FUNCTION MIN(-1,5 2) TO X
           STOP RUN.
