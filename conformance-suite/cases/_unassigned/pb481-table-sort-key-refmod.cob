*>--- conformance-case
*> id: pb481-table-sort-key-refmod
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB481 - ISO 14.9.40.2 Format 2 (the table SORT, a 2002 introduction) prints KEY [ data-name-1 ] ...;
*> 14.9.40.3 SR14 b): "Key data names shall not be subscripted", and 8.4.3.3.3's NOTE forbids the
*> reference-modifier. The capture kept the base name and dropped the (4:3), so this sorted on all of K and
*> displayed AAACCC BBBZZZ CCCAAA where a key of positions 4-6 would have ordered CCCAAA AAACCC BBBZZZ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB481N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 E OCCURS 3 TIMES.
             10 K PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "CCCAAA" TO K(1)
           MOVE "BBBZZZ" TO K(2)
           MOVE "AAACCC" TO K(3)
           SORT E ON ASCENDING KEY K(4:3).
           DISPLAY K(1) " " K(2) " " K(3).
           STOP RUN.
