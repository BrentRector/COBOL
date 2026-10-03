      *> reject-at: 2002 2014 2023
      *> kb/Work PB1034, the compile-time lane of the one operator rule. ISO 1989:2023 7.3.8.2 SR1 a) makes a
      *> constant-conditional relation "formed according to the rules in 8.8.4.2", and 8.8.4.2.2 Format 1 prints no
      *> `NOT >=` (rendered page PDF p217). `>>IF X NOT >= 3` used to be accepted and took the ELSE branch; it is now
      *> refused in the directive stage like any other malformed constant-conditional expression.
      *>   cite.py --check 7.3.8.2 "The condition shall be formed according to the rules in 8.8.4.2, Simple
      *>     relation conditions." -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1034NEG7.
       PROCEDURE DIVISION.
       MAIN.
       >>IF 5 NOT >= 3
           DISPLAY "SELECTED".
       >>END-IF
           STOP RUN.
