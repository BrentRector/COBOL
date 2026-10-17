      *> kb/Work PB3268 (review round 1, R1-1 / R1-2) - the intermediate result item of
      *> ISO 14.9.25.4 GR1 holds the VALUE a numeric function returns, on the carrier the renderer returns
      *> it on. GR1 writes the shape out as MOVE a TO temp / MOVE temp TO b / MOVE temp TO c, so a MOVE of
      *> one function to several receivers must store into each what the MOVE of that function to that one
      *> receiver stores; 15.4.1 adds "the returned value is the same for all instances of a given function
      *> within a single execution of the runtime element". The matrix below is that sentence run over
      *> function x argument-shape x receiver-set: each cell moves the call to two receivers and again to
      *> each receiver alone, and counts a cell BAD when they differ. The argument shapes are the ones the
      *> carrier depends on - a floating-point item (a binary64 argument), a floating-point literal (an
      *> SDIDI-carried argument, which takes a function with an SDIDI body OFF the float lane), a nested
      *> function (a binary64 approximation) and an arithmetic expression around each. The receiver sets
      *> include a COMP-2 beside a fixed-point item (the intermediate must hold a 1E+30 and a 1E-20) and
      *> receivers wider than 17 digits (a binary64 intermediate would round an SDIDI value away).
      *>
      *> Expected outcomes besides the matrix (which prints BAD=0000):
      *> VAR  VARIANCE(0.1E0 0.2E0 0.4E0) = 7/450 = 0.015555... into two PIC 9V9(25): the truncated 25 places.
      *> ANN  ANNUITY(0.1E0 3) = 0.1 / (1 - 1.1 ** -3) = 0.1331 / 0.331 = 0.40211480362537764350453172205...
      *> BIG  ABS of a COMP-2 holding 1E+30 into a COMP-2 and a PIC 9V99 leaves the COMP-2 equal to its source.
      *> TINY ABS of a COMP-2 holding 1E-20 into a COMP-2 and a PIC 9V99 leaves the COMP-2 equal to its source.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3268LM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CASES PIC 9(4) VALUE 0.
       01 BAD PIC 9(4) VALUE 0.
       01 F1 COMP-2 VALUE 1.5E0.
       01 F2 COMP-2 VALUE 1.0E-20.
       01 F9 COMP-2 VALUE 1.0E30.
       01 G1 COMP-1 VALUE 2.25E0.
       01 N PIC S9(5)V9(4) VALUE 12.3456.
       01 FA COMP-2.
       01 FB COMP-2.
       01 N1 PIC 9V99.
       01 VQ1 PIC 9V9(25).
       01 VQ2 PIC 9V9(25).
       01 MA0 PIC 9V9(25).
       01 SA0 PIC 9V9(25).
       01 MA1 PIC 9V9(25).
       01 SA1 PIC 9V9(25).
       01 MB0 PIC S9(5)V99.
       01 SB0 PIC S9(5)V99.
       01 MB1 PIC S9(18)V9(12).
       01 SB1 PIC S9(18)V9(12).
       01 MC0 COMP-2.
       01 SC0 COMP-2.
       01 MC1 PIC S9V99.
       01 SC1 PIC S9V99.
       01 MD0 COMP-2.
       01 SD0 COMP-2.
       01 MD1 COMP-2.
       01 SD1 COMP-2.
       01 ME0 COMP-1.
       01 SE0 COMP-1.
       01 ME1 PIC 9V9(15).
       01 SE1 PIC 9V9(15).
       01 MF0 PIC 9V9(20).
       01 SF0 PIC 9V9(20).
       01 MF1 COMP-2.
       01 SF1 COMP-2.
       01 MG0 PIC S9(7)V9(6) COMP-3.
       01 SG0 PIC S9(7)V9(6) COMP-3.
       01 MG1 PIC S9(9)V9(4) COMP-5.
       01 SG1 PIC S9(9)V9(4) COMP-5.
       PROCEDURE DIVISION.
       MAIN.
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F1) TO MA0 MA1
           MOVE FUNCTION ABS(F1) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F1) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F1) TO MB0 MB1
           MOVE FUNCTION ABS(F1) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F1) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F1) TO MC0 MC1
           MOVE FUNCTION ABS(F1) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F1) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F1) TO MD0 MD1
           MOVE FUNCTION ABS(F1) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F1) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F1) TO ME0 ME1
           MOVE FUNCTION ABS(F1) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F1) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F1) TO MF0 MF1
           MOVE FUNCTION ABS(F1) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F1) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F1) TO MG0 MG1
           MOVE FUNCTION ABS(F1) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F1) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F9) TO MA0 MA1
           MOVE FUNCTION ABS(F9) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F9) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F9) TO MB0 MB1
           MOVE FUNCTION ABS(F9) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F9) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F9) TO MC0 MC1
           MOVE FUNCTION ABS(F9) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F9) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F9) TO MD0 MD1
           MOVE FUNCTION ABS(F9) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F9) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F9) TO ME0 ME1
           MOVE FUNCTION ABS(F9) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F9) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F9) TO MF0 MF1
           MOVE FUNCTION ABS(F9) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F9) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F9) TO MG0 MG1
           MOVE FUNCTION ABS(F9) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F9) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F2) TO MA0 MA1
           MOVE FUNCTION ABS(F2) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F2) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F2) TO MB0 MB1
           MOVE FUNCTION ABS(F2) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F2) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F2) TO MC0 MC1
           MOVE FUNCTION ABS(F2) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F2) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F2) TO MD0 MD1
           MOVE FUNCTION ABS(F2) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F2) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F2) TO ME0 ME1
           MOVE FUNCTION ABS(F2) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F2) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F2) TO MF0 MF1
           MOVE FUNCTION ABS(F2) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F2) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F2) TO MG0 MG1
           MOVE FUNCTION ABS(F2) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F2) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(G1) TO MA0 MA1
           MOVE FUNCTION ABS(G1) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(G1) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(G1) TO MB0 MB1
           MOVE FUNCTION ABS(G1) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(G1) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(G1) TO MC0 MC1
           MOVE FUNCTION ABS(G1) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(G1) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(G1) TO MD0 MD1
           MOVE FUNCTION ABS(G1) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(G1) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(G1) TO ME0 ME1
           MOVE FUNCTION ABS(G1) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(G1) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(G1) TO MF0 MF1
           MOVE FUNCTION ABS(G1) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(G1) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(G1) TO MG0 MG1
           MOVE FUNCTION ABS(G1) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(G1) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION FRACTION-PART(F1) TO MA0 MA1
           MOVE FUNCTION FRACTION-PART(F1) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION FRACTION-PART(F1) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION FRACTION-PART(F1) TO MB0 MB1
           MOVE FUNCTION FRACTION-PART(F1) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION FRACTION-PART(F1) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION FRACTION-PART(F1) TO MC0 MC1
           MOVE FUNCTION FRACTION-PART(F1) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION FRACTION-PART(F1) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION FRACTION-PART(F1) TO MD0 MD1
           MOVE FUNCTION FRACTION-PART(F1) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION FRACTION-PART(F1) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION FRACTION-PART(F1) TO ME0 ME1
           MOVE FUNCTION FRACTION-PART(F1) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION FRACTION-PART(F1) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION FRACTION-PART(F1) TO MF0 MF1
           MOVE FUNCTION FRACTION-PART(F1) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION FRACTION-PART(F1) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION FRACTION-PART(F1) TO MG0 MG1
           MOVE FUNCTION FRACTION-PART(F1) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION FRACTION-PART(F1) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SUM(F1 N) TO MA0 MA1
           MOVE FUNCTION SUM(F1 N) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SUM(F1 N) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SUM(F1 N) TO MB0 MB1
           MOVE FUNCTION SUM(F1 N) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SUM(F1 N) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SUM(F1 N) TO MC0 MC1
           MOVE FUNCTION SUM(F1 N) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SUM(F1 N) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SUM(F1 N) TO MD0 MD1
           MOVE FUNCTION SUM(F1 N) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SUM(F1 N) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SUM(F1 N) TO ME0 ME1
           MOVE FUNCTION SUM(F1 N) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SUM(F1 N) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SUM(F1 N) TO MF0 MF1
           MOVE FUNCTION SUM(F1 N) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SUM(F1 N) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SUM(F1 N) TO MG0 MG1
           MOVE FUNCTION SUM(F1 N) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SUM(F1 N) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(F1 N) TO MA0 MA1
           MOVE FUNCTION MEAN(F1 N) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(F1 N) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(F1 N) TO MB0 MB1
           MOVE FUNCTION MEAN(F1 N) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(F1 N) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(F1 N) TO MC0 MC1
           MOVE FUNCTION MEAN(F1 N) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(F1 N) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(F1 N) TO MD0 MD1
           MOVE FUNCTION MEAN(F1 N) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(F1 N) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(F1 N) TO ME0 ME1
           MOVE FUNCTION MEAN(F1 N) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(F1 N) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(F1 N) TO MF0 MF1
           MOVE FUNCTION MEAN(F1 N) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(F1 N) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(F1 N) TO MG0 MG1
           MOVE FUNCTION MEAN(F1 N) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(F1 N) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION RANGE(F1 N) TO MA0 MA1
           MOVE FUNCTION RANGE(F1 N) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION RANGE(F1 N) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION RANGE(F1 N) TO MB0 MB1
           MOVE FUNCTION RANGE(F1 N) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION RANGE(F1 N) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION RANGE(F1 N) TO MC0 MC1
           MOVE FUNCTION RANGE(F1 N) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION RANGE(F1 N) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION RANGE(F1 N) TO MD0 MD1
           MOVE FUNCTION RANGE(F1 N) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION RANGE(F1 N) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION RANGE(F1 N) TO ME0 ME1
           MOVE FUNCTION RANGE(F1 N) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION RANGE(F1 N) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION RANGE(F1 N) TO MF0 MF1
           MOVE FUNCTION RANGE(F1 N) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION RANGE(F1 N) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION RANGE(F1 N) TO MG0 MG1
           MOVE FUNCTION RANGE(F1 N) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION RANGE(F1 N) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MIDRANGE(F1 N) TO MA0 MA1
           MOVE FUNCTION MIDRANGE(F1 N) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MIDRANGE(F1 N) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MIDRANGE(F1 N) TO MB0 MB1
           MOVE FUNCTION MIDRANGE(F1 N) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MIDRANGE(F1 N) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MIDRANGE(F1 N) TO MC0 MC1
           MOVE FUNCTION MIDRANGE(F1 N) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MIDRANGE(F1 N) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MIDRANGE(F1 N) TO MD0 MD1
           MOVE FUNCTION MIDRANGE(F1 N) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MIDRANGE(F1 N) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MIDRANGE(F1 N) TO ME0 ME1
           MOVE FUNCTION MIDRANGE(F1 N) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MIDRANGE(F1 N) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MIDRANGE(F1 N) TO MF0 MF1
           MOVE FUNCTION MIDRANGE(F1 N) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MIDRANGE(F1 N) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MIDRANGE(F1 N) TO MG0 MG1
           MOVE FUNCTION MIDRANGE(F1 N) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MIDRANGE(F1 N) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEDIAN(F1 N 3) TO MA0 MA1
           MOVE FUNCTION MEDIAN(F1 N 3) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEDIAN(F1 N 3) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEDIAN(F1 N 3) TO MB0 MB1
           MOVE FUNCTION MEDIAN(F1 N 3) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEDIAN(F1 N 3) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEDIAN(F1 N 3) TO MC0 MC1
           MOVE FUNCTION MEDIAN(F1 N 3) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEDIAN(F1 N 3) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEDIAN(F1 N 3) TO MD0 MD1
           MOVE FUNCTION MEDIAN(F1 N 3) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEDIAN(F1 N 3) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEDIAN(F1 N 3) TO ME0 ME1
           MOVE FUNCTION MEDIAN(F1 N 3) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEDIAN(F1 N 3) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEDIAN(F1 N 3) TO MF0 MF1
           MOVE FUNCTION MEDIAN(F1 N 3) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEDIAN(F1 N 3) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEDIAN(F1 N 3) TO MG0 MG1
           MOVE FUNCTION MEDIAN(F1 N 3) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEDIAN(F1 N 3) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(F1 N) TO MA0 MA1
           MOVE FUNCTION MAX(F1 N) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(F1 N) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(F1 N) TO MB0 MB1
           MOVE FUNCTION MAX(F1 N) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(F1 N) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(F1 N) TO MC0 MC1
           MOVE FUNCTION MAX(F1 N) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(F1 N) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(F1 N) TO MD0 MD1
           MOVE FUNCTION MAX(F1 N) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(F1 N) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(F1 N) TO ME0 ME1
           MOVE FUNCTION MAX(F1 N) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(F1 N) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(F1 N) TO MF0 MF1
           MOVE FUNCTION MAX(F1 N) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(F1 N) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(F1 N) TO MG0 MG1
           MOVE FUNCTION MAX(F1 N) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(F1 N) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MIN(F1 2) TO MA0 MA1
           MOVE FUNCTION MIN(F1 2) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MIN(F1 2) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MIN(F1 2) TO MB0 MB1
           MOVE FUNCTION MIN(F1 2) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MIN(F1 2) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MIN(F1 2) TO MC0 MC1
           MOVE FUNCTION MIN(F1 2) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MIN(F1 2) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MIN(F1 2) TO MD0 MD1
           MOVE FUNCTION MIN(F1 2) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MIN(F1 2) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MIN(F1 2) TO ME0 ME1
           MOVE FUNCTION MIN(F1 2) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MIN(F1 2) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MIN(F1 2) TO MF0 MF1
           MOVE FUNCTION MIN(F1 2) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MIN(F1 2) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MIN(F1 2) TO MG0 MG1
           MOVE FUNCTION MIN(F1 2) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MIN(F1 2) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(F1) TO MA0 MA1
           MOVE FUNCTION SQRT(F1) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(F1) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(F1) TO MB0 MB1
           MOVE FUNCTION SQRT(F1) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(F1) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(F1) TO MC0 MC1
           MOVE FUNCTION SQRT(F1) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(F1) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(F1) TO MD0 MD1
           MOVE FUNCTION SQRT(F1) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(F1) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(F1) TO ME0 ME1
           MOVE FUNCTION SQRT(F1) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(F1) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(F1) TO MF0 MF1
           MOVE FUNCTION SQRT(F1) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(F1) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(F1) TO MG0 MG1
           MOVE FUNCTION SQRT(F1) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(F1) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(F1) TO MA0 MA1
           MOVE FUNCTION SIN(F1) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(F1) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(F1) TO MB0 MB1
           MOVE FUNCTION SIN(F1) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(F1) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(F1) TO MC0 MC1
           MOVE FUNCTION SIN(F1) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(F1) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(F1) TO MD0 MD1
           MOVE FUNCTION SIN(F1) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(F1) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(F1) TO ME0 ME1
           MOVE FUNCTION SIN(F1) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(F1) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(F1) TO MF0 MF1
           MOVE FUNCTION SIN(F1) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(F1) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(F1) TO MG0 MG1
           MOVE FUNCTION SIN(F1) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(F1) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(G1) TO MA0 MA1
           MOVE FUNCTION SQRT(G1) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(G1) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(G1) TO MB0 MB1
           MOVE FUNCTION SQRT(G1) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(G1) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(G1) TO MC0 MC1
           MOVE FUNCTION SQRT(G1) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(G1) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(G1) TO MD0 MD1
           MOVE FUNCTION SQRT(G1) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(G1) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(G1) TO ME0 ME1
           MOVE FUNCTION SQRT(G1) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(G1) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(G1) TO MF0 MF1
           MOVE FUNCTION SQRT(G1) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(G1) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(G1) TO MG0 MG1
           MOVE FUNCTION SQRT(G1) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(G1) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(F1 N 3) TO MA0 MA1
           MOVE FUNCTION VARIANCE(F1 N 3) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(F1 N 3) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(F1 N 3) TO MB0 MB1
           MOVE FUNCTION VARIANCE(F1 N 3) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(F1 N 3) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(F1 N 3) TO MC0 MC1
           MOVE FUNCTION VARIANCE(F1 N 3) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(F1 N 3) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(F1 N 3) TO MD0 MD1
           MOVE FUNCTION VARIANCE(F1 N 3) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(F1 N 3) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(F1 N 3) TO ME0 ME1
           MOVE FUNCTION VARIANCE(F1 N 3) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(F1 N 3) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(F1 N 3) TO MF0 MF1
           MOVE FUNCTION VARIANCE(F1 N 3) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(F1 N 3) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(F1 N 3) TO MG0 MG1
           MOVE FUNCTION VARIANCE(F1 N 3) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(F1 N 3) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(1 2 F1) TO MA0 MA1
           MOVE FUNCTION MEAN(1 2 F1) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(1 2 F1) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(1 2 F1) TO MB0 MB1
           MOVE FUNCTION MEAN(1 2 F1) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(1 2 F1) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(1 2 F1) TO MC0 MC1
           MOVE FUNCTION MEAN(1 2 F1) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(1 2 F1) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(1 2 F1) TO MD0 MD1
           MOVE FUNCTION MEAN(1 2 F1) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(1 2 F1) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(1 2 F1) TO ME0 ME1
           MOVE FUNCTION MEAN(1 2 F1) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(1 2 F1) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(1 2 F1) TO MF0 MF1
           MOVE FUNCTION MEAN(1 2 F1) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(1 2 F1) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(1 2 F1) TO MG0 MG1
           MOVE FUNCTION MEAN(1 2 F1) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(1 2 F1) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO MA0 MA1
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO MB0 MB1
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO MC0 MC1
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO MD0 MD1
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO ME0 ME1
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO MF0 MF1
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO MG0 MG1
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO MA0
               MA1
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO MB0
               MB1
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO MC0
               MC1
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO MD0
               MD1
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO ME0
               ME1
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO MF0
               MF1
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO MG0
               MG1
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION STANDARD-DEVIATION(0.1E0 0.2E0 0.4E0) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ANNUITY(0.1E0 3) TO MA0 MA1
           MOVE FUNCTION ANNUITY(0.1E0 3) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ANNUITY(0.1E0 3) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ANNUITY(0.1E0 3) TO MB0 MB1
           MOVE FUNCTION ANNUITY(0.1E0 3) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ANNUITY(0.1E0 3) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ANNUITY(0.1E0 3) TO MC0 MC1
           MOVE FUNCTION ANNUITY(0.1E0 3) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ANNUITY(0.1E0 3) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ANNUITY(0.1E0 3) TO MD0 MD1
           MOVE FUNCTION ANNUITY(0.1E0 3) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ANNUITY(0.1E0 3) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ANNUITY(0.1E0 3) TO ME0 ME1
           MOVE FUNCTION ANNUITY(0.1E0 3) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ANNUITY(0.1E0 3) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ANNUITY(0.1E0 3) TO MF0 MF1
           MOVE FUNCTION ANNUITY(0.1E0 3) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ANNUITY(0.1E0 3) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ANNUITY(0.1E0 3) TO MG0 MG1
           MOVE FUNCTION ANNUITY(0.1E0 3) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ANNUITY(0.1E0 3) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO MA0 MA1
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO MB0 MB1
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO MC0 MC1
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO MD0 MD1
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO ME0 ME1
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO MF0 MF1
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO MG0 MG1
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION PRESENT-VALUE(0.1E0 1 2) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO MA0 MA1
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO MB0 MB1
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO MC0 MC1
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO MD0 MD1
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO ME0 ME1
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO MF0 MF1
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO MG0 MG1
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SUM(0.1E0 0.2E0) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO MA0 MA1
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO MB0 MB1
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO MC0 MC1
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO MD0 MD1
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO ME0 ME1
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO MF0 MF1
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO MG0 MG1
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(0.1E0 0.2E0) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(0.1E0 N) TO MA0 MA1
           MOVE FUNCTION MAX(0.1E0 N) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(0.1E0 N) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(0.1E0 N) TO MB0 MB1
           MOVE FUNCTION MAX(0.1E0 N) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(0.1E0 N) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(0.1E0 N) TO MC0 MC1
           MOVE FUNCTION MAX(0.1E0 N) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(0.1E0 N) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(0.1E0 N) TO MD0 MD1
           MOVE FUNCTION MAX(0.1E0 N) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(0.1E0 N) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(0.1E0 N) TO ME0 ME1
           MOVE FUNCTION MAX(0.1E0 N) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(0.1E0 N) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(0.1E0 N) TO MF0 MF1
           MOVE FUNCTION MAX(0.1E0 N) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(0.1E0 N) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(0.1E0 N) TO MG0 MG1
           MOVE FUNCTION MAX(0.1E0 N) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(0.1E0 N) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(0.1E0 F1) TO MA0 MA1
           MOVE FUNCTION MAX(0.1E0 F1) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(0.1E0 F1) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(0.1E0 F1) TO MB0 MB1
           MOVE FUNCTION MAX(0.1E0 F1) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(0.1E0 F1) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(0.1E0 F1) TO MC0 MC1
           MOVE FUNCTION MAX(0.1E0 F1) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(0.1E0 F1) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(0.1E0 F1) TO MD0 MD1
           MOVE FUNCTION MAX(0.1E0 F1) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(0.1E0 F1) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(0.1E0 F1) TO ME0 ME1
           MOVE FUNCTION MAX(0.1E0 F1) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(0.1E0 F1) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(0.1E0 F1) TO MF0 MF1
           MOVE FUNCTION MAX(0.1E0 F1) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(0.1E0 F1) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(0.1E0 F1) TO MG0 MG1
           MOVE FUNCTION MAX(0.1E0 F1) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(0.1E0 F1) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(-1.5E0) TO MA0 MA1
           MOVE FUNCTION ABS(-1.5E0) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(-1.5E0) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(-1.5E0) TO MB0 MB1
           MOVE FUNCTION ABS(-1.5E0) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(-1.5E0) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(-1.5E0) TO MC0 MC1
           MOVE FUNCTION ABS(-1.5E0) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(-1.5E0) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(-1.5E0) TO MD0 MD1
           MOVE FUNCTION ABS(-1.5E0) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(-1.5E0) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(-1.5E0) TO ME0 ME1
           MOVE FUNCTION ABS(-1.5E0) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(-1.5E0) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(-1.5E0) TO MF0 MF1
           MOVE FUNCTION ABS(-1.5E0) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(-1.5E0) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(-1.5E0) TO MG0 MG1
           MOVE FUNCTION ABS(-1.5E0) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(-1.5E0) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2.0E0) TO MA0 MA1
           MOVE FUNCTION SQRT(2.0E0) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2.0E0) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2.0E0) TO MB0 MB1
           MOVE FUNCTION SQRT(2.0E0) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2.0E0) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2.0E0) TO MC0 MC1
           MOVE FUNCTION SQRT(2.0E0) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2.0E0) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2.0E0) TO MD0 MD1
           MOVE FUNCTION SQRT(2.0E0) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2.0E0) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2.0E0) TO ME0 ME1
           MOVE FUNCTION SQRT(2.0E0) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2.0E0) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2.0E0) TO MF0 MF1
           MOVE FUNCTION SQRT(2.0E0) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2.0E0) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2.0E0) TO MG0 MG1
           MOVE FUNCTION SQRT(2.0E0) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2.0E0) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(1.0E0) TO MA0 MA1
           MOVE FUNCTION SIN(1.0E0) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(1.0E0) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(1.0E0) TO MB0 MB1
           MOVE FUNCTION SIN(1.0E0) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(1.0E0) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(1.0E0) TO MC0 MC1
           MOVE FUNCTION SIN(1.0E0) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(1.0E0) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(1.0E0) TO MD0 MD1
           MOVE FUNCTION SIN(1.0E0) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(1.0E0) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(1.0E0) TO ME0 ME1
           MOVE FUNCTION SIN(1.0E0) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(1.0E0) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(1.0E0) TO MF0 MF1
           MOVE FUNCTION SIN(1.0E0) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(1.0E0) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(1.0E0) TO MG0 MG1
           MOVE FUNCTION SIN(1.0E0) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(1.0E0) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION EXP(1.0E0) TO MA0 MA1
           MOVE FUNCTION EXP(1.0E0) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION EXP(1.0E0) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION EXP(1.0E0) TO MB0 MB1
           MOVE FUNCTION EXP(1.0E0) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION EXP(1.0E0) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION EXP(1.0E0) TO MC0 MC1
           MOVE FUNCTION EXP(1.0E0) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION EXP(1.0E0) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION EXP(1.0E0) TO MD0 MD1
           MOVE FUNCTION EXP(1.0E0) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION EXP(1.0E0) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION EXP(1.0E0) TO ME0 ME1
           MOVE FUNCTION EXP(1.0E0) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION EXP(1.0E0) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION EXP(1.0E0) TO MF0 MF1
           MOVE FUNCTION EXP(1.0E0) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION EXP(1.0E0) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION EXP(1.0E0) TO MG0 MG1
           MOVE FUNCTION EXP(1.0E0) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION EXP(1.0E0) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION LOG(0.5E0) TO MA0 MA1
           MOVE FUNCTION LOG(0.5E0) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION LOG(0.5E0) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION LOG(0.5E0) TO MB0 MB1
           MOVE FUNCTION LOG(0.5E0) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION LOG(0.5E0) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION LOG(0.5E0) TO MC0 MC1
           MOVE FUNCTION LOG(0.5E0) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION LOG(0.5E0) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION LOG(0.5E0) TO MD0 MD1
           MOVE FUNCTION LOG(0.5E0) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION LOG(0.5E0) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION LOG(0.5E0) TO ME0 ME1
           MOVE FUNCTION LOG(0.5E0) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION LOG(0.5E0) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION LOG(0.5E0) TO MF0 MF1
           MOVE FUNCTION LOG(0.5E0) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION LOG(0.5E0) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION LOG(0.5E0) TO MG0 MG1
           MOVE FUNCTION LOG(0.5E0) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION LOG(0.5E0) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO MA0 MA1
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO MB0 MB1
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO MC0 MC1
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO MD0 MD1
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO ME0 ME1
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO MF0 MF1
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO MG0 MG1
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(FUNCTION SQRT(2)) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO MA0 MA1
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO MB0 MB1
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO MC0 MC1
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO MD0 MD1
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO ME0 ME1
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO MF0 MF1
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO MG0 MG1
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(FUNCTION SQRT(2)) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO MA0 MA1
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO MB0 MB1
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO MC0 MC1
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO MD0 MD1
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO ME0 ME1
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO MF0 MF1
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO MG0 MG1
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MAX(FUNCTION SQRT(2) N) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F1 + 1) TO MA0 MA1
           MOVE FUNCTION ABS(F1 + 1) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F1 + 1) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F1 + 1) TO MB0 MB1
           MOVE FUNCTION ABS(F1 + 1) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F1 + 1) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F1 + 1) TO MC0 MC1
           MOVE FUNCTION ABS(F1 + 1) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F1 + 1) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F1 + 1) TO MD0 MD1
           MOVE FUNCTION ABS(F1 + 1) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F1 + 1) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F1 + 1) TO ME0 ME1
           MOVE FUNCTION ABS(F1 + 1) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F1 + 1) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F1 + 1) TO MF0 MF1
           MOVE FUNCTION ABS(F1 + 1) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F1 + 1) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(F1 + 1) TO MG0 MG1
           MOVE FUNCTION ABS(F1 + 1) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(F1 + 1) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(N * 2) TO MA0 MA1
           MOVE FUNCTION SQRT(N * 2) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(N * 2) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(N * 2) TO MB0 MB1
           MOVE FUNCTION SQRT(N * 2) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(N * 2) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(N * 2) TO MC0 MC1
           MOVE FUNCTION SQRT(N * 2) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(N * 2) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(N * 2) TO MD0 MD1
           MOVE FUNCTION SQRT(N * 2) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(N * 2) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(N * 2) TO ME0 ME1
           MOVE FUNCTION SQRT(N * 2) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(N * 2) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(N * 2) TO MF0 MF1
           MOVE FUNCTION SQRT(N * 2) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(N * 2) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(N * 2) TO MG0 MG1
           MOVE FUNCTION SQRT(N * 2) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(N * 2) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(N + 0.5E0) TO MA0 MA1
           MOVE FUNCTION SQRT(N + 0.5E0) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(N + 0.5E0) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(N + 0.5E0) TO MB0 MB1
           MOVE FUNCTION SQRT(N + 0.5E0) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(N + 0.5E0) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(N + 0.5E0) TO MC0 MC1
           MOVE FUNCTION SQRT(N + 0.5E0) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(N + 0.5E0) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(N + 0.5E0) TO MD0 MD1
           MOVE FUNCTION SQRT(N + 0.5E0) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(N + 0.5E0) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(N + 0.5E0) TO ME0 ME1
           MOVE FUNCTION SQRT(N + 0.5E0) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(N + 0.5E0) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(N + 0.5E0) TO MF0 MF1
           MOVE FUNCTION SQRT(N + 0.5E0) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(N + 0.5E0) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(N + 0.5E0) TO MG0 MG1
           MOVE FUNCTION SQRT(N + 0.5E0) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(N + 0.5E0) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(N ** 2) TO MA0 MA1
           MOVE FUNCTION ABS(N ** 2) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(N ** 2) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(N ** 2) TO MB0 MB1
           MOVE FUNCTION ABS(N ** 2) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(N ** 2) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(N ** 2) TO MC0 MC1
           MOVE FUNCTION ABS(N ** 2) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(N ** 2) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(N ** 2) TO MD0 MD1
           MOVE FUNCTION ABS(N ** 2) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(N ** 2) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(N ** 2) TO ME0 ME1
           MOVE FUNCTION ABS(N ** 2) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(N ** 2) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(N ** 2) TO MF0 MF1
           MOVE FUNCTION ABS(N ** 2) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(N ** 2) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(N ** 2) TO MG0 MG1
           MOVE FUNCTION ABS(N ** 2) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(N ** 2) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2 ** 3) TO MA0 MA1
           MOVE FUNCTION SQRT(2 ** 3) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2 ** 3) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2 ** 3) TO MB0 MB1
           MOVE FUNCTION SQRT(2 ** 3) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2 ** 3) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2 ** 3) TO MC0 MC1
           MOVE FUNCTION SQRT(2 ** 3) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2 ** 3) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2 ** 3) TO MD0 MD1
           MOVE FUNCTION SQRT(2 ** 3) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2 ** 3) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2 ** 3) TO ME0 ME1
           MOVE FUNCTION SQRT(2 ** 3) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2 ** 3) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2 ** 3) TO MF0 MF1
           MOVE FUNCTION SQRT(2 ** 3) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2 ** 3) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2 ** 3) TO MG0 MG1
           MOVE FUNCTION SQRT(2 ** 3) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2 ** 3) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(N / 3) TO MA0 MA1
           MOVE FUNCTION SIN(N / 3) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(N / 3) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(N / 3) TO MB0 MB1
           MOVE FUNCTION SIN(N / 3) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(N / 3) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(N / 3) TO MC0 MC1
           MOVE FUNCTION SIN(N / 3) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(N / 3) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(N / 3) TO MD0 MD1
           MOVE FUNCTION SIN(N / 3) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(N / 3) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(N / 3) TO ME0 ME1
           MOVE FUNCTION SIN(N / 3) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(N / 3) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(N / 3) TO MF0 MF1
           MOVE FUNCTION SIN(N / 3) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(N / 3) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIN(N / 3) TO MG0 MG1
           MOVE FUNCTION SIN(N / 3) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIN(N / 3) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION PI TO MA0 MA1
           MOVE FUNCTION PI TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION PI TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION PI TO MB0 MB1
           MOVE FUNCTION PI TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION PI TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION PI TO MC0 MC1
           MOVE FUNCTION PI TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION PI TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION PI TO MD0 MD1
           MOVE FUNCTION PI TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION PI TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION PI TO ME0 ME1
           MOVE FUNCTION PI TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION PI TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION PI TO MF0 MF1
           MOVE FUNCTION PI TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION PI TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION PI TO MG0 MG1
           MOVE FUNCTION PI TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION PI TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION E TO MA0 MA1
           MOVE FUNCTION E TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION E TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION E TO MB0 MB1
           MOVE FUNCTION E TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION E TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION E TO MC0 MC1
           MOVE FUNCTION E TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION E TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION E TO MD0 MD1
           MOVE FUNCTION E TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION E TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION E TO ME0 ME1
           MOVE FUNCTION E TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION E TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION E TO MF0 MF1
           MOVE FUNCTION E TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION E TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION E TO MG0 MG1
           MOVE FUNCTION E TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION E TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2) TO MA0 MA1
           MOVE FUNCTION SQRT(2) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2) TO MB0 MB1
           MOVE FUNCTION SQRT(2) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2) TO MC0 MC1
           MOVE FUNCTION SQRT(2) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2) TO MD0 MD1
           MOVE FUNCTION SQRT(2) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2) TO ME0 ME1
           MOVE FUNCTION SQRT(2) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2) TO MF0 MF1
           MOVE FUNCTION SQRT(2) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SQRT(2) TO MG0 MG1
           MOVE FUNCTION SQRT(2) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SQRT(2) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION RANDOM(3) TO MA0 MA1
           MOVE FUNCTION RANDOM(3) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION RANDOM(3) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION RANDOM(3) TO MB0 MB1
           MOVE FUNCTION RANDOM(3) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION RANDOM(3) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION RANDOM(3) TO MC0 MC1
           MOVE FUNCTION RANDOM(3) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION RANDOM(3) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION RANDOM(3) TO MD0 MD1
           MOVE FUNCTION RANDOM(3) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION RANDOM(3) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION RANDOM(3) TO ME0 ME1
           MOVE FUNCTION RANDOM(3) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION RANDOM(3) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION RANDOM(3) TO MF0 MF1
           MOVE FUNCTION RANDOM(3) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION RANDOM(3) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION RANDOM(3) TO MG0 MG1
           MOVE FUNCTION RANDOM(3) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION RANDOM(3) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO MA0 MA1
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO MB0 MB1
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO MC0 MC1
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO MD0 MD1
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO ME0 ME1
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO MF0 MF1
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO MG0 MG1
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION NUMVAL("1.23456789012345678") TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(1 2 4 8) TO MA0 MA1
           MOVE FUNCTION VARIANCE(1 2 4 8) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(1 2 4 8) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(1 2 4 8) TO MB0 MB1
           MOVE FUNCTION VARIANCE(1 2 4 8) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(1 2 4 8) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(1 2 4 8) TO MC0 MC1
           MOVE FUNCTION VARIANCE(1 2 4 8) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(1 2 4 8) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(1 2 4 8) TO MD0 MD1
           MOVE FUNCTION VARIANCE(1 2 4 8) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(1 2 4 8) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(1 2 4 8) TO ME0 ME1
           MOVE FUNCTION VARIANCE(1 2 4 8) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(1 2 4 8) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(1 2 4 8) TO MF0 MF1
           MOVE FUNCTION VARIANCE(1 2 4 8) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(1 2 4 8) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION VARIANCE(1 2 4 8) TO MG0 MG1
           MOVE FUNCTION VARIANCE(1 2 4 8) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION VARIANCE(1 2 4 8) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ANNUITY(0.05 7) TO MA0 MA1
           MOVE FUNCTION ANNUITY(0.05 7) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ANNUITY(0.05 7) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ANNUITY(0.05 7) TO MB0 MB1
           MOVE FUNCTION ANNUITY(0.05 7) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ANNUITY(0.05 7) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ANNUITY(0.05 7) TO MC0 MC1
           MOVE FUNCTION ANNUITY(0.05 7) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ANNUITY(0.05 7) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ANNUITY(0.05 7) TO MD0 MD1
           MOVE FUNCTION ANNUITY(0.05 7) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ANNUITY(0.05 7) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ANNUITY(0.05 7) TO ME0 ME1
           MOVE FUNCTION ANNUITY(0.05 7) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ANNUITY(0.05 7) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ANNUITY(0.05 7) TO MF0 MF1
           MOVE FUNCTION ANNUITY(0.05 7) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ANNUITY(0.05 7) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ANNUITY(0.05 7) TO MG0 MG1
           MOVE FUNCTION ANNUITY(0.05 7) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ANNUITY(0.05 7) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(-3.14159265358979) TO MA0 MA1
           MOVE FUNCTION ABS(-3.14159265358979) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(-3.14159265358979) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(-3.14159265358979) TO MB0 MB1
           MOVE FUNCTION ABS(-3.14159265358979) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(-3.14159265358979) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(-3.14159265358979) TO MC0 MC1
           MOVE FUNCTION ABS(-3.14159265358979) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(-3.14159265358979) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(-3.14159265358979) TO MD0 MD1
           MOVE FUNCTION ABS(-3.14159265358979) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(-3.14159265358979) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(-3.14159265358979) TO ME0 ME1
           MOVE FUNCTION ABS(-3.14159265358979) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(-3.14159265358979) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(-3.14159265358979) TO MF0 MF1
           MOVE FUNCTION ABS(-3.14159265358979) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(-3.14159265358979) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION ABS(-3.14159265358979) TO MG0 MG1
           MOVE FUNCTION ABS(-3.14159265358979) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION ABS(-3.14159265358979) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION FACTORIAL(5) TO MA0 MA1
           MOVE FUNCTION FACTORIAL(5) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION FACTORIAL(5) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION FACTORIAL(5) TO MB0 MB1
           MOVE FUNCTION FACTORIAL(5) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION FACTORIAL(5) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION FACTORIAL(5) TO MC0 MC1
           MOVE FUNCTION FACTORIAL(5) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION FACTORIAL(5) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION FACTORIAL(5) TO MD0 MD1
           MOVE FUNCTION FACTORIAL(5) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION FACTORIAL(5) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION FACTORIAL(5) TO ME0 ME1
           MOVE FUNCTION FACTORIAL(5) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION FACTORIAL(5) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION FACTORIAL(5) TO MF0 MF1
           MOVE FUNCTION FACTORIAL(5) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION FACTORIAL(5) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION FACTORIAL(5) TO MG0 MG1
           MOVE FUNCTION FACTORIAL(5) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION FACTORIAL(5) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIGN(F1) TO MA0 MA1
           MOVE FUNCTION SIGN(F1) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIGN(F1) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIGN(F1) TO MB0 MB1
           MOVE FUNCTION SIGN(F1) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIGN(F1) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIGN(F1) TO MC0 MC1
           MOVE FUNCTION SIGN(F1) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIGN(F1) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIGN(F1) TO MD0 MD1
           MOVE FUNCTION SIGN(F1) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIGN(F1) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIGN(F1) TO ME0 ME1
           MOVE FUNCTION SIGN(F1) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIGN(F1) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIGN(F1) TO MF0 MF1
           MOVE FUNCTION SIGN(F1) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIGN(F1) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION SIGN(F1) TO MG0 MG1
           MOVE FUNCTION SIGN(F1) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION SIGN(F1) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION INTEGER(F1) TO MA0 MA1
           MOVE FUNCTION INTEGER(F1) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION INTEGER(F1) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION INTEGER(F1) TO MB0 MB1
           MOVE FUNCTION INTEGER(F1) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION INTEGER(F1) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION INTEGER(F1) TO MC0 MC1
           MOVE FUNCTION INTEGER(F1) TO SC0
           IF MC0 NOT = SC0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION INTEGER(F1) TO SC1
           IF MC1 NOT = SC1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION INTEGER(F1) TO MD0 MD1
           MOVE FUNCTION INTEGER(F1) TO SD0
           IF MD0 NOT = SD0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION INTEGER(F1) TO SD1
           IF MD1 NOT = SD1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION INTEGER(F1) TO ME0 ME1
           MOVE FUNCTION INTEGER(F1) TO SE0
           IF ME0 NOT = SE0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION INTEGER(F1) TO SE1
           IF ME1 NOT = SE1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION INTEGER(F1) TO MF0 MF1
           MOVE FUNCTION INTEGER(F1) TO SF0
           IF MF0 NOT = SF0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION INTEGER(F1) TO SF1
           IF MF1 NOT = SF1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION INTEGER(F1) TO MG0 MG1
           MOVE FUNCTION INTEGER(F1) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION INTEGER(F1) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(1 2 2) TO MA0 MA1
           MOVE FUNCTION MEAN(1 2 2) TO SA0
           IF MA0 NOT = SA0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(1 2 2) TO SA1
           IF MA1 NOT = SA1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(1 2 2) TO MB0 MB1
           MOVE FUNCTION MEAN(1 2 2) TO SB0
           IF MB0 NOT = SB0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(1 2 2) TO SB1
           IF MB1 NOT = SB1
               ADD 1 TO BAD
           END-IF
           ADD 1 TO CASES
           MOVE FUNCTION MEAN(1 2 2) TO MG0 MG1
           MOVE FUNCTION MEAN(1 2 2) TO SG0
           IF MG0 NOT = SG0
               ADD 1 TO BAD
           END-IF
           MOVE FUNCTION MEAN(1 2 2) TO SG1
           IF MG1 NOT = SG1
               ADD 1 TO BAD
           END-IF
           DISPLAY "CASES=" CASES " BAD=" BAD
           MOVE FUNCTION VARIANCE(0.1E0 0.2E0 0.4E0) TO VQ1 VQ2
           DISPLAY "VAR=" VQ1 " " VQ2
           MOVE FUNCTION ANNUITY(0.1E0 3) TO VQ1 VQ2
           DISPLAY "ANN=" VQ1 " " VQ2
           MOVE FUNCTION ABS(F9) TO FA N1
           IF FA = F9 DISPLAY "BIG=SAME" ELSE DISPLAY "BIG=DIFFERENT".
           MOVE FUNCTION ABS(F2) TO FB N1
           IF FB = F2 DISPLAY "TINY=SAME" ELSE DISPLAY "TINY=DIFFERENT".
           STOP RUN.
