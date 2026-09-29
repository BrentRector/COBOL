      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1078 - ISO 12.4.5.2 SR4: "Literal-1 shall be an
      *>   alphanumeric literal and shall be neither a figurative
      *>   constant nor a zero-length literal." Both refused forms below
      *>   are COBOLNET2645 at every edition (the zero-length literal used
      *>   to compile and OPEN then answered status 30).
      *> cite.py --check 12.4.5.2 "Literal-1 shall be an alphanumeric
      *>   literal and shall be neither a figurative constant nor a
      *>   zero-length literal" -> OK  12.4.5.2 4)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1078N.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "".
           SELECT F2 ASSIGN TO SPACE.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(3).
       FD F2.
       01 R2 PIC X(3).
       PROCEDURE DIVISION.
           STOP RUN.
