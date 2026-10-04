      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1217 - ISO 13.18.10.3 SR1 (cite.py OK): "integer-2 shall be greater than integer-1" - GREATER, so equal bounds (5 TO 5 RECORDS) break the rule as
      *> surely as inverted ones; a fixed block size is written without the TO phrase.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1217BLOCKEQUAL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "W13PPB1217BLOCKEQUAL.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F BLOCK CONTAINS 5 TO 5 RECORDS.
       01  FR PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
