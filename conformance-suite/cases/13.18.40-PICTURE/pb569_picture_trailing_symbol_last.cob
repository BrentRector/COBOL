      *>--- conformance-case
      *> id: pb569_picture_trailing_symbol_last
      *> rules: [SR-13.18.40.3-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB569 - ISO 13.18.40.3 SR7: "If the symbol ',' or the symbol '.' is the last symbol of
      *> character-string-1, the PICTURE clause shall be the last clause of the data description entry
      *> and shall be followed immediately (without an intervening separator space) by the separator
      *> period." This is the POSITIVE half - every legal shape binds. The negatives are
      *> negative/pb569-picture-comma-not-last and negative/pb569-picture-period-not-last.
      *> EXPECTED, from the rules (not measured):
      *>   E1 PIC 999,.  - the ',' is the last picture symbol (a simple-insertion comma, 13.18.40.5)
      *>                   and the separator period follows it: MOVE 12 -> 012,
      *>   E2 USAGE DISPLAY PIC 999,. - the same, with PICTURE the LAST of two clauses: MOVE 5 -> 005,
      *>   E3 PIC 999..  - the first '.' is the decimal point (the last symbol), the second the
      *>                   separator period: MOVE 12 -> 012.
      *>   E4 PIC 999, VALUE ZERO. - ',' followed by a space is a SEPARATOR (8.3.5 rule 2), not a
      *>                   picture symbol, so character-string-1 is 999 (numeric) and SR7 does not
      *>                   apply: 000
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB569P1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC 999,.
       01 E2 USAGE DISPLAY PIC 999,.
       01 E3 PIC 999..
       01 E4 PIC 999, VALUE ZERO.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 12 TO E1 E3
           MOVE 5 TO E2
           DISPLAY "E1=[" E1 "] E2=[" E2 "] E3=[" E3 "] E4=[" E4 "]"
           STOP RUN.
