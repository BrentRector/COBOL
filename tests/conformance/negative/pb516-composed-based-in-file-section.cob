      *> reject-at: 2002 2014 2023
      *> kb/Work PB516 - the SAME rule (ISO 13.16.3 SR16, cite.py OK) for a BASED
      *> clause a TYPE clause COMPOSES: 13.18.57.4 GR1 (cite.py OK) makes the TYPE
      *> clause's effect "as though the data description identified by
      *> type-name-1 had been coded in place", so R under the FD is a BASED entry
      *> in the file section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB516FDT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB516FDT.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R TYPE T.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF BASED PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
