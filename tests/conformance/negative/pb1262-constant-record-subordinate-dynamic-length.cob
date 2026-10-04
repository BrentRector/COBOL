      *> reject-at: 2014 2023
      *> kb/Work PB1262 - SR13 names DYNAMIC LENGTH among the clauses barred from a subordinate entry (the subordinate screen listed five of the eight).
      *>   cite.py --check 13.16.3 "The ANY LENGTH, BASED, BLANK WHEN ZERO, DYNAMIC LENGTH, select-when, SYNCHRONIZED, and TYPEDEF clauses and validation-clauses" -> OK
      *> COBOLNET1549 (constant-record-rule). The positive twin is conformance/2002/pb1262_constant_record_fixed_table.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1262N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C CONSTANT RECORD.
          05 X PIC X VALUE "A" DYNAMIC LENGTH.
       PROCEDURE DIVISION.
           DISPLAY C.
           STOP RUN.
