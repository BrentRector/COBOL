      *> reject-at: 2002 2014 2023
      *> kb/Work PB1277 -- ISO 13.18.43.3 SR3 (FORMAT 1): "No record description entry
      *> for the file may specify a number of bytes greater than integer-1", and
      *> 13.18.43.4 GR3 counts that size in bytes "regardless of the types of
      *> characters used". F-REC is PIC N(10): ten national character positions of
      *> two bytes each (determination D-N1) = 20 bytes, greater than integer-1 = 10.
      *> Until PB1277 the size was counted in character positions (10) and this
      *> program compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1277N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1277n1.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F RECORD CONTAINS 10 CHARACTERS.
       01  F-REC PIC N(10).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT F
           CLOSE F
           STOP RUN.
