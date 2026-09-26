      *> reject-at: 2014 2023
      *> kb/Work PB1080 — ISO §12.4.5.8.3 SR3 "Data-name-1 shall not reference a dynamic-length elementary item
      *> or a variable-length group" and SR4 "Data-name-1 shall not be subject to a BASED clause in its data
      *> description". The dynamic-length item ran with a one-character status; the BASED item died
      *> EC-DATA-PTR-NULL at OPEN. (DYNAMIC LENGTH is a COBOL-2014 introduction, so the program exists from 2014.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1080ND.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb1080e.dat" FILE STATUS IS FS-D.
           SELECT F2 ASSIGN TO "pb1080f.dat" FILE STATUS IS FS-B.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       FD F2.
       01 R2 PIC X(10).
       WORKING-STORAGE SECTION.
       01 FS-D PIC X DYNAMIC LENGTH.
       01 FS-B PIC XX BASED.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
