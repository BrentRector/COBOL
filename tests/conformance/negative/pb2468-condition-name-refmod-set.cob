      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB2468 -- SET Format 4 condition-name-1 written with a
      *> reference modifier, SET CN(1:2) TO TRUE. ISO 14.9.39.2 Format 4
      *> names condition-name-1, whose reference (8.4.4.2; 8.4.2.3.2
      *> Format 2) has no reference modifier, and 8.4.3.3.3 SR5 allows
      *> one only on an identifier referencing a data item (cite.py:
      *> OK for each). COBOLNET3317 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG2468T.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TK PIC X(3).
          88 CN VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET CN(1:2) TO TRUE.
           STOP RUN.
