*> reject-at: 85 2002 2014 2023
*> kb/Work PB1457 - the AND phrase repeats data-name-2 (ISO 14.9.37.2 Format 2), and 14.9.37.3 SR8 speaks of "all
*> repetitions of data-name-2", so the 8.4.3.3.3 NOTE (no reference-modification where data-name-n is written)
*> reaches every conjunct, not only the first WHEN operand. TL (IX) (1:2) used to key on all of TL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1457N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 T OCCURS 3 TIMES ASCENDING KEY IS TK TL INDEXED BY IX.
             10 TK PIC X(3).
             10 TL PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH ALL T
               AT END DISPLAY "E"
               WHEN TK (IX) = "ABC"
                AND TL (IX) (1:2) = "AB"
                   DISPLAY "X"
           END-SEARCH.
           STOP RUN.
