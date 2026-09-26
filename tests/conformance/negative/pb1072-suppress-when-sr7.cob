      *> reject-at: 2023
      *> kb/Work PB1072 — ISO §12.4.5.6.3 SR7: "Literal-1 shall be an alphanumeric literal, a national literal,
      *> or a figurative constant, and shall be of the same category as data-name-1 or data-name-2. If ALL
      *> literal is specified, the literal shall be one character long." All three obligations are broken
      *> below (a numeric literal, a national literal on an alphanumeric key, an ALL literal two characters
      *> long); each compiled clean before the screen existed. (Below 2023 the SUPPRESS phrase itself is
      *> refused by its introduction gate.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1072NG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb1072ng.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-A1 SUPPRESS WHEN 5
               ALTERNATE RECORD KEY IS R-A2 SUPPRESS WHEN N"A"
               ALTERNATE RECORD KEY IS R-A3 SUPPRESS WHEN ALL "AB".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 R-KEY PIC X(2).
          05 R-A1  PIC X(3).
          05 R-A2  PIC X(3).
          05 R-A3  PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F CLOSE F.
           STOP RUN.
