      *> reject-at: 85 2002 2014 2023
      *> ISO 12.3.2 prints SOURCE-COMPUTER, OBJECT-COMPUTER, SPECIAL-NAMES and REPOSITORY in that sequence,
      *> each in its own bracket, and 12.3.3 never frees the order, so 5.2.1 binds it. kb/Work PB1508's
      *> sibling arm: SPECIAL-NAMES written before SOURCE-COMPUTER compiled clean.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1508NE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       SOURCE-COMPUTER. PB1508-COMPUTER.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
