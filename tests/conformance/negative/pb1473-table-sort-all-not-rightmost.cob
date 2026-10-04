      *> reject-at: 2002 2014 2023
      *> kb/Work PB1473 - THE SUBSCRIPT ALL OF A TABLE SORT'S SUBJECT STANDS ONLY AS ITS RIGHTMOST OR ONLY SUBSCRIPT.
      *>   cite.py --check 8.4.2.3.3 "as the rightmost or only subscript of a table in the table format of a SORT
      *>     statement. This is equivalent to omitting the rightmost or only subscript in this context." -> OK
      *>     §8.4.2.3.3 6)
      *>   cite.py --check 14.9.40.3 "Subscripting shall be specified in accordance with 8.4.2.3, Subscripts."
      *>     -> OK §14.9.40.3 13)
      *> E2 lies in R2 OCCURS 2, so `SORT E2(1, ALL)` is the legal spelling (the legal twin is
      *> 2002/pb1055_table_sort_nested_subject, leg 2) while ALL in the FIRST of two positions names no table the
      *> statement could sort. Each statement is refused (COBOLNET2363); with the nested-subject binding landed
      *> (kb/Work PB1055) an unscreened ALL would have compiled and sorted, which is why the position is pinned here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1473NG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T2.
          05 R2 OCCURS 2.
             10 E2 PIC 9 OCCURS 3.
       01 T3.
          05 P3 OCCURS 2.
             10 Q3 OCCURS 2.
                15 E3 PIC 9 OCCURS 2.
       PROCEDURE DIVISION.
       MAIN.
           SORT E2(ALL, 1) DESCENDING
           SORT E3(ALL, 1, 2) ASCENDING
           SORT E3(1, ALL, 2) ASCENDING
           STOP RUN.
