      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB917 - ISO 5.2.6.2 and 5.2.7 (cite.py OK on both).  13.4.5.2 prints BLOCK CONTAINS once with no ellipsis in every format, so a file description
      *> entry that writes it twice is non-conforming (13.4.5.3 SR2 makes the clauses order-free, not repeatable).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB917FDBLOCK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "W13PPB917FDBLOCK.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  F BLOCK CONTAINS 2 RECORDS BLOCK CONTAINS 3 RECORDS.
       01  FR PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
