      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1281 - ISO 13.18.44.3 SR5: "data-name-2 may be subordinate to an item whose data description
      *> entry contains an OCCURS clause. In this case, the reference to data-name-2 in the REDEFINES clause shall
      *> not be subscripted." cite.py: OK 13.18.44.3 5). Refused by the one data-name-n shape screen, quoting SR5
      *> (it used to fall to COBOLNET1654 "names no preceding entry" on the glued text A(1)).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W1020HPB1281SUBS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 3 TIMES.
             10 A PIC X(4).
             10 B REDEFINES A (1) PIC X(4).
       PROCEDURE DIVISION.
           DISPLAY B (1).
           STOP RUN.
