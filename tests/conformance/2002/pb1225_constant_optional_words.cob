      *> kb/Work PB1225 - IS, AS and OF are OPTIONAL words of the constant entry (ISO 13.10, 2002). The printed
      *> 13.10.2 general format underlines 1/01, CONSTANT, GLOBAL, BYTE-LENGTH, LENGTH and FROM only, and 5.2.3:
      *> optional words are "shown in uppercase and not underlined in general formats". Each entry below omits
      *> one or more of them and means what its full spelling means:
      *>   K  = 8     CONSTANT 8            (literal-1, 13.10.4 GR1)
      *>   K2 = AB    CONSTANT "AB"         (literal-1, GR1/GR2)
      *>   K3 = 7     CONSTANT LENGTH W     (LENGTH OF data-name-2, GR6; W is PIC X(7))
      *>   K3 again   CONSTANT AS LENGTH OF W - 13.10.3 SR9: a duplicated constant-name carries the same
      *>              specification, and OF is no part of data-name-2's specification
      *>   K4 = 7     CONSTANT IS GLOBAL LENGTH OF W
      *>   K5 = 7     CONSTANT BYTE-LENGTH W (BYTE-LENGTH OF data-name-1, GR5)
      *>   K6 = 5     CONSTANT GLOBAL 2 + 3 (arithmetic-expression-1, GR4)
      *> The negative half: negative/pb1225-constant-without-as-at-85 (the entry is a 2002 introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1225OPTW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT 8.
       01 K2 CONSTANT "AB".
       01 W PIC X(7).
       01 K3 CONSTANT LENGTH W.
       01 K3 CONSTANT AS LENGTH OF W.
       01 K4 CONSTANT IS GLOBAL LENGTH OF W.
       01 K5 CONSTANT BYTE-LENGTH W.
       01 K6 CONSTANT GLOBAL 2 + 3.
       PROCEDURE DIVISION.
           DISPLAY "K=" K " K2=" K2 " K3=" K3 " K4=" K4 " K5=" K5
               " K6=" K6
           STOP RUN.
