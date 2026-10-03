      *> ISO 8.4.3.2.4 GR2: "At the time reference is made to a function, its arguments are evaluated
      *> individually in the order specified in the list of arguments, from left to right. An argument being
      *> evaluated may itself be a function-identifier or may be an expression containing function-identifiers."
      *> GR1 makes the user-defined function and the intrinsic function the same kind of function-identifier,
      *> so one rule orders both. GR6a: the values of argument-1 are made available to the activated function
      *> "at the time control is transferred" - a BY REFERENCE identifier designates the caller's storage and
      *> is read there, so the function writing it (UAOBMP below) is visible to the argument on its left.
      *> Each leg fails if an earlier argument's value is read AFTER a later argument's activation changed it:
      *>   EXPR  - an arithmetic expression (BY CONTENT) to the left of an activation that changes its operand:
      *>           WS-A + 0 is evaluated first, 4, so the result is 4 * 1000 + 10 = 4010 (not 5010).
      *>   INTR  - an intrinsic function whose first argument is that data item: SUM(4, 10) = 14 (not 15).
      *>   NEST  - an intrinsic function nested as the first argument: MAX(4, 1) = 4 so 4010 (not 5010).
      *>   REF   - a BY REFERENCE identifier names the storage, so the function's store is seen: 5010.
      *>   VAL   - a BY VALUE formal: the value is taken at the argument, 5 * 10 + 10 = 60 (not 70).
      *>   SUB   - a subscripted identifier: WS-E (1) = 7 is the value, so 7 * 10 + 5 = 75 (not 85).
      *>   ORD   - alphanumeric arguments: the first WS-S is ABCD, the second QQQQ, the third is read after
      *>           UAOCHG stored ZZZZ; the least is the first, so ORD-MIN = 1 (not 2, the QQQQ it is without).
      *>   BOOL  - a boolean expression argument (8.8.2): WS-BA B-AND WS-BB is B"1" when it is evaluated, before
      *>           UAOCLR stores B"0" into WS-BA, so the activated function sees B"1" and answers 1 (not 0).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UARGORD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION UAOF2
           FUNCTION UAOVAL
           FUNCTION UAOBMP
           FUNCTION UAOCHG
           FUNCTION UAOCLR
           FUNCTION UAOBOOL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9(4).
       01 WS-I PIC 9(4).
       01 WS-T.
          05 WS-E PIC 9(4) OCCURS 3.
       01 WS-S PIC X(4).
       01 WS-R PIC 9(8).
       01 WS-BA PIC 1.
       01 WS-BB PIC 1.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 4 TO WS-A.
           COMPUTE WS-R = FUNCTION UAOF2(WS-A + 0,
               FUNCTION UAOBMP(WS-A)).
           DISPLAY "EXPR=" WS-R " A=" WS-A.
           MOVE 4 TO WS-A.
           COMPUTE WS-R = FUNCTION SUM(WS-A, FUNCTION UAOBMP(WS-A)).
           DISPLAY "INTR=" WS-R " A=" WS-A.
           MOVE 4 TO WS-A.
           COMPUTE WS-R = FUNCTION UAOF2(FUNCTION MAX(WS-A, 1),
               FUNCTION UAOBMP(WS-A)).
           DISPLAY "NEST=" WS-R " A=" WS-A.
           MOVE 4 TO WS-A.
           COMPUTE WS-R = FUNCTION UAOF2(WS-A, FUNCTION UAOBMP(WS-A)).
           DISPLAY "REF=" WS-R " A=" WS-A.
           MOVE 4 TO WS-A.
           COMPUTE WS-R = FUNCTION UAOVAL(WS-A + 1,
               FUNCTION UAOBMP(WS-A)).
           DISPLAY "VAL=" WS-R " A=" WS-A.
           MOVE 1 TO WS-I.
           MOVE 7 TO WS-E (1).
           MOVE 8 TO WS-E (2).
           COMPUTE WS-R = FUNCTION UAOVAL(WS-E (WS-I),
               FUNCTION UAOBMP(WS-I)).
           DISPLAY "SUB=" WS-R " I=" WS-I.
           MOVE "ABCD" TO WS-S.
           COMPUTE WS-R = FUNCTION ORD-MIN(WS-S,
               FUNCTION UAOCHG(WS-S), WS-S).
           DISPLAY "ORD=" WS-R " S=" WS-S.
           MOVE B"1" TO WS-BA.
           MOVE B"1" TO WS-BB.
           COMPUTE WS-R = FUNCTION UAOBOOL(WS-BA B-AND WS-BB,
               FUNCTION UAOCLR(WS-BA)).
           DISPLAY "BOOL=" WS-R " BA=" WS-BA.
           STOP RUN.
       END PROGRAM UARGORD.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UAOF2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-Y PIC 9(4).
       01 L-R PIC 9(8).
       PROCEDURE DIVISION USING L-X L-Y RETURNING L-R.
       P.
           COMPUTE L-R = L-X * 1000 + L-Y.
           GOBACK.
       END FUNCTION UAOF2.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UAOVAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-Y PIC 9(4).
       01 L-R PIC 9(8).
       PROCEDURE DIVISION USING BY VALUE L-X L-Y RETURNING L-R.
       P.
           COMPUTE L-R = L-X * 10 + L-Y.
           GOBACK.
       END FUNCTION UAOVAL.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UAOBMP.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           ADD 1 TO L-X.
           COMPUTE L-R = L-X * 2.
           IF L-R < 10 MOVE 5 TO L-R END-IF.
           GOBACK.
       END FUNCTION UAOBMP.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UAOCHG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC X(4).
       01 L-R PIC X(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE "ZZZZ" TO L-X.
           MOVE "QQQQ" TO L-R.
           GOBACK.
       END FUNCTION UAOCHG.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UAOCLR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 1.
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           MOVE B"0" TO L-X.
           MOVE 5 TO L-R.
           GOBACK.
       END FUNCTION UAOCLR.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. UAOBOOL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-P PIC 1.
       01 L-Y PIC 9(4).
       01 L-R PIC 9(8).
       PROCEDURE DIVISION USING L-P L-Y RETURNING L-R.
       P.
           IF L-P MOVE 1 TO L-R ELSE MOVE 0 TO L-R.
           GOBACK.
       END FUNCTION UAOBOOL.
