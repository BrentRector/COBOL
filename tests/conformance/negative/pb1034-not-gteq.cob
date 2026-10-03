      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1034. ISO 1989:2023 8.8.4.2.2 Format 1 (rendered page PDF p217, printed 187) brackets [NOT] on
      *> GREATER THAN, >, LESS THAN, <, EQUAL TO and = ONLY; IS >= carries no NOT, so `NOT >=` is no alternative of
      *> the format. The compiler used to accept it and fold it into `<`, so `IF A NOT >= B` compiled and ran in
      *> every edition. The operator rule now has the printed alternatives and nothing else, so the program does
      *> not parse (COBOL0001) - at every edition, because no edition prints the spelling.
      *>   cite.py --check 8.8.4.2.2 "Figure notes (relation condition Format 1 (General-relation) syntax diagram)"
      *>     -> OK (the figure note of the rendered diagram)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1034NEG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 5.
       01 B PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           IF A IS NOT >= B
               DISPLAY "LT"
           ELSE
               DISPLAY "GE"
           END-IF
           STOP RUN.
