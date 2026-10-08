      *> kb/Work PB1722: A TABLE'S INITIAL STATE IS EMITTED FROM ITS
      *> OCCURRENCE RUNS, NEVER ELEMENT BY ELEMENT. Expected values come
      *> from ISO/IEC 1989:2023 13.18.63.4 GR12 (odometer order), GR13
      *> ("all occurrences of literal-1 are reused, in the order
      *> specified"), GR14 (no TO = to the maximum), GR15 (the last FROM
      *> phrase wins), GR5 (a group VALUE initializes the area) and
      *> 14.9.20.4 GR5 c) 1. c (INITIALIZE ... TO VALUE restores the keyed
      *> occurrences only).
      *> R1 is 1000 x 1000: "A" "B" "C" cycle from (1 2) through (999 999)
      *> and "Z" overrides row 500, so T1 (2 1) is run position 999 = A,
      *> T1 (501 1) is 499999 = B, T1 (999 999) is 998997 = A, and
      *> T1 (1 1) / T1 (1000 1000) are outside every phrase.
      *> R2 seeds a REDEFINES-aliased image: X Y cycle over ranks 1..10.
      *> R3 is an ALL "AB" group fill over a nested table (15 positions).
      *> R4 cycles 1 2 3 4 over 20 x 30 x 7: rank 6 = 3, 7 = 4, 4199 = 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1722TVR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R1.
         03 G1 OCCURS 1000.
           05 T1 PIC X OCCURS 1000
              VALUE "A" "B" "C" FROM (1 2) TO (999 999)
                    "Z" FROM (500 1) TO (500 1000).
       01 R2.
         03 G2 OCCURS 4.
           05 T2 PIC X OCCURS 3 VALUE "X" "Y" FROM (1 2) TO (4 2).
       01 R2V REDEFINES R2 PIC X(12).
       01 R3 VALUE ALL "AB".
         03 G3 OCCURS 5.
           05 T3 PIC X OCCURS 3.
       01 R4.
         03 G4 OCCURS 20.
           05 H4 OCCURS 30.
             07 T4 PIC 99 OCCURS 7 VALUE 1 2 3 4 FROM (1 1 1).
       PROCEDURE DIVISION.
           DISPLAY "[" T1 (1 1) T1 (1 2) T1 (1 3) T1 (1 4) T1 (2 1)
                   T1 (500 1) T1 (500 1000) T1 (501 1) T1 (999 999)
                   T1 (1000 1000) "]".
           DISPLAY "[" R2V "]".
           DISPLAY "[" G3 (2) T3 (5 3) "]".
           DISPLAY T4 (1 1 1) T4 (1 1 7) T4 (1 2 1) T4 (20 30 7).
           MOVE ALL "*" TO R1.
           INITIALIZE R1 ALL TO VALUE.
           DISPLAY "[" T1 (1 1) T1 (1 2) T1 (1 3) T1 (1 4) T1 (2 1)
                   T1 (500 1) T1 (500 1000) T1 (501 1) T1 (999 999)
                   T1 (1000 1000) "]".
           INITIALIZE R1.
           DISPLAY "[" T1 (1 2) "]".
           STOP RUN.
