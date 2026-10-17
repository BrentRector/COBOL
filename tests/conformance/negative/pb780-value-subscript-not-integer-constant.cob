      *> reject-at: 2002 2014 2023
      *> kb/Work PB780 - an integer constant-name stands for subscript-1 of a Format-2 VALUE clause (13.10.3 SR2;
      *> conformance/2002/pb780_constant_names_literal_operands), and a constant-name substitutes as if its literal
      *> were written (13.10.4 GR1). KX stands for the ALPHANUMERIC literal "2", and 13.18.63.3 SR19 requires
      *> "Subscript-1 and subscript-2 shall be integer numeric literals".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB780VSX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  KX CONSTANT AS "2".
       01  T.
           05  E PIC X OCCURS 4 VALUE "A" FROM (KX).
       PROCEDURE DIVISION.
           DISPLAY T.
           STOP RUN.
