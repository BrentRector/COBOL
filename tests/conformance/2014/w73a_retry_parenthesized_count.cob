      *> kb/Work PB1162 - ISO 14.7.9.2 prints RETRY
      *>  arithmetic-expression-1 TIMES, and 8.8.1.1 makes an arithmetic
      *>  expression enclosed in parentheses an arithmetic expression,
      *>  so RETRY (N) TIMES and RETRY (N + 1) TIMES are legal. RETRY is
      *>  reserved from 2014 (8.9), so the '(' after it can open no
      *>  subscript (8.3.2.1 rule 1); the lexer's edition-blind
      *>  subscript trigger used to make both COBOL0001. With no sharing
      *>  conflict each statement succeeds on its first attempt
      *>  (14.7.9.3 GR4 applies only after one), so, derived: W 00, then
      *>  R 00 REC1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73ARETRY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "w73aretry.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS K FILE STATUS IS FS
               LOCK MODE IS MANUAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(4).
       WORKING-STORAGE SECTION.
       01 K PIC 9(4).
       01 FS PIC XX.
       01 N PIC S9V9 VALUE 1.5.
       PROCEDURE DIVISION.
           OPEN OUTPUT F.
           MOVE 1 TO K.
           MOVE "REC1" TO R.
           WRITE R RETRY (N + 1) TIMES.
           DISPLAY "W " FS.
           CLOSE F.
           OPEN I-O F.
           MOVE 1 TO K.
           MOVE SPACES TO R.
           READ F RETRY (N) TIMES.
           DISPLAY "R " FS " " R.
           CLOSE F.
           STOP RUN.
