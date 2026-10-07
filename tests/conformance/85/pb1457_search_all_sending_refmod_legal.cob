      *> kb/Work PB1457 - POSITIVE CONTROL for the data-name-n reference-modifier screen on SEARCH ALL Format 2
      *> (ISO 14.9.37.3 SR8 + 8.4.3.3.3 NOTE). The screen is for data-name-1 / data-name-2 ONLY: the KEY side is
      *> subscripted by the first index-name and carries NO reference modifier, while the sending side is
      *> identifier-3 (14.9.37.2), an identifier, which 8.4.3.3.3 SR1 allows to be reference-modified. Both
      *> conjuncts here compare an unmodified key operand against a reference-modified sender, so the program
      *> must compile and find the second occurrence (K1 = "AA", K2 = "20").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1457P1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W   PIC X(8) VALUE "AA20QQQQ".
       01 N   PIC 99.
       01 TB.
          05 T OCCURS 3 TIMES ASCENDING KEY IS K1 K2 INDEXED BY IX.
             10 K1 PIC X(2).
             10 K2 PIC X(2).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "AA" TO K1 (1).
           MOVE "10" TO K2 (1).
           MOVE "AA" TO K1 (2).
           MOVE "20" TO K2 (2).
           MOVE "BB" TO K1 (3).
           MOVE "10" TO K2 (3).
           SEARCH ALL T
               AT END DISPLAY "NONE"
               WHEN K1 (IX) = W (1:2)
                AND K2 (IX) = W (3:2)
                   SET N TO IX
                   DISPLAY "FOUND=" N
           END-SEARCH.
           STOP RUN.
