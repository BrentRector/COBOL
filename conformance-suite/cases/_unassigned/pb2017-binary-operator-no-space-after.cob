      *>--- conformance-case
      *> id: pb2017-binary-operator-no-space-after
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      * kb/Work PB2017 - ISO 8.7.1: the BINARY minus of A -1 has a
      * space before it but none after it.  (A sign is part of a
      * numeric literal only as the literal's leftmost character,
      * 8.3.3.3.2 2), and an operand cannot follow an operand, so
      * the minus is the subtraction operator.)  COBOLNET2993.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2017N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC S9(4) VALUE 6.
       01 R PIC S9(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R = A -1.
           STOP RUN.
       END PROGRAM PB2017N2.
