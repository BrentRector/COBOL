      *> reject-at: 2014 2023
      *> kb/Work PB1263 - ISO 13.18.38.3 SR9: "Data-name-2 shall not reference a variable-length
      *> group", which ISO 8.5.1.12.1 defines as "a group item whose data description has at least
      *> one dynamic-length elementary item or dynamic-capacity table as a subordinate item". G holds
      *> the DYNAMIC LENGTH item D.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1263N9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
           05 T OCCURS 3 ASCENDING KEY G INDEXED BY IX.
               10 G.
                   15 D PIC X DYNAMIC LENGTH.
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
