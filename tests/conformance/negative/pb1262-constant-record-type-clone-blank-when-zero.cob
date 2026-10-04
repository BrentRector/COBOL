      *> reject-at: 2002 2014 2023
      *> kb/Work PB1262 - SR13 reaches an item a TYPE clause brings into the CONSTANT RECORD (BLANK WHEN ZERO written in the TYPEDEF).
      *>   cite.py --check 13.16.3 "The ANY LENGTH, BASED, BLANK WHEN ZERO, DYNAMIC LENGTH, select-when, SYNCHRONIZED, and TYPEDEF clauses and validation-clauses" -> OK
      *> COBOLNET1549 (constant-record-rule). The positive twin is conformance/2002/pb1262_constant_record_fixed_table.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1262N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT TYPEDEF.
          05 T PIC 9 BLANK WHEN ZERO.
       01 C CONSTANT RECORD.
          05 X TYPE TT.
       PROCEDURE DIVISION.
           DISPLAY C.
           STOP RUN.
