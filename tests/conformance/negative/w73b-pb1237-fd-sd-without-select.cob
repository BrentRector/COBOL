      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1237 / PB1290 - ISO 13.4.5.3 SR1 (FD) and 13.4.6.3 SR1
      *> (SD): "File-name-1 shall be specified in a file control entry."
      *> F9 and S9 have no SELECT. Before the fix both compiled clean and
      *> the FD's records resolved against a file with no assignment.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W73BFDSR1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "w73b_fd1.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       FD F9.
       01 R9 PIC X(10).
       SD S9.
       01 RS9 PIC X(10).
       PROCEDURE DIVISION.
           MOVE "X" TO R9.
           DISPLAY R9.
           STOP RUN.
