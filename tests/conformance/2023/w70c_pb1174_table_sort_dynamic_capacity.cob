      *> kb/Work PB1174 (sibling sweep) — THE FORMAT-2 TABLE SORT OF A DYNAMIC-CAPACITY TABLE.
      *>
      *> THE RULES, --check validated:
      *>   cite.py --check 14.9.40.3 "Data-name-2 shall have an OCCURS clause in its data description entry"
      *>     -> OK §14.9.40.3 13) — an OCCURS DYNAMIC clause (§13.18.38 Format 4) IS an OCCURS clause, so the
      *>     table is a legal data-name-2. The pre-PB1174 binder asked for a FIXED integer and refused it
      *>     (COBOLNET1757 "neither a SELECTed/SD file nor an OCCURS table").
      *>   cite.py --check 14.9.40.4 "The number of occurrences of table elements referenced by data-name-2 is
      *>     determined by the rules in the OCCURS clause" -> OK §14.9.40.4 20) — for a dynamic-capacity table
      *>     that is its CURRENT capacity (§8.5.1.9.1), never the store behind it.
      *> Dynamic-capacity tables are a COBOL-2023 introduction, so the construct has no earlier-edition copy.
      *>
      *> WHY EACH LEG CAN FAIL:
      *>   1  Capacity 3 after three receiving references -> 10 20 30 (ascending), CAPACITY still 3: the sort
      *>      neither grows nor shrinks the table.
      *>   2  DESCENDING over the same three               -> 30 20 10.
      *>   3  A fourth occurrence created by R(4) = 25 and sorted ASCENDING -> 10 20 25 30, capacity 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W70CDYNSRT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 R PIC 99 OCCURS DYNAMIC CAPACITY IN RCAP FROM 1 TO 9.
       PROCEDURE DIVISION.
           MOVE 30 TO R(1) MOVE 10 TO R(2) MOVE 20 TO R(3)
           SORT R ASCENDING
           DISPLAY "1 " RCAP " " R(1) " " R(2) " " R(3)
           SORT R DESCENDING
           DISPLAY "2 " R(1) " " R(2) " " R(3)
           MOVE 25 TO R(4)
           SORT R ASCENDING
           DISPLAY "3 " RCAP " " R(1) " " R(2) " " R(3) " " R(4)
           STOP RUN.
