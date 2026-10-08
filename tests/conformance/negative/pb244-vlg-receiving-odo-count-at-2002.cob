      *> reject-at: 85 2002
      *> kb/Work PB244 - the receiving OCCURS DEPENDING count of a variable-length group MOVE
      *> (ISO 13.18.38.4 GR8 a) is a COBOL-2014 program: the DYNAMIC LENGTH clause that makes the
      *> group variable-length (ISO 8.5.1.12.1, 13.18.19) is a 2014 addition, so editions 85
      *> and 2002 reject it with the edition-band diagnostic COBOLNET0900 BEFORE the MOVE's
      *> group rule (14.9.25.4 GR9) is ever in question.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244NRC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 VALUE 3.
       01 K2 PIC 9 VALUE 1.
       01 G1.
          05 D1 PIC X DYNAMIC LENGTH LIMIT 5.
          05 T1 PIC X OCCURS 1 TO 3 DEPENDING ON K.
       01 G2.
          05 D2 PIC X DYNAMIC LENGTH LIMIT 5.
          05 T2 PIC X OCCURS 1 TO 3 DEPENDING ON K2.
       PROCEDURE DIVISION.
           MOVE G1 TO G2
           STOP RUN.
