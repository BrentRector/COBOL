      *> kb/Work PB1446 - ISO 8.3.3.3.2 3) and 4) under DECIMAL-POINT IS COMMA, at the
      *>   rule's introducing edition (unchanged since COBOL-85).
      *> 3) "may appear anywhere within the literal except as the rightmost character":
      *>    the leftmost-comma literals -,5 and ,5 are 0.5 in magnitude.
      *> 4) "The value of a fixed-point numeric literal is the algebraic quantity
      *>    represented by the characters in the fixed-point numeric literal":
      *>    -,5 = -0.50 (0005}), +000123,4500 = 123.45 (1234E),
      *>    1,25 * -2 = -2.50 (0025}), 10 - -1,5 = 11.50 (0115{).
      *>    "The size ... is equal to the number of digits in the string of characters
      *>    in the literal": -000000000000000012 has 18 digits, inside COBOL-85's 18.
      *> 8.3.5 1) "The COBOL character space is a separator": in the condition-name
      *>    VALUE `1 ,5` the space ends the literal 1, so the list is {1, 0.5} and
      *>    1,5 (one literal, 1.5) is not in it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1446V.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R1 PIC S9(3)V99 VALUE -,5.
       01  R2 PIC S9(3)V99 VALUE +000123,4500.
       01  R3 PIC S9(3)V99 VALUE ZERO.
       01  R4 PIC 9V9 VALUE ZERO.
           88 HALF-OR-ONE VALUE 1 ,5.
       01  R5 PIC S9(18) VALUE -000000000000000012.
       PROCEDURE DIVISION.
           DISPLAY R1
           DISPLAY R2
           COMPUTE R3 = 1,25 * -2
           DISPLAY R3
           COMPUTE R3 = 10 - -1,5
           DISPLAY R3
           MOVE ,5 TO R4
           IF HALF-OR-ONE DISPLAY "0,5 IN LIST" END-IF
           MOVE 1 TO R4
           IF HALF-OR-ONE DISPLAY "1 IN LIST" END-IF
           MOVE 1,5 TO R4
           IF NOT HALF-OR-ONE DISPLAY "1,5 NOT IN LIST" END-IF
           DISPLAY R5
           STOP RUN.
