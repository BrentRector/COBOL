      *> ISO/IEC 1989:2023 8.8.4.4.4 GR3 n) 1. (kb/Work PB1466, national arm): an item of category numeric is NUMERIC
      *> only when its content "consists entirely of a valid representation for the usage", and a USAGE NATIONAL
      *> numeric item's valid representation is the national digits. Content reaches such an item through a
      *> character channel - a group MOVE "without consideration for the individual elementary or group items"
      *> (14.9.25.4 GR4) - so the item stores its storage image and the class test reads THAT.
      *> WHY EACH LINE CAN FAIL (a decoded native carrier folds the test to TRUE):
      *>   N1-F   N"AB1" moved to a national group: the leaf holds letters, not digits.
      *>   N2-T   N"123": national digits.
      *>   X[AB] N[042] S[007-] P[005]  a group MOVE across alphanumeric, national numeric, signed national numeric and
      *>          packed leaves arrives whole - no leaf is displaced or lost.
      *>   N3-T   the moved national numeric content is numeric.
      *>   N4-F   MOVE SPACES to the alphanumeric group that holds the leaf: spaces are not national digits.
      *>   A[008] T[00008]  the promoted leaf still takes part in arithmetic and in a MOVE to a numeric item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1466NATNUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1 GROUP-USAGE NATIONAL.
          05 G1N PIC 9(3) USAGE NATIONAL.
       01 G4.
          05 G4X PIC X(2) VALUE "AB".
          05 G4N PIC 9(3) USAGE NATIONAL VALUE 42.
          05 G4S PIC S9(3) USAGE NATIONAL SIGN IS TRAILING SEPARATE
                VALUE -7.
          05 G4P PIC 9(3) PACKED-DECIMAL VALUE 5.
       01 G5.
          05 G5X PIC X(2).
          05 G5N PIC 9(3) USAGE NATIONAL.
          05 G5S PIC S9(3) USAGE NATIONAL SIGN IS TRAILING SEPARATE.
          05 G5P PIC 9(3) PACKED-DECIMAL.
       01 TOT PIC 9(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE N"AB1" TO G1
           IF G1N IS NUMERIC DISPLAY "N1-T" ELSE DISPLAY "N1-F" END-IF
           MOVE N"123" TO G1
           IF G1N IS NUMERIC DISPLAY "N2-T" ELSE DISPLAY "N2-F" END-IF
           MOVE G4 TO G5
           DISPLAY "X[" G5X "] N[" G5N "] S[" G5S "] P[" G5P "]"
           IF G5N IS NUMERIC DISPLAY "N3-T" ELSE DISPLAY "N3-F" END-IF
           MOVE SPACES TO G4
           IF G4N IS NUMERIC DISPLAY "N4-T" ELSE DISPLAY "N4-F" END-IF
           MOVE 7 TO G5N
           ADD 1 TO G5N
           DISPLAY "A[" G5N "]"
           MOVE G5N TO TOT
           DISPLAY "T[" TOT "]"
           STOP RUN.
