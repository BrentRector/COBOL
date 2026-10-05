      *> kb/Work PB486 - ISO 13.16.3 SR4 (cite.py OK): the REDEFINES clause and the
      *> TYPEDEF clause each "shall immediately follow the entry-name clause";
      *> "The remaining clauses may be written in any order." Written in that
      *> position they bind as declared: T is a 4-character type whose VALUE
      *> seeds V (13.18.57.4 GR1), R redefines S, and
      *> VALUE and PICTURE follow in a free order.
      *> Expected: ABCD / XYZ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB486LC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T IS TYPEDEF VALUE "ABCD" PIC X(4).
       01 V TYPE T.
       01 S VALUE "XYZ" PIC X(3).
       01 R REDEFINES S PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY V
           DISPLAY R
           STOP RUN.
