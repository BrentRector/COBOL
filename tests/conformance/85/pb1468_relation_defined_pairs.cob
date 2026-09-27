      *> kb/Work PB1468 - the pairs ISO 8.8.4.2.1 "Comparisons are defined for the following:" DOES define, each
      *> evaluated by the rule it names. Item 6 with 8.8.4.2.5: "The numeric integer operand shall be an integer
      *> literal or an integer numeric data item of usage display or national" - the integer is moved to an item of
      *> the other operand's class ("of the same length in terms of character positions as the number of digits in
      *> the integer"), so ND 12 PIC 9(4) is "0012" and a signed NS -12 is "0012" (14.9.25.4 GR6a drops the sign).
      *> Item 1: numeric operands compare algebraically "regardless of the manner in which their usage is described"
      *> (8.8.4.2.4). Item 8 as 8.8.4.2.13 states it: two index-names compare occurrence numbers (row 1), an
      *> index-name against a numeric item or literal compares its occurrence number (row 2), and an index data item
      *> against an index-name compares "the actual values ... without conversion" (row 3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1468-DEFINED-PAIRS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 E1 PIC X OCCURS 5 INDEXED BY I1 I2.
       01 XA PIC X(4) VALUE "0012".
       01 XE PIC ZZ9 VALUE " 12".
       01 ND PIC 9(4) VALUE 12.
       01 NS PIC S9(4) VALUE -12.
       01 NB PIC 9(4) COMP VALUE 12.
       01 IDA USAGE INDEX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET I1 TO 3.
           SET I2 TO 3.
           SET IDA TO I1.
           IF ND = XA DISPLAY "01 ND = XA   EQ" ELSE DISPLAY "01 ND = XA   NE".
           IF NS = XA DISPLAY "02 NS = XA   EQ" ELSE DISPLAY "02 NS = XA   NE".
           IF XE < ND DISPLAY "03 XE < ND   LT" ELSE DISPLAY "03 XE < ND   GE".
           IF 12 = XA DISPLAY "04 12 = XA   EQ" ELSE DISPLAY "04 12 = XA   NE".
           IF ND = SPACE DISPLAY "05 ND = SP   EQ" ELSE DISPLAY "05 ND = SP   NE".
           IF NB = ND DISPLAY "06 NB = ND   EQ" ELSE DISPLAY "06 NB = ND   NE".
           IF NS < NB DISPLAY "07 NS < NB   LT" ELSE DISPLAY "07 NS < NB   GE".
           IF I1 = I2 DISPLAY "08 I1 = I2   EQ" ELSE DISPLAY "08 I1 = I2   NE".
           IF I1 = 3 DISPLAY "09 I1 = 3    EQ" ELSE DISPLAY "09 I1 = 3    NE".
           IF I1 < NB DISPLAY "10 I1 < NB   LT" ELSE DISPLAY "10 I1 < NB   GE".
           IF IDA = I1 DISPLAY "11 IDA = I1  EQ" ELSE DISPLAY "11 IDA = I1  NE".
           SET I2 UP BY 1.
           IF IDA = I2 DISPLAY "12 IDA = I2  EQ" ELSE DISPLAY "12 IDA = I2  NE".
           STOP RUN.
