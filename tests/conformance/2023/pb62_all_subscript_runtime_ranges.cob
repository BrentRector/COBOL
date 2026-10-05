      *> PB62 - the ALL subscript in an intrinsic argument (15.3): "When ALL is specified as a subscript, the
      *> effect is as if each table element associated with that subscript position were specified", left to
      *> right with the rightmost ALL varying fastest - and its THREE ranges: a fixed OCCURS count; "If the ALL
      *> subscript is associated with a data item described with an OCCURS DEPENDING ON clause, the range of
      *> values is determined by the object of the OCCURS DEPENDING ON clause"; "If the subscript ALL is
      *> specified for a dynamic-capacity table, the range of values of the subscript is from 1 to the current
      *> capacity of the table". Before PB62 the expansion happened at BIND time by the OCCURS count: an ODO
      *> table staged loud at run time and a dynamic-capacity table did not bind at all. Every value below is
      *> derived from the returned-value rules over the CURRENT occurrences.
      *> ODO (WS-N = 3; 1.00 5.00 9.00): MEAN 15/3 = 5 (15.60.4 r1c); MEDIAN the middle of the sorted list = 5
      *>   (15.61.4 r1); MIDRANGE (1+9)/2 = 5 (15.62.4); RANGE 9-1 = 8 (15.76.4); SUM 15 (15.88.4); MAX 9;
      *>   ORD-MAX 3 (15.71.4 - the position of the greatest); then WS-N = 4 adds 2.00: MEDIAN's EVEN branch
      *>   (2+5)/2 = 3.5 (15.61.4 r2), MEAN 17/4 = 4.25.
      *> MIXED: two ALL arguments and a written one enumerate in source order: 7+8+9 + 1+5+9 = 39.
      *> DYN: a dynamic-capacity table at capacity 3 (10 20 30): SUM 60, MIDRANGE 20.
      *> ALLALL / 2ALL / ALL3: a two-level table (rows 1..2 x cols 1..3 holding 1..6): ALL ALL = 21 with the
      *>   rightmost ALL varying fastest (ORD-MAX = 6, the sixth implicit specification, value 6); a fixed
      *>   outer subscript with an inner ALL = 4+5+6 = 15; an outer ALL with a fixed inner = 3+6 = 9.
      *> S*: the string body over an ODO ALL - MAX "xyz", MIN "abc", ORD-MIN 1 (15.59.4 / 15.63.4 / 15.72.4).
      *> NEST: a dynamic-capacity table NESTED in a fixed table has a capacity PER OUTER OCCURRENCE (ND(1 ..)
      *>   holds 1 2, ND(2 ..) holds 3): SUM(ND(ALL ALL)) = 6 - each inner range is read for its outer index.
      *> PV: PRESENT-VALUE(RT AM(ALL)) - the repeated argument-2 over an ODO ALL (15.74.2 `argument-1
      *>   { argument-2 } ...`): 100/1.1 + 200/1.21 + 300/1.331 = 481.5927 (15.74.4).
      *> TRIM/CONCAT: the two string-family formats that repeat an argument - TRIM's 2023 `[ argument-2 ] ...`
      *>   (15.96.2, the figure notes: the ellipsis "denotes repetition of that bracketed portion") over an ODO
      *>   ALL of trim characters x,y: "xxABCDyy" -> "ABCD"; CONCAT (15.18.2 `argument-1, argument-2 ...`) over a
      *>   fixed ALL: "abc" "def" "ghi" -> "abcdefghi", truncated to the 8-character receiver.
      *> CD: FMT-15.21.2's two legs, fixed and pinned here - keyword-omitted CURRENT-DATE (8.4.3.2.3 SR2 under
      *>   FUNCTION ALL INTRINSIC) is the 21-character value (15.21.3), and FUNCTION CURRENT-DATE (1:4) is a
      *>   reference modification of the result (8.4.3.3.3 SR2 - it is alphanumeric): 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB62ALLRANGES.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 3.
       01 ODO.
          05 WS-E PIC S9(4)V99 OCCURS 1 TO 6 TIMES DEPENDING ON WS-N.
       01 FIX.
          05 F PIC 9(2) OCCURS 3 TIMES.
       01 DYN.
          05 D OCCURS DYNAMIC CAPACITY IN CAP PIC 9(3).
       01 TWO.
          05 R OCCURS 2 TIMES.
             10 C PIC 9 OCCURS 3 TIMES.
       01 STR.
          05 S PIC X(3) OCCURS 1 TO 5 DEPENDING ON WS-N.
       01 NEST.
          05 NR OCCURS 2 TIMES.
             10 ND OCCURS DYNAMIC PIC 9.
       01 WS-R PIC S9(4)V9(4).
       01 WS-I PIC 9(4).
       01 WS-A PIC X(3).
       01 RT PIC 9V99 VALUE 0.10.
       01 AM-GRP.
          05 AM PIC 9(3) OCCURS 1 TO 4 DEPENDING ON WS-N.
       01 TC-GRP.
          05 TC PIC X OCCURS 1 TO 3 DEPENDING ON WS-N.
       01 X8 PIC X(8) VALUE "xxABCDyy".
       01 W.
          05 WD PIC X(3) OCCURS 3 TIMES.
       01 R8 PIC X(8).
       PROCEDURE DIVISION.
           MOVE 1.00 TO WS-E(1) MOVE 5.00 TO WS-E(2) MOVE 9.00 TO WS-E(3)
           COMPUTE WS-R = MEAN(WS-E(ALL))      DISPLAY "MEAN=" WS-R
           COMPUTE WS-R = MEDIAN(WS-E(ALL))    DISPLAY "MEDIAN=" WS-R
           COMPUTE WS-R = MIDRANGE(WS-E(ALL))  DISPLAY "MIDRANGE=" WS-R
           COMPUTE WS-R = RANGE(WS-E(ALL))     DISPLAY "RANGE=" WS-R
           COMPUTE WS-R = SUM(WS-E(ALL))       DISPLAY "SUM=" WS-R
           COMPUTE WS-R = MAX(WS-E(ALL))       DISPLAY "MAX=" WS-R
           COMPUTE WS-I = ORD-MAX(WS-E(ALL))   DISPLAY "ORDMAX=" WS-I
           MOVE 4 TO WS-N MOVE 2.00 TO WS-E(4)
           COMPUTE WS-R = MEDIAN(WS-E(ALL))    DISPLAY "MEDIAN4=" WS-R
           COMPUTE WS-R = MEAN(WS-E(ALL))      DISPLAY "MEAN4=" WS-R
           MOVE 3 TO WS-N
           MOVE 7 TO F(1) MOVE 8 TO F(2) MOVE 9 TO F(3)
           COMPUTE WS-R = SUM(F(ALL) WS-E(ALL)) DISPLAY "MIXED=" WS-R
           SET CAP TO 3 MOVE 10 TO D(1) MOVE 20 TO D(2) MOVE 30 TO D(3)
           COMPUTE WS-R = SUM(D(ALL))           DISPLAY "DYN3=" WS-R
           COMPUTE WS-R = MIDRANGE(D(ALL))      DISPLAY "DYNMID=" WS-R
           MOVE 1 TO C(1 1) MOVE 2 TO C(1 2) MOVE 3 TO C(1 3)
           MOVE 4 TO C(2 1) MOVE 5 TO C(2 2) MOVE 6 TO C(2 3)
           COMPUTE WS-R = SUM(C(ALL ALL))       DISPLAY "ALLALL=" WS-R
           COMPUTE WS-R = SUM(C(2 ALL))         DISPLAY "2ALL=" WS-R
           COMPUTE WS-R = SUM(C(ALL 3))         DISPLAY "ALL3=" WS-R
           COMPUTE WS-I = ORD-MAX(C(ALL ALL))   DISPLAY "ORDALLALL=" WS-I
           MOVE "abc" TO S(1) MOVE "xyz" TO S(2) MOVE "mno" TO S(3)
           MOVE MAX(S(ALL)) TO WS-A             DISPLAY "SMAX=" WS-A
           MOVE MIN(S(ALL)) TO WS-A             DISPLAY "SMIN=" WS-A
           COMPUTE WS-I = ORD-MIN(S(ALL))       DISPLAY "SORDMIN=" WS-I
           MOVE 1 TO ND(1 1) MOVE 2 TO ND(1 2) MOVE 3 TO ND(2 1)
           COMPUTE WS-R = SUM(ND(ALL ALL))      DISPLAY "NEST=" WS-R
           MOVE 100 TO AM(1) MOVE 200 TO AM(2) MOVE 300 TO AM(3)
           COMPUTE WS-R = PRESENT-VALUE(RT AM(ALL)) DISPLAY "PV=" WS-R
           MOVE 2 TO WS-N MOVE "x" TO TC(1) MOVE "y" TO TC(2)
           MOVE TRIM(X8 TC(ALL)) TO R8              DISPLAY "TRIM=[" R8 "]"
           MOVE "abc" TO WD(1) MOVE "def" TO WD(2) MOVE "ghi" TO WD(3)
           MOVE CONCAT(WD(ALL)) TO R8               DISPLAY "CONCAT=[" R8 "]"
           DISPLAY "CD=" LENGTH(CURRENT-DATE) " " LENGTH(FUNCTION CURRENT-DATE (1:4))
           STOP RUN.
       END PROGRAM PB62ALLRANGES.
