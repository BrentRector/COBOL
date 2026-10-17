*>--- conformance-case
*> id: table_value_occurs
*> rules: [GR-13.18.63.4-9]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §13.18.63.4 GR 9 — a VALUE clause on (or subordinate to) an OCCURS clause initializes EVERY
      *> occurrence to the value (the COBOL-2002 table-format VALUE clause; COBOL-85 prohibited it). Covers a
      *> numeric element, a signed/scaled element, and a character element — every occurrence must carry the value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. TBLVALUE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 N PIC 9(3)    OCCURS 3 VALUE 7.
          05 D PIC S9(2)V9 OCCURS 2 VALUE 1.5.
          05 C PIC X(3)    OCCURS 2 VALUE "AB".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "N=" N(1) "," N(2) "," N(3).
           DISPLAY "D=" D(1) "," D(2).
           DISPLAY "C=" C(1) "," C(2).
           STOP RUN.
