      *> kb/Work PB1662 - ISO 8.8.4.2.13 row 2: "Relation tests may be
      *> made only between ... an index-name and a numeric data item or
      *> numeric literal." A function-identifier "references the unique
      *> data item that results from the evaluation of a function"
      *> (8.4.3.2.1), and a numeric or integer function is that item with
      *> class numeric (15.2 items 4 and 5), so it is row 2's numeric data
      *> item, whether the compiler folds its value (FUNCTION LENGTH of a
      *> fixed item) or computes it (INTEGER, SQRT, MAX). The screen used to
      *> admit the folded one and refuse the others, so the verdict
      *> depended on what the optimizer could compute. The occurrence
      *> number of I1 is 3.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB1662A.
000300 DATA DIVISION.
000400 WORKING-STORAGE SECTION.
000500 01  W-XA PIC X(3) VALUE "abc".
000600 01  W-T.
000700     05  W-E PIC X(4) OCCURS 5 INDEXED BY I1.
000800 PROCEDURE DIVISION.
000900     SET I1 TO 3.
001000     IF I1 = FUNCTION LENGTH(W-XA)
001100         DISPLAY "FOLDED EQ" ELSE DISPLAY "FOLDED NE".
001200     IF I1 = FUNCTION INTEGER(3.7)
001300         DISPLAY "INT EQ" ELSE DISPLAY "INT NE".
001400     IF I1 = FUNCTION INTEGER(4.2)
001500         DISPLAY "INT4 EQ" ELSE DISPLAY "INT4 NE".
001600     IF FUNCTION SQRT(9) = I1
001700         DISPLAY "SQRT EQ" ELSE DISPLAY "SQRT NE".
001800     IF I1 < FUNCTION MAX(2 4)
001900         DISPLAY "MAX LT" ELSE DISPLAY "MAX GE".
002000     STOP RUN.
