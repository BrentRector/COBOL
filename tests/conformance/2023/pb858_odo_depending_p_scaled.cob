      *> A TRAILING-P OCCURS DEPENDING ON OBJECT IS AN INTEGER (train 1021 review).
      *> ISO/IEC 1989:2023 §13.18.38.3 SR17: "Data-name-1 shall describe an integer";
      *> §5.5 2) b) 2. admits a fixed-point item with no digit position right of the radix
      *> point, which PIC 9P is (§13.18.40.4 GR14). So no COBOLNET0852. §13.18.38.4 GR7:
      *> N's value is the current number of occurrences, so T is 20 bytes at N = 20 and
      *> 30 bytes at N = 30 (FUNCTION LENGTH).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB858OP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9P VALUE 20.
       01 T.
          05 E PIC X OCCURS 10 TO 30 TIMES DEPENDING ON N.
       PROCEDURE DIVISION.
           DISPLAY FUNCTION LENGTH(T)
           MOVE 30 TO N
           DISPLAY FUNCTION LENGTH(T)
           STOP RUN.
