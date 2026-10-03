      *> kb/Work PB1055 — A FORMAT-2 TABLE SORT OF A DYNAMIC-CAPACITY TABLE THAT LIES INSIDE ANOTHER TABLE.
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 14.9.40.4 "The number of occurrences of table elements referenced by data-name-2 is
      *>     determined by the rules in the OCCURS clause." -> OK §14.9.40.4 20)
      *>   cite.py --check 8.4.2.3.3 "as the rightmost or only subscript of a table in the table format of a SORT
      *>     statement. This is equivalent to omitting the rightmost or only subscript in this context." -> OK
      *>     §8.4.2.3.3 6)
      *> OCCURS DYNAMIC is a COBOL-2014 construct. The subject DX lies in DROW OCCURS 2, so SORT DX(2) sorts DROW(2)'s
      *> table, whose current number of occurrences is that occurrence's OWN capacity (each outer occurrence holds its
      *> own dynamic table) and nothing past it.
      *> WHY THE LEG CAN FAIL (expected value derived from the rules): DROW(2) is filled to its capacity FROM 3 with
      *> 5, 3, 4, and SORT DX(2) ASCENDING leaves 3 4 5. A sort that took the capacity of the wrong outer occurrence,
      *> or none, would sort a different table or nothing at all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1055DY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DT.
          05 DROW OCCURS 2.
             10 DX PIC 9 OCCURS DYNAMIC CAPACITY IN DCAP FROM 3 TO 6.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 5 TO DX(2, 1)
           MOVE 3 TO DX(2, 2)
           MOVE 4 TO DX(2, 3)
           SORT DX(2) ASCENDING
           DISPLAY "A " DX(2, 1) DX(2, 2) DX(2, 3)
           STOP RUN.
