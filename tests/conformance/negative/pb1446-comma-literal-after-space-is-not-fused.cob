      *> reject-at: 2023
      *> kb/Work PB1446 - ISO 8.3.5: a space before the comma begins a NEW character-string,
      *>   so `1 ,5` is the integer 1 followed by the literal ,5 - two operands with no
      *>   operator - and it was fused into the interior-point literal 1,5 (= 1.5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1446N.
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
           COMPUTE R3 = 1 ,5
           DISPLAY R1 " " R2 " " R3
           STOP RUN.
