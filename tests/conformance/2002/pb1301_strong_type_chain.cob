      *> kb/Work PB1301 - STRONG-ness through a TYPE chain is ONE answer.
      *> ISO 13.18.57.4 GR1: 'The effect of the TYPE clause is as though the
      *>   data description identified by type-name-1 had been coded in place
      *>   of the TYPE clause' - so data typed through S2 (01 S2 TYPEDEF TYPE S,
      *>   S STRONG) is described with TYPE S and is strongly typed (8.5.3.1).
      *> Legal here: X1 at level 1 (13.18.57.3 SR6), Y inside a STRONG type
      *>   declaration (SR6 second arm), a MOVE between two S2-typed items, and
      *>   a weak chain over an elementary type (WE TYPEDEF TYPE E).
      *> Expected: AB7 / XYZ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1301CHN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S TYPEDEF STRONG.
          05 SA PIC X.
          05 SB PIC X.
       01 S2 TYPEDEF TYPE S.
       01 X1 TYPE S2.
       01 W TYPEDEF STRONG.
          05 Y TYPE S2.
          05 YC PIC 9.
       01 Z TYPE W.
       01 E TYPEDEF PIC X(3).
       01 WE TYPEDEF TYPE E.
       01 V TYPE WE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "A" TO SA OF X1
           MOVE "B" TO SB OF X1
           MOVE X1 TO Y OF Z
           MOVE 7 TO YC OF Z
           DISPLAY SA OF Y OF Z SB OF Y OF Z YC OF Z
           MOVE "XYZ" TO V
           DISPLAY V
           STOP RUN.
