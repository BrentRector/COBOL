      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1265 - ISO 13.18.38.2 Format 2 prints "OCCURS integer-1 TO integer-2 TIMES
      *> DEPENDING ON data-name-1" with no part bracketed; the optional TO is Format 3's (the report
      *> writer). OCCURS 5 DEPENDING ON N is in no data-division format.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1265ND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 2.
       01 R.
           05 T PIC X OCCURS 5 DEPENDING ON N.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
