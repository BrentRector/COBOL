      *> kb/Work PB1932 -- an object property written as a FUNCTION ARGUMENT
      *> is evaluated in its place in the left-to-right argument order.
      *> ISO 8.4.3.2.4 GR2: a function's arguments "are evaluated
      *> individually in the order specified in the list of arguments,
      *> from left to right".  ISO 8.4.3.9.4 GR1: a property used as a
      *> sending item is temp-1, whose value "is determined as though the
      *> associated get property method were invoked" -- so the GET IS that
      *> argument's evaluation.  Here the GET of P adds 1 to the EXTERNAL
      *> counter CNT (shared with the main program, 13.18.22.1) and returns
      *> it; CNT is reset to 0 before each line.
      *> The GET used to run ahead of every argument of the statement, so an
      *> argument to the LEFT of P read the incremented CNT (each line 2).
      *>  1 = 1   SUM(CNT, P OF A): CNT is evaluated first (0), then the
      *>          GET (1); 0 + 1.
      *>  2 = 1   SUM(CNT, MAX(P OF A, 0)): the same order one function deep.
      *>  3 = 2   SUM(P OF A, CNT): the GET is first (1), then CNT (1).
      *>  4 = 1   the user function P1932F(CNT + 0, P OF A): the expression
      *>          argument is evaluated (0) before the GET (1); 0 + 1.
      *>  5 = 2   P1932F(CNT, P OF A): CNT is an identifier "permitted as a
      *>          receiving operand", so it is passed BY REFERENCE (8.4.3.2.4
      *>          GR5 a)) and its value is "made available to the activated
      *>          function at the time control is transferred" (GR6 a)) --
      *>          after the GET has run: 1 + 1.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. P1932F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X PIC 9(4).
       01 Y PIC 9(4).
       01 R PIC 9(4).
       PROCEDURE DIVISION USING X Y RETURNING R.
       MAIN.
           COMPUTE R = X + Y.
       END FUNCTION P1932F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1932AO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P1932C
           PROPERTY P
           FUNCTION P1932F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9(4) EXTERNAL.
       01 A   USAGE OBJECT REFERENCE P1932C.
       01 W   PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE P1932C "NEW" RETURNING A.
           MOVE 0 TO CNT.
           COMPUTE W = FUNCTION SUM(CNT, P OF A).
           DISPLAY "1=" W.
           MOVE 0 TO CNT.
           COMPUTE W = FUNCTION SUM(CNT, FUNCTION MAX(P OF A, 0)).
           DISPLAY "2=" W.
           MOVE 0 TO CNT.
           COMPUTE W = FUNCTION SUM(P OF A, CNT).
           DISPLAY "3=" W.
           MOVE 0 TO CNT.
           COMPUTE W = FUNCTION P1932F(CNT + 0, P OF A).
           DISPLAY "4=" W.
           MOVE 0 TO CNT.
           COMPUTE W = FUNCTION P1932F(CNT, P OF A).
           DISPLAY "5=" W.
           STOP RUN.
       END PROGRAM PB1932AO.

       IDENTIFICATION DIVISION.
       CLASS-ID. P1932C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CNT PIC 9(4) EXTERNAL.
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY P.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC 9(4).
       PROCEDURE DIVISION RETURNING LK-R.
       MAIN.
           ADD 1 TO CNT.
           MOVE CNT TO LK-R.
       END METHOD.
       END OBJECT.
       END CLASS P1932C.
