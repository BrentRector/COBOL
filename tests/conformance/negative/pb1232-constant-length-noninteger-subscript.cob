      *> reject-at: 2002 2014 2023
      *> 13.10.3 SR3: "All subscripts of data-name-1 and data-name-2 shall be literals" - a subscript is an integer
      *> position, and SR2 admits a constant-name there only as the literal of its own class and category, so the
      *> non-integer constant KD (1.5) is not an integer literal subscript (kb/Work PB1232).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1232NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 KD CONSTANT AS 1.5.
       01 T.
          05 E PIC X(3) OCCURS 4.
       01 K CONSTANT AS LENGTH OF E (KD).
       PROCEDURE DIVISION.
           STOP RUN.
