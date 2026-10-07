*> reject-at: 85 2002 2014 2023
*> kb/Work PB1457 - ISO 14.9.37.2 Format 2 prints `WHEN data-name-1 IS EQUAL TO ...`, and 8.4.3.3.3's NOTE says
*> "where data-name-n is used in a general format or syntax rule, then reference-modification is not permitted".
*> SearchAllFormat2Rules.CheckKeySide resolved the written reference to the base item TK and never looked at the
*> (1:2): the program compiled clean and searched on the whole key. 14.9.37.3 SR8 REQUIRES the index-name subscript
*> on this operand, so only the reference-modifier arm of the data-name-n screen applies.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1457N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 T OCCURS 3 TIMES ASCENDING KEY IS TK INDEXED BY IX.
             10 TK PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ABC" TO TK (1).
           MOVE "BCD" TO TK (2).
           MOVE "CDE" TO TK (3).
           SEARCH ALL T
               AT END DISPLAY "E"
               WHEN TK (IX) (1:2) = "AB"
                   DISPLAY "X"
           END-SEARCH.
           STOP RUN.
