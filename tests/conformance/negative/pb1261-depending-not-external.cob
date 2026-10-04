      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1261 - ISO 13.18.38.3 SR21: in a record description entry containing the EXTERNAL
      *> clause, "data-name-1 shall reference a data item possessing the external attribute". N is
      *> internal (8.6.3), so two programs sharing E could disagree about its length.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1261N21.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 2.
       01 E EXTERNAL.
           05 T PIC X OCCURS 1 TO 5 DEPENDING ON N.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
