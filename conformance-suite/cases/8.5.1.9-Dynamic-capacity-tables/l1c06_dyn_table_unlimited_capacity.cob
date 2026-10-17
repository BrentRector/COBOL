      *>--- conformance-case
      *> id: l1c06_dyn_table_unlimited_capacity
      *> rules: [GR-8.5.1.9.1-2]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.5.1.9.1 2) Dynamic-capacity tables — a table with no TO
      *>   phrase has no declared ceiling
      *> Rule: "A dynamic-capacity table differs from an
      *>   occurs-depending table in that:
      *> ... 2) it may have an unlimited capacity, and"
      *>   cite.py --check 8.5.1.9.1 "it may have an unlimited capacity,
      *>     and"
      *>   -> OK  §8.5.1.9.1 2)  (General)
      *>   cite.py --check 8.5.1.9.1 "If neither is specified, the
      *>     current capacity is
      *>   initialized to zero."  -> OK  §8.5.1.9.1 3)  (General)
      *>   cite.py --check 8.5.1.9.1 "The actual limit for the current
      *>     capacity imposed by
      *>   the implementor and by current resource availability is
      *>     referred to as the
      *>   maximum capacity."  -> OK  §8.5.1.9.1 3)  (General)
      *>   cite.py --check 8.5.1.9.3 "a new element is automatically
      *>     created and the
      *>   capacity of the table is increased to the value given by the
      *>     subscript"
      *>   -> OK  §8.5.1.9.3  (Implicit changes in capacity)
      *>   cite.py --check 8.5.1.9.4 "the capacity of the
      *>     dynamic-capacity table may be
      *>   increased or decreased explicitly by means of the
      *>     dynamic-capacity-table format
      *>   SET statement"  -> OK  §8.5.1.9.4  (Explicit changes in
      *>     capacity)
      *> The table below has neither FROM nor TO: nothing in the
      *>   declaration bounds it, so
      *> only the implementor's maximum capacity (COBOL.NET:
      *>   1,073,741,823 occurrences,
      *> docs/CONFORMANCE.md DOC-A.1-61) limits growth - far above the
      *>   values used here.
      *> Derivation of each expected line:
      *>   INIT=[0]        no FROM and no VALUE: current capacity
      *>     initialized to zero.
      *>   GROW=[100000]   MOVE 7 TO WS-E (100000) is a receiving
      *>     reference past the
      *>                   current capacity: the table grows to the
      *>                     subscript, 100000.
      *>   E100000=007     the new element holds the moved value.
      *>   SET=[250000]    SET WS-CAP TO WS-REQ (250000, through a data
      *>     item; there is
      *>                   no TO, so no expected capacity is exceeded)
      *>                     raises it again.
      *>   E250000=005     MOVE 5 TO WS-E (250000) is now within the
      *>     capacity.
      *>   KEEP=007        growing did not disturb element 100000.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C06I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ED       PIC Z(7)9.
       01 WS-REQ   PIC 9(7) VALUE 250000.
       01 WS-TABLE.
          05 WS-E PIC 9(3) OCCURS DYNAMIC CAPACITY IN WS-CAP.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE WS-CAP TO ED.
           DISPLAY "INIT=[" FUNCTION TRIM (ED) "]".
           MOVE 7 TO WS-E (100000).
           MOVE WS-CAP TO ED.
           DISPLAY "GROW=[" FUNCTION TRIM (ED) "]".
           DISPLAY "E100000=" WS-E (100000).
           SET WS-CAP TO WS-REQ.
           MOVE WS-CAP TO ED.
           DISPLAY "SET=[" FUNCTION TRIM (ED) "]".
           MOVE 5 TO WS-E (250000).
           DISPLAY "E250000=" WS-E (250000).
           DISPLAY "KEEP=" WS-E (100000).
           STOP RUN.
