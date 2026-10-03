      *> reject-at: 2002 2014 2023
      *> kb/Work PB1231 - 13.10.3 SR5: "Neither the value of literal-1 nor the value of any of the literals in
      *> arithmetic-expression-1 shall be dependent, directly or indirectly, upon the value of constant-name-1".
      *> K names K2, which names K: the INDIRECT dependence. Each reference alone is legal - K2 is written after
      *> K - so only the cycle is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1231ICY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT AS K2 + 1.
       01 K2 CONSTANT AS K * 2.
       PROCEDURE DIVISION.
           DISPLAY K
           STOP RUN.
