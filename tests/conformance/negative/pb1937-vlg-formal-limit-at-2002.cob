      *> reject-at: 85 2002
      *> kb/Work PB1937 - a variable-length group whose dynamic-length member's LIMIT differs
      *> between the argument and the formal is a COBOL-2014 program: the DYNAMIC LENGTH clause
      *> (ISO 8.5.1.10 / 13.18.19) that makes the group variable-length is a 2014 addition, so
      *> editions 85 and 2002 reject it with COBOLNET0900 before 14.2.3 GR8 is ever in question.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1937NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 D PIC X DYNAMIC LENGTH LIMIT 10.
       PROCEDURE DIVISION.
           MOVE "abcdefgh" TO D
           DISPLAY "[" G "]"
           STOP RUN.
