      *> kb/Work PB2697 - the dynamic-capacity format of SET (COBOL-2023) counts a TRAILING-P amount
      *> by its VALUE. ISO 13.18.40.4 GR14: "The symbol 'P' specifies the location of an assumed
      *> decimal point when that point is not within the number that appears in the data item." N20
      *> and N10 (PIC 9P) store the digits 2 and 1; their values are 20 and 10. The runtime used to
      *> hand back the stored digits for every scale <= 0, so SET CAP TO N20 made the capacity 2.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES AND NOT FROM A RUN:
      *> TO=0020    14.9.39.4 GR30 a): "If TO is specified, the new capacity is specified by integer-1
      *>            or arithmetic-expression-4" - the value 20.
      *> UP=0030    GR30 b): "the new capacity is obtained by adding integer-1 or the value of
      *>            arithmetic-expression-4 to the current capacity of the table" - 20 + 10. Both are
      *>            within the table's 50, so neither EC-BOUND-TABLE-LIMIT nor EC-BOUND-SET exists.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2697CP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DT.
          05 E PIC 9(2) OCCURS DYNAMIC CAPACITY IN CAP FROM 1 TO 50.
       01 N20 PIC 9P VALUE 20.
       01 N10 PIC 9P VALUE 10.
       01 W   PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           SET CAP TO N20.
           MOVE CAP TO W.
           DISPLAY "TO=" W.
           SET CAP UP BY N10.
           MOVE CAP TO W.
           DISPLAY "UP=" W.
           STOP RUN.
