      *> kb/Work PB1302 - each type declaration is completed as a declaration,
      *>   so a TYPE clause naming a LATER declaration, and a SAME AS naming a
      *>   later entry, expand whatever order the entries are written in.
      *> ISO 13.18.58.3 SR2 forbids only a TYPE clause that directly or
      *>   indirectly references the declaration it is in; none does here.
      *> Expected: PQS/UVW.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1302FWD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 TYPEDEF.
          05 A TYPE T2.
          05 A2 PIC X.
       01 T2 TYPEDEF.
          05 B PIC X(2).
       01 R TYPE T1.
       01 G.
          05 GX PIC X.
          05 GY SAME AS H.
       01 H PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "PQ" TO B OF A OF R
           MOVE "S" TO A2 OF R
           MOVE "UVW" TO GY
           DISPLAY R "/" GY
           STOP RUN.
