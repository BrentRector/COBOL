      *> kb/Work PB1265 - ISO 13.18.38.2's data-division formats, each written in full and in
      *> its printed order, beside the negatives pb1265-occurs-* (a superset parse narrowed at
      *> bind time). Format 1: T1 OCCURS 3 TIMES. Format 2: T2 OCCURS 1 TO 4 TIMES DEPENDING ON
      *> N2 - N2 = 2, so the group R2 holds 2 occurrences (13.18.38.4 GR8) and MOVE "WXYZ" TO R2
      *> keeps WX. Format 4: CAPACITY IN, FROM, TO and INITIALIZED, all four once and in the
      *> printed order (5.2.1) - the capacity register C4 starts at FROM 2 (13.18.38.4 GR16).
      *> Fails if a conforming format is refused, or the extents are wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1265OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R1.
           05 T1 PIC X OCCURS 3 TIMES.
       01 N2 PIC 9 VALUE 2.
       01 R2.
           05 T2 PIC X OCCURS 1 TO 4 TIMES DEPENDING ON N2.
       01 R4.
           05 T4 PIC X OCCURS DYNAMIC CAPACITY IN C4 FROM 2 TO 9
                 INITIALIZED.
       PROCEDURE DIVISION.
           MOVE "ABC" TO R1
           MOVE "WXYZ" TO R2
           DISPLAY "R1=" R1 " R2=" R2 " C4=" C4
           STOP RUN.
