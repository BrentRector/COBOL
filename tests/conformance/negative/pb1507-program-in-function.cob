      *> reject-at: 2002 2014 2023
      *> kb/Work PB1507 - 10.6.1: the function-definition format is `FUNCTION-ID. ... [ procedure-division ] END
      *>   FUNCTION name.` - it has no contained program-definition slot. PIF1507P is written inside the function
      *>   PIF1507F. COBOLNET2274.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PIF1507F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R.
           MOVE 1 TO R
           GOBACK.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PIF1507P.
       PROCEDURE DIVISION.
           DISPLAY "INNER"
           GOBACK.
       END PROGRAM PIF1507P.
       END FUNCTION PIF1507F.
