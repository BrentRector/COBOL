      *> PB1446 - ISO 8.3.3.3.2 / 8.3.5: under DECIMAL-POINT IS COMMA a leftmost-comma
      *>   literal is its own character-string: `,5` is 0.5 and `1 + ,5` is 1.5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1446P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R1 PIC 9V9 VALUE ZERO.
       01  R2 PIC 9V9 VALUE ZERO.
       01  R3 PIC 9V9 VALUE ZERO.
       PROCEDURE DIVISION.
           MOVE 1,5 TO R1
           MOVE ,5 TO R2
           COMPUTE R3 = 1 + ,5
           DISPLAY R1 " " R2 " " R3
           STOP RUN.
