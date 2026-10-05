      *> A CELL-BACKED DYNAMIC TABLE UNDER A FIXED OCCURS KEEPS ITS TABLE VALUE (train 1021
      *> review of PB1042). ISO/IEC 1989:2023 §13.18.63.3 SR20: one subscript-1 "for each
      *> OCCURS clause for the subject of the entry or superordinate to that entry", so
      *> FROM (2 1) TO (2 2) names E(2 1) and E(2 2) under OUTER(2), and §13.18.63.4 GR12
      *> initializes them to "A" when the program is placed in its initial state (GR4 c).
      *> ADDRESS OF R (§8.4.3.11) puts R on cell storage; that may not change E's initial
      *> value, so both programs print [AA]. PB1042CA: dynamic under a fixed OCCURS.
      *> PB1042CB: a fixed OCCURS between two dynamic tables, FROM (1 2 1) TO (1 2 2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1042CA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 R.
          05 OUTER OCCURS 2.
             10 DT OCCURS DYNAMIC CAPACITY IN DC FROM 2 TO 3.
                15 E PIC X VALUE "A" FROM (2 1) TO (2 2).
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF R
           DISPLAY "[" E(2 1) E(2 2) "]"
           CALL "PB1042CB"
           STOP RUN.
       END PROGRAM PB1042CA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1042CB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 R.
          05 D1 OCCURS DYNAMIC CAPACITY IN C1 FROM 1 TO 2.
             10 MID OCCURS 2.
                15 D2 OCCURS DYNAMIC CAPACITY IN C2 FROM 2 TO 3.
                   20 E PIC X VALUE "A" FROM (1 2 1) TO (1 2 2).
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF R
           DISPLAY "[" E(1 2 1) E(1 2 2) "]"
           GOBACK.
       END PROGRAM PB1042CB.
