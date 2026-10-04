      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1265 - ISO 13.18.38.2: TO without DEPENDING is Format 3 only (and even there
      *> 13.18.38.3 SR24 pairs them). OCCURS 2 TO 5 used to bind silently as a FIXED 5 table.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1265NT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
           05 T PIC X OCCURS 2 TO 5.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
