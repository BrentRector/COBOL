      *> reject-at: 2023
      *> ISO 11.9.10.2 encloses LOCAL-STORAGE, SCREEN and WORKING-STORAGE in choice indicators inside
      *> braces; 5.2.6.4: "any single alternative shall be specified only once". kb/Work PB1508: the
      *> repeated WORKING-STORAGE was folded by a bitwise OR and compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1508NC.
       OPTIONS.
           INITIALIZE WORKING-STORAGE WORKING-STORAGE TO X"5A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
