      *> reject-at: 2002 2014 2023
      *> kb/Work PB1262 - SR19 reaches an item a TYPE clause brings into the CONSTANT RECORD (the composed entry, not only the written one).
      *>   cite.py --check 13.18.38.3 "A format 2 OCCURS clause shall not be specified in any data item subordinate to a data item described with the CONSTANT RECORD clause" -> OK
      *> COBOLNET1549 (constant-record-rule). The positive twin is conformance/2002/pb1262_constant_record_fixed_table.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1262N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 2.
       01 TT TYPEDEF.
          05 T PIC X OCCURS 1 TO 3 DEPENDING ON N.
       01 C CONSTANT RECORD.
          05 X TYPE TT.
       PROCEDURE DIVISION.
           DISPLAY C.
           STOP RUN.
