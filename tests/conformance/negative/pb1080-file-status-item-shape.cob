      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1080 — ISO §12.4.5.8.3 SR1 "Data-name-1 shall not be subject to any OCCURS clauses" and SR2
      *> "Data-name-1 shall reference a two-character data item of the category alphanumeric, defined in the
      *> working-storage, local-storage, or linkage section". Each FILE STATUS item below breaks one: a table
      *> element (died at OPEN), PIC 99 (crashed the C# backend), PIC X(3) (a padded status), an FD record
      *> (wrong section). All compiled clean before the screen existed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1080NG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb1080a.dat" FILE STATUS IS FS-T.
           SELECT F2 ASSIGN TO "pb1080b.dat" FILE STATUS IS FS-N.
           SELECT F3 ASSIGN TO "pb1080c.dat" FILE STATUS IS FS-3.
           SELECT F4 ASSIGN TO "pb1080d.dat" FILE STATUS IS R3.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       FD F2.
       01 R2 PIC X(10).
       FD F3.
       01 R3 PIC XX.
       FD F4.
       01 R4 PIC X(10).
       WORKING-STORAGE SECTION.
       01 T.
          05 FS-T PIC XX OCCURS 3.
       01 FS-N PIC 99.
       01 FS-3 PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
