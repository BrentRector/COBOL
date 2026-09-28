      *> reject-at: 85
      *> kb/Work PB1174 — THE EDITION FLOOR OF THE FORMAT-2 TABLE SORT over an OCCURS DEPENDING table.
      *>   cite.py --check 14.9.40.4 "The number of occurrences of table elements referenced by data-name-2 is
      *>     determined by the rules in the OCCURS clause" -> OK §14.9.40.4 20)
      *> SORT data-name-2 (§14.9.40 Format 2) is a COBOL-2002 introduction: COBOL-85's SORT has only the
      *> file format, so at --std 85 the statement is refused. The data description (an OCCURS DEPENDING table)
      *> is legal COBOL-85; the SORT of it is the one 2002+ construct here. The positive half is
      *> 2002/w70c_pb1174_table_sort_odo_count.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W70CN85S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 3.
       01 TBL.
          05 ITM PIC 9(2) OCCURS 1 TO 5 TIMES DEPENDING ON N.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 30 TO ITM(1) MOVE 10 TO ITM(2) MOVE 50 TO ITM(3)
           SORT ITM ASCENDING
           DISPLAY ITM(1)
           STOP RUN.
