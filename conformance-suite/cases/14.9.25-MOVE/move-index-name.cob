*>--- conformance-case
*> id: move-index-name
*> rules: [SR-14.9.25.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R16 - MOVE is none of 13.18.38.3 r7's five index-name contexts; the numeric-receiver
      *> form SILENTLY computed before (the same judgment COBOLNET0809 applies to class-index DATA items).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R16NEGM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB.
          05 TE OCCURS 3 TIMES INDEXED BY IX.
             10 TK PIC X(3).
       01 N PIC 9(4).
       PROCEDURE DIVISION.
           SET IX TO 2.
           MOVE IX TO N.
           STOP RUN.
