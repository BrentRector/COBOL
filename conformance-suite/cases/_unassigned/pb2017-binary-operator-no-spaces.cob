      *>--- conformance-case
      *> id: pb2017-binary-operator-no-spaces
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      * kb/Work PB2017 - ISO 8.7.1: an arithmetic operator "shall be
      * preceded by a space and followed by a space".  A*B touches
      * both operands.  It compiled and ran; COBOLNET2993.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2017N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(4) VALUE 6.
       01 B PIC S9(4) VALUE 3.
       01 R PIC S9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = A*B.
           STOP RUN.
       END PROGRAM PB2017N1.
