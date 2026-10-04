      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1263 - ISO 13.18.38.3 SR6: "The data item identified by data-name-2 shall not
      *> contain an OCCURS clause except when data-name-2 is the subject of the entry." K is not the
      *> subject T and its own entry OCCURS 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1263N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
           05 T OCCURS 3 ASCENDING KEY K INDEXED BY IX.
               10 K PIC 9 OCCURS 2.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
