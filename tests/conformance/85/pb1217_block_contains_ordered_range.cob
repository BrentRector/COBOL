      *> kb/Work PB1217 - the positive twin (compile-only).  ISO 13.18.10.3 SR1: with integer-1 specified, integer-2 shall be greater than integer-1.
      *> BLOCK CONTAINS 10 TO 100 CHARACTERS and the one-integer form BLOCK CONTAINS 5 RECORDS are both legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1217BLOCKOK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "W13PPB1217BLOCKOKF.dat".
           SELECT G ASSIGN TO "W13PPB1217BLOCKOKG.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F BLOCK CONTAINS 10 TO 100 CHARACTERS.
       01  FR PIC X.
       FD  G BLOCK CONTAINS 5 RECORDS.
       01  GR PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
