      *> reject-at: 2002 2014 2023
      *> kb/Work PB486 - ISO 13.16.3 SR4 (cite.py OK): "If the TYPEDEF clause is
      *> specified, the data-name format of the entry-name clause shall also be
      *> specified and the TYPEDEF clause shall immediately follow the entry-name
      *> clause." Here TYPEDEF follows PICTURE (it used to compile and run).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB486TD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T PIC X(4) IS TYPEDEF.
       01 V TYPE T.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCD" TO V
           DISPLAY V
           STOP RUN.
