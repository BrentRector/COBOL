      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB351 (row FMT-14.9.37.2). ISO 14.9.37.2 writes imperative-statement-2 after WHEN condition-1,
      *> and 14.5.1: "Any statement with a conditional phrase that is not terminated by its explicit scope
      *> terminator is a conditional statement" - so an IF with no END-IF there is a conditional statement where
      *> only an imperative statement is admitted. Refused COBOLNET2796 at every edition (the rule is the same
      *> in all four).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB351NS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 1.
       01 T.
          05 K PIC 9 OCCURS 3 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "123" TO T.
           SET IX TO 1.
           SEARCH K
               AT END DISPLAY "NOT-FOUND"
               WHEN K (IX) = 2
                   IF N = 1 DISPLAY "FOUND"
           END-SEARCH.
           STOP RUN.
