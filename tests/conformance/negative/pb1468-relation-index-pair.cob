      *> reject-at: 85 2002 2014 2023
      *> ISO 8.8.4.2.13: "Relation tests may be made only between 1) two index-names ... 2) an index-name and a
      *> numeric data item or numeric literal ... 3) an index data item and an index-name or another index data
      *> item." An index-name against an alphanumeric item is none of the three (COBOLNET2533); before kb/Work
      *> PB1468 it compiled clean and aborted the run unit ("computed expression in a string context").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1468-INDEX-PAIR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 E1 PIC X(4) OCCURS 5 INDEXED BY I1.
       01 XA PIC X VALUE "3".
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET I1 TO 3.
           IF I1 = XA DISPLAY "Y" ELSE DISPLAY "N" END-IF
           STOP RUN.
