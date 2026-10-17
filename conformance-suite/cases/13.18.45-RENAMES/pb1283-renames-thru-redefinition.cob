*>--- conformance-case
*> id: pb1283-renames-thru-redefinition
*> rules: [SR-13.18.45.3-11]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1283 - SR11 - data-name-3 (B) is a SHORTER redefinition of data-name-2 (A): its end does not follow A's end.
      *>   cite.py --check 13.18.45.3 "The beginning of the storage area described by data-name-3 shall not precede the beginning of the storage area described by data-name-2" -> OK
      *> COBOLNET2740 (renames-entry-rule). The positive twin is conformance/85/pb1283_renames_legal.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1283N8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R.
          05 A PIC X(4) VALUE "abcd".
          05 B REDEFINES A PIC X(2).
          05 C PIC X(2) VALUE "ef".
       66 X1 RENAMES A THRU B.
       PROCEDURE DIVISION.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
