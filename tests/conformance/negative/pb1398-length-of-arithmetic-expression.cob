      *> reject-at: 85 2002 2014 2023
      *> ISO 15.50.3 r1: "Argument-1 shall be an alphanumeric, national, or boolean literal; a data item of any
      *> class or category; a based entry; or a type-name." An ARITHMETIC EXPRESSION is none of them - the
      *> function-identifier forms kb/Work PB1398 now admits (FUNCTION LENGTH(FUNCTION SQRT(4))) reference a data
      *> item, an expression does not (COBOLNET1627). The diagnostic names what was written: "an arithmetic
      *> expression", where it used to say "a numeric literal" of every shape that reached it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1398EXP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       01 B PIC 9 VALUE 2.
       PROCEDURE DIVISION.
           DISPLAY FUNCTION LENGTH(A + B).
           STOP RUN.
