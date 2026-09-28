      *> reject-at: 2014 2023
      *> kb/Work PB1213 — ISO/IEC 1989:2023 §13.10.3 SR12: "Data-name-1 and data-name-2 shall not be dynamic-length
      *> elementary items or variable-length groups." G is a variable-length group (§8.5.1.12.1) because the
      *> dynamic-length elementary item W is subordinate to it. Before the fix the screen walked dynamic-capacity
      *> TABLES only, so this compiled and DISPLAY K printed 2 — the length of G's fixed part.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1213CL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A PIC X(2).
          05 W PIC X DYNAMIC LENGTH.
       01 K CONSTANT AS LENGTH OF G.
       PROCEDURE DIVISION.
           DISPLAY K.
           STOP RUN.
