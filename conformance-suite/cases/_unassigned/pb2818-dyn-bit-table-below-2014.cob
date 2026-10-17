*>--- conformance-case
*> id: pb2818-dyn-bit-table-below-2014
*> rules: []
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2818 - a dynamic-capacity table of USAGE BIT elements.
      *> The OCCURS DYNAMIC CAPACITY clause and DYNAMIC LENGTH are COBOL-
      *> 2014 additions (8.5.1.9 / 8.5.1.10), and USAGE BIT / PICTURE 1 are
      *> COBOL-2002's, so below 2014 the group is refused by the edition
      *> gate (COBOLNET0900) before any layout question arises.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2818OLD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 A PIC 1 USAGE BIT VALUE B"1".
          05 T PIC 1 USAGE BIT OCCURS DYNAMIC CAPACITY IN C1.
          05 D PIC X DYNAMIC LENGTH LIMIT 5.
       PROCEDURE DIVISION.
       MAIN.
           MOVE B"1" TO T (1)
           DISPLAY C1
           STOP RUN.
