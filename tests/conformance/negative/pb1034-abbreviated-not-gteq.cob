      *> reject-at: 85 2002 2014 2023
      *> kb/Work PB1034, the abbreviated-combined-relation consumer of the one operator rule. ISO 1989:2023
      *> 8.8.4.12.2 prints the abbreviated tail's operator as `{ NOT | simple-relational-operator |
      *> extended-relational-operator }`, and 8.7.5.1 prints those two operator sets: neither has `>=` with a NOT.
      *> `A > B AND NOT >= C` therefore does not parse; before the operator rule was narrowed it compiled and
      *> expanded as A NOT >= C (folded to A < C).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1034NEG6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 5.
       01 B PIC 9 VALUE 3.
       01 C PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN.
           IF A > B AND NOT >= C
               DISPLAY "X"
           ELSE
               DISPLAY "Y"
           END-IF
           STOP RUN.
