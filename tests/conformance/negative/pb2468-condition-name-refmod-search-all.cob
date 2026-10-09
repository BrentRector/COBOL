      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB2468 -- SEARCH ALL Format 2 WHEN CN(IX)(1:2), CN a
      *> level-88 of the ASCENDING KEY item, bound the condition-name
      *> and silently dropped the (1:2). ISO 14.9.37.2 Format 2 names
      *> condition-name-1, whose reference (8.4.4.2; 8.4.2.3.2 Format
      *> 2) has no reference modifier, and 8.4.3.3.3 SR5 allows one
      *> only on an identifier referencing a data item (cite.py: OK
      *> for each). COBOLNET3317 at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEG2468S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 T OCCURS 3 ASCENDING KEY TK INDEXED BY IX.
             10 TK PIC X(3).
                88 CN VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           SEARCH ALL T AT END DISPLAY "E"
               WHEN CN(IX)(1:2) DISPLAY "X"
           END-SEARCH.
           STOP RUN.
