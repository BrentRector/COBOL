      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1217 - ISO 13.18.10.3 SR1: "If integer-1 is specified, integer-2 shall be greater than integer-1."   cite.py: OK  13.18.10.3 1)  (Syntax rule)
      *> BLOCK CONTAINS 10 TO 5 CHARACTERS: integer-2 (5) is not greater than integer-1 (10).  The clause models nothing at run time, but the rule is a syntax rule
      *> and 4.2.2 requires its violation to be diagnosed.  (RECORD CONTAINS' twin is pb721-record-contains-inverted-range, under the same code.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1217BLOCKINVERTED.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "W13PPB1217BLOCKINVERTED.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F BLOCK CONTAINS 10 TO 5 CHARACTERS.
       01  FR PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
