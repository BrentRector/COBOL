      *> reject-at: 85 2002
      *> kb/Work PB244 shape (b) - the variable-length group that combines a
      *> dynamic-length item with an OCCURS DEPENDING table (and the one whose
      *> dynamic-length item sits in a fixed OCCURS table) is a COBOL-2014
      *> program: the DYNAMIC LENGTH clause that makes the group variable-length
      *> (ISO 8.5.1.12.1, 13.18.19) is a 2014 addition, so editions 85 and 2002
      *> reject it with the edition-band diagnostic COBOLNET0900 BEFORE the
      *> group's display format (14.9.11.4 GR7) is ever in question.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB244NEG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K PIC 9 VALUE 2.
       01 G1.
          05 D PIC X DYNAMIC LENGTH LIMIT 5.
          05 T PIC X OCCURS 1 TO 3 DEPENDING ON K.
       01 G2.
          05 TE OCCURS 2.
             10 DD PIC X DYNAMIC LENGTH LIMIT 5.
             10 FX PIC X.
       PROCEDURE DIVISION.
           DISPLAY "[" G1 "]" "[" G2 "]"
           STOP RUN.
