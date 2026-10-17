*>--- conformance-case
*> id: sort_table_format2
*> rules: [GR-14.9.40.4-23, GR-14.9.40.4-24]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.40 Format 2 — COBOL-2002 table SORT (sort a table in place). The "self-key" form sorts
      *> the elements of an OCCURS table on the elements themselves (ASCENDING/DESCENDING), with no input/
      *> output procedures or files. (Backfill regression test for the landed feature, DEVLOG 353.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SORTTBL2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 ITM PIC 9(2) OCCURS 5 TIMES.
       01 I PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 30 TO ITM(1).
           MOVE 10 TO ITM(2).
           MOVE 50 TO ITM(3).
           MOVE 20 TO ITM(4).
           MOVE 40 TO ITM(5).
           SORT ITM ASCENDING.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 5
               DISPLAY "ASC" I "=" ITM(I)
           END-PERFORM.
           SORT ITM DESCENDING.
           DISPLAY "TOP=" ITM(1) " BOT=" ITM(5).
           STOP RUN.
