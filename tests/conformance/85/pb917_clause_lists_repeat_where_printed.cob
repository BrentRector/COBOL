      *> kb/Work PB917 - the positive twin of the pb917-* negatives (compile-only).  ISO 5.2.7 licenses repetition only where an ellipsis stands, and 12.4.5.1 prints one:
      *> `ALTERNATE RECORD KEY IS ... [ WITH DUPLICATES ] ...` repeats, so TWO ALTERNATE RECORD KEY clauses on one file control entry are legal; every other clause appears once.
      *> The clauses of a file control entry (12.4.5.2 SR1) and of a data description entry may be written in any order, each once.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB917REPEATOK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G ASSIGN TO "W13PPB917REPEATOK.dat"
               ORGANIZATION IS INDEXED
               ALTERNATE RECORD KEY IS GA WITH DUPLICATES
               RECORD KEY IS GK
               ALTERNATE RECORD KEY IS GB WITH DUPLICATES.
       DATA DIVISION.
       FILE SECTION.
       FD  G BLOCK CONTAINS 2 RECORDS.
       01  GR.
           05  GK PIC X.
           05  GA PIC X.
           05  GB PIC X.
       WORKING-STORAGE SECTION.
       01  N VALUE 12 USAGE DISPLAY PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
