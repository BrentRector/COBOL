      *> ISO 8.4.3.2.4 GR1: "If function-prototype-name-1 is specified, the description, class, and category of
      *> the temporary data item is that specified by the description in the linkage section of the item
      *> specified in the RETURNING phrase of the procedure division header of the function prototype", and
      *> 14.2.2 SR5 places no category restriction on a function's RETURNING item. So a FLOAT, a BOOLEAN, a
      *> data-pointer, an index and every shape of group result is a legal function-identifier, read by the
      *> statements that admit its class and category. Each leg fails if its RETURNING category is refused or
      *> delivered wrongly:
      *>   FLT  - FLOAT-LONG 3 / 2 = 1.5 into PIC 9(4)V99: 000150.
      *>   BOOL - PIC 1(4) holding B"1010": MOVE gives 1010; B-NOT gives 0101 (8.8.2); B-AND B"1100" gives 1000.
      *>   BIT  - PIC 1 holding B"1": a simple boolean condition (8.8.4.3) over the function is true.
      *>   IDX  - a USAGE INDEX result set to occurrence 3 equals an index data item set to 3 (8.8.4.2.13).
      *>   PTR  - a data-pointer result is NULL for one function and not NULL for the other (8.8.4.2.16).
      *>   RED  - a group with an internal REDEFINES: 1234 read both ways.
      *>   ODO  - an OCCURS DEPENDING group of 2 occurrences moves as its current extent, 3 characters "2XY"
      *>          (13.18.38.4 GR8 a), padded with spaces by the alphanumeric receiver.
      *>   STR  - a strongly typed group displays its character image: 12AB.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UEVFLT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R USAGE FLOAT-LONG.
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           COMPUTE L-R = L-X / 2.
           GOBACK.
       END FUNCTION UEVFLT.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UEVBOOL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 1(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE B"1010" TO L-R.
           GOBACK.
       END FUNCTION UEVBOOL.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UEVBIT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 1.
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE B"1" TO L-R.
           GOBACK.
       END FUNCTION UEVBIT.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UEVIDX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F-TBL.
          05 F-EL PIC 9(2) OCCURS 5 INDEXED BY F-IX.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R USAGE INDEX.
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           SET F-IX TO 3.
           SET L-R TO F-IX.
           GOBACK.
       END FUNCTION UEVIDX.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UEVPNUL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R USAGE POINTER.
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           SET L-R TO NULL.
           GOBACK.
       END FUNCTION UEVPNUL.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UEVPSET.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 F-ITEM PIC X(4).
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R USAGE POINTER.
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           SET L-R TO ADDRESS OF F-ITEM.
           GOBACK.
       END FUNCTION UEVPSET.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UEVRED.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R.
          05 R-A PIC X(4).
          05 R-B REDEFINES R-A PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE "1234" TO R-A.
           GOBACK.
       END FUNCTION UEVRED.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UEVODO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R.
          05 O-N PIC 9.
          05 O-T PIC X OCCURS 1 TO 5 DEPENDING ON O-N.
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE 2 TO O-N.
           MOVE "X" TO O-T (1).
           MOVE "Y" TO O-T (2).
           GOBACK.
       END FUNCTION UEVODO.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UEVSTR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-T TYPEDEF STRONG.
          05 T-A PIC 9(2).
          05 T-B PIC X(2).
       01 L-R TYPE L-T.
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE 12 TO T-A OF L-R.
           MOVE "AB" TO T-B OF L-R.
           GOBACK.
       END FUNCTION UEVSTR.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UEVERY.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION UEVFLT
           FUNCTION UEVBOOL
           FUNCTION UEVBIT
           FUNCTION UEVIDX
           FUNCTION UEVPNUL
           FUNCTION UEVPSET
           FUNCTION UEVRED
           FUNCTION UEVODO
           FUNCTION UEVSTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-FIX PIC 9(4)V99.
       01 WS-B PIC 1(4).
       01 WS-TBL.
          05 WS-EL PIC 9(2) OCCURS 5 INDEXED BY WS-IX.
       01 WS-DI USAGE INDEX.
       01 WS-RED.
          05 WS-RA PIC X(4).
          05 WS-RB REDEFINES WS-RA PIC 9(4).
       01 WS-ANY PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE WS-FIX = FUNCTION UEVFLT(3).
           DISPLAY "FLT=" WS-FIX.
           MOVE FUNCTION UEVBOOL(3) TO WS-B.
           DISPLAY "BOOL=" WS-B.
           COMPUTE WS-B = B-NOT FUNCTION UEVBOOL(3).
           DISPLAY "NOT=" WS-B.
           COMPUTE WS-B = FUNCTION UEVBOOL(3) B-AND B"1100".
           DISPLAY "AND=" WS-B.
           IF FUNCTION UEVBIT(3) DISPLAY "BIT=TRUE"
               ELSE DISPLAY "BIT=FALSE".
           SET WS-IX TO 3.
           SET WS-DI TO WS-IX.
           IF FUNCTION UEVIDX(1) = WS-DI DISPLAY "IDX=EQ"
               ELSE DISPLAY "IDX=NE".
           IF FUNCTION UEVPNUL(1) = NULL DISPLAY "PNUL=NULL"
               ELSE DISPLAY "PNUL=SET".
           IF FUNCTION UEVPSET(1) = NULL DISPLAY "PSET=NULL"
               ELSE DISPLAY "PSET=SET".
           MOVE FUNCTION UEVRED(1) TO WS-RED.
           DISPLAY "RED=" WS-RA " " WS-RB.
           MOVE FUNCTION UEVODO(1) TO WS-ANY.
           DISPLAY "ODO=[" WS-ANY "]".
           DISPLAY "STR=" FUNCTION UEVSTR(1).
           STOP RUN.
       END PROGRAM UEVERY.
