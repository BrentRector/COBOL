      *> reject-at: 2002 2014 2023
      *> kb/Work PB1507 - 10.6.1: a program-definition contains only `[ program-definition ] ...` after its procedure
      *>   division, and a function-definition is a source unit of the compilation group itself. FIP1507F is written
      *>   INSIDE the program FIP1507 (before its END PROGRAM). COBOLNET2274. The legal shape is
      *>   conformance/2002/pb1507_source_unit_shapes_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FIP1507.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. FIP1507F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R.
           MOVE 1 TO R
           GOBACK.
       END FUNCTION FIP1507F.
       END PROGRAM FIP1507.
