      *> kb/Work PB1080 — ISO §12.4.5.8.3 SR2: the FILE STATUS item "shall reference a two-character data item
      *> of the category alphanumeric, defined in the working-storage, local-storage, or linkage section". An
      *> elementary PIC XX item and a two-character alphanumeric GROUP (§13.18.29.4 GR3 — a group with no
      *> GROUP-USAGE clause is an alphanumeric group item) both qualify, and §12.4.5.8.4 GR1 updates each with the
      *> I-O status: OPEN INPUT of an absent non-optional file is '35' (§9.1.13.6), OPEN OUTPUT '00'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1080FS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb1080-absent.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS-ELEM.
           SELECT F2 ASSIGN TO "pb1080-f2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS-GRP.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       FD F2.
       01 R2 PIC X(10).
       WORKING-STORAGE SECTION.
       01 FS-ELEM PIC XX.
       01 FS-GRP.
          05 FS-1 PIC X.
          05 FS-2 PIC X.
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT F1.
           DISPLAY "F1 OPEN [" FS-ELEM "]".
           OPEN OUTPUT F2.
           DISPLAY "F2 OPEN [" FS-GRP "] " FS-1 "/" FS-2.
           CLOSE F2.
           DISPLAY "F2 CLOSE [" FS-GRP "]".
           STOP RUN.
