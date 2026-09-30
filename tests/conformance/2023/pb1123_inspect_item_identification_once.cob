      *> PB1123 (INSPECT arm) - ISO 14.9.22.4 GR6: "Item identification for any identifier is done only once as the
      *>   first operation in the execution of the INSPECT statement."  (cite.py --check 14.9.22.4 -> OK 6)
      *> GR19: "Item identification of any identifier in the format 2 statement is done only once before
      *>   executing the format 1 statement."  (cite.py --check 14.9.22.4 -> OK 19)
      *> 14.6.4 7): the identifiers within a statement are evaluated in left to right order as the first
      *>   operation of the execution of that statement.  (cite.py --check 14.6.4 -> OK 7)
      *> So every subscript and reference modifier in the statement is evaluated BEFORE the statement tallies or
      *> replaces anything; a subscript that names a TALLYING counter keeps the value it had at the start.
      *> A - INSPECT YE(J) TALLYING J ... REPLACING: YE(J) is identified with J = 1, J then counts 3 more
      *>     ("AAA" holds three A) and the replaced image is written back to YE(1) - ZZZ AAA AAA AAA AAA -
      *>     not to YE(4), which re-evaluating J after the tally selected.
      *> B - a format 3 statement: T(ND) in the REPLACING phrase is identified as T(1) = "P" before ND is
      *>     incremented (GR19), so "AAB" becomes "PPB"; ND ends at 1 + 2 = 3.
      *> C - a counter subscripted by an EARLIER counter of the same statement: CNT(I) is identified with
      *>     I = 1, so the single "B" is tallied into CNT(1) while I takes the two "A": I = 3, CNT = 10000.
      *> D - a reference-modified identifier-1: R6(ND:3) is identified as R6(2:3) = "BCD" with ND = 2; the tally
      *>     adds two to ND (B and C) and the replaced image "xyD" is written back to R6(2:3), not to R6(4:3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1123INS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Y.
          05 YE PIC X(3) OCCURS 5 VALUE "AAA".
       01 J PIC 9 VALUE 1.
       01 S1 PIC X(6).
       01 ND PIC 9 VALUE 1.
       01 TT.
          05 T PIC X OCCURS 3 VALUE SPACE.
       01 I PIC 9 VALUE 1.
       01 CT.
          05 CNT PIC 9 OCCURS 5 VALUE 0.
       01 R6 PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "PQR" TO TT.
           INSPECT YE(J) TALLYING J FOR ALL "A" REPLACING ALL "A" BY "Z".
           DISPLAY "A=" J " " Y.
           MOVE "AAB" TO S1.
           MOVE 1 TO ND.
           INSPECT S1 TALLYING ND FOR ALL "A" REPLACING ALL "A" BY T(ND).
           DISPLAY "B=" S1 " " ND.
           MOVE "AAB" TO S1.
           MOVE 1 TO I.
           INSPECT S1 TALLYING I FOR ALL "A" CNT(I) FOR ALL "B".
           DISPLAY "C=" I " " CT.
           MOVE "ABCDEF" TO R6.
           MOVE 2 TO ND.
           INSPECT R6(ND:3) TALLYING ND FOR ALL "B" "C"
                  REPLACING ALL "B" BY "x" ALL "C" BY "y".
           DISPLAY "D=" R6 " " ND.
           STOP RUN.
