*>--- conformance-case
*> id: pb172_numeric_function_window_operand
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB172 - the OTHER half of the regression floor: widening the
      *> 8.8.1.1 intrinsic screen to the index-name window contexts must not
      *> disturb a NUMERIC or INTEGER function there. ISO 15.2 items 4 and 5 make
      *> these "of the class and category numeric", so every window site below
      *> stays legal while its alphanumeric counterpart (the pb172-* negative
      *> fixtures) is now rejected at the same site.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB172NUMFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V  PIC 9(4) VALUE 0.
       01 R  PIC X.
       01 T.
          05 E PIC X OCCURS 4 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABCD" TO T
           SET IX TO FUNCTION INTEGER(3)
           MOVE E(IX) TO R
           DISPLAY "SETTO=" R
           SET IX UP BY FUNCTION INTEGER(1)
           MOVE E(IX) TO R
           DISPLAY "SETUP=" R
           MOVE E(FUNCTION INTEGER(2)) TO R
           DISPLAY "SUB=" R
           PERFORM VARYING V FROM FUNCTION INTEGER(1)
                   BY FUNCTION INTEGER(1) UNTIL V > 2
               DISPLAY "V=" V
           END-PERFORM
           STOP RUN.
