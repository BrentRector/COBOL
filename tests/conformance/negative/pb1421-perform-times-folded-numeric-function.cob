      *> reject-at: 2002 2014 2023
      *> Train 1021 review finding C-3, sibling (kb/Work PB86 / PB1421) - 14.9.28.3 SR2 requires an integer
      *> PERFORM ... TIMES count, and HIGHEST-ALGEBRAIC of the numeric-edited E is a NUMERIC function (15.43.1:
      *> "Alphanumeric | Numeric"), refused by 8.4.3.2.3 SR11 though its folded value 999 is integral.
      *> It used to compile and iterate 999 times.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1421PT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E PIC ZZ9.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM FUNCTION HIGHEST-ALGEBRAIC(E) TIMES
               CONTINUE
           END-PERFORM
           STOP RUN.
