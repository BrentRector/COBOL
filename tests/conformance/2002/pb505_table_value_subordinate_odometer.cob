       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB505TV2.
      *> kb/Work PB505 at the FORMAT 2 (table) VALUE's INTRODUCTION EDITION (COBOL-2002, 13.18.63.2).
      *> 13.18.63.3 SR18/SR20/SR21 and 13.18.63.4 GR12-GR15 carry no version proviso, so the subordinate
      *> arm and the multi-dimension odometer behave here exactly as they do at 2023.  The dynamic-capacity
      *> legs of the 2023 golden are deliberately absent: OCCURS DYNAMIC is a 2014 construct.
      *> EXPECTED, derived from the spec (see tests/conformance/2023/pb505_table_value_geometry.cob for the
      *> quoted rules):
      *>   1[AB|CD|AB]  SR18's subordinate arm + GR14's implied TO (3) + GR13's cyclic reuse
      *>   2[123|456]   GR12's odometer over a 2x3 table
      *>   3[AB/CD|AB/CD]  a GROUP entry's table VALUE, GR5's area deposit per occurrence
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S-GRP.
           05 S-TAB OCCURS 3.
               10 S-X PIC X(2) VALUES ARE "AB" "CD" FROM (1).
       01 M-GRP.
           05 M-ROW OCCURS 2.
               10 M-C PIC 9 OCCURS 3 VALUES ARE 1 2 3 4 5 6 FROM (1 1) TO (2 3).
       01 G-REC.
           05 G-T OCCURS 2 VALUE "ABCD" FROM (1) TO (2).
               10 G-P PIC X(2).
               10 G-Q PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "1[" S-X(1) "|" S-X(2) "|" S-X(3) "]"
           DISPLAY "2[" M-C(1 1) M-C(1 2) M-C(1 3) "|"
                        M-C(2 1) M-C(2 2) M-C(2 3) "]"
           DISPLAY "3[" G-P(1) "/" G-Q(1) "|" G-P(2) "/" G-Q(2) "]"
           STOP RUN.
