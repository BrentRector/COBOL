      *> reject-at: 85 2002 2014 2023
      *> A FILE CONTROL ENTRY CLAUSE WRITTEN IN THE FILE DESCRIPTION ENTRY (kb/Work PB1238,
      *> PB1081). ORGANIZATION, ACCESS MODE, RECORD KEY, ALTERNATE RECORD KEY and FILE
      *> STATUS are clauses of the file control entry (ISO/IEC 1989:2023 §12.4.5.1); none
      *> of the three rendered §13.4.5.2 file description formats contains them. Until
      *> PB1238 they parsed and were silently DROPPED — this FD compiled to a sequential
      *> connector and FS was never set. Refused by name, COBOLNET2604.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1238FC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1238fc.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F ORGANIZATION IS RELATIVE FILE STATUS IS FS.
       01 R PIC X(10).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
