      *>--- conformance-case
      *> id: pb2017-unary-operator-no-space-after
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      * kb/Work PB2017 - ISO 8.7.1: a UNARY operator is exempt only
      * against a left parenthesis ("no space is required between a
      * left parenthesis and a unary operator or between a unary
      * operator and a left parenthesis").  -A abuts an identifier,
      * so it is not exempt.  COBOLNET2993.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2017N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(4) VALUE 6.
       01 B PIC S9(4) VALUE 3.
       01 R PIC S9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = -A + B.
           STOP RUN.
       END PROGRAM PB2017N3.
