      *> PB1409 - ISO 14.9.25.4 GR1 + 8.5.4 item 5: a logical record of a variable-length file
      *>   (RECORD CONTAINS 0 TO 10) whose current length is zero IS a zero-length item, so
      *>   READ INTO acts as MOVE of a zero-length LITERAL: an elementary move, and the SPACE
      *>   substituted for it EDITS into PIC XX/XX ("  /  "). The route only knew field senders, so
      *>   READ INTO stored five raw spaces. A 3-character record is a GROUP move (no editing,
      *>   GR4) and a zero-occurrence group sender still takes the same substitution (line 3).
      *>   cite.py: OK 14.9.25.4 1), OK 8.5.4 5)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1409G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1409g.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F RECORD CONTAINS 0 TO 10 CHARACTERS.
       01 FR0.
           05 FR0E PIC X OCCURS 0 TO 10 DEPENDING ON K.
       WORKING-STORAGE SECTION.
       01 K  PIC 99 VALUE 0.
       01 WE PIC XX/XX VALUE "ABCDE".
       PROCEDURE DIVISION.
           OPEN OUTPUT F
           WRITE FR0
           MOVE 3 TO K
           MOVE "ABC" TO FR0
           WRITE FR0
           CLOSE F
           OPEN INPUT F
           READ F INTO WE
           DISPLAY "1[" WE "]"
           READ F INTO WE
           DISPLAY "2[" WE "]"
           CLOSE F
           MOVE 0 TO K
           MOVE "ABCDE" TO WE
           MOVE FR0 TO WE
           DISPLAY "3[" WE "]"
           STOP RUN.
