      *> kb/Work PB1368 / PB1228 - CONSTANT ... FROM a compilation variable of each literal category, under
      *> DECIMAL-POINT IS COMMA (ISO 13.10, 2002). 13.10.4 GR2: "the class and category of constant-name-1 is the
      *> same as that of ... the literal represented by compilation-variable-name-1"; GR1: the constant is "as if
      *> ... the text represented by compilation-variable-name-1 were written where constant-name-1 is written".
      *> 12.3.7.4 NOTE 4: DECIMAL-POINT IS COMMA "is not processed until after the text manipulation stage ... and
      *> therefore does not impact literals specified in compiler directives" - so >>DEFINE R AS 1.5 is one and a
      *> half, and in this source unit the constant CR is that value, the literal 1,5.
      *>   NE = 3,0   CR * 2 = 3.0, edited by PIC 9,9 (comma the decimal separator)
      *>   NX = XY    a national variable as a PIC N(2) VALUE
      *>   BX = 101   a boolean variable as a PIC 1(3) VALUE
       >>DEFINE R AS 1.5
       >>DEFINE NN AS N"XY"
       >>DEFINE BB AS B"101"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1368CATS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CR  CONSTANT FROM R.
       01 CNN CONSTANT FROM NN.
       01 CBB CONSTANT FROM BB.
       01 M   PIC 9V9.
       01 NE  PIC 9,9.
       01 NX  PIC N(2) VALUE CNN.
       01 BX  PIC 1(3) VALUE CBB.
       PROCEDURE DIVISION.
           COMPUTE M = CR * 2
           MOVE M TO NE
           DISPLAY "NE=" NE " NX=" NX " BX=" BX
           STOP RUN.
