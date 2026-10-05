      *> reject-at: 85 2002
      *> kb/Work PB1042 — a dynamic-capacity table in an EXTERNAL record is legal from COBOL 2014,
      *> the edition that introduced OCCURS DYNAMIC (ISO/IEC 1989:2023 §8.5.1.9.1 3)); below it the
      *> Format 4 OCCURS clause does not exist and the entry is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1042NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PB1042NR EXTERNAL.
          05 T PIC X(2) OCCURS DYNAMIC CAPACITY IN TC.
       PROCEDURE DIVISION.
           MOVE "AB" TO T(1)
           DISPLAY TC
           STOP RUN.
