      *> kb/Work PB1174 — THE FORMAT-2 TABLE SORT SORTS THE TABLE'S CURRENT OCCURRENCES, NOT ITS PHYSICAL ARRAY.
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 14.9.40.4 "The number of occurrences of table elements referenced by data-name-2 is
      *>     determined by the rules in the OCCURS clause" -> OK §14.9.40.4 20)
      *>   cite.py --check 13.18.38.4 "The value of the data item referenced by data-name-1 represents the current
      *>     number of occurrences of the subject of the entry" -> OK §13.18.38.4 7)
      *> So with N = 3 the table ITM has three occurrences, and only {30, 10, 50} are sorted; occurrences 4..5 are
      *> not elements of the table at all, so they can never enter occurrences 1..3. (Their content after the
      *> statement is not an occurrence of the sorted table; this implementation leaves it untouched, which the
      *> DISPLAY of 4..5 pins so a change to that choice is seen, not silent.)
      *> The Format-2 SORT is a COBOL-2002 introduction; negative/w70c-pb1174-table-sort-below-2002 pins its
      *> refusal at 85.
      *>
      *> WHY EACH LEG CAN FAIL (the pre-PB1174 build sorted all five physical elements):
      *>   1  N = 3, ASCENDING        -> 10 30 50 | 05 01   (pre-fix: 01 05 10 | 30 50 — two stale values pulled
      *>                                                    into the table, the real 50 pushed out of it)
      *>   2  N = 4, DESCENDING       -> 50 30 10 05 | 01   (the count is read at the statement, not fixed)
      *>   3  N = 1                   -> 50 | 30 10 05 01   (one occurrence is already in order; nothing past it
      *>                                                    moves — pre-fix: 01 05 10 30 50)
      *>   4  N = 7 (above integer-2) under EC-BOUND-ODO checking -> RAISED EC-BOUND-ODO (§13.18.38.4 GR7), and
      *>      the declarative's RESUME NEXT STATEMENT abandons the SORT: 50 30 10 05 01 unchanged.
       >>TURN EC-BOUND-ODO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W70CPB1174.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 5.
       01 TBL.
          05 ITM PIC 9(2) OCCURS 1 TO 5 TIMES DEPENDING ON N.
       01 I PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-BOUND-ODO.
       D1P.
           DISPLAY "RAISED " FUNCTION EXCEPTION-STATUS.
           RESUME NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           MOVE 30 TO ITM(1) MOVE 10 TO ITM(2) MOVE 50 TO ITM(3)
           MOVE 05 TO ITM(4) MOVE 01 TO ITM(5)
           MOVE 3 TO N
           SORT ITM ASCENDING
           PERFORM SHOW
           MOVE 4 TO N
           SORT ITM DESCENDING
           PERFORM SHOW
           MOVE 1 TO N
           SORT ITM ASCENDING
           PERFORM SHOW
           MOVE 7 TO N
           SORT ITM ASCENDING
           PERFORM SHOW
           STOP RUN.
       SHOW.
           MOVE 5 TO N
           DISPLAY ITM(1) " " ITM(2) " " ITM(3) " " ITM(4) " " ITM(5).
