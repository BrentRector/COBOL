      *> kb/Work PB2113 (owner ruling D10; DESIGN-frontend-grammar
      *> 9.4/9.5 D10.1) - arithmetic-expression subscripts and keyword-
      *> omitted argument lists, both PARSED from the reference's own
      *> parentheses placed at COBOL 2002, the edition whose REPOSITORY
      *> paragraph lets the word FUNCTION be omitted
      *> (negative/pb2113-keyword-omitted-arguments-85 is the edition
      *> below). Annex D.3.5.3's own examples, on a smaller table: HARRY
      *> (BAKER-INDEX - 3, 4, (XCOUNTER * 2) - 3) with BAKER-INDEX 4 and
      *> XCOUNTER 2 is HARRY (1, 4, 1) -> 41; and "an arithmetic
      *> expression that starts with a unary operator and follows an
      *> identifier", EASY (XCOUNTER (- YCOUNTER)) with YCOUNTER -3, is
      *> EASY (2, 3) -> E7, because XCOUNTER carries no OCCURS and so
      *> cannot own the '(' (8.4.2.3.3 SR2). 8.3.5 1) makes the space a
      *> separator and 8.3.3.3.2 2) puts a literal's sign at its
      *> leftmost character, while 8.7.1 surrounds an operator with
      *> spaces: HARRY (V1 +1 1) is three subscripts (2, 1, 1) -> 21 and
      *> HARRY (V1 + 1 1 1) three with the first V1 + 1 (3, 1, 1) -> 31.
      *> With FUNCTION ALL INTRINSIC the word FUNCTION may be omitted
      *> (8.4.3.2.3 SR2): MAX (HARRY (1 4 1) 7 -100) = 41, MIN (4, 2; 9)
      *> = 2 (8.3.5 2) separators), HARRY (FUNCTION MIN (V1 4) MAX (1 2)
      *> 1) = HARRY (2, 2, 1) -> 22, a keyword-omitted result reference-
      *> modified UPPER-CASE ("abc") (2:1) = B (8.4.3.3.3 SR2), and the
      *> table(ALL) argument SUM (EE (ALL)) = 7 + 9 + 4 = 20 (15.3).
      *> Each leg fails if a separator, a sign, a nested parenthesis or
      *> an argument boundary is read any other way.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2113B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XCOUNTER PIC S99 VALUE 2.
       01 YCOUNTER PIC S99 VALUE -3.
       01 V1 PIC 9 VALUE 2.
       01 N PIC 99.
       01 TBL.
          02 BAKER OCCURS 4 INDEXED BY BAKER-INDEX.
             03 DOG OCCURS 5.
                04 EASY PIC XX.
                04 FOX.
                   05 GEORGE OCCURS 4.
                      06 HARRY PIC 99.
       01 T3.
          02 EE PIC 9 OCCURS 3.
       PROCEDURE DIVISION.
           INITIALIZE TBL.
           MOVE 7 TO EE (1).
           MOVE 9 TO EE (2).
           MOVE 4 TO EE (3).
           MOVE 41 TO HARRY (1, 4, 1).
           MOVE "E7" TO EASY (2, 3).
           MOVE 21 TO HARRY (2, 1, 1).
           MOVE 31 TO HARRY (3, 1, 1).
           MOVE 22 TO HARRY (2, 2, 1).
           SET BAKER-INDEX TO 4.
           DISPLAY "ANNEX1 "
               HARRY (BAKER-INDEX - 3, 4, (XCOUNTER * 2) - 3).
           DISPLAY "ANNEX2 " EASY (XCOUNTER (- YCOUNTER)).
           DISPLAY "SIGNED " HARRY (V1 +1 1).
           DISPLAY "OPER   " HARRY (V1 + 1 1 1).
           MOVE MAX (HARRY (1 4 1) 7 -100) TO N.
           DISPLAY "MAX    " N.
           MOVE MIN (4, 2; 9) TO N.
           DISPLAY "MIN    " N.
           DISPLAY "NESTED " HARRY (FUNCTION MIN (V1 4) MAX (1 2) 1).
           DISPLAY "RESULT " UPPER-CASE ("abc") (2:1).
           MOVE SUM (EE (ALL)) TO N.
           DISPLAY "ALL    " N.
           STOP RUN.
