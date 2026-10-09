      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB2468 -- IF CN(1:2), CN a level-88, bound the
      *> condition-name and silently dropped the (1:2). ISO 8.4.4.2
      *> prints condition-name-1 (qualified, and subscripted per
      *> 8.4.2.3.2 Format 2) and 8.8.4.5.2 condition-name-1 alone: no
      *> reference modifier; 8.4.3.3.3 SR5 allows one only where "an
      *> identifier referencing a data item of class alphanumeric,
      *> boolean, or national is permitted" (cite.py: OK for each).
      *> COBOLNET3317 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG2468I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TK PIC X(3).
          88 CN VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF CN(1:2) DISPLAY "X" END-IF.
           STOP RUN.
