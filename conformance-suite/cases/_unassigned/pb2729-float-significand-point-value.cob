*>--- conformance-case
*> id: pb2729-float-significand-point-value
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2729 - a floating-point literal is "formed from two fixed-point numeric literals separated by
      *> the letter 'E'" (8.3.3.3.3 rule 1), and a fixed-point literal's decimal point "may appear anywhere within
      *> the literal except as the rightmost character" (8.3.3.3.2 rule 3), so `3.E2` in a VALUE clause is no
      *> literal: the lexer reads `3` then a period, and a period with no space after it is no separator (8.3.5
      *> rule 3). It used to lex as one floating-point literal and kill the backend (CS1061, exit 70).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2729VAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y PIC 9(5) VALUE 3.E2.
       PROCEDURE DIVISION.
           DISPLAY Y
           STOP RUN.
