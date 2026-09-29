      *> reject-at: 2023
      *> RECORD CONTAINS ON A LINE SEQUENTIAL FILE (kb/Work PB1238).
      *> ISO/IEC 1989:2023 §13.4.5.3 SR4: "If the LINE SEQUENTIAL phrase of the ORGANIZATION
      *> clause of the sequential format of the 12.4.5, File control entry is specified
      *> neither the BLOCK CONTAINS clause nor the RECORD CONTAINS clause shall be
      *> specified." LINE SEQUENTIAL is new in ISO/IEC 1989:2023, so the rule is 2023-only
      *> (below it the organization itself is refused). Refused, COBOLNET2605.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1238LS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1238ls.txt"
               ORGANIZATION LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F RECORD CONTAINS 10 CHARACTERS.
       01 R PIC X(10).
       PROCEDURE DIVISION.
           DISPLAY "RAN"
           STOP RUN.
