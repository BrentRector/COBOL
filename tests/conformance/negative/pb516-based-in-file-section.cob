      *> reject-at: 2002 2014 2023
      *> kb/Work PB516 - ISO 13.16.3 SR16 (cite.py OK): "The BASED clause may be
      *> specified only in data description entries in the linkage section, in
      *> the working-storage section, and in the local-storage section." A BASED
      *> record under a file description entry is refused (it used to compile:
      *> a record area the file fills, described as an unallocated template).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB516FD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB516FD.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R BASED PIC X(10).
       WORKING-STORAGE SECTION.
       77  Z PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
