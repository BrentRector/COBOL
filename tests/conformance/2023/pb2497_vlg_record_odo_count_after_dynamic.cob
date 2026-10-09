      *> kb/Work PB2497 (train 1045 review) - A RECORD WHOSE OCCURS
      *> DEPENDING TABLE OF VARIABLE-LENGTH ELEMENTS FOLLOWS A
      *> DYNAMIC-LENGTH ITEM AND ITS DATA-NAME-1 (determination
      *> D-FRA (viii), docs/CONFORMANCE.md section 3).
      *>
      *> A LINE SEQUENTIAL record carries no extent table, so a READ
      *> takes the count from data-name-1 K. 13.18.38.3 SR20 puts K
      *> before the table but NOT before the dynamic-length item P, so
      *> where K sits depends on how much of the record P holds, which
      *> depends on the count. Record 1 is "7a0": P = "7a", K = 0, no
      *> occurrence. Decomposed at the maximum (fixed run K + three X,
      *> 4 positions) P gets nothing and K reads "7"; that count does
      *> not read back, so the smallest count that does is taken: at 0
      *> the fixed run is K alone, P takes the 2 positions beyond it
      *> (D-FRA (ii)'s take step, exact with one variable-length
      *> member) and K reads 0. Expected P=[7a] K=0; it used to read
      *> P=[] K=7. Record 2 is "abcd2vwxyz1pqrst2": every dynamic
      *> member at its maximum, so the first decomposition already
      *> places K = 2, and the second, at 2, gives P=[abcd] K=2
      *> D1=[vwxyz] X1=1 D2=[pqrst] X2=2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2497CNT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb2497cnt.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 P PIC X DYNAMIC LENGTH LIMIT 4.
          05 K PIC 9.
          05 E OCCURS 0 TO 3 DEPENDING ON K.
             10 D PIC X DYNAMIC LENGTH LIMIT 5.
             10 X PIC X.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           MOVE "7a" TO P
           MOVE 0 TO K
           WRITE R
           MOVE "abcd" TO P
           MOVE 2 TO K
           MOVE "vwxyz" TO D (1)
           MOVE "1" TO X (1)
           MOVE "pqrst" TO D (2)
           MOVE "2" TO X (2)
           WRITE R
           CLOSE F
           OPEN INPUT F
           READ F
           DISPLAY "1 P=[" P "] K=" K
           READ F
           DISPLAY "2 P=[" P "] K=" K " D1=[" D (1) "] X1=" X (1)
               " D2=[" D (2) "] X2=" X (2)
           CLOSE F
           STOP RUN.
