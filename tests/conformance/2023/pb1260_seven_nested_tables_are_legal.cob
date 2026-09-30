      *> PB1260 - ISO 13.18.38.3 SR10: tables may nest "as long as the
      *>   number of subscripts required does not exceed seven" - so SEVEN
      *>   nested OCCURS entries are legal (the control for the eight-deep
      *>   negative pb1260-table-needs-eight-subscripts).
      *> cite.py --check 13.18.38.3 "as long as the number of subscripts
      *>   required does not exceed seven" -> OK  13.18.38.3 10)
      *> Derivation: the program compiles; MOVE "Z" TO the (1 2 1 2 1 2 1)
      *>   occurrence of the innermost table, and reading it back gives Z.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1260D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          02 A1 OCCURS 2.
           03 A2 OCCURS 2.
            04 A3 OCCURS 2.
             05 A4 OCCURS 2.
              06 A5 OCCURS 2.
               07 A6 OCCURS 2.
                08 A7 PIC X OCCURS 2.
       PROCEDURE DIVISION.
           MOVE "Z" TO A7 (1 2 1 2 1 2 1)
           DISPLAY "A7=" A7 (1 2 1 2 1 2 1)
           STOP RUN.
