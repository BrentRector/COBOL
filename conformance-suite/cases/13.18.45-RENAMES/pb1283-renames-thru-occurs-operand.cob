*>--- conformance-case
*> id: pb1283-renames-thru-occurs-operand
*> rules: [SR-13.18.45.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1283 - SR3 - data-name-3 carries an OCCURS clause.
      *>   cite.py --check 13.18.45.3 "Data-name-1, data-name-2 and data-name-3 shall not be subject to any OCCURS clauses" -> OK
      *> COBOLNET2740 (renames-entry-rule). The positive twin is conformance/85/pb1283_renames_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1283N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 B PIC X VALUE "C".
          05 C PIC X OCCURS 3 VALUE "D".
       66 X1 RENAMES B THRU C.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
