      *> kb/Work PB1246 - the positive twin of the pb1246-* negatives.  ISO 13.11.1 (a record description's first entry has level-number 1), 8.5.1.3.2 (a level-77
      *> item stands alone; all items immediately subordinate to a group carry numerically equal level-numbers greater than the group's) and
      *> 13.18.33.3 SR3 (level-numbers 1 through 9 may be written 01 through 09).  Level-numbers need not be consecutive: G's members are 02, B's are 07.
      *>   G = A "AB" + B (B1 "C", B2 "4") + C "E"  -> "ABC4E";   77 W "Z", 77 V "Y", H's one member (level 49) "K".
      *> Expected: [Z][ABC4E][Y][K]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W13PPB1246LEGAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  W PIC X VALUE "Z".
       01  G.
           02  A PIC X(2) VALUE "AB".
           02  B.
               07  B1 PIC X VALUE "C".
               07  B2 PIC 9 VALUE 4.
           02  C PIC X VALUE "E".
       77  V PIC X VALUE "Y".
       01  H.
           49  K PIC X VALUE "K".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "[" W "][" G "][" V "][" H "]".
           STOP RUN.
