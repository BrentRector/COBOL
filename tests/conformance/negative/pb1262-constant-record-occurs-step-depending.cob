      *> reject-at: 2002 2014 2023
      *> kb/Work PB1262 - SR23 - the DEPENDING phrase in its Format 3 (STEP) spelling, which the data division parses, subordinate to a CONSTANT RECORD.
      *>   cite.py --check 13.18.38.3 "The DEPENDING phrase shall not be specified in any data item subordinate to a data item described with the CONSTANT RECORD clause" -> OK
      *> COBOLNET1549 (constant-record-rule). The positive twin is conformance/2002/pb1262_constant_record_fixed_table.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1262N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 2.
       01 C CONSTANT RECORD.
          05 T PIC X VALUE "A" OCCURS 1 TO 5 DEPENDING ON N STEP 1.
       PROCEDURE DIVISION.
           DISPLAY C.
           STOP RUN.
