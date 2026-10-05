      *> reject-at: 85
      *> kb/Work PB1446 - ISO 8.3.3.3.2 4) "The size of a fixed-point numeric literal is
      *>   equal to the number of digits in the string of characters in the literal":
      *>   leading zeros are digits, so -0000000000000000001 (value -1) has size 19 and
      *>   exceeds COBOL-85's 18-digit literal ceiling (COBOLNET0802); 2002 and later
      *>   admit 1 through 31 digits.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1446S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R1 PIC S9(18) VALUE ZERO.
       PROCEDURE DIVISION.
           MOVE -0000000000000000001 TO R1
           DISPLAY R1
           STOP RUN.
