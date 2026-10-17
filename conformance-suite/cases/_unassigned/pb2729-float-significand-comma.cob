*>--- conformance-case
*> id: pb2729-float-significand-comma
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2729 - under DECIMAL-POINT IS COMMA the comma is the decimal point (12.3.7.4 GR14 a), and a
      *> floating-point literal is "formed from two fixed-point numeric literals separated by the letter 'E'"
      *> (8.3.3.3.3 rule 1) whose decimal point "may appear anywhere within the literal except as the rightmost
      *> character" (8.3.3.3.2 rule 3), so `3,E2` is no literal: the lexer reads `3` then a comma, and a comma
      *> with no space after it is no separator (8.3.5 rule 2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2729COM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y PIC 9(5) VALUE 3,E2.
       PROCEDURE DIVISION.
           DISPLAY Y
           STOP RUN.
