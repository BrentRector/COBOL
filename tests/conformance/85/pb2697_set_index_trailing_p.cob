      *> kb/Work PB2697 - an index moved or set by a TRAILING-P amount moves by the amount's VALUE.
      *> ISO 13.18.40.4 GR14: "The symbol 'P' specifies the location of an assumed decimal point when
      *> that point is not within the number that appears in the data item." N100 (PIC 9PP) stores
      *> the digit 1 and its value is 100; N20 (PIC 9P) stores 2 and its value is 20.
      *> 14.9.39.4 GR4 a): the index is "incremented (UP BY) or decremented (DOWN BY) by the result
      *> of the evaluation of arithmetic-expression-2" - the VALUE, not the stored digits. The runtime
      *> used to hand back the stored digits for every scale <= 0, so UP BY N100 moved the index by 1.
      *> SET index-name UP/DOWN BY identifier and SET index-name TO identifier are 1985 formats, and a
      *> P picture is a 1985 picture, so 85 is the introducing edition; the rule is unchanged since.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES AND NOT FROM A RUN:
      *> UP=0101    1 + 100.
      *> DOWN=0081  101 - 20.
      *> TO=0100    Format 1 sets the index to the occurrence number the value 100 denotes.
      *> EL=0007    EL (IX) is occurrence 100, the one EL (100) names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2697IX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 EL PIC 9(3) OCCURS 300 INDEXED BY IX.
       01 N100 PIC 9PP VALUE 100.
       01 N20  PIC 9P  VALUE 20.
       01 W    PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           SET IX TO 1.
           SET IX UP BY N100.
           SET W TO IX.
           DISPLAY "UP=" W.
           SET IX DOWN BY N20.
           SET W TO IX.
           DISPLAY "DOWN=" W.
           SET IX TO N100.
           SET W TO IX.
           DISPLAY "TO=" W.
           MOVE 7 TO EL (IX).
           MOVE EL (100) TO W.
           DISPLAY "EL=" W.
           STOP RUN.
