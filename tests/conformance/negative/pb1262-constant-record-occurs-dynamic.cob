      *> reject-at: 2014 2023
      *> kb/Work PB1262 - SR33 - a dynamic-capacity table (OCCURS DYNAMIC) subordinate to a CONSTANT RECORD.
      *>   cite.py --check 13.18.38.3 "The dynamic-capacity-table format of the OCCURS clause shall not be specified in any data item described with the CONSTANT RECORD clause" -> OK
      *> COBOLNET1549 (constant-record-rule). The positive twin is conformance/2002/pb1262_constant_record_fixed_table.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1262N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C CONSTANT RECORD.
          05 T PIC X OCCURS DYNAMIC FROM 2 VALUE "A".
       PROCEDURE DIVISION.
           DISPLAY C.
           STOP RUN.
