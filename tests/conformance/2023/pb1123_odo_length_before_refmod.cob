      *> PB1123 (reference-modified ODO group arm) - ISO 14.6.4: item identification is the ordered list
      *>   "6) length evaluation for an occurs-depending group item  7) reference modification", and the
      *>   evaluation of each step is done in full before the next. (cite.py --check 14.6.4 -> OK, items 6 and 7.)
      *> G is an OCCURS 1 TO 9 DEPENDING ON N group holding "ZZZZZ" with N = 5 (the rest "-"). In
      *> G(FUNCTION SETN(9):) the function SETN stores 9 into N as a side effect and returns 1. The length of G
      *> is evaluated at step 6, BEFORE the reference modifier's function runs at step 7, so G is 5 positions
      *> long when identified and G(1:) is "ZZZZZ" - not the 9-position "ZZZZZ----" that reading N after the
      *> function ran gives.
      *>
      *>   X=ZZZZZ
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1123SETN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 EXTERNAL.
       LINKAGE SECTION.
       01 V PIC 9.
       01 R PIC 9.
       PROCEDURE DIVISION USING V RETURNING R.
           MOVE V TO N
           MOVE 1 TO R
           GOBACK.
       END FUNCTION PB1123SETN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1123ODO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB1123SETN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 EXTERNAL.
       01 G.
          05 GE PIC X OCCURS 1 TO 9 DEPENDING ON N.
       01 X PIC X(9).
       PROCEDURE DIVISION.
           MOVE 9 TO N
           MOVE ALL "-" TO G
           MOVE 5 TO N
           MOVE "ZZZZZ" TO G
           MOVE G(FUNCTION PB1123SETN(9):) TO X
           DISPLAY "X=" X
           STOP RUN.
       END PROGRAM PB1123ODO.
