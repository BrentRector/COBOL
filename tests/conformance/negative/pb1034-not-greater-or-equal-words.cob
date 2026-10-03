      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1034. ISO 1989:2023 8.8.4.2.2 Format 1: `IS GREATER THAN OR EQUAL TO` has no [NOT] bracket
      *> (rendered page PDF p217), so NOT GREATER THAN OR EQUAL TO is the word-form twin of `NOT >=` and equally
      *> no alternative. (`NOT GREATER THAN` IS printed: IS [NOT] GREATER THAN.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1034NEG3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 5.
       01 B PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           IF A NOT GREATER THAN OR EQUAL TO B
               DISPLAY "LT"
           ELSE
               DISPLAY "GE"
           END-IF
           STOP RUN.
